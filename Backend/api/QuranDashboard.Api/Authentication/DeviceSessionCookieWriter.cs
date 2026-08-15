using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public static class DeviceSessionCookieWriter
{
    public static void Write(HttpResponse response, IssuedUserDeviceSession session)
    {
        response.Cookies.Append(
            DeviceSessionAuthentication.SessionCookieName,
            session.Token,
            CreateOptions(session.ExpiresAtUtc, httpOnly: true, path: "/api"));
        response.Cookies.Append(
            DeviceSessionAuthentication.CsrfCookieName,
            session.CsrfToken,
            CreateOptions(session.ExpiresAtUtc, httpOnly: false, path: "/"));
    }

    public static void Delete(HttpResponse response)
    {
        response.Cookies.Delete(
            DeviceSessionAuthentication.SessionCookieName,
            CreateOptions(DateTimeOffset.UnixEpoch, httpOnly: true, path: "/api"));
        response.Cookies.Delete(
            DeviceSessionAuthentication.CsrfCookieName,
            CreateOptions(DateTimeOffset.UnixEpoch, httpOnly: false, path: "/"));
    }

    private static CookieOptions CreateOptions(DateTimeOffset expiresAtUtc, bool httpOnly, string path) => new()
    {
        HttpOnly = httpOnly,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = path,
        Expires = expiresAtUtc,
        MaxAge = expiresAtUtc > DateTimeOffset.UtcNow
            ? expiresAtUtc - DateTimeOffset.UtcNow
            : TimeSpan.Zero,
        IsEssential = true,
    };
}
