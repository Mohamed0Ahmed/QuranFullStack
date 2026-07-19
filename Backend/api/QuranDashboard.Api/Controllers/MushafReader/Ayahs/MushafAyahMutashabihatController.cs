using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetAyahMutashabihat;

namespace QuranDashboard.Api.Controllers.MushafReader.Ayahs;

[ApiController]
[Route("api/mushaf/ayahs")]
public sealed class MushafAyahMutashabihatController(GetAyahMutashabihatHandler handler) : ControllerBase
{
    [HttpGet("{verseKey}/mutashabihat")]
    public async Task<ActionResult<ApiResponse<AyahMutashabihatResponse>>> GetAyahMutashabihat(
        string verseKey,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(new GetAyahMutashabihatQuery(verseKey), cancellationToken);

        return outcome switch
        {
            GetAyahMutashabihatOutcome.Success success =>
                Ok(ApiResponse<AyahMutashabihatResponse>.Ok(success.Response, ApiMessages.MushafAyahMutashabihatLoaded)),
            GetAyahMutashabihatOutcome.InvalidVerseKey =>
                BadRequest(ApiResponse<AyahMutashabihatResponse>.Fail(ApiMessages.MushafInvalidVerseKey)),
            GetAyahMutashabihatOutcome.NotFound =>
                NotFound(ApiResponse<AyahMutashabihatResponse>.Fail(ApiMessages.NotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetAyahMutashabihatOutcome)} variant."),
        };
    }
}
