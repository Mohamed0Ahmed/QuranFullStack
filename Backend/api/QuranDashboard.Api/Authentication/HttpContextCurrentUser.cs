using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public AuthenticatedInteractiveIdentity Identity
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var sub = principal?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(sub))
            {
                throw new InvalidOperationException(
                    "No authenticated 'sub' claim is available. ICurrentUser must be resolved only " +
                    "inside an authenticated request (behind [Authorize]).");
            }

            return new AuthenticatedInteractiveIdentity(sub, null, false);
        }
    }
}
