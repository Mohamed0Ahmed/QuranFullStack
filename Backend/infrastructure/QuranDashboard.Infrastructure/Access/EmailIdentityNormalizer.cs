using System.Net.Mail;
using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Infrastructure.Access;

public sealed class EmailIdentityNormalizer : IEmailIdentityNormalizer
{
    public bool TryNormalize(string? email, out string? normalizedEmail)
    {
        normalizedEmail = null;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();
        if (!MailAddress.TryCreate(trimmed, out var address)
            || !string.Equals(address.Address, trimmed, StringComparison.Ordinal)
            || trimmed.IndexOf('@') <= 0
            || trimmed.LastIndexOf('@') != trimmed.IndexOf('@'))
        {
            return false;
        }

        normalizedEmail = trimmed.ToUpperInvariant();
        return true;
    }

    public string Normalize(string email)
    {
        if (TryNormalize(email, out var normalizedEmail))
        {
            return normalizedEmail!;
        }

        throw new InvalidEmailIdentityException();
    }
}
