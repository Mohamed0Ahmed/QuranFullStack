using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Protection;

[Collection(nameof(AbwabDbCollection))]
public sealed class OneActiveRecordTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SecondActiveRecord_ForSameCategoryAndType_IsRejected()
    {
        var category = await SeedCategoryAsync();

        var first = AbwabTreeSeeding.NewManualProtection(category, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly);
        await AbwabTreeSeeding.InsertAsync(fixture, first);

        var second = AbwabTreeSeeding.NewManualProtection(category, ManualProtectionType.Deletion, ManualProtectionScope.Subtree);
        var act = () => AbwabTreeSeeding.InsertAsync(fixture, second);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SameCategory_DifferentType_BothActive_IsAllowed()
    {
        var category = await SeedCategoryAsync();

        var deletion = AbwabTreeSeeding.NewManualProtection(category, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly);
        var internalStructure = AbwabTreeSeeding.NewManualProtection(category, ManualProtectionType.InternalStructure, ManualProtectionScope.Subtree);

        var act = () => AbwabTreeSeeding.InsertAsync(fixture, deletion, internalStructure);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LiftedRecord_DoesNotBlockReapplyingTheSameType()
    {
        var category = await SeedCategoryAsync();

        var lifted = AbwabTreeSeeding.NewManualProtection(category, ManualProtectionType.Relationship, ManualProtectionScope.CategoryOnly);
        lifted.IsDeleted = true;
        lifted.DeletedAtUtc = DateTimeOffset.UnixEpoch;
        lifted.LiftedByActorSubject = "tester";
        lifted.LiftedAtUtc = DateTimeOffset.UnixEpoch;
        await AbwabTreeSeeding.InsertAsync(fixture, lifted);

        var reapplied = AbwabTreeSeeding.NewManualProtection(category, ManualProtectionType.Relationship, ManualProtectionScope.Subtree);
        var act = () => AbwabTreeSeeding.InsertAsync(fixture, reapplied);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ScopeIsAColumn_OnTheSingleActiveRecord_NotASecondRow()
    {
        var category = await SeedCategoryAsync();

        var applied = AbwabTreeSeeding.NewManualProtection(category, ManualProtectionType.QuranContent, ManualProtectionScope.CategoryOnly);
        await AbwabTreeSeeding.InsertAsync(fixture, applied);

        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var activeRows = await context.AbwabManualProtections
            .AsNoTracking()
            .Where(p => p.CategoryId == category && p.ProtectionType == ManualProtectionType.QuranContent && !p.IsDeleted)
            .ToListAsync();

        activeRows.Should().ContainSingle();
        activeRows[0].ProtectionScope.Should().Be(ManualProtectionScope.CategoryOnly);
    }

    private async Task<Guid> SeedCategoryAsync()
    {
        var section = AbwabTreeSeeding.NewSection("قسم اختبار سجل الحماية الفعال الوحيد");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب اختبار سجل الحماية الفعال الوحيد", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        return root.CategoryId;
    }
}
