namespace QuranDashboard.Domain.Access;

public sealed class AccessAuditEvent
{
    private AccessAuditEvent()
    {
    }

    public AccessAuditEvent(
        DateTimeOffset occurredAtUtc,
        AccessAuditActionType actionType,
        AccessAuditActorType actorType,
        int? actorUserId,
        int targetUserId,
        string actorSnapshotJson,
        string targetSnapshotJson,
        string? permissionCode,
        string? beforeStateJson,
        string? afterStateJson,
        string? reason,
        AccessAuditMetadata metadata)
    {
        OccurredAtUtc = occurredAtUtc;
        ActionType = actionType;
        ActorType = actorType;
        ActorUserId = actorUserId;
        TargetUserId = targetUserId;
        ActorSnapshotJson = RequireDocument(actorSnapshotJson, nameof(actorSnapshotJson));
        TargetSnapshotJson = RequireDocument(targetSnapshotJson, nameof(targetSnapshotJson));
        PermissionCode = permissionCode;
        BeforeStateJson = RequireOptionalDocument(beforeStateJson, nameof(beforeStateJson));
        AfterStateJson = RequireOptionalDocument(afterStateJson, nameof(afterStateJson));
        Reason = reason;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    public long Id { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public AccessAuditActionType ActionType { get; private set; }
    public AccessAuditActorType ActorType { get; private set; }
    public int? ActorUserId { get; private set; }
    public int TargetUserId { get; private set; }
    public string ActorSnapshotJson { get; private set; } = string.Empty;
    public string TargetSnapshotJson { get; private set; } = string.Empty;
    public string? PermissionCode { get; private set; }
    public string? BeforeStateJson { get; private set; }
    public string? AfterStateJson { get; private set; }
    public string? Reason { get; private set; }
    public AccessAuditMetadata Metadata { get; private set; } = null!;

    public User? ActorUser { get; private set; }
    public User TargetUser { get; private set; } = null!;

    private static string RequireDocument(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Audit document must be non-empty.", parameterName);
        }

        return value;
    }

    private static string? RequireOptionalDocument(string? value, string parameterName)
    {
        return value is null ? null : RequireDocument(value, parameterName);
    }
}
