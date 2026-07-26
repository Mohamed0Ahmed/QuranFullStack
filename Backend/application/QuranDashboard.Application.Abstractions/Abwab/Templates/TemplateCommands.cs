namespace QuranDashboard.Application.Abstractions.Abwab.Templates;

public sealed record AddDoorTemplateCommand(
    string Name,
    string? Description,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record EditDoorTemplateCommand(
    Guid DoorTemplateId,
    string Name,
    string? Description,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record DeleteDoorTemplateCommand(
    Guid DoorTemplateId,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record RestoreDoorTemplateCommand(
    Guid DoorTemplateId,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

// Structural node commands carry the expected TemplateRevision (§6.4); content-only edits carry the
// row's expected xmin alone, because they change no structure and bump no counter.
public sealed record AddTemplateNodeCommand(
    Guid DoorTemplateId,
    Guid? ParentTemplateNodeId,
    string Name,
    string? RepresentativeQuranExcerpt,
    string? Description,
    long ExpectedTemplateRevision,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record EditTemplateNodeCommand(
    Guid TemplateNodeId,
    string Name,
    string? RepresentativeQuranExcerpt,
    string? Description,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record ReparentTemplateNodeCommand(
    Guid TemplateNodeId,
    Guid? NewParentTemplateNodeId,
    uint ExpectedVersion,
    long ExpectedTemplateRevision,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record ReorderTemplateNodesCommand(
    Guid DoorTemplateId,
    Guid? ParentTemplateNodeId,
    IReadOnlyList<Guid> OrderedTemplateNodeIds,
    long ExpectedTemplateRevision,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record RemoveTemplateNodeCommand(
    Guid TemplateNodeId,
    uint ExpectedVersion,
    long ExpectedTemplateRevision,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record AddTemplateNodeAliasCommand(
    Guid TemplateNodeId,
    string Value,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record EditTemplateNodeAliasCommand(
    Guid TemplateNodeSearchAliasId,
    string Value,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record RemoveTemplateNodeAliasCommand(
    Guid TemplateNodeSearchAliasId,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record RestoreTemplateNodeAliasCommand(
    Guid TemplateNodeSearchAliasId,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record ApplyDoorTemplateCommand(
    Guid DoorTemplateId,
    Guid TargetCategoryId,
    long ExpectedTemplateRevision,
    long ExpectedTreeRevision,
    uint ExpectedTargetVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
