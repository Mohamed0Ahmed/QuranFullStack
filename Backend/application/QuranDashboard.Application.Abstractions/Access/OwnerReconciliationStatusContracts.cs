namespace QuranDashboard.Application.Abstractions.Access;

public sealed record OwnerReconciliationStatus(
    string ConfigurationFingerprint,
    bool CanApply,
    bool IsReady,
    IReadOnlyList<OwnerReconciliationStatusCandidate> Candidates,
    AccessOwnerReconciliationSummary? LastReconciliation);

public sealed record OwnerReconciliationStatusCandidate(
    string NormalizedEmail,
    int? UserId,
    string State);
