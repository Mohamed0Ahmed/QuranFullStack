using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Api.Controllers.System;
using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.Smoke;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Api.Linking;

public sealed class LinkingTestFixture : IAsyncLifetime
{
    private const string ConfirmationProcessorService = "LinkingConfirmationJobProcessorService";
    private const string SeedResourceSuffix = "mushaf-reader-seed.sql";
    private readonly FakeExternalUserProfileSource profileSource = new();
    private readonly SmokeSqlCommandCapture commandCapture = new();
    private PostgreSqlDatabaseLease? databaseLease;
    private WebApplicationFactory<HealthController>? standardFactory;
    private WebApplicationFactory<HealthController>? pausedConfirmationFactory;

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
                    && descriptor.ImplementationType?.Name == ConfirmationProcessorService);
                services.Remove(processor);
            }));
        return SmokeApiHost.CreateClient(pausedConfirmationFactory);
    }

    public async Task ProcessNextConfirmationAsync()
    {
        var factory = pausedConfirmationFactory
            ?? throw new InvalidOperationException("The paused confirmation host is not running.");
        await using var scope = factory.Services.CreateAsyncScope();
        var processed = await scope.ServiceProvider
            .GetRequiredService<ILinkingConfirmationJobProcessor>()
            .ProcessNextAsync(CancellationToken.None);
        if (!processed)
        {
            throw new InvalidOperationException("The paused confirmation host did not find a queued job.");
        }
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

    private async Task DisposeFactoriesAsync()
    {
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
