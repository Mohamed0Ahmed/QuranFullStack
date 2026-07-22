namespace QuranDashboard.Application.Abstractions.Abwab;

// Stable machine-readable conflict codes surfaced through the ApiResponse envelope (409). These are
// protocol identifiers, not user-facing messages, so they stay English and MUST NOT drift.
public static class AbwabConflictCodes
{
    public const string TimelineGenerationStale = "abwab.timeline_generation_stale";

    public const string WriteBarrierClosed = "abwab.write_barrier_closed";

    // Security surface (owner/permission writes). The barrier being non-Writable is surfaced to these
    // operational/admin paths as `stabilization_active` rather than the raw `write_barrier_closed`.
    public const string StabilizationActive = "abwab.stabilization_active";

    public const string PermissionAssignmentStale = "abwab.permission_assignment_stale";

    public const string PermissionBaselineLocked = "abwab.permission_baseline_locked";

    public const string LastSystemOwner = "abwab.last_system_owner";
}
