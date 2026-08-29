using QuranDashboard.Application.Abstractions.Quran.PhraseSearch.Responses;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseRepetitionOccurrences;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseRepetitions;
using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSearchCapabilities;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

[Route("api/quran/phrase-search/repetitions")]
public sealed class PhraseSearchRepetitionsController(
    GetPhraseRepetitionsHandler repetitionsHandler,
    GetPhraseRepetitionOccurrencesHandler occurrencesHandler,
    GetPhraseSearchCapabilitiesHandler capabilitiesHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PhraseRepetitionsPageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ApiResponse<PhraseRepetitionsPageResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseRepetitionsPageResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseRepetitionsPageResponse>>> GetRepetitions(
        [FromQuery] string? mode,
        [FromQuery(Name = "length")] int? wordCount,
        [FromQuery] string? q64,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (HasBindingError("length", nameof(wordCount)))
        {
            return BadRequest(Failure<PhraseRepetitionsPageResponse>(
                PhraseSearchApiMessages.InvalidLength,
                PhraseSearchErrorCodes.InvalidLength));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(Failure<PhraseRepetitionsPageResponse>(
                PhraseSearchApiMessages.InvalidPaging,
                PhraseSearchErrorCodes.InvalidPaging));
        }

        if (await PhraseSearchConditionalGet.MatchesCurrentBuildAsync(
            capabilitiesHandler,
            Request,
            Response,
            cancellationToken))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var outcome = await repetitionsHandler.HandleAsync(
            new GetPhraseRepetitionsQuery(mode, wordCount, q64, sort, page, pageSize),
            cancellationToken);

        return outcome switch
        {
            GetPhraseRepetitionsOutcome.Success success =>
                PhraseSearchConditionalGet.OkWithValidator(
                    this,
                    Request,
                    Response,
                    ApiResponse<PhraseRepetitionsPageResponse>.Ok(
                        success.Response,
                        PhraseSearchApiMessages.RepetitionsLoaded),
                    success.Response.ActiveBuildId),
            GetPhraseRepetitionsOutcome.InvalidMode =>
                BadRequest(Failure<PhraseRepetitionsPageResponse>(
                    PhraseSearchApiMessages.InvalidMode,
                    PhraseSearchErrorCodes.InvalidMode)),
            GetPhraseRepetitionsOutcome.InvalidLength =>
                BadRequest(Failure<PhraseRepetitionsPageResponse>(
                    PhraseSearchApiMessages.InvalidLength,
                    PhraseSearchErrorCodes.InvalidLength)),
            GetPhraseRepetitionsOutcome.InvalidQuery invalid =>
                BadRequest(PhraseSearchApiFailure.Invalid<PhraseRepetitionsPageResponse>(invalid.Kind)),
            GetPhraseRepetitionsOutcome.InvalidSort =>
                BadRequest(Failure<PhraseRepetitionsPageResponse>(
                    PhraseSearchApiMessages.InvalidSort,
                    PhraseSearchErrorCodes.InvalidSort)),
            GetPhraseRepetitionsOutcome.InvalidPaging =>
                BadRequest(Failure<PhraseRepetitionsPageResponse>(
                    PhraseSearchApiMessages.InvalidPaging,
                    PhraseSearchErrorCodes.InvalidPaging)),
            GetPhraseRepetitionsOutcome.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    Failure<PhraseRepetitionsPageResponse>(
                        PhraseSearchApiMessages.IndexUnavailable,
                        PhraseSearchErrorCodes.IndexUnavailable)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetPhraseRepetitionsOutcome)} variant."),
        };
    }

    [HttpGet("{buildId}/{variantId}/occurrences")]
    [ProducesResponseType(typeof(ApiResponse<PhraseOccurrencePageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(ApiResponse<PhraseOccurrencePageResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PhraseOccurrencePageResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<PhraseOccurrencePageResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<PhraseOccurrencePageResponse>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<PhraseOccurrencePageResponse>>> GetOccurrences(
        [FromRoute] Guid buildId,
        [FromRoute] long variantId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (HasBindingError(nameof(buildId), nameof(variantId)))
        {
            return BadRequest(Failure<PhraseOccurrencePageResponse>(
                PhraseSearchApiMessages.InvalidReference,
                PhraseSearchErrorCodes.InvalidReference));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(Failure<PhraseOccurrencePageResponse>(
                PhraseSearchApiMessages.InvalidPaging,
                PhraseSearchErrorCodes.InvalidPaging));
        }

        if (await PhraseSearchConditionalGet.MatchesCurrentBuildAsync(
            capabilitiesHandler,
            Request,
            Response,
            cancellationToken))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var outcome = await occurrencesHandler.HandleAsync(
            new GetPhraseRepetitionOccurrencesQuery(buildId, variantId, page, pageSize),
            cancellationToken);

        return outcome switch
        {
            GetPhraseRepetitionOccurrencesOutcome.Success success =>
                PhraseSearchConditionalGet.OkWithValidator(
                    this,
                    Request,
                    Response,
                    ApiResponse<PhraseOccurrencePageResponse>.Ok(
                        success.Response,
                        PhraseSearchApiMessages.OccurrencesLoaded),
                    success.Response.ActiveBuildId),
            GetPhraseRepetitionOccurrencesOutcome.InvalidReference =>
                BadRequest(Failure<PhraseOccurrencePageResponse>(
                    PhraseSearchApiMessages.InvalidReference,
                    PhraseSearchErrorCodes.InvalidReference)),
            GetPhraseRepetitionOccurrencesOutcome.InvalidPaging =>
                BadRequest(Failure<PhraseOccurrencePageResponse>(
                    PhraseSearchApiMessages.InvalidPaging,
                    PhraseSearchErrorCodes.InvalidPaging)),
            GetPhraseRepetitionOccurrencesOutcome.BuildChanged =>
                Conflict(Failure<PhraseOccurrencePageResponse>(
                    PhraseSearchApiMessages.IndexChanged,
                    PhraseSearchErrorCodes.IndexChanged)),
            GetPhraseRepetitionOccurrencesOutcome.NotFound =>
                NotFound(Failure<PhraseOccurrencePageResponse>(
                    PhraseSearchApiMessages.VariantNotFound,
                    PhraseSearchErrorCodes.VariantNotFound)),
            GetPhraseRepetitionOccurrencesOutcome.Unavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    Failure<PhraseOccurrencePageResponse>(
                        PhraseSearchApiMessages.IndexUnavailable,
                        PhraseSearchErrorCodes.IndexUnavailable)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetPhraseRepetitionOccurrencesOutcome)} variant."),
        };
    }

    private static ApiResponse<T> Failure<T>(string message, string errorCode) =>
        ApiResponse<T>.Fail(message, [errorCode]);

    private bool HasBindingError(params string[] keys) => keys.Any(key =>
        ModelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0);
}
