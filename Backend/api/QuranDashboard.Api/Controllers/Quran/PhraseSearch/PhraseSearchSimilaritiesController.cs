using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.SearchPhraseSimilarities;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/similarities/search")]
public sealed class PhraseSearchSimilaritiesController(
    SearchPhraseSimilaritiesHandler handler) : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(PhraseSearchComputePolicy.Name)]
    [RequestTimeout(PhraseSearchComputePolicy.Name)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilaritySearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilaritySearchResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilaritySearchResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilaritySearchResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseSimilaritySearchResponse>>> Get(
        [FromQuery] string? resolutionRef,
        [FromQuery] int? minimumMatchedWords,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (HasBindingError(nameof(minimumMatchedWords)))
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilaritySearchResponse>(
                PhraseRequestInvalidKind.MinimumMatchedWords));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilaritySearchResponse>(
                PhraseRequestInvalidKind.Paging));
        }

        var outcome = await handler.HandleAsync(
            new SearchPhraseSimilaritiesQuery(
                resolutionRef,
                minimumMatchedWords,
                sort,
                page,
                pageSize),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseSimilaritySearchResponse>.Success success =>
                Ok(ApiResponse<PhraseSimilaritySearchResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.SimilaritiesLoaded)),
            PhraseReadOutcome<PhraseSimilaritySearchResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilaritySearchResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseSimilaritySearchResponse>.BuildChanged =>
                Conflict(PhraseSearchApiFailure.BuildChanged<PhraseSimilaritySearchResponse>()),
            PhraseReadOutcome<PhraseSimilaritySearchResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseSimilaritySearchResponse>()),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseReadOutcome<PhraseSimilaritySearchResponse>)} variant."),
        };
    }

    private bool HasBindingError(string key) =>
        ModelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0;
}
