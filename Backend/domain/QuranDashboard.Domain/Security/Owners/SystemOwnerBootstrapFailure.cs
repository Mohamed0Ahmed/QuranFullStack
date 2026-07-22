namespace QuranDashboard.Domain.Security.Owners;

public enum SystemOwnerBootstrapFailure
{
    WrongIssuer = 1,
    UnverifiedEmail = 2,
    DisabledAccount = 3,
    DuplicateMismatch = 4,
}
