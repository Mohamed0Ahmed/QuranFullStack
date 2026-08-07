using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Abstractions.Security;

public sealed record AuthorizationState(
    int UserId,
    UserStatus Status,
    bool IsOwner,
    IReadOnlySet<string> PermissionCodes);
