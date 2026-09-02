using QuranDashboard.Application.Abstractions.Linking.DoorLinks;
using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfDoorLinkRecordsWriter
{
    public async Task<DoorLinkMutationWriteResult> DeleteAsync(
        int doorId,
        uint expectedDoorVersion,
        DoorLinkSelection selection,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await lockProtocol.AcquireDoorInclusionGraphMutationAsync(cancellationToken);
            var door = await LockDoorAsync(doorId, cancellationToken);
            var invalidDoor = ValidateDoor(door, expectedDoorVersion);
            if (invalidDoor is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return invalidDoor;
            }

            var lockedUnits = await LockDoorUnitsAsync(doorId, cancellationToken);
            var lockedAyahs = await LockDoorUnitAyahsAsync(doorId, cancellationToken);
            var liveUnitIds = await LoadLiveUnitIdsAsync(doorId, cancellationToken);
            var suppliedIds = selection.UnitIds.ToHashSet();
            if (!suppliedIds.IsSubsetOf(liveUnitIds))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new DoorLinkMutationWriteResult.UnitNotFound();
            }

            var selectedIds = selection.Mode == DoorLinkSelectionMode.Only
                ? suppliedIds
                : liveUnitIds.Where(unitId => !suppliedIds.Contains(unitId)).ToHashSet();
            if (selectedIds.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new DoorLinkMutationWriteResult.Success(
                    new DoorLinkMutationDto(0, door!.Version),
                    true);
            }

            var selectedUnits = lockedUnits.Where(unit => selectedIds.Contains(unit.Id)).ToList();
            if (selectedUnits.Count != selectedIds.Count)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new DoorLinkMutationWriteResult.UnitNotFound();
            }

            if (await HasCrossDoorMappingsAsync(doorId, selectedIds, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return new DoorLinkMutationWriteResult.UnitNotFound();
            }

            var affectedAyahIds = lockedAyahs
                .Where(ayah => selectedIds.Contains(ayah.UnitId))
                .Select(ayah => ayah.AyahId)
                .Distinct()
                .ToList();
            var synchronizedUnitIds = await inclusionSynchronizer.PrepareTargetUnitSuppressionsAsync(
                doorId,
                selectedIds,
                actorUserId,
                cancellationToken);
            var synchronizedUnitIdSet = synchronizedUnitIds.ToHashSet();
            var directUnitIds = selectedIds
                .Where(unitId => !synchronizedUnitIdSet.Contains(unitId))
                .Order()
                .ToArray();
            var affectedContributionIds = await (
                    from mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                    join contribution in db.LinkingSourceContributions.AsNoTracking()
                        on mapping.SourceContributionId equals contribution.Id
                    where directUnitIds.Contains(mapping.UnitId) && contribution.DoorId == doorId
                    select mapping.SourceContributionId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync(cancellationToken);

            await inclusionSynchronizer.SynchronizeAsync(
                doorId,
                AbwabDoorInclusionMutationSet.Create([], [], selectedIds, []),
                actorUserId,
                cancellationToken);

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM linking_source_contribution_units mapping
                USING linking_source_contributions contribution
                WHERE mapping.unit_id = ANY ({directUnitIds})
                  AND contribution.id = mapping.source_contribution_id
                  AND contribution.door_id = {doorId}
                """,
                cancellationToken);
            await NormalizeContributionOrdersAsync(affectedContributionIds, cancellationToken);

            var now = DateTimeOffset.UtcNow;
            await UpdateContributionsAfterDeletionAsync(
                doorId,
                affectedContributionIds,
                actorUserId,
                now,
                cancellationToken);
            await DeleteUnitsAsync(selectedIds.Order().ToArray(), cancellationToken);
            await RebuildDoorAyahsAsync(
                doorId,
                affectedAyahIds,
                actorUserId,
                true,
                cancellationToken);
            await BumpDoorAsync(door!, actorUserId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new DoorLinkMutationWriteResult.Success(
                new DoorLinkMutationDto(selectedIds.Count, door!.Version),
                false);
        }
        catch (AbwabDoorInclusionSynchronizationConflictException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DoorLinkMutationWriteResult.DoorVersionStale();
        }
        catch (AbwabDoorInclusionSynchronizationUnavailableException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new DoorLinkMutationWriteResult.SynchronizationUnavailable();
        }
    }

    private async Task<IReadOnlyList<LinkingUnit>> LockDoorUnitsAsync(
        int doorId,
        CancellationToken cancellationToken) =>
        await db.LinkingUnits.FromSqlInterpolated(
                $"""
                SELECT id, door_id, identity, identity_hash, is_grouped, created_at, created_by
                FROM linking_units
                WHERE door_id = {doorId}
                ORDER BY id
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

    private async Task<IReadOnlyList<LinkingUnitAyah>> LockDoorUnitAyahsAsync(
        int doorId,
        CancellationToken cancellationToken) =>
        await db.LinkingUnitAyahs.FromSqlInterpolated(
                $"""
                SELECT unit_ayah.id, unit_ayah.unit_id, unit_ayah.ayah_id, unit_ayah.order_value
                FROM linking_unit_ayahs unit_ayah
                JOIN linking_units unit ON unit.id = unit_ayah.unit_id
                WHERE unit.door_id = {doorId}
                ORDER BY unit_ayah.unit_id, unit_ayah.order_value, unit_ayah.id
                FOR UPDATE OF unit_ayah
                """)
            .ToListAsync(cancellationToken);

    private async Task<HashSet<long>> LoadLiveUnitIdsAsync(
        int doorId,
        CancellationToken cancellationToken) =>
        (await (
                from unit in db.LinkingUnits.AsNoTracking()
                where unit.DoorId == doorId
                where db.LinkingSourceContributionUnits.AsNoTracking()
                    .Where(mapping => mapping.UnitId == unit.Id)
                    .Join(
                        db.LinkingSourceContributions.AsNoTracking().Where(contribution =>
                            contribution.DoorId == doorId && contribution.DeletedAtUtc == null),
                        mapping => mapping.SourceContributionId,
                        contribution => contribution.Id,
                        (_, _) => 1)
                    .Any()
                select unit.Id)
            .ToListAsync(cancellationToken))
        .ToHashSet();

    private async Task<bool> HasCrossDoorMappingsAsync(
        int doorId,
        IReadOnlySet<long> unitIds,
        CancellationToken cancellationToken)
    {
        var ids = unitIds.ToArray();
        return await (
                from mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                join contribution in db.LinkingSourceContributions.AsNoTracking()
                    on mapping.SourceContributionId equals contribution.Id
                where ids.Contains(mapping.UnitId) && contribution.DoorId != doorId
                select mapping.UnitId)
            .AnyAsync(cancellationToken);
    }

    private async Task UpdateContributionsAfterDeletionAsync(
        int doorId,
        IReadOnlyList<long> contributionIds,
        int actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (contributionIds.Count == 0)
        {
            return;
        }

        var ids = contributionIds.ToArray();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE linking_source_contributions contribution
            SET updated_at = {now},
                updated_by = {actorUserId},
                deleted_at = CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM linking_source_contribution_units mapping
                        WHERE mapping.source_contribution_id = contribution.id)
                    THEN NULL
                    ELSE {now}
                END,
                deleted_by = CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM linking_source_contribution_units mapping
                        WHERE mapping.source_contribution_id = contribution.id)
                    THEN NULL
                    ELSE {actorUserId}
                END
            WHERE contribution.id = ANY ({ids})
              AND contribution.door_id = {doorId}
              AND contribution.deleted_at IS NULL
            """,
            cancellationToken);
    }
}
