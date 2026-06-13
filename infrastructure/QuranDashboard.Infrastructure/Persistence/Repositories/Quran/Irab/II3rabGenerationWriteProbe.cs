using Npgsql;

namespace QuranDashboard.Infrastructure.Persistence.Repositories.Quran.Irab;

/// <summary>
/// Optional hook invoked after segment i‘rab columns are staged but before the hard-check gate runs.
/// Production registers a no-op; tests replace it to inject deliberate validation violations.
/// </summary>
public interface II3rabGenerationWriteProbe
{
    Task AfterSegmentUpdateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct);
}
