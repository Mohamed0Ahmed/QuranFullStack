namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed class AbwabTimelineGenerationStaleException(long expectedGeneration, long currentGeneration)
    : Exception($"Timeline generation is stale: expected {expectedGeneration}, current {currentGeneration}.")
{
    public string Code => AbwabConflictCodes.TimelineGenerationStale;

    public long ExpectedGeneration { get; } = expectedGeneration;

    public long CurrentGeneration { get; } = currentGeneration;
}
