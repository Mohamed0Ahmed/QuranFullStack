using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Security.Owners;

// The atomic, idempotent, permanently-audited zero-to-one bootstrap (FR-026). `ExpectedIssuer` and the
// verified-email / enabled-account flags are supplied by the operational caller (config-derived in
// production, explicit in tests); a mismatch fails the bootstrap closed.
public sealed record BootstrapSystemOwnerCommand(
    string Issuer,
    string Subject,
    string ExpectedIssuer,
    bool EmailVerified,
    bool AccountEnabled,
    ExpectedTimelineGeneration ExpectedTimelineGeneration,
    string ActorSubject) : IAbwabMutationCommand;
