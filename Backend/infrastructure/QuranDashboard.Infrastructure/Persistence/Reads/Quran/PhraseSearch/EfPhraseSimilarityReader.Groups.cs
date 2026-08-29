using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Domain.Quran.PhraseSearch;
using QuranDashboard.Infrastructure.Caching.Quran.PhraseSearch;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Quran.PhraseSearch;

public sealed partial class EfPhraseSimilarityReader
{
    public async Task<PhraseSearchReadResult<PhraseSimilarityGroupsResponse>> GetGroupsAsync(
        PhraseTextMode mode,
        short wordCount,
        short threshold,
        PhraseSimilaritySort sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var snapshot = await PhraseSearchReadSnapshot.OpenAsync(db, cancellationToken);
        if (snapshot is null)
        {
            return new PhraseSearchReadResult<PhraseSimilarityGroupsResponse>.Unavailable();
        }

        var cacheKey = PhraseSearchCacheKeys.SimilarityGroups(
            snapshot.ActiveBuildId,
            mode,
            wordCount,
            threshold,
            sort,
            page,
            pageSize);
        if (cache.TryGet(cacheKey, out PhraseSimilarityGroupsResponse cached))
        {
            await snapshot.CompleteAsync(cancellationToken);
            return new PhraseSearchReadResult<PhraseSimilarityGroupsResponse>.Success(cached);
        }

        var stats = db.QuranPhraseSimilarityAnchorStats
            .AsNoTracking()
            .Where(stat => stat.BuildId == snapshot.ActiveBuildId
                && stat.Mode == mode
                && stat.WordCount == wordCount
                && stat.Threshold == threshold);
        var totalCount = await stats.CountAsync(cancellationToken);
        var offset = CalculateOffset(page, pageSize);
        var candidates =
            from stat in stats
            join variant in db.QuranPhraseVariants.AsNoTracking()
                on new { stat.BuildId, Id = stat.VariantId }
                equals new { variant.BuildId, variant.Id }
            select new { Stat = stat, Variant = variant };
        var ordered = sort switch
        {
            PhraseSimilaritySort.Strength => candidates
                .OrderByDescending(row => row.Stat.BestMatchedCount)
                .ThenByDescending(row => row.Stat.NeighborCount)
                .ThenBy(row => row.Stat.VariantId),
            PhraseSimilaritySort.Connections => candidates
                .OrderByDescending(row => row.Stat.NeighborCount)
                .ThenByDescending(row => row.Stat.BestMatchedCount)
                .ThenBy(row => row.Stat.VariantId),
            PhraseSimilaritySort.MushafOrder => candidates
                .OrderBy(row => row.Variant.FirstQuranWordId)
                .ThenBy(row => row.Stat.VariantId),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseSimilaritySort)} value: {sort}."),
        };
        var rows = offset > int.MaxValue
            ? []
            : await ordered
                .Select(row => new SimilarityGroupRow(
                    new SimilarityVariantRow(
                        row.Variant.Id,
                        row.Variant.Mode,
                        row.Variant.WordCount,
                        row.Variant.ExactTokenIds,
                        row.Variant.DisplayText,
                        row.Variant.OccurrenceCount,
                        row.Variant.AyahCount,
                        row.Variant.SurahCount),
                    row.Stat.NeighborCount,
                    row.Stat.BestMatchedCount))
                .Skip((int)offset)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        var occurrences = await occurrenceHydrator.LoadFirstAsync(
            snapshot.ActiveBuildId,
            rows.Select(row => row.Anchor.Id).ToList(),
            cancellationToken);
        var bestNeighbors = await LoadBestNeighborsAsync(
            snapshot.ActiveBuildId,
            rows,
            threshold,
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
            bestNeighbors.GetValueOrDefault(row.Anchor.Id),
            occurrenceHydrator.ToPreview(
                occurrences.GetValueOrDefault(row.Anchor.Id)
                    ?? throw new InvalidDataException(
                        "PhraseSearch similarity group has no attested occurrence."))))
            .ToList();
        var response = new PhraseSimilarityGroupsResponse(
            snapshot.ActiveBuildId,
            PhraseTextModeContract.CanonicalKey(mode),
            wordCount,
            threshold,
            PhraseSimilaritySortContract.CanonicalKey(sort),
            page,
            pageSize,
            totalCount,
            items);
        await snapshot.CompleteAsync(cancellationToken);
        cache.Set(cacheKey, response, PhraseSearchCacheKeys.PageWeight(pageSize));
        return new PhraseSearchReadResult<PhraseSimilarityGroupsResponse>.Success(response);
    }

    private async Task<IReadOnlyDictionary<long, PhraseSimilarityPhraseDto>> LoadBestNeighborsAsync(
        Guid buildId,
        IReadOnlyList<SimilarityGroupRow> groups,
        short threshold,
        CancellationToken cancellationToken)
    {
        if (groups.Count == 0)
        {
            return new Dictionary<long, PhraseSimilarityPhraseDto>();
        }

        var anchorIds = groups.Select(group => group.Anchor.Id).ToHashSet();
        var minimumMatchedWords = PhraseSimilarityContract.MinimumMatchedWords(
            groups[0].Anchor.WordCount,
            threshold);
        var edges = await db.QuranPhraseSimilarityEdges
            .AsNoTracking()
            .Where(edge => edge.BuildId == buildId
                && edge.MatchedCount >= minimumMatchedWords
                && (anchorIds.Contains(edge.LeftVariantId)
                    || anchorIds.Contains(edge.RightVariantId)))
            .Select(edge => new
            {
                edge.LeftVariantId,
                edge.RightVariantId,
                edge.MatchedCount,
            })
            .ToListAsync(cancellationToken);
        var candidates = new List<BestNeighborRow>(edges.Count * 2);
        foreach (var edge in edges)
        {
            if (anchorIds.Contains(edge.LeftVariantId))
            {
                candidates.Add(new BestNeighborRow(
                    edge.LeftVariantId,
                    edge.RightVariantId,
                    edge.MatchedCount));
            }

            if (anchorIds.Contains(edge.RightVariantId))
            {
                candidates.Add(new BestNeighborRow(
                    edge.RightVariantId,
                    edge.LeftVariantId,
                    edge.MatchedCount));
            }
        }

        var bestByAnchor = candidates
            .GroupBy(candidate => candidate.AnchorVariantId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(candidate => candidate.MatchedCount)
                    .ThenBy(candidate => candidate.NeighborVariantId)
                    .First().NeighborVariantId);
        var neighborIds = bestByAnchor.Values.Distinct().ToList();
        var neighbors = await db.QuranPhraseVariants
            .AsNoTracking()
            .Where(variant => variant.BuildId == buildId && neighborIds.Contains(variant.Id))
            .Select(variant => new SimilarityVariantRow(
                variant.Id,
                variant.Mode,
                variant.WordCount,
                variant.ExactTokenIds,
                variant.DisplayText,
                variant.OccurrenceCount,
                variant.AyahCount,
                variant.SurahCount))
            .ToListAsync(cancellationToken);
        var neighborDtos = neighbors.ToDictionary(neighbor => neighbor.Id, ToDto);
        return bestByAnchor.ToDictionary(
            pair => pair.Key,
            pair => neighborDtos[pair.Value]);
    }

    private sealed record SimilarityGroupRow(
        SimilarityVariantRow Anchor,
        int NeighborCount,
        short? BestMatchedCount);

    private sealed record BestNeighborRow(
        long AnchorVariantId,
        long NeighborVariantId,
        short MatchedCount);
}
