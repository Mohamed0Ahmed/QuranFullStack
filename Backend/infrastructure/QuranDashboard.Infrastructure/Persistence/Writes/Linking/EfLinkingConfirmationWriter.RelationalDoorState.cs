using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task SynchronizeRelationalDoorStateAsync(
        int doorId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            CREATE TEMP TABLE linking_confirmation_desired_ayahs ON COMMIT DROP AS
            SELECT DISTINCT unit_ayah.ayah_id
            FROM linking_confirmation_affected_ayahs affected
            JOIN linking_unit_ayahs unit_ayah ON unit_ayah.ayah_id = affected.ayah_id
            JOIN linking_units unit ON unit.id = unit_ayah.unit_id
            JOIN linking_source_contribution_units contribution_unit
              ON contribution_unit.unit_id = unit.id
            JOIN linking_source_contributions contribution
              ON contribution.id = contribution_unit.source_contribution_id
             AND contribution.deleted_at IS NULL
            WHERE unit.door_id = {doorId}
              AND contribution.door_id = {doorId};

            ALTER TABLE linking_confirmation_desired_ayahs
                ADD PRIMARY KEY (ayah_id);

            CREATE TEMP TABLE linking_confirmation_desired_words ON COMMIT DROP AS
            SELECT DISTINCT unit_ayah.ayah_id, unit_word.quran_word_id
            FROM linking_confirmation_affected_ayahs affected
            JOIN linking_unit_ayahs unit_ayah ON unit_ayah.ayah_id = affected.ayah_id
            JOIN linking_unit_ayah_words unit_word ON unit_word.unit_ayah_id = unit_ayah.id
            JOIN linking_units unit ON unit.id = unit_ayah.unit_id
            JOIN linking_source_contribution_units contribution_unit
              ON contribution_unit.unit_id = unit.id
            JOIN linking_source_contributions contribution
              ON contribution.id = contribution_unit.source_contribution_id
             AND contribution.deleted_at IS NULL
            WHERE unit.door_id = {doorId}
              AND contribution.door_id = {doorId};

            ALTER TABLE linking_confirmation_desired_words
                ADD PRIMARY KEY (ayah_id, quran_word_id);
            """,
            cancellationToken);

        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO linking_door_ayahs (door_id, ayah_id, created_at, created_by)
                SELECT {doorId}, desired.ayah_id, CURRENT_TIMESTAMP, {actorUserId}
                FROM linking_confirmation_desired_ayahs desired
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM linking_door_ayahs existing
                    WHERE existing.door_id = {doorId}
                      AND existing.ayah_id = desired.ayah_id)
                ORDER BY desired.ayah_id
                LIMIT {PersistenceBatchSize}
                """,
                cancellationToken));

        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT word.ctid
                    FROM linking_door_ayah_words word
                    JOIN linking_door_ayahs door_ayah ON door_ayah.id = word.door_ayah_id
                    JOIN linking_confirmation_affected_ayahs affected
                      ON affected.ayah_id = door_ayah.ayah_id
                    LEFT JOIN linking_confirmation_desired_words desired
                      ON desired.ayah_id = door_ayah.ayah_id
                     AND desired.quran_word_id = word.quran_word_id
                    WHERE door_ayah.door_id = {doorId}
                      AND desired.ayah_id IS NULL
                    LIMIT {PersistenceBatchSize}
                )
                DELETE FROM linking_door_ayah_words word
                USING batch
                WHERE word.ctid = batch.ctid
                """,
                cancellationToken));

        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO linking_door_ayah_words (
                    door_ayah_id, quran_word_id, ayah_id, created_at, created_by)
                SELECT door_ayah.id, desired.quran_word_id, desired.ayah_id,
                       CURRENT_TIMESTAMP, {actorUserId}
                FROM linking_confirmation_desired_words desired
                JOIN linking_door_ayahs door_ayah
                  ON door_ayah.door_id = {doorId}
                 AND door_ayah.ayah_id = desired.ayah_id
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM linking_door_ayah_words existing
                    WHERE existing.door_ayah_id = door_ayah.id
                      AND existing.quran_word_id = desired.quran_word_id)
                ORDER BY desired.ayah_id, desired.quran_word_id
                LIMIT {PersistenceBatchSize}
                """,
                cancellationToken));

        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT door_ayah.ctid
                    FROM linking_door_ayahs door_ayah
                    JOIN linking_confirmation_affected_ayahs affected
                      ON affected.ayah_id = door_ayah.ayah_id
                    LEFT JOIN linking_confirmation_desired_ayahs desired
                      ON desired.ayah_id = door_ayah.ayah_id
                    WHERE door_ayah.door_id = {doorId}
                      AND desired.ayah_id IS NULL
                    LIMIT {PersistenceBatchSize}
                )
                DELETE FROM linking_door_ayahs door_ayah
                USING batch
                WHERE door_ayah.ctid = batch.ctid
                """,
                cancellationToken));

        var exact = await db.Database.SqlQuery<bool>(
                $"""
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM linking_confirmation_affected_ayahs affected
                    LEFT JOIN linking_confirmation_desired_ayahs desired
                      ON desired.ayah_id = affected.ayah_id
                    LEFT JOIN linking_door_ayahs door_ayah
                      ON door_ayah.door_id = {doorId}
                     AND door_ayah.ayah_id = affected.ayah_id
                    WHERE (desired.ayah_id IS NULL) <> (door_ayah.id IS NULL)
                    UNION ALL
                    SELECT 1
                    FROM linking_confirmation_desired_words desired
                    JOIN linking_door_ayahs door_ayah
                      ON door_ayah.door_id = {doorId}
                     AND door_ayah.ayah_id = desired.ayah_id
                    LEFT JOIN linking_door_ayah_words word
                      ON word.door_ayah_id = door_ayah.id
                     AND word.quran_word_id = desired.quran_word_id
                    WHERE word.door_ayah_id IS NULL
                    UNION ALL
                    SELECT 1
                    FROM linking_door_ayah_words word
                    JOIN linking_door_ayahs door_ayah ON door_ayah.id = word.door_ayah_id
                    JOIN linking_confirmation_affected_ayahs affected
                      ON affected.ayah_id = door_ayah.ayah_id
                    LEFT JOIN linking_confirmation_desired_words desired
                      ON desired.ayah_id = door_ayah.ayah_id
                     AND desired.quran_word_id = word.quran_word_id
                    WHERE door_ayah.door_id = {doorId}
                      AND desired.ayah_id IS NULL
                ) AS "Value"
                """)
            .SingleAsync(cancellationToken);
        if (!exact)
        {
            throw new LinkingStaleVersionException();
        }

        var updated = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE abwab_doors
            SET updated_at = CURRENT_TIMESTAMP,
                updated_by = {actorUserId}
            WHERE id = {doorId}
            """,
            cancellationToken);
        if (updated != 1)
        {
            throw new LinkingStaleVersionException();
        }
    }
}
