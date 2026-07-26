using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateConcurrencyTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AStaleExpectedTemplateRevision_MapsToTemplateRevisionStale_AndTouchesNoRow()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 0);
        var node = await ReloadAsync(db, nodes[1].TemplateNodeId);

        var act = () => writePort.ReparentTemplateNodeAsync(
            new ReparentTemplateNodeCommand(
                node.TemplateNodeId, nodes[0].TemplateNodeId, node.Version, template.TemplateRevision + 5,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.TemplateRevisionStale);

        (await ReloadAsync(db, node.TemplateNodeId)).ParentTemplateNodeId.Should().BeNull();
    }

    [Fact]
    public async Task AStaleExpectedRowVersion_MapsToRowStale()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 0);

        var act = () => writePort.ReparentTemplateNodeAsync(
            new ReparentTemplateNodeCommand(
                nodes[1].TemplateNodeId, nodes[0].TemplateNodeId, 999999, template.TemplateRevision,
                ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RowStale);
    }

    // §15.3 "Restore ABA": a stale generation is refused before the operation body runs, so no row and
    // no counter may move.
    [Fact]
    public async Task AStaleExpectedTimelineGeneration_MapsToTimelineGenerationStale_WithZeroRowsTouched()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 0);
        var node = await ReloadAsync(db, nodes[1].TemplateNodeId);

        var act = () => writePort.ReparentTemplateNodeAsync(
            new ReparentTemplateNodeCommand(
                node.TemplateNodeId, nodes[0].TemplateNodeId, node.Version, template.TemplateRevision,
                ExpectedTimelineGeneration.Of(99), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabTimelineGenerationStaleException>())
            .Which.Code.Should().Be(AbwabConflictCodes.TimelineGenerationStale);

        (await ReloadAsync(db, node.TemplateNodeId)).ParentTemplateNodeId.Should().BeNull();
        (await CurrentTemplateRevisionAsync(db, template.DoorTemplateId)).Should().Be(template.TemplateRevision);
    }

    [Fact]
    public async Task TwoConcurrentStructuralWritesOnTheSameExpectedRevision_YieldOneSuccessAndNoMerge()
    {
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 3, depth: 0);

        await using var readDb = AbwabKernelHarness.CreateProductionContext(fixture);
        var second = await ReloadAsync(readDb, nodes[1].TemplateNodeId);
        var third = await ReloadAsync(readDb, nodes[2].TemplateNodeId);

        var (firstPort, firstDb) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        var (secondPort, secondDb) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = firstDb;
        await using var __ = secondDb;

        var outcomes = await Task.WhenAll(
            AttemptAsync(() => firstPort.ReparentTemplateNodeAsync(
                new ReparentTemplateNodeCommand(second.TemplateNodeId, nodes[0].TemplateNodeId, second.Version, template.TemplateRevision, ExpectedTimelineGeneration.Of(0), "tester"),
                CancellationToken.None)),
            AttemptAsync(() => secondPort.ReparentTemplateNodeAsync(
                new ReparentTemplateNodeCommand(third.TemplateNodeId, nodes[0].TemplateNodeId, third.Version, template.TemplateRevision, ExpectedTimelineGeneration.Of(0), "tester"),
                CancellationToken.None)));

        outcomes.Count(succeeded => succeeded).Should().Be(1, "the expected TemplateRevision serialises structural writes — there is no merge");
        (await CurrentTemplateRevisionAsync(readDb, template.DoorTemplateId)).Should().Be(template.TemplateRevision + 1);
    }

    private static async Task<bool> AttemptAsync(Func<Task> operation)
    {
        try
        {
            await operation();
            return true;
        }
        catch (AbwabWriteConflictException)
        {
            return false;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private static async Task<TemplateNode> ReloadAsync(QuranDashboardDbContext db, Guid nodeId) =>
        await db.Set<TemplateNode>().AsNoTracking().SingleAsync(n => n.TemplateNodeId == nodeId);

    private static async Task<long> CurrentTemplateRevisionAsync(QuranDashboardDbContext db, Guid templateId) =>
        (await db.Set<DoorTemplate>().AsNoTracking().SingleAsync(t => t.DoorTemplateId == templateId)).TemplateRevision;
}
