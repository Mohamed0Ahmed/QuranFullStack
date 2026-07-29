using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabRelationsReader
{
    Task<IReadOnlyList<AbwabDoorRelationDto>?> GetForDoorAsync(int doorId, CancellationToken cancellationToken);
}
