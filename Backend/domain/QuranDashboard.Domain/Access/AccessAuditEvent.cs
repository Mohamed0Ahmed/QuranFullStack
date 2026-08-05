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
        string metadataJson)
    {
        OccurredAtUtc = occurredAtUtc;
        ActionType = actionType;
        ActorType = actorType;
        ActorUserId = actorUserId;
        TargetUserId = targetUserId;
        ActorSnapshotJson = actorSnapshotJson;
        TargetSnapshotJson = targetSnapshotJson;
        PermissionCode = permissionCode;
        BeforeStateJson = beforeStateJson;
        AfterStateJson = afterStateJson;
        Reason = reason;
        MetadataJson = metadataJson;
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
    public string MetadataJson { get; private set; } = string.Empty;

    public User? ActorUser { get; private set; }
    public User TargetUser { get; private set; } = null!;
}
