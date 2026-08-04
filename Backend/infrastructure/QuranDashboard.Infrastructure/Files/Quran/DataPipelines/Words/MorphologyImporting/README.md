# Morphology Importing (source-read pipeline)

**Layer:** Infrastructure · source read + assemble · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `BACKEND_STRUCTURE.md`, `LOGGING_GUIDELINES.md`

## What this area does

Reads a staged word-level morphology source package and assembles it into the objects
the write pipeline persists into `quran_word_morphology` (and the lemma/root/stem/pos
dimension tables). This is the source-read half; the DB-write half lives in
`Persistence/DataPipelines/Quran/Words/MorphologyImporting/`.

Two source shapes are supported side-by-side (Feature 020):

- **Legacy multi-file** (`quran-morphology`) — the original manifest readers
  (`JsonQulReaders`, `JsonAlignedCorpusReader`, `MorphologyManifestReader`).
- **Enriched / corpus value-based** (`quran-enriched-morphology`, `--enriched`) —
  `Enriched/` (`EnrichedMorphologyReader`, `EnrichedMorphologyManifestReader`,
  `EnrichedDimensionBuilder`, `EnrichedMorphologyDryValidator`,
  `EnrichedMorphologyImportSource`). Kept distinct so the legacy path stays runnable
  for parity/diff until the cleanup phase.

## Key pieces

- `MorphologyImportSource.cs` / `MorphologyAssembler.cs` — orchestration + shaping.
- `BuckwalterArabicMap.cs`, `SegmentArabicRenderer.cs`, `PosTagSeed.cs` — transliteration,
  Arabic rendering of segments, POS-tag seed data.
- `MorphologySourceValidation.cs` — source-shape checks before assembly.
- `Corrections/` — deterministic correction passes applied during import:
  - `WordLemmaNormalization*` (+ `word-lemma-normalization.json`,
    `word-lemma-mapping-evidence.json`) — fixes wrong/collapsed lemma text.
  - `SegmentStemCorrection*` (+ `segment-stem-corrected-arabic.json`).
- `Enriched/` — the enriched source reader + dimension builder + dry-validator.

## Invariants / caveats (read before changing)

- **Identity key is clean imlaei-simple**, not Uthmani. Uthmani is display-only.
- **`PRO` POS seed correction caveat** — the seed for pronoun/`PRO` tagging is corrected;
  do not "simplify" it back to the raw source value.
- **Lemma text collapse/collisions** are a known hazard — distinct lemmas must not
  collapse onto one text; corrections + collision detection guard this.
- **U+06DF dot-render offender** — certain marks render wrong with the wrong font/renderer;
  segment Arabic rendering must preserve them (see the frontend Mushaf README for the
  font side).
- **Do not silently modify source data.** Corrections are explicit, versioned JSON with
  evidence files; add a correction there, not by mutating the staged package.

## Related (evidence, not duplicated here)

- Write mechanics: `Persistence/DataPipelines/Quran/README.md`.
- CLI verbs `import-morphology [--enriched]` / `validate-enriched-morphology`:
  `tools/QuranDashboard.DataImporter/README.md`.
- Contract truth for morphology import is this README + the code here; the thin index is
  `docs/contracts/import-pipelines.md`. The schema these writes target is declared by the EF
  configurations under `Persistence/Configurations/Quran/`, not by any report.
  (Planning artifacts for the features that built this area were swept per the
  planning-artifact lifecycle rule — recover from git history if needed.)
- The curation files under `Corrections/` carry `sourceAudit` / `sources` fields naming
  `docs/feature-017-lexical-explorers-polish/*` audit reports. Those folders no longer
  exist in the working tree. **The fields are provenance and must not be rewritten** —
  they record which document the curation was derived from at the time it was made.
  Retrieve those reports from git history if a curation entry ever needs re-deriving.
