using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateReparentGuardTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SelfParenting_IsRejectedAsATemplateCycle()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 1);
        var node = await ReloadAsync(db, nodes[0].TemplateNodeId);

        var act = () => writePort.ReparentTemplateNodeAsync(
            Reparent(node.TemplateNodeId, node.TemplateNodeId, node.Version, template.TemplateRevision),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.TemplateCycle);
    }

    [Fact]
    public async Task ADestinationInsideTheMovedNodesDescendantTree_IsRejectedAsATemplateCycle()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 3);
        var root = await ReloadAsync(db, nodes[0].TemplateNodeId);

        var act = () => writePort.ReparentTemplateNodeAsync(
            Reparent(root.TemplateNodeId, nodes[3].TemplateNodeId, root.Version, template.TemplateRevision),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.TemplateCycle);

        var unchanged = await ReloadAsync(db, root.TemplateNodeId);
        unchanged.ParentTemplateNodeId.Should().BeNull("a rejected reparent must leave the parent chain untouched");
    }

    // The chain is walked from the DESTINATION upward inside the operation, so a concurrently
    // committed reparent that made the destination a descendant is still caught.
    [Fact]
    public async Task TheParentChain_IsValidatedUnderTheTransaction_AgainstAConcurrentlyCommittedReparent()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 1);
        var firstRoot = await ReloadAsync(db, nodes[0].TemplateNodeId);
        var secondRoot = await ReloadAsync(db, nodes[2].TemplateNodeId);

        var (otherWritePort, otherDb) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var __ = otherDb;
        await otherWritePort.ReparentTemplateNodeAsync(
            Reparent(secondRoot.TemplateNodeId, firstRoot.TemplateNodeId, secondRoot.Version, template.TemplateRevision),
            CancellationToken.None);

        var reloadedRevision = await CurrentTemplateRevisionAsync(db, template.DoorTemplateId);
        var act = () => writePort.ReparentTemplateNodeAsync(
            Reparent(firstRoot.TemplateNodeId, secondRoot.TemplateNodeId, firstRoot.Version, reloadedRevision),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.TemplateCycle);
    }

    [Fact]
    public async Task AValidReparentToARootPosition_IsAccepted()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 2);
        var leaf = await ReloadAsync(db, nodes[2].TemplateNodeId);

        await writePort.ReparentTemplateNodeAsync(
            Reparent(leaf.TemplateNodeId, null, leaf.Version, template.TemplateRevision),
            CancellationToken.None);

        (await ReloadAsync(db, leaf.TemplateNodeId)).ParentTemplateNodeId.Should().BeNull();
    }

    // A removed node is still a row the in-transaction read returns, so both destination paths must
    // reject it explicitly: a child placed under one would be an unreachable orphan that no restore
    // command and no application recursion can ever see again.
    [Fact]
    public async Task ReparentingUnderARemovedNode_IsRejectedAsRowStale()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 0);
        var removedId = await RemoveAsync(writePort, db, nodes[1].TemplateNodeId, template.TemplateRevision);

        var mover = await ReloadAsync(db, nodes[0].TemplateNodeId);
        var templateRevision = await CurrentTemplateRevisionAsync(db, template.DoorTemplateId);
        var act = () => writePort.ReparentTemplateNodeAsync(
            Reparent(mover.TemplateNodeId, removedId, mover.Version, templateRevision),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RowStale);

        (await ReloadAsync(db, mover.TemplateNodeId)).ParentTemplateNodeId.Should().BeNull();
    }

    [Fact]
    public async Task AddingANodeUnderARemovedParent_IsRejectedAsRowStale()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 0);
        var removedId = await RemoveAsync(writePort, db, nodes[0].TemplateNodeId, template.TemplateRevision);

        var templateRevision = await CurrentTemplateRevisionAsync(db, template.DoorTemplateId);
        var act = () => writePort.AddTemplateNodeAsync(
            new AddTemplateNodeCommand(
                template.DoorTemplateId, removedId, "عقدة يتيمة", null, null,
                templateRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>())
            .Which.Code.Should().Be(AbwabConflictCodes.RowStale);

        (await db.Set<TemplateNode>().AsNoTracking().CountAsync(n => n.ParentTemplateNodeId == removedId)).Should().Be(0);
    }

    private static async Task<Guid> RemoveAsync(
        IAbwabTemplateWritePort writePort,
        QuranDashboardDbContext db,
        Guid templateNodeId,
        long expectedTemplateRevision)
    {
        var node = await ReloadAsync(db, templateNodeId);
        await writePort.RemoveTemplateNodeAsync(
            new RemoveTemplateNodeCommand(
                node.TemplateNodeId, node.Version, expectedTemplateRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        return node.TemplateNodeId;
    }

    private static ReparentTemplateNodeCommand Reparent(Guid nodeId, Guid? newParentId, uint expectedVersion, long expectedTemplateRevision) =>
        new(nodeId, newParentId, expectedVersion, expectedTemplateRevision, ExpectedTimelineGeneration.Of(0), "tester");

    private static async Task<TemplateNode> ReloadAsync(QuranDashboardDbContext db, Guid nodeId) =>
        await db.Set<TemplateNode>().AsNoTracking().SingleAsync(n => n.TemplateNodeId == nodeId);

    private static async Task<long> CurrentTemplateRevisionAsync(QuranDashboardDbContext db, Guid templateId) =>
        (await db.Set<DoorTemplate>().AsNoTracking().SingleAsync(t => t.DoorTemplateId == templateId)).TemplateRevision;
}
