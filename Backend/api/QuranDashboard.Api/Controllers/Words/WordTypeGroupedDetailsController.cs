using QuranDashboard.Application.Abstractions.Common.Paging;
using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSummary;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedWords;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedAyahs;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSurahs;

namespace QuranDashboard.Api.Controllers.Words;

// Scoped root/stem/lemma grouped detail reads. Shares the existing table route base without growing
// WordTypesController. Route kind is the plural key (roots|stems|lemmas); an unknown value is a 400.
[ApiController]
[Route("api/words/word-types/table")]
public sealed class WordTypeGroupedDetailsController(
    GetWordTypeGroupedSummaryHandler summaryHandler,
    GetWordTypeGroupedWordsHandler wordsHandler,
    GetWordTypeGroupedAyahsHandler ayahsHandler,
    GetWordTypeGroupedSurahsHandler surahsHandler) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultDetailPageSize = 25;

    [HttpGet("{kind}/{dimensionId:int}")]
    public async Task<ActionResult<ApiResponse<WordTypeGroupedSummaryDto>>> GetSummary(
        string kind,
        int dimensionId,
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        CancellationToken cancellationToken)
    {
        var outcome = await summaryHandler.HandleAsync(
            new GetWordTypeGroupedSummaryQuery(kind, dimensionId, type, childCode, caseFilter, tense, voice),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeGroupedSummaryOutcome.Success success =>
                Ok(ApiResponse<WordTypeGroupedSummaryDto>.Ok(success.Summary, ApiMessages.WordTypeGroupedSummaryLoaded)),
            GetWordTypeGroupedSummaryOutcome.InvalidKind =>
                BadRequest(ApiResponse<WordTypeGroupedSummaryDto>.Fail(ApiMessages.WordTypesInvalidGroupedKind)),
            GetWordTypeGroupedSummaryOutcome.InvalidId =>
                BadRequest(ApiResponse<WordTypeGroupedSummaryDto>.Fail(ApiMessages.WordTypesInvalidGroupedId)),
            GetWordTypeGroupedSummaryOutcome.InvalidFilter =>
                BadRequest(ApiResponse<WordTypeGroupedSummaryDto>.Fail(ApiMessages.WordTypesInvalidFilter)),
            GetWordTypeGroupedSummaryOutcome.NotFound =>
                NotFound(ApiResponse<WordTypeGroupedSummaryDto>.Fail(ApiMessages.WordTypesGroupedNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeGroupedSummaryOutcome)} variant."),
        };
    }

    // Paged, display-only member words of the scoped group. Accepts the identical five-field scope plus
    // page/pageSize; no sort (member order is the fixed occurrence order).
    [HttpGet("{kind}/{dimensionId:int}/words")]
    public async Task<ActionResult<ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>>> GetWords(
        string kind,
        int dimensionId,
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await wordsHandler.HandleAsync(
            new GetWordTypeGroupedWordsQuery(
                kind, dimensionId, type, childCode, caseFilter, tense, voice,
                page ?? DefaultPage, pageSize ?? DefaultDetailPageSize),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeGroupedWordsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Ok(success.Page, ApiMessages.WordTypeGroupedWordsLoaded)),
            GetWordTypeGroupedWordsOutcome.InvalidKind =>
                BadRequest(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesInvalidGroupedKind)),
            GetWordTypeGroupedWordsOutcome.InvalidId =>
                BadRequest(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesInvalidGroupedId)),
            GetWordTypeGroupedWordsOutcome.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesInvalidFilter)),
            GetWordTypeGroupedWordsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            GetWordTypeGroupedWordsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<WordTypeGroupedMemberWordDto>>.Fail(ApiMessages.WordTypesGroupedNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeGroupedWordsOutcome)} variant."),
        };
    }

    // Paged, scoped ayahs of the group. Accepts the identical five-field scope plus page/pageSize; no sort.
    // Ayahs page in Mushaf order and carry canonical Uthmani text with the scoped matched word ids/positions.
    [HttpGet("{kind}/{dimensionId:int}/ayahs")]
    public async Task<ActionResult<ApiResponse<PagedResult<WordTypeAyahMatchDto>>>> GetAyahs(
        string kind,
        int dimensionId,
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await ayahsHandler.HandleAsync(
            new GetWordTypeGroupedAyahsQuery(
                kind, dimensionId, type, childCode, caseFilter, tense, voice,
                page ?? DefaultPage, pageSize ?? DefaultDetailPageSize),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeGroupedAyahsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Ok(success.Page, ApiMessages.WordTypeGroupedAyahsLoaded)),
            GetWordTypeGroupedAyahsOutcome.InvalidKind =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidGroupedKind)),
            GetWordTypeGroupedAyahsOutcome.InvalidId =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidGroupedId)),
            GetWordTypeGroupedAyahsOutcome.InvalidFilter =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidFilter)),
            GetWordTypeGroupedAyahsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesInvalidPaging)),
            GetWordTypeGroupedAyahsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<WordTypeAyahMatchDto>>.Fail(ApiMessages.WordTypesGroupedNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeGroupedAyahsOutcome)} variant."),
        };
    }

    // Single-shot scoped surahs: mentioned + missing lists for the group. Accepts the identical five-field
    // scope and never page/pageSize — surahs are not paged.
    [HttpGet("{kind}/{dimensionId:int}/surahs")]
    public async Task<ActionResult<ApiResponse<WordTypeSurahsResponse>>> GetSurahs(
        string kind,
        int dimensionId,
        [FromQuery] string? type,
        [FromQuery] string? childCode,
        [FromQuery(Name = "case")] string? caseFilter,
        [FromQuery] string? tense,
        [FromQuery] string? voice,
        CancellationToken cancellationToken)
    {
        var outcome = await surahsHandler.HandleAsync(
            new GetWordTypeGroupedSurahsQuery(kind, dimensionId, type, childCode, caseFilter, tense, voice),
            cancellationToken);

        return outcome switch
        {
            GetWordTypeGroupedSurahsOutcome.Success success =>
                Ok(ApiResponse<WordTypeSurahsResponse>.Ok(success.Surahs, ApiMessages.WordTypeGroupedSurahsLoaded)),
            GetWordTypeGroupedSurahsOutcome.InvalidKind =>
                BadRequest(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesInvalidGroupedKind)),
            GetWordTypeGroupedSurahsOutcome.InvalidId =>
                BadRequest(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesInvalidGroupedId)),
            GetWordTypeGroupedSurahsOutcome.InvalidFilter =>
                BadRequest(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesInvalidFilter)),
            GetWordTypeGroupedSurahsOutcome.NotFound =>
                NotFound(ApiResponse<WordTypeSurahsResponse>.Fail(ApiMessages.WordTypesGroupedNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordTypeGroupedSurahsOutcome)} variant."),
        };
    }
}
