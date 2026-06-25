using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemSummary;

public abstract record GetStemSummaryOutcome
{
    private GetStemSummaryOutcome() { }

    public sealed record Success(StemSummaryDto Summary) : GetStemSummaryOutcome;
    public sealed record InvalidId : GetStemSummaryOutcome;
    public sealed record NotFound : GetStemSummaryOutcome;
}
