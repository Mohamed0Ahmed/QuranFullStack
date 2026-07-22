namespace QuranDashboard.Application.Abstractions.Security;

// The result of the domain operation run inside a security write. A no-op (idempotent re-grant/re-revoke,
// already-active owner) stages nothing and produces NO audit event; an audited outcome carries the events
// to append permanently.
public sealed record SecurityAuditOutcome(bool IsNoOp, IReadOnlyList<SecurityAuditEventDraft> Events)
{
    public static SecurityAuditOutcome NoOp { get; } = new(true, []);

    public static SecurityAuditOutcome Audited(params SecurityAuditEventDraft[] events) => new(false, events);
}
