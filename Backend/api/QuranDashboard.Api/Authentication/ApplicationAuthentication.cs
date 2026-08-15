using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace QuranDashboard.Api.Authentication;

public static class ApplicationAuthentication
{
    public const string Scheme = "ApplicationAuthentication";

    public static string SelectScheme(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        return context.Request.Cookies.ContainsKey(DeviceSessionAuthentication.SessionCookieName)
            ? DeviceSessionAuthentication.Scheme
            : JwtBearerDefaults.AuthenticationScheme;
    }
}
