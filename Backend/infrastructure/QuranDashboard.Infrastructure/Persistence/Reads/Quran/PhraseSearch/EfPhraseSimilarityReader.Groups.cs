using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader
{
    public async Task<PhraseSearchReadResult<PhraseSimilarityGroupsResponse>> GetGroupsAsync(
        PhraseTextMode mode,
        short wordCount,
        short threshold,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseSimilarityGroupsResponse>.Unavailable();
        }

        var stats = db.QuranPhraseSimilarityAnchorStats
            .AsNoTracking()
            .Where(stat => stat.BuildId == snapshot.ActiveBuildId
                && stat.Mode == mode
                && stat.WordCount == wordCount
                && stat.Threshold == threshold);
        var totalCount = await stats.CountAsync(cancellationToken);
        var offset = CalculateOffset(page, pageSize);
        var rows = offset > int.MaxValue
            ? []
            : await (
                from stat in stats
                join variant in db.QuranPhraseVariants.AsNoTracking()
                    on new { stat.BuildId, Id = stat.VariantId }
                    equals new { variant.BuildId, variant.Id }
                orderby stat.NeighborCount descending, stat.VariantId
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
                .Skip((int)offset)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        var occurrences = await occurrenceHydrator.LoadFirstAsync(
            snapshot.ActiveBuildId,
            rows.Select(row => row.Anchor.Id).ToList(),
            cancellationToken);
        var items = rows.Select(row => new PhraseSimilarityGroupDto(
            ToDto(row.Anchor),
            row.NeighborCount,
            row.BestMatchedCount,
            row.BestMatchedCount is null
                ? null
                : decimal.Round(
                    row.BestMatchedCount.Value * 100m / row.Anchor.WordCount,
                    1,
                    MidpointRounding.AwayFromZero),
            occurrenceHydrator.WithoutScore(
                occurrences.GetValueOrDefault(row.Anchor.Id)
                    ?? throw new InvalidDataException(
                        "PhraseSearch similarity group has no attested occurrence."))))
            .ToList();
        var response = new PhraseSimilarityGroupsResponse(
            snapshot.ActiveBuildId,
            PhraseTextModeContract.CanonicalKey(mode),
            wordCount,
            threshold,
            page,
            pageSize,
            totalCount,
            items);
        await snapshot.CompleteAsync(cancellationToken);
        return new PhraseSearchReadResult<PhraseSimilarityGroupsResponse>.Success(response);
    }

    private sealed record SimilarityGroupRow(
        SimilarityVariantRow Anchor,
        int NeighborCount,
        short? BestMatchedCount);
}
