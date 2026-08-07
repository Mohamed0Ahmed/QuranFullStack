namespace QuranDashboard.Application.Abstractions.Security;

public interface IInteractiveIdentityEvidenceValidator
{
    Task<AuthenticatedInteractiveIdentity?> ValidateAsync(
        string evidenceToken,
        string expectedSubject,
        CancellationToken cancellationToken);
}
