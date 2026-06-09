namespace QuranDashboard.Application.Abstractions.Quran.Words.Display;

public interface IDisplayWordsRebuilder
{
    Task<bool> AnyTargetTableHasDataAsync(CancellationToken ct);

    Task<DisplayWordsRebuildResult> RebuildAsync(bool force, int expectedReadableWords, CancellationToken ct);
}
