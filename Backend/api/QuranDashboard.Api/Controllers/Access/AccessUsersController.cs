using System.ComponentModel.DataAnnotations;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Access.Commands.AcceptAccessUser;
using QuranDashboard.Application.Access.Commands.DisableAccessUser;
using QuranDashboard.Application.Access.Commands.ReactivateAccessUser;
using QuranDashboard.Application.Access.Queries.GetAccessUser;
using QuranDashboard.Application.Access.Queries.ListAccessUsers;

namespace QuranDashboard.Api.Controllers.Access;

[ApiController]
[Route("api/access/users")]
[RequireOwner]
public sealed class AccessUsersController(
    ListAccessUsersHandler listHandler,
    GetAccessUserHandler getHandler,
    AcceptAccessUserHandler acceptHandler,
    DisableAccessUserHandler disableHandler,
    ReactivateAccessUserHandler reactivateHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AccessUserSummary>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<AccessUserSummary>>>> List(
        CancellationToken cancellationToken,
        [FromQuery] string? status = null,
        [FromQuery] bool? isOwner = null,
        [FromQuery] string? search = null,
        [FromQuery, Range(AccessUserPaging.MinimumPage, AccessUserPaging.MaximumPage)]
        int page = AccessUserPaging.MinimumPage,
        [FromQuery, Range(AccessUserPaging.MinimumPageSize, AccessUserPaging.MaximumPageSize)]
        int pageSize = AccessUserPaging.DefaultPageSize)
    {
        var outcome = await listHandler.HandleAsync(
            new AccessUserListQuery(status, isOwner, search, page, pageSize),
            cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationUsersLoaded);
    }

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<ApiResponse<AccessUserDetail>>> Get(
        int userId,
        CancellationToken cancellationToken)
    {
        var outcome = await getHandler.HandleAsync(userId, cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationUserLoaded);
    }

    [HttpPost("{userId:int}/accept")]
    public async Task<ActionResult<ApiResponse<AccessUserDetail>>> Accept(
        int userId,
        [FromBody] AcceptAccessUserBody body,
        CancellationToken cancellationToken)
    {
        var outcome = await acceptHandler.HandleAsync(
            new AcceptAccessUserCommand(userId, body.ExpectedVersion, body.PermissionCodes, body.Reason),
            cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationUserAccepted);
    }

    [HttpPost("{userId:int}/disable")]
    public async Task<ActionResult<ApiResponse<AccessUserDetail>>> Disable(
        int userId,
        [FromBody] DisableAccessUserBody body,
        CancellationToken cancellationToken)
    {
        var outcome = await disableHandler.HandleAsync(
            new DisableAccessUserCommand(userId, body.ExpectedVersion, body.Reason),
            cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationUserDisabled);
    }

    [HttpPost("{userId:int}/reactivate")]
    public async Task<ActionResult<ApiResponse<AccessUserDetail>>> Reactivate(
        int userId,
        [FromBody] ReactivateAccessUserBody body,
        CancellationToken cancellationToken)
    {
        var outcome = await reactivateHandler.HandleAsync(
            new ReactivateAccessUserCommand(userId, body.ExpectedVersion, body.Reason),
            cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationUserReactivated);
    }
}
