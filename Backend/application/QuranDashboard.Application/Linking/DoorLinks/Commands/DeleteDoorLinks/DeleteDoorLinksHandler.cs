using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Commands.DeleteDoorLinks;

public sealed class DeleteDoorLinksHandler(IDoorLinkRecordsWriter writer)
{
    public async Task<DeleteDoorLinksOutcome> HandleAsync(
        DeleteDoorLinksCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var selection = command.Selection;
        if (command.DoorId <= 0
            || command.ExpectedDoorVersion == 0
            || command.ActorUserId <= 0
            || selection is null
            || !Enum.IsDefined(selection.Mode)
            || selection.UnitIds is null
            || selection.UnitIds.Any(unitId => unitId <= 0)
            || selection.UnitIds.Count != selection.UnitIds.Distinct().Count()
            || (selection.Mode == DoorLinkSelectionMode.Only && selection.UnitIds.Count == 0))
        {
            return new DeleteDoorLinksOutcome.InvalidRequest();
        }

        var result = await writer.DeleteAsync(
            command.DoorId,
            command.ExpectedDoorVersion,
            selection,
            command.ActorUserId,
            cancellationToken);

        return result switch
        {
            DoorLinkMutationWriteResult.Success success =>
                new DeleteDoorLinksOutcome.Success(success.Result),
            DoorLinkMutationWriteResult.DoorNotFound =>
                new DeleteDoorLinksOutcome.DoorNotFound(),
            DoorLinkMutationWriteResult.UnitNotFound =>
                new DeleteDoorLinksOutcome.UnitNotFound(),
            DoorLinkMutationWriteResult.DoorArchived =>
                new DeleteDoorLinksOutcome.DoorArchived(),
            DoorLinkMutationWriteResult.DoorVersionStale =>
                new DeleteDoorLinksOutcome.DoorVersionStale(),
            DoorLinkMutationWriteResult.InvalidWords =>
                new DeleteDoorLinksOutcome.InvalidRequest(),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(DoorLinkMutationWriteResult)} variant."),
        };
    }
}
