using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeScopeCounts;

public abstract record GetWordTypeScopeCountsOutcome
{
    private GetWordTypeScopeCountsOutcome() { }

    // A zero-row scope is still Success (all-zero DTO); only an invalid type/child/feature/flag/search
    // maps to InvalidFilter (400).
    public sealed record Success(WordTypeScopeCountsDto Counts) : GetWordTypeScopeCountsOutcome;
    public sealed record InvalidFilter : GetWordTypeScopeCountsOutcome;
}
