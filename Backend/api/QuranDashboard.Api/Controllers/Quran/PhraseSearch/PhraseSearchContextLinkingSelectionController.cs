using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseContextLinkingSelection;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/contexts/linking-selection")]
public sealed class PhraseSearchContextLinkingSelectionController(
    ResolvePhraseContextLinkingSelectionHandler handler) : ControllerBase
{
    [HttpPost]
    [RequireOwner]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextLinkingSelectionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextLinkingSelectionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextLinkingSelectionResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseContextLinkingSelectionResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseContextLinkingSelectionResponse>>> Resolve(
        [FromBody] PhraseSearchContextLinkingSelectionBody? body,
        CancellationToken cancellationToken)
    {
        if (!PhraseSearchContextLinkingSelectionBodyMapper.TryMap(body, out var query))
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextLinkingSelectionResponse>(
                PhraseRequestInvalidKind.Selection));
        }

        var outcome = await handler.HandleAsync(query, cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.Success success =>
                Ok(ApiResponse<PhraseContextLinkingSelectionResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.ContextLinkingSelectionResolved)),
            PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseContextLinkingSelectionResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.BuildChanged =>
                Conflict(PhraseSearchApiFailure.BuildChanged<PhraseContextLinkingSelectionResponse>()),
            PhraseReadOutcome<PhraseContextLinkingSelectionResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseContextLinkingSelectionResponse>()),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseReadOutcome<PhraseContextLinkingSelectionResponse>)} variant."),
        };
    }
}
