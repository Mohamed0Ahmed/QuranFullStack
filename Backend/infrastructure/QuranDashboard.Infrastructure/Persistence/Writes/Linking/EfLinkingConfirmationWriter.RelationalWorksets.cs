using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Preflight;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingConfirmationWriter
{
    private async Task ApplyPreparedRelationalStateAsync(
        Guid preflightId,
        long operationId,
        int doorId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        await PersistPreparedContributionsAsync(
            preflightId,
            operationId,
            doorId,
            actorUserId,
            cancellationToken);
        await CreateRelationalWorksetsAsync(preflightId, doorId, cancellationToken);
        var previousSnapshots = await LoadPreviousUnitSnapshotsAsync(
            preflightId,
            doorId,
            cancellationToken);
        await ValidateUnitIdentitiesAsync(preflightId, doorId, cancellationToken);
        await InsertPreparedUnitsAsync(doorId, actorUserId, cancellationToken);
        await MapPreparedUnitsAsync(preflightId, doorId, cancellationToken);
        await InsertPreparedUnitChildrenAsync(actorUserId, cancellationToken);
        await SynchronizePreparedContributionLinksAsync(cancellationToken);
        await CreateRelationalOrphanWorksetAsync(cancellationToken);
        var mutations = await CreatePreparedMutationSetAsync(previousSnapshots, cancellationToken);
        await inclusionSynchronizer.SynchronizeAsync(
            doorId,
            mutations,
            actorUserId,
            cancellationToken);
        await RemoveRelationalOrphanUnitsAsync(cancellationToken);
        await SynchronizeRelationalDoorStateAsync(doorId, actorUserId, cancellationToken);
    }

    private async Task PersistPreparedContributionsAsync(
        Guid preflightId,
        long operationId,
        int doorId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var newSource = LinkingPreflightTokens.ToToken(LinkingPreflightClassification.NewSource);
        var update = LinkingPreflightTokens.ToToken(LinkingPreflightClassification.Update);
        var expectedNew = await CountPreparedSourcesAsync(preflightId, newSource, cancellationToken);
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO linking_source_contributions (
                operation_id, door_id, order_value, contribution_mode, source_kind,
                source_identity, source_identity_hash, label, scope, root_id, lemma_id, stem_id,
                unique_simple_word_id, unique_tashkeel_word_id, word_type_tashkeel_word_id,
                resolved_ayah_count, resolved_at_utc, created_at, created_by, updated_at, updated_by)
            SELECT {operationId}, {doorId}, source.order_value, source.contribution_mode, source.source_kind,
                   source.contribution_identity, source.contribution_identity_hash, source.label,
                   (source.descriptor_document ->> 'scopeJson')::jsonb,
                   (source.descriptor_document ->> 'rootId')::integer,
                   (source.descriptor_document ->> 'lemmaId')::integer,
                   (source.descriptor_document ->> 'stemId')::integer,
                   (source.descriptor_document ->> 'uniqueSimpleWordId')::integer,
                   (source.descriptor_document ->> 'uniqueTashkeelWordId')::integer,
                   (source.descriptor_document ->> 'wordTypeTashkeelWordId')::integer,
                   source.total_ayah_count,
                   COALESCE(preflight.ready_at_utc, preflight.updated_at_utc),
                   CURRENT_TIMESTAMP, {actorUserId}, CURRENT_TIMESTAMP, {actorUserId}
            FROM linking_prepared_sources source
            JOIN linking_prepared_preflights preflight ON preflight.id = source.preflight_id
            WHERE source.preflight_id = {preflightId}
              AND source.classification = {newSource}
            """,
            cancellationToken);
        if (inserted != expectedNew)
        {
            throw new LinkingStaleVersionException();
        }

        var expectedUpdates = await CountPreparedSourcesAsync(preflightId, update, cancellationToken);
        var updated = await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE linking_source_contributions contribution
            SET operation_id = {operationId},
                order_value = source.order_value,
                contribution_mode = source.contribution_mode,
                source_kind = source.source_kind,
                source_identity = source.contribution_identity,
                source_identity_hash = source.contribution_identity_hash,
                label = source.label,
                scope = (source.descriptor_document ->> 'scopeJson')::jsonb,
                root_id = (source.descriptor_document ->> 'rootId')::integer,
                lemma_id = (source.descriptor_document ->> 'lemmaId')::integer,
                stem_id = (source.descriptor_document ->> 'stemId')::integer,
                unique_simple_word_id = (source.descriptor_document ->> 'uniqueSimpleWordId')::integer,
                unique_tashkeel_word_id = (source.descriptor_document ->> 'uniqueTashkeelWordId')::integer,
                word_type_tashkeel_word_id =
                    (source.descriptor_document ->> 'wordTypeTashkeelWordId')::integer,
                resolved_ayah_count = source.total_ayah_count,
                resolved_at_utc = COALESCE(preflight.ready_at_utc, preflight.updated_at_utc),
                updated_at = CURRENT_TIMESTAMP,
                updated_by = {actorUserId}
            FROM linking_prepared_sources source
            JOIN linking_prepared_preflights preflight ON preflight.id = source.preflight_id
            WHERE source.preflight_id = {preflightId}
              AND source.classification = {update}
              AND contribution.id = source.existing_contribution_id
              AND contribution.door_id = {doorId}
              AND contribution.deleted_at IS NULL
            """,
            cancellationToken);
        if (updated != expectedUpdates)
        {
            throw new LinkingStaleVersionException();
        }
    }

    private async Task CreateRelationalWorksetsAsync(
        Guid preflightId,
        int doorId,
        CancellationToken cancellationToken)
    {
        var newSource = LinkingPreflightTokens.ToToken(LinkingPreflightClassification.NewSource);
        var update = LinkingPreflightTokens.ToToken(LinkingPreflightClassification.Update);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            CREATE TEMP TABLE linking_confirmation_sources ON COMMIT DROP AS
            SELECT source.id AS prepared_source_id,
                   contribution.id AS contribution_id
            FROM linking_prepared_sources source
            JOIN linking_source_contributions contribution
              ON contribution.door_id = {doorId}
             AND contribution.source_identity_hash = source.contribution_identity_hash
             AND contribution.source_identity = source.contribution_identity
             AND contribution.deleted_at IS NULL
            WHERE source.preflight_id = {preflightId}
              AND source.classification IN ({newSource}, {update});

            ALTER TABLE linking_confirmation_sources
                ADD PRIMARY KEY (prepared_source_id);
            CREATE UNIQUE INDEX ix_linking_confirmation_sources_contribution
                ON linking_confirmation_sources (contribution_id);

            CREATE TEMP TABLE linking_confirmation_affected_ayahs (
                ayah_id integer PRIMARY KEY)
                ON COMMIT DROP;

            INSERT INTO linking_confirmation_affected_ayahs (ayah_id)
            SELECT DISTINCT unit_ayah.ayah_id
            FROM linking_prepared_affected_contributions affected
            JOIN linking_source_contribution_units contribution_unit
              ON contribution_unit.source_contribution_id = affected.contribution_id
            JOIN linking_unit_ayahs unit_ayah ON unit_ayah.unit_id = contribution_unit.unit_id
            WHERE affected.preflight_id = {preflightId}
            ON CONFLICT DO NOTHING;

            INSERT INTO linking_confirmation_affected_ayahs (ayah_id)
            SELECT DISTINCT ayah.ayah_id
            FROM linking_prepared_ayahs ayah
            JOIN linking_confirmation_sources source
              ON source.prepared_source_id = ayah.source_id
            WHERE ayah.preflight_id = {preflightId}
              AND ayah.is_requested
            ON CONFLICT DO NOTHING;

            CREATE TEMP TABLE linking_confirmation_orphan_candidates (
                unit_id bigint PRIMARY KEY)
                ON COMMIT DROP;

            CREATE TEMP TABLE linking_confirmation_previous_units ON COMMIT DROP AS
            SELECT source.prepared_source_id,
                   existing.unit_id,
                   existing.order_value
            FROM linking_confirmation_sources source
            JOIN linking_source_contribution_units existing
              ON existing.source_contribution_id = source.contribution_id;

            CREATE INDEX ix_linking_confirmation_previous_units_source
                ON linking_confirmation_previous_units (prepared_source_id, order_value, unit_id);
            """,
            cancellationToken);

        var expectedSources = await db.LinkingPreparedSources.AsNoTracking()
            .CountAsync(
                source => source.PreflightId == preflightId
                    && (source.Classification == newSource || source.Classification == update),
                cancellationToken);
        var mappedSources = await db.Database.SqlQuery<int>(
                $"SELECT COUNT(*)::integer AS \"Value\" FROM linking_confirmation_sources")
            .SingleAsync(cancellationToken);
        if (mappedSources != expectedSources)
        {
            throw new LinkingStaleVersionException();
        }
    }

    private async Task ValidateUnitIdentitiesAsync(
        Guid preflightId,
        int doorId,
        CancellationToken cancellationToken)
    {
        var collision = await db.Database.SqlQuery<bool>(
                $"""
                SELECT EXISTS (
                    SELECT 1
                    FROM linking_prepared_units candidate
                    JOIN linking_confirmation_sources candidate_source
                      ON candidate_source.prepared_source_id = candidate.source_id
                    JOIN linking_prepared_units other
                      ON other.preflight_id = candidate.preflight_id
                     AND other.unit_identity_hash = candidate.unit_identity_hash
                     AND other.id > candidate.id
                     AND other.unit_identity <> candidate.unit_identity
                    JOIN linking_confirmation_sources other_source
                      ON other_source.prepared_source_id = other.source_id
                    WHERE candidate.preflight_id = {preflightId}
                    UNION ALL
                    SELECT 1
                    FROM linking_prepared_units candidate
                    JOIN linking_confirmation_sources candidate_source
                      ON candidate_source.prepared_source_id = candidate.source_id
                    JOIN linking_units existing
                      ON existing.door_id = {doorId}
                     AND existing.identity_hash = candidate.unit_identity_hash
                     AND existing.identity <> candidate.unit_identity
                    WHERE candidate.preflight_id = {preflightId}
                ) AS "Value"
                """)
            .SingleAsync(cancellationToken);
        if (collision)
        {
            throw new InvalidOperationException("A linking unit identity hash collision was detected.");
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            CREATE TEMP TABLE linking_confirmation_new_units ON COMMIT DROP AS
            SELECT DISTINCT ON (prepared.unit_identity_hash)
                   prepared.unit_identity_hash,
                   prepared.unit_identity,
                   prepared.is_grouped
            FROM linking_prepared_units prepared
            JOIN linking_confirmation_sources source
              ON source.prepared_source_id = prepared.source_id
            LEFT JOIN linking_units existing
              ON existing.door_id = {doorId}
             AND existing.identity_hash = prepared.unit_identity_hash
            WHERE prepared.preflight_id = {preflightId}
              AND existing.id IS NULL
            ORDER BY prepared.unit_identity_hash, prepared.id;

            CREATE UNIQUE INDEX ix_linking_confirmation_new_units_hash
                ON linking_confirmation_new_units (unit_identity_hash);
            """,
            cancellationToken);
    }

    private async Task MapPreparedUnitsAsync(
        Guid preflightId,
        int doorId,
        CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            CREATE TEMP TABLE linking_confirmation_units ON COMMIT DROP AS
            SELECT prepared.id AS prepared_unit_id,
                   prepared.source_id AS prepared_source_id,
                   prepared.order_value,
                   unit.id AS unit_id,
                   new_unit.unit_identity_hash IS NOT NULL AS is_new
            FROM linking_prepared_units prepared
            JOIN linking_confirmation_sources source
              ON source.prepared_source_id = prepared.source_id
            JOIN linking_units unit
              ON unit.door_id = {doorId}
             AND unit.identity_hash = prepared.unit_identity_hash
             AND unit.identity = prepared.unit_identity
            LEFT JOIN linking_confirmation_new_units new_unit
              ON new_unit.unit_identity_hash = prepared.unit_identity_hash
            WHERE prepared.preflight_id = {preflightId};

            ALTER TABLE linking_confirmation_units
                ADD PRIMARY KEY (prepared_unit_id);
            CREATE INDEX ix_linking_confirmation_units_source
                ON linking_confirmation_units (prepared_source_id, unit_id);
            """,
            cancellationToken);

        var newSource = LinkingPreflightTokens.ToToken(LinkingPreflightClassification.NewSource);
        var update = LinkingPreflightTokens.ToToken(LinkingPreflightClassification.Update);
        var expectedUnits = await db.LinkingPreparedUnits.AsNoTracking()
            .CountAsync(
                unit => unit.PreflightId == preflightId
                    && db.LinkingPreparedSources.Any(source =>
                        source.Id == unit.SourceId
                        && source.PreflightId == preflightId
                        && (source.Classification == newSource || source.Classification == update)),
                cancellationToken);
        var mappedUnits = await db.Database.SqlQuery<int>(
                $"SELECT COUNT(*)::integer AS \"Value\" FROM linking_confirmation_units")
            .SingleAsync(cancellationToken);
        var duplicateSourceUnit = await db.Database.SqlQuery<bool>(
                $"""
                SELECT EXISTS (
                    SELECT 1
                    FROM linking_confirmation_units
                    GROUP BY prepared_source_id, unit_id
                    HAVING COUNT(*) > 1
                ) AS "Value"
                """)
            .SingleAsync(cancellationToken);
        if (mappedUnits != expectedUnits || duplicateSourceUnit)
        {
            throw new LinkingStaleVersionException();
        }
    }

    private async Task<int> CountPreparedSourcesAsync(
        Guid preflightId,
        string classification,
        CancellationToken cancellationToken) =>
        await db.LinkingPreparedSources.AsNoTracking().CountAsync(
            source => source.PreflightId == preflightId && source.Classification == classification,
            cancellationToken);
}
