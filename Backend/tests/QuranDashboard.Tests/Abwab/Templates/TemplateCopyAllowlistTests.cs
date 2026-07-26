using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Relationships;
using QuranDashboard.Domain.Abwab.Tree;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Templates;

[Collection(nameof(AbwabDbCollection))]
public sealed class TemplateCopyAllowlistTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    // Asserts the allowlist field-by-field against the PERSISTED rows: every field Category carries
    // TODAY is pinned either to the template's value or to its fresh default, so changing what an
    // application copies from an existing field fails here. A brand-new Category field is NOT caught
    // automatically — the enumeration is by name — so adding one means adding its pin below.
    [Fact]
    public async Task EveryFieldOfAProducedCategory_IsEitherACopiedAllowlistFieldOrAFreshDefault()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;

        var template = AbwabRelationshipTemplateSeeding.NewDoorTemplate("قالب النسخ");
        var root = AbwabRelationshipTemplateSeeding.NewTemplateNode(
            template.DoorTemplateId, "جذر القالب", siblingOrder: 0, representativeQuranExcerpt: "مقطع تمثيلي", description: "وصف الجذر");
        var child = AbwabRelationshipTemplateSeeding.NewTemplateNode(
            template.DoorTemplateId, "فرع القالب", siblingOrder: 0, parentTemplateNodeId: root.TemplateNodeId, description: "وصف الفرع");
        var alias = AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(root.TemplateNodeId, "مرادف الجذر");
        var removedAlias = AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(root.TemplateNodeId, "مرادف مُزال");
        removedAlias.IsDeleted = true;
        removedAlias.DeletedAtUtc = DateTimeOffset.UnixEpoch;

        await AbwabTreeSeeding.InsertAsync(fixture, template);
        await AbwabTreeSeeding.InsertAsync(fixture, root, child);
        await AbwabTreeSeeding.InsertAsync(fixture, alias, removedAlias);

        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);

        var createdRoot = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.ParentCategoryId == target.CategoryId);
        var createdChild = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.ParentCategoryId == createdRoot.CategoryId);

        createdRoot.Name.Should().Be(root.Name);
        createdRoot.NormalizedName.Should().Be(ArabicNameNormalizer.Normalize(root.Name));
        createdRoot.RepresentativeQuranExcerpt.Should().Be(root.RepresentativeQuranExcerpt);
        createdRoot.Description.Should().Be(root.Description);
        createdRoot.SiblingOrder.Should().Be(root.SiblingOrder);
        createdRoot.ParentCategoryId.Should().Be(target.CategoryId);
        createdChild.Name.Should().Be(child.Name);
        createdChild.Description.Should().Be(child.Description);
        createdChild.ParentCategoryId.Should().Be(createdRoot.CategoryId);

        // ACTIVE aliases only: a removed alias is template history, not content.
        (await db.AbwabCategorySearchAliases.AsNoTracking()
            .Where(a => a.CategoryId == createdRoot.CategoryId).Select(a => a.Value).ToListAsync())
            .Should().BeEquivalentTo([alias.Value]);

        foreach (var created in new[] { createdRoot, createdChild })
        {
            created.CategoryId.Should().NotBe(Guid.Empty).And.NotBe(target.CategoryId);
            created.SectionId.Should().BeNull("a produced category is a descendant, never a section root");
            created.SectionOrder.Should().BeNull();
            created.GlobalOrder.Should().BeNull();
            created.CategoryContentRevision.Should().Be(0);
            created.OrdinaryProtectionActorSubject.Should().BeNull();
            created.OrdinaryProtectionLastEditedAtUtc.Should().BeNull("applying a template starts no 24-hour window (§9)");
            created.DeletionOperationId.Should().BeNull();
            created.IsDeleted.Should().BeFalse();
            created.DeletedAtUtc.Should().BeNull();
        }

        createdRoot.AncestorIds.Should().Equal([.. target.AncestorIds, target.CategoryId]);
        createdRoot.Depth.Should().Be(target.Depth + 1);
        createdChild.Depth.Should().Be(createdRoot.Depth + 1);
    }

    // The target is seeded WITH a row of each forbidden family first, and the assertion is scoped to
    // the ids the application created: a global before/after count would pass on an empty database
    // even if the handler copied every forbidden row it could reach.
    [Theory]
    [InlineData(typeof(CategoryRelationship))]
    [InlineData(typeof(ManualProtection))]
    public async Task NoForbiddenChildFamilyRowIsEverCopied(Type forbiddenFamily)
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 1);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);
        var neighbour = (await AbwabRelationshipTemplateSeeding.ManyCategoryEndpointsAsync(fixture, count: 1, label: "طرف مجاور"))[0];

        var (lower, higher) = CategoryRelationship.Canonicalize(target.CategoryId, neighbour.CategoryId);
        await AbwabTreeSeeding.InsertAsync(
            fixture,
            AbwabRelationshipTemplateSeeding.NewMutualRelationship(lower, higher),
            // QuranContent, not InternalStructure: the target must carry a protection row that does
            // NOT block the application, or the operation under test never runs.
            AbwabTreeSeeding.NewManualProtection(target.CategoryId, ManualProtectionType.QuranContent, ManualProtectionScope.CategoryOnly));

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);

        var createdIds = await db.AbwabCategories.AsNoTracking()
            .Where(c => c.AncestorIds.Contains(target.CategoryId))
            .Select(c => c.CategoryId)
            .ToListAsync();
        createdIds.Should().HaveCount(4, "2 roots each with 1 child were produced, so the scoped assertion below is non-vacuous");

        (await CountForFamilyAsync(db, forbiddenFamily, createdIds)).Should().Be(0,
            $"the copy allowlist fails CLOSED: no {forbiddenFamily.Name} row is ever attached to a produced category");
    }

    // A template node may legally hold two aliases normalizing to the same value (§7.4 defines no
    // template-alias uniqueness rule), but the category alias index IS unique — copying both would
    // fail the constraint as an unmapped 500 instead of a §11 conflict.
    [Fact]
    public async Task TwoTemplateAliasesNormalizingToTheSameValue_ProduceExactlyOneCategoryAlias()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, nodes) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 1, depth: 0);

        await AbwabTreeSeeding.InsertAsync(
            fixture,
            AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(nodes[0].TemplateNodeId, "الصبر"),
            AbwabRelationshipTemplateSeeding.NewTemplateNodeAlias(nodes[0].TemplateNodeId, "الصــبر"));

        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);

        var createdRoot = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.ParentCategoryId == target.CategoryId);
        (await db.AbwabCategorySearchAliases.AsNoTracking().CountAsync(a => a.CategoryId == createdRoot.CategoryId))
            .Should().Be(1, "the duplicate normalized alias collapses instead of violating the unique category-alias index");
    }

    [Fact]
    public async Task TheProducedCategories_HaveNoNotificationOrAuditHistoryOfTheirOwnBeyondTheOneApplicationEvent()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateTemplateWritePort(fixture);
        await using var _ = db;
        var (template, _) = await AbwabRelationshipTemplateSeeding.DeepTemplateAsync(fixture, rootCount: 2, depth: 2);
        var (target, _) = await AbwabRelationshipTemplateSeeding.CategorySubtreeAsync(fixture, childCount: 0);
        var auditEventsBefore = await db.AbwabAuditEvents.AsNoTracking().CountAsync();

        await AbwabWriterTestHarness.ApplyTemplateAsync(writePort, db, template.DoorTemplateId, target.CategoryId);

        (await db.AbwabAuditEvents.AsNoTracking().CountAsync() - auditEventsBefore).Should().Be(1,
            "one application is one audited event — no per-created-category history is copied or synthesised");
        (await db.AbwabNotificationRecords.AsNoTracking().CountAsync()).Should().Be(0);
    }

    private static async Task<int> CountForFamilyAsync(
        QuranDashboardDbContext db,
        Type forbiddenFamily,
        IReadOnlyList<Guid> createdCategoryIds) =>
        forbiddenFamily == typeof(CategoryRelationship)
            ? await db.AbwabCategoryRelationships.AsNoTracking().CountAsync(r =>
                (r.LowerCategoryId != null && createdCategoryIds.Contains(r.LowerCategoryId.Value)) ||
                (r.HigherCategoryId != null && createdCategoryIds.Contains(r.HigherCategoryId.Value)) ||
                (r.SourceCategoryId != null && createdCategoryIds.Contains(r.SourceCategoryId.Value)) ||
                (r.TargetCategoryId != null && createdCategoryIds.Contains(r.TargetCategoryId.Value)))
            : await db.AbwabManualProtections.AsNoTracking().CountAsync(p => createdCategoryIds.Contains(p.CategoryId));
}
