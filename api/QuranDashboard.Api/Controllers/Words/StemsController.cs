using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.Stems.Responses;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemSummary;
using QuranDashboard.Application.Quran.Words.Stems.Queries.GetStemsPage;

namespace QuranDashboard.Api.Controllers.Words;

/// <summary>
/// Stems Explorer (Feature 016) read-only endpoints under the existing Words
/// area. Route base: <c>api/words/stems</c>. Sibling of Feature 015
/// <c>RootsController</c>. Story-phase actions are added incrementally; this
/// phase adds the catalogue and summary endpoints while later phases add the
/// detail actions.
/// </summary>
[ApiController]
[Route("api/words/stems")]
public sealed class StemsController(
    GetStemsPageHandler listHandler,
    GetStemSummaryHandler summaryHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultListPageSize = 1000;

    /// <summary>
    /// يُرجع صفحة واحدة من الأصول الصرفية مع بحث عربي مُطبّع (contains) وخيارات
    /// ترتيب وتصفّح، وكل الأعداد والعلاقة المعجمية/الجذرية الغالبة لكل أصل.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<StemListItemDto>>>> Get(
        [FromQuery] string? search,
        [FromQuery(Name = "sort")] string? paramSort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await listHandler.HandleAsync(
            new GetStemsPageQuery(
                search,
                paramSort,
                page ?? DefaultPage,
                pageSize ?? DefaultListPageSize),
            cancellationToken);

        return outcome switch
        {
            GetStemsPageOutcome.Success success =>
                Ok(ApiResponse<PagedResult<StemListItemDto>>.Ok(success.Page, ApiMessages.StemsListLoaded)),
            GetStemsPageOutcome.InvalidSort =>
                BadRequest(ApiResponse<PagedResult<StemListItemDto>>.Fail(ApiMessages.StemsInvalidSort)),
            GetStemsPageOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<StemListItemDto>>.Fail(ApiMessages.StemsInvalidPaging)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetStemsPageOutcome)} variant."),
        };
    }

    /// <summary>
    /// يُرجع ملخّص الأصل الصرفي المحدد (أعداده والعلاقة الغالبة وتوزيع الأنواع).
    /// </summary>
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
}
