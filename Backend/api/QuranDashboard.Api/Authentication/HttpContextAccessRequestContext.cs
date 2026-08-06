using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Api.Authentication;

public sealed class HttpContextAccessRequestContext(IHttpContextAccessor httpContextAccessor) : IAccessRequestContext
{
    public string? CorrelationId => httpContextAccessor.HttpContext?.TraceIdentifier;
}
