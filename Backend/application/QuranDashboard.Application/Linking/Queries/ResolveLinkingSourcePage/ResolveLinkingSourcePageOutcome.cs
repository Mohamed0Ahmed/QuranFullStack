using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Application.Linking.Queries.ResolveLinkingSourcePage;

public abstract record ResolveLinkingSourcePageOutcome
{
    private ResolveLinkingSourcePageOutcome() { }

    public sealed record Success(LinkingResolvedSourcePageDto Page) : ResolveLinkingSourcePageOutcome;
    public sealed record InvalidRequest(string Field) : ResolveLinkingSourcePageOutcome;
    public sealed record InvalidDescriptor(LinkingDescriptorViolation Violation) : ResolveLinkingSourcePageOutcome;
    public sealed record NotFound(string Reference) : ResolveLinkingSourcePageOutcome;
    public sealed record LinkingDataStale : ResolveLinkingSourcePageOutcome;
    public sealed record SourceViewStale : ResolveLinkingSourcePageOutcome;
    public sealed record TransientFailure : ResolveLinkingSourcePageOutcome;
}
