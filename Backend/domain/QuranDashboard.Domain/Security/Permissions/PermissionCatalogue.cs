namespace QuranDashboard.Domain.Security.Permissions;

// Codes are stable protocol identifiers persisted in the DB and matched by auth policies — never rename in place.
public static class PermissionCatalogue
{
    public const string AttributionView = "attribution.view";

    public const string AttributionManage = "attribution.manage";

    public const string PermissionAdminister = "permission.administer";
    public const string AuditRestore = "audit.restore";
    public const string SafetyPointManage = "safetyPoint.manage";

    public const string SectionView = "section.view";
    public const string SectionAdd = "section.add";
    public const string SectionEdit = "section.edit";
    public const string SectionReorder = "section.reorder";
    public const string SectionDelete = "section.delete";

    public const string CategoryView = "category.view";
    public const string CategoryAdd = "category.add";
    public const string CategoryEdit = "category.edit";
    public const string CategoryMove = "category.move";
    public const string CategoryReorder = "category.reorder";
    public const string CategoryDelete = "category.delete";

    public const string ProtectionView = "protection.view";
    public const string ProtectionApply = "protection.apply";
    public const string ProtectionLift = "protection.lift";

    public static IReadOnlyList<PermissionCode> All { get; } =
    [
        new(AttributionView, systemOwnerOnly: false, dashboardAdminBaseline: true),
        new(AttributionManage, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(PermissionAdminister, systemOwnerOnly: true, dashboardAdminBaseline: false),
        new(AuditRestore, systemOwnerOnly: true, dashboardAdminBaseline: false),
        new(SafetyPointManage, systemOwnerOnly: true, dashboardAdminBaseline: false),
        new(SectionView, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(SectionAdd, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(SectionEdit, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(SectionReorder, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(SectionDelete, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(CategoryView, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(CategoryAdd, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(CategoryEdit, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(CategoryMove, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(CategoryReorder, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(CategoryDelete, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(ProtectionView, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(ProtectionApply, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(ProtectionLift, systemOwnerOnly: false, dashboardAdminBaseline: false),
    ];

    public static IReadOnlyList<string> Codes { get; } = All.Select(entry => entry.Code).ToList();

    public static IReadOnlyList<string> BaselineCodes { get; } =
        All.Where(entry => entry.DashboardAdminBaseline).Select(entry => entry.Code).ToList();

    public static IReadOnlyList<string> SystemOwnerOnlyCodes { get; } =
        All.Where(entry => entry.SystemOwnerOnly).Select(entry => entry.Code).ToList();

    public static PermissionCode? Find(string code) =>
        All.FirstOrDefault(entry => string.Equals(entry.Code, code, StringComparison.Ordinal));

    public static bool Contains(string code) => Find(code) is not null;

    public static bool IsSystemOwnerOnly(string code) => Find(code)?.SystemOwnerOnly ?? false;

    public static bool IsBaseline(string code) => Find(code)?.DashboardAdminBaseline ?? false;
}
