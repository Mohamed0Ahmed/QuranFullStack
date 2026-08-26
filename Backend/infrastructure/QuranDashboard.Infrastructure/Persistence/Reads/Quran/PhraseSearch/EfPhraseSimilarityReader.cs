using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader(
    QuranDashboardDbContext db,
    PhraseSimilarityOccurrenceHydrator occurrenceHydrator,
    PhraseSearchReadCache cache) : IPhraseSimilarityReader
{
    private readonly QuranDashboardDbContext db = db;
    private readonly PhraseSimilarityOccurrenceHydrator occurrenceHydrator = occurrenceHydrator;
    private readonly PhraseSearchReadCache cache = cache;

    private async Task<SimilarityVariantRow?> LoadVariantAsync(
        Guid buildId,
        PhraseResolutionReference resolution,
        CancellationToken cancellationToken)
    {
        var exactTokenIds = resolution.ExactTokenIds.ToArray();
        return await db.QuranPhraseVariants
            .AsNoTracking()
            .Where(variant => variant.BuildId == buildId
                && variant.Mode == resolution.Mode
                && variant.WordCount == exactTokenIds.Length
                && variant.ExactTokenIds.SequenceEqual(exactTokenIds))
            .Select(variant => new SimilarityVariantRow(
                variant.Id,
                variant.Mode,
                variant.WordCount,
                variant.ExactTokenIds,
                variant.DisplayText,
                variant.OccurrenceCount,
                variant.AyahCount,
                variant.SurahCount))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<PhraseSimilarityMatchDto>> CreateMatchesAsync(
        Guid buildId,
        SimilarityVariantRow anchor,
        IReadOnlyList<SimilarityMatchRow> rows,
        CancellationToken cancellationToken)
    {
        var variantIds = rows
            .Select(row => row.Variant.Id)
            .Append(anchor.Id)
            .Distinct()
            .ToList();
        var occurrences = await occurrenceHydrator.LoadFirstAsync(
            buildId,
            variantIds,
            cancellationToken);
        if (!occurrences.TryGetValue(anchor.Id, out var anchorOccurrence))
        {
            throw new InvalidDataException("PhraseSearch similarity anchor has no attested occurrence.");
        }

        var items = new List<PhraseSimilarityMatchDto>(rows.Count);
        foreach (var row in rows)
        {
            var score = PhraseHammingScore.Calculate(anchor.ExactTokenIds, row.Variant.ExactTokenIds);
            if (score.MatchedCount != row.StoredMatchedCount)
            {
                throw new InvalidDataException("PhraseSearch similarity score does not match its exact token arrays.");
            }

            if (!occurrences.TryGetValue(row.Variant.Id, out var comparedOccurrence))
            {
                throw new InvalidDataException("PhraseSearch similarity result has no attested occurrence.");
            }

            items.Add(new PhraseSimilarityMatchDto(
                ToDto(row.Variant),
                score.MatchedCount,
                score.DifferenceCount,
                score.MatchPercent,
                score.MatchedPositions,
                score.DifferingPositions,
                occurrenceHydrator.ApplyScore(anchorOccurrence, score),
                occurrenceHydrator.ApplyScore(comparedOccurrence, score)));
        }

        return items;
    }

    private static PhraseSimilarityPhraseDto ToDto(SimilarityVariantRow variant) => new(
        variant.Id,
        PhraseTextModeContract.CanonicalKey(variant.Mode),
        variant.WordCount,
        variant.DisplayText,
        variant.OccurrenceCount,
        variant.AyahCount,
        variant.SurahCount);

    private static long CalculateOffset(int page, int pageSize) => ((long)page - 1) * pageSize;

    private sealed record SimilarityVariantRow(
        long Id,
        PhraseTextMode Mode,
        short WordCount,
        int[] ExactTokenIds,
        string DisplayText,
        long OccurrenceCount,
        int AyahCount,
        short SurahCount);

    private sealed record SimilarityMatchRow(
        SimilarityVariantRow Variant,
        short StoredMatchedCount);
}
