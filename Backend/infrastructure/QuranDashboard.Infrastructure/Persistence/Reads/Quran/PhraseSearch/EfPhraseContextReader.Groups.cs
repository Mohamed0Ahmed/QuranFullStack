using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseContextReader
{
    private const string ContextGroupsSql = """
        , occurrence_contexts AS MATERIALIZED (
          SELECT occurrence.occurrence_id,
                 occurrence.ayah_id,
                 occurrence.verse_key,
                 occurrence.surah_number,
                 occurrence.surah_name_arabic,
                 occurrence.ayah_number,
                 occurrence.page_from,
                 occurrence.page_to,
                 occurrence.start_word_number,
                 occurrence.end_word_number,
                 occurrence.readable_word_count,
                 COALESCE(
                   ARRAY_AGG(
                     CASE
                       WHEN @simple_mode THEN word.unique_simple_word_id
                       ELSE word.unique_tashkeel_word_id
                     END
                     ORDER BY word.word_number DESC
                   ) FILTER (WHERE word.word_number < occurrence.start_word_number),
                   ARRAY[]::integer[]
                 ) AS previous_ids,
                 COALESCE(
                   ARRAY_AGG(
                     CASE
                       WHEN @simple_mode THEN word.unique_simple_word_id
                       ELSE word.unique_tashkeel_word_id
                     END
                     ORDER BY word.word_number
                   ) FILTER (WHERE word.word_number > occurrence.end_word_number),
                   ARRAY[]::integer[]
                 ) AS following_ids
          FROM filtered_occurrences AS occurrence
          JOIN quran_words AS word
            ON word.ayah_id = occurrence.ayah_id
           AND NOT word.is_ayah_marker
          GROUP BY occurrence.occurrence_id,
                   occurrence.ayah_id,
                   occurrence.verse_key,
                   occurrence.surah_number,
                   occurrence.surah_name_arabic,
                   occurrence.ayah_number,
                   occurrence.page_from,
                   occurrence.page_to,
                   occurrence.start_word_number,
                   occurrence.end_word_number,
                   occurrence.readable_word_count
        ), grouped_contexts AS MATERIALIZED (
          SELECT previous_ids,
                 following_ids,
                 COUNT(*)::integer AS occurrence_count,
                 (ARRAY_AGG(
                   occurrence_id
                   ORDER BY surah_number,
                            ayah_number,
                            start_word_number,
                            occurrence_id
                 ))[1] AS representative_occurrence_id
          FROM occurrence_contexts
          GROUP BY previous_ids, following_ids
        ), group_summary AS (
          SELECT COUNT(*)::integer AS total_count
          FROM grouped_contexts
        ), ranked_groups AS (
          SELECT grouped.*,
                 ROW_NUMBER() OVER (
                   ORDER BY grouped.occurrence_count DESC,
                            grouped.previous_ids,
                            grouped.following_ids
                 ) AS row_number
          FROM grouped_contexts AS grouped
        ), page_groups AS (
          SELECT grouped.*
          FROM ranked_groups AS grouped
          WHERE grouped.row_number > @offset
            AND grouped.row_number <= @offset + @page_size
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
               summary.total_count,
               overall.occurrence_id,
               overall.ayah_id,
               overall.verse_key,
               overall.surah_number,
               overall.surah_name_arabic,
               overall.ayah_number,
               overall.page_from,
               overall.page_to,
               overall.start_word_number,
               overall.end_word_number,
               grouped.row_number,
               grouped.previous_ids,
               grouped.following_ids,
               grouped.occurrence_count,
               representative.occurrence_id,
               representative.ayah_id,
               representative.verse_key,
               representative.surah_number,
               representative.surah_name_arabic,
               representative.ayah_number,
               representative.page_from,
               representative.page_to,
               representative.start_word_number,
               representative.end_word_number
        FROM population_integrity AS integrity
        CROSS JOIN group_summary AS summary
        LEFT JOIN overall_representative AS overall
          ON TRUE
        LEFT JOIN page_groups AS grouped
          ON TRUE
        LEFT JOIN occurrence_contexts AS representative
          ON representative.occurrence_id = grouped.representative_occurrence_id
        ORDER BY grouped.row_number
        """;

    public async Task<PhraseSearchReadResult<PhraseContextGroupsResponse>> GetGroupsAsync(
        PhraseContextSelection selection,
        PhraseCursorPage paging,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseContextGroupsResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != selection.Resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextGroupsResponse>.BuildChanged();
        }

        var cacheKey = PhraseSearchCacheKeys.ContextGroups(selection, paging);
        if (cache.TryGet(cacheKey, out PhraseContextGroupsResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextGroupsResponse>.Success(cached);
        }

        var variantId = await LoadVariantIdAsync(
            snapshot.ActiveBuildId,
            selection.Resolution,
            cancellationToken);
        if (variantId is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextGroupsResponse>.InvalidReference();
        }

        var loaded = await ReadGroupsAsync(
            snapshot.ActiveBuildId,
            variantId.Value,
            selection,
            paging,
            cancellationToken);
        var overallRepresentative = loaded.OverallRepresentative;
        if (loaded.TotalCount == 0 || overallRepresentative is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseContextGroupsResponse>.InvalidReference();
        }

        var requestedRows = loaded.Items
            .Select(item => item.Representative)
            .Append(overallRepresentative)
            .ToList();
        var occurrences = await LoadContextOccurrencesAsync(
            requestedRows,
            selection.Resolution,
            cancellationToken);
        var representative = occurrences[overallRepresentative.OccurrenceId];
        var items = loaded.Items
            .Select(group => CreateGroup(
                selection.Resolution,
                group,
                occurrences[group.Representative.OccurrenceId]))
            .ToList();
        var scope = codec.ComputeScope(selection);
        var response = new PhraseContextGroupsResponse(
            snapshot.ActiveBuildId,
            CreateResolvedQuery(selection.Resolution, representative),
            CreateSelectedPath(selection.Resolution, selection.Previous, PhraseContextSide.Previous, representative),
            CreateSelectedPath(selection.Resolution, selection.Following, PhraseContextSide.Following, representative),
            loaded.TotalCount,
            CreateNextCursor(
                snapshot.ActiveBuildId,
                PhraseCursorKind.ContextGroups,
                paging.Offset,
                paging.PageSize,
                loaded.TotalCount,
                scope),
            items);
        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(cacheKey, response, PhraseSearchCacheKeys.PageWeight(paging.PageSize));
        return new PhraseSearchReadResult<PhraseContextGroupsResponse>.Success(response);
    }

    private async Task<ContextGroupPageLoad> ReadGroupsAsync(
        Guid buildId,
        long variantId,
        PhraseContextSelection selection,
        PhraseCursorPage paging,
        CancellationToken cancellationToken)
    {
        await using var command = CreateContextCommand(string.Concat(
            FilteredOccurrencesSql,
            ContextGroupsSql));
        AddContextParameters(command, buildId, variantId, selection);
        command.Parameters.AddWithValue("offset", NpgsqlDbType.Bigint, (long)paging.Offset);
        command.Parameters.AddWithValue("page_size", paging.PageSize);

        var totalCount = 0;
        var hasInvalidExactIdentity = false;
        ContextOccurrenceRow? overallRepresentative = null;
        var items = new List<FullContextGroup>();
        await using var reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hasInvalidExactIdentity = reader.GetBoolean(0);
            totalCount = reader.GetInt32(1);
            if (hasInvalidExactIdentity)
            {
                continue;
            }

            if (!reader.IsDBNull(2))
            {
                overallRepresentative ??= ReadOccurrenceRow(reader, 2);
            }

            if (reader.IsDBNull(12))
            {
                continue;
            }

            var previousIds = reader.GetFieldValue<int[]>(13);
            var followingIds = reader.GetFieldValue<int[]>(14);
            var occurrenceCount = reader.GetInt32(15);
            if (reader.IsDBNull(16))
            {
                throw new InvalidDataException("PhraseSearch context group has no representative occurrence.");
            }

            items.Add(new FullContextGroup(
                previousIds,
                followingIds,
                occurrenceCount,
                ReadOccurrenceRow(reader, 16)));
        }

        if (hasInvalidExactIdentity)
        {
            throw new InvalidDataException("PhraseSearch context source contains a word without the selected exact identity.");
        }

        return new ContextGroupPageLoad(totalCount, overallRepresentative, items);
    }

    private PhraseFullContextGroupDto CreateGroup(
        PhraseResolutionReference resolution,
        FullContextGroup group,
        ContextOccurrence representative)
    {
        if (!FullPathIds(representative, PhraseContextSide.Previous).SequenceEqual(group.PreviousIds)
            || !FullPathIds(representative, PhraseContextSide.Following).SequenceEqual(group.FollowingIds))
        {
            throw new InvalidDataException("PhraseSearch context group does not match its representative occurrence.");
        }

        var context = new PhraseFullContextReference(
            resolution.BuildId,
            resolution.Mode,
            resolution.ExactTokenIds,
            group.PreviousIds,
            group.FollowingIds);
        return new PhraseFullContextGroupDto(
            codec.EncodeFullContext(context),
            FullPathTokens(representative, PhraseContextSide.Previous),
            CreateResolvedQuery(resolution, representative).Tokens,
            FullPathTokens(representative, PhraseContextSide.Following),
            group.OccurrenceCount,
            representative.Row.SurahNameArabic,
            representative.Row.AyahNumber,
            representative.Row.VerseKey);
    }

    private sealed record ContextGroupPageLoad(
        int TotalCount,
        ContextOccurrenceRow? OverallRepresentative,
        IReadOnlyList<FullContextGroup> Items);

    private sealed record FullContextGroup(
        IReadOnlyList<int> PreviousIds,
        IReadOnlyList<int> FollowingIds,
        int OccurrenceCount,
        ContextOccurrenceRow Representative);
}
