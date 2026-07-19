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
