using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeSurahs;

namespace QuranDashboard.Api.Controllers.Words;

// Per-word detail endpoints under the same api/words/word-types base; word identity is the
// tashkeel word id plus its context code. Split out of the tree/list/table part in
// WordTypesController.cs, which declares the shared handlers, route attribute, and paging
// defaults. Same class by design — a second controller class would retag these endpoints in the
// generated OpenAPI document. (Scoped root/stem/lemma grouped details are a different route
// family and live in WordTypeGroupedDetailsController.)
public sealed partial class WordTypesController
{
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
