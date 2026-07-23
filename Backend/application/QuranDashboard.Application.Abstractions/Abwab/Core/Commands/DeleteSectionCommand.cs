using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Abwab.Core;

public sealed record DeleteSectionCommand(
    Guid SectionId,
    uint ExpectedVersion,
    long ExpectedTreeRevision,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
