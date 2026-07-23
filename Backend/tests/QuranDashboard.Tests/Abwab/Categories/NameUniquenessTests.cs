using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;

namespace QuranDashboard.Tests.Abwab.Categories;

[Collection(nameof(AbwabDbCollection))]
public sealed class NameUniquenessTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ActiveSectionNames_AreUnique()
    {
        var first = AbwabTreeSeeding.NewSection("قسم التلاوة");
        await AbwabTreeSeeding.InsertAsync(fixture, first);

        var duplicate = AbwabTreeSeeding.NewSection("قسم التلاوة");

        var act = () => AbwabTreeSeeding.InsertAsync(fixture, duplicate);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ActiveRootCategoryNames_AreUnique_GloballyAcrossSections()
    {
        var sectionA = AbwabTreeSeeding.NewSection("القسم أ");
        var sectionB = AbwabTreeSeeding.NewSection("القسم ب");
        await AbwabTreeSeeding.InsertAsync(fixture, sectionA, sectionB);

        var rootInA = AbwabTreeSeeding.NewRootCategory("باب الإيمان", sectionA.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, rootInA);

        var rootInB = AbwabTreeSeeding.NewRootCategory("باب الإيمان", sectionB.SectionId, sectionOrder: 0, globalOrder: 1);

        var act = () => AbwabTreeSeeding.InsertAsync(fixture, rootInB);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ActiveSiblingCategoryNames_AreUnique_PerParentOnly()
    {
        var section = AbwabTreeSeeding.NewSection("قسم الفقه");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var parentOne = AbwabTreeSeeding.NewRootCategory("باب الطهارة", section.SectionId, sectionOrder: 0, globalOrder: 0);
        var parentTwo = AbwabTreeSeeding.NewRootCategory("باب الصلاة", section.SectionId, sectionOrder: 1, globalOrder: 1);
        await AbwabTreeSeeding.InsertAsync(fixture, parentOne, parentTwo);

        var childOfParentOne = AbwabTreeSeeding.NewChildCategory("فصل الوضوء", parentOne, siblingOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, childOfParentOne);

        var siblingDuplicate = AbwabTreeSeeding.NewChildCategory("فصل الوضوء", parentOne, siblingOrder: 1);
        var actSibling = () => AbwabTreeSeeding.InsertAsync(fixture, siblingDuplicate);
        await actSibling.Should().ThrowAsync<DbUpdateException>();

        var sameNameUnderOtherParent = AbwabTreeSeeding.NewChildCategory("فصل الوضوء", parentTwo, siblingOrder: 0);
        var actOtherParent = () => AbwabTreeSeeding.InsertAsync(fixture, sameNameUnderOtherParent);
        await actOtherParent.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SoftDeletedCategory_DoesNotBlockReuseOfItsName()
    {
        var section = AbwabTreeSeeding.NewSection("قسم الحديث");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var deletedRoot = AbwabTreeSeeding.NewRootCategory("باب الإسناد", section.SectionId, sectionOrder: 0, globalOrder: 0);
        deletedRoot.IsDeleted = true;
        deletedRoot.DeletedAtUtc = DateTimeOffset.UnixEpoch;
        await AbwabTreeSeeding.InsertAsync(fixture, deletedRoot);

        var activeRoot = AbwabTreeSeeding.NewRootCategory("باب الإسناد", section.SectionId, sectionOrder: 1, globalOrder: 1);

        var act = () => AbwabTreeSeeding.InsertAsync(fixture, activeRoot);

        await act.Should().NotThrowAsync();
    }
}
