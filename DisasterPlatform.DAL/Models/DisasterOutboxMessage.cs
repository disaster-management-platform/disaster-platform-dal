namespace Platform.DAL.Models;

public sealed class DisasterOutboxMessage
{
    public Guid Id { get; set; }
    public int DisasterId { get; set; }
    public required string EventType { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public long Version { get; set; }
    public required string Payload { get; set; }
}
