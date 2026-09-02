using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Infrastructure.Persistence.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Linking;

internal sealed partial class EfLinkingWorkspaceWriter
{
    private static void EnsureLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new LinkingWorkspaceViolationException(new LinkingWorkspaceViolation(
                LinkingWorkspaceViolationCode.LabelInvalid, "label", label));
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
