using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Security;

// Raised when a write violates the permission baseline: granting a non-assignable / SystemOwnerOnly code to
// an ordinary user, or revoking a DashboardAdminBaseline code (e.g. attribution.view). Mapped to 409 /
// abwab.permission_baseline_locked.
public sealed class PermissionBaselineLockedException(string permissionCode)
    : Exception($"Permission '{permissionCode}' is baseline-locked and cannot be assigned or removed as requested.")
{
    public string Code => AbwabConflictCodes.PermissionBaselineLocked;

    public string PermissionCode { get; } = permissionCode;
}
