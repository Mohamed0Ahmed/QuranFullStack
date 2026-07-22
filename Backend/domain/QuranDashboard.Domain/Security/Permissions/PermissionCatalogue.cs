namespace QuranDashboard.Domain.Security.Permissions;

// Codes are stable protocol identifiers persisted in the DB and matched by auth policies — never rename in place.
public static class PermissionCatalogue
{
    public const string AttributionView = "attribution.view";

    public const string AttributionManage = "attribution.manage";

    public const string PermissionAdminister = "permission.administer";
    public const string AuditRestore = "audit.restore";
    public const string SafetyPointManage = "safetyPoint.manage";

    public static IReadOnlyList<PermissionCode> All { get; } =
    [
        new(AttributionView, systemOwnerOnly: false, dashboardAdminBaseline: true),
        new(AttributionManage, systemOwnerOnly: false, dashboardAdminBaseline: false),
        new(PermissionAdminister, systemOwnerOnly: true, dashboardAdminBaseline: false),
        new(AuditRestore, systemOwnerOnly: true, dashboardAdminBaseline: false),
        new(SafetyPointManage, systemOwnerOnly: true, dashboardAdminBaseline: false),
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
