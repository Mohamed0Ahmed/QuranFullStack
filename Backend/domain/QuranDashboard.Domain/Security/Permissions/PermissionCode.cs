namespace QuranDashboard.Domain.Security.Permissions;

// One catalogue entry: the exact permission code plus its metadata. Also the EF entity behind the seeded
// `permission_codes` table (the "seed" catalogue in the 5-catalogue parity check). `IsAssignable` is
// computed (SystemOwnerOnly codes are never assignable via the permission-assignment table) and is not
// persisted.
public sealed class PermissionCode
{
    public PermissionCode()
    {
    }

    public PermissionCode(string code, bool systemOwnerOnly, bool dashboardAdminBaseline)
    {
        Code = code;
        SystemOwnerOnly = systemOwnerOnly;
        DashboardAdminBaseline = dashboardAdminBaseline;
    }

    public string Code { get; set; } = string.Empty;

    public bool SystemOwnerOnly { get; set; }

    public bool DashboardAdminBaseline { get; set; }

    public bool IsAssignable => !SystemOwnerOnly;
}
