using Microsoft.Net.Http.Headers;

namespace QuranDashboard.Api.Common;

// Conditional GET without middleware: ASP.NET Core evaluates If-None-Match only inside
// OutputCache/ResponseCaching and File results, neither of which this API uses.
internal static class ConditionalGet
{
    // no-store keeps the browser's own HTTP cache out of the picture entirely. An ETag-bearing response
    // is otherwise heuristically revalidatable, which would put a second, invisible validator layer in
    // front of the one the facades hold — two caches racing over the same resource.
    public static void SetValidatorHeaders(HttpResponse response, string etag)
    {
        response.Headers[HeaderNames.ETag] = etag;
        response.Headers[HeaderNames.CacheControl] = "no-store";
    }

    // Exact ordinal match on a member of the request's list. Anything else — absent, garbage, "*",
    // or a list without an exact member — is a non-match and earns a full 200. Fail-open is deliberate
    // for a single first-party client: a mis-sent header costs a body, never a stale representation.
    public static bool Matches(HttpRequest request, string etag)
    {
        var header = request.Headers.IfNoneMatch;
        if (header.Count == 0)
        {
            return false;
        }

        foreach (var value in header)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var member in value.Split(','))
            {
                var candidate = member.Trim();

                if (candidate.StartsWith("W/", StringComparison.Ordinal))
                {
                    candidate = candidate[2..];
                }

                if (string.Equals(candidate, etag, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
