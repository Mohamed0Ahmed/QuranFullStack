using QuranDashboard.Domain.Abwab.Relationships;

namespace QuranDashboard.Application.Abstractions.Abwab.Relationships;

// FirstCategoryId/SecondCategoryId are shape-relative: for BroaderNarrower, First is the broader
// (source) and Second the narrower (target); for the mutual types the pair is canonicalized to
// lower/higher by the writer, so a reverse submission lands on the same active unique-index key.
public sealed record AddRelationshipCommand(
    RelationshipType RelationshipType,
    Guid FirstCategoryId,
    Guid SecondCategoryId,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record EditRelationshipCommand(
    Guid CategoryRelationshipId,
    RelationshipType RelationshipType,
    Guid FirstCategoryId,
    Guid SecondCategoryId,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record DeleteRelationshipCommand(
    Guid CategoryRelationshipId,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;

public sealed record RestoreRelationshipCommand(
    Guid CategoryRelationshipId,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
