using System.Security.Claims;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abwab.Tree;
using QuranDashboard.Application.Security;

namespace QuranDashboard.Api.Abwab.Tree;

// T048/T060: composite-read redaction is enforced here as a backend DTO projection over the caller's
// resolved permissions — never frontend hiding. Read/search/snapshot only; no mutation action here.
[ApiController]
[Route("api/abwab/tree")]
public sealed class AbwabTreeController(
    GetAbwabTreeSnapshotHandler snapshotHandler,
    SearchAbwabCategoriesHandler searchHandler,
    EffectivePermissionResolver permissionResolver,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AbwabTreeSnapshotDto>>> GetSnapshot(CancellationToken cancellationToken)
    {
        var permissions = await ResolveCallerPermissionsAsync(cancellationToken);
        var snapshot = await snapshotHandler.HandleAsync(new GetAbwabTreeSnapshotQuery(), cancellationToken);
        var redacted = AbwabCompositeReadRedactor.Redact(snapshot, permissions);

        if (redacted is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<AbwabTreeSnapshotDto>.Fail(ApiMessages.AbwabCompositeReadDenied));
        }

        return Ok(ApiResponse<AbwabTreeSnapshotDto>.Ok(redacted, ApiMessages.AbwabTreeSnapshotLoaded));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<CategorySearchResultDto>>> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        var permissions = await ResolveCallerPermissionsAsync(cancellationToken);
        var result = await searchHandler.HandleAsync(new SearchAbwabCategoriesQuery(query ?? string.Empty), cancellationToken);
        var redacted = AbwabCompositeReadRedactor.Redact(result, permissions);

        if (redacted is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<CategorySearchResultDto>.Fail(ApiMessages.AbwabCompositeReadDenied));
        }

        return Ok(ApiResponse<CategorySearchResultDto>.Ok(redacted, ApiMessages.AbwabCategorySearchLoaded));
    }

    private async Task<IReadOnlySet<string>> ResolveCallerPermissionsAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
        var effective = await permissionResolver.ResolveAsync(currentUser.Sub, roleName, cancellationToken);
        return effective.ToHashSet(StringComparer.Ordinal);
    }
}
