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
    /// <response code="200">تم تنفيذ فحوصات الحالة؛ الحالة الكلية ضمن البيانات (healthy أو degraded).</response>
    /// <response code="503">الحالة الكلية unhealthy؛ البيانات (بما فيها فحص كل اعتمادية) لا تزال مُرفقة.</response>
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
            HealthStatus.Degraded => ApiMessages.HealthDegraded,
            _ => ApiMessages.HealthUnhealthy
        };

        var data = new HealthReportData(overallStatus, checks);

        if (report.Status == HealthStatus.Unhealthy)
        {
            // Railway/infra probes key on HTTP status, so an unhealthy dependency must not report 200.
            // ApiResponse.Fail carries no data, so the failure envelope is built inline to still carry
            // the per-check detail probes/consumers need.
            var failureEnvelope = new ApiResponse<HealthReportData>
            {
                IsSuccess = false,
                Message = message,
                Data = data
            };
            return StatusCode(StatusCodes.Status503ServiceUnavailable, failureEnvelope);
        }

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
