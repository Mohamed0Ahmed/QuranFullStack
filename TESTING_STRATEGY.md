# Testing Strategy — Quran Dashboard Workspace

## 1. Purpose and authority

This file is the single source of truth for **test selection, verification depth, execution
lanes, slow data-pipeline triggers, and the phase / milestone / review / PR / release
gates** across the whole monorepo (Backend + Frontend). Each agent's native root and area
entrypoints route here when test selection, execution, or reporting is triggered; they do not
carry a second test policy. The commands and the trigger matrix live in this file and nowhere
else.

When any other instruction file, skill, README, or workflow conflicts with this
strategy about *which tests to run and when*, **this strategy controls test selection**
— unless an active feature specification explicitly requires stronger verification for
its own scope. Test *quality* rules (test-guard, CODING_PRINCIPLES) and Quranic data
safety rules are unaffected and always apply.

**This document carries no test counts and no durations.** It used to carry counts, and they
drifted: a number written in prose is wrong the moment the next test lands, and nothing in this
repo re-checks it — there is no CI (§8). So the rule is:

> **Never record a test count, file count, pass total, or measured duration in this file, in a
> README, or in a commit message as a standing fact. Run the command and read the number.** A
> figure belongs in evidence for a specific run, dated and attached to that run — never in prose
> that outlives it.

Every `dotnet test` run prints `Passed! - Failed: N, Passed: N, Skipped: N, Total: N`; the
Angular test builder prints `Test Files` and `Tests`. That output is the number; this file is
not. Any runtime guidance you find here or in a plan is order-of-magnitude selection guidance,
never an assertion of fact.

**Selection is by lane, not by hand-written filter.** Backend lanes are arguments to
`Backend/scripts/test-backend`, which resolves each lane against the class catalog at
`Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv` and builds the
`--filter` itself. Frontend lanes are `npm run test:*` scripts backed by named Angular test
configurations in `Frontend/quran-dashboard-ui/angular.json`. Do not hand-write a
`FullyQualifiedName` filter or an ad-hoc `--include` glob as a gate: a lane is reproducible,
catalog-validated, and reportable by name, and a hand-written filter is none of those.

**Tier vocabulary.** The former Tier A–E labels are superseded by the lanes in §3 and §4 and
the trigger matrix in §5. One survives as a name: `tier-b` is the Backend no-pipeline
milestone lane.

Baselines are taken on a developer machine with Docker up, `resources/import-sources/` staged,
and the canonical dump at `resources/db-dumps/quran-canonical/` present and regenerated against
this tree's migration head.

## 2. Core principles

- Verification MUST be fresh against the final code state. Evidence produced before
  the most recent code or configuration change is stale and MUST NOT close a phase,
  PR, or release gate.
- Test scope MUST match the changed scope and its risk. Running an unrelated broad
  suite is not a substitute for the focused tests that cover the change.
- Broad lanes run **once**, at a milestone, engineering-review, or pre-PR boundary — not
  after individual edits. Full Backend or Frontend suites are NOT automatically required
  after every phase.
- Slow tests are preserved, not deleted: pipeline and acceptance tests MUST remain in
  the repository and in their lanes even though they run less frequently.
- Quran data-safety checks MUST NOT be silently weakened, skipped, or rescheduled out
  of their lanes.
- A failed or unexpectedly skipped required test MUST NOT be counted as passing
  evidence.
- An existing test failure MUST be reported as a failure. Narrowing a selection to make a
  run pass MUST NOT be used to hide it.
- Agents MUST record the actual lane and command they ran and the actual outcomes they
  observed (pass/fail/skip counts), not inferred or remembered results.
- Deleting a test requires documented obsolete/redundant proof and named replacement
  coverage.

## 3. Backend lanes — `Backend/scripts/test-backend`

`Backend/scripts/test-backend --help` prints the authoritative usage. Every focused lane below
**requires** exactly one of `--build` or `--no-build`, and every lane accepts `--list-tests`
(discovery only — it starts no container and never shards) and `--results-dir PATH`. `pre-pr`
has its own flag rules; see the note under the table.

| Lane | What it selects from the catalog | Command | Run when | Must not run when |
| --- | --- | --- | --- | --- |
| `fast` | every `Kind=Fast` class, including fast logic that lives in Pipeline namespaces | `Backend/scripts/test-backend fast --no-build` | small logic iterations; fast regression after test-only logic changes | never for a case that needs a collection fixture, canonical artifact, migration, or child process — those are not `Kind=Fast` |
| `feature` | one validated `Feature` value, or one exact class/method | `Backend/scripts/test-backend feature Access --no-build`, `feature --class FULL_CLASS_NAME`, `feature --test FULL_METHOD_NAME` | implementation and coherent feature-slice completion | never as a substitute for separately triggered Smoke or canonical evidence |
| `access` | every `Feature=Access` class | `Backend/scripts/test-backend access --no-build` | authorization slice completion and formal review | never to pull in Pipeline, canonical data, or Frontend tests |
| `access-db` | `Feature=Access` classes that are `Kind=Database` or carry `Schema` in `Concerns` | `Backend/scripts/test-backend access-db --no-build` | Access persistence, constraint, EF model, catalogue, grant, or audit-storage change | does not cover the CLI wrapper or the staged migration upgrade |
| `migration` | every `Kind=Migration` class | `Backend/scripts/test-backend migration --no-build` | migration, EF model, backfill, collision/refusal, or schema-guard work | never after unrelated UI or ordinary service edits |
| `process` | every `Kind=Process` class | `Backend/scripts/test-backend process --no-build` | wrapper, exit-code, executable-directory configuration, or operator-boundary change | never to duplicate lower-level migration/service permutations as child processes |
| `smoke` | `Gate=Smoke` excluding `Kind=Canonical` — the route/composition classes, not the data tier | `Backend/scripts/test-backend smoke --no-build` | route, contract, auth, middleware, model binding, startup, DI, configuration, or shared `DbContext` composition change; formal review (§6) | not after every edit |
| `tier-b` | every `Gate=TierB` class | `Backend/scripts/test-backend tier-b --no-build` | feature milestone, engineering review, ordinary Backend pre-PR | never widened with Pipeline or Smoke merely to look broader |
| `pipeline` | `Gate=Pipeline` excluding `Kind=Canonical`, optionally narrowed by `--feature` | `Backend/scripts/test-backend pipeline --feature Translations --no-build`; omit `--feature` only for a shared-pipeline trigger | importer, manifest handling, pipeline entity/schema, shared pipeline, or reachable persistence change | never for isolated authorization, general API, caching, or Frontend-only changes |
| `canonical-data` | every `Kind=Canonical` class | `Backend/scripts/test-backend canonical-data --no-build` | canonical source, manifest/hash, dump, Quran schema/persistence, shared pipeline, or release acceptance | never for isolated authorization/UI work; missing required resources are a gate failure, not a skip (§3.4) |
| `pre-pr` | every catalog row — the full Backend suite, run exactly once | `Backend/scripts/test-backend pre-pr` | shared-infrastructure prerequisite, release/full-regression acceptance, or an explicit formal-review trigger | never after individual edits; ordinary isolated authorization pre-PR is `access` + `smoke` + `tier-b` and excludes Pipeline/canonical |

`pre-pr` is the one lane with different flag rules: executing it **always builds once**, so it
rejects `--no-build` and needs no `--build`; discovering it requires the exact form
`pre-pr --list-tests --no-build`.

`pre-pr` is deliberately not composed from `fast + access + tier-b + pipeline + smoke +
canonical-data`. Those lanes overlap, so a composed run would repeat tests, rebuild fixtures,
and make the measurement meaningless.

### 3.1 The catalog is the selection contract

Two tab-separated catalogs under `Backend/tests/QuranDashboard.Tests/TestSupport/Execution/`
define what the lanes mean:

- `test-gates.tsv` — one row per test class: `FullyQualifiedClassName`, `Feature`, `Kind`,
  `Gate`, `Concerns`. The valid `feature` lane keys are exactly the distinct `Feature` values;
  an unknown key fails usage rather than running nothing.
- `test-resources.tsv` — one row per xUnit collection: `CollectionName`, `ResourceClassName`,
  `ParallelPolicy`, `StatePolicy`.

`TestGateCatalogTests` is what makes the catalogs trustworthy, and it is a required test rather
than bookkeeping: it proves every discovered test class has exactly one catalog entry, that the
primary gates partition every discovered class, that the full-Backend selection uses every
discovered class, that `Kind=Fast` classes use no resource collection, that Smoke classes map
to the Smoke gate with only the data tier canonical, that collection resources match the
compiled collection definitions, and that the two PostgreSQL ownership shards (§3.3) partition
the lane they replace. **Adding a test class therefore requires adding its catalog row in the
same change** — otherwise that test runs in no lane and `TestGateCatalogTests` fails by name.

### 3.2 Build, output, and hang timeouts

- `--build` builds `Backend/QuranDashboard.sln` once, single-threaded, before testing.
- `--no-build` requires the existing test assembly; lanes that select `Kind=Migration` or
  `Kind=Process` additionally require built `QuranDashboard.AccessAdmin` output, and lanes that
  select `Gate=Pipeline` or `Kind=Canonical` require built `QuranDashboard.DataImporter`
  output. A missing output is an explicit error naming the path, not a silent stale run.
- Build once for a code state, then use `--no-build` for every subsequent lane against it.
- The runner applies its own hang timeout — shorter for an all-`Fast` selection, longer
  otherwise. Do not override it, and do not pipe a run into `tail`: the output *is* the
  evidence.

### 3.3 One database server at a time, and the two-shard lanes

Every database-bearing fixture leases from **one shared `postgres:16-alpine` runtime** guarded
by a cross-process OS lock (`TestSupport/PostgreSql/CrossProcessPostgreSqlLock.cs`), so two
Backend test processes MUST NOT run concurrently — including one in an IDE alongside one in a
terminal.

The single exception is `QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests`, whose fixture
takes an **exclusive `postgres:18-alpine` server** because the canonical dump is written by an
18 `pg_dump` that a 16 `pg_restore` refuses (the measured reasoning is in
`Backend/tests/QuranDashboard.Tests/README.md`). To keep the two majors from ever running at
once, `Backend/scripts/test-backend` splits **any lane that selects that class alongside at
least one other class into two sequential `dotnet test` invocations**: shard 1 on the shared 16
runtime, shard 2 on the exclusive 18 server, with a bounded wait in between for the previous
container to disappear. In practice:

- `pre-pr` runs as **two complementary, non-overlapping shards**. Any claim that the full
  Backend suite is a single invocation is false.
- `canonical-data` runs as **two shards** as well, for the same reason.
- `smoke` runs as **one** invocation — the lane excludes `Kind=Canonical`, so it never selects
  the data tier.

The runner prints its shard labels and expanded filters; report them. Shard exit statuses are
combined, so a lane fails if either shard fails.

Each run exports a unique run ID, labels its Docker resources with it, and calls
`Backend/scripts/cleanup-test-runtime --run-id RUN_ID` on exit — which removes only resources
carrying all five project test labels *and* that run ID, and never prunes. **Confirm zero
project-owned containers after a run** and report the cleanup state; the same script can be run
by hand with `--dry-run` first.

### 3.4 Canonical resources fail the lane; they do not silently skip

When a lane selects a canonical class, the runner **preflights the resource it needs before
starting anything** and exits non-zero when it is missing:

- every `Quran.Import.*` class and `DisplayWordsRealImportIdentityLinksTests` require
  `resources/import-sources/quran-foundation/`;
- `EnrichedMorphologyArtifactTests` requires the staged enriched morphology artifact;
- `SmokeDataReadTests` requires both `resources/db-dumps/quran-canonical/quran-canonical.dump`
  and its `manifest.json`.

The runner prints `canonical data tier: ran`, `failed`, `not selected`, `discovery only`, or
`failed preflight`. Quote that line — it is the skip accounting for the canonical tier.

The in-test source gates (`Quran/Import/FoundationImportSourceGate.cs`,
`Quran/WordsDisplay/CanonicalImportSourceTestGate.cs`, `Smoke/Data/SmokeDumpGate.cs`) still
self-skip when the resources are absent, because a run started **outside** the runner — an IDE,
a plain `dotnet test` — must not start a server it cannot use. That fallback is not the gate.
**A canonical claim is only evidence when it came from the runner, whose preflight makes
"absent" impossible rather than invisible.**

A dump that is *present* but corrupt or stale throws loud instead of skipping: `SmokeDumpGate`
checks the archive's sha256 against the manifest, the manifest's migration id against this
tree's head, and the producer's major version against the restore image — all before the
container starts. **Any migration invalidates the dump. Regenerate it with
`Backend/scripts/create-smoke-dump --yes` in the same change**, never at the next run's
expense; this has bitten repeatedly.

### 3.5 The EF pending-model check

`Backend/scripts/check-pending-model --build|--no-build` reports whether the EF Core model has
pending changes. It never adds and never applies a migration. Run it alongside the `migration`
lane whenever the EF model or schema is in scope — it is a separate command, not part of any
test lane.

## 4. Frontend lanes — `npm run test:*`

Run these from `Frontend/quran-dashboard-ui/`. Each `test:*` lane delegates to the `test`
script, which owns the two-fork Vitest cap (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) and the
run timeout; a direct `ng test` invocation bypasses both and is not a lane. The named Angular
test configurations live in `angular.json`.

| Lane | What it selects | Command | Run when | Must not run when |
| --- | --- | --- | --- | --- |
| Fast / unit | the pure model/utility/data/state/cache/helper specs in the `fast` configuration | `npm run test:fast` | pure state, mapping, codec, cache, URL, or validation work | do not assume it avoids Angular bundle startup; do not add a TestBed/component spec to it |
| Feature-focused | one feature configuration | `npm run test:feature:abwab` \| `:auth` \| `:dashboard` \| `:mushaf` \| `:words` | feature implementation and coherent feature-slice completion | do not run unrelated feature directories |
| Authorization | the cross-cutting auth/config/route/security specs | `npm run test:authorization` | Frontend auth fixtures/contracts, token handling, callback, secure-origin behavior, route posture, or production auth config changed | a Backend-only authorization change needs no Frontend test unless a Frontend or generated contract changed |
| Composition | every component/directive spec plus the named application/overlay compositions | `npm run test:composition` | shared component harness, Angular rendering, overlay composition, or broad component infrastructure changed | not after pure Backend or pure utility changes; this is jsdom, not a browser — never call it E2E |
| Shared | app-shell, core, shared, and environment specs | `npm run test:shared` | core/shared/routing/app-shell/environment/global test setup changed | not a substitute for the affected feature lane |
| Full suite | every `src/**/*.spec.ts` | `npm run test:full` (`npm test` is the same run) | when a broad Frontend gate is required on its own | not for Backend-only changes |
| Type-check / build | leaf app and spec TypeScript projects; production bundle | `npm run typecheck:app`, `npm run typecheck:spec`, `npm run typecheck`, `npm run build:verify` | compilation, templates, routing, config, generated DTOs, or bundle-affecting work | never cite a root `npx tsc --noEmit`: the root `tsconfig.json` is `"files": []` plus project references and `--noEmit` does not follow references, so it type-checks nothing |
| Pre-PR | `check:permission-catalogue` → `check:audit-action-types` → `typecheck` → `build:verify` → `test:full`, in that order | `npm run test:pre-pr` | once before a PR that changed Frontend code; engineering review when Frontend is in scope | never for a Backend-only change with no generated/frontend contract diff |
| Gate self-check | asserts the named configurations still select what they claim | `npm run test:gates` | shared Frontend test/config infrastructure, `angular.json` test configurations, or spec-layout changes | — |

`npm run test:gates` runs `testing/verify-test-gates.mjs`, not Vitest. **It is not part of
`test:pre-pr`** — when you change the configurations or move specs between areas, run it as its
own step and report it separately.

No Frontend lane requires PostgreSQL, EF migrations, Backend startup, Docker, or importer
processes.

**Backend-only rule.** Run no Frontend tests for a Backend-only change when no file under
`Frontend/quran-dashboard-ui/`, no committed OpenAPI document, and no generated Frontend API
contract changed. If a Backend contract change regenerates Frontend DTOs or changes Frontend
auth fixtures/models, run the focused Frontend contract/authorization lane and the required
type-check; run `test:pre-pr` only because Frontend files then changed, not merely because the
Backend changed.

## 5. Execution-trigger matrix

| Changed scope | During implementation | Slice/phase completion | Formal engineering review | Pre-PR | Explicitly excluded |
| --- | --- | --- | --- | --- | --- |
| Pure Backend logic | Exact method/class, then `fast` | Feature lane + build once if required | Feature lane + Tier B | Tier B once | Smoke, Pipeline, canonical, Frontend |
| Access service/contract | Exact Access class, `--no-build` | `access`; Smoke only if composition/contract affected | `access` + `smoke` + Tier B | Access + triggered Smoke + Tier B once | Pipeline, canonical, full Frontend |
| Access schema/persistence | Exact class or `access-db` | `access-db` + relevant Access | Access + migration if schema in scope + Smoke + Tier B | Access + migration + Smoke + Tier B | Pipeline/canonical unless shared Quran persistence changed |
| Migration upgrade | Exact migration case, then `migration` | `migration` once | Migration + Access + Smoke + Tier B + pending-model check | Same reviewed gates once | Head template for migration cases |
| AccessAdmin wrapper | Exact process case | `process` | Process + Access + Smoke + Tier B | Same reviewed gates once | Extra process E2E duplicates |
| API route/auth/middleware/binding/DI | Exact API family | API family + route `smoke` | Focused family + Smoke + Tier B | Smoke + Tier B once | Canonical Smoke Data unless independently triggered |
| One importer/pipeline family | Exact Fast class, then `pipeline --feature FEATURE_KEY` | Named Pipeline feature | Named feature + Tier B; Smoke only for API composition | Named feature + Tier B once | Other Pipeline features and canonical unless source/shared trigger |
| Shared pipeline/Quran persistence | Representative feature first | `pipeline` for all affected classes | Tier B + full Pipeline + required schema checks | Tier B + Pipeline once | Frontend unless files/contracts changed |
| Canonical source/manifest/hash/dump | Exact canonical class | `canonical-data` | Pipeline + canonical + Smoke, zero unexpected skips | Full Backend `pre-pr` once for release/canonical acceptance | No source synthesis or skipped release evidence |
| Shared Backend test/runtime infrastructure | Exact contract + one pilot feature | Each converted feature only | Access + Smoke + Tier B + representative Pipeline | Full Backend `pre-pr` once | Repeated full runs during conversion |
| Frontend pure utility/state | Exact spec or `test:fast` | Feature lane | Focused lane + `test:pre-pr` once | Reuse unchanged review evidence | All Backend |
| Frontend feature component | Exact spec, then feature | Feature lane | Feature/composition as affected + `test:pre-pr` once | Reuse unchanged review evidence | Unrelated Backend/Pipeline |
| Frontend auth/security | Exact spec, then authorization | `test:authorization` | Authorization + `test:pre-pr` once | Reuse unchanged review evidence | Backend suites unless Backend also changed |
| Shared Frontend test/config infrastructure | Exact affected specs + `test:gates` | Fast/authorization/one feature | Focused lanes + `test:pre-pr` once | Reuse unchanged review evidence | E2E unless browser-flow behavior changed |
| Backend-only, no generated/frontend diff | Backend lane only | Backend lane only | Backend gates only | Backend pre-PR only | Every Frontend test/build |

Formal-review evidence and pre-PR evidence are the same final-state run when the tree and
environment have not changed. Do not rerun an unchanged broad gate merely to rename the
milestone.

## 6. The route-smoke gate

**A change touching `Backend/api/` routes, request/response contracts,
authentication/authorization, middleware, or model binding REQUIRES the `smoke` lane**, in
addition to the focused tests for the changed endpoints:

```bash
Backend/scripts/test-backend smoke --no-build
```

It boots the real API composition — `SmokeApiHost` builds `WebApplicationFactory<HealthController>`
in the `Testing` environment over a migrated-but-empty database leased from the shared
PostgreSQL runtime — and drives every registered route through routing, authorization, model
binding, and serialization for every persona in `SmokePersonas.All`, against the real JWT
bearer pipeline with offline token validation.

`SmokeCoverageParityTests` locks `SmokeRouteCatalog` bidirectionally to the live
`EndpointDataSource`: a registered route with no catalog entry fails by name, and a catalog
entry whose route no longer exists fails by name too. **Adding or changing an API route
requires adding or updating the matching `SmokeRouteCatalog` entry in the same change.** This
is not optional bookkeeping — it is the reason the gate can be trusted.

The data tier is a **separate lane**. `QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests`
restores the canonical dump so the seeded read routes are asserted against real data, and it is
`Kind=Canonical`: the `smoke` lane excludes it, and `canonical-data` and `pre-pr` include it as
their exclusive-PostgreSQL shard (§3.3). Evidence must therefore say **which lane ran**: "`smoke`
passed, N tests" and "`canonical-data` passed, canonical data tier: ran" are both valid; an
unqualified "smoke passed" is not.

## 7. Build requirements

- Backend compilation-affecting changes REQUIRE one `--build` lane run (or
  `dotnet build Backend/QuranDashboard.sln`) before any `--no-build` lane against that state.
  Build once, then reuse it.
- Frontend template, routing, configuration, or bundle-affecting changes REQUIRE
  `npm run build:verify` before final PR completion. Do not prepend a build to ordinary test
  runs — the test builder compiles its own bundle.
- Before final PR completion, every changed application MUST build.
- Builds MUST be run **after the latest fix, not before it**: a previously successful
  build is stale evidence once any relevant code or configuration changes.

## 8. Continuous integration — none in this tree

**There is no CI. `.github/workflows/` does not exist**, so no workflow runs builds,
tests, contract guards, or fork-cap assertions on push or pull request.

Consequences that every gate above depends on:

- Every lane is a **local, human-or-agent-executed** gate. Nothing verifies that it ran.
  Evidence is the recorded command output, and only that.
- No automated check blocks a PR. "CI is green" is never available as evidence here and
  MUST NOT be claimed.
- Preserving the Vitest two-fork cap and the Backend hang timeouts is a review obligation,
  because nothing enforces them.
- Because the only runs happen on developer machines that *do* have
  `resources/import-sources/` staged, the canonical lanes genuinely execute — and through
  the runner, an unstaged machine fails the lane rather than skipping it (§3.4).

If a CI workflow is added later, this section MUST be rewritten to describe what it
actually runs, and any lane that starts relying on it MUST say so explicitly.

## 9. Failure and skip handling

- Any required test failure blocks completion of the lane's gate.
- Unexpectedly skipped tests MUST be listed and explained in the evidence.
- Missing canonical resources fail the lane at preflight (§3.4). A canonical claim backed by a
  run that never had the resources is not evidence.
- A lane that shards MUST report both shard results; either shard failing fails the lane.
- Selections MUST NOT be narrowed to route around a failing test. If a pre-existing
  failure is discovered, report it as such — separately from the change's own
  results — and do not absorb or hide it.
- Environment failures (Docker down, missing resources, OOM, a stranded container from a
  previous run) MUST be reported separately from product failures, and the affected lane
  re-run once resolved.
- No success claim without fresh command output observed in the current session.

## 10. Responsibilities by workflow

**Implementer** — selects the narrowest meaningful lane from the changed scope; runs focused
lanes during implementation; reports the exact lane, command, reason, result, skips, and
cleanup state; MUST NOT substitute an unrelated broad lane for missing focused coverage.

**Implementer, catalog obligations** — adding a test class requires its `test-gates.tsv` row in
the same change (§3.1); **adding or changing an API route requires the matching
`SmokeRouteCatalog` entry in the same change** (§6); adding a migration requires regenerating
the canonical dump in the same change (§3.4).

**Phase orchestrator** — derives final verification from this strategy; ordinary phases run the
narrow lanes; escalates to `tier-b` at milestones and to `pipeline`/`canonical-data` only when
§5 triggers fire; MUST NOT run broad or pipeline lanes automatically for every phase.

**Reviewer** (`engineering-review`) — verifies the executed lanes match the changed
risk; MUST NOT demand broad lanes when §5 accepts focused evidence; MUST block when a
pipeline/canonical trigger existed but its lane was not run; **MUST block when the change
touched an API route, contract, auth, middleware, or binding and the `smoke` lane did not
run**, and when a route changed without the matching `SmokeRouteCatalog` update; treats a
canonical preflight failure as missing evidence. The formal reviewer owns the final broad
review gates.

**Pre-PR workflow** — applies the Pre-PR column of §5; adds pipeline/canonical lanes only when
triggered. For route, contract, auth, middleware, or binding changes it MUST run the `smoke`
lane and record its result.

**Release workflow** — runs the full Backend `pre-pr` lane (two shards), `canonical-data`, and
the Frontend `test:pre-pr` lane on a machine with `resources/import-sources/` and the canonical
dump staged, and accounts for every skipped test.

## 11. Browser E2E — opt-in, never a required gate

A browser E2E layer exists: `Frontend/quran-dashboard-ui/playwright.config.ts` + `e2e/`,
chromium only, two sequential projects — `default` (2 workers) then `abwab` (1 worker).

```bash
cd Frontend/quran-dashboard-ui
npm run e2e                                   # headless, both projects
npm run e2e:headed                            # visible browser
npm run e2e:ui                                # Playwright UI mode
npx playwright test e2e/mushaf-reader.e2e.ts  # one flow file
```

It is an **opt-in local gate, not part of any required lane**: it is not required pre-PR and
not required for release, and an E2E run MAY be reported only as supplementary evidence, which
it MUST then be labelled as. It is **not** the backend route-smoke gate (§6) and never
substitutes for it. Promoting it into a required lane is a separate decision, to be made only
after it has proven stable across several runs. Specs are named `*.e2e.ts`, and that is not
cosmetic: the Angular unit-test builder globs its `include` patterns with `cwd` at the project's
`sourceRoot` (`src`), so no `angular.json` pattern can reach `e2e/` at all, while
`playwright.config.ts` matches only `/.*\.e2e\.ts$/` — so a `*.spec.ts` placed under `e2e/` is
run by **nothing** while looking like coverage. See
`Frontend/quran-dashboard-ui/testing/README.md`.

The suite boots the Angular dev server **and** the backend `https` launch profile
(`ASPNETCORE_ENVIRONMENT=Development`), so it reads the real local `quran_dashboard` database.
It requires `dotnet build Backend/QuranDashboard.sln` beforehand (the backend boots with
`--no-build`) and mkcert certificates in the frontend project root.

Every flow is read-only and every count assertion is loose, **with one named, deliberate
exception**: the Abwab specs (`abwab-structure.e2e.ts`, `abwab-operations.e2e.ts`,
`abwab-archive.e2e.ts`, `abwab-url-and-a11y.e2e.ts` added in Slice B2, plus
`abwab-global-order.e2e.ts` added by `abwab-global-order`, `abwab-tree-row-budget.e2e.ts`,
`abwab-slice-j-widths.e2e.ts`, and `abwab-relations.e2e.ts` added by slice K) write against the
local dev DB through a per-test sandbox section created over the API (`e2e/fixtures/abwab.ts`),
not the seeded/canonical data.
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
**The Abwab specs run in their own single-worker Playwright project, not the default
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
future flow that cannot be scoped to ids it created itself).

## 12. Deferred optimizations

Test implementation and fixture optimizations are separate, explicitly-requested work
— not part of applying this strategy: sharing the enriched-morphology artifact load,
safely reducing repeated canonical imports, and optional further test consolidation. Do not
perform them as a side effect of running or reviewing tests.
