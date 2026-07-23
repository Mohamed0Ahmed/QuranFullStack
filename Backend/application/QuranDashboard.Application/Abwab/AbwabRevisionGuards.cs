using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Timeline;

namespace QuranDashboard.Application.Abwab;

// Shared by every 029 writer (Sections and Categories both carry ExpectedTreeRevision + expected
// xmin per Master Plan §8's concurrency column) — kept domain-neutral so neither area depends on
// the other's folder for a generic revision comparison.
internal static class AbwabRevisionGuards
{
    public static void GuardTreeRevision(AbwabRevisionState revision, long expectedTreeRevision)
    {
        if (revision.TreeRevision != expectedTreeRevision)
        {
            throw new AbwabWriteConflictException(
                AbwabConflictCodes.TreeRevisionStale,
                $"Tree revision is stale: expected {expectedTreeRevision}, current {revision.TreeRevision}.");
        }
    }

    public static void GuardRowVersion(uint currentVersion, uint expectedVersion)
    {
        if (currentVersion != expectedVersion)
        {
            throw new AbwabWriteConflictException(AbwabConflictCodes.RowStale, "The row changed since it was last read.");
        }
    }
}
