namespace QuranDashboard.Domain.Access;

public sealed class User
{
    public int Id { get; set; }
    public string LogtoSub { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? DisplayName { get; set; }
    public string? Title { get; set; }

    public int? RoleId { get; set; }

    public Role? Role { get; set; }

    public UserStatus Status { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
