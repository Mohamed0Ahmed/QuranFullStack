using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordMissingSurahs;

public abstract record GetUniqueWordMissingSurahsOutcome
{
    private GetUniqueWordMissingSurahsOutcome() { }

    public sealed record Success(UniqueWordMissingSurahsResponse Response) : GetUniqueWordMissingSurahsOutcome;
    public sealed record InvalidKind : GetUniqueWordMissingSurahsOutcome;
    public sealed record InvalidId : GetUniqueWordMissingSurahsOutcome;
    public sealed record NotFound : GetUniqueWordMissingSurahsOutcome;
}
