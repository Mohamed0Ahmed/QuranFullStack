using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Contracts;

namespace QuranDashboard.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        var payload = new
        {
            status = "ok",
            service = "Quran Dashboard API",
            utc = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.Ok(payload));
    }
}
