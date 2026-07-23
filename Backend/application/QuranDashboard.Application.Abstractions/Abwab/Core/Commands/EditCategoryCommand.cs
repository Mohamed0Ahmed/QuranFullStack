using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record EditCategoryCommand(
    Guid CategoryId,
    string Name,
    string? Description,
    string? RepresentativeQuranExcerpt,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
