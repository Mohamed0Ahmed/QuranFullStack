# Quickstart: Validating Abwab Core — Sections, Categories, Tree, and Protection

**Feature**: `029-abwab-core` | **Source**: Master Plan §18.3 exit/acceptance

This guide lists the runnable checks that prove each §18.3 exit gate. It is a **validation guide**,
not implementation. Detailed obligations live in [`contracts/`](./contracts/) and
[`data-model.md`](./data-model.md). The feature is complete only when **every** check passes in the
mandatory internal order (schema/read → protection → writers → frontend slice).

## Prerequisites

- .NET 10 SDK; Docker running (Testcontainers PostgreSQL 4.4.0).
- Node + the frontend workspace `Frontend/quran-dashboard-ui` (reuses `028`'s `@angular/forms`,
  §14.1 primitives, IndexedDB cache, and the Playwright harness).
- Backend test project: `Backend/tests/QuranDashboard.Tests` (xUnit + FluentAssertions).
- Accepted `028` substrate (tracked ChangeSet UoW, `AbwabWriteBarrier`,
  `ExpectedTimelineGeneration`, server clock).

```bash
# Backend: migration-based real-Postgres domain tests
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj

# Frontend: unit tests MUST run with the preserved fork cap (from package.json "test")
cd Frontend/quran-dashboard-ui && npm test   # VITEST_MIN_FORKS/VITEST_MAX_FORKS enforced
# Playwright core browser suite (mock/HTTP parity, RTL, large-tree, no-drag, context-preserve)
```

## Stage 1 — Schema and read-only tree

**Expect** (real PostgreSQL): migration creates Section/Category/Alias/revision and seeds **exactly
one permanent default section** (`أبواب غير مصنفة`). Root names are **globally unique across
sections**; sibling names unique by the **§5.1 normalization contract**; aliases follow **separate
owned-row** uniqueness/search. The `كل الأبواب` projection, independent root orders, explicit child
order, and ancestry/depth read correctly. Section/Category/Order **restore snapshots round-trip**.
**No** category/section mutation endpoint or editable UI exists yet. See
[`tree-read-contract.md`](./contracts/tree-read-contract.md),
[`restore-adapters-contract.md`](./contracts/restore-adapters-contract.md).

## Stage 2 — Protection storage and resolver

**Expect** (real PostgreSQL): exactly **one active ManualProtection record per `(CategoryId,
type)`**; the resolver returns type/scope, the **direct/inherited source ancestor**, and
**server-derived expiry** (server-clock DTOs) with action classification; inheritance is evaluated
from **current `AncestorIds`**; the **deep-tree query budget** holds; authorized protection
view/lift by **immutable ID** works on a **soft-deleted** target. The **ManualProtection adapter is
accepted before any protected writer exists**. See
[`manual-protection-contract.md`](./contracts/manual-protection-contract.md).

## Stage 3 — Activate tracked writers

**Expect** (real PostgreSQL / API):
- Every action runs on **one audited ChangeSet** carrying `ExpectedTimelineGeneration` / expected
  `xmin` / expected `TreeRevision`.
- Section name/non-empty-delete races map **exactly** to `abwab.section_name_conflict` /
  `abwab.section_not_empty` (and `abwab.permanent_default_section`) identically across API, core
  mock/HTTP, frontend, and contract tests.
- Create/promote-root defaulting, **global-order preservation on section move**, self/descendant/
  overlapping bulk-move rejection, descendant ancestry rewrite, one-`TreeRevision` reorder, and
  concurrent move/reorder conflict.
- Alias add/edit/remove authorized by **`category.edit`** (never a child verb); **tracked soft
  delete**; physical delete rejected; CategorySearchAlias adapter round-trips.
- `RepresentativeQuranExcerpt` is an optional **plain string** (no Quran FK, no ayah validation)
  activating ordinary protection.
- Ordinary 24-hour tests prove **only** direct-content edits/moves are gated and start the window;
  last-editor/SystemOwner + stronger manual/stabilization denial match **§9**.
- Manual apply/lift/preset: idempotent same-scope apply with **no audit no-op**, expected-version
  audited scope change, `abwab.manual_protection_scope_conflict`, apply/lift/preset atomicity,
  stable preview blocker identity, adapter round-trips.
- **Full five-type preset**: none/some/all pre-existing types, mixed scopes, one scope applied to
  all five, required Expected Versions per changed scope, all-matching no-op, per-type later lift,
  and a **concurrent stale scope edit rolling back the entire command**.
- Atomic **subtree delete/operation-restore**: child/parent order, all-row tracked atomicity,
  protection on every affected category, a generic **RESTRICT/no-cascade + dependent-visibility
  core fixture**, conflict rollback, versioned adapter round-trips — **no forward relationship/link
  schema dependency**.
- **Reservation seam** present but inert; `032` installs/tests the Pending-aware checker before
  Submit.
- **There is no drag-and-drop.** See [`categories-api.md`](./contracts/categories-api.md),
  [`sections-api.md`](./contracts/sections-api.md).

## Stage 4 — Domain frontend vertical slice

**Expect**: core port + core mock, backend contract, HTTP mapping, and UI action visibility in
**parity**; **composite-read** tests over every grant combination of `category.view` /
`section.view` / `protection.view` with **0** partial leaks (backend DTO projection); category
editors **reuse the `028` Reactive Forms package**; the **§6.3 audit render payloads** (category
create/edit, bulk-move, subtree-deletion, manual-protection — **ordering folded into** bulk-move +
category-edit, no standalone component) publish; the browser/source suite passes
**mock/HTTP parity, stale-cache, rollback, RTL keyboard/focus, large-tree, explicit action,
no-edit-session-lock, no-drag, post-mutation context preservation**. See
[`tree-read-contract.md`](./contracts/tree-read-contract.md),
[`audit-render-contract.md`](./contracts/audit-render-contract.md).

## Final gate

- All §18.3 exit/acceptance criteria pass in CI, in the mandatory internal order.
- The **three** registered restore adapters (Section, Category incl. all three orders + subtree
  delete/operation-restore, ManualProtection) are versioned, round-trip tested, and **marked
  accepted for `033`**; order is a tested facet within Category/Section, and a §8 registry test
  fails CI on a standalone Order (duplicate) or any missing registration.
- `029` builds **no** relationship/template (`030`), attribution/link (`031`), workspace/review/
  notification surface (`032`), audit-restore read model/planner/execution (`033`), or realtime
  (`034`).
