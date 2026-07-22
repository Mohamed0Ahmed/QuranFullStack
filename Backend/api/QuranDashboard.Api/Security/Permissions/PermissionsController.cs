using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using QuranDashboard.Api.Authentication;
using QuranDashboard.Api.RateLimiting;
using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Security.Permissions;
using QuranDashboard.Domain.Security.Permissions;

namespace QuranDashboard.Api.Security.Permissions;

// The ONLY security surface exposed to the dashboard: Owner-only permission administration. System Owner
// MEMBERSHIP administration (add/remove/bootstrap) is deliberately NOT exposed here — see the area README.
// Rejection is enforced by backend policy (SystemOwner); frontend hiding is non-authoritative.
[ApiController]
[Route("api/security/permissions")]
[Authorize(Policy = AuthorizationPolicyNames.SystemOwner)]
[EnableRateLimiting(RateLimitPolicyNames.PermissionAdmin)]
public sealed class PermissionsController(
    PermissionAdministrationHandler handler,
    IPermissionAssignmentStore assignments,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PermissionAdminViewDto>>> List(CancellationToken cancellationToken)
    {
        var catalogue = PermissionCatalogue.All
            .Select(entry => new PermissionCatalogueEntryDto(
                entry.Code, entry.SystemOwnerOnly, entry.DashboardAdminBaseline, entry.IsAssignable))
            .ToList();

        var current = (await assignments.ListAllAsync(cancellationToken))
            .Select(assignment => new PermissionAssignmentDto(
                assignment.TargetKind.ToString(),
                assignment.TargetKey,
                assignment.PermissionCode,
                assignment.Version,
                assignment.IsGranted))
            .ToList();

        return Ok(ApiResponse<PermissionAdminViewDto>.Ok(
            new PermissionAdminViewDto(catalogue, current), ApiMessages.PermissionsLoaded));
    }

    [HttpPost("grant")]
    public async Task<ActionResult<ApiResponse<object>>> Grant(GrantPermissionRequest request, CancellationToken cancellationToken)
    {
        if (!TryBuildTarget(request.TargetKind, request.PermissionCode, out var kind, out var failure))
        {
            return BadRequest(failure);
        }

        var command = new GrantPermissionCommand(
            kind,
            request.TargetKey,
            request.PermissionCode,
            ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
            request.ExpectedVersion,
            currentUser.Sub);

        await handler.GrantAsync(command, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, ApiMessages.PermissionGranted));
    }

    [HttpPost("revoke")]
    public async Task<ActionResult<ApiResponse<object>>> Revoke(RevokePermissionRequest request, CancellationToken cancellationToken)
    {
        if (!TryBuildTarget(request.TargetKind, request.PermissionCode, out var kind, out var failure))
        {
            return BadRequest(failure);
        }

        var command = new RevokePermissionCommand(
            kind,
            request.TargetKey,
            request.PermissionCode,
            ExpectedTimelineGeneration.Of(request.ExpectedTimelineGeneration),
            request.ExpectedVersion,
            currentUser.Sub);

        await handler.RevokeAsync(command, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, ApiMessages.PermissionRevoked));
    }

    private static bool TryBuildTarget(
        string targetKind,
        string permissionCode,
        out PermissionTargetKind kind,
        out ApiResponse<object> failure)
    {
        failure = ApiResponse<object>.Fail(ApiMessages.ValidationFailed);

        if (!PermissionTargetKindParsing.TryParse(targetKind, out kind))
        {
            failure = ApiResponse<object>.Fail(ApiMessages.ValidationFailed, [$"targetKind: '{targetKind}' is not valid."]);
            return false;
        }

        if (!PermissionCatalogue.Contains(permissionCode))
        {
            failure = ApiResponse<object>.Fail(ApiMessages.UnknownPermissionCode, [permissionCode]);
            return false;
        }

        return true;
    }
}
