using QuranDashboard.Application.Abstractions.Linking;
using QuranDashboard.Application.Abstractions.Linking.DoorLinks;

namespace QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkSnapshot;

public sealed class GetDoorLinkSnapshotHandler(
    IDoorLinkRecordsReader reader,
    ILinkingDataRevisionReadScope revisionScope,
    ILinkingScalabilityPolicy policy)
{
    public async Task<GetDoorLinkSnapshotOutcome> HandleAsync(
        GetDoorLinkSnapshotQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.DoorId <= 0)
        {
            return new GetDoorLinkSnapshotOutcome.InvalidRequest();
        }

        try
        {
            return await revisionScope.ExecuteAsync<GetDoorLinkSnapshotOutcome>(
                policy.MaximumAutomaticAttempts,
                async (revision, token) => Map(await reader.ReadSnapshotAsync(
                    query.DoorId,
                    revision,
                    token)),
                cancellationToken);
        }
        catch (LinkingDataRevisionReadRetryExhaustedException)
        {
            return new GetDoorLinkSnapshotOutcome.TransientFailure();
        }
    }

    private static GetDoorLinkSnapshotOutcome Map(DoorLinkSnapshotReadResult result) => result switch
    {
        DoorLinkSnapshotReadResult.Success success =>
            new GetDoorLinkSnapshotOutcome.Success(success.Snapshot),
        DoorLinkSnapshotReadResult.DoorNotFound =>
            new GetDoorLinkSnapshotOutcome.DoorNotFound(),
        DoorLinkSnapshotReadResult.DoorArchived =>
            new GetDoorLinkSnapshotOutcome.DoorArchived(),
        _ => throw new InvalidOperationException(
            $"Unhandled {nameof(DoorLinkSnapshotReadResult)} variant."),
    };
}
