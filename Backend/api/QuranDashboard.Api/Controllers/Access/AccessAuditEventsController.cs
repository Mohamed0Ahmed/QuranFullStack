using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Access.Queries.ListAccessAuditEvents;

namespace QuranDashboard.Api.Controllers.Access;

[ApiController]
[Route("api/access/audit-events")]
[RequireOwner]
public sealed class AccessAuditEventsController(ListAccessAuditEventsHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AccessAuditEventPage>>> List(
        CancellationToken cancellationToken,
        [FromQuery] int? targetUserId = null,
        [FromQuery] int? actorUserId = null,
        [FromQuery] string? actionType = null,
        [FromQuery] string? permissionCode = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = 25)
    {
        var outcome = await handler.HandleAsync(
            new AccessAuditQuery(
                targetUserId,
                actorUserId,
                actionType,
                permissionCode,
                fromUtc,
                toUtc,
                cursor,
                pageSize),
            cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationAuditEventsLoaded);
    }
}
