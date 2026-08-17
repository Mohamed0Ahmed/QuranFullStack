using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

public sealed partial class EfLinkingSourceResolutionReader
{
    private async Task<IReadOnlyList<LinkingMatchedWordRow>> ResolveRootAsync(
        LinkingSourceDescriptor.Root source,
        CancellationToken cancellationToken)
    {
        var typeCodes = source.TypeCodes.ToArray();
        var rootExists = await _dbContext.QuranRoots
            .AsNoTracking()
            .AnyAsync(root => root.Id == source.RootId, cancellationToken);
        if (!rootExists)
        {
            throw NotFound("rootId", source.RootId);
        }

        return await (
            from morphology in _dbContext.WordMorphologies.AsNoTracking()
            join word in _dbContext.QuranWords.AsNoTracking() on morphology.QuranWordId equals word.Id
            where morphology.RootId == source.RootId
                && (typeCodes.Length == 0 || typeCodes.Contains(morphology.HeadPos))
                && !word.IsAyahMarker
            select new LinkingMatchedWordRow(word.AyahId, word.Id, word.WordNumber))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<LinkingMatchedWordRow>> ResolveLemmaAsync(
        LinkingSourceDescriptor.Lemma source,
        CancellationToken cancellationToken)
    {
        var typeCodes = source.TypeCodes.ToArray();

        var lemmaExists = await _dbContext.QuranLemmas
            .AsNoTracking()
            .AnyAsync(lemma => lemma.Id == source.LemmaId, cancellationToken);
        if (!lemmaExists)
        {
            throw NotFound("lemmaId", source.LemmaId);
        }

        return await (
            from segment in _dbContext.WordMorphologySegments.AsNoTracking()
            join word in _dbContext.QuranWords.AsNoTracking() on segment.QuranWordId equals word.Id
            where segment.LemmaId == source.LemmaId
                && (typeCodes.Length == 0 || typeCodes.Contains(segment.Pos))
                && !word.IsAyahMarker
            select new LinkingMatchedWordRow(word.AyahId, word.Id, word.WordNumber))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<LinkingMatchedWordRow>> ResolveStemAsync(
        LinkingSourceDescriptor.Stem source,
        CancellationToken cancellationToken)
    {
        var typeCodes = source.TypeCodes.ToArray();

        var stemExists = await _dbContext.QuranStems
            .AsNoTracking()
            .AnyAsync(stem => stem.Id == source.StemId, cancellationToken);
        if (!stemExists)
        {
            throw NotFound("stemId", source.StemId);
        }

        return await (
            from segment in _dbContext.WordMorphologySegments.AsNoTracking()
            join word in _dbContext.QuranWords.AsNoTracking() on segment.QuranWordId equals word.Id
            where segment.Kind == StemSegmentKind
                && segment.StemId == source.StemId
                && (typeCodes.Length == 0 || typeCodes.Contains(segment.Pos))
                && !word.IsAyahMarker
            select new LinkingMatchedWordRow(word.AyahId, word.Id, word.WordNumber))
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
