using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record AddCategoryCommand(
    string Name,
    string? Description,
    string? RepresentativeQuranExcerpt,
    Guid? ParentCategoryId,
    Guid? SectionId,
    long ExpectedTreeRevision,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
