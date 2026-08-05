using System.Globalization;
using System.Text;

namespace QuranDashboard.Tests.TestSupport.PostgreSql;

internal static class PostgreSqlDatabaseName
{
    internal const string Prefix = "qdb_test_";
    internal const int MaximumLength = 63;

    private const int RandomSuffixLength = 8;

    private static readonly NpgsqlCommandBuilder IdentifierQuoter = new();

    private static int counter;

    internal static string CreateForOwner(string owner)
    {
        return Compose(Slug(owner));
    }

    internal static string CreateTemplate()
    {
        return Compose("template");
    }

    internal static string CreateSchema(string owner)
    {
        return Compose(Slug(owner));
    }

    internal static string Quote(string identifier)
    {
        return IdentifierQuoter.QuoteIdentifier(identifier);
    }

    internal static string Slug(string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);

        var slug = new StringBuilder(owner.Length);
        foreach (var character in owner)
        {
            var lowered = char.ToLowerInvariant(character);
            var accepted = lowered is (>= 'a' and <= 'z') or (>= '0' and <= '9');
            if (accepted)
            {
                slug.Append(lowered);
            }
            else if (slug.Length > 0 && slug[^1] != '_')
            {
                slug.Append('_');
            }
        }

        var trimmed = slug.ToString().Trim('_');
        if (trimmed.Length == 0 || char.IsAsciiDigit(trimmed[0]))
        {
            trimmed = $"o{trimmed}";
        }

        return trimmed;
    }

    private static string Compose(string slug)
    {
        var processId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
        var sequence = Interlocked.Increment(ref counter).ToString(CultureInfo.InvariantCulture);
        var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(RandomSuffixLength / 2))
            .ToLowerInvariant();

        var fixedLength = Prefix.Length + processId.Length + sequence.Length + random.Length + 3;
        var slugBudget = MaximumLength - fixedLength;
        if (slugBudget < 1)
        {
            throw new InvalidOperationException(
                $"A PostgreSQL identifier of at most {MaximumLength} bytes leaves no room for an owner slug.");
        }

        var boundedSlug = slug.Length > slugBudget ? slug[..slugBudget].TrimEnd('_') : slug;
        if (boundedSlug.Length == 0)
        {
            boundedSlug = "o";
        }

        return $"{Prefix}{boundedSlug}_{processId}_{sequence}_{random}";
    }
}
