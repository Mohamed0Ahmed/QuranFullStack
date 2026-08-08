namespace QuranDashboard.Api.Access;

public sealed class PermissionCatalogueStartupOptions
{
    public const string SectionName = "Access:PermissionCatalogueStartupSync";

    public bool Enabled { get; set; } = true;
}
