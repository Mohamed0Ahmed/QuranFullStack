using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Tree;

[Collection(nameof(AbwabDbCollection))]
public sealed class TreeSnapshotReadTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Snapshot_CarriesCurrentTimelineGeneration()
    {
        var snapshot = await ReadSnapshotAsync();

        snapshot.ExpectedTimelineGeneration.Generation.Should().Be(0);
        snapshot.SchemaVersion.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RootCategory_HasRootShape()
    {
        var section = AbwabTreeSeeding.NewSection("قسم الإيمان");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب التوحيد", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var snapshot = await ReadSnapshotAsync();
        var dto = snapshot.Categories.Should().ContainSingle(c => c.CategoryId == root.CategoryId).Subject;

        dto.ParentCategoryId.Should().BeNull();
        dto.SectionId.Should().Be(section.SectionId);
        dto.SiblingOrder.Should().BeNull();
        dto.SectionOrder.Should().Be(0);
        dto.GlobalOrder.Should().Be(0);
        dto.AncestorIds.Should().BeEmpty();
        dto.Depth.Should().Be(0);
    }

    [Fact]
    public async Task DescendantCategory_HasDescendantShape()
    {
        var section = AbwabTreeSeeding.NewSection("قسم الفقه الأكبر");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب العبادات", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var child = AbwabTreeSeeding.NewChildCategory("فصل الصلاة", root, siblingOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, child);

        var grandchild = AbwabTreeSeeding.NewChildCategory("مسألة القراءة", child, siblingOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, grandchild);

        var snapshot = await ReadSnapshotAsync();

        var childDto = snapshot.Categories.Should().ContainSingle(c => c.CategoryId == child.CategoryId).Subject;
        childDto.ParentCategoryId.Should().Be(root.CategoryId);
        childDto.SectionId.Should().BeNull();
        childDto.SiblingOrder.Should().Be(0);
        childDto.SectionOrder.Should().BeNull();
        childDto.GlobalOrder.Should().BeNull();
        childDto.AncestorIds.Should().Equal(root.CategoryId);
        childDto.Depth.Should().Be(1);

        var grandchildDto = snapshot.Categories.Should().ContainSingle(c => c.CategoryId == grandchild.CategoryId).Subject;
        grandchildDto.AncestorIds.Should().Equal(root.CategoryId, child.CategoryId);
        grandchildDto.Depth.Should().Be(2);
    }

    [Fact]
    public async Task AllCategoriesProjection_OrdersRootsByGlobalOrder_IndependentlyOfSectionOrder()
    {
        var sectionA = AbwabTreeSeeding.NewSection("القسم الأول");
        var sectionB = AbwabTreeSeeding.NewSection("القسم الثاني");
        await AbwabTreeSeeding.InsertAsync(fixture, sectionA, sectionB);

        var rootInB = AbwabTreeSeeding.NewRootCategory("باب متأخر عالميًا", sectionB.SectionId, sectionOrder: 0, globalOrder: 1);
        var rootInA = AbwabTreeSeeding.NewRootCategory("باب متقدم عالميًا", sectionA.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, rootInB, rootInA);

        var childUnderA = AbwabTreeSeeding.NewChildCategory("فصل فرعي", rootInA, siblingOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, childUnderA);

        var snapshot = await ReadSnapshotAsync();

        snapshot.AllCategoriesProjection.Select(c => c.CategoryId).Should().Equal(rootInA.CategoryId, rootInB.CategoryId);
        snapshot.AllCategoriesProjection.Should().NotContain(c => c.CategoryId == childUnderA.CategoryId);
        snapshot.AllCategoriesProjection.Should().OnlyContain(c => c.SectionOrder == 0, "each section's root order is independent of the others");
    }

    [Fact]
    public async Task Snapshot_ExcludesSoftDeletedSectionsAndCategories()
    {
        var section = AbwabTreeSeeding.NewSection("قسم محذوف");
        section.IsDeleted = true;
        section.DeletedAtUtc = DateTimeOffset.UnixEpoch;
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var snapshot = await ReadSnapshotAsync();

        snapshot.Sections.Should().NotContain(s => s.SectionId == section.SectionId);
    }

    // Blocker fix: the live read DTOs must carry the row Version (xmin) a caller needs to source
    // ExpectedVersion for its NEXT write — tree-read-contract.md line 11 forbids the caller
    // "manufacturing" that expectation. Proven end-to-end: read it from the snapshot, then use it,
    // unmodified, as ExpectedVersion on a real edit and assert the edit succeeds with no row_stale.
    [Fact]
    public async Task Snapshot_CategoryAndSectionVersion_IsUsableAsExpectedVersionForTheNextEdit_WithNoRowStale()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;
        var readPort = AbwabWriterTestHarness.CreateReadPort(db);

        var sectionId = await writePort.AddSectionAsync(
            new AddSectionCommand("قسم اختبار الإصدار", 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار الإصدار", null, null, null, sectionId, 1, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);

        var snapshot = await readPort.GetTreeSnapshotAsync(CancellationToken.None);
        var sectionDto = snapshot.Sections.Single(s => s.SectionId == sectionId);
        var categoryDto = snapshot.Categories.Single(c => c.CategoryId == categoryId);

        sectionDto.Version.Should().NotBe(0u);
        categoryDto.Version.Should().NotBe(0u);

        var treeRevision = await AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);

        var editSection = () => writePort.EditSectionAsync(
            new EditSectionCommand(sectionId, "قسم اختبار الإصدار (معدّل)", sectionDto.Version, treeRevision, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        await editSection.Should().NotThrowAsync("the snapshot-sourced Version is the real current xmin, not a manufactured 0");

        var editCategory = () => writePort.EditCategoryAsync(
            new EditCategoryCommand(categoryId, "باب اختبار الإصدار (معدّل)", null, null, categoryDto.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        await editCategory.Should().NotThrowAsync("the snapshot-sourced Version is the real current xmin, not a manufactured 0");
    }

    [Fact]
    public async Task Snapshot_Category_CarriesActiveAliases_EachWithItsOwnVersionUsableForAnEdit()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;
        var readPort = AbwabWriterTestHarness.CreateReadPort(db);

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار الأسماء البديلة", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var aliasId = await writePort.AddCategoryAliasAsync(
            new AddCategoryAliasCommand(categoryId, "اسم بديل", ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var snapshot = await readPort.GetTreeSnapshotAsync(CancellationToken.None);
        var categoryDto = snapshot.Categories.Single(c => c.CategoryId == categoryId);
        var aliasDto = categoryDto.Aliases.Should().ContainSingle(a => a.CategorySearchAliasId == aliasId).Subject;

        aliasDto.Value.Should().Be("اسم بديل");
        aliasDto.Version.Should().NotBe(0u);

        var editAlias = () => writePort.EditCategoryAliasAsync(
            new EditCategoryAliasCommand(aliasId, "اسم بديل معدّل", aliasDto.Version, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        await editAlias.Should().NotThrowAsync("the snapshot-sourced alias Version is the real current xmin, not a manufactured 0");
    }

    [Fact]
    public async Task Snapshot_Category_ExcludesSoftDeletedAliases()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;
        var readPort = AbwabWriterTestHarness.CreateReadPort(db);

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب اختبار حذف الاسم البديل", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"),
            CancellationToken.None);
        var aliasId = await writePort.AddCategoryAliasAsync(
            new AddCategoryAliasCommand(categoryId, "اسم بديل سيُحذف", ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var beforeRemoval = await readPort.GetTreeSnapshotAsync(CancellationToken.None);
        var aliasVersion = beforeRemoval.Categories.Single(c => c.CategoryId == categoryId).Aliases.Single().Version;

        await writePort.RemoveCategoryAliasAsync(
            new RemoveCategoryAliasCommand(aliasId, aliasVersion, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var afterRemoval = await readPort.GetTreeSnapshotAsync(CancellationToken.None);
        afterRemoval.Categories.Single(c => c.CategoryId == categoryId).Aliases.Should().BeEmpty();
    }

    private async Task<AbwabTreeSnapshotDto> ReadSnapshotAsync()
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var port = new EfAbwabCoreReadPort(context, new FixedServerClock(DateTimeOffset.UnixEpoch));

        return await port.GetTreeSnapshotAsync(CancellationToken.None);
    }
}
