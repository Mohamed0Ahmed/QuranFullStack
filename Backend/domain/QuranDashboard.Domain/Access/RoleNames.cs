namespace QuranDashboard.Domain.Access;

/// <summary>
/// Canonical role names. Each value is the identity of a seeded <see cref="Role"/> (persisted as
/// <c>roles.name</c>), the key by which capabilities are enforced in code, and the value a role-based
/// authorization policy requires. Changing a value is both a data and a contract change, so these are
/// pinned string constants, not derived from anything.
/// </summary>
public static class RoleNames
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Editor = "Editor";
}
