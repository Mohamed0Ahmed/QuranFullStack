using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;

namespace QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahMutashabihat;

public abstract record GetAyahMutashabihatOutcome
{
    private GetAyahMutashabihatOutcome() { }

    public sealed record Success(AyahMutashabihatResponse Response) : GetAyahMutashabihatOutcome;

    public sealed record InvalidVerseKey : GetAyahMutashabihatOutcome;

    public sealed record NotFound : GetAyahMutashabihatOutcome;
}
