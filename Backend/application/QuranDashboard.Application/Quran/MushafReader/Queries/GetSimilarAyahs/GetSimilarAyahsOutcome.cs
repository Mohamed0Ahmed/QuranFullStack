using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetSimilarAyahs;

public abstract record GetSimilarAyahsOutcome
{
    private GetSimilarAyahsOutcome() { }

    public sealed record Success(SimilarAyahsResponse Response) : GetSimilarAyahsOutcome;

    public sealed record InvalidVerseKey : GetSimilarAyahsOutcome;

    public sealed record NotFound : GetSimilarAyahsOutcome;
}
