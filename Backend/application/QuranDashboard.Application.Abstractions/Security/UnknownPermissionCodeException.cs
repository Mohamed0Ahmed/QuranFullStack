namespace QuranDashboard.Application.Abstractions.Security;

// Raised when a grant/revoke names a code outside the fixed catalogue. This is a client/validation error
// (mapped to 400), not a concurrency conflict — it never reaches the serialized commit.
public sealed class UnknownPermissionCodeException(string permissionCode)
    : Exception($"Unknown permission code '{permissionCode}'.")
{
    public string PermissionCode { get; } = permissionCode;
}
