using QuranDashboard.Application.Abstractions.Abwab.Restore;
using QuranDashboard.Domain.Abwab.Categories;
using QuranDashboard.Infrastructure.Abwab.Restore;

namespace QuranDashboard.Tests.Abwab.RestoreAdapters;

// §8: template application creates ordinary Category rows, so its inversion is performed by the ONE
// 029 CategoryRestoreAdapter. The interpreter maps the event to that adapter and registers nothing.
public sealed class TemplateApplicationInterpreterTests
{
    [Fact]
    public void TheInterpreter_InvertsThroughTheCategoryAdapter_ObservedPerformingTheInversion()
    {
        var observed = new ObservingCategoryAdapter();
        var interpreter = new TemplateApplicationEventInterpreter(observed);
        var created = new[] { Aggregate("باب مُنشأ") };

        var reconstructed = interpreter.ReconstructCreatedCategories(interpreter.CaptureCreatedCategories(created));

        observed.CaptureCalls.Should().Be(1, "the 029 Category adapter — not a second adapter — captures the created rows");
        observed.ReconstructCalls.Should().Be(1, "the 029 Category adapter performs the inversion");
        reconstructed.Single().Category.Name.Should().Be("باب مُنشأ");
    }

    [Fact]
    public void TheInterpreter_IsVersionedAndScopedToTheApplicationEventKind()
    {
        var interpreter = new TemplateApplicationEventInterpreter(new CategoryRestoreAdapter());

        interpreter.EventKind.Should().Be(TemplateApplicationEventInterpreter.Kind);
        interpreter.InterpreterSchemaVersion.Should().Be(TemplateApplicationEventInterpreter.Schema);
    }

    [Fact]
    public void TheInterpreter_IsNotAnAdapterDescriptor()
    {
        typeof(TemplateApplicationEventInterpreter).Should().NotBeAssignableTo<IAbwabRestoreAdapterDescriptor>(
            "registering the interpreter as a descriptor would add a duplicate registry entry and must fail CI");
    }

    [Fact]
    public void TheAdapterRegistry_IsUnchangedByTheInterpreter()
    {
        var services = new ServiceCollection();
        services.AddAbwabRestoreAdapters();
        using var provider = services.BuildServiceProvider();

        provider.GetService<TemplateApplicationEventInterpreter>().Should().NotBeNull(
            "the interpreter is registered as an event-kind interpreter");

        // Set equality against the ONE canonical expected list, not a guessed exclusion: registering
        // the interpreter must leave the registry byte-for-byte the §8 five.
        provider.GetServices<IAbwabRestoreAdapterDescriptor>()
            .Select(descriptor => descriptor.PersistedType)
            .Should().BeEquivalentTo(RestoreRegistryTests.ExpectedPersistedTypes,
                "the interpreter registers NO adapter of its own (§8)");
    }

    private static CategoryAggregate Aggregate(string name) =>
        new(new Category { CategoryId = Guid.NewGuid(), Name = name, NormalizedName = name }, []);

    private sealed class ObservingCategoryAdapter : IAbwabRestoreAdapter<CategoryAggregate, CategoryRestoreSnapshot>
    {
        private readonly CategoryRestoreAdapter _inner = new();

        public int CaptureCalls { get; private set; }

        public int ReconstructCalls { get; private set; }

        public string PersistedType => _inner.PersistedType;

        public int SnapshotSchemaVersion => _inner.SnapshotSchemaVersion;

        public CategoryRestoreSnapshot Capture(CategoryAggregate product)
        {
            CaptureCalls++;
            return _inner.Capture(product);
        }

        public CategoryAggregate Reconstruct(CategoryRestoreSnapshot snapshot)
        {
            ReconstructCalls++;
            return _inner.Reconstruct(snapshot);
        }
    }
}
