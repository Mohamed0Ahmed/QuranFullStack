using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

[Collection(nameof(AbwabDbCollection))]
public sealed class RevisionStateSeedTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync() => await AbwabKernelHarness.ResetKernelStateAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RevisionState_IsSeededExactlyOnce_AtZero()
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);

        var rows = await context.AbwabRevisionStates.AsNoTracking().ToListAsync();

        rows.Should().ContainSingle();
        var state = rows[0];
        state.Id.Should().Be(1);
        state.AuditHeadSequence.Should().Be(0);
        state.TimelineGeneration.Should().Be(0);
        state.TreeRevision.Should().Be(0);
    }

    [Fact]
    public async Task AuditHead_IncrementsMonotonically_UnderRowLock()
    {
        var first = await AbwabKernelHarness.CommitAsync(fixture, AbwabKernelHarness.Request(expectedGeneration: 0));
        var second = await AbwabKernelHarness.CommitAsync(fixture, AbwabKernelHarness.Request(expectedGeneration: 0));

        first.ChangeSetSequence.Should().Be(1);
        second.ChangeSetSequence.Should().Be(2);
        (await AbwabKernelHarness.ReadAuditHeadAsync(fixture)).Should().Be(2);
    }
}
