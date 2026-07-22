using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abwab.Concurrency;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab.Kernel;

// FR-019 / SC-008 (T029): the stabilization registry test MUST fail if any Abwab writer lacks the barrier.
// The mechanism is DISTINCT from the FK guard: it governs per-writer barrier registration, not schema FKs.
public sealed class WriteBarrierRegistryTests
{
    [Fact]
    public void EveryDiscoveredProductionWriter_IsRegisteredAgainstTheBarrier()
    {
        var registry = new AbwabWriterRegistry();
        AbwabWriterRegistrations.RegisterAll(registry);

        var discovered = AbwabWriterStabilizationGuard.DiscoverWriters(AbwabWriterRegistrations.WriterAssemblies);
        var missing = AbwabWriterStabilizationGuard.FindWritersMissingBarrier(discovered, registry.RegisteredWriters);

        missing.Should().BeEmpty(
            "every Abwab writer MUST pass the AbwabWriteBarrier gate; unregistered writer(s): "
            + string.Join(", ", missing.Select(type => type.FullName)));
    }

    [Fact]
    public void Guard_ReportsAWriter_ThatLacksTheBarrier()
    {
        // Non-vacuity: prove the guard actually fails when a discovered writer is not registered — this is
        // the exact condition that breaks the build when a 029+ writer forgets the gate.
        var missing = AbwabWriterStabilizationGuard.FindWritersMissingBarrier(
            [typeof(FixtureWriterMissingGeneration)],
            new HashSet<Type>());

        missing.Should().ContainSingle().Which.Should().Be(typeof(FixtureWriterMissingGeneration));
    }

    [Fact]
    public void Guard_Passes_WhenTheWriterIsRegistered()
    {
        var registry = new AbwabWriterRegistry();
        registry.Register<FixtureWriterMissingGeneration>();

        var missing = AbwabWriterStabilizationGuard.FindWritersMissingBarrier(
            [typeof(FixtureWriterMissingGeneration)],
            registry.RegisteredWriters);

        missing.Should().BeEmpty();
    }
}
