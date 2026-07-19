using QuranDashboard.Application.Abstractions.Quran.Words;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordAyahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordMissingSurahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSummary;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordSurahs;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Api.Controllers.Words;

[ApiController]
[Route("api/words/unique")]
public sealed class UniqueWordsController(
    GetUniqueWordsPageHandler listHandler,
    GetUniqueWordSummaryHandler summaryHandler,
    GetUniqueWordSurahsHandler surahsHandler,
    GetUniqueWordMissingSurahsHandler missingSurahsHandler,
    GetUniqueWordAyahsHandler ayahsHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 1000;
    private const int DefaultAyahPageSize = 100;

    [HttpGet("{kind}")]
    public async Task<ActionResult<ApiResponse<PagedResult<UniqueWordListItemDto>>>> Get(
        string kind,
        [FromQuery] string? search,
        [FromQuery(Name = "sort")] string? paramSort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] int? occMin,
        [FromQuery] int? occMax,
        [FromQuery] int? ayahsMin,
        [FromQuery] int? ayahsMax,
        [FromQuery] int? surahsMin,
        [FromQuery] int? surahsMax,
        [FromQuery] string? primaryType,
        [FromQuery] int? rootId,
        CancellationToken cancellationToken)
    {
        var outcome = await listHandler.HandleAsync(
            new GetUniqueWordsPageQuery(
                kind,
                search,
                paramSort,
                page ?? DefaultPage,
                pageSize ?? DefaultListPageSize,
                UniqueWordsCountFilter.FromRaw(
                    occMin, occMax,
                    ayahsMin, ayahsMax,
                    surahsMin, surahsMax),
                UniqueWordsAssociationFilter.FromRaw(primaryType, rootId)),
            cancellationToken);

        return outcome switch
        {
            GetUniqueWordsPageOutcome.Success success =>
                Ok(ApiResponse<PagedResult<UniqueWordListItemDto>>.Ok(success.Page, ApiMessages.UniqueWordsListLoaded)),
            GetUniqueWordsPageOutcome.InvalidKind =>
                BadRequest(ApiResponse<PagedResult<UniqueWordListItemDto>>.Fail(ApiMessages.UniqueWordsInvalidKind)),
            GetUniqueWordsPageOutcome.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<UniqueWordListItemDto>>.Fail(ApiMessages.UniqueWordsInvalidSort)),
            GetUniqueWordsPageOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<UniqueWordListItemDto>>.Fail(ApiMessages.UniqueWordsInvalidPaging)),
            GetUniqueWordsPageOutcome.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<UniqueWordListItemDto>>.Fail(ApiMessages.UniqueWordsInvalidFilter)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetUniqueWordsPageOutcome)} variant."),
        };
    }

    [HttpGet("{kind}/{id:int}")]
    public async Task<ActionResult<ApiResponse<UniqueWordSummaryDto>>> GetSummary(
        string kind,
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await summaryHandler.HandleAsync(
            new GetUniqueWordSummaryQuery(kind, id),
            cancellationToken);

        return outcome switch
        {
            GetUniqueWordSummaryOutcome.Success success =>
                Ok(ApiResponse<UniqueWordSummaryDto>.Ok(success.Summary, ApiMessages.UniqueWordSummaryLoaded)),
            GetUniqueWordSummaryOutcome.InvalidKind =>
                BadRequest(ApiResponse<UniqueWordSummaryDto>.Fail(ApiMessages.UniqueWordsInvalidKind)),
            GetUniqueWordSummaryOutcome.InvalidId =>
                BadRequest(ApiResponse<UniqueWordSummaryDto>.Fail(ApiMessages.UniqueWordsInvalidId)),
            GetUniqueWordSummaryOutcome.NotFound =>
                NotFound(ApiResponse<UniqueWordSummaryDto>.Fail(ApiMessages.UniqueWordNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetUniqueWordSummaryOutcome)} variant."),
        };
    }

    [HttpGet("{kind}/{id:int}/surahs")]
    public async Task<ActionResult<ApiResponse<UniqueWordSurahsResponse>>> GetSurahs(
        string kind,
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await surahsHandler.HandleAsync(
            new GetUniqueWordSurahsQuery(kind, id),
            cancellationToken);

        return outcome switch
        {
            GetUniqueWordSurahsOutcome.Success success =>
                Ok(ApiResponse<UniqueWordSurahsResponse>.Ok(success.Response, ApiMessages.UniqueWordSurahsLoaded)),
            GetUniqueWordSurahsOutcome.InvalidKind =>
                BadRequest(ApiResponse<UniqueWordSurahsResponse>.Fail(ApiMessages.UniqueWordsInvalidKind)),
            GetUniqueWordSurahsOutcome.InvalidId =>
                BadRequest(ApiResponse<UniqueWordSurahsResponse>.Fail(ApiMessages.UniqueWordsInvalidId)),
            GetUniqueWordSurahsOutcome.NotFound =>
                NotFound(ApiResponse<UniqueWordSurahsResponse>.Fail(ApiMessages.UniqueWordNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetUniqueWordSurahsOutcome)} variant."),
        };
    }

    [HttpGet("{kind}/{id:int}/missing-surahs")]
    public async Task<ActionResult<ApiResponse<UniqueWordMissingSurahsResponse>>> GetMissingSurahs(
        string kind,
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await missingSurahsHandler.HandleAsync(
            new GetUniqueWordMissingSurahsQuery(kind, id),
            cancellationToken);

        return outcome switch
        {
            GetUniqueWordMissingSurahsOutcome.Success success =>
                Ok(ApiResponse<UniqueWordMissingSurahsResponse>.Ok(success.Response, ApiMessages.UniqueWordMissingSurahsLoaded)),
            GetUniqueWordMissingSurahsOutcome.InvalidKind =>
                BadRequest(ApiResponse<UniqueWordMissingSurahsResponse>.Fail(ApiMessages.UniqueWordsInvalidKind)),
            GetUniqueWordMissingSurahsOutcome.InvalidId =>
                BadRequest(ApiResponse<UniqueWordMissingSurahsResponse>.Fail(ApiMessages.UniqueWordsInvalidId)),
            GetUniqueWordMissingSurahsOutcome.NotFound =>
                NotFound(ApiResponse<UniqueWordMissingSurahsResponse>.Fail(ApiMessages.UniqueWordNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetUniqueWordMissingSurahsOutcome)} variant."),
        };
    }

    [HttpGet("{kind}/{id:int}/ayahs")]
    public async Task<ActionResult<ApiResponse<PagedResult<UniqueWordAyahMatchDto>>>> GetAyahs(
        string kind,
        int id,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await ayahsHandler.HandleAsync(
            new GetUniqueWordAyahsQuery(
                kind,
                id,
                page ?? DefaultPage,
                pageSize ?? DefaultAyahPageSize),
            cancellationToken);

        return outcome switch
        {
            GetUniqueWordAyahsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<UniqueWordAyahMatchDto>>.Ok(success.Page, ApiMessages.UniqueWordAyahsLoaded)),
            GetUniqueWordAyahsOutcome.InvalidKind =>
                BadRequest(ApiResponse<PagedResult<UniqueWordAyahMatchDto>>.Fail(ApiMessages.UniqueWordsInvalidKind)),
            GetUniqueWordAyahsOutcome.InvalidId =>
                BadRequest(ApiResponse<PagedResult<UniqueWordAyahMatchDto>>.Fail(ApiMessages.UniqueWordsInvalidId)),
            GetUniqueWordAyahsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<UniqueWordAyahMatchDto>>.Fail(ApiMessages.UniqueWordsInvalidPaging)),
            GetUniqueWordAyahsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<UniqueWordAyahMatchDto>>.Fail(ApiMessages.UniqueWordNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetUniqueWordAyahsOutcome)} variant."),
        };
    }
}
