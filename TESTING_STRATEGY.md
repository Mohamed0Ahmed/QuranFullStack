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

The **Backend** baselines below were re-taken **2026-07-29** (Abwab feature, Slice A — last
re-measured after the archived-parent create smoke that closed the last gap in the
section-inheritance fix) on a developer machine with Docker up,
`resources/import-sources/` staged, and the canonical dump at
`resources/db-dumps/quran-canonical/` present, against this tree. The **Frontend** row is
carried forward from the 2026-07-27 measurement and was NOT re-run on 2026-07-29 — no Frontend
spec file changed (only generated API model files were added, unconsumed by any component in
this Backend-only slice), so it is reproduced rather than re-measured (§2: no success claim
without fresh output; this row is a carried baseline, not evidence for any gate). The pipeline
row is likewise **derived, not re-run** on 2026-07-29: `1,827 − 1,076 − 134 = 617`, unchanged,
which is what the partition identity below is for:

| Run | Tests | Duration | Skipped |
| --- | --- | --- | --- |
| Backend full suite | 1,827 | 5 m 35 s | 0 |
| Backend no-pipeline (Tier B/C) | 1,076 | 18-20 s | 0 |
| Backend ten pipeline families (Tier D) — derived, last timed 2026-07-28 | 617 | 3 m 54 s | 0 |
| Backend route smoke (§3 Tier A/C route gate) | 134 | 51-52 s | 0 |
| Frontend full suite (169 spec files) — carried from 2026-07-27 | 1,938 | 171 s | 0 |

Counts and durations are indicative, not contractual. The zero-skip column holds only
because the staged canonical resources *and* the canonical dump were present; on a machine
without them the canonical families self-skip (§3 Tier D, §3 Tier E) and the data-smoke rows
self-skip, leaving 121 passed on the smoke tier (§3 Tier A/C).

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
# Focused namespace (20 tests, ~1 s):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.Api.RateLimiting"

# Whole API slice — Access, ApiBehavior, Health, Middleware, RateLimiting (60 tests, ~10 s):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.Api"

# One explorer read-model family (266 tests, ~18 s):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.Quran.WordsWordTypes"

# Route smoke tier (134 tests, ~51-52 s) — REQUIRED at Tier A when the phase touched an API
# route, a request/response contract, authentication/authorization, middleware, or binding:
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."

# Frontend words feature (93 spec files, 1,384 tests, ~98 s):
cd Frontend/quran-dashboard-ui
npm test -- --include="src/app/features/words/**/*.spec.ts"
```

The complete Backend or Frontend suite MUST NOT be demanded for an ordinary phase.

### Tier B — Feature milestone / user-story completion

Run after a vertical slice, a substantial User Story, or a related group of phases.

Backend broad regression — the **no-pipeline** run (~18-20 s, 1,076 tests):

```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke."
```

Coverage statement (be accurate about this): this run keeps every `Tests.Api.*` family,
the `Quran.MushafReader`, `Quran.Words`, `Quran.WordsMorphologyExplorers`,
`Quran.WordsRoots`, and `Quran.WordsWordTypes` read-model families, and
`Tests.TestSupport.Logging`. It excludes the ten pipeline namespaces **completely —
including the fast unit tests that live inside them** (617 tests) — and it excludes
`QuranDashboard.Tests.Smoke.` (134 tests), which has its own gate below. It is a fast broad
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
when that dump is absent (134 passed with it; **121 reasoned, not freshly re-measured** —
134 minus the 13 data-tier tests that self-skip without the dump, none of which are among
the Abwab additions, so the pre-existing 74/61 split's 13-test gap carries forward
unchanged). A present dump that is corrupt or stale — sha256 or migration-head mismatch —
fails loud rather than skipping. "134 passed, 0 skipped" and "121 passed, data tier skipped"
are both valid evidence; an unqualified "smoke passed" is not.

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

- full Backend suite (~5 m 20 s);
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
| Frontend feature only (`words`, `mushaf`, `auth`, `dashboard`) | A | C | No |
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

# Whole API slice (60 tests, ~10 s):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Api"

# Broad no-pipeline regression (1,076 tests, ~18-20 s; excludes the ten pipeline namespaces
# and the smoke namespace entirely):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName!~.Quran.Import.&FullyQualifiedName!~.Quran.WordsDisplay.&FullyQualifiedName!~.Quran.WordsMorphology.&FullyQualifiedName!~.Quran.WordsMorphologyEnriched.&FullyQualifiedName!~.Quran.WordsSimpleI3rab.&FullyQualifiedName!~.Quran.Mutashabihat.&FullyQualifiedName!~.Quran.Navigation.&FullyQualifiedName!~.Quran.Tafsirs.&FullyQualifiedName!~.Quran.Translations.&FullyQualifiedName!~.Quran.FullI3rab.&FullyQualifiedName!~QuranDashboard.Tests.Smoke."

# Route smoke tier (134 tests, ~51-52 s; boots the real API composition over Testcontainers):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."

# Full Backend suite (1,827 tests, ~5 m 35 s):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build

# One pipeline family (example — Translations):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build --filter "FullyQualifiedName~.Quran.Translations."

# All ten pipeline families (617 tests, ~3 m 54 s):
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~.Quran.Import.|FullyQualifiedName~.Quran.WordsDisplay.|FullyQualifiedName~.Quran.WordsMorphology.|FullyQualifiedName~.Quran.WordsMorphologyEnriched.|FullyQualifiedName~.Quran.WordsSimpleI3rab.|FullyQualifiedName~.Quran.Mutashabihat.|FullyQualifiedName~.Quran.Navigation.|FullyQualifiedName~.Quran.Tafsirs.|FullyQualifiedName~.Quran.Translations.|FullyQualifiedName~.Quran.FullI3rab."
```

`--no-build` requires a preceding `dotnet build Backend/QuranDashboard.sln` for the
current code state (§7).

**Partition check (re-measured 2026-07-29, not asserted):** the no-pipeline filter, the
all-pipeline filter, and the smoke filter partition the suite losslessly —
**1,076 + 617 + 134 = 1,827**, exactly the full-suite total, with zero failures and zero
skips. Three of the four runs were fresh on 2026-07-29; the pipeline term is the identity's
remainder rather than a fourth run, which is legitimate only because the other three were
measured against this same tree. `Tests.Abwab` (36 tests: schema, write
behavior, and tree-read tests) landed in the no-pipeline set by default, exactly as this
paragraph predicts a new top-level namespace will; no pipeline filter needed a change since
Abwab tables are non-pipeline. No test family falls outside all three tiers, and none is
counted twice. Re-verify this three-way identity whenever a namespace is added: a new
top-level namespace lands in the no-pipeline set by default, a new *pipeline* family must be
added to both pipeline filters, and anything landing under `QuranDashboard.Tests.Smoke.`
must be a genuine route-smoke test — the identity is what catches an accidental namespace
collision with the two legacy `*SmokeTests` classes that live in other families.

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

# Focused feature glob (93 files, 1,384 tests, ~98 s):
npm test -- --include="src/app/features/words/**/*.spec.ts"

# Full Frontend suite (169 files, 1,938 tests, ~2.9 min):
npm test

# Production build (separate from tests — the test builder ignores dist/):
npm run build

# Browser E2E — opt-in, chromium only, boots both servers (see e2e/README.md):
npm run e2e                       # headless
npm run e2e:headed                # visible browser
npm run e2e:ui                    # Playwright UI mode
npm run e2e -- e2e/mushaf-reader.e2e.ts   # one flow file
```

The frontend features are `auth`, `dashboard`, `mushaf`, and `words`; shared code lives
in `src/app/core/` and `src/app/shared/`, with app-shell specs at `src/app/*.spec.ts`.

The E2E suite boots the Angular dev server **and** the backend `https` launch profile
(`ASPNETCORE_ENVIRONMENT=Development`), so it reads the real local `quran_dashboard` database.
Every flow is read-only and every count assertion is loose; do not add write flows to it without
first moving it onto an isolated database. It requires `dotnet build Backend/QuranDashboard.sln`
beforehand (the backend boots with `--no-build`) and mkcert certificates in the frontend project
root. It is **not** the backend route-smoke tier (§3 Tier A/C, §5) and does not substitute for it:
the smoke tier is a required gate for route/contract/auth changes, the E2E layer is not.

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
