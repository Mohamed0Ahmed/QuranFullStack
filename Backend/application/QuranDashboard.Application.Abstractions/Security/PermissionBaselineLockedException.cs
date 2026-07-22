using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Security;

public sealed class PermissionBaselineLockedException(string permissionCode)
    : Exception($"Permission '{permissionCode}' is baseline-locked and cannot be assigned or removed as requested.")
{
    public string Code => AbwabConflictCodes.PermissionBaselineLocked;

    public string PermissionCode { get; } = permissionCode;
}
