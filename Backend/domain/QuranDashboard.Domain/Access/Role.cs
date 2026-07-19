namespace QuranDashboard.Domain.Access;

public sealed class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
