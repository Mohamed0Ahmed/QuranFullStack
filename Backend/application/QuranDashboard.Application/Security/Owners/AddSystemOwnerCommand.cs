using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Security.Owners;

// Operational (no dashboard surface) add of a System Owner. An Abwab writer: barrier + generation contract,
// registered against the stabilization registry.
public sealed record AddSystemOwnerCommand(
    string Issuer,
    string Subject,
    bool AccountEnabled,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
