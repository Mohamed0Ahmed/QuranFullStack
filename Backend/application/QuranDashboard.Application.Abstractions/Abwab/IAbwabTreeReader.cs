using QuranDashboard.Application.Abstractions.Abwab.Responses;

namespace QuranDashboard.Application.Abstractions.Abwab;

public interface IAbwabTreeReader
{
    Task<AbwabTreeDto> GetTreeAsync(CancellationToken cancellationToken);
}
