using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Abwab;

internal sealed class EfAbwabDoorInclusionsReader(QuranDashboardDbContext db)
    : IAbwabDoorInclusionsReader
{
    public async Task<AbwabDoorInclusionTopologyDto?> GetAsync(
        int doorId,
        CancellationToken cancellationToken)
    {
        var door = await db.AbwabDoors.AsNoTracking()
            .Where(candidate => candidate.Id == doorId)
            .Select(candidate => new { candidate.Id, candidate.Version })
            .SingleOrDefaultAsync(cancellationToken);
        if (door is null)
        {
            return null;
        }

        var sources = await (
                from inclusion in db.AbwabDoorInclusions.AsNoTracking()
                join source in db.AbwabDoors.AsNoTracking()
                    on inclusion.SourceDoorId equals source.Id
                where inclusion.TargetDoorId == doorId && inclusion.DeletedAtUtc == null
                orderby source.Name, source.Id, inclusion.Id
                select new AbwabDirectInclusionDoorDto(
                    inclusion.Id,
                    source.Id,
                    source.Name,
                    source.DeletedAtUtc != null))
            .ToListAsync(cancellationToken);

        var consumers = await (
                from inclusion in db.AbwabDoorInclusions.AsNoTracking()
                join target in db.AbwabDoors.AsNoTracking()
                    on inclusion.TargetDoorId equals target.Id
                where inclusion.SourceDoorId == doorId && inclusion.DeletedAtUtc == null
                orderby target.Name, target.Id, inclusion.Id
                select new AbwabDirectInclusionDoorDto(
                    inclusion.Id,
                    target.Id,
                    target.Name,
                    target.DeletedAtUtc != null))
            .ToListAsync(cancellationToken);

        return new AbwabDoorInclusionTopologyDto(door.Id, door.Version, sources, consumers);
    }
}
