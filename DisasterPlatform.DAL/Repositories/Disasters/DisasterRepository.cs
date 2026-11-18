using System.Text.Json;
using DisasterLocation.Core;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Platform.DAL.Contexts;
using Platform.DAL.Models;

namespace Platform.DAL.Repositories.Disasters;

public sealed class DisasterRepository(DisasterDbContext context) : IDisasterRepository
{
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
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private static string Snapshot(Disaster d) => JsonSerializer.Serialize(new
    {
        d.Nombre, d.Ubicacion, d.Severidad, d.Descripcion, d.Latitud, d.Longitud, d.Radio,
        d.DisasterTypeId, d.StartedAt, d.RegisteredAt, d.UpdatedAt, d.AttentionLevel, d.Phase, d.Version
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

    public async Task<DisasterSaveResult> AddAsync(Disaster disaster, CancellationToken ct = default)
    {
        if (disaster.ClientRequestId is Guid requestId)
        {
            var existing = await GetByClientRequestIdAsync(requestId, ct);
            if (existing is not null) return new DisasterSaveResult(existing, false);
        }
        disaster.CreationSnapshot = Snapshot(disaster);
        context.Disasters.Add(disaster);
        try
        {
            await context.SaveChangesAsync(ct);
            return new DisasterSaveResult(disaster, true);
        }
        catch (DbUpdateException ex) when (disaster.ClientRequestId.HasValue &&
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation,
                SchemaName: PlatformDatabase.DisasterSchema, ConstraintName: "IX_Desastres_ClientRequestId" })
        {
            context.Entry(disaster).State = EntityState.Detached;
            var existing = await GetByClientRequestIdAsync(disaster.ClientRequestId.Value, ct);
            if (existing is null) throw;
            return new DisasterSaveResult(existing, false);
        }
    }
}
