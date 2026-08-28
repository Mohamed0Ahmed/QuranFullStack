namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Foundation;

public sealed record QuranImportWriteResult(
    bool PhraseIndexCleanupCompleted,
    IReadOnlyList<string> Warnings)
{
    public static QuranImportWriteResult Completed { get; } = new(true, []);

    public static QuranImportWriteResult WithCleanupWarning(string warning) =>
        new(false, [warning]);
}
