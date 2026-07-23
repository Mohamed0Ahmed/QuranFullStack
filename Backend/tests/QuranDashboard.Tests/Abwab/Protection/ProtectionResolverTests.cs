using QuranDashboard.Application.Abstractions.Abwab.Core;
using QuranDashboard.Application.Abwab.Protection;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Infrastructure.Persistence.Configurations.Abwab;
using QuranDashboard.Infrastructure.Persistence.Reads.Abwab;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Protection;

[Collection(nameof(AbwabDbCollection))]
public sealed class ProtectionResolverTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch.AddYears(1);

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Category_WithNoProtection_ResolvesUnprotected()
    {
        var (_, _, leaf) = await SeedChainAsync();

        var resolution = await ResolveAsync(leaf.CategoryId, ManualProtectionType.Deletion);

        resolution!.IsProtected.Should().BeFalse();
        resolution.IsDirect.Should().BeFalse();
        resolution.SourceCategoryId.Should().BeNull();
        resolution.ActionClassification.Should().Be(ProtectionActionClassification.Unprotected);
    }

    [Fact]
    public async Task Category_WithItsOwnActiveProtection_ResolvesDirect_WithItselfAsSource()
    {
        var (_, _, leaf) = await SeedChainAsync();
        var protection = AbwabTreeSeeding.NewManualProtection(leaf.CategoryId, ManualProtectionType.Deletion, ManualProtectionScope.CategoryOnly);
        await AbwabTreeSeeding.InsertAsync(fixture, protection);

        var resolution = await ResolveAsync(leaf.CategoryId, ManualProtectionType.Deletion);

        resolution!.IsProtected.Should().BeTrue();
        resolution.IsDirect.Should().BeTrue();
        resolution.SourceCategoryId.Should().Be(leaf.CategoryId);
        resolution.Scope.Should().Be(ManualProtectionScope.CategoryOnly);
        resolution.ActionClassification.Should().Be(ProtectionActionClassification.ManuallyProtected);
    }

    [Fact]
    public async Task Descendant_UnderASubtreeProtectedAncestor_ResolvesInherited_WithThatAncestorAsSource()
    {
        var (root, _, leaf) = await SeedChainAsync();
        var protection = AbwabTreeSeeding.NewManualProtection(root.CategoryId, ManualProtectionType.InternalStructure, ManualProtectionScope.Subtree);
        await AbwabTreeSeeding.InsertAsync(fixture, protection);

        var resolution = await ResolveAsync(leaf.CategoryId, ManualProtectionType.InternalStructure);

        resolution!.IsProtected.Should().BeTrue();
        resolution.IsDirect.Should().BeFalse();
        resolution.SourceCategoryId.Should().Be(root.CategoryId);
        resolution.Scope.Should().Be(ManualProtectionScope.Subtree);
    }

    [Fact]
    public async Task Descendant_UnderACategoryOnlyProtectedAncestor_DoesNotInherit()
    {
        var (root, _, leaf) = await SeedChainAsync();
        var protection = AbwabTreeSeeding.NewManualProtection(root.CategoryId, ManualProtectionType.InternalStructure, ManualProtectionScope.CategoryOnly);
        await AbwabTreeSeeding.InsertAsync(fixture, protection);

        var resolution = await ResolveAsync(leaf.CategoryId, ManualProtectionType.InternalStructure);

        resolution!.IsProtected.Should().BeFalse();
    }

    [Fact]
    public async Task NearestSubtreeAncestor_Wins_OverAFartherOne()
    {
        var (root, mid, leaf) = await SeedChainAsync();
        var farProtection = AbwabTreeSeeding.NewManualProtection(root.CategoryId, ManualProtectionType.CategoryData, ManualProtectionScope.Subtree);
        var nearProtection = AbwabTreeSeeding.NewManualProtection(mid.CategoryId, ManualProtectionType.CategoryData, ManualProtectionScope.Subtree);
        await AbwabTreeSeeding.InsertAsync(fixture, farProtection, nearProtection);

        var resolution = await ResolveAsync(leaf.CategoryId, ManualProtectionType.CategoryData);

        resolution!.SourceCategoryId.Should().Be(mid.CategoryId);
    }

    [Fact]
    public async Task ResolutionIsEvaluatedFromCurrentAncestorIds_NotAStaleSnapshot()
    {
        var (root, _, leaf) = await SeedChainAsync();
        var protection = AbwabTreeSeeding.NewManualProtection(root.CategoryId, ManualProtectionType.Relationship, ManualProtectionScope.Subtree);
        await AbwabTreeSeeding.InsertAsync(fixture, protection);

        var protectedBefore = await ResolveAsync(leaf.CategoryId, ManualProtectionType.Relationship);
        protectedBefore!.IsProtected.Should().BeTrue();

        await using (var context = AbwabKernelHarness.CreateProductionContext(fixture))
        {
            var tracked = await context.AbwabCategories.SingleAsync(c => c.CategoryId == leaf.CategoryId);
            tracked.ParentCategoryId = null;
            tracked.SectionId = SectionConfiguration.PermanentDefaultSectionId;
            tracked.SiblingOrder = null;
            tracked.SectionOrder = 99;
            tracked.GlobalOrder = 99;
            tracked.AncestorIds = [];
            tracked.Depth = 0;
            context.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());
            await context.SaveChangesAsync();
        }

        var resolvedAfterMove = await ResolveAsync(leaf.CategoryId, ManualProtectionType.Relationship);
        resolvedAfterMove!.IsProtected.Should().BeFalse("moving the category out from under the protected ancestor changes its current AncestorIds");
    }

    [Fact]
    public async Task OrdinaryWindow_Active_ClassifiesAsOrdinaryWindowActive_WhenNoManualProtectionApplies()
    {
        var (_, _, leaf) = await SeedChainAsync();

        await using (var context = AbwabKernelHarness.CreateProductionContext(fixture))
        {
            var tracked = await context.AbwabCategories.SingleAsync(c => c.CategoryId == leaf.CategoryId);
            tracked.OrdinaryProtectionActorSubject = "last-editor";
            tracked.OrdinaryProtectionLastEditedAtUtc = Now.AddHours(-1);
            context.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());
            await context.SaveChangesAsync();
        }

        var resolution = await ResolveAsync(leaf.CategoryId, ManualProtectionType.CategoryData);

        resolution!.ActionClassification.Should().Be(ProtectionActionClassification.OrdinaryWindowActive);
    }

    [Fact]
    public async Task OrdinaryWindow_Expired_ClassifiesAsUnprotected()
    {
        var (_, _, leaf) = await SeedChainAsync();

        await using (var context = AbwabKernelHarness.CreateProductionContext(fixture))
        {
            var tracked = await context.AbwabCategories.SingleAsync(c => c.CategoryId == leaf.CategoryId);
            tracked.OrdinaryProtectionActorSubject = "last-editor";
            tracked.OrdinaryProtectionLastEditedAtUtc = Now.AddHours(-25);
            context.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());
            await context.SaveChangesAsync();
        }

        var resolution = await ResolveAsync(leaf.CategoryId, ManualProtectionType.CategoryData);

        resolution!.ActionClassification.Should().Be(ProtectionActionClassification.Unprotected);
    }

    [Fact]
    public async Task ManualProtection_TakesPrecedenceOverAnActiveOrdinaryWindow()
    {
        var (_, _, leaf) = await SeedChainAsync();
        var protection = AbwabTreeSeeding.NewManualProtection(leaf.CategoryId, ManualProtectionType.CategoryData, ManualProtectionScope.CategoryOnly);
        await AbwabTreeSeeding.InsertAsync(fixture, protection);

        await using (var context = AbwabKernelHarness.CreateProductionContext(fixture))
        {
            var tracked = await context.AbwabCategories.SingleAsync(c => c.CategoryId == leaf.CategoryId);
            tracked.OrdinaryProtectionActorSubject = "last-editor";
            tracked.OrdinaryProtectionLastEditedAtUtc = Now.AddHours(-1);
            context.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());
            await context.SaveChangesAsync();
        }

        var resolution = await ResolveAsync(leaf.CategoryId, ManualProtectionType.CategoryData);

        resolution!.ActionClassification.Should().Be(ProtectionActionClassification.ManuallyProtected);
    }

    [Fact]
    public async Task Expiry_IsDerivedFromTheServerClock_NeverFromStoredClientInput()
    {
        var (_, _, leaf) = await SeedChainAsync();
        var lastEdited = Now.AddHours(-2);

        await using (var context = AbwabKernelHarness.CreateProductionContext(fixture))
        {
            var tracked = await context.AbwabCategories.SingleAsync(c => c.CategoryId == leaf.CategoryId);
            tracked.OrdinaryProtectionActorSubject = "last-editor";
            tracked.OrdinaryProtectionLastEditedAtUtc = lastEdited;
            context.AbwabChangeSets.Add(AbwabTreeSeeding.NewChangeSet());
            await context.SaveChangesAsync();
        }

        var profile = await ResolveProfileAsync(leaf.CategoryId);

        profile!.ServerTimeUtc.Should().Be(Now);
        profile.OrdinaryProtection.ExpiresAtUtc.Should().Be(lastEdited.AddHours(24));
    }

    [Fact]
    public async Task Profile_Resolves_AllFiveProtectionTypes()
    {
        var (_, _, leaf) = await SeedChainAsync();

        var profile = await ResolveProfileAsync(leaf.CategoryId);

        profile!.ManualProtections.Select(p => p.ProtectionType).Should().BeEquivalentTo(
            Enum.GetValues<ManualProtectionType>());
    }

    [Fact]
    public async Task Resolve_ForANonExistentCategory_ReturnsNull()
    {
        var resolution = await ResolveAsync(Guid.NewGuid(), ManualProtectionType.Deletion);

        resolution.Should().BeNull();
    }

    private async Task<ManualProtectionResolutionDto?> ResolveAsync(Guid categoryId, ManualProtectionType type)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var resolver = new ProtectionResolver(new EfManualProtectionReadPort(context), new FixedServerClock(Now));
        return await resolver.ResolveTypeAsync(categoryId, type, CancellationToken.None);
    }

    private async Task<CategoryProtectionProfileDto?> ResolveProfileAsync(Guid categoryId)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var resolver = new ProtectionResolver(new EfManualProtectionReadPort(context), new FixedServerClock(Now));
        return await resolver.ResolveProfileAsync(categoryId, CancellationToken.None);
    }

    private async Task<(Category Root, Category Mid, Category Leaf)> SeedChainAsync()
    {
        var section = AbwabTreeSeeding.NewSection("قسم اختبار المحلل");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب اختبار المحلل", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        var mid = AbwabTreeSeeding.NewChildCategory("فصل وسيط", root, siblingOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, mid);

        var leaf = AbwabTreeSeeding.NewChildCategory("فرع مستهدف", mid, siblingOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, leaf);

        return (root, mid, leaf);
    }
}
