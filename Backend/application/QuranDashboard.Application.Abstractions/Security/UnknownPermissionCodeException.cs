namespace QuranDashboard.Application.Abstractions.Security;

public sealed class UnknownPermissionCodeException(string permissionCode)
    : Exception($"Unknown permission code '{permissionCode}'.")
{
    public string PermissionCode { get; } = permissionCode;
}
