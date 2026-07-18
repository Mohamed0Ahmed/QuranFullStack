using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

/// <summary>
/// Resolves <see cref="ICurrentUser.Sub"/> from the authenticated request's <c>sub</c> claim. BE-1
/// keeps raw claim types (<c>MapInboundClaims = false</c>), so the Logto identity key survives as the
/// literal <c>"sub"</c> claim. This must only be resolved inside an authenticated request (behind
/// <c>[Authorize]</c>); a missing claim means it was used outside one, which is a programming error.
/// </summary>
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
