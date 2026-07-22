using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Security;
using QuranDashboard.Application.Security.Owners;
using QuranDashboard.Application.Security.Permissions;
using QuranDashboard.Domain.Security.Owners;
using QuranDashboard.Domain.Security.Permissions;
using QuranDashboard.Infrastructure.Security.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;

namespace QuranDashboard.Tests.Abwab._Support;

internal static class SecurityTestHarness
{
    public static readonly IServerClock Clock = new FixedClock(DateTimeOffset.UnixEpoch);

    public static QuranDashboardDbContext CreateContext(PostgresFixture fixture) =>
        new(new DbContextOptionsBuilder<QuranDashboardDbContext>().UseNpgsql(fixture.ConnectionString).Options);

    public static Task ResetAsync(PostgresFixture fixture) => AbwabSubstrateReset.FullResetAsync(fixture);

    public static async Task SetBarrierStabilizingAsync(PostgresFixture fixture)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await Exec(connection, "UPDATE abwab_write_barrier SET state = 1 WHERE id = 1");
    }

    public static async Task SetGenerationAsync(PostgresFixture fixture, long generation)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await Exec(connection, $"UPDATE abwab_revision_state SET timeline_generation = {generation} WHERE id = 1");
    }

    public static async Task<SecurityAuditCommitResult> GrantAsync(
        PostgresFixture fixture, GrantPermissionCommand command, IEffectivePermissionCache? cache = null)
    {
        await using var db = CreateContext(fixture);
        return await BuildPermissions(db, cache).GrantAsync(command, CancellationToken.None);
    }

    public static async Task<SecurityAuditCommitResult> RevokeAsync(
        PostgresFixture fixture, RevokePermissionCommand command, IEffectivePermissionCache? cache = null)
    {
        await using var db = CreateContext(fixture);
        return await BuildPermissions(db, cache).RevokeAsync(command, CancellationToken.None);
    }

    public static async Task<SecurityAuditCommitResult> AddOwnerAsync(PostgresFixture fixture, AddSystemOwnerCommand command)
    {
        await using var db = CreateContext(fixture);
        return await BuildOwners(db).AddAsync(command, CancellationToken.None);
    }

    public static async Task<SecurityAuditCommitResult> RemoveOwnerAsync(PostgresFixture fixture, RemoveSystemOwnerCommand command)
    {
        await using var db = CreateContext(fixture);
        return await BuildOwners(db).RemoveAsync(command, CancellationToken.None);
    }

    public static async Task<SecurityAuditCommitResult> BootstrapOwnerAsync(PostgresFixture fixture, BootstrapSystemOwnerCommand command)
    {
        await using var db = CreateContext(fixture);
        return await BuildOwners(db).BootstrapAsync(command, CancellationToken.None);
    }

    public static PermissionAdministrationHandler BuildPermissions(QuranDashboardDbContext db, IEffectivePermissionCache? cache = null) =>
        new(new SecurityAuditedCommitExecutor(db, Clock), new PermissionAssignmentStore(db), cache ?? new NoOpCache(), Clock);

    public static SystemOwnerAdministrationHandler BuildOwners(QuranDashboardDbContext db) =>
        new(new SecurityAuditedCommitExecutor(db, Clock), new SystemOwnerStore(db), Clock);

    public static EffectivePermissionResolver BuildResolver(QuranDashboardDbContext db, IEffectivePermissionCache cache) =>
        new(new PermissionAssignmentStore(db), new SystemOwnerStore(db), cache);

    public static async Task<long> ReadAuditHeadAsync(PostgresFixture fixture) =>
        await ScalarAsync<long>(fixture, "SELECT audit_head_sequence FROM abwab_revision_state WHERE id = 1");

    public static async Task<long> ReadSecurityEventCountAsync(PostgresFixture fixture) =>
        await ScalarAsync<long>(fixture, "SELECT COUNT(*) FROM security_audit_events");

    public static async Task<long> ReadChangeSetCountAsync(PostgresFixture fixture) =>
        await ScalarAsync<long>(fixture, "SELECT COUNT(*) FROM abwab_change_sets");

    public static async Task<int> ActiveOwnerCountAsync(PostgresFixture fixture) =>
        (int)await ScalarAsync<long>(fixture, "SELECT COUNT(*) FROM system_owner_memberships WHERE is_active AND is_account_enabled");

    private static async Task<T> ScalarAsync<T>(PostgresFixture fixture, string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (T)Convert.ChangeType(await command.ExecuteScalarAsync() ?? default(T)!, typeof(T));
    }

    private static async Task Exec(NpgsqlConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private sealed class FixedClock(DateTimeOffset now) : IServerClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    public sealed class RecordingCache : IEffectivePermissionCache
    {
        private readonly Dictionary<string, IReadOnlyList<string>> _entries = new(StringComparer.Ordinal);

        public int InvalidateCount { get; private set; }

        public Task<IReadOnlyList<string>?> GetAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult(_entries.TryGetValue(subject, out var value) ? value : null);

        public Task SetAsync(string subject, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
        {
            _entries[subject] = permissions;
            return Task.CompletedTask;
        }

        public void Invalidate()
        {
            InvalidateCount++;
            _entries.Clear();
        }
    }

    private sealed class NoOpCache : IEffectivePermissionCache
    {
        public Task<IReadOnlyList<string>?> GetAsync(string subject, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>?>(null);

        public Task SetAsync(string subject, IReadOnlyList<string> permissions, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public void Invalidate()
        {
        }
    }
}
