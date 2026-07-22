namespace QuranDashboard.Application.Abstractions.Abwab;

// Optimistic-concurrency token carried by every Abwab mutation command and every actionable read.
// A command whose generation no longer matches the current timeline generation is refused with the
// exact 409 (abwab.timeline_generation_stale) BEFORE any row mutation.
public readonly record struct ExpectedTimelineGeneration(long Generation)
{
    public static ExpectedTimelineGeneration Of(long generation) => new(generation);
}
