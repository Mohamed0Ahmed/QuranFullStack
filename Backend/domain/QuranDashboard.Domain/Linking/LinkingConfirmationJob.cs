namespace QuranDashboard.Domain.Linking;

public sealed class LinkingConfirmationJob
{
    public Guid Id { get; set; }
    public Guid PreflightId { get; set; }
    public int ActorUserId { get; set; }
    public int DoorId { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public LinkingConfirmationJobStatus Status { get; set; }
    public LinkingConfirmationJobStage Stage { get; set; }
    public int ProcessedItems { get; set; }
    public int TotalItems { get; set; }
    public DateTimeOffset? CancellationRequestedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public Guid? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    public Guid? CleanupOwner { get; set; }
    public DateTimeOffset? CleanupLeaseExpiresAtUtc { get; set; }
    public int CleanupAttemptCount { get; set; }
    public DateTimeOffset? CleanupStartedAtUtc { get; set; }
    public DateTimeOffset QueuedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long? OperationId { get; set; }
    public string? OutcomeDocumentJson { get; set; }
    public LinkingConfirmationJobFailureCode? FailureCode { get; set; }
    public uint Version { get; set; }
}
