namespace QuranDashboard.Application.Abstractions.Abwab;

// Raised by the write executor when a command's ExpectedTimelineGeneration no longer matches the
// current generation. Thrown BEFORE any row mutation; mapped to 409 / abwab.timeline_generation_stale.
public sealed class AbwabTimelineGenerationStaleException(long expectedGeneration, long currentGeneration)
    : Exception($"Timeline generation is stale: expected {expectedGeneration}, current {currentGeneration}.")
{
    public string Code => AbwabConflictCodes.TimelineGenerationStale;

    public long ExpectedGeneration { get; } = expectedGeneration;

    public long CurrentGeneration { get; } = currentGeneration;
}
