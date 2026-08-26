using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseRepetitionOccurrences;

public abstract record GetPhraseRepetitionOccurrencesOutcome
{
    private GetPhraseRepetitionOccurrencesOutcome() { }

    public sealed record Success(PhraseOccurrencePageResponse Response)
        : GetPhraseRepetitionOccurrencesOutcome;

    public sealed record InvalidReference : GetPhraseRepetitionOccurrencesOutcome;
    public sealed record InvalidPaging : GetPhraseRepetitionOccurrencesOutcome;
    public sealed record Unavailable : GetPhraseRepetitionOccurrencesOutcome;
    public sealed record BuildChanged : GetPhraseRepetitionOccurrencesOutcome;
    public sealed record NotFound : GetPhraseRepetitionOccurrencesOutcome;
}
