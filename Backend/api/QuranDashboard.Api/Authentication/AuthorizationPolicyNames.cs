using QuranDashboard.Domain.Access;

namespace QuranDashboard.Api.Authentication;

/// <summary>
/// Names of the role-based authorization policies registered in <see cref="AuthenticationRegistration"/>.
/// Each aligns 1:1 with a <see cref="RoleNames"/> value and requires an authenticated caller in that
/// role. The policies are registered ready for future admin surfaces; in this phase they are applied to
/// no endpoint or controller.
/// </summary>
public static class AuthorizationPolicyNames
{
    public const string Owner = RoleNames.Owner;
    public const string Admin = RoleNames.Admin;
    public const string Editor = RoleNames.Editor;
}
