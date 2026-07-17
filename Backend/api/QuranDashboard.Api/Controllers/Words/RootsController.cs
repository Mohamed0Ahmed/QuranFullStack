using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootAyahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootLemmas;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMissingSurahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootStems;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootSummary;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootWords;

namespace QuranDashboard.Api.Controllers.Words;

/// <summary>
/// Roots Explorer (Feature 015) read-only endpoints under the existing Words
/// area. Route base: <c>api/words/roots</c>. Mirrors Feature 014
/// <c>UniqueWordsController</c>. Story-phase actions are added incrementally;
/// US2/US3/US4/US5 handlers are injected when their actions land.
/// </summary>
/// <remarks>
/// This part owns the list endpoint and the shared construction/defaults. The per-root detail
/// and drilldown endpoints live in <c>RootsController.Details.cs</c>. Both parts are the same
/// controller on purpose: the endpoint group split must not retag the generated OpenAPI document.
/// </remarks>
[ApiController]
[Route("api/words/roots")]
public sealed partial class RootsController(
    GetRootsPageHandler listHandler,
    GetRootSummaryHandler summaryHandler,
    GetRootAyahsHandler ayahsHandler,
    GetRootWordsHandler wordsHandler,
    GetRootMentionedSurahsHandler mentionedSurahsHandler,
    GetRootMissingSurahsHandler missingSurahsHandler,
    GetRootLemmasHandler lemmasHandler,
    GetRootStemsHandler stemsHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 1000;
    private const int DefaultDetailPageSize = 100;

    /// <summary>
    /// يُرجع صفحة واحدة من الجذور مع بحث عربي مُطبّع (contains) وخيارات ترتيب
    /// وتصفّح، وكل الأعداد الثمانية لكل جذر.
    /// </summary>
    /// <param name="search">نص البحث العربي المُطبّع (contains، اختياري).</param>
    /// <param name="paramSort">
    /// مفتاح الترتيب (اختياري، الافتراضي <c>mushaf-order</c>). الصيغة:
    /// <c>عمود</c> أو <c>عمود-asc</c> أو <c>عمود-desc</c>.
    /// الأعمدة المتاحة: <c>alpha</c> (تصاعدي طبيعيًا)، و<c>occurrences</c> و<c>ayahs</c>
    /// و<c>surahs</c> و<c>simple</c> و<c>tashkeel</c> و<c>lemmas</c> و<c>stems</c> (تنازلية طبيعيًا).
    /// المفتاح المجرّد يعني الاتجاه الطبيعي للعمود، لذا <c>occurrences</c> ≡
    /// <c>occurrences-desc</c> و<c>alpha</c> ≡ <c>alpha-asc</c>.
    /// و<c>mushaf-order</c> هو ترتيب المصحف التصاعدي فقط ولا يقبل لاحقة اتجاه.
    /// أي مفتاح آخر يُرجع 400.
    /// </param>
    /// <param name="page">رقم الصفحة (الافتراضي 1).</param>
    /// <param name="pageSize">حجم الصفحة (الافتراضي 1000).</param>
    /// <param name="occMin">الحد الأدنى لعدد المواضع (اختياري).</param>
    /// <param name="occMax">الحد الأعلى لعدد المواضع (اختياري).</param>
    /// <param name="ayahsMin">الحد الأدنى لعدد الآيات (اختياري).</param>
    /// <param name="ayahsMax">الحد الأعلى لعدد الآيات (اختياري).</param>
    /// <param name="surahsMin">الحد الأدنى لعدد السور (اختياري).</param>
    /// <param name="surahsMax">الحد الأعلى لعدد السور (اختياري).</param>
    /// <param name="simpleWordsMin">الحد الأدنى لعدد الكلمات بدون تشكيل (اختياري).</param>
    /// <param name="simpleWordsMax">الحد الأعلى لعدد الكلمات بدون تشكيل (اختياري).</param>
    /// <param name="tashkeelWordsMin">الحد الأدنى لعدد الكلمات بالتشكيل (اختياري).</param>
    /// <param name="tashkeelWordsMax">الحد الأعلى لعدد الكلمات بالتشكيل (اختياري).</param>
    /// <param name="lemmasMin">الحد الأدنى لعدد الصيغ المعجمية (اختياري).</param>
    /// <param name="lemmasMax">الحد الأعلى لعدد الصيغ المعجمية (اختياري).</param>
    /// <param name="stemsMin">الحد الأدنى لعدد الأصول الصرفية (اختياري).</param>
    /// <param name="stemsMax">الحد الأعلى لعدد الأصول الصرفية (اختياري).</param>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تحميل صفحة الجذور بنجاح.</response>
    /// <response code="400">مفتاح ترتيب أو مرشّح أو تقسيم صفحات غير صالح.</response>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<RootListItemDto>>>> Get(
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
        [FromQuery] int? lemmasMin,
        [FromQuery] int? lemmasMax,
        [FromQuery] int? stemsMin,
        [FromQuery] int? stemsMax,
        CancellationToken cancellationToken)
    {
        var outcome = await listHandler.HandleAsync(
            new GetRootsPageQuery(
                search,
                paramSort,
                page ?? DefaultPage,
                pageSize ?? DefaultListPageSize,
                RootsCountFilter.FromRaw(
                    occMin, occMax,
                    ayahsMin, ayahsMax,
                    surahsMin, surahsMax,
                    simpleWordsMin, simpleWordsMax,
                    tashkeelWordsMin, tashkeelWordsMax,
                    lemmasMin, lemmasMax,
                    stemsMin, stemsMax)),
            cancellationToken);

        return outcome switch
        {
            GetRootsPageOutcome.Success success =>
                Ok(ApiResponse<PagedResult<RootListItemDto>>.Ok(success.Page, ApiMessages.RootsListLoaded)),
            GetRootsPageOutcome.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<RootListItemDto>>.Fail(ApiMessages.RootsInvalidSort)),
            GetRootsPageOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<RootListItemDto>>.Fail(ApiMessages.RootsInvalidPaging)),
            GetRootsPageOutcome.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<RootListItemDto>>.Fail(ApiMessages.RootsInvalidFilter)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetRootsPageOutcome)} variant."),
        };
    }
}
