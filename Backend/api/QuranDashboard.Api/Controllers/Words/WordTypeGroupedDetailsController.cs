using QuranDashboard.Application.Abstractions.Quran.Words.WordTypes.Responses;
using QuranDashboard.Application.Quran.Words.WordTypes.Queries.GetWordTypeGroupedSummary;

namespace QuranDashboard.Api.Controllers.Words;

// Scoped root/stem/lemma grouped detail reads. Shares the existing table route base without growing
// WordTypesController. Route kind is the plural key (roots|stems|lemmas); an unknown value is a 400.
[ApiController]
[Route("api/words/word-types/table")]
public sealed class WordTypeGroupedDetailsController(
    GetWordTypeGroupedSummaryHandler summaryHandler) : ControllerBase
{
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
}
