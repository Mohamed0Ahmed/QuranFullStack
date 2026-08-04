using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Domain.Abwab;

namespace QuranDashboard.Infrastructure.Persistence.Writes.Abwab;

internal sealed class EfAbwabRelationsWriter(QuranDashboardDbContext db) : IAbwabRelationsWriter
{
    public async Task<IReadOnlyList<AbwabDoorRelationDto>> AddAsync(
        int doorId,
        AbwabRelationType type,
        AbwabRelationDirection? direction,
        IReadOnlyList<int> targetDoorIds,
        CancellationToken cancellationToken)
    {
        var targetIds = targetDoorIds.Distinct().ToList();
        if (targetIds.Contains(doorId))
        {
            throw new AbwabRelationSelfException();
        }

        var requestedIds = targetIds.Append(doorId).ToList();
        var doors = await db.AbwabDoors.AsNoTracking()
            .Where(d => requestedIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name, d.DeletedAtUtc })
            .ToListAsync(cancellationToken);

        if (doors.Count != requestedIds.Count)
        {
            throw new AbwabNotFoundException();
        }

        if (doors.Exists(d => d.DeletedAtUtc != null))
        {
            throw new AbwabRelationArchivedDoorException();
        }

        var namesById = doors.ToDictionary(d => d.Id, d => d.Name);
        await GuardAgainstExistingAsync(doorId, type, targetIds, namesById, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var relations = targetIds
            .Select(targetId => new AbwabDoorRelation
            {
                DoorAId = Math.Min(doorId, targetId),
                DoorBId = Math.Max(doorId, targetId),
                RelationType = type,
                BroaderDoorId = ResolveBroaderDoorId(type, direction, doorId, targetId),
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            })
            .ToList();

        db.AbwabDoorRelations.AddRange(relations);
        await SaveTranslatingDuplicateAsync(cancellationToken);

        return relations
            .Select(relation => new AbwabDoorRelationDto(
                relation.Id,
                relation.DoorAId == doorId ? relation.DoorBId : relation.DoorAId,
                namesById[relation.DoorAId == doorId ? relation.DoorBId : relation.DoorAId],
                relation.RelationType,
                ResolveDirection(relation.RelationType, relation.BroaderDoorId, doorId)))
            .ToList();
    }

    public async Task<bool> DeleteAsync(int relationId, CancellationToken cancellationToken)
    {
        var relation = await db.AbwabDoorRelations
            .FirstOrDefaultAsync(r => r.Id == relationId && r.DeletedAtUtc == null, cancellationToken);
        if (relation is null)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        relation.DeletedAtUtc = now;
        relation.UpdatedAtUtc = now;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }

        return true;
    }

    private async Task GuardAgainstExistingAsync(
        int doorId,
        AbwabRelationType type,
        IReadOnlyList<int> targetIds,
        IReadOnlyDictionary<int, string> namesById,
        CancellationToken cancellationToken)
    {
        var existingPartnerIds = await db.AbwabDoorRelations.AsNoTracking()
            .Where(r => r.DeletedAtUtc == null
                && r.RelationType == type
                && (r.DoorAId == doorId || r.DoorBId == doorId))
            .Select(r => r.DoorAId == doorId ? r.DoorBId : r.DoorAId)
            .ToListAsync(cancellationToken);

        var collisions = targetIds
            .Where(existingPartnerIds.Contains)
            .Select(id => namesById[id])
            .ToList();

        if (collisions.Count > 0)
        {
            throw new AbwabRelationDuplicateException(collisions);
        }
    }

    private async Task SaveTranslatingDuplicateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new AbwabRelationDuplicateException([]);
        }
    }

    private static int? ResolveBroaderDoorId(
        AbwabRelationType type, AbwabRelationDirection? direction, int anchorDoorId, int targetDoorId)
    {
        if (type != AbwabRelationType.Comprehensiveness)
        {
            return null;
        }

        return direction switch
        {
            AbwabRelationDirection.AnchorMoreComprehensive => anchorDoorId,
            AbwabRelationDirection.AnchorLessComprehensive => targetDoorId,
            _ => throw new ArgumentOutOfRangeException(
                nameof(direction), direction, "A Comprehensiveness relation requires a stated direction."),
        };
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
