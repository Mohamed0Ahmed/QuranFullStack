# Backend test suite

Integration-heavy backend test suite for API, read models, import pipelines, and generated data.
Folders are clustered by Quran domain/use case, not by project layer.

## Folder map

- `Api/Middleware/` — HTTP-boundary tests for global exception handling.
- `Api/Access/` — first-login provisioning, Owner reconciliation, permission catalogues, scoped
  authorization state, requirement handlers, controlled denial envelopes, authorization metadata
  validation, and real-PostgreSQL Owner administration transition/relink/audit atomicity coverage.
- `Quran/Import/` — foundation import, validation, reconstruction, and source-staging checks.
- `Quran/MushafReader/` — retained corrupt-data and word-analysis fallback protections.
- `Quran/Mutashabihat/`, `Navigation/`, `Tafsirs/`, `Translations/`, `FullI3rab/` — retained
  source, import, validation, rollback, and schema protections per pipeline.
- `Quran/WordsWordTypes/` — the retained child-catalogue drift gate. Explorer fixture folders
  remain only where the resource catalog or retained helpers still consume them.
- `Quran/WordsMorphology/`, `WordsMorphologyEnriched/`, `WordsSimpleI3rab/`, `WordsDisplay/` —
  morphology import, enriched morphology, generated simple i3rab, and display-word rebuild coverage.
- `Smoke/` — the route-smoke tier (`QuranDashboard.Tests.Smoke`). Boots the real API
  composition once under `ASPNETCORE_ENVIRONMENT=Testing` over a migrated-but-empty database
  leased from the shared `postgres:16-alpine` runtime — the sweep's expectations are derived
  against an empty schema, so nothing seeds it — and drives every registered route through
  routing, authorization, model binding, and serialization. `SmokeRouteCatalog` is
  bidirectionally locked to the live `EndpointDataSource` by `SmokeCoverageParityTests`,
  so **adding or changing an API route requires updating the catalog in the same change**
  or the suite fails by route name.
  `SmokePersona` is the closed caller-state enum in `Smoke/SmokePersonas.cs`; its RSA test tokens run
  through the real JwtBearer handler, and `TestAccessPersonasContractTests` pins the complete current
  state set. The twenty-one Abwab writes use a data-driven authorization matrix for every current
  persona: denied requests must leave Abwab rows and validators unchanged and issue no Abwab SQL, while
  the exact direct grant and active Owner reach the existing domain outcome. Focused Abwab smoke tests
  cover the four anonymous reads, the required relation/template status contracts, and conditional GET
  match/mismatch behavior. The twelve Owner-only access-administration routes are catalogued and dispatched
  under anonymous and active direct-grant personas so only an active local Owner can reach their actions.
- `Smoke/Data/` — the data tier (`QuranDashboard.Tests.Smoke.Data`), which restores the
  canonical Quran dump so the seeded read routes are asserted against real data instead of
  an empty schema. See the dump note under *Related*.
- `TestSupport/` — shared helpers used across clusters: `Access/` personas and email-identity
  vectors, `Http/ApiEnvelope.cs`,
  `DependencyInjection/OwnedServiceProviderRegistry.cs` (disposes fixture-owned root
  `ServiceProvider`s in reverse creation order), `Execution/` (the `test-gates.tsv` /
  `test-resources.tsv` catalogs the `Backend/scripts/test-backend` lanes read),
  `Process/` (`ProcessGlobalStateScope` applies and restores the current directory, named
  environment variables, and `Console.Out`/`Console.Error` — restoring on every exit path,
  including a scoped body that throws, a boundary that fails while entering, and a restore
  step that itself fails, which it records in `RestoreFailures` instead of throwing from
  `Dispose`; `ProcessExecution` runs a child process draining stdout and stderr
  concurrently — a sequential drain deadlocks once either stream fills the pipe buffer —
  with a two-minute default timeout that kills the entire process tree, waits for
  termination, and still returns what was drained),
  `Logging/RecordingLoggerProvider.cs`, and `PostgreSql/` (**its own
  `TestSupport/PostgreSql/README.md` owns the mechanics** — lease shapes, template eligibility,
  the five cleanup labels, the cross-process lock, disposal order, and the external-database
  prohibition) — the one shared
  `postgres:16-alpine` runtime, its migrated template, and the per-collection database leases
  the Access, explorer, FullI3rab, foundation Import, Mutashabihat, Navigation, Tafsirs,
  Translations, WordsDisplay, WordsMorphology, WordsSimpleI3rab, and route-smoke fixtures take
  instead of starting their own container. The two FullI3rab collections lease separately — `FullI3rabImportTestFixture` and
  `FullI3rabSchemaFixture` never share one database — and so do the two WordsDisplay
  collections: `WordsDisplayTestFixture` (synthetic seed) and `DisplayWordsRealImportFixture`
  (real foundation import) each take their own lease. `AccessMigrationTestFixture` leases an
  **empty** database from the same runtime and hands each case its own `PostgreSqlSchemaLease`
  (a unique schema plus `SearchPath`, dropped `CASCADE` on disposal): staged upgrades replay the
  real migration chain from nothing, so the migrated template is forbidden to them and
  `AccessMigrationPathTests` proves both halves — one case's migration history and tables never
  appear in another case's schema, and the leased database's `public` schema stays relation-free,
  which no head-template clone could be. Cases whose starting point *is* migration head — the live
  schema-drift mutations, the fresh-head preflight acceptance, and the retired-permission refusals —
  live in `AccessSchemaDriftTests`, which takes its own migrated clone per case.
  `PermissionCatalogueStartupSyncTests` takes its own migrated clone the same way, and for the same
  reason it cannot use `AccessCollection`: its subject is what a **never-synchronized** database does
  when the API host boots, which a shared fixture that every other case has already populated cannot
  show. Its one degraded-startup case leases no database at all — it boots the sync **enabled**
  against an unreachable connection string and asserts the host still starts, still answers a
  database-free 400, and logged the failure at Error, which is the whole of the *start degraded,
  never refuse to start* policy in `WebApplicationExtensions.SynchronizePermissionCatalogueAsync`.
  It is also the one Access class that boots hosts with the startup catalogue sync switched
  **on**; outside `Smoke/SmokeApiHost` every other API factory switches it off (see the
  `Access:PermissionCatalogueStartupSync` note below). Those classes and
  `AccessAdminCommandTests` — whose valid wrapper run also takes its own migrated clone — share only
  the `DisableParallelization = true` `AccessProcessGlobalCollection` with the staged class, never
  its empty-database fixture. Those four classes are the whole collection, and it is non-parallel
  because together they mutate the current directory, the connection environment variable, and the
  console streams. `AccessAdminCommandTests` owns the only two child-process launches under
  `Api/Access/`:
  a valid `identity scan` through `Backend/scripts/access-admin` and one unreachable-database
  `authorization preflight` proving the wrapper propagates exit code 4 with no stack trace. Parsing,
  executable-directory configuration, and every migration permutation stay in-process.
  `Smoke/Data/SmokeDataFixture` is the one fixture that cannot join that runtime, so it takes an
  **exclusive server lease** (`PostgreSqlTestProcess.LeaseExclusiveServerAsync`) instead: its own
  `postgres:18-alpine` container. Why it must, and what keeps the two majors from ever running at
  once, is below and in `TestSupport/PostgreSql/README.md`.
- `AccessTestFixture.ResetAsync` is the whole `ResetPerTest` contract the `AccessCollection` row of
  `TestSupport/Execution/test-resources.tsv` declares: it truncates `users` **and** `permissions`
  with `RESTART IDENTITY CASCADE`, which reaches `user_permissions` and `access_audit_events`
  through their foreign keys, and leaves the migration-seeded `roles` untouched — those four tables
  are everything the collection mutates. `permissions` belongs in that list because
  `PermissionCatalogueSynchronizerTests` proves the synchronizer never deletes an unknown code, so
  the `future.example` row it writes would otherwise outlive the case that wrote it.
  `AccessCollectionResetContractTests` fails if the truncation list ever narrows again.
- **`Access:PermissionCatalogueStartupSync:Enabled=false` is mandatory in an API test factory unless
  the class is testing the startup sync itself.** `Program.cs` synchronizes the permission catalogue
  between `UseApiPipeline()` and `app.Run()`, and every `WebApplicationFactory` here boots that real
  entry point. `AccessTestFixture` sets it off because `ResetAsync` truncates `permissions` and each
  case opts into the sync it asserts, so a lazy host build would otherwise steal those inserts;
  `Api/RateLimiting/RateLimitingApiFactory` sets it off because it points at a deliberately dead
  database and must not spend the startup budget failing to reach it;
  `Quran/WordsWordTypes/WordTypesTestFixture` sets it off because its connection string
  can come from `ExternalReadOnlyDatabaseOptIn.TryLease(WordTypesConnectionVariable)` — a database
  this process does not own, whose read-only contract is an opt-in convention rather than a
  connection-level grant, so nothing but this flag would stop the sync writing to it.
  `Smoke/SmokeApiHost` — the single composition behind both `SmokeApiFixture` and
  `Smoke/Data/SmokeDataFixture`, and the only host that leaves it **on** — does so deliberately: it
  leases a real migrated database, so its green `/api/health` is evidence that the boot-time sync
  works end to end.
- `SmokeApiFixture.ResetAsync` is the whole `ResetPerTest` contract the `SmokeCollection` row of that
  catalog declares, and it is deliberately the collection's **only** restore entry point: it truncates
  `users` and the six `abwab_*` tables with `RESTART IDENTITY CASCADE`, resets the fake profile source,
  and invalidates the abwab read caches. That last
  step is not housekeeping — raw SQL never moves `AbwabCacheGeneration`'s counter, so
  `CachedAbwabTreeReader` keeps serving the truncated tree from `IMemoryCache` until something does.
  Retained cases that read or write one of those tables call it first; write cases restore before
  their case rather than after. `SmokeBootGuardTests` and `SmokeCoverageParityTests` assert
  composition rather than data and call nothing.
- `AbwabSchemaTestCollection` is `UniqueKeyIsolation`: its cases share one database for the whole run
  and do not restore it, so retained cases create uniquely keyed rows and assert only their own keys;
  classes inside one collection never run concurrently.
- `TranslationImportTestFixture` owns exactly one root `ServiceProvider` for its collection and
  reaches it through `CreateScope()`; every helper on it needs the collection, and throws without
  one. Writing a synthetic source package needs no database, so it lives in the disposable
  `TranslationSyntheticPackage` (temp dirs in, `Dispose()` deletes them) — the fixture owns one and
  disposes it after releasing its lease, and the `Kind=Fast` classes in `Quran/Translations/`
  own their own, which is what keeps the container-free `fast` lane container-free.
- `MorphologyImportTestFixture` serves plain cases from one root `ServiceProvider` and gives
  `CreateScope(configure)` its own extra root, because several cases replace singletons such as
  `IWordLemmaNormalizationReader`. `configure` must run after `AddMorphologyImportServices()` —
  last registration wins is what makes those replacements take effect. Every root, plain or
  overridden, is registry-owned and disposed before the lease.
- `I3rabGenerationTestFixture` serves every read and reset helper from one registry-owned root
  through `CreateScope()`, and builds one throwaway root per `RunGenerationAsync` call because
  each run needs its own `I3rabExpectedCounts` singleton and, for the tamper theories, its own
  `configure` overrides. That root is disposed inside the call — `GenerateI3rabResult` is a
  record of primitives, so nothing survives it — which keeps every generation run from leaving a
  live connection pool behind against one leased clone. `configure` must stay last: the tamper
  cases replace `II3rabAssembler` and `II3rabGenerationWriteProbe`, and last registration wins.

## Retained lanes and gates

`TestSupport/Execution/test-gates.tsv` is the source of truth for all retained classes. The daily
pair is `Backend/scripts/test-backend smoke` plus `Backend/scripts/test-backend tier-b`; their union
selects every Permanent class and no Gate class. Change and release gates are:

| Lane | Trigger/selection |
|---|---|
| `gate-contract` | Any retained row with a non-empty `Concerns` value |
| `pipeline` | `Gate=Pipeline` and `Kind!=Canonical`; `canonical-data` owns every excluded canonical class |
| `canonical-data` | Every `Kind=Canonical` row, including the nine canonical pipeline rows and canonical smoke-data class |
| `migration` | Migration tooling and migration-path work |
| `access-db` | `Feature=Access` and (`Kind=Database` or `Concerns` contains `Schema`); `gate-contract` owns the excluded non-Access `AbwabSchemaTests` and `WordTypesChildCatalogueDriftTests` |
| `pre-pr` | Mandatory pre-release gate over every retained row |

Each gate runs on its own trigger; a daily run does not substitute for it. The focused `fast`,
`access`, `process`, and `feature` lanes are diagnostic entry points. Flags, discovery behavior,
resource preflights, and sharding mechanics live in `../../scripts/README.md`.

`Concerns` is reserved for change-gate families, never for daily membership. The accepted vocabulary
is `Authorization` (authorization contract gates), `Cli` (command-line contract gates), `Execution`
(shared test-runtime contracts), `Schema` (schema and catalogue integrity gates), and `Startup`
(startup/preflight contracts). The current catalog uses `Schema` on 5 rows, `Execution` on 7, and
`Startup` on 1; `Authorization` and `Cli` are accepted but currently unused. Permanent rows keep
`Concerns` empty; `gate-contract` selects every retained row with a non-empty value.

## Navigation conventions

- Start with the feature/domain folder that matches the backend area you are changing.
- Shared fixtures usually live inside the owning cluster, named after that area
  (`ImportTestFixture`, `MushafReaderTestFixture`, `WordTypesTestFixture`, and similar).
- SQL seed files live beside the cluster that consumes them when real read-model shape matters.

## Invariants

- **Quran-source safety first.** Do not invent Quran text, tafsir, translations, morphology,
  or other religious content in tests.
- Source-backed tests should keep using staged packages under `resources/import-sources/` and
  fixture wiring that preserves provenance.
- A source-backed fixture decides on the staged package **before** it acquires a database. When
  `resources/import-sources/quran-foundation/` is absent, `Quran/Import/FoundationImportSourceGate.cs`
  skips every foundation-import case and `ImportTestFixture.InitializeAsync` returns without
  leasing — so a run started outside `Backend/scripts/test-backend` (an IDE, a plain `dotnet test`)
  starts no server at all. The runner refuses the lane earlier still, in its canonical preflight.
  `Quran/WordsDisplay/CanonicalImportSourceTestGate.cs` gates the real display-words import the
  same way: every `DisplayWordsRealImportIdentityLinksTests` case skips and
  `DisplayWordsRealImportFixture.InitializeAsync` returns before it leases anything.
- `WordsDisplayTestFixture.CreateHandler()` hands out one child scope per call, registered through
  `OwnedServiceProviderRegistry` so reverse-order disposal releases every scope before the root
  provider and before the lease, and so one failing scope disposal cannot strand the leased
  database. Every call must get its own `QuranDashboardDbContext` — do not resolve the rebuild
  handler from the root provider and reuse it across runs: `SqlDisplayWordsRebuilder` opens the
  underlying connection itself, outside EF's bookkeeping, and never closes it, so a reused context
  enters the next rebuild with its connection already open.
- Synthetic packages/helpers are acceptable for structural or validation scenarios only when they
  do not fabricate scripture content.
- Many clusters use real PostgreSQL infrastructure and EF migrations through shared fixtures;
  keep fixture reuse local to the owning domain instead of centralizing feature-specific setup.

## Related

- Backend map: `../../README.md`
- Report/evidence conventions: `../../report/README.md`
- Which tests to run and when: `../../../TESTING_CONSTITUTION.md` and the active plan's
  `Testing Decision`. This README owns lane membership; `../../scripts/README.md` owns command
  mechanics. There is no CI, so every selected lane is a local gate that nothing verifies ran.
- How the shared database runtime works: `TestSupport/PostgreSql/README.md`.
- `resources/db-dumps/quran-canonical/` (`quran-canonical.dump` + `manifest.json`) is
  **produced by `../../scripts/create-smoke-dump` and consumed by `Smoke/Data/`**. It is a
  derived cache of the canonical import — never synthetic, never a substitute for the
  staged sources under `resources/import-sources/`. **Under `../../scripts/test-backend` an
  absent dump is a lane failure, not a skip:** the runner preflights the dump and its manifest
  before anything starts and exits non-zero with `canonical data tier: failed preflight`
  (`../../scripts/test-backend:501-519`). The in-test gate's own verdicts apply only to a run
  started outside the runner — an IDE, a plain `dotnet test` — where they exist so nothing
  starts a server it cannot use: **absent → that tier's cases skip**, **present but stale or
  corrupt → it throws loud** (sha256 mismatch against the manifest, a manifest migration id that
  is not this tree's head, or a producer major the restore image cannot read — all checked
  before the container starts). A stale dump quietly skipping is the one failure
  `Smoke/Data/SmokeDumpGate.cs` exists to make impossible.
  Regenerate with `Backend/scripts/create-smoke-dump --yes`; never hand-edit either file.

### Why `Smoke/Data/` runs postgres **18** while every other fixture runs **16**

The dump is written by the host's `pg_dump`, which is **18.4**, and `pg_restore` refuses an
archive whose header comes from a newer `pg_dump` than itself — a `postgres:16-alpine`
restore fails with "unsupported version in file header" (measured: `pg_restore --list` on a
16 client exits 1 with `unsupported version (1.16) in file header`). So `SmokeDataFixture`
pins `postgres:18-alpine` while `SmokeApiFixture`, `AccessTestFixture`, and every pipeline
fixture stay on `postgres:16-alpine`. `SmokeDumpGate` checks the manifest's `pgDumpVersion`
against the restore image's major version *before* starting the container, so a producer
upgrade (say to 19) reports the mismatch by name instead of failing mid-restore. Do not
"fix" the divergence by downgrading the producer: the schema-owning fixtures and the
restore fixture are independent choices, and the restore image must be ≥ the producer.

Restoring the 18 archive onto a **16 server** with the host's 18 client does not close the
gap either, and was measured rather than assumed: every `pg_restore` ≥ 17 emits
`SET transaction_timeout = 0` in its fixed output state, PostgreSQL 16 rejects that unknown
GUC, and the restore exits 1 (`unrecognized configuration parameter "transaction_timeout"`)
with no client flag or server setting that suppresses it. The divergence is therefore
structural, and the separation is at the **process** level: a lane that would run both
majors runs as two sequential `dotnet test` invocations — every other class first on the
shared 16 runtime, then exactly `SmokeDataReadTests` on the exclusive 18 server. Not one
canonical assertion moves in either direction; `Backend/scripts/test-backend` builds the two
filters from the catalog, and `TestGateCatalogTests` proves they never overlap and always
add back up to the whole lane.
