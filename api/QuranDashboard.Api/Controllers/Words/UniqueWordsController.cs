using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Responses;
using QuranDashboard.Application.Quran.Words.Queries.GetUniqueWordsPage;

namespace QuranDashboard.Api.Controllers.Words;

/// <summary>
/// نقاط قراءة الكلمات الفريدة (الميزة 014). للقراءة فقط؛ لا تغيّر بيانات
/// المصحف. نقطة القائمة تُضاف في T040، ونقاط التفصيل في T064 وT081.
/// </summary>
[ApiController]
[Route("api/words/unique")]
public sealed class UniqueWordsController(GetUniqueWordsPageHandler listHandler) : ControllerBase
{
    /// <summary>القيمة الافتراضية لرقم الصفحة (1-based).</summary>
    private const int DefaultPage = 1;

    /// <summary>حجم الصفحة الافتراضي لقائمة الكلمات الفريدة.</summary>
    private const int DefaultPageSize = 50;

    /// <summary>
    /// يُرجع صفحة واحدة من الكلمات الفريدة للنوع المحدد (<c>tashkeel</c> أو
    /// <c>simple</c>) مع بحث عربي مُطبّع (contains) وخيارات ترتيب وتصفّح.
    /// يستخدم الأعداد المحسوبة مسبقًا ولا يُجمّع <c>quran_words</c> لكل بطاقة.
    /// </summary>
    /// <param name="kind">نوع الكلمات: <c>tashkeel</c> أو <c>simple</c>.</param>
    /// <param name="search">بحث عربي اختياري (contains بعد التطبيع).</param>
    /// <param name="paramSort">ترتيب اختياري: <c>mushaf-order</c> أو <c>occurrences</c> أو <c>alpha</c>.</param>
    /// <param name="page">رقم الصفحة (1-based)؛ افتراضي 1.</param>
    /// <param name="pageSize">حجم الصفحة؛ افتراضي 50.</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل الصفحة بنجاح (حتى لو كانت النتائج فارغة).</response>
    /// <response code="400">نوع أو ترتيب أو معطيات تصفّح غير صالحة.</response>
    [HttpGet("{kind}")]
    public async Task<ActionResult<ApiResponse<PagedResult<UniqueWordListItemDto>>>> Get(
        string kind,
        [FromQuery] string? search,
        [FromQuery(Name = "sort")] string? paramSort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await listHandler.HandleAsync(
            new GetUniqueWordsPageQuery(
                kind,
                search,
                paramSort,
                page ?? DefaultPage,
                pageSize ?? DefaultPageSize),
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
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetUniqueWordsPageOutcome)} variant."),
        };
    }
}
