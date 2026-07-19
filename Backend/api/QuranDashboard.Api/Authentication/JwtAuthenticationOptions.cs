namespace QuranDashboard.Api.Authentication;

public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Auth";

    public string Authority { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}

internal sealed class JwtAuthenticationOptionsValidator : IValidateOptions<JwtAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtAuthenticationOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Authority)
            || !Uri.TryCreate(options.Authority, UriKind.Absolute, out var authority)
            || authority.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add($"{JwtAuthenticationOptions.SectionName}:{nameof(JwtAuthenticationOptions.Authority)} must be an absolute https URI.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{JwtAuthenticationOptions.SectionName}:{nameof(JwtAuthenticationOptions.Audience)} must not be blank.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
