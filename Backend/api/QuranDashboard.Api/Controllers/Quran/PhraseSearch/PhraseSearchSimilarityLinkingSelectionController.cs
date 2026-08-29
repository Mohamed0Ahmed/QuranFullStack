using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.ResolvePhraseSimilarityLinkingSelection;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/similarities/linking-selection")]
public sealed class PhraseSearchSimilarityLinkingSelectionController(
    ResolvePhraseSimilarityLinkingSelectionHandler handler) : ControllerBase
{
    [HttpPost]
    [RequireOwner]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityLinkingSelectionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityLinkingSelectionResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityLinkingSelectionResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityLinkingSelectionResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseSimilarityLinkingSelectionResponse>>> Resolve(
        [FromBody] PhraseSearchSimilarityLinkingSelectionBody? body,
        CancellationToken cancellationToken)
    {
        if (!PhraseSearchSimilarityLinkingSelectionBodyMapper.TryMap(body, out var query))
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityLinkingSelectionResponse>(
                PhraseRequestInvalidKind.Selection));
        }

        var outcome = await handler.HandleAsync(query, cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Success success =>
                Ok(ApiResponse<PhraseSimilarityLinkingSelectionResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.SimilarityLinkingSelectionResolved)),
            PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityLinkingSelectionResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.BuildChanged =>
                Conflict(PhraseSearchApiFailure.BuildChanged<PhraseSimilarityLinkingSelectionResponse>()),
            PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseSimilarityLinkingSelectionResponse>()),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseReadOutcome<PhraseSimilarityLinkingSelectionResponse>)} variant."),
        };
    }
}
