namespace QuranDashboard.Domain.Security.Permissions;

// Values are pinned explicitly and persisted as-is; do not renumber (a shift would silently reassign
// already-stored assignments).
public enum PermissionTargetKind
{
    Role = 1,
    Subject = 2,
}
