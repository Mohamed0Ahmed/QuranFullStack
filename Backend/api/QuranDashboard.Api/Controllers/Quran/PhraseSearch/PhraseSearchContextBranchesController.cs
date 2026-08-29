using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseContextBranches;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSearchCapabilities;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/contexts/branches")]
public sealed class PhraseSearchContextBranchesController(
    GetPhraseContextBranchesHandler handler,
    GetPhraseSearchCapabilitiesHandler capabilitiesHandler) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextBranchesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextBranchesResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextBranchesResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextBranchesResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseContextBranchesResponse>>> Get(
        [FromQuery] string? resolutionRef,
        [FromQuery] string? previousRef,
        [FromQuery] string? followingRef,
        [FromQuery] string? previousAlternativesRef,
        [FromQuery] string? followingAlternativesRef,
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

        if (await PhraseSearchConditionalGet.MatchesCurrentBuildAsync(
            capabilitiesHandler,
            Request,
            Response,
            cancellationToken))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var outcome = await handler.HandleAsync(
            new GetPhraseContextBranchesQuery(
                resolutionRef,
                previousRef,
                followingRef,
                previousAlternativesRef,
                followingAlternativesRef,
                previousCursor,
                followingCursor,
                previousPageSize,
                followingPageSize),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseContextBranchesResponse>.Success success =>
                PhraseSearchConditionalGet.OkWithValidator(
                    this,
                    Request,
                    Response,
                    ApiResponse<PhraseContextBranchesResponse>.Ok(
                        success.Response,
                        PhraseSearchApiMessages.ContextBranchesLoaded),
                    success.Response.ActiveBuildId),
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
