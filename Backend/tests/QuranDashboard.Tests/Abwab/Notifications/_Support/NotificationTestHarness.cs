using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Infrastructure.Abwab.Notifications;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Notifications._Support;

internal static class NotificationTestHarness
{
    public static readonly IServerClock Clock = new FixedClock(DateTimeOffset.UnixEpoch);

    public static QuranDashboardDbContext CreateContext(PostgresFixture fixture) =>
        new(new DbContextOptionsBuilder<QuranDashboardDbContext>().UseNpgsql(fixture.ConnectionString).Options);

    public static Task ResetAsync(PostgresFixture fixture) => AbwabSubstrateReset.FullResetAsync(fixture);

    public static NotificationStorageWriter Writer(QuranDashboardDbContext db) => new(db, Clock);

    public static NotificationReadStateRepository ReadStates(QuranDashboardDbContext db) => new(db, Clock);

    public static NotificationWriteRequest Request(
        string sourceIdentity,
        string recipientSubject = "recipient-1",
        string payload = "payload") =>
        new(recipientSubject, sourceIdentity, payload);

    public static async Task<long> CountRecordsAsync(PostgresFixture fixture) =>
        await ScalarAsync<long>(fixture, "SELECT COUNT(*) FROM abwab_notification_records");

    public static async Task<long> CountReadStatesAsync(PostgresFixture fixture) =>
        await ScalarAsync<long>(fixture, "SELECT COUNT(*) FROM abwab_notification_read_states");

    public static async Task<long> CountChangeSetsAsync(PostgresFixture fixture) =>
        await ScalarAsync<long>(fixture, "SELECT COUNT(*) FROM abwab_change_sets");

    public static async Task<long> CountAuditEventsAsync(PostgresFixture fixture) =>
        await ScalarAsync<long>(fixture, "SELECT COUNT(*) FROM abwab_audit_events");

    public static async Task<long> CountSecurityAuditEventsAsync(PostgresFixture fixture) =>
        await ScalarAsync<long>(fixture, "SELECT COUNT(*) FROM security_audit_events");

    public static async Task<long> ReadAuditHeadAsync(PostgresFixture fixture) =>
        await ScalarAsync<long>(fixture, "SELECT audit_head_sequence FROM abwab_revision_state WHERE id = 1");

    public static async Task ExecuteRawAsync(PostgresFixture fixture, string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(PostgresFixture fixture, string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)Convert.ChangeType(await command.ExecuteScalarAsync() ?? default(T)!, typeof(T));
    }

    private sealed class FixedClock(DateTimeOffset now) : IServerClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }
}
