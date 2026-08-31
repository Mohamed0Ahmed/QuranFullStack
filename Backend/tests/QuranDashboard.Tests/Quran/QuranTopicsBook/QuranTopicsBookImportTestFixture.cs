using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using QuranDashboard.DataImporter;
using QuranDashboard.DataImporter.Import.VerbRunners;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.TestSupport.Process;
using QuranDashboard.Tests.TestSupport.PostgreSql;
using DataImporterProgram = QuranDashboard.DataImporter.Program;

namespace QuranDashboard.Tests.Quran.QuranTopicsBook;

public sealed class QuranTopicsBookImportTestFixture : IAsyncLifetime
{
    private readonly List<string> tempDirectories = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        foreach (var directory in tempDirectories)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return Task.CompletedTask;
    }

    internal async Task<QuranTopicsBookTestDatabase> LeaseDatabaseAsync()
    {
        var lease = await PostgreSqlTestProcess.LeaseMigratedDatabaseAsync(
            nameof(QuranTopicsBookImportTestFixture));
        return new QuranTopicsBookTestDatabase(lease, CreateTempDirectory());
    }

    internal QuranDashboardDbContext CreateDbContext(QuranTopicsBookTestDatabase database) =>
        new(new DbContextOptionsBuilder<QuranDashboardDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options);

    internal async Task SeedCanonicalMushafSliceAsync(QuranTopicsBookTestDatabase database)
    {
        var assembly = typeof(QuranTopicsBookImportTestFixture).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("mushaf-reader-seed.sql", StringComparison.Ordinal));
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded seed script '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var seedSql = await reader.ReadToEndAsync();

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(seedSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    internal async Task<int> CreateActiveOwnerAsync(QuranTopicsBookTestDatabase database)
    {
        await using var db = CreateDbContext(database);
        var ownerRoleId = await db.AccessRoles
            .Where(role => role.Name == RoleNames.Owner)
            .Select(role => role.Id)
            .SingleAsync();
        var owner = new User
        {
            LogtoSub = $"quran-topics-book-owner-{Guid.NewGuid():N}",
            Email = $"quran-topics-book-owner-{Guid.NewGuid():N}@example.test",
            NormalizedEmail = $"QURAN-TOPICS-BOOK-OWNER-{Guid.NewGuid():N}@EXAMPLE.TEST",
            RoleId = ownerRoleId,
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.AccessUsers.Add(owner);
        await db.SaveChangesAsync();
        return owner.Id;
    }

    internal async Task<QuranTopicsBookCommandRun> RunCommandAsync(
        QuranTopicsBookTestDatabase database,
        string sourcePath,
        int actorUserId,
        bool validateOnly = false)
    {
        var reportDirectory = Path.Combine(database.TempDirectory, $"report-{Guid.NewGuid():N}");
        var arguments = new List<string>
        {
            "import-quran-topics-book",
            "--source",
            sourcePath,
            "--actor-user-id",
            actorUserId.ToString(),
            "--report-out",
            reportDirectory,
        };
        if (validateOnly)
        {
            arguments.Add("--validate-only");
        }

        var processState = ProcessGlobalStateScope.Enter(
            environmentVariables: new Dictionary<string, string?>
            {
                ["ConnectionStrings__QuranDashboardDb"] = database.ConnectionString,
                ["DOTNET_ENVIRONMENT"] = "Production",
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
            },
            captureConsole: true);
        try
        {
            var exitCode = await DataImporterProgram.Main([.. arguments]);
            return new QuranTopicsBookCommandRun(exitCode, reportDirectory, processState.ConsoleOutput);
        }
        finally
        {
            processState.Dispose();
            if (processState.RestoreFailures.Count > 0)
            {
                throw new InvalidOperationException("The Quran topics import test did not restore process state.");
            }
        }
    }

    internal async Task<QuranTopicsBookCommandRun> RunWithAmbiguousCommitAsync(
        QuranTopicsBookTestDatabase database,
        string sourcePath,
        int actorUserId)
    {
        var reportDirectory = Path.Combine(database.TempDirectory, $"report-{Guid.NewGuid():N}");
        using var processState = ProcessGlobalStateScope.Enter(captureConsole: true);
        var exitCode = await ImportQuranTopicsBookRunner.RunAsync(
            [
                "--source",
                sourcePath,
                "--actor-user-id",
                actorUserId.ToString(),
                "--report-out",
                reportDirectory,
            ],
            () => CreateImporterHost(database, new ThrowAfterCommitAcknowledgementInterceptor()),
            () => { });
        return new QuranTopicsBookCommandRun(exitCode, reportDirectory, processState.ConsoleOutput);
    }

    internal async Task<IReadOnlyDictionary<string, int>> ReadTargetCountsAsync(
        QuranTopicsBookTestDatabase database,
        IReadOnlyList<string> tables)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            await using var command = new NpgsqlCommand(
                $"SELECT count(*)::integer FROM public.{table}",
                connection);
            counts.Add(table, Convert.ToInt32(await command.ExecuteScalarAsync()));
        }

        return counts;
    }

    private string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"quran-topics-book-{Guid.NewGuid():N}");
        tempDirectories.Add(directory);
        return directory;
    }

    private IHost CreateImporterHost(
        QuranTopicsBookTestDatabase database,
        IInterceptor? transactionInterceptor = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QuranDashboardDb"] = database.ConnectionString,
            })
            .Build();
        return new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddApplication();
                services.AddInfrastructure(configuration);
                if (transactionInterceptor is not null)
                {
                    services.RemoveAll<QuranDashboardDbContext>();
                    services.RemoveAll<DbContextOptions<QuranDashboardDbContext>>();
                    services.AddDbContext<QuranDashboardDbContext>(options =>
                        options.UseNpgsql(database.ConnectionString).AddInterceptors(transactionInterceptor));
                }
            })
            .Build();
    }

    private sealed class ThrowAfterCommitAcknowledgementInterceptor : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("simulated commit acknowledgement failure"));
    }
}

internal sealed class QuranTopicsBookTestDatabase(
    PostgreSqlDatabaseLease lease,
    string tempDirectory) : IAsyncDisposable
{
    internal string ConnectionString => lease.ConnectionString;

    internal string TempDirectory => tempDirectory;

    public ValueTask DisposeAsync() => lease.DisposeAsync();
}

internal sealed record QuranTopicsBookCommandRun(
    int ExitCode,
    string ReportDirectory,
    string ConsoleOutput);

[CollectionDefinition(nameof(QuranTopicsBookImportTestCollection), DisableParallelization = true)]
public sealed class QuranTopicsBookImportTestCollection
    : ICollectionFixture<QuranTopicsBookImportTestFixture>;
