using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QuranDashboard.Tests.Smoke._Fixtures;

public sealed class SmokeApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private SmokeWebApplicationFactory? _inMemoryFactory;
    private SmokeWebApplicationFactory? _kestrelFactory;

    public string ConnectionString { get; private set; } = string.Empty;

    public HttpClient InMemoryClient { get; private set; } = null!;

    public HttpClient KestrelClient { get; private set; } = null!;

    public IServiceProvider InMemoryServices => _inMemoryFactory!.Services;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        await MigrateAsync();

        _inMemoryFactory = new SmokeWebApplicationFactory(ConnectionString);
        InMemoryClient = _inMemoryFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        });

        // UseKestrel must be called on the factory instance itself: a WithWebHostBuilder-derived
        // wrapper delegates CreateHost to its parent, whose null port field silently drops the
        // port-0 request and Kestrel binds default addresses instead of an ephemeral loopback port.
        _kestrelFactory = new SmokeWebApplicationFactory(ConnectionString);
        _kestrelFactory.UseKestrel(0);
        KestrelClient = _kestrelFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (_kestrelFactory is not null)
        {
            await _kestrelFactory.DisposeAsync();
        }

        if (_inMemoryFactory is not null)
        {
            await _inMemoryFactory.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    private async Task MigrateAsync()
    {
        var services = new ServiceCollection()
            .AddDbContext<QuranDashboardDbContext>(options => options.UseNpgsql(ConnectionString));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.MigrateAsync();
    }

    private sealed class SmokeWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => SmokeHostConfigurator.Configure(builder, connectionString);
    }
}
