using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Abstractions.Security;

/// <summary>
/// The provisioned local user projected for callers: the Logto <c>sub</c>, the server-verified email,
/// an optional display name, the account <see cref="UserStatus"/>, the assigned role id and its role
/// name. <see cref="RoleId"/> and <see cref="RoleName"/> are both null until a role is assigned (an
/// Owner assigns one, or the Owner-bootstrap path assigns the Owner role on first login).
/// </summary>
public sealed record ProvisionedUser(
    string Sub,
    string Email,
    string? DisplayName,
    UserStatus Status,
    int? RoleId,
    string? RoleName);
