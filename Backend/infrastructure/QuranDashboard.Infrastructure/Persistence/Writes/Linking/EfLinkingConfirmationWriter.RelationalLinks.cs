using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task SynchronizePreparedContributionLinksAsync(
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO linking_confirmation_orphan_candidates (unit_id)
            SELECT DISTINCT existing.unit_id
            FROM linking_confirmation_sources source
            JOIN linking_source_contribution_units existing
              ON existing.source_contribution_id = source.contribution_id
            WHERE NOT EXISTS (
                SELECT 1
                FROM linking_confirmation_units desired
                WHERE desired.prepared_source_id = source.prepared_source_id
                  AND desired.unit_id = existing.unit_id)
            ON CONFLICT DO NOTHING
            """,
            cancellationToken);

        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT existing.ctid
                    FROM linking_source_contribution_units existing
                    JOIN linking_confirmation_sources source
                      ON source.contribution_id = existing.source_contribution_id
                    WHERE existing.order_value > 0
                    ORDER BY existing.source_contribution_id, existing.order_value
                    LIMIT {PersistenceBatchSize}
                )
                UPDATE linking_source_contribution_units existing
                SET order_value = -existing.order_value
                FROM batch
                WHERE existing.ctid = batch.ctid
                """,
                cancellationToken));

        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT existing.ctid
                    FROM linking_source_contribution_units existing
                    JOIN linking_confirmation_sources source
                      ON source.contribution_id = existing.source_contribution_id
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM linking_confirmation_units desired
                        WHERE desired.prepared_source_id = source.prepared_source_id
                          AND desired.unit_id = existing.unit_id)
                    ORDER BY existing.source_contribution_id, existing.unit_id
                    LIMIT {PersistenceBatchSize}
                )
                DELETE FROM linking_source_contribution_units existing
                USING batch
                WHERE existing.ctid = batch.ctid
                """,
                cancellationToken));

        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT existing.ctid, desired.order_value
                    FROM linking_source_contribution_units existing
                    JOIN linking_confirmation_sources source
                      ON source.contribution_id = existing.source_contribution_id
                    JOIN linking_confirmation_units desired
                      ON desired.prepared_source_id = source.prepared_source_id
                     AND desired.unit_id = existing.unit_id
                    WHERE existing.order_value < 0
                    ORDER BY existing.source_contribution_id, desired.order_value
                    LIMIT {PersistenceBatchSize}
                )
                UPDATE linking_source_contribution_units existing
                SET order_value = batch.order_value
                FROM batch
                WHERE existing.ctid = batch.ctid
                """,
                cancellationToken));

        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO linking_source_contribution_units (
                    source_contribution_id, unit_id, order_value)
                SELECT source.contribution_id, desired.unit_id, desired.order_value
                FROM linking_confirmation_units desired
                JOIN linking_confirmation_sources source
                  ON source.prepared_source_id = desired.prepared_source_id
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM linking_source_contribution_units existing
                    WHERE existing.source_contribution_id = source.contribution_id
                      AND existing.unit_id = desired.unit_id)
                ORDER BY source.contribution_id, desired.order_value
                LIMIT {PersistenceBatchSize}
                """,
                cancellationToken));

        var exact = await db.Database.SqlQuery<bool>(
                $"""
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM linking_source_contribution_units existing
                    JOIN linking_confirmation_sources source
                      ON source.contribution_id = existing.source_contribution_id
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM linking_confirmation_units desired
                        WHERE desired.prepared_source_id = source.prepared_source_id
                          AND desired.unit_id = existing.unit_id
                          AND desired.order_value = existing.order_value)
                    UNION ALL
                    SELECT 1
                    FROM linking_confirmation_units desired
                    JOIN linking_confirmation_sources source
                      ON source.prepared_source_id = desired.prepared_source_id
                    LEFT JOIN linking_source_contribution_units existing
                      ON existing.source_contribution_id = source.contribution_id
                     AND existing.unit_id = desired.unit_id
                     AND existing.order_value = desired.order_value
                    WHERE existing.unit_id IS NULL
                ) AS "Value"
                """)
            .SingleAsync(cancellationToken);
        if (!exact)
        {
            throw new LinkingStaleVersionException();
        }
    }

    private async Task CreateRelationalOrphanWorksetAsync(CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            CREATE TEMP TABLE linking_confirmation_orphans ON COMMIT DROP AS
            SELECT candidate.unit_id
            FROM linking_confirmation_orphan_candidates candidate
            WHERE NOT EXISTS (
                SELECT 1
                FROM linking_source_contribution_units link
                WHERE link.unit_id = candidate.unit_id);

            ALTER TABLE linking_confirmation_orphans
                ADD PRIMARY KEY (unit_id);
            """,
            cancellationToken);

    private async Task RemoveRelationalOrphanUnitsAsync(CancellationToken cancellationToken)
    {
        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT description.ctid
                    FROM linking_unit_ayah_descriptions description
                    JOIN linking_unit_ayahs ayah ON ayah.id = description.unit_ayah_id
                    JOIN linking_confirmation_orphans orphan ON orphan.unit_id = ayah.unit_id
                    LIMIT {PersistenceBatchSize}
                )
                DELETE FROM linking_unit_ayah_descriptions description
                USING batch
                WHERE description.ctid = batch.ctid
                """,
                cancellationToken));
        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT word.ctid
                    FROM linking_unit_ayah_words word
                    JOIN linking_unit_ayahs ayah ON ayah.id = word.unit_ayah_id
                    JOIN linking_confirmation_orphans orphan ON orphan.unit_id = ayah.unit_id
                    LIMIT {PersistenceBatchSize}
                )
                DELETE FROM linking_unit_ayah_words word
                USING batch
                WHERE word.ctid = batch.ctid
                """,
                cancellationToken));
        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT ayah.ctid
                    FROM linking_unit_ayahs ayah
                    JOIN linking_confirmation_orphans orphan ON orphan.unit_id = ayah.unit_id
                    LIMIT {PersistenceBatchSize}
                )
                DELETE FROM linking_unit_ayahs ayah
                USING batch
                WHERE ayah.ctid = batch.ctid
                """,
                cancellationToken));
        await ExecuteBatchesAsync(
            () => db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                WITH batch AS (
                    SELECT unit.ctid
                    FROM linking_units unit
                    JOIN linking_confirmation_orphans orphan ON orphan.unit_id = unit.id
                    LIMIT {PersistenceBatchSize}
                )
                DELETE FROM linking_units unit
                USING batch
                WHERE unit.ctid = batch.ctid
                """,
                cancellationToken));
    }
}
