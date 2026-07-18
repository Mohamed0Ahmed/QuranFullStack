using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Infrastructure.Access;

/// <summary>
/// Resolves a user's server-verified profile (email/username/name) from the Logto Management API.
/// The API's inbound access token (audience = our API resource) cannot call Logto's userinfo endpoint,
/// so the trusted email source is the Management API: a machine-to-machine client-credentials token —
/// cached in memory until just before expiry — authorizes <c>GET /api/users/{sub}</c>. Every returned
/// value originates from Logto and is never client-supplied.
/// </summary>
public sealed class LogtoManagementApiUserProfileSource(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<LogtoManagementApiOptions> options) : IExternalUserProfileSource
{
    private const string TokenCacheKey = "auth:logto:management-api:m2m-token";
    private const int TokenExpirySkewSeconds = 60;

    // Single global token key, so one gate keeps concurrent cold-cache callers from stampeding the
    // Logto token endpoint. Static because the typed HttpClient makes this service transient.
    private static readonly SemaphoreSlim TokenGate = new(1, 1);

    private readonly HttpClient _httpClient = httpClient;
    private readonly IMemoryCache _cache = cache;
    private readonly LogtoManagementApiOptions _options = options.Value;

    public async Task<ExternalUserProfile> GetProfileAsync(string logtoSub, CancellationToken ct)
    {
        EnsureConfigured();

        var token = await GetManagementTokenAsync(ct);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{Endpoint}/api/users/{Uri.EscapeDataString(logtoSub)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<LogtoUserResponse>(ct)
            ?? throw new InvalidOperationException(
                $"The Logto Management API returned an empty body for user '{logtoSub}'.");

        return new ExternalUserProfile(user.PrimaryEmail, user.Username, user.Name);
    }

    private async Task<string> GetManagementTokenAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? cached) && cached is not null)
        {
            return cached;
        }

        await TokenGate.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(TokenCacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var token = await RequestManagementTokenAsync(ct);
            var ttlSeconds = Math.Max(token.ExpiresIn - TokenExpirySkewSeconds, 1);
            _cache.Set(TokenCacheKey, token.AccessToken, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds),
            });
            return token.AccessToken;
        }
        finally
        {
            TokenGate.Release();
        }
    }

    private async Task<LogtoTokenResponse> RequestManagementTokenAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/oidc/token");
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.AppId}:{_options.AppSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["resource"] = _options.Resource,
            ["scope"] = "all",
        });

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<LogtoTokenResponse>(ct)
            ?? throw new InvalidOperationException("The Logto token endpoint returned an empty body.");
        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("The Logto token endpoint returned no access_token.");
        }

        return token;
    }

    private string Endpoint => _options.Endpoint.TrimEnd('/');

    private void EnsureConfigured()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            missing.Add($"{LogtoManagementApiOptions.SectionName}:{nameof(LogtoManagementApiOptions.Endpoint)}");
        }

        if (string.IsNullOrWhiteSpace(_options.Resource))
        {
            missing.Add($"{LogtoManagementApiOptions.SectionName}:{nameof(LogtoManagementApiOptions.Resource)}");
        }

        if (string.IsNullOrWhiteSpace(_options.AppId))
        {
            missing.Add($"{LogtoManagementApiOptions.SectionName}:{nameof(LogtoManagementApiOptions.AppId)}");
        }

        if (string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            missing.Add($"{LogtoManagementApiOptions.SectionName}:{nameof(LogtoManagementApiOptions.AppSecret)}");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "The Logto Management API is not configured. Set the following keys via user-secrets or " +
                $"environment variables: {string.Join(", ", missing)}.");
        }
    }

    private sealed record LogtoTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private sealed record LogtoUserResponse(
        [property: JsonPropertyName("primaryEmail")] string? PrimaryEmail,
        [property: JsonPropertyName("username")] string? Username,
        [property: JsonPropertyName("name")] string? Name);
}
