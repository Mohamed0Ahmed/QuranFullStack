using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public sealed record LinkingOperationIntent(
    int DoorId,
    bool IsDoorArchived,
    IReadOnlyList<LinkingOperationSourceIntent> Sources);

public sealed record LinkingOperationSourceIntent(
    string SourceIdentity,
    LinkingSourceKind SourceKind,
    string Label,
    LinkingContributionMode ContributionMode,
    bool? AutomaticWordMatchesEnabled,
    int OrderValue,
    int ResolvedAyahCount,
    DateTimeOffset ResolvedAtUtc,
    IReadOnlyList<LinkingOperationUnitIntent> Units,
    LinkingPreflightInvalidReason? InvalidReason);

public sealed record LinkingOperationUnitIntent(
    string Identity,
    bool IsGrouped,
    IReadOnlyList<LinkingOperationAyahIntent> Ayahs);

public sealed record LinkingOperationAyahIntent(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    IReadOnlyList<int> WordIds,
    IReadOnlyList<string> Descriptions,
    LinkingPreflightInvalidReason? InvalidReason,
    IReadOnlyList<int> SourceMatchedWordIds);
