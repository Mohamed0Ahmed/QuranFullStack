namespace QuranDashboard.Application.Abstractions.Security;

// The outcome of a security-audit commit. `Audited` is false for an idempotent no-op (nothing changed, no
// cache invalidation). `EventCount` is how many permanent security-audit events were appended.
public sealed record SecurityAuditCommitResult(bool Audited, int EventCount);
