using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Queries.GetDoorRelations;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab")]
public sealed class AbwabDoorRelationsController(GetDoorRelationsHandler getRelationsHandler) : ControllerBase
{
    [HttpGet("doors/{doorId:int}/relations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>>> GetForDoor(
        int doorId, CancellationToken cancellationToken)
    {
        var outcome = await getRelationsHandler.HandleAsync(new GetDoorRelationsQuery(doorId), cancellationToken);

        return outcome switch
        {
            GetDoorRelationsOutcome.Success success =>
                Ok(ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Ok(success.Relations, ApiMessages.AbwabDoorRelationsLoaded)),
            GetDoorRelationsOutcome.NotFound =>
                NotFound(ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Fail(ApiMessages.AbwabDoorNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(GetDoorRelationsOutcome)} variant."),
        };
    }
}
