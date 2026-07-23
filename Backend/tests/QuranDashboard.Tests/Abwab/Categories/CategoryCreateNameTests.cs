using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class CategoryCreateNameTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_ARootWithADuplicateNameInAnotherSection_MapsToCategoryNameConflict()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var sectionA = await writePort.AddSectionAsync(new AddSectionCommand("قسم أ", await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var sectionB = await writePort.AddSectionAsync(new AddSectionCommand("قسم ب", await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب الإيمان", null, null, null, sectionA, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revisionForConflict = await Rev(db);
        var act = () => writePort.AddCategoryAsync(
            new AddCategoryCommand("باب الإيمان", null, null, null, sectionB, revisionForConflict, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryNameConflict);
    }

    [Fact]
    public async Task Add_ASiblingWithADuplicateNameUnderTheSameParent_MapsToCategoryNameConflict()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var sectionId = await writePort.AddSectionAsync(new AddSectionCommand("قسم", await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var rootId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب الطهارة", null, null, null, sectionId, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.AddCategoryAsync(
            new AddCategoryCommand("فصل الوضوء", null, null, rootId, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revisionForConflict = await Rev(db);
        var act = () => writePort.AddCategoryAsync(
            new AddCategoryCommand("فصل الوضوء", null, null, rootId, null, revisionForConflict, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryNameConflict);
    }

    [Fact]
    public async Task Add_ASiblingWithTheSameNameUnderADifferentParent_Succeeds()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var sectionId = await writePort.AddSectionAsync(new AddSectionCommand("قسم", await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var parentOne = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب الطهارة", null, null, null, sectionId, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var parentTwo = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب الصلاة", null, null, null, sectionId, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await writePort.AddCategoryAsync(
            new AddCategoryCommand("فصل مشترك", null, null, parentOne, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var revisionForSecond = await Rev(db);
        var act = () => writePort.AddCategoryAsync(
            new AddCategoryCommand("فصل مشترك", null, null, parentTwo, null, revisionForSecond, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Rename_ToACollidingRootName_MapsToCategoryNameConflict()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var sectionId = await writePort.AddSectionAsync(new AddSectionCommand("قسم", await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب أول", null, null, null, sectionId, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var secondId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب ثان", null, null, null, sectionId, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var second = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == secondId);

        var act = () => writePort.EditCategoryAsync(
            new EditCategoryCommand(secondId, "باب أول", null, null, second.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryNameConflict);
    }

    [Fact]
    public async Task Add_ARootWithoutAnExplicitSectionId_LandsInThePermanentDefaultSection_AndAppendsBothRootOrders()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var permanentDefault = await db.AbwabSections.AsNoTracking().SingleAsync(s => s.IsPermanentDefault);

        var categoryId = await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب بلا قسم محدد", null, null, null, null, await Rev(db), ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var category = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);

        category.SectionId.Should().Be(permanentDefault.SectionId);
        category.SectionOrder.Should().NotBeNull();
        category.GlobalOrder.Should().NotBeNull();
        category.ParentCategoryId.Should().BeNull();
        category.AncestorIds.Should().BeEmpty();
        category.Depth.Should().Be(0);
    }

    [Fact]
    public async Task Add_UnderAMissingParent_MapsToCategoryUnavailable()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var act = () => writePort.AddCategoryAsync(
            new AddCategoryCommand("باب يتيم", null, null, Guid.NewGuid(), null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryUnavailable);
    }

    [Fact]
    public async Task Add_WithAStaleExpectedTreeRevision_MapsToTreeRevisionStale()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        await writePort.AddCategoryAsync(
            new AddCategoryCommand("باب أول", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var act = () => writePort.AddCategoryAsync(
            new AddCategoryCommand("باب ثان", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.TreeRevisionStale);
    }

    private static Task<long> Rev(QuranDashboardDbContext db) => AbwabWriterTestHarness.CurrentTreeRevisionAsync(db);
}
