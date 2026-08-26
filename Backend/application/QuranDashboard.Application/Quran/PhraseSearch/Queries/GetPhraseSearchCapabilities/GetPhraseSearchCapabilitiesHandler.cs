using QuranDashboard.Application.Abstractions.Quran.PhraseSearch;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;

namespace QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSearchCapabilities;

public sealed class GetPhraseSearchCapabilitiesHandler(IPhraseRepetitionsReader reader)
{
    public async Task<GetPhraseSearchCapabilitiesOutcome> HandleAsync(
        CancellationToken cancellationToken)
    {
        var result = await reader.GetCapabilitiesAsync(cancellationToken);

        return result switch
        {
            PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>.Success success =>
                new GetPhraseSearchCapabilitiesOutcome.Success(success.Value),
            PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>.Unavailable =>
                new GetPhraseSearchCapabilitiesOutcome.Unavailable(),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseSearchReadResult<PhraseSearchCapabilitiesResponse>)} variant."),
        };
    }
}
