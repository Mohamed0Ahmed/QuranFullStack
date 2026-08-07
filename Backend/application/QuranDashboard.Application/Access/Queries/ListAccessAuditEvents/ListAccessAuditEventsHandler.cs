using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Application.Access.Queries.ListAccessAuditEvents;

public sealed class ListAccessAuditEventsHandler(IAccessAuditReader reader)
{
    public Task<AccessOperationResult<AccessAuditEventPage>> HandleAsync(
        AccessAuditQuery query,
        CancellationToken cancellationToken)
    {
        if (!IsValid(query))
        {
            return Task.FromResult(AccessOperationResult<AccessAuditEventPage>.Failed(
                AccessOperationFailure.InvalidRequest));
        }

        return ReadAsync(query, cancellationToken);
    }

    private async Task<AccessOperationResult<AccessAuditEventPage>> ReadAsync(
        AccessAuditQuery query,
        CancellationToken cancellationToken)
    {
        var page = await reader.ListAsync(query, cancellationToken);
        return AccessOperationResult<AccessAuditEventPage>.Success(page);
    }

    private static bool IsValid(AccessAuditQuery query)
    {
        return query.TargetUserId is null or > 0
            && query.ActorUserId is null or > 0
            && AccessAdministrationValidation.IsValidPage(1, query.PageSize)
            && IsValidActionType(query.ActionType)
            && IsValidPermissionCode(query.PermissionCode)
            && (query.FromUtc is null || query.ToUtc is null || query.FromUtc <= query.ToUtc)
            && AccessAuditCursorCodec.TryDecode(query.Cursor, out _, out _);
    }

    private static bool IsValidActionType(string? actionType) => actionType is null
        || Enum.TryParse<AccessAuditActionType>(actionType, ignoreCase: false, out var parsed)
        && Enum.IsDefined(parsed);

    private static bool IsValidPermissionCode(string? permissionCode) => permissionCode is null
        || permissionCode.Length is > 0 and <= 128;
}
