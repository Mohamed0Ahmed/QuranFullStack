using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Application.Security.Permissions;

// Owner-only grant/revoke, run through the separate security-audit unit of work. The catalogue/assignability
// checks and the stale-version check run INSIDE the serialized commit (after the barrier + generation gates)
// so first-grant and grant-vs-revoke are ordered. An idempotent re-grant/re-revoke is a no-op with no audit
// and no cache invalidation; a real change is permanently audited and invalidates the effective-permission
// cache post-commit.
public sealed class PermissionAdministrationHandler(
    ISecurityAuditWriteExecutor executor,
    IPermissionAssignmentStore assignments,
    IEffectivePermissionCache cache,
    IServerClock clock)
{
    public async Task<SecurityAuditCommitResult> GrantAsync(GrantPermissionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var code = RequireCatalogueCode(command.PermissionCode);

        var request = new SecurityAuditWriteRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async operationToken =>
            {
                // SystemOwnerOnly codes are never assignable to an ordinary user (SC-007 / §5.2).
                if (code.SystemOwnerOnly)
                {
                    throw new PermissionBaselineLockedException(command.PermissionCode);
                }

                var existing = await assignments.FindTrackedAsync(
                    command.TargetKind, command.TargetKey, command.PermissionCode, operationToken);
                var currentVersion = existing?.Version ?? 0;
                if (command.ExpectedVersion != currentVersion)
                {
                    throw new PermissionAssignmentStaleException(command.ExpectedVersion, currentVersion);
                }

                if (existing is { IsGranted: true })
                {
                    return SecurityAuditOutcome.NoOp;
                }

                if (existing is null)
                {
                    assignments.Add(new PermissionAssignment(
                        Guid.NewGuid(), command.TargetKind, command.TargetKey, command.PermissionCode, clock.UtcNow));
                }
                else
                {
                    existing.Grant(clock.UtcNow);
                }

                return SecurityAuditOutcome.Audited(new SecurityAuditEventDraft("permission.granted", DescribeTarget(command)));
            });

        return await CommitAsync(request, cancellationToken);
    }

    public async Task<SecurityAuditCommitResult> RevokeAsync(RevokePermissionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var code = RequireCatalogueCode(command.PermissionCode);

        var request = new SecurityAuditWriteRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async operationToken =>
            {
                // The DashboardAdminBaseline (attribution.view) cannot be removed (FR-031).
                if (code.DashboardAdminBaseline)
                {
                    throw new PermissionBaselineLockedException(command.PermissionCode);
                }

                var existing = await assignments.FindTrackedAsync(
                    command.TargetKind, command.TargetKey, command.PermissionCode, operationToken);
                var currentVersion = existing?.Version ?? 0;
                if (command.ExpectedVersion != currentVersion)
                {
                    throw new PermissionAssignmentStaleException(command.ExpectedVersion, currentVersion);
                }

                if (existing is null or { IsGranted: false })
                {
                    return SecurityAuditOutcome.NoOp;
                }

                existing.Revoke(clock.UtcNow);
                return SecurityAuditOutcome.Audited(new SecurityAuditEventDraft("permission.revoked", DescribeTarget(command)));
            });

        return await CommitAsync(request, cancellationToken);
    }

    private async Task<SecurityAuditCommitResult> CommitAsync(SecurityAuditWriteRequest request, CancellationToken cancellationToken)
    {
        var result = await executor.ExecuteAsync(request, cancellationToken);
        if (result.Audited)
        {
            cache.Invalidate();
        }

        return result;
    }

    private static PermissionCode RequireCatalogueCode(string code) =>
        PermissionCatalogue.Find(code) ?? throw new UnknownPermissionCodeException(code);

    private static string DescribeTarget(GrantPermissionCommand command) =>
        $"{command.TargetKind}:{command.TargetKey}:{command.PermissionCode}";

    private static string DescribeTarget(RevokePermissionCommand command) =>
        $"{command.TargetKind}:{command.TargetKey}:{command.PermissionCode}";
}
