namespace QuranDashboard.Domain.Security.Owners;

public sealed class SystemOwnerMembership
{
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
