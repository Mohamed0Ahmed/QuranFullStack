namespace QuranDashboard.Tests.Smoke._Support;

public static class SmokePersonas
{
    public const string Owner = "smoke-owner";
    public const string Granted = "smoke-granted";
    public const string NoPermissions = "smoke-nopermissions";

    // Never seeded — /api/access/me with this sub exercises the real provisioning path
    // through the fake profile source; a pre-seeded Active user would short-circuit it.
    public const string Fresh = "smoke-fresh";

    public const string OwnerEmail = "smoke-owner@example.test";
    public const string GrantedEmail = "smoke-granted@example.test";
    public const string NoPermissionsEmail = "smoke-nopermissions@example.test";
}
