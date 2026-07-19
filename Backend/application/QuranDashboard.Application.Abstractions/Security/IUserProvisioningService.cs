namespace QuranDashboard.Application.Abstractions.Security;

// Get-or-create by Logto sub: an existing subject is returned unchanged; a first-time subject is created
// with a server-verified email, Pending status and no role.
public interface IUserProvisioningService
{
    Task<ProvisionedUser> GetOrCreateAsync(string logtoSub, CancellationToken ct);
}
