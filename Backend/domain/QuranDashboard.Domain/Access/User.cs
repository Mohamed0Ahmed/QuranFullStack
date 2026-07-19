namespace QuranDashboard.Domain.Access;

public sealed class User
{
    public int Id { get; set; }
    public string LogtoSub { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? DisplayName { get; set; }
    public string? Title { get; set; }

    // Nullable FK → Roles: a fresh user has no role until an Owner assigns one. The Owner-bootstrap
    // user is the sole exception (created directly with the Owner role).
    public int? RoleId { get; set; }

    public Role? Role { get; set; }

    public UserStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
