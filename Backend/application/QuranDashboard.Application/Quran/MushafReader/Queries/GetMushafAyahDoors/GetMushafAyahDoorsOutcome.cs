using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafAyahDoors;

public abstract record GetMushafAyahDoorsOutcome
{
    private GetMushafAyahDoorsOutcome() { }

    public sealed record Success(MushafAyahDoorsResponse Response) : GetMushafAyahDoorsOutcome;

    public sealed record InvalidVerseKey : GetMushafAyahDoorsOutcome;

    public sealed record NotFound : GetMushafAyahDoorsOutcome;
}
