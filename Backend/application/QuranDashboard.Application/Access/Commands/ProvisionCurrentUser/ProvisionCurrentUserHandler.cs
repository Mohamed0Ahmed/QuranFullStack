using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Access.Commands.ProvisionCurrentUser;

public sealed class ProvisionCurrentUserHandler(
    ICurrentUser currentUser,
    IUserProvisioningService provisioningService)
{
    public Task<ProvisionedUser> HandleAsync(CancellationToken ct)
        => provisioningService.GetOrCreateAsync(currentUser.Identity, ct);
}
