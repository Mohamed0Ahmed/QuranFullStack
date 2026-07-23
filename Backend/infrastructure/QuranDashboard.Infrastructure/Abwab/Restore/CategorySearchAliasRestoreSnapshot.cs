namespace QuranDashboard.Infrastructure.Abwab.Restore;

public sealed record CategorySearchAliasRestoreSnapshot(
    Guid CategorySearchAliasId,
    string Value,
    string NormalizedValue,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);
