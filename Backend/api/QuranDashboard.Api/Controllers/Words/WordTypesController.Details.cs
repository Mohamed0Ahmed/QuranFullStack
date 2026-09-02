using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes;

namespace QuranDashboard.Api.Controllers.Words;

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
        var outcome = await wordExplorer.GetSummaryAsync(tashkeelWordId, contextCode, caseFilter, tense, voice, cancellationToken);

        return outcome switch
        {
            WordTypeWordResult.Summary.Success success =>
                Ok(ApiResponse<WordTypeSummaryDto>.Ok(success.Value, ApiMessages.WordTypeSummaryLoaded)),
            WordTypeWordResult.Summary.InvalidIdentity =>
                BadRequest(ApiResponse<WordTypeSummaryDto>.Fail(ApiMessages.WordTypesInvalidIdentity)),
            WordTypeWordResult.Summary.NotFound =>
                NotFound(ApiResponse<WordTypeSummaryDto>.Fail(ApiMessages.WordTypeNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeWordResult.Summary)} variant."),
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
        var outcome = await wordExplorer.GetAyahsAsync(
            tashkeelWordId, contextCode, caseFilter, tense, voice,
            page ?? DefaultPage, pageSize ?? DefaultDetailPageSize, cancellationToken);

        return outcome switch
        {
            WordTypeWordResult.Ayahs.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Ok(success.Page, ApiMessages.WordTypeAyahsLoaded)),
            WordTypeWordResult.Ayahs.InvalidIdentity =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidIdentity)),
            WordTypeWordResult.Ayahs.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            WordTypeWordResult.Ayahs.NotFound =>
                NotFound(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypeNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeWordResult.Ayahs)} variant."),
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
        var outcome = await wordExplorer.GetSurahsAsync(tashkeelWordId, contextCode, caseFilter, tense, voice, cancellationToken);

        return outcome switch
        {
            WordTypeWordResult.Surahs.Success success =>
                Ok(ApiResponse<WordTypeSurahsResponse>.Ok(success.Value, ApiMessages.WordTypeSurahsLoaded)),
            WordTypeWordResult.Surahs.InvalidIdentity =>
                BadRequest(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesInvalidIdentity)),
            WordTypeWordResult.Surahs.NotFound =>
                NotFound(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypeNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeWordResult.Surahs)} variant."),
        };
    }
}
