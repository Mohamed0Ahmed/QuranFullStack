using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextResults;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSearchCapabilities;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/contexts/results")]
public sealed class PhraseSearchContextResultsController(
    GetPhraseContextResultsHandler handler,
    GetPhraseSearchCapabilitiesHandler capabilitiesHandler) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextResultsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextResultsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextResultsResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextResultsResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseContextResultsResponse>>> Get(
        [FromQuery] string? resolutionRef,
        [FromQuery] string? previousRef,
        [FromQuery] string? followingRef,
        [FromQuery] string? previousAlternativesRef,
        [FromQuery] string? followingAlternativesRef,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextResultsResponse>(
                PhraseRequestInvalidKind.Paging));
        }

        if (await PhraseSearchConditionalGet.MatchesCurrentBuildAsync(
            capabilitiesHandler,
            Request,
            Response,
            cancellationToken))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var outcome = await handler.HandleAsync(
            new GetPhraseContextResultsQuery(
                resolutionRef,
                previousRef,
                followingRef,
                previousAlternativesRef,
                followingAlternativesRef,
                page,
                pageSize),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseContextResultsResponse>.Success success =>
                PhraseSearchConditionalGet.OkWithValidator(
                    this,
                    Request,
                    Response,
                    ApiResponse<PhraseContextResultsResponse>.Ok(
                        success.Response,
                        PhraseSearchApiMessages.ContextResultsLoaded),
                    success.Response.ActiveBuildId),
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
