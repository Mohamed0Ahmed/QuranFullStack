using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Application.Access.Commands.ProvisionCurrentUser;
using QuranDashboard.Domain.Access;

namespace QuranDashboard.Api.Controllers.Access;

[ApiController]
[Route("api/access")]
[Authorize]
public sealed class AccessController(ProvisionCurrentUserHandler provisionCurrentUserHandler) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var user = await provisionCurrentUserHandler.HandleAsync(cancellationToken);

        var data = new CurrentUserResponse(
            user.Sub,
            user.Email,
            user.DisplayName,
            MapStatus(user.Status),
            user.RoleId,
            user.RoleName);

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
    string? RoleName);
