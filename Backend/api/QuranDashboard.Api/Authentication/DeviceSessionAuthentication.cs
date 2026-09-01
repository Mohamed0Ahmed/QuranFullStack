using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public static class DeviceSessionAuthentication
{
    public const string Scheme = "DeviceSession";
    public const string SessionCookieName = "__Secure-quran-dashboard-session";
    public const string CsrfCookieName = "XSRF-TOKEN";
    public const string CsrfHeaderName = "X-XSRF-TOKEN";
    public const string SessionIdClaim = "quran_dashboard_session_id";
}

public sealed class DeviceSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IUserDeviceSessionStore sessionStore,
    TimeProvider timeProvider)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(DeviceSessionAuthentication.SessionCookieName, out var token)
            || string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var session = await sessionStore.ResolveAsync(token, timeProvider.GetUtcNow(), Context.RequestAborted);
        if (session is null)
        {
            return AuthenticateResult.Fail("The device session is invalid or expired.");
        }

        var claims = new[]
        {
            new Claim("sub", session.LogtoSub),
            new Claim(DeviceSessionAuthentication.SessionIdClaim, session.Id.ToString("D")),
        };
        var identity = new ClaimsIdentity(claims, DeviceSessionAuthentication.Scheme);
        var principal = new ClaimsPrincipal(identity);

        return AuthenticateResult.Success(
            new AuthenticationTicket(principal, DeviceSessionAuthentication.Scheme));
    }
}
