namespace QuranDashboard.Application.Abstractions.Security;

public interface IUserProvisioningService
{
    Task<ProvisionedUser> GetOrCreateAsync(string logtoSub, CancellationToken ct);
}
