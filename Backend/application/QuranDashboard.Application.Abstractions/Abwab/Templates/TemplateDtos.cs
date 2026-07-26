namespace QuranDashboard.Application.Abstractions.Abwab.Templates;

public sealed record TemplateNodeAliasDto(
    Guid TemplateNodeSearchAliasId,
    string Value,
    bool IsDeleted,
    uint Version);

public sealed record TemplateNodeDto(
    Guid TemplateNodeId,
    Guid? ParentTemplateNodeId,
    string Name,
    string? RepresentativeQuranExcerpt,
    string? Description,
    int SiblingOrder,
    bool IsDeleted,
    uint Version,
    IReadOnlyList<TemplateNodeAliasDto> Aliases);

public sealed record DoorTemplateSummaryDto(
    Guid DoorTemplateId,
    string Name,
    string? Description,
    long TemplateRevision,
    bool IsDeleted,
    uint Version,
    int NodeCount);

public sealed record DoorTemplateListDto(
    int SchemaVersion,
    IReadOnlyList<DoorTemplateSummaryDto> Templates,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    DateTimeOffset ServerTimeUtc) : IAbwabActionableRead;

public sealed record DoorTemplateDetailDto(
    int SchemaVersion,
    DoorTemplateSummaryDto Template,
    IReadOnlyList<TemplateNodeDto> Nodes,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    long TreeRevision,
    DateTimeOffset ServerTimeUtc) : IAbwabActionableRead;

public sealed record TemplateHistoryEntryDto(
    long AuditSequence,
    string ActorSubject,
    DateTimeOffset ActedAtUtc,
    string Payload);

// The separate template-history projection (§6.3) — template CRUD never appears in the main product
// audit list, so it is read through its own port method rather than the audit read model.
//
// `HasMore` is never omitted when the projection is capped: an append-only history that silently
// stopped at its limit would read to the operator as the complete record of a template.
public sealed record TemplateHistoryDto(
    int SchemaVersion,
    Guid DoorTemplateId,
    IReadOnlyList<TemplateHistoryEntryDto> Entries,
    bool HasMore,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    DateTimeOffset ServerTimeUtc) : IAbwabActionableRead;
