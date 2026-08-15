namespace QuranDashboard.Application.Linking.DoorLinks.Queries.GetDoorLinkAyahs;

public sealed record GetDoorLinkAyahsQuery(
    int DoorId,
    long UnitId,
    uint? ExpectedDoorVersion,
    long? ExpectedLinkingDataRevision,
    int Page,
    int PageSize);
