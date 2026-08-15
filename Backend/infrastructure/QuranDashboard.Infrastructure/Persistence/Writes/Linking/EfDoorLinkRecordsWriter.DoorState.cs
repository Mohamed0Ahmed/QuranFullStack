using QuranDashboard.Application.Abstractions.Linking.DoorLinks;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfDoorLinkRecordsWriter
{
    private async Task<AbwabDoor?> LockDoorAsync(int doorId, CancellationToken cancellationToken) =>
        (await db.AbwabDoors.FromSqlInterpolated(
                $"""
                SELECT id, section_id, parent_id, name, description, representative_ayah_text,
                       order_value, global_order_value, created_at, created_by, updated_at, updated_by,
                       approved_at, approved_by, deleted_at, deleted_by, xmin
                FROM abwab_doors
                WHERE id = {doorId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private static DoorLinkMutationWriteResult? ValidateDoor(AbwabDoor? door, uint expectedDoorVersion)
    {
        if (door is null)
        {
            return new DoorLinkMutationWriteResult.DoorNotFound();
        }

        if (door.DeletedAtUtc is not null)
        {
            return new DoorLinkMutationWriteResult.DoorArchived();
        }

        return door.Version == expectedDoorVersion
            ? null
            : new DoorLinkMutationWriteResult.DoorVersionStale();
    }

    private async Task<LockedUnitState?> LockLiveUnitAsync(
        int doorId,
        long unitId,
        CancellationToken cancellationToken)
    {
        var unit = (await db.LinkingUnits.FromSqlInterpolated(
                $"""
                SELECT id, door_id, identity, identity_hash, is_grouped, created_at, created_by
                FROM linking_units
                WHERE id = {unitId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
            .SingleOrDefault();
        if (unit is null || unit.DoorId != doorId)
        {
            return null;
        }

        var ayahs = await db.LinkingUnitAyahs.FromSqlInterpolated(
                $"""
                SELECT id, unit_id, ayah_id, order_value
                FROM linking_unit_ayahs
                WHERE unit_id = {unitId}
                ORDER BY order_value, id
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);
        var isLive = await (
                from mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                join contribution in db.LinkingSourceContributions.AsNoTracking()
                    on mapping.SourceContributionId equals contribution.Id
                where mapping.UnitId == unitId
                    && contribution.DoorId == doorId
                    && contribution.DeletedAtUtc == null
                select mapping.UnitId)
            .AnyAsync(cancellationToken);
        var hasCrossDoorMapping = await (
                from mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                join contribution in db.LinkingSourceContributions.AsNoTracking()
                    on mapping.SourceContributionId equals contribution.Id
                where mapping.UnitId == unitId && contribution.DoorId != doorId
                select mapping.UnitId)
            .AnyAsync(cancellationToken);

        return ayahs.Count > 0 && isLive && !hasCrossDoorMapping
            ? new LockedUnitState(unit, ayahs)
            : null;
    }

    private async Task<LinkingUnit?> LockIdentityCollisionAsync(
        int doorId,
        long excludedUnitId,
        byte[] identityHash,
        CancellationToken cancellationToken) =>
        (await db.LinkingUnits.FromSqlInterpolated(
                $"""
                SELECT id, door_id, identity, identity_hash, is_grouped, created_at, created_by
                FROM linking_units
                WHERE door_id = {doorId}
                  AND identity_hash = {identityHash}
                  AND id <> {excludedUnitId}
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken))
        .SingleOrDefault();

    private async Task<IReadOnlyList<long>> LoadMappedContributionIdsAsync(
        int doorId,
        long unitId,
        CancellationToken cancellationToken) =>
        await (
                from mapping in db.LinkingSourceContributionUnits.AsNoTracking()
                join contribution in db.LinkingSourceContributions.AsNoTracking()
                    on mapping.SourceContributionId equals contribution.Id
                where mapping.UnitId == unitId && contribution.DoorId == doorId
                select mapping.SourceContributionId)
            .Distinct()
            .OrderBy(id => id)
            .ToListAsync(cancellationToken);

    private async Task BumpDoorAsync(
        AbwabDoor door,
        int actorUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        door.UpdatedAtUtc = now;
        door.UpdatedBy = actorUserId;
        await SaveChangesAsync(cancellationToken);
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("A locked door-link mutation encountered a concurrency conflict.");
        }
    }

    private async Task NormalizeContributionOrdersAsync(
        IReadOnlyList<long> contributionIds,
        CancellationToken cancellationToken)
    {
        foreach (var contributionId in contributionIds)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH ranked AS (
                    SELECT source_contribution_id, unit_id,
                           ROW_NUMBER() OVER (ORDER BY order_value, unit_id) AS position
                    FROM linking_source_contribution_units
                    WHERE source_contribution_id = {contributionId}
                )
                UPDATE linking_source_contribution_units mapping
                SET order_value = (-ranked.position)::integer
                FROM ranked
                WHERE mapping.source_contribution_id = ranked.source_contribution_id
                  AND mapping.unit_id = ranked.unit_id;

                UPDATE linking_source_contribution_units
                SET order_value = -order_value
                WHERE source_contribution_id = {contributionId};
                """,
                cancellationToken);
        }
    }

    private async Task DeleteUnitsAsync(
        IReadOnlyList<long> unitIds,
        CancellationToken cancellationToken)
    {
        if (unitIds.Count == 0)
        {
            return;
        }

        var ids = unitIds.ToArray();
        var hasMappings = await db.LinkingSourceContributionUnits.AsNoTracking()
            .AnyAsync(mapping => ids.Contains(mapping.UnitId), cancellationToken);
        if (hasMappings)
        {
            throw new InvalidOperationException("A door-link unit still has contribution mappings.");
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM linking_unit_ayah_descriptions
            WHERE unit_ayah_id IN (
                SELECT id FROM linking_unit_ayahs WHERE unit_id = ANY ({ids}));

            DELETE FROM linking_unit_ayah_words
            WHERE unit_ayah_id IN (
                SELECT id FROM linking_unit_ayahs WHERE unit_id = ANY ({ids}));

            DELETE FROM linking_unit_ayahs
            WHERE unit_id = ANY ({ids});

            DELETE FROM linking_units
            WHERE id = ANY ({ids});
            """,
            cancellationToken);
    }

    private async Task RebuildDoorAyahsAsync(
        int doorId,
        IReadOnlyList<int> affectedAyahIds,
        int actorUserId,
        bool deleteEmptyAyahs,
        CancellationToken cancellationToken)
    {
        if (affectedAyahIds.Count == 0)
        {
            return;
        }

        var ayahIds = affectedAyahIds.Distinct().ToArray();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO linking_door_ayahs (door_id, ayah_id, created_at, created_by)
            SELECT DISTINCT {doorId}, unit_ayah.ayah_id, CURRENT_TIMESTAMP, {actorUserId}
            FROM linking_unit_ayahs unit_ayah
            JOIN linking_units unit ON unit.id = unit_ayah.unit_id
            JOIN linking_source_contribution_units mapping ON mapping.unit_id = unit.id
            JOIN linking_source_contributions contribution
              ON contribution.id = mapping.source_contribution_id
             AND contribution.deleted_at IS NULL
            WHERE unit.door_id = {doorId}
              AND contribution.door_id = {doorId}
              AND unit_ayah.ayah_id = ANY ({ayahIds})
            ON CONFLICT (door_id, ayah_id) DO NOTHING;

            DELETE FROM linking_door_ayah_words word
            USING linking_door_ayahs door_ayah
            WHERE word.door_ayah_id = door_ayah.id
              AND door_ayah.door_id = {doorId}
              AND door_ayah.ayah_id = ANY ({ayahIds});

            INSERT INTO linking_door_ayah_words (
                door_ayah_id, quran_word_id, ayah_id, created_at, created_by)
            SELECT DISTINCT door_ayah.id, unit_word.quran_word_id, unit_ayah.ayah_id,
                   CURRENT_TIMESTAMP, {actorUserId}
            FROM linking_door_ayahs door_ayah
            JOIN linking_unit_ayahs unit_ayah ON unit_ayah.ayah_id = door_ayah.ayah_id
            JOIN linking_unit_ayah_words unit_word ON unit_word.unit_ayah_id = unit_ayah.id
            JOIN linking_units unit ON unit.id = unit_ayah.unit_id
            JOIN linking_source_contribution_units mapping ON mapping.unit_id = unit.id
            JOIN linking_source_contributions contribution
              ON contribution.id = mapping.source_contribution_id
             AND contribution.deleted_at IS NULL
            WHERE door_ayah.door_id = {doorId}
              AND unit.door_id = {doorId}
              AND contribution.door_id = {doorId}
              AND door_ayah.ayah_id = ANY ({ayahIds})
            ON CONFLICT (door_ayah_id, quran_word_id) DO NOTHING;
            """,
            cancellationToken);

        if (deleteEmptyAyahs)
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DELETE FROM linking_door_ayahs door_ayah
                WHERE door_ayah.door_id = {doorId}
                  AND door_ayah.ayah_id = ANY ({ayahIds})
                  AND NOT EXISTS (
                      SELECT 1
                      FROM linking_unit_ayahs unit_ayah
                      JOIN linking_units unit ON unit.id = unit_ayah.unit_id
                      JOIN linking_source_contribution_units mapping ON mapping.unit_id = unit.id
                      JOIN linking_source_contributions contribution
                        ON contribution.id = mapping.source_contribution_id
                       AND contribution.deleted_at IS NULL
                      WHERE unit_ayah.ayah_id = door_ayah.ayah_id
                        AND unit.door_id = {doorId}
                        AND contribution.door_id = {doorId})
                """,
                cancellationToken);
        }
    }

    private sealed record LockedUnitState(LinkingUnit Unit, IReadOnlyList<LinkingUnitAyah> Ayahs);
}
