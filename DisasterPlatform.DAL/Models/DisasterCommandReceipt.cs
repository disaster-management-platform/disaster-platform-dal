namespace Platform.DAL.Models;

public sealed class DisasterCommandReceipt
{
    public Guid ClientRequestId { get; set; }
    public required string CommandJson { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public Guid? LeaseId { get; set; }
    public int Attempts { get; set; }
    public string? ResultJson { get; set; }
}
