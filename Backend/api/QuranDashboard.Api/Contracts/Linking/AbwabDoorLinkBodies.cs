namespace QuranDashboard.Api.Contracts.Linking;

public sealed record AbwabDoorLinkRecordsQueryBody
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public uint? ExpectedDoorVersion { get; init; }
}

public sealed record AbwabDoorLinkAyahsQueryBody
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public uint? ExpectedDoorVersion { get; init; }
    public long? ExpectedLinkingDataRevision { get; init; }
}

internal static class AbwabDoorLinkConflictCodes
{
    internal const string DoorArchived = "DOOR_ARCHIVED";
    internal const string DoorLinksStale = "DOOR_LINKS_STALE";
    internal const string LinkingDataStale = "LINKING_DATA_STALE";
}
