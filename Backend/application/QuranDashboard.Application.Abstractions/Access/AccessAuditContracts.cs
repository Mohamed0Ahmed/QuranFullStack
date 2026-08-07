using System.Text.Json;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Abstractions.Access;

public sealed record AccessAuditQuery(
    int? TargetUserId,
    int? ActorUserId,
    string? ActionType,
    string? PermissionCode,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string? Cursor,
    int PageSize);

public sealed record AccessAuditEventItem(
    long Id,
    DateTimeOffset OccurredAtUtc,
    string ActionType,
    string ActorType,
    int? ActorUserId,
    int TargetUserId,
    JsonElement ActorSnapshot,
    JsonElement TargetSnapshot,
    string? PermissionCode,
    JsonElement BeforeState,
    JsonElement AfterState,
    string? Reason,
    JsonElement Metadata);

public sealed record AccessAuditEventPage(
    IReadOnlyList<AccessAuditEventItem> Items,
    string? NextCursor);

public sealed record AccessAuditUserSnapshot(
    int Id,
    string LogtoSub,
    string Email,
    string NormalizedEmail,
    string? DisplayName,
    UserStatus Status,
    bool IsOwner);

public sealed record AccessAuditUserState(
    string LogtoSub,
    UserStatus Status,
    bool IsOwner,
    IReadOnlyList<string> PermissionCodes);

public sealed record AccessAuditEntry(
    AccessAuditActionType ActionType,
    int ActorUserId,
    AccessAuditUserSnapshot Actor,
    AccessAuditUserSnapshot Target,
    string? PermissionCode,
    AccessAuditUserState Before,
    AccessAuditUserState After,
    string Reason,
    string Operation);

public sealed record AccessOwnerReconciliationSummary(
    DateTimeOffset OccurredAtUtc,
    string ActionType,
    int TargetUserId,
    string? Reason);

public interface IAccessAuditAppender
{
    void Append(AccessAuditEntry entry);
}

public interface IAccessAuditReader
{
    Task<AccessAuditEventPage> ListAsync(AccessAuditQuery query, CancellationToken cancellationToken);

    Task<AccessOwnerReconciliationSummary?> GetLatestOwnerReconciliationAsync(CancellationToken cancellationToken);
}
