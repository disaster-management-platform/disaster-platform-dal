namespace DisasterLocation.Core;

public interface IDisasterRepository
{
    Task<DisasterReport?> GetReportBySourceAsync(string sourceId, string reportId, CancellationToken ct);
    Task<DisasterReport?> GetReportAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<DisasterReport>> GetReportsAsync(int? disasterId, CancellationToken ct);
    Task<DisasterReportReceipt> ReceiveReportAsync(DisasterReport report, Disaster? preliminary, CancellationToken ct);
    Task<bool> AssociateReportAsync(int disasterId, Guid reportId, long expectedVersion, string actor, CancellationToken ct);
    Task<bool> UpdateContextAsync(int id, long expectedVersion, string actor, string operation,
        string? geographyJson, string? organizationsJson, string? registeringOrganizationId,
        string? emergencyResponsibleId, string? sourceId, CancellationToken ct);
    Task<IReadOnlyList<DisasterAlert>> GetAlertsAsync(int id, CancellationToken ct);
    Task<DisasterSyncPage> GetChangesAsync(long after, int limit, DisasterScope scope, CancellationToken ct = default);
    Task<DisasterSyncPage> GetChangesAsync(long after, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<Disaster>> GetAllAsync(CancellationToken ct = default);
    Task<Disaster?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Disaster?> GetByClientRequestIdAsync(Guid clientRequestId, CancellationToken ct = default);
    Task<bool> ChangePhaseAsync(int id, DisasterPhase previous, DisasterPhase phase, long expectedVersion,
        string actor, CancellationToken ct, string? reason = null, bool? commitmentsConfirmed = null);
    Task<bool> UpdateAsync(Disaster disaster, long expectedVersion, string actor, CancellationToken ct);
    Task<IReadOnlyList<DisasterHistoryEntry>> GetHistoryAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<DisasterType>> GetTypesAsync(CancellationToken ct = default);
    Task<DisasterType?> AddTypeAsync(string name, CancellationToken ct);
    Task<bool> DeactivateTypeAsync(int id, CancellationToken ct);
    Task<DisasterSaveResult> AddAsync(Disaster disaster, CancellationToken ct = default);
}
public sealed record DisasterSaveResult(Disaster Disaster, bool Created);

public sealed record DisasterSyncPage(IReadOnlyList<Disaster> Items, long Cursor, bool HasMore);
