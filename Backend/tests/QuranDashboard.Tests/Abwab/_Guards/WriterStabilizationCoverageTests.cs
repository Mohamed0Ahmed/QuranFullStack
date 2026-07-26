using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Application.Abwab.Concurrency;
using QuranDashboard.Infrastructure.Abwab.Caching;
using QuranDashboard.Infrastructure.Abwab.Persistence;
using QuranDashboard.Tests.Abwab._Fixtures;
using QuranDashboard.Tests.Abwab._Support;
using QuranDashboard.Tests.Abwab.Kernel._Support;

namespace QuranDashboard.Tests.Abwab._Guards;

public sealed class WriterStabilizationDiscoveryTests
{
    [Fact]
    public void Every030RelationshipOrTemplateWriter_IsDiscoveredAndRegisteredAgainstTheBarrier()
    {
        var registry = new AbwabWriterRegistry();
        AbwabWriterRegistrations.RegisterAll(registry);

        var discovered030Writers = AbwabWriterStabilizationGuard
            .DiscoverWriters(AbwabWriterRegistrations.WriterAssemblies)
            .Where(IsA030RelationshipOrTemplateWriter)
            .ToList();

        var missing = AbwabWriterStabilizationGuard.FindWritersMissingBarrier(discovered030Writers, registry.RegisteredWriters);

        missing.Should().BeEmpty(
            "every 030 relationship/template mutation command type MUST be registered against the "
            + "AbwabWriteBarrier gate, matched by namespace or by type name so a command placed under the "
            + "shared Abwab.Core namespace (the 029 precedent) is still discovered (vacuous until T027/T064 "
            + "declare command types); unregistered: "
            + string.Join(", ", missing.Select(type => type.FullName)));
    }

    [Theory]
    [InlineData("Relationship")]
    [InlineData("Template")]
    public void Each030WriterHalf_IsNotEmpty_SoThisGuardCannotSilentlyBecomeVacuous(string discriminator)
    {
        var discovered = AbwabWriterStabilizationGuard
            .DiscoverWriters(AbwabWriterRegistrations.WriterAssemblies)
            .Where(writer => writer.Name.Contains(discriminator, StringComparison.Ordinal))
            .ToList();

        discovered.Should().NotBeEmpty(
            $"T027/T064 registered the 030 {discriminator} mutation command types, so a later namespace/filter change "
            + "must not be able to reduce the coverage guard above to asserting nothing");
    }

    private static bool IsA030RelationshipOrTemplateWriter(Type writer) =>
        (writer.Namespace ?? string.Empty).Contains(".Abwab.Relationships", StringComparison.Ordinal) ||
        (writer.Namespace ?? string.Empty).Contains(".Abwab.Templates", StringComparison.Ordinal) ||
        writer.Name.Contains("Relationship", StringComparison.Ordinal) ||
        writer.Name.Contains("Template", StringComparison.Ordinal);
}

[Collection(nameof(AbwabDbCollection))]
public sealed class WriterStabilizationCoverageTests(PostgresFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => AbwabSubstrateReset.FullResetAsync(fixture);

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AnyRegisteredWriterOperation_IsDeniedWithStabilizationActive_BeforeItsOperationRuns()
    {
        await SecurityTestHarness.SetBarrierStabilizingAsync(fixture);

        var operationInvoked = false;
        await using var context = AbwabKernelHarness.CreateProductionContext(fixture);
        var executor = new AbwabAuditedCommitExecutor(context, new FixedServerClock(DateTimeOffset.UnixEpoch), new NullAbwabCachePublisher());

        var request = new AbwabAuditedOperationRequest(
            ExpectedTimelineGeneration.Of(0),
            "tester",
            (_, _) =>
            {
                operationInvoked = true;
                throw new InvalidOperationException("must never run while Stabilizing");
            });

        var act = () => executor.ExecuteAsync(request, CancellationToken.None);

        (await act.Should().ThrowAsync<AbwabStabilizationActiveException>())
            .Which.Code.Should().Be(AbwabConflictCodes.StabilizationActive);

        operationInvoked.Should().BeFalse(
            "the barrier check MUST precede any writer's operation — every 030 relationship/template "
            + "command rides this same executor path once registered (T027/T064)");
    }
}
