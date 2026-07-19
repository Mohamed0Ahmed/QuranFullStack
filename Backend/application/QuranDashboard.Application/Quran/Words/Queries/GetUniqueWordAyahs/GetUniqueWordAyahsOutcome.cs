using QuranDashboard.Application.Abstractions.Quran.Words.Responses;

namespace QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordAyahs;

public abstract record GetUniqueWordAyahsOutcome
{
    private GetUniqueWordAyahsOutcome() { }

    public sealed record Success(PagedResult<UniqueWordAyahMatchDto> Page) : GetUniqueWordAyahsOutcome;
    public sealed record InvalidKind : GetUniqueWordAyahsOutcome;
    public sealed record InvalidId : GetUniqueWordAyahsOutcome;
    public sealed record InvalidPaging : GetUniqueWordAyahsOutcome;
    public sealed record NotFound : GetUniqueWordAyahsOutcome;
}
