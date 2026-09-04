using QuranDashboard.Application.Abstractions.Linking.ConfirmationJobs;
using QuranDashboard.Application.Abstractions.Linking.PreparedPreflights;
using QuranDashboard.Infrastructure.Testing.DatabaseActivity;
using QuranDashboard.Tests.Api.Access;

namespace QuranDashboard.Tests.Api.Linking;

public abstract class LinkingMutableWriterTest(
    AccessTestFixture fixture,
    IReadOnlyCollection<DatabaseBackgroundActivity>? backgroundActivities = null)
    : IAsyncLifetime, ILinkingDataPoller
{
    protected static readonly DatabaseBackgroundActivity[] PreparedPreflightProcessor =
    [
        DatabaseBackgroundActivity.LinkingPreparedPreflightProcessor,
    ];

    protected static readonly DatabaseBackgroundActivity[] LinkingProcessors =
    [
        DatabaseBackgroundActivity.LinkingPreparedPreflightProcessor,
        DatabaseBackgroundActivity.LinkingConfirmationJobProcessor,
    ];

    private ILinkingDataPoller? dataPoller;

    protected AccessTestFixture Fixture { get; } = fixture;

    public Task InitializeAsync() => Fixture.BeginScenarioAsync(backgroundActivities);

    public Task DisposeAsync() => Fixture.EndScenarioAsync();

    protected HttpClient CreateClient() => Fixture.CreateApiClient();

    protected async Task RestartApiAsync() => await Fixture.RestartScenarioApiAsync();

    protected Task ProcessNextPreflightAsync() => ProcessNextPreflightAsync(Fixture.Services);

    protected Task ProcessNextConfirmationAsync() => ProcessNextConfirmationAsync(Fixture.Services);

    protected async Task ClaimAndExpireConfirmationLeaseAsync(Guid expectedJobId)
    {
        await using (var scope = Fixture.Services.CreateAsyncScope())
        {
            var lease = await scope.ServiceProvider
                .GetRequiredService<ILinkingConfirmationJobStore>()
                .ClaimAsync(CancellationToken.None);
            lease.Should().NotBeNull();
            lease!.JobId.Should().Be(expectedJobId);
        }

        await using var connection = new NpgsqlConnection(Fixture.ApplicationConnectionString);
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

    protected async Task<int> ReadConfirmationAttemptCountAsync(Guid jobId)
    {
        await using var connection = new NpgsqlConnection(Fixture.ApplicationConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT attempt_count FROM linking_confirmation_jobs WHERE id = @job_id";
        command.Parameters.AddWithValue("job_id", jobId);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    protected async Task RemoveInclusionSynchronizationContributionAsync(int inclusionId)
    {
        await using var connection = new NpgsqlConnection(Fixture.ApplicationConnectionString);
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

    public Task<JsonElement> PollDataAsync(
        HttpClient client,
        string path,
        string resourceKind,
        string resourceId,
        Func<JsonElement, bool> completed,
        TimeSpan? timeout = null) =>
        (dataPoller ??= new LinkingDataPoller(SanitizedCommandTail)).PollDataAsync(
            client,
            path,
            resourceKind,
            resourceId,
            completed,
            timeout);

    protected async Task<LinkingPersistentStateCounts> ReadPersistentStateCountsAsync(int doorId)
    {
        await using var connection = new NpgsqlConnection(Fixture.ApplicationConnectionString);
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

    private IReadOnlyList<string> SanitizedCommandTail() => Fixture.CommandCapture.CommandTexts
        .TakeLast(3)
        .Select(command => string.Join(
            ' ',
            command.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)))
        .Select(command => command.Length <= 240 ? command : command[..240])
        .ToArray();

    private static async Task ProcessNextPreflightAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var processed = await scope.ServiceProvider
            .GetRequiredService<ILinkingPreparedPreflightProcessor>()
            .ProcessOneAsync(CancellationToken.None);
        if (!processed)
        {
            throw new InvalidOperationException("The paused host did not find a queued preflight.");
        }
    }

    private static async Task ProcessNextConfirmationAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var processed = await scope.ServiceProvider
            .GetRequiredService<ILinkingConfirmationJobProcessor>()
            .ProcessNextAsync(CancellationToken.None);
        if (!processed)
        {
            throw new InvalidOperationException("The paused host did not find a queued confirmation job.");
        }
    }
}

public sealed record LinkingPersistentStateCounts(
    long Preflights,
    long ConfirmationJobs,
    long Operations,
    long SourceContributions,
    long Units,
    long DoorAyahs);
