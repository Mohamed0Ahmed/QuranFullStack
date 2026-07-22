using QuranDashboard.Application.Abstractions.Abwab;

namespace QuranDashboard.Application.Abstractions.Security;

public sealed class PermissionAssignmentStaleException(long expectedVersion, long currentVersion)
    : Exception($"Permission assignment version is stale: expected {expectedVersion}, current {currentVersion}.")
{
    public string Code => AbwabConflictCodes.PermissionAssignmentStale;

    public long ExpectedVersion { get; } = expectedVersion;

    public long CurrentVersion { get; } = currentVersion;
}
