using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeScopeCounts;

public abstract record GetWordTypeScopeCountsOutcome
{
    private GetWordTypeScopeCountsOutcome() { }

    public sealed record Success(WordTypeScopeCountsDto Counts) : GetWordTypeScopeCountsOutcome;
    public sealed record InvalidFilter : GetWordTypeScopeCountsOutcome;
}
