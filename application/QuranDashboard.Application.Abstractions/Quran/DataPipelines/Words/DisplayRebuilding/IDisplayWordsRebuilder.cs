namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.Words.DisplayRebuilding;

public interface IDisplayWordsRebuilder
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    Task<DisplayWordsRebuildResult> RebuildAsync(bool force, int expectedReadableWords, CancellationToken ct);
}
