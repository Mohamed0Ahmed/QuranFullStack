using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Security.Owners;

// Operational (no dashboard surface) removal of a System Owner, subject to the final-owner invariant.
public sealed record RemoveSystemOwnerCommand(
    string Subject,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
