using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextBranches;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/contexts/branches")]
public sealed class PhraseSearchContextBranchesController(
    GetPhraseContextBranchesHandler handler) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextBranchesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextBranchesResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextBranchesResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextBranchesResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseContextBranchesResponse>>> Get(
        [FromQuery] string? resolutionRef,
        [FromQuery] string? previousRef,
        [FromQuery] string? followingRef,
        [FromQuery] string? previousCursor,
        [FromQuery] string? followingCursor,
        [FromQuery] int? previousPageSize,
        [FromQuery] int? followingPageSize,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextBranchesResponse>(
                PhraseRequestInvalidKind.Paging));
        }

        var outcome = await handler.HandleAsync(
            new GetPhraseContextBranchesQuery(
                resolutionRef,
                previousRef,
                followingRef,
                previousCursor,
                followingCursor,
                previousPageSize,
                followingPageSize),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseContextBranchesResponse>.Success success =>
                Ok(ApiResponse<PhraseContextBranchesResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.ContextBranchesLoaded)),
            PhraseReadOutcome<PhraseContextBranchesResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextBranchesResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseContextBranchesResponse>.BuildChanged =>
                Conflict(PhraseSearchApiFailure.BuildChanged<PhraseContextBranchesResponse>()),
            PhraseReadOutcome<PhraseContextBranchesResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseContextBranchesResponse>()),
            _ => throw new InvalidOperationException($"Unhandled {nameof(PhraseReadOutcome<PhraseContextBranchesResponse>)} variant."),
        };
    }
}
