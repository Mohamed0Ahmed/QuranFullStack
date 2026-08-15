namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public sealed record LinkingSourcePreflightDto(
    string SourceIdentity,
    string Label,
    string SourceKind,
    string ContributionMode,
    bool? AutomaticWordMatchesEnabled,
    string Classification,
    long? ExistingContributionId,
    uint? ExistingContributionVersion,
    int TotalLinkCount,
    LinkingPreflightCountsDto Counts,
    IReadOnlyList<LinkingAyahPreflightDto> Ayahs);
