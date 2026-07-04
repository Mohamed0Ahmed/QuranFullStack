# Feature 020 — No-Schema Enriched Morphology Import Implementation Plan

**Branch:** `020-lexical-polish-and-project-hygiene`
**Date:** 2026-07-04
**Type:** PLAN ONLY. No code, no JSON copy, no migration (create or run), no importer run, no DB reset/drop/update, no DB data change, no commit. Documentation only.
**Dashboard:** `~/Desktop/projects/Dashboard/App` · **SourceAudit:** `~/Desktop/projects/QuranMorphologySourceAudit`
**Predecessor:** `enriched-morphology-import-impact-report.md` (verdict `IMPORT_IMPACT_NEEDS_DECISION`). Its two open decisions are now **signed off** (see below); this plan supersedes its schema-delta section.

## Verdict

**NO_SCHEMA_IMPORT_PLAN_READY**

Every required section has a concrete, code-grounded recommendation that fits the **current schema with zero new columns and no migration**. The one residual design choice (stem identity) has a clear default and is not blocking. Remaining items are risks to watch during implementation, not open decisions.

## Signed-off decisions this plan is built on

- Use the **Dashboard-ready artifact** (`corpus-based-enriched-morphology.dashboard-ready.json` — 77,432 records, **128,219** segments, 0 fallback, all `corpusPresent:true`, boundary ayahs rebuilt from aligned Corpus, `quranWordIdVerifiedAgainstDashboard:true`). Not the earlier `full.json` fallback artifact.
- DB will be **reset/dropped/rebuilt later**, in a separate execution step.
- **Do not preserve** old lemma/root/stem dimension IDs.
- **No new DB columns. No schema migration. Current schema is sufficient.** Goal: replace wrong data with correct data in the existing tables.
- Buckwalter stays **internal/audit-only** in the columns that already store it (`form_buckwalter`, `root_buckwalter`, `lemma_buckwalter`); **never** displayed in UI.
- Extra JSON fields (`corpusPresent`, `provenance`, `*MappingStatus`, `*QulCanonical`, `stemBuckwalter`, `quranWordIdVerifiedAgainstDashboard`) are **source/report-only** and must **not** require new columns.

### Why "no new columns" is mechanically guaranteed

The import writer (`EfBulkMorphologyWriter`) consumes a fixed DTO, `MorphologySourceData`, whose records —
`AlignedWordDto`, `AlignedSegmentDto`, `ResolvedRootDto`, `ResolvedLemmaDto`, `ResolvedStemDto` — have **no members** for `corpusPresent`, `provenance`, `*MappingStatus`, `*QulCanonical`, or `stemBuckwalter`. Reusing this DTO + writer unchanged means the audit-only fields have nowhere to land in the DB even by accident. The whole plan reduces to: **feed the existing writer a correct `MorphologySourceData` built value-based from the enriched JSON, instead of one built from QUL location links.**

---

## 1. Resource staging plan

**Target folder (new):** `resources/import-sources/quran-enriched-morphology/`
(Do **not** create or copy anything in this task — this is where it will live when the import task begins. `resources/` is local/gitignored.)

Rationale: the enriched artifact is a **single self-contained per-word JSON** (Corpus + QUL already merged and rendered, one record per readable word, segments inline). The existing `resources/import-sources/quran-morphology/` is a **multi-source manifest** (aligned corpus + 3 QUL files + 2 curation artifacts) consumed by the old assembler. A separate folder keeps the **old pathway runnable** for parallel comparison and keeps provenance clean.

**Staged contents:**

| File | Purpose |
| --- | --- |
| `corpus-based-enriched-morphology.dashboard-ready.json` | the artifact (~96 MB) |
| `manifest.json` | file set + digests + expected counts (below) |

**Required manifest fields** (mirror the digest discipline already in `MorphologyManifestReader` / `MorphologyManifestFile`):

- `relativePath` — `corpus-based-enriched-morphology.dashboard-ready.json`
- `sha256` — SHA-256 of the artifact, verified at load (`ValidateChecksum` pattern)
- `sizeBytes` — exact byte size, verified (`ValidateFileSize` pattern) — expect `96,051,070`
- `recordCount` — **77,432**, verified (`ValidateRecordCount` pattern)
- `segmentCount` — **128,219** (new expected-count check for the enriched reader)
- `sourceArtifact` / `provenance` — cite the SourceAudit generator + report: `scripts/generate_dashboard_ready.py`, `reports/corpus-based-enriched-morphology-dashboard-ready-generation-report.md` (verdict `DASHBOARD_READY_GENERATION_PASS`)
- `quranWordIdVerifiedAgainstDashboard: true` — recorded here as an import-provenance fact (it is **not** a per-row DB column)

**SHA-256 validation:** compute on load and compare to `manifest.sha256`; refuse the import on mismatch, exactly as the current manifest reader refuses (`InvalidDataException` → `MorphologyInvariants.SourceMismatch`). `SourceUnchangedAsync` re-verifies the digest to guard the two-phase "load → write" window.

**Note:** the actual copy of the 96 MB artifact into Dashboard resources is **explicitly out of scope** for both this planning task and is a deliberate, separately-approved step at implementation time.

---

## 2. Import pathway plan

**Recommendation: add a NEW enriched import pathway (a new `IMorphologyImportSource` implementation) that returns the existing `MorphologySourceData` DTO; reuse the existing writer/handler/report unchanged. Do NOT rewrite the old QUL-link assembler in place.**

### Current pipeline (as-built, grounding)

- **Handler:** `ImportMorphologyHandler` (application) → `importSource.LoadAsync` → `importWriter.ImportAsync` → `reportWriter.WriteAsync`. Refuses if targets non-empty unless `--force`.
- **Source:** `MorphologyImportSource : IMorphologyImportSource` reads the multi-file manifest (`MorphologyManifestReader`, `JsonAlignedCorpusReader`, `JsonQulRootReader/LemmaReader/StemReader`) and calls `MorphologyAssembler.Assemble(...)`.
- **Assembler:** `MorphologyAssembler` (~701 lines) renders BW→Arabic (`SegmentArabicRenderer` + `BuckwalterArabicMap`), resolves segment/word dimensions, **takes word-level lemma/root/stem identity partly from QUL location links** (`ResolveLemmaId` word-head path), applies corrections (`WordLemmaNormalizationApplier`, `SegmentStemCorrectionReader`, `CuratedLemmaDisambiguation`), and **builds the root/lemma/stem dictionaries + assigns sequential ids** in-pipeline.
- **Writer:** `EfBulkMorphologyWriter : IMorphologyImportWriter` bulk-writes `MorphologySourceData`.
- **Report:** `MarkdownJsonMorphologyReportWriter : IMorphologyReportWriter`.
- **DI:** `MorphologyImportDependencyInjection.AddMorphologyImport()`.
- **CLI verb:** `import-morphology` in `tools/QuranDashboard.DataImporter` (runs after `import-foundation`; `generate-i3rab` runs after it).

### New pathway — classes to ADD (implementation task, not now)

| New class | Role |
| --- | --- |
| `EnrichedMorphologyImportSource : IMorphologyImportSource` | reads the single dashboard-ready JSON + manifest; produces `MorphologySourceData` |
| `EnrichedMorphologyReader` | streaming JSON reader for the 96 MB per-word file (one record → one `AlignedWordDto` + its `AlignedSegmentDto[]`) |
| `EnrichedDimensionBuilder` | builds `ResolvedRoots/Lemmas/Stems` **value-based** from Corpus BW + Arabic and assigns fresh ids; resolves each word/segment `root_id`/`lemma_id`/`stem_id` — **never** from a QUL location link |
| `EnrichedMorphologyManifestReader` (or generalize `MorphologyManifestReader`) | single-file manifest + SHA-256 + count checks (§1) |

### Classes REUSED unchanged (the seam that keeps schema fixed)

`MorphologySourceData` (+ `AlignedWordDto`/`AlignedSegmentDto`/`Resolved*Dto`), `IMorphologyImportWriter` / **`EfBulkMorphologyWriter`**, `IMorphologyReportWriter` / `MarkdownJsonMorphologyReportWriter`, `ImportMorphologyHandler`, `ImportMorphologyCommand`, `MorphologyInvariants`, `PosTagSeed`, and the feature-mapping helpers (`MapVerbTense` / `MapVerbVoice` / `MapCaseFeature` / `BuildFeaturesJson` — extract to a shared helper or copy the pure logic). Because the enriched forms are already `display_ar`-clean, `SegmentArabicRenderer` may be reused for parity checks or skipped for rendering.

### Classes REPLACED / BYPASSED (kept temporarily, obsolete after cut-over)

`MorphologyImportSource` (old source), `JsonAlignedCorpusReader`, `JsonQulRootReader/LemmaReader/StemReader`, and the whole `Corrections/` package (see §6). The QUL-link + correction logic inside `MorphologyAssembler`.

### Transition (old source stays temporarily, but is not truth after cut-over)

- Bind the new source behind a **selector** so both pathways can run: e.g. a command flag (`import-morphology --enriched`) or a distinct verb, resolving `IMorphologyImportSource` to `EnrichedMorphologyImportSource` vs the old `MorphologyImportSource`. Default binding flips to enriched at cut-over.
- Old pathway remains **runnable for parallel parity** (diff new-vs-old dimension assignments; confirm the §0 defects in the impact report are fixed and nothing else regresses) — but once the enriched import runs against the reset DB, the enriched data is the **only truth**; the old source and its correction artifacts are then removed (§6). No half-migrated manifest, no dual truth.

---

## 3. Existing-schema mapping (no new columns)

Current columns (verified from the domain entities):

- `WordMorphology`: `QuranWordId(PK)`, `Location`, `HeadPos`, `SegmentCount`, `RootId?`, `LemmaId?`, `StemId?`, `IsVerb`, `VerbTense?`, `VerbVoice?`, `CaseFeature?`, `HeadFeaturesJson?`.
- `WordMorphologySegment`: `Id`, `QuranWordId`, `SegmentLocation`, `SegmentNumber`, `Kind`, `Pos`, `FormBuckwalter`, `FormArabicNormalized?`, `ArabicRenderTier?`, `ArabicRenderSource`, `RootBuckwalter?`, `LemmaBuckwalter?`, `RootId?`, `LemmaId?`, `StemId?`, `FeaturesRaw`, `FeaturesJson?`, `I3rab*` (4).
- `QuranRoot`: `Id`, `RootText`, `RootBuckwalter?`, `WordsCount`, `DistinctLemmasCount`, `FirstWordOrderInMushaf`.
- `QuranLemma`: `Id`, `LemmaText`, `LemmaBuckwalter?`, `RootId?`, `WordsCount`, `FirstWordOrderInMushaf`.
- `QuranStem`: `Id`, `StemText`, `WordsCount`, `FirstWordOrderInMushaf`.  ← **no buckwalter column**
- `PosTag`: `Code(PK)`, `ArabicLabel`, `EnglishLabel`, `Category`, `SortOrder`, `Description?`.

### `quran_word_morphology` ← word record

| JSON | Column | Notes |
| --- | --- | --- |
| `quranWordId` | `quran_word_id` (PK, FK→`quran_words.id`) | join anchor; ID_MATCH_CONFIRMED |
| `location` | `location` | |
| head-STEM `pos` | `head_pos` | from the primary STEM segment |
| segment count | `segment_count` | |
| head dimensions | `root_id` / `lemma_id` / `stem_id` | from the head STEM's resolved ids (§4) — value-based, not QUL link |
| head `features` | `is_verb`, `verb_tense`, `verb_voice`, `case_feature`, `head_features_json` | reuse `MapVerbTense/Voice/CaseFeature` + `BuildFeaturesJson` on the head segment |

### `quran_word_morphology_segments` ← `segments[]`

| JSON | Column | Notes |
| --- | --- | --- |
| `segmentNumber` | `segment_number` | |
| `location`+`segmentNumber` | `segment_location` | importer composes `{location}:{segmentNumber}` (as today) |
| `kind` | `kind` | |
| `pos` | `pos` | FK→`quran_pos_tags.code` |
| `formBuckwalter` | `form_buckwalter` | internal/audit only |
| `formArabic` | `form_arabic_normalized` | already `display_ar`-clean |
| — | `arabic_render_tier` / `arabic_render_source` | **importer sets constants** (e.g. tier `clean`, source `corpus_enriched_bridge`). These are the **render-quality** axis — do NOT store `*MappingStatus` here |
| `rootBuckwalter` | `root_buckwalter` | internal/audit only |
| `lemmaBuckwalter` | `lemma_buckwalter` | internal/audit only |
| resolved ids | `root_id` / `lemma_id` / `stem_id` | value-based (§4) |
| `featuresRaw` | `features_raw` | |
| `featuresJson`/`features` | `features_json` | jsonb |
| — | `i3rab_*` (4 cols) | **not set by this importer**; filled later by `generate-i3rab` (§9) |

### `quran_roots` / `quran_lemmas` / `quran_stems` / `quran_pos_tags`

- `quran_roots` ← `rootArabic`→`root_text`, `rootBuckwalter`→`root_buckwalter`; `words_count`/`distinct_lemmas_count`/`first_word_order_in_mushaf` computed during dimension build.
- `quran_lemmas` ← primary `lemmaArabic`→`lemma_text`, `lemmaBuckwalter`→`lemma_buckwalter`, `root_id` link; counts/order computed.
- `quran_stems` ← `stemArabic`→`stem_text`; counts/order computed. (`stemBuckwalter` has **no column** — audit-only, dropped.)
- `quran_pos_tags` ← **`PosTagSeed.GetAll()` unchanged** (49 tags). Enriched `pos` values must all resolve against this seed (validated — see gate on unknown POS).

### Ignored / report-only (NOT stored — no column, by design)

`corpusPresent`, `provenance`, `lemmaArabicMappingStatus`, `rootArabicMappingStatus`, `stemArabicMappingStatus`, `lemmaArabicQulCanonical`, `stemArabicQulCanonical`, `stemBuckwalter`, `quranWordIdVerifiedAgainstDashboard`, `quranWordIdSource`, `boundaryAyah`, `boundaryHandling`, `textUthmani/textImlaei/textUthmaniSimple` (owned by foundation — use for validation only, never re-import). These live in the SourceAudit artifact + this report; the runtime DB carries none of them.

---

## 4. Dimension identity plan

**Rebuild root/lemma/stem dictionaries from scratch from the enriched source during reset/import. Assign fresh ids. Do not preserve old ids.**

- **Root identity** = Corpus `rootBuckwalter` + `rootArabic`. Both columns exist (`root_text`, `root_buckwalter`). Key on normalized `rootBuckwalter` (unambiguous); store the QUL-dictionary `rootArabic` as `root_text` (normalize display whitespace at read time; store verbatim — see risks).
- **Lemma identity** = Corpus `lemmaBuckwalter` + primary `lemmaArabic`. Both columns exist (`lemma_text`, `lemma_buckwalter`). Key on `lemmaBuckwalter`; this is exactly what fixes the §0 QUL-shift defect. Link `root_id` from the segment's root.
- **Stem identity** remains the existing persisted schema rule: normalized `stem_text`. `stemBuckwalter` is audit-only and may be used only in validation/report diagnostics, not to create separate persisted stem dimension rows. No `stem_buckwalter` column will be added; `quran_stems` has no Buckwalter column and `ResolvedStemDto` has no Buckwalter member. Therefore persisted stem dimension identity is `stem_text` under the current schema, and `stemBuckwalter` must not create DB rows that cannot later be distinguished in the DB.
- **Never** use QUL word-level location links for lemma/root/stem identity. All identity comes from Corpus Buckwalter + bridge Arabic values already in the artifact.
- **Do not preserve old dimension ids.** Assign ids by the existing ordering convention (`first_word_order_in_mushaf`). Produce an **import validation/diff report** (old→new id map, per-word lemma/root/stem changes, the §7 corrected-lemma checks) so the renumber is auditable. Because the DB is fully reset, FKs are rebuilt consistently and no in-place remap is needed.

---

## 5. Boundary and special words plan

- The artifact already has **0 fallback** and **all records `corpusPresent:true`**; the importer must **never create fallback rows** (no `FALLBACK_QUL`, no `provenance`, no pseudo-segments).
- The 4 boundary ayahs — **`2:181`, `2:282`, `8:6`, `13:37`** — import **exactly as represented** in the dashboard-ready artifact (they are pre-rebuilt from the aligned Corpus). The 3 formerly-missing words carry real segments: `2:181:14` عَلِيمٌ (1 seg), `8:6:12` يَنظُرُ+ونَ (**2 segs**), `13:37:20` وَاقٍ (1 seg).
- **No QUL location index-join anywhere** — including these ayahs. The enriched source emits no `qulWordLevelLemma*` field, so the historical QUL boundary shift cannot corrupt any word. Segmentation/POS/features/`form|lemma|root|stem` Buckwalter come only from the artifact (Corpus-derived); QUL served only as value-based dictionary confirmation upstream in SourceAudit and is not re-consulted by location at import.

---

## 6. Old correction artifacts to retire (obsolete/bypassed after cut-over)

Once the enriched pathway is the truth, these become dead weight and must be **bypassed, then removed** (their DI registrations in `AddMorphologyImport` drop out):

- **Implementation Phase 1:** bypass the old QUL/correction path, but keep old files/classes available for comparison and fallback during implementation validation. The old pathway can remain runnable temporarily for parity/diff while the enriched pathway proves itself.
- **Implementation Phase 2:** remove old correction artifacts/classes only after the enriched import path, reset/rebuild, validation gates, and affected API smoke checks pass. Do not delete old artifacts before the new importer proves itself; after cut-over, enriched data is the truth.

| Artifact | Location | Why obsolete |
| --- | --- | --- |
| `WordLemmaNormalizationReader` / `Applier` / `Validator` / `Models` / `ProblemClasses` | `MorphologyImporting/Corrections/` | patched QUL word-level lemma defects per word; superseded by Corpus-derived lemma (and incomplete — see impact report §0) |
| `word-lemma-normalization.json`, `word-lemma-mapping-evidence.json` | `MorphologyImporting/Corrections/` | inputs to the above |
| `IWordLemmaNormalizationReader`, `ISegmentStemCorrectionReader` DI bindings | `MorphologyImportDependencyInjection` | no longer resolved by the enriched source |
| `SegmentStemCorrectionReader` / `Models` + `segment-stem-corrected-arabic.json` | `MorphologyImporting/Corrections/` | stem Arabic now comes from the artifact's `display_ar` bridge — **confirm no residual stem is fixed only by this artifact before deleting** |
| `CuratedLemmaDisambiguation` + QUL word-link path in `MorphologyAssembler` | `MorphologyAssembler` | QUL location-link dimension identity is exactly what is removed |
| `JsonQulRootReader/LemmaReader/StemReader`, `JsonAlignedCorpusReader`, old `MorphologyImportSource` | `MorphologyImporting/` | replaced by the enriched reader/source |
| `qul/*.json` word-link files in `resources/import-sources/quran-morphology/` | resources (gitignored) | no longer read for dimension identity |

**Must-not-return assumptions:** QUL word-level lemma link as lemma truth; per-word QUL lemma correction artifacts; `arabic_render_tier` treated as a QUL-confirmation signal (it is render-quality; the QUL-confirmation `*MappingStatus` axis is audit-only and unstored); any importer that index-joins QUL by location for dimension identity.

---

## 7. Validation gates for future implementation (hard, post reset/import)

| # | Gate |
| --- | --- |
| 1 | exactly **77,432** `quran_word_morphology` rows |
| 2 | exactly **128,219** `quran_word_morphology_segments` rows |
| 3 | **0** ayah-marker rows |
| 4 | every `quran_word_id` matches an existing `quran_words.id` |
| 5 | no duplicate word-morphology rows (unique `quran_word_id`) |
| 6 | no duplicate `(quran_word_id, segment_number)` |
| 7 | **no fallback/provenance** in DB (no pseudo-segments; audit fields never stored) |
| 8 | **no QUL word-level lemma truth** — word-level lemma never sourced from a QUL location link |
| 9 | **0 unresolved roots** for Corpus root-bearing segments |
| 10 | boundary ayahs (`2:181`, `2:282`, `8:6`, `13:37`) imported exactly from the dashboard-ready artifact; `8:6:12` = 2 segments |
| 11a | `41:44:16` lemma **not** `ءَامَنَ`; expected Corpus-derived `شفاء` (root `ش ف ي`) |
| 11b | `11:29:17` lemma **not** `ءَامَنَ`; expected Corpus-derived `ملاقوا` / `لاقى` per the dimension rule (root `ل ق ي`) |
| 11c | `2:102:41` lemma **not** `فَرَّقُ`; expected `مرء` (root `م ر ا`) |
| 11d | `2:144:20` lemma **not** `كَانَ`; expected `شطر` (root `ش ط ر`) |
| 12 | **Buckwalter never appears in UI-facing responses** — `form/root/lemma buckwalter` stay internal; assert Arabic-only in API output |
| 13 | every `pos` resolves against `PosTagSeed` (no unknown POS codes) |

Gates 1–2 are firm single values (the artifact is already reconciled to 128,219 — no Option-A/B ambiguity remains).

---

## 8. Testing plan (before reset/import)

- **Importer unit tests:** `EnrichedMorphologyReader` parses a fixture slice (incl. `8:6:12` 2-segment word, a PREFIX/STEM/SUFFIX word, a boundary word) into correct `AlignedWordDto`/`AlignedSegmentDto`; audit-only fields are dropped; head feature mapping (`is_verb`/tense/voice/case) correct.
- **Dimension-builder tests:** value-based root/lemma/stem identity from Corpus BW; the four §7 corrected lemmas resolve to the expected values; roots 0-unresolved for root-bearing segments; no QUL location link consulted (assert the QUL readers are not invoked).
- **Integration test (real infrastructure):** import a representative fixture through `EnrichedMorphologyImportSource` → `EfBulkMorphologyWriter` against a real Postgres test DB; assert row counts, uniqueness, FK integrity, no audit columns exist/needed.
- **Real import validation test/report:** full-artifact dry run producing the import diff/validation report (old→new dimension ids, §7 gate results). Reuse `MarkdownJsonMorphologyReportWriter`.
- **API smoke checks** (after import): `api/words/lemmas`, `api/words/roots`, `api/words/stems`, `api/words/word-types`, `api/mushaf/words/{loc}/analysis` — corrected lemmas surface, no Buckwalter leaks, shapes unchanged.
- Full frontend suite is **not required** for this planning task (and per the frontend test-worker cap, keep `VITEST_MAX_FORKS` if run later).
- Self-checks per `CLAUDE.md`: clean-code guard + test-guard (test behavior not implementation; real DTOs/entities constructed; persistence tests on real infra; Quranic test data source-safe).

---

## 9. Rollout / reset plan (do NOT execute now)

Because the local DB is reset/rebuilt, no old dimension-id preservation is required. Order:

1. **Stage source** — copy artifact + `manifest.json` (sha256/size/counts) into `resources/import-sources/quran-enriched-morphology/`.
2. **Implement importer path** — new `EnrichedMorphologyImportSource` + reader + dimension builder + manifest reader; DI selector; reuse writer/handler/report.
3. **Run tests** — unit + integration + dry-run validation report (§8); parallel parity diff vs old pathway.
4. **Reset DB** — `./scripts/reset-db --yes` (drops/recreates; applies EF migrations — **no new migration added**). Ref: `Backend/report/database-inventory/database-reset-and-seeding-order.md`.
5. **Import foundation + enriched morphology** — `import-foundation` → `rebuild-words --force` → `import-morphology` (enriched binding) → **`generate-i3rab`** (fills the `i3rab_*` segment columns the morphology importer leaves null).
6. **Validate reports** — enforce all §7 gates; review the dimension diff report.
7. **Smoke affected APIs/UI** — Lemmas / Roots / Stems / Word Types / Mushaf word analysis.

Steps 4–7 are execution-only and out of scope here.

---

## Remaining risks (watch during implementation — not blockers)

1. **Stem homographs merge** — no `stem_buckwalter` column means vocalization-distinct stems sharing `stem_text` collapse to one row (existing behavior). Acceptable under no-schema; `stemBuckwalter` stays source/report/audit-only and must not create persisted rows that cannot be distinguished later in `quran_stems`.
2. **Bridge lemma vocalization artifacts** — e.g. `8:6:12` lemma renders `نَّظَرَ` (leading shadda from the bridge). Faithful to Buckwalter; group/dedupe on `lemma_buckwalter`, not display text.
3. **`root_text` whitespace** — QUL trilateral spacing is irregular (`و   ق   ي`); normalize at display, store verbatim.
4. **`arabic_render_tier` semantics** — set a render-quality constant; never overload it with `*MappingStatus` (that axis is unstored).
5. **Dimension-id renumber** — explorer routes/bookmarks use ids; full reset makes this safe locally, but ship the old→new diff report for auditability.
6. **96 MB single file** — stream-parse in the reader; do not `JsonDocument.Parse` the whole file into memory in the hot path.
7. **Artifact dependency** — the boundary ayahs depend on the Dashboard `quranic-corpus-morphology-qpc-aligned.json` used to build the artifact; if that source is regenerated, re-run the SourceAudit reconciliation before re-importing.

---

*Plan only. No code, no JSON copy, no migration created/run, no importer run, no DB reset/drop/update, no DB data change, no commit. Verdict: **NO_SCHEMA_IMPORT_PLAN_READY**.*
