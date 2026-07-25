# Contract: Relationship dormancy across category subtree delete / operation-restore

**Feature**: `030-abwab-relationships-templates` | **Source**: Master Plan §18.4 (Relationship
workstream, final bullet), §7.1, §7.3, §8. This contract fills the **generic dependent-visibility seam**
`029` shipped with a core fixture; `030` supplies the first **real** dependent.

## Obligation

When a category subtree is soft-deleted by the `029` `CategorySubtreeHandler`:

- Every `CategoryRelationship` row touching a deleted category **remains present and unchanged** —
  **no cascade delete**, no soft-delete stamp on the relationship row, **no history loss**.
- Those rows are **dormant**: filtered out of relationship read projections and not actionable while an
  endpoint is deleted.
- On category **operation-restore**, the **same rows** (same `CategoryRelationshipId`, same history)
  become **visible again** — no re-creation, no new identity.
- **Stored-endpoint `Relationship` protection is enforced** on the delete and restore paths, exactly as
  §7.3 defines the stored-endpoint target set.

## Mechanism

- Endpoint FKs are **RESTRICT / no-cascade**; the relationship row carries no deletion state of its own.
- Dormancy is a **read projection** derived from the endpoints' current deleted state — **not** a
  written flag. A category restore therefore needs **no relationship-side write** to reverse, and the
  relationship inverse adapter has nothing extra to restore.
- The `029` subtree handler is **not modified** to know about relationships: dormancy falls out of the
  schema property plus the relationship read projection.

## Audit and restore consequences

- A category subtree-delete/operation-restore ChangeSet renders **dormant attached-state counts** for
  relationships (§6.3) — attached state is labelled **dormant**, never falsely shown as deleted.
  The counts are computed **on read** by a `030` relationship-count projection over the affected
  category set and rendered through the generic `dormantDependentCounts` seam of the `029` render
  model — the stored `029` event payload is unchanged.
- Because no relationship row is written, a category subtree delete produces **no** relationship
  ChangeSet and **no** relationship audit event.

## Tests (real PostgreSQL)

1. Delete a subtree whose categories carry mutual and directional relationships → **0** relationship
   rows deleted, **0** relationship rows modified, **0** history rows lost; the rows read as dormant.
2. Operation-restore the same subtree → **100%** of those rows visible again with identical IDs and
   identical history; **0** new rows created.
3. Attempt a subtree delete where a **stored endpoint** carries direct or inherited `Relationship`
   protection → blocked with `abwab.manual_protection`; likewise on the restore path.
4. Attempt a **physical** delete of a category still referenced by a relationship row → rejected by the
   RESTRICT FK (no cascade path exists).
5. Delete only **one** endpoint of a relationship → the row is dormant while that endpoint is deleted
   and visible again once it is restored.

## Boundary

This contract covers **relationships only**. Link/member/highlight/note dormancy against the same `029`
seam is owned by `031`; workspace/request state at the deletion seam is owned by `032`. `030` neither
implements nor depends on either.
