namespace QuranDashboard.Application.Abstractions.Security;

// Raised when first-login provisioning collides on the email unique index (not logto_sub): a subject was
// deleted and recreated in the IdP, so a brand-new sub carries a server-verified email that still belongs
// to an existing local user. An expected business conflict, not a server fault; carries only the
// conflicting email, never any other user's data.
public sealed class UserProvisioningEmailConflictException(string email) : Exception(
    $"A user with email '{email}' is already registered under a different Logto subject.")
{
    public string Email { get; } = email;
}
