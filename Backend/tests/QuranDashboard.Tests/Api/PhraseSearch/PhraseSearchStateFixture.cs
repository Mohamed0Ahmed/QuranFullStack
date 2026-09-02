using Microsoft.AspNetCore.Mvc.Testing;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Api.PhraseSearch;

public sealed class PhraseSearchStateFixture : IAsyncLifetime
{
    private const int ActiveBuildFormatVersion = 2;
    private const string SourceFingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private readonly FakeExternalUserProfileSource profileSource = new();
    private readonly SmokeSqlCommandCapture commandCapture = new();
    private PostgreSqlDatabaseLease? databaseLease;
    private WebApplicationFactory<HealthController>? apiFactory;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        databaseLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(PhraseSearchStateFixture));
        ConnectionString = databaseLease.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (apiFactory is not null)
        {
            await apiFactory.DisposeAsync();
            apiFactory = null;
        }

        if (databaseLease is not null)
        {
            await databaseLease.DisposeAsync();
            databaseLease = null;
        }
    }

    public HttpClient CreateClient()
    {
        apiFactory ??= SmokeApiHost.Build(ConnectionString, profileSource, commandCapture);
        return SmokeApiHost.CreateClient(apiFactory);
    }

    public async Task ResetToMissingActiveStateAsync()
    {
        await DisposeFactoryAsync();
        await ExecuteAsync(
            """
            DELETE FROM quran_phrase_index_builds;
            UPDATE quran_phrase_index_state
            SET source_revision = 0,
                source_fingerprint = NULL,
                active_build_id = NULL,
                previous_build_id = NULL,
                is_stale = FALSE,
                stale_reason = NULL,
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE id = 1;
            """);
    }

    public async Task<Guid> CreateActiveBuildAsync(bool stale)
    {
        await DisposeFactoryAsync();
        var buildId = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM quran_phrase_index_builds;
            INSERT INTO quran_phrase_index_builds (
                id, status, format_version, exact_ready, similarity_ready, builder_version,
                source_revision, source_fingerprint, started_at_utc, validated_at_utc,
                activated_at_utc, failed_at_utc, completed_at_utc, search_token_count,
                variant_count, occurrence_count, similarity_edge_count,
                similarity_anchor_stat_count, validation_verdict, report_path, failure_summary)
            VALUES (
                @build_id, 3, @format_version, TRUE, TRUE, 'phrase-index-v2',
                1, @source_fingerprint, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP, NULL, CURRENT_TIMESTAMP, 0,
                0, 0, 0, 0, 'pass', NULL, NULL);
            UPDATE quran_phrase_index_state
            SET source_revision = 1,
                source_fingerprint = @source_fingerprint,
                active_build_id = @build_id,
                previous_build_id = NULL,
                is_stale = @is_stale,
                stale_reason = CASE WHEN @is_stale THEN 'test stale source state' ELSE NULL END,
                updated_at_utc = CURRENT_TIMESTAMP
            WHERE id = 1;
            """;
        command.Parameters.AddWithValue("build_id", buildId);
        command.Parameters.AddWithValue("format_version", ActiveBuildFormatVersion);
        command.Parameters.AddWithValue("source_fingerprint", SourceFingerprint);
        command.Parameters.AddWithValue("is_stale", stale);
        await command.ExecuteNonQueryAsync();
        return buildId;
    }

    private async Task DisposeFactoryAsync()
    {
        if (apiFactory is not null)
        {
            await apiFactory.DisposeAsync();
            apiFactory = null;
        }
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(nameof(PhraseSearchApiCollection))]
public sealed class PhraseSearchApiCollection : ICollectionFixture<PhraseSearchStateFixture>;
