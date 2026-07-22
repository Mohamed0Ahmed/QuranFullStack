namespace QuranDashboard.Application.Abstractions.Security;

public interface ISecurityAuditWriteExecutor
{
    Task<SecurityAuditCommitResult> ExecuteAsync(SecurityAuditWriteRequest request, CancellationToken cancellationToken);
}
