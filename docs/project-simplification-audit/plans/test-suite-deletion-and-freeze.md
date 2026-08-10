# Test Suite Deletion & Freeze — Cutover Implementation Plan

**Status:** plan only. Creating this file authorizes nothing — no deletion, no test run, no build,
no commit, no review, no Spec Kit feature. Each phase becomes executable only when the owner
authorizes it.

**Execution authority.** This plan replaces `06-test-suite-rationalization-v2.md` as the intended
execution approach for the test estate. That plan's phased migration model — E2E replacement
journeys before deletion, 48→17 backend consolidation, the `[TestGate]` attribute registry cutover —
is **abandoned**. Its P0 (constitution) and P1 (authenticated E2E capability) already landed at
`4e9de652` and are treated here as existing assets, not as work. Nothing in P2a/P2b, P3a/P3b, P4 or
P5 of that plan is continued.

**Primary context:** `TESTING_CONSTITUTION.md`,
`../reports/test-suite-rationalization-current-state.md` (the audit; measured at `0d5e5a97`).
Figures quoted below are audit-derived reference figures. **No figure in this plan is an acceptance
criterion.** Acceptance is §7 and the Phase 4 gate.

**Goal.** Cut testing cost immediately by deleting test infrastructure and tests we do not need now,
retaining only the smallest high-value safety net, then freezing automated-test growth until the
product stabilizes. This is a deletion + cleanup + freeze cutover. It is not a migration, not a
consolidation, and not a test-improvement initiative.

---

## 1. Binding policy (locked — do not relitigate during implementation)

| # | Rule |
|---|---|
| P1 | **Zero frontend `*.spec.ts`.** All 248 delete. No repair first. Vitest/jsdom/unit-test infrastructure that becomes unused is removed. |
| P2 | **E2E is an allowlist, not a suite.** Keep only already-useful critical smoke/journey files (§3.2). Everything else is deleted, not repaired. No deleted unit test is migrated to Playwright. |
| P3 | **Backend permanent tests protect only** security/authentication/authorization, critical business rules and invariants, and critical transactional/concurrency/audit invariants whose loss could corrupt real product data. |
| P4 | **Nothing is retained for** endpoint permutations, DTO/property mapping, straightforward reads, implementation details, coverage percentage, trivial validation, framework behavior, or internal refactor protection. |
| P5 | **Quran/import/schema/data gates stay, but out of the daily loop** (§3.2). They are not redesigned or consolidated — only the minimum needed to keep them runnable and discoverable after deletion. |
| P6 | **No consolidation project.** A surviving high-value test that is structurally imperfect but correct is left alone. Do not merge classes into an ideal architecture. |
| P7 | **Never repair before deleting.** Stale, failing, dark, broken, fixture-dependent, or behavior-inconsistent tests inside the deletion scope are deleted as-is. A deleted test is never required to pass first. |
| P8 | **Freeze after cutover** (§6). New automated tests require explicit owner approval. |

---

## 2. Current state (established, not re-audited)

| Area | State at `4e9de652` |
|---|---|
| Frontend unit specs | 248 `src/**/*.spec.ts` |
| Frontend unit infra | `angular.json` `architect.test` (Vitest builder + 10 named configurations), `tsconfig.spec.json`, `src/test-setup.ts`, `testing/verify-test-gates.mjs` + `testing/README.md`, 13 `test:*` npm scripts, `typecheck:spec`, devDeps `vitest` + `jsdom` |
| Frontend test-only helpers under `src/` | `core/auth/auth.testing.ts`, `features/mushaf/state/mushaf-study-source-catalog.api.mock.ts`, `features/words/data-access/testing/api-test-bed.ts` — **verified: zero non-spec consumers** |
| Frontend E2E | 18 `e2e/*.e2e.ts`; fixtures `app-test.ts`, `logto.ts`, `auth.ts`, `mushaf.ts`, `abwab.ts`; `run-backend.mjs`; two Playwright projects (`default`, `abwab` at 1 worker) |
| E2E health | 8 `abwab-*` sandbox specs still seed through **anonymous** API writes and receive `401` (`e2e/README.md`) — dark. `authenticated-smoke.e2e.ts` + `fixtures/auth.ts` are the working authenticated path. |
| Backend | one project, 351 `.cs`; `test-gates.tsv` catalogues every test class, bidirectionally parity-locked by `TestGateCatalogTests` (the audit's row count is historical — the current file is the source of truth) |
| Backend lanes | `Backend/scripts/test-backend`: `fast, access, access-db, migration, process, smoke, tier-b, canonical-data, feature, pipeline, pre-pr` — selected from the TSV's `Feature/Kind/Gate/Concerns` columns |
| Audit classification | 25 `KEEP — SECURITY` (+ `AuthorizationBoundaryTests`, added at HEAD = 26), 23 `KEEP — BUSINESS RULE`, 68 `RELEASE_OR_CHANGE_GATE`, 23 `MERGE`, 129 `DELETE` |
| Policy docs | `TESTING_CONSTITUTION.md` (authority), `TESTING_STRATEGY.md` (545 lines, transitional), `docs/TESTING_DEBT.md`, `test-guard` skill (+ `references/jest.md`, `references/frontend-test-harness-constraints.md`) |

---

## 3. The four buckets

Every test file in the repository lands in exactly one of these. The cutover's whole job is to sort
them once and then act.

### 3.1 Permanent retained tests (backend only)

Backend classes the audit named `KEEP — SECURITY` (§4.1) or `KEEP — BUSINESS RULE` (§4.2), plus any
post-audit class that Phase 1 step 2a classifies as Permanent (`AuthorizationBoundaryTests` is the
known example). Retained **as they are** — no consolidation, no rewrite, no renaming (P6). These are
the daily safety net.

The audit's `MERGE` classes are **not** a bucket here. Under P6 there is no folding step, so each
one resolves in Phase 1 to either *retain in place* (it uniquely protects a P3 concern) or *delete*
(its protection demonstrably survives elsewhere). The default is delete, but only **after** the
survival mapping in Phase 1 proves it — never by assumption and never by consolidation.

### 3.2 Release / change gates

**Backend.** Classes whose subject is Quran source data, importers, generators, rebuilders,
migrations, schema shape, catalogue integrity, or shared test-runtime contracts. Retained in full,
excluded from daily verification, fired on change of the thing they guard and before release.

**Frontend E2E allowlist — the proposed minimal surviving set (6 files):**

| File | Why it survives |
|---|---|
| `e2e/authenticated-smoke.e2e.ts` | the only proof the authenticated persona path works end to end |
| `e2e/abwab-permissions.e2e.ts` | public read allowed / write affordance absent / anonymous write `401` envelope — a security smoke, and it is green today (creates no sandbox) |
| `e2e/shell-nav.e2e.ts` | app shell + navigation smoke: the cheapest proof the app boots and routes |
| `e2e/mushaf-reader.e2e.ts` | the core reading journey (paging, deep link, surah jump, fonts) |
| `e2e/mushaf-ayah-study.e2e.ts` | the study surface journey (tafsir / translation / i3rab / similar / متشابهات) |
| `e2e/words-explorers.e2e.ts` | the explorer journey and its URL contract |

Membership requires **all three**: it is a critical journey or security smoke; it passes today
unmodified; and it is **safe with respect to persistent test state**. Precisely:

- **When the supported local E2E environment is runnable**, a candidate survives only if it passes
  **unmodified**. A candidate that runs and fails is deleted — not debugged, repaired, re-fixtured,
  re-selectored, or re-asserted, during this initiative or as a condition of it (P7).
- **When the whole supported E2E environment cannot run** for environmental reasons (certificates,
  database, Docker), retention is **provisional**, under the Phase 1 environmental rule: the
  limitation is recorded and the candidates carry forward on their stated critical-journey value.
- That environmental exception is a statement about the *environment*, never about a *test*. It is
  not permission to repair, and it never converts a candidate that actually ran and failed into a
  survivor.

**Persistent-state safety — passing is not sufficient.** A surviving candidate **must not leave
privileged, destructive, or materially contaminating persistent test state beyond its documented
teardown.** Disqualifying residue includes a standing Active Owner test account, an unreverted
privileged grant or permission assignment, and persistent sandbox or domain mutations that affect
later development. This applies especially to authenticated E2E paths, whose fixtures provision real
identities and real grants. A candidate that passes but violates this requirement is **classified
Delete** — it is not repaired, re-teardowned, or re-fixtured during this initiative.

### 3.3 Deleted

Everything else: every frontend `*.spec.ts`, every E2E file not on the confirmed allowlist (the dark
`abwab-*` sandbox specs among them), the backend `DELETE` set plus the `MERGE` classes not retained
in place, and every piece of infrastructure, helper, script, configuration, catalog row, and
document that exists only to serve them — each identified by the Phase 1 manifest, never by a count
carried over from the audit.

### 3.4 Frozen

All future automated tests, by default (§6).

---

## Phase 1 — Freeze the allowlists and reconcile against the repository

**Goal.** Produce one deletion manifest that the destructive phase executes mechanically, and prove
no currently critical Security/Business test is inside it.

**Scope.**

1. **Backend bucket derivation** over **every current `test-gates.tsv` row**:
   - *Retain (permanent)* — the 48 classes named verbatim in audit §4.1/§4.2, plus
     `AuthorizationBoundaryTests`.
   - *Retain (gate)* — derived from the catalog and class names: `Kind=Canonical`, `Kind=Migration`,
     `Concerns=Schema`, `Concerns=Execution` (shared test-runtime contracts), the import/generator/
     rebuilder/manifest/schema-shape classes inside the ten pipeline features enumerated in audit
     §4.3, plus the named `AccessMigrationPathTests`, `AccessSchemaDriftTests`,
     `PermissionCatalogueStartupSyncTests`, `AbwabSchemaTests`, `WordTypesChildCatalogueDriftTests`,
     `EmailIdentityPreflightTests`.

     **`TestGateCatalogTests` is a mandatory retained Gate — named explicitly, not left to the
     inferred `Concerns=Execution` rule.** Phase 2's per-cluster reconciliation, its end-of-phase
     parity run, and Phase 4's classification check all depend on its bidirectional source↔catalog
     parity guarantee. It survives the cutover. (`SmokeRouteCatalog` and its parity protection,
     `test-resources.tsv`, and the PostgreSQL test-runtime support are retained as already
     specified.)
   - *Delete* — the residual, which is the audit's `DELETE` set plus any `MERGE` candidate cleared
     by step 2b below.
   - Audit §4.3's cluster counts (~68 gate classes) are a **cross-check only**. A derived set that
     differs is recorded in one line and accepted; it is not investigated further.
2. **Rename/move reconciliation.** Confirm every audit-named class still exists at its named
   fully-qualified name.

   **2a. Post-audit classes.** Backend test classes added after the audit baseline (`0d5e5a97`) are
   classified **directly under the current P3/P4/P5 policy** — P3 defines permanent high-value
   protection, P4 what may not be retained permanently, P5 the release/change gates — into the same
   three buckets, Permanent / Gate / Delete. They are
   **not** automatically retained because they are new, and **not** automatically deleted because
   the audit never saw them. `AuthorizationBoundaryTests` is the known example, not the only
   permitted one — enumerate them from the current catalog, not from this plan.

   **2b. `MERGE` survival mapping.** Before any audit-`MERGE` class enters the delete list, record
   one concise line per class in the form `protected behavior -> surviving retained class/gate`.
   Nothing is redesigned, folded, or moved to make the mapping true — the surviving protection must
   already exist. If a candidate's protected behavior cannot be clearly located in the retained
   estate, **escalate to the owner and leave it retained in place**; do not delete it, and do not
   repair or restructure it to resolve the ambiguity. Once survival is proven, the default is
   delete.
3. **Safety sweep.** Every class in the delete list whose name or subject touches auth, permission,
   owner, audit, transaction, rollback, concurrency, corruption, or Quran-data integrity gets one
   line stating where that protection survives. Any that cannot be answered in one line is
   **escalated to the owner**, not silently deleted and not repaired.
4. **Frontend E2E allowlist confirmation.** Run each of the six candidates (§3.2) **unmodified**.
   - Green → allowlist.
   - Runs and fails → **delete list, immediately**. Do not debug it, repair it, re-fixture it,
     adjust its selectors, or update its assertions. This is the prior dark-test repair workflow and
     it is not repeated here (P7).
   - The supported local environment cannot run E2E at all (certificates, database, Docker) →
     record that environmental limitation plainly and carry the six candidates forward
     **provisionally** on their stated critical-journey value. An environment that cannot run the
     suite is not evidence that a test is stale. Provisional retention is resolved by the Phase 4
     rule below.
   - Passing green → also apply the **persistent-state safety** check (§3.2): a candidate that
     leaves privileged, destructive, or materially contaminating state past its documented teardown
     goes to the delete list, unrepaired.
5. **Frontend spec scope confirmation.** `find src -name '*.spec.ts'` is the whole deletion set; no
   per-file review. Confirm only that the three test-only helpers in §2 still have zero non-spec
   consumers.
6. **Permanent-set pre-deletion baseline.** Last step of Phase 1 — after the Permanent / Gate /
   Delete classification is final and every escalation is cleared, but **before any deletion**, run
   the **final Permanent retained backend set once** (`test-backend feature --class …` per class, or
   the narrowest existing selector that resolves to exactly that set).

   Purpose: establish that the Security/Authorization/critical-Business safety net being carried
   into the cutover is already green **before** anything is deleted.

   - Run **only** the Permanent retained classes. Do not run the full backend suite, and do not run
     canonical/import/schema/release gates as part of this baseline.
   - **Do not repair a failing retained test.** If any Permanent retained test fails here, **stop and
     escalate to the owner** — a red safety net is a decision point, not a task.
   - Record the result in `scratchpad/test-suite-deletion-manifest.md` or the phase execution notes.
     **No new report.**

   This is a baseline check, not a testing initiative.

**Files/areas.** `Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv`,
`Backend/tests/QuranDashboard.Tests/**`, `Frontend/quran-dashboard-ui/e2e/**`,
`Frontend/quran-dashboard-ui/src/**`.

**The manifest.** Phase 1's single output is a working execution artifact at the repository-root
path **`scratchpad/test-suite-deletion-manifest.md`**.

- It is **not a report** and not a deliverable — no narrative, no metrics, no per-class essays. It
  is the list the later phases execute and check against.
- **It is never committed or staged.** `scratchpad/` is untracked; keep it that way (a local
  `.git/info/exclude` entry is acceptable — it changes nothing in the repository).
- **The same manifest file must persist through Phases 1–4.** Phase 2 deletes from it, Phase 3 keeps
  the lane classification honest against it, and Phase 4's mechanical checks read it. **Do not
  regenerate or replace it.** Amend it in place **only when an authorized classification changes**
  (a cleared escalation, a mixed-file retention, a provisional E2E candidate that later fails), and
  **record each amendment** — what changed, from which bucket to which, and why.
- It may be deleted once the whole cutover closes successfully (after the Phase 4 gate passes).

**Deletion boundaries.** Phase 1 deletes nothing and modifies no test — the step-6 baseline runs the
Permanent set, it does not touch it.

**Safety checks.** The three retain/delete lists partition **every current `test-gates.tsv` row**
exactly — no row unclassified, none in two lists. Every `MERGE` candidate carries its survival
mapping. Every escalation is resolved by the owner before Phase 2 starts.

**Must NOT do.** No new audit document, no metrics, no per-class essays, no runtime measurement, no
repair of a failing candidate, no reclassification of a `KEEP` class into deletion because it looks
expensive.

**Acceptance.** `scratchpad/test-suite-deletion-manifest.md` exists, is untracked, and lists:
backend classes to retain (permanent), backend classes to retain (gate), backend classes to delete
(each former `MERGE` candidate carrying its survival mapping), E2E files to retain, E2E files to
delete, and the frontend spec glob. Owner has cleared every escalation. **The Permanent-set
pre-deletion baseline (step 6) has run and is green, with its result recorded** — or the owner has
explicitly resolved a failure before Phase 2 opens.

**Dependencies.** None.

---

## Phase 2 — Hard deletion

**Goal.** Remove the estate in one destructive pass, driven by
`scratchpad/test-suite-deletion-manifest.md`, without repairing anything. Any class that an
owner-resolved escalation moves out of the delete list during this phase (a non-separable mixed
file, an unmappable `MERGE` candidate) is amended into the manifest with an explicit Permanent or
Gate classification before execution resumes, so Phases 3 and 4 check against the truth.

**Scope.**

*Frontend*
- Delete all `src/**/*.spec.ts` (248).
- Delete `src/test-setup.ts`, `src/app/core/auth/auth.testing.ts`,
  `src/app/features/mushaf/state/mushaf-study-source-catalog.api.mock.ts`,
  `src/app/features/words/data-access/testing/` (consumers verified zero in Phase 1).
- **Delete every E2E file classified as Delete in the Phase 1 manifest.** The manifest is the
  criterion; historical E2E counts are not.
- **Then**, with the final allowlist known, repository-search **every** E2E fixture and helper under
  `e2e/` (`fixtures/*.ts`, `run-backend.mjs`, and anything else there) and delete only those with
  **zero surviving consumers**. No fixture is deleted because it was historically associated with
  deleted tests — `e2e/fixtures/abwab.ts` included.

*Backend*
- Delete each manifest class file **together with its `test-gates.tsv` row, in the same change.**
  The catalog is bidirectionally parity-locked by `TestGateCatalogTests`; one without the other
  fails the suite by name.
- Delete fixtures, seeds, collection definitions, interceptors, and `TestSupport` helpers that lose
  **all** consumers as a result — each verified by a repository-wide symbol search before removal,
  never by assumption.
- Leave `SmokeRouteCatalog` and its parity lock intact. Leave `test-resources.tsv` and the
  PostgreSQL runtime support intact. **`TestGateCatalogTests` is retained as a Gate and must remain
  runnable throughout this phase** — the per-cluster reconciliation and the end-of-phase parity run
  both depend on it.

**Deletion boundaries.** Do not touch production code. Do not touch retained test classes — not to
consolidate, rename, reformat, or "tidy while here" (P6). Do not touch `Backend/scripts/test-backend`
in this phase beyond nothing; lane behavior changes belong to Phase 3.

**Mixed files — the manifest is class-based, deletion is often file-based.** If a `.cs` file
contains both a retained class and a deletion-target class:
- **Never delete the file whole.**
- **Separable** — the deletion-target class can be removed without changing the retained class's
  setup, fixtures, collection behavior, fields, execution context, or semantics → remove only that
  class and continue.
- **Not separable** — separation is non-trivial or risks disturbing the retained class → **stop the
  phase and escalate to the owner.** Do **not** keep both classes and carry on: Phase 2 may not
  continue with an unclassified extra class sitting in the tree.
- After the owner resolves the case, **amend `scratchpad/test-suite-deletion-manifest.md` so every
  surviving class is explicitly classified Permanent or Gate**, then resume execution.
- The same rule governs shared fixture files: a file with any surviving consumer stays.

**The invariant this protects: every surviving backend test class belongs explicitly to Permanent or
Gate.** "Retained because it was awkward to remove" is not a bucket.

**Execution and commit shape.** Work through the manifest **area cluster by area cluster in the
working tree** — that keeps each step small and self-reviewable. Do not commit per cluster. The
phase produces **exactly one phase-scoped commit**, created only after the phase review passes.

**Safety checks (run in the working tree, not per commit).**

*Per area cluster — mechanical only, no test process:*
- Source/catalog reconciliation: the class names remaining in the source tree and the class names
  remaining in `test-gates.tsv` still agree, in both directions.
- No mixed retained file was deleted whole.
- No retained-list class entered the deletion diff (diff-vs-manifest check).

*Once, after all Phase 2 deletions are complete and before the focused phase review:*
- Run the existing `TestGateCatalogTests` — the real bidirectional parity gate. This requirement is
  not weakened; it is executed once at the end instead of after every cluster.

**Must NOT do.** Do not repair, un-skip, re-fixture, or make green anything scheduled for deletion.
Do not require the deleted suite to pass before deleting. Do not delete a shared helper whose
consumers were not verified. Do not migrate deleted coverage into Playwright.

**Acceptance.** `find src -name '*.spec.ts'` returns 0. `e2e/` contains exactly the allowlist.
`test-gates.tsv` contains exactly the retained permanent + gate classes, and **every surviving
backend class carries an explicit Permanent or Gate classification in the manifest** — no class
survives unclassified, including any kept through a mixed-file escalation. The backend solution
compiles and the end-of-phase `TestGateCatalogTests` run is green. One phase-scoped commit exists,
made after the phase review. No further verification claim is made at this point — the repository is
knowingly mid-cutover until Phase 3 lands.

**Dependencies.** Phase 1 complete, every escalation cleared, and the Permanent-set pre-deletion
baseline green and recorded.

---

## Phase 3 — Cleanup and Test Freeze

**Goal.** Remove everything the deletion orphaned, keep the retained gates discoverable and
runnable, and put the freeze in force.

**Scope — frontend configuration and scripts.**
- `angular.json` — delete the whole `architect.test` target and its 10 configurations.
- Delete `tsconfig.spec.json` and the entire `testing/` folder (`verify-test-gates.mjs`,
  `README.md`).
- `package.json` — delete `test`, `test:fast`, the six `test:feature:*`, `test:authorization`,
  `test:composition`, `test:shared`, `test:full`, `test:gates`, `typecheck:spec`. Rewrite
  `typecheck` to `typecheck:app` only. Rewrite `test:pre-pr` to
  `check:permission-catalogue && check:audit-action-types && check:golden-ui && check:no-unit-specs && typecheck && build:verify`.
- `package.json` devDependencies — remove packages used **only** by the retired unit-test system.
  `vitest` and `jsdom` are the known removals; check the whole `package.json` rather than stopping
  there. A
  package is kept if Playwright, production code, the Angular build, `typecheck:app`, formatting,
  API generation, or docs generation still uses it (`@playwright/test`, `typescript`, `@types/node`,
  `@angular/build` are keeps). Refresh `package-lock.json`.
- **Add** `scripts/check-no-unit-specs.mjs` — fails if any `src/**/*.spec.ts` exists; wired as
  `check:no-unit-specs` beside the existing `check:*` scripts. This is the freeze's enforcement, and
  it belongs to **ordinary feature verification**, not only to `test:pre-pr`: the documented normal
  frontend chain during the freeze is

  ```bash
  npm run check:no-unit-specs
  npm run typecheck:app
  npm run build:verify
  ```

  Keep the three commands **independently meaningful**. Do **not** fold the check into
  `build:verify` — hiding it inside the build makes both commands mean less and puts a policy gate
  where a compilation gate belongs. Document this chain in `TESTING_CONSTITUTION.md` (§6),
  `Frontend/quran-dashboard-ui/README.md`, and the frontend instruction files' testing rows, so the
  zero-`*.spec.ts` policy is machine-enforced during everyday work rather than only at pre-PR.
- `playwright.config.ts` and the `e2e` script are left unchanged: both projects still resolve to a
  non-empty set (`abwab-permissions.e2e.ts` keeps the `abwab` project alive). Change them only if a
  project resolves to zero files.

**Scope — backend gate separation.** Keep the existing lane vocabulary; do not redesign (P5).
- Daily verification = `test-backend smoke` + `test-backend tier-b` (the retained permanent set).
- Change/release gates = `pipeline`, `canonical-data`, `migration`, `access-db`; `pre-pr` is the
  release gate that runs everything.
- **Catalog corrections are permitted in both directions**, and only as the smallest classification
  edit that fixes the selection:
  - *Gate class still inside the daily union* — a retained release/change-gate class selected by
    `smoke`/`tier-b`: move it out by editing its `test-gates.tsv` `Gate`/`Concerns` value. If and
    only if no existing lane then selects it, add exactly one lane (`gate-schema`, selecting
    `Concerns=Schema`) to `Backend/scripts/test-backend`. That is the entire permitted script change.
  - *Permanent class outside the daily union* — a retained Security/Business class **not** selected
    by `smoke` ∪ `tier-b`: move it in by editing its `test-gates.tsv` `Gate`/`Concerns` value so an
    **existing** daily lane selects it. **No new daily lane is added** for this, ever.
- In both directions: do not redesign the lane system, do not change the class's protected behavior,
  do not rename or restructure the class, and make no classification edit larger than the one the
  selection requires. No changed-path mapping, no flags, no registry redesign.

**Scope — documentation and instructions.** Every live file whose described truth the deletion
falsified:
- `TESTING_CONSTITUTION.md` — add the freeze clause (§6). It stays the sole policy authority.
- **Delete** `TESTING_STRATEGY.md`; its surviving operational truth moves to
  `Backend/tests/QuranDashboard.Tests/README.md` (lanes, fixtures, gates) and `e2e/README.md`
  (journeys, prerequisites, invariants). Repoint every **live** reference (root
  `SKILLS_AND_ARCHITECTURE_GUIDE.md`, `docs/README.md`, `Backend/CLAUDE.md`, `Backend/AGENTS.md`,
  `Backend/scripts/README.md`, `Backend/README.md` chain, `Backend/api/**/README.md`,
  `Backend/tests/**/README.md`, `Frontend/quran-dashboard-ui/README.md`, `e2e/README.md`,
  `src/app/features/abwab/README.md`, and the surviving E2E file that names it). Planning and audit
  artifacts under `docs/project-simplification-audit/` are history and are left alone.
- **Delete** `docs/TESTING_DEBT.md`. Under a freeze, "coverage we chose not to write" is the default
  state, not tracked debt.
- Feature/area READMEs that describe unit-spec lanes or spec-owned behavior:
  `Frontend/quran-dashboard-ui/README.md`, `src/app/features/{mushaf,abwab,words,access-admin}/README.md`,
  `.architecture/UI_STYLE_SYSTEM.md`, `Backend/tests/QuranDashboard.Tests/README.md`,
  `Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/README.md`, `Backend/scripts/README.md`,
  `e2e/README.md` (drop the dark-Abwab-sandbox narrative and the deleted flows).
- Instruction files — confirm the testing rows in root/`Backend`/`Frontend` `CLAUDE.md` and
  `AGENTS.md` route to `TESTING_CONSTITUTION.md` in one sentence and restate nothing; correct any
  row that still implies a frontend unit lane.
- Skills — `.claude/skills/test-guard/SKILL.md` and `.agents/skills/test-guard/SKILL.md` scoped to
  backend + Playwright; **delete** `references/jest.md` and
  `references/frontend-test-harness-constraints.md`. Repoint the testing references in
  `engineering-review` (+ `SPEC_KIT_IMPLEMENTATION_REVIEW.md`), `focused-review`,
  `performance-backend-review`, `deploy-smoke`, `pr-context-prep`, `speckit-analyze` (both agent
  trees), `speckit-converge`.

  **Preserve the canonical skill structure while doing so.** `.claude/skills` holds the canonical
  full skill bodies; `.agents/skills` holds pointers/adapters wherever that convention already
  applies there. Update each side in its own idiom — edit the body in `.claude`, edit the pointer in
  `.agents`. **Do not copy a full skill body into `.agents/skills`**, and do not invert or flatten
  the two trees while correcting testing references.

**Deletion boundaries.** Do not delete a shared helper, script, config, or package whose remaining
consumers were not verified. Do not rewrite documents beyond the statements the deletion falsified.

**Safety checks — structural, not raw-text.** A broad grep for `vitest` / `jsdom` / `spec.ts` /
`TESTING_STRATEGY.md` produces legitimate matches (the freeze check *must* mention `spec.ts`; the
constitution *may*; the lockfile *may* carry transitive entries), so the gate is these explicit
checks instead:

*Frontend structure*
- `find src -name '*.spec.ts'` returns 0.
- `angular.json` has no unit-test target (no `architect.test`, no test configurations).
- `package.json` has no direct `vitest` or `jsdom` dependency, and no script or config key
  referencing a deleted unit-test lane, target, or tsconfig.
- `tsconfig.spec.json`, `src/test-setup.ts`, `testing/`, and the retired test-only helpers are
  absent.
- No live script, config, or document points at a deleted unit-test path or lane.
- A surviving reference to `*.spec.ts` is acceptable **only** where it intentionally enforces or
  documents the prohibition — `scripts/check-no-unit-specs.mjs`, `TESTING_CONSTITUTION.md`, and the
  documented normal-verification chain.
- `check:no-unit-specs` exists as its own script, appears in the documented normal frontend
  verification chain, and is **not** embedded inside `build:verify`.

*Dependencies*
- Direct ownership is what must disappear. `vitest` or `jsdom` **may remain transitively** in
  `package-lock.json` if retained tooling pulls them in; that is not a failure.
- Do not force unrelated dependency changes, overrides, or upgrades to evict a transitive package.

*Documentation*
- No **live** reference to the deleted `TESTING_STRATEGY.md` or `docs/TESTING_DEBT.md` remains in
  instruction files, READMEs, scripts, configs, or Skills.
- Historical references inside `docs/project-simplification-audit/` artifacts are **allowed** and
  are left alone.

**Must NOT do.** Do not introduce CI. Do not build a changed-path trigger engine, a gate registry,
or a `[TestGate]` attribute migration. Do not set a coverage target anywhere.

**Acceptance.** Every structural check above passes, in its stated form — direct dependency
ownership gone (transitive lockfile entries permitted), `*.spec.ts` mentioned only by the
prohibition's own enforcement and documentation, live references to the deleted policy documents
gone while audit-history references stay. `check:no-unit-specs` fails on a planted spec and passes
clean, and the three-command normal verification chain is documented in `TESTING_CONSTITUTION.md`,
the frontend README, and the frontend instruction rows. Retained gates are named and runnable from a
documented command. `TESTING_CONSTITUTION.md` carries the freeze.

**Dependencies.** Phase 2 complete.

---

## Phase 4 — Final verification gate

**Goal.** One short, explicit gate — not a re-audit.

**Scope.** Run, in order, and record the output:

1. `dotnet build Backend/QuranDashboard.sln` — repository builds.
2. `npm run typecheck:app` and `npm run build:verify` — frontend typecheck/build succeed.
3. `Backend/scripts/test-backend smoke --build` and `Backend/scripts/test-backend tier-b --no-build`
   — the surviving permanent tests pass.
4. `npm run e2e` — the surviving allowlist passes, if runnable in the supported local environment.
   If the environment cannot run it (certs, database, Docker), record that plainly as not-run; do
   not repair, and do not substitute another claim.

   **Provisional-retention resolution.** If Phase 1 could not run E2E and the environment becomes
   runnable here for the first time, this step is where provisional retention is settled. A
   provisionally retained candidate that now runs and fails — or that passes but violates the
   persistent-state safety requirement (§3.2) — is handled as:
   - do **not** debug, repair, re-fixture, or re-assert it;
   - classify it **Delete** and amend `scratchpad/test-suite-deletion-manifest.md` in place,
     recording the amendment;
   - perform **only** the Phase 2 deletion and Phase 3 cleanup steps that file requires — the file
     itself, plus any fixture, config, or documentation reference it newly orphans (fixtures still
     verified by consumer search);
   - rerun this Phase 4 gate.

   **Do not restart the initiative**, do not reopen Phases 1–3 in full, and do not repair the test.
5. Retained release/change gates are discoverable and runnable: each gate lane resolves to a
   non-empty class set and its command is documented.
6. **Mechanical lane classification check.** Using `test-backend <lane> --list-tests --no-build`
   selections against the final `test-gates.tsv` and `scratchpad/test-suite-deletion-manifest.md`
   (the Phase 1 manifest, amended in Phase 2), prove all four:
   - every permanent retained class **is** selected by the daily retained-test union
     (`smoke` ∪ `tier-b`);
   - **no** release/change-gate class is selected by that daily union;
   - every retained gate class **is** selected by at least one documented change/release lane;
   - **no** retained class is unreachable from all intended lanes — the union of the daily lanes and
     every documented gate lane covers the whole catalog.

   A failure here is fixed by the smallest correction to the offending row's `Gate`/`Concerns`
   value — in either direction, per Phase 3's bidirectional allowance. It is not fixed by
   redesigning the lane system, adding a daily lane, adding any lane beyond Phase 3's single
   permitted `gate-schema`, changing a class's protected behavior, or deleting a retained class.
7. **Structural reference check** — Phase 3's explicit frontend-structure, dependency, and
   documentation checks, re-run as written. Transitive `vitest`/`jsdom` lockfile entries and
   `docs/project-simplification-audit/` history are expected matches, not failures.
8. `git diff --check` is clean.

**Deletion boundaries.** None — this phase deletes nothing.

**Safety checks.** A failure in 1–3 is a real regression and is fixed before closing. A failure in a
*deleted* test is impossible by construction and is never a gate. A pre-existing environmental
flake (e.g. the audit's `inotify` exhaustion, §3.3) is recorded as environmental, not repaired here.

**Must NOT do.** Do not run the deleted suites. Do not require deleted tests to pass. Do not add a
test to make a gate pass. Do not expand scope into fixing unrelated red.

**Acceptance.** All eight verification steps recorded with real output; steps 1–3 and 5–8 green;
step 4 green or explicitly recorded as not-runnable-locally. If step 4's provisional-retention
resolution deleted a file, the gate is rerun and the rerun is the recorded result.

**Dependencies.** Phase 3 complete.

---

## 6. The Test Freeze (in force from Phase 3)

Added to `TESTING_CONSTITUTION.md` as its operative default:

- **Default for future features: no new automated tests.** No test-per-component, per-service,
  per-endpoint, or per-DTO. No coverage target — coverage is not measured and may not be cited.
- **Normal feature verification** is: backend build; the frontend chain
  `npm run check:no-unit-specs` → `npm run typecheck:app` → `npm run build:verify`, run as three
  independent commands; targeted runtime / manual / browser smoke where appropriate; engineering
  review. The freeze check runs in ordinary verification, not only at pre-PR, and is never folded
  into `build:verify`.
- **Retained permanent tests may be minimally updated** only when a change intentionally alters a
  protected Security or critical Business invariant. Updating them to accommodate a refactor is a
  signal the test was defective, not a reason to grow it.
- **A retained release/change gate may be minimally updated** when the exact source, schema,
  catalogue, importer, generator, rebuilder, migration, or canonical-data contract it protects is
  intentionally changed. The update may only reflect that approved contract change. It must not
  broaden coverage, add new test classes or files, or turn the gate into a general permanent test
  suite. This exception does not permit new general tests, test-suite expansion, migration of
  deleted coverage, or coverage-driven testing.
- **Creating any new automated test — backend class, backend method, or E2E file — requires explicit
  owner approval**, recorded in the change's `Testing Decision`. The frontend `*.spec.ts` prohibition
  is absolute and machine-enforced by `check:no-unit-specs`.
- **Gates fire on their own trigger:** Quran data, importers, generators, rebuilders, migrations,
  schema, and canonical datasets run their retained gate before release or when that subject changes.
- **Lifting the freeze is a separate future initiative — "Testing Foundation V2"** — to be opened
  after the product and its major behaviors stabilize. It is out of scope here, and no work in this
  plan may be shaped around anticipating it.

---

## 7. Acceptance criteria (whole cutover)

1. Zero `src/**/*.spec.ts`; `check:no-unit-specs` fails if one is added, exists as its own command,
   and is part of the documented normal frontend verification chain (`check:no-unit-specs` →
   `typecheck:app` → `build:verify`) rather than only `test:pre-pr` or hidden inside `build:verify`.
2. No unit-test target, config, setup file, or lane script remains; no **direct** `vitest`/`jsdom`
   dependency remains (transitive lockfile entries are permitted); nothing live points at a deleted
   path — verified by Phase 3's structural checks, not by a broad grep.
3. `e2e/` contains exactly the confirmed allowlist plus its live fixtures; no deleted E2E file was
   repaired on its way out.
4. `test-gates.tsv` contains exactly the retained permanent + gate classes, parity-locked and green;
   `TestGateCatalogTests` survives as a retained Gate; every surviving backend class is explicitly
   classified Permanent or Gate; the Phase 4 lane classification check passes on all four of its
   conditions.
5. Every audit-named `KEEP — SECURITY` and `KEEP — BUSINESS RULE` behavior is still covered by an
   existing class, retained in place and unconsolidated; every deleted `MERGE` candidate has a
   recorded `protected behavior -> surviving retained class/gate` mapping.
6. No **permanent retained** backend test exists solely for endpoint permutations, DTO/property
   mapping, straightforward reads, implementation details, trivial validation, framework behavior,
   or refactor protection. This criterion governs the daily permanent set only: a release/change
   gate legitimately retains schema, import, catalogue, and data-integrity coverage that would not
   qualify as a permanent daily test, and **no Quran/import/schema gate is weakened, thinned, or
   deleted on the strength of this wording**.
7. Release/change gates exist, are separated from daily verification, and are runnable by a
   documented command.
8. `TESTING_CONSTITUTION.md` carries the freeze; every live instruction file, README, script, and
   Skill routes to it and restates nothing.
9. The Phase 4 gate is recorded green, with **Phase 4 verification step 4** permitted to be
   explicitly recorded as not-runnable-locally.

---

## 8. Stop conditions

Stop and report to the owner rather than proceeding if:

- A class in the deletion list protects a Security or critical Business invariant with no answer to
  "where does this survive" — escalate; do not delete, and do not repair.
- An audit-`MERGE` candidate's protected behavior cannot be mapped to a surviving retained class or
  gate — escalate and leave it retained in place.
- A `.cs` file holds both a retained and a deletion-target class and the two cannot be separated
  without risk to the retained one — **stop Phase 2 and escalate.** Do not keep both and continue.
  Execution resumes only after the owner resolves it and the manifest classifies every surviving
  class as Permanent or Gate.
- A Permanent retained backend test fails the Phase 1 pre-deletion baseline — stop and escalate; do
  not repair it, and do not begin deletion against a red safety net.
- Deleting a shared helper, fixture, or package would break a consumer outside the deleted estate.
- A retained gate cannot be made runnable after deletion without redesigning the lane machinery
  beyond the single permitted lane addition (§Phase 3).
- The backend build cannot be made green by deletion alone — i.e. production code would have to
  change.
- The reconciliation manifest cannot partition the catalog exactly.

---

## 9. Explicit non-goals

- No test migration, no replacement E2E authoring, no repair of dark or stale tests.
- No consolidation, renaming, or restructuring of surviving tests.
- No gate redesign, changed-path trigger engine, or `[TestGate]` registry migration.
- No coverage measurement or target, before or after.
- No CI introduction.
- No new audit or report artifact — Phase 1's output is a working manifest, not a document.
- Testing Foundation V2 is out of scope.
