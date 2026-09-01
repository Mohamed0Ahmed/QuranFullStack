using System.Security.Cryptography;
using System.Text;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public sealed class DeviceSessionAntiforgeryMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get,
        HttpMethods.Head,
        HttpMethods.Options,
        HttpMethods.Trace,
    };

    public async Task InvokeAsync(
        HttpContext context,
        IUserDeviceSessionStore sessionStore,
        TimeProvider timeProvider)
    {
        var sessionIdValue = context.User.FindFirst(DeviceSessionAuthentication.SessionIdClaim)?.Value;
        if (SafeMethods.Contains(context.Request.Method) || string.IsNullOrWhiteSpace(sessionIdValue))
        {
            await next(context);
            return;
        }

        var cookieToken = context.Request.Cookies[DeviceSessionAuthentication.CsrfCookieName];
        var headerToken = context.Request.Headers[DeviceSessionAuthentication.CsrfHeaderName].ToString();
        var valid = Guid.TryParse(sessionIdValue, out var sessionId)
                    && TokensMatch(cookieToken, headerToken)
                    && await sessionStore.ValidateCsrfAsync(
                        sessionId,
                        headerToken,
                        timeProvider.GetUtcNow(),
                        context.RequestAborted);
        if (valid)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail(ApiMessages.InvalidCsrfToken),
            context.RequestAborted);
    }

    private static bool TokensMatch(string? cookieToken, string headerToken)
    {
        if (string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(headerToken))
        {
            return false;
        }

        var cookieBytes = Encoding.UTF8.GetBytes(cookieToken);
        var headerBytes = Encoding.UTF8.GetBytes(headerToken);
        return cookieBytes.Length == headerBytes.Length
               && CryptographicOperations.FixedTimeEquals(cookieBytes, headerBytes);
    }
}
