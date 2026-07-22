namespace QuranDashboard.Application.Abstractions.Security;

// One permanent security-audit event to append inside a security write (e.g. "permission.granted",
// "owner.removed"). Distinct from the product AbwabAuditEventDraft — these never advance the product head.
public sealed record SecurityAuditEventDraft(string EventType, string Payload);
