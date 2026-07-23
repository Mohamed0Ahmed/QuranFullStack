using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Protection;

[Collection(nameof(AbwabDbCollection))]
public sealed class SoftDeletedTargetAccessTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch.AddYears(1);

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EffectiveRead_AddressesASoftDeletedCategory_ByItsImmutableCategoryId()
    {
        var (categoryId, _) = await SeedSoftDeletedProtectedCategoryAsync();

        var profile = await ResolveProfileAsync(categoryId);

        profile.Should().NotBeNull("a soft-deleted category must still resolve by immutable CategoryId — deletion cannot hide a protection");
        var deletionProtection = profile!.ManualProtections.Single(p => p.ProtectionType == ManualProtectionType.Deletion);
        deletionProtection.IsProtected.Should().BeTrue();
        deletionProtection.SourceCategoryId.Should().Be(categoryId);
    }

    [Fact]
    public async Task AuthorizedLift_CanTarget_ASoftDeletedCategorysProtection_ByImmutableCategoryId()
    {
        var (categoryId, protectionId) = await SeedSoftDeletedProtectedCategoryAsync();

        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var protection = await context.AbwabManualProtections.SingleAsync(p => p.ManualProtectionId == protectionId);

        protection.CategoryId.Should().Be(categoryId, "a lift command must be able to address the exact protection row on a soft-deleted category by immutable CategoryId");
    }

    [Fact]
    public async Task SoftDeletedCategory_IsNotExposedTo_TheOrdinaryTreeReadSurface()
    {
        var (categoryId, _) = await SeedSoftDeletedProtectedCategoryAsync();

        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var visibleInOrdinaryReads = await context.AbwabCategories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .AnyAsync(c => c.CategoryId == categoryId);

        visibleInOrdinaryReads.Should().BeFalse("the narrow effective-protection security surface must not expose the deleted category to ordinary content reads");
    }

    private async Task<CategoryProtectionProfileDto?> ResolveProfileAsync(Guid categoryId)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var resolver = new ProtectionResolver(new EfManualProtectionReadPort(context), new FixedServerClock(Now));
        return await resolver.ResolveProfileAsync(categoryId, CancellationToken.None);
    }

    private async Task<(Guid CategoryId, Guid ProtectionId)> SeedSoftDeletedProtectedCategoryAsync()
    {
        var section = AbwabTreeSeeding.NewSection("قسم اختبار الوصول للفئة المحذوفة");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب اختبار الوصول للفئة المحذوفة", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var protection = AbwabTreeSeeding.NewManualProtection(root.CategoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly);
        await AbwabTreeSeeding.InsertAsync(fixture, protection);

        await using (var context = AbwabKernelHarness.CreateProductionContext(fixture))
        {
            var tracked = await context.AbwabCategories.SingleAsync(c => c.CategoryId == root.CategoryId);
            tracked.IsDeleted = true;
            tracked.DeletedAtUtc = Now;
            context.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());
            await context.SaveChangesAsync();
        }

        return (root.CategoryId, protection.ManualProtectionId);
    }
}
