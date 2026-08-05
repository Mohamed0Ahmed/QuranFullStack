# Test Runtime Optimization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use
> `superpowers:subagent-driven-development` (recommended) or
> `superpowers:executing-plans` to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substantially shorten implementation feedback loops while retaining every
meaningful security, migration, database, CLI, startup, rollback, and Quran-data
guarantee.

**Architecture:** Stable scripts will select validated, scope-aware test lanes. Ordinary
Backend integration fixtures will share one explicitly project-owned PostgreSQL 16
Testcontainer per test process, with a cross-process operating-system lock and an isolated
database per collection. Migration-upgrade and canonical-data paths remain isolated from
the migrated-template fast path.

**Tech Stack:** .NET 10, xUnit 2, EF Core, Npgsql, Testcontainers for .NET 4.4,
PostgreSQL 16/18, Bash, Angular 20, Vitest 3, TypeScript 5.9.

**Branch:** `feature/security-authorization-permissions`. Keeping this prerequisite on the
Authorization branch is intentional. Do not branch from `dev` for this work and do not
start Authorization Phase 3 until this plan has been implemented, verified, and formally
reviewed.

**Artifact status:** This is the user-directed implementation plan. It is a temporary,
feature-scoped working artifact and remains available through implementation and
engineering review. Its later deletion follows the repository planning-artifact lifecycle;
deletion is not part of this plan-only task.

## Global Constraints

- Do not change Authorization production behavior.
- Do not create or modify migrations.
- Do not change Quran source data, source packages, manifests, hashes, or importer behavior.
- Do not weaken security, authorization, migration-upgrade, collision/refusal,
  constraint, schema-drift, rollback/atomicity, append-only audit, CLI, startup/DI,
  configuration, or Quran-data guarantees.
- During implementation, run the narrowest meaningful test first, build once after
  compilation changes, and use `--no-build` for subsequent focused iterations.
- Run each broad Backend and Frontend post-change gate once, only after all relevant
  implementation phases are complete.
- Never pipe long-running test output into `tail`; keep progress visible and use bounded
  hang timeouts.
- Never run two project-owned PostgreSQL test containers concurrently.
- Never target an external database unless a centralized guard proves it is explicitly
  disposable test infrastructure.
- Never delete a test merely because it is slow. Every deletion or consolidation requires
  documented replacement coverage.
- Preserve the working-tree state belonging to the user. Each implementation phase stages
  and commits only its own reviewed files when the user later authorizes commits.

---

## 1. Audit Baseline and Planning Truth

The completed audit is the baseline for this plan:

| Measure | Baseline |
| --- | ---: |
| Backend full-suite wall time | 479.46 seconds |
| Backend runtime cases | 1,958 |
| Backend PostgreSQL starts | 22 |
| Collection-owned PostgreSQL fixtures | 21 |
| Extra nested Abwab PostgreSQL fixture | 1 |
| Peak simultaneous PostgreSQL containers | 8 |
| Backend peak test-run RSS | 1,464,508 KB |
| Frontend full-suite wall time | 306.50 seconds |
| Frontend test files | 207 |
| Frontend runtime cases | 2,696 |
| Frontend bundle generation | 34.947 seconds |
| Frontend test-body time | 92.10 seconds |

The Frontend run therefore spent about 214.4 seconds—roughly 70% of wall time—outside
reported test bodies. Deleting trivial assertions cannot materially improve the full
suite; avoiding repeated full bundle/setup cycles during implementation is the main gain.

The Backend suite currently has no traits, categories, runsettings, stable gate script, or
complete gate validator. Static source reconciliation found 246 test classes in 24
test-bearing namespaces. Two further namespaces, `TestSupport.Http` and
`TestSupport.Logging`, currently contain helpers only. The existing three-way namespace
partition happens to cover 114 Tier B classes, 122 Pipeline classes, and 10 Smoke classes,
but those counts are audit evidence rather than living invariants.

`QuranDashboard.Tests.TestSupport.Access` contains two executable classes and six fixed
Facts. It is absent from the existing focused Access command and Backend test README; the
subtractive Tier B filter includes it only accidentally. Static source contains 21
`PostgreSqlBuilder` fixture types—20 PostgreSQL 16 and one PostgreSQL 18—while the measured
full run started PostgreSQL 22 times because `AbwabTreeReadTests` constructs one nested
fixture. Implementation must instrument actual starts rather than treating source-builder
counts and runtime starts as interchangeable.

The Frontend has no stable fast, feature, authorization, component-integration, shared
infrastructure, type-check, or pre-PR commands. The current `npm test` fork cap is
load-bearing and must remain:

```text
VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2
```

No standing test counts or durations will be added to `TESTING_STRATEGY.md` or a living
README. Counts and timings belong only in dated run evidence. The numbers above are the
dated baseline for this implementation plan.

## 2. Backend Test Taxonomy and Execution Gates

### 2.1 One explicit class taxonomy

Use one checked-in, tab-separated class catalog rather than copied namespace filters or
hundreds of repetitive attributes:

```text
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv
```

This is the repository's stable manifest implementation. Class-level xUnit traits were
evaluated but are not the recommended first step: annotating 246 files would create broad
test churn without adding information beyond one exact validated row per class. The
catalog still yields stable category filters and does not infer execution lanes from
fragile namespace substrings.

Each test-bearing class has exactly one row:

```text
FullyQualifiedClassName    Feature    Kind    Gate    Concerns
```

The axes are:

| Axis | Values | Rule |
| --- | --- | --- |
| `Feature` | `Abwab`, `Access`, `ApiBehavior`, `Health`, `Middleware`, `RateLimiting`, `FullI3rab`, `FoundationImport`, `MushafReader`, `Mutashabihat`, `Navigation`, `Tafsirs`, `Translations`, `Words`, `WordsDisplay`, `WordsMorphology`, `WordsMorphologyEnriched`, `WordsMorphologyExplorers`, `WordsRoots`, `WordsSimpleI3rab`, `WordsWordTypes`, `Smoke` | Exactly one |
| `Kind` | `Fast`, `Database`, `Migration`, `Process`, `Canonical` | Exactly one; a collection fixture sets the minimum cost for the whole class |
| `Gate` | `TierB`, `Pipeline`, `Smoke` | Exactly one primary regression gate |
| `Concerns` | Validated comma-separated values such as `Schema`, `Authorization`, `Startup`, `Cli` | Optional and repeatable |

The initial assignment is deterministic:

- `Gate=TierB` for the existing no-pipeline set, including `TestSupport.Access`;
- `Gate=Pipeline` for the ten existing Pipeline namespaces;
- `Gate=Smoke` for the ten existing Smoke classes;
- `Kind=Migration` only for `AccessMigrationPathTests`;
- `Kind=Process` only for `AccessAdminCommandTests`;
- `Kind=Canonical` for the ten source/canonical classes below;
- `Kind=Database` for every other class carrying an xUnit collection fixture;
- `Kind=Fast` for every remaining class.

The canonical class list is exact:

```text
QuranDashboard.Tests.Quran.Import.ForceReloadTests
QuranDashboard.Tests.Quran.Import.ImlaeiCleanKeyImportTests
QuranDashboard.Tests.Quran.Import.ImportCountsTests
QuranDashboard.Tests.Quran.Import.ImportReconstructionTests
QuranDashboard.Tests.Quran.Import.ReRunGuardTests
QuranDashboard.Tests.Quran.Import.ValidationFailureTests
QuranDashboard.Tests.Quran.Import.ValidationReportTests
QuranDashboard.Tests.Quran.WordsDisplay.DisplayWordsRealImportIdentityLinksTests
QuranDashboard.Tests.Quran.WordsMorphologyEnriched.EnrichedMorphologyArtifactTests
QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests
```

This assignment deliberately permits useful overlap between execution lanes. For example,
pipeline-namespace classes with `Kind=Fast` run in both the Fast lane and their Pipeline
feature lane. A method inside a database/process class cannot be advertised as Fast while
xUnit still constructs that class's resource-owning collection fixture.

The complete Access feature is also exact:

```text
Fast:
  AbwabPermissionCatalogueTests
  AccessSchemaModelTests (Concerns=Schema)
  EmailIdentityNormalizerTests
  RoleClaimsTransformationTests
  EmailIdentityContractTests
  TestAccessPersonasContractTests
Database:
  AccessAuditEventPersistenceTests
  AccessMeEndpointTests
  AccessRolesTests
  AuthorizationPolicyRegistrationTests
  CachedUserRoleResolverTests
  EmailIdentityPreflightTests
  PermissionCatalogueSynchronizerTests
  UserPermissionPersistenceTests
  UserProvisioningServiceTests
Migration:
  AccessMigrationPathTests
Process:
  AccessAdminCommandTests
```

Phase 4 refines that current assignment without dropping a case:

- keep only staged-from-previous-migration, backfill, collision/refusal, and final
  constraint cases in `AccessMigrationPathTests` (`Kind=Migration`);
- move head-migrated live catalogue/schema-drift cases, including all 15 current mutation
  rows, to `AccessSchemaDriftTests` (`Kind=Database`, `Concerns=Schema`);
- move controlled operational/CLI behavior to `AccessAdminCommandTests`
  (`Kind=Process`);
- place only those process-global classes in one nonparallel Access collection.

The head-schema drift class may clone the migrated template for every case because its
starting invariant is migration head; it must still execute every real PostgreSQL mutation
and `pg_catalog` inspection. The staged migration class may never use that template.

`TestSupport.Http` and `TestSupport.Logging` remain helper-only and receive no catalog row
until they contain a real test class.

### 2.2 Catalog and resource validation

Add:

```text
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/TestGateCatalog.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/TestGateCatalogTests.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-resources.tsv
```

`test-resources.tsv` maps every xUnit collection to its resource class, parallel policy,
and one explicit state policy:

```text
ImmutableSeed | ResetPerTest | UniqueKeyIsolation | FreshLeasePerCase
```

The test project copies both catalogs to its output. The Bash runner reads only
`test-gates.tsv`; documentation never repeats generated filter strings.

`TestGateCatalogTests` reflects the compiled assembly and proves:

1. every class containing a `Fact` or `Theory` has exactly one catalog row;
2. every row names a real test class;
3. `Feature`, `Kind`, `Gate`, and `Concerns` use only allowed values;
4. every `[Collection]` name exists in `test-resources.tsv`;
5. every database collection declares one allowed state policy;
6. a `Fast` class does not carry a PostgreSQL, migration, process, or canonical resource;
7. `Api.Access` and `TestSupport.Access` both map to `Feature=Access`;
8. all ten Pipeline namespaces map to `Gate=Pipeline`;
9. every Smoke class maps to `Gate=Smoke`;
10. `SmokeDataReadTests` is canonical while the other nine Smoke classes are route Smoke;
11. the class sets selected by `TierB`, `Pipeline`, and `Smoke` are pairwise disjoint and
    their union equals every discovered class;
12. the Full Backend gate uses discovery without a filter.

The script accepts only exact feature keys or discovered class/method names. It never
concatenates an arbitrary caller-supplied VSTest expression. Audit counts remain in dated
evidence; neither validator freezes 246, 1,958, 21, 22, or any duration as a repository
invariant.

For a class row, the runner emits a parenthesized
`FullyQualifiedName~FULLY_QUALIFIED_CLASS_NAME.` term and ORs only catalog-selected terms.
For one method it uses exact `FullyQualifiedName=FULLY_QUALIFIED_METHOD_NAME`. The trailing
class dot prevents `WordsMorphology` from accidentally matching
`WordsMorphologyEnriched`. `--list-tests` output is checked against the chosen catalog rows
before a new public gate is accepted.

### 2.3 Backend lane contract

All runtime ranges are initial selection guidance, not promises. Phase 1 records a focused
pre-change duration for each new lane before optimization, and Phase 9 replaces estimates
with dated evidence.

| Lane | Purpose and inclusion | Command | Build | PostgreSQL / migrations / process | Parallelism | Approximate runtime | Run when | Must not run when |
| --- | --- | --- | --- | --- | --- | ---: | --- | --- |
| Fast / Unit | Every `Kind=Fast` class, including fast logic that lives in Pipeline namespaces | `Backend/scripts/test-backend fast --no-build` | Once before the first run if compilation changed | None | xUnit collection defaults; no resource collection is selected | 5–30 s | Small logic iterations and fast regression after test-only logic changes | Never include a collection fixture, canonical artifact, migration, or process class |
| Feature-focused | One exact discovered class/method or one validated `Feature` value | `Backend/scripts/test-backend feature Access --no-build`; `--class` and `--test` are exact alternatives | Scope-dependent | Inherited from selected class rows | Resource collections keep their declared policy | 1–240 s by feature | Implementation and coherent feature-slice completion | Never substitute for separately triggered Smoke or canonical evidence |
| Access focused | Every `Feature=Access` class, including `Api.Access` and `TestSupport.Access` | `Backend/scripts/test-backend access --no-build` | Backend and AccessAdmin built once | Shared PG16, real migrations, and exactly two external wrapper launches: valid invocation and controlled DB failure | DB collections isolated; migration/process collection remains globally sequential | 60–180 s | Authorization slice completion and formal review | Never include Pipeline, canonical data, or Frontend tests |
| Access database/schema | `Feature=Access` with `Kind=Database` plus `Concerns=Schema` | `Backend/scripts/test-backend access-db --no-build` | Backend build once | Shared PG16; head template for ordinary DB classes; no staged migration class | Isolated collection database | 10–45 s | Access persistence, constraint, EF model, catalogue, grants, or audit storage changed | Do not include CLI wrapper or staged upgrade |
| Migration upgrade | Every `Kind=Migration` class, initially only `AccessMigrationPathTests` | `Backend/scripts/test-backend migration --no-build` | Backend/AccessAdmin build once; run EF pending-model command separately when model/schema is in scope | Shared PG16 empty database and per-case schemas; real staged migrations; no template | Globally sequential | 45–180 s | Migration, EF model, backfill, collision/refusal, or schema-guard work | Never run after unrelated UI or ordinary service edits |
| CLI / Process | Every `Kind=Process` class, initially only `AccessAdminCommandTests` | `Backend/scripts/test-backend process --no-build` | Solution and AccessAdmin output built once | Child process for one valid and one controlled-failure wrapper boundary; parser/config logic stays in-process | Globally sequential; bounded process-tree termination | 10–45 s | Wrapper, exit-code, executable-directory configuration, or operator boundary changed | Never duplicate lower-level migration/service permutations as child processes |
| Route Smoke | `Gate=Smoke` excluding `Kind=Canonical`: nine route/composition classes | `Backend/scripts/test-backend smoke --no-build` | Backend build once | Shared PG16 migrated clone; no dump restore | One Smoke collection, isolated from other databases | 30–90 s | Route, contract, auth, middleware, model binding, startup, DI, configuration, or shared DbContext composition changed; formal review | Do not run after every edit or include `SmokeDataReadTests` |
| Tier B no-pipeline | Every `Gate=TierB` class | `Backend/scripts/test-backend tier-b --no-build` | Backend/AccessAdmin build already complete | Shared PG16 for eight current resource families, staged Access migrations, and the two AccessAdmin wrapper processes; template forbidden for migration kind | Existing independent collections may run in parallel; process-global Access collection is sequential | 60–240 s | Feature milestone, engineering review, and ordinary Backend pre-PR | Never add Pipeline or Smoke merely to make it broader |
| Data Pipeline / Importer | `Gate=Pipeline` excluding `Kind=Canonical`, optionally narrowed to one validated Feature | `Backend/scripts/test-backend pipeline --feature Translations --no-build`; omit `--feature` only for a shared-pipeline trigger | Backend/DataImporter build once | Shared PG16 for registered DB classes; in-process importer logic | Isolated collection databases; collection mutation remains serialized | 10–240 s by feature; 180–360 s all | Importer, manifest handling, pipeline entity/schema, shared pipeline, or reachable persistence changed | Never run for isolated authorization, general API, caching, or Frontend-only changes |
| Canonical Quran Data | Every `Kind=Canonical` class, including the exact ten-class list in §2.1 | `Backend/scripts/test-backend canonical-data --no-build` | Backend/DataImporter build once | Shared PG16 for eligible cases; Smoke dump uses the Phase 5 PG16/PG18 decision; host `pg_restore` may launch | Sequential where source/restore fixtures require it; no competing PG container | 180–420 s | Canonical source, manifest/hash, dump, Quran schema/persistence, shared pipeline, or release acceptance changed | Never run for isolated authorization/UI work; required-resource absence is a gate failure |
| Full Backend pre-PR | Every discovered Backend test exactly once; never composed from overlapping focused lanes | `Backend/scripts/test-backend pre-pr` | Solution build once, then test with `--no-build` | All resources; Phase 5 decides one unfiltered invocation or two complementary PG16/PG18 shards | Full-run policy, with controlled collection parallelism | 420–600 s test wall plus separately reported build; measured test point 479.46 s | Once for this shared-infrastructure prerequisite, release/full-regression acceptance, or explicit formal-review trigger | Never after individual edits; ordinary isolated authorization pre-PR uses Access + Smoke + Tier B and excludes Pipeline/canonical |

The Full Backend command is intentionally not implemented as
`fast + access + tier-b + pipeline + smoke + canonical`: those lanes overlap and would
repeat tests, rebuild fixtures, and distort the measurement. Phase 5 defines the only
permitted two-shard fallback when a dedicated PostgreSQL 18 process remains necessary.

## 3. Frontend Test Taxonomy and Execution Gates

### 3.1 Named Angular configurations

Use the Frontend's existing Angular test builder rather than adding a second shell runner.
Add named `architect.test.configurations` entries to
`Frontend/quran-dashboard-ui/angular.json`, then expose them through `package.json`.
Every alias delegates to the existing `npm test` command, so this load-bearing cap remains
in one place:

```text
VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2
```

The exact package commands are:

```json
{
  "test": "VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2 timeout --signal=TERM --kill-after=30s 15m ng test",
  "test:fast": "npm test -- --configuration=fast",
  "test:feature:abwab": "npm test -- --configuration=feature-abwab",
  "test:feature:auth": "npm test -- --configuration=feature-auth",
  "test:feature:dashboard": "npm test -- --configuration=feature-dashboard",
  "test:feature:mushaf": "npm test -- --configuration=feature-mushaf",
  "test:feature:words": "npm test -- --configuration=feature-words",
  "test:authorization": "npm test -- --configuration=authorization",
  "test:composition": "npm test -- --configuration=composition",
  "test:shared": "npm test -- --configuration=shared",
  "test:full": "npm test",
  "test:gates": "node testing/verify-test-gates.mjs",
  "typecheck:app": "timeout --signal=TERM --kill-after=30s 10m tsc -p tsconfig.app.json --noEmit",
  "typecheck:spec": "timeout --signal=TERM --kill-after=30s 10m tsc -p tsconfig.spec.json --noEmit",
  "typecheck": "npm run typecheck:app && npm run typecheck:spec",
  "build:verify": "timeout --signal=TERM --kill-after=30s 10m ng build",
  "test:pre-pr": "npm run typecheck && npm run build:verify && npm run test:full"
}
```

GNU `timeout` keeps output visible and returns its stable timeout status; this repository's
existing environment-variable scripts are already Unix-oriented. The configuration
`include` sets are exact:

```text
feature-abwab:     src/app/features/abwab/**/*.spec.ts
feature-auth:      src/app/features/auth/**/*.spec.ts
feature-dashboard: src/app/features/dashboard/**/*.spec.ts
feature-mushaf:    src/app/features/mushaf/**/*.spec.ts
feature-words:     src/app/features/words/**/*.spec.ts

authorization:
  src/app/app.config.auth.spec.ts
  src/app/app.routes.spec.ts
  src/app/core/auth/**/*.spec.ts
  src/app/core/data-access/secure-url.interceptor.spec.ts
  src/app/features/auth/**/*.spec.ts
  src/environments/environment-guard.spec.ts

composition:
  src/app/**/*.component.spec.ts
  src/app/**/*.directive.spec.ts
  src/app/app.nested-layers.spec.ts
  src/app/core/navigation/detail-overlay/detail-overlay-history.bootstrap.spec.ts
  src/app/core/navigation/detail-overlay/detail-overlay-history.service.spec.ts
  src/app/features/words/entity-detail-overlay/entity-detail-overlay-ayah-continuity.spec.ts
  src/app/features/words/entity-detail-overlay/entity-detail-overlay-invariant.spec.ts

shared:
  src/app/*.spec.ts
  src/app/core/**/*.spec.ts
  src/app/shared/**/*.spec.ts
  src/environments/**/*.spec.ts

fast:
  src/app/**/models/**/*.spec.ts
  src/app/**/utils/**/*.spec.ts
  src/app/**/data/**/*.spec.ts
  src/app/**/*.model.spec.ts
  src/app/**/*-url-sync.spec.ts
  src/app/**/*-url-hydration.spec.ts
  src/app/**/*-cache.spec.ts
  src/app/**/*-display-text.spec.ts
  src/app/**/*-ligature.spec.ts
  src/app/core/navigation/detail-overlay/detail-overlay-url-codec.spec.ts
  src/app/core/navigation/idle-preload.strategy.spec.ts
  src/app/core/navigation/route-paths.spec.ts
  src/app/features/abwab/components/abwab-tree/abwab-tree-keyboard.controller.spec.ts
  src/app/features/abwab/state/abwab-tree.builder.spec.ts
  src/app/features/mushaf/components/mutashabihat-groups-card/mutashabihat-occurrence-preview.spec.ts
  src/app/features/mushaf/state/mushaf-api-load.helpers.spec.ts
  src/app/features/mushaf/state/mushaf-reader-session.spec.ts
  src/app/features/words/state/unique-words-drilldown.controller.spec.ts
  src/app/features/words/state/words-range-filters.spec.ts
  src/app/shared/ui/context-menu/context-menu-placement.spec.ts
  src/app/shared/ui/pagination/pagination-window.spec.ts
  src/app/shared/ui/skeleton/grid-template-columns.spec.ts
  src/app/shared/url/deep-link-href.spec.ts
  src/environments/environment-guard.spec.ts
```

Create `Frontend/quran-dashboard-ui/testing/verify-test-gates.mjs`. It inventories
`src/**/*.spec.ts`, expands the named configuration globs, and proves every file has one
primary area (`feature-*` or `shared`) and belongs to the full gate. It also proves every
component/directive and every named composition file is in `composition`, every
authorization boundary above is selected, all include patterns match at least one file,
and no `e2e/**/*.e2e.ts` enters a Vitest configuration. Fast, authorization, composition,
and shared are intentionally overlapping selectors, not a substitute for one full run.

### 3.2 Frontend lane contract

| Lane | Purpose and inclusion | Command | Build / process | Parallelism | Approximate runtime | Run when | Must not run when |
| --- | --- | --- | --- | --- | ---: | --- | --- |
| Fast / Unit | The exact pure model/utility/data/state/cache/helper set in the `fast` configuration | `npm run test:fast` | Angular test bundle only | Existing two-fork cap | 60–150 s | Pure state, mapping, codec, cache, URL, or validation work | Do not assume fast avoids Angular bundle startup; never add a TestBed/component spec without evidence |
| Feature-focused | One exact feature configuration: Abwab, Auth, Dashboard, Mushaf, or Words | `npm run test:feature:words` (replace `words` with the affected feature) | Test bundle only | Existing cap | 20–240 s by feature | Feature implementation and coherent feature-slice completion | Do not run unrelated feature directories |
| Authorization-focused | The exact cross-cutting auth/config/route/security set in §3.1 | `npm run test:authorization` | Test bundle only | Existing cap | 30–90 s | Frontend auth fixtures/contracts, token handling, callback, secure-origin behavior, route posture, or production auth config changed | Backend-only authorization changes require no Frontend test unless a Frontend/generated contract changed |
| Component/composition integration | All component/directive specs and the named application/overlay compositions in §3.1 | `npm run test:composition` | Test bundle and jsdom; no browser geometry | Existing cap | 150–270 s | Shared component harness, Angular rendering, overlay composition, or broad component infrastructure changed | Do not run after pure Backend or pure utility changes; do not call this browser E2E |
| Shared frontend infrastructure | App-shell, core, shared, and environment specs | `npm run test:shared` | Test bundle/jsdom | Existing cap | 90–210 s | Core/shared/routing/app shell/environment/global test setup changed | Do not substitute for an affected feature lane |
| Full Frontend pre-PR | All current `src/**/*.spec.ts`, with app/spec type-check and production build | `npm run test:pre-pr` | Vitest, two leaf TypeScript checks, production Angular build | Existing cap; steps sequential | 340–430 s total; 285–330 s test portion | Once before PR when Frontend code/config changed; engineering review when Frontend is in scope | Never run for Backend-only changes with no generated/frontend contract diff |
| Build / TypeScript | App type-check, spec type-check, or timeout-bounded production build independently | `npm run typecheck:app`, `npm run typecheck:spec`, `npm run build:verify` | TypeScript or Angular compiler | Tool default | 15–90 s | Compilation, templates, routing, config, generated DTOs, or bundle-affecting work | Never cite root `npx tsc --noEmit`; root config checks nothing |

No Frontend lane requires PostgreSQL, EF migrations, Backend startup, Docker, or importer
processes. “Process” in this table means only Angular/Vitest/TypeScript worker processes.

**Backend-only rule:** Run no Frontend tests for a Backend-only change when no file under
`Frontend/quran-dashboard-ui/`, committed OpenAPI document, or generated Frontend API contract
changed. If a Backend contract change regenerates Frontend DTOs or changes Frontend auth
fixtures/models, run the focused Frontend contract/authorization lane and the required
type-check; run full Frontend pre-PR only because Frontend files then changed, not merely
because the Backend changed.

## 4. Testcontainers Target Architecture

### 4.1 Process owner without an xUnit package migration

The repository uses xUnit 2.9.3, which does not provide native assembly fixtures. Do not
upgrade xUnit or add an assembly-fixture compatibility package for this prerequisite.
Create:

```text
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/
  PostgreSqlTestProcess.cs
  PostgreSqlTestServer.cs
  PostgreSqlDatabaseLease.cs
  PostgreSqlSchemaLease.cs
  ExclusivePostgreSqlLease.cs
  CrossProcessPostgreSqlLock.cs
  PostgreSqlResourceLabels.cs
  PostgreSqlDatabaseName.cs
  PostgreSqlTestProcessContractTests.cs
  README.md
Backend/tests/QuranDashboard.Tests/TestSupport/DependencyInjection/
  OwnedServiceProviderRegistry.cs
  OwnedServiceProviderRegistryTests.cs
```

`PostgreSqlTestProcess` owns a process-static
`Lazy<Task<PostgreSqlTestServer>>` using `ExecutionAndPublication`. Existing xUnit
collection fixtures remain `IAsyncLifetime` consumers of leases; no collection owns the
server. The lazy startup task does not inherit the first caller's cancellation token,
because one canceled fixture must not poison the process runtime. A caller may cancel only
its own wait via `WaitAsync`.

The test-only surface is:

```csharp
internal static class PostgreSqlTestProcess
{
    internal static Task<PostgreSqlDatabaseLease> LeaseMigratedDatabaseAsync(
        string owner,
        CancellationToken cancellationToken = default);

    internal static Task<PostgreSqlDatabaseLease> LeaseEmptyDatabaseAsync(
        string owner,
        CancellationToken cancellationToken = default);

    internal static PostgreSqlDatabaseLease UseExternalReadOnlyDatabase(
        string connectionString);

    internal static Task<ExclusivePostgreSqlLease> LeaseExclusiveServerAsync(
        string owner,
        string image,
        CancellationToken cancellationToken = default);
}
```

Each owned lease exposes `DatabaseName`, `ConnectionString`, and a test-evidence-only
`ServerInstanceId`. The process runtime stays alive even when the active lease count reaches
zero; otherwise later xUnit collections could cause a second PostgreSQL 16 start.

Keep xUnit collection parallelization enabled, but bound active ordinary database leases
with a process semaphore. The initial stable-script value is four:

```text
QURAN_DASHBOARD_TEST_DB_PARALLELISM=4
```

The runtime accepts only integers 1–4 and defaults to 4 for direct `dotnet test`. A lease
holds one slot for its lifetime. Fast tests do not take a slot; migration/process-global
tests and PG18 remain sequential. Phase 2 proves a fifth request waits and proceeds after a
slot is released. Raising the cap requires a new measured lifecycle review, not an ad hoc
agent override.

### 4.2 Ownership labels and run identity

Every project-owned database container carries:

```text
com.qurandashboard.test.owner=backend-tests
com.qurandashboard.test.repository=quran-dashboard
com.qurandashboard.test.run-id=RUN_ID
com.qurandashboard.test.kind=postgresql
com.qurandashboard.test.host-pid=HOST_PID
```

`Backend/scripts/test-backend` creates and exports
`QURAN_DASHBOARD_TEST_RUN_ID`. Direct `dotnet test` execution generates a process-local run
ID inside `PostgreSqlTestProcess`. `RUN_ID` is a newly generated GUID in compact text form;
`HOST_PID` is the decimal test-host PID. Cleanup may select only containers matching all
five labels and the exact run ID. Ryuk keeps its Testcontainers labels and is reported
separately; it is never counted as a PostgreSQL duplicate.

The runtime creates no persistent Docker volume and no project network unless Testcontainers
technically requires one. Any such network must carry the same repository/run labels and
be disposed by the owner. `cleanup-test-runtime` prints exact container/network candidates
before acting and refuses unlabelled resources, blank run IDs, development volumes, and
global prune operations.

### 4.3 Cross-process lock

`CrossProcessPostgreSqlLock` opens an exclusive handle under:

```text
${TMPDIR:-/tmp}/quran-dashboard-tests/PROJECT_USER_HASH-postgres.lock
```

The lock:

- derives `PROJECT_USER_HASH` as the first 16 lowercase SHA-256 hex characters of
  `realpath(QuranDashboard.Tests.csproj) + "\n" + current numeric user ID`;
- uses `FileShare.None` or an equivalent byte-range OS lock;
- is acquired before either PostgreSQL 16 or PostgreSQL 18 is created;
- is held until every project-owned PostgreSQL container for that test process is disposed;
- retries with visible progress every five seconds;
- reports holder PID/start time and times out after a bounded 15 minutes with nonzero exit;
- is never deleted or recreated, which avoids split-inode lock domains;
- relies on the operating system releasing the handle after a crash;
- is covered by a child-process test proving a second holder waits and then proceeds.

This prevents two concurrent `dotnet test` processes from starting competing project
PostgreSQL runtimes. It deliberately serializes database-bearing processes and must be
measured; container-free unit tests do not acquire it.

### 4.4 Migrated template database

The runtime starts one `postgres:16-alpine` container lazily. Its maintenance connection
uses database `postgres` with `Pooling=false`; clone/drop DDL runs outside transactions
through one `SemaphoreSlim`. On the first migrated lease it:

1. creates one uniquely named internal template from `template0`;
2. applies the complete real EF migration chain exactly once;
3. verifies migration head and, when EF/model/schema is in scope, the pending-model check;
4. disposes the DbContext, root provider, data source, and exact template pool;
5. sets `ALLOW_CONNECTIONS false` and `IS_TEMPLATE true`;
6. clones the template under a unique database name for each ordinary collection.

Generated names use a validated ASCII owner slug, process ID, monotonic counter, and random
suffix. Names are bounded to PostgreSQL's 63-byte identifier limit, registered before
cleanup can begin, and quoted with `NpgsqlCommandBuilder.QuoteIdentifier`.

**Template eligible**

- ordinary fixtures whose setup currently calls `Database.MigrateAsync()`;
- final live schema/constraint tests that inspect migration-head state;
- Smoke API’s empty migrated schema;
- the five explorer fixtures currently using `EnsureCreatedAsync`, but only after their
  Phase 3 compatibility tests prove their seed SQL and read behavior against a migrated
  clone;
- the Abwab collection fixture; its empty-state case resets that collection database
  instead of constructing a nested fixture.

**Template forbidden**

- `AccessMigrationPathTests`;
- any staged upgrade beginning from a named previous migration;
- tests whose invariant is `EnsureCreated` itself rather than application behavior;
- EF pending-model checks;
- deliberate pre-head schema mutation/refusal cases;
- the canonical PostgreSQL 18 path until the Phase 5 compatibility decision passes.

If an explorer fixture proves that migrated-head and current-model schema creation are not
equivalent for its test purpose, create one separate **current-model template database**
with `EnsureCreatedAsync` on the same PostgreSQL 16 server. Build that template once. Do
not reintroduce a second container or silently change the test's guarantee.

### 4.5 Per-collection isolation and cleanup order

Every collection receives a unique database lease. Existing feature seed/reset behavior
stays in the feature fixture; only server/schema provisioning moves to shared test support.
During Phase 3, each collection's declared `StatePolicy` is verified:

- `ImmutableSeed`: tests only read the seeded slice;
- `ResetPerTest`: setup truncates/restores all mutated tables before every case;
- `UniqueKeyIsolation`: every case creates uniquely keyed rows and queries/asserts only its
  own keys, with a focused order-independence regression;
- `FreshLeasePerCase`: safety-sensitive cases receive a new database/schema lease.

A fixture with an unproven policy stays serialized and blocks migration acceptance. Do not
rely on a particular test order or silently share mutable rows.

Ten current fixture families construct root `ServiceProvider` instances without complete
ownership. `OwnedServiceProviderRegistry` must cover FullI3rab import/schema,
Mutashabihat, Navigation, Tafsir, Translations, WordsDisplay real/synthetic, Morphology
import, and SimpleI3rab. It disposes roots asynchronously in reverse creation order and
continues after individual failures.

Fixture disposal order is mandatory:

1. dispose `HttpClient` and `WebApplicationFactory`;
2. dispose child scopes;
3. dispose explicit roots and then every registry-owned `ServiceProvider`/`NpgsqlDataSource`;
4. clear the connection pool for the lease connection string;
5. drop the lease database with a bounded command, using `WITH (FORCE)` only after normal
   provider/pool cleanup;
6. record cleanup failure while continuing remaining cleanup steps;
7. at test-process shutdown, dispose the PostgreSQL container;
8. release the operating-system lock;
9. let Ryuk remain the crash-only fallback.

Never call `NpgsqlConnection.ClearAllPools()` while another collection may be active.
Clear only the unique lease pool. Clone/drop/exit cleanup shares the same DDL semaphore.

Register `AppDomain.CurrentDomain.ProcessExit` once. Its idempotent bounded cleanup rejects
new leases, attempts every owned lease cleanup, unsets/drops template state, disposes the
container, and releases the OS lock last. The handler catches/logs every failure and never
throws. The stable script also installs `EXIT`, `INT`, and `TERM` cleanup for only its exact
run ID. Hard `SIGKILL` cannot execute managed or shell traps, so Resource Reaper remains
enabled and `.WithReuse(true)` is forbidden. Phase 9 must prove normal exit reaches zero
owned PostgreSQL containers without relying on Ryuk.

### 4.6 External database policy

Normal gates ignore and unset these existing feature-specific overrides:

```text
MUSHAF_READER_REAL_DB_CONNECTION
UNIQUE_WORDS_REAL_DB_CONNECTION
ROOTS_EXPLORER_REAL_DB_CONNECTION
MORPHOLOGY_EXPLORERS_REAL_DB_CONNECTION
WORD_TYPES_REAL_DB_CONNECTION
```

Preserve their optional read-only diagnostic value only through
`UseExternalReadOnlyDatabase`, outside normal gates, with one explicit operator opt-in.
The exact opt-in is:

```text
QURAN_DASHBOARD_TEST_EXTERNAL_DB_MODE=READ_ONLY_ACKNOWLEDGED
```

It must accompany exactly one of the five feature connection variables above.
`ExternalReadOnlyDatabaseGuard` rejects non-loopback/non-local hosts and any explicit
remote, shared, staging, or production target. An external read-only lease never starts
the shared server, enters the owned-resource registry, migrates, seeds, drops, runs schema
SQL, or clears a pool. Disposal is a no-op after the fixture has disposed its own
hosts/providers. Add a regression that wraps an owned database connection as external,
disposes the wrapper, proves its data still exists, then disposes the true owner.

No mutating external-database path is part of this plan. If one is ever introduced, it
must be centralized behind `DisposableExternalDatabaseGuard` and require all of:

```text
QURAN_DASHBOARD_TEST_EXTERNAL_DB_CONNECTION=EXPLICIT_CONNECTION_STRING
QURAN_DASHBOARD_TEST_EXTERNAL_DB_DISPOSABLE=I_UNDERSTAND_THIS_DATABASE_WILL_BE_MUTATED
```

The guard rejects a blank database, the default `postgres` database, the development
`quran_dashboard` database, any database name not beginning `qdb_test_`, and any connection
without the exact sentinel. It must also prove the server is local and dedicated before
mutation; a name or connection string alone is never ownership evidence. Normal scripts
unset every external variable and always use Testcontainers.

## 5. Migration-Path and Process Isolation

`AccessMigrationTestFixture` moves from a collection-owned container to one empty database
lease on the shared PostgreSQL 16 server. Each test continues creating a unique schema and
`SearchPath`, so migration history and schema mutations remain isolated.

`AccessMigrationPathTests` remains in a
`DisableParallelization = true` process-global collection and must continue proving:

- migration from the required previous migration;
- additive schema before normalized-email enforcement;
- collision refusal without merge or relink;
- missing-backfill refusal;
- final required/unique normalized-email behavior.

No method in this class may call `LeaseMigratedDatabaseAsync`.

Rename the current collection to `AccessProcessGlobalCollection` and keep only
`AccessMigrationPathTests`, `AccessSchemaDriftTests`, and `AccessAdminCommandTests` in it.
`AccessSchemaDriftTests` uses a new migrated clone for each case and retains the fresh-head
acceptance case, retired-permission refusals, and all current missing/invalid table,
column, index, identity, foreign-key, and check-constraint mutations. This is a runtime
optimization of setup, not a conversion of unique PostgreSQL catalogue guarantees into
mocked/pure assertions.

`AccessSchemaModelTests` remains the Fast EF-metadata guarantee; it is not a replacement
for live catalogue inspection. No schema-drift row is converted or deleted in this plan
because the audited rows exercise distinct PostgreSQL object types/definitions. Pure
comparison/formatting theories may be added later only if a production boundary already
exposes pure logic; this prerequisite does not refactor Authorization production code to
manufacture a faster test seam.

Add:

```text
Backend/tests/QuranDashboard.Tests/TestSupport/Process/ProcessGlobalStateScope.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Process/ProcessExecution.cs
```

`ProcessGlobalStateScope` captures/restores the current directory, selected environment
variables, `Console.Out`, and `Console.Error` in `Dispose`, including failure paths.
`ProcessExecution` drains stdout/stderr concurrently, waits with a two-minute timeout, kills
the entire process tree on timeout, waits for termination, and returns a stable exit/result
record. `AccessMigrationCollection` remains `DisableParallelization = true`, covering both
`AccessMigrationPathTests` and `AccessAdminCommandTests`; this is required because together
they mutate current directory, connection environment, and console streams.

Keep parsing, executable-directory configuration, and migration permutations in-process.
Keep exactly two true wrapper launches: the existing valid `identity scan` invocation and
one controlled unreachable-database invocation that proves wrapper propagation of exit code
4 without a stack trace. Do not add process launches for cases already covered through
`Program.Main` or lower-level services. The valid wrapper receives its own migrated
template clone; the failure wrapper uses only the controlled loopback port-1 connection and
does not acquire a database.

Do not modify `QuranDashboard.AccessAdmin` production behavior for this optimization.

## 6. PostgreSQL 18 Canonical Smoke Decision Gate

Compatibility must be proven, not assumed.

### Decision A — consolidate on shared PostgreSQL 16

- [ ] Confirm the current manifest hash and migration head with `SmokeDumpGate`.
- [ ] Record host `pg_restore --version`; the audited host version was 18.4.
- [ ] Create an isolated migrated clone on the shared PostgreSQL 16 server.
- [ ] Run host `pg_restore` 18 against that clone using child-process-only credentials,
      `--data-only`, `--disable-triggers`, and the existing controlled job count.
- [ ] Require exit code 0 and retain stderr in failure evidence.
- [ ] Run every `SmokeDataReadTests` route and manifest row-count assertion.
- [ ] Verify the complete canonical-data lane has no unexpected skip.
- [ ] Verify only the PostgreSQL 16 container and Ryuk existed during the proof.

If all checks pass, `SmokeDataFixture` uses a migrated shared-server lease and host
`pg_restore`. The restore still targets an isolated database and retains every current
manifest/count/route assertion.

### Decision B — retain exclusive PostgreSQL 18

Select this path if archive/server compatibility, restore semantics, or canonical evidence
cannot be proven on PostgreSQL 16.

- Mark `SmokeDataCollection` globally nonparallel.
- Make the PostgreSQL 18 owner acquire the same cross-process OS lock as PostgreSQL 16.
- Run `canonical-data` as two complementary invocations: every canonical class except
  `SmokeDataReadTests` on PG16, then exactly `SmokeDataReadTests` on PG18.
- Run Full Backend pre-PR as two complementary invocations: every discovered test except
  `SmokeDataReadTests`, then exactly `SmokeDataReadTests`.
- Have `TestGateCatalogTests` prove that each pair has an empty intersection and its union
  is the intended complete class set.
- Fully dispose the first test process and PostgreSQL 16 before the PostgreSQL 18 process
  starts; dispose PostgreSQL 18 before releasing the shared lock.
- Prove by Docker events that PG16 and PG18 never overlap.

Both decisions preserve canonical Quran coverage. A compatibility failure selects Decision
B; it does not justify skipping or reducing `SmokeDataReadTests`. Decision A permits one
unfiltered Full Backend invocation. Decision B permits only the lossless two-shard fallback
above; focused-lane composition is not a Full Backend substitute.

## 7. Stable Script Contracts

### 7.1 Backend

Create:

```text
Backend/scripts/test-backend
Backend/scripts/cleanup-test-runtime
Backend/scripts/check-pending-model
```

Common syntax:

```text
Backend/scripts/test-backend fast|access|access-db|migration|process|smoke|tier-b|canonical-data
  --build|--no-build [--results-dir PATH]
Backend/scripts/test-backend feature FEATURE_KEY --build|--no-build [--results-dir PATH]
Backend/scripts/test-backend feature --class FULL_CLASS_NAME --build|--no-build
Backend/scripts/test-backend feature --test FULL_METHOD_NAME --build|--no-build
Backend/scripts/test-backend pipeline [--feature FEATURE_KEY] --build|--no-build
Backend/scripts/test-backend pre-pr [--results-dir PATH]
Backend/scripts/test-backend pre-pr --list-tests --no-build
```

Rules:

- focused commands require the caller to choose `--build` or `--no-build`; there is no
  ambiguous default;
- `--build` runs `dotnet build Backend/QuranDashboard.sln` exactly once before the lane;
- `--no-build` fails early with a clear diagnostic if expected test/tool output is absent;
- executing `pre-pr` always builds once and then runs the complete class set exactly once;
  its discovery-only form permits `--list-tests --no-build`;
- always pass `--no-restore` to test execution after the build/restore precondition;
- use `--blame-hang --blame-hang-dump-type none`; Fast/container-free lanes use a
  five-minute hang timeout and every database/canonical/full lane uses twenty minutes so
  the visible 15-minute cross-process lock timeout fails first;
- keep console verbosity at `normal` for broad/resource lanes and `minimal` only for
  explicitly fast focused lanes;
- never redirect or pipe through `tail`;
- use `-m:1 -p:BuildInParallel=false` for MSBuild orchestration while leaving verified
  xUnit collection parallelism available and setting the four-slot database cap;
- print selected catalog rows, expanded filters, resource needs, and cleanup run ID before
  execution;
- print `canonical data tier: not selected` for route Smoke, and print `ran` or
  `failed preflight` for commands that address canonical resources; canonical/pre-PR
  evidence treats required-resource absence as failure;
- preserve the test command’s exit code after cleanup;
- reject unknown lane/selector/feature with exit code 2;
- accept only cataloged feature keys and exact discovered class/method names, never a raw
  caller-supplied filter;
- trap exit/signals and clean only fully labelled resources for the current run ID.

Examples:

```bash
Backend/scripts/test-backend fast --build
Backend/scripts/test-backend feature --class QuranDashboard.Tests.Api.Access.EmailIdentityNormalizerTests --no-build
Backend/scripts/test-backend access --no-build
Backend/scripts/test-backend access-db --no-build
Backend/scripts/test-backend migration --no-build
Backend/scripts/test-backend process --no-build
Backend/scripts/test-backend smoke --no-build
Backend/scripts/test-backend tier-b --no-build
Backend/scripts/test-backend pipeline --feature Translations --no-build
Backend/scripts/test-backend canonical-data --no-build
Backend/scripts/test-backend pre-pr
Backend/scripts/check-pending-model --no-build
```

`check-pending-model` invokes `dotnet ef migrations has-pending-model-changes` against
`infrastructure/QuranDashboard.Infrastructure`, with
`api/QuranDashboard.Api` as startup project and `QuranDashboardDbContext` as context. It
supports the same explicit `--build|--no-build` convention, never adds or applies a
migration, and runs only when EF model/schema/migrations are in scope.

### 7.2 Frontend

Do not add a Frontend shell wrapper. The stable command surface is the exact
`package.json` naming convention from §3.1:

```text
test:fast
test:feature:FEATURE_KEY
test:authorization
test:composition
test:shared
test:full
test:pre-pr
test:gates
typecheck:app
typecheck:spec
typecheck
build:verify
```

Named `angular.json` configurations own file selection, and
`testing/verify-test-gates.mjs` validates them. All test aliases inherit the two-fork cap
through `npm test`, show normal Vitest progress, return the underlying exit code, and never
start Backend, Playwright, or Docker work. JUnit is added only to the Phase 9 measurement
while retaining the default reporter.

The one-file focused form remains:

```bash
npm test -- --include=src/app/core/auth/current-user.store.spec.ts
```

Use only repository-relative `src/**/*.spec.ts` paths verified to exist; do not use raw
Vitest `--run` or bypass the Angular builder.

## 8. Test Deletion and Consolidation Decisions

These changes occur only in Phase 7, after stable gates and shared fixtures pass.

| Current test | Claimed guarantee | Decision and replacement | Baseline body time / expected saving | Why coverage is not weakened |
| --- | --- | --- | ---: | --- |
| `Frontend/quran-dashboard-ui/src/app/app.sanity.spec.ts` — `Test runner sanity > executes a passing assertion` | Vitest can execute `expect(true).toBe(true)` | Delete. The configuration validator and every remaining real spec prove runner execution | 0.0027 s; negligible body saving, one less compiled spec entry | It tests a framework guarantee and no product contract |
| `Frontend/quran-dashboard-ui/src/app/core/auth/auth.testing.spec.ts` — two fixture-literal cases | The four future `/me` fixtures contain selected literal fields | Delete the runtime self-test. Keep `auth.testing.ts` unchanged: its interface/object annotation is already compile-checked and `provideAuthTesting()` is consumed by four composition specs | About 0.004 s; negligible | The spec compares a fixture module to literals declared in that same module, not application behavior. Authorization Phase 3—not this prerequisite—must add real store/guard/component assertions when those fixture fields acquire production consumers |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesFixtureSmokeTests.cs` | Fixture can connect and claims seed success | Delete after `WordTypesMainReadTests.Rows_ReturnExpectedTotals_ForMainTypes` passes on the migrated lease | 0.0786 s body; no container-start saving because collection remains | The surviving query proves connection, schema, seed rows, and expected totals—strictly stronger than `CanConnectAsync` |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersFixtureSmokeTests.cs` | Fixture can connect and claims seed success | Delete after `LemmasListReadTests.GetLemmasPage_returns_default_page_with_all_seeded_lemmas` passes on the migrated lease | 0.0263 s body; no container-start saving because collection remains | The surviving query proves connection, schema, seeding, and read behavior |
| `Backend/tests/QuranDashboard.Tests/TestSupport/Access/EmailIdentityContractTests.cs` — `Vectors_CoverValidInvalidAndNormalizedDuplicateCases` | Test data is shaped as valid/invalid and duplicate groups | Remove only this self-test method. Add a theory in `Api/Access/EmailIdentityNormalizerTests.cs` consuming every `DuplicateNormalizedInputs` group and asserting all inputs normalize to one identical value | 0.0385 s removed, offset by real theory cases; no promised saving | Valid/invalid vectors are already consumed by real normalizer theories; duplicate vectors gain production-behavior coverage |

Required reclassification, with no deleted assertion:

| Current location | Target | Guarantee and runtime effect |
| --- | --- | --- |
| `AccessMigrationPathTests.AuthorizationPreflight_RejectsLiveSchemaDrift` (15 rows), fresh-head preflight, and retired-permission cases | `AccessSchemaDriftTests`, `Kind=Database`, `Concerns=Schema` | Retain every live PostgreSQL mutation/catalogue assertion, but start each from a migrated template clone instead of replaying the full chain |
| `AccessMigrationPathTests.AuthorizationPreflight_UnreachableDatabase_ReportsAControlledOperationalFailure` | Convert to the second true wrapper case in `AccessAdminCommandTests`, `Kind=Process` | Preserve controlled exit code 4/no-stack behavior at the stronger executable-wrapper boundary; together with the existing valid wrapper this leaves exactly two child launches |

The approximately 0.15-second total is test-body time and is not a promised wall-time
saving; Frontend compilation/setup and database topology dominate. The deletion gate is
about removing disconnected or strictly weaker checks, not making a benchmark look better.

Do not consolidate `AccessSchemaModelTests` with `AccessMigrationPathTests`; EF metadata and
applied PostgreSQL upgrade behavior are different guarantees. Do not consolidate route Smoke
with lower-level API/Abwab tests. Do not delete classes named `*RedundancyReadTests`; those
are differential regression guards, not redundant tests.

## 9. Implementation Phases

### Phase 1 — Test taxonomy, catalog, and stable commands

**Goal:** Establish a lossless Backend class taxonomy and stable Backend command selection
before changing fixture behavior.

**Files**

- Create the Backend gate/resource catalogs and validator files listed in §2.2.
- Create `Backend/scripts/test-backend` and `Backend/scripts/check-pending-model`.
- Modify `Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj` to copy catalog
  files to test output.
- Do not edit documentation or Frontend files in this phase.

**Dependencies:** Completed audit; current compiled test discovery.

- [ ] Add a failing `TestGateCatalogTests` case proving `TestSupport.Access` must be in the
      Access lane and every discovered class has exactly one Feature, Kind, and Gate.
- [ ] Build once and run only `TestGateCatalogTests`; expect failure naming missing/overlap
      entries.
- [ ] Populate `test-gates.tsv` and `test-resources.tsv` from compiled discovery plus source
      inspection, using the exact rules in §2.1.
- [ ] Add the Backend runner and make every public lane print its resolved filter with
      `--list-tests` support.
- [ ] Prove the legacy Tier B/Pipeline/Smoke filters and new primary Gate rows select the
      same classes before retiring copied filters.
- [ ] Run the catalog validator and `--list-tests` for each lane; do not execute broad test
      bodies.
- [ ] Record focused lane discovery counts and pre-optimization lane timings only for
      `fast`, `access`, `access-db`, and `migration`; do not rerun the full baseline.

**Tests during implementation**

```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --no-build \
  --filter "FullyQualifiedName~QuranDashboard.Tests.TestSupport.Execution.TestGateCatalogTests"

Backend/scripts/test-backend fast --list-tests --no-build
Backend/scripts/test-backend access --list-tests --no-build
Backend/scripts/test-backend pipeline --list-tests --no-build
Backend/scripts/test-backend pre-pr --list-tests --no-build
```

**Explicitly not run:** Smoke bodies, Tier B bodies, Pipeline, canonical data, full Backend,
and all Frontend tests.

**Rollback:** Remove the new catalogs/runner and csproj content entry. Existing direct
filters remain unchanged until Phase 8 updates policy.

**Acceptance:** Every discovered class has one valid catalog row, the primary Gate
partition is lossless, Access includes `TestSupport.Access`, pipeline Fast classes remain
selectable as Fast, public filters resolve deterministically, and unknown selectors fail
with exit 2.

**Review checkpoint:** Taxonomy review against compiled discovery and `TESTING_STRATEGY.md`.

**Independent commit:** Yes — proposed message `test: add stable backend test gates`.

### Phase 2 — Shared PostgreSQL 16 lifecycle foundation

**Goal:** Implement and prove one labelled PostgreSQL 16 server, OS lock, migrated template,
isolated leases, and bounded process cleanup without migrating every fixture.

**Files**

- Create the PostgreSQL and provider-ownership support files listed in §4.1 except the
  PostgreSQL README, which lands in Phase 8.
- Modify `Backend/scripts/test-backend` and add `Backend/scripts/cleanup-test-runtime`.
- Pilot `Backend/tests/QuranDashboard.Tests/Abwab/AbwabSchemaFixture.cs` and
  `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTreeReadTests.cs`.

**Dependencies:** Phase 1 runner/catalogs.

- [ ] Write a failing test that concurrently creates two migrated leases and requires one
      `ServerInstanceId`, two different database names, and isolated tables/data.
- [ ] Write a failing slot test proving four database leases may run and a fifth waits
      until one disposes.
- [ ] Write a failing child-process lock test proving a second holder waits.
- [ ] Implement run labels, OS lock, lazy PG16 runtime, template migration, name validation,
      lease drop, and idempotent process disposal.
- [ ] Prove one canceled caller does not poison lazy process startup.
- [ ] Prove exact-pool cleanup, idempotent lease disposal, and failure of one cleanup step do
      not prevent later cleanup.
- [ ] Migrate `AbwabSchemaFixture` to `LeaseMigratedDatabaseAsync`.
- [ ] Replace the nested `new AbwabSchemaFixture()` with `ResetAbwabAsync`, truncating all
      six Abwab tables with identity reset/cascade and invalidating tree/template caches.
- [ ] Rename the nested test's “fresh schema” wording to the actual “empty database”
      guarantee.
- [ ] Run the Abwab schema/read classes and runtime tests only.
- [ ] Run one scripted focused process, inspect labelled containers while active, and prove
      zero project containers after exit.

**Tests during implementation**

```bash
Backend/scripts/test-backend feature \
  --class QuranDashboard.Tests.TestSupport.PostgreSql.PostgreSqlTestProcessContractTests --build

Backend/scripts/test-backend feature Abwab --no-build
```

**Explicitly not run:** Access, Smoke, Tier B, Pipeline, canonical data, full Backend, and
Frontend.

**Rollback:** Restore only `AbwabSchemaFixture` to its collection container and remove the
new runtime/scripts. No other fixture depends on the foundation yet.

**Acceptance:** One PG16 builder/runtime for concurrent lease tests, no more than four
active database leases, isolated state, no nested Abwab fixture, cross-process wait, safe
name generation, targeted pool cleanup, zero leak after normal exit, and cleanup selects
only the exact run ID.

**Review checkpoint:** Dedicated concurrency/lifecycle architecture review before Phase 3.

**Independent commit:** Yes — proposed message `test: add shared postgres test runtime`.

### Phase 3 — Move ordinary fixtures to isolated databases

**Goal:** Replace the remaining ordinary collection-owned containers with isolated
databases on the shared PostgreSQL 16 runtime and remove repeated full migration chains.

**Files likely to change**

```text
Backend/tests/QuranDashboard.Tests/Api/Access/AccessTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/FullI3rab/FullI3rabImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/FullI3rab/FullI3rabSchemaFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Import/ImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/MushafReader/MushafReaderTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Mutashabihat/MutashabihatImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Navigation/NavigationImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/DisplayWordsRealImportFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/WordsDisplayTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/RootsExplorerTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsSimpleI3rab/I3rabGenerationTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesTestFixture.cs
Backend/tests/QuranDashboard.Tests/Smoke/SmokeApiFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRollbackTests.cs
```

Implement Phase 3 as four reviewable cohorts, never one bulk edit:

1. Tier B ordinary: Access, MushafReader, UniqueWords, MorphologyExplorers, RootsExplorer,
   WordTypes.
2. Source pipeline: FullI3rab, Foundation Import, Mutashabihat, Navigation, Tafsir,
   Translations.
3. Derived pipeline: WordsDisplay synthetic/real, Morphology, SimpleI3rab.
4. Route Smoke plus the Translation package-writer cleanup.

**Dependencies:** Phase 2 architecture review approved.

- [ ] Convert migration-backed fixtures one domain at a time to migrated leases.
- [ ] Assign and prove one `StatePolicy` for every converted collection; add/reset only
      where source inspection shows mutable cross-test leakage.
- [ ] Dispose every root provider before lease disposal.
- [ ] Move source-presence gates before database acquisition in source-backed fixtures.
- [ ] Replace the uninitialized `TranslationImportTestFixture` used only as a package writer
      with an explicit disposable package helper.
- [ ] Route the five legacy real-DB overrides through the explicit read-only external lease
      and opt-in from §4.6; normal gates must unset them.
- [ ] For each of the five `EnsureCreatedAsync` explorer fixtures, first run its seeded
      representative read against a migrated clone; use a current-model template only if the
      guarantee genuinely differs.
- [ ] Register and dispose all ten root-provider families named in §4.5 before releasing
      their database lease.
- [ ] Run only the converted domain’s feature lane after each domain conversion.
- [ ] Run `access-db` for `AccessTestFixture` and route `smoke` for `SmokeApiFixture`.
- [ ] Do not run Tier B yet: `AccessMigrationTestFixture` still owns the last independent
      PG16 builder until Phase 4.

**Tests during implementation:** build once after each compiled domain batch, then run the
matching feature lane, for example:

```bash
Backend/scripts/test-backend feature Translations --build
Backend/scripts/test-backend access-db --no-build
Backend/scripts/test-backend smoke --no-build
```

**Explicitly not run:** Pipeline families not currently being converted, Tier B,
canonical-data, full Backend, and Frontend.

**Rollback:** Convert by domain in reviewable commits or one commit with domain-separated
hunks. Revert only the failing domain to its old fixture while the shared runtime remains.

**Acceptance:** Every converted fixture shares one server, each collection owns a unique
database, normal gates cannot silently use an external database, no root provider remains
undisposed, focused fixture-family runs leak nothing, and source now contains only the
shared PG16 builder plus the not-yet-converted Access migration PG16 builder and PG18
canonical builder.

**Review checkpoint:** Fixture-isolation and test-guard review after each of the four
cohorts; do not begin the next cohort with an unresolved ownership/cleanup finding.

**Independent commit:** Yes; prefer one reviewed commit per cohort, all independently
revertible. Final cohort message example: `test: share postgres across ordinary fixtures`.

### Phase 4 — Migration path and process-global isolation

**Goal:** Move staged migration tests onto the shared server without the template and harden
all process-global/process boundaries.

**Files**

```text
Backend/tests/QuranDashboard.Tests/Api/Access/AccessMigrationTestFixture.cs
Backend/tests/QuranDashboard.Tests/Api/Access/AccessMigrationPathTests.cs
Backend/tests/QuranDashboard.Tests/Api/Access/AccessSchemaDriftTests.cs
Backend/tests/QuranDashboard.Tests/Api/Access/AccessAdminCommandTests.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Process/ProcessGlobalStateScope.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Process/ProcessExecution.cs
```

**Dependencies:** Phases 1–3.

- [ ] Add failing timeout/restore tests for `ProcessExecution` and
      `ProcessGlobalStateScope`.
- [ ] Give `AccessMigrationTestFixture` one empty database lease and preserve unique
      per-case schemas.
- [ ] Add a regression proving migration history and tables created in schema A are absent
      from schema B.
- [ ] Assert migration tests never receive the head-template connection string.
- [ ] Move head-schema/preflight cases and all 15 mutation rows to
      `AccessSchemaDriftTests`; give each a fresh migrated template clone.
- [ ] Rename the collection to `AccessProcessGlobalCollection`, retain
      `DisableParallelization = true`, and keep only the three process-global classes in it.
- [ ] Replace unbounded process waits with bounded stdout/stderr-draining execution and
      process-tree termination.
- [ ] Use state scopes for current directory, connection environment, and console streams.
- [ ] Keep the existing valid wrapper process and convert the existing controlled
      unreachable-database case into the second wrapper process; keep
      parsing/configuration/migration permutations in-process.
- [ ] Run exact changed helper/migration/process tests during iteration, then run complete
      Access once at phase completion.

**Tests during implementation**

```bash
Backend/scripts/test-backend feature \
  --class QuranDashboard.Tests.Api.Access.AccessMigrationPathTests --build
Backend/scripts/test-backend feature \
  --class QuranDashboard.Tests.Api.Access.AccessSchemaDriftTests --no-build
Backend/scripts/test-backend feature \
  --class QuranDashboard.Tests.Api.Access.AccessAdminCommandTests --no-build
Backend/scripts/test-backend access --no-build
```

**Explicitly not run:** EF pending-model check because no production EF model or migration
changes are allowed in this prerequisite; Smoke, Tier B, Pipeline, canonical data, full
Backend, and Frontend.

**Rollback:** Revert Access migration fixture to its collection container while leaving
ordinary shared fixtures intact; restore previous process helper use if the bounded helper
breaks a documented CLI boundary.

**Acceptance:** All staged upgrade/refusal cases pass on empty schemas without the head
template; all 15 live drift mutations pass on isolated migrated clones; process timeouts
are deterministic; and global state is restored after success and failure. Static search
finds one PG16 builder in shared support and one PG18 builder in canonical Smoke—no
collection-owned PG16 builder remains.

**Review checkpoint:** Security/migration review; every previously protected refusal
invariant receives file-and-test evidence.

**Independent commit:** Yes — proposed message `test: isolate migration and process boundaries`.

### Phase 5 — Canonical Smoke PostgreSQL decision

**Goal:** Execute the proof in §6 and implement exactly one of Decision A or Decision B.

**Files**

```text
Backend/tests/QuranDashboard.Tests/Smoke/Data/SmokeDataFixture.cs
Backend/tests/QuranDashboard.Tests/Smoke/Data/SmokeDataCollection.cs
Backend/tests/QuranDashboard.Tests/Smoke/Data/SmokeDumpGate.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlTestProcess.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/CrossProcessPostgreSqlLock.cs
Backend/scripts/test-backend
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv
```

**Dependencies:** Shared runtime, canonical archive present and verified, host
`pg_restore` available.

- [ ] Run the PG16 compatibility proof once.
- [ ] Record the decision and evidence before editing the permanent fixture path.
- [ ] Implement Decision A if and only if restore plus all canonical Smoke assertions pass.
- [ ] Otherwise implement Decision B’s exclusive PG18 path.
- [ ] For Decision B, prove the two complementary `canonical-data` and `pre-pr` shard
      filters are exhaustive and nonoverlapping.
- [ ] Run `smoke` and `canonical-data` once each.
- [ ] Query Docker events and prove no PG16/PG18 overlap and zero PostgreSQL leak.

**Tests during implementation**

```bash
Backend/scripts/test-backend feature \
  --class QuranDashboard.Tests.Smoke.Data.SmokeDataReadTests --build
Backend/scripts/test-backend canonical-data --no-build
Backend/scripts/test-backend smoke --no-build
```

The first command is the compatibility proof against the candidate PG16 path. The
canonical command is the one final verification of the implemented Decision A/B fixture;
do not repeat it for timing.

**Explicitly not run:** Tier B, unrelated Pipeline families, full Backend, Frontend.

**Rollback:** Decision A can revert to Decision B without changing assertions. Decision B
can retain the existing PG18 fixture implementation, but only behind the shared lock and
lossless two-process sequencing. Any rollback that permits PG16/PG18 overlap blocks
acceptance.

**Acceptance:** Canonical data coverage and manifest/count/route evidence remain complete;
only one database container exists at any instant; chosen restore path is documented by
evidence rather than assumption; `pre-pr` remains an exactly-once Full Backend gate.

**Review checkpoint:** Quran-data safety and Testcontainers exclusivity review.

**Independent commit:** Yes — proposed message depends on result:
`test: restore canonical smoke on shared postgres` or
`test: serialize canonical postgres restore`.

### Phase 6 — Frontend commands and environment cleanup

**Goal:** Add stable Frontend lanes and remove test-order leakage without changing
production behavior.

**Files**

```text
Frontend/quran-dashboard-ui/package.json
Frontend/quran-dashboard-ui/angular.json
Frontend/quran-dashboard-ui/testing/verify-test-gates.mjs
Frontend/quran-dashboard-ui/src/test-setup.ts
Frontend/quran-dashboard-ui/src/app/app.nested-layers.spec.ts
Frontend/quran-dashboard-ui/src/app/core/navigation/idle-preload.strategy.spec.ts
Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/entity-detail-overlay-invariant.spec.ts
Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/entity-detail-overlay-host.component.spec.ts
Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/entity-detail-overlay-ayah-continuity.spec.ts
Frontend/quran-dashboard-ui/src/app/shared/ui/context-menu/context-menu.component.spec.ts
```

**Dependencies:** Audit file inventory; existing two-fork cap.

- [ ] Add a failing configuration validator for missing/unknown specs.
- [ ] Add the exact named Angular configurations and package aliases from §3.1.
- [ ] Preserve the fork cap solely in the existing `npm test` command.
- [ ] Add an `afterEach` safety net in `src/test-setup.ts` that restores real timers,
      mocks/spies, and Vitest-stubbed globals; clears local/session storage; removes only
      `document.documentElement[data-theme]`; and restores only body overflow.
- [ ] Replace direct `matchMedia`, `ResizeObserver`, and `requestIdleCallback` assignments
      in the listed specs with `vi.stubGlobal`.
- [ ] Do not add a global `matchMedia` polyfill, globally reset TestBed, or wipe body
      children; those would alter harness contracts or race Angular teardown.
- [ ] Add file-local fixture-host destruction/removal to `context-menu.component.spec.ts`.
- [ ] Run the configuration validator, state-sensitive specs, authorization lane, and one
      representative feature lane.
- [ ] Run app/spec type-check once after setup/configuration changes.

**Tests during implementation**

```bash
npm run test:gates
npm run test:authorization
npm run test:feature:words
npm run typecheck
```

**Explicitly not run:** full Frontend, production build until final phase, E2E, all Backend.

**Rollback:** Remove the named configurations/package aliases and restore file-local global
setup. The existing `npm test` command remains available throughout.

**Acceptance:** Every spec has a documented primary area and full gate, every named
configuration selects its intended files, state-sensitive specs restore globals even after
failure, context-menu DOM is removed locally, and the two-fork cap remains effective.

**Review checkpoint:** Frontend test-guard and order-isolation review.

**Independent commit:** Yes — proposed message `test: add frontend execution gates`.

### Phase 7 — Proven test consolidation

**Goal:** Apply only the five evidence-backed decisions in §8.

**Files:** exactly the test/support files named in §8 plus
`TestSupport/Execution/test-gates.tsv`; `auth.testing.ts` is not modified.

**Dependencies:** Backend/Frontend gate validators from Phases 1 and 6, migrated fixture
behavior from Phase 3, and the named surviving/replacement assertions from §8.

- [ ] Add the real duplicate-normalization theory first and verify it fails if inputs no
      longer normalize identically.
- [ ] Run surviving seeded read tests before deleting the two fixture-smoke tests.
- [ ] Delete `app.sanity.spec.ts`.
- [ ] Confirm `auth.testing.ts` remains imported and its existing
      `AccessMeContractFixtures` annotation compiles, then delete only
      `auth.testing.spec.ts`.
- [ ] Delete the two Backend connectivity-only files.
- [ ] Remove only `Vectors_CoverValidInvalidAndNormalizedDuplicateCases`, retaining the
      collision-scan behavior test in the same class.
- [ ] Update the Backend catalog and run both Backend/Frontend gate validators.
- [ ] Run only replacement/surviving focused tests.

**Tests during implementation**

```bash
Backend/scripts/test-backend feature \
  --class QuranDashboard.Tests.Api.Access.EmailIdentityNormalizerTests --build
Backend/scripts/test-backend feature \
  --class QuranDashboard.Tests.Quran.WordsWordTypes.WordTypesMainReadTests --no-build
Backend/scripts/test-backend feature \
  --class QuranDashboard.Tests.Quran.WordsMorphologyExplorers.LemmasListReadTests --no-build
cd Frontend/quran-dashboard-ui
npm run test:gates
npm run test:authorization
```

**Explicitly not run:** broad Backend/Frontend, Pipeline, canonical data, E2E.

**Rollback:** This phase is one independent commit. Revert the deletion commit if any
replacement guarantee is disputed; do not weaken the replacement to preserve the deletion.

**Acceptance:** Every deleted guarantee is obsolete or has a named stronger survivor,
catalog/configurations contain no dead paths/classes, and security/canonical/migration
coverage is untouched.

**Review checkpoint:** Dedicated test-deletion review using the repository test-guard.

**Independent commit:** Yes — proposed message `test: remove redundant harness checks`.

### Phase 8 — Agent instructions and living policy

**Goal:** Make scope-aware execution the first-session default while keeping detailed
commands outside entrypoints.

**Files**

```text
AGENTS.md
CLAUDE.md
Backend/AGENTS.md
Backend/CLAUDE.md
Frontend/quran-dashboard-ui/AGENTS.md
Frontend/quran-dashboard-ui/CLAUDE.md
TESTING_STRATEGY.md
Backend/tests/QuranDashboard.Tests/README.md
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/README.md
Backend/scripts/README.md
Frontend/quran-dashboard-ui/README.md
Frontend/quran-dashboard-ui/testing/README.md
Backend/api/QuranDashboard.Api/README.md
.claude/skills/test-guard/SKILL.md
.claude/skills/engineering-review/SKILL.md
.claude/skills/pr-context-prep/SKILL.md
docs/security/authorization-permissions-implementation-plan.md
```

**Dependencies:** Commands and lifecycle behavior verified in Phases 1–7.

- [ ] Replace copied command fragments with references to the stable Backend catalog/script
      and Frontend named configurations.
- [ ] Add the execution-trigger matrix from §13.4 to `TESTING_STRATEGY.md`.
- [ ] Document template eligibility, PG18 decision, labels, lock, disposal order, and
      external DB prohibition in the nearest Backend test-support README.
- [ ] Document Frontend lanes and Backend-only no-Frontend rule.
- [ ] Insert the exact concise entrypoint wording from §13.5.
- [ ] Search the repository for every superseded filter/command and update or explicitly
      preserve each reference.
- [ ] Replace frozen Smoke counts and copied filters in review/preparation skills and the
      active Authorization plan with stable command references.
- [ ] Keep the API README's route-catalog obligation and point it to `test-backend smoke`.
- [ ] Run Markdown/path/command help validation only; do not run tests because documentation
      changed.

**Tests during implementation:** None. Validate only `Backend/scripts/test-backend --help`,
`Backend/scripts/check-pending-model --help`, `npm run test:gates`, Markdown links, and
repository references.

**Explicitly not run:** all tests and builds.

**Rollback:** Revert this documentation commit independently; stable scripts remain usable.

**Acceptance:** One authoritative detailed matrix, no dangling script/README links, six
entrypoints concise and consistent, no stale `TestSupport` description, no claim that
canonical source fixtures universally self-skip, and no frozen test counts in living docs.

**Review checkpoint:** Documentation/contract consistency review.

**Independent commit:** Yes — proposed message `docs: codify scope-aware test execution`.

### Phase 9 — Final measurement and acceptance

**Goal:** Verify correctness once on final state, record comparable before/after evidence,
and hand the prerequisite to formal engineering review.

**Files**

```text
Backend/report/testing/test-runtime-audit-and-execution-policy.md
```

Create this final before/after audit report if it is still absent; otherwise update the
implementation-era copy in place.
Update this implementation plan only if implementation evidence required an approved design
deviation.

**Dependencies:** Phases 1–8 reviewed; canonical resources staged for the canonical run.

- [ ] Run the exact catalog/lifecycle contract classes, complete Access once, and route
      Smoke once as final-state sanity checks.
- [ ] Hand the unchanged final tree to a fresh formal reviewer.
- [ ] The formal reviewer runs `Backend/scripts/test-backend pre-pr` once. It builds once
      and records either one unfiltered invocation (Decision A) or the two complementary,
      nonoverlapping PG16/PG18 shards (Decision B).
- [ ] Confirm all retained tests pass and required canonical cases do not unexpectedly skip.
- [ ] Confirm PostgreSQL starts, database/template creation counts, migration-chain counts,
      external processes, peak container concurrency, and final zero-container state.
- [ ] Re-inventory nonparallel collections and tests that mutate environment, current
      directory, console, browser globals, storage, or DOM-global state.
- [ ] The formal reviewer runs the measurement-expanded Frontend pre-PR command in §13.7:
      the same type-check/build/full sequence as `npm run test:pre-pr`, with the full suite
      executed once and default progress retained while adding JUnit.
- [ ] Record slowest 20 tests and classes/files from machine-readable results.
- [ ] Record exact before/after commands, environment, durations, skips, risks, and
      intentionally unrun E2E.
- [ ] Run Git diff checks without staging or committing unrelated work.
- [ ] The formal reviewer reconciles the complete diff, focused evidence, broad evidence,
      resource inventory, and report before issuing the prerequisite verdict.

**Tests during implementation:** The implementation agent runs exact catalog/lifecycle
contracts, Access, and route Smoke as listed above. The fresh formal reviewer owns the
single Backend and Frontend broad comparison commands in §13.7.

**Explicitly not run:** Playwright E2E unless separately triggered by a real browser-flow
change; Authorization Phase 3 tests because Phase 3 has not started.

**Rollback:** Any correctness regression identifies and rolls back the responsible
independent phase, then runs only its focused gate. A second broad measurement requires
explicit approval because the one-run budget has been consumed; do not filter around
failures or silently relabel the failed run as a baseline.

**Acceptance:** All success criteria in §10 and report sections requested by the audit
contract are evidenced.

**Review checkpoint:** Formal `engineering-review`.

**Independent commit:** Evidence/report may be committed independently when authorized,
proposed message `docs: record test runtime acceptance`. The repository’s feature-artifact
deletion rule still applies after review passes.

## 10. Measurable Success Criteria

### Lifecycle and coverage

- Ordinary Backend integration execution starts no more than one PostgreSQL 16 database
  container per test process.
- Two concurrent `dotnet test` processes cannot hold project PostgreSQL runtimes at the same
  time.
- No PostgreSQL 16/18 overlap occurs.
- Ryuk is inventoried separately and does not count as a PostgreSQL duplicate.
- Zero project-owned test containers remain after each test process exits.
- Every ordinary collection has a unique database or proven-safe schema.
- At most four ordinary database collection leases are active; Fast tests remain
  independently parallel.
- Ordinary head-schema fixtures apply the full migration chain once to the process template;
  per-collection clones do not reapply it. Staged migration tests report their own expected
  migration applications separately.
- Lease isolation and cleanup tests pass regardless of collection order; no fixture reads
  mutable state created by another collection.
- Static source contains one project-owned PG16 builder after Phase 4; after Phase 5 it
  contains either that builder alone (Decision A) or that builder plus one locked PG18
  canonical builder (Decision B).
- Migration-upgrade tests retain staged-from-previous-migration execution and every current
  collision/backfill/constraint refusal.
- Every Backend and Frontend test belongs to at least one validated execution gate.
- Focused Access execution selects `Api.Access` and `TestSupport.Access` without Pipeline or
  Frontend tests.
- Data Pipeline and canonical-data tests do not run for isolated authorization work.
- Broad Backend/Frontend suites execute once at the final milestone, not after each edit.
- All retained security, audit, migration, CLI, startup, rollback, and Quran-data guarantees
  pass.

### Runtime targets

These are target bands for measurement and regression detection, not guaranteed promises:

| Scope | Target |
| --- | --- |
| Backend pure focused feedback after build | 1–30 seconds |
| Backend focused DB class after build | 10–60 seconds |
| Frontend one-file focused feedback | 30–90 seconds, because Angular bundle startup remains |
| Complete Access lane | Establish the Phase 1 baseline first; provisional post-change planning band 60–180 seconds |
| Full Backend comparison | Provisional 400–500 seconds; investigate any result above 503 seconds (baseline +5%) and never trade coverage for the target |
| Full Frontend comparison | Provisional 285–330 seconds; primary gain comes from lane selection, so no large full-suite reduction is promised |

Implementation feedback is “materially reduced” when ordinary authorization work completes
through exact/Fast/Access DB gates without the 479-second Full Backend or 306-second Full
Frontend run, and its slice/review gates complete through Access + triggered Smoke + Tier B
without Pipeline/canonical/full Frontend execution. Record the Access before/after delta,
but do not fail correctness because an unsupported percentage reduction was not achieved.

## 11. Risk Analysis and Safeguards

| Risk | Safeguard and acceptance evidence |
| --- | --- |
| Shared-server state leakage | Unique database per collection, declared state policy within each collection, cross-database sentinel test, database dropped after fixture |
| Database-name collision/injection | Bounded generated names, monotonic counter plus random suffix, identifier validation and quoting |
| Connection-pool retention | Dispose factories/providers/data sources, clear lease pool, then bounded forced drop; assert database disappears |
| Failed cleanup | Independent cleanup steps collect errors; idempotent ProcessExit owner; exact-label script trap; Ryuk for hard death; final zero-container inspection |
| Concurrent `dotnet test` processes | OS-held exclusive file handle acquired before container creation; child-process contention test |
| Template masks migration defects | Template forbidden for staged migrations and pending-model checks; template itself built through real migrations once |
| PostgreSQL 18 archive incompatibility | Explicit Phase 5 proof; fallback uses two exhaustive, nonoverlapping invocations with locked exclusive PG18 and all canonical assertions |
| External non-disposable database | Normal gates unset overrides; optional external mode is read-only/non-owned/no-DDL; any future mutation requires the disposable sentinel plus local dedicated-server proof |
| Process-global leakage | Globally sequential collection plus disposable state scope for cwd, env, and console; failure-path tests |
| False speedup by skipped tests | Catalog/configuration validators, required skip accounting, comparable full run, no filter narrowing after failure |
| Filter accidentally omits tests | Reflection/discovery validator proves one class-catalog row, primary Gate partition, and full unfiltered/two-shard identity; scripts own filters |
| Shared PostgreSQL contention slows full run | Four-slot database semaphore, DDL semaphore, server/database timing evidence; Fast tests stay parallel and container-free |
| Cleanup script touches unrelated resources | Require owner, repository, kind, and exact run-ID labels; print exact IDs before removal; refuse blank run ID |
| Test-order dependence | Per-collection databases, existing reset contracts retained, shuffled/parallel representative run during lifecycle review |

---

## 12. Complete Phased Task List

1. Establish the validated Backend class/resource catalogs and stable lane commands.
2. Implement the labelled shared PG16 runtime, OS lock, template, leases, and cleanup.
3. Pilot Abwab, then migrate every ordinary fixture and dispose resource roots correctly.
4. Preserve staged Access migration isolation and harden process-global/process boundaries.
5. Prove PG18 archive compatibility with shared PG16 or retain an exclusive sequential PG18
   path with no overlap.
6. Add validated Frontend lanes, package commands, and browser-global cleanup.
7. Apply only the five proven test consolidation decisions.
8. Update the six entrypoints and living testing/lifecycle documentation.
9. Run one comparable final Backend and Frontend measurement and formal engineering review.

## 13. Required Final Implementation Handoff

### 13.1 Recommended target architecture

Use one process-static lazy, explicitly labelled PostgreSQL 16 Testcontainer per Backend
test process; xUnit v2 collection fixtures lease databases but never own the server.
Protect both PG16 and any PG18 fallback with one process-lifetime OS lock. Build one
migration-head template and clone an isolated database per ordinary collection. Keep
feature seed/reset logic local. Use an empty non-template database with per-case schemas
for Access migration-upgrade tests. Use PG16 for canonical Smoke only after a real
restore-and-assertion proof; otherwise run the PG16 and exclusive PG18 portions as
exhaustive, nonoverlapping invocations. Dispose providers and exact pools before databases,
dispose the server before releasing the lock, and retain exact-label cleanup plus Ryuk as
fallbacks.

### 13.2 Complete phased task list

The execution order is Phase 1 through Phase 9 in §9. No later phase may bypass an earlier
review checkpoint. Phase 5 chooses exactly one canonical restore path. Phase 7 cannot begin
until replacement guarantees pass. Phase 8 documents only verified commands. Phase 9 is the
only broad post-change measurement.

### 13.3 Proposed changed-file inventory

**New Backend infrastructure and gates**

```text
Backend/scripts/test-backend
Backend/scripts/cleanup-test-runtime
Backend/scripts/check-pending-model
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/TestGateCatalog.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/TestGateCatalogTests.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-gates.tsv
Backend/tests/QuranDashboard.Tests/TestSupport/Execution/test-resources.tsv
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlTestProcess.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlTestServer.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlDatabaseLease.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlSchemaLease.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/ExclusivePostgreSqlLease.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/CrossProcessPostgreSqlLock.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlResourceLabels.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlDatabaseName.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/PostgreSqlTestProcessContractTests.cs
Backend/tests/QuranDashboard.Tests/TestSupport/PostgreSql/README.md
Backend/tests/QuranDashboard.Tests/TestSupport/DependencyInjection/OwnedServiceProviderRegistry.cs
Backend/tests/QuranDashboard.Tests/TestSupport/DependencyInjection/OwnedServiceProviderRegistryTests.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Process/ProcessGlobalStateScope.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Process/ProcessExecution.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Process/ProcessGlobalStateScopeTests.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Process/ProcessExecutionTests.cs
```

**Modified Backend fixtures/tests**

The exact 21 current builder fixtures are:

```text
Backend/tests/QuranDashboard.Tests/Abwab/AbwabSchemaFixture.cs
Backend/tests/QuranDashboard.Tests/Api/Access/AccessMigrationTestFixture.cs
Backend/tests/QuranDashboard.Tests/Api/Access/AccessTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/FullI3rab/FullI3rabImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/FullI3rab/FullI3rabSchemaFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Import/ImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/MushafReader/MushafReaderTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Mutashabihat/MutashabihatImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Navigation/NavigationImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/DisplayWordsRealImportFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/WordsDisplayTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/RootsExplorerTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsSimpleI3rab/I3rabGenerationTestFixture.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesTestFixture.cs
Backend/tests/QuranDashboard.Tests/Smoke/SmokeApiFixture.cs
Backend/tests/QuranDashboard.Tests/Smoke/Data/SmokeDataFixture.cs
```

All 20 PG16 files move to shared leases; the PG18 file either moves to PG16 after proof or
uses the locked exclusive fallback. Additional modified/deleted Backend files are:

```text
Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj
Backend/tests/QuranDashboard.Tests/Abwab/AbwabTreeReadTests.cs
Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRollbackTests.cs
Backend/tests/QuranDashboard.Tests/Api/Access/AccessMigrationPathTests.cs
Backend/tests/QuranDashboard.Tests/Api/Access/AccessSchemaDriftTests.cs
Backend/tests/QuranDashboard.Tests/Api/Access/AccessAdminCommandTests.cs
Backend/tests/QuranDashboard.Tests/Api/Access/EmailIdentityNormalizerTests.cs
Backend/tests/QuranDashboard.Tests/TestSupport/Access/EmailIdentityContractTests.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesFixtureSmokeTests.cs
Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersFixtureSmokeTests.cs
Backend/tests/QuranDashboard.Tests/Quran/MushafReader/mushaf-reader-seed.sql
Backend/tests/QuranDashboard.Tests/Quran/Words/unique-words-seed.sql
Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/roots-explorer-seed.sql
Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/morphology-explorers-seed.sql
```

The four seed SQL files change only if their current header would become false after a
fixture moves from `EnsureCreated` to a migrated clone; seed statements and Quran test data
remain unchanged.

**New/modified Frontend test infrastructure**

```text
Frontend/quran-dashboard-ui/package.json
Frontend/quran-dashboard-ui/angular.json
Frontend/quran-dashboard-ui/testing/README.md
Frontend/quran-dashboard-ui/testing/verify-test-gates.mjs
Frontend/quran-dashboard-ui/src/test-setup.ts
Frontend/quran-dashboard-ui/src/app/app.nested-layers.spec.ts
Frontend/quran-dashboard-ui/src/app/core/navigation/idle-preload.strategy.spec.ts
Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/entity-detail-overlay-invariant.spec.ts
Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/entity-detail-overlay-host.component.spec.ts
Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/entity-detail-overlay-ayah-continuity.spec.ts
Frontend/quran-dashboard-ui/src/app/shared/ui/context-menu/context-menu.component.spec.ts
Frontend/quran-dashboard-ui/src/app/app.sanity.spec.ts
Frontend/quran-dashboard-ui/src/app/core/auth/auth.testing.spec.ts
```

**Policy/evidence**

```text
AGENTS.md
CLAUDE.md
Backend/AGENTS.md
Backend/CLAUDE.md
Frontend/quran-dashboard-ui/AGENTS.md
Frontend/quran-dashboard-ui/CLAUDE.md
TESTING_STRATEGY.md
Backend/tests/QuranDashboard.Tests/README.md
Backend/scripts/README.md
Frontend/quran-dashboard-ui/README.md
Frontend/quran-dashboard-ui/testing/README.md
Backend/api/QuranDashboard.Api/README.md
.claude/skills/test-guard/SKILL.md
.claude/skills/engineering-review/SKILL.md
.claude/skills/pr-context-prep/SKILL.md
docs/security/authorization-permissions-implementation-plan.md
Backend/report/testing/test-runtime-audit-and-execution-policy.md
```

No production project, migration, source package, importer implementation, API route, or
Authorization production file is in the proposed inventory.

### 13.4 Test execution-trigger matrix

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

### 13.5 Proposed AGENTS.md / CLAUDE.md wording

Replace the existing test-selection passage with the following exact text; do not append a
second competing policy. Use it in both root entrypoints:

**`AGENTS.md`**

```markdown
### Scope-aware test execution

Before selecting tests, inspect the changed files and read `TESTING_STRATEGY.md` plus the
nearest test README. Run the narrowest meaningful gate first. For Backend compilation
changes, build once, then use `Backend/scripts/test-backend --no-build`. Broad gates run once at
milestone, engineering-review, or pre-PR boundaries—not after individual edits.
Pipeline/canonical gates run only for their documented triggers, and Backend-only work does
not require Frontend tests.

Keep long output visible; never pipe it into `tail`. Use the configured hang timeouts, do
not run concurrent PostgreSQL test processes, and leave no Testcontainers running. Report
the exact gate, command, reason, result, skips, and cleanup state; there is no CI fallback.
The formal reviewer owns
final broad review gates. Deleting a test requires documented obsolete/redundant proof and
named replacement coverage. Commands and the trigger matrix live in
`TESTING_STRATEGY.md`.
```

**`CLAUDE.md`**

```markdown
### Scope-aware test execution

Before selecting tests, inspect the changed files and read `TESTING_STRATEGY.md` plus the
nearest test README. Run the narrowest meaningful gate first. For Backend compilation
changes, build once, then use `Backend/scripts/test-backend --no-build`. Broad gates run once at
milestone, engineering-review, or pre-PR boundaries—not after individual edits.
Pipeline/canonical gates run only for their documented triggers, and Backend-only work does
not require Frontend tests.

Keep long output visible; never pipe it into `tail`. Use the configured hang timeouts, do
not run concurrent PostgreSQL test processes, and leave no Testcontainers running. Report
the exact gate, command, reason, result, skips, and cleanup state; there is no CI fallback.
The formal reviewer owns
final broad review gates. Deleting a test requires documented obsolete/redundant proof and
named replacement coverage. Commands and the trigger matrix live in
`TESTING_STRATEGY.md`.
```

Replace the Backend test-selection passage with this exact text in both Backend
entrypoints:

**`Backend/AGENTS.md`**

```markdown
## Backend Test Selection

Read `../TESTING_STRATEGY.md` and
`tests/QuranDashboard.Tests/README.md`, inspect the changed scope, then use
`scripts/test-backend`. Start with an exact method/class or the narrowest feature/Access
lane. Build once, use `--no-build` afterward, and run Smoke, Tier B, Pipeline, canonical,
and pre-PR gates only at their documented triggers. Pipeline/canonical tests never run for
isolated authorization work.

Do not run database-bearing Backend commands concurrently: the shared runtime and OS lock
permit only one project-owned PostgreSQL database container at a time. Keep output visible,
never pipe to `tail`, retain the hang timeout, and confirm zero owned containers after
exit. Report the exact lane, command, reason, result, skips, and cleanup state; there is no
CI fallback.
The formal reviewer owns final broad review gates; test deletion requires documented
replacement coverage.
```

**`Backend/CLAUDE.md`**

```markdown
## Backend Test Selection

Read `../TESTING_STRATEGY.md` and
`tests/QuranDashboard.Tests/README.md`, inspect the changed scope, then use
`scripts/test-backend`. Start with an exact method/class or the narrowest feature/Access
lane. Build once, use `--no-build` afterward, and run Smoke, Tier B, Pipeline, canonical,
and pre-PR gates only at their documented triggers. Pipeline/canonical tests never run for
isolated authorization work.

Do not run database-bearing Backend commands concurrently: the shared runtime and OS lock
permit only one project-owned PostgreSQL database container at a time. Keep output visible,
never pipe to `tail`, retain the hang timeout, and confirm zero owned containers after
exit. Report the exact lane, command, reason, result, skips, and cleanup state; there is no
CI fallback.
The formal reviewer owns final broad review gates; test deletion requires documented
replacement coverage.
```

Replace the Frontend test-selection passage with this exact text in both Frontend
entrypoints:

**`Frontend/quran-dashboard-ui/AGENTS.md`**

```markdown
## Frontend Test Selection

Read `../../TESTING_STRATEGY.md` and `testing/README.md`, inspect the changed scope, then
use the `npm run test:*` commands. Start with one spec or the narrowest fast, feature, or
authorization lane. The full Frontend suite and production build run once at
engineering-review/pre-PR boundaries when Frontend files changed; Backend-only work with
no generated/frontend contract diff requires no Frontend test.

Preserve the two-fork Vitest cap and configured timeouts. Keep output visible, never pipe
to `tail`, and report the exact lane, command, reason, result, and skips. The formal
reviewer owns the final full Frontend gate; there is no CI fallback. Deleting a test requires documented
obsolete/redundant proof and named replacement coverage.
```

**`Frontend/quran-dashboard-ui/CLAUDE.md`**

```markdown
## Frontend Test Selection

Read `../../TESTING_STRATEGY.md` and `testing/README.md`, inspect the changed scope, then
use the `npm run test:*` commands. Start with one spec or the narrowest fast, feature, or
authorization lane. The full Frontend suite and production build run once at
engineering-review/pre-PR boundaries when Frontend files changed; Backend-only work with
no generated/frontend contract diff requires no Frontend test.

Preserve the two-fork Vitest cap and configured timeouts. Keep output visible, never pipe
to `tail`, and report the exact lane, command, reason, result, and skips. The formal
reviewer owns the final full Frontend gate; there is no CI fallback. Deleting a test requires documented
obsolete/redundant proof and named replacement coverage.
```

### 13.6 Tests proposed for deletion or consolidation

Apply exactly the five decisions in §8. Four files containing five runtime cases are
deleted because stronger behavior tests already exist or the assertion is a
framework/test-data self-check. One additional email-vector self-test is replaced by real
normalizer behavior. Expected direct body savings are approximately 0.15 seconds and do
not drive the architecture; the purpose is test quality and an honest gate inventory. No
unique security, migration, schema, rollback, audit, CLI, startup, or Quran-data coverage
is deleted.

### 13.7 Before/after measurement methodology

Use the same machine, branch state, staged canonical resources, warm Docker images, .NET
configuration, and Frontend fork cap as the audit. Record build and test wall time
separately.

Do not rerun a “before” suite. The completed audit's 479.46-second Backend and
306.50-second Frontend runs are the fixed before measurements; reuse their exact recorded
commands/environment. Run only the after comparisons below.

Backend comparison:

```bash
Backend/scripts/test-backend pre-pr \
  --results-dir /tmp/quran-dashboard-test-runtime-after/backend
```

The script records build and test wall time separately, emits TRX, and runs exactly one
complete class set. Under Decision B it writes separate PG16 and PG18 TRX files and an
aggregate wall time; the report explicitly notes that the safe two-process orchestration
differs from the single-process 479.46-second baseline. It never sums overlapping lanes.

Capture bounded Docker events for the exact test interval and calculate:

- PostgreSQL create/start/destroy counts;
- Ryuk create/destroy count;
- peak active PostgreSQL containers;
- database/template/schema creation counts;
- full migration-chain applications;
- external process launches;
- zero owned containers after exit.

The PostgreSQL runtime also writes one machine-readable lifecycle summary into the results
directory. Docker events prove container topology; the summary supplies template/database/
schema/migration counts that Docker cannot infer. Any event collector runs under a printed
PID, bounded to the test interval, and is terminated and waited in the script trap—never
left as hidden background work.

Frontend comparison:

```bash
cd Frontend/quran-dashboard-ui
npm run typecheck
npm run build:verify
npm test -- \
  --reporters=default \
  --reporters=junit \
  --outputFile=/tmp/quran-dashboard-test-runtime-after/frontend.junit.xml
```

Keep default progress visible while writing JUnit to the evidence path. Parse TRX/JUnit for
the slowest 20 tests and classes/files. Record Angular bundle generation and test-body time
separately when the builder reports them. Compare wall time, not summed parallel test-body
durations. Run each broad comparison once.

### 13.8 Safety and rollback strategy

- Implement on the current Authorization branch in independent, reviewable phase commits
  only after the user authorizes execution.
- Never combine lifecycle foundation, bulk fixture migration, PG18 decision, deletion, and
  documentation in one commit.
- Pilot one fixture before bulk migration.
- Preserve the staged migration lane outside the template path.
- Print exact labelled cleanup candidates before any removal and require the current run ID.
- Never clean an unlabelled container, development volume, or network.
- Revert the responsible phase commit if its focused acceptance fails; do not narrow a gate.
- Keep the canonical PG18 path available until PG16 compatibility is proven.
- Keep every deletion in one reversible phase with named replacement evidence.
- Do not create migrations or modify production/source/import behavior as a workaround.

### 13.9 Recommended engineering-review checkpoints

1. Phase 1: class/resource catalog and filter completeness.
2. Phase 2: shared-runtime concurrency, lock, ownership, and cleanup architecture.
3. Phase 3: per-fixture state isolation and provider/pool disposal.
4. Phase 4: migration/security refusal invariants and process-global restoration.
5. Phase 5: canonical Quran data evidence and PG16/PG18 exclusivity.
6. Phase 6: Frontend configuration completeness and global-state cleanup.
7. Phase 7: per-test replacement-coverage review.
8. Phase 8: policy/reference consistency.
9. Phase 9: formal `engineering-review` over the complete diff and final broad evidence.

### 13.10 Recommended model and reasoning level

| Phase | Recommended model | Reasoning |
| --- | --- | --- |
| 1 — taxonomy/scripts | `gpt-5.6-terra` | `high`; filter completeness and shell exit semantics |
| 2 — shared runtime | `gpt-5.6-sol` | `xhigh`; concurrency, process lifetime, Docker ownership |
| 3 — fixture migration | `gpt-5.6-sol` | `high`; broad integration and isolation correctness |
| 4 — migration/process | `gpt-5.6-sol` | `xhigh`; security refusals, staged migration, global state |
| 5 — canonical decision | `gpt-5.6-sol` | `xhigh`; Quran-data safety and cross-version PostgreSQL behavior |
| 6 — Frontend gates | `gpt-5.6-terra` | `high`; Angular builder constraints and order isolation |
| 7 — consolidation | `gpt-5.6-sol` | `high`; proof that coverage is not weakened |
| 8 — documentation | `gpt-5.6-terra` | `medium`; exact consistency across six entrypoints |
| 9 — verification/review | `gpt-5.6-sol` | `xhigh`; evidence reconciliation and formal review |

Use a fresh reviewer after each phase. Use the most capable model for the Phase 2, Phase 4,
Phase 5, and final whole-diff review.

### 13.11 Plan-only confirmation

- This artifact is a plan only.
- No implementation was performed while creating it.
- No Docker resource was stopped, removed, created, or changed.
- No test, fixture, script, README, instruction file, configuration, migration, production
  file, or Git state was changed other than adding this requested plan file.
- No Backend or Frontend baseline was rerun.
- Authorization Phase 3 was not started.
- The current branch remains `feature/security-authorization-permissions`.
