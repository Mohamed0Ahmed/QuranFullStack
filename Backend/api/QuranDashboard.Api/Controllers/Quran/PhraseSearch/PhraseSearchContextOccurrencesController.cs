using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextOccurrences;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/contexts/occurrences")]
public sealed class PhraseSearchContextOccurrencesController(
    GetPhraseContextOccurrencesHandler handler) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextOccurrencesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextOccurrencesResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextOccurrencesResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextOccurrencesResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseContextOccurrencesResponse>>> Get(
        [FromQuery] string? contextRef,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextOccurrencesResponse>(
                PhraseRequestInvalidKind.Paging));
        }

        var outcome = await handler.HandleAsync(
            new GetPhraseContextOccurrencesQuery(contextRef, cursor, pageSize),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseContextOccurrencesResponse>.Success success =>
                Ok(ApiResponse<PhraseContextOccurrencesResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.ContextOccurrencesLoaded)),
            PhraseReadOutcome<PhraseContextOccurrencesResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextOccurrencesResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseContextOccurrencesResponse>.BuildChanged =>
                Conflict(PhraseSearchApiFailure.BuildChanged<PhraseContextOccurrencesResponse>()),
            PhraseReadOutcome<PhraseContextOccurrencesResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseContextOccurrencesResponse>()),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseReadOutcome<PhraseContextOccurrencesResponse>)} variant."),
        };
    }
}
