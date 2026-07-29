# Feature: Abwab (الأبواب) — Slice B2, browser e2e and the test-doc amendments

Planning document. Expands `plan-slice-b.md` §9 (phase 6, outlined at 7 tasks) into concrete
tasks, now that B1 has merged and the real testids and URL keys exist to write against.

Companion to `plan.md` (Slice A) and `plan-slice-b.md` (Slice B1, phases 4–5).

---

## 1. Why this document exists

`plan-slice-b.md` §9 set the bar for phase 6 but deliberately did not plan it: e2e selectors
cannot be planned against components that do not exist. B1 is now committed (`041f4935`,
`97b4ade0`), so every testid below is **copied from the shipped markup**, not invented.

**Scope:** the doors e2e flows plus the two test-doc amendments the write-invariant deviation
requires. **Out of scope:** any frontend behavior change. If a flow cannot be written without
changing the app, that is a **finding to report**, not a licence to edit the feature.

---

## 2. The deviation this slice is built around

`e2e/README.md:39-41` states the suite's invariant: *"Read-only flows and loose count
assertions only. The suite reads the live local dev DB; exact row counts would break on the
next reseed."*

`TESTING_STRATEGY.md` §6 states a **stronger** precondition: *"do not add write flows to it
without first moving it onto an isolated database."*

Slice B2 violates the §6 precondition outright — deliberately, per the locked decision. That is
why T606 exists and why it must amend **both documents by name** (`plan.md` R1: the amendment
must name `TESTING_STRATEGY.md` §6 explicitly, not by implication).

**The residue must be stated in both files.** Teardown archives the sandbox doors and deletes
the now-empty section. There is no hard delete and no section restore, so:

- every run leaves **archived orphan doors** in the local dev DB, permanently;
- those doors' section is gone, so any future restore of one detaches it
  (`AbwabRestoredDoorDto.detachedFromArchivedSection === true`).

This is tolerable on a local dev DB with loose count assertions. **It is not "self-cleaning",
and neither document may claim that it is.**

---

## 3. Constraints the harness imposes

Read before writing any flow. Each of these has already broken a suite somewhere.

| Constraint | Source | Consequence for these flows |
|---|---|---|
| `fullyParallel: true`, `workers: 2` | `playwright.config.ts:21-22` | **Two workers write to one database concurrently.** Every flow must own a uniquely-named sandbox section; no flow may assert a global count. |
| `retries: 0` | `playwright.config.ts:23` | A flaky write flow fails the suite outright. Prefer `expect.poll`/web-first assertions over fixed waits. |
| Specs are `*.e2e.ts`, never `*.spec.ts` | `e2e/README.md:32-34` | A `.spec.ts` under `e2e/` is swallowed by the Vitest glob and runs headless in the unit suite. |
| Zero external network calls | `e2e/fixtures/app-test.ts:14-24` | The context fixture **fails the test** if any request leaves localhost. Do not add CDN fonts or avatars to a flow. |
| Fresh context per test; never add `storageState` | `e2e/README.md:35-37` | Do not try to share a sandbox section across tests by persisting state. |
| `reuseExistingServer: true` | `playwright.config.ts:11` | Never leave a server on :4200 or :5015 outside Playwright's control — a stray one is adopted silently. |
| Backend boots `--no-build` | `playwright.config.ts:47` | `dotnet build Backend/QuranDashboard.sln` must run first. |
| mkcert certs required | `e2e/README.md:23-25` | Present and verified in this workspace; a missing cert reports only as a port timeout. |

---

## 4. The shipped surface these flows drive

**Route:** `/abwab`. **Query keys** (`models/abwab.models.ts`, `ABWAB_QUERY_KEYS`):
`section` · `view` · `archive` · `door` · `card` · `q`.

**Testids** — copied from the shipped markup. Dynamic ones interpolate the door/section id.

| Area | Testids |
|---|---|
| Page | `abwab-page`, `abwab-page-empty`, `abwab-page-add-root`, `abwab-page-add-root-ghost`, `abwab-page-archive-toggle`, `abwab-page-manage-sections`, `abwab-page-archive-empty` |
| Toolbar | `abwab-toolbar-tab-all`, `abwab-toolbar-tab-<sectionId>`, `abwab-toolbar-search`, `abwab-toolbar-view-tree`, `abwab-toolbar-view-cards` |
| Tree | `abwab-tree`, `abwab-tree-row-<id>`, `abwab-tree-chevron-<id>`, `abwab-tree-checkbox-<id>`, `abwab-tree-order-<id>`, `abwab-tree-order-input-<id>` |
| Cards | `abwab-cards`, `abwab-card-<id>`, `abwab-card-checkbox-<id>`, `abwab-cards-crumb` |
| Side panel | `abwab-side-panel-active-door`, `abwab-side-panel-empty`, `abwab-side-panel-clear`, `abwab-side-panel-op-add-child`, `-op-edit`, `-op-move`, `-op-archive`, `abwab-side-panel-bulk-toggle`, `-bulk-bar`, `-bulk-count`, `-bulk-names`, `-bulk-move`, `-bulk-archive`, `-bulk-clear` |
| Door modal | `abwab-door-modal`, `-backdrop`, `-name`, `-description`, `-ayah`, `-alias-input`, `-context`, `-meta`, `-error`, `-save`, `-cancel`, `-discard-confirm`, `-discard-confirm-yes`, `-discard-confirm-no` |
| Move picker | `abwab-move-picker`, `-backdrop`, `-section-<id>`, `-section-none`, `-dest-<id>`, `-dest-asmain`, `-confirm`, `-cancel` |
| Sections modal | `abwab-sections-modal`, `-backdrop`, `-name-input`, `-add`, `-row-<id>`, `-rename-<id>`, `-rename-input-<id>`, `-rename-save-<id>`, `-delete-<id>`, `-error`, `-close` |
| Archive view | `abwab-archive-view`, `abwab-archive-row-<id>`, `abwab-archive-chevron-<id>`, `abwab-archive-restore-<id>`, `abwab-archive-restore-hint-<id>` |
| Context menu | `abwab-page-context-menu`, `abwab-page-ctx-edit`, `-ctx-add-child`, `-ctx-move`, `-ctx-archive`, `-ctx-backdrop` |
| Confirms | `abwab-page-archive-confirm`, `-yes`, `-no`; `abwab-page-bulk-archive-confirm`, `-yes`, `-no` |
| Chip | `qd-chip`, `qd-chip-remove` |
| Announcer | `abwab-announcer` |

**Testids that must stay absent** — `abwab-side-panel-op-relations`, `abwab-side-panel-op-protect`.
Both exist today only as *negative* assertions in the unit specs. An e2e flow asserting their
absence is a cheap second lock on "zero dead controls".

---

## 5. Tasks

### T601 — the sandbox fixture

**Create** `e2e/fixtures/abwab.ts`.

A Playwright fixture that, per test:

1. creates a sandbox section over the API (not the UI) whose name embeds the worker index and a
   timestamp so the two parallel workers can never collide — e.g. `e2e-sandbox-w<idx>-<ms>`;
2. yields `{ sectionId, sectionName, createDoor(...) }` to the test, recording every door id it
   creates;
3. tears down in the only lawful order — **archive every recorded door, then delete the
   now-empty section**. Section delete `409`s while the section holds live doors
   (`AbwabSectionsController.cs:71`, `ApiMessages.AbwabSectionHasLiveDoors`), so the order is
   forced, not stylistic.

Teardown must be **best-effort and non-failing**: a flow that already archived a door must not
turn teardown into a second failure masking the first.

Setup and teardown go through `request` (the API), not the UI — a UI-driven teardown fails
whenever the flow under test broke the UI, which is exactly when you need teardown to work.

**One helper, not the same dance repeated per flow.**

### T602 — structure flow (`e2e/abwab-structure.e2e.ts`)

Section create/rename via `abwab-page-manage-sections`; the new tab appearing as
`abwab-toolbar-tab-<id>` after the refetch; root door create via `abwab-page-add-root`; child
create via `abwab-side-panel-op-add-child`, asserting the modal's `abwab-door-modal-context`
names the parent; alias chips added on Enter in `abwab-door-modal-alias-input` and removed via
`qd-chip-remove`; edit via `abwab-side-panel-op-edit`; the dirty guard
(`abwab-door-modal-discard-confirm` appears on cancel with unsaved input, `-no` keeps the modal
open, `-yes` closes it).

### T603 — operations flow (`e2e/abwab-operations.e2e.ts`)

Inline reorder: click `abwab-tree-order-<id>`, type into `abwab-tree-order-input-<id>`, Enter
commits / Escape reverts. Single move through `abwab-side-panel-op-move` →
`abwab-move-picker-section-<id>` → `abwab-move-picker-dest-<id>` or `-dest-asmain` → `-confirm`.
Bulk: `abwab-side-panel-bulk-toggle`, tick `abwab-tree-checkbox-<id>`, assert
`abwab-side-panel-bulk-count`, then bulk move and bulk archive including
`abwab-page-bulk-archive-confirm-yes`. Context menu via right-click on `abwab-tree-row-<id>`,
asserting it offers exactly edit / add-child / move / archive **and that
`abwab-side-panel-op-relations` and `-op-protect` are absent**.

### T604 — archive and restore flow (`e2e/abwab-archive.e2e.ts`)

Archive a door via `abwab-side-panel-op-archive` → `abwab-page-archive-confirm-yes`; assert it
leaves the live tree. Toggle `abwab-page-archive-toggle`, assert `abwab-archive-view` renders it
with its hierarchy. Archive a parent **and** its child separately, then assert the child's
`abwab-archive-restore-<id>` is **disabled** and `abwab-archive-restore-hint-<id>` reads
«استرجع الأب أولًا». Restore the parent, then the child.

**The detach announcement:** create a sandbox door in a sandbox section, archive the door,
delete the section (now empty, so the delete succeeds), restore the door, and assert
`abwab-announcer` carries «استُرجع الباب خارج قسمه المحذوف». This is the one flow that proves
input 3 end-to-end. It also means this flow's own teardown has no section left to delete —
handle that in the fixture rather than letting teardown throw.

**Do not assert any restore descendant count** — there is none by design (`plan-slice-b.md` §6.4, R12).

### T605 — URL state and a11y flow (`e2e/abwab-url-and-a11y.e2e.ts`)

Each of `section`, `view`, `archive`, `door`, `card`, `q` survives a reload and Back/Forward.
Invalid values fail closed to the default (`?view=banana` → tree, `?section=0` → «كل الأبواب»,
`?door=-1` → no selection). Cards drill-down writes `card=<id>` and `abwab-cards-crumb` walks
back. Search by **alias** (not just name) filters the tree.

A11y: `abwab-tree` exposes `role="tree"`; rows are `treeitem` with `aria-level` and
`aria-expanded`; exactly **one** row is tabbable (`tabindex="0"`); ArrowDown/ArrowUp move focus
over visible rows only; **RTL — ArrowLeft expands / enters the first child, ArrowRight collapses
/ moves to the parent**; Enter selects; `Shift+F10` opens `abwab-page-context-menu`. Getting the
arrow direction backwards is R11 and is invisible to an LTR reviewer — assert it explicitly.

### T606 — the two doc amendments

Amend **both**, each by name:

1. **`e2e/README.md`** — the "Invariants" bullet at `:39-41` no longer holds unqualified.
   Replace it with the honest rule: read-only for every suite **except** the abwab flows, which
   write through a per-test sandbox section and tear down by archiving then deleting it. State
   the **residue** from §2. Add abwab to the scope paragraph at `:5-9`.
2. **`TESTING_STRATEGY.md` §6** — its precondition (*"do not add write flows to it without first
   moving it onto an isolated database"*) is **knowingly violated**. Amend §6 by name to record
   the decision, the sandbox mechanism, the residue, and the condition under which the
   precondition would be reinstated (an isolated e2e database). Do not soften the original
   sentence into vagueness — record that it was overridden and why.

**`TESTING_STRATEGY.md` §5 stays untouched** (`plan-slice-b.md` §3.2): §5 is the backend command
catalog, and Slice B adds no backend test.

### T607 — counts and READMEs

Re-measure, do not compute:

- `TESTING_STRATEGY.md` §6 frontend Vitest counts. **Post-B1 measured: 190 files / 2,122 tests /
  182.40s** (was 169 / 1,938 / ~2.9 min before Slice B).
- The e2e count. **Pre-B2 measured: 28 passed / ~57s.**
- `e2e/README.md` scope paragraph — add the abwab flows.
- The abwab feature `README.md` — add the e2e flows and the sandbox fixture to its test section.

---

## 6. Verification

```bash
cd /projects/Dashboard/App
dotnet build Backend/QuranDashboard.sln          # the backend boots --no-build
cd Frontend/quran-dashboard-ui
npm run e2e:typecheck                            # tsc over e2e/ + playwright.config.ts
npm run e2e -- e2e/abwab-structure.e2e.ts        # per-flow while iterating
npm run e2e                                      # the whole suite — the gate
npm test                                         # unchanged by this slice; must stay 190/2122
npm run build
cd ../.. && git diff --stat dev -- Backend/ 'Frontend/quran-dashboard-ui/src/app/core/api/generated/' openapi/
```

The last command MUST be empty. **No frontend behavior changes in this slice** — if a flow
cannot be written without one, report it.

**Budget.** e2e today 28 tests / ~57s with 2 workers. Estimate ~14–20 added tests and a run of
~2.5–4 min: write flows are slower than reads (each does setup, several round trips, and
teardown). If the suite passes ~6 min, say so in the completion note — that is a real cost the
strategy documents should carry.

---

## 7. Risks

**R18 — Parallel workers sharing one database.** `fullyParallel: true, workers: 2`. Every flow
must own its sandbox section and assert only within it. A single global count assertion makes
the suite order-dependent and it will fail intermittently with `retries: 0`.

**R19 — Teardown masking a real failure.** If teardown throws, Playwright reports the teardown
error and the actual assertion failure gets buried. Teardown must be best-effort.

**R20 — The detach flow deletes its own sandbox section.** T604's detach case deliberately
removes the section teardown would otherwise delete. The fixture must tolerate an
already-deleted section.

**R21 — Residue accumulates.** Every run leaves archived orphan doors permanently. Over many
runs the local dev tree's archive view fills with `e2e-sandbox-*` doors. That is accepted, but
both documents must say it, and nobody should later "fix" it with a hard delete — there is no
hard-delete route (`plan.md` §4, soft delete via `deleted_at`).

**R22 — STOP: no app changes.** A missing testid or an untestable behavior is a **finding**, not
a licence to edit `src/app/features/abwab/`. Report it; B1 is merged and reviewed as its own
commit.

---

## 8. Task-count summary

| Task | What |
|---|---|
| T601 | sandbox fixture |
| T602 | structure flow |
| T603 | operations flow |
| T604 | archive / restore / detach flow |
| T605 | URL state + a11y flow |
| T606 | the two doc amendments |
| T607 | counts and READMEs |
| **Total** | **7** — matches the `plan-slice-b.md` §9 estimate |
