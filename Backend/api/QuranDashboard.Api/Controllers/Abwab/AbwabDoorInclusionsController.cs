using QuranDashboard.Api.Authorization;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Contracts.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Inclusions;
using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Application.Abwab.Commands.AddDoorInclusions;
using QuranDashboard.Application.Abwab.Commands.DeleteDoorInclusion;
using QuranDashboard.Application.Abwab.Queries.GetDoorInclusions;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab/doors/{targetDoorId:int}/inclusions")]
public sealed class AbwabDoorInclusionsController(
    AuthorizationStateAccessEvaluator stateEvaluator,
    GetDoorInclusionsHandler getHandler,
    AddDoorInclusionsHandler addHandler,
    DeleteDoorInclusionHandler deleteHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<AbwabDoorInclusionTopologyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        int targetDoorId,
        CancellationToken cancellationToken)
    {
        var outcome = await getHandler.HandleAsync(
            new GetDoorInclusionsQuery(targetDoorId),
            cancellationToken);

        return outcome switch
        {
            GetDoorInclusionsOutcome.Success success =>
                Ok(ApiResponse<AbwabDoorInclusionTopologyDto>.Ok(
                    success.Topology,
                    ApiMessages.AbwabDoorInclusionsLoaded)),
            GetDoorInclusionsOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsInvalidRequest)),
            GetDoorInclusionsOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorNotFound)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(GetDoorInclusionsOutcome)} variant."),
        };
    }

    [HttpPost]
    [RequirePermission(AbwabPermissions.Inclusions.Create)]
    [ProducesResponseType(typeof(ApiResponse<AbwabDoorInclusionAddResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Add(
        int targetDoorId,
        [FromBody] AddAbwabDoorInclusionsBody? body,
        CancellationToken cancellationToken)
    {
        if (body is null || body.SourceDoorIds is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsInvalidRequest));
        }

        var actorUserId = await ResolveUserIdAsync();
        var outcome = await addHandler.HandleAsync(
            new AddDoorInclusionsCommand(
                targetDoorId,
                body.ExpectedTargetDoorVersion,
                body.SourceDoorIds,
                actorUserId),
            cancellationToken);

        return outcome switch
        {
            AddDoorInclusionsOutcome.Success success =>
                Created(
                    $"/api/abwab/doors/{targetDoorId}/inclusions",
                    ApiResponse<AbwabDoorInclusionAddResultDto>.Ok(
                        success.Result,
                        ApiMessages.AbwabDoorInclusionsCreated)),
            AddDoorInclusionsOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsInvalidRequest)),
            AddDoorInclusionsOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorNotFound)),
            AddDoorInclusionsOutcome.ArchivedDoor =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsArchivedDoor)),
            AddDoorInclusionsOutcome.Duplicate =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsDuplicate)),
            AddDoorInclusionsOutcome.Cycle =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsCycle)),
            AddDoorInclusionsOutcome.StaleTargetVersion =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsStaleTarget)),
            AddDoorInclusionsOutcome.SynchronizationUnavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsUnavailable)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(AddDoorInclusionsOutcome)} variant."),
        };
    }

    [HttpDelete("{inclusionId:int}")]
    [RequirePermission(AbwabPermissions.Inclusions.Delete)]
    [ProducesResponseType(typeof(ApiResponse<AbwabDoorInclusionDetachResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Delete(
        int targetDoorId,
        int inclusionId,
        [FromBody] DeleteAbwabDoorInclusionBody? body,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsInvalidRequest));
        }

        var actorUserId = await ResolveUserIdAsync();
        var outcome = await deleteHandler.HandleAsync(
            new DeleteDoorInclusionCommand(
                targetDoorId,
                inclusionId,
                body.ExpectedTargetDoorVersion,
                actorUserId),
            cancellationToken);

        return outcome switch
        {
            DeleteDoorInclusionOutcome.Success success =>
                Ok(ApiResponse<AbwabDoorInclusionDetachResultDto>.Ok(
                    success.Result,
                    ApiMessages.AbwabDoorInclusionDetached)),
            DeleteDoorInclusionOutcome.InvalidRequest =>
                BadRequest(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsInvalidRequest)),
            DeleteDoorInclusionOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionNotFound)),
            DeleteDoorInclusionOutcome.ArchivedTarget =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionArchivedTarget)),
            DeleteDoorInclusionOutcome.StaleTargetVersion =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsStaleTarget)),
            DeleteDoorInclusionOutcome.SynchronizationUnavailable =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.Fail(ApiMessages.AbwabDoorInclusionsUnavailable)),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(DeleteDoorInclusionOutcome)} variant."),
        };
    }

    private async Task<int> ResolveUserIdAsync()
    {
        var state = await stateEvaluator.ResolveActiveStateAsync(User);
        return state?.UserId
            ?? throw new InvalidOperationException(
                "An authorized Abwab inclusion request resolved no authorization state.");
    }
}
