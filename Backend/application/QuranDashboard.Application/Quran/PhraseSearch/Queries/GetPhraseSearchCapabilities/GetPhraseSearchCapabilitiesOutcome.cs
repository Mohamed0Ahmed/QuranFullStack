using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSearchCapabilities;

public abstract record GetPhraseSearchCapabilitiesOutcome
{
    private GetPhraseSearchCapabilitiesOutcome() { }

    public sealed record Success(PhraseSearchCapabilitiesResponse Response)
        : GetPhraseSearchCapabilitiesOutcome;

    public sealed record Unavailable : GetPhraseSearchCapabilitiesOutcome;
}
