using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed class RelationalDoorStateRebuilder(QuranDashboardDbContext db)
{
    public async Task RebuildAsync(
        int doorId,
        IReadOnlyCollection<int> affectedAyahIds,
        int actorUserId,
        bool deleteEmptyAyahs,
        CancellationToken cancellationToken)
    {
        if (affectedAyahIds.Count == 0)
        {
            return;
        }

        var ayahIds = affectedAyahIds.Distinct().Order().ToArray();
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

        var exact = await db.Database.SqlQuery<bool>(
                $"""
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM linking_unit_ayahs unit_ayah
                    JOIN linking_units unit ON unit.id = unit_ayah.unit_id
                    JOIN linking_source_contribution_units mapping ON mapping.unit_id = unit.id
                    JOIN linking_source_contributions contribution
                      ON contribution.id = mapping.source_contribution_id
                     AND contribution.deleted_at IS NULL
                    LEFT JOIN linking_door_ayahs door_ayah
                      ON door_ayah.door_id = {doorId}
                     AND door_ayah.ayah_id = unit_ayah.ayah_id
                    WHERE unit.door_id = {doorId}
                      AND contribution.door_id = {doorId}
                      AND unit_ayah.ayah_id = ANY ({ayahIds})
                      AND door_ayah.id IS NULL
                    UNION ALL
                    SELECT 1
                    FROM linking_door_ayahs door_ayah
                    WHERE {deleteEmptyAyahs}
                      AND door_ayah.door_id = {doorId}
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
                    UNION ALL
                    SELECT 1
                    FROM linking_unit_ayah_words unit_word
                    JOIN linking_unit_ayahs unit_ayah ON unit_ayah.id = unit_word.unit_ayah_id
                    JOIN linking_units unit ON unit.id = unit_ayah.unit_id
                    JOIN linking_source_contribution_units mapping ON mapping.unit_id = unit.id
                    JOIN linking_source_contributions contribution
                      ON contribution.id = mapping.source_contribution_id
                     AND contribution.deleted_at IS NULL
                    JOIN linking_door_ayahs door_ayah
                      ON door_ayah.door_id = {doorId}
                     AND door_ayah.ayah_id = unit_ayah.ayah_id
                    LEFT JOIN linking_door_ayah_words door_word
                      ON door_word.door_ayah_id = door_ayah.id
                     AND door_word.quran_word_id = unit_word.quran_word_id
                    WHERE unit.door_id = {doorId}
                      AND contribution.door_id = {doorId}
                      AND unit_ayah.ayah_id = ANY ({ayahIds})
                      AND door_word.door_ayah_id IS NULL
                    UNION ALL
                    SELECT 1
                    FROM linking_door_ayah_words door_word
                    JOIN linking_door_ayahs door_ayah ON door_ayah.id = door_word.door_ayah_id
                    WHERE door_ayah.door_id = {doorId}
                      AND door_ayah.ayah_id = ANY ({ayahIds})
                      AND NOT EXISTS (
                          SELECT 1
                          FROM linking_unit_ayah_words unit_word
                          JOIN linking_unit_ayahs unit_ayah ON unit_ayah.id = unit_word.unit_ayah_id
                          JOIN linking_units unit ON unit.id = unit_ayah.unit_id
                          JOIN linking_source_contribution_units mapping ON mapping.unit_id = unit.id
                          JOIN linking_source_contributions contribution
                            ON contribution.id = mapping.source_contribution_id
                           AND contribution.deleted_at IS NULL
                          WHERE unit.door_id = {doorId}
                            AND contribution.door_id = {doorId}
                            AND unit_ayah.ayah_id = door_ayah.ayah_id
                            AND unit_word.quran_word_id = door_word.quran_word_id)
                ) AS "Value"
                """)
            .SingleAsync(cancellationToken);
        if (!exact)
        {
            throw new LinkingStaleVersionException();
        }
    }
}
