using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Commands.ReplaceDoorLinkWords;

public sealed class ReplaceDoorLinkWordsHandler(IDoorLinkRecordsWriter writer)
{
    public async Task<ReplaceDoorLinkWordsOutcome> HandleAsync(
        ReplaceDoorLinkWordsCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.DoorId <= 0
            || command.UnitId <= 0
            || command.ExpectedDoorVersion == 0
            || command.ActorUserId <= 0
            || command.SelectedWords is null
            || command.SelectedWords.Any(word => word.AyahId <= 0 || word.QuranWordId <= 0)
            || command.SelectedWords.Count != command.SelectedWords.Distinct().Count())
        {
            return new ReplaceDoorLinkWordsOutcome.InvalidRequest();
        }

        var result = await writer.ReplaceWordsAsync(
            command.DoorId,
            command.UnitId,
            command.ExpectedDoorVersion,
            command.SelectedWords,
            command.ActorUserId,
            cancellationToken);

        return result switch
        {
            DoorLinkMutationWriteResult.Success success =>
                new ReplaceDoorLinkWordsOutcome.Success(success.Result),
            DoorLinkMutationWriteResult.DoorNotFound =>
                new ReplaceDoorLinkWordsOutcome.DoorNotFound(),
            DoorLinkMutationWriteResult.UnitNotFound =>
                new ReplaceDoorLinkWordsOutcome.UnitNotFound(),
            DoorLinkMutationWriteResult.DoorArchived =>
                new ReplaceDoorLinkWordsOutcome.DoorArchived(),
            DoorLinkMutationWriteResult.DoorVersionStale =>
                new ReplaceDoorLinkWordsOutcome.DoorVersionStale(),
            DoorLinkMutationWriteResult.InvalidWords =>
                new ReplaceDoorLinkWordsOutcome.InvalidRequest(),
            DoorLinkMutationWriteResult.SynchronizationUnavailable =>
                new ReplaceDoorLinkWordsOutcome.SynchronizationUnavailable(),
            _ => throw new InvalidOperationException(
                $"Unhandled {nameof(DoorLinkMutationWriteResult)} variant."),
        };
    }
}
