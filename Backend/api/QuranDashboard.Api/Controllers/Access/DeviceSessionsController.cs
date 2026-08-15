using QuranDashboard.Api.Authentication;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Application.Access.Commands.CreateDeviceSession;
using QuranDashboard.Application.Access.Commands.RevokeDeviceSession;

namespace QuranDashboard.Api.Controllers.Access;

[ApiController]
[Route("api/auth/sessions")]
public sealed class DeviceSessionsController(
    CreateDeviceSessionHandler createDeviceSessionHandler,
    RevokeDeviceSessionHandler revokeDeviceSessionHandler) : ControllerBase
{
    private const string InteractiveIdentityEvidenceHeader = "X-Interactive-Identity-Evidence";

    [HttpPost]
    [RequireSessionBootstrap]
    public async Task<ActionResult<ApiResponse<DeviceSessionResponse>>> Create(CancellationToken cancellationToken)
    {
        var identityEvidenceToken = Request.Headers[InteractiveIdentityEvidenceHeader].ToString();
        Request.Cookies.TryGetValue(DeviceSessionAuthentication.SessionCookieName, out var previousSessionToken);
        var session = await createDeviceSessionHandler.HandleAsync(
            identityEvidenceToken,
            previousSessionToken,
            cancellationToken);

        DeviceSessionCookieWriter.Write(Response, session);

        return Ok(ApiResponse<DeviceSessionResponse>.Ok(
            new DeviceSessionResponse(session.ExpiresAtUtc),
            ApiMessages.DeviceSessionCreated));
    }

    [HttpDelete("current")]
    [RequireCurrentSession]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<ApiResponse<object>>> RevokeCurrent(CancellationToken cancellationToken)
    {
        var sessionIdValue = User.FindFirst(DeviceSessionAuthentication.SessionIdClaim)?.Value;
        if (!Guid.TryParse(sessionIdValue, out var sessionId))
        {
            return Unauthorized(ApiResponse<object>.Fail(ApiMessages.Unauthorized));
        }

        await revokeDeviceSessionHandler.HandleAsync(sessionId, cancellationToken);
        DeviceSessionCookieWriter.Delete(Response);
        return NoContent();
    }
}

public sealed record DeviceSessionResponse(DateTimeOffset ExpiresAtUtc);
