namespace QuranDashboard.Application.Abstractions.Security;

public sealed class UserProvisioningEmailConflictException(string email) : Exception(
    $"A user with email '{email}' is already registered under a different Logto subject.")
{
    public string Email { get; } = email;
}
