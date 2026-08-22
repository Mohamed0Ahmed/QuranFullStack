using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafAyahDoors;

namespace QuranDashboard.Api.Controllers.MushafReader.Ayahs;

[ApiController]
[Route("api/mushaf/ayahs")]
public sealed class MushafAyahDoorsController(GetMushafAyahDoorsHandler handler) : ControllerBase
{
    [HttpGet("{verseKey}/doors")]
    [ProducesResponseType(typeof(ApiResponse<MushafAyahDoorsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MushafAyahDoorsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<MushafAyahDoorsResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MushafAyahDoorsResponse>>> GetDoors(
        string verseKey,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(new GetMushafAyahDoorsQuery(verseKey), cancellationToken);

        return outcome switch
        {
            GetMushafAyahDoorsOutcome.Success success =>
                Ok(ApiResponse<MushafAyahDoorsResponse>.Ok(
                    success.Response,
                    ApiMessages.MushafAyahDoorsLoaded)),
            GetMushafAyahDoorsOutcome.InvalidVerseKey =>
                BadRequest(ApiResponse<MushafAyahDoorsResponse>.Fail(ApiMessages.MushafInvalidVerseKey)),
            GetMushafAyahDoorsOutcome.NotFound =>
                NotFound(ApiResponse<MushafAyahDoorsResponse>.Fail(ApiMessages.NotFound)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(GetMushafAyahDoorsOutcome)} variant."),
        };
    }
}
