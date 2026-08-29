namespace QuranDashboard.DataImporter.Import.ArgumentParsing;

internal sealed record AbwabSnapshotImportArguments(
    string SourcePath,
    string? ReportDirectory,
    bool AllowRemote,
    bool Confirmed)
{
    internal static bool TryParse(
        string[] args,
        out AbwabSnapshotImportArguments options,
        out string error)
    {
        string? sourcePath = null;
        string? reportDirectory = null;
        var allowRemote = false;
        var confirmed = false;
        error = string.Empty;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--source":
                    if (sourcePath is not null || !TryReadValue(args, ref index, out sourcePath))
                    {
                        options = null!;
                        error = "--source must be supplied exactly once with a snapshot-v4 JSON file.";
                        return false;
                    }

                    break;
                case "--report-out":
                    if (reportDirectory is not null || !TryReadValue(args, ref index, out reportDirectory))
                    {
                        options = null!;
                        error = "--report-out may be supplied once with a directory path.";
                        return false;
                    }

                    break;
                case "--allow-remote":
                    if (allowRemote)
                    {
                        options = null!;
                        error = "--allow-remote may be supplied only once.";
                        return false;
                    }

                    allowRemote = true;
                    break;
                case "--yes":
                    if (confirmed)
                    {
                        options = null!;
                        error = "--yes may be supplied only once.";
                        return false;
                    }

                    confirmed = true;
                    break;
                default:
                    options = null!;
                    error = $"Unknown argument '{args[index]}'.";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            options = null!;
            error = "--source <snapshot-v4.json> is required.";
            return false;
        }

        if (allowRemote != confirmed)
        {
            options = null!;
            error = "--allow-remote and --yes must be supplied together.";
            return false;
        }

        options = new AbwabSnapshotImportArguments(
            Path.GetFullPath(sourcePath),
            string.IsNullOrWhiteSpace(reportDirectory) ? null : Path.GetFullPath(reportDirectory),
            allowRemote,
            confirmed);
        return true;
    }

    private static bool TryReadValue(string[] args, ref int index, out string? value)
    {
        if (++index >= args.Length
            || string.IsNullOrWhiteSpace(args[index])
            || args[index].StartsWith('-'))
        {
            value = null;
            return false;
        }

        value = args[index];
        return true;
    }
}
