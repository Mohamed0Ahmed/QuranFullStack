using QuranDashboard.Domain.Access;

namespace QuranDashboard.Api.Authentication;

public static class AuthorizationPolicyNames
{
    public const string Owner = RoleNames.Owner;
    public const string Admin = RoleNames.Admin;
    public const string Editor = RoleNames.Editor;

    // Backend-authoritative System Owner membership (immutable issuer/subject), distinct from the Owner
    // ROLE. Gates the permission-administration surface; frontend hiding is non-authoritative.
    public const string SystemOwner = "SystemOwner";
}
