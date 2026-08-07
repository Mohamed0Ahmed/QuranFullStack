using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Access;

internal sealed class EfLogtoSubjectRelinkService(
    QuranDashboardDbContext db,
    AccessUserMutationTransaction transaction,
    IAccessAuditAppender auditAppender,
    IInteractiveIdentityEvidenceValidator evidenceValidator,
    IExternalUserProfileSource profileSource,
    IEmailIdentityNormalizer emailIdentityNormalizer,
    IOwnerBootstrapConfigurationSource configurationSource,
    IOwnerReconciliationService reconciliationService) : ILogtoSubjectRelinkService
{
    public async Task<AccessOperationResult<LogtoSubjectRelinkPreview>> PreviewAsync(
        PreviewLogtoSubjectRelinkCommand command,
        CancellationToken cancellationToken)
    {
        var target = await db.AccessUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == command.UserId, cancellationToken);
        if (target is null)
        {
            return AccessOperationResult<LogtoSubjectRelinkPreview>.Failed(AccessOperationFailure.NotFound);
        }

        var failure = await ValidateBindingAsync(target, command.NewSub, command.EvidenceToken, cancellationToken);
        return failure is not null
            ? AccessOperationResult<LogtoSubjectRelinkPreview>.Failed(failure.Value)
            : AccessOperationResult<LogtoSubjectRelinkPreview>.Success(new LogtoSubjectRelinkPreview(
                target.Id,
                target.LogtoSub,
                command.NewSub,
                target.Version,
                AccessUserContractMapper.IsOwner(target)));
    }

    public Task<AccessOperationResult<AccessUserDetail>> ConfirmAsync(
        string actorSub,
        ConfirmLogtoSubjectRelinkCommand command,
        CancellationToken cancellationToken)
    {
        return transaction.ExecuteAsync(
            actorSub,
            command.UserId,
            command.ExpectedVersion,
            session => ConfirmCoreAsync(session, command, cancellationToken),
            AccessUserContractMapper.ToDetail,
            cancellationToken,
            MapDatabaseFailure);
    }

    private async Task<AccessOperationFailure?> ConfirmCoreAsync(
        AccessUserMutationSession session,
        ConfirmLogtoSubjectRelinkCommand command,
        CancellationToken cancellationToken)
    {
        var target = session.Target;
        if (!string.Equals(target.LogtoSub, command.OldSub, StringComparison.Ordinal))
        {
            return AccessOperationFailure.StaleVersion;
        }

        var failure = await ValidateBindingAsync(target, command.NewSub, command.EvidenceToken, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        var actorSnapshot = AccessUserContractMapper.ToAuditSnapshot(session.Actor);
        var permissionCodes = target.UserPermissions
            .OrderBy(grant => grant.Permission.DisplayOrder)
            .Select(grant => grant.Permission.Code)
            .ToArray();
        var beforeState = AccessUserContractMapper.ToAuditState(target, permissionCodes);
        target.LogtoSub = command.NewSub;
        target.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var afterSnapshot = AccessUserContractMapper.ToAuditSnapshot(target);
        var afterState = AccessUserContractMapper.ToAuditState(target, permissionCodes);
        auditAppender.Append(new AccessAuditEntry(
            AccessAuditActionType.LogtoSubjectRelinked,
            session.Actor.Id,
            actorSnapshot,
            afterSnapshot,
            null,
            beforeState,
            afterState,
            command.Reason,
            "relink-logto-sub"));
        return null;
    }

    private async Task<AccessOperationFailure?> ValidateBindingAsync(
        User target,
        string newSub,
        string evidenceToken,
        CancellationToken cancellationToken)
    {
        if (string.Equals(target.LogtoSub, newSub, StringComparison.Ordinal))
        {
            return AccessOperationFailure.InvalidRequest;
        }

        var evidence = await evidenceValidator.ValidateAsync(evidenceToken, newSub, cancellationToken);
        if (evidence is null
            || !evidence.EmailVerified
            || !string.Equals(evidence.Sub, newSub, StringComparison.Ordinal)
            || !emailIdentityNormalizer.TryNormalize(evidence.Email, out var normalizedEvidenceEmail)
            || !string.Equals(normalizedEvidenceEmail, target.NormalizedEmail, StringComparison.Ordinal))
        {
            return AccessOperationFailure.InvalidRelinkEvidence;
        }

        var profile = await profileSource.GetProfileAsync(newSub, cancellationToken);
        if (!emailIdentityNormalizer.TryNormalize(profile.Email, out var normalizedProfileEmail)
            || !string.Equals(normalizedProfileEmail, target.NormalizedEmail, StringComparison.Ordinal))
        {
            return AccessOperationFailure.InvalidRelinkEvidence;
        }

        if (await db.AccessUsers.AsNoTracking().AnyAsync(
                user => user.Id != target.Id && user.NormalizedEmail == target.NormalizedEmail,
                cancellationToken))
        {
            return AccessOperationFailure.NormalizedEmailCollision;
        }

        if (await db.AccessUsers.AsNoTracking().AnyAsync(user => user.LogtoSub == newSub, cancellationToken))
        {
            return AccessOperationFailure.SubjectAlreadyLinked;
        }

        return AccessUserContractMapper.IsOwner(target)
            ? await ValidateOwnerConfigurationAsync(target, cancellationToken)
            : null;
    }

    private async Task<AccessOperationFailure?> ValidateOwnerConfigurationAsync(
        User target,
        CancellationToken cancellationToken)
    {
        var configuration = configurationSource.GetCurrent();
        if (!configuration.NormalizedEmails.Contains(target.NormalizedEmail))
        {
            return AccessOperationFailure.OwnerConfigurationNotReconciled;
        }

        var status = await reconciliationService.GetStatusAsync(cancellationToken);
        return status.Candidates.Any(candidate => candidate.UserId == target.Id
                && string.Equals(candidate.NormalizedEmail, target.NormalizedEmail, StringComparison.Ordinal)
                && candidate.State == OwnerReconciliationCandidateState.Unchanged)
            ? null
            : AccessOperationFailure.OwnerConfigurationNotReconciled;
    }

    private static AccessOperationFailure? MapDatabaseFailure(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: "23505" }
            ? AccessOperationFailure.SubjectAlreadyLinked
            : null;
}
