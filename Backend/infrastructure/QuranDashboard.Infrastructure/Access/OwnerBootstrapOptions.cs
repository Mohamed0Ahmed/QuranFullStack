using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace QuranDashboard.Infrastructure.Access;

/// <summary>
/// Bootstrap of the single Owner account by email (decision B3). When a user logs in whose
/// identity-provider-verified email equals <see cref="BootstrapOwnerEmail"/> (OrdinalIgnoreCase),
/// provisioning assigns the Owner role with Active status instead of the default Pending/no-role. An
/// EMPTY value DISABLES bootstrap (no owner is auto-created) and is a valid configuration that must not
/// fail startup; a non-empty value is format-validated fail-fast.
/// </summary>
public sealed class OwnerBootstrapOptions
{
    public const string SectionName = "Auth";

    /// <summary>The email bootstrapped to Owner/Active on login. Empty = bootstrap disabled.</summary>
    public string BootstrapOwnerEmail { get; set; } = string.Empty;
}

/// <summary>
/// Validates <see cref="OwnerBootstrapOptions"/> fail-fast at startup: an empty value is accepted
/// (bootstrap simply disabled), and only a supplied value is checked for a well-formed email address.
/// </summary>
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
