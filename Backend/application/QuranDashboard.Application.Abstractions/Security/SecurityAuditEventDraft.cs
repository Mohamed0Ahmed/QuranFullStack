namespace QuranDashboard.Application.Abstractions.Security;

public sealed record SecurityAuditEventDraft(string EventType, string Payload);
