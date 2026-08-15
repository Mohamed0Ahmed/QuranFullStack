using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task InsertPreparedUnitsAsync(
        int doorId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO linking_units (
                    door_id, identity, identity_hash, is_grouped, created_at, created_by)
                SELECT {doorId}, candidate.unit_identity, candidate.unit_identity_hash,
                       candidate.is_grouped, CURRENT_TIMESTAMP, {actorUserId}
                FROM linking_confirmation_new_units candidate
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM linking_units existing
                    WHERE existing.door_id = {doorId}
                      AND existing.identity_hash = candidate.unit_identity_hash)
                ORDER BY candidate.unit_identity_hash
                LIMIT {PersistenceBatchSize}
                """,
                cancellationToken));

        var allInserted = await db.Database.SqlQuery<bool>(
                $"""
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM linking_confirmation_new_units candidate
                    LEFT JOIN linking_units unit
                      ON unit.door_id = {doorId}
                     AND unit.identity_hash = candidate.unit_identity_hash
                     AND unit.identity = candidate.unit_identity
                    WHERE unit.id IS NULL
                ) AS "Value"
                """)
            .SingleAsync(cancellationToken);
        if (!allInserted)
        {
            throw new LinkingStaleVersionException();
        }
    }

    private async Task InsertPreparedUnitChildrenAsync(
        int actorUserId,
        CancellationToken cancellationToken)
    {
        await InsertPreparedUnitAyahsAsync(cancellationToken);
        await RemoveUnrequestedPreparedUnitWordsAsync(cancellationToken);
        await InsertPreparedUnitWordsAsync(cancellationToken);
        await InsertPreparedUnitDescriptionsAsync(actorUserId, cancellationToken);
        await ValidatePreparedUnitChildrenAsync(cancellationToken);
    }

    private async Task InsertPreparedUnitAyahsAsync(CancellationToken cancellationToken) =>
        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO linking_unit_ayahs (unit_id, ayah_id, order_value)
                SELECT candidate.unit_id, candidate.ayah_id, candidate.order_value
                FROM (
                    SELECT DISTINCT ON (unit.unit_id, ayah.ayah_id)
                           unit.unit_id,
                           ayah.ayah_id,
                           ayah.ayah_order AS order_value
                    FROM linking_confirmation_units unit
                    JOIN linking_prepared_ayahs ayah
                      ON ayah.unit_id = unit.prepared_unit_id
                     AND ayah.is_requested
                    WHERE unit.is_new
                    ORDER BY unit.unit_id, ayah.ayah_id, unit.prepared_unit_id
                ) candidate
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM linking_unit_ayahs existing
                    WHERE existing.unit_id = candidate.unit_id
                      AND existing.ayah_id = candidate.ayah_id)
                ORDER BY candidate.unit_id, candidate.order_value, candidate.ayah_id
                LIMIT {PersistenceBatchSize}
                """,
                cancellationToken));

    private async Task InsertPreparedUnitWordsAsync(CancellationToken cancellationToken) =>
        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO linking_unit_ayah_words (unit_ayah_id, quran_word_id, ayah_id)
                SELECT candidate.unit_ayah_id, candidate.quran_word_id, candidate.ayah_id
                FROM (
                    SELECT DISTINCT unit_ayah.id AS unit_ayah_id,
                           word.quran_word_id,
                           ayah.ayah_id
                    FROM linking_confirmation_units unit
                    JOIN linking_prepared_ayahs ayah
                      ON ayah.unit_id = unit.prepared_unit_id
                     AND ayah.is_requested
                    JOIN linking_prepared_ayah_words word
                      ON word.prepared_ayah_id = ayah.id
                     AND word.is_requested
                    JOIN linking_unit_ayahs unit_ayah
                      ON unit_ayah.unit_id = unit.unit_id
                     AND unit_ayah.ayah_id = ayah.ayah_id
                ) candidate
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM linking_unit_ayah_words existing
                    WHERE existing.unit_ayah_id = candidate.unit_ayah_id
                      AND existing.quran_word_id = candidate.quran_word_id)
                ORDER BY candidate.unit_ayah_id, candidate.quran_word_id
                LIMIT {PersistenceBatchSize}
                """,
                cancellationToken));

    private async Task RemoveUnrequestedPreparedUnitWordsAsync(CancellationToken cancellationToken) =>
        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT DISTINCT persisted_word.ctid
                    FROM linking_confirmation_units unit
                    JOIN linking_unit_ayahs persisted_ayah
                      ON persisted_ayah.unit_id = unit.unit_id
                    JOIN linking_unit_ayah_words persisted_word
                      ON persisted_word.unit_ayah_id = persisted_ayah.id
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM linking_confirmation_units desired_unit
                        JOIN linking_prepared_ayahs prepared_ayah
                          ON prepared_ayah.unit_id = desired_unit.prepared_unit_id
                        JOIN linking_prepared_ayah_words prepared_word
                          ON prepared_word.prepared_ayah_id = prepared_ayah.id
                         AND prepared_word.is_requested
                        WHERE desired_unit.unit_id = unit.unit_id
                          AND prepared_ayah.ayah_id = persisted_ayah.ayah_id
                          AND prepared_word.quran_word_id = persisted_word.quran_word_id)
                    LIMIT {PersistenceBatchSize}
                )
                DELETE FROM linking_unit_ayah_words persisted_word
                USING batch
                WHERE persisted_word.ctid = batch.ctid
                """,
                cancellationToken));

    private async Task InsertPreparedUnitDescriptionsAsync(
        int actorUserId,
        CancellationToken cancellationToken) =>
        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO linking_unit_ayah_descriptions (
                    unit_ayah_id, order_value, body, created_at, created_by, updated_at, updated_by)
                SELECT candidate.unit_ayah_id, candidate.order_value, candidate.body,
                       CURRENT_TIMESTAMP, {actorUserId}, CURRENT_TIMESTAMP, {actorUserId}
                FROM (
                    SELECT DISTINCT ON (unit_ayah.id, description.order_value)
                           unit_ayah.id AS unit_ayah_id,
                           description.order_value,
                           description.body
                    FROM linking_confirmation_units unit
                    JOIN linking_prepared_ayahs ayah
                      ON ayah.unit_id = unit.prepared_unit_id
                     AND ayah.is_requested
                    JOIN linking_prepared_ayah_descriptions description
                      ON description.prepared_ayah_id = ayah.id
                    JOIN linking_unit_ayahs unit_ayah
                      ON unit_ayah.unit_id = unit.unit_id
                     AND unit_ayah.ayah_id = ayah.ayah_id
                    WHERE unit.is_new
                    ORDER BY unit_ayah.id, description.order_value, unit.prepared_unit_id
                ) candidate
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM linking_unit_ayah_descriptions existing
                    WHERE existing.unit_ayah_id = candidate.unit_ayah_id
                      AND existing.order_value = candidate.order_value)
                ORDER BY candidate.unit_ayah_id, candidate.order_value
                LIMIT {PersistenceBatchSize}
                """,
                cancellationToken));

    private async Task ValidatePreparedUnitChildrenAsync(CancellationToken cancellationToken)
    {
        var complete = await db.Database.SqlQuery<bool>(
                $"""
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM linking_confirmation_units unit
                    JOIN linking_prepared_ayahs ayah
                      ON ayah.unit_id = unit.prepared_unit_id
                     AND ayah.is_requested
                    LEFT JOIN linking_unit_ayahs persisted
                      ON persisted.unit_id = unit.unit_id
                     AND persisted.ayah_id = ayah.ayah_id
                    WHERE unit.is_new
                      AND persisted.id IS NULL
                    UNION ALL
                    SELECT 1
                    FROM linking_confirmation_units unit
                    JOIN linking_prepared_ayahs ayah
                      ON ayah.unit_id = unit.prepared_unit_id
                     AND ayah.is_requested
                    JOIN linking_prepared_ayah_words word
                      ON word.prepared_ayah_id = ayah.id
                     AND word.is_requested
                    JOIN linking_unit_ayahs persisted_ayah
                      ON persisted_ayah.unit_id = unit.unit_id
                     AND persisted_ayah.ayah_id = ayah.ayah_id
                    LEFT JOIN linking_unit_ayah_words persisted_word
                      ON persisted_word.unit_ayah_id = persisted_ayah.id
                     AND persisted_word.quran_word_id = word.quran_word_id
                    WHERE persisted_word.unit_ayah_id IS NULL
                    UNION ALL
                    SELECT 1
                    FROM linking_confirmation_units unit
                    JOIN linking_unit_ayahs persisted_ayah
                      ON persisted_ayah.unit_id = unit.unit_id
                    JOIN linking_unit_ayah_words persisted_word
                      ON persisted_word.unit_ayah_id = persisted_ayah.id
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM linking_confirmation_units desired_unit
                        JOIN linking_prepared_ayahs prepared_ayah
                          ON prepared_ayah.unit_id = desired_unit.prepared_unit_id
                        JOIN linking_prepared_ayah_words prepared_word
                          ON prepared_word.prepared_ayah_id = prepared_ayah.id
                         AND prepared_word.is_requested
                        WHERE desired_unit.unit_id = unit.unit_id
                          AND prepared_ayah.ayah_id = persisted_ayah.ayah_id
                          AND prepared_word.quran_word_id = persisted_word.quran_word_id)
                    UNION ALL
                    SELECT 1
                    FROM linking_confirmation_units unit
                    JOIN linking_prepared_ayahs ayah
                      ON ayah.unit_id = unit.prepared_unit_id
                     AND ayah.is_requested
                    JOIN linking_prepared_ayah_descriptions description
                      ON description.prepared_ayah_id = ayah.id
                    JOIN linking_unit_ayahs persisted_ayah
                      ON persisted_ayah.unit_id = unit.unit_id
                     AND persisted_ayah.ayah_id = ayah.ayah_id
                    LEFT JOIN linking_unit_ayah_descriptions persisted_description
                      ON persisted_description.unit_ayah_id = persisted_ayah.id
                     AND persisted_description.order_value = description.order_value
                     AND persisted_description.body = description.body
                    WHERE unit.is_new
                      AND persisted_description.id IS NULL
                ) AS "Value"
                """)
            .SingleAsync(cancellationToken);
        if (!complete)
        {
            throw new LinkingStaleVersionException();
        }
    }
}
