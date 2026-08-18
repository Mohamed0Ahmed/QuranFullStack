using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab.Inclusions;

internal sealed partial class EfAbwabDoorInclusionsWriter
{
    private async Task<AbwabDoorInclusionAddWriteResult> AddWithinTransactionAsync(
        int targetDoorId,
        uint expectedTargetDoorVersion,
        IReadOnlyList<int> sourceDoorIds,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        await syncLock.TakeAfterGlobalLocksBeforeDoorAndUnitLocksAsync(cancellationToken);

        var activeEdges = await db.AbwabDoorInclusions.AsNoTracking()
            .Where(inclusion => inclusion.DeletedAtUtc == null)
            .Select(inclusion => new AbwabDoorInclusionGraph.Edge(
                inclusion.SourceDoorId,
                inclusion.TargetDoorId))
            .ToListAsync(cancellationToken);
        var graph = new AbwabDoorInclusionGraph(activeEdges);
        if (sourceDoorIds.Any(sourceDoorId => graph.ContainsDirectEdge(sourceDoorId, targetDoorId)))
        {
            return new AbwabDoorInclusionAddWriteResult.Duplicate();
        }

        if (graph.WouldCycle(targetDoorId, sourceDoorIds))
        {
            return new AbwabDoorInclusionAddWriteResult.Cycle();
        }

        var doorIdsToLock = sourceDoorIds
            .Append(targetDoorId)
            .Concat(graph.ReachableConsumersOf(targetDoorId))
            .Distinct()
            .Order()
            .ToArray();
        var lockedDoors = await LockDoorsAsync(doorIdsToLock, cancellationToken);
        if (lockedDoors.Count != doorIdsToLock.Length)
        {
            return new AbwabDoorInclusionAddWriteResult.NotFound();
        }

        var doorsById = lockedDoors.ToDictionary(door => door.Id);
        var targetDoor = doorsById[targetDoorId];
        if (targetDoor.DeletedAtUtc is not null
            || sourceDoorIds.Any(sourceDoorId => doorsById[sourceDoorId].DeletedAtUtc is not null))
        {
            return new AbwabDoorInclusionAddWriteResult.ArchivedDoor();
        }

        if (targetDoor.Version != expectedTargetDoorVersion)
        {
            return new AbwabDoorInclusionAddWriteResult.StaleTargetVersion();
        }

        var now = DateTimeOffset.UtcNow;
        var inclusions = sourceDoorIds.Select(sourceDoorId => new AbwabDoorInclusion
        {
            TargetDoorId = targetDoorId,
            SourceDoorId = sourceDoorId,
            CreatedAtUtc = now,
            CreatedBy = actorUserId,
            UpdatedAtUtc = now,
            UpdatedBy = actorUserId,
        }).ToList();
        db.AbwabDoorInclusions.AddRange(inclusions);
        await db.SaveChangesAsync(cancellationToken);

        var synchronizedDoorIds = await synchronizer.AddInclusionsAsync(
            inclusions,
            actorUserId,
            cancellationToken);
        var changedDoorIds = synchronizedDoorIds.Append(targetDoorId).Distinct().ToArray();
        foreach (var changedDoorId in changedDoorIds)
        {
            var door = doorsById[changedDoorId];
            door.UpdatedAtUtc = now;
            door.UpdatedBy = actorUserId;
        }

        await db.SaveChangesAsync(cancellationToken);

        var added = inclusions
            .OrderBy(inclusion => inclusion.SourceDoorId)
            .Select(inclusion => new AbwabDoorInclusionDto(
                inclusion.Id,
                inclusion.TargetDoorId,
                inclusion.SourceDoorId,
                doorsById[inclusion.SourceDoorId].Name,
                false))
            .ToArray();
        return new AbwabDoorInclusionAddWriteResult.Success(
            new AbwabDoorInclusionAddResultDto(targetDoorId, targetDoor.Version, added));
    }

    private async Task<IReadOnlyList<AbwabDoor>> LockDoorsAsync(
        int[] doorIds,
        CancellationToken cancellationToken) =>
        await db.AbwabDoors.FromSqlInterpolated(
                $"""
                SELECT id, section_id, parent_id, name, description, representative_ayah_text,
                       order_value, global_order_value, created_at, created_by, updated_at, updated_by,
                       approved_at, approved_by, deleted_at, deleted_by, xmin
                FROM abwab_doors
                WHERE id = ANY ({doorIds})
                ORDER BY id
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);
}
