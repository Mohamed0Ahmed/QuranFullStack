using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemLemmas;

public abstract record GetStemLemmasOutcome
{
    private GetStemLemmasOutcome() { }

    public sealed record Success(StemLemmasResponse Lemmas) : GetStemLemmasOutcome;
    public sealed record InvalidId : GetStemLemmasOutcome;
    public sealed record NotFound : GetStemLemmasOutcome;
}
