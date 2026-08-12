using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingSourceResolutionReader
{
    Task<LinkingResolvedSourceDto> ResolveAsync(
        LinkingSourceDescriptor descriptor,
        CancellationToken cancellationToken);
}
