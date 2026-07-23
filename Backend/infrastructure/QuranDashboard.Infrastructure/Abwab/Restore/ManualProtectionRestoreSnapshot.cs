using QuranDashboard.Domain.Abwab.Protection;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

public sealed record ManualProtectionRestoreSnapshot(
    int SchemaVersion,
    Guid ManualProtectionId,
    Guid CategoryId,
    ManualProtectionType ProtectionType,
    ManualProtectionScope ProtectionScope,
    string AppliedByActorSubject,
    DateTimeOffset AppliedAtUtc,
    string? LiftedByActorSubject,
    DateTimeOffset? LiftedAtUtc,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);
