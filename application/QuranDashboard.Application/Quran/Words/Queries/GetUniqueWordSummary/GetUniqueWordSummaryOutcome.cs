using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSummary;

/// <summary>
/// Discriminated outcome for selected unique-word summary.
/// </summary>
public abstract record GetUniqueWordSummaryOutcome
{
    private GetUniqueWordSummaryOutcome() { }

    public sealed record Success(UniqueWordSummaryDto Summary) : GetUniqueWordSummaryOutcome;
    public sealed record InvalidKind : GetUniqueWordSummaryOutcome;
    public sealed record InvalidId : GetUniqueWordSummaryOutcome;
    public sealed record NotFound : GetUniqueWordSummaryOutcome;
}
