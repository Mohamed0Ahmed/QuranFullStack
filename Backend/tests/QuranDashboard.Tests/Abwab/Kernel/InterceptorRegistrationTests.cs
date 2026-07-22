using Microsoft.EntityFrameworkCore.Infrastructure;
using QuranDashboard.Infrastructure.Abwab.Persistence;

namespace QuranDashboard.Tests.Abwab.Kernel;

// FR-011 (T037): the CI bypass check guarding the SavingChanges interceptor. It composes the real
// production DI graph and asserts the AbwabWriteGuardInterceptor is wired into the DbContext, so the
// write-kernel guard cannot be silently removed. DISTINCT from the T081 forbidden-write-API source gate:
// that scans source for bypass APIs at build time; this proves the runtime interceptor is actually installed.
public sealed class InterceptorRegistrationTests
{
    [Fact]
    public void ProductionDbContext_HasTheWriteGuardInterceptor_Wired()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = "Host=localhost;Database=guard;Username=u;Password=p",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<DbContextOptions<QuranDashboardDbContext>>();
        var coreOptions = options.FindExtension<CoreOptionsExtension>();

        coreOptions.Should().NotBeNull();
        coreOptions!.Interceptors.Should().NotBeNull();
        coreOptions.Interceptors!.OfType<AbwabWriteGuardInterceptor>().Should().NotBeEmpty(
            "the SavingChanges write-kernel guard must be wired into the production DbContext and cannot be skipped");
    }
}
