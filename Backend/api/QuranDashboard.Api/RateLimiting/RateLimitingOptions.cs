namespace QuranDashboard.Api.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    // Secure by default: ships false everywhere; enabled via environment override only after the
    // deploy-time verification gates. When false the partitioner returns a no-op limiter.
    public bool Enabled { get; set; }

    public string ClientIpHeaderName { get; set; } = "X-Real-IP";

    // General limiter (token bucket) — applies to every non-exempt request except /api/health*.
    public int TokenLimit { get; set; } = 30;

    public int TokensPerPeriod { get; set; } = 30;

    public int ReplenishmentPeriodSeconds { get; set; } = 15;

    public int QueueLimit { get; set; }

    // Health limiter (fixed window) — applies to /api/health* only, per IP.
    public int HealthPermitLimit { get; set; } = 300;

    public int HealthWindowSeconds { get; set; } = 60;

    // Named security policies — ALWAYS enabled on their endpoints (not gated by Enabled). Stricter than the
    // general limiter: permission administration and operational owner bootstrap are low-volume, high-value.
    public int PermissionAdminPermitLimit { get; set; } = 20;

    public int PermissionAdminWindowSeconds { get; set; } = 60;

    public int OwnerBootstrapPermitLimit { get; set; } = 5;

    public int OwnerBootstrapWindowSeconds { get; set; } = 60;
}

// Registered with ValidateOnStart() so invalid configuration throws at startup, not as runtime
// rate-limiter errors.
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

        if (options.PermissionAdminPermitLimit <= 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.PermissionAdminPermitLimit)} must be greater than 0.");
        }

        if (options.PermissionAdminWindowSeconds <= 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.PermissionAdminWindowSeconds)} must be greater than 0.");
        }

        if (options.OwnerBootstrapPermitLimit <= 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.OwnerBootstrapPermitLimit)} must be greater than 0.");
        }

        if (options.OwnerBootstrapWindowSeconds <= 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:{nameof(RateLimitingOptions.OwnerBootstrapWindowSeconds)} must be greater than 0.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
