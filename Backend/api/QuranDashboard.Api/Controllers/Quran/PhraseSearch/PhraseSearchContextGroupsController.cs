using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextGroups;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/contexts/groups")]
public sealed class PhraseSearchContextGroupsController(
    GetPhraseContextGroupsHandler handler) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextGroupsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextGroupsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextGroupsResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextGroupsResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseContextGroupsResponse>>> Get(
        [FromQuery] string? resolutionRef,
        [FromQuery] string? previousRef,
        [FromQuery] string? followingRef,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextGroupsResponse>(
                PhraseRequestInvalidKind.Paging));
        }

        var outcome = await handler.HandleAsync(
            new GetPhraseContextGroupsQuery(resolutionRef, previousRef, followingRef, cursor, pageSize),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseContextGroupsResponse>.Success success =>
                Ok(ApiResponse<PhraseContextGroupsResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.ContextGroupsLoaded)),
            PhraseReadOutcome<PhraseContextGroupsResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextGroupsResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseContextGroupsResponse>.BuildChanged =>
                Conflict(PhraseSearchApiFailure.BuildChanged<PhraseContextGroupsResponse>()),
            PhraseReadOutcome<PhraseContextGroupsResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseContextGroupsResponse>()),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseReadOutcome<PhraseContextGroupsResponse>)} variant."),
        };
    }
}
