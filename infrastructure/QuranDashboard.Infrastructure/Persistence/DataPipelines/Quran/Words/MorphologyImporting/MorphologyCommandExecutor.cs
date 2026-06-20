
namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.Words.MorphologyImporting;

// Thin Npgsql command helpers shared by the morphology import write/validation/totals paths. Kept in
// one place so the long-running CommandTimeout and scalar/non-query plumbing are not duplicated across
// the extracted helpers.
internal static class MorphologyCommandExecutor
{
    public const int CommandTimeoutSeconds = 600;

    public static async Task<int> ExecuteScalarIntAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public static async Task ExecuteNonQueryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken ct)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        await command.ExecuteNonQueryAsync(ct);
    }
}
