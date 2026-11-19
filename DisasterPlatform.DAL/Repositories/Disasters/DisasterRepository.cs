using System.Text.Json;
using DisasterLocation.Core;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Platform.DAL.Contexts;
using Platform.DAL.Models;

namespace Platform.DAL.Repositories.Disasters;

public sealed class DisasterRepository(DisasterDbContext context) : IDisasterRepository
{
    public async Task<DisasterSyncPage> GetChangesAsync(long after, int limit, CancellationToken ct = default)
    {
        var rows = await context.Disasters.AsNoTracking().Where(d => EF.Property<long>(d, "SyncSequence") > after)
            .OrderBy(d => EF.Property<long>(d, "SyncSequence")).Take(limit + 1)
            .Select(d => new { Disaster = d, Sequence = EF.Property<long>(d, "SyncSequence") }).ToListAsync(ct);
        var more = rows.Count > limit;
        var page = rows.Take(limit).ToArray();
        return new DisasterSyncPage(page.Select(x => x.Disaster).ToArray(), page.Length == 0 ? after : page[^1].Sequence, more);
    }

    public async Task<IReadOnlyList<Disaster>> GetAllAsync(CancellationToken ct = default) =>
        await context.Disasters.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(ct);
    public Task<Disaster?> GetByIdAsync(int id, CancellationToken ct = default) =>
        context.Disasters.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Disaster?> GetByClientRequestIdAsync(Guid id, CancellationToken ct = default) =>
        context.Disasters.AsNoTracking().SingleOrDefaultAsync(x => x.ClientRequestId == id, ct);

    public async Task<IReadOnlyList<DisasterType>> GetTypesAsync(CancellationToken ct = default) =>
        await context.DisasterTypes.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);
    public async Task<DisasterType?> AddTypeAsync(string name, CancellationToken ct)
    {
        var type = new DisasterType { Name = name };
        context.DisasterTypes.Add(type);
        try { await context.SaveChangesAsync(ct); return type; }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, SchemaName: PlatformDatabase.DisasterSchema,
                ConstraintName: "IX_DisasterTypes_Name" })
        {
            context.Entry(type).State = EntityState.Detached;
            return null;
        }
    }
    public async Task<bool> DeactivateTypeAsync(int id, CancellationToken ct) =>
        await context.DisasterTypes.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), ct) != 0;

    public async Task<IReadOnlyList<DisasterHistoryEntry>> GetHistoryAsync(int id, CancellationToken ct) =>
        await context.PhaseAudit.AsNoTracking().Where(x => x.DisasterId == id).OrderBy(x => x.Version)
            .Select(x => new DisasterHistoryEntry(x.Id, x.Actor, x.OccurredAt, x.Version,
                x.Before, x.After, x.Operation, x.Result, x.Changes, x.Reason, x.CommitmentsConfirmed)).ToListAsync(ct);

    public async Task<bool> ChangePhaseAsync(int id, DisasterPhase previous, DisasterPhase phase, long expectedVersion,
        string actor, CancellationToken ct, string? reason = null, bool? commitmentsConfirmed = null)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var before = await GetByIdAsync(id, ct);
        if (before is null || before.Version != expectedVersion || before.Phase != previous) return false;
        var now = PlatformDatabase.Timestamp(DateTimeOffset.UtcNow);
        var affected = await context.Disasters.Where(d => d.Id == id && d.Version == expectedVersion)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Phase, phase).SetProperty(d => d.Version, expectedVersion + 1)
                .SetProperty(d => d.UpdatedAt, now), ct);
        if (affected == 0) return false;
        var after = await GetByIdAsync(id, ct);
        var audit = Audit(before, after!, actor, "PhaseChanged", now);
        audit.Reason = reason;
        audit.CommitmentsConfirmed = commitmentsConfirmed;
        context.PhaseAudit.Add(audit);
        Enqueue(after!, "DisasterPhaseChanged", now);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<bool> UpdateAsync(Disaster disaster, long expectedVersion, string actor, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var before = await GetByIdAsync(disaster.Id, ct);
        if (before is null || before.Version != expectedVersion) return false;
        var now = PlatformDatabase.Timestamp(DateTimeOffset.UtcNow);
        var affected = await context.Disasters.Where(d => d.Id == disaster.Id && d.Version == expectedVersion)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Nombre, disaster.Nombre).SetProperty(d => d.Ubicacion, disaster.Ubicacion)
                .SetProperty(d => d.Severidad, disaster.Severidad).SetProperty(d => d.Descripcion, disaster.Descripcion)
                .SetProperty(d => d.Latitud, disaster.Latitud).SetProperty(d => d.Longitud, disaster.Longitud)
                .SetProperty(d => d.Radio, disaster.Radio).SetProperty(d => d.DisasterTypeId, disaster.DisasterTypeId)
                .SetProperty(d => d.StartedAt, disaster.StartedAt).SetProperty(d => d.AttentionLevel, disaster.AttentionLevel)
                .SetProperty(d => d.UpdatedAt, now).SetProperty(d => d.Version, expectedVersion + 1), ct);
        if (affected == 0) return false;
        var after = await GetByIdAsync(disaster.Id, ct);
        context.PhaseAudit.Add(Audit(before, after!, actor, "Updated", now));
        if (before.AttentionLevel != after!.AttentionLevel) Enqueue(after, "DisasterAttentionChanged", now);
        if (before.Latitud != after.Latitud || before.Longitud != after.Longitud || before.Radio != after.Radio)
            Enqueue(after, "DisasterAreaChanged", now);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private static string Snapshot(Disaster d) => JsonSerializer.Serialize(new
    {
        d.Nombre, d.Ubicacion, d.Severidad, d.Descripcion, d.Latitud, d.Longitud, d.Radio,
        d.DisasterTypeId, d.StartedAt, d.RegisteredAt, d.UpdatedAt, d.AttentionLevel, d.Phase, d.Version,
        d.RegisteringOrganizationId, d.EmergencyResponsibleId, d.GeographyJson, d.OrganizationsJson
    });
    private static DisasterPhaseAudit Audit(Disaster before, Disaster after, string actor, string operation, DateTimeOffset now)
    {
        using var oldValues = JsonDocument.Parse(Snapshot(before));
        using var newValues = JsonDocument.Parse(Snapshot(after));
        var changes = new Dictionary<string, object>();
        foreach (var property in newValues.RootElement.EnumerateObject())
        {
            var old = oldValues.RootElement.GetProperty(property.Name);
            if (old.GetRawText() != property.Value.GetRawText()) changes[property.Name] = new { Before = old, After = property.Value };
        }
        return new DisasterPhaseAudit { Id = Guid.NewGuid(), DisasterId = after.Id, Before = before.Phase,
            After = after.Phase, Version = after.Version, Actor = actor, OccurredAt = now,
            Operation = operation, Changes = JsonSerializer.Serialize(changes) };
    }

    public Task<DisasterReport?> GetReportBySourceAsync(string sourceId, string reportId, CancellationToken ct) =>
        context.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.SourceId == sourceId && x.SourceReportId == reportId, ct);

    public Task<DisasterReport?> GetReportAsync(Guid id, CancellationToken ct) =>
        context.Reports.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<DisasterReport>> GetReportsAsync(int? disasterId, CancellationToken ct) =>
        await context.Reports.AsNoTracking().Where(x => x.DisasterId == disasterId)
            .OrderByDescending(x => x.ReceivedAt).ToListAsync(ct);

    public async Task<DisasterReportReceipt> ReceiveReportAsync(DisasterReport report, Disaster? preliminary, CancellationToken ct)
    {
        var existing = await GetReportBySourceAsync(report.SourceId, report.SourceReportId, ct);
        if (existing is not null) return new(existing, false);
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            if (report.SourceId == "manual-web" && Guid.TryParse(report.SourceReportId, out var identity))
            {
                await LockIdentityAsync(identity, ct);
                if (await GetByClientRequestIdAsync(identity, ct) is not null) throw new System.Data.DBConcurrencyException();
                existing = await GetReportBySourceAsync(report.SourceId, report.SourceReportId, ct);
                if (existing is not null) return new(existing, false);
            }
            if (preliminary is not null)
            {
                preliminary.CreationSnapshot = Snapshot(preliminary);
                context.Disasters.Add(preliminary);
                await context.SaveChangesAsync(ct);
                report.DisasterId = preliminary.Id;
                var now = preliminary.RegisteredAt!.Value;
                Enqueue(preliminary, "DisasterDetected", now, report.SourceId);
                context.PhaseAudit.Add(new DisasterPhaseAudit { Id = Guid.NewGuid(), DisasterId = preliminary.Id,
                    Before = preliminary.Phase, After = preliminary.Phase, Actor = report.ReceivedBy,
                    OccurredAt = now, Operation = "Detected", Version = preliminary.Version,
                    Changes = JsonSerializer.Serialize(new { SourceId = report.SourceId, ReportId = report.Id, After = preliminary }) });
            }
            context.Reports.Add(report);
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new(report, true);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, SchemaName: PlatformDatabase.DisasterSchema,
                ConstraintName: "IX_DisasterReports_SourceId_SourceReportId" })
        {
            await transaction.RollbackAsync(ct);
            context.ChangeTracker.Clear();
            existing = await context.Reports.AsNoTracking().SingleAsync(x => x.SourceId == report.SourceId && x.SourceReportId == report.SourceReportId, ct);
            return new(existing, false);
        }
    }

    public async Task<bool> AssociateReportAsync(int disasterId, Guid reportId, long expectedVersion, string actor, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var before = await GetByIdAsync(disasterId, ct);
        if (before is null || before.Version != expectedVersion) return false;
        var now = PlatformDatabase.Timestamp(DateTimeOffset.UtcNow);
        if (await context.Disasters.Where(x => x.Id == disasterId && x.Version == expectedVersion)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Version, expectedVersion + 1).SetProperty(x => x.UpdatedAt, now), ct) == 0) return false;
        if (await context.Reports.Where(x => x.Id == reportId && x.DisasterId == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.DisasterId, disasterId), ct) == 0) return false;
        var reports = await context.Reports.AsNoTracking().Where(x => x.DisasterId == disasterId)
            .OrderBy(x => x.ReceivedAt).ThenBy(x => x.Id).Select(x => new { x.Id, x.OriginalContent }).ToListAsync(ct);
        var points = new List<(double Latitude, double Longitude)>();
        if (before.CreationSnapshot is not null) AddPoint(before.CreationSnapshot, points);
        foreach (var report in reports) AddPoint(report.OriginalContent, points);
        var incoming = new List<(double Latitude, double Longitude)>();
        AddPoint(reports.Single(r => r.Id == reportId).OriginalContent, incoming);
        var area = incoming.Count == 0 ? null : DisasterArea.FromPoints(points);
        if (area is not null)
        {
            var previous = before.GeographyJson is null ? null : JsonSerializer.Deserialize<DisasterGeography>(before.GeographyJson);
            var geography = JsonSerializer.Serialize(new DisasterGeography(area.Polygon, previous?.Municipalities,
                previous?.Neighborhoods, previous?.RiskZones));
            if (System.Text.Encoding.UTF8.GetByteCount(geography) > 65_536)
                throw new ArgumentOutOfRangeException(nameof(reports), "La geografía supera 64 KiB.");
            await context.Disasters.Where(x => x.Id == disasterId).ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Latitud, area.Latitude).SetProperty(x => x.Longitud, area.Longitude)
                .SetProperty(x => x.Radio, area.Radius).SetProperty(x => x.GeographyJson, geography), ct);
        }
        var after = await GetByIdAsync(disasterId, ct);
        var audit = Audit(before, after!, actor, "ReportAssociated", now);
        var changes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(audit.Changes!)!;
        changes["ReportId"] = JsonSerializer.SerializeToElement(reportId);
        audit.Changes = JsonSerializer.Serialize(changes);
        context.PhaseAudit.Add(audit);
        if (area is not null) Enqueue(after!, "DisasterAreaChanged", now);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private static void AddPoint(string content, List<(double Latitude, double Longitude)> points)
    {
        using var document = JsonDocument.Parse(content);
        var fields = document.RootElement;
        if (fields.ValueKind != JsonValueKind.Object) return;
        JsonElement latitude = default, longitude = default;
        foreach (var property in fields.EnumerateObject())
        {
            if (property.Name.Equals("latitud", StringComparison.OrdinalIgnoreCase)) latitude = property.Value;
            if (property.Name.Equals("longitud", StringComparison.OrdinalIgnoreCase)) longitude = property.Value;
        }
        if (latitude.ValueKind == JsonValueKind.Number && longitude.ValueKind == JsonValueKind.Number
            && latitude.TryGetDouble(out var lat) && longitude.TryGetDouble(out var lng)
            && double.IsFinite(lat) && double.IsFinite(lng) && lat is >= -90 and <= 90 && lng is >= -180 and <= 180)
            points.Add((lat, lng));
    }

    public async Task<bool> UpdateContextAsync(int id, long expectedVersion, string actor, string operation,
        string? geographyJson, string? organizationsJson, string? registeringOrganizationId,
        string? emergencyResponsibleId, string? sourceId, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var before = await GetByIdAsync(id, ct);
        if (before is null || before.Version != expectedVersion) return false;
        var now = PlatformDatabase.Timestamp(DateTimeOffset.UtcNow);
        var query = context.Disasters.Where(x => x.Id == id && x.Version == expectedVersion);
        var affected = operation == "GeographyUpdated"
            ? await query.ExecuteUpdateAsync(s => s.SetProperty(x => x.GeographyJson, geographyJson)
                .SetProperty(x => x.Version, expectedVersion + 1).SetProperty(x => x.UpdatedAt, now), ct)
            : await query.ExecuteUpdateAsync(s => s.SetProperty(x => x.OrganizationsJson, organizationsJson)
                .SetProperty(x => x.RegisteringOrganizationId, registeringOrganizationId)
                .SetProperty(x => x.EmergencyResponsibleId, emergencyResponsibleId)
                .SetProperty(x => x.Version, expectedVersion + 1).SetProperty(x => x.UpdatedAt, now), ct);
        if (affected == 0) return false;
        var after = await GetByIdAsync(id, ct);
        var audit = Audit(before, after!, actor, operation, now);
        audit.Reason = sourceId;
        context.PhaseAudit.Add(audit);
        if (operation == "GeographyUpdated") Enqueue(after!, "DisasterAreaChanged", now, sourceId);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<DisasterAlert>> GetAlertsAsync(int id, CancellationToken ct) =>
        await context.Outbox.AsNoTracking().Where(x => x.DisasterId == id).OrderBy(x => x.Version)
            .Select(x => new DisasterAlert(x.Id, x.EventType, x.OccurredAt, x.Version, x.Payload)).ToListAsync(ct);

    private void Enqueue(Disaster disaster, string eventType, DateTimeOffset now, string? sourceId = null) =>
        context.Outbox.Add(new DisasterOutboxMessage { Id = Guid.NewGuid(), DisasterId = disaster.Id,
            EventType = eventType, Version = disaster.Version, OccurredAt = now, Payload = JsonSerializer.Serialize(new { Disaster = JsonSerializer.Deserialize<JsonElement>(Snapshot(disaster)), SourceId = sourceId }) });

    private async Task LockIdentityAsync(Guid identity, CancellationToken ct) =>
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"disaster-operation:" + identity.ToString()}, 0))", ct);

    public async Task<DisasterSaveResult> AddAsync(Disaster disaster, CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        if (disaster.ClientRequestId is Guid requestId)
        {
            await LockIdentityAsync(requestId, ct);
            if (await GetReportBySourceAsync("manual-web", requestId.ToString(), ct) is not null)
                throw new System.Data.DBConcurrencyException();
            var existing = await GetByClientRequestIdAsync(requestId, ct);
            if (existing is not null) return new DisasterSaveResult(existing, false);
        }
        disaster.CreationSnapshot = Snapshot(disaster);
        context.Disasters.Add(disaster);
        try
        {
            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new DisasterSaveResult(disaster, true);
        }
        catch (DbUpdateException ex) when (disaster.ClientRequestId.HasValue &&
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation,
                SchemaName: PlatformDatabase.DisasterSchema, ConstraintName: "IX_Desastres_ClientRequestId" })
        {
            await transaction.RollbackAsync(ct);
            context.Entry(disaster).State = EntityState.Detached;
            var existing = await GetByClientRequestIdAsync(disaster.ClientRequestId.Value, ct);
            if (existing is null) throw;
            return new DisasterSaveResult(existing, false);
        }
    }
}
