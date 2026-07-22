namespace QuranDashboard.Application.Abstractions.Security;

public sealed record SecurityAuditOutcome(bool IsNoOp, IReadOnlyList<SecurityAuditEventDraft> Events)
{
    public static SecurityAuditOutcome NoOp { get; } = new(true, []);

    public static SecurityAuditOutcome Audited(params SecurityAuditEventDraft[] events) => new(false, events);
}
