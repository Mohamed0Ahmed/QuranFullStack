using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuranDashboard.Infrastructure;
using QuranDashboard.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QuranDashboard.Tests.Quran.FullI3rab;

/// <summary>
/// Minimal Phase-1 fixture for Feature 010 (Quran Full I'rab Foundation): starts a PostgreSQL
/// container via Testcontainers, registers the infrastructure services, and applies the EF migrations.
/// <para>
/// Source-reader, assembler, writer, report-builder, and import-run helpers are intentionally absent
/// here; they are introduced in later phases. Phase 1 only needs the schema to exist.
/// </para>
/// </summary>
public sealed class FullI3rabSchemaFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        return postgresContainer.DisposeAsync().AsTask();
    }

    public ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = postgresContainer.GetConnectionString()
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
