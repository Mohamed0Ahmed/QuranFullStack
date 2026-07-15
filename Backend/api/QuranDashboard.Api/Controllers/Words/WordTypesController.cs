using QuranDashboard.Application.Abstractions.Common.Paging;
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
public sealed class WordTypesController(
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

    /// <summary>
    /// يُرجع شجرة أنواع الكلمات: الأنواع الرئيسية (اسم/فعل/حرف) وفروعها مع عدّاداتها.
    /// </summary>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل شجرة الأنواع بنجاح.</response>
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

    /// <summary>
    /// يُرجع صفوف الكلمات ضمن نطاق نوع محدّد، مقسّمة إلى صفحات مع دعم الفرز ومرشّحات الحالة والزمن والبناء.
    /// </summary>
    /// <param name="type">رمز النوع الرئيسي (مطلوب لتحديد النطاق).</param>
    /// <param name="childCode">رمز النوع الفرعي إن وجد.</param>
    /// <param name="caseFilter">مرشّح الحالة الإعرابية (اختياري).</param>
    /// <param name="tense">مرشّح الزمن (اختياري).</param>
    /// <param name="voice">مرشّح البناء للمعلوم/المجهول (اختياري).</param>
    /// <param name="search">نص البحث في هوية الكلمة (اختياري).</param>
    /// <param name="sort">مفتاح الفرز (اختياري).</param>
    /// <param name="page">رقم الصفحة (الافتراضي 1).</param>
    /// <param name="pageSize">حجم الصفحة (الافتراضي 1000).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل صفوف الكلمات بنجاح.</response>
    /// <response code="400">مرشّح أو فرز أو تقسيم صفحات غير صالح.</response>
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

    /// <summary>
    /// يُرجع صفوف عرض الجدول ضمن نطاق نوع محدّد بحسب تبويب العرض (كلمات/جذور/أصول/صيغ)، مقسّمة إلى صفحات ومجمّعة ومعدودة على الخادم.
    /// </summary>
    /// <param name="tableView">تبويب العرض: words أو roots أو stems أو lemmas (الافتراضي words).</param>
    /// <param name="type">رمز النوع الرئيسي (مطلوب لتحديد النطاق).</param>
    /// <param name="childCode">رمز النوع الفرعي إن وجد.</param>
    /// <param name="caseFilter">مرشّح الحالة الإعرابية (اختياري).</param>
    /// <param name="tense">مرشّح الزمن (اختياري).</param>
    /// <param name="voice">مرشّح البناء للمعلوم/المجهول (اختياري).</param>
    /// <param name="search">نص البحث في هوية الكلمة (اختياري).</param>
    /// <param name="sort">مفتاح الفرز (اختياري).</param>
    /// <param name="page">رقم الصفحة (الافتراضي 1).</param>
    /// <param name="pageSize">حجم الصفحة (الافتراضي 1000).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل صفوف الجدول بنجاح.</response>
    /// <response code="400">تبويب عرض أو مرشّح أو فرز أو تقسيم صفحات غير صالح.</response>
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

    /// <summary>
    /// يُرجع الإحصاء الرباعي لنطاق النوع النشط: عدد الكلمات والجذور والأصول والصيغ ضمن النطاق ذاته الذي يعرضه الجدول.
    /// </summary>
    /// <param name="type">رمز النوع الرئيسي (مطلوب لتحديد النطاق).</param>
    /// <param name="childCode">رمز النوع الفرعي إن وجد.</param>
    /// <param name="caseFilter">مرشّح الحالة الإعرابية (اختياري).</param>
    /// <param name="tense">مرشّح الزمن (اختياري).</param>
    /// <param name="voice">مرشّح البناء للمعلوم/المجهول (اختياري).</param>
    /// <param name="search">نص البحث في هوية الكلمة (اختياري).</param>
    /// <param name="hasRoot">مرشّح وجود الجذر ثلاثي الحالة (اختياري).</param>
    /// <param name="hasStem">مرشّح وجود الأصل الصرفي ثلاثي الحالة (اختياري).</param>
    /// <param name="hasLemma">مرشّح وجود الصيغة المعجمية ثلاثي الحالة (اختياري).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل إحصاء النطاق بنجاح (نطاق بلا نتائج يُرجع أصفارًا).</response>
    /// <response code="400">مرشّح غير صالح.</response>
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

    /// <summary>
    /// يُرجع ملخّص كلمة واحدة ضمن نطاق النوع؛ هوية الكلمة هي معرّف الكلمة المشكولة مع رمز السياق.
    /// </summary>
    /// <param name="tashkeelWordId">معرّف الكلمة المشكولة.</param>
    /// <param name="contextCode">رمز السياق الصرفي (جزء من الهوية).</param>
    /// <param name="caseFilter">مرشّح الحالة الإعرابية (اختياري).</param>
    /// <param name="tense">مرشّح الزمن (اختياري).</param>
    /// <param name="voice">مرشّح البناء للمعلوم/المجهول (اختياري).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل ملخّص الكلمة بنجاح.</response>
    /// <response code="400">هوية كلمة غير صالحة.</response>
    /// <response code="404">الكلمة غير موجودة ضمن النطاق المحدّد.</response>
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

    /// <summary>
    /// يُرجع آيات ورود الكلمة ضمن نطاق النوع، مقسّمة إلى صفحات بترتيب المصحف مع مواضع التطابق.
    /// </summary>
    /// <param name="tashkeelWordId">معرّف الكلمة المشكولة.</param>
    /// <param name="contextCode">رمز السياق الصرفي (جزء من الهوية).</param>
    /// <param name="caseFilter">مرشّح الحالة الإعرابية (اختياري).</param>
    /// <param name="tense">مرشّح الزمن (اختياري).</param>
    /// <param name="voice">مرشّح البناء للمعلوم/المجهول (اختياري).</param>
    /// <param name="page">رقم الصفحة (الافتراضي 1).</param>
    /// <param name="pageSize">حجم الصفحة (الافتراضي 100).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل آيات الكلمة بنجاح.</response>
    /// <response code="400">هوية كلمة أو تقسيم صفحات غير صالح.</response>
    /// <response code="404">الكلمة غير موجودة ضمن النطاق المحدّد.</response>
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

    /// <summary>
    /// يُرجع سور ورود الكلمة ضمن نطاق النوع: قائمتا السور الواردة والمفقودة دون تقسيم صفحات.
    /// </summary>
    /// <param name="tashkeelWordId">معرّف الكلمة المشكولة.</param>
    /// <param name="contextCode">رمز السياق الصرفي (جزء من الهوية).</param>
    /// <param name="caseFilter">مرشّح الحالة الإعرابية (اختياري).</param>
    /// <param name="tense">مرشّح الزمن (اختياري).</param>
    /// <param name="voice">مرشّح البناء للمعلوم/المجهول (اختياري).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل سور الكلمة بنجاح.</response>
    /// <response code="400">هوية كلمة غير صالحة.</response>
    /// <response code="404">الكلمة غير موجودة ضمن النطاق المحدّد.</response>
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
