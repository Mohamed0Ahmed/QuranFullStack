# Quran source-read pipelines

Source-read and assemble half of Quran data pipelines. This folder reads staged inputs,
normalizes them, and assembles domain objects for the write-side pipeline.

## Folder map

- `Foundation/` — reads staged surah/ayah/word/page layout foundation package.
- `FullI3rab/` — reads staged full-i3rab source files and assembles ayah-level markup entries.
- `Mutashabihat/` — reads staged similar-phrase datasets and assembles group/occurrence models.
- `Navigation/` — reads staged navigation metadata datasets and assembles navigation records.
- `Tafsirs/` — reads staged tafsir manifests and source text entries.
- `Translations/` — reads staged translation sources plus display metadata.
- `Words/` — word-specific pipelines:
  - `MorphologyImporting/` — staged morphology-package readers, corrections, and assembly.
  - `SimpleI3rabGeneration/` — seeded simple-i3rab generation; no external source package.

## Import vs generate

- Everything here except `Words/SimpleI3rabGeneration/` is an **import** pipeline: it expects
  staged source files and usually a manifest-backed source root.
- `Words/SimpleI3rabGeneration/` is a **generate** pipeline: it derives output from seeded rules
  and existing structured data, not from an external Quran source package.

## Boundaries

- This side reads, validates, normalizes, and assembles. It does not own bulk DB writes,
  post-write validation runners, or import report persistence.
- Write-side counterparts live in `../../../Persistence/DataPipelines/Quran/README.md`.
- If a child folder has its own README, treat that leaf README as the current truth for that domain.

## Leaf READMEs

- `Foundation/README.md`
- `Words/MorphologyImporting/README.md`
- `Words/SimpleI3rabGeneration/README.md`

## Invariants

- Preserve source traceability; do not silently modify staged source packages.
- Keep import/generate distinction explicit in code and docs; generated data is not a disguised import.
- Shared domain identity rules established by foundation and morphology assembly flow downstream;
  do not change them casually here.
