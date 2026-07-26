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

Baseline measurements referenced below were taken 2026-07-25 on a developer machine
(2,080 Backend tests / 2,080 Frontend tests, all green). Durations are indicative, not
contractual.

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

The command MUST be derived from the files and behavior actually changed. The
examples below are the validated commands for the current Abwab feature (030) — they
are examples, not universal commands for unrelated future features.

```bash
# Focused namespace (seconds):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab.Relationships"

# Broader Abwab phase (~35 s, ~483 tests: ~414 Abwab + 69 Api):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "(FullyQualifiedName~QuranDashboard.Tests.Abwab)|(FullyQualifiedName~QuranDashboard.Tests.Api)"

# Frontend Abwab (~40 s):
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/features/abwab/**/*.spec.ts"
```

The complete Backend or Frontend suite MUST NOT be demanded for an ordinary phase.

### Tier B — Feature milestone / user-story completion

Run after a vertical slice, a substantial User Story, or a related group of phases.

Backend broad regression — the **no-pipeline** run (~45 s, 1,463 tests):

```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke"
```

Coverage statement (be accurate about this): this run keeps all Abwab, Api, and Quran
explorer/read-model tests plus every unit test *outside* the ten excluded namespaces.
It excludes those ten namespaces **completely — including the fast unit tests that
live inside them** (~617 tests total). It also excludes the `Tests.Smoke` namespace,
which runs as its own suite (see "Smoke suite" below). It is a fast broad regression
tier, not full coverage; the excluded families run under Tier D/E gates.

Frontend at a milestone:

- run all specs for the changed feature plus adjacent shared areas
  (`--include` globs);
- run the complete Frontend suite (`npm test`, ~3.5 min) when the milestone completes
  a full feature integration or touches broad shared frontend infrastructure
  (core/, shared/, routing, app shell, theming).

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
  --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke"
```

```bash
cd Frontend/quran-dashboard-ui
npm run build
npm test
```

Backend-only change → the Frontend commands are NOT required (and vice versa), unless
an API contract or shared integration risk justifies them. Do not pad the gate with
irrelevant commands.

### Smoke suite — real-pipeline gate (namespace `QuranDashboard.Tests.Smoke`)

Boots the REAL API composition (`WebApplicationFactory<Program>`, in-memory TestServer
plus a Kestrel-on-port sentinel, environment `Testing`) over a Testcontainers
PostgreSQL, and drives every registered route through routing, authorization, model
binding, and serialization. A reflection parity test
(`SmokeCoverageParityTests`) fails when any registered route lacks a
`SmokeRouteCatalog` entry — adding an endpoint without a smoke entry fails CI.

```bash
# Full smoke suite (~35 s pipeline-only; +~80 s data-smoke on a staged machine):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"
```

Rules:

- REQUIRED pre-PR for any change touching `Backend/api/` routes, request/response
  contracts, authentication/authorization, middleware, or model binding.
- Excluded from the Tier B/C no-pipeline filter by namespace (selection stays
  namespace-based; no traits).
- The data-smoke tier (`Tests.Smoke.Data`) restores the verified canonical Quran dump
  from `resources/db-dumps/quran-canonical/` (produced by
  `Backend/scripts/create-smoke-dump`; a derived cache of the canonical import, never
  synthetic). It self-skips when the dump is absent — including in CI — but a PRESENT
  dump that is corrupt or stale (sha256/migration mismatch) fails loud.
- CI runs the suite as a dedicated `backend-tests` step; data-smoke rows skip there.

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
- Tier D MUST NOT be demanded for an unrelated Abwab, authentication, caching, or
  frontend-only change.
- Adding or changing Abwab-only entities, DbSets, mappings, or migrations does not
  trigger Tier D when the change is isolated from Quran pipeline tables and shared
  persistence behavior.
- Canonical-source tests self-skip when `resources/import-sources/` is absent
  (including in CI). A Tier D run whose required canonical tests skipped is
  incomplete — run it where the staged resources exist.

### Tier E — Release / staged canonical gate

Run on a machine where `resources/import-sources/` (including the enriched
morphology artifact) is staged:

- full Backend suite (~5–5.5 min);
- full Frontend suite;
- Backend + Frontend production builds;
- required Playwright/E2E checks (`npm run e2e`);
- canonical-source acceptance (Quran.Import canonical tests, WordsDisplay
  real-import tests) and enriched-artifact acceptance when applicable.

The release gate MUST verify that required canonical tests did not silently skip:
check the run summary's `Skipped:` count and account for every skipped test. **A
command that exits 0 while required canonical tests were skipped (e.g. because
`resources/import-sources/` was absent) is NOT valid release evidence.** The current
CI workflow does not provide this gate because its clones do not contain the staged
canonical resources.

## 4. Change-to-tier decision matrix

| Change type | Minimum phase tier | Pre-PR tier | Pipeline tests required? |
| --- | --- | --- | --- |
| Abwab Backend only | A | C | No |
| Abwab Frontend only | A | C | No |
| Shared API/auth infrastructure | A + adjacent Api/Access/Security tests | C | Only if pipeline execution paths are affected |
| Explorer/read-model change (MushafReader, Words*) | Focused explorer tests | B/C | No, unless shared pipeline persistence changed |
| EF migration affecting only Abwab tables | A + Abwab migration/schema tests (`Abwab.Ci` schema compatibility runs in every A/B filter) | C | No |
| EF migration affecting Quran pipeline tables | Affected pipeline families | C + D | Yes |
| Importer/DataPipeline code change | Focused pipeline family | D (+ C for the rest) | Yes |
| Canonical resource/artifact change | Relevant acceptance family | D/E | Yes |
| Model-wide `QuranDashboardDbContext` / shared persistence change that can affect pipeline tables or execution | B | C + D | Yes |
| Abwab-only entities, DbSets, mappings, or migrations (isolated from pipeline tables and shared persistence behavior) | A | C | No |
| API endpoint added/changed, or auth/middleware/binding/contract change | A + Smoke suite | C + Smoke suite | No (data-smoke self-skips off staged machines) |
| Release candidate (`dev → main`) | — | E | Yes (staged resources, zero unexplained skips) |

## 5. Backend command catalog (validated)

All filters use **dot-bounded** namespace substrings. Naked substrings overlap:
`Quran.Words` also matches `Quran.WordsDisplay`; `Quran.WordsMorphology` also matches
`Quran.WordsMorphologyEnriched` and `Quran.WordsMorphologyExplorers`. Always keep the
leading and trailing dots as written; the enriched family MUST be listed explicitly.

```bash
# Focused namespace (any area):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab.Relationships"

# Abwab + Api (current-feature slice, ~35 s):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "(FullyQualifiedName~QuranDashboard.Tests.Abwab)|(FullyQualifiedName~QuranDashboard.Tests.Api)"

# Broad no-pipeline regression (~45 s, excludes the ten namespaces entirely):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke"

# Smoke suite (real pipeline over every registered route; data-smoke self-skips without the staged dump):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke"

# Full Backend suite (~5–5.5 min):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build

# One pipeline family (example — Translations):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~.Quran.Translations."

# All ten pipeline families:
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~.Quran.Import.|FullyQualifiedName~.Quran.WordsDisplay.|FullyQualifiedName~.Quran.WordsMorphology.|FullyQualifiedName~.Quran.WordsMorphologyEnriched.|FullyQualifiedName~.Quran.WordsSimpleI3rab.|FullyQualifiedName~.Quran.Mutashabihat.|FullyQualifiedName~.Quran.Navigation.|FullyQualifiedName~.Quran.Tafsirs.|FullyQualifiedName~.Quran.Translations.|FullyQualifiedName~.Quran.FullI3rab."
```

`--no-build` requires a preceding `dotnet build Backend/QuranDashboard.sln` for the
current code state (§7). The no-pipeline filter, the all-pipeline filter, and the
Smoke namespace filter partition the suite by construction: no-pipeline excludes the
ten pipeline namespaces AND `Tests.Smoke`; all-pipeline selects exactly the ten
namespaces; Smoke selects exactly its own namespace. (Historical pre-Smoke discovery
counts: 1,435 + 603 listed = 2,038; theory expansion brought execution to 2,080.)

## 6. Frontend command catalog

The Vitest fork cap (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) is baked into the
`npm test` script and MUST be preserved; direct `ng test` invocations MUST prefix the
env vars themselves (see `AGENTS.md`). Do not add new npm scripts for testing.

```bash
cd Frontend/quran-dashboard-ui

# Focused spec file:
npm test -- --include="src/app/features/abwab/data-access/abwab-relationships-cache.spec.ts"

# Focused feature glob:
npm test -- --include="src/app/features/abwab/**/*.spec.ts"

# Full Frontend suite (~3.5 min):
npm test

# Production build (separate from tests — the test builder ignores dist/):
npm run build

# E2E / release (existing script; also run by CI):
npm run e2e
```

## 7. Build requirements

- Backend compilation-affecting changes REQUIRE `dotnet build Backend/QuranDashboard.sln`
  before any `--no-build` test run against that state.
- Frontend template, routing, configuration, or bundle-affecting changes REQUIRE
  `npm run build` before final PR completion. Do not prepend `npm run build` to
  ordinary test runs — the test builder compiles its own bundle.
- Before final PR completion, every changed application MUST build.
- Builds MUST be run **after the latest fix, not before it**: a previously successful
  build is stale evidence once any relevant code or configuration changes.

## 8. Failure and skip handling

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

## 9. Responsibilities by workflow

**Implementer** — selects Tier A from the changed scope; runs focused tests during
implementation; reports exact commands and outputs; MUST NOT substitute an unrelated
broad suite for missing focused coverage.

**Phase orchestrator** (e.g. `speckit-phase-loop`) — derives final verification from
this strategy; ordinary phases run Tier A; escalates to Tier B at milestones and to
Tier D when changed paths hit §3 Tier D triggers; MUST NOT run full pipeline suites
automatically for every phase.

**Reviewer** (`engineering-review`) — verifies the executed tier matches the changed
risk; MUST NOT demand full suites when this strategy accepts focused evidence; MUST
block when a Tier D trigger existed but its tests were not run; treats skipped
required canonical tests as missing evidence.

**Pre-PR workflow** — applies Tier C; adds Tier D only when triggered; adds the Smoke
suite when the change touches API routes, contracts, auth, middleware, or binding.

**Any implementer adding or changing an API route** — MUST add/update the matching
`SmokeRouteCatalog` entry in the same change; `SmokeCoverageParityTests` fails CI
otherwise.

**Release workflow** — applies Tier E and verifies canonical tests actually ran (no
unexplained skips).

## 10. Scope examples

| Change | Tier(s) | Command family |
| --- | --- | --- |
| Abwab relationship handler change | A → C | `~Tests.Abwab.Relationships` during work; Abwab+Api at phase end; no-pipeline pre-PR |
| Angular Abwab component change | A → C | `--include="src/app/features/abwab/**/*.spec.ts"`; full `npm test` + `npm run build` pre-PR |
| Shared authorization middleware change | A → C | Abwab+Api filter (covers Api/Access, Api/Security, Abwab/Permissions); no-pipeline pre-PR |
| New Abwab-only EF migration | A → C | Abwab+Api filter (includes `Abwab.Ci` schema-compatibility + migration-based fixture) |
| Translation importer change | D | `~.Quran.Translations.` first; no-pipeline + affected families (or full suite) pre-PR |
| Model-wide `QuranDashboardDbContext` persistence change that can affect pipeline tables or execution | B → C + D | no-pipeline first, then full Backend suite pre-PR |
| Enriched morphology artifact replacement | D/E | `~.Quran.WordsMorphologyEnriched.` on a machine with the staged artifact; verify zero skips |

## 11. Deferred optimizations

Test implementation and fixture optimizations are separate, explicitly-requested work
— not part of applying this strategy: sharing the enriched-morphology artifact load,
safely reducing repeated canonical imports, introducing test traits/categories to
replace namespace filters, and optional test consolidation. Do not perform them as a
side effect of running or reviewing tests.

The Smoke suite deliberately follows the existing namespace-selection convention
(`FullyQualifiedName~QuranDashboard.Tests.Smoke`); it introduces no traits, so the
trait deferral above still stands.
