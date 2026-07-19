namespace QuranDashboard.Application.Abstractions.Security;

/// <summary>
/// First-login provisioning of the local <c>Users</c> row keyed by the Logto <c>sub</c>. Phase 1 is
/// get-or-create only: an already-provisioned subject is returned unchanged, while a first-time
/// subject is created with a server-verified email, <c>Pending</c> status and no role.
/// </summary>
public interface IUserProvisioningService
{
    Task<ProvisionedUser> GetOrCreateAsync(string logtoSub, CancellationToken ct);
}
