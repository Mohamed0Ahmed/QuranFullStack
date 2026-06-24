using System.Globalization;
using System.Text.RegularExpressions;
using QuranDashboard.Application.Abstractions.Quran.DataPipelines.FullI3rab;

namespace QuranDashboard.Infrastructure.Files.Quran.DataPipelines.FullI3rab;

internal static class FullI3rabValidationChecks
{
    private static readonly Regex HtmlTagPattern = new(@"</?([a-zA-Z0-9]+)", RegexOptions.Compiled);
    private static readonly Regex HtmlClassPattern = new(@"class\s*=\s*""([^""]*)""", RegexOptions.Compiled);

    public static FullI3rabCheckResult Hard(string id, string expected, string observed, bool passed) =>
        new(id, FullI3rabImportConstants.HardSeverity, expected, observed, passed);

    public static FullI3rabCheckResult Warning(string id, string expected, string observed, bool passed) =>
        new(id, FullI3rabImportConstants.WarningSeverity, expected, observed, passed);

    public static void EnsureAllHardChecksPassed(IReadOnlyList<FullI3rabCheckResult> checks)
    {
        if (checks.Any(check =>
                check.Severity == FullI3rabImportConstants.HardSeverity
                && !check.Passed))
        {
            throw new FullI3rabValidationException(checks);
        }
    }

    public static FullI3rabCheckResult EvaluateHtmlAllowlist(string i3rabHtml)
    {
        var tags = new HashSet<string>(StringComparer.Ordinal);
        var classes = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match match in HtmlTagPattern.Matches(i3rabHtml))
        {
            if (match.Groups.Count > 1)
            {
                tags.Add(match.Groups[1].Value.ToLowerInvariant());
            }
        }

        foreach (Match match in HtmlClassPattern.Matches(i3rabHtml))
        {
            if (match.Groups.Count <= 1)
            {
                continue;
            }

            foreach (var token in match.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                classes.Add(token);
            }
        }

        var disallowedTags = tags
            .Where(tag => !FullI3rabInvariants.AllowedHtmlTags.Contains(tag))
            .Order(StringComparer.Ordinal)
            .ToList();
        var disallowedClasses = classes
            .Where(@class => !FullI3rabInvariants.AllowedHtmlClasses.Contains(@class))
            .Order(StringComparer.Ordinal)
            .ToList();

        var passed = disallowedTags.Count == 0 && disallowedClasses.Count == 0;
        var observed = passed
            ? "within allowlist"
            : $"tags:[{string.Join(',', disallowedTags)}] classes:[{string.Join(',', disallowedClasses)}]";

        return Warning(
            FullI3rabInvariants.CheckHtmlAllowlist,
            "tags/classes within allowlist",
            observed,
            passed);
    }

    public static string FormatCount(int value) => value.ToString(CultureInfo.InvariantCulture);
}
