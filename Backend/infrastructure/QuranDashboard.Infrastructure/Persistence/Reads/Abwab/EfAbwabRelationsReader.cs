using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

internal sealed class EfAbwabRelationsReader(QuranDashboardDbContext db) : IAbwabRelationsReader
{
    public async Task<IReadOnlyList<AbwabDoorRelationDto>?> GetForDoorAsync(
        int doorId, CancellationToken cancellationToken)
    {
        var doorExists = await db.AbwabDoors.AsNoTracking()
            .AnyAsync(d => d.Id == doorId, cancellationToken);
        if (!doorExists)
        {
            return null;
        }

        var rows = await (
            from relation in db.AbwabDoorRelations.AsNoTracking()
            join doorA in db.AbwabDoors.AsNoTracking() on relation.DoorAId equals doorA.Id
            join doorB in db.AbwabDoors.AsNoTracking() on relation.DoorBId equals doorB.Id
            where relation.DeletedAtUtc == null
                && doorA.DeletedAtUtc == null
                && doorB.DeletedAtUtc == null
                && (relation.DoorAId == doorId || relation.DoorBId == doorId)
            orderby relation.RelationType,
                relation.DoorAId == doorId ? doorB.Name : doorA.Name,
                relation.Id
            select new
            {
                relation.Id,
                relation.RelationType,
                relation.BroaderDoorId,
                OtherDoorId = relation.DoorAId == doorId ? relation.DoorBId : relation.DoorAId,
                OtherDoorName = relation.DoorAId == doorId ? doorB.Name : doorA.Name,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new AbwabDoorRelationDto(
                row.Id,
                row.OtherDoorId,
                row.OtherDoorName,
                row.RelationType,
                ResolveDirection(row.RelationType, row.BroaderDoorId, doorId)))
            .ToList();
    }

    private static AbwabRelationDirection? ResolveDirection(
        AbwabRelationType type, int? broaderDoorId, int anchorDoorId)
    {
        if (type != AbwabRelationType.Comprehensiveness)
        {
            return null;
        }

        return broaderDoorId == anchorDoorId
            ? AbwabRelationDirection.AnchorMoreComprehensive
            : AbwabRelationDirection.AnchorLessComprehensive;
    }
}
