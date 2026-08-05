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
  composition once under `ASPNETCORE_ENVIRONMENT=Testing` over a Testcontainers
  `postgres:16-alpine`, and drives every registered route through routing, authorization,
  model binding, and serialization. `SmokeRouteCatalog` is bidirectionally locked to the
  live `EndpointDataSource` by `SmokeCoverageParityTests`, so **adding or changing an API
  route requires updating the catalog in the same change** or the suite fails by route name.
  Three personas (anonymous / authenticated-unknown-sub / owner) run over the real
  JwtBearer handler with RSA test tokens.
- `Smoke/Data/` — the data tier (`QuranDashboard.Tests.Smoke.Data`), which restores the
  canonical Quran dump so the seeded read routes are asserted against real data instead of
  an empty schema. See the dump note under *Related*.
- `TestSupport/` — shared helpers used across clusters: `Access/` personas and email-identity
  vectors, `Http/ApiEnvelope.cs`,
  `DependencyInjection/OwnedServiceProviderRegistry.cs` (disposes fixture-owned root
  `ServiceProvider`s in reverse creation order), `Execution/` (the `test-gates.tsv` /
  `test-resources.tsv` catalogs the `Backend/scripts/test-backend` lanes read),
  `Logging/RecordingLoggerProvider.cs`, and `PostgreSql/` — the one shared
  `postgres:16-alpine` runtime, its migrated template, and the per-collection database leases
  the Access, explorer, FullI3rab, foundation Import, Mutashabihat, Navigation, Tafsirs, and
  Translations fixtures take instead of starting their own container. The two FullI3rab
  collections lease separately — `FullI3rabImportTestFixture` and `FullI3rabSchemaFixture`
  never share one database. The remaining pipeline fixtures (WordsDisplay, WordsMorphology,
  WordsSimpleI3rab), the Access migration fixture, and the smoke fixtures still build their
  own container.
- `TranslationImportTestFixture` owns exactly one root `ServiceProvider` for its collection and
  reaches it through `CreateScope()`. Constructed directly, without `InitializeAsync`, it leases
  nothing and starts nothing: `WriteSyntheticPackageAsync` works, every database helper throws.
  Four `Kind=Fast` classes in `Quran/Translations/` construct it purely to write a synthetic
  package, and the container-free `fast` lane must stay container-free.

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
- Synthetic packages/helpers are acceptable for structural or validation scenarios only when they
  do not fabricate scripture content.
- Many clusters use real PostgreSQL infrastructure and EF migrations through shared fixtures;
  keep fixture reuse local to the owning domain instead of centralizing feature-specific setup.

## Related

- Backend map: `../../README.md`
- Report/evidence conventions: `../../report/README.md`
- Which tests to run and when: `../../../TESTING_STRATEGY.md` — execution tiers A–E, the
  dot-bounded namespace filters, and the pipeline triggers. Note §8: there is no CI, so
  every tier is a local gate.
- `resources/db-dumps/quran-canonical/` (`quran-canonical.dump` + `manifest.json`) is
  **produced by `../../scripts/create-smoke-dump` and consumed by `Smoke/Data/`**. It is a
  derived cache of the canonical import — never synthetic, never a substitute for the
  staged sources under `resources/import-sources/`. The gate has two deliberately opposite
  verdicts: **absent → the whole data tier skips** (an ordinary machine that never generated
  it), **present but stale or corrupt → it throws loud** (sha256 mismatch against the
  manifest, or a manifest migration id that is not this tree's head). A stale dump quietly
  skipping is the one failure `Smoke/Data/SmokeDumpGate.cs` exists to make impossible.
  Regenerate with `Backend/scripts/create-smoke-dump --yes`; never hand-edit either file.

### Why `Smoke/Data/` runs postgres **18** while every other fixture runs **16**

The dump is written by the host's `pg_dump`, which is **18.4**, and `pg_restore` refuses an
archive whose header comes from a newer `pg_dump` than itself — a `postgres:16-alpine`
restore fails with "unsupported version in file header". So `SmokeDataFixture` pins
`postgres:18-alpine` while `SmokeApiFixture`, `AccessTestFixture`, and every pipeline
fixture stay on `postgres:16-alpine`. `SmokeDumpGate` checks the manifest's `pgDumpVersion`
against the restore image's major version *before* starting the container, so a producer
upgrade (say to 19) reports the mismatch by name instead of failing mid-restore. Do not
"fix" the divergence by downgrading the producer: the schema-owning fixtures and the
restore fixture are independent choices, and the restore image must be ≥ the producer.
