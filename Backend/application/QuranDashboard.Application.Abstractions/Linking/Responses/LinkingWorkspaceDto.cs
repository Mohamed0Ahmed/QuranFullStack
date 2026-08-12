using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.Responses;

public sealed record LinkingWorkspaceDto(
    uint? WorkspaceVersion,
    IReadOnlyList<LinkingWorkspaceSourceDto> Sources);

public sealed record LinkingWorkspaceSourceDto(
    long Id,
    uint SourceVersion,
    int OrderValue,
    LinkingSourceDescriptor Descriptor,
    string SourceIdentity,
    string InclusionMode,
    IReadOnlyList<int> AyahOverrides,
    IReadOnlyList<LinkingWorkspaceSelectedWordDto> SelectedWords,
    bool? AutomaticWordMatchesEnabled,
    string? ManualLinkShape,
    IReadOnlyList<LinkingWorkspaceManualAyahDto> ManualAyahs,
    IReadOnlyList<LinkingWorkspaceDescriptionDto> Descriptions,
    int? LastResolvedCount,
    DateTimeOffset? LastResolvedAtUtc);

public sealed record LinkingWorkspaceSelectedWordDto(int AyahId, int QuranWordId);

public sealed record LinkingWorkspaceManualAyahDto(int AyahId, string VerseKey, int OrderValue, int? PageHint);

public sealed record LinkingWorkspaceDescriptionDto(int AyahId, int OrderValue, string Body);
