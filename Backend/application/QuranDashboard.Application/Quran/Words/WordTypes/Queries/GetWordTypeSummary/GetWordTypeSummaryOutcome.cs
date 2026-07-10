using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;

public abstract record GetWordTypeSummaryOutcome
{
    private GetWordTypeSummaryOutcome() { }

    public sealed record Success(WordTypeSummaryDto Summary) : GetWordTypeSummaryOutcome;
    public sealed record InvalidIdentity : GetWordTypeSummaryOutcome;
    public sealed record NotFound : GetWordTypeSummaryOutcome;
}
