using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaAyahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMissingSurahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaStems;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaWords;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmasPage;
using QuranDashboard.Application.Quran.Words.Lemmas.Queries.GetLemmaSummary;

namespace QuranDashboard.Api.Controllers.Words;

/// <summary>
/// Lemmas Explorer (Feature 016) read-only endpoints under the existing Words
/// area. Route base: <c>api/words/lemmas</c>. Sibling of Feature 015
/// <c>RootsController</c>. Story-phase actions are added incrementally;
/// US3/US4/US5/US6 handlers are injected when their actions land.
/// </summary>
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

    /// <summary>
/// يُرجع صفحة واحدة من الصيغ المعجمية مع بحث عربي مُطبّع (contains) وخيارات
/// ترتيب وتصفّح، وكل الأعداد لكل صيغة ومعرّف الجذر المملوك.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<LemmaListItemDto>>>> Get(
        [FromQuery] string? search,
        [FromQuery(Name = "sort")] string? paramSort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await listHandler.HandleAsync(
            new GetLemmasPageQuery(
                search,
                paramSort,
                page ?? DefaultPage,
                pageSize ?? DefaultListPageSize),
            cancellationToken);

        return outcome switch
        {
            GetLemmasPageOutcome.Success success =>
                Ok(ApiResponse<PagedResult<LemmaListItemDto>>.Ok(success.Page, ApiMessages.LemmasListLoaded)),
            GetLemmasPageOutcome.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<LemmaListItemDto>>.Fail(ApiMessages.LemmasInvalidSort)),
            GetLemmasPageOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<LemmaListItemDto>>.Fail(ApiMessages.LemmasInvalidPaging)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetLemmasPageOutcome)} variant."),
        };
    }

    /// <summary>
    /// يُرجع ملخّص الصيغة المعجمية المحددة (أعدادها وتوزيع الأنواع الكامل).
    /// يُستخدم لاستعادة حالة لوحة التفاصيل من رابط المشاركة.
    /// </summary>
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

    /// <summary>
    /// يُرجع صفحة من الكلمات المرتبطة بالصّيغة المعجمية المحددة بحسب نوعها
    /// (بسيطة أو بالتشكيل)، مع عدد مرات الظهور في سياق هذه الصيغة.
    /// </summary>
    [HttpGet("{id:int}/words/{wordKind}")]
    public async Task<ActionResult<ApiResponse<PagedResult<LemmaWordItemDto>>>> GetWords(
        int id,
        string wordKind,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await wordsHandler.HandleAsync(
            new GetLemmaWordsQuery(
                id,
                wordKind,
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

    /// <summary>
    /// يُرجع صفحة من الآيات التي وردت فيها الصيغة المعجمية المحددة، مع معرّفات
    /// الكلمات المطابقة للتمييز البصري.
    /// </summary>
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

    /// <summary>
    /// يُرجع السور التي وردت فيها الصيغة المعجمية المحددة مع عدد مرات الظهور
    /// في كل سورة.
    /// </summary>
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

    /// <summary>
    /// يُرجع السور التي لم ترد فيها الصيغة المعجمية المحددة.
    /// </summary>
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

    /// <summary>
    /// يُرجع الأصول الصرفية التي وردت فيها الصيغة المعجمية المحددة مع عدد
    /// مرات الظهور في سياق كل أصل.
    /// </summary>
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
