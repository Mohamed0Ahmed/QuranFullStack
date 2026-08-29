using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSimilarityGroups;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSimilarityMatches;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/similarity-groups")]
public sealed class PhraseSearchSimilarityGroupsController(
    GetPhraseSimilarityGroupsHandler groupsHandler,
    GetPhraseSimilarityMatchesHandler matchesHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityGroupsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityGroupsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityGroupsResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseSimilarityGroupsResponse>>> GetGroups(
        [FromQuery] string? mode,
        [FromQuery(Name = "length")] int? wordCount,
        [FromQuery] int? threshold,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (HasBindingError("length"))
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityGroupsResponse>(
                PhraseRequestInvalidKind.Length));
        }

        if (HasBindingError(nameof(threshold)))
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityGroupsResponse>(
                PhraseRequestInvalidKind.Threshold));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityGroupsResponse>(
                PhraseRequestInvalidKind.Paging));
        }

        var outcome = await groupsHandler.HandleAsync(
            new GetPhraseSimilarityGroupsQuery(mode, wordCount, threshold, sort, page, pageSize),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseSimilarityGroupsResponse>.Success success =>
                Ok(ApiResponse<PhraseSimilarityGroupsResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.SimilarityGroupsLoaded)),
            PhraseReadOutcome<PhraseSimilarityGroupsResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityGroupsResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseSimilarityGroupsResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseSimilarityGroupsResponse>()),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseReadOutcome<PhraseSimilarityGroupsResponse>)} variant."),
        };
    }

    [HttpGet("{buildId}/{variantId}/matches")]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityMatchesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityMatchesResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityMatchesResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityMatchesResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseSimilarityMatchesResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseSimilarityMatchesResponse>>> GetMatches(
        [FromRoute] Guid buildId,
        [FromRoute] long variantId,
        [FromQuery] int? threshold,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (HasBindingError(nameof(buildId), nameof(variantId)))
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityMatchesResponse>(
                PhraseRequestInvalidKind.Reference));
        }

        if (HasBindingError(nameof(threshold)))
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityMatchesResponse>(
                PhraseRequestInvalidKind.Threshold));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityMatchesResponse>(
                PhraseRequestInvalidKind.Paging));
        }

        var outcome = await matchesHandler.HandleAsync(
            new GetPhraseSimilarityMatchesQuery(
                buildId,
                variantId,
                threshold,
                page,
                pageSize),
            cancellationToken);
        return outcome switch
        {
            PhraseReadOutcome<PhraseSimilarityMatchesResponse>.Success success =>
                Ok(ApiResponse<PhraseSimilarityMatchesResponse>.Ok(
                    success.Response,
                    PhraseSearchApiMessages.SimilarityMatchesLoaded)),
            PhraseReadOutcome<PhraseSimilarityMatchesResponse>.Invalid invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseSimilarityMatchesResponse>(invalid.Kind)),
            PhraseReadOutcome<PhraseSimilarityMatchesResponse>.NotFound =>
                NotFound(PhraseSearchApiFailure.SimilarityGroupNotFound<PhraseSimilarityMatchesResponse>()),
            PhraseReadOutcome<PhraseSimilarityMatchesResponse>.BuildChanged =>
                Conflict(PhraseSearchApiFailure.BuildChanged<PhraseSimilarityMatchesResponse>()),
            PhraseReadOutcome<PhraseSimilarityMatchesResponse>.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    PhraseSearchApiFailure.Unavailable<PhraseSimilarityMatchesResponse>()),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(PhraseReadOutcome<PhraseSimilarityMatchesResponse>)} variant."),
        };
    }

    private bool HasBindingError(params string[] keys) => keys.Any(key =>
        ModelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0);
}
