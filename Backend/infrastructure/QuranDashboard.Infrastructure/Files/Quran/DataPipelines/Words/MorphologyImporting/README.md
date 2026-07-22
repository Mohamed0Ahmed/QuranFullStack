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
- **Enriched dimension builder minting (`Enriched/EnrichedDimensionBuilder.cs`)** — value-based
  identity with hard DB uniqueness. `lemma_text` homographs collapse to ONE row (honours
  `UNIQUE(lemma_text)`). Minting is two-phase and order-sensitive: **phase 1** mints only head
  dimensions with a unique `first_word_order`; **phase 2** resolves references and mints nothing —
  a value that was never a head stays `null` rather than fabricating a `first_word_order` (fabricating
  one reintroduces the phase-2 duplicate-key defect). Stem identity is the normalized `stem_text`
  only: the small yeh is stripped for the stem key but kept on the segment display form.
- **U+06DF dot-render offender** — certain marks render wrong with the wrong font/renderer;
  segment Arabic rendering must preserve them (see the frontend Mushaf README for the
  font side).
- **Do not silently modify source data.** Corrections are explicit, versioned JSON with
  evidence files; add a correction there, not by mutating the staged package.

## Related (evidence, not duplicated here)

- Write mechanics: `Persistence/DataPipelines/Quran/README.md`.
- CLI verbs `import-morphology [--enriched]` / `validate-enriched-morphology`:
  `tools/QuranDashboard.DataImporter/README.md`.
- Spec: `specs/004-word-morphology-foundation/`. DB baseline:
  `Backend/report/database/current-database-tables-and-relationships-report.md`.
  (Prior feature-020/019 evidence reports were purged — recover from git history if needed.)
