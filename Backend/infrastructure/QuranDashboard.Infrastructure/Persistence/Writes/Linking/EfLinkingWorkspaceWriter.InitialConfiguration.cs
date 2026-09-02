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

    private async Task<LinkingSourceConfiguration> PrepareInitialConfigurationAsync(
        LinkingSourceKind sourceKind,
        LinkingSourceConfiguration initialConfiguration,
        LinkingResolvedSourceCompact compact,
        CancellationToken cancellationToken)
    {
        if (initialConfiguration.SourceKind != sourceKind)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.ConfigurationIncoherent,
                "initialConfiguration",
                null));
        }

        EnsureConfigurationMembership(initialConfiguration, compact);
        await EnsureSelectedWordsAsync(initialConfiguration.SelectedWords, cancellationToken);

        return initialConfiguration;
    }

    private async Task ApplyInitialConfigurationAsync(
        long sourceId,
        LinkingSourceConfiguration initialConfiguration,
        int userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await ReplaceOverridesAsync(
            sourceId,
            initialConfiguration.AyahOverrides,
            cancellationToken);
        await ReplaceSelectedWordsAsync(
            sourceId,
            initialConfiguration.SelectedWords,
            cancellationToken);
        await ReplaceDescriptionsAsync(
            sourceId,
            initialConfiguration.NormalizedDescriptions,
            userId,
            now,
            cancellationToken);
    }

    private static void EnsureConfigurationMembership(
        LinkingSourceConfiguration configuration,
        LinkingResolvedSourceCompact compact)
    {
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

}
