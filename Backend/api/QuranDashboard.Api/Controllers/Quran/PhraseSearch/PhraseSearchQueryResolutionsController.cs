using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSearchCapabilities;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseQuery;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[ApiController]
[Route("api/quran/phrase-search/query-resolutions")]
public sealed class PhraseSearchQueryResolutionsController(
    ResolvePhraseQueryHandler handler,
    GetPhraseSearchCapabilitiesHandler capabilitiesHandler) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseQueryResolutionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ApiResponse<PhraseQueryResolutionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseQueryResolutionResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseQueryResolutionResponse>>> Get(
        [FromQuery] string? mode,
        [FromQuery] string? q64,
        CancellationToken cancellationToken)
    {
        if (await PhraseSearchConditionalGet.MatchesCurrentBuildAsync(
            capabilitiesHandler,
            Request,
            Response,
            cancellationToken))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var outcome = await handler.HandleAsync(
            new ResolvePhraseQueryQuery(mode, q64),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseQueryResolutionResponse>.Success success =>
                PhraseSearchConditionalGet.OkWithValidator(
                    this,
                    Request,
                    Response,
                    ApiResponse<PhraseQueryResolutionResponse>.Ok(
                        success.Response,
                        PhraseSearchApiMessages.QueryResolved),
                    success.Response.ActiveBuildId),
            PhraseReadOutcome<PhraseQueryResolutionResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseQueryResolutionResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseQueryResolutionResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseQueryResolutionResponse>()),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseReadOutcome<PhraseQueryResolutionResponse>)} variant."),
        };
    }
}
