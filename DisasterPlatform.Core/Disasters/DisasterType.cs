namespace DisasterLocation.Core;

public sealed class DisasterType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record DisasterHistoryEntry(Guid Id, string Actor, DateTimeOffset OccurredAt,
    long Version, DisasterPhase Before, DisasterPhase After, string Operation, string Result, string? Changes, string? Reason, bool? CommitmentsConfirmed);
