using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuranDashboard.Application;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Infrastructure;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.AccessAdmin;

internal static class Program
{
    private const int SuccessExitCode = 0;
    private const int UsageExitCode = 2;
    private const int PreflightFailureExitCode = 3;

    internal static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            PrintUsage();
            return UsageExitCode;
        }

        using var host = CreateHost(args);
        using var scope = host.Services.CreateScope();

        return (args[0], args[1]) switch
        {
            ("identity", "scan") when args.Length == 2 =>
                await ScanIdentityAsync(scope.ServiceProvider),
            ("identity", "backfill") when args.Length == 3 && args[2] == "--apply" =>
                await BackfillIdentityAsync(scope.ServiceProvider),
            ("catalogue", "sync") when args.Length == 2 =>
                await SynchronizeCatalogueAsync(scope.ServiceProvider),
            ("authorization", "preflight") when args.Length == 2 =>
                await RunPreflightAsync(scope.ServiceProvider),
            _ => UsageFailure(),
        };
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
        return result.UnknownDatabaseCodes.Count == 0
            ? SuccessExitCode
            : PreflightFailureExitCode;
    }

    private static async Task<int> RunPreflightAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<QuranDashboardDbContext>();
        var pendingMigrations = db.Database.GetPendingMigrations().ToArray();
        var identity = await services.GetRequiredService<IEmailIdentityPreflight>()
            .ScanAsync(CancellationToken.None);
        var databaseCodes = await db.AccessPermissions
            .AsNoTracking()
            .Select(permission => permission.Code)
            .ToListAsync();
        var canonicalCodes = AbwabPermissionCatalogue.All
            .Select(permission => permission.Code)
            .ToHashSet(StringComparer.Ordinal);
        var missingCodes = canonicalCodes
            .Except(databaseCodes, StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
        var unknownCodes = databaseCodes
            .Where(code => !canonicalCodes.Contains(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        PrintIdentityResult(identity);
        Console.WriteLine($"pending_migrations={string.Join(',', pendingMigrations)}");
        Console.WriteLine($"catalogue_missing={string.Join(',', missingCodes)}");
        Console.WriteLine($"catalogue_unknown={string.Join(',', unknownCodes)}");

        return pendingMigrations.Any()
            || !identity.IsClean
            || missingCodes.Length > 0
            || unknownCodes.Length > 0
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

    private static int UsageFailure()
    {
        PrintUsage();
        return UsageExitCode;
    }

    private static IHost CreateHost(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                if (context.HostingEnvironment.IsDevelopment())
                {
                    configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
                }

                configuration.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddApplication();
                services.AddInfrastructure(context.Configuration);
            })
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
