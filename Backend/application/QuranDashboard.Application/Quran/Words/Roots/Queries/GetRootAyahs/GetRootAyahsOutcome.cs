using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;

namespace QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootAyahs;

public abstract record GetRootAyahsOutcome
{
    private GetRootAyahsOutcome() { }

    public sealed record Success(PagedResult<RootAyahMatchDto> Page) : GetRootAyahsOutcome;
    public sealed record InvalidId : GetRootAyahsOutcome;
    public sealed record InvalidPaging : GetRootAyahsOutcome;
    public sealed record NotFound : GetRootAyahsOutcome;
}
