namespace QuranDashboard.Application.Abstractions.Abwab;

public static class AbwabConflictCodes
{
    public const string TimelineGenerationStale = "abwab.timeline_generation_stale";

    public const string WriteBarrierClosed = "abwab.write_barrier_closed";

    public const string StabilizationActive = "abwab.stabilization_active";

    public const string PermissionAssignmentStale = "abwab.permission_assignment_stale";

    public const string PermissionBaselineLocked = "abwab.permission_baseline_locked";

    public const string LastSystemOwner = "abwab.last_system_owner";
}
