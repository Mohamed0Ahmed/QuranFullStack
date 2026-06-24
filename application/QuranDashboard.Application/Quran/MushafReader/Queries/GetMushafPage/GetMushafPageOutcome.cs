using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafPage;

public abstract record GetMushafPageOutcome
{
    private GetMushafPageOutcome() { }

    public sealed record Success(MushafPageResponse Response) : GetMushafPageOutcome;

    public sealed record InvalidPageNumber : GetMushafPageOutcome;

    public sealed record NotFound : GetMushafPageOutcome;
}
