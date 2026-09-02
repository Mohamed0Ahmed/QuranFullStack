namespace QuranDashboard.DataImporter.Import.ArgumentParsing;

internal sealed record QuranTopicsBookImportArguments(
    string SourcePath,
    int ActorUserId,
    string? ReportDirectory,
    bool ValidateOnly,
    bool AllowRemote,
    bool Confirmed)
{
    internal static bool TryParse(
        string[] args,
        out QuranTopicsBookImportArguments options,
        out string error)
    {
        string? sourcePath = null;
        string? actorUserIdText = null;
        string? reportDirectory = null;
        var validateOnly = false;
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
                        return Fail(out options, out error, "--source must be supplied exactly once with a JSON file.");
                    }

                    break;
                case "--actor-user-id":
                    if (actorUserIdText is not null || !TryReadValue(args, ref index, out actorUserIdText))
                    {
                        return Fail(out options, out error, "--actor-user-id must be supplied exactly once with a positive integer.");
                    }

                    break;
                case "--report-out":
                    if (reportDirectory is not null || !TryReadValue(args, ref index, out reportDirectory))
                    {
                        return Fail(out options, out error, "--report-out may be supplied once with a directory path.");
                    }

                    break;
                case "--validate-only":
                    if (validateOnly)
                    {
                        return Fail(out options, out error, "--validate-only may be supplied only once.");
                    }

                    validateOnly = true;
                    break;
                case "--allow-remote":
                    if (allowRemote)
                    {
                        return Fail(out options, out error, "--allow-remote may be supplied only once.");
                    }

                    allowRemote = true;
                    break;
                case "--yes":
                    if (confirmed)
                    {
                        return Fail(out options, out error, "--yes may be supplied only once.");
                    }

                    confirmed = true;
                    break;
                default:
                    return Fail(out options, out error, $"Unknown argument '{args[index]}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return Fail(out options, out error, "--source <book.json> is required.");
        }

        if (!int.TryParse(actorUserIdText, out var actorUserId) || actorUserId <= 0)
        {
            return Fail(out options, out error, "--actor-user-id <positive integer> is required.");
        }

        if (allowRemote != confirmed)
        {
            return Fail(out options, out error, "--allow-remote and --yes must be supplied together.");
        }

        options = new QuranTopicsBookImportArguments(
            Path.GetFullPath(sourcePath),
            actorUserId,
            string.IsNullOrWhiteSpace(reportDirectory) ? null : Path.GetFullPath(reportDirectory),
            validateOnly,
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

    private static bool Fail(
        out QuranTopicsBookImportArguments options,
        out string error,
        string message)
    {
        options = null!;
        error = message;
        return false;
    }
}
