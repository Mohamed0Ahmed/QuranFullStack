using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Access.Commands.ReplaceUserPermissions;
using QuranDashboard.Application.Access.Queries.GetUserPermissions;

namespace QuranDashboard.Api.Controllers.Access;

[ApiController]
[Route("api/access/users/{userId:int}/permissions")]
[RequireOwner]
public sealed class AccessUserPermissionsController(
    GetUserPermissionsHandler getHandler,
    ReplaceUserPermissionsHandler replaceHandler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AccessUserPermissions>>> Get(
        int userId,
        CancellationToken cancellationToken)
    {
        var outcome = await getHandler.HandleAsync(userId, cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationPermissionsLoaded);
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<AccessUserPermissions>>> Replace(
        int userId,
        [FromBody] ReplaceUserPermissionsBody body,
        CancellationToken cancellationToken)
    {
        var outcome = await replaceHandler.HandleAsync(
            new ReplaceUserPermissionsCommand(
                userId,
                body.ExpectedVersion,
                body.PermissionCodes!,
                body.Reason),
            cancellationToken);
        return this.ToActionResult(outcome, ApiMessages.AccessAdministrationPermissionsReplaced);
    }
}
