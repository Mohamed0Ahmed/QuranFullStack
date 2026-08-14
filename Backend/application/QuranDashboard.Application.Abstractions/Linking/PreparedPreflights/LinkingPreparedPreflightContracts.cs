using QuranDashboard.Application.Abstractions.Linking.Preflight;
using QuranDashboard.Application.Abstractions.Linking.Responses;
using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;

public sealed record CreateLinkingPreparedPreflightRequest(
    Guid PreparationKey,
    int DoorId,
    long? ExpectedLinkingDataRevision,
    IReadOnlyList<LinkingPreparedSourceRequest> Sources);

public sealed record LinkingPreparedSourceRequest(
    int OrderValue,
    LinkingPreparedWorkspaceSourceReference? WorkspaceSource,
    LinkingPreparedInlineSource? InlineSource);

public sealed record LinkingPreparedWorkspaceSourceReference(long SourceId, uint SourceVersion);

public sealed record LinkingPreparedInlineSource(
    LinkingSourceDescriptor Descriptor,
    LinkingWorkspaceConfigurationInput Configuration);

public sealed record LinkingPreparedPreflightReceipt(
    LinkingPreparedPreflightStatusDto Status,
    bool IsNew);

public sealed record LinkingPreparedResultSummary(
    LinkingPreflightCountsDto Counts,
    bool IsNoOp,
    bool IsBlocked);

public sealed record LinkingPreparedPreflightStatusDto(
    Guid PreflightId,
    string Status,
    string Stage,
    int ProcessedSources,
    int TotalSources,
    int ProcessedAyahs,
    int? TotalAyahs,
    int PollAfterMs,
    long LinkingDataRevision,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool? IsNoOp,
    bool? IsBlocked,
    string? PreflightToken,
    LinkingPreflightCountsDto? Totals,
    IReadOnlyList<LinkingPreparedSourceSummaryDto> Sources,
    string? FailureCode);

public sealed record LinkingPreparedSourceSummaryDto(
    long PreparedSourceId,
    int OrderValue,
    string ResolutionIdentity,
    string Label,
    string SourceKind,
    string ContributionMode,
    bool? AutomaticWordMatchesEnabled,
    string? Classification,
    LinkingPreflightCountsDto? Counts,
    long? ExistingContributionId,
    uint? ExpectedContributionVersion,
    int? TotalAyahCount);

public sealed record LinkingPreparedDetailPageDto(
    Guid PreflightId,
    long LinkingDataRevision,
    string DetailKind,
    long? PreparedSourceId,
    string Filter,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<LinkingPreparedDetailItemDto> Items);

public sealed record LinkingPreparedDetailItemDto(
    LinkingResolvedAyahDto Ayah,
    IReadOnlyList<LinkingPreparedAyahOverlayDto> SourceOverlays);

public sealed record LinkingPreparedAyahOverlayDto(
    long PreparedSourceId,
    int SourceOrder,
    long? PreparedUnitId,
    bool IsRequested,
    int UnitOrder,
    int AyahOrder,
    bool IsGrouped,
    string Classification,
    string? InvalidReason,
    IReadOnlyList<int> MatchedQuranWordIds,
    IReadOnlyList<int> RequestedQuranWordIds,
    IReadOnlyList<string> Descriptions,
    IReadOnlyList<LinkingOverlappingSourceDto> OverlappingSources,
    LinkingWordChangesDto WordChanges,
    LinkingDoorWordImpactDto DoorWordImpact,
    LinkingDescriptionChangesDto DescriptionChanges);

public static class LinkingPreparedDetailFilters
{
    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "ALL",
        "NEW_AYAH",
        "OVERLAP_OTHER_SOURCE",
        "UNCHANGED",
        "UPDATE",
        "REMOVE",
        "INVALID",
    };
}
