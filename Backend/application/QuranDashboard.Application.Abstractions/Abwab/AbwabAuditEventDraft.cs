namespace QuranDashboard.Application.Abstractions.Abwab;

// One append-only event to record inside the commit. EventOrdinal is the deterministic order WITHIN
// the operation (distinct from the ChangeSet's cross-operation commit sequence).
public sealed record AbwabAuditEventDraft(int EventOrdinal, string Payload);
