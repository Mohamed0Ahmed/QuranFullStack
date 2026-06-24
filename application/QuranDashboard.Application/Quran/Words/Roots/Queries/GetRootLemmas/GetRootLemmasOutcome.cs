using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootLemmas;

public abstract record GetRootLemmasOutcome
{
    private GetRootLemmasOutcome() { }

    public sealed record Success(RootLemmasResponse Lemmas) : GetRootLemmasOutcome;
    public sealed record InvalidId : GetRootLemmasOutcome;
    public sealed record NotFound : GetRootLemmasOutcome;
}
