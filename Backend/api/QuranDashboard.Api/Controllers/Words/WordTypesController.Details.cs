using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;

namespace QuranDashboard.Api.Controllers.Words;

// Same partial class as WordTypesController.cs by design: a second controller class would retag
// these endpoints in the generated OpenAPI document.
public sealed partial class WordTypesController
{
    [HttpGet("words/{tashkeelWordId:int}")]
    public async Task<ActionResult<ApiResponse<WordTypeSummaryDto>>> GetSummary(
        int tashkeelWordId,
        [FromQuery] string? contextCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        CancellationToken cancellationToken)
    {
        var outcome = await summaryHandler.HandleAsync(
            new GetWordTypeSummaryQuery(tashkeelWordId, contextCode, caseFilter, tense, voice),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeSummaryOutcome.Success success =>
                Ok(ApiResponse<WordTypeSummaryDto>.Ok(success.Summary, ApiMessages.WordTypeSummaryLoaded)),
            GetWordTypeSummaryOutcome.InvalidIdentity =>
                BadRequest(ApiResponse<WordTypeSummaryDto>.Fail(ApiMessages.WordTypesInvalidIdentity)),
            GetWordTypeSummaryOutcome.NotFound =>
                NotFound(ApiResponse<WordTypeSummaryDto>.Fail(ApiMessages.WordTypeNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeSummaryOutcome)} variant."),
        };
    }

    [HttpGet("words/{tashkeelWordId:int}/ayahs")]
    public async Task<ActionResult<ApiResponse<PagedResult<WordTypeAyahMatchDto>>>> GetAyahs(
        int tashkeelWordId,
        [FromQuery] string? contextCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await ayahsHandler.HandleAsync(
            new GetWordTypeAyahsQuery(tashkeelWordId, contextCode, caseFilter, tense, voice, page ?? DefaultPage, pageSize ?? DefaultDetailPageSize),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeAyahsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Ok(success.Page, ApiMessages.WordTypeAyahsLoaded)),
            GetWordTypeAyahsOutcome.InvalidIdentity =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidIdentity)),
            GetWordTypeAyahsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            GetWordTypeAyahsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypeNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeAyahsOutcome)} variant."),
        };
    }

    [HttpGet("words/{tashkeelWordId:int}/surahs")]
    public async Task<ActionResult<ApiResponse<WordTypeSurahsResponse>>> GetSurahs(
        int tashkeelWordId,
        [FromQuery] string? contextCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        CancellationToken cancellationToken)
    {
        var outcome = await surahsHandler.HandleAsync(
            new GetWordTypeSurahsQuery(tashkeelWordId, contextCode, caseFilter, tense, voice),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeSurahsOutcome.Success success =>
                Ok(ApiResponse<WordTypeSurahsResponse>.Ok(success.Surahs, ApiMessages.WordTypeSurahsLoaded)),
            GetWordTypeSurahsOutcome.InvalidIdentity =>
                BadRequest(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesInvalidIdentity)),
            GetWordTypeSurahsOutcome.NotFound =>
                NotFound(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypeNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeSurahsOutcome)} variant."),
        };
    }
}
