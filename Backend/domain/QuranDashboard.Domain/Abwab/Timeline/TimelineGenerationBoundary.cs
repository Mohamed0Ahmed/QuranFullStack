namespace QuranDashboard.Domain.Abwab.Timeline;

public sealed class TimelineGenerationBoundary
{
    public const long RootGeneration = 0;

    public long Generation { get; set; }

    public bool IsRoot { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? Reason { get; set; }
}
