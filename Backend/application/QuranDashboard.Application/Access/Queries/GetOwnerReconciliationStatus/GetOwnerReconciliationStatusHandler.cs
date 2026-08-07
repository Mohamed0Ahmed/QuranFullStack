using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Application.Access.Queries.GetOwnerReconciliationStatus;

public sealed class GetOwnerReconciliationStatusHandler(
    IOwnerReconciliationService reconciliationService,
    IAccessAuditReader auditReader)
{
    public async Task<OwnerReconciliationStatus> HandleAsync(CancellationToken cancellationToken)
    {
        var status = await reconciliationService.GetStatusAsync(cancellationToken);
        var lastReconciliation = await auditReader.GetLatestOwnerReconciliationAsync(cancellationToken);
        return new OwnerReconciliationStatus(
            status.ConfigurationFingerprint,
            status.CanApply,
            status.IsReady,
            status.Candidates
                .Select(candidate => new OwnerReconciliationStatusCandidate(
                    candidate.NormalizedEmail,
                    candidate.UserId,
                    candidate.State.ToString()))
                .ToArray(),
            lastReconciliation);
    }
}
