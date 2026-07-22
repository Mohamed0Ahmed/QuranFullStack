using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab.Audit;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

// FR-012 / SC-002 (T025): physical/hard delete is rejected and soft-delete is enforced (real PG).
[Collection(nameof(AbwabDbCollection))]
public sealed class SoftDeleteTests(PostgresFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await AbwabKernelHarness.ResetKernelStateAsync(fixture);
        await AbwabKernelHarness.EnsureFixtureTableAsync(fixture);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PhysicalDelete_OfAuditableEntity_IsRejected()
    {
        var id = await SeedFixtureRowAsync();

        await using var context = AbwabKernelHarness.CreateGuardedContext(fixture, AbwabPersonalDeletePolicy.Default);
        var entity = await context.Fixtures.FindAsync(id);
        context.Fixtures.Remove(entity!);

        var act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<AbwabPhysicalDeleteRejectedException>();
    }

    [Fact]
    public async Task SoftDelete_WithChangeSet_Succeeds_AndRowRemains()
    {
        var id = await SeedFixtureRowAsync();

        await using (var context = AbwabKernelHarness.CreateGuardedContext(fixture, AbwabPersonalDeletePolicy.Default))
        {
            var entity = await context.Fixtures.FindAsync(id);
            entity!.IsDeleted = true;
            context.ChangeSets.Add(NewChangeSet(sequence: 2));

            await context.SaveChangesAsync();
        }

        await using var verify = AbwabKernelHarness.CreateGuardedContext(fixture, AbwabPersonalDeletePolicy.Default);
        var persisted = await verify.Fixtures.FindAsync(id);
        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeTrue();
    }

    private async Task<Guid> SeedFixtureRowAsync()
    {
        var id = Guid.NewGuid();
        await using var context = AbwabKernelHarness.CreateGuardedContext(fixture, AbwabPersonalDeletePolicy.Default);
        context.Fixtures.Add(new KernelFixtureWriteTarget { Id = id, Label = "seed" });
        context.ChangeSets.Add(NewChangeSet(sequence: 1));
        await context.SaveChangesAsync();
        return id;
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
