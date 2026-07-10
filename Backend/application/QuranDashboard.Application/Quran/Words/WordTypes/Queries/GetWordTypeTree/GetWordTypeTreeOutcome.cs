using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;

namespace QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTree;

public abstract record GetWordTypeTreeOutcome
{
    private GetWordTypeTreeOutcome() { }

    public sealed record Success(WordTypeTreeDto Tree) : GetWordTypeTreeOutcome;
}
