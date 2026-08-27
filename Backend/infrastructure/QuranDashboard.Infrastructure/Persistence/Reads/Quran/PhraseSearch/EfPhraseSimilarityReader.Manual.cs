using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader
{
    public async Task<PhraseSearchReadResult<PhraseSimilaritySearchResponse>> SearchAsync(
        PhraseResolutionReference resolution,
        short minimumMatchedWords,
        PhraseSimilaritySort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseSimilaritySearchResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != resolution.BuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilaritySearchResponse>.BuildChanged();
        }

        var cacheKey = PhraseSearchCacheKeys.SimilaritySearch(
            resolution,
            minimumMatchedWords,
            sort,
            page,
            pageSize);
        if (cache.TryGet(cacheKey, out PhraseSimilaritySearchResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilaritySearchResponse>.Success(cached);
        }

        var anchor = await LoadVariantAsync(
            snapshot.ActiveBuildId,
            resolution,
            cancellationToken);
        if (anchor is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilaritySearchResponse>.InvalidReference();
        }

        var totals = await ReadSimilarityAyahTotalsAsync(
            snapshot.ActiveBuildId,
            anchor,
            minimumMatchedWords,
            cancellationToken);
        var items = await ReadSimilarityAyahPageAsync(
            snapshot.ActiveBuildId,
            anchor,
            minimumMatchedWords,
            sort,
            page,
            pageSize,
            cancellationToken);
        var response = new PhraseSimilaritySearchResponse(
            snapshot.ActiveBuildId,
            PhraseTextModeContract.CanonicalKey(anchor.Mode),
            anchor.WordCount,
            minimumMatchedWords,
            PhraseSimilaritySortContract.CanonicalKey(sort),
            page,
            pageSize,
            totals.AyahCount,
            totals.OccurrenceCount,
            ToDto(anchor),
            items);
        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(cacheKey, response, PhraseSearchCacheKeys.PageWeight(pageSize));
        return new PhraseSearchReadResult<PhraseSimilaritySearchResponse>.Success(response);
    }

    private static void AddManualParameters(
        NpgsqlCommand command,
        Guid buildId,
        SimilarityVariantRow anchor,
        short minimumMatchedWords)
    {
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("mode", NpgsqlDbType.Smallint, (short)anchor.Mode);
        command.Parameters.AddWithValue("word_count", NpgsqlDbType.Smallint, anchor.WordCount);
        command.Parameters.AddWithValue("anchor_variant_id", anchor.Id);
        command.Parameters.AddWithValue(
            "exact_token_ids",
            NpgsqlDbType.Array | NpgsqlDbType.Integer,
            anchor.ExactTokenIds);
        command.Parameters.AddWithValue(
            "minimum_matched_words",
            NpgsqlDbType.Smallint,
            minimumMatchedWords);
    }
}
