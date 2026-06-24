namespace QuranDashboard.DataImporter.Import.ArgumentParsing;

internal static class ImportArguments
{

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
