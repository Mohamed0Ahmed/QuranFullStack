namespace QuranDashboard.Tests.Abwab;

public sealed class AbwabSchemaFixture : IAsyncLifetime
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

[CollectionDefinition(nameof(AbwabSchemaTestCollection))]
public sealed class AbwabSchemaTestCollection : ICollectionFixture<AbwabSchemaFixture>;
