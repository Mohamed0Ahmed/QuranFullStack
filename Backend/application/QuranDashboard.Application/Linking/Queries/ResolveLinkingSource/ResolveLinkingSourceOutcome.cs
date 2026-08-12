using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Application.Linking.Queries.ResolveLinkingSource;

public abstract record ResolveLinkingSourceOutcome
{
    private ResolveLinkingSourceOutcome() { }

    public sealed record Success(LinkingResolvedSourceDto Source) : ResolveLinkingSourceOutcome;

    public sealed record InvalidDescriptor(LinkingDescriptorViolation Violation) : ResolveLinkingSourceOutcome;

    public sealed record NotFound(string Reference) : ResolveLinkingSourceOutcome;
}
