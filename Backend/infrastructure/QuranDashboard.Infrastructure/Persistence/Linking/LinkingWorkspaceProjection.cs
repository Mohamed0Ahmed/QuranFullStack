using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Linking;

internal static class LinkingWorkspaceProjection
{
    public static readonly LinkingWorkspaceDto Empty = new(null, []);

    public static async Task<LinkingWorkspaceDto> ProjectAsync(
        QuranDashboardDbContext db,
        LinkingWorkspace workspace,
        CancellationToken cancellationToken)
    {
        var sources = await db.LinkingWorkspaceSources
            .AsNoTracking()
            .Where(source => source.WorkspaceId == workspace.Id)
            .OrderBy(source => source.OrderValue)
            .ThenBy(source => source.Id)
            .ToListAsync(cancellationToken);

        if (sources.Count == 0)
        {
            return new LinkingWorkspaceDto(workspace.Version, []);
        }

        var sourceIds = sources.Select(source => source.Id).ToList();

        var manualAyahRows = await (
            from manualAyah in db.LinkingWorkspaceSourceManualAyahs.AsNoTracking()
            join ayah in db.QuranAyahs.AsNoTracking() on manualAyah.AyahId equals ayah.Id
            where sourceIds.Contains(manualAyah.WorkspaceSourceId)
            orderby manualAyah.WorkspaceSourceId, manualAyah.OrderValue
            select new ManualAyahRow(
                manualAyah.WorkspaceSourceId,
                manualAyah.AyahId,
                ayah.VerseKey,
                manualAyah.OrderValue,
                manualAyah.PageHint))
            .ToListAsync(cancellationToken);

        var overrideRows = await db.LinkingWorkspaceSourceAyahOverrides
            .AsNoTracking()
            .Where(ayahOverride => sourceIds.Contains(ayahOverride.WorkspaceSourceId))
            .OrderBy(ayahOverride => ayahOverride.AyahId)
            .Select(ayahOverride => new OverrideRow(ayahOverride.WorkspaceSourceId, ayahOverride.AyahId))
            .ToListAsync(cancellationToken);

        var wordRows = await db.LinkingWorkspaceSourceWords
            .AsNoTracking()
            .Where(word => sourceIds.Contains(word.WorkspaceSourceId))
            .OrderBy(word => word.AyahId)
            .ThenBy(word => word.QuranWordId)
            .Select(word => new WordRow(word.WorkspaceSourceId, word.AyahId, word.QuranWordId))
            .ToListAsync(cancellationToken);

        var manualAyahsBySource = manualAyahRows
            .GroupBy(row => row.WorkspaceSourceId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var overridesBySource = overrideRows
            .GroupBy(row => row.WorkspaceSourceId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var wordsBySource = wordRows
            .GroupBy(row => row.WorkspaceSourceId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var projected = new List<LinkingWorkspaceSourceDto>(sources.Count);

        foreach (var source in sources)
        {
            var manualAyahs = manualAyahsBySource.GetValueOrDefault(source.Id, []);

            projected.Add(new LinkingWorkspaceSourceDto(
                source.Id,
                source.Version,
                source.OrderValue,
                LinkingSourceStorage.Decode(source, [.. manualAyahs.Select(row => row.VerseKey)]),
                source.SourceIdentity,
                LinkingWorkspaceTokens.ToToken(source.InclusionMode),
                [.. overridesBySource.GetValueOrDefault(source.Id, []).Select(row => row.AyahId)],
                [
                    .. wordsBySource.GetValueOrDefault(source.Id, [])
                        .Select(row => new LinkingWorkspaceSelectedWordDto(row.AyahId, row.QuranWordId))
                ],
                source.AutomaticWordMatchesEnabled,
                source.ManualLinkShape.HasValue
                    ? LinkingWorkspaceTokens.ToToken(source.ManualLinkShape.Value)
                    : null,
                [
                    .. manualAyahs.Select(row => new LinkingWorkspaceManualAyahDto(
                        row.AyahId, row.VerseKey, row.OrderValue, row.PageHint))
                ],
                [],
                source.LastResolvedCount,
                source.LastResolvedAtUtc));
        }

        return new LinkingWorkspaceDto(workspace.Version, projected);
    }

    private sealed record ManualAyahRow(
        long WorkspaceSourceId,
        int AyahId,
        string VerseKey,
        int OrderValue,
        int? PageHint);

    private sealed record OverrideRow(long WorkspaceSourceId, int AyahId);

    private sealed record WordRow(long WorkspaceSourceId, int AyahId, int QuranWordId);
}
