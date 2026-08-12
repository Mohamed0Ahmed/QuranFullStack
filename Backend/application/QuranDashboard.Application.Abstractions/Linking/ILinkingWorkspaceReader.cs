using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingWorkspaceReader
{
    Task<LinkingWorkspaceDto> LoadAsync(int userId, CancellationToken cancellationToken);
}
