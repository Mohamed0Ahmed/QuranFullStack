using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Abstractions.Security;

public sealed record ProvisionedUser(
    string Sub,
    string Email,
    string? DisplayName,
    UserStatus Status,
    bool IsOwner,
    IReadOnlyList<string> Permissions);
