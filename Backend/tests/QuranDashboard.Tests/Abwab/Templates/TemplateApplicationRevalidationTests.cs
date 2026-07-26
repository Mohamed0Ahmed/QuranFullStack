using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Templates;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateApplicationRevalidationTests(PostgresFixture fixture) : IAsyncLifetime
{
    public enum StaleExpectation
    {
        TreeRevision,
        TargetVersion,
        TemplateRevision,
    }

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ADestinationNameCollision_MapsToCategoryNameConflict_AndCreatesNoPartialTree()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 1);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        var collidingChild = AbwabTreeSeeding.NewChildCategory(nodes[2].Name, target, siblingOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, collidingChild);

        await AssertRolledBackAsync(
            () => AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId),
            AbwabConflictCodes.CategoryNameConflict,
            db,
            target.CategoryId,
            expectedExistingDescendants: 1);
    }

    [Fact]
    public async Task ManualInternalStructureProtectionOnTheTarget_MapsToManualProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 1);
        var (target, _) = await AbwabRelationshipTemplateSeeding.ProtectedCategoryAsync(fixture, ManualProtectionType.InternalStructure);

        await AssertRolledBackAsync(
            () => AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId),
            AbwabConflictCodes.ManualProtection,
            db,
            target.CategoryId,
            expectedExistingDescendants: 0);
    }

    // The gate must NOT hang off the per-created-root path: a template with zero active nodes creates
    // nothing, so a recursion-scoped check would let it commit a TreeRevision bump and an audit event
    // against a protected door.
    [Fact]
    public async Task AnEmptyTemplateAgainstAProtectedTarget_IsStillBlockedByManualProtection()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var template = AbwabRelationshipTemplateSeeding.NewDoorTemplate("قالب بلا عقد");
        await AbwabTreeSeeding.InsertAsync(fixture, template);
        var (target, _) = await AbwabRelationshipTemplateSeeding.ProtectedCategoryAsync(fixture, ManualProtectionType.InternalStructure);

        await AssertRolledBackAsync(
            () => AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId),
            AbwabConflictCodes.ManualProtection,
            db,
            target.CategoryId,
            expectedExistingDescendants: 0);
    }

    [Fact]
    public async Task AnEmptyTemplateAgainstAnUnprotectedTarget_StillCommitsNothingButItsOwnAuditedOperation()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var template = AbwabRelationshipTemplateSeeding.NewDoorTemplate("قالب بلا عقد");
        await AbwabTreeSeeding.InsertAsync(fixture, template);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);
        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        var treeRevisionBefore = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);

        (await db.AbwabCategories.AsNoTracking().CountAsync(c => c.AncestorIds.Contains(target.CategoryId))).Should().Be(0);
        (await db.AbwabChangeSets.AsNoTracking().CountAsync() - changeSetsBefore).Should().Be(1,
            "an application is one audited operation even when the template contributes no node");
        (await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db)).Should().Be(treeRevisionBefore + 1);
    }

    [Fact]
    public async Task AMissingTarget_MapsToCategoryUnavailable()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 0);
        var missingTargetId = Guid.NewGuid();

        await AssertRolledBackAsync(
            () => AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, missingTargetId),
            AbwabConflictCodes.CategoryUnavailable,
            db,
            missingTargetId,
            expectedExistingDescendants: 0);
    }

    [Theory]
    [InlineData(StaleExpectation.TreeRevision, AbwabConflictCodes.TreeRevisionStale)]
    [InlineData(StaleExpectation.TargetVersion, AbwabConflictCodes.RowStale)]
    [InlineData(StaleExpectation.TemplateRevision, AbwabConflictCodes.TemplateRevisionStale)]
    public async Task AStaleExpectation_MapsToItsOwnConflictCode_AndRollsTheWholeApplicationBack(
        StaleExpectation staleExpectation,
        string expectedCode)
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 1);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        await AssertRolledBackAsync(
            () => AbwabWriterTestHarness.ApplyTemplateAsync(
                writePort,
                db,
                template.DoorTemplateId,
                target.CategoryId,
                expectedTreeRevision: staleExpectation == StaleExpectation.TreeRevision ? 999 : null,
                expectedTargetVersion: staleExpectation == StaleExpectation.TargetVersion ? 999999u : null,
                expectedTemplateRevision: staleExpectation == StaleExpectation.TemplateRevision ? 42 : null),
            expectedCode,
            db,
            target.CategoryId,
            expectedExistingDescendants: 0);
    }

    // §7.4 and template-application-contract.md: a cyclic template can never be applied. The ring is
    // seeded directly, because no writer path can produce one.
    [Fact]
    public async Task ACyclicTemplate_IsRejectedAsTemplateCycle_AndCreatesNothing()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.CyclicTemplateAsync(fixture);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        await AssertRolledBackAsync(
            () => AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId),
            AbwabConflictCodes.TemplateCycle,
            db,
            target.CategoryId,
            expectedExistingDescendants: 0);
    }

    [Fact]
    public async Task AStaleTimelineGeneration_IsRefusedBeforeAnythingIsWritten()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 1);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);
        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        var treeRevision = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var act = () => writePort.ApplyDoorTemplateAsync(
            new ApplyDoorTemplateCommand(
                template.DoorTemplateId, target.CategoryId, template.TemplateRevision,
                treeRevision, target.Version,
                ExpectedTimelineGeneration.Of(77), "tester"),
            CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabTimelineGenerationStaleException>())
            .Which.Code.Should().Be(AbwabConflictCodes.TimelineGenerationStale);
        (await db.AbwabChangeSets.AsNoTracking().CountAsync()).Should().Be(changeSetsBefore);
    }

    // The name guard runs INSIDE the one transaction, so a colliding child committed after the caller
    // read the tree is still caught rather than producing a duplicate sibling name.
    [Fact]
    public async Task AConcurrentlyCommittedCollidingChild_IsCaughtInsideTheTransaction()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 0);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        var treeRevision = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);
        await AbwabTreeSeeding.InsertAsync(fixture, AbwabTreeSeeding.NewChildCategory(nodes[0].Name, target, siblingOrder: 0));

        await AssertRolledBackAsync(
            () => AbwabWriterTestHarness.ApplyTemplateAsync(
                writePort, db, template.DoorTemplateId, target.CategoryId, expectedTreeRevision: treeRevision),
            AbwabConflictCodes.CategoryNameConflict,
            db,
            target.CategoryId,
            expectedExistingDescendants: 1);
    }

    private static async Task AssertRolledBackAsync(
        Func<Task> application,
        string expectedCode,
        QuranDashboardDbContext db,
        Guid targetCategoryId,
        int expectedExistingDescendants)
    {
        var changeSetsBefore = await db.AbwabChangeSets.AsNoTracking().CountAsync();
        var treeRevisionBefore = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        (await application.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(expectedCode);

        (await db.AbwabCategories.AsNoTracking().CountAsync(c => c.AncestorIds.Contains(targetCategoryId)))
            .Should().Be(expectedExistingDescendants, "a failed application leaves no partial tree");
        (await db.AbwabChangeSets.AsNoTracking().CountAsync()).Should().Be(changeSetsBefore, "a failed application writes no ChangeSet");
        (await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db)).Should().Be(treeRevisionBefore);
    }
}
