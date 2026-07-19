using QuranDashboard.Domain.Access;

namespace QuranDashboard.Api.Authentication;

public static class AuthorizationPolicyNames
{
    public const string Owner = RoleNames.Owner;
    public const string Admin = RoleNames.Admin;
    public const string Editor = RoleNames.Editor;
}
