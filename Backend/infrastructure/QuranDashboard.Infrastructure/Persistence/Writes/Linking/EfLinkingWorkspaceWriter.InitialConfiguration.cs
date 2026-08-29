using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Caching.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingWorkspaceWriter
{
    private Task<LinkingResolvedSourceCompact> ResolveCompactSourceAsync(
        LinkingSourceDescriptor descriptor,
        long linkingDataRevision,
        CancellationToken cancellationToken)
    {
        var sourceIdentity = LinkingSourceIdentity.For(descriptor);

        return sourceCache.GetOrLoadAsync(
            LinkingSourceCacheKeys.For(descriptor.Kind, sourceIdentity, linkingDataRevision),
            sourceIdentity,
            token => efResolution.ResolveCompactAsync(descriptor, token),
            cancellationToken);
    }

    private async Task<InitialConfiguration> PrepareInitialConfigurationAsync(
        LinkingSourceDescriptor descriptor,
        LinkingWorkspaceConfigurationInput initialConfiguration,
        LinkingResolvedSourceCompact compact,
        CancellationToken cancellationToken)
    {
        var configuration = NormalizeInitialConfiguration(descriptor, initialConfiguration);
        var isManual = descriptor.Kind == LinkingSourceKind.ManualMushafAyahs;

        EnsureConfigurationCoherence(isManual, configuration);
        EnsureConfigurationMembership(isManual, configuration, compact);
        await EnsureSelectedWordsAsync(configuration.SelectedWords, cancellationToken);

        return new InitialConfiguration(configuration, NormalizeDescriptions(configuration.Descriptions));
    }

    private async Task ApplyInitialConfigurationAsync(
        long sourceId,
        InitialConfiguration initialConfiguration,
        int userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await ReplaceOverridesAsync(
            sourceId,
            initialConfiguration.Configuration.AyahOverrides,
            cancellationToken);
        await ReplaceSelectedWordsAsync(
            sourceId,
            initialConfiguration.Configuration.SelectedWords,
            cancellationToken);
        await ReplaceDescriptionsAsync(
            sourceId,
            initialConfiguration.Descriptions,
            userId,
            now,
            cancellationToken);
    }

    private static LinkingWorkspaceConfigurationInput NormalizeInitialConfiguration(
        LinkingSourceDescriptor descriptor,
        LinkingWorkspaceConfigurationInput configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration with
        {
            Label = descriptor.Label,
            AyahOverrides = [.. configuration.AyahOverrides.Distinct().Order()],
            SelectedWords = NormalizeSelectedWords(configuration.SelectedWords),
        };
    }

    private static IReadOnlyList<LinkingWorkspaceSelectedWordInput> NormalizeSelectedWords(
        IReadOnlyList<LinkingWorkspaceSelectedWordInput> selectedWords)
    {
        var selectedByWordId = new Dictionary<int, LinkingWorkspaceSelectedWordInput>();

        foreach (var selectedWord in selectedWords)
        {
            if (selectedByWordId.TryGetValue(selectedWord.QuranWordId, out var existing)
                && existing.AyahId != selectedWord.AyahId)
            {
                throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                    LinkingWorkspaceViolationCode.SelectedWordAyahConflict,
                    "initialConfiguration.selectedWords.quranWordId",
                    selectedWord.QuranWordId.ToString(CultureInfo.InvariantCulture)));
            }

            selectedByWordId[selectedWord.QuranWordId] = selectedWord;
        }

        return [.. selectedByWordId.Values
            .OrderBy(selectedWord => selectedWord.AyahId)
            .ThenBy(selectedWord => selectedWord.QuranWordId)];
    }

    private static void EnsureConfigurationMembership(
        bool isManual,
        LinkingWorkspaceConfigurationInput configuration,
        LinkingResolvedSourceCompact compact)
    {
        if (!Enum.IsDefined(configuration.InclusionMode)
            || configuration.ManualLinkShape is { } manualLinkShape
                && !Enum.IsDefined(manualLinkShape))
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.ConfigurationIncoherent,
                "initialConfiguration",
                null));
        }

        var memberIds = compact.AyahIds.ToHashSet();
        var invalidAyah = configuration.AyahOverrides
            .Concat(configuration.SelectedWords.Select(word => word.AyahId))
            .Concat(configuration.Descriptions.Select(description => description.AyahId))
            .FirstOrDefault(ayahId => !memberIds.Contains(ayahId));
        if (invalidAyah != 0)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.AyahReferenceUnknown,
                "initialConfiguration.ayahId",
                invalidAyah.ToString(CultureInfo.InvariantCulture)));
        }

        if (!isManual && configuration.SelectedWords.Count > 0)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.WordsNotAllowedOnAutomaticSource,
                "initialConfiguration.selectedWords",
                null));
        }

        var wordsByAyah = compact.Ayahs.ToDictionary(
            ayah => ayah.AyahId,
            ayah => ayah.QuranWordIds.ToHashSet());
        var invalidWord = configuration.SelectedWords.FirstOrDefault(word =>
            !wordsByAyah.GetValueOrDefault(word.AyahId, []).Contains(word.QuranWordId));
        if (invalidWord is not null)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.SelectedWordInvalid,
                "initialConfiguration.selectedWords.quranWordId",
                invalidWord.QuranWordId.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private async Task AddManualAyahsAsync(
        long sourceId,
        LinkingSourceDescriptor.ManualMushafAyahs manual,
        CancellationToken cancellationToken)
    {
        var verseKeys = manual.VerseKeys.Select(verseKey => verseKey.Value).ToList();

        var ayahs = await db.QuranAyahs
            .AsNoTracking()
            .Where(ayah => verseKeys.Contains(ayah.VerseKey))
            .Select(ayah => new { ayah.Id, ayah.VerseKey, ayah.PageFrom })
            .ToListAsync(cancellationToken);

        var ayahsByVerseKey = ayahs.ToDictionary(ayah => ayah.VerseKey, StringComparer.Ordinal);

        var orderValue = 1;
        foreach (var verseKey in verseKeys)
        {
            if (!ayahsByVerseKey.TryGetValue(verseKey, out var ayah))
            {
                throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                    LinkingWorkspaceViolationCode.AyahReferenceUnknown,
                    "manualAyahs.verseKey",
                    verseKey));
            }

            db.LinkingWorkspaceSourceManualAyahs.Add(new LinkingWorkspaceSourceManualAyah
            {
                WorkspaceSourceId = sourceId,
                AyahId = ayah.Id,
                OrderValue = orderValue++,
                PageHint = ayah.PageFrom,
            });
        }
    }

    private sealed record InitialConfiguration(
        LinkingWorkspaceConfigurationInput Configuration,
        IReadOnlyList<LinkingWorkspaceAyahDescriptions> Descriptions);
}
