using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Security;

// Raised when a grant/revoke carries an expected assignment version that no longer matches the stored
// version. Thrown under the serialized commit BEFORE the assignment is mutated; mapped to 409 /
// abwab.permission_assignment_stale.
public sealed class PermissionAssignmentStaleException(long expectedVersion, long currentVersion)
    : Exception($"Permission assignment version is stale: expected {expectedVersion}, current {currentVersion}.")
{
    public string Code => AbwabConflictCodes.PermissionAssignmentStale;

    public long ExpectedVersion { get; } = expectedVersion;

    public long CurrentVersion { get; } = currentVersion;
}
