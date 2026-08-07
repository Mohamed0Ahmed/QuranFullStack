namespace QuranDashboard.Application.Abstractions.Security;

public interface IUserProvisioningService
{
    Task<ProvisionedUser> GetOrCreateAsync(AuthenticatedInteractiveIdentity identity, CancellationToken ct);

    Task<ProvisionedUser> GetOrCreateAsync(string logtoSub, CancellationToken ct)
        => GetOrCreateAsync(new AuthenticatedInteractiveIdentity(logtoSub, null, false), ct);
}
