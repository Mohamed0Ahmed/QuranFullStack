using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class OwnerBootstrapOptions
{
    public const string SectionName = "OwnerBootstrap";

    public List<string> Emails { get; set; } = [];

    public IReadOnlySet<string> NormalizedEmails { get; private set; } = new HashSet<string>(StringComparer.Ordinal);

    public string ConfigurationFingerprint { get; private set; } = string.Empty;

    internal void SetNormalizedEmails(IReadOnlySet<string> normalizedEmails)
    {
        NormalizedEmails = new HashSet<string>(normalizedEmails, StringComparer.Ordinal);
        ConfigurationFingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Join('\n', NormalizedEmails.Order(StringComparer.Ordinal))))).ToLowerInvariant();
    }
}

internal sealed class OwnerBootstrapOptionsValidator(
    IEmailIdentityNormalizer emailIdentityNormalizer) : IValidateOptions<OwnerBootstrapOptions>
{
    public ValidateOptionsResult Validate(string? name, OwnerBootstrapOptions options)
    {
        var normalizedEmails = new HashSet<string>(StringComparer.Ordinal);
        foreach (var email in options.Emails)
        {
            if (!emailIdentityNormalizer.TryNormalize(email, out var normalizedEmail))
            {
                return ValidateOptionsResult.Fail(
                    $"{OwnerBootstrapOptions.SectionName}:{nameof(OwnerBootstrapOptions.Emails)} contains an invalid email address.");
            }

            if (!normalizedEmails.Add(normalizedEmail!))
            {
                return ValidateOptionsResult.Fail(
                    $"{OwnerBootstrapOptions.SectionName}:{nameof(OwnerBootstrapOptions.Emails)} contains duplicate normalized email addresses.");
            }
        }

        if (normalizedEmails.Count == 0)
        {
            return ValidateOptionsResult.Fail(
                $"{OwnerBootstrapOptions.SectionName}:{nameof(OwnerBootstrapOptions.Emails)} must contain at least one email.");
        }

        options.SetNormalizedEmails(normalizedEmails);
        return ValidateOptionsResult.Success;
    }
}
