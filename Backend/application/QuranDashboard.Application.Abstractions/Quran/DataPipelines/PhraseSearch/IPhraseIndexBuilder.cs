namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

public interface IPhraseIndexBuilder
{
    Task<PhraseIndexBuildExecution> BuildAsync(
        bool force,
        string reportRootDirectory,
        CancellationToken ct);
}
