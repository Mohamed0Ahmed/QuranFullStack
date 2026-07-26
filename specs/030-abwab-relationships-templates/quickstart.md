# Quickstart: Validating Abwab Relationships and Templates

**Feature**: `030-abwab-relationships-templates` | **Source**: Master Plan §18.4 exit/acceptance

This guide lists the runnable checks that prove each §18.4 exit gate. It is a **validation guide**, not
implementation. Detailed obligations live in [`contracts/`](./contracts/) and
[`data-model.md`](./data-model.md).

§18.4 allows the two workstreams to run **in parallel**; the feature is complete only when **both**
have finished **their own adapter and their own vertical slice** and every check below passes in CI.

## Prerequisites

- .NET 10 SDK; Docker running (Testcontainers PostgreSQL 4.4.0).
- Node + the frontend workspace `Frontend/quran-dashboard-ui` (reuses the `028` §14.1 primitives,
  IndexedDB cache, `@angular/forms`, and the Playwright harness).
- Backend test project: `Backend/tests/QuranDashboard.Tests` (xUnit + FluentAssertions).
- Accepted `028` substrate (tracked ChangeSet UoW, `AbwabWriteBarrier`, `ExpectedTimelineGeneration`,
  server clock) **and** accepted `029` core (category writer, protection resolver,
  `CategoryRestoreAdapter`, `ArabicNameNormalizer`).

```bash
# Backend: migration-based real-Postgres domain tests
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj

# Frontend: unit tests MUST run with the preserved fork cap (from package.json "test")
cd Frontend/quran-dashboard-ui && npm test   # VITEST_MIN_FORKS/VITEST_MAX_FORKS enforced
npm run check:no-drag                        # static no-drag source gate (covers the template editor)
# Playwright: relationship + template browser specs on the reusable 028 harness
```

## Workstream A — Category relationships

**Expect** (real PostgreSQL): the one typed `CategoryRelationship` table enforces **one shape per row**,
**canonical lower/higher ordering**, and **no self-link** via CHECKs; filtered unique indexes over
active rows reject duplicate mutual pairs per type and duplicate directional edges — a **reverse**
duplicate collapses onto the same key. Broader/Narrower writes reject **cycles under the transaction**,
including a **race-created** cycle from concurrent writes, while an explicit direct **A→C is allowed**
alongside A→B→C. Delete/restore are **tracked soft delete/restore** with physical delete rejected, and a
**restore collision** against a now-active pair/edge fails.

**Expect** (protection): `Relationship` protection targets are **proposed** on add, **current ∪
proposed** on edit, and **stored** on delete/restore; direct **or inherited** protection on **any**
target blocks the **entire** mutation, including the **protected-old-to-unprotected-new edit**.
Relationship mutations start the ordinary 24-hour window **0** times and are blocked by it **0** times.

**Expect** (dormancy): a category subtree delete leaves relationship rows **intact and dormant** with
**0** cascade deletions and **0** history loss; category **operation-restore** makes the same rows
visible again with the same IDs; stored-endpoint protection is enforced on both paths.

**Expect** (slice): relationship port + mock + HTTP adapter in **parity** with identical `abwab.*`
codes; relationship cache keys publish **only** after commit and invalidate + reload on success and on
conflict; explicit actions only, **no drag**; the **specialized relationship audit payload** renders;
the versioned **Relationship** adapter round-trips.

See [`relationships-api.md`](./contracts/relationships-api.md),
[`relationship-dormancy-contract.md`](./contracts/relationship-dormancy-contract.md),
[`audit-render-contract.md`](./contracts/audit-render-contract.md).

## Workstream B — Door templates

**Expect** (editor, real PostgreSQL / API): manual editor CRUD over the aggregate, nodes, aliases, and
order; **no create-from-real-door path and no cross-door copy path exist**; node reparent rejects
**self** and **descendant** destinations and validates the parent chain under the transaction;
**stale/concurrent** reparent/reorder and **cyclic restore** are rejected; a valid reparent updates
sibling order **atomically** and bumps `TemplateRevision` **exactly once**;
`TemplateNodeSearchAlias` remove/restore is **tracked soft delete** with physical delete rejected and
**no alias history lost**.

**Expect** (permissions): the frozen ownership holds at the handler — `template.add` creates **only**
the aggregate; **every** node/alias add/edit/reparent/reorder/internal remove requires `template.edit`;
lifecycle uses `template.delete` / `template.restore`; application uses `template.apply` alone. Partial
grants borrow **no** verb, and hidden UI actions invoked directly are still rejected.

**Expect** (application): one template applied to one target through the **`029` category writer**
creates **every template root as a direct child**, revalidates **uniqueness and protection under the
transaction**, and produces **exactly 1** ChangeSet and **exactly 1** `TreeRevision` bump. The copy
allowlist is exact — **0** links, highlights, notes, requests, sources, decisions, notifications,
workflow/audit history, or technical revisions are copied. Any failure rolls the **whole** application
back.

**Expect** (audit): the **frozen template snapshot at application time** renders and is unchanged by
later template edits; template **CRUD** appears **only** in the separate template-history view with
**0** main product-audit rows.

**Expect** (slice): template port + mock + HTTP adapter in **parity**; editor uses the installed
Reactive Forms package with explicit save and **no edit-session lock**; ordering/reparent are explicit
actions, **no drag**; the **one** DoorTemplate aggregate adapter round-trips.

See [`templates-api.md`](./contracts/templates-api.md),
[`template-application-contract.md`](./contracts/template-application-contract.md),
[`audit-render-contract.md`](./contracts/audit-render-contract.md).

## Final gate (§18.4 exit / acceptance)

- The **Relationship** adapter, the **one DoorTemplate aggregate** adapter, the **application-event
  interpreter**, and its **verified reuse of the `029` Category adapter** are all accepted.
- The extended §8 registry test asserts the registered set is exactly
  `{Section, Category, ManualProtection, Relationship, DoorTemplate}` and **fails CI** on a duplicate
  (notably a "template-created category" adapter, a standalone `TemplateNode`/alias adapter, or the
  interpreter registered as a descriptor) or on a missing registration. See
  [`restore-adapters-contract.md`](./contracts/restore-adapters-contract.md).
- **No** relationship or template writer bypasses the audit / protection / concurrency / stabilization
  foundation — barrier and `SavingChanges` guards hold, and every writer is blocked during the two-hour
  stabilization window.
- Only §11-catalogue `abwab.*` strings are used: **0** strings beyond §11 and **0** renamed or
  remapped (the four §11 strings newly declared in code — `abwab.relationship_duplicate`,
  `abwab.relationship_cycle`, `abwab.template_cycle`, `abwab.template_revision_stale` — are §11
  members, not additions), and the **API contract generation/drift check** (§15.2 gate 4) passes for
  both new endpoint families — each code matches across backend, **generated contract**, mock, HTTP
  adapter, and UI.
- The **numeric performance budgets are frozen** from recorded hardware/data assumptions and p95
  measurements **before** either workstream's writers and UI are accepted (§15.3) — a measurement
  gate, never permission to weaken correctness.
- **Both** workstreams have finished their own adapter and vertical slice; the Spec Kit does not exit
  with either incomplete.
- `030` builds **no** `028` kernel/CI work, **no** `029` core category/section/protection behavior,
  **no** Quran FK or link/source structure (`031`), **no** workspace/review/notification surface
  (`032`), **no** audit read model / preview / planner / restore execution (`033`), and **no** realtime
  transport (`034`).
