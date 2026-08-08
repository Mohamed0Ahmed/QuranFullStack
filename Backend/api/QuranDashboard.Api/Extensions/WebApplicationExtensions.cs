using System.Data.Common;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using QuranDashboard.Api.Access;
using QuranDashboard.Api.Authorization.Validation;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Infrastructure.Persistence;

namespace QuranDashboard.Api.Extensions;

public static class WebApplicationExtensions
{
    private static readonly TimeSpan PermissionCatalogueStartupBudget = TimeSpan.FromSeconds(15);

    public static async Task SynchronizePermissionCatalogueAsync(this WebApplication app)
    {
        if (!app.Services.GetRequiredService<IOptions<PermissionCatalogueStartupOptions>>().Value.Enabled)
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(WebApplicationExtensions));
        using var startupBudget = new CancellationTokenSource(PermissionCatalogueStartupBudget);
        await using var scope = app.Services.CreateAsyncScope();
        try
        {
            var pendingMigrations = (await scope.ServiceProvider
                .GetRequiredService<QuranDashboardDbContext>()
                .Database.GetPendingMigrationsAsync(startupBudget.Token))
                .ToArray();
            if (pendingMigrations.Length > 0)
            {
                logger.LogWarning(
                    "Permission catalogue startup sync skipped: {pendingMigrationCount} pending migrations",
                    pendingMigrations.Length);
                return;
            }

            var result = await scope.ServiceProvider
                .GetRequiredService<IPermissionCatalogueSynchronizer>()
                .SynchronizeAsync(startupBudget.Token);
            logger.LogInformation(
                "Permission catalogue synchronized: added {addedCount}, updated {updatedCount}, "
                + "unknown {unknownCount}, retired {retiredCount}",
                result.AddedCodes.Count,
                result.UpdatedCodes.Count,
                result.UnknownDatabaseCodes.Count,
                result.RetiredCanonicalCodes.Count);
        }
        catch (Exception exception) when (exception is DbException
            or SocketException
            or InvalidOperationException
            or OptionsValidationException
            or OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Permission catalogue startup sync failed; the application starts degraded");
        }
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "QuranDashboard API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseCors("AngularDev");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Services.GetRequiredService<UnsafeEndpointMetadataValidator>()
            .Validate(app.Services.GetServices<EndpointDataSource>().SelectMany(source => source.Endpoints));

        return app;
    }
}
