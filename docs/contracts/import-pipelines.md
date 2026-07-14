# Import pipelines & CLI verbs

Index only — defers to the linked code + README, which are the authority. See [docs/contracts/README.md](./README.md).

Covers the operator-only DataImporter verbs, the file→DB data pipelines, and the
validation / import report outputs. This page does **not** restate verb lists, manifest
schemas, report shapes, or output paths — **importer verbs, report locations, and
source-safety rules live in the code + these READMEs.**

## Authoritative sources

- CLI verbs / importer host → [`DataImporter/README.md`](../../Backend/tools/QuranDashboard.DataImporter/README.md)
- File data pipelines (overview) → [`Files/Quran/DataPipelines/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/README.md)
  - Foundation → [`DataPipelines/Foundation/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Foundation/README.md)
  - Morphology importing → [`DataPipelines/Words/MorphologyImporting/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/README.md)
  - Simple i3rab generation → [`DataPipelines/Words/SimpleI3rabGeneration/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/SimpleI3rabGeneration/README.md)
- Persistence data pipelines → [`Persistence/DataPipelines/Quran/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/README.md)

**Precedence:** importer code + pipeline READMEs win.
