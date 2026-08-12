using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Reads.Quran.Words.Stems;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

public sealed partial class EfLinkingSourceResolutionReader
{
    private async Task<IReadOnlyList<LinkingResolvedAyahDto>> ResolveRootAsync(
        LinkingSourceDescriptor.Root source,
        CancellationToken cancellationToken)
    {
        var rootExists = await _dbContext.QuranRoots
            .AsNoTracking()
            .AnyAsync(root => root.Id == source.RootId, cancellationToken);
        if (!rootExists)
        {
            throw NotFound("rootId", source.RootId);
        }

        var matches = await (
            from morphology in _dbContext.WordMorphologies.AsNoTracking()
            join word in _dbContext.QuranWords.AsNoTracking() on morphology.QuranWordId equals word.Id
            where morphology.RootId == source.RootId && !word.IsAyahMarker
            select new LinkingMatchedWordRow(word.AyahId, word.Id, word.WordNumber))
            .Distinct()
            .ToListAsync(cancellationToken);

        return await HydrateMatchesAsync(matches, includeAyahMarkers: false, cancellationToken);
    }

    private async Task<IReadOnlyList<LinkingResolvedAyahDto>> ResolveLemmaAsync(
        LinkingSourceDescriptor.Lemma source,
        CancellationToken cancellationToken)
    {
        var typeCode = StemsAyahTypeCode.Normalize(source.TypeCode);

        var lemmaExists = await _dbContext.QuranLemmas
            .AsNoTracking()
            .AnyAsync(lemma => lemma.Id == source.LemmaId, cancellationToken);
        if (!lemmaExists)
        {
            throw NotFound("lemmaId", source.LemmaId);
        }

        var matches = await (
            from segment in _dbContext.WordMorphologySegments.AsNoTracking()
            join word in _dbContext.QuranWords.AsNoTracking() on segment.QuranWordId equals word.Id
            where segment.LemmaId == source.LemmaId
                && (typeCode == null || segment.Pos == typeCode)
                && !word.IsAyahMarker
            select new LinkingMatchedWordRow(word.AyahId, word.Id, word.WordNumber))
            .Distinct()
            .ToListAsync(cancellationToken);

        return await HydrateMatchesAsync(matches, includeAyahMarkers: false, cancellationToken);
    }

    private async Task<IReadOnlyList<LinkingResolvedAyahDto>> ResolveStemAsync(
        LinkingSourceDescriptor.Stem source,
        CancellationToken cancellationToken)
    {
        var typeCode = StemsAyahTypeCode.Normalize(source.TypeCode);

        var stemExists = await _dbContext.QuranStems
            .AsNoTracking()
            .AnyAsync(stem => stem.Id == source.StemId, cancellationToken);
        if (!stemExists)
        {
            throw NotFound("stemId", source.StemId);
        }

        var matches = await (
            from segment in _dbContext.WordMorphologySegments.AsNoTracking()
            join word in _dbContext.QuranWords.AsNoTracking() on segment.QuranWordId equals word.Id
            where segment.Kind == StemSegmentKind
                && segment.StemId == source.StemId
                && (typeCode == null || segment.Pos == typeCode)
                && !word.IsAyahMarker
            select new LinkingMatchedWordRow(word.AyahId, word.Id, word.WordNumber))
            .Distinct()
            .ToListAsync(cancellationToken);

        return await HydrateMatchesAsync(matches, includeAyahMarkers: false, cancellationToken);
    }
}
