using QuranDashboard.Domain.Abwab.Templates;

namespace QuranDashboard.Infrastructure.Abwab.Restore;

public sealed record DoorTemplateAggregate(
    DoorTemplate Template,
    IReadOnlyList<TemplateNode> Nodes,
    IReadOnlyList<TemplateNodeSearchAlias> Aliases);

public sealed record TemplateNodeAliasRestoreSnapshot(
    Guid TemplateNodeSearchAliasId,
    Guid TemplateNodeId,
    string Value,
    string NormalizedValue,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);

public sealed record TemplateNodeRestoreSnapshot(
    Guid TemplateNodeId,
    Guid? ParentTemplateNodeId,
    string Name,
    string NormalizedName,
    string? RepresentativeQuranExcerpt,
    string? Description,
    int SiblingOrder,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);

public sealed record DoorTemplateRestoreSnapshot(
    int SchemaVersion,
    Guid DoorTemplateId,
    string Name,
    string NormalizedName,
    string? Description,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc,
    IReadOnlyList<TemplateNodeRestoreSnapshot> Nodes,
    IReadOnlyList<TemplateNodeAliasRestoreSnapshot> Aliases);
