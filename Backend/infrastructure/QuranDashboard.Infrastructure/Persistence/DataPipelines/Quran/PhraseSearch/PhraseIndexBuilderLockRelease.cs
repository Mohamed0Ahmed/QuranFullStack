namespace QuranDashboard.Infrastructure.Persistence.DataPipelines.Quran.PhraseSearch;

internal static class PhraseIndexBuilderLockRelease
{
    internal static async Task ReleaseAsync(
        PhraseIndexBuildDatabase database,
        NpgsqlConnection? connection,
        PhraseIndexBuildRun run)
    {
        if (!run.BuilderLockHeld
            || connection is null
            || connection.State != ConnectionState.Open)
        {
            return;
        }

        try
        {
            await database.ReleaseBuilderLockAsync(connection);
        }
        catch (Exception)
        {
            try
            {
                await connection.DisposeAsync();
            }
            catch (Exception)
            {
            }

            try
            {
                NpgsqlConnection.ClearPool(connection);
            }
            catch (Exception)
            {
            }
        }
    }
}
