using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

// FR-036 / SC-012 (T079): an injected audit/event failure rolls back all rows — no half-written ChangeSet
// and no orphan audit events — with the head unchanged (real PG).
[Collection(nameof(AbwabDbCollection))]
public sealed class AuditAtomicityTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync() => await AbwabKernelHarness.ResetKernelStateAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InjectedEventFailure_RollsBackTheWholeCommit()
    {
        // Duplicate EventOrdinal within one operation violates the unique (change_set_id, event_ordinal)
        // constraint — a real audit-write failure inside the transaction.
        var request = new AbwabWriteRequest(
            ExpectedTimelineGeneration.Of(0),
            "tester",
            [new AbwabAuditEventDraft(3, "first"), new AbwabAuditEventDraft(3, "duplicate")]);

        var act = () => AbwabKernelHarness.CommitAsync(fixture, request);

        await act.Should().ThrowAsync<Exception>();

        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        (await context.AbwabChangeSets.CountAsync()).Should().Be(0, "no half-written ChangeSet may survive");
        (await context.AbwabAuditEvents.CountAsync()).Should().Be(0);
        (await AbwabKernelHarness.ReadAuditHeadAsync(fixture)).Should().Be(0);
    }
}
