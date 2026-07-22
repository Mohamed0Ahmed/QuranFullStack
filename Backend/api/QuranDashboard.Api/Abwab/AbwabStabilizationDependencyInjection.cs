using QuranDashboard.Application.Abwab.Concurrency;

namespace QuranDashboard.Api.Abwab;

// Stabilization composition root: builds the AbwabWriterRegistry from the explicit registration list and
// fails fast at startup if any discovered IAbwabWriter was not registered against the barrier. This is the
// runtime counterpart of the stabilization registry test.
public static class AbwabStabilizationDependencyInjection
{
    public static IServiceCollection AddAbwabStabilization(this IServiceCollection services)
    {
        var registry = new AbwabWriterRegistry();
        AbwabWriterRegistrations.RegisterAll(registry);

        var discovered = AbwabWriterStabilizationGuard.DiscoverWriters(AbwabWriterRegistrations.WriterAssemblies);
        var missing = AbwabWriterStabilizationGuard.FindWritersMissingBarrier(discovered, registry.RegisteredWriters);
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Abwab writers are not registered against the write barrier: "
                + string.Join(", ", missing.Select(type => type.FullName)));
        }

        services.AddSingleton(registry);
        return services;
    }
}
