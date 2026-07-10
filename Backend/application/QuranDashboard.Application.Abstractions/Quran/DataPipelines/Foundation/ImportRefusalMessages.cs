namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public static class ImportRefusalMessages
{
    public const string TablesNotEmpty =
        "Import refused: Quran target tables already contain data. Re-run with --force to replace atomically.";
}
