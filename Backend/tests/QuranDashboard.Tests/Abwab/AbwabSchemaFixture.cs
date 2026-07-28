namespace QuranDashboard.Tests.Abwab;

public sealed class AbwabSchemaFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    // Built once and owned by the fixture. Each caller takes its own scope, which is what actually gives a
    // test an isolated DbContext — a provider per call gave the same isolation and leaked one undisposed
    // provider (with its connection pool) per call for the whole run.
    private ServiceProvider? serviceProvider;

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = postgresContainer.GetConnectionString()
            })
            .Build();

        serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddInfrastructure(configuration)
            .BuildServiceProvider();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (serviceProvider is not null)
        {
            await serviceProvider.DisposeAsync();
        }

        await postgresContainer.DisposeAsync();
    }

    public ServiceProvider Services =>
        serviceProvider ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet.");
}

[CollectionDefinition(nameof(AbwabSchemaTestCollection))]
public sealed class AbwabSchemaTestCollection : ICollectionFixture<AbwabSchemaFixture>;
