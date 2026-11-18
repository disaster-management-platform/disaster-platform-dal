namespace Platform.DAL.Models;

public sealed class DisasterOutboxMessage
{
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public Guid? LeaseId { get; set; }
    public int Attempts { get; set; }
    public Guid Id { get; set; }
    public int DisasterId { get; set; }
    public required string EventType { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public long Version { get; set; }
    public required string Payload { get; set; }
}
