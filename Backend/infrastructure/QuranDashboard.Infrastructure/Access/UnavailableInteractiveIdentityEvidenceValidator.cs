using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Infrastructure.Access;

internal sealed class UnavailableInteractiveIdentityEvidenceValidator : IInteractiveIdentityEvidenceValidator
{
    public Task<AuthenticatedInteractiveIdentity?> ValidateAsync(
        string evidenceToken,
        string expectedSubject,
        CancellationToken cancellationToken) => Task.FromResult<AuthenticatedInteractiveIdentity?>(null);
}
