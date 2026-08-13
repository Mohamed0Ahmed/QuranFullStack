namespace QuranDashboard.Application.Abstractions.Linking.Preflight;

public sealed record LinkingOperationClassification(
    bool IsNoOp,
    bool IsBlocked,
    LinkingClassificationCounts Totals,
    IReadOnlyList<LinkingSourceClassification> Sources);

public sealed record LinkingSourceClassification(
    LinkingOperationSourceIntent Source,
    LinkingPreflightClassification Classification,
    long? ExistingContributionId,
    uint? ExistingContributionVersion,
    LinkingClassificationCounts Counts,
    IReadOnlyList<LinkingAyahClassification> Ayahs);

public sealed record LinkingAyahClassification(
    int AyahId,
    string VerseKey,
    int SurahNumber,
    int AyahNumber,
    LinkingPreflightClassification Classification,
    IReadOnlyList<LinkingOverlappingSource> OverlappingSources,
    LinkingWordChanges WordChanges,
    LinkingDoorWordImpact DoorWordImpact,
    LinkingDescriptionChanges DescriptionChanges,
    LinkingPreflightInvalidReason? InvalidReason);

public sealed record LinkingOverlappingSource(
    string SourceIdentity,
    string Label,
    string SourceKind);

public sealed record LinkingWordChanges(
    IReadOnlyList<int> Added,
    IReadOnlyList<int> Removed,
    IReadOnlyList<int> Unchanged);

public sealed record LinkingDoorWordImpact(
    IReadOnlyList<int> Added,
    IReadOnlyList<int> Existing,
    IReadOnlyList<int> Removed);

public sealed record LinkingDescriptionChanges(
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Changed,
    IReadOnlyList<string> Unchanged);

public sealed record LinkingClassificationCounts(
    int Requested,
    int New,
    int Overlapping,
    int Unchanged,
    int Updated,
    int Removed,
    int Invalid);
