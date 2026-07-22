namespace QuranDashboard.Domain.Security.Owners;

// The four sanctioned zero-to-one bootstrap rejections (FR-026 / SC-006). Any of these fails the bootstrap
// closed; none of them ever creates or audits an owner.
public enum SystemOwnerBootstrapFailure
{
    WrongIssuer = 1,
    UnverifiedEmail = 2,
    DisabledAccount = 3,
    DuplicateMismatch = 4,
}
