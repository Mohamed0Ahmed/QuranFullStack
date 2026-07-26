using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Infrastructure.Abwab.Restore;

namespace QuranDashboard.Tests.Abwab.RestoreAdapters;

public sealed class RestoreRegistryTests
{
    internal static readonly string[] ExpectedPersistedTypes = ["Section", "Category", "ManualProtection", "Relationship", "DoorTemplate"];

    [Fact]
    public void ExactlyTheExpectedAdaptersAreRegistered()
    {
        var services = new ServiceCollection();
        services.AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();

        var descriptors = provider.GetServices<IAbwabRestoreAdapterDescriptor>().ToList();

        descriptors.Should().HaveCount(ExpectedPersistedTypes.Length);
    }

    [Fact]
    public void RegisteredPersistedTypes_MatchExactlyThePersistedAbwabTypes()
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
            new FakeDescriptor("Relationship"),
            new FakeDescriptor("DoorTemplate"),
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
            new FakeDescriptor("ManualProtection"),
        };

        var isValid = ValidateRegistry(registered, out var reason);

        isValid.Should().BeFalse();
        reason.Should().Contain("Relationship");
    }

    [Fact]
    public void ValidateRegistry_Passes_ForTheRealAdapterRegistration()
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
