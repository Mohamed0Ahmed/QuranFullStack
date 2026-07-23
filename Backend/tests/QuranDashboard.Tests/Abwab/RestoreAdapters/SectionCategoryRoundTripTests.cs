using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Sections;
using QuranDashboard.Infrastructure.Abwab.Restore;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.RestoreAdapters;

[Collection(nameof(AbwabDbCollection))]
public sealed class SectionCategoryRoundTripTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly SectionRestoreAdapter SectionAdapter = new();
    private static readonly CategoryRestoreAdapter CategoryAdapter = new();

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SectionAdapter_RoundTrips_WriteSnapshotReconstruct_ToProductStateEquality()
    {
        var written = AbwabTreeSeeding.NewSection("قسم قابل للاستعادة", sortOrder: 7);
        await AbwabTreeSeeding.InsertAsync(fixture, written);

        var reloaded = await ReloadSectionAsync(written.SectionId);

        var snapshot = SectionAdapter.Capture(reloaded);
        var reconstructed = SectionAdapter.Reconstruct(snapshot);

        reconstructed.Should().BeEquivalentTo(reloaded, options => options.Excluding(s => s.Version));
    }

    [Fact]
    public async Task SectionAdapter_RoundTrips_ThePermanentDefaultSection()
    {
        var permanentDefault = await ReloadPermanentDefaultSectionAsync();

        var snapshot = SectionAdapter.Capture(permanentDefault);
        var reconstructed = SectionAdapter.Reconstruct(snapshot);

        reconstructed.IsPermanentDefault.Should().BeTrue();
        reconstructed.Should().BeEquivalentTo(permanentDefault, options => options.Excluding(s => s.Version));
    }

    [Fact]
    public async Task CategoryAdapter_RoundTrips_TheAggregate_IncludingOrderFacetsAndAliases()
    {
        var section = AbwabTreeSeeding.NewSection("قسم أصل الفئة");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب قابل للاستعادة", section.SectionId, sectionOrder: 3, globalOrder: 9);
        root.RepresentativeQuranExcerpt = "نص تمثيلي اصطناعي";
        root.Description = "وصف اصطناعي للاختبار";
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var child = AbwabTreeSeeding.NewChildCategory("فصل فرعي قابل للاستعادة", root, siblingOrder: 4);
        await AbwabTreeSeeding.InsertAsync(fixture, child);

        var activeAlias = AbwabTreeSeeding.NewAlias(child.CategoryId, "مرادف فعال");
        var deletedAlias = AbwabTreeSeeding.NewAlias(child.CategoryId, "مرادف محذوف");
        deletedAlias.IsDeleted = true;
        deletedAlias.DeletedAtUtc = DateTimeOffset.UnixEpoch;
        await AbwabTreeSeeding.InsertAsync(fixture, activeAlias, deletedAlias);

        var (reloadedCategory, reloadedAliases) = await ReloadCategoryAggregateAsync(child.CategoryId);

        var snapshot = CategoryAdapter.Capture(new CategoryAggregate(reloadedCategory, reloadedAliases));
        var reconstructed = CategoryAdapter.Reconstruct(snapshot);

        reconstructed.Category.Should().BeEquivalentTo(
            reloadedCategory,
            options => options.Excluding(c => c.Version));

        reconstructed.Aliases.Should().BeEquivalentTo(
            reloadedAliases,
            options => options.Excluding(a => a.Version));

        snapshot.SiblingOrder.Should().Be(4);
        snapshot.SectionOrder.Should().BeNull();
        snapshot.GlobalOrder.Should().BeNull();
    }

    [Fact]
    public async Task CategoryAdapter_RoundTrips_ARootCategorysIndependentOrderFacets()
    {
        var section = AbwabTreeSeeding.NewSection("قسم ترتيب الجذور");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب له ترتيب مستقل", section.SectionId, sectionOrder: 2, globalOrder: 11);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var (reloadedCategory, reloadedAliases) = await ReloadCategoryAggregateAsync(root.CategoryId);

        var snapshot = CategoryAdapter.Capture(new CategoryAggregate(reloadedCategory, reloadedAliases));

        snapshot.SiblingOrder.Should().BeNull();
        snapshot.SectionOrder.Should().Be(2);
        snapshot.GlobalOrder.Should().Be(11);
    }

    [Fact]
    public void SectionSnapshot_ExcludesXminAndLogicalCounters()
    {
        typeof(SectionRestoreSnapshot).GetProperties().Select(p => p.Name)
            .Should().NotContain(["Version", "Xmin"]);
    }

    [Fact]
    public void CategorySnapshot_ExcludesXminAndLogicalCounters()
    {
        typeof(CategoryRestoreSnapshot).GetProperties().Select(p => p.Name)
            .Should().NotContain(["Version", "Xmin", "CategoryContentRevision", "TreeRevision"]);
    }

    private async Task<Section> ReloadSectionAsync(Guid sectionId)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        return await context.AbwabSections.AsNoTracking().SingleAsync(s => s.SectionId == sectionId);
    }

    private async Task<Section> ReloadPermanentDefaultSectionAsync()
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        return await context.AbwabSections.AsNoTracking().SingleAsync(s => s.IsPermanentDefault);
    }

    private async Task<(Category Category, List<CategorySearchAlias> Aliases)> ReloadCategoryAggregateAsync(Guid categoryId)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);

        var category = await context.AbwabCategories.AsNoTracking().SingleAsync(c => c.CategoryId == categoryId);
        var aliases = await context.AbwabCategorySearchAliases
            .AsNoTracking()
            .Where(a => a.CategoryId == categoryId)
            .OrderBy(a => a.CategorySearchAliasId)
            .ToListAsync();

        return (category, aliases);
    }
}
