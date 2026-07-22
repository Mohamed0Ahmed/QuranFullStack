namespace QuranDashboard.Domain.Abwab.Timeline;

// Singleton monotonic timeline state. Migration seeds exactly one immutable generation-zero root;
// root edit/delete/duplicate are forbidden (DB trigger + partial unique index). Only 033's sealed
// restore transaction may insert non-root boundaries; nothing in 028 does.
public sealed class TimelineGenerationBoundary
{
    public const long RootGeneration = 0;

    public long Generation { get; set; }

    public bool IsRoot { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? Reason { get; set; }
}
