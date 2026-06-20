
namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;

internal sealed class NullI3rabGenerationWriteProbe : II3rabGenerationWriteProbe
{
    public Task AfterSegmentUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct) =>
        Task.CompletedTask;
}
