using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Application.Access.Commands.ProvisionCurrentUser;
using QuranDashboard.Application.Security;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Api.Controllers.Access;

[ApiController]
[Route("api/access")]
[Authorize]
public sealed class AccessController(
    ProvisionCurrentUserHandler provisionCurrentUserHandler,
    EffectivePermissionResolver effectivePermissionResolver) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await provisionCurrentUserHandler.HandleAsync(cancellationToken);

        // `/me` carries the caller's effective permissions so the UI, backend policy, and cache converge on
        // the committed winner. Frontend hiding is UX only; the backend policy is the authority.
        var permissions = await effectivePermissionResolver.ResolveAsync(user.Sub, user.RoleName, cancellationToken);

        var data = new CurrentUserResponse(
            user.Sub,
            user.Email,
            user.DisplayName,
            MapStatus(user.Status),
            user.RoleId,
            user.RoleName,
            permissions);

        return Ok(ApiResponse<CurrentUserResponse>.Ok(data, ApiMessages.CurrentUserLoaded));
    }

    private static string MapStatus(UserStatus status) => status switch
    {
        UserStatus.Pending => "pending",
        UserStatus.Active => "active",
        UserStatus.Disabled => "disabled",
        _ => throw new InvalidOperationException($"Unhandled {nameof(UserStatus)} variant '{status}'.")
    };
}

public sealed record CurrentUserResponse(
    string Sub,
    string Email,
    string? DisplayName,
    string Status,
    int? RoleId,
    string? RoleName,
    IReadOnlyList<string> Permissions);
