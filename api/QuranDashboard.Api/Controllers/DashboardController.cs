using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Contracts;

namespace QuranDashboard.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    [HttpGet("info")]
    public ActionResult<ApiResponse<object>> GetInfo()
    {
        var payload = new
        {
            name = "Quran Dashboard",
            description = "Backend foundation for the Quran Dashboard application.",
            scope = "foundation",
            utc = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.Ok(payload));
    }
}
