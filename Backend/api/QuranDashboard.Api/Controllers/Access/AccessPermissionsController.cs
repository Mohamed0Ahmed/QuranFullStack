using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Access.Queries.GetPermissionCatalogue;

namespace QuranDashboard.Api.Controllers.Access;

[ApiController]
[Route("api/access/permissions")]
[RequireOwner]
public sealed class AccessPermissionsController(GetPermissionCatalogueHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PermissionCatalogueResponse>>> Get(
        CancellationToken cancellationToken)
    {
        var catalogue = await handler.HandleAsync(cancellationToken);
        return Ok(ApiResponse<PermissionCatalogueResponse>.Ok(
            catalogue,
            ApiMessages.AccessAdministrationPermissionCatalogueLoaded));
    }
}
