using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.DAL.Contexts;
using Platform.DAL.Models;

namespace Platform.DAL.Repositories.Disasters;

public sealed class DisasterCommandStore(DisasterDbContext db)
{
    public async Task<(DisasterCommandReceipt? Receipt, bool Conflict)> AcceptAsync(Guid id, string command, int capacity, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(6042, 1)", ct);
        var existing = await db.Commands.AsNoTracking().SingleOrDefaultAsync(x => x.ClientRequestId == id, ct);
        if (existing is not null)
            return (existing, !JsonElement.DeepEquals(JsonSerializer.Deserialize<JsonElement>(existing.CommandJson), JsonSerializer.Deserialize<JsonElement>(command)));
        if (await db.Commands.CountAsync(x => x.ResultJson == null, ct) >= capacity) return (null, false);
        var receipt = new DisasterCommandReceipt { ClientRequestId = id, CommandJson = command, ReceivedAt = DateTimeOffset.UtcNow };
        db.Commands.Add(receipt); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return (receipt, false);
    }

    public Task<DisasterCommandReceipt?> GetAsync(Guid id, CancellationToken ct) => db.Commands.AsNoTracking().SingleOrDefaultAsync(x => x.ClientRequestId == id, ct);

    public Task<int> FinishAsync(Guid id, string result, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync(
        $"UPDATE disasters.\"DisasterCommandReceipts\" SET \"ResultJson\"={result}::jsonb WHERE \"ClientRequestId\"={id} AND (\"ResultJson\" IS NULL OR \"ResultJson\"->>'status'='RetryableFailure')", ct);

    public async Task<IReadOnlyList<DisasterCommandReceipt>> ClaimCommandsAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var rows = await db.Commands.FromSqlRaw("SELECT * FROM disasters.\"DisasterCommandReceipts\" WHERE \"PublishedAt\" IS NULL AND \"ResultJson\" IS NULL AND (\"LeaseUntil\" IS NULL OR \"LeaseUntil\" < CURRENT_TIMESTAMP) ORDER BY \"ReceivedAt\" LIMIT 1 FOR UPDATE SKIP LOCKED").ToListAsync(ct);
        foreach (var row in rows) { row.LeaseId = Guid.NewGuid(); row.LeaseUntil = DateTimeOffset.UtcNow.AddSeconds(30); row.Attempts++; }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return rows;
    }
    public async Task<IReadOnlyList<DisasterOutboxMessage>> ClaimEventsAsync(CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var rows = await db.Outbox.FromSqlRaw("SELECT * FROM disasters.\"DisasterOutboxMessages\" WHERE \"PublishedAt\" IS NULL AND (\"LeaseUntil\" IS NULL OR \"LeaseUntil\" < CURRENT_TIMESTAMP) ORDER BY \"OccurredAt\" LIMIT 1 FOR UPDATE SKIP LOCKED").ToListAsync(ct);
        foreach (var row in rows) { row.LeaseId = Guid.NewGuid(); row.LeaseUntil = DateTimeOffset.UtcNow.AddSeconds(30); row.Attempts++; }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return rows;
    }
    public Task<int> ConfirmCommandAsync(Guid id, Guid? lease, CancellationToken ct) => db.Commands.Where(x => x.ClientRequestId == id && x.LeaseId == lease)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.PublishedAt, DateTimeOffset.UtcNow).SetProperty(x => x.LeaseUntil, (DateTimeOffset?)null), ct);
    public Task<int> ConfirmEventAsync(Guid id, Guid? lease, CancellationToken ct) => db.Outbox.Where(x => x.Id == id && x.LeaseId == lease)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.PublishedAt, DateTimeOffset.UtcNow).SetProperty(x => x.LeaseUntil, (DateTimeOffset?)null), ct);
}
