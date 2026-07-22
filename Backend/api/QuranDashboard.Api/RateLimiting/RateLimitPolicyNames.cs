namespace QuranDashboard.Api.RateLimiting;

// Named rate-limiter policies for the security surface (FR-040). Unlike the IP-partitioned global limiter
// (which ships opt-in via RateLimitingOptions.Enabled), these named policies are ALWAYS active on the
// endpoints they decorate — the safe, enabled default protection for the permission-administration and
// operational owner-bootstrap paths.
public static class RateLimitPolicyNames
{
    public const string PermissionAdmin = "permission-admin";

    public const string OwnerBootstrap = "owner-bootstrap";
}
