namespace DisasterLocation.Core;

public sealed class Disaster
{
    public int Id { get; set; }
    public Guid? ClientRequestId { get; set; }
    public required string Nombre { get; set; }
    public required string Ubicacion { get; set; }
    public int Severidad { get; set; }
    public required string Descripcion { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double? Radio { get; set; }
    public int? DisasterTypeId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? RegisteredAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public AttentionLevel? AttentionLevel { get; set; }
    public string? RegisteringOrganizationId { get; set; }
    public string? EmergencyResponsibleId { get; set; }
    public string? GeographyJson { get; set; }
    public string? OrganizationsJson { get; set; }
    public string? CreationSnapshot { get; set; }
    public DisasterPhase Phase { get; set; }
    public long Version { get; set; } = 1;
}
