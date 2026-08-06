using Microsoft.AspNetCore.Mvc;
using QuranDashboard.Api.Authorization.Metadata;
using QuranDashboard.Api.Common;
using QuranDashboard.Api.Contracts;
using QuranDashboard.Application.Abstractions.Security.Permissions;

namespace QuranDashboard.Tests.Api.Access;

[ApiController]
[Route("api/test/authorization")]
public sealed class AuthorizationPipelineProbeController(AuthorizationPipelineProbe probe) : ControllerBase
{
    [HttpPost("permission")]
    [RequirePermission(AbwabPermissions.Doors.Create)]
    public IActionResult Permission()
    {
        probe.RecordInvocation();
        return Ok(ApiResponse<object>.Ok(new { authorization = "permission" }, ApiMessages.OperationSuccess));
    }

    [HttpPost("owner")]
    [RequireOwner]
    public IActionResult Owner()
    {
        probe.RecordInvocation();
        return Ok(ApiResponse<object>.Ok(new { authorization = "owner" }, ApiMessages.OperationSuccess));
    }
}

public sealed class AuthorizationPipelineProbe
{
    private int _invocationCount;

    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public void RecordInvocation() => Interlocked.Increment(ref _invocationCount);
}
