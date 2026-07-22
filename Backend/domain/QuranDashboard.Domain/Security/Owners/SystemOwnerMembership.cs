namespace QuranDashboard.Domain.Security.Owners;

// Immutable issuer/subject System Owner membership (§7.7/§11). The (Issuer, Subject) identity is the ONLY
// source of owner authority — there is no email/role/runtime fallback (FR-024). Membership state is
// mutated through behaviour only (Deactivate / account-enabled), never by rewriting the identity, and an
// owner counts as active for the final-owner invariant only while both the membership and the account are
// enabled.
public sealed class SystemOwnerMembership
{
    // EF materialization constructor.
    private SystemOwnerMembership()
    {
    }

    public SystemOwnerMembership(
        Guid id,
        string issuer,
        string subject,
        bool accountEnabled,
        DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Id = id;
        Issuer = issuer;
        Subject = subject;
        IsActive = true;
        IsAccountEnabled = accountEnabled;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Issuer { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsAccountEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    // The observable authority: an owner is only active while the membership is live AND the account is
    // enabled. A disabled account silently stops counting on the next sensitive request (FR-025).
    public bool IsActiveOwner => IsActive && IsAccountEnabled;

    public SystemOwnerIdentity Identity => new(Issuer, Subject);

    public void Deactivate(DateTimeOffset atUtc)
    {
        IsActive = false;
        DeactivatedAtUtc = atUtc;
    }

    public void Reactivate()
    {
        IsActive = true;
        DeactivatedAtUtc = null;
    }

    public void SetAccountEnabled(bool enabled) => IsAccountEnabled = enabled;
}
