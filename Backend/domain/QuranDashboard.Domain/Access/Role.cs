namespace QuranDashboard.Domain.Access;

/// <summary>
/// A fixed, seeded authorization role. The row set is closed (see <see cref="RoleNames"/>): an Owner
/// assigns an existing role to a user but roles are never created from the UI. Capabilities are
/// enforced in code keyed by <see cref="Name"/>; <see cref="DisplayName"/> is the Arabic label shown to
/// users.
/// </summary>
public sealed class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
