using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSummary;

public abstract record GetWordTypeGroupedSummaryOutcome
{
    private GetWordTypeGroupedSummaryOutcome() { }

    public sealed record Success(WordTypeGroupedSummaryDto Summary) : GetWordTypeGroupedSummaryOutcome;
    public sealed record InvalidKind : GetWordTypeGroupedSummaryOutcome;
    public sealed record InvalidId : GetWordTypeGroupedSummaryOutcome;
    public sealed record InvalidFilter : GetWordTypeGroupedSummaryOutcome;
    public sealed record NotFound : GetWordTypeGroupedSummaryOutcome;
}
