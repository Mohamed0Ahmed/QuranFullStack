using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes;

namespace QuranDashboard.Api.Controllers.Words;

[ApiController]
[Route("api/words/word-types")]
public sealed partial class WordTypesController(
    WordTypesCatalogueExplorer catalogueExplorer,
    WordTypeWordExplorer wordExplorer) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 1000;
    private const int DefaultDetailPageSize = 100;

    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<WordTypeTreeDto>>> GetTree(CancellationToken cancellationToken)
    {
        var tree = await catalogueExplorer.GetTreeAsync(cancellationToken);
        return Ok(ApiResponse<WordTypeTreeDto>.Ok(tree, ApiMessages.WordTypesTreeLoaded));
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
        var outcome = await catalogueExplorer.GetRowsAsync(
            type, childCode, caseFilter, tense, voice, search, sort,
            page ?? DefaultPage, pageSize ?? DefaultListPageSize, hasRoot, hasStem, hasLemma, cancellationToken);

        return outcome switch
        {
            WordTypesCatalogueResult.Rows.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeRowDto>>.Ok(success.Page, ApiMessages.WordTypesRowsLoaded)),
            WordTypesCatalogueResult.Rows.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<WordTypeRowDto>>.Fail(ApiMessages.WordTypesInvalidFilter)),
            WordTypesCatalogueResult.Rows.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<WordTypeRowDto>>.Fail(ApiMessages.WordTypesInvalidSort)),
            WordTypesCatalogueResult.Rows.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeRowDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypesCatalogueResult.Rows)} variant."),
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
        var outcome = await catalogueExplorer.GetTableAsync(
            tableView, type, childCode, caseFilter, tense, voice, search, sort,
            page ?? DefaultPage, pageSize ?? DefaultListPageSize, hasRoot, hasStem, hasLemma, cancellationToken);

        return outcome switch
        {
            WordTypesCatalogueResult.Table.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeTableRowDto>>.Ok(success.Page, ApiMessages.WordTypesTableLoaded)),
            WordTypesCatalogueResult.Table.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<WordTypeTableRowDto>>.Fail(ApiMessages.WordTypesInvalidFilter)),
            WordTypesCatalogueResult.Table.InvalidTableView =>
                BadRequest(ApiResponse<PagedResult<WordTypeTableRowDto>>.Fail(ApiMessages.WordTypesInvalidTableView)),
            WordTypesCatalogueResult.Table.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<WordTypeTableRowDto>>.Fail(ApiMessages.WordTypesInvalidSort)),
            WordTypesCatalogueResult.Table.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeTableRowDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypesCatalogueResult.Table)} variant."),
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
        var outcome = await catalogueExplorer.GetScopeCountsAsync(
            type, childCode, caseFilter, tense, voice, search, hasRoot, hasStem, hasLemma, cancellationToken);

        return outcome switch
        {
            WordTypesCatalogueResult.ScopeCounts.Success success =>
                Ok(ApiResponse<WordTypeScopeCountsDto>.Ok(success.Counts, ApiMessages.WordTypesScopeCountsLoaded)),
            WordTypesCatalogueResult.ScopeCounts.InvalidFilter =>
                BadRequest(ApiResponse<WordTypeScopeCountsDto>.Fail(ApiMessages.WordTypesInvalidFilter)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(WordTypesCatalogueResult.ScopeCounts)} variant."),
        };
    }

}
