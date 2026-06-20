namespace QuranDashboard.DataImporter.Import.ArgumentParsing;

/// <summary>
/// Shared parsing of the common DataImporter argument surface:
/// <c>--source &lt;path&gt;</c>, <c>--report-out &lt;path&gt;</c>, and <c>--force</c>.
/// </summary>
/// <remarks>
/// Behavior is parameterized by <paramref name="requireSource"/> and
/// <paramref name="validateSourceExists"/> so every verb preserves its exact
/// pre-split acceptance rules and error messages.
/// </remarks>
internal static class ImportArguments
{
    /// <summary>
    /// Parses verbs that accept <c>--source</c>, <c>--report-out</c>, and <c>--force</c>.
    /// </summary>
    /// <param name="requireSource">When <c>true</c>, a missing <c>--source</c> is a hard error (Foundation verb).</param>
    /// <param name="validateSourceExists">When <c>true</c>, a supplied <c>--source</c> must point to an existing directory.</param>
    internal static bool TryParse(
        string[] args,
        bool requireSource,
        bool validateSourceExists,
        out string? sourcePath,
        out string? reportOutDir,
        out bool force,
        out string errorMessage)
    {
        sourcePath = null;
        reportOutDir = null;
        force = false;
        errorMessage = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--source":
                    if (!TryReadValue(args, ref index, out sourcePath))
                    {
                        errorMessage = "Missing value for --source.";
                        return false;
                    }

                    break;
                case "--report-out":
                    if (!TryReadValue(args, ref index, out reportOutDir))
                    {
                        errorMessage = "Missing value for --report-out.";
                        return false;
                    }

                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    errorMessage = $"Unknown argument '{args[index]}'.";
                    return false;
            }
        }

        if (requireSource)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                errorMessage = "--source is required.";
                return false;
            }

            sourcePath = Path.GetFullPath(sourcePath);
            if (!Directory.Exists(sourcePath))
            {
                errorMessage = $"Source directory was not found: {sourcePath}";
                return false;
            }
        }
        else if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            sourcePath = Path.GetFullPath(sourcePath);
            if (validateSourceExists && !Directory.Exists(sourcePath))
            {
                errorMessage = $"Source directory was not found: {sourcePath}";
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            reportOutDir = Path.GetFullPath(reportOutDir);
        }

        return true;
    }

    /// <summary>
    /// Parses verbs that accept only <c>--report-out</c> and <c>--force</c>
    /// (rebuild-words, generate-i3rab).
    /// </summary>
    internal static bool TryParseWithoutSource(
        string[] args,
        out string? reportOutDir,
        out bool force,
        out string errorMessage)
    {
        reportOutDir = null;
        force = false;
        errorMessage = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--report-out":
                    if (!TryReadValue(args, ref index, out reportOutDir))
                    {
                        errorMessage = "Missing value for --report-out.";
                        return false;
                    }

                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    errorMessage = $"Unknown argument '{args[index]}'.";
                    return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            reportOutDir = Path.GetFullPath(reportOutDir);
        }

        return true;
    }

    private static bool TryReadValue(string[] args, ref int index, out string? value)
    {
        if (index + 1 >= args.Length)
        {
            value = null;
            return false;
        }

        value = args[++index];
        return true;
    }
}
