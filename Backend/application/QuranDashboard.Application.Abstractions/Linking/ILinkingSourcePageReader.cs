using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking;

public interface ILinkingSourcePageReader
{
    Task<LinkingResolvedSourcePageDto> ResolvePageAsync(
        LinkingSourceDescriptor descriptor,
        long linkingDataRevision,
        string sourceViewIdentity,
        LinkingSourcePageView view,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed record LinkingSourcePageView(
    LinkingSourcePageSegment Segment,
    LinkingInclusionMode? InclusionMode,
    IReadOnlyList<int> AyahOverrideIds,
    IReadOnlyList<string> TypeCodes);

public enum LinkingSourcePageSegment
{
    All = 1,
    Included = 2,
    Excluded = 3,
}
