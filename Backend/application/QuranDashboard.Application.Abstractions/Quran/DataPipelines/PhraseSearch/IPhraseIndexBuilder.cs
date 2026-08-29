namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

public interface IPhraseIndexBuilder
{
    Task<PhraseIndexBuildExecution> BuildAsync(
        string reportRootDirectory,
        CancellationToken ct);
}
