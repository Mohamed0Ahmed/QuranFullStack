namespace QuranDashboard.Application.Abstractions.Security;

/// <summary>
/// Thrown when first-login provisioning collides on the <c>email</c> unique index rather than the
/// <c>logto_sub</c> index. This happens when a subject is deleted and recreated in the identity
/// provider: the new login carries a brand-new <c>sub</c> whose server-verified email still belongs to
/// an existing local user. This is an expected business conflict, not a server fault. Carries only the
/// conflicting email (never any other user's data).
/// </summary>
public sealed class UserProvisioningEmailConflictException(string email) : Exception(
    $"A user with email '{email}' is already registered under a different Logto subject.")
{
    public string Email { get; } = email;
}
