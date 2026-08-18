using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task<IReadOnlyDictionary<long, AbwabDoorInclusionSourceSnapshot>>
        LoadPreviousUnitSnapshotsAsync(
            Guid preflightId,
            int doorId,
            CancellationToken cancellationToken)
    {
        var unitIds = await db.Database.SqlQuery<long>(
                $"""
                SELECT DISTINCT previous.unit_id AS "Value"
                FROM linking_confirmation_previous_units previous
                UNION
                SELECT existing.id AS "Value"
                FROM linking_prepared_units prepared
                JOIN linking_confirmation_sources source
                  ON source.prepared_source_id = prepared.source_id
                JOIN linking_units existing
                  ON existing.door_id = {doorId}
                 AND existing.identity_hash = prepared.unit_identity_hash
                 AND existing.identity = prepared.unit_identity
                WHERE prepared.preflight_id = {preflightId}
                ORDER BY "Value"
                """)
            .ToListAsync(cancellationToken);
        return await AbwabDoorInclusionSourceSnapshot.LoadAsync(db, unitIds, cancellationToken);
    }

    private async Task<AbwabDoorInclusionMutationSet> CreatePreparedMutationSetAsync(
        IReadOnlyDictionary<long, AbwabDoorInclusionSourceSnapshot> previousSnapshots,
        CancellationToken cancellationToken)
    {
        var previousRows = await db.Database.SqlQuery<PreparedUnitOwnershipRow>(
                $"""
                SELECT prepared_source_id AS "PreparedSourceId",
                       unit_id AS "UnitId",
                       order_value AS "OrderValue"
                FROM linking_confirmation_previous_units
                ORDER BY prepared_source_id, order_value, unit_id
                """)
            .ToListAsync(cancellationToken);
        var currentRows = await db.Database.SqlQuery<PreparedCurrentUnitOwnershipRow>(
                $"""
                SELECT prepared_source_id AS "PreparedSourceId",
                       unit_id AS "UnitId",
                       order_value AS "OrderValue",
                       is_new AS "IsNew"
                FROM linking_confirmation_units
                ORDER BY prepared_source_id, order_value, unit_id
                """)
            .ToListAsync(cancellationToken);
        var deletedUnitIds = (await db.Database.SqlQuery<long>(
                $"""
                SELECT unit_id AS "Value"
                FROM linking_confirmation_orphans
                ORDER BY unit_id
                """)
            .ToListAsync(cancellationToken))
            .ToHashSet();
        var addedUnitIds = currentRows
            .Where(row => row.IsNew)
            .Select(row => row.UnitId)
            .ToHashSet();
        var replacements = CreatePreparedReplacements(
            previousRows,
            currentRows,
            deletedUnitIds,
            addedUnitIds);
        deletedUnitIds.ExceptWith(replacements.Select(replacement => replacement.PreviousUnitId));
        addedUnitIds.ExceptWith(replacements.Select(replacement => replacement.CurrentUnitId));

        var currentUnitIds = currentRows.Select(row => row.UnitId).Distinct().Order().ToArray();
        var currentSnapshots = await AbwabDoorInclusionSourceSnapshot.LoadAsync(
            db,
            currentUnitIds,
            cancellationToken);
        if (currentSnapshots.Count != currentUnitIds.Length)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        var editedUnitIds = previousSnapshots.Keys
            .Intersect(currentSnapshots.Keys)
            .Where(unitId => !AbwabDoorInclusionFingerprint.Compute(previousSnapshots[unitId])
                .AsSpan()
                .SequenceEqual(AbwabDoorInclusionFingerprint.Compute(currentSnapshots[unitId])))
            .Order()
            .ToArray();

        return AbwabDoorInclusionMutationSet.Create(
            addedUnitIds,
            editedUnitIds,
            deletedUnitIds,
            replacements);
    }

    private static IReadOnlyList<AbwabDoorInclusionUnitReplacement> CreatePreparedReplacements(
        IReadOnlyList<PreparedUnitOwnershipRow> previousRows,
        IReadOnlyList<PreparedCurrentUnitOwnershipRow> currentRows,
        IReadOnlySet<long> deletedUnitIds,
        IReadOnlySet<long> addedUnitIds)
    {
        var replacements = new List<AbwabDoorInclusionUnitReplacement>();
        var sourceIds = previousRows.Select(row => row.PreparedSourceId)
            .Concat(currentRows.Select(row => row.PreparedSourceId))
            .Distinct()
            .Order();
        foreach (var sourceId in sourceIds)
        {
            var previousForSource = previousRows
                .Where(row => row.PreparedSourceId == sourceId)
                .ToArray();
            var currentForSource = currentRows
                .Where(row => row.PreparedSourceId == sourceId)
                .ToArray();
            var previousUnitIds = previousForSource.Select(row => row.UnitId).ToHashSet();
            var currentUnitIds = currentForSource.Select(row => row.UnitId).ToHashSet();
            var removedOccurrences = previousForSource
                .Where(row => !currentUnitIds.Contains(row.UnitId))
                .ToArray();
            var addedOccurrences = currentForSource
                .Where(row => !previousUnitIds.Contains(row.UnitId))
                .ToArray();
            if (removedOccurrences.Length == 0 || addedOccurrences.Length == 0)
            {
                continue;
            }

            if (removedOccurrences.Length != addedOccurrences.Length
                || removedOccurrences.Select(row => row.OrderValue).Distinct().Count()
                    != removedOccurrences.Length
                || addedOccurrences.Select(row => row.OrderValue).Distinct().Count()
                    != addedOccurrences.Length)
            {
                throw new AbwabDoorInclusionSynchronizationConflictException();
            }

            var previousByOrder = removedOccurrences.ToDictionary(row => row.OrderValue);
            var currentByOrder = addedOccurrences.ToDictionary(row => row.OrderValue);
            if (!previousByOrder.Keys.Order().SequenceEqual(currentByOrder.Keys.Order()))
            {
                throw new AbwabDoorInclusionSynchronizationConflictException();
            }

            foreach (var orderValue in previousByOrder.Keys.Order())
            {
                var previousUnitId = previousByOrder[orderValue].UnitId;
                var currentUnitId = currentByOrder[orderValue].UnitId;
                if (deletedUnitIds.Contains(previousUnitId) && addedUnitIds.Contains(currentUnitId))
                {
                    replacements.Add(new AbwabDoorInclusionUnitReplacement(previousUnitId, currentUnitId));
                }
            }
        }

        var distinctReplacements = replacements.Distinct().ToArray();
        if (distinctReplacements.Select(replacement => replacement.PreviousUnitId).Distinct().Count()
                != distinctReplacements.Length
            || distinctReplacements.Select(replacement => replacement.CurrentUnitId).Distinct().Count()
                != distinctReplacements.Length)
        {
            throw new AbwabDoorInclusionSynchronizationConflictException();
        }

        return distinctReplacements;
    }

    private sealed record PreparedUnitOwnershipRow(
        long PreparedSourceId,
        long UnitId,
        int OrderValue);

    private sealed record PreparedCurrentUnitOwnershipRow(
        long PreparedSourceId,
        long UnitId,
        int OrderValue,
        bool IsNew);
}
