using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingWorkspaceWriter
{
    Task<LinkingWorkspaceDto> AddSourceAsync(
        int userId,
        LinkingSourceDescriptor descriptor,
        LinkingWorkspaceConfigurationInput? initialConfiguration,
        uint? expectedWorkspaceVersion,
        CancellationToken cancellationToken);

    Task<LinkingWorkspaceDto> RemoveSourceAsync(
        int userId,
        long sourceId,
        uint expectedWorkspaceVersion,
        CancellationToken cancellationToken);

    Task<LinkingWorkspaceDto> UpdateSourceTypesAsync(
        int userId,
        long sourceId,
        IReadOnlyList<string> typeCodes,
        uint expectedWorkspaceVersion,
        uint expectedSourceVersion,
        CancellationToken cancellationToken);

    Task<LinkingWorkspaceDto> ReorderSourcesAsync(
        int userId,
        IReadOnlyList<long> orderedSourceIds,
        uint expectedWorkspaceVersion,
        CancellationToken cancellationToken);

    Task<LinkingWorkspaceDto> ClearSourcesAsync(
        int userId,
        uint expectedWorkspaceVersion,
        CancellationToken cancellationToken);

    Task<LinkingWorkspaceDeltaAcknowledgement> ApplyDeltaAsync(
        int userId,
        long sourceId,
        LinkingWorkspaceDeltaInput delta,
        CancellationToken cancellationToken);
}
