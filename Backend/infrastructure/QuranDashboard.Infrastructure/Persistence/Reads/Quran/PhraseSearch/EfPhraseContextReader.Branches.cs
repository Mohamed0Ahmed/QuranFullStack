using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    private const string ContextBranchesSql = """
        , side_settings AS MATERIALIZED (
          SELECT 1::smallint AS side,
                 CARDINALITY(@previous_exact_token_ids::integer[]) AS selected_count,
                 @previous_ends_at_boundary AS selected_ends_at_boundary,
                 @previous_offset::bigint AS page_offset,
                 @previous_page_size::integer AS page_size
          UNION ALL
          SELECT 2::smallint,
                 CARDINALITY(@following_exact_token_ids::integer[]),
                 @following_ends_at_boundary,
                 @following_offset::bigint,
                 @following_page_size::integer
        ), side_occurrences AS MATERIALIZED (
          SELECT setting.side,
                 setting.selected_count,
                 setting.selected_ends_at_boundary,
                 occurrence.occurrence_id,
                 occurrence.surah_number,
                 occurrence.ayah_number,
                 occurrence.start_word_number,
                 occurrence.end_word_number,
                 occurrence.readable_word_count,
                 CASE
                   WHEN setting.side = 1
                     THEN occurrence.start_word_number - 1 = setting.selected_count
                   ELSE occurrence.readable_word_count - occurrence.end_word_number = setting.selected_count
                 END AS is_at_boundary,
                 CASE
                   WHEN @simple_mode THEN next_word.unique_simple_word_id
                   ELSE next_word.unique_tashkeel_word_id
                 END AS next_exact_token_id,
                 next_word.text_uthmani AS next_display_text,
                 CASE
                   WHEN setting.side = 1
                     THEN occurrence.start_word_number - 1 = setting.selected_count + 1
                   ELSE occurrence.readable_word_count - occurrence.end_word_number = setting.selected_count + 1
                 END AS child_is_at_boundary
          FROM side_settings AS setting
          CROSS JOIN filtered_occurrences AS occurrence
          LEFT JOIN quran_words AS next_word
            ON NOT setting.selected_ends_at_boundary
           AND next_word.ayah_id = occurrence.ayah_id
           AND NOT next_word.is_ayah_marker
           AND next_word.word_number = CASE
                 WHEN setting.side = 1
                   THEN occurrence.start_word_number - setting.selected_count - 1
                 ELSE occurrence.end_word_number + setting.selected_count + 1
               END
        ), side_summaries AS (
          SELECT setting.side,
                 COUNT(occurrence.occurrence_id) AS passes_through_count,
                 COUNT(occurrence.occurrence_id) FILTER (
                   WHERE occurrence.is_at_boundary
                 ) AS boundary_count
          FROM side_settings AS setting
          LEFT JOIN side_occurrences AS occurrence
            ON occurrence.side = setting.side
          GROUP BY setting.side
        ), token_options AS (
          SELECT occurrence.side,
                 occurrence.next_exact_token_id AS exact_token_id,
                 (ARRAY_AGG(
                   occurrence.next_display_text
                   ORDER BY occurrence.surah_number,
                            occurrence.ayah_number,
                            occurrence.start_word_number,
                            occurrence.occurrence_id
                 ))[1] AS display_text,
                 FALSE AS is_boundary,
                 COUNT(*) AS passes_through_count,
                 COUNT(*) FILTER (
                   WHERE occurrence.child_is_at_boundary
                 ) AS side_ends_here_count
          FROM side_occurrences AS occurrence
          WHERE NOT occurrence.selected_ends_at_boundary
            AND NOT occurrence.is_at_boundary
          GROUP BY occurrence.side, occurrence.next_exact_token_id
        ), boundary_options AS (
          SELECT summary.side,
                 NULL::integer AS exact_token_id,
                 NULL::text AS display_text,
                 TRUE AS is_boundary,
                 summary.boundary_count AS passes_through_count,
                 summary.boundary_count AS side_ends_here_count
          FROM side_summaries AS summary
          JOIN side_settings AS setting
            ON setting.side = summary.side
          WHERE NOT setting.selected_ends_at_boundary
            AND summary.boundary_count > 0
        ), all_options AS MATERIALIZED (
          SELECT * FROM token_options
          UNION ALL
          SELECT * FROM boundary_options
        ), option_summaries AS (
          SELECT setting.side,
                 COUNT(option.side)::integer AS total_options
          FROM side_settings AS setting
          LEFT JOIN all_options AS option
            ON option.side = setting.side
          GROUP BY setting.side
        ), ranked_options AS (
          SELECT option.*,
                 ROW_NUMBER() OVER (
                   PARTITION BY option.side
                   ORDER BY option.passes_through_count DESC,
                            option.exact_token_id NULLS FIRST
                 ) AS row_number
          FROM all_options AS option
        ), page_options AS (
          SELECT option.*
          FROM ranked_options AS option
          JOIN side_settings AS setting
            ON setting.side = option.side
          WHERE option.row_number > setting.page_offset
            AND option.row_number <= setting.page_offset + setting.page_size
        ), filtered_summary AS (
          SELECT COUNT(*)::integer AS total_count
          FROM filtered_occurrences
        ), overall_representative AS (
          SELECT occurrence.*
          FROM filtered_occurrences AS occurrence
          ORDER BY occurrence.surah_number,
                   occurrence.ayah_number,
                   occurrence.start_word_number,
                   occurrence.occurrence_id
          LIMIT 1
        )
        SELECT integrity.has_invalid_exact_identity,
               filtered.total_count,
               representative.occurrence_id,
               representative.ayah_id,
               representative.verse_key,
               representative.surah_number,
               representative.surah_name_arabic,
               representative.ayah_number,
               representative.page_from,
               representative.page_to,
               representative.start_word_number,
               representative.end_word_number,
               setting.side,
               summary.passes_through_count,
               summary.boundary_count,
               option_summary.total_options,
               option.row_number,
               option.exact_token_id,
               option.display_text,
               option.is_boundary,
               option.passes_through_count,
               option.side_ends_here_count
        FROM population_integrity AS integrity
        CROSS JOIN filtered_summary AS filtered
        CROSS JOIN side_settings AS setting
        JOIN side_summaries AS summary
          ON summary.side = setting.side
        JOIN option_summaries AS option_summary
          ON option_summary.side = setting.side
        LEFT JOIN overall_representative AS representative
          ON TRUE
        LEFT JOIN page_options AS option
          ON option.side = setting.side
        ORDER BY setting.side, option.row_number
        """;

    public async Task<PhraseSearchReadResult<PhraseContextBranchesResponse>> GetBranchesAsync(
        PhraseContextSelection selection,
        PhraseContextBranchPaging paging,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != selection.Resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.BuildChanged();
        }

        var cacheKey = PhraseSearchCacheKeys.ContextBranches(selection, paging);
        if (cache.TryGet(cacheKey, out PhraseContextBranchesResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.Success(cached);
        }

        var variantId = await LoadVariantIdAsync(
            snapshot.ActiveBuildId,
            selection.Resolution,
            cancellationToken);
        if (variantId is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.InvalidReference();
        }

        var loaded = await ReadBranchesAsync(
            snapshot.ActiveBuildId,
            variantId.Value,
            selection,
            paging,
            cancellationToken);
        var representativeRow = loaded.Representative;
        if (loaded.TotalOccurrenceCount == 0 || representativeRow is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextBranchesResponse>.InvalidReference();
        }

        var occurrences = await LoadContextOccurrencesAsync(
            [representativeRow],
            selection.Resolution,
            cancellationToken);
        var representative = occurrences[representativeRow.OccurrenceId];
        var scope = codec.ComputeScope(selection);
        var previous = CreateSidePage(
            selection,
            loaded.Previous,
            PhraseContextSide.Previous,
            paging.PreviousOffset,
            paging.PreviousPageSize,
            scope);
        var following = CreateSidePage(
            selection,
            loaded.Following,
            PhraseContextSide.Following,
            paging.FollowingOffset,
            paging.FollowingPageSize,
            scope);
        var bothBoundariesFixed = selection.Previous?.EndsAtBoundary == true
            && selection.Following?.EndsAtBoundary == true;
        var response = new PhraseContextBranchesResponse(
            snapshot.ActiveBuildId,
            CreateResolvedQuery(selection.Resolution, representative),
            CreateSelectedPath(selection.Resolution, selection.Previous, PhraseContextSide.Previous, representative),
            CreateSelectedPath(selection.Resolution, selection.Following, PhraseContextSide.Following, representative),
            previous,
            following,
            loaded.TotalOccurrenceCount,
            bothBoundariesFixed ? loaded.TotalOccurrenceCount : null);
        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(
            cacheKey,
            response,
            PhraseSearchCacheKeys.PageWeight(
                paging.PreviousPageSize + paging.FollowingPageSize));
        return new PhraseSearchReadResult<PhraseContextBranchesResponse>.Success(response);
    }

    private async Task<ContextBranchLoad> ReadBranchesAsync(
        Guid buildId,
        long variantId,
        PhraseContextSelection selection,
        PhraseContextBranchPaging paging,
        CancellationToken cancellationToken)
    {
        await using var command = CreateContextCommand(string.Concat(
            FilteredOccurrencesSql,
            ContextBranchesSql));
        AddContextParameters(command, buildId, variantId, selection);
        command.Parameters.AddWithValue("previous_offset", NpgsqlDbType.Bigint, (long)paging.PreviousOffset);
        command.Parameters.AddWithValue("previous_page_size", paging.PreviousPageSize);
        command.Parameters.AddWithValue("following_offset", NpgsqlDbType.Bigint, (long)paging.FollowingOffset);
        command.Parameters.AddWithValue("following_page_size", paging.FollowingPageSize);

        var totalOccurrenceCount = 0;
        var hasInvalidExactIdentity = false;
        ContextOccurrenceRow? representative = null;
        var sides = new Dictionary<PhraseContextSide, MutableBranchSideLoad>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hasInvalidExactIdentity = reader.GetBoolean(0);
            totalOccurrenceCount = reader.GetInt32(1);
            if (hasInvalidExactIdentity)
            {
                continue;
            }

            if (!reader.IsDBNull(2))
            {
                representative ??= ReadOccurrenceRow(reader, 2);
            }

            var side = (PhraseContextSide)reader.GetInt16(12);
            if (!sides.TryGetValue(side, out var sideLoad))
            {
                sideLoad = new MutableBranchSideLoad(
                    reader.GetInt64(13),
                    reader.GetInt64(14),
                    reader.GetInt32(15),
                    []);
                sides.Add(side, sideLoad);
            }

            if (reader.IsDBNull(16))
            {
                continue;
            }

            int? exactTokenId = reader.IsDBNull(17) ? null : reader.GetInt32(17);
            string? displayText = reader.IsDBNull(18) ? null : reader.GetString(18);
            var isBoundary = reader.GetBoolean(19);
            if (!isBoundary && (exactTokenId is null || displayText is null))
            {
                throw new InvalidDataException("PhraseSearch context token branch has no attested source word.");
            }

            sideLoad.Options.Add(new BranchOption(
                exactTokenId,
                displayText,
                isBoundary,
                reader.GetInt64(20),
                reader.GetInt64(21)));
        }

        if (hasInvalidExactIdentity)
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        if (!sides.TryGetValue(PhraseContextSide.Previous, out var previous)
            || !sides.TryGetValue(PhraseContextSide.Following, out var following))
        {
            throw new InvalidDataException("PhraseSearch context branch query did not return both sides.");
        }

        if (previous.PassesThroughCount != totalOccurrenceCount
            || following.PassesThroughCount != totalOccurrenceCount)
        {
            throw new InvalidDataException("PhraseSearch context branch totals do not match the filtered population.");
        }

        return new ContextBranchLoad(
            totalOccurrenceCount,
            representative,
            previous.ToImmutable(),
            following.ToImmutable());
    }

    private sealed record ContextBranchLoad(
        int TotalOccurrenceCount,
        ContextOccurrenceRow? Representative,
        BranchSideLoad Previous,
        BranchSideLoad Following);

    private sealed record BranchSideLoad(
        long PassesThroughCount,
        long BoundaryCount,
        int TotalOptions,
        IReadOnlyList<BranchOption> Options);

    private sealed record BranchOption(
        int? ExactTokenId,
        string? DisplayText,
        bool IsBoundary,
        long PassesThroughCount,
        long SideEndsHereCount);

    private sealed record MutableBranchSideLoad(
        long PassesThroughCount,
        long BoundaryCount,
        int TotalOptions,
        List<BranchOption> Options)
    {
        internal BranchSideLoad ToImmutable() => new(
            PassesThroughCount,
            BoundaryCount,
            TotalOptions,
            Options);
    }
}
