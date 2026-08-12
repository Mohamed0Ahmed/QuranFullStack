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

        var descriptions = NormalizeDescriptions(configuration.Descriptions);

        var overrideAyahIds = configuration.AyahOverrides.Distinct().ToList();
        await EnsureAyahsExistAsync(overrideAyahIds, cancellationToken);

        var sourceAyahIds = selectedWords.Count > 0 || descriptions.Count > 0
            ? await LoadSourceAyahIdsAsync(source, cancellationToken)
            : [];

        EnsureSelectedWordAyahs(selectedWords, sourceAyahIds);
        await EnsureSelectedWordsAsync(selectedWords, cancellationToken);
        EnsureDescriptionAyahs(descriptions, sourceAyahIds);

        var now = DateTimeOffset.UtcNow;
        source.Label = configuration.Label;
        source.InclusionMode = configuration.InclusionMode;
        source.AutomaticWordMatchesEnabled = configuration.AutomaticWordMatchesEnabled;
        source.ManualLinkShape = configuration.ManualLinkShape;
        source.UpdatedAtUtc = now;
        source.UpdatedBy = userId;

        await ReplaceOverridesAsync(source.Id, overrideAyahIds, cancellationToken);
        await ReplaceSelectedWordsAsync(source.Id, selectedWords, cancellationToken);
        await ReplaceDescriptionsAsync(source.Id, descriptions, userId, now, cancellationToken);

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

    private static List<LinkingWorkspaceAyahDescriptions> NormalizeDescriptions(
        IReadOnlyList<LinkingWorkspaceDescriptionInput> descriptions)
    {
        var violation = LinkingWorkspaceDescriptionValidation.TryNormalize(descriptions, out var normalized);

        return violation is null
            ? [.. normalized]
            : throw new LinkingWorkspaceViolationException(violation);
    }

    private async Task<HashSet<int>> LoadSourceAyahIdsAsync(
        LinkingWorkspaceSource source,
        CancellationToken cancellationToken)
    {
        if (source.SourceKind != LinkingSourceKind.ManualMushafAyahs)
        {
            var resolved = await resolution.ResolveAsync(
                LinkingSourceStorage.Decode(source, []), cancellationToken);

            return [.. resolved.Ayahs.Select(ayah => ayah.AyahId)];
        }

        var manualAyahIds = await db.LinkingWorkspaceSourceManualAyahs
            .AsNoTracking()
            .Where(manualAyah => manualAyah.WorkspaceSourceId == source.Id)
            .Select(manualAyah => manualAyah.AyahId)
            .ToListAsync(cancellationToken);

        return [.. manualAyahIds];
    }

    private static void EnsureSelectedWordAyahs(
        IReadOnlyList<LinkingWorkspaceSelectedWordInput> selectedWords,
        HashSet<int> sourceAyahIds)
    {
        foreach (var selectedWord in selectedWords.Where(word => !sourceAyahIds.Contains(word.AyahId)))
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.SelectedWordAyahOutsideManualSet,
                "selectedWords.ayahId",
                selectedWord.AyahId.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static void EnsureDescriptionAyahs(
        IReadOnlyList<LinkingWorkspaceAyahDescriptions> descriptions,
        HashSet<int> sourceAyahIds)
    {
        foreach (var ayah in descriptions.Where(ayah => !sourceAyahIds.Contains(ayah.AyahId)))
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.DescriptionAyahOutsideSource,
                LinkingWorkspaceDescriptionValidation.DescriptionsField,
                ayah.AyahId.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private async Task EnsureSelectedWordsAsync(
        IReadOnlyList<LinkingWorkspaceSelectedWordInput> selectedWords,
        CancellationToken cancellationToken)
    {
        if (selectedWords.Count == 0)
        {
            return;
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

    private async Task ReplaceDescriptionsAsync(
        long sourceId,
        IReadOnlyList<LinkingWorkspaceAyahDescriptions> descriptions,
        int userId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await db.LinkingWorkspaceSourceDescriptions
            .Where(description => description.WorkspaceSourceId == sourceId)
            .ToListAsync(cancellationToken);

        var existingByAyah = existing
            .GroupBy(description => description.AyahId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(description => description.OrderValue).ToList());

        foreach (var ayah in descriptions)
        {
            var rows = existingByAyah.GetValueOrDefault(ayah.AyahId, []);
            existingByAyah.Remove(ayah.AyahId);

            ResequenceDescriptions(sourceId, ayah, rows, userId, now);
        }

        foreach (var rows in existingByAyah.Values)
        {
            db.LinkingWorkspaceSourceDescriptions.RemoveRange(rows);
        }
    }

    private void ResequenceDescriptions(
        long sourceId,
        LinkingWorkspaceAyahDescriptions ayah,
        List<LinkingWorkspaceSourceDescription> rows,
        int userId,
        DateTimeOffset now)
    {
        for (var index = 0; index < ayah.Bodies.Count; index++)
        {
            var orderValue = index + 1;
            var body = ayah.Bodies[index];

            if (index >= rows.Count)
            {
                db.LinkingWorkspaceSourceDescriptions.Add(new LinkingWorkspaceSourceDescription
                {
                    WorkspaceSourceId = sourceId,
                    AyahId = ayah.AyahId,
                    OrderValue = orderValue,
                    Body = body,
                    CreatedAtUtc = now,
                    CreatedBy = userId,
                    UpdatedAtUtc = now,
                    UpdatedBy = userId,
                });

                continue;
            }

            var row = rows[index];
            if (row.OrderValue == orderValue && string.Equals(row.Body, body, StringComparison.Ordinal))
            {
                continue;
            }

            row.OrderValue = orderValue;
            row.Body = body;
            row.UpdatedAtUtc = now;
            row.UpdatedBy = userId;
        }

        if (rows.Count > ayah.Bodies.Count)
        {
            db.LinkingWorkspaceSourceDescriptions.RemoveRange(rows.Skip(ayah.Bodies.Count));
        }
    }
}
