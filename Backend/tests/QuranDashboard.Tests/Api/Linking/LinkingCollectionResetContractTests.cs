using System.Net.Http.Json;
using QuranDashboard.TestRuntime;
using QuranDashboard.Tests.Api.Access;
using QuranDashboard.Tests.TestRuntime;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Linking;

[Collection(nameof(MutableDatabaseCollection))]
public sealed class LinkingCollectionResetContractTests(AccessTestFixture fixture)
    : LinkingMutableWriterTest(fixture)
{
    [Fact]
    public async Task RestartScenarioAsync_ClearsCompleteLinkingStateAndPreservesProtectedStateAndSequences()
    {
        var contract = DatabaseContractReader.Read(TestRuntimeTestPaths.ContractPath);
        var protectedFingerprint = await Fixture.ComputeProtectedStateFingerprintAsync();
        using var client = CreateClient();
        var scenario = new LinkingTestScenario(this, client, AccessTestFixture.OwnerSub);
        scenario.ConfigureOwner();
        await scenario.ProvisionOwnerAsync();
        await AddWorkspaceSourceAsync(client);
        var doorId = await scenario.CreateTargetDoorAsync("reset-contract");
        var prepared = await scenario.PrepareReadyPreflightAsync(doorId, ProcessNextPreflightAsync);
        using var acceptedResponse = await client.PostAsJsonAsync(
            $"/api/linking/preflights/{prepared.Id}/confirmation-jobs",
            new { preflightToken = prepared.Token, idempotencyKey = Guid.NewGuid() });
        acceptedResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = await ApiEnvelope.ReadDataAsync(acceptedResponse);
        var jobId = accepted.GetProperty("job").GetProperty("jobId").GetGuid();
        await ProcessNextConfirmationAsync();
        _ = await scenario.PollConfirmationAsync(jobId, status => status == "succeeded");
        (await ReadPersistentStateCountsAsync(doorId)).Should().Be(
            new LinkingPersistentStateCounts(1, 1, 1, 1, 1, 1));
        var sequences = await ReadLinkingSequenceValuesAsync();

        await Fixture.RestartScenarioAsync();

        await AssertLinkingResetMatchesContractAsync(contract);
        (await ReadLinkingSequenceValuesAsync()).Should().BeEquivalentTo(sequences);
        (await Fixture.ComputeProtectedStateFingerprintAsync()).Should().Be(protectedFingerprint);
    }

    private static async Task AddWorkspaceSourceAsync(HttpClient client)
    {
        using var loadResponse = await client.GetAsync("/api/linking/workspace");
        loadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var addResponse = await client.PostAsJsonAsync(
            "/api/linking/workspace/sources",
            new
            {
                descriptor = LinkingTestScenario.ManualSourceDescriptor(),
                initialConfiguration = new
                {
                    inclusionMode = "all_except",
                    ayahOverrides = Array.Empty<int>(),
                    selectedWords = Array.Empty<object>(),
                    automaticWordMatchesEnabled = (bool?)null,
                    manualLinkShape = "independent",
                    descriptions = Array.Empty<object>(),
                },
                workspaceVersion = (uint?)null,
            });
        addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task AssertLinkingResetMatchesContractAsync(DatabaseContract contract)
    {
        string[] expectedTables =
        [
            "linking_workspaces",
            "linking_workspace_sources",
            "linking_workspace_source_manual_ayahs",
            "linking_workspace_source_ayah_overrides",
            "linking_workspace_source_words",
            "linking_workspace_source_descriptions",
            "linking_operations",
            "linking_confirmation_jobs",
            "linking_door_ayahs",
            "linking_door_ayah_words",
            "linking_source_contributions",
            "linking_units",
            "linking_source_contribution_units",
            "linking_unit_ayahs",
            "linking_unit_ayah_words",
            "linking_unit_ayah_descriptions",
            "linking_data_state",
            "linking_prepared_preflights",
            "linking_prepared_sources",
            "linking_prepared_units",
            "linking_prepared_ayahs",
            "linking_prepared_ayah_words",
            "linking_prepared_ayah_descriptions",
            "linking_prepared_affected_contributions",
        ];
        contract.DataClasses.MutableApplicationState
            .Where(table => table.StartsWith("linking_", StringComparison.Ordinal))
            .Should().BeEquivalentTo(expectedTables);

        await using var connection = new NpgsqlConnection(Fixture.ApplicationConnectionString);
        await connection.OpenAsync();
        foreach (var table in expectedTables.Where(table => table != contract.LinkingDataBaseline.Table))
        {
            await using var count = new NpgsqlCommand($"SELECT count(*) FROM public.\"{table}\"", connection);
            Convert.ToInt64(await count.ExecuteScalarAsync()).Should().Be(0, table);
        }

        await using var singleton = new NpgsqlCommand(
            $"SELECT id, generation, updated_at_utc FROM public.\"{contract.LinkingDataBaseline.Table}\"",
            connection);
        await using var reader = await singleton.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.GetInt64(0).Should().Be(contract.LinkingDataBaseline.Id);
        reader.GetInt64(1).Should().Be(contract.LinkingDataBaseline.Generation);
        reader.GetFieldValue<DateTimeOffset>(2).Should().Be(contract.LinkingDataBaseline.UpdatedAtUtc);
        (await reader.ReadAsync()).Should().BeFalse();
    }

    private async Task<IReadOnlyDictionary<string, long?>> ReadLinkingSequenceValuesAsync()
    {
        await using var connection = new NpgsqlConnection(Fixture.ApplicationConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT sequencename, last_value
            FROM pg_catalog.pg_sequences
            WHERE schemaname = 'public'
              AND sequencename LIKE 'linking\_%' ESCAPE '\'
            ORDER BY sequencename
            """,
            connection);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new Dictionary<string, long?>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetInt64(1));
        }

        return values;
    }
}
