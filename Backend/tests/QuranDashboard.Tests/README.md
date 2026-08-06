# Backend test suite

Integration-heavy backend test suite for API, read models, import pipelines, and generated data.
Folders are clustered by Quran domain/use case, not by project layer.

## Folder map

- `Api/Middleware/` — HTTP-boundary tests for global exception handling.
- `Quran/Import/` — foundation import, validation, reconstruction, and source-staging checks.
- `Quran/MushafReader/` — page reader, ayah study, similar ayahs, mutashabihat, catalogs,
  word analysis, and cache behavior.
- `Quran/Mutashabihat/`, `Navigation/`, `Tafsirs/`, `Translations/`, `FullI3rab/` — domain import
  and report-shape coverage per pipeline.
- `Quran/Words/` — Unique Words explorer reads and logging.
- `Quran/WordsRoots/`, `WordsMorphologyExplorers/`, `WordsWordTypes/` — explorer read-model tests
  for Roots, Lemmas/Stems, and Word Types.
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
  Every `SmokePersona` — the closed enum in `Smoke/SmokePersonas.cs`, swept through
  `SmokePersonas.All` rather than a hand-kept list — runs over the real JwtBearer handler with
  RSA test tokens. Adding a persona to the enum therefore widens the sweep; nothing has to be
  updated alongside it.
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
  live in `AccessSchemaDriftTests`, which takes its own migrated clone per case. That class and
  `AccessAdminCommandTests` — whose valid wrapper run also takes its own migrated clone — share only
  the `DisableParallelization = true` `AccessProcessGlobalCollection` with the staged class, never
  its empty-database fixture. Those three classes are the whole collection, and it is non-parallel
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
- `SmokeApiFixture.ResetAsync` is the whole `ResetPerTest` contract the `SmokeCollection` row of that
  catalog declares, and it is deliberately the collection's **only** restore entry point: it truncates
  `users` and the six `abwab_*` tables with `RESTART IDENTITY CASCADE`, resets the fake profile source,
  evicts every persona from the shared role cache, and invalidates the abwab read caches. That last
  step is not housekeeping — raw SQL never moves `AbwabCacheGeneration`'s counter, so
  `CachedAbwabTreeReader` keeps serving the truncated tree from `IMemoryCache` until something does.
  Every case that reads or writes one of those tables calls it first, the two empty-schema sweeps
  (`SmokeRoutePipelineTests`, `SmokePublicReadRegressionTests`) included: the write cases restore
  *before* their case rather than after, so their rows outlive them, and the id-scoped abwab reads
  derive 404 only while those tables are empty. `SmokeBootGuardTests` and `SmokeCoverageParityTests`
  assert composition rather than data and call nothing. `SmokeCollectionResetContractTests` dirties
  all seven tables, reads them back empty, and fails if the cache invalidation is dropped.
- `AbwabSchemaTestCollection` is `UniqueKeyIsolation`: its cases share one database for the whole run
  and do not restore it, so each creates uniquely keyed rows and asserts only its own keys.
  `AbwabCollectionKeyIsolationTests` is that policy's regression — the representative write sequence
  three times over with no restore between, read back through the production tree reader. Fixed keys
  collide on the second write (section name is unique, door name is unique within
  `(section, parent)`, both filtered to live rows), and a case that began asserting table totals
  instead of its own keys fails the scoped reads. `AbwabTreeReadTests` is the one case that truncates
  first, because "empty" is its subject; classes inside one collection never run concurrently.
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
- Which tests to run and when: `../../../TESTING_STRATEGY.md` — the Backend lanes of
  `../../scripts/test-backend` (§3), the Frontend lanes (§4), and the execution-trigger matrix
  (§5). Note §8: there is no CI, so every lane is a local gate that nothing verifies ran.
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
