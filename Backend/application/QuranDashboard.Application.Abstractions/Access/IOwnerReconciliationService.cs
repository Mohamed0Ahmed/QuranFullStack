using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Abstractions.Access;

public interface IOwnerReconciliationService
{
    Task<OwnerReconciliationResult> GetStatusAsync(CancellationToken cancellationToken);

    Task<OwnerReconciliationResult> ReconcileAsync(string reason, CancellationToken cancellationToken);

    Task<OwnerReconciliationResult> ReconcileInteractiveSignInAsync(
        AuthenticatedInteractiveIdentity identity,
        CancellationToken cancellationToken);
}

public sealed record OwnerReconciliationResult(
    string ConfigurationFingerprint,
    IReadOnlyList<OwnerReconciliationCandidate> Candidates,
    bool CanApply,
    bool IsReady);

public sealed record OwnerReconciliationCandidate(
    string NormalizedEmail,
    int? UserId,
    OwnerReconciliationCandidateState State);

public enum OwnerReconciliationCandidateState
{
    Unchanged,
    Added,
    Removed,
    AwaitingVerifiedSignIn,
    Unresolved,
    ConfiguredDisabled,
    OwnerHasDirectGrants,
    RemovalBlockedByLastOwner,
}

public interface IOwnerBootstrapConfigurationSource
{
    OwnerBootstrapConfiguration GetCurrent();
}

public sealed record OwnerBootstrapConfiguration(
    IReadOnlySet<string> NormalizedEmails,
    string ConfigurationFingerprint);

public interface IOwnerReconciliationStore
{
    Task<OwnerReconciliationSnapshot> ReadSnapshotAsync(
        OwnerBootstrapConfiguration configuration,
        CancellationToken cancellationToken);

    Task<OwnerReconciliationCommitResult> TryCommitAsync(
        OwnerReconciliationCommitIntent intent,
        CancellationToken cancellationToken);
}

public sealed record OwnerReconciliationCommitIntent(
    OwnerBootstrapConfiguration ExpectedConfiguration,
    OwnerReconciliationSnapshot PlanningSnapshot,
    OwnerReconciliationMutation PreparedMutation);

public enum OwnerReconciliationCommitResult
{
    Applied,
    StateChanged,
    LockUnavailable,
}

public sealed record OwnerReconciliationSnapshot(
    int OwnerRoleId,
    IReadOnlyList<OwnerReconciliationUser> Users);

public sealed record OwnerReconciliationUser(
    int Id,
    string LogtoSub,
    string NormalizedEmail,
    string? DisplayName,
    UserStatus Status,
    int? RoleId,
    bool IsOwner,
    string? RoleName,
    IReadOnlyList<OwnerReconciliationGrant> DirectGrants);

public sealed record OwnerReconciliationGrant(int UserId, int PermissionId, string PermissionCode);

public sealed record OwnerReconciliationMutation(
    IReadOnlyList<OwnerReconciliationRoleChange> RoleChanges,
    IReadOnlyList<OwnerReconciliationGrantRevocation> GrantRevocations,
    IReadOnlyList<OwnerReconciliationAuditEntry> AuditEntries);

public sealed record OwnerReconciliationRoleChange(
    int UserId,
    bool IsOwner,
    UserStatus Status);

public sealed record OwnerReconciliationGrantRevocation(int UserId, int PermissionId);

public sealed record OwnerReconciliationAuditEntry(
    AccessAuditActionType ActionType,
    OwnerReconciliationAuditUser Target,
    string? PermissionCode,
    OwnerReconciliationAuditUser Before,
    OwnerReconciliationAuditUser After,
    string Reason,
    string ConfigurationFingerprint,
    string ConfiguredNormalizedEmail,
    string EvidenceSource);

public sealed record OwnerReconciliationAuditUser(
    int Id,
    string LogtoSub,
    string NormalizedEmail,
    string? DisplayName,
    UserStatus Status,
    string? RoleName,
    bool IsOwner);

public sealed class OwnerReconciliationReasonValidationException() : ArgumentException(
    "Owner reconciliation requires a non-blank reason no longer than 1024 characters.",
    "reason");
