namespace QuranDashboard.Domain.Linking;

public sealed class LinkingOperation
{
    public long Id { get; set; }

    public int DoorId { get; set; }

    public int ActorUserId { get; set; }

    public Guid IdempotencyKey { get; set; }

    public DateTimeOffset ConfirmedAtUtc { get; set; }

    public int SourceCount { get; set; }

    public int AyahCount { get; set; }

    public string OutcomeJson { get; set; } = string.Empty;
}
