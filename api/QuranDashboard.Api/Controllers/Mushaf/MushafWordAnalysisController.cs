using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetWordAnalysis;

namespace QuranDashboard.Api.Controllers.Mushaf;

[ApiController]
[Route("api/mushaf/words")]
public sealed class MushafWordAnalysisController(GetWordAnalysisHandler handler) : ControllerBase
{
    [HttpGet("{wordLocation}/analysis")]
    public async Task<ActionResult<ApiResponse<WordAnalysisResponse>>> GetAnalysis(
        string wordLocation,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(
            new GetWordAnalysisQuery(wordLocation),
            cancellationToken);

        return outcome switch
        {
            GetWordAnalysisOutcome.Success success =>
                Ok(ApiResponse<WordAnalysisResponse>.Ok(success.Response, ApiMessages.MushafWordAnalysisLoaded)),
            GetWordAnalysisOutcome.InvalidWordLocation =>
                BadRequest(ApiResponse<WordAnalysisResponse>.Fail(ApiMessages.MushafInvalidWordLocation)),
            GetWordAnalysisOutcome.NotAnalyzable =>
                BadRequest(ApiResponse<WordAnalysisResponse>.Fail(ApiMessages.MushafWordNotAnalyzable)),
            GetWordAnalysisOutcome.NotFound =>
                NotFound(ApiResponse<WordAnalysisResponse>.Fail(ApiMessages.NotFound)),
            GetWordAnalysisOutcome.IncompleteData =>
                NotFound(ApiResponse<WordAnalysisResponse>.Fail(ApiMessages.MushafWordAnalysisIncomplete)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetWordAnalysisOutcome)} variant."),
        };
    }
}
