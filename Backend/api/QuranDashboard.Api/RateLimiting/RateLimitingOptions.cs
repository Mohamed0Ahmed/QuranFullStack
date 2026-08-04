namespace QuranDashboard.Api.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; }

    public string ClientIpHeaderName { get; set; } = "X-Real-IP";

    public int TokenLimit { get; set; } = 30;

    public int TokensPerPeriod { get; set; } = 30;

    public int ReplenishmentPeriodSeconds { get; set; } = 15;

    public int QueueLimit { get; set; }

    public int HealthPermitLimit { get; set; } = 300;

    public int HealthWindowSeconds { get; set; } = 60;
}

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
