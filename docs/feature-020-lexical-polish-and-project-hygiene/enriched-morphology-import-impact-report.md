# Feature 020 — Enriched Morphology Import Impact & Mapping Report

**Branch:** `020-lexical-polish-and-project-hygiene`
**Date:** 2026-07-04
**Type:** READ-ONLY impact & mapping analysis. No copy, no import, no migration, no DB change, no importer code, no commit.
**Dashboard:** `~/Desktop/projects/Dashboard/App` · **SourceAudit:** `~/Desktop/projects/QuranMorphologySourceAudit`
**Path-convention note:** the task suggested `docs/features/020-…`; the repo convention is `docs/feature-020-…` (existing but empty). This report follows the repo convention.

## Verdict

**IMPORT_IMPACT_NEEDS_DECISION**

The analysis is complete and an implementation plan is within reach, but **two material decisions** must be signed off first (both detailed below):

1. **Fallback convention for the 3 Corpus-missing words** — the SourceAudit enriched JSON marks them `corpusPresent:false` / `FALLBACK_QUL`, but the Dashboard's **already-imported aligned corpus gives them real, clean Corpus segments** (a *better* result). Importing the enriched fallback verbatim would be a regression.
2. **How far to replace the current QUL-word-link + correction-artifact dimension model** — the enriched (Corpus-truth) approach fixes a live defect but renumbers/rederives root/lemma/stem identity, which is FK'd and surfaced in explorer routes.

Everything else (resource location, mapping, schema deltas, boundary rule, UI surface, gates, docs hygiene) has a clear recommendation.

---

## 0. Motivating evidence — the current pipeline is measurably wrong at the word level

The live DB still carries the QUL-shifted lemma defect that SourceAudit flagged (queried read-only):

| Location | Word | Current DB `lemma_text` | Correct (Corpus) | Current root (correct) |
| --- | --- | --- | --- | --- |
| `41:44:16` | وَشِفَآءٌ | **ءَامَنَ** ✗ | شفاء | ش ف ي ✓ |
| `11:29:17` | مُّلَٰقُوا۟ | **ءَامَنَ** ✗ | لاقى/ملاقو | ل ق ي ✓ |
| `2:102:41` | ٱلْمَرْءِ | **فَرَّقُ** ✗ | مرء | م ر ا ✓ |
| `2:144:20` | شَطْرَهُ | **كَانَ** ✗ | شطر | ش ط ر ✓ |

**Root cause (code-confirmed):** `MorphologyAssembler.Assemble` sets word-level `lemma_id`/`root_id`/`stem_id` from **QUL word-location links** (`if (lemmas.TryGetValue(location, out var lv)) qulLemma = lv;`), patched by a `WordLemmaNormalization` correction artifact. Roots resolve correctly (segment `root_buckwalter` → `quran_roots`), but the lemma link stays QUL-sourced and the correction artifact does not cover these words. This is exactly the "never trust QUL word-level lemma links" failure the enriched JSON eliminates by deriving lemma from Corpus Buckwalter. The enriched import is therefore a **correctness fix**, not just a refactor.

---

## 1. Current pipeline (as-built, for grounding)

- **Source:** `resources/import-sources/quran-morphology/` via a manifest: `corpus/quranic-corpus-morphology-qpc-aligned.json` + `qul/word-root.json` + `qul/word-lemma.json` + `qul/word-stem-corrected-arabic.json`, plus curation artifacts (`word-lemma-normalization.json`, `segment-stem-corrected-arabic.json`).
- **Assembler:** `MorphologyAssembler` renders Corpus segments to Arabic (`SegmentArabicRenderer` + `BuckwalterArabicMap`), resolves segment `root_id`/`lemma_id`/`stem_id` from Corpus Buckwalter, but takes the **word-level dimension identity from QUL links**, and **builds the root/lemma/stem dictionaries + assigns their ids** in-pipeline (sequential `nextDimId`, `first_word_order_in_mushaf`).
- **DB (current counts):** `quran_word_morphology` 77,432 · `quran_word_morphology_segments` **128,219** · `quran_roots` 1,642 · `quran_lemmas` 4,790 · `quran_stems` 12,108 · `quran_pos_tags` 49.
- **The Dashboard corpus is QPC-*aligned*** (not raw Corpus 0.4). The 3 SourceAudit "Corpus-missing" words already have real segments here: `3487` عَلِيمٌ (ADJ), `23788` يَنظُرُ (V)+ونَ (SUFFIX PRON), `34313` وَاقٍ (N) — all `clean` render tier. **This is the source of the 128,219-vs-128,222 divergence.**
- **QUL location index-join is used everywhere**, including the 4 boundary ayahs — the current importer does **not** honor the boundary rule the enriched JSON enforces.

---

## 2. Decision 1 — Recommended resource location

**Recommend a NEW folder: `resources/import-sources/quran-enriched-morphology/`** (do not copy the file in this task).

Why not reuse `quran-morphology/`:
- The enriched artifact is a **single, self-contained per-word JSON** with Corpus + QUL **already merged and rendered** (one record per readable word, segments inline). The existing folder is a **multi-source manifest** (separate corpus + 3 QUL files + curation artifacts) consumed by a different assembler.
- A separate folder keeps the **old importer runnable during transition** (parallel-run parity checks), keeps provenance clean, and avoids a half-migrated manifest.
- The enriched file is ~96 MB; stage it deliberately with its own manifest + SHA-256 digest (matching the existing `MorphologyManifestReader` digest discipline) when the import task begins — not now.

---

## 3. Decision 2 — Import strategy

**Recommend: add a NEW enriched import pathway (new `IMorphologyImportSource` implementation), run it in parallel for parity, deprecate the old pathway after cut-over.** Do not modify the existing assembler in place first.

Rationale / risks:
- The current `MorphologyAssembler` (~400 lines) is tightly wired to the QUL-link + correction-artifact model (`WordLemmaNormalizationApplier`, `SegmentStemCorrectionReader`, `CuratedLemmaDisambiguation`, `SEG-LEMMA-ID-*`/`SEG-STEM-ID-*` invariants). Rewriting it in place risks regressions across the Roots/Lemmas/Stems explorers that depend on **stable dimension ids**.
- A new pathway can be validated against the current DB (diff lemma/root/stem assignments, confirm the §0 defects are fixed and nothing else regresses) **before** switching.
- **Key work either way:** the enriched JSON carries Arabic values + Buckwalter but **not** dimension ids. The new importer must still **build the roots/lemmas/stems dictionaries and assign ids** — now keyed on **Corpus lemma Buckwalter + bridge Arabic** (value-based), never on QUL location links.

---

## 4. JSON → DB mapping

### Word record → `quran_word_morphology` (+ new columns)

| Enriched JSON | DB column | Notes |
| --- | --- | --- |
| `quranWordId` | `quran_word_id` (PK, FK→`quran_words.id`) | verified `ID_MATCH_CONFIRMED`; also the join anchor |
| `location` | `location` | canonical human key (`quran_words` has UNIQUE index) |
| `surah`/`ayah`/`wordNumber` | *(via `quran_words`)* | already on `quran_words`; not duplicated here today |
| `textUthmani`/`textImlaei`/`textUthmaniSimple` | *(on `quran_words`)* | **do not re-import** — owned by foundation; use for validation only |
| `corpusPresent` | **NEW** `corpus_present bool` | not present today |
| `provenance` | **NEW** `provenance text null` | e.g. `QUL_FALLBACK_NOT_CORPUS` |
| *(derived head)* `pos`,`is_verb`,`verb_tense`,`verb_voice`,`case_feature`,features | `head_pos`,`is_verb`,`verb_tense`,`verb_voice`,`case_feature`,`head_features_json` | derive from head STEM as today |
| *(derived)* segment count | `segment_count` | count of segments |
| *(derived, Corpus-truth)* lemma/root/stem identity | `lemma_id`/`root_id`/`stem_id` | **rederived from Corpus BW + value dict, NOT QUL link** |

### `segments[]` → `quran_word_morphology_segments` (+ new columns)

| Enriched JSON | DB column | Notes |
| --- | --- | --- |
| `segmentNumber` | `segment_number` | |
| `location`+seg | `segment_location` | `{location}:{segmentNumber}` |
| `kind` | `kind` | PREFIX/STEM/SUFFIX |
| `pos` | `pos` (FK→`quran_pos_tags.code`) | |
| `features` | `features_json` (jsonb) | |
| `featuresRaw` | `features_raw` | |
| `formArabic` | `form_arabic_normalized` | already `display_ar`-clean in the artifact |
| `formBuckwalter` | `form_buckwalter` | **internal/audit only — never UI** |
| `lemmaBuckwalter` | `lemma_buckwalter` | internal/audit only |
| `rootBuckwalter` | `root_buckwalter` | internal/audit only |
| `stemBuckwalter` | **NEW** `stem_buckwalter text null` | not stored today |
| `lemmaArabic` | *(feeds)* `lemma_id`→`quran_lemmas.lemma_text` | primary display via dimension row |
| `rootArabic` | *(feeds)* `root_id`→`quran_roots.root_text` | |
| `stemArabic` | *(feeds)* `stem_id`→`quran_stems.stem_text` | |
| `lemmaArabicQulCanonical` | **NEW** `lemma_arabic_qul_canonical text null` | confirmation side-field |
| `stemArabicQulCanonical` | **NEW** `stem_arabic_qul_canonical text null` | confirmation side-field |
| `lemmaArabicMappingStatus` | **NEW** `lemma_arabic_mapping_status text` | distinct axis from `arabic_render_tier` |
| `rootArabicMappingStatus` | **NEW** `root_arabic_mapping_status text` | |
| `stemArabicMappingStatus` | **NEW** `stem_arabic_mapping_status text` | |
| `quranWordIdVerifiedAgainstDashboard` | *(import manifest/provenance, NOT a column)* | one-time provenance fact |

`arabic_render_tier`/`arabic_render_source` and the `i3rab_*` columns stay as-is (see §7).

---

## 5. Schema impact — can current schema store it without loss?

**No — a small additive migration is required.** Existing columns already cover `form_buckwalter`, `root_buckwalter`, `lemma_buckwalter`, `form_arabic_normalized`, `features_raw`, `features_json`, and the dimension FKs. **New columns needed** (additive, nullable/defaulted — no data loss, out of scope to implement here):

| Field | Where | New object |
| --- | --- | --- |
| `corpusPresent` | word | `corpus_present bool not null default true` |
| `provenance` | word | `provenance text null` |
| `lemmaArabicMappingStatus` | segment | `lemma_arabic_mapping_status text` |
| `rootArabicMappingStatus` | segment | `root_arabic_mapping_status text` |
| `stemArabicMappingStatus` | segment | `stem_arabic_mapping_status text` |
| `lemmaArabicQulCanonical` | segment | `lemma_arabic_qul_canonical text null` |
| `stemArabicQulCanonical` | segment | `stem_arabic_qul_canonical text null` |
| `stemBuckwalter` (internal) | segment | `stem_buckwalter text null` |
| `quranWordIdVerifiedAgainstDashboard` | **none** | store in import provenance/manifest, not a per-row column |
| fallback handling | word | covered by `corpus_present` + `provenance` (§6) |

No new **table** is required — the two morphology tables + the three dimension tables absorb everything.

---

## 6. Decision — Fallback words (`2:181:14`, `8:6:12`, `13:37:20`)

**Conflict to resolve:** the enriched JSON marks these `corpusPresent:false` / `provenance:"QUL_FALLBACK_NOT_CORPUS"` / all statuses `FALLBACK_QUL`. But the Dashboard's **aligned corpus already stores them as real, clean Corpus segments** (§1) — a strictly better result. Importing the enriched fallback verbatim would **downgrade** these 3 words.

Options:
- **(A) Keep the Dashboard's aligned-corpus segments for these 3** (recommended): import them as `corpus_present=true`, no `FALLBACK_QUL`; the enriched fallback records are *reconciled*, not imported literally. Requires the enriched generator (SourceAudit) to note that Dashboard alignment supersedes its raw-corpus fallback for these 3.
- **(B) Adopt the enriched fallback convention** as-is: `corpus_present=false`, `provenance=QUL_FALLBACK_NOT_CORPUS`, statuses `FALLBACK_QUL`. Simpler/1:1 with the artifact, but a regression in data quality.

**If (B) is chosen**, importer behavior is exactly: one segment per word, BW fields null, all three mapping statuses `FALLBACK_QUL`, `corpus_present=false`, `provenance="QUL_FALLBACK_NOT_CORPUS"`, excluded from Corpus-truth stats, queryable and clearly flagged as the only sanctioned QUL word-level exception. **This is a sign-off decision, not an engineering default.**

---

## 7. Boundary ayahs safety (`2:181`, `2:282`, `8:6`, `13:37`)

**Required change:** the current importer index-joins QUL by location **everywhere**, so it currently violates the boundary rule inside these ayahs. The new importer must:
- Take segmentation / POS / features / `form/lemma/root/stem` Buckwalter **only from Corpus** (the enriched JSON already does this and omits any `qulWordLevelLemmaLink`).
- Perform QUL confirmation **value-based only** (Corpus BW-bridge skeleton matched against the QUL dictionary *inventory*), never by QUL word-location link.
- Emit no QUL-link field; if any contrast field is ever carried, mark it `boundary_unreliable` inside these ayahs.

Net: the boundary shift cannot corrupt any word because no dimension value is ever sourced from a QUL location join. `2:282` stays fully Corpus-present.

---

## 8. UI / API impact

| Area | API / surface | Expected change | Stays unchanged |
| --- | --- | --- | --- |
| **Lemmas** (الصيغ المعجمية) | `api/words/lemmas` (`LemmasController`) | **lemma values & grouping corrected** (شفاء/ملاقو/مرء no longer under ءامن/فرق); dimension **ids may renumber** | endpoint contract shape |
| **Roots** (الجذور) | `api/words/roots` (`RootsController`) | minor — roots already ~correct; ids may renumber; normalize `root_text` spacing (`ش   ف   ي`) | grouping logic |
| **Stems** (الأصول الصرفية) | `api/words/stems` | stem values stable; ids may renumber | |
| **Word Types** | `api/words/word-types` | if type derives from POS → unchanged; if from lemma → inherits the lemma fix | POS-derived typing |
| **Word Analysis** | `api/mushaf/words/{loc}/analysis` (`MushafWordAnalysisController`) | shows corrected lemma; new mapping-status/canonical side-fields available | segment rendering, layout |
| **Simple I3rab** | segment `i3rab_*` columns | source is Corpus POS/features (unchanged); segment set stable if aligned-corpus segmentation kept | i3rab rules/labels |
| **Frontend** | `features/words`, `features/mushaf` | lemma/root/stem displays reflect corrected data; **explorer route ids may shift** (bookmarks) | page structure |
| **Tests / report generators** | morphology invariants, dimension-id checks | update expected counts + drop QUL-link-correction assertions | foundation/id-provenance tests |

**Hard UI rule:** Buckwalter (`*_buckwalter`, `*QulCanonical` is Arabic and OK) must **never** be displayed; it stays stored for audit/disambiguation only. **Dimension-id stability is the top UI risk** — root/lemma/stem ids are FK'd and used in explorer URLs; a rederivation must either preserve or explicitly remap them (a decision for the implementation plan).

---

## 9. Import validation gates (future implementation must enforce)

| # | Gate | Note |
| --- | --- | --- |
| 1 | 77,432 readable word-morphology rows | = readable universe |
| 2 | 0 ayah-marker rows | markers excluded |
| 3 | every `quranWordId` matches an existing `quran_words.id` | verified `ID_MATCH_CONFIRMED` |
| 4 | no duplicate word-morphology rows / no duplicate `(quran_word_id, segment_number)` | |
| 5 | segment count reconciled | **128,219** if aligned-corpus segments kept for the 3 words (Option A); **128,222** only if raw+fallback (Option B) — the count depends on Decision §6 and must be stated explicitly |
| 6 | 3 fallback words present & queryable per the chosen §6 convention | A: `corpus_present=true` real segments · B: `FALLBACK_QUL` |
| 7 | all Corpus-present words have Corpus-derived segments | |
| 8 | all mapping statuses in allowed vocab (`MAPPED_QUL_DICTIONARY`/`CONFIRMED_IN_QUL_DICTIONARY`/`BRIDGE_ONLY_UNCONFIRMED`/`NOT_APPLICABLE`/`FALLBACK_QUL`) | |
| 9 | 0 unresolved roots for Corpus root-bearing segments | matches SourceAudit gate 10 (root 0 bridge-only) |
| 10 | no Buckwalter leakage into any UI-facing Arabic display field | assert `form_arabic_normalized`/dimension text are Arabic-block only |
| 11 | **word-level lemma never sourced from a QUL location link** | derived from Corpus BW + value dict; §0 defects fixed |
| 12 | boundary ayahs use no QUL location index-join | §7 |
| 13 | dimension-id stability preserved or explicitly remapped | protects explorer routes/FKs |

---

## 10. Docs / project hygiene

- **Canonical new report:** this file (`docs/feature-020-lexical-polish-and-project-hygiene/enriched-morphology-import-impact-report.md`) + the future implementation plan alongside it.
- **Reference-only (SourceAudit evidence, external workspace — do not copy into Dashboard):** `corpus-based-enriched-json-alignment-gate.md`, `corpus-enriched-full-generation-readiness-report.md`, `dashboard-qpc-word-id-provenance-check.md`, `corpus-based-enriched-morphology-full-generation-report.md`, and the `samples/*summary*.json`. Cite them; the 96 MB artifact stays in SourceAudit.
- **Obsolete assumptions that must NOT return** (the "wrong direction" of the removed Feature 020 docs and much of Feature 017):
  - "QUL word-level lemma link is the lemma truth" — replaced by Corpus-derived lemma.
  - Patching QUL lemma defects via per-word correction artifacts (`word-lemma-normalization*.json`, `word-level-lemma-alignment-corrections*.json`, the `WordLemmaNormalization*` reader/applier) — superseded; these fixes are incomplete (see §0) and become dead weight once lemma is Corpus-sourced.
  - Treating `arabic_render_tier` as a QUL-confirmation signal — it is a render-quality axis; the new `*MappingStatus` columns are the QUL-confirmation axis.
  - Any importer that index-joins QUL by location for dimension identity.

---

*Report only. Nothing copied into Dashboard, nothing imported, no DB change, no migration, no importer code, no commit. Verdict: `IMPORT_IMPACT_NEEDS_DECISION` — resolve §6 (fallback convention) and §3/§8 (dimension-id rederivation & stability) before locking an implementation plan.*
