using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseRepetitions;

public abstract record GetPhraseRepetitionsOutcome
{
    private GetPhraseRepetitionsOutcome() { }

    public sealed record Success(PhraseRepetitionsPageResponse Response) : GetPhraseRepetitionsOutcome;
    public sealed record InvalidMode : GetPhraseRepetitionsOutcome;
    public sealed record InvalidLength : GetPhraseRepetitionsOutcome;
    public sealed record InvalidQuery(PhraseRequestInvalidKind Kind) : GetPhraseRepetitionsOutcome;
    public sealed record InvalidSort : GetPhraseRepetitionsOutcome;
    public sealed record InvalidPaging : GetPhraseRepetitionsOutcome;
    public sealed record Unavailable : GetPhraseRepetitionsOutcome;
}
