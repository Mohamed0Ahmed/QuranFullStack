using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Sections;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class AliasUniquenessTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DuplicateActiveAlias_WithinSameCategory_IsRejected()
    {
        var (section, root) = await SeedCategoryAsync("باب الزكاة", globalOrder: 0);

        var firstAlias = AbwabTreeSeeding.NewAlias(root.CategoryId, "الصدقة");
        await AbwabTreeSeeding.InsertAsync(fixture, firstAlias);

        var duplicateAlias = AbwabTreeSeeding.NewAlias(root.CategoryId, "الصدقة");

        var act = () => AbwabTreeSeeding.InsertAsync(fixture, duplicateAlias);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SameAliasValue_AcrossDifferentCategories_IsAllowed()
    {
        var (section, first) = await SeedCategoryAsync("باب الصوم", globalOrder: 0);
        var second = AbwabTreeSeeding.NewRootCategory("باب الحج", section.SectionId, sectionOrder: 1, globalOrder: 1);
        await AbwabTreeSeeding.InsertAsync(fixture, second);

        await AbwabTreeSeeding.InsertAsync(fixture, AbwabTreeSeeding.NewAlias(first.CategoryId, "الفريضة"));

        var act = () => AbwabTreeSeeding.InsertAsync(fixture, AbwabTreeSeeding.NewAlias(second.CategoryId, "الفريضة"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AliasValue_DoesNotParticipateInCategoryNameUniqueness()
    {
        var (section, named) = await SeedCategoryAsync("باب النكاح", globalOrder: 0);
        var other = AbwabTreeSeeding.NewRootCategory("باب الطلاق", section.SectionId, sectionOrder: 1, globalOrder: 1);
        await AbwabTreeSeeding.InsertAsync(fixture, other);

        var act = () => AbwabTreeSeeding.InsertAsync(fixture, AbwabTreeSeeding.NewAlias(other.CategoryId, "باب النكاح"));

        await act.Should().NotThrowAsync("aliases are separately owned rows with their own uniqueness scope");
    }

    [Fact]
    public async Task SoftDeletedAlias_DoesNotBlockReuseOfItsValue()
    {
        var (section, root) = await SeedCategoryAsync("باب البيوع", globalOrder: 0);

        var deletedAlias = AbwabTreeSeeding.NewAlias(root.CategoryId, "التجارة");
        deletedAlias.IsDeleted = true;
        deletedAlias.DeletedAtUtc = DateTimeOffset.UnixEpoch;
        await AbwabTreeSeeding.InsertAsync(fixture, deletedAlias);

        var act = () => AbwabTreeSeeding.InsertAsync(fixture, AbwabTreeSeeding.NewAlias(root.CategoryId, "التجارة"));

        await act.Should().NotThrowAsync();
    }

    private async Task<(Section Section, Category Root)> SeedCategoryAsync(
        string categoryName,
        int globalOrder)
    {
        var section = AbwabTreeSeeding.NewSection($"قسم {Guid.NewGuid():N}");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory(categoryName, section.SectionId, sectionOrder: 0, globalOrder: globalOrder);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        return (section, root);
    }
}
