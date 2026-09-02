using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes;

namespace QuranDashboard.Api.Controllers.Words;

[ApiController]
[Route("api/words/word-types/table")]
public sealed class WordTypeGroupedDetailsController(
    WordTypeGroupedExplorer groupedExplorer) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultDetailPageSize = 100;

    [HttpGet("{kind}/{dimensionId:int}")]
    public async Task<ActionResult<ApiResponse<WordTypeGroupedSummaryDto>>> GetSummary(
        string kind,
        int dimensionId,
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        CancellationToken cancellationToken)
    {
        var outcome = await groupedExplorer.GetSummaryAsync(kind, dimensionId, type, childCode, caseFilter, tense, voice, cancellationToken);

        return outcome switch
        {
            WordTypeGroupedResult.Summary.Success success =>
                Ok(ApiResponse<WordTypeGroupedSummaryDto>.Ok(success.Value, ApiMessages.WordTypeGroupedSummaryLoaded)),
            WordTypeGroupedResult.Summary.InvalidKind =>
                BadRequest(ApiResponse<WordTypeGroupedSummaryDto>.Fail(ApiMessages.WordTypesInvalidGroupedKind)),
            WordTypeGroupedResult.Summary.InvalidId =>
                BadRequest(ApiResponse<WordTypeGroupedSummaryDto>.Fail(ApiMessages.WordTypesInvalidGroupedId)),
            WordTypeGroupedResult.Summary.InvalidFilter =>
                BadRequest(ApiResponse<WordTypeGroupedSummaryDto>.Fail(ApiMessages.WordTypesInvalidFilter)),
            WordTypeGroupedResult.Summary.NotFound =>
                NotFound(ApiResponse<WordTypeGroupedSummaryDto>.Fail(ApiMessages.WordTypesGroupedNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeGroupedResult.Summary)} variant."),
        };
    }

    [HttpGet("{kind}/{dimensionId:int}/words")]
    public async Task<ActionResult<ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>>> GetWords(
        string kind,
        int dimensionId,
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await groupedExplorer.GetWordsAsync(
            kind, dimensionId, type, childCode, caseFilter, tense, voice,
            page ?? DefaultPage, pageSize ?? DefaultDetailPageSize, cancellationToken);

        return outcome switch
        {
            WordTypeGroupedResult.Words.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Ok(success.Page, ApiMessages.WordTypeGroupedWordsLoaded)),
            WordTypeGroupedResult.Words.InvalidKind =>
                BadRequest(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesInvalidGroupedKind)),
            WordTypeGroupedResult.Words.InvalidId =>
                BadRequest(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesInvalidGroupedId)),
            WordTypeGroupedResult.Words.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesInvalidFilter)),
            WordTypeGroupedResult.Words.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            WordTypeGroupedResult.Words.NotFound =>
                NotFound(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesGroupedNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeGroupedResult.Words)} variant."),
        };
    }

    [HttpGet("{kind}/{dimensionId:int}/ayahs")]
    public async Task<ActionResult<ApiResponse<PagedResult<WordTypeAyahMatchDto>>>> GetAyahs(
        string kind,
        int dimensionId,
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await groupedExplorer.GetAyahsAsync(
            kind, dimensionId, type, childCode, caseFilter, tense, voice,
            page ?? DefaultPage, pageSize ?? DefaultDetailPageSize, cancellationToken);

        return outcome switch
        {
            WordTypeGroupedResult.Ayahs.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Ok(success.Page, ApiMessages.WordTypeGroupedAyahsLoaded)),
            WordTypeGroupedResult.Ayahs.InvalidKind =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidGroupedKind)),
            WordTypeGroupedResult.Ayahs.InvalidId =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidGroupedId)),
            WordTypeGroupedResult.Ayahs.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidFilter)),
            WordTypeGroupedResult.Ayahs.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            WordTypeGroupedResult.Ayahs.NotFound =>
                NotFound(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesGroupedNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeGroupedResult.Ayahs)} variant."),
        };
    }

    [HttpGet("{kind}/{dimensionId:int}/surahs")]
    public async Task<ActionResult<ApiResponse<WordTypeSurahsResponse>>> GetSurahs(
        string kind,
        int dimensionId,
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        CancellationToken cancellationToken)
    {
        var outcome = await groupedExplorer.GetSurahsAsync(kind, dimensionId, type, childCode, caseFilter, tense, voice, cancellationToken);

        return outcome switch
        {
            WordTypeGroupedResult.Surahs.Success success =>
                Ok(ApiResponse<WordTypeSurahsResponse>.Ok(success.Value, ApiMessages.WordTypeGroupedSurahsLoaded)),
            WordTypeGroupedResult.Surahs.InvalidKind =>
                BadRequest(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesInvalidGroupedKind)),
            WordTypeGroupedResult.Surahs.InvalidId =>
                BadRequest(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesInvalidGroupedId)),
            WordTypeGroupedResult.Surahs.InvalidFilter =>
                BadRequest(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesInvalidFilter)),
            WordTypeGroupedResult.Surahs.NotFound =>
                NotFound(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesGroupedNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypeGroupedResult.Surahs)} variant."),
        };
    }
}
