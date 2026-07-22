namespace QuranDashboard.Application.Abstractions.Security;

public sealed record SecurityAuditCommitResult(bool Audited, int EventCount);
