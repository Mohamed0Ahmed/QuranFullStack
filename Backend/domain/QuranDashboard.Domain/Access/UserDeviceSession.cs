namespace QuranDashboard.Domain.Access;

public sealed class UserDeviceSession
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string CsrfTokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
