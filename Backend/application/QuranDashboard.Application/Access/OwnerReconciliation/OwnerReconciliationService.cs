using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Access.OwnerReconciliation;

public sealed class OwnerReconciliationService(
    IOwnerReconciliationStore store,
    IOwnerBootstrapConfigurationSource configurationSource,
    IExternalUserProfileSource profileSource,
    IEmailIdentityNormalizer emailIdentityNormalizer) : IOwnerReconciliationService
{
    private const int MaximumReasonLength = 1024;

    public async Task<OwnerReconciliationResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var configuration = configurationSource.GetCurrent();
        var snapshot = await store.ReadSnapshotAsync(configuration, cancellationToken);
        var plan = await BuildPlanAsync(snapshot, configuration, null, ReconciliationMode.Status, cancellationToken);
        return ToResult(plan, applied: false);
    }

    public async Task<OwnerReconciliationResult> ReconcileAsync(string reason, CancellationToken cancellationToken)
    {
        ValidateReason(reason);
        return await ReconcileAsync(
            ReconciliationMode.Operator,
            null,
            reason,
            "m2m-safe-reconciliation",
            cancellationToken);
    }

    public Task<OwnerReconciliationResult> ReconcileInteractiveSignInAsync(
        AuthenticatedInteractiveIdentity identity,
        CancellationToken cancellationToken)
    {
        return ReconcileAsync(
            ReconciliationMode.InteractiveSignIn,
            identity,
            "Verified configured Owner sign-in.",
            "interactive-oidc",
            cancellationToken);
    }

    private async Task<OwnerReconciliationResult> ReconcileAsync(
        ReconciliationMode mode,
        AuthenticatedInteractiveIdentity? identity,
        string reason,
        string evidenceSource,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var configuration = configurationSource.GetCurrent();
            var snapshot = await store.ReadSnapshotAsync(configuration, cancellationToken);
            var plan = await BuildPlanAsync(snapshot, configuration, identity, mode, cancellationToken);
            if (!plan.CanApply)
            {
                return ToResult(plan, applied: false);
            }

            var mutation = BuildMutation(plan, reason, evidenceSource);
            if (mutation.RoleChanges.Count == 0 && mutation.GrantRevocations.Count == 0)
            {
                return ToResult(plan, applied: false);
            }

            var commitResult = await store.TryCommitAsync(
                new OwnerReconciliationCommitIntent(configuration, snapshot, mutation),
                cancellationToken);
            switch (commitResult)
            {
                case OwnerReconciliationCommitResult.Applied:
                    return ToResult(plan, applied: true);
                case OwnerReconciliationCommitResult.LockUnavailable:
                    throw new OwnerReconciliationLockUnavailableException();
                case OwnerReconciliationCommitResult.StateChanged when attempt == 0:
                    continue;
                case OwnerReconciliationCommitResult.StateChanged:
                    throw new InvalidOperationException(
                        "Owner reconciliation local state changed during both commit attempts.");
                default:
                    throw new InvalidOperationException("Unknown Owner reconciliation commit result.");
            }
        }

        throw new InvalidOperationException("Owner reconciliation did not produce a result.");
    }

    private async Task<OwnerReconciliationPlan> BuildPlanAsync(
        OwnerReconciliationSnapshot snapshot,
        OwnerBootstrapConfiguration configuration,
        AuthenticatedInteractiveIdentity? identity,
        ReconciliationMode mode,
        CancellationToken cancellationToken)
    {
        var usersByEmail = snapshot.Users
            .Where(user => configuration.NormalizedEmails.Contains(user.NormalizedEmail))
            .ToDictionary(user => user.NormalizedEmail, StringComparer.Ordinal);
        var evaluations = new List<OwnerCandidateEvaluation>();

        foreach (var configuredEmail in configuration.NormalizedEmails.Order(StringComparer.Ordinal))
        {
            if (!usersByEmail.TryGetValue(configuredEmail, out var user))
            {
                evaluations.Add(new OwnerCandidateEvaluation(
                    configuredEmail,
                    null,
                    OwnerReconciliationCandidateState.AwaitingVerifiedSignIn));
                continue;
            }

            if (user.Status == UserStatus.Disabled)
            {
                evaluations.Add(new OwnerCandidateEvaluation(
                    configuredEmail,
                    user,
                    user.IsOwner && user.DirectGrants.Count > 0
                        ? OwnerReconciliationCandidateState.OwnerHasDirectGrants
                        : OwnerReconciliationCandidateState.ConfiguredDisabled));
                continue;
            }

            var profile = await profileSource.GetProfileAsync(user.LogtoSub, cancellationToken);
            if (!MatchesLocalIdentity(profile, user))
            {
                evaluations.Add(new OwnerCandidateEvaluation(
                    configuredEmail,
                    user,
                    OwnerReconciliationCandidateState.Unresolved));
                continue;
            }

            if (user.IsOwner && user.Status == UserStatus.Active)
            {
                evaluations.Add(new OwnerCandidateEvaluation(
                    configuredEmail,
                    user,
                    user.DirectGrants.Count == 0
                        ? OwnerReconciliationCandidateState.Unchanged
                        : OwnerReconciliationCandidateState.OwnerHasDirectGrants));
                continue;
            }

            var isInteractivePromotion = mode == ReconciliationMode.InteractiveSignIn
                && identity is not null
                && MatchesInteractiveEvidence(identity, user, configuredEmail);
            evaluations.Add(new OwnerCandidateEvaluation(
                configuredEmail,
                user,
                isInteractivePromotion
                    ? OwnerReconciliationCandidateState.Added
                    : OwnerReconciliationCandidateState.AwaitingVerifiedSignIn));
        }

        if (mode is ReconciliationMode.Status or ReconciliationMode.Operator)
        {
            foreach (var user in snapshot.Users.Where(user => user.IsOwner
                         && !configuration.NormalizedEmails.Contains(user.NormalizedEmail)))
            {
                var profile = await profileSource.GetProfileAsync(user.LogtoSub, cancellationToken);
                evaluations.Add(new OwnerCandidateEvaluation(
                    user.NormalizedEmail,
                    user,
                    MatchesLocalIdentity(profile, user)
                        ? OwnerReconciliationCandidateState.Removed
                        : OwnerReconciliationCandidateState.Unresolved));
            }
        }

        var removalCount = evaluations.Count(candidate => candidate.State == OwnerReconciliationCandidateState.Removed
            && candidate.User!.Status == UserStatus.Active);
        var activeOwnerCount = snapshot.Users.Count(user => user.IsOwner && user.Status == UserStatus.Active);
        if (activeOwnerCount - removalCount < 1)
        {
            evaluations = evaluations.Select(candidate => candidate.State == OwnerReconciliationCandidateState.Removed
                    ? candidate with { State = OwnerReconciliationCandidateState.RemovalBlockedByLastOwner }
                    : candidate)
                .ToList();
        }

        var additions = evaluations
            .Where(candidate => candidate.State == OwnerReconciliationCandidateState.Added)
            .ToArray();
        var removals = evaluations
            .Where(candidate => candidate.State == OwnerReconciliationCandidateState.Removed)
            .ToArray();
        var grantCleanup = evaluations
            .Where(candidate => candidate.State == OwnerReconciliationCandidateState.OwnerHasDirectGrants)
            .ToArray();
        var hasHardBlocker = evaluations.Any(candidate => candidate.State is OwnerReconciliationCandidateState.Unresolved
            or OwnerReconciliationCandidateState.RemovalBlockedByLastOwner);
        var activeOwnersAfterChanges = activeOwnerCount + additions.Length - removals.Length;
        var mutationRequested = mode switch
        {
            ReconciliationMode.Operator => removals.Length > 0 || grantCleanup.Length > 0,
            ReconciliationMode.InteractiveSignIn => additions.Length > 0 || grantCleanup.Length > 0,
            _ => false,
        };
        var canApply = activeOwnersAfterChanges > 0
            && !hasHardBlocker
            && (mode != ReconciliationMode.InteractiveSignIn || mutationRequested);
        var isReady = activeOwnersAfterChanges > 0
            && !hasHardBlocker
            && additions.Length == 0
            && removals.Length == 0
            && grantCleanup.Length == 0;

        return new OwnerReconciliationPlan(
            snapshot,
            configuration.ConfigurationFingerprint,
            evaluations,
            additions,
            removals,
            grantCleanup,
            canApply,
            isReady);
    }

    private OwnerReconciliationMutation BuildMutation(
        OwnerReconciliationPlan plan,
        string reason,
        string evidenceSource)
    {
        var roleChanges = new List<OwnerReconciliationRoleChange>();
        var grantRevocations = new List<OwnerReconciliationGrantRevocation>();
        var auditEntries = new List<OwnerReconciliationAuditEntry>();
        var ownerRoleName = RoleNames.Owner;

        foreach (var candidate in plan.Additions)
        {
            var user = candidate.User!;
            var before = ToAuditUser(user);
            var after = before with { IsOwner = true, RoleName = ownerRoleName, Status = UserStatus.Active };
            RevokeGrants(user, before, reason, plan.ConfigurationFingerprint, candidate.NormalizedEmail, evidenceSource,
                grantRevocations, auditEntries);
            roleChanges.Add(new OwnerReconciliationRoleChange(user.Id, true, UserStatus.Active));
            auditEntries.Add(CreateAuditEntry(
                AccessAuditActionType.OwnerGrantedByReconciliation,
                after,
                null,
                before,
                after,
                reason,
                plan.ConfigurationFingerprint,
                candidate.NormalizedEmail,
                evidenceSource));
        }

        foreach (var candidate in plan.Removals)
        {
            var user = candidate.User!;
            var before = ToAuditUser(user);
            var after = before with { IsOwner = false, RoleName = null };
            RevokeGrants(user, before, reason, plan.ConfigurationFingerprint, candidate.NormalizedEmail, evidenceSource,
                grantRevocations, auditEntries);
            roleChanges.Add(new OwnerReconciliationRoleChange(user.Id, false, user.Status));
            auditEntries.Add(CreateAuditEntry(
                AccessAuditActionType.OwnerRemovedByReconciliation,
                after,
                null,
                before,
                after,
                reason,
                plan.ConfigurationFingerprint,
                candidate.NormalizedEmail,
                evidenceSource));
        }

        foreach (var candidate in plan.GrantCleanup)
        {
            var user = candidate.User!;
            var state = ToAuditUser(user);
            RevokeGrants(user, state, reason, plan.ConfigurationFingerprint, candidate.NormalizedEmail, evidenceSource,
                grantRevocations, auditEntries);
        }

        return new OwnerReconciliationMutation(roleChanges, grantRevocations, auditEntries);
    }

    private static void RevokeGrants(
        OwnerReconciliationUser user,
        OwnerReconciliationAuditUser state,
        string reason,
        string configurationFingerprint,
        string configuredNormalizedEmail,
        string evidenceSource,
        ICollection<OwnerReconciliationGrantRevocation> revocations,
        ICollection<OwnerReconciliationAuditEntry> auditEntries)
    {
        foreach (var grant in user.DirectGrants.OrderBy(grant => grant.PermissionCode, StringComparer.Ordinal))
        {
            revocations.Add(new OwnerReconciliationGrantRevocation(grant.UserId, grant.PermissionId));
            auditEntries.Add(CreateAuditEntry(
                AccessAuditActionType.PermissionRevoked,
                state,
                grant.PermissionCode,
                state,
                state,
                reason,
                configurationFingerprint,
                configuredNormalizedEmail,
                evidenceSource));
        }
    }

    private bool MatchesLocalIdentity(ExternalUserProfile profile, OwnerReconciliationUser user)
    {
        return emailIdentityNormalizer.TryNormalize(profile.Email, out var normalizedProfileEmail)
            && string.Equals(normalizedProfileEmail, user.NormalizedEmail, StringComparison.Ordinal);
    }

    private bool MatchesInteractiveEvidence(
        AuthenticatedInteractiveIdentity identity,
        OwnerReconciliationUser user,
        string configuredEmail)
    {
        return identity.EmailVerified
            && string.Equals(identity.Sub, user.LogtoSub, StringComparison.Ordinal)
            && emailIdentityNormalizer.TryNormalize(identity.Email, out var normalizedInteractiveEmail)
            && string.Equals(normalizedInteractiveEmail, configuredEmail, StringComparison.Ordinal)
            && string.Equals(normalizedInteractiveEmail, user.NormalizedEmail, StringComparison.Ordinal);
    }

    private static OwnerReconciliationAuditEntry CreateAuditEntry(
        AccessAuditActionType actionType,
        OwnerReconciliationAuditUser target,
        string? permissionCode,
        OwnerReconciliationAuditUser before,
        OwnerReconciliationAuditUser after,
        string reason,
        string configurationFingerprint,
        string configuredNormalizedEmail,
        string evidenceSource)
    {
        return new OwnerReconciliationAuditEntry(
            actionType,
            target,
            permissionCode,
            before,
            after,
            reason,
            configurationFingerprint,
            configuredNormalizedEmail,
            evidenceSource);
    }

    private static OwnerReconciliationAuditUser ToAuditUser(OwnerReconciliationUser user)
        => new(
            user.Id,
            user.LogtoSub,
            user.NormalizedEmail,
            user.DisplayName,
            user.Status,
            user.RoleName,
            user.IsOwner);

    private static OwnerReconciliationResult ToResult(OwnerReconciliationPlan plan, bool applied)
    {
        var candidates = plan.Evaluations
            .OrderBy(candidate => candidate.NormalizedEmail, StringComparer.Ordinal)
            .Select(candidate => new OwnerReconciliationCandidate(
                candidate.NormalizedEmail,
                candidate.User?.Id,
                candidate.State))
            .ToArray();
        return new OwnerReconciliationResult(
            plan.ConfigurationFingerprint,
            candidates,
            plan.CanApply,
            plan.IsReady || (plan.CanApply && applied));
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > MaximumReasonLength)
        {
            throw new OwnerReconciliationReasonValidationException();
        }
    }

    private sealed record OwnerCandidateEvaluation(
        string NormalizedEmail,
        OwnerReconciliationUser? User,
        OwnerReconciliationCandidateState State);

    private sealed record OwnerReconciliationPlan(
        OwnerReconciliationSnapshot Snapshot,
        string ConfigurationFingerprint,
        IReadOnlyList<OwnerCandidateEvaluation> Evaluations,
        IReadOnlyList<OwnerCandidateEvaluation> Additions,
        IReadOnlyList<OwnerCandidateEvaluation> Removals,
        IReadOnlyList<OwnerCandidateEvaluation> GrantCleanup,
        bool CanApply,
        bool IsReady)
    {
        public bool HasChanges => Additions.Count > 0 || Removals.Count > 0 || GrantCleanup.Count > 0;

    }

    private enum ReconciliationMode
    {
        Status,
        Operator,
        InteractiveSignIn,
    }
}

public sealed class OwnerReconciliationLockUnavailableException() : InvalidOperationException(
    "Owner reconciliation could not acquire its serialization lock.");
