using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Infrastructure.Persistence.Writes.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionsWriter(
    QuranDashboardDbContext db,
    LinkingWriteLockProtocol lockProtocol,
    EfAbwabDoorInclusionSynchronizer synchronizer) : IAbwabDoorInclusionsWriter
{
    public async Task<AbwabDoorInclusionAddWriteResult> AddAsync(
        int targetDoorId,
        uint expectedTargetDoorVersion,
        IReadOnlyList<int> sourceDoorIds,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (targetDoorId <= 0
            || actorUserId <= 0
            || sourceDoorIds.Count == 0
            || sourceDoorIds.Any(sourceDoorId => sourceDoorId <= 0)
            || sourceDoorIds.Distinct().Count() != sourceDoorIds.Count
            || sourceDoorIds.Contains(targetDoorId))
        {
            return new AbwabDoorInclusionAddWriteResult.InvalidRequest();
        }

        var normalizedSourceDoorIds = sourceDoorIds.Order().ToArray();
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await AddWithinTransactionAsync(
                targetDoorId,
                expectedTargetDoorVersion,
                normalizedSourceDoorIds,
                actorUserId,
                cancellationToken);
            if (result is AbwabDoorInclusionAddWriteResult.Success)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return result;
        }
        catch (AbwabDoorInclusionSynchronizationUnavailableException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AbwabDoorInclusionAddWriteResult.SynchronizationUnavailable();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AbwabDoorInclusionAddWriteResult.StaleTargetVersion();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            return new AbwabDoorInclusionAddWriteResult.Duplicate();
        }
    }
}
