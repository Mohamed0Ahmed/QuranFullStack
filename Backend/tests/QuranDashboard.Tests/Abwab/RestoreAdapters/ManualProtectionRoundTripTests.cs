using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Domain.Abwab.Protection;
using QuranDashboard.Infrastructure.Abwab.Restore;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.RestoreAdapters;

[Collection(nameof(AbwabDbCollection))]
public sealed class ManualProtectionRoundTripTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly ManualProtectionRestoreAdapter Adapter = new();

    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Adapter_RoundTrips_AnActiveProtection_ToProductStateEquality()
    {
        var category = await SeedCategoryAsync();

        var protection = AbwabTreeSeeding.NewManualProtection(
            category.CategoryId,
            ManualProtectionType.Deletion,
            ManualProtectionScope.Subtree,
            appliedBy: "actor-a",
            appliedAtUtc: DateTimeOffset.UnixEpoch.AddDays(1));
        await AbwabTreeSeeding.InsertAsync(fixture, protection);

        var reloaded = await ReloadAsync(protection.ManualProtectionId);

        var snapshot = Adapter.Capture(reloaded);
        var reconstructed = Adapter.Reconstruct(snapshot);

        reconstructed.Should().BeEquivalentTo(reloaded, options => options.Excluding(p => p.Version));
    }

    [Fact]
    public async Task Adapter_RoundTrips_ALiftedSoftDeletedProtection()
    {
        var category = await SeedCategoryAsync();

        var protection = AbwabTreeSeeding.NewManualProtection(
            category.CategoryId,
            ManualProtectionType.CategoryData,
            ManualProtectionScope.CategoryOnly);
        protection.LiftedByActorSubject = "actor-b";
        protection.LiftedAtUtc = DateTimeOffset.UnixEpoch.AddDays(2);
        protection.IsDeleted = true;
        protection.DeletedAtUtc = protection.LiftedAtUtc;
        await AbwabTreeSeeding.InsertAsync(fixture, protection);

        var reloaded = await ReloadAsync(protection.ManualProtectionId);

        var snapshot = Adapter.Capture(reloaded);
        var reconstructed = Adapter.Reconstruct(snapshot);

        reconstructed.Should().BeEquivalentTo(reloaded, options => options.Excluding(p => p.Version));
        reconstructed.IsDeleted.Should().BeTrue();
        reconstructed.LiftedByActorSubject.Should().Be("actor-b");
    }

    [Fact]
    public void Snapshot_ExcludesXminAndLogicalCounters()
    {
        typeof(ManualProtectionRestoreSnapshot).GetProperties().Select(p => p.Name)
            .Should().NotContain(["Version", "Xmin", "TreeRevision", "CategoryContentRevision"]);
    }

    // The exact registered SET is asserted once, in RestoreRegistryTests (§8 gate); this test owns only
    // the ManualProtection adapter's own registration so the two do not drift as adapters are added.
    [Fact]
    public void Adapter_IsRegistered_ExactlyOnce_AndOrderIsNeverAStandaloneAdapter()
    {
        var services = new ServiceCollection().AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();

        var descriptors = provider.GetServices<IAbwabRestoreAdapterDescriptor>().ToList();

        descriptors.Should().ContainSingle(d => d.PersistedType == "ManualProtection");
        descriptors.Should().NotContain(d => d.PersistedType == "Order", "order round-trips as a facet within Section/Category, never as a standalone adapter (§8)");
    }

    private async Task<Category> SeedCategoryAsync()
    {
        var section = AbwabTreeSeeding.NewSection("قسم اختبار حماية الاستعادة");
        await AbwabTreeSeeding.InsertAsync(fixture, section);

        var root = AbwabTreeSeeding.NewRootCategory("باب اختبار حماية الاستعادة", section.SectionId, sectionOrder: 0, globalOrder: 0);
        await AbwabTreeSeeding.InsertAsync(fixture, root);

        return root;
    }

    private async Task<ManualProtection> ReloadAsync(Guid manualProtectionId)
    {
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        return await context.AbwabManualProtections.AsNoTracking().SingleAsync(p => p.ManualProtectionId == manualProtectionId);
    }
}
