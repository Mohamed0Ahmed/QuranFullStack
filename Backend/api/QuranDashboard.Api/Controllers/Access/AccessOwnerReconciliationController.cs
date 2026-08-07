using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Access.Queries.GetOwnerReconciliationStatus;

namespace QuranDashboard.Api.Controllers.Access;

[ApiController]
[Route("api/access/owner-reconciliation/status")]
[RequireOwner]
public sealed class AccessOwnerReconciliationController(GetOwnerReconciliationStatusHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<OwnerReconciliationStatus>>> Get(CancellationToken cancellationToken)
    {
        var status = await handler.HandleAsync(cancellationToken);
        return Ok(ApiResponse<OwnerReconciliationStatus>.Ok(
            status,
            ApiMessages.AccessAdministrationOwnerReconciliationLoaded));
    }
}
