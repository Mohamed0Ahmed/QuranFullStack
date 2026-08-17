using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

public sealed partial class EfLinkingSourceResolutionReader
{
    private Task<IReadOnlyList<LinkingSourceTypeDto>> ResolveAvailableTypesAsync(
        LinkingSourceDescriptor descriptor,
        CancellationToken cancellationToken) => descriptor switch
        {
            LinkingSourceDescriptor.Root source => ResolveRootTypesAsync(source.RootId, cancellationToken),
            LinkingSourceDescriptor.Lemma source => ResolveLemmaTypesAsync(source.LemmaId, cancellationToken),
            LinkingSourceDescriptor.Stem source => ResolveStemTypesAsync(source.StemId, cancellationToken),
            LinkingSourceDescriptor.UniqueWord source => ResolveUniqueWordTypesAsync(source, cancellationToken),
            _ => Task.FromResult<IReadOnlyList<LinkingSourceTypeDto>>([]),
        };

    private async Task<IReadOnlyList<LinkingSourceTypeDto>> ResolveRootTypesAsync(
        int rootId,
        CancellationToken cancellationToken) =>
        OrderTypes(await (
            from morphology in _dbContext.WordMorphologies.AsNoTracking()
            join tag in _dbContext.PosTags.AsNoTracking() on morphology.HeadPos equals tag.Code
            where morphology.RootId == rootId
            group morphology by new { tag.Code, tag.ArabicLabel }
            into rows
            select new LinkingSourceTypeDto(rows.Key.Code, rows.Key.ArabicLabel, rows.Count()))
            .ToListAsync(cancellationToken));

    private async Task<IReadOnlyList<LinkingSourceTypeDto>> ResolveLemmaTypesAsync(
        int lemmaId,
        CancellationToken cancellationToken) =>
        OrderTypes(await (
            from segment in _dbContext.WordMorphologySegments.AsNoTracking()
            join tag in _dbContext.PosTags.AsNoTracking() on segment.Pos equals tag.Code
            where segment.LemmaId == lemmaId
            group segment by new { tag.Code, tag.ArabicLabel }
            into rows
            select new LinkingSourceTypeDto(rows.Key.Code, rows.Key.ArabicLabel, rows.Count()))
            .ToListAsync(cancellationToken));

    private async Task<IReadOnlyList<LinkingSourceTypeDto>> ResolveStemTypesAsync(
        int stemId,
        CancellationToken cancellationToken) =>
        OrderTypes(await (
            from segment in _dbContext.WordMorphologySegments.AsNoTracking()
            join tag in _dbContext.PosTags.AsNoTracking() on segment.Pos equals tag.Code
            where segment.Kind == StemSegmentKind && segment.StemId == stemId
            group segment by new { tag.Code, tag.ArabicLabel }
            into rows
            select new LinkingSourceTypeDto(rows.Key.Code, rows.Key.ArabicLabel, rows.Count()))
            .ToListAsync(cancellationToken));

    private async Task<IReadOnlyList<LinkingSourceTypeDto>> ResolveUniqueWordTypesAsync(
        LinkingSourceDescriptor.UniqueWord source,
        CancellationToken cancellationToken)
    {
        var matches = source.Mode == LinkingUniqueWordMode.Tashkeel
            ? _dbContext.QuranWords.AsNoTracking()
                .Where(word => !word.IsAyahMarker && word.UniqueTashkeelWordId == source.WordId)
            : _dbContext.QuranWords.AsNoTracking()
                .Where(word => !word.IsAyahMarker && word.UniqueSimpleWordId == source.WordId);

        return OrderTypes(await (
            from word in matches
            join morphology in _dbContext.WordMorphologies.AsNoTracking()
                on word.Id equals morphology.QuranWordId
            join tag in _dbContext.PosTags.AsNoTracking() on morphology.HeadPos equals tag.Code
            group word by new { tag.Code, tag.ArabicLabel }
            into rows
            select new LinkingSourceTypeDto(rows.Key.Code, rows.Key.ArabicLabel, rows.Count()))
            .ToListAsync(cancellationToken));
    }

    private static IReadOnlyList<LinkingSourceTypeDto> OrderTypes(
        IEnumerable<LinkingSourceTypeDto> types) =>
        [
            .. types
                .OrderByDescending(type => type.OccurrencesCount)
                .ThenBy(type => type.Code, StringComparer.Ordinal)
        ];
}
