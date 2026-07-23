using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Infrastructure.Abwab.Restore;

namespace QuranDashboard.Tests.Abwab.RestoreAdapters;

// T082/§8: a static registry test proving exactly one adapter per persisted 029 type — Section,
// Category (all three orders + subtree delete/operation-restore as facets), ManualProtection — and
// that a duplicate/standalone "Order" registration (or any missing registration) fails CI.
public sealed class RestoreRegistryTests
{
    private static readonly string[] ExpectedPersistedTypes = ["Section", "Category", "ManualProtection"];

    [Fact]
    public void ExactlyThreeAdaptersAreRegistered()
    {
        var services = new ServiceCollection();
        services.AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();

        var descriptors = provider.GetServices<IAbwabRestoreAdapterDescriptor>().ToList();

        descriptors.Should().HaveCount(3);
    }

    [Fact]
    public void RegisteredPersistedTypes_MatchExactlyTheThreePersisted029Types()
    {
        var services = new ServiceCollection();
        services.AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();

        var persistedTypes = provider.GetServices<IAbwabRestoreAdapterDescriptor>().Select(d => d.PersistedType).ToList();

        persistedTypes.Should().BeEquivalentTo(ExpectedPersistedTypes);
    }

    [Fact]
    public void EveryRegisteredAdapter_HasAUniquePersistedType_NoDuplicateRegistration()
    {
        var services = new ServiceCollection();
        services.AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();

        var descriptors = provider.GetServices<IAbwabRestoreAdapterDescriptor>().ToList();

        descriptors.Select(d => d.PersistedType).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void OrderIsNeverARegisteredPersistedType_ItIsAFacetOfSectionAndCategory()
    {
        var services = new ServiceCollection();
        services.AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();

        var persistedTypes = provider.GetServices<IAbwabRestoreAdapterDescriptor>().Select(d => d.PersistedType).ToList();

        persistedTypes.Should().NotContain("Order");
    }

    [Fact]
    public void ValidateRegistry_FailsCi_WhenAStandaloneOrderAdapterIsAdded()
    {
        var registered = new List<IAbwabRestoreAdapterDescriptor>
        {
            new FakeDescriptor("Section"),
            new FakeDescriptor("Category"),
            new FakeDescriptor("ManualProtection"),
            new FakeDescriptor("Order"),
        };

        var isValid = ValidateRegistry(registered, out var reason);

        isValid.Should().BeFalse();
        reason.Should().Contain("Order");
    }

    [Fact]
    public void ValidateRegistry_FailsCi_WhenARequiredPersistedTypeIsMissing()
    {
        var registered = new List<IAbwabRestoreAdapterDescriptor>
        {
            new FakeDescriptor("Section"),
            new FakeDescriptor("Category"),
        };

        var isValid = ValidateRegistry(registered, out var reason);

        isValid.Should().BeFalse();
        reason.Should().Contain("ManualProtection");
    }

    [Fact]
    public void ValidateRegistry_Passes_ForTheRealThreeAdapterRegistration()
    {
        var services = new ServiceCollection();
        services.AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();
        var registered = provider.GetServices<IAbwabRestoreAdapterDescriptor>().ToList();

        var isValid = ValidateRegistry(registered, out var reason);

        isValid.Should().BeTrue(reason);
    }

    private static bool ValidateRegistry(IReadOnlyCollection<IAbwabRestoreAdapterDescriptor> registered, out string reason)
    {
        var actualTypes = registered.Select(d => d.PersistedType).ToHashSet(StringComparer.Ordinal);
        var expectedTypes = ExpectedPersistedTypes.ToHashSet(StringComparer.Ordinal);

        var missing = expectedTypes.Except(actualTypes).ToList();
        var extra = actualTypes.Except(expectedTypes).ToList();
        var duplicates = registered
            .GroupBy(d => d.PersistedType, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        var problems = new List<string>();
        if (missing.Count > 0)
        {
            problems.Add($"missing registration(s): {string.Join(", ", missing)}");
        }

        if (extra.Count > 0)
        {
            problems.Add($"unexpected/duplicate registration(s) (e.g. a standalone Order adapter): {string.Join(", ", extra)}");
        }

        if (duplicates.Count > 0)
        {
            problems.Add($"duplicate persisted type(s): {string.Join(", ", duplicates)}");
        }

        reason = string.Join("; ", problems);
        return problems.Count == 0;
    }

    private sealed class FakeDescriptor(string persistedType) : IAbwabRestoreAdapterDescriptor
    {
        public string PersistedType { get; } = persistedType;

        public int SnapshotSchemaVersion => 1;
    }
}
