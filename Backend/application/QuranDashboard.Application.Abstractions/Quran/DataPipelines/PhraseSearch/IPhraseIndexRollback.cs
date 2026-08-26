namespace QuranDashboard.Application.Abstractions.Quran.DataPipelines.PhraseSearch;

public interface IPhraseIndexRollback
{
    Task<PhraseIndexRollbackExecution> RollbackAsync(CancellationToken ct);
}
