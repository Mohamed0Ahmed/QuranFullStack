using QuranDashboard.Application.Abstractions.Quran.Words.Roots.Responses;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootAyahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootLemmas;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMentionedSurahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootMissingSurahs;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootStems;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootSummary;
using QuranDashboard.Application.Quran.Words.Roots.Queries.GetRootWords;

namespace QuranDashboard.Api.Controllers.Words;

public sealed partial class RootsController
{
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

    [HttpGet("{id:int}/ayahs")]
    public async Task<ActionResult<ApiResponse<PagedResult<RootAyahMatchDto>>>> GetAyahs(
        int id,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? typeCode,
        CancellationToken cancellationToken)
    {
        var outcome = await ayahsHandler.HandleAsync(
            new GetRootAyahsQuery(
                id,
                page ?? DefaultPage,
                pageSize ?? DefaultDetailPageSize,
                NormalizeTypeCode(typeCode)),
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

    private static string? NormalizeTypeCode(string? typeCode) =>
        string.IsNullOrWhiteSpace(typeCode) ? null : typeCode.Trim();

    [HttpGet("{id:int}/words/{wordKind}")]
    public async Task<ActionResult<ApiResponse<PagedResult<RootWordItemDto>>>> GetWords(
        int id,
        string wordKind,
        [FromQuery] string? typeCode,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var outcome = await wordsHandler.HandleAsync(
            new GetRootWordsQuery(
                id,
                wordKind,
                NormalizeTypeCode(typeCode),
                page ?? DefaultPage,
                pageSize ?? DefaultDetailPageSize),
            cancellationToken);

        return outcome switch
        {
            GetRootWordsOutcome.Success success =>
                Ok(ApiResponse<PagedResult<RootWordItemDto>>.Ok(success.Page, ApiMessages.RootWordsLoaded)),
            GetRootWordsOutcome.InvalidId =>
                BadRequest(ApiResponse<PagedResult<RootWordItemDto>>.Fail(ApiMessages.RootsInvalidId)),
            GetRootWordsOutcome.InvalidKind =>
                BadRequest(ApiResponse<PagedResult<RootWordItemDto>>.Fail(ApiMessages.RootsInvalidKind)),
            GetRootWordsOutcome.InvalidPaging =>
                BadRequest(ApiResponse<PagedResult<RootWordItemDto>>.Fail(ApiMessages.RootsInvalidPaging)),
            GetRootWordsOutcome.NotFound =>
                NotFound(ApiResponse<PagedResult<RootWordItemDto>>.Fail(ApiMessages.RootNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetRootWordsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/surahs")]
    public async Task<ActionResult<ApiResponse<RootSurahsResponse>>> GetSurahs(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await mentionedSurahsHandler.HandleAsync(
            new GetRootMentionedSurahsQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetRootMentionedSurahsOutcome.Success success =>
                Ok(ApiResponse<RootSurahsResponse>.Ok(success.Surahs, ApiMessages.RootSurahsLoaded)),
            GetRootMentionedSurahsOutcome.InvalidId =>
                BadRequest(ApiResponse<RootSurahsResponse>.Fail(ApiMessages.RootsInvalidId)),
            GetRootMentionedSurahsOutcome.NotFound =>
                NotFound(ApiResponse<RootSurahsResponse>.Fail(ApiMessages.RootNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetRootMentionedSurahsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/missing-surahs")]
    public async Task<ActionResult<ApiResponse<RootMissingSurahsResponse>>> GetMissingSurahs(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await missingSurahsHandler.HandleAsync(
            new GetRootMissingSurahsQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetRootMissingSurahsOutcome.Success success =>
                Ok(ApiResponse<RootMissingSurahsResponse>.Ok(success.MissingSurahs, ApiMessages.RootMissingSurahsLoaded)),
            GetRootMissingSurahsOutcome.InvalidId =>
                BadRequest(ApiResponse<RootMissingSurahsResponse>.Fail(ApiMessages.RootsInvalidId)),
            GetRootMissingSurahsOutcome.NotFound =>
                NotFound(ApiResponse<RootMissingSurahsResponse>.Fail(ApiMessages.RootNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetRootMissingSurahsOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/lemmas")]
    public async Task<ActionResult<ApiResponse<RootLemmasResponse>>> GetLemmas(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await lemmasHandler.HandleAsync(
            new GetRootLemmasQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetRootLemmasOutcome.Success success =>
                Ok(ApiResponse<RootLemmasResponse>.Ok(success.Lemmas, ApiMessages.RootLemmasLoaded)),
            GetRootLemmasOutcome.InvalidId =>
                BadRequest(ApiResponse<RootLemmasResponse>.Fail(ApiMessages.RootsInvalidId)),
            GetRootLemmasOutcome.NotFound =>
                NotFound(ApiResponse<RootLemmasResponse>.Fail(ApiMessages.RootNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetRootLemmasOutcome)} variant."),
        };
    }

    [HttpGet("{id:int}/stems")]
    public async Task<ActionResult<ApiResponse<RootStemsResponse>>> GetStems(
        int id,
        CancellationToken cancellationToken)
    {
        var outcome = await stemsHandler.HandleAsync(
            new GetRootStemsQuery(id),
            cancellationToken);

        return outcome switch
        {
            GetRootStemsOutcome.Success success =>
                Ok(ApiResponse<RootStemsResponse>.Ok(success.Stems, ApiMessages.RootStemsLoaded)),
            GetRootStemsOutcome.InvalidId =>
                BadRequest(ApiResponse<RootStemsResponse>.Fail(ApiMessages.RootsInvalidId)),
            GetRootStemsOutcome.NotFound =>
                NotFound(ApiResponse<RootStemsResponse>.Fail(ApiMessages.RootNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetRootStemsOutcome)} variant."),
        };
    }
}
