namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public sealed record LinkingPreflightResultDto(
    int DoorId,
    string DoorName,
    long LinkingDataRevision,
    bool IsNoOp,
    bool IsBlocked,
    string PreflightToken,
    int TotalLinkCount,
    LinkingPreflightCountsDto Totals,
    IReadOnlyList<LinkingSourcePreflightDto> Sources);

public sealed record LinkingPreflightCountsDto(
    int Requested,
    int New,
    int Overlapping,
    int Unchanged,
    int Updated,
    int Removed,
    int Invalid);
