using QuranDashboard.Application.Abstractions.Linking.Responses;

namespace QuranDashboard.Application.Abstractions.Linking.DoorLinks;

public sealed record DoorLinkRecordsPageDto(
    int DoorId,
    uint DoorVersion,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<DoorLinkRecordSummaryDto> Items);

public sealed record DoorLinkRecordSummaryDto(
    long UnitId,
    bool IsGrouped,
    int AyahCount,
    int SelectedWordCount,
    int DescriptionCount,
    IReadOnlyList<string> SourceLabels,
    string FirstVerseKey,
    string LastVerseKey);

public sealed record DoorLinkAyahsPageDto(
    int DoorId,
    uint DoorVersion,
    long UnitId,
    bool IsGrouped,
    long LinkingDataRevision,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<DoorLinkAyahDto> Items);

public sealed record DoorLinkAyahDto(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    string SurahNameArabic,
    short PageFrom,
    short PageTo,
    IReadOnlyList<int> SelectedWordIds,
    IReadOnlyList<string> Descriptions,
    IReadOnlyList<LinkingResolvedWordDto> Words);

public abstract record DoorLinkRecordsReadResult
{
    private DoorLinkRecordsReadResult() { }

    public sealed record Success(DoorLinkRecordsPageDto Page) : DoorLinkRecordsReadResult;
    public sealed record DoorNotFound : DoorLinkRecordsReadResult;
    public sealed record DoorArchived : DoorLinkRecordsReadResult;
    public sealed record DoorVersionStale : DoorLinkRecordsReadResult;
}

public abstract record DoorLinkAyahsReadResult
{
    private DoorLinkAyahsReadResult() { }

    public sealed record Success(DoorLinkAyahsPageDto Page) : DoorLinkAyahsReadResult;
    public sealed record DoorNotFound : DoorLinkAyahsReadResult;
    public sealed record DoorArchived : DoorLinkAyahsReadResult;
    public sealed record DoorVersionStale : DoorLinkAyahsReadResult;
    public sealed record UnitNotFound : DoorLinkAyahsReadResult;
}
