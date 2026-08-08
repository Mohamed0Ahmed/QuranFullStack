using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Infrastructure.Persistence.Reads.Access;

public sealed class EfAccessAuditReader(QuranDashboardDbContext db) : IAccessAuditReader
{
    private static readonly JsonSerializerOptions MetadataSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<AccessAuditEventPage> ListAsync(AccessAuditQuery query, CancellationToken cancellationToken)
    {
        var events = db.AccessAuditEvents
            .AsNoTracking()
            .Include(eventItem => eventItem.ActorUser)
            .Include(eventItem => eventItem.TargetUser)
            .AsQueryable();
        if (query.TargetUserId is { } targetUserId)
        {
            events = events.Where(eventItem => eventItem.TargetUserId == targetUserId);
        }

        if (query.ActorUserId is { } actorUserId)
        {
            events = events.Where(eventItem => eventItem.ActorUserId == actorUserId);
        }

        if (query.ActionType is { } actionType)
        {
            var parsedActionType = Enum.Parse<AccessAuditActionType>(actionType, ignoreCase: false);
            events = events.Where(eventItem => eventItem.ActionType == parsedActionType);
        }

        if (query.PermissionCode is { } permissionCode)
        {
            events = events.Where(eventItem => eventItem.PermissionCode == permissionCode);
        }

        if (query.FromUtc is { } fromUtc)
        {
            events = events.Where(eventItem => eventItem.OccurredAtUtc >= fromUtc);
        }

        if (query.ToUtc is { } toUtc)
        {
            events = events.Where(eventItem => eventItem.OccurredAtUtc <= toUtc);
        }

        if (query.Cursor is not null)
        {
            AccessAuditCursorCodec.TryDecode(query.Cursor, out var occurredAtUtc, out var id);
            events = events.Where(eventItem => eventItem.OccurredAtUtc < occurredAtUtc
                || eventItem.OccurredAtUtc == occurredAtUtc && eventItem.Id < id);
        }

        var page = await events
            .OrderByDescending(eventItem => eventItem.OccurredAtUtc)
            .ThenByDescending(eventItem => eventItem.Id)
            .Take(query.PageSize + 1)
            .ToListAsync(cancellationToken);
        var hasMore = page.Count > query.PageSize;
        var items = page
            .Take(query.PageSize)
            .Select(ToItem)
            .ToArray();
        var nextCursor = hasMore
            ? AccessAuditCursorCodec.Encode(page[query.PageSize - 1].OccurredAtUtc, page[query.PageSize - 1].Id)
            : null;
        return new AccessAuditEventPage(items, nextCursor);
    }

    public async Task<AccessOwnerReconciliationSummary?> GetLatestOwnerReconciliationAsync(
        CancellationToken cancellationToken)
    {
        var eventItem = await db.AccessAuditEvents
            .FromSqlRaw(
                """
                SELECT *
                FROM access_audit_events
                WHERE metadata -> 'provenance' ->> 'operation' = 'owner-reconciliation'
                """)
            .AsNoTracking()
            .Where(item => item.ActorType == AccessAuditActorType.System)
            .OrderByDescending(item => item.OccurredAtUtc)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return eventItem is null
            ? null
            : new AccessOwnerReconciliationSummary(
                eventItem.OccurredAtUtc,
                eventItem.ActionType.ToString(),
                eventItem.TargetUserId,
                eventItem.Reason);
    }

    private static AccessAuditEventItem ToItem(AccessAuditEvent eventItem) => new(
        eventItem.Id,
        eventItem.OccurredAtUtc,
        eventItem.ActionType.ToString(),
        eventItem.ActorType.ToString(),
        eventItem.ActorUserId,
        eventItem.TargetUserId,
        eventItem.ActorUser?.DisplayName,
        eventItem.ActorUser?.Email,
        eventItem.TargetUser?.DisplayName,
        eventItem.TargetUser?.Email,
        ParseDocument(eventItem.ActorSnapshotJson),
        ParseDocument(eventItem.TargetSnapshotJson),
        eventItem.PermissionCode,
        ParseDocument(eventItem.BeforeStateJson),
        ParseDocument(eventItem.AfterStateJson),
        eventItem.Reason,
        JsonSerializer.SerializeToElement(eventItem.Metadata, MetadataSerializerOptions));

    private static JsonElement ParseDocument(string? json)
    {
        using var document = JsonDocument.Parse(json ?? "{}");
        return document.RootElement.Clone();
    }
}
