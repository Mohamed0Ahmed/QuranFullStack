using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseQuery;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[ApiController]
[Route("api/quran/phrase-search/query-resolutions")]
public sealed class PhraseSearchQueryResolutionsController(
    ResolvePhraseQueryHandler handler) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseQueryResolutionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseQueryResolutionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseQueryResolutionResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseQueryResolutionResponse>>> Get(
        [FromQuery] string? mode,
        [FromQuery] string? q64,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(
            new ResolvePhraseQueryQuery(mode, q64),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseQueryResolutionResponse>.Success success =>
                Ok(ApiResponse<PhraseQueryResolutionResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.QueryResolved)),
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
