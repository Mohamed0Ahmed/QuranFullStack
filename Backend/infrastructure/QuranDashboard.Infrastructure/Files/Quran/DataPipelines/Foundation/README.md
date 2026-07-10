# Foundation import (source-read pipeline)

**Layer:** Infrastructure · source read + assemble · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `LOGGING_GUIDELINES.md`

## What this area does

Reads the staged Quran foundation package (surahs, ayahs, words, and Mushaf page/line layout)
and assembles it for the write pipeline. This is the **base import every other domain depends
on** — it establishes the word identities and Mushaf layout that morphology/i3rab/tafsir/etc.
attach to. CLI: `import-foundation --source <path>` (source is **required**; no default path).

## Key pieces

- `QuranImportSource.cs` — `LoadAsync(sourceRoot)`; resolves files via the manifest.
- `ManifestReader.cs`, `JsonLayoutSourceReader.cs`, `JsonMetadataSourceReader.cs`,
  `JsonWordSourceReader.cs` — read layout, surah/ayah metadata, and words.
- Validation lives in `application/.../Quran/DataPipelines/Foundation/Validation/`
  (`QuranImportValidator` + checks: `ImlaeiCleanKeyCheck`, `IdContiguityCheck`,
  `LayoutCoverageCheck`, `PageReconstructionCheck`, `SourceAlignmentCheck`,
  `DenormPlacementCheck`). Check ids/counts/verdicts are the acceptance contract.

## Current invariants / caveats (read before changing)

- **Word identity key is clean imlaei-simple** — bound here (`ImlaeiCleanKeyCheck`). Uthmani is
  display only. This decision propagates to every downstream explorer/read model; do not change it.
- **IDs are contiguous and order-significant** (`IdContiguityCheck`); layout must fully cover the
  Mushaf (`LayoutCoverageCheck` / `PageReconstructionCheck`). A failing check fails the import.
- **Foundation seeds first** — reset/reseed order starts here (see
  `Backend/report/database-inventory/database-reset-and-seeding-order.md`).
- **Do not silently modify source data**; preserve traceability to the staged package.

## Related

- Write mechanics: `../../../Persistence/DataPipelines/Quran/README.md`.
- CLI: `tools/QuranDashboard.DataImporter/README.md`. DB baseline: `Backend/report/database/`.
