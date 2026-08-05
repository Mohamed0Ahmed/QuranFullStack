using System.Data.Common;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Infrastructure;
using QuranDashboard.Infrastructure.Access;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.AccessAdmin;

internal static class Program
{
    private const int SuccessExitCode = 0;
    private const int UsageExitCode = 2;
    private const int PreflightFailureExitCode = 3;
    private const int OperationalFailureExitCode = 4;

    private enum AccessAdminCommand
    {
        IdentityScan,
        IdentityBackfill,
        CatalogueSync,
        AuthorizationPreflight,
    }

    internal static async Task<int> Main(string[] args)
    {
        var command = ParseCommand(args);
        if (command is null)
        {
            return UsageFailure();
        }

        try
        {
            using var host = CreateHost(args);
            using var scope = host.Services.CreateScope();

            return command.Value switch
            {
                AccessAdminCommand.IdentityScan => await ScanIdentityAsync(scope.ServiceProvider),
                AccessAdminCommand.IdentityBackfill => await BackfillIdentityAsync(scope.ServiceProvider),
                AccessAdminCommand.CatalogueSync => await SynchronizeCatalogueAsync(scope.ServiceProvider),
                AccessAdminCommand.AuthorizationPreflight => await RunPreflightAsync(scope.ServiceProvider),
                _ => throw new InvalidOperationException("Unknown AccessAdmin command."),
            };
        }
        catch (Exception exception) when (exception is DbException or SocketException or InvalidOperationException)
        {
            return OperationalFailure(exception);
        }
    }

    private static async Task<int> ScanIdentityAsync(IServiceProvider services)
    {
        var result = await services.GetRequiredService<IEmailIdentityPreflight>()
            .ScanAsync(CancellationToken.None);
        PrintIdentityResult(result);
        return result.IsClean ? SuccessExitCode : PreflightFailureExitCode;
    }

    private static async Task<int> BackfillIdentityAsync(IServiceProvider services)
    {
        var changed = await services.GetRequiredService<IEmailIdentityPreflight>()
            .BackfillAsync(CancellationToken.None);
        Console.WriteLine($"normalized_email_backfilled={changed}");
        return SuccessExitCode;
    }

    private static async Task<int> SynchronizeCatalogueAsync(IServiceProvider services)
    {
        var result = await services.GetRequiredService<IPermissionCatalogueSynchronizer>()
            .SynchronizeAsync(CancellationToken.None);
        Console.WriteLine($"catalogue_added={result.AddedCodes.Count}");
        Console.WriteLine($"catalogue_updated={result.UpdatedCodes.Count}");
        Console.WriteLine($"catalogue_unknown={string.Join(',', result.UnknownDatabaseCodes)}");
        Console.WriteLine($"catalogue_retired={string.Join(',', result.RetiredCanonicalCodes)}");
        return result.UnknownDatabaseCodes.Count == 0 && result.RetiredCanonicalCodes.Count == 0
            ? SuccessExitCode
            : PreflightFailureExitCode;
    }

    private static async Task<int> RunPreflightAsync(IServiceProvider services)
    {
        var schema = await services.GetRequiredService<AuthorizationSchemaPreflight>()
            .InspectAsync(CancellationToken.None);
        PrintSchemaResult(schema);
        if (!schema.IsClean)
        {
            return PreflightFailureExitCode;
        }

        var db = services.GetRequiredService<QuranDashboardDbContext>();
        var pendingMigrations = db.Database.GetPendingMigrations().ToArray();
        var identity = await services.GetRequiredService<IEmailIdentityPreflight>()
            .ScanAsync(CancellationToken.None);
        var databaseRows = await db.AccessPermissions
            .AsNoTracking()
            .Select(permission => new { permission.Code, permission.RetiredAtUtc })
            .ToListAsync();
        var canonicalCodes = AbwabPermissionCatalogue.All
            .Select(permission => permission.Code)
            .ToHashSet(StringComparer.Ordinal);
        var missingCodes = canonicalCodes
            .Except(databaseRows.Select(row => row.Code), StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
        var unknownCodes = databaseRows
            .Where(row => !canonicalCodes.Contains(row.Code))
            .Select(row => row.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
        var retiredCodes = databaseRows
            .Where(row => canonicalCodes.Contains(row.Code) && row.RetiredAtUtc is not null)
            .Select(row => row.Code)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        PrintIdentityResult(identity);
        Console.WriteLine($"pending_migrations={string.Join(',', pendingMigrations)}");
        Console.WriteLine($"catalogue_missing={string.Join(',', missingCodes)}");
        Console.WriteLine($"catalogue_unknown={string.Join(',', unknownCodes)}");
        Console.WriteLine($"catalogue_retired={string.Join(',', retiredCodes)}");

        return pendingMigrations.Any()
            || !identity.IsClean
            || missingCodes.Length > 0
            || unknownCodes.Length > 0
            || retiredCodes.Length > 0
            ? PreflightFailureExitCode
            : SuccessExitCode;
    }

    private static void PrintIdentityResult(EmailIdentityScanResult result)
    {
        Console.WriteLine($"users={result.UserCount}");
        Console.WriteLine($"invalid_user_ids={string.Join(',', result.InvalidUserIds)}");
        Console.WriteLine($"missing_normalized_user_ids={string.Join(',', result.MissingNormalizedEmailUserIds)}");
        Console.WriteLine($"mismatched_normalized_user_ids={string.Join(',', result.MismatchedNormalizedEmailUserIds)}");
        Console.WriteLine(
            $"normalized_collisions={string.Join(';', result.Collisions.Select(collision =>
                $"{collision.NormalizedEmail}:{string.Join(',', collision.UserIds)}"))}");
    }

    private static void PrintSchemaResult(AuthorizationSchemaPreflightResult result)
    {
        Console.WriteLine($"schema_violations={string.Join(',', result.Violations)}");
    }

    private static AccessAdminCommand? ParseCommand(string[] args)
    {
        return args switch
        {
            ["identity", "scan"] => AccessAdminCommand.IdentityScan,
            ["identity", "backfill", "--apply"] => AccessAdminCommand.IdentityBackfill,
            ["catalogue", "sync"] => AccessAdminCommand.CatalogueSync,
            ["authorization", "preflight"] => AccessAdminCommand.AuthorizationPreflight,
            _ => null,
        };
    }

    private static int UsageFailure()
    {
        PrintUsage();
        return UsageExitCode;
    }

    private static int OperationalFailure(Exception exception)
    {
        Console.Error.WriteLine($"access_admin_failure={exception.GetType().Name}");
        Console.Error.WriteLine(
            "The command could not reach a usable database. Verify the configured environment and ConnectionStrings__QuranDashboardDb.");
        return OperationalFailureExitCode;
    }

    internal static IHost CreateHost(string[] args)
    {
        var toolDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)
            ?? AppContext.BaseDirectory;

        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.AddJsonFile(
                    Path.Combine(toolDirectory, "appsettings.json"),
                    optional: false,
                    reloadOnChange: false);
                if (context.HostingEnvironment.IsDevelopment())
                {
                    configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
                }

                configuration.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) => services.AddInfrastructure(context.Configuration))
            .Build();
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin identity scan");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin identity backfill --apply");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin catalogue sync");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin authorization preflight");
    }
}
