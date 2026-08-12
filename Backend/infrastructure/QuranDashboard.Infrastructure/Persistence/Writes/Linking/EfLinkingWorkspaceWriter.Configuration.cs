using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingWorkspaceWriter
{
    public async Task<LinkingWorkspaceDto> ReplaceSourceConfigurationAsync(
        int userId,
        long sourceId,
        LinkingWorkspaceConfigurationInput configuration,
        uint expectedSourceVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var workspace = await LoadWorkspaceAsync(userId, cancellationToken)
            ?? throw new LinkingWorkspaceSourceNotFoundException(sourceId);

        var source = await db.LinkingWorkspaceSources
            .FirstOrDefaultAsync(
                candidate => candidate.Id == sourceId && candidate.WorkspaceId == workspace.Id,
                cancellationToken)
            ?? throw new LinkingWorkspaceSourceNotFoundException(sourceId);

        db.Entry(source).Property(entity => entity.Version).OriginalValue = expectedSourceVersion;

        var isManual = source.SourceKind == LinkingSourceKind.ManualMushafAyahs;

        EnsureLabel(configuration.Label);
        EnsureConfigurationCoherence(isManual, configuration);

        var selectedWords = DistinctSelectedWords(configuration.SelectedWords);

        if (!isManual && selectedWords.Count > 0)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.WordsNotAllowedOnAutomaticSource, "selectedWords", null));
        }

        var overrideAyahIds = configuration.AyahOverrides.Distinct().ToList();
        await EnsureAyahsExistAsync(overrideAyahIds, cancellationToken);
        await EnsureSelectedWordsAsync(source.Id, selectedWords, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        source.Label = configuration.Label;
        source.InclusionMode = configuration.InclusionMode;
        source.AutomaticWordMatchesEnabled = configuration.AutomaticWordMatchesEnabled;
        source.ManualLinkShape = configuration.ManualLinkShape;
        source.UpdatedAtUtc = now;
        source.UpdatedBy = userId;

        await ReplaceOverridesAsync(source.Id, overrideAyahIds, cancellationToken);
        await ReplaceSelectedWordsAsync(source.Id, selectedWords, cancellationToken);

        await SaveTranslatingWriteExceptionsAsync(cancellationToken);

        return await LinkingWorkspaceProjection.ProjectAsync(db, workspace, cancellationToken);
    }

    private static List<LinkingWorkspaceSelectedWordInput> DistinctSelectedWords(
        IReadOnlyList<LinkingWorkspaceSelectedWordInput> selectedWords)
    {
        var ayahIdByWordId = new Dictionary<int, int>();
        var distinct = new List<LinkingWorkspaceSelectedWordInput>();

        foreach (var selectedWord in selectedWords)
        {
            if (!ayahIdByWordId.TryGetValue(selectedWord.QuranWordId, out var firstAyahId))
            {
                ayahIdByWordId.Add(selectedWord.QuranWordId, selectedWord.AyahId);
                distinct.Add(selectedWord);

                continue;
            }

            if (firstAyahId != selectedWord.AyahId)
            {
                throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                    LinkingWorkspaceViolationCode.SelectedWordAyahConflict,
                    "selectedWords.quranWordId",
                    selectedWord.QuranWordId.ToString(CultureInfo.InvariantCulture)));
            }
        }

        return distinct;
    }

    private static void EnsureLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.LabelInvalid, "label", label));
        }
    }

    private static void EnsureConfigurationCoherence(bool isManual, LinkingWorkspaceConfigurationInput configuration)
    {
        var coherent = isManual
            ? configuration.ManualLinkShape.HasValue && configuration.AutomaticWordMatchesEnabled is null
            : configuration.AutomaticWordMatchesEnabled.HasValue && configuration.ManualLinkShape is null;

        if (!coherent)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.ConfigurationIncoherent,
                isManual ? "manualLinkShape" : "automaticWordMatchesEnabled",
                null));
        }
    }

    private async Task EnsureAyahsExistAsync(IReadOnlyList<int> ayahIds, CancellationToken cancellationToken)
    {
        if (ayahIds.Count == 0)
        {
            return;
        }

        var existing = await db.QuranAyahs
            .AsNoTracking()
            .Where(ayah => ayahIds.Contains(ayah.Id))
            .Select(ayah => ayah.Id)
            .ToListAsync(cancellationToken);

        var missing = ayahIds.Except(existing).ToList();
        if (missing.Count > 0)
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.AyahReferenceUnknown,
                "ayahOverrides",
                missing[0].ToString(CultureInfo.InvariantCulture)));
        }
    }

    private async Task EnsureSelectedWordsAsync(
        long sourceId,
        IReadOnlyList<LinkingWorkspaceSelectedWordInput> selectedWords,
        CancellationToken cancellationToken)
    {
        if (selectedWords.Count == 0)
        {
            return;
        }

        var manualAyahIds = await db.LinkingWorkspaceSourceManualAyahs
            .AsNoTracking()
            .Where(manualAyah => manualAyah.WorkspaceSourceId == sourceId)
            .Select(manualAyah => manualAyah.AyahId)
            .ToListAsync(cancellationToken);

        var manualAyahIdSet = manualAyahIds.ToHashSet();

        foreach (var selectedWord in selectedWords.Where(word => !manualAyahIdSet.Contains(word.AyahId)))
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.SelectedWordAyahOutsideManualSet,
                "selectedWords.ayahId",
                selectedWord.AyahId.ToString(CultureInfo.InvariantCulture)));
        }

        var wordIds = selectedWords.Select(word => word.QuranWordId).ToList();
        var words = await db.QuranWords
            .AsNoTracking()
            .Where(word => wordIds.Contains(word.Id))
            .Select(word => new { word.Id, word.AyahId, word.IsAyahMarker })
            .ToListAsync(cancellationToken);

        var wordsById = words.ToDictionary(word => word.Id);

        foreach (var selectedWord in selectedWords)
        {
            if (!wordsById.TryGetValue(selectedWord.QuranWordId, out var word)
                || word.IsAyahMarker
                || word.AyahId != selectedWord.AyahId)
            {
                throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                    LinkingWorkspaceViolationCode.SelectedWordInvalid,
                    "selectedWords.quranWordId",
                    selectedWord.QuranWordId.ToString(CultureInfo.InvariantCulture)));
            }
        }
    }

    private async Task ReplaceOverridesAsync(
        long sourceId,
        IReadOnlyList<int> ayahIds,
        CancellationToken cancellationToken)
    {
        var existing = await db.LinkingWorkspaceSourceAyahOverrides
            .Where(ayahOverride => ayahOverride.WorkspaceSourceId == sourceId)
            .ToListAsync(cancellationToken);

        var desired = ayahIds.ToHashSet();

        db.LinkingWorkspaceSourceAyahOverrides.RemoveRange(
            existing.Where(ayahOverride => !desired.Contains(ayahOverride.AyahId)));

        var retained = existing.Select(ayahOverride => ayahOverride.AyahId).ToHashSet();

        foreach (var ayahId in desired.Where(ayahId => !retained.Contains(ayahId)))
        {
            db.LinkingWorkspaceSourceAyahOverrides.Add(new LinkingWorkspaceSourceAyahOverride
            {
                WorkspaceSourceId = sourceId,
                AyahId = ayahId,
            });
        }
    }

    private async Task ReplaceSelectedWordsAsync(
        long sourceId,
        IReadOnlyList<LinkingWorkspaceSelectedWordInput> selectedWords,
        CancellationToken cancellationToken)
    {
        var existing = await db.LinkingWorkspaceSourceWords
            .Where(word => word.WorkspaceSourceId == sourceId)
            .ToListAsync(cancellationToken);

        var desired = selectedWords.ToDictionary(word => word.QuranWordId);

        db.LinkingWorkspaceSourceWords.RemoveRange(
            existing.Where(word => !desired.ContainsKey(word.QuranWordId)));

        var retained = existing.Select(word => word.QuranWordId).ToHashSet();

        foreach (var selectedWord in selectedWords.Where(word => !retained.Contains(word.QuranWordId)))
        {
            db.LinkingWorkspaceSourceWords.Add(new LinkingWorkspaceSourceWord
            {
                WorkspaceSourceId = sourceId,
                QuranWordId = selectedWord.QuranWordId,
                AyahId = selectedWord.AyahId,
            });
        }
    }
}
