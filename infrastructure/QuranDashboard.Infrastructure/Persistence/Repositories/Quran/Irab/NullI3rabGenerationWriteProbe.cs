using Npgsql;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

internal sealed class NullI3rabGenerationWriteProbe : II3rabGenerationWriteProbe
{
    public Task AfterSegmentUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct) =>
        Task.CompletedTask;
}
