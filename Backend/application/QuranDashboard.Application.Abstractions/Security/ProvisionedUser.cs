using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Abstractions.Security;

/// <summary>
/// The provisioned local user projected for callers: the Logto <c>sub</c>, the server-verified email,
/// an optional display name, the account <see cref="UserStatus"/>, and the assigned role id (null
/// until an Owner assigns one).
/// </summary>
public sealed record ProvisionedUser(string Sub, string Email, string? DisplayName, UserStatus Status, int? RoleId);
