using QuranDashboard.Application.Abstractions.Abwab.Responses;
using QuranDashboard.Application.Abwab.Commands.Doors.CreateDoor;
using QuranDashboard.Application.Abwab.Commands.Doors.EditDoor;
using QuranDashboard.Application.Abwab.Commands.Doors.MoveDoor;
using QuranDashboard.Application.Abwab.Commands.Doors.ReorderDoor;
using QuranDashboard.Application.Abwab.Commands.Doors.BulkMoveDoors;
using QuranDashboard.Application.Abwab.Commands.Doors.BulkArchiveDoors;
using QuranDashboard.Application.Abwab.Commands.Doors.DeleteDoor;
using QuranDashboard.Application.Abwab.Commands.Doors.RestoreDoor;

namespace QuranDashboard.Api.Controllers.Abwab;

[ApiController]
[Route("api/abwab/doors")]
public sealed class AbwabDoorsController(
    CreateDoorHandler createHandler,
    EditDoorHandler editHandler,
    MoveDoorHandler moveHandler,
    ReorderDoorHandler reorderHandler,
    BulkMoveDoorsHandler bulkMoveHandler,
    BulkArchiveDoorsHandler bulkArchiveHandler,
    DeleteDoorHandler deleteHandler,
    RestoreDoorHandler restoreHandler) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AbwabDoorDto>>> Create(
        [FromBody] CreateDoorCommand command, CancellationToken cancellationToken)
    {
        var outcome = await createHandler.HandleAsync(command, cancellationToken);

        return outcome switch
        {
            CreateDoorOutcome.Success success =>
                Created($"api/abwab/doors/{success.Door.Id}", ApiResponse<AbwabDoorDto>.Ok(success.Door, ApiMessages.AbwabDoorCreated)),
            CreateDoorOutcome.InvalidName =>
                BadRequest(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorInvalidName)),
            CreateDoorOutcome.ParentNotFound =>
                NotFound(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorParentNotFound)),
            CreateDoorOutcome.SectionNotFound =>
                NotFound(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorSectionNotFound)),
            CreateDoorOutcome.DuplicateName =>
                Conflict(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorDuplicateName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(CreateDoorOutcome)} variant."),
        };
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AbwabDoorDto>>> Edit(
        int id, [FromBody] EditDoorBody body, CancellationToken cancellationToken)
    {
        var outcome = await editHandler.HandleAsync(
            new EditDoorCommand(id, body.Name, body.Description, body.RepresentativeAyahText, body.Aliases, body.Version),
            cancellationToken);

        return outcome switch
        {
            EditDoorOutcome.Success success =>
                Ok(ApiResponse<AbwabDoorDto>.Ok(success.Door, ApiMessages.AbwabDoorEdited)),
            EditDoorOutcome.InvalidName =>
                BadRequest(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorInvalidName)),
            EditDoorOutcome.NotFound =>
                NotFound(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorNotFound)),
            EditDoorOutcome.StaleVersion =>
                Conflict(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorStaleVersion)),
            EditDoorOutcome.DuplicateName =>
                Conflict(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorDuplicateName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(EditDoorOutcome)} variant."),
        };
    }

    [HttpPost("{id:int}/move")]
    public async Task<ActionResult<ApiResponse<AbwabDoorDto>>> Move(
        int id, [FromBody] MoveDoorBody body, CancellationToken cancellationToken)
    {
        var outcome = await moveHandler.HandleAsync(
            new MoveDoorCommand(id, body.TargetSectionId, body.TargetParentId, body.Version), cancellationToken);

        return outcome switch
        {
            MoveDoorOutcome.Success success =>
                Ok(ApiResponse<AbwabDoorDto>.Ok(success.Door, ApiMessages.AbwabDoorMoved)),
            MoveDoorOutcome.NotFound =>
                NotFound(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorNotFound)),
            MoveDoorOutcome.ParentNotFound =>
                NotFound(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorParentNotFound)),
            MoveDoorOutcome.SectionNotFound =>
                NotFound(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorSectionNotFound)),
            MoveDoorOutcome.WouldCycle =>
                Conflict(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorWouldCycle)),
            MoveDoorOutcome.StaleVersion =>
                Conflict(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorStaleVersion)),
            MoveDoorOutcome.DuplicateName =>
                Conflict(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorDuplicateName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(MoveDoorOutcome)} variant."),
        };
    }

    [HttpPost("{id:int}/order")]
    public async Task<ActionResult<ApiResponse<AbwabDoorDto>>> Reorder(
        int id, [FromBody] ReorderDoorBody body, CancellationToken cancellationToken)
    {
        var outcome = await reorderHandler.HandleAsync(new ReorderDoorCommand(id, body.Position, body.Version), cancellationToken);

        return outcome switch
        {
            ReorderDoorOutcome.Success success =>
                Ok(ApiResponse<AbwabDoorDto>.Ok(success.Door, ApiMessages.AbwabDoorReordered)),
            ReorderDoorOutcome.NotFound =>
                NotFound(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorNotFound)),
            ReorderDoorOutcome.InvalidPosition =>
                BadRequest(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorInvalidPosition)),
            ReorderDoorOutcome.StaleVersion =>
                Conflict(ApiResponse<AbwabDoorDto>.Fail(ApiMessages.AbwabDoorStaleVersion)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(ReorderDoorOutcome)} variant."),
        };
    }

    [HttpPost("bulk-move")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AbwabDoorDto>>>> BulkMove(
        [FromBody] BulkMoveDoorsCommand command, CancellationToken cancellationToken)
    {
        var outcome = await bulkMoveHandler.HandleAsync(command, cancellationToken);

        return outcome switch
        {
            BulkMoveDoorsOutcome.Success success =>
                Ok(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Ok(success.Doors, ApiMessages.AbwabDoorsBulkMoved)),
            BulkMoveDoorsOutcome.InvalidRequest =>
                BadRequest(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabDoorsBulkInvalidRequest)),
            BulkMoveDoorsOutcome.NotFound =>
                NotFound(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabDoorNotFound)),
            BulkMoveDoorsOutcome.ParentNotFound =>
                NotFound(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabDoorParentNotFound)),
            BulkMoveDoorsOutcome.SectionNotFound =>
                NotFound(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabDoorSectionNotFound)),
            BulkMoveDoorsOutcome.WouldCycle =>
                Conflict(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabDoorWouldCycle)),
            BulkMoveDoorsOutcome.StaleVersion =>
                Conflict(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabDoorStaleVersion)),
            BulkMoveDoorsOutcome.DuplicateName =>
                Conflict(ApiResponse<IReadOnlyList<AbwabDoorDto>>.Fail(ApiMessages.AbwabDoorDuplicateName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(BulkMoveDoorsOutcome)} variant."),
        };
    }

    [HttpPost("bulk-archive")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<int>>>> BulkArchive(
        [FromBody] BulkArchiveDoorsCommand command, CancellationToken cancellationToken)
    {
        var outcome = await bulkArchiveHandler.HandleAsync(command, cancellationToken);

        return outcome switch
        {
            BulkArchiveDoorsOutcome.Success success =>
                Ok(ApiResponse<IReadOnlyList<int>>.Ok(success.ArchivedDoorIds, ApiMessages.AbwabDoorsBulkArchived)),
            BulkArchiveDoorsOutcome.InvalidRequest =>
                BadRequest(ApiResponse<IReadOnlyList<int>>.Fail(ApiMessages.AbwabDoorsBulkInvalidRequest)),
            BulkArchiveDoorsOutcome.NotFound =>
                NotFound(ApiResponse<IReadOnlyList<int>>.Fail(ApiMessages.AbwabDoorNotFound)),
            BulkArchiveDoorsOutcome.StaleVersion =>
                Conflict(ApiResponse<IReadOnlyList<int>>.Fail(ApiMessages.AbwabDoorStaleVersion)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(BulkArchiveDoorsOutcome)} variant."),
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        int id, [FromBody] DeleteDoorBody body, CancellationToken cancellationToken)
    {
        var outcome = await deleteHandler.HandleAsync(new DeleteDoorCommand(id, body.Version), cancellationToken);

        return outcome switch
        {
            DeleteDoorOutcome.Success => NoContent(),
            DeleteDoorOutcome.NotFound =>
                NotFound(ApiResponse<object>.Fail(ApiMessages.AbwabDoorNotFound)),
            DeleteDoorOutcome.StaleVersion =>
                Conflict(ApiResponse<object>.Fail(ApiMessages.AbwabDoorStaleVersion)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(DeleteDoorOutcome)} variant."),
        };
    }

    [HttpPost("{id:int}/restore")]
    public async Task<ActionResult<ApiResponse<AbwabRestoredDoorDto>>> Restore(
        int id, [FromBody] RestoreDoorBody body, CancellationToken cancellationToken)
    {
        var outcome = await restoreHandler.HandleAsync(new RestoreDoorCommand(id, body.Version), cancellationToken);

        return outcome switch
        {
            RestoreDoorOutcome.Success success =>
                Ok(ApiResponse<AbwabRestoredDoorDto>.Ok(
                    new AbwabRestoredDoorDto(success.Door, success.DetachedFromArchivedSection),
                    ApiMessages.AbwabDoorRestored)),
            RestoreDoorOutcome.NotFound =>
                NotFound(ApiResponse<AbwabRestoredDoorDto>.Fail(ApiMessages.AbwabDoorNotFound)),
            RestoreDoorOutcome.StaleVersion =>
                Conflict(ApiResponse<AbwabRestoredDoorDto>.Fail(ApiMessages.AbwabDoorStaleVersion)),
            RestoreDoorOutcome.ParentStillArchived =>
                Conflict(ApiResponse<AbwabRestoredDoorDto>.Fail(ApiMessages.AbwabDoorParentStillArchived)),
            RestoreDoorOutcome.DuplicateName =>
                Conflict(ApiResponse<AbwabRestoredDoorDto>.Fail(ApiMessages.AbwabDoorDuplicateName)),
            _ => throw new InvalidOperationException($"Unhandled {nameof(RestoreDoorOutcome)} variant."),
        };
    }
}
