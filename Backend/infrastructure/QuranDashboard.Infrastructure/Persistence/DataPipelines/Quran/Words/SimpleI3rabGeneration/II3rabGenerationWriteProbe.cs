
namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.SimpleI3rabGeneration;

public interface II3rabGenerationWriteProbe
{
    Task AfterSegmentUpdateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct);
}
