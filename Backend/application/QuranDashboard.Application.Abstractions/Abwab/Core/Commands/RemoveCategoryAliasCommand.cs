using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record RemoveCategoryAliasCommand(
    Guid CategorySearchAliasId,
    uint ExpectedVersion,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
