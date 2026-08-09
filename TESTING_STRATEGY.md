# Testing Strategy V2 — Risk-Based, Focused Verification

## 1. Purpose and authority

This file is the single source of truth for **test selection, verification depth, execution
lanes, slow data-pipeline triggers, and focused / protected / final / release boundaries**
across the whole monorepo (Backend + Frontend). Each agent's native root and area
entrypoints route here when test selection, execution, or reporting is triggered; they do not
carry a second test policy. The command vocabulary and cumulative trigger algorithm live in this
file and nowhere else.

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

**Gate evidence is selected by lane, not by hand-written filter.** Backend lanes are arguments to
`Backend/scripts/test-backend`, which resolves each lane against the class catalog at
`Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv` and builds the
`--filter` itself. Frontend lanes are `npm run test:*` scripts backed by named Angular test
configurations in `Frontend/quran-dashboard-ui/angular.json`. Exact Backend selectors and the
timeout/fork-capped Frontend command in §4 are valid focused local feedback, but they are not named
final-gate evidence. Do not hand-write a `FullyQualifiedName` filter, use bare `ng test`, or present
an ad-hoc `--include` glob as a gate: a lane is reproducible, catalog-validated, and reportable by
name, and a hand-written filter is none of those.

**Tier vocabulary.** The former Tier A–E labels are superseded by the lanes in §3 and §4 and
the cumulative trigger algorithm in §5. One survives as a name: `tier-b` is the Backend no-pipeline
broad lane.

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
  selected by §5. An unchanged result may satisfy later milestone, engineering-review, or pre-PR
  labels; those workflows do not rerun it merely because their stage began.
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
| `PROTECTED_TRIGGER` | A named guard for access/security, route composition, schema, process, importer/Quran persistence, canonical data, shared test runtime, parity, or exporter-visible risk. | Implementation/change workflow, selected by this strategy. | Once the protected slice is coherent; repeat on the final state if a later relevant change invalidates it. |
| `FINAL_BOUNDARY` | Broad/composite safety selected from the union of every trigger in the cumulative final diff. | Implementation/change workflow. | After the change and its fixes settle; each selected broad composite runs once. |
| `RELEASE_ONLY` | Existing explicit release acceptance composition. | Authorized release workflow. | Only at release/hotfix acceptance; never as a deferred substitute for missing feature evidence. |

Safe now: use these four boundaries, reuse same-state evidence across non-executing stages, select
the final union from the cumulative diff, and schedule `check-api-contract` as §6.1 defines. This is
an ownership/selection overlay, not a reduction of any current protection-bearing trigger.

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

| Lane | Class | What it selects from the catalog | Command | Run when | Must not run when |
| --- | --- | --- | --- | --- | --- |
| `fast` | `FOCUSED` | every `Kind=Fast` class, including fast logic that lives in Pipeline namespaces | `Backend/scripts/test-backend fast --no-build` | small logic iterations; fast regression after test-only logic changes | never for a case that needs a collection fixture, canonical artifact, migration, or child process — those are not `Kind=Fast` |
| `feature` | `FOCUSED` | one validated `Feature` value, or one exact class/method | `Backend/scripts/test-backend feature Access --no-build`, `feature --class FULL_CLASS_NAME`, `feature --test FULL_METHOD_NAME` | implementation and coherent feature-slice feedback | never as a substitute for separately triggered Smoke or canonical evidence |
| `access` | `PROTECTED_TRIGGER` | every `Feature=Access` class | `Backend/scripts/test-backend access --no-build` | coherent authorization, identity, Owner, permission, or Access-contract slice | never to pull in Pipeline, canonical data, or Frontend tests |
| `access-db` | `PROTECTED_TRIGGER` | `Feature=Access` classes that are `Kind=Database` or carry `Schema` in `Concerns` | `Backend/scripts/test-backend access-db --no-build` | Access persistence, constraint, EF model, catalogue, grant, transaction, or audit-storage change | does not cover the CLI wrapper or the staged migration upgrade |
| `migration` | `PROTECTED_TRIGGER` | every `Kind=Migration` class | `Backend/scripts/test-backend migration --no-build` | migration, EF model, backfill, collision/refusal, or schema-guard work | never after unrelated UI or ordinary service edits |
| `process` | `PROTECTED_TRIGGER` | every `Kind=Process` class | `Backend/scripts/test-backend process --no-build` | wrapper, exit-code, executable-directory configuration, or operator-boundary change | never to duplicate lower-level migration/service permutations as child processes; §5 preserves the AccessAdmin wrapper union |
| `smoke` | `PROTECTED_TRIGGER` | `Gate=Smoke` excluding `Kind=Canonical` — the route/composition classes, not the data tier | `Backend/scripts/test-backend smoke --no-build` | route, contract, auth, middleware, model binding, serialization, startup, DI, configuration, or shared `DbContext` composition change (§6) | not after every edit |
| `tier-b` | `FINAL_BOUNDARY` | every `Gate=TierB` class | `Backend/scripts/test-backend tier-b --no-build` | ordinary Backend cumulative final diff and the existing milestone/review/pre-PR requirements | never widened with Pipeline or Smoke merely to look broader |
| `pipeline` | `PROTECTED_TRIGGER` | `Gate=Pipeline` excluding `Kind=Canonical`, optionally narrowed by `--feature` | `Backend/scripts/test-backend pipeline --feature Translations --no-build`; omit `--feature` only for a shared-pipeline trigger | importer, manifest handling, pipeline entity/schema, shared Pipeline, Quran persistence, or reachable persistence change | never for isolated authorization, general API, caching, or Frontend-only changes |
| `canonical-data` | `PROTECTED_TRIGGER` | every `Kind=Canonical` class | `Backend/scripts/test-backend canonical-data --no-build` | canonical source, manifest/hash, dump, Quran schema/persistence, shared Pipeline, or release acceptance | never for isolated authorization/UI work; missing required resources are a gate failure, not a skip (§3.4) |
| `pre-pr` | `PROTECTED_TRIGGER` | every catalog row — the full Backend suite, run exactly once | `Backend/scripts/test-backend pre-pr` | shared test/runtime infrastructure, canonical-source/full-regression acceptance, release, or another current explicit full-suite trigger | its name alone never creates an extra unchanged-state run; ordinary isolated authorization remains `access` + `smoke` + `tier-b` and excludes Pipeline/canonical |

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
No Backend gate is primarily `RELEASE_ONLY`; release composes the protected `pre-pr` and
`canonical-data` gates without changing their protection-bearing feature triggers.

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

| Lane/check | Class | What it selects | Command | Run when | Must not run when |
| --- | --- | --- | --- | --- | --- |
| Exact spec | `FOCUSED` | one explicitly named spec | `npm test -- --watch=false --include=src/app/.../name.spec.ts` | fastest meaningful local feedback for one implementation task or review fix | never cite it as named final-gate evidence |
| Fast / unit | `FOCUSED` | the pure model/utility/data/state/cache/helper specs in the `fast` configuration | `npm run test:fast` | pure state, mapping, codec, cache, URL, or validation work | do not assume it avoids Angular bundle startup; do not add a TestBed/component spec to it |
| Feature-focused | `FOCUSED` | one feature configuration | `npm run test:feature:access-admin`, `npm run test:feature:abwab`, `npm run test:feature:auth`, `npm run test:feature:dashboard`, `npm run test:feature:mushaf`, `npm run test:feature:words` | feature implementation and coherent feature-slice verification | do not run unrelated feature directories |
| Authorization | `PROTECTED_TRIGGER` | the cross-cutting auth/config/route/security specs | `npm run test:authorization` | Frontend auth fixtures/contracts, token handling, callback, secure-origin behavior, route posture, or production auth config changed | a Backend-only authorization change needs no Frontend test unless a Frontend or generated contract changed |
| Composition | `PROTECTED_TRIGGER` | every component/directive spec plus the named application/overlay compositions | `npm run test:composition` | shared component harness, Angular rendering, overlay composition, or broad component infrastructure changed | not after pure Backend or pure utility changes; this is jsdom, not a browser — never call it E2E |
| Shared | `FOCUSED` | app-shell, core, shared, and environment specs | `npm run test:shared` | core/shared/routing/app-shell/environment/global test setup changed | not a substitute for the affected feature lane |
| Permission parity | `PROTECTED_TRIGGER` | Backend/Frontend permission-catalogue parity | `npm run check:permission-catalogue` | permission catalogue or its generated Frontend output changed; also a leg of the final composite | not a replacement for authorization or consumer tests |
| Audit-action parity | `PROTECTED_TRIGGER` | Backend/Frontend audit-action type parity | `npm run check:audit-action-types` | an audit-action type surface changed; also a leg of the final composite | not a replacement for audit/transaction behavior tests |
| Leaf type-checks | `FOCUSED` | one TypeScript project | `npm run typecheck:app`, `npm run typecheck:spec` | local compilation feedback when only one project is implicated | not a substitute for combined strictness when a cross-project/generated/config change triggers it |
| Combined type-check | `PROTECTED_TRIGGER` | app and spec projects, in order | `npm run typecheck` | generated DTO, config, routing, template, or cross-project type change; also a leg of the final composite | never cite a root `npx tsc --noEmit`: the root `tsconfig.json` is `"files": []` plus project references and `--noEmit` does not follow references, so it type-checks nothing |
| Production build | `FINAL_BOUNDARY` | production template, bundle, and configuration build | `npm run build:verify` | standalone only when the final composite is not selected | do not run it immediately before an unchanged `test:pre-pr` that already contains it |
| Full suite | `FINAL_BOUNDARY` | every `src/**/*.spec.ts` | `npm run test:full` (`npm test` is the same run) | when a broad Frontend gate is required on its own | not for Backend-only changes; do not run it immediately before unchanged `test:pre-pr` |
| Pre-PR composite | `FINAL_BOUNDARY` | `check:permission-catalogue` → `check:audit-action-types` → `typecheck` → `build:verify` → `test:full`, in that order | `npm run test:pre-pr` | current review/pre-PR/release requirements for a Frontend source/spec/config/generated cumulative diff | its stage-shaped name does not create a second run when final-state evidence is unchanged |
| Gate self-check | `PROTECTED_TRIGGER` | asserts the named configurations still select what they claim | `npm run test:gates` | shared Frontend test/config infrastructure, `angular.json` test configurations, or spec-layout changes | it remains outside `test:pre-pr` and must be reported separately |
| E2E type-check | `FOCUSED` | Playwright code/config/fixture types | `npm run e2e:typecheck` | E2E code, config, or fixtures changed | it does not promote browser E2E to required status |
| Browser E2E | `MEASUREMENT_REQUIRED` | opt-in real-browser flows from §11 | `npm run e2e` and the §11 variants | supplementary evidence when explicitly chosen | not a required final or release gate without the §12 stability/auth/runtime evidence |

`npm run test:gates` runs `testing/verify-test-gates.mjs`, not Vitest. **It is not part of
`test:pre-pr`** — when you change the configurations or move specs between areas, run it as its
own step and report it separately.

The exact-spec command deliberately enters through `npm test --`, so it preserves the owned two-fork
cap and timeout. It is local feedback followed by the affected named lane whenever §5 requires one;
an ad-hoc include is never promoted to final evidence. There is no generic `test:contract` lane:
generated-model changes use exact consumer specs plus the affected feature lane, add
`test:authorization` only for auth/session/security scope, and retain the triggered type-check/final
gate until measured evidence authorizes something narrower.

No current required Frontend gate is primarily `RELEASE_ONLY`; release reuses the final
`test:pre-pr` composition. No named Frontend lane is `REDUNDANT_OR_MISPLACED`; only duplicate
standalone invocations of an unchanged composite leg are misplaced.

No Frontend lane requires PostgreSQL, EF migrations, Backend startup, Docker, or importer
processes.

**Backend-only rule.** Run no Frontend tests for a Backend-only change when no file under
`Frontend/quran-dashboard-ui/`, no committed OpenAPI document, and no generated Frontend API
contract changed. If a Backend contract change regenerates Frontend DTOs or changes Frontend
auth fixtures/models, run exact affected consumer specs and the affected feature lane, add
`test:authorization` when auth/session/security is in scope, and run the required type-check; run
`test:pre-pr` only because Frontend files then changed, not merely because the Backend changed.

## 5. Cumulative-final-diff gate selection

The implementation/change workflow applies this algorithm when a protected slice is coherent and
again when the feature/change or a set of review fixes is complete. Engineering review classifies
the resulting evidence; it does not execute the algorithm's commands.

1. **Fix the comparison base.** Compare the feature/change base (normally its merge base with
   `dev`) with the complete current worktree. Include committed, staged, unstaged, generated, and
   in-scope untracked files. A last commit, last finding, or last fix is not the base.
2. **Classify semantics, not extensions alone.** Map every changed path and behavior against all of
   these families: Backend logic/compilation; auth/authorization/identity/Owner/permissions/audit;
   routes/metadata/middleware/binding/contracts/serialization/startup/DI/configuration/shared
   `DbContext`; exporter/generator-visible inputs from §6.1; migration/schema/model/backfill/
   collision/refusal/process wrapper; importer/manifests/source handling/Pipeline/Quran persistence/
   canonical source/artifact/hash/dump/transactions; Backend test catalog/resources/fixtures/
   collections/PostgreSQL runtime/locks/shards/runner/build/output/cleanup; Frontend feature/shared/
   auth/composition/routing/template/environment/generated/config/spec-layout scope; and browser-only
   geometry/focus/history/RTL-input/real-network behavior.
3. **Union; never subtract.** Add every focused, protected, final, and release-only requirement
   triggered anywhere in the cumulative diff. A later fix may add a gate; it cannot erase a gate
   triggered by an earlier part of the still-present diff.
4. **Apply every protected mapping below.** Focused feedback does not replace a named protected
   gate. When scope is ambiguous, retain the stronger current trigger and request an owner decision.
5. **Add the final broad boundary.** An ordinary Backend cumulative diff selects `tier-b`; shared
   test/runtime infrastructure, canonical-source acceptance, release, or another current explicit
   full-regression trigger selects Backend `pre-pr`. A Frontend source/spec/config/generated diff
   selects `test:pre-pr` under current policy. Documentation-only changes do not gain product gates.
6. **Collapse only exact composite duplication.** When `test:pre-pr` is selected for the same final
   state, do not separately rerun its unchanged parity/typecheck/build/full-test legs immediately
   before it. Do not infer equivalent collapse across overlapping Backend selectors.
7. **Execute and record reality.** Record exact command, selection reason, result, skips, Backend
   shards/canonical-tier status where applicable, cleanup, and environment state. Missing resources,
   failed preflight/shard/cleanup, unexpected skips, and unknown results cannot close the gate.
8. **Recompute after any fix set.** Run focused/protected verification while fixes are in motion;
   after the last fix, restart at step 1 and execute the final union from the whole remaining diff.

### Authoritative trigger mappings

- **Pure Backend logic:** exact class/method or `fast`, then the affected feature lane and final
  `tier-b`; no Smoke, Pipeline, canonical, or Frontend gate without an independent trigger.
- **Access service/contract:** exact Access feedback followed by `access` + `smoke` + final `tier-b`.
  Add route-catalog and API-contract guards when their route/exporter triggers apply. Frontend
  authorization is selected only when Frontend/generated auth scope changed.
- **Access persistence/constraints/catalogue/grants/audit storage:** exact/`access-db`, affected
  `access`, `smoke`, and final `tier-b`; add the migration/model requirements below only when schema
  or EF model is actually in scope.
- **Migration/schema/EF model/backfill:** exact migration feedback, `migration`, relevant
  `access-db`/`access`, `check-pending-model`, same-change `create-smoke-dump --yes`, `smoke`, and final
  `tier-b`. Preserve the empty-database staged-upgrade path; add Pipeline/canonical only for an
  independent shared Quran persistence trigger.
- **AccessAdmin wrapper/operator boundary:** exact process feedback followed by `process` + `access`
  + `smoke` + final `tier-b`. Lower-level Access tests are not process-boundary evidence.
- **API route/auth/middleware/binding/serialization/startup/DI:** affected API-family feedback,
  `smoke`, and same-change `SmokeRouteCatalog` parity, plus `check-api-contract` when exporter-visible
  and final `tier-b`. Canonical Smoke Data remains separate unless independently triggered.
- **One isolated importer/Pipeline family:** exact/fast feedback, named `pipeline --feature
  FEATURE_KEY`, and final `tier-b`; add Smoke only for API composition and do not pull unrelated
  Pipeline/canonical scope.
- **Shared Pipeline or Quran persistence:** representative/named feedback, full affected `pipeline`,
  `canonical-data` whenever the existing shared/Quran trigger applies, required schema/transaction
  checks, and final `tier-b`; no Frontend gate without a real Frontend/generated diff.
- **Canonical source/manifest/hash/dump:** exact canonical feedback followed at the feature/change
  boundary by full `pipeline` + `canonical-data` + `smoke` + full Backend `pre-pr`. Report both
  Backend shards and canonical-tier/preflight status; release is not the backstop.
- **Shared Backend test/runtime/catalog/shard infrastructure:** exact contract plus a pilot feature,
  `access` + `smoke` + `tier-b` + representative `pipeline`, and full Backend `pre-pr`; PostgreSQL
  processes remain sequential and canonical status remains explicit.
- **Frontend utility/state:** exact spec or `test:fast`, affected feature/shared lane, and final
  `test:pre-pr`; no Backend gate.
- **Frontend feature component:** exact spec, affected feature, `test:composition` when implicated,
  and final `test:pre-pr`; no unrelated Backend/Pipeline gate.
- **Frontend auth/config:** exact spec, `test:authorization`, affected feature/shared lane,
  `typecheck` when implicated, and final `test:pre-pr`; Backend only when it also changed.
- **Frontend test/config/spec layout:** affected focused lane plus `test:gates`, then the current
  final `test:pre-pr`; browser E2E remains supplementary.
- **Exporter-visible contract:** `check-api-contract`, generated-diff review, exact affected consumer
  specs, affected feature or authorization lane, `typecheck`, route Smoke when API composition
  changed, and the current final Backend/Frontend gates. No generic `test:contract` lane exists.
- **Browser-only behavior:** preserve affected unit/feature evidence and label any explicitly chosen
  targeted E2E run supplementary. jsdom does not prove browser geometry, and this strategy does not
  promote E2E.
- **Backend-only with no generated/Frontend diff:** Backend gates only; every Frontend test/build is
  excluded.

Current milestone, formal-review, pre-PR, and release requirements remain. An unchanged protected or
final result may satisfy the later label when its tree and relevant environment are identical; a new
stage name does not create a rerun. Release acceptance remains full Backend `pre-pr` (two shards) +
`canonical-data` + Frontend `test:pre-pr`, with staged resources and complete skip/shard accounting.

## 6. The route-smoke gate

**A change that alters API routes, request/response contracts, authentication/authorization,
middleware, model binding, serialization, startup, DI, configuration, or shared `DbContext`
composition REQUIRES the `smoke` lane**, regardless of which path supplied that behavior and in
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

### 6.1 The OpenAPI contract guard

`Backend/scripts/check-api-contract` is a `PROTECTED_TRIGGER`. Backend owns its mechanics; the
implementation/change workflow runs it after the exporter/generator-visible surface is coherent and
before final evidence is packaged. Its trigger is semantic, not a `Backend/api/` path test, and
includes at least:

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

The command continues to Release-build/export offline with permission-catalogue startup sync
disabled, regenerate the committed Swagger and Frontend models, and fail on diff. Review intentional
generated changes. This guard complements rather than replaces route `smoke`, focused Backend
controller/contract tests, focused Frontend consumer/auth tests, Frontend `typecheck`/build/final
verification, or human compatibility review. Its end-to-end cost remains `MEASUREMENT_REQUIRED`, but
its scheduling is required now because unscheduled regeneration has already allowed stale committed
contract output.

## 7. Build requirements

- Backend compilation-affecting changes REQUIRE one `--build` lane run (or
  `dotnet build Backend/QuranDashboard.sln`) before any `--no-build` lane against that state.
  Build once, then reuse it.
- Frontend template, routing, configuration, or bundle-affecting changes REQUIRE
  `npm run build:verify` at the selected final boundary. Do not prepend a build to ordinary test
  runs — the test builder compiles its own bundle — and do not run it separately immediately before
  unchanged `test:pre-pr`, which already contains it.
- Before final PR completion, every changed application MUST have fresh build evidence from the
  implementation/change workflow.
- Builds MUST cover the **cumulative final state**: a previously successful build is stale once any
  relevant code or configuration changes. Focused fixes may use focused verification while they are
  in motion; rebuild once after the fix set settles.

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
  evidence. After fixes settle, recompute §5 from the whole diff and run the new union once.
- An unrelated prose or packaging-only change creates no product-test requirement. A contract/policy
  documentation change uses its static policy checks rather than an invented lane.
- A relevant toolchain/dependency, staged-resource, migration-head, database-runtime/major, or test-
  infrastructure environment change also invalidates evidence that depended on it.
- There is no lane-scoped freshness optimization across later relevant changes. That remains blocked
  on §12 measurement.

## 10. Responsibilities by workflow

**Implementation/change workflow** — selects the narrowest meaningful focused verification, runs
protected gates when their real triggers fire, derives the final union from §5, executes each
selected final composite once against the cumulative state, and reports the exact command, reason,
result, skips, shards/canonical tier, cleanup, and environment. It MUST NOT substitute an unrelated
broad lane for focused coverage or run broad gates merely because a phase/stage ended.

**Implementation catalog obligations** — adding a test class requires its `test-gates.tsv` row in
the same change (§3.1); **adding or changing an API route requires the matching
`SmokeRouteCatalog` entry in the same change** (§6); adding a migration requires regenerating
the canonical dump in the same change (§3.4); and exporter-visible scope requires
`check-api-contract` under §6.1.

**`TESTING_STRATEGY.md`** — owns command vocabulary, classifications, selection triggers,
cumulative-diff mapping, freshness/reuse, and failure/skip evidence semantics. No Skill or README
may create a second selection policy or weaken a protected trigger.

**Engineering review** — consumes supplied same-diff evidence, compares it with §§3–6, and reports
sufficient, stale, missing, failed, skipped, or unknown evidence in its verdict. It reports missing
Pipeline/canonical/Smoke/route-parity/contract evidence when triggered, but does not build, run tests,
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

**Release workflow** — runs the full Backend `pre-pr` lane (two shards), `canonical-data`, and
the Frontend `test:pre-pr` lane on a machine with `resources/import-sources/` and the canonical
dump staged, and accounts for every skipped test. Release is not a deferred substitute for a
missing feature/change protection.

## 11. Browser E2E — opt-in, never a required gate

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

`e2e:typecheck` is `FOCUSED` feedback when E2E code/config/fixtures change; it does not promote a
browser run. Browser E2E is `MEASUREMENT_REQUIRED` and remains an **opt-in local gate, not part of
any required lane**: it is not required pre-PR and
not required for release, and an E2E run MAY be reported only as supplementary evidence, which
it MUST then be labelled as. It is **not** the backend route-smoke gate (§6) and never
substitutes for it. Promoting it into a required lane is a separate decision, to be made only
after current auth/bootstrap readiness, runtime, repeat stability, residue, and historical
flakiness have been observed and recorded under §12. Specs are named `*.e2e.ts`, and that is not
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
8. E2E runtime, repeat stability, auth/bootstrap readiness, residue, and historical flakiness only if
   the owner later considers changing its opt-in status.

A single run supports no variance claim. Measurements may inform a later bounded plan; they do not
authorize lane-scoped freshness, lane deletion/merge/rename, migration/dump relaxation, reduced
shared-runtime/full-Backend protection, generated-only removal of current Frontend final gates, E2E
promotion, CI, or a runtime/percentage target.

Test implementation and fixture optimizations remain separate, explicitly requested work: sharing
the enriched-morphology artifact load, safely reducing repeated canonical imports, and optional test
consolidation. Do not perform them as a side effect of running, classifying, or packaging evidence.
