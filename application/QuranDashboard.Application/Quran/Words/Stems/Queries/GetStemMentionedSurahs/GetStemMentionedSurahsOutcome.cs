using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;

namespace QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMentionedSurahs;

public abstract record GetStemMentionedSurahsOutcome
{
    private GetStemMentionedSurahsOutcome() { }

    public sealed record Success(StemSurahsResponse Surahs) : GetStemMentionedSurahsOutcome;
    public sealed record InvalidId : GetStemMentionedSurahsOutcome;
    public sealed record NotFound : GetStemMentionedSurahsOutcome;
}
