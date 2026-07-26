using QuranDashboard.Domain.Abwab.Templates;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateApplicationShapeTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EveryTemplateRoot_IsCreatedAsADirectChildOfTheTarget()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 3, depth: 1);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);

        var directChildren = await db.AbwabCategories.AsNoTracking()
            .Where(c => c.ParentCategoryId == target.CategoryId)
            .ToListAsync();

        directChildren.Should().HaveCount(3, "each template ROOT becomes a direct child of the one target");
        directChildren.Should().OnlyContain(c => c.Depth == 1);
        directChildren.OrderBy(c => c.SiblingOrder).Select(c => c.Name).Should().Equal(
            await TemplateRootNamesInOrderAsync(db, template.DoorTemplateId),
            "order is an allowlisted copied field: the created roots follow the TEMPLATE roots' order");
        (await db.AbwabCategories.AsNoTracking().CountAsync(c => c.AncestorIds.Contains(target.CategoryId)))
            .Should().Be(6, "the whole structure is copied: 3 roots each with 1 child");
    }

    [Fact]
    public async Task OneApplication_ProducesExactlyOneChangeSet_AndExactlyOneTreeRevisionBump()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 2);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        var treeRevisionBefore = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);

        (await db.AbwabChangeSets.AsNoTracking().CountAsync() - changeSetsBefore).Should().Be(1,
            "the whole application is ONE audited operation, however many categories it created");
        (await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db)).Should().Be(treeRevisionBefore + 1,
            "TreeRevision bumps exactly once for a whole application");
    }

    [Fact]
    public async Task TheCreatedCategories_CarryTheirOwnFreshTechnicalState()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 1);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);

        var created = await db.AbwabCategories.AsNoTracking()
            .Where(c => c.AncestorIds.Contains(target.CategoryId))
            .ToListAsync();

        created.Should().OnlyContain(c => c.CategoryContentRevision == 0);
        created.Should().OnlyContain(c => c.DeletionOperationId == null && !c.IsDeleted);
        created.Should().OnlyContain(c => c.OrdinaryProtectionLastEditedAtUtc == null,
            "applying a template starts no ordinary 24-hour window (§9)");
    }

    private static async Task<IReadOnlyList<string>> TemplateRootNamesInOrderAsync(QuranDashboardDbContext db, Guid doorTemplateId) =>
        await db.Set<TemplateNode>().AsNoTracking()
            .Where(n => n.DoorTemplateId == doorTemplateId && n.ParentTemplateNodeId == null && !n.IsDeleted)
            .OrderBy(n => n.SiblingOrder)
            .Select(n => n.Name)
            .ToListAsync();
}
