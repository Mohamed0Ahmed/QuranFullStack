using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Infrastructure.Background;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.Http;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Api.Linking;

public sealed class LinkingTestFixture : IAsyncLifetime
{
    private const string SeedResourceSuffix = "mushaf-reader-seed.sql";
    private readonly FakeExternalUserProfileSource profileSource = new();
    private readonly SmokeSqlCommandCapture commandCapture = new();
    private PostgreSqlDatabaseLease? databaseLease;
    private WebApplicationFactory<HealthController>? standardFactory;
    private WebApplicationFactory<HealthController>? pausedConfirmationFactory;
    private WebApplicationFactory<HealthController>? pausedWorkersFactory;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        databaseLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(LinkingTestFixture));
        ConnectionString = databaseLease.ConnectionString;
        try
        {
            await SeedMushafSliceAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        await DisposeFactoriesAsync();
        if (databaseLease is not null)
        {
            await databaseLease.DisposeAsync();
            databaseLease = null;
        }
    }

    public HttpClient CreateClient()
    {
        standardFactory ??= SmokeApiHost.Build(
            ConnectionString,
            profileSource,
            commandCapture);
        return SmokeApiHost.CreateClient(standardFactory);
    }

    public HttpClient CreatePausedConfirmationClient()
    {
        pausedConfirmationFactory ??= SmokeApiHost.Build(
                ConnectionString,
                profileSource,
                commandCapture)
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                var processor = services.Single(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService)
                    && descriptor.ImplementationType == typeof(LinkingConfirmationJobProcessorService));
                services.Remove(processor);
            }));
        return SmokeApiHost.CreateClient(pausedConfirmationFactory);
    }

    public HttpClient CreatePausedWorkersClient()
    {
        pausedWorkersFactory ??= BuildPausedWorkersFactory();
        return SmokeApiHost.CreateClient(pausedWorkersFactory);
    }

    public Task ProcessNextConfirmationAsync() =>
        ProcessNextConfirmationAsync(
            pausedConfirmationFactory
                ?? throw new InvalidOperationException("The paused confirmation host is not running."));

    public Task ProcessNextPausedPreflightAsync() =>
        ProcessNextPreflightAsync(
            pausedWorkersFactory
                ?? throw new InvalidOperationException("The paused workers host is not running."));

    public Task ProcessNextPausedConfirmationAsync() =>
        ProcessNextConfirmationAsync(
            pausedWorkersFactory
                ?? throw new InvalidOperationException("The paused workers host is not running."));

    public async Task RestartPausedWorkersAsync()
    {
        if (pausedWorkersFactory is null)
        {
            throw new InvalidOperationException("The paused workers host is not running.");
        }

        await pausedWorkersFactory.DisposeAsync();
        pausedWorkersFactory = null;
    }

    public async Task ClaimAndExpireConfirmationLeaseAsync(Guid expectedJobId)
    {
        var factory = pausedWorkersFactory
            ?? throw new InvalidOperationException("The paused workers host is not running.");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var lease = await scope.ServiceProvider
                .GetRequiredService<ILinkingConfirmationJobStore>()
                .ClaimAsync(CancellationToken.None);
            lease.Should().NotBeNull();
            lease!.JobId.Should().Be(expectedJobId);
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE linking_confirmation_jobs
            SET lease_expires_at_utc = CURRENT_TIMESTAMP - INTERVAL '1 second'
            WHERE id = @job_id
            """;
        command.Parameters.AddWithValue("job_id", expectedJobId);
        (await command.ExecuteNonQueryAsync()).Should().Be(1);
    }

    public async Task<int> ReadConfirmationAttemptCountAsync(Guid jobId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT attempt_count FROM linking_confirmation_jobs WHERE id = @job_id";
        command.Parameters.AddWithValue("job_id", jobId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task RemoveInclusionSynchronizationContributionAsync(int inclusionId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DELETE FROM linking_source_contributions
            WHERE door_inclusion_id = @inclusion_id
            """;
        command.Parameters.AddWithValue("inclusion_id", inclusionId);
        (await command.ExecuteNonQueryAsync()).Should().Be(1);
    }

    public async Task<JsonElement> PollDataAsync(
        HttpClient client,
        string path,
        string resourceKind,
        string resourceId,
        Func<JsonElement, bool> completed,
        TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        JsonElement last = default;
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync(path);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            last = await ApiEnvelope.ReadDataAsync(response);
            if (completed(last))
            {
                return last;
            }

            await Task.Delay(50);
        }

        var sanitizedLogs = string.Join(" | ", SanitizedCommandTail());
        throw new TimeoutException(
            $"Timed out waiting for resourceKind={resourceKind}; resourceId={resourceId}; "
            + $"lastBusinessState={DescribeBusinessState(last)}; sanitizedSqlTail={sanitizedLogs}");
    }

    public async Task ResetAsync()
    {
        await DisposeFactoriesAsync();
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "TRUNCATE users, abwab_sections, abwab_doors, abwab_door_aliases, abwab_door_relations, "
            + "abwab_door_inclusions, abwab_door_inclusion_unit_syncs, abwab_templates, abwab_template_nodes, "
            + "linking_confirmation_jobs, linking_operations, linking_prepared_affected_contributions, "
            + "linking_prepared_ayah_descriptions, linking_prepared_ayah_words, linking_prepared_ayahs, "
            + "linking_prepared_units, linking_prepared_sources, linking_prepared_preflights, "
            + "linking_source_contribution_units, linking_source_contributions, linking_unit_ayah_descriptions, "
            + "linking_unit_ayah_words, linking_unit_ayahs, linking_units, linking_door_ayah_words, "
            + "linking_door_ayahs RESTART IDENTITY CASCADE;";
        await command.ExecuteNonQueryAsync();
        profileSource.Reset();
        commandCapture.Reset();
    }

    public IReadOnlyList<string> SanitizedCommandTail()
    {
        return commandCapture.CommandTexts
            .TakeLast(3)
            .Select(command => string.Join(
                ' ',
                command.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
            .Select(command => command.Length <= 240 ? command : command[..240])
            .ToArray();
    }

    public async Task<LinkingPersistentStateCounts> ReadPersistentStateCountsAsync(int doorId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                (SELECT COUNT(*) FROM linking_prepared_preflights WHERE door_id = @door_id),
                (SELECT COUNT(*) FROM linking_confirmation_jobs WHERE door_id = @door_id),
                (SELECT COUNT(*) FROM linking_operations WHERE door_id = @door_id),
                (SELECT COUNT(*) FROM linking_source_contributions WHERE door_id = @door_id),
                (SELECT COUNT(*) FROM linking_units WHERE door_id = @door_id),
                (SELECT COUNT(*) FROM linking_door_ayahs WHERE door_id = @door_id)
            """;
        command.Parameters.AddWithValue("door_id", doorId);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new LinkingPersistentStateCounts(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2),
            reader.GetInt64(3),
            reader.GetInt64(4),
            reader.GetInt64(5));
    }

    private async Task DisposeFactoriesAsync()
    {
        if (pausedWorkersFactory is not null)
        {
            await pausedWorkersFactory.DisposeAsync();
            pausedWorkersFactory = null;
        }
        if (pausedConfirmationFactory is not null)
        {
            await pausedConfirmationFactory.DisposeAsync();
            pausedConfirmationFactory = null;
        }
        if (standardFactory is not null)
        {
            await standardFactory.DisposeAsync();
            standardFactory = null;
        }
    }

    private WebApplicationFactory<HealthController> BuildPausedWorkersFactory() =>
        SmokeApiHost.Build(ConnectionString, profileSource, commandCapture)
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                RemoveHostedService<LinkingPreparedPreflightProcessorService>(services);
                RemoveHostedService<LinkingConfirmationJobProcessorService>(services);
            }));

    private static void RemoveHostedService<TService>(IServiceCollection services)
    {
        var descriptor = services.Single(candidate =>
            candidate.ServiceType == typeof(IHostedService)
            && candidate.ImplementationType == typeof(TService));
        services.Remove(descriptor);
    }

    private static async Task ProcessNextPreflightAsync(
        WebApplicationFactory<HealthController> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var processed = await scope.ServiceProvider
            .GetRequiredService<ILinkingPreparedPreflightProcessor>()
            .ProcessOneAsync(CancellationToken.None);
        if (!processed)
        {
            throw new InvalidOperationException("The paused host did not find a queued preflight.");
        }
    }

    private static async Task ProcessNextConfirmationAsync(
        WebApplicationFactory<HealthController> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var processed = await scope.ServiceProvider
            .GetRequiredService<ILinkingConfirmationJobProcessor>()
            .ProcessNextAsync(CancellationToken.None);
        if (!processed)
        {
            throw new InvalidOperationException("The paused host did not find a queued confirmation job.");
        }
    }

    private static string DescribeBusinessState(JsonElement data)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            return "not-observed";
        }

        var status = data.TryGetProperty("status", out var statusValue)
            ? statusValue.GetString() ?? "unknown"
            : "unknown";
        var stage = data.TryGetProperty("stage", out var stageValue)
            ? stageValue.GetString() ?? "unknown"
            : "unknown";
        var failureCode = data.TryGetProperty("failureCode", out var failureValue)
            && failureValue.ValueKind == JsonValueKind.String
                ? failureValue.GetString() ?? "unknown"
                : "none";
        return $"status:{status},stage:{stage},failureCode:{failureCode}";
    }

    private async Task SeedMushafSliceAsync()
    {
        var assembly = typeof(LinkingTestFixture).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith(SeedResourceSuffix, StringComparison.Ordinal));
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded seed script '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var seedSql = await reader.ReadToEndAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(seedSql, connection);
        await command.ExecuteNonQueryAsync();
    }

}

[CollectionDefinition(nameof(LinkingCollection))]
public sealed class LinkingCollection : ICollectionFixture<LinkingTestFixture>;

public sealed record LinkingPersistentStateCounts(
    long Preflights,
    long ConfirmationJobs,
    long Operations,
    long SourceContributions,
    long Units,
    long DoorAyahs);
