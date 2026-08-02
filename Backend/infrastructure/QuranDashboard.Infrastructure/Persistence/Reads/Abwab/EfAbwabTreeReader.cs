using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

internal sealed class EfAbwabTreeReader(QuranDashboardDbContext db) : IAbwabTreeReader
{
    public async Task<AbwabTreeDto> GetTreeAsync(CancellationToken cancellationToken)
    {
        // Archived sections are never restorable in this slice (no restore route exists for sections —
        // only doors have one), so an archived section is excluded outright rather than included-and-flagged.
        var sections = await db.AbwabSections.AsNoTracking()
            .Where(s => s.DeletedAtUtc == null)
            .OrderBy(s => s.OrderValue).ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        // Every door, live and archived — doors DO have a restore route, so §4's "archived included,
        // flagged" applies here. Ordered by scope (section, parent) then sibling order, Id as a pure
        // hardening tie-break (OrderValue is unique within a scope by construction, never across scopes).
        var doors = await db.AbwabDoors.AsNoTracking()
            .OrderBy(d => d.SectionId).ThenBy(d => d.ParentId).ThenBy(d => d.OrderValue).ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);

        var aliasesByDoor = await db.AbwabDoorAliases.AsNoTracking()
            .Where(a => a.DeletedAtUtc == null)
            .OrderBy(a => a.Id)
            .GroupBy(a => a.DoorId)
            .ToDictionaryAsync(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.Value).ToList(), cancellationToken);

        // Both counts are LIVE-only: they are the "how many doors are here right now" badge, not inflated
        // by an archived subtree that the main view no longer shows. Archived doors stay individually
        // visible via IsArchived; they just do not count toward a parent's or section's live total.
        var liveChildCounts = doors
            .Where(d => d.DeletedAtUtc == null && d.ParentId.HasValue)
            .GroupBy(d => d.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        var liveSectionCounts = doors
            .Where(d => d.DeletedAtUtc == null)
            .GroupBy(d => d.SectionId)
            .ToDictionary(g => g.Key, g => g.Count());

        // The sections list above holds only live ones, so it cannot answer "is this door's section
        // retired" for an archived door — the one door restore has to ask about.
        var retiredSectionIds = await db.AbwabSections.AsNoTracking()
            .Where(s => s.DeletedAtUtc != null)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        var retiredSections = retiredSectionIds.ToHashSet();

        var relationCounts = await GetLiveRelationCountsAsync(cancellationToken);

        var sectionDtos = sections
            .Select(s => new AbwabTreeSectionDto(
                s.Id, s.Name, s.OrderValue, s.Version, liveSectionCounts.GetValueOrDefault(s.Id)))
            .ToList();

        var doorDtos = doors
            .Select(d => new AbwabTreeDoorDto(
                d.Id, d.SectionId, retiredSections.Contains(d.SectionId), d.ParentId, d.Name, d.Description, d.RepresentativeAyahText,
                d.OrderValue, d.GlobalOrderValue, d.Version, d.DeletedAtUtc != null, liveChildCounts.GetValueOrDefault(d.Id),
                relationCounts.GetValueOrDefault(d.Id),
                aliasesByDoor.GetValueOrDefault(d.Id, [])))
            .ToList();

        var version = await GetSnapshotVersionAsync(cancellationToken);

        return new AbwabTreeDto(version, sectionDtos, doorDtos);
    }

    private async Task<Dictionary<int, int>> GetLiveRelationCountsAsync(CancellationToken cancellationToken)
    {
        var livePairs = await (
            from relation in db.AbwabDoorRelations.AsNoTracking()
            join doorA in db.AbwabDoors.AsNoTracking() on relation.DoorAId equals doorA.Id
            join doorB in db.AbwabDoors.AsNoTracking() on relation.DoorBId equals doorB.Id
            where relation.DeletedAtUtc == null
                && doorA.DeletedAtUtc == null
                && doorB.DeletedAtUtc == null
            select new { relation.DoorAId, relation.DoorBId })
            .ToListAsync(cancellationToken);

        var counts = new Dictionary<int, int>();
        foreach (var pair in livePairs)
        {
            counts[pair.DoorAId] = counts.GetValueOrDefault(pair.DoorAId) + 1;
            counts[pair.DoorBId] = counts.GetValueOrDefault(pair.DoorBId) + 1;
        }

        return counts;
    }

    // max(updated_at, deleted_at) across the three tables, one query per table: each row's own greatest
    // of the two, then MAX() aggregates across rows — MaxAsync on a nullable projection returns null for
    // an empty table rather than throwing, which is exactly "no rows yet" for an empty schema. Three
    // near-identical queries rather than one generic helper: the entities share no common interface, and
    // introducing one just for this single read would be an abstraction with a single caller.
    private async Task<DateTimeOffset?> GetSnapshotVersionAsync(CancellationToken cancellationToken)
    {
        var sectionsMax = await db.AbwabSections.AsNoTracking()
            .Select(s => (DateTimeOffset?)(s.DeletedAtUtc != null && s.DeletedAtUtc > s.UpdatedAtUtc ? s.DeletedAtUtc.Value : s.UpdatedAtUtc))
            .MaxAsync(cancellationToken);
        var doorsMax = await db.AbwabDoors.AsNoTracking()
            .Select(d => (DateTimeOffset?)(d.DeletedAtUtc != null && d.DeletedAtUtc > d.UpdatedAtUtc ? d.DeletedAtUtc.Value : d.UpdatedAtUtc))
            .MaxAsync(cancellationToken);
        var aliasesMax = await db.AbwabDoorAliases.AsNoTracking()
            .Select(a => (DateTimeOffset?)(a.DeletedAtUtc != null && a.DeletedAtUtc > a.UpdatedAtUtc ? a.DeletedAtUtc.Value : a.UpdatedAtUtc))
            .MaxAsync(cancellationToken);

        DateTimeOffset?[] candidates = [sectionsMax, doorsMax, aliasesMax];
        return candidates.Max();
    }
}
