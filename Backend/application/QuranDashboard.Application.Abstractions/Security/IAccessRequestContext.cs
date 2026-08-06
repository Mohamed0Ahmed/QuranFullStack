namespace QuranDashboard.Application.Abstractions.Security;

public interface IAccessRequestContext
{
    string? CorrelationId { get; }
}
