using System.Text.Json;

namespace DisasterLocation.Core;

public enum InformationSourceKind { Internal = 0, External = 1 }

public sealed class DisasterReport
{
    public Guid Id { get; set; }
    public InformationSourceKind SourceKind { get; set; }
    public required string SourceId { get; set; }
    public required string SourceReportId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public required string ReceivedBy { get; set; }
    public string? RegisteringOrganizationId { get; set; }
    public required string OriginalContent { get; set; }
    public required string ReceiptSnapshot { get; set; }
    public int? DisasterId { get; set; }
}

public sealed record DisasterReportReceipt(DisasterReport Report, bool Created);
public sealed record DisasterOrganization(string OrganizationId, string Responsibility);
public sealed record DisasterGeography(JsonElement? Polygon, string[]? Municipalities = null,
    string[]? Neighborhoods = null, string[]? RiskZones = null);
public sealed record DisasterAlert(Guid Id, string EventType, DateTimeOffset OccurredAt, long Version, string Payload);
