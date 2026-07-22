using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

[Collection(nameof(AbwabDbCollection))]
public sealed class AuditHeadMonotonicityTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync() => await AbwabKernelHarness.ResetKernelStateAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConcurrentCommits_ReceiveStrictlyIncreasingSequences()
    {
        var first = AbwabKernelHarness.CommitAsync(fixture, AbwabKernelHarness.Request(expectedGeneration: 0));
        var second = AbwabKernelHarness.CommitAsync(fixture, AbwabKernelHarness.Request(expectedGeneration: 0));

        var results = await Task.WhenAll(first, second);

        results.Select(result => result.ChangeSetSequence).OrderBy(sequence => sequence).Should().Equal(1, 2);
        (await AbwabKernelHarness.ReadAuditHeadAsync(fixture)).Should().Be(2);
    }

    [Fact]
    public async Task Rollback_LeavesHeadGenerationAndTreeUnchanged()
    {
        var request = new AbwabWriteRequest(
            ExpectedTimelineGeneration.Of(0),
            "tester",
            [new AbwabAuditEventDraft(7, "a"), new AbwabAuditEventDraft(7, "b")]);

        var act = () => AbwabKernelHarness.CommitAsync(fixture, request);

        await act.Should().ThrowAsync<Exception>();

        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var state = await context.AbwabRevisionStates.AsNoTracking().SingleAsync();
        state.AuditHeadSequence.Should().Be(0);
        state.TimelineGeneration.Should().Be(0);
        state.TreeRevision.Should().Be(0);
    }
}
