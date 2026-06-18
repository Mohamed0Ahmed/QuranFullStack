using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahStudy;

namespace QuranDashboard.Api.Controllers.Mushaf;

[ApiController]
[Route("api/mushaf/ayahs")]
public sealed class MushafAyahStudyController(GetAyahStudyHandler handler) : ControllerBase
{
    [HttpGet("{verseKey}/study")]
    public async Task<ActionResult<ApiResponse<AyahStudyResponse>>> GetStudy(
        string verseKey,
        [FromQuery] string? tafsirSource,
        [FromQuery] string? translationSource,
        [FromQuery] string? fullI3rabSource,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(
            new GetAyahStudyQuery(verseKey, tafsirSource, translationSource, fullI3rabSource),
            cancellationToken);

        return outcome switch
        {
            GetAyahStudyOutcome.Success success =>
                Ok(ApiResponse<AyahStudyResponse>.Ok(success.Response, ApiMessages.MushafAyahStudyLoaded)),
            GetAyahStudyOutcome.InvalidVerseKey =>
                BadRequest(ApiResponse<AyahStudyResponse>.Fail(ApiMessages.MushafInvalidVerseKey)),
            GetAyahStudyOutcome.NotFound =>
                NotFound(ApiResponse<AyahStudyResponse>.Fail(ApiMessages.NotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetAyahStudyOutcome)} variant."),
        };
    }
}
