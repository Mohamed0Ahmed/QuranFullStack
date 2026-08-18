using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab.Inclusions;

public interface IAbwabDoorInclusionsReader
{
    Task<AbwabDoorInclusionTopologyDto?> GetAsync(
        int doorId,
        CancellationToken cancellationToken);
}
