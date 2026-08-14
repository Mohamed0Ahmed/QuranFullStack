namespace QuranDashboard.Domain.Linking;

public sealed class LinkingPreparedPreflight
{
    public Guid Id { get; set; }
    public int ActorUserId { get; set; }
    public int DoorId { get; set; }
    public Guid PreparationKey { get; set; }
    public LinkingPreparedPreflightStatus Status { get; set; }
    public LinkingPreparedPreflightStage Stage { get; set; }
    public int RequestSchemaVersion { get; set; }
    public string RequestDocumentJson { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string? IntentHash { get; set; }
    public long LinkingDataRevision { get; set; }
    public uint? ExpectedDoorVersion { get; set; }
    public string? PreflightToken { get; set; }
    public bool? IsNoOp { get; set; }
    public bool? IsBlocked { get; set; }
    public int? RequestedCount { get; set; }
    public int? NewCount { get; set; }
    public int? OverlappingCount { get; set; }
    public int? UnchangedCount { get; set; }
    public int? UpdatedCount { get; set; }
    public int? RemovedCount { get; set; }
    public int? InvalidCount { get; set; }
    public int ProcessedSources { get; set; }
    public int TotalSources { get; set; }
    public int ProcessedAyahs { get; set; }
    public int? TotalAyahs { get; set; }
    public DateTimeOffset? CancellationRequestedAtUtc { get; set; }
    public DateTimeOffset? ConfirmationAcceptedAtUtc { get; set; }
    public Guid? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public Guid? CleanupOwner { get; set; }
    public DateTimeOffset? CleanupLeaseExpiresAtUtc { get; set; }
    public int CleanupAttemptCount { get; set; }
    public DateTimeOffset? CleanupStartedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? ReadyAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? ConfirmedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public LinkingPreparedPreflightFailureCode? FailureCode { get; set; }
    public uint Version { get; set; }
}
