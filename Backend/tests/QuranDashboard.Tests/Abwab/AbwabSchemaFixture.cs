using QuranDashboard.Application.Abstractions.Abwab;
using QuranDashboard.Domain.Abwab;
using QuranDashboard.Domain.Linking;
using QuranDashboard.Tests.TestSupport.PostgreSql;

namespace QuranDashboard.Tests.Abwab;

public sealed class AbwabSchemaFixture : IAsyncLifetime
{
    private PostgreSqlDatabaseLease? databaseLease;

    // Built once and owned by the fixture. Each caller takes its own scope, which is what actually gives a
    // test an isolated DbContext — a provider per call gave the same isolation and leaked one undisposed
    // provider (with its connection pool) per call for the whole run.
    private ServiceProvider? serviceProvider;

    public async Task InitializeAsync()
    {
        databaseLease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(nameof(AbwabSchemaFixture));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = databaseLease.ConnectionString
            })
            .Build();

        serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (serviceProvider is not null)
        {
            await serviceProvider.DisposeAsync();
        }

        if (databaseLease is not null)
        {
            await databaseLease.DisposeAsync();
        }
    }

    public ServiceProvider Services =>
        serviceProvider ?? throw new InvalidOperationException($"{nameof(InitializeAsync)} has not run yet.");

    public async Task ResetAbwabAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var truncate = "TRUNCATE TABLE "
            + string.Join(", ", AbwabResetTableNames(dbContext))
            + " RESTART IDENTITY CASCADE";

        await dbContext.Database.ExecuteSqlRawAsync(truncate);

        // The tree and templates readers are cached behind a process-wide generation counter, so rows
        // deleted underneath them stay visible until that counter moves.
        var cache = scope.ServiceProvider.GetRequiredService<IAbwabCacheInvalidator>();
        cache.InvalidateTree();
        cache.InvalidateTemplates();
    }

    private static IEnumerable<string> AbwabResetTableNames(QuranDashboardDbContext dbContext)
    {
        Type[] abwabEntities =
        [
            typeof(AbwabSection),
            typeof(AbwabDoor),
            typeof(AbwabDoorAlias),
            typeof(AbwabDoorRelation),
            typeof(AbwabDoorInclusion),
            typeof(AbwabDoorInclusionUnitSync),
            typeof(AbwabTemplate),
            typeof(AbwabTemplateNode),
            typeof(LinkingConfirmationJob),
            typeof(LinkingOperation),
            typeof(LinkingPreparedAffectedContribution),
            typeof(LinkingPreparedAyahDescription),
            typeof(LinkingPreparedAyahWord),
            typeof(LinkingPreparedAyah),
            typeof(LinkingPreparedUnit),
            typeof(LinkingPreparedSource),
            typeof(LinkingPreparedPreflight),
            typeof(LinkingSourceContributionUnit),
            typeof(LinkingSourceContribution),
            typeof(LinkingUnitAyahDescription),
            typeof(LinkingUnitAyahWord),
            typeof(LinkingUnitAyah),
            typeof(LinkingUnit),
            typeof(LinkingDoorAyahWord),
            typeof(LinkingDoorAyah)
        ];

        return abwabEntities.Select(entity =>
        {
            var entityType = dbContext.Model.FindEntityType(entity)
                ?? throw new InvalidOperationException($"{entity.Name} is not mapped.");
            var tableName = entityType.GetTableName()
                ?? throw new InvalidOperationException($"{entity.Name} is not mapped to a table.");
            var schema = entityType.GetSchema();

            return schema is null
                ? PostgreSqlDatabaseName.Quote(tableName)
                : $"{PostgreSqlDatabaseName.Quote(schema)}.{PostgreSqlDatabaseName.Quote(tableName)}";
        });
    }
}

[CollectionDefinition(nameof(AbwabSchemaTestCollection))]
public sealed class AbwabSchemaTestCollection : ICollectionFixture<AbwabSchemaFixture>;
