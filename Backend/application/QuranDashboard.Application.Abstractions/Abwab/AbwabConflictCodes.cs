namespace QuranDashboard.Application.Abstractions.Abwab;

// Stable machine-readable conflict codes surfaced through the ApiResponse envelope (409). These are
// protocol identifiers, not user-facing messages, so they stay English and MUST NOT drift.
public static class AbwabConflictCodes
{
    public const string TimelineGenerationStale = "abwab.timeline_generation_stale";

    public const string WriteBarrierClosed = "abwab.write_barrier_closed";
}
