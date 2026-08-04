using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Commands.Relations.AddDoorRelations;
using QuranDashboard.Application.Abwab.Commands.Relations.DeleteDoorRelation;
using QuranDashboard.Application.Abwab.Queries.GetDoorRelations;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab")]
public sealed class AbwabDoorRelationsController(
    GetDoorRelationsHandler getRelationsHandler,
    AddDoorRelationsHandler addRelationsHandler,
    DeleteDoorRelationHandler deleteRelationHandler) : ControllerBase
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

    [HttpPost("doors/{doorId:int}/relations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>>> AddForDoor(
        int doorId, [FromBody] AddDoorRelationsBody body, CancellationToken cancellationToken)
    {
        var outcome = await addRelationsHandler.HandleAsync(
            new AddDoorRelationsCommand(doorId, body.Type, body.Direction, body.TargetDoorIds), cancellationToken);

        return outcome switch
        {
            AddDoorRelationsOutcome.Success success =>
                Created($"api/abwab/doors/{doorId}/relations",
                    ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Ok(success.Relations, ApiMessages.AbwabDoorRelationsCreated)),
            AddDoorRelationsOutcome.InvalidRequest =>
                BadRequest(ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Fail(ApiMessages.AbwabDoorRelationsInvalidRequest)),
            AddDoorRelationsOutcome.InvalidType =>
                BadRequest(ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Fail(ApiMessages.AbwabDoorRelationInvalidType)),
            AddDoorRelationsOutcome.InvalidDirection =>
                BadRequest(ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Fail(ApiMessages.AbwabDoorRelationInvalidDirection)),
            AddDoorRelationsOutcome.SelfRelation =>
                BadRequest(ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Fail(ApiMessages.AbwabDoorRelationSelf)),
            AddDoorRelationsOutcome.ArchivedDoor =>
                BadRequest(ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Fail(ApiMessages.AbwabDoorRelationArchivedDoor)),
            AddDoorRelationsOutcome.NotFound =>
                NotFound(ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Fail(ApiMessages.AbwabDoorNotFound)),
            AddDoorRelationsOutcome.Duplicate duplicate =>
                Conflict(ApiResponse<IReadOnlyList<AbwabDoorRelationDto>>.Fail(
                    ApiMessages.AbwabDoorRelationDuplicateWith(duplicate.DoorNames))),
            _ => throw new InvalidOperationException($"Unhandled {nameof(AddDoorRelationsOutcome)} variant."),
        };
    }

    [HttpDelete("relations/{relationId:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int relationId, CancellationToken cancellationToken)
    {
        var outcome = await deleteRelationHandler.HandleAsync(
            new DeleteDoorRelationCommand(relationId), cancellationToken);

        return outcome switch
        {
            DeleteDoorRelationOutcome.Success => NoContent(),
            DeleteDoorRelationOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorRelationNotFound)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(DeleteDoorRelationOutcome)} variant."),
        };
    }
}
