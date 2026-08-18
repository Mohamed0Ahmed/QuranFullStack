using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

internal sealed class EfAbwabTreeReader(QuranDashboardDbContext db) : IAbwabTreeReader
{
    public async Task<AbwabTreeDto> GetTreeAsync(CancellationToken cancellationToken)
    {
        var sections = await db.AbwabSections.AsNoTracking()
            .Where(s => s.DeletedAtUtc == null)
            .OrderBy(s => s.OrderValue).ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var doors = await db.AbwabDoors.AsNoTracking()
            .OrderBy(d => d.SectionId).ThenBy(d => d.ParentId).ThenBy(d => d.OrderValue).ThenBy(d => d.Id)
            .ToListAsync(cancellationToken);

        var aliasesByDoor = await db.AbwabDoorAliases.AsNoTracking()
            .Where(a => a.DeletedAtUtc == null)
            .OrderBy(a => a.Id)
            .GroupBy(a => a.DoorId)
            .ToDictionaryAsync(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.Value).ToList(), cancellationToken);

        var liveChildCounts = doors
            .Where(d => d.DeletedAtUtc == null && d.ParentId.HasValue)
            .GroupBy(d => d.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
        var liveSectionCounts = doors
            .Where(d => d.DeletedAtUtc == null)
            .GroupBy(d => d.SectionId)
            .ToDictionary(g => g.Key, g => g.Count());

        var retiredSectionIds = await db.AbwabSections.AsNoTracking()
            .Where(s => s.DeletedAtUtc != null)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
        var retiredSections = retiredSectionIds.ToHashSet();

        var relationCounts = await GetLiveRelationCountsAsync(cancellationToken);
        var linkMetrics = await GetLiveLinkMetricsAsync(cancellationToken);
        var inclusionCounts = await GetActiveInclusionCountsAsync(cancellationToken);

        var sectionDtos = sections
            .Select(s => new AbwabTreeSectionDto(
                s.Id, s.Name, s.OrderValue, s.Version, liveSectionCounts.GetValueOrDefault(s.Id)))
            .ToList();

        var doorDtos = doors.Select(d =>
        {
            var metrics = linkMetrics.GetValueOrDefault(d.Id);
            var inclusions = inclusionCounts.GetValueOrDefault(d.Id);
            return new AbwabTreeDoorDto(
                d.Id, d.SectionId, retiredSections.Contains(d.SectionId), d.ParentId, d.Name, d.Description, d.RepresentativeAyahText,
                d.OrderValue, d.GlobalOrderValue, d.Version, d.DeletedAtUtc != null, liveChildCounts.GetValueOrDefault(d.Id),
                relationCounts.GetValueOrDefault(d.Id),
                metrics.LinkCount,
                metrics.SelectedWordCount,
                inclusions.SourceCount,
                inclusions.ConsumerCount,
                aliasesByDoor.GetValueOrDefault(d.Id, []));
        }).ToList();

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

    private async Task<Dictionary<int, DoorLinkMetrics>> GetLiveLinkMetricsAsync(
        CancellationToken cancellationToken)
    {
        var linkCounts = await LiveUnits()
            .GroupBy(unit => unit.DoorId)
            .Select(units => new { DoorId = units.Key, Count = units.Count() })
            .ToDictionaryAsync(count => count.DoorId, count => count.Count, cancellationToken);

        var selectedWordCounts = await (
                from unit in LiveUnits()
                join unitAyah in db.LinkingUnitAyahs.AsNoTracking() on unit.Id equals unitAyah.UnitId
                join word in db.LinkingUnitAyahWords.AsNoTracking() on unitAyah.Id equals word.UnitAyahId
                group word by unit.DoorId
                into wordsByDoor
                select new { DoorId = wordsByDoor.Key, Count = wordsByDoor.Count() })
            .ToDictionaryAsync(count => count.DoorId, count => count.Count, cancellationToken);

        return linkCounts.ToDictionary(
            count => count.Key,
            count => new DoorLinkMetrics(count.Value, selectedWordCounts.GetValueOrDefault(count.Key)));
    }

    private async Task<Dictionary<int, DoorInclusionCounts>> GetActiveInclusionCountsAsync(
        CancellationToken cancellationToken)
    {
        var edges = await db.AbwabDoorInclusions.AsNoTracking()
            .Where(inclusion => inclusion.DeletedAtUtc == null)
            .Select(inclusion => new { inclusion.TargetDoorId, inclusion.SourceDoorId })
            .ToListAsync(cancellationToken);
        var counts = new Dictionary<int, DoorInclusionCounts>();

        foreach (var edge in edges)
        {
            var targetCounts = counts.GetValueOrDefault(edge.TargetDoorId);
            counts[edge.TargetDoorId] = targetCounts with { SourceCount = targetCounts.SourceCount + 1 };

            var sourceCounts = counts.GetValueOrDefault(edge.SourceDoorId);
            counts[edge.SourceDoorId] = sourceCounts with { ConsumerCount = sourceCounts.ConsumerCount + 1 };
        }

        return counts;
    }

    private IQueryable<Domain.Linking.LinkingUnit> LiveUnits() =>
        db.LinkingUnits.AsNoTracking()
            .Where(unit =>
                db.LinkingSourceContributionUnits.AsNoTracking()
                    .Where(mapping => mapping.UnitId == unit.Id)
                    .Join(
                        db.LinkingSourceContributions.AsNoTracking()
                            .Where(contribution =>
                                contribution.DoorId == unit.DoorId
                                && contribution.DeletedAtUtc == null),
                        mapping => mapping.SourceContributionId,
                        contribution => contribution.Id,
                        (_, _) => 1)
                    .Any());

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

    private readonly record struct DoorLinkMetrics(int LinkCount, int SelectedWordCount);

    private readonly record struct DoorInclusionCounts(int SourceCount, int ConsumerCount);
}
