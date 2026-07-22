namespace QuranDashboard.Application.Abstractions.Security;

// The single barrier-gated entry every owner/permission write runs through. It enforces the barrier +
// generation-freshness gates and the permanent security-audit append, while guaranteeing the product
// timeline head is never advanced (§6.1/§6.2/§6.7/§7.7/§8).
public interface ISecurityAuditWriteExecutor
{
    Task<SecurityAuditCommitResult> ExecuteAsync(SecurityAuditWriteRequest request, CancellationToken cancellationToken);
}
