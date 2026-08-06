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
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionCatalogueItem>>>> Get(
        CancellationToken cancellationToken)
    {
        var catalogue = await handler.HandleAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PermissionCatalogueItem>>.Ok(
            catalogue,
            ApiMessages.AccessAdministrationPermissionCatalogueLoaded));
    }
}
