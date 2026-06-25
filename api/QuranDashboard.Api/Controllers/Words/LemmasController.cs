using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses;
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
    GetLemmaSummaryHandler summaryHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 1000;

    /// <summary>
    /// يُرجع صفحة واحدة من الصيغ المعجمية مع بحث عربي مُطبّع (contains) وخيارات
    /// ترتيب وتصفّح، وكل الأعداد لكل صيغة والنوع الغالب ومعرّف الجذر المملوك.
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
    /// يُرجع ملخّص الصيغة المعجمية المحددة (أعدادها، نوعها الغالب، وتوزيع الأنواع
    /// الكامل). يُستخدم لاستعادة حالة لوحة التفاصيل من رابط المشاركة.
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
}
