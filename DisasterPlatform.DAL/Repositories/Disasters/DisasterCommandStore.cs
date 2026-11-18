using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.DAL.Contexts;
using Platform.DAL.Models;

namespace Platform.DAL.Repositories.Disasters;

public sealed class DisasterCommandStore(DisasterDbContext db)
{
    public async Task<(DisasterCommandReceipt? Receipt, bool Conflict)> AcceptAsync(Guid id, string command, int capacity, CancellationToken ct, int maxRecoveries = 3)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(6042, 1)", ct);
        var existing = await db.Commands.FromSqlInterpolated($"SELECT * FROM disasters.\"DisasterCommandReceipts\" WHERE \"ClientRequestId\"={id} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (!JsonElement.DeepEquals(JsonSerializer.Deserialize<JsonElement>(existing.CommandJson), JsonSerializer.Deserialize<JsonElement>(command)))
                return (existing, true);
            if (existing.ResultJson is not null && JsonSerializer.Deserialize<JsonElement>(existing.ResultJson).GetProperty("status").GetString() == "RetryableFailure"
                && existing.RecoveryCount < maxRecoveries && (existing.NextRecoveryAt is null || existing.NextRecoveryAt <= DateTimeOffset.UtcNow))
            {
                if (await db.Commands.CountAsync(x => x.ResultJson == null, ct) >= capacity) return (null, false);
                existing.ResultJson = null;
                existing.PublishedAt = null;
                existing.LeaseId = null;
                existing.LeaseUntil = null;
                existing.RecoveryCount++;
                await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct);
            return (existing, false);
        }
        if (await db.Commands.CountAsync(x => x.ResultJson == null, ct) >= capacity) return (null, false);
        var receipt = new DisasterCommandReceipt { ClientRequestId = id, CommandJson = command, ReceivedAt = DateTimeOffset.UtcNow };
        db.Commands.Add(receipt); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return (receipt, false);
    }

    public Task<DisasterCommandReceipt?> GetAsync(Guid id, CancellationToken ct) => db.Commands.AsNoTracking().SingleOrDefaultAsync(x => x.ClientRequestId == id, ct);

    public Task<int> FinishAsync(Guid id, string result, int recoveryCount, int recoveryDelaySeconds, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync(
        $"UPDATE disasters.\"DisasterCommandReceipts\" SET \"ResultJson\"={result}::jsonb, \"NextRecoveryAt\"=CURRENT_TIMESTAMP + ({recoveryDelaySeconds} * power(2, LEAST({recoveryCount}, 10))) * INTERVAL '1 second' WHERE \"ClientRequestId\"={id} AND \"RecoveryCount\"={recoveryCount} AND \"ResultJson\" IS NULL", ct);

    public Task<int> RenewCommandAsync(Guid id, Guid? lease, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync(
        $"UPDATE disasters.\"DisasterCommandReceipts\" SET \"LeaseUntil\"=CURRENT_TIMESTAMP + INTERVAL '30 seconds' WHERE \"ClientRequestId\"={id} AND \"LeaseId\"={lease} AND \"LeaseUntil\">CURRENT_TIMESTAMP AND \"ResultJson\" IS NULL", ct);
    public Task<int> RenewEventAsync(Guid id, Guid? lease, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync(
        $"UPDATE disasters.\"DisasterOutboxMessages\" SET \"LeaseUntil\"=CURRENT_TIMESTAMP + INTERVAL '30 seconds' WHERE \"Id\"={id} AND \"LeaseId\"={lease} AND \"LeaseUntil\">CURRENT_TIMESTAMP AND \"PublishedAt\" IS NULL", ct);

    public async Task<IReadOnlyList<DisasterCommandReceipt>> ClaimCommandsAsync(int limit, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var rows = await db.Commands.FromSqlInterpolated($"SELECT * FROM disasters.\"DisasterCommandReceipts\" WHERE \"PublishedAt\" IS NULL AND \"ResultJson\" IS NULL AND (\"LeaseUntil\" IS NULL OR \"LeaseUntil\" < CURRENT_TIMESTAMP) ORDER BY \"ReceivedAt\" LIMIT {limit} FOR UPDATE SKIP LOCKED").ToListAsync(ct);
        foreach (var row in rows) { row.LeaseId = Guid.NewGuid(); row.LeaseUntil = DateTimeOffset.UtcNow.AddSeconds(30); row.Attempts++; }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return rows;
    }
    public async Task<IReadOnlyList<DisasterOutboxMessage>> ClaimEventsAsync(int limit, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var rows = await db.Outbox.FromSqlInterpolated($"SELECT * FROM disasters.\"DisasterOutboxMessages\" WHERE \"PublishedAt\" IS NULL AND (\"LeaseUntil\" IS NULL OR \"LeaseUntil\" < CURRENT_TIMESTAMP) ORDER BY \"OccurredAt\" LIMIT {limit} FOR UPDATE SKIP LOCKED").ToListAsync(ct);
        foreach (var row in rows) { row.LeaseId = Guid.NewGuid(); row.LeaseUntil = DateTimeOffset.UtcNow.AddSeconds(30); row.Attempts++; }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return rows;
    }
    public Task<int> ConfirmCommandAsync(Guid id, Guid? lease, CancellationToken ct) => db.Commands.Where(x => x.ClientRequestId == id && x.LeaseId == lease)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.PublishedAt, DateTimeOffset.UtcNow).SetProperty(x => x.LeaseUntil, (DateTimeOffset?)null), ct);
    public Task<int> ConfirmEventAsync(Guid id, Guid? lease, CancellationToken ct) => db.Outbox.Where(x => x.Id == id && x.LeaseId == lease)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.PublishedAt, DateTimeOffset.UtcNow).SetProperty(x => x.LeaseUntil, (DateTimeOffset?)null), ct);
}
