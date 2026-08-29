using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSearchCapabilities;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[ApiController]
[Route("api/quran/phrase-search/capabilities")]
public sealed class PhraseSearchCapabilitiesController(
    GetPhraseSearchCapabilitiesHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PhraseSearchCapabilitiesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSearchCapabilitiesResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseSearchCapabilitiesResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(cancellationToken);

        return outcome switch
        {
            GetPhraseSearchCapabilitiesOutcome.Success success =>
                Ok(ApiResponse<PhraseSearchCapabilitiesResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.CapabilitiesLoaded)),
            GetPhraseSearchCapabilitiesOutcome.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<PhraseSearchCapabilitiesResponse>.Fail(
                        PhraseSearchApiMessages.IndexUnavailable,
                        [PhraseSearchErrorCodes.IndexUnavailable])),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetPhraseSearchCapabilitiesOutcome)} variant."),
        };
    }
}
