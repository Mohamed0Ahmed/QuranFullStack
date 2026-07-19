using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetSimilarAyahs;

namespace QuranDashboard.Api.Controllers.MushafReader.Ayahs;

[ApiController]
[Route("api/mushaf/ayahs")]
public sealed class MushafAyahSimilaritiesController(GetSimilarAyahsHandler handler) : ControllerBase
{
    [HttpGet("{verseKey}/similar-ayahs")]
    public async Task<ActionResult<ApiResponse<SimilarAyahsResponse>>> GetSimilarAyahs(
        string verseKey,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(new GetSimilarAyahsQuery(verseKey), cancellationToken);

        return outcome switch
        {
            GetSimilarAyahsOutcome.Success success =>
                Ok(ApiResponse<SimilarAyahsResponse>.Ok(success.Response, ApiMessages.MushafSimilarAyahsLoaded)),
            GetSimilarAyahsOutcome.InvalidVerseKey =>
                BadRequest(ApiResponse<SimilarAyahsResponse>.Fail(ApiMessages.MushafInvalidVerseKey)),
            GetSimilarAyahsOutcome.NotFound =>
                NotFound(ApiResponse<SimilarAyahsResponse>.Fail(ApiMessages.NotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetSimilarAyahsOutcome)} variant."),
        };
    }
}
