namespace QuranDashboard.DataImporter.Import.VerbRunners;

internal static class VerbConsole
{

    internal static void WriteReportPath(string? reportOutDir)
    {
        if (!string.IsNullOrWhiteSpace(reportOutDir))
        {
            Console.WriteLine($"Report written to: {reportOutDir}");
        }
    }

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
