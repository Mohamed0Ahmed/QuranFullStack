namespace QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkRecords;

public sealed record GetDoorLinkRecordsQuery(
    int DoorId,
    uint? ExpectedDoorVersion,
    int Page,
    int PageSize);
