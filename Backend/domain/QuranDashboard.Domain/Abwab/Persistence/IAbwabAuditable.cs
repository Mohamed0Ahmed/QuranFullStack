namespace QuranDashboard.Domain.Abwab.Persistence;

// Marks an Abwab domain write target governed by the kernel: every add/modify/delete of such an
// entity MUST run inside a tracked ChangeSet, physical deletes are rejected, and removal is
// expressed as a soft-delete. Kernel machinery (ChangeSet, AuditEvent, revision/barrier/boundary
// state) deliberately does NOT implement this — it is the audit substrate, not an audited writer.
// 028 owns no such entity; foundation-only fixtures stand in for the future 029+ domain writers.
public interface IAbwabAuditable
{
    bool IsDeleted { get; }
}
