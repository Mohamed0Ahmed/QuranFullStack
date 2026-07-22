using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

[Collection(nameof(AbwabDbCollection))]
public sealed class CachePublicationTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync() => await AbwabKernelHarness.ResetKernelStateAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SuccessfulCommit_Publishes_OnlyAfterTheRowIsDurable()
    {
        var visibleAtPublish = false;
        var publisher = new RecordingCachePublisher(async commit =>
            visibleAtPublish = await ChangeSetExistsAsync(commit.ChangeSetId));

        await AbwabKernelHarness.CommitAsync(fixture, AbwabKernelHarness.Request(expectedGeneration: 0), publisher);

        publisher.Published.Should().ContainSingle();
        visibleAtPublish.Should().BeTrue("publication must occur only after the transaction commits");
    }

    [Fact]
    public async Task RolledBackCommit_DoesNotPublish()
    {
        var publisher = new RecordingCachePublisher();
        var request = new AbwabWriteRequest(
            ExpectedTimelineGeneration.Of(0),
            "tester",
            [new AbwabAuditEventDraft(1, "a"), new AbwabAuditEventDraft(1, "duplicate")]);

        var act = () => AbwabKernelHarness.CommitAsync(fixture, request, publisher);

        await act.Should().ThrowAsync<Exception>();
        publisher.Published.Should().BeEmpty("a rolled-back write must never publish caches");
    }

    [Fact]
    public async Task ProviderRetries_AreLockedOff()
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);

        context.Database.CreateExecutionStrategy().RetriesOnFailure.Should().BeFalse(
            "retrying a manual Abwab transaction would re-run non-idempotent work");
    }

    private async Task<bool> ChangeSetExistsAsync(Guid changeSetId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM abwab_change_sets WHERE id = @id";
        command.Parameters.AddWithValue("id", changeSetId);
        return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
    }
}
