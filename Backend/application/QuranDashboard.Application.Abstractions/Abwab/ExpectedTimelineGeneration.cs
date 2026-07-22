namespace QuranDashboard.Application.Abstractions.Abwab;

public readonly record struct ExpectedTimelineGeneration(long Generation)
{
    public static ExpectedTimelineGeneration Of(long generation) => new(generation);
}
