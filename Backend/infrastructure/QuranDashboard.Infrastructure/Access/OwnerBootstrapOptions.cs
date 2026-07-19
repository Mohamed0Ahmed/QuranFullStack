using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace QuranDashboard.Infrastructure.Access;

// An EMPTY BootstrapOwnerEmail DISABLES owner bootstrap and is a valid config that must not fail
// startup; a non-empty value is format-validated fail-fast (see OwnerBootstrapOptionsValidator).
public sealed class OwnerBootstrapOptions
{
    public const string SectionName = "Auth";

    public string BootstrapOwnerEmail { get; set; } = string.Empty;
}

internal sealed class OwnerBootstrapOptionsValidator : IValidateOptions<OwnerBootstrapOptions>
{
    public ValidateOptionsResult Validate(string? name, OwnerBootstrapOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BootstrapOwnerEmail))
        {
            return ValidateOptionsResult.Success;
        }

        return MailAddress.TryCreate(options.BootstrapOwnerEmail, out _)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"{OwnerBootstrapOptions.SectionName}:{nameof(OwnerBootstrapOptions.BootstrapOwnerEmail)} " +
                "must be a valid email address when set.");
    }
}
