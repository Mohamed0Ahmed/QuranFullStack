using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string Sub
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                throw new InvalidOperationException(
                    "No authenticated 'sub' claim is available. ICurrentUser must be resolved only " +
                    "inside an authenticated request (behind [Authorize]).");
            }

            return sub;
        }
    }
}
