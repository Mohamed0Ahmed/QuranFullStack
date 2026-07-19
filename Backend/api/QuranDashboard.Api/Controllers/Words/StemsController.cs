using QuranDashboard.Application.Abstractions.Quran.Words.Stems;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemAyahs;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMissingSurahs;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemLemmas;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemWords;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemSummary;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

namespace QuranDashboard.Api.Controllers.Words;

[ApiController]
[Route("api/words/stems")]
public sealed class StemsController(
    GetStemsPageHandler listHandler,
    GetStemAyahsHandler ayahsHandler,
    GetStemWordsHandler wordsHandler,
    GetStemMentionedSurahsHandler mentionedSurahsHandler,
    GetStemMissingSurahsHandler missingSurahsHandler,
    GetStemLemmasHandler lemmasHandler,
    GetStemSummaryHandler summaryHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 1000;
    private const int DefaultDetailPageSize = 100;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<StemListItemDto>>>> Get(
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
        [FromQuery] int? simpleWordsMin,
        [FromQuery] int? simpleWordsMax,
        [FromQuery] int? tashkeelWordsMin,
        [FromQuery] int? tashkeelWordsMax,
        [FromQuery] int? rootId,
        [FromQuery] int? lemmaId,
        CancellationToken cancellationToken)
    {
        var outcome = await listHandler.HandleAsync(
            new GetStemsPageQuery(
                search,
                paramSort,
                page ?? DefaultPage,
                pageSize ?? DefaultListPageSize,
                StemsCountFilter.FromRaw(
                    occMin, occMax,
                    ayahsMin, ayahsMax,
                    surahsMin, surahsMax,
                    simpleWordsMin, simpleWordsMax,
                    tashkeelWordsMin, tashkeelWordsMax),
                StemsAssociationFilter.FromRaw(rootId, lemmaId)),
            cancellationToken);

        return outcome switch
        {
            GetStemsPageOutcome.Success success =>
                Ok(ApiResponse<PagedResult<StemListItemDto>>.Ok(success.Page, ApiMessages.StemsListLoaded)),
            GetStemsPageOutcome.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<StemListItemDto>>.Fail(ApiMessages.StemsInvalidSort)),
            GetStemsPageOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<StemListItemDto>>.Fail(ApiMessages.StemsInvalidPaging)),
            GetStemsPageOutcome.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<StemListItemDto>>.Fail(ApiMessages.StemsInvalidFilter)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetStemsPageOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<StemSummaryDto>>> GetSummary(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await summaryHandler.HandleAsync(
            new GetStemSummaryQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetStemSummaryOutcome.Success success =>
                Ok(ApiResponse<StemSummaryDto>.Ok(success.Summary, ApiMessages.StemSummaryLoaded)),
            GetStemSummaryOutcome.InvalidId =>
                BadRequest(ApiResponse<StemSummaryDto>.Fail(ApiMessages.StemsInvalidId)),
            GetStemSummaryOutcome.NotFound =>
                NotFound(ApiResponse<StemSummaryDto>.Fail(ApiMessages.StemNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetStemSummaryOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/words/{wordKind}")]
    public async Task<ActionResult<ApiResponse<PagedResult<StemWordItemDto>>>> GetWords(
        int id,
        string wordKind,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await wordsHandler.HandleAsync(
            new GetStemWordsQuery(
                id,
                wordKind,
                page ?? DefaultPage,
                pageSize ?? DefaultDetailPageSize),
            cancellationToken);

        return outcome switch
        {
            GetStemWordsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<StemWordItemDto>>.Ok(success.Page, ApiMessages.StemWordsLoaded)),
            GetStemWordsOutcome.InvalidId =>
                BadRequest(ApiResponse<PagedResult<StemWordItemDto>>.Fail(ApiMessages.StemsInvalidId)),
            GetStemWordsOutcome.InvalidKind =>
                BadRequest(ApiResponse<PagedResult<StemWordItemDto>>.Fail(ApiMessages.StemsInvalidKind)),
            GetStemWordsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<StemWordItemDto>>.Fail(ApiMessages.StemsInvalidPaging)),
            GetStemWordsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<StemWordItemDto>>.Fail(ApiMessages.StemNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetStemWordsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/ayahs")]
    public async Task<ActionResult<ApiResponse<PagedResult<StemAyahMatchDto>>>> GetAyahs(
        int id,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery(Name = "typeCode")] string? typeCode,
        CancellationToken cancellationToken)
    {
        var outcome = await ayahsHandler.HandleAsync(
            new GetStemAyahsQuery(
                id,
                page ?? DefaultPage,
                pageSize ?? DefaultDetailPageSize,
                typeCode),
            cancellationToken);

        return outcome switch
        {
            GetStemAyahsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<StemAyahMatchDto>>.Ok(success.Page, ApiMessages.StemAyahsLoaded)),
            GetStemAyahsOutcome.InvalidId =>
                BadRequest(ApiResponse<PagedResult<StemAyahMatchDto>>.Fail(ApiMessages.StemsInvalidId)),
            GetStemAyahsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<StemAyahMatchDto>>.Fail(ApiMessages.StemsInvalidPaging)),
            GetStemAyahsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<StemAyahMatchDto>>.Fail(ApiMessages.StemNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetStemAyahsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/surahs")]
    public async Task<ActionResult<ApiResponse<StemSurahsResponse>>> GetSurahs(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await mentionedSurahsHandler.HandleAsync(
            new GetStemMentionedSurahsQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetStemMentionedSurahsOutcome.Success success =>
                Ok(ApiResponse<StemSurahsResponse>.Ok(success.Surahs, ApiMessages.StemSurahsLoaded)),
            GetStemMentionedSurahsOutcome.InvalidId =>
                BadRequest(ApiResponse<StemSurahsResponse>.Fail(ApiMessages.StemsInvalidId)),
            GetStemMentionedSurahsOutcome.NotFound =>
                NotFound(ApiResponse<StemSurahsResponse>.Fail(ApiMessages.StemNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetStemMentionedSurahsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/missing-surahs")]
    public async Task<ActionResult<ApiResponse<StemMissingSurahsResponse>>> GetMissingSurahs(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await missingSurahsHandler.HandleAsync(
            new GetStemMissingSurahsQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetStemMissingSurahsOutcome.Success success =>
                Ok(ApiResponse<StemMissingSurahsResponse>.Ok(success.MissingSurahs, ApiMessages.StemMissingSurahsLoaded)),
            GetStemMissingSurahsOutcome.InvalidId =>
                BadRequest(ApiResponse<StemMissingSurahsResponse>.Fail(ApiMessages.StemsInvalidId)),
            GetStemMissingSurahsOutcome.NotFound =>
                NotFound(ApiResponse<StemMissingSurahsResponse>.Fail(ApiMessages.StemNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetStemMissingSurahsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/lemmas")]
    public async Task<ActionResult<ApiResponse<StemLemmasResponse>>> GetLemmas(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await lemmasHandler.HandleAsync(
            new GetStemLemmasQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetStemLemmasOutcome.Success success =>
                Ok(ApiResponse<StemLemmasResponse>.Ok(success.Lemmas, ApiMessages.StemLemmasLoaded)),
            GetStemLemmasOutcome.InvalidId =>
                BadRequest(ApiResponse<StemLemmasResponse>.Fail(ApiMessages.StemsInvalidId)),
            GetStemLemmasOutcome.NotFound =>
                NotFound(ApiResponse<StemLemmasResponse>.Fail(ApiMessages.StemNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetStemLemmasOutcome)} variant."),
        };
    }
}
