namespace QuranDashboard.Application.Abstractions.Abwab;

public sealed record AbwabAuditedOperationOutcome(bool IsNoOp, IReadOnlyList<AbwabAuditEventDraft> Events)
{
    public static AbwabAuditedOperationOutcome NoOp { get; } = new(true, []);

    public static AbwabAuditedOperationOutcome Audited(IReadOnlyList<AbwabAuditEventDraft> events) => new(false, events);

    public static AbwabAuditedOperationOutcome Audited(params AbwabAuditEventDraft[] events) => new(false, events);
}
