namespace QuranDashboard.Domain.Access;

public sealed class UserPermission
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    public int GrantedByUserId { get; set; }
    public DateTimeOffset GrantedAtUtc { get; set; }

    public User User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
    public User GrantedByUser { get; set; } = null!;
}
