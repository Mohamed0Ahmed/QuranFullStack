using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Access;

internal sealed class EfAccessUserMutationService(
    QuranDashboardDbContext db,
    AccessUserMutationTransaction transaction,
    IAccessAuditAppender auditAppender) : IAccessUserMutationService
{
    public Task<AccessOperationResult<AccessUserDetail>> AcceptAsync(
        string actorSub,
        AcceptAccessUserCommand command,
        CancellationToken cancellationToken)
    {
        return transaction.ExecuteAsync(
            actorSub,
            command.UserId,
            command.ExpectedVersion,
            session => AcceptCoreAsync(session, command, cancellationToken),
            AccessUserContractMapper.ToDetail,
            cancellationToken);
    }

    public Task<AccessOperationResult<AccessUserDetail>> DisableAsync(
        string actorSub,
        DisableAccessUserCommand command,
        CancellationToken cancellationToken)
    {
        return transaction.ExecuteAsync(
            actorSub,
            command.UserId,
            command.ExpectedVersion,
            session => DisableCoreAsync(session, command),
            AccessUserContractMapper.ToDetail,
            cancellationToken);
    }

    public Task<AccessOperationResult<AccessUserDetail>> ReactivateAsync(
        string actorSub,
        ReactivateAccessUserCommand command,
        CancellationToken cancellationToken)
    {
        return transaction.ExecuteAsync(
            actorSub,
            command.UserId,
            command.ExpectedVersion,
            session => ReactivateCoreAsync(session, command),
            AccessUserContractMapper.ToDetail,
            cancellationToken);
    }

    public Task<AccessOperationResult<AccessUserPermissions>> ReplacePermissionsAsync(
        string actorSub,
        ReplaceUserPermissionsCommand command,
        CancellationToken cancellationToken)
    {
        return transaction.ExecuteAsync(
            actorSub,
            command.UserId,
            command.ExpectedVersion,
            session => ReplacePermissionsCoreAsync(session, command, cancellationToken),
            AccessUserContractMapper.ToPermissions,
            cancellationToken);
    }

    private async Task<AccessOperationFailure?> AcceptCoreAsync(
        AccessUserMutationSession session,
        AcceptAccessUserCommand command,
        CancellationToken cancellationToken)
    {
        var target = session.Target;
        if (AccessUserContractMapper.IsOwner(target))
        {
            return AccessOperationFailure.TargetIsOwner;
        }

        if (target.Status != UserStatus.Pending || target.UserPermissions.Count != 0)
        {
            return AccessOperationFailure.InvalidTransition;
        }

        var permissions = await ResolvePermissionsAsync(command.PermissionCodes ?? [], cancellationToken);
        if (permissions is null)
        {
            return AccessOperationFailure.InvalidPermissionCodes;
        }

        var beforeState = AccessUserContractMapper.ToAuditState(target, []);
        target.Status = UserStatus.Active;
        target.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var afterSnapshot = AccessUserContractMapper.ToAuditSnapshot(target);
        var afterState = AccessUserContractMapper.ToAuditState(
            target,
            permissions.Select(permission => permission.Code).ToArray());
        Append(
            AccessAuditActionType.UserAccepted,
            session.Actor,
            afterSnapshot,
            null,
            beforeState,
            afterState,
            command.Reason,
            "accept-user");
        Append(
            AccessAuditActionType.UserActivated,
            session.Actor,
            afterSnapshot,
            null,
            beforeState,
            afterState,
            command.Reason,
            "accept-user");

        foreach (var permission in permissions)
        {
            var grant = new UserPermission
            {
                User = target,
                Permission = permission,
                GrantedByUserId = session.Actor.Id,
                GrantedAtUtc = DateTimeOffset.UtcNow,
            };
            target.UserPermissions.Add(grant);
            db.AccessUserPermissions.Add(grant);
            Append(
                AccessAuditActionType.PermissionGranted,
                session.Actor,
                afterSnapshot,
                permission.Code,
                beforeState,
                afterState,
                command.Reason,
                "accept-user");
        }

        return null;
    }

    private Task<AccessOperationFailure?> DisableCoreAsync(
        AccessUserMutationSession session,
        DisableAccessUserCommand command)
    {
        var target = session.Target;
        if (AccessUserContractMapper.IsOwner(target))
        {
            return Task.FromResult<AccessOperationFailure?>(AccessOperationFailure.TargetIsOwner);
        }

        if (target.Status != UserStatus.Active)
        {
            return Task.FromResult<AccessOperationFailure?>(AccessOperationFailure.InvalidTransition);
        }

        var grants = target.UserPermissions
            .OrderBy(grant => grant.Permission.DisplayOrder)
            .ToArray();
        var beforeState = AccessUserContractMapper.ToAuditState(
            target,
            grants.Select(grant => grant.Permission.Code).ToArray());
        target.Status = UserStatus.Disabled;
        target.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var afterSnapshot = AccessUserContractMapper.ToAuditSnapshot(target);
        var afterState = AccessUserContractMapper.ToAuditState(target, []);

        foreach (var grant in grants)
        {
            Append(
                AccessAuditActionType.PermissionRevoked,
                session.Actor,
                afterSnapshot,
                grant.Permission.Code,
                beforeState,
                afterState,
                command.Reason,
                "disable-user");
        }

        db.AccessUserPermissions.RemoveRange(grants);
        foreach (var grant in grants)
        {
            target.UserPermissions.Remove(grant);
        }
        Append(
            AccessAuditActionType.UserDisabled,
            session.Actor,
            afterSnapshot,
            null,
            beforeState,
            afterState,
            command.Reason,
            "disable-user");
        return Task.FromResult<AccessOperationFailure?>(null);
    }

    private Task<AccessOperationFailure?> ReactivateCoreAsync(
        AccessUserMutationSession session,
        ReactivateAccessUserCommand command)
    {
        var target = session.Target;
        if (AccessUserContractMapper.IsOwner(target))
        {
            return Task.FromResult<AccessOperationFailure?>(AccessOperationFailure.TargetIsOwner);
        }

        if (target.Status != UserStatus.Disabled || target.UserPermissions.Count != 0)
        {
            return Task.FromResult<AccessOperationFailure?>(AccessOperationFailure.InvalidTransition);
        }

        var beforeState = AccessUserContractMapper.ToAuditState(target, []);
        target.Status = UserStatus.Active;
        target.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var afterSnapshot = AccessUserContractMapper.ToAuditSnapshot(target);
        var afterState = AccessUserContractMapper.ToAuditState(target, []);
        Append(
            AccessAuditActionType.UserReactivated,
            session.Actor,
            afterSnapshot,
            null,
            beforeState,
            afterState,
            command.Reason,
            "reactivate-user");
        return Task.FromResult<AccessOperationFailure?>(null);
    }

    private async Task<AccessOperationFailure?> ReplacePermissionsCoreAsync(
        AccessUserMutationSession session,
        ReplaceUserPermissionsCommand command,
        CancellationToken cancellationToken)
    {
        var target = session.Target;
        if (AccessUserContractMapper.IsOwner(target))
        {
            return AccessOperationFailure.TargetIsOwner;
        }

        if (target.Status != UserStatus.Active)
        {
            return AccessOperationFailure.InvalidTransition;
        }

        var desiredPermissions = await ResolvePermissionsAsync(command.PermissionCodes, cancellationToken);
        if (desiredPermissions is null)
        {
            return AccessOperationFailure.InvalidPermissionCodes;
        }

        var currentGrants = target.UserPermissions
            .OrderBy(grant => grant.Permission.DisplayOrder)
            .ToArray();
        var currentCodes = currentGrants.Select(grant => grant.Permission.Code).ToArray();
        var desiredCodes = desiredPermissions.Select(permission => permission.Code).ToArray();
        var desiredCodeSet = desiredCodes.ToHashSet(StringComparer.Ordinal);
        var currentCodeSet = currentCodes.ToHashSet(StringComparer.Ordinal);
        var revoked = currentGrants
            .Where(grant => !desiredCodeSet.Contains(grant.Permission.Code))
            .ToArray();
        var granted = desiredPermissions
            .Where(permission => !currentCodeSet.Contains(permission.Code))
            .ToArray();
        if (revoked.Length == 0 && granted.Length == 0)
        {
            return null;
        }

        var beforeState = AccessUserContractMapper.ToAuditState(target, currentCodes);
        target.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var afterSnapshot = AccessUserContractMapper.ToAuditSnapshot(target);
        var afterState = AccessUserContractMapper.ToAuditState(target, desiredCodes);

        foreach (var grant in revoked)
        {
            Append(
                AccessAuditActionType.PermissionRevoked,
                session.Actor,
                afterSnapshot,
                grant.Permission.Code,
                beforeState,
                afterState,
                command.Reason,
                "replace-user-permissions");
        }

        db.AccessUserPermissions.RemoveRange(revoked);
        foreach (var grant in revoked)
        {
            target.UserPermissions.Remove(grant);
        }
        foreach (var permission in granted)
        {
            var grant = new UserPermission
            {
                User = target,
                Permission = permission,
                GrantedByUserId = session.Actor.Id,
                GrantedAtUtc = DateTimeOffset.UtcNow,
            };
            target.UserPermissions.Add(grant);
            db.AccessUserPermissions.Add(grant);
            Append(
                AccessAuditActionType.PermissionGranted,
                session.Actor,
                afterSnapshot,
                permission.Code,
                beforeState,
                afterState,
                command.Reason,
                "replace-user-permissions");
        }

        return null;
    }

    private async Task<IReadOnlyList<Permission>?> ResolvePermissionsAsync(
        IReadOnlyList<string> requestedCodes,
        CancellationToken cancellationToken)
    {
        if (requestedCodes.Count > AbwabPermissionCatalogue.All.Count
            || requestedCodes.Any(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        var codes = requestedCodes.Select(code => code.Trim()).ToArray();
        if (codes.Distinct(StringComparer.Ordinal).Count() != codes.Length)
        {
            return null;
        }

        var definitions = AbwabPermissionCatalogue.All
            .ToDictionary(definition => definition.Code, StringComparer.Ordinal);
        if (codes.Any(code => !definitions.ContainsKey(code)))
        {
            return null;
        }

        var permissions = await db.AccessPermissions
            .Where(permission => codes.Contains(permission.Code) && permission.RetiredAtUtc == null)
            .ToListAsync(cancellationToken);
        if (permissions.Count != codes.Length)
        {
            return null;
        }

        return permissions
            .OrderBy(permission => definitions[permission.Code].DisplayOrder)
            .ToArray();
    }

    private void Append(
        AccessAuditActionType actionType,
        User actor,
        AccessAuditUserSnapshot afterTargetSnapshot,
        string? permissionCode,
        AccessAuditUserState beforeState,
        AccessAuditUserState afterState,
        string reason,
        string operation)
    {
        auditAppender.Append(new AccessAuditEntry(
            actionType,
            actor.Id,
            AccessUserContractMapper.ToAuditSnapshot(actor),
            afterTargetSnapshot,
            permissionCode,
            beforeState,
            afterState,
            reason,
            operation));
    }
}
