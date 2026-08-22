using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaAyahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMissingSurahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaStems;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaWords;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaSummary;

namespace QuranDashboard.Api.Controllers.Words;

[ApiController]
[Route("api/words/lemmas")]
public sealed class LemmasController(
    GetLemmasPageHandler listHandler,
    GetLemmaAyahsHandler ayahsHandler,
    GetLemmaWordsHandler wordsHandler,
    GetLemmaMentionedSurahsHandler mentionedSurahsHandler,
    GetLemmaMissingSurahsHandler missingSurahsHandler,
    GetLemmaStemsHandler stemsHandler,
    GetLemmaSummaryHandler summaryHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 1000;
    private const int DefaultDetailPageSize = 100;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<LemmaListItemDto>>>> Get(
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
        [FromQuery] int? stemsMin,
        [FromQuery] int? stemsMax,
        [FromQuery] int? rootId,
        CancellationToken cancellationToken)
    {
        var outcome = await listHandler.HandleAsync(
            new GetLemmasPageQuery(
                search,
                paramSort,
                page ?? DefaultPage,
                pageSize ?? DefaultListPageSize,
                LemmasCountFilter.FromRaw(
                    occMin, occMax,
                    ayahsMin, ayahsMax,
                    surahsMin, surahsMax,
                    simpleWordsMin, simpleWordsMax,
                    tashkeelWordsMin, tashkeelWordsMax,
                    stemsMin, stemsMax),
                LemmasAssociationFilter.FromRaw(rootId)),
            cancellationToken);

        return outcome switch
        {
            GetLemmasPageOutcome.Success success =>
                Ok(ApiResponse<PagedResult<LemmaListItemDto>>.Ok(success.Page, ApiMessages.LemmasListLoaded)),
            GetLemmasPageOutcome.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<LemmaListItemDto>>.Fail(ApiMessages.LemmasInvalidSort)),
            GetLemmasPageOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<LemmaListItemDto>>.Fail(ApiMessages.LemmasInvalidPaging)),
            GetLemmasPageOutcome.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<LemmaListItemDto>>.Fail(ApiMessages.LemmasInvalidFilter)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetLemmasPageOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<LemmaSummaryDto>>> GetSummary(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await summaryHandler.HandleAsync(
            new GetLemmaSummaryQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetLemmaSummaryOutcome.Success success =>
                Ok(ApiResponse<LemmaSummaryDto>.Ok(success.Summary, ApiMessages.LemmaSummaryLoaded)),
            GetLemmaSummaryOutcome.InvalidId =>
                BadRequest(ApiResponse<LemmaSummaryDto>.Fail(ApiMessages.LemmasInvalidId)),
            GetLemmaSummaryOutcome.NotFound =>
                NotFound(ApiResponse<LemmaSummaryDto>.Fail(ApiMessages.LemmaNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetLemmaSummaryOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/words/{wordKind}")]
    public async Task<ActionResult<ApiResponse<PagedResult<LemmaWordItemDto>>>> GetWords(
        int id,
        string wordKind,
        [FromQuery] string? typeCode,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await wordsHandler.HandleAsync(
            new GetLemmaWordsQuery(
                id,
                wordKind,
                NormalizeTypeCode(typeCode),
                page ?? DefaultPage,
                pageSize ?? DefaultDetailPageSize),
            cancellationToken);

        return outcome switch
        {
            GetLemmaWordsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<LemmaWordItemDto>>.Ok(success.Page, ApiMessages.LemmaWordsLoaded)),
            GetLemmaWordsOutcome.InvalidId =>
                BadRequest(ApiResponse<PagedResult<LemmaWordItemDto>>.Fail(ApiMessages.LemmasInvalidId)),
            GetLemmaWordsOutcome.InvalidKind =>
                BadRequest(ApiResponse<PagedResult<LemmaWordItemDto>>.Fail(ApiMessages.LemmasInvalidKind)),
            GetLemmaWordsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<LemmaWordItemDto>>.Fail(ApiMessages.LemmasInvalidPaging)),
            GetLemmaWordsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<LemmaWordItemDto>>.Fail(ApiMessages.LemmaNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetLemmaWordsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/ayahs")]
    public async Task<ActionResult<ApiResponse<PagedResult<LemmaAyahMatchDto>>>> GetAyahs(
        int id,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? typeCode,
        CancellationToken cancellationToken)
    {
        var outcome = await ayahsHandler.HandleAsync(
            new GetLemmaAyahsQuery(
                id,
                page ?? DefaultPage,
                pageSize ?? DefaultDetailPageSize,
                NormalizeTypeCode(typeCode)),
            cancellationToken);

        return outcome switch
        {
            GetLemmaAyahsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<LemmaAyahMatchDto>>.Ok(success.Page, ApiMessages.LemmaAyahsLoaded)),
            GetLemmaAyahsOutcome.InvalidId =>
                BadRequest(ApiResponse<PagedResult<LemmaAyahMatchDto>>.Fail(ApiMessages.LemmasInvalidId)),
            GetLemmaAyahsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<LemmaAyahMatchDto>>.Fail(ApiMessages.LemmasInvalidPaging)),
            GetLemmaAyahsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<LemmaAyahMatchDto>>.Fail(ApiMessages.LemmaNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetLemmaAyahsOutcome)} variant."),
        };
    }

    private static string? NormalizeTypeCode(string? typeCode) =>
        string.IsNullOrWhiteSpace(typeCode) ? null : typeCode.Trim();

    [HttpGet("{id:int}/surahs")]
    public async Task<ActionResult<ApiResponse<LemmaSurahsResponse>>> GetSurahs(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await mentionedSurahsHandler.HandleAsync(
            new GetLemmaMentionedSurahsQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetLemmaMentionedSurahsOutcome.Success success =>
                Ok(ApiResponse<LemmaSurahsResponse>.Ok(success.Surahs, ApiMessages.LemmaSurahsLoaded)),
            GetLemmaMentionedSurahsOutcome.InvalidId =>
                BadRequest(ApiResponse<LemmaSurahsResponse>.Fail(ApiMessages.LemmasInvalidId)),
            GetLemmaMentionedSurahsOutcome.NotFound =>
                NotFound(ApiResponse<LemmaSurahsResponse>.Fail(ApiMessages.LemmaNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetLemmaMentionedSurahsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/missing-surahs")]
    public async Task<ActionResult<ApiResponse<LemmaMissingSurahsResponse>>> GetMissingSurahs(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await missingSurahsHandler.HandleAsync(
            new GetLemmaMissingSurahsQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetLemmaMissingSurahsOutcome.Success success =>
                Ok(ApiResponse<LemmaMissingSurahsResponse>.Ok(success.MissingSurahs, ApiMessages.LemmaMissingSurahsLoaded)),
            GetLemmaMissingSurahsOutcome.InvalidId =>
                BadRequest(ApiResponse<LemmaMissingSurahsResponse>.Fail(ApiMessages.LemmasInvalidId)),
            GetLemmaMissingSurahsOutcome.NotFound =>
                NotFound(ApiResponse<LemmaMissingSurahsResponse>.Fail(ApiMessages.LemmaNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetLemmaMissingSurahsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/stems")]
    public async Task<ActionResult<ApiResponse<LemmaStemsResponse>>> GetStems(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await stemsHandler.HandleAsync(
            new GetLemmaStemsQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetLemmaStemsOutcome.Success success =>
                Ok(ApiResponse<LemmaStemsResponse>.Ok(success.Stems, ApiMessages.LemmaStemsLoaded)),
            GetLemmaStemsOutcome.InvalidId =>
                BadRequest(ApiResponse<LemmaStemsResponse>.Fail(ApiMessages.LemmasInvalidId)),
            GetLemmaStemsOutcome.NotFound =>
                NotFound(ApiResponse<LemmaStemsResponse>.Fail(ApiMessages.LemmaNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetLemmaStemsOutcome)} variant."),
        };
    }
}
