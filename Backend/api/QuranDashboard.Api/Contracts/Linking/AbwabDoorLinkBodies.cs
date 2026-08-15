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

public sealed record ReplaceAbwabDoorLinkWordsBody
{
    public uint? ExpectedDoorVersion { get; init; }
    public IReadOnlyList<AbwabDoorLinkSelectedWordBody?>? SelectedWords { get; init; }
}

public sealed record AbwabDoorLinkSelectedWordBody
{
    public int AyahId { get; init; }
    public int QuranWordId { get; init; }
}

public sealed record DeleteAbwabDoorLinksBody
{
    public uint? ExpectedDoorVersion { get; init; }
    public string? SelectionMode { get; init; }
    public IReadOnlyList<long>? UnitIds { get; init; }
}

internal static class AbwabDoorLinkConflictCodes
{
    internal const string DoorArchived = "DOOR_ARCHIVED";
    internal const string DoorLinksStale = "DOOR_LINKS_STALE";
    internal const string LinkingDataStale = "LINKING_DATA_STALE";
}
