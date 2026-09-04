using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Quran.MushafReader;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Smoke.Data;

public sealed class SmokeDataFixture : IAsyncLifetime
{
    private readonly SmokeSqlCommandCapture commandCapture = new();
    private readonly PersistentTestDatabaseReader database = new(guarded: true);
    private readonly FakeExternalUserProfileSource profileSource = new();

    private WebApplicationFactory<HealthController>? apiFactory;

    public async Task InitializeAsync()
    {
        await database.InitializeAsync();
        apiFactory = SmokeApiHost.Build(
            database.BaseConnectionString,
            profileSource,
            commandCapture,
            readOnlySharedState: true);
    }

    public async Task DisposeAsync()
    {
        if (apiFactory is not null)
        {
            await apiFactory.DisposeAsync();
            apiFactory = null;
        }

        await database.DisposeAsync();
    }

    public HttpClient CreateClient() => SmokeApiHost.CreateClient(
        apiFactory
        ?? throw new InvalidOperationException(
            $"{nameof(SmokeDataFixture)} has not been initialized. Ensure it is used as an ICollectionFixture."));

    internal IServiceProvider ApiServices => (apiFactory
        ?? throw new InvalidOperationException(
            $"{nameof(SmokeDataFixture)} has not been initialized. Ensure it is used as an ICollectionFixture."))
        .Services;

    internal QuranFidelityOracle Oracle { get; } = QuranFidelityOracleDocument.ReadOracle();

    internal async Task<IReadOnlyDictionary<string, int>> CountRowsAsync(IEnumerable<string> tables)
    {
        var expectedTables = Oracle.RowCounts.Keys.ToHashSet(StringComparer.Ordinal);
        var requestedTables = tables.ToArray();
        var unexpected = requestedTables.FirstOrDefault(table => !expectedTables.Contains(table));
        if (unexpected is not null)
        {
            throw new InvalidOperationException(
                $"Canonical reader oracle contains no reviewed row-count expectation for '{unexpected}'.");
        }

        await using var connection = new NpgsqlConnection(database.ReadOnlyConnectionString);
        await connection.OpenAsync();

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var table in requestedTables)
        {
            await using var command = new NpgsqlCommand(
                $"SELECT count(*) FROM public.\"{table}\";",
                connection);
            counts[table] = Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        return counts;
    }
}
