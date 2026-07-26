using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Infrastructure.Abwab.Restore;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateAliasTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RemoveThenRestore_IsTrackedSoftDelete_AndKeepsTheSameAliasRow()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (_, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 0);

        var aliasId = await writePort.AddTemplateNodeAliasAsync(
            new AddTemplateNodeAliasCommand(nodes[0].TemplateNodeId, "مرادف", ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        await writePort.RemoveTemplateNodeAliasAsync(
            new RemoveTemplateNodeAliasCommand(aliasId, (await ReloadAsync(db, aliasId)).Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        (await ReloadAsync(db, aliasId)).IsDeleted.Should().BeTrue();

        await writePort.RestoreTemplateNodeAliasAsync(
            new RestoreTemplateNodeAliasCommand(aliasId, (await ReloadAsync(db, aliasId)).Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var restored = await ReloadAsync(db, aliasId);
        restored.IsDeleted.Should().BeFalse();
        restored.DeletedAtUtc.Should().BeNull();
        (await db.Set<TemplateNodeSearchAlias>().AsNoTracking().CountAsync()).Should().Be(1, "restore reuses the row, it never writes a second one");
    }

    [Fact]
    public async Task APhysicalAliasDelete_IsRejectedByThe028Guard()
    {
        var (_, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 0);
        var alias = AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(nodes[0].TemplateNodeId, "مرادف");
        await AbwabTreeSeeding.InsertAsync(fixture, alias);

        await using var db = AbwabKernelHarness.CreateProductionContext(
            fixture, new QuranDashboard.Infrastructure.Abwab.Persistence.AbwabWriteGuardInterceptor(
                QuranDashboard.Infrastructure.Abwab.Persistence.AbwabPersonalDeletePolicy.Default));
        db.Set<TemplateNodeSearchAlias>().Remove(
            await db.Set<TemplateNodeSearchAlias>().SingleAsync(a => a.TemplateNodeSearchAliasId == alias.TemplateNodeSearchAliasId));
        db.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());

        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<AbwabPhysicalDeleteRejectedException>();
    }

    [Fact]
    public async Task AliasHistory_IncludingSoftDeletedRows_SurvivesAnAdapterRoundTrip()
    {
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 0);
        var active = AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(nodes[0].TemplateNodeId, "مرادف فعّال");
        var removed = AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(nodes[0].TemplateNodeId, "مرادف مُزال");
        removed.IsDeleted = true;
        removed.DeletedAtUtc = DateTimeOffset.UnixEpoch.AddHours(3);
        await AbwabTreeSeeding.InsertAsync(fixture, active, removed);

        var adapter = new DoorTemplateRestoreAdapter();
        var aggregate = await AbwabRelationshipTemplateSeeding.LoadTemplateAggregateAsync(fixture, template.DoorTemplateId);

        var reconstructed = adapter.Reconstruct(adapter.Capture(aggregate));

        reconstructed.Aliases.Should().HaveCount(2, "no alias history is lost");
        reconstructed.Aliases.Single(a => a.TemplateNodeSearchAliasId == removed.TemplateNodeSearchAliasId)
            .IsDeleted.Should().BeTrue();
    }

    private static async Task<TemplateNodeSearchAlias> ReloadAsync(QuranDashboardDbContext db, Guid aliasId) =>
        await db.Set<TemplateNodeSearchAlias>().AsNoTracking().SingleAsync(a => a.TemplateNodeSearchAliasId == aliasId);
}
