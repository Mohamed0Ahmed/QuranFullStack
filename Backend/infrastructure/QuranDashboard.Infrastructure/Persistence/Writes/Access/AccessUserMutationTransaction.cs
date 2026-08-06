using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Domain.Access;
using QuranDashboard.Infrastructure.Access;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Access;

internal sealed class AccessUserMutationTransaction(
    QuranDashboardDbContext db,
    IUserRoleResolver roleResolver)
{
    public async Task<AccessOperationResult<T>> ExecuteAsync<T>(
        string actorSub,
        int targetUserId,
        uint expectedVersion,
        Func<AccessUserMutationSession, Task<AccessOperationFailure?>> mutateAsync,
        Func<User, T> project,
        CancellationToken cancellationToken,
        Func<DbUpdateException, AccessOperationFailure?>? mapDatabaseFailure = null)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockUsersAsync(actorSub, targetUserId, cancellationToken);

        var actor = await db.AccessUsers
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.LogtoSub == actorSub, cancellationToken);
        if (actor is null || actor.Status != UserStatus.Active || !AccessUserContractMapper.IsOwner(actor))
        {
            return AccessOperationResult<T>.Failed(AccessOperationFailure.ActorNoLongerOwner);
        }

        await LockTargetGrantsAsync(targetUserId, cancellationToken);
        var target = await db.AccessUsers
            .Include(user => user.Role)
            .Include(user => user.UserPermissions)
            .ThenInclude(grant => grant.Permission)
            .SingleOrDefaultAsync(user => user.Id == targetUserId, cancellationToken);
        if (target is null)
        {
            return AccessOperationResult<T>.Failed(AccessOperationFailure.NotFound);
        }

        if (target.Version != expectedVersion)
        {
            return AccessOperationResult<T>.Failed(AccessOperationFailure.StaleVersion);
        }

        db.Entry(target).Property(user => user.Version).OriginalValue = expectedVersion;
        var oldTargetSub = target.LogtoSub;
        var failure = await mutateAsync(new AccessUserMutationSession(actor, target));
        if (failure is not null)
        {
            return AccessOperationResult<T>.Failed(failure.Value);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return AccessOperationResult<T>.Failed(AccessOperationFailure.StaleVersion);
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            var mappedFailure = mapDatabaseFailure?.Invoke(exception);
            if (mappedFailure is not null)
            {
                return AccessOperationResult<T>.Failed(mappedFailure.Value);
            }

            throw;
        }

        roleResolver.Evict(oldTargetSub);
        roleResolver.Evict(target.LogtoSub);
        return AccessOperationResult<T>.Success(project(target));
    }

    private Task LockUsersAsync(string actorSub, int targetUserId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM users WHERE logto_sub = {actorSub} OR id = {targetUserId} ORDER BY id FOR UPDATE;",
            cancellationToken);

    private Task LockTargetGrantsAsync(int targetUserId, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT user_id FROM user_permissions WHERE user_id = {targetUserId} FOR UPDATE;",
            cancellationToken);
}

internal sealed record AccessUserMutationSession(User Actor, User Target);
