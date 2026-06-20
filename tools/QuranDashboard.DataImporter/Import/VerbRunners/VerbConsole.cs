namespace QuranDashboard.DataImporter.Import.VerbRunners;

/// <summary>
/// Shared console-output helpers used by every verb runner. Behavior is identical
/// to the pre-split inline helpers in <c>Program.cs</c>.
/// </summary>
internal static class VerbConsole
{
    /// <summary>
    /// Emits the optional "Report written to: ..." line. Only prints when a report
    /// directory was actually supplied and is non-empty.
    /// </summary>
    internal static void WriteReportPath(string? reportOutDir)
    {
        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            Console.WriteLine($"Report written to: {reportOutDir}");
        }
    }

    /// <summary>
    /// Writes the handler message to stdout on success (then invokes the
    /// success-details callback) or to stderr on failure, and returns the
    /// handler's exit code.
    /// </summary>
    internal static int WriteHandlerResult(
        bool succeeded,
        string message,
        int exitCode,
        Action writeSuccessDetails)
    {
        if (succeeded)
        {
            Console.WriteLine(message);
            writeSuccessDetails();
            return exitCode;
        }

        Console.Error.WriteLine(message);
        return exitCode;
    }
}
