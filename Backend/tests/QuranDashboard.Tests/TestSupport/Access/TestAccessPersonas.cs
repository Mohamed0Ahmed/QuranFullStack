using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.TestSupport.Access;

internal sealed record TestAccessPersona(
    string Key,
    string? Sub,
    UserStatus? Status,
    bool IsOwner,
    string? PermissionCode,
    IReadOnlyDictionary<string, object> TokenClaims)
{
    public User BuildUser(int? ownerRoleId = null, DateTimeOffset? now = null)
    {
        if (Sub is null || Status is null)
        {
            throw new InvalidOperationException($"Persona '{Key}' does not describe a local user.");
        }

        if (IsOwner != (ownerRoleId is not null))
        {
            throw new InvalidOperationException($"Persona '{Key}' has inconsistent Owner state.");
        }

        var timestamp = now ?? DateTimeOffset.UtcNow;
        return new User
        {
            LogtoSub = Sub,
            Email = $"{Sub}@example.test",
            RoleId = ownerRoleId,
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
        new("Anonymous", null, null, false, null, new Dictionary<string, object>()),
        new("InvalidToken", "smoke-invalid-token", null, false, null, new Dictionary<string, object>()),
        new("AuthenticatedUnknown", "smoke-unknown", null, false, null, new Dictionary<string, object>()),
        new("Pending", "smoke-pending", UserStatus.Pending, false, null, new Dictionary<string, object>()),
        new("Disabled", "smoke-disabled", UserStatus.Disabled, false, null, new Dictionary<string, object>()),
        new("ReadOnly", "smoke-read-only", UserStatus.Active, false, null, new Dictionary<string, object>()),
        new("ExactPermission", "smoke-exact-permission", UserStatus.Active, false, "abwab.doors.create", new Dictionary<string, object>()),
        new("NeighboringPermission", "smoke-neighboring-permission", UserStatus.Active, false, "abwab.doors.edit", new Dictionary<string, object>()),
        new("Owner", "smoke-owner", UserStatus.Active, true, null, new Dictionary<string, object>()),
        new("DisabledOwner", "smoke-disabled-owner", UserStatus.Disabled, true, null, new Dictionary<string, object>()),
        new(
            "ClaimSmuggling",
            "smoke-claim-smuggling",
            UserStatus.Active,
            false,
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
