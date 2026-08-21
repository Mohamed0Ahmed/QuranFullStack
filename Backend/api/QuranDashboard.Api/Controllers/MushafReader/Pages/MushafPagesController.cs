using System.Globalization;
using QuranDashboard.Application.Abstractions.Quran.MushafReader.Responses;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafDoorHighlights;
using QuranDashboard.Application.Quran.MushafReader.Queries.GetMushafPage;

namespace QuranDashboard.Api.Controllers.MushafReader.Pages;

[ApiController]
[Route("api/mushaf/pages")]
public sealed class MushafPagesController(
    GetMushafPageHandler pageHandler,
    GetMushafDoorHighlightsHandler doorHighlightsHandler) : ControllerBase
{
    [HttpGet("{pageNumber}")]
    public async Task<ActionResult<ApiResponse<MushafPageResponse>>> Get(
        string pageNumber,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(pageNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPage))
        {
            return BadRequest(ApiResponse<MushafPageResponse>.Fail(ApiMessages.MushafInvalidPageNumber));
        }

        var outcome = await pageHandler.HandleAsync(new GetMushafPageQuery(parsedPage), cancellationToken);

        return outcome switch
        {
            GetMushafPageOutcome.Success success =>
                Ok(ApiResponse<MushafPageResponse>.Ok(success.Response, ApiMessages.MushafPageLoaded)),
            GetMushafPageOutcome.InvalidPageNumber =>
                BadRequest(ApiResponse<MushafPageResponse>.Fail(ApiMessages.MushafInvalidPageNumber)),
            GetMushafPageOutcome.NotFound =>
                NotFound(ApiResponse<MushafPageResponse>.Fail(ApiMessages.NotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetMushafPageOutcome)} variant."),
        };
    }

    [HttpGet("{pageNumber}/door-highlights")]
    [ProducesResponseType(typeof(ApiResponse<MushafDoorHighlightsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MushafDoorHighlightsResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MushafDoorHighlightsResponse>>> GetDoorHighlights(
        string pageNumber,
        [FromQuery(Name = "doorIds")] int[]? doorIds,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(pageNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPage))
        {
            return BadRequest(ApiResponse<MushafDoorHighlightsResponse>.Fail(ApiMessages.MushafInvalidPageNumber));
        }

        var outcome = await doorHighlightsHandler.HandleAsync(
            new GetMushafDoorHighlightsQuery(parsedPage, doorIds ?? []),
            cancellationToken);

        return outcome switch
        {
            GetMushafDoorHighlightsOutcome.Success success =>
                Ok(ApiResponse<MushafDoorHighlightsResponse>.Ok(
                    success.Response,
                    ApiMessages.MushafDoorHighlightsLoaded)),
            GetMushafDoorHighlightsOutcome.InvalidPageNumber =>
                BadRequest(ApiResponse<MushafDoorHighlightsResponse>.Fail(ApiMessages.MushafInvalidPageNumber)),
            GetMushafDoorHighlightsOutcome.InvalidDoorIds =>
                BadRequest(ApiResponse<MushafDoorHighlightsResponse>.Fail(ApiMessages.MushafInvalidDoorIds)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(GetMushafDoorHighlightsOutcome)} variant."),
        };
    }
}
