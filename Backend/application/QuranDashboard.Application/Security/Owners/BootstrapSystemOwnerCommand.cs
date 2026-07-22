using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Security.Owners;

public sealed record BootstrapSystemOwnerCommand(
    string Issuer,
    string Subject,
    string ExpectedIssuer,
    bool EmailVerified,
    bool AccountEnabled,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
