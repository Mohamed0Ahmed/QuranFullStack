using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeRows;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeScopeCounts;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTable;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeTree;

namespace QuranDashboard.Api.Controllers.Words;

[ApiController]
[Route("api/words/word-types")]
public sealed partial class WordTypesController(
    GetWordTypeTreeHandler treeHandler,
    GetWordTypeRowsHandler rowsHandler,
    GetWordTypeTableHandler tableHandler,
    GetWordTypeScopeCountsHandler scopeCountsHandler,
    GetWordTypeSummaryHandler summaryHandler,
    GetWordTypeAyahsHandler ayahsHandler,
    GetWordTypeSurahsHandler surahsHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 1000;
    private const int DefaultDetailPageSize = 100;

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
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool? hasRoot,
        [FromQuery] bool? hasStem,
        [FromQuery] bool? hasLemma,
        CancellationToken cancellationToken)
    {
        var outcome = await rowsHandler.HandleAsync(
            new GetWordTypeRowsQuery(type, childCode, caseFilter, tense, voice, search, sort, page ?? DefaultPage, pageSize ?? DefaultListPageSize, hasRoot, hasStem, hasLemma),
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

    [HttpGet("table")]
    public async Task<ActionResult<ApiResponse<PagedResult<WordTypeTableRowDto>>>> GetTable(
        [FromQuery] string? tableView,
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        [FromQuery] string? search,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool? hasRoot,
        [FromQuery] bool? hasStem,
        [FromQuery] bool? hasLemma,
        CancellationToken cancellationToken)
    {
        var outcome = await tableHandler.HandleAsync(
            new GetWordTypeTableQuery(type, childCode, caseFilter, tense, voice, search, tableView, sort, page ?? DefaultPage, pageSize ?? DefaultListPageSize, hasRoot, hasStem, hasLemma),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeTableOutcome.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeTableRowDto>>.Ok(success.Page, ApiMessages.WordTypesTableLoaded)),
            GetWordTypeTableOutcome.InvalidTableView =>
                BadRequest(ApiResponse<PagedResult<WordTypeTableRowDto>>.Fail(ApiMessages.WordTypesInvalidTableView)),
            GetWordTypeTableOutcome.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<WordTypeTableRowDto>>.Fail(ApiMessages.WordTypesInvalidFilter)),
            GetWordTypeTableOutcome.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<WordTypeTableRowDto>>.Fail(ApiMessages.WordTypesInvalidSort)),
            GetWordTypeTableOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeTableRowDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeTableOutcome)} variant."),
        };
    }

    [HttpGet("scope-counts")]
    public async Task<ActionResult<ApiResponse<WordTypeScopeCountsDto>>> GetScopeCounts(
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        [FromQuery] string? search,
        [FromQuery] bool? hasRoot,
        [FromQuery] bool? hasStem,
        [FromQuery] bool? hasLemma,
        CancellationToken cancellationToken)
    {
        var outcome = await scopeCountsHandler.HandleAsync(
            new GetWordTypeScopeCountsQuery(type, childCode, caseFilter, tense, voice, search, hasRoot, hasStem, hasLemma),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeScopeCountsOutcome.Success success =>
                Ok(ApiResponse<WordTypeScopeCountsDto>.Ok(success.Counts, ApiMessages.WordTypesScopeCountsLoaded)),
            GetWordTypeScopeCountsOutcome.InvalidFilter =>
                BadRequest(ApiResponse<WordTypeScopeCountsDto>.Fail(ApiMessages.WordTypesInvalidFilter)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeScopeCountsOutcome)} variant."),
        };
    }

}
