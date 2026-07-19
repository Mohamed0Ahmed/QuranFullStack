namespace QuranDashboard.Api.RateLimiting;

/// <summary>
/// Bound configuration for the API rate limiter. Two profiles are exposed: a general
/// token-bucket limiter (all non-exempt requests except health) and a fixed-window
/// limiter dedicated to <c>/api/health*</c>. Secure by default: <see cref="Enabled"/>
/// ships <c>false</c> everywhere and is turned on via environment override only after
/// the deploy-time verification gates.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Master switch. When <c>false</c> the partitioner returns a no-op limiter for every request.</summary>
    public bool Enabled { get; set; }

    /// <summary>Single-valued header carrying the true client IP behind the edge proxy (Railway: <c>X-Real-IP</c>).</summary>
    public string ClientIpHeaderName { get; set; } = "X-Real-IP";

    // General limiter (token bucket) — applies to every non-exempt request except /api/health*.
    public int TokenLimit { get; set; } = 30;

    public int TokensPerPeriod { get; set; } = 30;

    public int ReplenishmentPeriodSeconds { get; set; } = 15;

    public int QueueLimit { get; set; }

    // Health limiter (fixed window) — applies to /api/health* only, per IP.
    public int HealthPermitLimit { get; set; } = 300;

    public int HealthWindowSeconds { get; set; } = 60;
}

/// <summary>
/// Fail-fast validation of <see cref="RateLimitingOptions"/>. Registered with
/// <c>ValidateOnStart()</c> so invalid configuration throws at startup rather than
/// surfacing as runtime rate-limiter errors.
/// </summary>
internal sealed class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitingOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ClientIpHeaderName))
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.ClientIpHeaderName)} must not be blank.");
        }

        if (options.TokenLimit <= 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.TokenLimit)} must be greater than 0.");
        }

        if (options.TokensPerPeriod <= 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.TokensPerPeriod)} must be greater than 0.");
        }

        if (options.ReplenishmentPeriodSeconds <= 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.ReplenishmentPeriodSeconds)} must be greater than 0.");
        }

        if (options.QueueLimit < 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.QueueLimit)} must be greater than or equal to 0.");
        }

        if (options.HealthPermitLimit <= 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.HealthPermitLimit)} must be greater than 0.");
        }

        if (options.HealthWindowSeconds <= 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.HealthWindowSeconds)} must be greater than 0.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
