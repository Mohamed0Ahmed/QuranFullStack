using Npgsql;
using QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

namespace QuranDashboard.Tests.Quran.WordsSimpleI3rab;

internal sealed class SqlI3rabGenerationWriteProbe(string sql) : II3rabGenerationWriteProbe
{
    public async Task AfterSegmentUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken ct)
    {
        foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            await using var command = new NpgsqlCommand(statement, connection, transaction);
            await command.ExecuteNonQueryAsync(ct);
        }
    }
}
