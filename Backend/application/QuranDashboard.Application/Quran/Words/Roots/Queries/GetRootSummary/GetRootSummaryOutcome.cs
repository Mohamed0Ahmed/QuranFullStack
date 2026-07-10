using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootSummary;

public abstract record GetRootSummaryOutcome
{
    private GetRootSummaryOutcome() { }

    public sealed record Success(RootSummaryDto Summary) : GetRootSummaryOutcome;
    public sealed record InvalidId : GetRootSummaryOutcome;
    public sealed record NotFound : GetRootSummaryOutcome;
}
