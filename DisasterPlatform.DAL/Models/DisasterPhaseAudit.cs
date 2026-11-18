using DisasterLocation.Core;
namespace Platform.DAL.Models;

public sealed class DisasterPhaseAudit
{
    public Guid Id { get; set; }
    public int DisasterId { get; set; }
    public DisasterPhase Before { get; set; }
    public DisasterPhase After { get; set; }
    public string Actor { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public string Operation { get; set; } = "PhaseChanged";
    public string Result { get; set; } = "Success";
    public string? Reason { get; set; }
    public bool? CommitmentsConfirmed { get; set; }
    public string? Changes { get; set; }
    public long Version { get; set; }
}
