using System.Globalization;
using QuranDashboard.Application.Abstractions.Linking;

namespace QuranDashboard.Api.Contracts.Linking;

internal static class LinkingBodyViolations
{
    internal static LinkingDescriptorViolation Malformed(string field, string? value = null) =>
        new(LinkingDescriptorViolationCode.MalformedDescriptor, field, value);

    internal static string? Text(int? value) => value?.ToString(CultureInfo.InvariantCulture);

    internal static bool TryIdentifier(int? value, string field, out LinkingDescriptorViolation violation)
    {
        if (LinkingSourceDescriptorValidation.IdentifierError(value ?? 0, field) is null)
        {
            violation = null!;
            return true;
        }

        violation = Malformed(field, Text(value));
        return false;
    }

    internal static bool TryToken(
        string? value,
        IReadOnlyList<string> vocabulary,
        string field,
        out LinkingDescriptorViolation violation)
    {
        if (LinkingSourceDescriptorValidation.TokenError(value, vocabulary, field) is null)
        {
            violation = null!;
            return true;
        }

        violation = Malformed(field, value);
        return false;
    }
}
