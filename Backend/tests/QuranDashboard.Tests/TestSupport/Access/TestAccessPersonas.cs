using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.TestSupport.Access;

internal sealed record TestAccessPersona(
    string Key,
    string? Sub,
    UserStatus? Status,
    string? RoleName,
    string? PermissionCode,
    IReadOnlyDictionary<string, object> TokenClaims)
{
    public User BuildUser(int? roleId = null, DateTimeOffset? now = null)
    {
        if (Sub is null || Status is null)
        {
            throw new InvalidOperationException($"Persona '{Key}' does not describe a local user.");
        }

        var timestamp = now ?? DateTimeOffset.UtcNow;
        return new User
        {
            LogtoSub = Sub,
            Email = $"{Sub}@example.test",
            RoleId = roleId,
            Status = Status.Value,
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
        };
    }
}

internal static class TestAccessPersonas
{
    public static IReadOnlyList<TestAccessPersona> All { get; } =
    [
        new("Anonymous", null, null, null, null, new Dictionary<string, object>()),
        new("InvalidToken", "smoke-invalid-token", null, null, null, new Dictionary<string, object>()),
        new("AuthenticatedUnknown", "smoke-unknown", null, null, null, new Dictionary<string, object>()),
        new("Pending", "smoke-pending", UserStatus.Pending, null, null, new Dictionary<string, object>()),
        new("Disabled", "smoke-disabled", UserStatus.Disabled, null, null, new Dictionary<string, object>()),
        new("ReadOnly", "smoke-read-only", UserStatus.Active, null, null, new Dictionary<string, object>()),
        new("ExactPermission", "smoke-exact-permission", UserStatus.Active, null, "abwab.doors.create", new Dictionary<string, object>()),
        new("NeighboringPermission", "smoke-neighboring-permission", UserStatus.Active, null, "abwab.doors.edit", new Dictionary<string, object>()),
        new(
            "Owner",
            "smoke-owner",
            UserStatus.Active,
            RoleNames.Owner,
            null,
            new Dictionary<string, object>
            {
                ["email"] = "smoke-owner@example.test",
                ["email_verified"] = true,
            }),
        new("DisabledOwner", "smoke-disabled-owner", UserStatus.Disabled, RoleNames.Owner, null, new Dictionary<string, object>()),
        new(
            "ClaimSmuggling",
            "smoke-claim-smuggling",
            UserStatus.Active,
            null,
            null,
            new Dictionary<string, object>
            {
                ["role"] = RoleNames.Owner,
                ["permission"] = "abwab.doors.create",
            }),
    ];

    public static TestAccessPersona For(string key) =>
        All.Single(persona => persona.Key == key);
}
