namespace QuranDashboard.Domain.Security.Permissions;

// A uniquely-keyed (target-kind, target-key, permission-code) assignment. `Version` increments on every
// real state change and is the optimistic-concurrency token the grant/revoke commands carry: an absent
// assignment is version 0, a present one is version >= 1. Grants and revokes are serialized by the
// security-audit commit; an idempotent re-grant/re-revoke is a version-preserving no-op (no audit).
public sealed class PermissionAssignment
{
    private PermissionAssignment()
    {
    }

    public PermissionAssignment(
        Guid id,
        PermissionTargetKind targetKind,
        string targetKey,
        string permissionCode,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        TargetKind = targetKind;
        TargetKey = targetKey;
        PermissionCode = permissionCode;
        IsGranted = true;
        Version = 1;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public PermissionTargetKind TargetKind { get; private set; }

    public string TargetKey { get; private set; } = string.Empty;

    public string PermissionCode { get; private set; } = string.Empty;

    public bool IsGranted { get; private set; }

    public long Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Grant(DateTimeOffset atUtc)
    {
        IsGranted = true;
        Version += 1;
        UpdatedAtUtc = atUtc;
    }

    public void Revoke(DateTimeOffset atUtc)
    {
        IsGranted = false;
        Version += 1;
        UpdatedAtUtc = atUtc;
    }
}
