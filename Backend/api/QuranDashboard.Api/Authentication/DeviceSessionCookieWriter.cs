using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public static class DeviceSessionCookieWriter
{
    public static void Write(
        HttpResponse response,
        IssuedUserDeviceSession session,
        DateTimeOffset nowUtc)
    {
        response.Cookies.Append(
            DeviceSessionAuthentication.SessionCookieName,
            session.Token,
            CreateOptions(session.ExpiresAtUtc, nowUtc, httpOnly: true, path: "/api"));
        response.Cookies.Append(
            DeviceSessionAuthentication.CsrfCookieName,
            session.CsrfToken,
            CreateOptions(session.ExpiresAtUtc, nowUtc, httpOnly: false, path: "/"));
    }

    public static void Delete(HttpResponse response, DateTimeOffset nowUtc)
    {
        response.Cookies.Delete(
            DeviceSessionAuthentication.SessionCookieName,
            CreateOptions(DateTimeOffset.UnixEpoch, nowUtc, httpOnly: true, path: "/api"));
        response.Cookies.Delete(
            DeviceSessionAuthentication.CsrfCookieName,
            CreateOptions(DateTimeOffset.UnixEpoch, nowUtc, httpOnly: false, path: "/"));
    }

    private static CookieOptions CreateOptions(
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc,
        bool httpOnly,
        string path) => new()
    {
        HttpOnly = httpOnly,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = path,
        Expires = expiresAtUtc,
        MaxAge = expiresAtUtc > nowUtc
            ? expiresAtUtc - nowUtc
            : TimeSpan.Zero,
        IsEssential = true,
    };
}
