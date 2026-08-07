namespace QuranDashboard.Application.Abstractions.Access;

public interface IEmailIdentityNormalizer
{
    bool TryNormalize(string? email, out string? normalizedEmail);

    string Normalize(string email);
}

public sealed class InvalidEmailIdentityException() : InvalidOperationException(
    "The supplied email is not a valid identity.");
