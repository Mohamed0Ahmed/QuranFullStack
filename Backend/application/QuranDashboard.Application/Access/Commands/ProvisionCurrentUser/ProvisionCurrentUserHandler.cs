using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Application.Access.Commands.ProvisionCurrentUser;

public sealed class ProvisionCurrentUserHandler(
    ICurrentUser currentUser,
    IInteractiveIdentityEvidenceValidator identityEvidenceValidator,
    IUserProvisioningService provisioningService)
{
    public async Task<ProvisionedUser> HandleAsync(
        string identityEvidenceToken,
        CancellationToken cancellationToken)
    {
        var authenticatedSubject = currentUser.Identity.Sub;
        var validatedIdentity = await identityEvidenceValidator.ValidateAsync(
            identityEvidenceToken,
            authenticatedSubject,
            cancellationToken);
        var provisioningIdentity = validatedIdentity
            ?? new AuthenticatedInteractiveIdentity(authenticatedSubject, null, false);

        return await provisioningService.GetOrCreateAsync(provisioningIdentity, cancellationToken);
    }
}
