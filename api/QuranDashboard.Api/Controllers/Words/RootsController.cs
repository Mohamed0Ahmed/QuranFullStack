using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootAyahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootsPage;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootSummary;

namespace QuranDashboard.Api.Controllers.Words;

/// <summary>
/// Roots Explorer (Feature 015) read-only endpoints under the existing Words
/// area. Route base: <c>api/words/roots</c>. Mirrors Feature 014
/// <c>UniqueWordsController</c>. Story-phase actions are added incrementally;
/// US2/US3/US4/US5 handlers are injected when their actions land.
/// </summary>
[ApiController]
[Route("api/words/roots")]
public sealed class RootsController(
    GetRootsPageHandler listHandler,
    GetRootSummaryHandler summaryHandler,
    GetRootAyahsHandler ayahsHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 1000;
    private const int DefaultAyahPageSize = 100;

    /// <summary>
    /// يُرجع صفحة واحدة من الجذور مع بحث عربي مُطبّع (contains) وخيارات ترتيب
    /// وتصفّح، وكل الأعداد الثمانية لكل جذر.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<RootListItemDto>>>> Get(
        [FromQuery] string? search,
        [FromQuery(Name = "sort")] string? paramSort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await listHandler.HandleAsync(
            new GetRootsPageQuery(
                search,
                paramSort,
                page ?? DefaultPage,
                pageSize ?? DefaultListPageSize),
            cancellationToken);

        return outcome switch
        {
            GetRootsPageOutcome.Success success =>
                Ok(ApiResponse<PagedResult<RootListItemDto>>.Ok(success.Page, ApiMessages.RootsListLoaded)),
            GetRootsPageOutcome.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<RootListItemDto>>.Fail(ApiMessages.RootsInvalidSort)),
            GetRootsPageOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<RootListItemDto>>.Fail(ApiMessages.RootsInvalidPaging)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetRootsPageOutcome)} variant."),
        };
    }

    /// <summary>
    /// يُرجع ملخّص الجذر المحدد (أعداده الثمانية). يُستخدم لاستعادة حالة لوحة
    /// التفاصيل من رابط مشاركة.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<RootSummaryDto>>> GetSummary(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await summaryHandler.HandleAsync(
            new GetRootSummaryQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetRootSummaryOutcome.Success success =>
                Ok(ApiResponse<RootSummaryDto>.Ok(success.Summary, ApiMessages.RootSummaryLoaded)),
            GetRootSummaryOutcome.InvalidId =>
                BadRequest(ApiResponse<RootSummaryDto>.Fail(ApiMessages.RootsInvalidId)),
            GetRootSummaryOutcome.NotFound =>
                NotFound(ApiResponse<RootSummaryDto>.Fail(ApiMessages.RootNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetRootSummaryOutcome)} variant."),
        };
    }

    /// <summary>
    /// يُرجع صفحة من الآيات التي ورد فيها الجذر المحدد، مع معرّفات الكلمات
    /// المطابقة للتمييز البصري.
    /// </summary>
    [HttpGet("{id:int}/ayahs")]
    public async Task<ActionResult<ApiResponse<PagedResult<RootAyahMatchDto>>>> GetAyahs(
        int id,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await ayahsHandler.HandleAsync(
            new GetRootAyahsQuery(
                id,
                page ?? DefaultPage,
                pageSize ?? DefaultAyahPageSize),
            cancellationToken);

        return outcome switch
        {
            GetRootAyahsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<RootAyahMatchDto>>.Ok(success.Page, ApiMessages.RootAyahsLoaded)),
            GetRootAyahsOutcome.InvalidId =>
                BadRequest(ApiResponse<PagedResult<RootAyahMatchDto>>.Fail(ApiMessages.RootsInvalidId)),
            GetRootAyahsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<RootAyahMatchDto>>.Fail(ApiMessages.RootsInvalidPaging)),
            GetRootAyahsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<RootAyahMatchDto>>.Fail(ApiMessages.RootNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetRootAyahsOutcome)} variant."),
        };
    }
}
