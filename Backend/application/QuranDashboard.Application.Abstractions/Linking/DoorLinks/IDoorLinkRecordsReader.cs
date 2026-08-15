namespace QuranDashboard.Application.Abstractions.Linking.DoorLinks;

public interface IDoorLinkRecordsReader
{
    Task<DoorLinkSnapshotReadResult> ReadSnapshotAsync(
        int doorId,
        long linkingDataRevision,
        CancellationToken cancellationToken);

    Task<DoorLinkRecordsReadResult> ReadRecordsAsync(
        int doorId,
        uint? expectedDoorVersion,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DoorLinkAyahsReadResult> ReadAyahsAsync(
        int doorId,
        long unitId,
        uint expectedDoorVersion,
        long linkingDataRevision,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
