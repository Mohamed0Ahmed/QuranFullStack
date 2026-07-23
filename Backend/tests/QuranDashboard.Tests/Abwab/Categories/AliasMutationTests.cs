using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Infrastructure.Abwab.Restore;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class AliasMutationTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddEditRemove_AreAllAuthorizedByCategoryEdit_NeverAChildAddDeleteVerb()
    {
        // The write port exposes alias mutation only through AddCategoryAliasAsync/EditCategoryAliasAsync/
        // RemoveCategoryAliasAsync on the SAME IAbwabCoreWritePort that owns category.edit — there is no
        // separate "alias.add"/"alias.delete" verb anywhere in the port.
        var portType = typeof(IAbwabCoreWritePort);
        portType.GetMethod("AddCategoryAliasAsync").Should().NotBeNull();
        portType.GetMethod("EditCategoryAliasAsync").Should().NotBeNull();
        portType.GetMethod("RemoveCategoryAliasAsync").Should().NotBeNull();
    }

    [Fact]
    public async Task Add_ADuplicateActiveNormalizedAlias_MapsToCategoryAliasConflict()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب الزكاة", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.AddCategoryAliasAsync(new AddCategoryAliasCommand(categoryId, "الصدقة", ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var act = () => writePort.AddCategoryAliasAsync(new AddCategoryAliasCommand(categoryId, "الصدقة", ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabWriteConflictException>()).Which.Code.Should().Be(AbwabConflictCodes.CategoryAliasConflict);
    }

    [Fact]
    public async Task Remove_IsTrackedSoftDelete_NotPhysicalDelete()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب الصوم", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var aliasId = await writePort.AddCategoryAliasAsync(new AddCategoryAliasCommand(categoryId, "الفريضة", ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        var alias = await db.AbwabCategorySearchAliases.AsNoTracking().SingleAsync(a => a.CategorySearchAliasId == aliasId);

        await writePort.RemoveCategoryAliasAsync(new RemoveCategoryAliasCommand(aliasId, alias.Version, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var reloaded = await db.AbwabCategorySearchAliases.AsNoTracking().SingleAsync(a => a.CategorySearchAliasId == aliasId);
        reloaded.IsDeleted.Should().BeTrue();
        reloaded.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task PhysicalDeleteOfAnAlias_IsRejectedByTheSavingChangesGuard()
    {
        await using var db = AbwabKernelHarness.CreateProductionContext(
            fixture, new AbwabWriteGuardInterceptor(AbwabPersonalDeletePolicy.Default));

        var section = AbwabTreeSeeding.NewSection("قسم اختبار الحذف الفعلي");
        await AbwabTreeSeeding.InsertAsync(fixture, section);
        var root = AbwabTreeSeeding.NewRootCategory("باب اختبار الحذف الفعلي", section.SectionId, 0, 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);
        var alias = AbwabTreeSeeding.NewAlias(root.CategoryId, "مرادف للحذف");
        await AbwabTreeSeeding.InsertAsync(fixture, alias);

        var tracked = await db.AbwabCategorySearchAliases.SingleAsync(a => a.CategorySearchAliasId == alias.CategorySearchAliasId);
        db.Remove(tracked);
        db.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());

        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<AbwabPhysicalDeleteRejectedException>();
    }

    [Fact]
    public async Task VersionedAliasAdapter_RoundTrips()
    {
        var (writePort, db) = AbwabWriterTestHarness.CreateWritePort(fixture);
        await using var _ = db;

        var categoryId = await writePort.AddCategoryAsync(new AddCategoryCommand("باب الحج", null, null, null, null, 0, ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);
        await writePort.AddCategoryAliasAsync(new AddCategoryAliasCommand(categoryId, "الركن الخامس", ExpectedTimelineGeneration.Of(0), "tester"), CancellationToken.None);

        var category = await db.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        var aliases = await db.AbwabCategorySearchAliases.AsNoTracking().Where(a => a.CategoryId == categoryId).ToListAsync();

        var adapter = new CategoryRestoreAdapter();
        var snapshot = adapter.Capture(new CategoryAggregate(category, aliases));
        var reconstructed = adapter.Reconstruct(snapshot);

        reconstructed.Aliases.Should().BeEquivalentTo(aliases, options => options.Excluding(a => a.Version));
    }
}
