using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextResults;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/contexts/results")]
public sealed class PhraseSearchContextResultsController(
    GetPhraseContextResultsHandler handler) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextResultsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextResultsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextResultsResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextResultsResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseContextResultsResponse>>> Get(
        [FromQuery] string? resolutionRef,
        [FromQuery] string? previousRef,
        [FromQuery] string? followingRef,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextResultsResponse>(
                PhraseRequestInvalidKind.Paging));
        }

        var outcome = await handler.HandleAsync(
            new GetPhraseContextResultsQuery(resolutionRef, previousRef, followingRef, page, pageSize),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseContextResultsResponse>.Success success =>
                Ok(ApiResponse<PhraseContextResultsResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.ContextResultsLoaded)),
            PhraseReadOutcome<PhraseContextResultsResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextResultsResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseContextResultsResponse>.BuildChanged =>
                Conflict(PhraseSearchApiFailure.BuildChanged<PhraseContextResultsResponse>()),
            PhraseReadOutcome<PhraseContextResultsResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseContextResultsResponse>()),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseReadOutcome<PhraseContextResultsResponse>)} variant."),
        };
    }
}
