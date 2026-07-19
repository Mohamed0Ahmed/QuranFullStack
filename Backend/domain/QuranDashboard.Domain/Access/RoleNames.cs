namespace QuranDashboard.Domain.Access;

// Each value is a seeded role's persisted roles.name and the key auth policies match on; changing one
// is both a data and a contract change, so they stay pinned string constants.
public static class RoleNames
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Editor = "Editor";
}
