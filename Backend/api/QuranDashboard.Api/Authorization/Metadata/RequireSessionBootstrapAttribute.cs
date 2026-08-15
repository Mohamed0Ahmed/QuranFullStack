using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace QuranDashboard.Api.Authorization.Metadata;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireSessionBootstrapAttribute : AuthorizeAttribute
{
    public RequireSessionBootstrapAttribute()
    {
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
    }
}
