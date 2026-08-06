using System.Data.Common;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuranDashboard.Application;
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
        OwnerValidation,
        OwnerStatus,
        OwnerDryRun,
        OwnerApply,
    }

    internal static async Task<int> Main(string[] args)
    {
        var request = ParseCommand(args);
        if (request is null)
        {
            return UsageFailure();
        }

        try
        {
            using var host = CreateHost(args);
            using var scope = host.Services.CreateScope();

            return request.Command switch
            {
                AccessAdminCommand.IdentityScan => await ScanIdentityAsync(scope.ServiceProvider),
                AccessAdminCommand.IdentityBackfill => await BackfillIdentityAsync(scope.ServiceProvider),
                AccessAdminCommand.CatalogueSync => await SynchronizeCatalogueAsync(scope.ServiceProvider),
                AccessAdminCommand.AuthorizationPreflight => await RunPreflightAsync(scope.ServiceProvider),
                AccessAdminCommand.OwnerValidation => await ValidateOwnerConfigurationAsync(scope.ServiceProvider),
                AccessAdminCommand.OwnerStatus => await ReadOwnerStatusAsync(scope.ServiceProvider),
                AccessAdminCommand.OwnerDryRun => await ReadOwnerDryRunAsync(scope.ServiceProvider),
                AccessAdminCommand.OwnerApply => await ApplyOwnerReconciliationAsync(
                    scope.ServiceProvider,
                    request.Reason!,
                    request.ConfirmsProduction),
                _ => throw new InvalidOperationException("Unknown AccessAdmin command."),
            };
        }
        catch (OwnerReconciliationReasonValidationException)
        {
            return UsageFailure();
        }
        catch (HttpRequestException)
        {
            return ProviderFailure();
        }
        catch (Exception exception) when (exception is DbException
            or SocketException
            or InvalidOperationException
            or OptionsValidationException)
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
        var owners = await services.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);
        PrintOwnerResult(owners);
        Console.WriteLine($"pending_migrations={string.Join(',', pendingMigrations)}");
        Console.WriteLine($"catalogue_missing={string.Join(',', missingCodes)}");
        Console.WriteLine($"catalogue_unknown={string.Join(',', unknownCodes)}");
        Console.WriteLine($"catalogue_retired={string.Join(',', retiredCodes)}");

        return pendingMigrations.Any()
            || !identity.IsClean
            || missingCodes.Length > 0
            || unknownCodes.Length > 0
            || retiredCodes.Length > 0
            || !owners.IsReady
            ? PreflightFailureExitCode
            : SuccessExitCode;
    }

    private static async Task<int> ValidateOwnerConfigurationAsync(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<OwnerBootstrapOptions>>().Value;

        Console.WriteLine($"owner_configured={options.NormalizedEmails.Count}");
        Console.WriteLine($"owner_config_fingerprint={options.ConfigurationFingerprint}");
        var result = await services.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);
        PrintOwnerResult(result);
        return result.IsReady ? SuccessExitCode : PreflightFailureExitCode;
    }

    private static async Task<int> ReadOwnerStatusAsync(IServiceProvider services)
    {
        var result = await services.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);
        PrintOwnerResult(result);
        return result.IsReady ? SuccessExitCode : PreflightFailureExitCode;
    }

    private static async Task<int> ReadOwnerDryRunAsync(IServiceProvider services)
    {
        var result = await services.GetRequiredService<IOwnerReconciliationService>()
            .GetStatusAsync(CancellationToken.None);
        PrintOwnerResult(result);
        Console.WriteLine("owner_apply_reacquires_lock_and_recomputes=true");
        return result.IsReady ? SuccessExitCode : PreflightFailureExitCode;
    }

    private static async Task<int> ApplyOwnerReconciliationAsync(
        IServiceProvider services,
        string reason,
        bool confirmsProduction)
    {
        if (services.GetRequiredService<IHostEnvironment>().IsProduction() && !confirmsProduction)
        {
            return UsageFailure();
        }

        var result = await services.GetRequiredService<IOwnerReconciliationService>()
            .ReconcileAsync(reason, CancellationToken.None);
        PrintOwnerResult(result);
        return result.IsReady ? SuccessExitCode : PreflightFailureExitCode;
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

    private static void PrintOwnerResult(OwnerReconciliationResult result)
    {
        Console.WriteLine($"owner_config_fingerprint={result.ConfigurationFingerprint}");
        Console.WriteLine($"owner_unchanged={result.Candidates.Count(candidate => candidate.State == OwnerReconciliationCandidateState.Unchanged)}");
        Console.WriteLine($"owner_added={result.Candidates.Count(candidate => candidate.State == OwnerReconciliationCandidateState.Added)}");
        Console.WriteLine($"owner_removed={result.Candidates.Count(candidate => candidate.State == OwnerReconciliationCandidateState.Removed)}");
        Console.WriteLine($"owner_awaiting_verified_sign_in={result.Candidates.Count(candidate => candidate.State == OwnerReconciliationCandidateState.AwaitingVerifiedSignIn)}");
        Console.WriteLine($"owner_unresolved={result.Candidates.Count(candidate => candidate.State == OwnerReconciliationCandidateState.Unresolved)}");
        Console.WriteLine($"owner_configured_disabled={result.Candidates.Count(candidate => candidate.State == OwnerReconciliationCandidateState.ConfiguredDisabled)}");
        Console.WriteLine($"owner_direct_grant_cleanup={result.Candidates.Count(candidate => candidate.State == OwnerReconciliationCandidateState.OwnerHasDirectGrants)}");
        Console.WriteLine($"owner_removal_blocked_last_owner={result.Candidates.Count(candidate => candidate.State == OwnerReconciliationCandidateState.RemovalBlockedByLastOwner)}");
        Console.WriteLine($"owner_can_apply={result.CanApply.ToString().ToLowerInvariant()}");
        Console.WriteLine($"owner_ready={result.IsReady.ToString().ToLowerInvariant()}");
    }

    private static AccessAdminRequest? ParseCommand(string[] args)
    {
        return args switch
        {
            ["identity", "scan"] => new AccessAdminRequest(AccessAdminCommand.IdentityScan),
            ["identity", "backfill", "--apply"] => new AccessAdminRequest(AccessAdminCommand.IdentityBackfill),
            ["catalogue", "sync"] => new AccessAdminRequest(AccessAdminCommand.CatalogueSync),
            ["authorization", "preflight"] => new AccessAdminRequest(AccessAdminCommand.AuthorizationPreflight),
            ["owners", "validate"] => new AccessAdminRequest(AccessAdminCommand.OwnerValidation),
            ["owners", "status"] => new AccessAdminRequest(AccessAdminCommand.OwnerStatus),
            ["owners", "reconcile", "--dry-run"] => new AccessAdminRequest(AccessAdminCommand.OwnerDryRun),
            ["owners", "reconcile", "--apply", "--reason", var reason] when !string.IsNullOrWhiteSpace(reason)
                => new AccessAdminRequest(AccessAdminCommand.OwnerApply, reason),
            ["owners", "reconcile", "--apply", "--reason", var reason, "--confirm-production"]
                when !string.IsNullOrWhiteSpace(reason)
                => new AccessAdminRequest(AccessAdminCommand.OwnerApply, reason, true),
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

    private static int ProviderFailure()
    {
        Console.Error.WriteLine("access_admin_failure=LogtoProviderUnavailable");
        Console.Error.WriteLine(
            "The command could not retrieve required Logto identity data. Verify the Logto Management API configuration and availability.");
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
            .ConfigureLogging(logging => logging.AddFilter(
                "System.Net.Http.HttpClient.IExternalUserProfileSource",
                LogLevel.None))
            .ConfigureServices((context, services) => services
                .AddOwnerReconciliation()
                .AddInfrastructure(context.Configuration))
            .Build();
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin identity scan");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin identity backfill --apply");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin catalogue sync");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin authorization preflight");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin owners validate");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin owners status");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin owners reconcile --dry-run");
        Console.Error.WriteLine("  QuranDashboard.AccessAdmin owners reconcile --apply --reason <text> [--confirm-production]");
    }

    private sealed record AccessAdminRequest(
        AccessAdminCommand Command,
        string? Reason = null,
        bool ConfirmsProduction = false);
}
