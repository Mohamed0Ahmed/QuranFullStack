using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Security.Owners;

namespace QuranDashboard.Application.Security.Owners;

// The final-owner invariant runs INSIDE the serialized commit (after the barrier + generation gates), so
// concurrent removals are ordered and can never leave zero active owners.
public sealed class SystemOwnerAdministrationHandler(
    ISecurityAuditWriteExecutor executor,
    ISystemOwnerStore owners,
    IServerClock clock)
{
    public Task<SecurityAuditCommitResult> AddAsync(AddSystemOwnerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new SecurityAuditWriteRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async operationToken =>
            {
                var roster = await owners.ListTrackedAsync(operationToken);
                var existing = roster.FirstOrDefault(owner =>
                    string.Equals(owner.Subject, command.Subject, StringComparison.Ordinal) &&
                    string.Equals(owner.Issuer, command.Issuer, StringComparison.Ordinal));

                if (existing is { IsActive: true })
                {
                    existing.SetAccountEnabled(command.AccountEnabled);
                    return existing.IsActiveOwner ? SecurityAuditOutcome.NoOp
                        : SecurityAuditOutcome.Audited(new SecurityAuditEventDraft("owner.disabled", command.Subject));
                }

                if (existing is not null)
                {
                    existing.Reactivate();
                    existing.SetAccountEnabled(command.AccountEnabled);
                }
                else
                {
                    owners.Add(new SystemOwnerMembership(
                        Guid.NewGuid(), command.Issuer, command.Subject, command.AccountEnabled, clock.UtcNow));
                }

                return SecurityAuditOutcome.Audited(new SecurityAuditEventDraft("owner.added", command.Subject));
            });

        return executor.ExecuteAsync(request, cancellationToken);
    }

    public Task<SecurityAuditCommitResult> RemoveAsync(RemoveSystemOwnerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new SecurityAuditWriteRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async operationToken =>
            {
                var roster = await owners.ListTrackedAsync(operationToken);
                var target = roster.FirstOrDefault(owner =>
                    string.Equals(owner.Subject, command.Subject, StringComparison.Ordinal));

                if (target is null || !target.IsActive)
                {
                    return SecurityAuditOutcome.NoOp;
                }

                if (!SystemOwnerInvariant.AllowsDeactivation(roster, target.Id))
                {
                    throw new LastSystemOwnerException();
                }

                target.Deactivate(clock.UtcNow);
                return SecurityAuditOutcome.Audited(new SecurityAuditEventDraft("owner.removed", command.Subject));
            });

        return executor.ExecuteAsync(request, cancellationToken);
    }

    public Task<SecurityAuditCommitResult> BootstrapAsync(BootstrapSystemOwnerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var request = new SecurityAuditWriteRequest(
            command.ExpectedTimelineGeneration,
            command.ActorSubject,
            async operationToken =>
            {
                if (!string.Equals(command.Issuer, command.ExpectedIssuer, StringComparison.Ordinal))
                {
                    throw new SystemOwnerBootstrapException(SystemOwnerBootstrapFailure.WrongIssuer);
                }

                if (!command.EmailVerified)
                {
                    throw new SystemOwnerBootstrapException(SystemOwnerBootstrapFailure.UnverifiedEmail);
                }

                if (!command.AccountEnabled)
                {
                    throw new SystemOwnerBootstrapException(SystemOwnerBootstrapFailure.DisabledAccount);
                }

                var roster = await owners.ListTrackedAsync(operationToken);
                var identity = new SystemOwnerIdentity(command.Issuer, command.Subject);

                if (roster.Count > 0)
                {
                    var alreadyBootstrapped = roster.Count == 1 && roster[0].Identity == identity;
                    return alreadyBootstrapped
                        ? SecurityAuditOutcome.NoOp
                        : throw new SystemOwnerBootstrapException(SystemOwnerBootstrapFailure.DuplicateMismatch);
                }

                owners.Add(new SystemOwnerMembership(
                    Guid.NewGuid(), command.Issuer, command.Subject, command.AccountEnabled, clock.UtcNow));
                return SecurityAuditOutcome.Audited(new SecurityAuditEventDraft("owner.bootstrapped", command.Subject));
            });

        return executor.ExecuteAsync(request, cancellationToken);
    }
}
