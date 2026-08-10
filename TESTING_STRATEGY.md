# Testing Strategy V2 — Risk-Based, Focused Verification

## 1. Purpose and authority

`TESTING_CONSTITUTION.md` is the repository's sole testing-policy authority. This strategy survives
only as a transitional operational reference for the current commands, lanes, and fixtures until
Phase 7 removes it; any policy statement here that conflicts with the constitution is superseded.

When any other instruction file, skill, README, or workflow conflicts with this
strategy about the mechanics of a still-active lane, this strategy controls those mechanics unless
an active feature specification changes them for its own scope. Test selection policy comes from
the constitution and the active plan's `Testing Decision`. Test-quality rules (`test-guard`,
`CODING_PRINCIPLES.md`) and Quranic data-safety rules remain independently authoritative.

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

**Gate evidence is selected by lane, not by hand-written filter.** Backend lanes are arguments to
`Backend/scripts/test-backend`, which resolves each lane against the class catalog at
`Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv` and builds the
`--filter` itself. Frontend lanes are `npm run test:*` scripts backed by named Angular test
configurations in `Frontend/quran-dashboard-ui/angular.json`. Exact Backend selectors and the
timeout/fork-capped Frontend command in §4 are valid focused local feedback, but they are not named
final-gate evidence. Do not hand-write a `FullyQualifiedName` filter, use bare `ng test`, or present
an ad-hoc `--include` glob as a gate: a lane is reproducible, catalog-validated, and reportable by
name, and a hand-written filter is none of those.

**Tier vocabulary.** The former Tier A–E labels are superseded by the lanes in §3 and §4. One
survives as a name: `tier-b` is the Backend no-pipeline broad lane.

Baselines are taken on a developer machine with Docker up, `resources/import-sources/` staged,
and the canonical dump at `resources/db-dumps/quran-canonical/` present and regenerated against
this tree's migration head.

## 2. Core principles

- Verification MUST be fresh against the state it claims to cover. Final selection is derived from
  the cumulative base-to-worktree diff, including generated and in-scope untracked files, never only
  the latest finding or fix.
- Test scope MUST match the changed scope and its risk. Running an unrelated broad
  suite is not a substitute for the focused tests that cover the change.
- Broad/composite lanes run **once when possible**, at the meaningful final feature/change boundary
  selected by the active plan's `Testing Decision`. An unchanged result may satisfy later milestone,
  engineering-review, or pre-PR labels; those workflows do not rerun it merely because their stage
  began.
- Focused verification runs during implementation and review-fix work. A coherent protected slice
  runs its required protection before final closure; security, route, schema, canonical/Quran,
  persistence/transaction, and shared test-runtime risk never becomes focused-only.
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

### 2.1 Operational boundaries and evidence disposition

`MEASUREMENT_REQUIRED` and `REDUNDANT_OR_MISPLACED` are classification verdicts, not additional
execution boundaries.

| Boundary | Purpose | Execution owner | Timing |
| --- | --- | --- | --- |
| `FOCUSED` | Fastest meaningful feedback for the actual unit, feature, or fix. | Implementation/change workflow. | During each implementation task or focused review fix. |
| `PROTECTED_TRIGGER` | A named guard for access/security, route composition, schema, process, importer/Quran persistence, canonical data, shared test runtime, parity, or exporter-visible risk. | Implementation/change workflow, selected by the constitution and active plan. | Once the protected slice is coherent; repeat on the final state if a later relevant change invalidates it. |
| `FINAL_BOUNDARY` | Broad/composite safety named by the active `Testing Decision`. | Implementation/change workflow. | After the change and its fixes settle; each selected broad composite runs once. |
| `RELEASE_ONLY` | Existing explicit release acceptance composition. | Authorized release workflow. | Only at release/hotfix acceptance; never as a deferred substitute for missing feature evidence. |

These boundary labels describe operational evidence only. `TESTING_CONSTITUTION.md` and the active
plan's `Testing Decision` select which checks run; this file does not add tests or gates.

Measurement required before any later change: lane-scoped freshness after relevant changes,
migration-to-dump relaxation, reduced shared-runtime or full-Backend `pre-pr` protection,
generated-Frontend-only removal of current final gates, lane merge/rename/deletion, or required E2E
promotion. §12 owns the observation gaps. No current lane is redundant merely because it overlaps
another; only duplicate unchanged invocations of an already-selected composite are misplaced.

Unchanged protections include PostgreSQL ownership/serialization and two-major sharding, canonical
fail-not-skip preflight and separate data-tier reporting, migration upgrade/pending-model/dump-head
safety, Quran source/provenance/refusal/rollback and persistence checks, auth/access/audit/transaction
coverage, route/middleware/binding/serialization Smoke with bidirectional catalog parity, Frontend
auth/parity/typecheck/build/gate-partition/jsdom/browser boundaries, and visible failed/skipped/
preflight/shard/cleanup/environment/unknown evidence. No speed goal weakens any of them.

## 3. Backend lanes — `Backend/scripts/test-backend`

`Backend/scripts/test-backend --help` prints the authoritative usage. Every lane other than `pre-pr`
**requires** exactly one of `--build` or `--no-build`, and every lane accepts `--list-tests`
(discovery only — it starts no container and never shards) and `--results-dir PATH`. `pre-pr`
has its own flag rules; see the note under the table.

| Lane | Class | What it selects from the catalog | Command | Conditional risk coverage | Operational boundary |
| --- | --- | --- | --- | --- | --- |
| `fast` | `FOCUSED` | every `Kind=Fast` class, including fast logic that lives in Pipeline namespaces | `Backend/scripts/test-backend fast --no-build` | fast logic and test-only logic feedback | does not cover collection fixtures, canonical artifacts, migrations, or child processes |
| `feature` | `FOCUSED` | one validated `Feature` value, or one exact class/method | `Backend/scripts/test-backend feature Access --no-build`, `feature --class FULL_CLASS_NAME`, `feature --test FULL_METHOD_NAME` | one coherent feature, class, or method slice | does not substitute for separately selected Smoke or canonical evidence |
| `access` | `PROTECTED_TRIGGER` | every `Feature=Access` class | `Backend/scripts/test-backend access --no-build` | authorization, identity, Owner, permission, and Access-contract behavior | excludes Pipeline, canonical data, and Frontend tests |
| `access-db` | `PROTECTED_TRIGGER` | `Feature=Access` classes that are `Kind=Database` or carry `Schema` in `Concerns` | `Backend/scripts/test-backend access-db --no-build` | Access persistence, constraints, EF model, catalogue, grants, transactions, and audit storage | does not cover the CLI wrapper or staged migration upgrade |
| `migration` | `PROTECTED_TRIGGER` | every `Kind=Migration` class | `Backend/scripts/test-backend migration --no-build` | migration, EF model, backfill, collision/refusal, and schema-guard behavior | excludes unrelated UI and ordinary service behavior |
| `process` | `PROTECTED_TRIGGER` | every `Kind=Process` class | `Backend/scripts/test-backend process --no-build` | wrapper, exit-code, executable-directory configuration, and operator boundaries | does not duplicate lower-level migration/service permutations as child processes; the lane stays distinct because it exercises wrapper behavior |
| `smoke` | `PROTECTED_TRIGGER` | `Gate=Smoke` excluding `Kind=Canonical` — the route/composition classes, not the data tier | `Backend/scripts/test-backend smoke --no-build` | route/composition risks described in §6 | does not become a blanket check for every edit |
| `tier-b` | `FINAL_BOUNDARY` | every `Gate=TierB` class | `Backend/scripts/test-backend tier-b --no-build` | broad Backend behavior excluding Pipeline and route Smoke | is not widened with Pipeline or Smoke merely to look broader |
| `pipeline` | `PROTECTED_TRIGGER` | `Gate=Pipeline` excluding `Kind=Canonical`, optionally narrowed by `--feature` | `Backend/scripts/test-backend pipeline --feature Translations --no-build`; omit `--feature` only for a shared-pipeline selection | importer, manifest, pipeline entity/schema, shared Pipeline, Quran persistence, and reachable persistence behavior | excludes isolated authorization, general API, caching, and Frontend-only behavior |
| `canonical-data` | `PROTECTED_TRIGGER` | every `Kind=Canonical` class | `Backend/scripts/test-backend canonical-data --no-build` | canonical sources, manifests/hashes, dumps, Quran schema/persistence, and shared Pipeline behavior | excludes isolated authorization/UI behavior; missing selected resources are a gate failure, not a skip (§3.4) |
| `pre-pr` | `PROTECTED_TRIGGER` | every catalog row — the full Backend suite, run exactly once | `Backend/scripts/test-backend pre-pr` | full Backend regression coverage | its name alone never creates an extra unchanged-state run |

`pre-pr` is the one lane with different flag rules: executing it **always builds once**, so it
rejects `--no-build` and needs no `--build`; discovering it requires the exact form
`pre-pr --list-tests --no-build`.

`pre-pr` is deliberately not composed from `fast + access + tier-b + pipeline + smoke +
canonical-data`. Those lanes overlap, so a composed run would repeat tests, rebuild fixtures,
and make the measurement meaningless.

No Backend lane is `REDUNDANT_OR_MISPLACED`. The catalog intentionally cross-cuts primary
`Gate=TierB|Pipeline|Smoke` partitions with feature, Access, migration, process, fast, and canonical
selection. Overlap is not deletion or consolidation evidence. `cleanup-test-runtime` is runner
mechanics, not a selectable gate; its run-ID ownership and zero-leftover reporting remain mandatory.
No Backend gate is primarily `RELEASE_ONLY`. When an authorized release `Testing Decision` selects
`pre-pr` or `canonical-data`, their classifications and mechanics do not change.

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
expense; this has bitten repeatedly. Dump regeneration is a `PROTECTED_TRIGGER`, and no cadence
relaxation is permitted without the measurements in §12.

### 3.5 The EF pending-model check

`Backend/scripts/check-pending-model --build|--no-build` reports whether the EF Core model has
pending changes. It never adds and never applies a migration. Run it alongside the `migration`
lane whenever the EF model or schema is in scope — it is a separate command, not part of any
test lane. It is a `PROTECTED_TRIGGER`, not a substitute for the staged migration lane or dump
regeneration.

## 4. Frontend lanes — `npm run test:*`

Run these from `Frontend/quran-dashboard-ui/`. Each `test:*` lane delegates to the `test`
script, which owns the two-fork Vitest cap (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) and the
run timeout; a direct `ng test` invocation bypasses both and is not a lane. The named Angular
test configurations live in `angular.json`.

| Lane/check | Class | What it selects | Command | Conditional risk coverage | Operational boundary |
| --- | --- | --- | --- | --- | --- |
| Exact spec | `FOCUSED` | one explicitly named spec | `npm test -- --watch=false --include=src/app/.../name.spec.ts` | one implementation task or review-fix behavior | is not named gate evidence |
| Fast / unit | `FOCUSED` | the pure model/utility/data/state/cache/helper specs in the `fast` configuration | `npm run test:fast` | pure state, mapping, codec, cache, URL, and validation behavior | still incurs Angular bundle startup; excludes TestBed/component specs |
| Feature-focused | `FOCUSED` | one feature configuration | `npm run test:feature:access-admin`, `npm run test:feature:abwab`, `npm run test:feature:auth`, `npm run test:feature:dashboard`, `npm run test:feature:mushaf`, `npm run test:feature:words` | one coherent feature slice | excludes unrelated feature directories |
| Authorization | `PROTECTED_TRIGGER` | the cross-cutting auth/config/route/security specs | `npm run test:authorization` | Frontend auth fixtures/contracts, token handling, callbacks, secure origins, route posture, and production auth configuration | covers Frontend behavior, not Backend authorization |
| Composition | `PROTECTED_TRIGGER` | every component/directive spec plus the named application/overlay compositions | `npm run test:composition` | shared component harnesses, Angular rendering, overlay composition, and broad component infrastructure | is jsdom, not browser E2E; excludes pure Backend and pure utility behavior |
| Shared | `FOCUSED` | app-shell, core, shared, and environment specs | `npm run test:shared` | core/shared/routing/app-shell/environment/global test setup behavior | does not substitute for feature-specific coverage |
| Permission parity | `PROTECTED_TRIGGER` | Backend/Frontend permission-catalogue parity | `npm run check:permission-catalogue` | permission catalogue and generated Frontend output parity | does not replace authorization or consumer tests; is also a composite leg |
| Audit-action parity | `PROTECTED_TRIGGER` | Backend/Frontend audit-action type parity | `npm run check:audit-action-types` | audit-action type parity | does not replace audit/transaction behavior tests; is also a composite leg |
| Leaf type-checks | `FOCUSED` | one TypeScript project | `npm run typecheck:app`, `npm run typecheck:spec` | one TypeScript project's compilation | does not provide combined cross-project strictness |
| Combined type-check | `PROTECTED_TRIGGER` | app and spec projects, in order | `npm run typecheck` | generated DTO, config, routing, template, and cross-project types | a root `npx tsc --noEmit` checks nothing because the root config has `"files": []` and project references |
| Production build | `FINAL_BOUNDARY` | production template, bundle, and configuration build | `npm run build:verify` | production templates, bundles, and configuration | is already a `test:pre-pr` leg; do not duplicate it immediately before an unchanged composite |
| Full suite | `FINAL_BOUNDARY` | every `src/**/*.spec.ts` | `npm run test:full` (`npm test` is the same run) | broad Frontend unit/component behavior | excludes Backend behavior; do not duplicate it immediately before an unchanged composite |
| Pre-PR composite | `FINAL_BOUNDARY` | `check:permission-catalogue` → `check:audit-action-types` → `typecheck` → `build:verify` → `test:full`, in that order | `npm run test:pre-pr` | the complete Frontend composite | its stage-shaped name does not create a second run when final-state evidence is unchanged |
| Gate self-check | `PROTECTED_TRIGGER` | asserts the named configurations still select what they claim | `npm run test:gates` | shared Frontend test/config infrastructure, Angular configurations, and spec layout | remains outside `test:pre-pr` and must be reported separately when selected |
| E2E type-check | `FOCUSED` | Playwright code/config/fixture types | `npm run e2e:typecheck` | Playwright code, config, and fixture types | does not substitute for a selected browser journey |
| Browser E2E | `PLAN_SELECTED` | real-browser flows from §11 | `npm run e2e` and the §11 variants | browser-only flows and integration behavior | does not substitute for backend route smoke or lower-layer checks protecting different risks |

`npm run test:gates` runs `testing/verify-test-gates.mjs`, not Vitest. **It is not part of
`test:pre-pr`**. When the `Testing Decision` selects it, run and report it as its own step.

The exact-spec command deliberately enters through `npm test --`, so it preserves the owned two-fork
cap and timeout. It is local feedback followed by any named lane selected by the active plan;
an ad-hoc include is never promoted to final evidence. There is no generic `test:contract` lane.
For generated-model verification selected by the constitution and active plan, the available
focused mapping is exact consumer specs plus the affected feature lane, with `test:authorization`
only for auth/session/security scope; the `Testing Decision` separately selects any type-check or
final composite.

No named Frontend lane is `REDUNDANT_OR_MISPLACED`; only duplicate standalone invocations of an
unchanged composite leg are misplaced. Release selection follows its authorized plan's
`Testing Decision`.

No Frontend lane requires PostgreSQL, EF migrations, Backend startup, Docker, or importer
processes.

**Conditional scope note.** A `Testing Decision` may classify work as Backend-only when no file under
`Frontend/quran-dashboard-ui/`, committed OpenAPI document, or generated Frontend API contract is in
scope. Regenerated Frontend DTOs or changed Frontend auth fixtures/models make the work cross-stack;
the constitution and active plan then select any Frontend checks. This document supplies no automatic
Frontend mapping from either classification.

## 5. Test selection

`TESTING_CONSTITUTION.md` and the active implementation plan's `Testing Decision` are the only test
selection authorities. Use §3, §4, §6, and the nearest test README only to resolve the command,
fixture, environment, and reporting mechanics for a selected check. This transitional document does
not require a broad final suite, classify browser E2E as supplementary, or promote any lane based on
the changed file set.

## 6. The route-smoke gate

When `TESTING_CONSTITUTION.md` and the active plan's `Testing Decision` select route/composition
verification, the `smoke` lane covers API routes, request/response contracts,
authentication/authorization, middleware, model binding, serialization, startup, DI,
configuration, and shared `DbContext` composition:

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

### 6.1 The OpenAPI contract guard

`Backend/scripts/check-api-contract` is a `PROTECTED_TRIGGER`. Backend owns its mechanics. When the
constitution and active plan select it, execute it after the exporter/generator-visible surface is
coherent and before its evidence is packaged. Its risk coverage is semantic, not a `Backend/api/`
path test, and includes at least:

- controller/action routes, verbs, binding, parameters, result types, status/response metadata, and
  endpoint documentation read by the exporter;
- request/response DTO graphs wherever they live, including API-local contracts, Application
  Abstractions responses, shared paging/envelope types, nullability, enums, and inherited or
  polymorphic schemas;
- serialization names, ignore/converter/polymorphism metadata, and schema-shaping attributes;
- Swagger/OpenAPI registration, document/version/security/schema filters, JSON-schema mappings,
  exporter startup/configuration, and API/Swashbuckle tooling or package changes;
- Frontend OpenAPI generator configuration, pruning/generation scripts, and committed generated-model
  rules; and
- anything else that alters what `export-swagger`, Swashbuckle, `ng-openapi-gen`, or pruning reads,
  even when the changed input lives outside `Backend/api/`.

When selected, the command Release-builds/exports offline with permission-catalogue startup sync
disabled, regenerates the committed Swagger and Frontend models, and fails on diff. Review
intentional generated changes. This guard complements rather than replaces any separately selected
route `smoke`, focused Backend controller/contract tests, focused Frontend consumer/auth tests,
Frontend type-check/build/composite verification, or human compatibility review. Its end-to-end cost
remains `MEASUREMENT_REQUIRED`; the constitution and active plan alone decide whether to schedule it.

## 7. Build mechanics

- When a `Testing Decision` selects a Backend build, use one `--build` lane run or
  `dotnet build Backend/QuranDashboard.sln` before any selected `--no-build` lane against that state.
  Build once, then reuse it.
- When a `Testing Decision` selects the Frontend production build, run `npm run build:verify`. Do not
  prepend it to ordinary test runs because the test builder compiles its own bundle, and do not run it
  separately immediately before unchanged `test:pre-pr`, which already contains it.
- Selected build evidence must cover the cumulative state it claims: a previous build becomes stale
  after a relevant code or configuration change. Focused fixes may use focused verification while in
  motion; rerun the selected build after the fix set settles.

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

Every evidence record names the command/lane, cumulative scope or focused reason, code state, result,
skips, and any required shard/canonical/cleanup state. Apply freshness and reuse as follows:

- Focused evidence proves only its local task or fix; it never claims the cumulative final gate.
- Protected/final evidence may be classified by engineering review and packaged by PR context, and
  may satisfy a later required workflow label only while the tree and relevant environment remain
  unchanged. A new workflow label does not stale it.
- A later relevant change to code, tests, configuration, generated API artifacts, migrations/model,
  canonical sources/artifacts/dump, runner/catalog/harness, or another covered dependency stales final
  evidence. After fixes settle, reread the active plan's `Testing Decision` and rerun affected checks.
- An unrelated prose or packaging-only change creates no product-test requirement. A contract/policy
  documentation change uses its static policy checks rather than an invented lane.
- A relevant toolchain/dependency, staged-resource, migration-head, database-runtime/major, or test-
  infrastructure environment change also invalidates evidence that depended on it.
- There is no lane-scoped freshness optimization across later relevant changes. That remains blocked
  on §12 measurement.

## 10. Responsibilities by workflow

**Implementation/change workflow** — follows the constitution and active plan's `Testing Decision`,
then uses this document for the selected commands and reporting mechanics. It reports the exact
command, reason, result, skips, shards/canonical tier, cleanup, and environment.

**Implementation catalog obligations** — adding a test class requires its `test-gates.tsv` row in
the same change (§3.1); **adding or changing an API route requires the matching
`SmokeRouteCatalog` entry in the same change** (§6); and adding a migration requires regenerating
the canonical dump in the same change (§3.4). When the `Testing Decision` selects
`check-api-contract`, its §6.1 operational mechanics apply.

**`TESTING_STRATEGY.md`** — temporarily owns command vocabulary, lane mechanics, freshness, and
failure/skip evidence semantics. It does not select tests; `TESTING_CONSTITUTION.md` and the active
plan's `Testing Decision` do.

**Engineering review** — consumes supplied same-diff evidence, compares it with §§3–6, and reports
sufficient, stale, missing, failed, skipped, or unknown evidence in its verdict. It reports missing
selected Pipeline/canonical/Smoke/route-parity/contract evidence, but does not build, run tests,
invoke Test Guard, or recreate evidence.

**PR context preparation** — packages existing scope and evidence and labels gaps honestly. It does
not rerun tests, generate evidence, or independently adjudicate merge readiness. Missing or stale
evidence returns to the implementation/change workflow.

**Commit workflow** — owns Git-integrity checks and the explicitly requested Git action. It does not
run or demand builds, tests, review evidence, or deployment evidence.

**Test Guard** — owns test-code quality guidance/review. It does not select lanes, execute tests, or
decide executed-evidence sufficiency.

**Deploy smoke** — owns explicitly requested deployment preflight/runtime observations and process
lifecycle. It does not run test lanes or present runtime curls as route-Smoke/canonical evidence.

**Any other Skill** — keeps its implemented responsibility and never silently adds build/test
execution or creates an implicit evidence gate.

**Release workflow** — follows its authorized plan's `Testing Decision`. When that decision selects
the Backend `pre-pr`, `canonical-data`, or Frontend `test:pre-pr` lanes, this document supplies their
operational requirements.

## 11. Browser E2E — transitional operational guidance

A browser E2E layer exists: `Frontend/quran-dashboard-ui/playwright.config.ts` + `e2e/`,
chromium only, two sequential projects — `default` (2 workers) then `abwab` (1 worker).

```bash
cd Frontend/quran-dashboard-ui
npm run e2e                                   # headless, both projects
npm run e2e:headed                            # visible browser
npm run e2e:ui                                # Playwright UI mode
npm run e2e:typecheck                         # focused type-check of E2E code/config/fixtures
npx playwright test e2e/mushaf-reader.e2e.ts  # one flow file
```

When selected, `e2e:typecheck` provides focused feedback on E2E code, config, and fixtures. Whether a
browser journey is required follows `TESTING_CONSTITUTION.md` rule 4 and the active plan's
`Testing Decision`;
the former opt-in-only policy is retired. Browser E2E is not the backend route-smoke gate (§6) and
never substitutes for it. Specs are named `*.e2e.ts`, and that is not
cosmetic: the Angular unit-test builder globs its `include` patterns with `cwd` at the project's
`sourceRoot` (`src`), so no `angular.json` pattern can reach `e2e/` at all, while
`playwright.config.ts` matches only `/.*\.e2e\.ts$/` — so a `*.spec.ts` placed under `e2e/` is
run by **nothing** while looking like coverage. See
`Frontend/quran-dashboard-ui/testing/README.md`.

The suite boots the Angular dev server and a Playwright-owned backend in the `Testing` environment.
`e2e/run-backend.mjs` reads the local source connection from the API user secret or
`ConnectionStrings__QuranDashboardDb`, rejects non-local PostgreSQL hosts, clones that database with
`pg_dump`/`pg_restore`, and passes the clone through the backend environment. Graceful shutdown drops
the clone, including all temporary accounts and write residue. The suite requires a prior backend
build, mkcert certificates, and the PostgreSQL client commands named in `e2e/README.md`.

Every flow is read-only and every count assertion is loose, **with one named, deliberate
exception**: the Abwab specs (`abwab-structure.e2e.ts`, `abwab-operations.e2e.ts`,
`abwab-archive.e2e.ts`, `abwab-url-and-a11y.e2e.ts` added in Slice B2, plus
`abwab-global-order.e2e.ts` added by `abwab-global-order`, `abwab-tree-row-budget.e2e.ts`,
`abwab-slice-j-widths.e2e.ts`, and `abwab-relations.e2e.ts` added by slice K) write against the
disposable clone through a per-test sandbox section created over the API (`e2e/fixtures/abwab.ts`),
not the source database. Each test's section name embeds the worker index and a timestamp so workers
never collide, no test asserts a global count (only ids its own sandbox produced), and teardown
archives **every live door in the sandbox section**
— swept from the tree by `sectionId`, since flows create doors through the UI too and those ids
were never handed out by the fixture — and then deletes the now-empty section. That order is
forced, not stylistic: section delete `409`s while live doors remain. Each archive re-reads the
door's current version first, because every write resequences the scope and bumps its siblings'
`xmin`; archiving from one up-front snapshot succeeds once and then `409`s silently for the rest,
which is what used to leave live sandbox doors and undeleted sandbox sections behind. Teardown is
best-effort, so a flow that already broke does not get a second, masking failure from it. Archived
doors may remain inside the clone because the feature has no hard delete; dropping the clone removes
them. A live `e2e-sandbox-*` door or section after its test teardown remains a teardown bug.
**The Abwab specs run in their own single-worker Playwright project, not the default
2-worker one.** A `Global`-scope reorder (`abwab-global-order.e2e.ts`) resequences the whole
live-root set across the database, not just the acting test's own sandbox, so two Abwab specs in
different workers can race the same rows — measured directly: at 2 workers this produced a
wrong-result failure (not even a `409`) from another worker's teardown resequencing mid-test; at 1
worker the Abwab project passes repeatably. `e2e/README.md` owns the project split and current runtime
invariants.

## 12. Measurement-blocked cadence decisions and deferred optimizations

`docs/project-simplification-audit/data/testing-cadence-observations.tsv` is a temporary dated
observation log, not policy and not required gate evidence. During the next two or three real
features, append a row only from observed output; do not estimate or fabricate data, and do not fail
a feature because temporary audit logging was missed. Repeated invocations are separate rows with
their actual reason. The header records UTC time, change/diff identity, boundary, command,
classification/trigger, result, wall time, fix iteration, canonical tier, shard/cleanup state,
evidence reference, and notes.

A later cadence decision remains blocked until the evidence contains, or explicitly cannot obtain:

1. actual gate-invocation and review-fix frequency across two or three representative features;
2. dated `canonical-data` and Backend `pre-pr` observations with both shards/canonical accounting;
3. authorized `create-smoke-dump` timing — never regenerate a dump merely to measure it without a
   safe local source database and explicit authority;
4. end-to-end `check-api-contract` timing and generated-diff behavior;
5. focused Frontend consumer/authorization plus typecheck timing for a generated-contract change;
6. current Frontend `test:pre-pr` component timings rather than a prose estimate;
7. whether suspected stage/fix duplicate invocations actually occur and why; and
8. E2E runtime, repeat stability, auth/bootstrap readiness, residue, and historical flakiness when a
   later plan needs current operational measurements.

A single run supports no variance claim. Measurements may inform a later bounded plan; they do not
authorize lane-scoped freshness, lane deletion/merge/rename, migration/dump relaxation, reduced
shared-runtime/full-Backend protection, generated-only removal of current Frontend final gates, CI,
or a runtime/percentage target.

Test implementation and fixture optimizations remain separate, explicitly requested work: sharing
the enriched-morphology artifact load, safely reducing repeated canonical imports, and optional test
consolidation. Do not perform them as a side effect of running, classifying, or packaging evidence.
