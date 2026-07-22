using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Security.Owners;

public sealed record RemoveSystemOwnerCommand(
    string Subject,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
