# Contract: Relationships API

**Feature**: `030-abwab-relationships-templates` | **Source**: Master Plan §11 (Relationships), §7.3,
§9, §5.2, §6.6 — realizes §18.4 (Relationship workstream) only.

## Operations

`add` / `edit` / `delete` / `restore`, plus the authorized relationship reads. **Explicit action
endpoints with endpoint revisions — no drag semantics.** Envelope `ApiResponse<T>`; every mutation DTO
carries `ExpectedTimelineGeneration` and the expected `xmin` of the targeted relationship row (add
carries no row expectation). §11's "endpoint revisions" phrase is realized exactly as:
edit/delete/restore carry the relationship row's expected `xmin`; add carries
`ExpectedTimelineGeneration` only — no relationship row exists yet and no endpoint-category `xmin`
is carried, because endpoint validity and protection are revalidated under the transaction
(`abwab.category_unavailable`, `abwab.manual_protection`). This reading is recorded explicitly, not
silently assumed. Every actionable read carries `TimelineGeneration`. One audited ChangeSet
per operation on the `028` kernel (barrier + `AbwabRevisionState` lock).

## Permissions (§5.2 — exact codes, mechanical verb mapping)

`relationship.view`, `relationship.add`, `relationship.edit`, `relationship.delete`,
`relationship.restore`. No synonym and no borrowed verb; backend handler enforcement is authoritative
and frontend visibility is UX only.

## Shape rules (§7.3)

- **Mutual** (`Similar`, `Opposite`): non-null `LowerCategoryId` / `HigherCategoryId` with
  `LowerCategoryId < HigherCategoryId`; directional columns null.
- **Directional** (`BroaderNarrower`): non-null `SourceCategoryId` (broader) / `TargetCategoryId`
  (narrower); mutual columns null. The inverse label is **derived for display** — never a second row.
- CHECKs enforce the one-shape rule, the canonical ordering, and **no self-link**.
- Filtered unique indexes over **active** rows forbid a duplicate mutual pair **per type** and a
  duplicate directional edge. Because of the canonical ordering, a **reverse** duplicate collapses onto
  the same key.
- Broader/Narrower writes reject **cycles under the transaction**; an explicit direct **A→C is
  allowed** even when A→B→C already exists.

## Protection rules (§7.3, §9)

| Operation | Protected `Relationship` targets |
|---|---|
| add | proposed endpoints |
| edit | **union** of current and proposed endpoints |
| delete / restore | stored endpoints |

Applicable **direct or inherited** protection on **any** target blocks the **entire** mutation. An edit
therefore cannot escape protection by replacing a protected old endpoint with an unprotected new one.

**Ordinary 24-hour protection does not apply**: a relationship mutation neither starts/restarts nor is
blocked by that window (§9, §2.1). Manual protection and the two-hour stabilization layer still apply.

## Lifecycle

Delete and restore are **tracked soft delete / restore**; physical delete is rejected by the `028`
`SavingChanges` guard. Restore revalidates the active-row unique index **inside the transaction**, so a
restore whose canonical pair/edge is active again fails instead of producing a second active row.

## Conflict codes (§11 — exact strings, no additions)

| Situation | Code |
|---|---|
| duplicate active mutual pair (incl. reverse) or directional edge; restore collision | `abwab.relationship_duplicate` |
| Broader/Narrower edge (or race-created edge) would close a cycle | `abwab.relationship_cycle` |
| expected relationship `xmin` fails | `abwab.row_stale` |
| command `ExpectedTimelineGeneration` differs from the locked generation | `abwab.timeline_generation_stale` |
| direct/inherited `Relationship` protection blocks any target | `abwab.manual_protection` |
| any write during the two-hour window | `abwab.stabilization_active` |
| required active endpoint category no longer exists | `abwab.category_unavailable` |

Malformed input (including a self-link submitted through the API) fails with the framework HTTP 400
produced by the accepted `[ApiController]` model/domain validation convention; authorization failures
return the framework HTTP 403 produced by the `[Authorize]` permission policies. Neither carries an
`abwab.*` body code — matching the accepted `028`/`029` behavior; no shared 400/403 envelope is
introduced. No new, renamed, or remapped Abwab code is introduced by this feature.

## Frontend parity (§14.1, §14.3)

The relationship **port**, its **mock**, and the **HTTP adapter** expose the same operations and the
same `abwab.*` codes, proven by a parity suite. Reads carry the server `TimelineGeneration`; a mutation
result never synthesizes one. On success **and** on conflict the relationship projection is invalidated
and reloaded — no silent retry, no local merge — and unsaved input plus still-valid working context are
preserved. Relationship actions are explicit buttons/forms; **no drag-and-drop**.

## Tests

- Real PostgreSQL: one-shape/canonical-order/no-self CHECKs; duplicate and **reverse-duplicate**;
  duplicate directional edge; cycle rejection; **race-created cycle** under concurrent writes; direct
  A→C allowed alongside A→B→C; tracked soft delete/restore with physical delete rejected; **restore
  collision**; stale row.
- Protection: add/edit/delete/restore target sets, direct **and** inherited, including the
  **protected-old-to-unprotected-new edit**; whole-mutation blocking.
- Ordinary-window proof: **0** window starts and **0** window blocks caused by relationship mutations.
- Cross-layer: identical codes across API, mock, HTTP adapter, generated contract, and UI conflict
  handling.
