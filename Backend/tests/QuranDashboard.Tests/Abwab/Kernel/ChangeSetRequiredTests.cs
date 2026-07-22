using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Audit;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

[Collection(nameof(AbwabDbCollection))]
public sealed class ChangeSetRequiredTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await AbwabKernelHarness.ResetKernelStateAsync(fixture);
        await AbwabKernelHarness.EnsureFixtureTableAsync(fixture);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Write_WithoutTrackedChangeSet_IsRejected()
    {
        await using var context = AbwabKernelHarness.CreateGuardedContext(fixture, AbwabPersonalDeletePolicy.Default);
        context.Fixtures.Add(new KernelFixtureWriteTarget { Id = Guid.NewGuid(), Label = "no-change-set" });

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<AbwabWriteWithoutChangeSetException>();
    }

    [Fact]
    public async Task Write_WithTrackedChangeSet_Succeeds()
    {
        var id = Guid.NewGuid();

        await using (var context = AbwabKernelHarness.CreateGuardedContext(fixture, AbwabPersonalDeletePolicy.Default))
        {
            context.Fixtures.Add(new KernelFixtureWriteTarget { Id = id, Label = "with-change-set" });
            context.ChangeSets.Add(NewChangeSet(sequence: 1));

            await context.SaveChangesAsync();
        }

        await using var verify = AbwabKernelHarness.CreateGuardedContext(fixture, AbwabPersonalDeletePolicy.Default);
        (await verify.Fixtures.FindAsync(id)).Should().NotBeNull();
    }

    private static ChangeSet NewChangeSet(long sequence) => new()
    {
        Id = Guid.NewGuid(),
        TimelineGeneration = 0,
        ChangeSetSequence = sequence,
        ActorSubject = "tester",
        CreatedAtUtc = DateTimeOffset.UnixEpoch,
    };
}
