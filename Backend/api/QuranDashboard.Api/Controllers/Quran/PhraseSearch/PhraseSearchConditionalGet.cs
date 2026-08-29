using QuranDashboard.Application.Quran.PhraseSearch.Queries.GetPhraseSearchCapabilities;
using System.Security.Cryptography;
using System.Text;

namespace QuranDashboard.Api.Controllers.Quran.PhraseSearch;

internal static class PhraseSearchConditionalGet
{
    private static readonly byte[] ProcessValidatorKey = RandomNumberGenerator.GetBytes(32);

    internal static async Task<bool> MatchesCurrentBuildAsync(
        GetPhraseSearchCapabilitiesHandler capabilitiesHandler,
        HttpRequest request,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        if (!HasValidatorCandidate(request))
        {
            return false;
        }

        var outcome = await capabilitiesHandler.HandleAsync(cancellationToken);
        if (outcome is not GetPhraseSearchCapabilitiesOutcome.Success success)
        {
            return false;
        }

        var etag = CreateETag(success.Response.ActiveBuildId, request);
        if (!ConditionalGet.Matches(request, etag))
        {
            return false;
        }

        ConditionalGet.SetValidatorHeaders(response, etag);
        return true;
    }

    internal static OkObjectResult OkWithValidator<T>(
        ControllerBase controller,
        HttpRequest request,
        HttpResponse response,
        ApiResponse<T> body,
        Guid activeBuildId)
    {
        ConditionalGet.SetValidatorHeaders(response, CreateETag(activeBuildId, request));
        return controller.Ok(body);
    }

    private static string CreateETag(Guid activeBuildId, HttpRequest request)
    {
        var resourceIdentity = string.Concat(
            request.PathBase.ToUriComponent(),
            request.Path.ToUriComponent(),
            request.QueryString.ToUriComponent());
        var payload = Encoding.UTF8.GetBytes($"{activeBuildId:N}\n{resourceIdentity}");
        var hash = HMACSHA256.HashData(ProcessValidatorKey, payload);
        return $"\"phrase-search-{Convert.ToHexString(hash).ToLowerInvariant()}\"";
    }

    private static bool HasValidatorCandidate(HttpRequest request)
    {
        foreach (var value in request.Headers.IfNoneMatch)
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

                if (candidate.Length >= 2
                    && candidate[0] == '"'
                    && candidate[^1] == '"')
                {
                    return true;
                }
            }
        }

        return false;
    }
}
