using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

public sealed record RelationshipRestoreSnapshot(
    int SchemaVersion,
    Guid CategoryRelationshipId,
    RelationshipType RelationshipType,
    Guid? LowerCategoryId,
    Guid? HigherCategoryId,
    Guid? SourceCategoryId,
    Guid? TargetCategoryId,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);
