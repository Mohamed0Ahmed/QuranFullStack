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
- `TestSupport/` — shared helpers used across clusters; today this holds logging capture under
  `TestSupport/Logging/RecordingLoggerProvider.cs`.

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
- Synthetic packages/helpers are acceptable for structural or validation scenarios only when they
  do not fabricate scripture content.
- Many clusters use real PostgreSQL infrastructure and EF migrations through shared fixtures;
  keep fixture reuse local to the owning domain instead of centralizing feature-specific setup.

## Related

- Backend map: `../../README.md`
- Report/evidence conventions: `../../report/README.md`
