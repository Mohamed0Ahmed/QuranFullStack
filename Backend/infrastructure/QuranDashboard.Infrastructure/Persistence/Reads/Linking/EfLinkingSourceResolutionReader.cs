using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Caching.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

public sealed partial class EfLinkingSourceResolutionReader(QuranDashboardDbContext dbContext)
{
    private const string StemSegmentKind = "STEM";

    private readonly QuranDashboardDbContext _dbContext = dbContext;

    public async Task<LinkingResolvedSourceCompact> ResolveCompactAsync(
        LinkingSourceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var references = descriptor switch
        {
            LinkingSourceDescriptor.Root source =>
                FromMatches(await ResolveRootAsync(source, cancellationToken), false),
            LinkingSourceDescriptor.Lemma source =>
                FromMatches(await ResolveLemmaAsync(source, cancellationToken), false),
            LinkingSourceDescriptor.Stem source =>
                FromMatches(await ResolveStemAsync(source, cancellationToken), false),
            LinkingSourceDescriptor.UniqueWord source =>
                FromMatches(await ResolveUniqueWordAsync(source, cancellationToken), true),
            LinkingSourceDescriptor.WordType source =>
                FromMatches(await ResolveWordTypeAsync(source, cancellationToken), false),
            LinkingSourceDescriptor.ManualMushafAyahs source =>
                await ResolveManualMushafAsync(source, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.Kind,
                "Unknown linking source kind."),
        };

        var orderedAyahIds = await _dbContext.QuranAyahs
            .AsNoTracking()
            .Where(ayah => references.AyahIds.Contains(ayah.Id))
            .OrderBy(ayah => ayah.SurahNumber)
            .ThenBy(ayah => ayah.AyahNumber)
            .Select(ayah => ayah.Id)
            .ToListAsync(cancellationToken);
        var wordRows = await _dbContext.QuranWords
            .AsNoTracking()
            .Where(word =>
                references.AyahIds.Contains(word.AyahId)
                && (references.IncludeAyahMarkers || !word.IsAyahMarker))
            .OrderBy(word => word.AyahId)
            .ThenBy(word => word.WordNumber)
            .ThenBy(word => word.Id)
            .Select(word => new { word.AyahId, QuranWordId = word.Id })
            .ToListAsync(cancellationToken);
        var wordIdsByAyah = wordRows
            .GroupBy(word => word.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)[.. group.Select(word => word.QuranWordId)]);
        var matchesByAyah = GroupMatchedWordIds(references.Matches);
        var compactAyahs = orderedAyahIds
            .Select(ayahId => new LinkingResolvedSourceCompact.CompactAyah(
                ayahId,
                wordIdsByAyah.GetValueOrDefault(ayahId, []),
                matchesByAyah.GetValueOrDefault(ayahId, [])))
            .ToList();

        return LinkingResolvedSourceCompact.Create(
            LinkingSourceIdentity.For(descriptor),
            compactAyahs,
            references.IncludeAyahMarkers);
    }

    public async Task<IReadOnlyList<LinkingResolvedAyahDto>> HydrateAsync(
        LinkingResolvedSourceCompact compact,
        IReadOnlyList<int> ayahIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(compact);
        ArgumentNullException.ThrowIfNull(ayahIds);

        if (ayahIds.Count == 0)
        {
            return [];
        }

        var selected = ayahIds.ToHashSet();
        var compactAyahs = compact.Ayahs.Where(ayah => selected.Contains(ayah.AyahId)).ToList();
        var orderedAyahs = await LinkingAyahHydration.LoadByIdsAsync(
            _dbContext,
            ayahIds,
            cancellationToken);
        var matches = compactAyahs.ToDictionary(
            ayah => ayah.AyahId,
            ayah => ayah.MatchedQuranWordIds);

        return await LinkingAyahHydration.ProjectAsync(
            _dbContext,
            orderedAyahs,
            matches,
            compact.IncludesAyahMarkers,
            cancellationToken);
    }

    public async Task<LinkingCompactSourcePage> ResolveCompactPageAsync(
        LinkingResolvedSourceCompact compact,
        LinkingSourcePageView view,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.QuranAyahs
            .AsNoTracking()
            .Where(ayah => compact.AyahIds.Contains(ayah.Id));
        if (view.Segment != LinkingSourcePageSegment.All)
        {
            var includedByOverride = view.InclusionMode == LinkingInclusionMode.Only;
            var requestIncluded = view.Segment == LinkingSourcePageSegment.Included;
            var requireOverride = includedByOverride == requestIncluded;
            query = requireOverride
                ? query.Where(ayah => view.AyahOverrideIds.Contains(ayah.Id))
                : query.Where(ayah => !view.AyahOverrideIds.Contains(ayah.Id));
        }

        var total = await query.CountAsync(cancellationToken);
        var ids = await query
            .OrderBy(ayah => ayah.SurahNumber)
            .ThenBy(ayah => ayah.AyahNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ayah => ayah.Id)
            .ToListAsync(cancellationToken);
        return new LinkingCompactSourcePage(
            total,
            [.. ids.Select(id => compact.AyahsById[id])]);
    }

    private static LinkingSourceReferenceSet FromMatches(
        IReadOnlyList<LinkingMatchedWordRow> matches,
        bool includeAyahMarkers)
    {
        var ayahIds = matches.Select(match => match.AyahId).Distinct().ToList();
        return new LinkingSourceReferenceSet(ayahIds, matches, includeAyahMarkers);
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<int>> GroupMatchedWordIds(
        IReadOnlyList<LinkingMatchedWordRow> matches) =>
        matches
            .GroupBy(match => match.AyahId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)
                [
                    .. group
                        .OrderBy(match => match.WordNumber)
                        .ThenBy(match => match.QuranWordId)
                        .Select(match => match.QuranWordId)
                ]);

    private static LinkingSourceNotFoundException NotFound(string field, int id) =>
        new(string.Create(CultureInfo.InvariantCulture, $"{field}={id}"));

    internal sealed record LinkingMatchedWordRow(int AyahId, int QuranWordId, int WordNumber);

    private sealed record LinkingSourceReferenceSet(
        IReadOnlyList<int> AyahIds,
        IReadOnlyList<LinkingMatchedWordRow> Matches,
        bool IncludeAyahMarkers);

    public sealed record LinkingCompactSourcePage(
        int TotalAyahs,
        IReadOnlyList<LinkingResolvedSourceCompact.CompactAyah> Ayahs);
}
