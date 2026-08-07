using System.Diagnostics;
using QuranDashboard.Application.Abstractions.Security;

namespace QuranDashboard.Infrastructure.Access;

internal sealed class AmbientAccessRequestContext : IAccessRequestContext
{
    public string? CorrelationId => Activity.Current?.TraceId.ToString();
}
