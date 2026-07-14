using System.Reflection;

namespace QuranDashboard.Api.Controllers.Dashboard;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(IHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// يُرجع معلومات التطبيق: الاسم والإصدار وبيئة التشغيل.
    /// </summary>
    /// <response code="200">تم تحميل معلومات التطبيق بنجاح.</response>
    [HttpGet("info")]
    public ActionResult<ApiResponse<AppInfoData>> GetInfo()
    {
        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "0.1.0";

        var data = new AppInfoData("المنهج القرآني", version, environment.EnvironmentName);
        return Ok(ApiResponse<AppInfoData>.Ok(data, ApiMessages.DashboardInfo));
    }
}

public sealed record AppInfoData(string AppName, string Version, string Environment);
