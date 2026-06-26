using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;

namespace QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaStems;

public abstract record GetLemmaStemsOutcome
{
    private GetLemmaStemsOutcome() { }

    public sealed record Success(LemmaStemsResponse Stems) : GetLemmaStemsOutcome;
    public sealed record InvalidId : GetLemmaStemsOutcome;
    public sealed record NotFound : GetLemmaStemsOutcome;
}
