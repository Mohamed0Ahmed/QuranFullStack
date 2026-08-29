namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal sealed class PhraseIndexExactStager
{
    private readonly PhraseExactWindowStager windowStager;
    private readonly PhraseExactGenerationPersister generationPersister;

    public PhraseIndexExactStager(
        PhraseExactWindowStager windowStager,
        PhraseExactGenerationPersister generationPersister)
    {
        this.windowStager = windowStager;
        this.generationPersister = generationPersister;
    }

    internal async Task<PhraseExactStageResult> StageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid buildId,
        IReadOnlyList<PhraseSourceToken> sourceTokens,
        short maximumAyahLength,
        CancellationToken ct)
    {
        var metrics = await windowStager.StageAsync(
            connection,
            transaction,
            buildId,
            sourceTokens,
            maximumAyahLength,
            ct);
        return await generationPersister.PersistAsync(
            connection,
            transaction,
            buildId,
            metrics,
            ct);
    }
}
