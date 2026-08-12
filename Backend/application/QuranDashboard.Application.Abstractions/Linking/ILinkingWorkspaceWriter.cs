using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingWorkspaceWriter
{
    Task<LinkingWorkspaceDto> AddSourceAsync(
        int userId,
        LinkingSourceDescriptor descriptor,
        uint? expectedWorkspaceVersion,
        CancellationToken cancellationToken);

    Task<LinkingWorkspaceDto> RemoveSourceAsync(
        int userId,
        long sourceId,
        uint expectedWorkspaceVersion,
        CancellationToken cancellationToken);

    Task<LinkingWorkspaceDto> ReorderSourcesAsync(
        int userId,
        IReadOnlyList<long> orderedSourceIds,
        uint expectedWorkspaceVersion,
        CancellationToken cancellationToken);

    Task<LinkingWorkspaceDto> ReplaceSourceConfigurationAsync(
        int userId,
        long sourceId,
        LinkingWorkspaceConfigurationInput configuration,
        uint expectedSourceVersion,
        CancellationToken cancellationToken);

    Task<LinkingWorkspaceDto> ClearSourcesAsync(
        int userId,
        uint expectedWorkspaceVersion,
        CancellationToken cancellationToken);
}
