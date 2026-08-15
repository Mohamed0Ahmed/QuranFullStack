using Microsoft.AspNetCore.Authorization;
using QuranDashboard.Api.Authentication;

namespace QuranDashboard.Api.Authorization.Metadata;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireCurrentSessionAttribute : AuthorizeAttribute
{
    public RequireCurrentSessionAttribute()
    {
        AuthenticationSchemes = DeviceSessionAuthentication.Scheme;
    }
}
