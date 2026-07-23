namespace QuranDashboard.Infrastructure.Abwab.Restore;

public sealed record SectionRestoreSnapshot(
    int SchemaVersion,
    Guid SectionId,
    string Name,
    string NormalizedName,
    int SortOrder,
    bool IsPermanentDefault,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);
