namespace QuranDashboard.Domain.Security.Permissions;

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
