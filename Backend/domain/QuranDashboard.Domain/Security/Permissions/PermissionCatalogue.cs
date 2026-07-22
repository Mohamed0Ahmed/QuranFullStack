namespace QuranDashboard.Domain.Security.Permissions;

// The single canonical permission catalogue. Every other representation — the seeded `permission_codes`
// rows, the registered authorization policies, the `/me` projection, the frontend `permission-codes.ts`
// constant, and the parity test's own expectation — is checked against THIS list, so the 5-catalogue
// parity test fails on any drift (SC-007). Codes are stable protocol identifiers: never rename in place.
public static class PermissionCatalogue
{
    // DashboardAdminBaseline — always effective for the dashboard-admin baseline; cannot be revoked.
    public const string AttributionView = "attribution.view";

    // Ordinary assignable dashboard permission (grant/revoke happy path).
    public const string AttributionManage = "attribution.manage";

    // SystemOwnerOnly — held implicitly by System Owners, never assignable to an ordinary user.
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
