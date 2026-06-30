using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTree;

namespace QuranDashboard.Api.Controllers.Words;

[ApiController]
[Route("api/words/word-types")]
public sealed class WordTypesController(
    GetWordTypeTreeHandler treeHandler,
    GetWordTypeRowsHandler rowsHandler,
    GetWordTypeSummaryHandler summaryHandler,
    GetWordTypeAyahsHandler ayahsHandler,
    GetWordTypeSurahsHandler surahsHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 25;
    private const int DefaultDetailPageSize = 25;

    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<WordTypeTreeDto>>> GetTree(CancellationToken cancellationToken)
    {
        var outcome = await treeHandler.HandleAsync(new GetWordTypeTreeQuery(), cancellationToken);

        return outcome switch
        {
            GetWordTypeTreeOutcome.Success success =>
                Ok(ApiResponse<WordTypeTreeDto>.Ok(success.Tree, ApiMessages.WordTypesTreeLoaded)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeTreeOutcome)} variant."),
        };
    }

    [HttpGet("words")]
    public async Task<ActionResult<ApiResponse<PagedResult<WordTypeRowDto>>>> GetRows(
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await rowsHandler.HandleAsync(
            new GetWordTypeRowsQuery(type, childCode, caseFilter, tense, voice, sort, page ?? DefaultPage, pageSize ?? DefaultListPageSize),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeRowsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeRowDto>>.Ok(success.Page, ApiMessages.WordTypesRowsLoaded)),
            GetWordTypeRowsOutcome.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<WordTypeRowDto>>.Fail(ApiMessages.WordTypesInvalidFilter)),
            GetWordTypeRowsOutcome.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<WordTypeRowDto>>.Fail(ApiMessages.WordTypesInvalidSort)),
            GetWordTypeRowsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeRowDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeRowsOutcome)} variant."),
        };
    }

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
