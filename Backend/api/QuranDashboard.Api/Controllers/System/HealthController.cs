using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace QuranDashboard.Api.Controllers.System;

[ApiController]
[Route("api/health")]
public sealed class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    /// <summary>
    /// يُرجع الحالة الصحية للتطبيق واعتمادياته (قاعدة البيانات) مع حالة كل فحص على حدة.
    /// </summary>
    /// <param name="cancellationToken">رمز إلغاء الطلب.</param>
    /// <response code="200">تم تنفيذ فحوصات الحالة؛ الحالة الكلية ضمن البيانات (healthy أو degraded أو unhealthy).</response>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<HealthReportData>>> Get(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);

        var checks = report.Entries.Select(e => new HealthCheckItem(
            e.Key,
            MapStatus(e.Value.Status))).ToList();

        var overallStatus = MapStatus(report.Status);

        var message = report.Status switch
        {
            HealthStatus.Healthy => ApiMessages.HealthOk,
            _ => ApiMessages.HealthDegraded
        };

        var data = new HealthReportData(overallStatus, checks);
        return Ok(ApiResponse<HealthReportData>.Ok(data, message));
    }

    private static string MapStatus(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => "healthy",
        HealthStatus.Degraded => "degraded",
        _ => "unhealthy"
    };
}

public sealed record HealthReportData(string Status, IReadOnlyList<HealthCheckItem> Checks);

public sealed record HealthCheckItem(string Name, string Status);
