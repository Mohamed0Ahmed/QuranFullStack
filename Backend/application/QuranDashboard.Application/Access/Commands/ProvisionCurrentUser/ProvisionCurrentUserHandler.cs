using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Access.Commands.ProvisionCurrentUser;

/// <summary>
/// Provisions (get-or-create) the local user for the authenticated caller and returns its projection.
/// The subject is taken from <see cref="ICurrentUser"/>, so this must run only inside an authenticated
/// request.
/// </summary>
public sealed class ProvisionCurrentUserHandler(
    ICurrentUser currentUser,
    IUserProvisioningService provisioningService)
{
    public Task<ProvisionedUser> HandleAsync(CancellationToken ct)
        => provisioningService.GetOrCreateAsync(currentUser.Sub, ct);
}
