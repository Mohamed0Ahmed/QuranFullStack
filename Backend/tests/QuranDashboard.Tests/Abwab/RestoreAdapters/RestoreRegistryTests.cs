using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Infrastructure.Abwab.Restore;

namespace QuranDashboard.Tests.Abwab.RestoreAdapters;

public sealed class RestoreRegistryTests
{
    internal static readonly string[] ExpectedPersistedTypes = ["Section", "Category", "ManualProtection", "Relationship", "DoorTemplate"];

    [Fact]
    public void RegisteredPersistedTypes_AreExactlyTheExpectedSet_NoMissingNoDuplicate()
    {
        var persistedTypes = ResolveRegisteredDescriptors().Select(d => d.PersistedType).ToList();

        persistedTypes.Should().BeEquivalentTo(ExpectedPersistedTypes,
            "§8 keys the registry by persisted type: the set must be exact, with no missing registration, "
            + "no duplicate, and no standalone facet adapter (Order, TemplateNode, TemplateNodeSearchAlias)");
    }

    [Fact]
    public void ValidateRegistry_Passes_ForTheRealAdapterRegistration()
    {
        var isValid = ValidateRegistry(ResolveRegisteredDescriptors(), out var reason);

        isValid.Should().BeTrue(reason);
    }

    [Theory]
    [InlineData("Order", "a standalone Order adapter — Order is a facet of Section and Category")]
    [InlineData("TemplateNode", "a standalone TemplateNode adapter — a facet of the one DoorTemplate aggregate adapter")]
    [InlineData("TemplateNodeSearchAlias", "a standalone alias adapter — a facet of the one DoorTemplate aggregate adapter")]
    [InlineData("RelationshipEndpoint", "a relationship-endpoint adapter — endpoints are ordinary Category rows")]
    [InlineData(nameof(TemplateApplicationEventInterpreter), "the application-event interpreter registered as an adapter descriptor")]
    public void ValidateRegistry_FailsCi_WhenAnExtraAdapterIsRegistered(string extraPersistedType, string why)
    {
        var registered = ExpectedPersistedTypes
            .Append(extraPersistedType)
            .Select(type => new FakeDescriptor(type))
            .ToList<IAbwabRestoreAdapterDescriptor>();

        var isValid = ValidateRegistry(registered, out var reason);

        isValid.Should().BeFalse(why);
        reason.Should().Contain(extraPersistedType);
    }

    [Fact]
    public void ValidateRegistry_FailsCi_WhenASecondTemplateCreatedCategoryAdapterIsRegistered()
    {
        var registered = ExpectedPersistedTypes
            .Append("Category")
            .Select(type => new FakeDescriptor(type))
            .ToList<IAbwabRestoreAdapterDescriptor>();

        var isValid = ValidateRegistry(registered, out var reason);

        isValid.Should().BeFalse(
            "template application creates ordinary Category rows, so a second \"template-created category\" "
            + "adapter is a §8 duplicate");
        reason.Should().Contain("Category");
    }

    [Theory]
    [InlineData("Section")]
    [InlineData("Category")]
    [InlineData("ManualProtection")]
    [InlineData("Relationship")]
    [InlineData("DoorTemplate")]
    public void ValidateRegistry_FailsCi_WhenARequiredPersistedTypeIsMissing(string missingPersistedType)
    {
        var registered = ExpectedPersistedTypes
            .Where(type => type != missingPersistedType)
            .Select(type => new FakeDescriptor(type))
            .ToList<IAbwabRestoreAdapterDescriptor>();

        var isValid = ValidateRegistry(registered, out var reason);

        isValid.Should().BeFalse();
        reason.Should().Contain(missingPersistedType);
    }

    [Fact]
    public void TheApplicationEventInterpreter_IsRegisteredAsAnEventInterpreter_AndIsNotAnAdapterDescriptorAtAll()
    {
        var services = new ServiceCollection();
        services.AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();

        provider.GetServices<IAbwabRestoreEventInterpreter>()
            .Should().ContainSingle().Which.Should().BeOfType<TemplateApplicationEventInterpreter>();
        typeof(TemplateApplicationEventInterpreter).Should().NotBeAssignableTo<IAbwabRestoreAdapterDescriptor>(
            "§8 forbids the interpreter from being an adapter at all — it delegates to the one 029 Category adapter, "
            + "so it can never reach the registry even by a mistaken registration");
    }

    private static List<IAbwabRestoreAdapterDescriptor> ResolveRegisteredDescriptors()
    {
        var services = new ServiceCollection();
        services.AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();

        return [.. provider.GetServices<IAbwabRestoreAdapterDescriptor>()];
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
            problems.Add($"unexpected registration(s) — a standalone facet or interpreter adapter: {string.Join(", ", extra)}");
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
