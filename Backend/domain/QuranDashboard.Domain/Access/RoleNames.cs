namespace QuranDashboard.Domain.Access;

// Persisted roles.name, matched by auth policies — never rename in place.
public static class RoleNames
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Editor = "Editor";
}
