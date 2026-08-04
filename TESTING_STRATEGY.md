# Testing Strategy — Quran Dashboard Workspace

## 1. Purpose and authority

This file is the single source of truth for **test selection, verification depth, test
execution tiers, slow data-pipeline triggers, and the phase / milestone / PR / release
gates** across the whole monorepo (Backend + Frontend).

When any other instruction file, skill, README, or workflow conflicts with this
strategy about *which tests to run and when*, **this strategy controls test selection**
— unless an active feature specification explicitly requires stronger verification for
its own scope. Test *quality* rules (test-guard, CODING_PRINCIPLES) and Quranic data
safety rules are unaffected and always apply.

**This document does not carry test counts.** It used to, and they drifted: the figures here
stated 1,843 backend tests and 191 files / 2,161 frontend tests while the suites actually ran
1,862 and 199 / 2,525. A count written in prose is wrong the moment the next test lands, and
nothing in this repo re-checks it — there is no CI (§8). So the rule is:

> **Never record a test count, file count, or pass total in this file, in a README, or in a
> commit message as a standing fact. Run the command and read the number.** A count belongs in
> evidence for a specific run, dated and attached to that run — never in prose that outlives it.

The commands that produce each current number are in §5 (Backend) and §6 (Frontend). Every
`dotnet test` line prints `Passed! - Failed: N, Passed: N, Skipped: N, Total: N`; `npm test`
prints `Test Files` and `Tests`. That output is the number; this file is not.

Baselines are taken on a developer machine with Docker up, `resources/import-sources/` staged,
and the canonical dump at `resources/db-dumps/quran-canonical/` present and regenerated against
this tree's migration head. A stale dump fails loud rather than skipping (§3 Tier A/C).

**The partition identity is a rule, not a number.** The no-pipeline filter, the all-pipeline
filter, and the smoke filter MUST partition the Backend suite losslessly: run all three plus the
unfiltered suite and confirm the three totals sum to the full total, with no family falling
outside all three and none counted twice. Re-verify whenever a namespace is added — a new
top-level namespace lands in the no-pipeline set by default, a new *pipeline* family must be
added to both pipeline filters, and anything under `QuranDashboard.Tests.Smoke.` must be a
genuine route-smoke test. This identity is what catches an accidental namespace collision with
the two legacy `*SmokeTests` classes that live in other families.

**A no-new-tests posture is a legitimate finding, and the way to record it is a dated evidence
line in `docs/TESTING_DEBT.md`, not a frozen count here.** Several features (`abwab-relations`,
`abwab-templates`) shipped deliberately without new tests; their uncovered areas and paying
triggers are recorded there. Note that routes catalogued **`ParityOnly`** satisfy
`SmokeCoverageParityTests` — two `[Fact]`s over the whole catalog, not a per-route theory —
without adding a dispatched case, so adding routes does not necessarily move the smoke total.

Durations quoted anywhere in this file are indicative order-of-magnitude guidance for choosing a
tier, never assertions of fact. The zero-skip property holds only when the staged canonical
resources *and* the canonical dump are present; without them the canonical families and the
data-smoke rows self-skip (§3 Tier D, §3 Tier E).

## 2. Core principles

- Verification MUST be fresh against the final code state. Evidence produced before
  the most recent code or configuration change is stale and MUST NOT close a phase,
  PR, or release gate.
- Test scope MUST match the changed scope and its risk. Running an unrelated broad
  suite is not a substitute for the focused tests that cover the change.
- Full Backend or Frontend suites are NOT automatically required after every phase.
- Slow tests are preserved, not deleted: pipeline and acceptance tests MUST remain in
  the repository and in their gates even though they run less frequently.
- Quran data-safety checks MUST NOT be silently weakened, skipped, or rescheduled out
  of their gates.
- A failed or unexpectedly skipped required test MUST NOT be counted as passing
  evidence.
- An existing test failure MUST be reported as a failure. Narrowing a filter to make a
  run pass MUST NOT be used to hide it.
- Agents MUST record the actual commands they ran and the actual outcomes they
  observed (pass/fail/skip counts), not inferred or remembered results.

## 3. Verification tiers

### Tier A — Focused development / per-phase

Run during implementation and at ordinary phase completion.

Required:

- tests added or changed by the current work;
- focused regression tests for the affected feature or subsystem;
- directly affected API, persistence, authorization, or UI contract tests;
- an affected build/compile check when the changed scope requires one (§7).

The command MUST be derived from the files and behavior actually changed. The examples
below are validated against the namespaces that exist in this tree — they are examples,
not universal commands for unrelated future features.

```bash
# Focused namespace (fastest; any area):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.Api.RateLimiting"

# Whole API slice — Access, ApiBehavior, Health, Middleware, RateLimiting (seconds):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.Api"

# One explorer read-model family (seconds):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.Quran.WordsWordTypes"

# Route smoke tier (~1 min; boots Testcontainers) — REQUIRED at Tier A when the phase touched an API
# route, a request/response contract, authentication/authorization, middleware, or binding:
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."

# Frontend words feature (~2 min):
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/features/words/**/*.spec.ts"
```

The complete Backend or Frontend suite MUST NOT be demanded for an ordinary phase.

### Tier B — Feature milestone / user-story completion

Run after a vertical slice, a substantial User Story, or a related group of phases.

Backend broad regression — the **no-pipeline** run:

```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke."
```

Coverage statement (be accurate about this): **the filter above is the definition — it is
subtractive, so it keeps every namespace under `QuranDashboard.Tests.` that no `!~` term
names.** Do not maintain a second list of kept families here; it drifts the moment a namespace
is added. To see what a run actually covers, read the `!~` terms above against
`Backend/tests/QuranDashboard.Tests/` (`grep -rn "^namespace" Backend/tests/QuranDashboard.Tests/ | sort -u`).
It excludes the pipeline namespaces **completely —
including the fast unit tests that live inside them** — and it excludes
`QuranDashboard.Tests.Smoke.`, which has its own gate below. It is a fast broad
regression tier, not full coverage; the excluded families run under the Tier A/C route gate
and the Tier D/E gates.

Frontend at a milestone:

- run all specs for the changed feature plus adjacent shared areas
  (`--include` globs);
- run the complete Frontend suite (`npm test`, ~2.9 min) when the milestone completes
  a full feature integration or touches broad shared frontend infrastructure
  (`core/`, `shared/`, routing, app shell, theming).

### Tier C — Ordinary final feature / pre-PR gate

For an ordinary feature PR whose changes do NOT trigger Tier D:

- Backend build;
- Backend Tier B no-pipeline regression;
- Frontend production build — when Frontend code changed;
- complete Frontend test suite — when Frontend code changed;
- any additional focused suites required by changed shared infrastructure.

```bash
dotnet build Backend/QuranDashboard.sln

dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke."
```

```bash
cd Frontend/quran-dashboard-ui
npm run build
npm test
```

Backend-only change → the Frontend commands are NOT required (and vice versa), unless
an API contract or shared integration risk justifies them. Do not pad the gate with
irrelevant commands.

**Route gate.** A change touching `Backend/api/` routes, request/response contracts,
authentication/authorization, middleware, or model binding REQUIRES the Smoke suite
(`QuranDashboard.Tests.Smoke.`), in addition to the `Tests.Api.*` families and the focused
tests for the changed endpoints:

```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."
```

It boots the real API composition (`WebApplicationFactory<HealthController>` under
`ASPNETCORE_ENVIRONMENT=Testing`, Testcontainers PostgreSQL) and drives every registered
route through routing, authorization, model binding, and serialization; a bidirectional
`EndpointDataSource` parity gate fails by name when a registered route has no
`SmokeRouteCatalog` entry, or a catalog entry no longer matches a route.

The evidence MUST state **whether the data tier ran or skipped**. `Tests.Smoke.Data`
restores the canonical Quran dump from `resources/db-dumps/quran-canonical/` and self-skips
when that dump is absent, so the tier has two legitimate totals: the full one with the dump
staged, and that total minus the `QuranDashboard.Tests.Smoke.Data` tests without it. Read both
from the run; do not carry either here.

A present dump that is corrupt or stale — sha256 or migration-head mismatch — fails loud rather
than skipping. This has bitten repeatedly: `AddAbwabGlobalOrderValue` and then
`AddAbwabDoorRelations` each moved the migration head and the dump had to be regenerated
(`Backend/scripts/create-smoke-dump --yes`) before any smoke run counted as evidence.
**Any migration invalidates the dump — regenerate it in the same change, never at the next
run's expense.**

Evidence must be qualified: "N passed, 0 skipped, data tier ran" and "N passed, data tier
skipped" are both valid; an unqualified "smoke passed" is not, and neither is a bare number
with no statement of which of the two it is.

### Tier D — Slow pipeline / canonical acceptance (trigger-based)

The ten pipeline families:

```text
Quran.Import            Quran.WordsDisplay      Quran.WordsMorphology
Quran.WordsMorphologyEnriched                   Quran.WordsSimpleI3rab
Quran.Mutashabihat      Quran.Navigation        Quran.Tafsirs
Quran.Translations      Quran.FullI3rab
```

Tier D is REQUIRED when the change touches any of:

- Quran `DataPipelines` code (importers, generation, rebuild);
- importer/data-generation tools (`tools/QuranDashboard.DataImporter`);
- pipeline readers, validators, assemblers, writers, report writers, or
  refusal/force/re-run behavior;
- canonical source packages under `resources/import-sources/`;
- morphology or enriched-morphology artifacts;
- pipeline-specific entities, EF mappings, tables, migrations, or database contracts;
- shared model-wide DbContext configuration, conventions, interceptors,
  transactions, migrations, or persistence infrastructure that can affect Quran
  pipeline tables or execution;
- PostgreSQL bulk-copy or transaction infrastructure the pipelines use;
- package/framework upgrades capable of affecting these paths (EF Core, Npgsql,
  Testcontainers, .NET runtime).

Rules:

- When only one pipeline is affected, run its focused family first
  (`--filter "FullyQualifiedName~.Quran.Translations."`).
- Before merging a pipeline-triggered PR, run either the **full Backend suite** or
  the **no-pipeline run plus every affected pipeline family**, according to actual
  risk (shared persistence changes → full suite).
- Tier D MUST NOT be demanded for an unrelated API, authentication, caching, or
  frontend-only change.
- Adding or changing entities, DbSets, mappings, or migrations that are isolated from
  Quran pipeline tables and shared persistence behavior does not trigger Tier D.
- Canonical-source tests self-skip when `resources/import-sources/` is absent. A Tier D
  run whose required canonical tests skipped is incomplete — run it where the staged
  resources exist.

### Tier E — Release / staged canonical gate

Run on a machine where `resources/import-sources/` (including the enriched
morphology artifact) is staged:

- full Backend suite;
- full Frontend suite;
- Backend + Frontend production builds;
- canonical-source acceptance (Quran.Import canonical tests, WordsDisplay
  real-import tests) and enriched-artifact acceptance when applicable;
- the Smoke suite **with the canonical dump staged** at
  `resources/db-dumps/quran-canonical/` — zero unexplained skips, exactly as for the
  canonical families. A release run whose data-smoke rows skipped is incomplete: stage the
  dump (`Backend/scripts/create-smoke-dump`) and re-run.

A browser E2E layer exists (`Frontend/quran-dashboard-ui/playwright.config.ts` + `e2e/`,
chromium only, `npm run e2e`). It is an **opt-in local gate, not part of any required tier**:
it is not required for Tier C and not required for this release gate, which remains the full
Backend and Frontend suites plus both production builds. Promoting it into a required tier is a
separate decision, to be made only after it has proven stable across several runs. An E2E run
MAY be reported as supplementary evidence, and MUST then state that it is supplementary.

The release gate MUST verify that required canonical tests did not silently skip:
check the run summary's `Skipped:` count and account for every skipped test. **A
command that exits 0 while required canonical tests were skipped (e.g. because
`resources/import-sources/` was absent) is NOT valid release evidence.** No automated
pipeline provides this gate — see §8.

## 4. Change-to-tier decision matrix

| Change type | Minimum phase tier | Pre-PR tier | Pipeline tests required? |
| --- | --- | --- | --- |
| API Backend only (`Tests.Api.*`), touching no route, contract, auth, middleware, or binding | A | C | No |
| Frontend feature only (any one directory under `src/app/features/`) | A | C | No |
| Shared API/auth infrastructure | A + adjacent `Api.Access` / `Api.Middleware` tests + Smoke | C + Smoke | Only if pipeline execution paths are affected |
| Explorer/read-model change (MushafReader, Words*) | Focused explorer family | B/C | No, unless shared pipeline persistence changed |
| EF migration affecting only non-pipeline tables | A + affected migration/schema tests | C | No |
| EF migration affecting Quran pipeline tables | Affected pipeline families | C + D | Yes |
| Importer/DataPipeline code change | Focused pipeline family | D (+ C for the rest) | Yes |
| Canonical resource/artifact change | Relevant acceptance family | D/E | Yes |
| Model-wide `QuranDashboardDbContext` / shared persistence change that can affect pipeline tables or execution | B | C + D | Yes |
| API endpoint added/changed, or auth/middleware/binding/contract change | A + `Tests.Api.*` + Smoke | C + `Tests.Api.*` + Smoke | No. State whether the data tier ran or skipped (§3 Tier A/C) |
| Release candidate (`dev → main`) | — | E | Yes (staged resources, zero unexplained skips) |
| Frontend routing, app shell, or a public browse surface (optional extra confidence) | A | C (E2E optional, never a blocker) | No |

## 5. Backend command catalog (validated)

All filters use **dot-bounded** namespace substrings. Naked substrings overlap:
`Quran.Words` also matches `Quran.WordsDisplay`; `Quran.WordsMorphology` also matches
`Quran.WordsMorphologyEnriched` and `Quran.WordsMorphologyExplorers`. Always keep the
leading and trailing dots as written; the enriched family MUST be listed explicitly.

The same dot-bounding rule applies to the smoke namespace: the filter term MUST be
`QuranDashboard.Tests.Smoke.` with the trailing dot. Two legacy classes named `*SmokeTests`
(`Quran.WordsWordTypes.WordTypesFixtureSmokeTests` and
`Quran.WordsMorphologyExplorers.MorphologyExplorersFixtureSmokeTests`) belong to their own
families; a naked `Smoke` substring would sweep them into the wrong tier.

The namespaces that exist in this tree:

```text
Tests.Abwab               Tests.Api.Access          Tests.Api.ApiBehavior
Tests.Api.Health          Tests.Api.Middleware      Tests.Api.RateLimiting
Tests.TestSupport.Logging Tests.Quran.FullI3rab     Tests.Quran.Import
Tests.Quran.MushafReader  Tests.Quran.Mutashabihat  Tests.Quran.Navigation
Tests.Quran.Tafsirs       Tests.Quran.Translations  Tests.Quran.Words
Tests.Quran.WordsDisplay  Tests.Quran.WordsMorphology
Tests.Quran.WordsMorphologyEnriched                 Tests.Quran.WordsMorphologyExplorers
Tests.Quran.WordsRoots    Tests.Quran.WordsSimpleI3rab
Tests.Quran.WordsWordTypes
Tests.Smoke               Tests.Smoke.Data
```

```bash
# Focused namespace (any area):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api.RateLimiting"

# Whole API slice (seconds):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"

# Broad no-pipeline regression (~30 s; excludes the ten pipeline namespaces
# and the smoke namespace entirely):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke."

# Route smoke tier (~1 min; boots the real API composition over Testcontainers):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."

# Full Backend suite (several minutes):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build

# One pipeline family (example — Translations):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~.Quran.Translations."

# All ten pipeline families (several minutes):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~.Quran.Import.|FullyQualifiedName~.Quran.WordsDisplay.|FullyQualifiedName~.Quran.WordsMorphology.|FullyQualifiedName~.Quran.WordsMorphologyEnriched.|FullyQualifiedName~.Quran.WordsSimpleI3rab.|FullyQualifiedName~.Quran.Mutashabihat.|FullyQualifiedName~.Quran.Navigation.|FullyQualifiedName~.Quran.Tafsirs.|FullyQualifiedName~.Quran.Translations.|FullyQualifiedName~.Quran.FullI3rab."
```

`--no-build` requires a preceding `dotnet build Backend/QuranDashboard.sln` for the
current code state (§7).

**Partition check — run it, do not read it here.** The no-pipeline filter, the all-pipeline
filter, and the smoke filter MUST partition the suite losslessly: their three totals sum to the
unfiltered total, no family falls outside all three, none is counted twice. Verify by running
all four and summing; a remainder term may stand in for the pipeline run **only** when the other
three were measured against the same tree in the same session.

Re-verify whenever a namespace is added: a new top-level namespace lands in the no-pipeline set
by default, a new *pipeline* family must be added to both pipeline filters, and anything landing
under `QuranDashboard.Tests.Smoke.` must be a genuine route-smoke test — the identity is what
catches an accidental namespace collision with the two legacy `*SmokeTests` classes that live in
other families.

## 6. Frontend command catalog

The Vitest fork cap (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) is baked into the
`npm test` script and MUST be preserved; direct `ng test` invocations MUST prefix the
env vars themselves. The cap is owned by `Frontend/quran-dashboard-ui/package.json`
(the `test` script) and documented in `Frontend/quran-dashboard-ui/README.md`. Nothing
enforces it automatically — there is no CI gate (§8), so preserving it is a review
obligation.

```bash
cd Frontend/quran-dashboard-ui

# Focused spec file:
npm test -- --include="src/app/features/words/data-access/*.spec.ts"

# Focused feature glob (~2 min):
npm test -- --include="src/app/features/words/**/*.spec.ts"

# Full Frontend suite (several minutes):
npm test

# Production build (separate from tests — the test builder ignores dist/):
npm run build

# Browser E2E — opt-in, chromium only, boots both servers (see e2e/README.md).
# Two sequential Playwright projects: `default` (2 workers) then `abwab` (1 worker).
# The abwab project MUST stay at 1 worker — a Global-scope Abwab reorder resequences
# every live root and can race a second worker's write (e2e/README.md):
npm run e2e                                   # headless (both projects, the gate)
npm run e2e:headed                            # visible browser
npm run e2e:ui                                # Playwright UI mode
npx playwright test e2e/mushaf-reader.e2e.ts  # one flow file, any worker count
```

A frontend feature is one directory under `src/app/features/` — `ls src/app/features/` is the
list, and a Tier A glob is `src/app/features/<name>/**/*.spec.ts`. Shared code lives in
`src/app/core/` and `src/app/shared/`, with app-shell specs at `src/app/*.spec.ts`.

The E2E suite boots the Angular dev server **and** the backend `https` launch profile
(`ASPNETCORE_ENVIRONMENT=Development`), so it reads the real local `quran_dashboard` database.
Every flow is read-only and every count assertion is loose, **with one named, deliberate
exception**: the eight Abwab specs (`abwab-structure.e2e.ts`, `abwab-operations.e2e.ts`,
`abwab-archive.e2e.ts`, `abwab-url-and-a11y.e2e.ts` added in Slice B2,
plus `abwab-global-order.e2e.ts` added by
`abwab-global-order`, `abwab-tree-row-budget.e2e.ts`, `abwab-slice-j-widths.e2e.ts`, and
`abwab-relations.e2e.ts` added by slice K) write against the local dev DB through a per-test sandbox section created
over the API (`e2e/fixtures/abwab.ts`), not the seeded/canonical data.
**This knowingly overrides the precondition above** — it does not move the suite onto an
isolated database first, because no such database exists yet for this suite. The sandbox is the
mitigation: each test's section name embeds the worker index and a timestamp so parallel workers
never collide, no test asserts a global count (only ids its own sandbox produced), and teardown
archives **every live door in the sandbox section**
— swept from the tree by `sectionId`, since flows create doors through the UI too and those ids
were never handed out by the fixture — and then deletes the now-empty section. That order is
forced, not stylistic: section delete `409`s while live doors remain. Each archive re-reads the
door's current version first, because every write resequences the scope and bumps its siblings'
`xmin`; archiving from one up-front snapshot succeeds once and then `409`s silently for the rest,
which is what used to leave live sandbox doors and undeleted sandbox sections behind. Teardown is
best-effort, so a flow that already broke does not get a second, masking failure from it.
**The residue that remains is archived doors, and it is permanent, not "self-cleaning":** there is
no hard delete and no section restore in this feature, so every run leaves its sandbox doors
**archived** in the local dev DB forever, and restoring one later is refused until the user names a
live destination section, since the one it belonged to is gone. What must **not** remain after a run
is any live `e2e-sandbox-*` door or any `e2e-sandbox-*` section — either one is a teardown bug, not
accepted residue, and `GET /api/abwab/tree` is how you check. This is tolerable on a local,
disposable dev database with loose, id-scoped assertions.
**The eight Abwab specs run in their own single-worker Playwright project, not the default
2-worker one.** A `Global`-scope reorder (`abwab-global-order.e2e.ts`) resequences the whole
live-root set across the database, not just the acting test's own sandbox, so two Abwab specs in
different workers can race the same rows — measured directly: at 2 workers this produced a
wrong-result failure (not even a `409`) from another worker's teardown resequencing mid-test; at 1
worker the Abwab project passes repeatably. `e2e/README.md` carries the measured counts and their
date — it is the one place they are recorded, so do not restate a figure here. See it also for the
`playwright.config.ts` project split.
**The precondition above is reinstated** — future write flows for other features again require an
isolated e2e database first — the moment this suite runs anywhere the accumulating archived
residue is not acceptable, or the sandbox-per-test mitigation stops being sufficient (e.g. a
future flow that cannot be scoped to ids it created itself). It requires
`dotnet build Backend/QuranDashboard.sln` beforehand (the backend boots with `--no-build`) and
mkcert certificates in the frontend project root. It is **not** the backend route-smoke tier
(§3 Tier A/C, §5) and does not substitute for it: the smoke tier is a required gate for
route/contract/auth changes, the E2E layer is not.

## 7. Build requirements

- Backend compilation-affecting changes REQUIRE `dotnet build Backend/QuranDashboard.sln`
  before any `--no-build` test run against that state.
- Frontend template, routing, configuration, or bundle-affecting changes REQUIRE
  `npm run build` before final PR completion. Do not prepend `npm run build` to
  ordinary test runs — the test builder compiles its own bundle.
- Before final PR completion, every changed application MUST build.
- Builds MUST be run **after the latest fix, not before it**: a previously successful
  build is stale evidence once any relevant code or configuration changes.

## 8. Continuous integration — none in this tree

**There is no CI. `.github/workflows/` does not exist**, so no workflow runs builds,
tests, contract guards, or fork-cap assertions on push or pull request.

Consequences that every gate above depends on:

- Every tier in §3 is a **local, human-or-agent-executed** gate. Nothing verifies that
  it ran. Evidence is the recorded command output, and only that.
- No automated check blocks a PR. "CI is green" is never available as evidence here and
  MUST NOT be claimed.
- Because the only runs happen on developer machines that *do* have
  `resources/import-sources/` staged, the canonical families genuinely execute rather
  than self-skipping — the opposite of the usual CI-clone hazard. The `Skipped:`
  accounting in §3 Tier E still applies: check it, do not assume it.

If a CI workflow is added later, this section MUST be rewritten to describe what it
actually runs, and any tier that starts relying on it MUST say so explicitly.

## 9. Failure and skip handling

- Any required test failure blocks completion of the tier's gate.
- Unexpectedly skipped tests MUST be listed and explained in the evidence.
- Required canonical tests that skip because resources are missing FAIL the release
  gate (§3 Tier E).
- Filters MUST NOT be narrowed to route around a failing test. If a pre-existing
  failure is discovered, report it as such — separately from the change's own
  results — and do not absorb or hide it.
- Environment failures (Docker down, missing resources, OOM) MUST be reported
  separately from product failures, and the affected tier re-run once resolved.
- No success claim without fresh command output observed in the current session.

## 10. Responsibilities by workflow

**Implementer** — selects Tier A from the changed scope; runs focused tests during
implementation; reports exact commands and outputs; MUST NOT substitute an unrelated
broad suite for missing focused coverage.

**Implementer, route obligation** — anyone **adding or changing an API route MUST add or
update the matching `SmokeRouteCatalog` entry in the same change**, because
`SmokeCoverageParityTests` fails otherwise. The catalog is bidirectionally locked to the
live `EndpointDataSource`: a new uncatalogued route fails by name, and a catalog entry
whose route no longer exists fails by name too. This is not optional bookkeeping — it is
the reason the gate can be trusted.

**Phase orchestrator** — derives final verification from this strategy; ordinary phases
run Tier A; escalates to Tier B at milestones and to Tier D when changed paths hit §3
Tier D triggers; MUST NOT run full pipeline suites automatically for every phase.

**Reviewer** (`engineering-review`) — verifies the executed tier matches the changed
risk; MUST NOT demand full suites when this strategy accepts focused evidence; MUST
block when a Tier D trigger existed but its tests were not run; **MUST block when the
change touched an API route, contract, auth, middleware, or binding and the Smoke suite
did not run**, and when a route changed without the matching `SmokeRouteCatalog` update;
treats skipped required canonical tests as missing evidence.

**Pre-PR workflow** — applies Tier C; adds Tier D only when triggered. For route,
contract, auth, middleware, or binding changes it MUST run the Smoke suite and record its
result, stating whether the data tier ran or skipped (§3 Tier A/C).

**Release workflow** — applies Tier E and verifies canonical tests actually ran (no
unexplained skips).

## 11. Scope examples

| Change | Tier(s) | Command family |
| --- | --- | --- |
| Rate-limiting policy change (rate limiting is middleware — §3 Tier C route gate applies) | A → C | `~Tests.Api.RateLimiting` during work; `~Tests.Api` + `~QuranDashboard.Tests.Smoke.` at phase end; no-pipeline + Smoke pre-PR |
| Authentication / authorization change | A → C | `~Tests.Api.Access` + `~Tests.Api.Middleware` + `~QuranDashboard.Tests.Smoke.`; no-pipeline pre-PR |
| API route added/removed, or a request/response contract change | A → C | `~QuranDashboard.Tests.Smoke.` with the matching `SmokeRouteCatalog` entry updated in the same change, plus `~Tests.Api`; no-pipeline + Smoke pre-PR, evidence naming whether the data tier ran or skipped |
| Angular words-explorer component change | A → C | `--include="src/app/features/words/**/*.spec.ts"`; full `npm test` + `npm run build` pre-PR |
| Word-types read-model change | A → B | `~Tests.Quran.WordsWordTypes` during work; no-pipeline at milestone |
| Mushaf reader change | A → C | `~Tests.Quran.MushafReader` + `--include="src/app/features/mushaf/**/*.spec.ts"` |
| Translation importer change | D | `~.Quran.Translations.` first; no-pipeline + affected families (or full suite) pre-PR |
| Model-wide `QuranDashboardDbContext` persistence change that can affect pipeline tables or execution | B → C + D | no-pipeline first, then full Backend suite pre-PR |
| Enriched morphology artifact replacement | D/E | `~.Quran.WordsMorphologyEnriched.` on a machine with the staged artifact; verify zero skips |

## 12. Deferred optimizations

Test implementation and fixture optimizations are separate, explicitly-requested work
— not part of applying this strategy: sharing the enriched-morphology artifact load,
safely reducing repeated canonical imports, introducing test traits/categories to
replace namespace filters, and optional test consolidation. Do not perform them as a
side effect of running or reviewing tests.
