using Microsoft.Extensions.Configuration;
using QuranDashboard.Application;
using QuranDashboard.Infrastructure;
using Testcontainers.PostgreSql;

namespace QuranDashboard.Tests.Quran.Import;

public sealed class ImportTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string SourceRoot { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await postgresContainer.StartAsync();

        SourceRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..",
            "resources", "import-sources", "quran-foundation"));

        if (!Directory.Exists(SourceRoot))
        {
            throw new DirectoryNotFoundException($"Import source staging tree was not found: {SourceRoot}");
        }

        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await postgresContainer.DisposeAsync();
    }

    public ServiceProvider CreateServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = postgresContainer.GetConnectionString()
            })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    public async Task<ImportQuranFoundationHandler> CreateHandlerAsync()
    {
        await TruncateQuranTablesAsync();
        return CreateHandlerWithoutTruncate();
    }

    public ImportQuranFoundationHandler CreateHandlerWithoutTruncate()
    {
        return CreateServiceProvider().GetRequiredService<ImportQuranFoundationHandler>();
    }

    public async Task TruncateQuranTablesAsync()
    {
        await using var scope = CreateServiceProvider().CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE quran_words, quran_mushaf_lines, quran_mushaf_pages, quran_ayahs, quran_surahs RESTART IDENTITY CASCADE;");
    }
}
