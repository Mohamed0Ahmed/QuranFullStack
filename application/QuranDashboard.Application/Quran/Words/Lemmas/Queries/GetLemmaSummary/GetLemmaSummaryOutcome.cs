using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaSummary;

public abstract record GetLemmaSummaryOutcome
{
    private GetLemmaSummaryOutcome() { }

    public sealed record Success(LemmaSummaryDto Summary) : GetLemmaSummaryOutcome;
    public sealed record InvalidId : GetLemmaSummaryOutcome;
    public sealed record NotFound : GetLemmaSummaryOutcome;
}
