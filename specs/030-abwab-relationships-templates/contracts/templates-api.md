# Contract: Templates API (editor)

**Feature**: `030-abwab-relationships-templates` | **Source**: Master Plan §11 (Templates), §7.4, §5.2,
§6.4, §9 — realizes §18.4 (Template workstream) only. Application to a real category has its own
contract: [`template-application-contract.md`](./template-application-contract.md).

## Operations

Aggregate `create` / `edit` / `delete` / `restore`, and the node/alias internals: node
`add` / `edit` / `reparent` / `reorder` / `remove`, alias `add` / `edit` / `remove` / `restore`, plus
authorized template reads. **Explicit action endpoints — no drag semantics.** Envelope
`ApiResponse<T>`; every mutation DTO carries `ExpectedTimelineGeneration`, the expected
`TemplateRevision` where the operation is structural, and the targeted row's expected `xmin`. One
audited ChangeSet per operation on the `028` kernel.

**There is no create-from-existing operation** — no endpoint, command, UI action, or backend service
reads real categories into a template, and no template or node may be copied across doors (§7.4).

## Permission ownership (§5.2 — frozen, no borrowed verbs)

| Command | Required code |
|---|---|
| create the DoorTemplate aggregate | `template.add` **only** |
| node add / edit / reparent / reorder / internal remove; alias add / edit / remove / restore | `template.edit` |
| aggregate delete / restore | `template.delete` / `template.restore` |
| apply to one real category | `template.apply` |
| read templates / history | `template.view` |

`template.add` grants **nothing** beyond creating the aggregate. Aggregate subresources invent no
child-CRUD permissions. Partial grants cannot borrow another verb, and frontend hiding authorizes
nothing — backend handler enforcement is authoritative.

§5.2 names node/alias "internal removal" under `template.edit` and does not separately name alias
**restore**; alias restore is the same internal, aggregate-scoped operation and maps to
`template.edit` here (aggregate lifecycle restore alone uses `template.restore`). Recorded
explicitly as the mechanical completion of §5.2 — a genuine change returns to the Master Plan.

## Structure rules (§7.4, §6.4)

- Node create/reparent/reorder use **expected `TemplateRevision`** and **tracked rows**.
- Reject **self-parenting** and a destination **inside the moved node's descendant tree**.
- Validate the parent chain **under the transaction**.
- Update affected **sibling orders atomically**; `SiblingOrder` is explicit.
- Bump `TemplateRevision` **exactly once** per grouped operation.
- **No cyclic template can be saved, applied, rendered, or restored.**
- `TemplateNodeSearchAlias` mirrors the category-alias value/normalization/soft-delete contract;
  remove/restore is **tracked soft delete**, physical delete is rejected, and alias **history is never
  lost**.
- `RepresentativeQuranExcerpt` on a node is an optional **plain string** — no Quran FK, no ayah
  validation.
- Names/aliases normalize through the one §5.1 `ArabicNameNormalizer`.

## Protection and gating (§9)

Template editor CRUD has **no real-category manual target** and **no ordinary 24-hour window** — it is
gated by the template permission plus the global two-hour stabilization layer. (Manual
`InternalStructure` protection applies to **application**, not to editing.)

## Conflict codes (§11 — exact strings, no additions)

| Situation | Code |
|---|---|
| node create/reparent would create a cycle (incl. a cyclic restore) | `abwab.template_cycle` |
| expected `TemplateRevision` fails | `abwab.template_revision_stale` |
| expected row `xmin` fails | `abwab.row_stale` |
| command `ExpectedTimelineGeneration` differs from the locked generation | `abwab.timeline_generation_stale` |
| any write during the two-hour window | `abwab.stabilization_active` |

Malformed input fails with the framework HTTP 400 produced by the accepted `[ApiController]`
model/domain validation convention; authorization failures return the framework HTTP 403 produced by
the `[Authorize]` permission policies. Neither carries an `abwab.*` body code — matching the accepted
`028`/`029` behavior; no shared 400/403 envelope is introduced. No new, renamed, or remapped Abwab
code is introduced.

## Frontend parity (§14.1, §14.3)

The template **port**, its **mock**, and the **HTTP adapter** expose the same operations and codes,
proven by a parity suite. The editor uses the already-installed Reactive Forms package with **explicit
save** (no autosave, no edit-session lock). Ordering and reparent are **explicit actions** — the
`check:no-drag` source gate and the browser no-drag proof cover the template editor too. On success or
conflict the template projection is invalidated and reloaded; unsaved input and still-valid context are
preserved.

## Tests

- Real PostgreSQL / API: self-reparent and descendant-reparent rejection; parent-chain validation under
  the transaction; **stale and concurrent** reparent/reorder; **cyclic restore**; valid reparent
  updating sibling order atomically with **exactly one** `TemplateRevision` bump; alias soft
  delete/restore with physical delete rejected and history intact.
- Negative/absence: **no** create-from-real-door path and **no** cross-door copy path exist (source and
  API-level proof).
- Permission matrix: every verb tested with each partial grant — `template.add`-only cannot add nodes or
  aliases; `template.edit`-only cannot delete/restore the aggregate or apply it; hidden UI actions
  invoked directly are still rejected.
