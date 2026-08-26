using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader
{
    public async Task<PhraseSearchReadResult<PhraseSimilarityMatchesResponse>> GetMatchesAsync(
        Guid expectedBuildId,
        long anchorVariantId,
        short threshold,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseSimilarityMatchesResponse>.Unavailable();
        }

        if (snapshot.ActiveBuildId != expectedBuildId)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilarityMatchesResponse>.BuildChanged();
        }

        var group = await (
            from stat in db.QuranPhraseSimilarityAnchorStats.AsNoTracking()
            join variant in db.QuranPhraseVariants.AsNoTracking()
                on new { stat.BuildId, Id = stat.VariantId }
                equals new { variant.BuildId, variant.Id }
            where stat.BuildId == snapshot.ActiveBuildId
                && stat.VariantId == anchorVariantId
                && stat.Threshold == threshold
            select new SimilarityGroupRow(
                new SimilarityVariantRow(
                    variant.Id,
                    variant.Mode,
                    variant.WordCount,
                    variant.ExactTokenIds,
                    variant.DisplayText,
                    variant.OccurrenceCount,
                    variant.AyahCount,
                    variant.SurahCount),
                stat.NeighborCount,
                stat.BestMatchedCount))
            .SingleOrDefaultAsync(cancellationToken);
        if (group is null)
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilarityMatchesResponse>.NotFound();
        }

        var minimumMatchedWords = PhraseSimilarityContract.MinimumMatchedWords(
            group.Anchor.WordCount,
            threshold);
        var rows = await ReadDirectNeighborsPageAsync(
            snapshot.ActiveBuildId,
            group.Anchor.Id,
            minimumMatchedWords,
            CalculateOffset(page, pageSize),
            pageSize,
            cancellationToken);
        var response = new PhraseSimilarityMatchesResponse(
            snapshot.ActiveBuildId,
            threshold,
            page,
            pageSize,
            group.NeighborCount,
            ToDto(group.Anchor),
            await CreateMatchesAsync(
                snapshot.ActiveBuildId,
                group.Anchor,
                rows,
                cancellationToken));
        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseSimilarityMatchesResponse>.Success(response);
    }
}
