using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootStems;

public abstract record GetRootStemsOutcome
{
    private GetRootStemsOutcome() { }

    public sealed record Success(RootStemsResponse Stems) : GetRootStemsOutcome;
    public sealed record InvalidId : GetRootStemsOutcome;
    public sealed record NotFound : GetRootStemsOutcome;
}
