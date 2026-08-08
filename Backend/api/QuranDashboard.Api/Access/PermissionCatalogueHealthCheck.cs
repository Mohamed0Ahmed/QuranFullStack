using Microsoft.Extensions.Diagnostics.HealthChecks;
using QuranDashboard.Application.Abstractions.Access;

namespace QuranDashboard.Api.Access;

public sealed class PermissionCatalogueHealthCheck(IPermissionCatalogueReader reader) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var catalogue = await reader.GetActiveAsync(cancellationToken);
        return catalogue.AssignmentReady
            ? HealthCheckResult.Healthy("The permission catalogue is persisted and assignable.")
            : new HealthCheckResult(
                context.Registration.FailureStatus,
                "The permission catalogue is not fully persisted, so permission assignment is unavailable.");
    }
}
