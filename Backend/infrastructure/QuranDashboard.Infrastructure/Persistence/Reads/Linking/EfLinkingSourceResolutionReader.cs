using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Linking;

public sealed partial class EfLinkingSourceResolutionReader(QuranDashboardDbContext dbContext)
    : ILinkingSourceResolutionReader
{
    private const string ResolvedAyahsField = "ayahs";
    private const string StemSegmentKind = "STEM";

    private static readonly IReadOnlyDictionary<int, IReadOnlyList<int>> NoMatchedWords =
        new Dictionary<int, IReadOnlyList<int>>();

    private readonly QuranDashboardDbContext _dbContext = dbContext;

    public async Task<LinkingResolvedSourceDto> ResolveAsync(
        LinkingSourceDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var ayahs = descriptor switch
        {
            LinkingSourceDescriptor.Root source =>
                await ResolveRootAsync(source, cancellationToken),
            LinkingSourceDescriptor.Lemma source =>
                await ResolveLemmaAsync(source, cancellationToken),
            LinkingSourceDescriptor.Stem source =>
                await ResolveStemAsync(source, cancellationToken),
            LinkingSourceDescriptor.UniqueWord source =>
                await ResolveUniqueWordAsync(source, cancellationToken),
            LinkingSourceDescriptor.WordType source =>
                await ResolveWordTypeAsync(source, cancellationToken),
            LinkingSourceDescriptor.ManualMushafAyahs source =>
                await ResolveManualMushafAsync(source, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(descriptor),
                descriptor.Kind,
                "Unknown linking source kind."),
        };

        return new LinkingResolvedSourceDto(
            LinkingSourceIdentity.For(descriptor),
            DateTimeOffset.UtcNow,
            ayahs.Count,
            ayahs);
    }

    private static void GuardResolvedAyahCount(int ayahCount)
    {
        if (ayahCount > LinkingLimits.MaxResolvedAyahs)
        {
            throw new LinkingInvalidDescriptorException(new LinkingDescriptorViolation(
                LinkingDescriptorViolationCode.ResolvedAyahLimitExceeded,
                ResolvedAyahsField,
                ayahCount.ToString(CultureInfo.InvariantCulture)));
        }
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

    private async Task<IReadOnlyList<LinkingResolvedAyahDto>> HydrateMatchesAsync(
        IReadOnlyList<LinkingMatchedWordRow> matches,
        bool includeAyahMarkers,
        CancellationToken cancellationToken)
    {
        var matchedWordIdsByAyah = GroupMatchedWordIds(matches);
        GuardResolvedAyahCount(matchedWordIdsByAyah.Count);

        if (matchedWordIdsByAyah.Count == 0)
        {
            return [];
        }

        var ayahs = await LinkingAyahHydration.LoadByIdsAsync(
            _dbContext,
            [.. matchedWordIdsByAyah.Keys],
            cancellationToken);

        return await LinkingAyahHydration.ProjectAsync(
            _dbContext,
            ayahs,
            matchedWordIdsByAyah,
            includeAyahMarkers,
            cancellationToken);
    }

    private static LinkingSourceNotFoundException NotFound(string field, int id) =>
        new(string.Create(CultureInfo.InvariantCulture, $"{field}={id}"));

    internal sealed record LinkingMatchedWordRow(int AyahId, int QuranWordId, int WordNumber);
}
