using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Security.Owners;

public sealed record AddSystemOwnerCommand(
    string Issuer,
    string Subject,
    bool AccountEnabled,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
