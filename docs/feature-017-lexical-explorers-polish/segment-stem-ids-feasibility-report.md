# Segment-Level `stem_id` — Feasibility & Implementation-Readiness Report

**Feature:** 017 — Lexical Explorers Polish
**Proposed change:** Add `stem_id` to `quran_word_morphology_segments` so every STEM segment links to its own `quran_stems` row (surface secondary stems of multi-STEM words in Stems Explorer).
**Task type:** REPORT ONLY — no code, migration, importer, test, or commit changes. Read-only `SELECT` queries were run against the local dev DB.
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-29
**Predecessors:** `stems-explorer-current-state-polish-report.md`, `segment-dimension-ids-feasibility-report.md`, `segment-dimension-ids-db-verification-report.md`

> Evidence tags: `[FACT-DB]` verified by live read-only query this session · `[FACT-CODE]` verified from repository source · `[REC]` recommendation/judgement.

---

## 1. Verdict

### **READY_WITH_CURATION**

- **Single-STEM words (76,949 / 77,432 = 99.38%)** `[FACT-DB]`: trivially safe — `segment.stem_id = head stem_id`. The STEM segment *is* the head; the QUL whole-word stem is its stem. Deterministic, source-faithful, no matching.
- **Multi-STEM secondary segments (483 segments across 483 words = 0.62%)** `[FACT-DB]`: **cannot be mapped from existing import sources.** There is no per-segment stem source, `quran_stems` has no buckwalter, and the only mechanical option (Arabic text-match `segment.form ↔ quran_stems.stem_text`) is **deterministic but not safe** — it depends on contextual idghām rendering (1 of 483 already fails), links to contextual *artifact* stem rows, and asserts a stem identity that **no source provides**. Per the strict rule, this must be a **curated artifact**, not auto text-matching.
- **Curatable?** Yes — the entire multi-STEM surface is tiny and bounded: **6 distinct lemmas, 14 distinct secondary forms** `[FACT-DB]`, essentially the function words ما / من / لا / لو / لن. A reviewed mapping file (sibling of the existing `word-lemma-corrected-arabic.json` + `CuratedLemmaDisambiguation` pattern) is entirely feasible.
- **Important caveat on value** `[FACT-DB]`: **0 orphan stems** and only **6 stems are referenced solely by multi-STEM words**. Every stem the secondary segments would link to is **already visible** from head/standalone occurrences. So the premise "secondary stems are invisible today" is largely **false** — no new stem page appears; the change only **re-attributes 483 function-word occurrences** and lets a compound show under two stem pages. The product benefit is modest; this should be a conscious decision (see §11).

**Operationally:** this is **not** a small in-017 migration. It needs a new curated source artifact + linguistic review + EF/migration + importer rework + reseed + Stems Explorer rework. Treat as a **separate prerequisite feature (018-style)** — *if* product still wants it after weighing §11.

---

## 2. Executive Summary

The lemma/root segment-dimension work succeeded because the **Corpus tags every segment with a lemma and a root** (`segment.lemma_buckwalter`, `segment.root_buckwalter`), and `quran_lemmas`/`quran_roots` carry a matching buckwalter — a real bridge, validated at import. `[FACT-CODE]`

**Stems have none of that.** `[FACT-CODE]`
- The Corpus segment record (`AlignedSegmentDto`) carries `RootBuckwalter` and `LemmaBuckwalter` but **no stem field at all**.
- `quran_stems` is built **only** from the QUL **whole-word** stem (`qul/word-stem-corrected-arabic.json`, one stem per readable word) and stores **no buckwalter** — identity is `stem_text` only.
- The importer already enforces an invariant **`SEG-STEM-ID-ABSENT`** that asserts segments carry no stem id. `[FACT-CODE]`

So a STEM segment has no source-provided stem identity except in the single-STEM case, where it equals the word's head stem. For the **483 multi-STEM words** the secondary STEM segment has no source stem; the only bridge is text identity against a `quran_stems.stem_text` that is itself a mixture of clean and contextual-artifact strings. That is curation, not import.

Because the multi-STEM set is small, bounded, and almost entirely function words, the change is *feasible with curation* — but the live data also shows it would surface **no genuinely new stem**, only re-attribute function-word occurrences. The verdict is therefore **READY_WITH_CURATION**, with an explicit "is it worth it?" decision attached.

---

## 3. Current Schema / Data Model

### 3.1 Segment columns today `[FACT-CODE]` (`WordMorphologySegment`, `WordMorphologySegmentConfiguration`)
`id, quran_word_id, segment_location, segment_number, kind, pos, form_buckwalter, form_arabic_normalized, arabic_render_tier, arabic_render_source, root_buckwalter, lemma_buckwalter, root_id, lemma_id, features_raw, features_json, i3rab_*`.
- **`root_id`, `lemma_id`** added by migration `20260627144247_AddSegmentDimensionIds`; **no `stem_id`**, and no stem text/buckwalter column.
- FK pattern: `HasOne(Root/Lemma).WithMany().HasForeignKey().OnDelete(Restrict)`; indexes `IX_quran_word_morphology_segments_root_id` / `_lemma_id`; plus a partial `IX_..._segments_stem` on `quran_word_id WHERE kind='STEM'`.

### 3.2 How `lemma_id` / `root_id` are populated `[FACT-CODE]` (`MorphologyAssembler`)
- Dimensions are keyed by the **QUL whole-word** value: `lemmaIndex[qulLemma]`, `rootIndex[qulRoot]`, `stemIndex[qulStem]`. Each dimension records a representative **Corpus** buckwalter via `DimensionEntry.AddBuckwalter(corpusLemma/corpusRoot)`.
- `ResolveSegmentDimensions` → `ResolveLemmaId` / `ResolveRootId`:
  - **Single-STEM word:** `segment.lemma_id = word head lemma_id` (reuse; no matching).
  - **Multi-STEM word:** resolve each STEM segment by `segment.lemma_buckwalter` → `quran_lemmas.lemma_buckwalter`; tie-break by Arabic form match; then a hand-curated table `CuratedLemmaDisambiguation` (e.g. `('ACC','>an~') → 'أَنّ'`); else **fail closed** (`SEG-LEMMA-ID-NO-FANOUT`, no silent guessing).
  - `ResolveRootId`: `segment.root_buckwalter` → `quran_roots.root_buckwalter` (unique key, 100% clean).

### 3.3 Why `stem_id` was **not** added previously `[FACT-CODE]`
The prior feasibility/verification reports rejected it for three reasons, all still true:
1. **No segment stem source** — `AlignedSegmentDto` has root & lemma buckwalter, never a stem.
2. **`quran_stems` has no buckwalter** — only `id, stem_text, words_count, first_word_order_in_mushaf`; no bridge key.
3. **It was deemed unnecessary** — `head_pos = stemSegment.Pos`, so the lemma-style head/segment POS divergence does not affect stems.
The importer encodes this as the active invariant **`SEG-STEM-ID-ABSENT`**.

### 3.4 What `quran_stems` stores `[FACT-CODE]` / `[FACT-DB]`
- Columns: `id, stem_text, words_count, first_word_order_in_mushaf`. Unique index on `stem_text`. **No buckwalter, no lemma/root linkage.**
- Built per-word from `qulStem = stems[location]` (`qul/word-stem-corrected-arabic.json`) — **one stem per readable word**. `[FACT-CODE]`
- 12,108 rows. `[FACT-DB]`

**Does `quran_stems` contain enough to match a segment STEM to a stem row?** Only via `stem_text` (Arabic). There is no buckwalter and no per-segment stem source — so for the secondary segment of a multi-STEM word the only candidate key is contextual Arabic text, which is unsafe (§4, §6).

---

## 4. Source Availability for Segment-Level Stem Mapping

| Question | Answer | Evidence |
|---|---|---|
| Does each STEM segment have a reliable **source** stem identity? | **Only single-STEM words.** For them the segment = head, so the QUL whole-word stem is its stem. | `[FACT-CODE]` |
| Is the stem source word-level only, or per-segment? | **Word-level only.** `stems[location]` is one stem per readable word; `stemIndex` keyed by `qulStem`. | `[FACT-CODE]` |
| For multi-STEM words, can we assign a distinct `stem_id` per STEM segment **deterministically from source**? | **No.** The Corpus segment carries no stem; QUL gives one stem for the whole compound. The secondary segment has no source stem. | `[FACT-CODE]` |
| Cases where two STEM segments cannot be distinguished by source? | **All 483 multi-STEM words** for the *secondary* segment — there is no per-segment stem field to distinguish them. | `[FACT-CODE]` |
| Is text matching required? | **Yes**, it is the only mechanical bridge for secondary segments. | `[FACT-CODE]` |
| Is it safe? | **No** (deterministic ≠ safe). See §6 Strategy B. | `[FACT-DB]` |
| Buckwalter bridge segment → stem? | **None.** `quran_stems` has no buckwalter; `AlignedSegmentDto` has no stem buckwalter. | `[FACT-CODE]` |
| Arabic-normalized bridge segment → `quran_stems.stem_text`? | Exists *mechanically* (`form_arabic_normalized` vs `stem_text`) and resolves 482/483 — but against contextual/artifact rows; **not provenance**. | `[FACT-DB]` |
| Ambiguous/failure cases? | 1/483 has no text match (`72:16:1:3` لَّوِ — idghām rendering of لو); 60/483 match the *same* row as the head stem (circular); `stem_text` carries contextual shadda (مَّا, لَّا) so matches are rendering-dependent. | `[FACT-DB]` |

**`word-stem-corrected-arabic.json`** is the QUL whole-word stem source (read in `MorphologyImportSource` / `MorphologyManifestReader`). It maps **word → one stem**; it does **not** carry segment-level stems. `[FACT-CODE]`

---

## 5. Multi-STEM Inventory and Examples

All figures live-DB verified this session (read-only). `[FACT-DB]`

### 5.1 Totals
| Metric | Value |
|---|---:|
| Readable words (head morphology rows) | **77,432** |
| Segment rows | **128,219** |
| STEM segments (`kind='STEM'`) | **77,915** |
| `quran_stems` | **12,108** |
| `quran_lemmas` | **4,790** |
| `quran_roots` | **1,642** |

### 5.2 STEM-segment count per word
| STEM segments in word | Words |
|---:|---:|
| 1 | **76,949** |
| 2 | **483** |
| 0 | **0** |
| >2 | **0** |

Every word has exactly 1 or 2 STEM segments. **No 0-STEM and no 3+-STEM words exist.** (76,949 + 483×2 = 77,915 STEM segments ✓.)

### 5.3 Secondary STEM segments (the target of this change)
| Metric | Value |
|---|---:|
| Secondary STEM segments (non-first STEM in 2-STEM words) | **483** |
| Distinct lemma_ids on those segments | **6** |
| Distinct secondary forms | **14** |
| Secondary forms exactly text-matching a `quran_stems.stem_text` | **482 / 483** |
| Secondary segment with **no** text match | **1** (`72:16:1:3`, COND, لَّوِ) |
| Secondary text-match → **same** row as the word's head stem (circular) | **60** |
| Secondary text-match → a **different** stem row | **422** |
| `quran_stems` rows referenced by **no** head word (orphans) | **0** |
| Stems referenced **only** by multi-STEM words (clitic artifacts) | **6** |

### 5.4 Top multi-STEM POS patterns
| Pattern (segment POS, in order) | Words | Typical word |
|---|---:|---|
| P + REL | 228 | مِمَّا / فِيمَا / مِمَّن |
| ACC + PREV | 160 | إِنَّمَا |
| SUB + NEG | 42 | أَلَّا |
| CONJ + REL | 10 | — |
| COND + SUP | 8 | — |
| ACC + REL | 8 | أَنَّمَا |
| INT + PRO | 5 | أَلَّا (تفسيرية) |
| V + REL | 4 | بِئْسَمَا |
| (others ≤3 each) | ~18 | عَمَّا, لَوْ-compounds, … |

The entire multi-STEM surface is **clitic/particle compounds** — preposition+relative, accusative-particle+preventive-ما, أن+لا, etc. — **not** content vocabulary.

### 5.5 Representative examples (verbatim from DB)
| Location | Seg | POS | Form | Segment lemma_id | Word head stem_id → text |
|---|---:|---|---|---:|---|
| 2:3:6 | 2 | P | مِ | 130 (مِن) | 816 → **مِ** |
| 2:3:6 | 3 | REL | مَّا | 4 (ما) | 816 → **مِ** |
| 2:11:9 | 1 | ACC | إِنَّ | 11 | 12 → **إِنَّ** |
| 2:11:9 | 2 | PREV | مَا | 4 (ما) | 12 → **إِنَّ** |
| 2:90:1 | 1 | V | بِئْسَ | 2934 | 2935 → **بِئْسَ** |
| 2:90:1 | 2 | REL | مَا | 4 (ما) | 2935 → **بِئْسَ** |
| 72:16:1 | 3 | COND | لَّوِ | 175 (لو) | (head) → … *(no stem_text match)* |

Reading 2:3:6 (مِمَّا): the word is filed today under stem **مِ** (the مِن clitic). The **مَّا** segment is invisible as a stem. Proposed secondary `stem_id` would link it to the **ما** stem — *which already exists and is already visible from standalone ما.*

### 5.6 How many secondary stems become *newly visible*?
**Essentially zero.** `[FACT-DB]` `orphan_stems = 0` and only 6 stems are multi-STEM-only (and those 6 — أَ(61), عَمَّ, رُّبَ, ؤُمَّ, ئَ, مَّ — are the *primary* clitic artifacts, not the secondary targets). Every stem a secondary segment would link to already appears as some word's head stem. So:
- **No new stem page appears.**
- The function-word stems (ما, لا, من, لو, لن) would **gain ~483 extra occurrences** and the 483 compound words would appear under a second stem page.
- 6 pre-existing clitic-artifact stems (أَ, عَمَّ, …) are a separate data-quality wrinkle that *proper* segment-level modeling could clean up — but that is re-modeling, not just "add a column."

---

## 6. Mapping Strategies Evaluated

### Strategy A — Populate `segment.stem_id` from source identity at import
| Axis | Assessment |
|---|---|
| Feasibility | **Partial.** Works for single-STEM (= head stem). **Fails for the 483 secondary segments — no source stem exists.** |
| Data correctness | Single-STEM: exact. Secondary: not derivable. |
| Ambiguity | None for single-STEM; total for secondary (no source). |
| Operational risk | Low (single-STEM only). |
| Testability | High for single-STEM. |
| Migration/importer | Yes (column + importer + reseed). |
| Fail-closed | Yes — secondary stays null. |
| Quran-text safety | Safe (reads existing data). |
**Verdict:** Necessary but **insufficient** — leaves the 483 secondary segments null, i.e. does **not** meet the product goal.

### Strategy B — Match `segment.form_arabic_normalized` → `quran_stems.stem_text`
| Axis | Assessment |
|---|---|
| Feasibility | Mechanically yes; resolves **482/483**. |
| Determinism | **Yes** — `stem_text` is unique, so a match is 0-or-1. |
| **Safety** | **NO.** (1) Depends on **contextual idghām rendering** — مَّا/لَّا/لَّوِ carry compound-induced shadda; the 1 failure (لَّوِ) proves fragility, and a future re-render could break more. (2) Matches against **contextual artifact stem rows** — 60/483 are circular (match the head clitic itself). (3) It **invents provenance**: no QUL/Corpus source states the secondary segment's stem; assigning one is a linguistic decision. |
| Operational risk | High — silent mislinks, rendering-coupled. |
| Migration/importer | Yes. |
| Fail-closed | Only if unmatched → null + hard check (but 482 "successes" would pass silently). |
| Quran-text safety | No text mutation, but **violates source-traceability** (links not present in any source). |
**Verdict:** **Rejected as the population mechanism.** Deterministic but not safe — exactly the case the task says to mark *requiring curated artifact*.

### Strategy C — Curated segment→stem artifact (text-match as a *seed*, human-reviewed)
| Axis | Assessment |
|---|---|
| Feasibility | **High** — only 483 segments, **6 lemmas / 14 forms**; fully enumerable. |
| Data correctness | High — each link is reviewed; the لَّوِ outlier and the 60 circular cases are decided explicitly; new `quran_stems` rows created only where linguistically justified. |
| Ambiguity | Resolved by review (mirrors `CuratedLemmaDisambiguation`). |
| Operational risk | Low — small, versioned, SHA-tracked artifact; importer fails closed on anything not in the artifact. |
| Testability | High — artifact SHA + per-row assertions + `SEG-STEM-*` hard checks. |
| Migration/importer | Yes (column + artifact reader + importer + reseed). |
| Fail-closed | Yes — unlisted secondary segment ⇒ hard-check failure, not a guess. |
| Quran-text safety | Safe — artifact is curated metadata, source files untouched. |
**Verdict:** **The only safe way to meet the goal.** This is the `READY_WITH_CURATION` path.

### Strategy D — Do not add `segment.stem_id`; keep head-stem only
| Axis | Assessment |
|---|---|
| Feasibility | Trivial (status quo). |
| Correctness | Stems stay word/head-level; types already correct (`head_pos = stem POS`). |
| Cost | Zero. |
| Product gap | Secondary function-word stems remain un-attributed (the 483 compounds); but **no stem is actually hidden** (0 orphans). |
**Verdict:** Lowest-risk; acceptable given the modest, function-word-only benefit (§11).

---

## 7. Recommended Schema / Importer Design (if approved via Strategy C)

> Only pursue if §11 confirms the product wants it. Design mirrors the proven `lemma_id`/`root_id` work.

### 7.1 Column
- `quran_word_morphology_segments.stem_id INT NULL`.
- **Nullable**, but with an invariant: **non-null for every `kind='STEM'` segment**, **null for every non-STEM segment** (mirror `SEG-LEMMA-ID-STEM-ONLY` + `SEG-LEMMA-ID-REQUIRED-FOR-STEM`). Replace the current `SEG-STEM-ID-ABSENT` with `SEG-STEM-ID-STEM-ONLY` / `SEG-STEM-ID-REQUIRED-FOR-STEM`.
- **FK** → `quran_stems.id`, `OnDelete(Restrict)` (mirror Lemma/Root segment FKs).
- **Index** `IX_quran_word_morphology_segments_stem_id`.

### 7.2 Population (importer)
1. **Single-STEM word:** `segment.stem_id = word head stem_id` (reuse, like `ResolveLemmaId` single-STEM branch). No matching. Covers 76,949 / 77,432.
2. **Multi-STEM word:** for the primary STEM segment, `stem_id = head stem_id`; for each **secondary** STEM segment, look up the **curated artifact** (`segment_location → stem_id`). The artifact may reference an existing stem or a **new** `quran_stems` row (e.g. لَّوِ → canonical لو stem). **No text-match fallback in production** — unlisted ⇒ fail closed.
3. **`AlignedSegmentDto`** gains `int? StemId`; `CopySegmentsAsync` writes the new column (extend the `COPY quran_word_morphology_segments (...)` column list).

### 7.3 New curated source artifact `[REC]`
- `resources/import-sources/.../qul/segment-stem-corrected-arabic.json` (or similar), staged + manifest-tracked (SHA/size), read like `word-lemma-corrected-arabic.json`.
- Contents: the **483** secondary segment locations → resolved `stem_text`/`stem_id`, each linguistically reviewed. New stem rows declared explicitly where needed.
- Because it is only 6 lemmas / 14 forms, review is a bounded, one-time effort.

### 7.4 Hard checks (in `MorphologyValidationRunner`, fail ⇒ rollback)
| Check | Assertion |
|---|---|
| `SEG-STEM-ID-STEM-ONLY` | non-STEM segment ⇒ `stem_id` null |
| `SEG-STEM-ID-REQUIRED-FOR-STEM` | every STEM segment has non-null `stem_id` |
| `SEG-STEM-ID-SINGLE-STEM-HEAD-CONSISTENT` | single-STEM word: `segment.stem_id = head stem_id` |
| `SEG-STEM-ID-MULTI-STEM-CURATED` | every secondary STEM segment is covered by the curated artifact (no guessing) |
| `SEG-STEM-ID-RESOLVES` | every `segment.stem_id` references a real `quran_stems` row |
| `SEG-STEM-ID-NO-FANOUT` | one segment → at most one stem |
| `MORPH-DIM-COUNTS` | head-stem and word-level `quran_word_morphology.stem_id` unchanged |
| `MORPH-SOURCE-UNCHANGED` | corpus/QUL source SHAs unchanged (artifact is additive metadata) |

### 7.5 Out of scope / do-not
- **Do not** change `quran_word_morphology.stem_id` (head) — keep head consistent with the **primary** STEM segment.
- **No** production text-match fallback (Strategy B).
- **No** Quran text mutation; **no** edits to QUL/Corpus source files.

---

## 8. Stems Explorer Impact (if `segment.stem_id` exists)

Today Stems Explorer is word/head-level: matches via `quran_word_morphology.stem_id`, types via `head_pos` (see `stems-explorer-current-state-polish-report.md`). With `segment.stem_id`:

| Concern | Change |
|---|---|
| Catalogue counts | Recompute from `segment.stem_id` (mirror the segment-matched Lemmas reader); function-word stems gain compound occurrences. |
| Summary counts | Same — occurrences/ayahs/surahs/words by `segment.stem_id`. |
| Type distribution | By `segment.pos` of segments with `stem_id = @id` (already correct since `head_pos = stem POS`; secondary segments add their own POS). |
| Ayah matches | `WordMorphologySegments.Where(s => s.StemId == id)` → words → ayahs (replaces `m.StemId`). |
| Ayah type filter | `s.StemId == id && (typeCode == null || s.Pos == typeCode)` — segment-level, like Lemmas. |
| Words tab rows | Group matched **words** (a word may match via a secondary segment). |
| Lemmas tab | Related lemmas via `segment.lemma_id` of the same stem's segments (cleaner than the current head-morphology hop). |
| Highlighting | A compound word matches under two stems → highlight must use the **matched word ids** (a word highlights on both its stem pages); segment-level highlight ids optional. |
| URL identity | Unchanged (`stem` = `quran_stems.id`); curated new stem rows get ids like any other. |
| Cache keys | Unchanged keys; **values change** ⇒ flush cache / restart API after reseed. |

**Answers:**
- **Move to `segment.stem_id`?** Only if the curated artifact lands. Then yes, for parity with Lemmas.
- **Count by segment or word?** Count distinct **words** per stem (occurrence = a word whose STEM segment carries the stem); a word with two STEM segments of the *same* stem counts once (the 60 circular cases — `SEG-STEM-ID-NO-FANOUT` + distinct-word counting prevent double counting).
- **Word under two stem pages?** Show it on both, highlighting only that stem's matched segment/word; counts are per-stem distinct words.
- **Highlighting:** word ids suffice; segment ids are a nice-to-have.
- **Frontend contract change?** None required — Stems DTOs already match the shared shapes; this is a backend reader swap.

---

## 9. Migration / Reset Strategy

| Option | Assessment |
|---|---|
| Small migration **in Feature 017** | **Not recommended.** This needs a new curated/reviewed source artifact + linguistic sign-off + reseed + Stems reader rework — too large and too source-sensitive for the in-flight polish branch. |
| **Separate prerequisite Feature (018-style)** | **Recommended *if* product approves the goal.** Mirrors how `lemma_id`/`root_id` were delivered as their own prerequisite. Clean scope: artifact + migration + importer + checks + reseed, then a follow-up Stems-reader feature. |
| **Blocked** | Not warranted — the set is small and curatable; it is not impossible, just curation-gated. |

Reset: columns are import-populated ⇒ **local reseed** (`import-morphology --force` then cascade `generate-i3rab --force`). We are in development and full local reseed is available. Do **not** ship a silent text-match backfill on shared DBs.

---

## 10. Test & Validation Plan

- **Unit (assembler/resolver):** single-STEM ⇒ `segment.stem_id == head stem_id`; multi-STEM secondary ⇒ value from curated artifact; unlisted secondary ⇒ issue raised (fail closed); non-STEM ⇒ null.
- **Curated-artifact reader:** SHA/manifest present; every one of the 483 secondary segment locations covered; no duplicate/contradictory entries; new-stem declarations valid.
- **Integration (real import, Testcontainers):** run `SEG-STEM-*` checks; assert counts — STEM segments 77,915, secondary 483, single-STEM 76,949; `quran_word_morphology.stem_id` unchanged vs baseline.
- **Hard-check failure tests:** missing artifact entry, fan-out, non-STEM with stem_id, STEM without stem_id, dangling FK — each must roll back.
- **Regression — Lemmas:** `segment.lemma_id` path and Lemmas reads unchanged.
- **Regression — current head-stem semantics:** existing Stems read tests still pass until the reader is switched (separate feature).
- **Multi-STEM examples:** assert specific links — 2:3:6 secondary مَّا → ما stem; 2:11:9 secondary مَا → ما; 72:16:1 لَّوِ → curated لو stem (the outlier); a circular case (primary=secondary stem) counts the word once.
- **Source-safety:** `MORPH-SOURCE-UNCHANGED` green (artifact additive; QUL/Corpus untouched).

---

## 11. Risks & Open Decisions

1. **DECISION — is the goal worth it?** Live data shows **0 stems are invisible today** and the whole multi-STEM surface is **483 function-word compounds (6 lemmas)**. The change adds occurrence-attribution for ما/لا/من/لو/لن and dual-page display for compounds — it does **not** reveal hidden content stems. Weigh this modest benefit against a curation+migration+reseed+reader feature. *If the motivation was "secondary content stems are hidden," the data does not support that premise.*
2. **Curation correctness (linguistic).** The 483 links are real Arabic-grammar judgments (is the ما in إنّما "the same stem" as relative ما? do the 4 POS senses of مَّا collapse to one stem row, given `stem_text` is unique?). Needs a scholar/curator sign-off, not code.
3. **Contextual rendering.** `stem_text` and `form_arabic_normalized` carry idghām shadda (مَّا, لَّا, لَّوِ). Curation must decide canonical stem text and avoid coupling links to a render tier (the لَّوِ outlier shows the risk).
4. **Artifact stem rows.** 6 clitic-only stems (أَ, عَمَّ, رُّبَ, …) already exist as head stems of compounds. Segment-level modeling could rationalize these — but that widens scope beyond "add secondary stem_id."
5. **Cache/staleness.** Reader value changes require cache flush/API restart after reseed.
6. **Double-counting.** 60 circular secondary segments must count their word once (distinct-word counting + `SEG-STEM-ID-NO-FANOUT`).

---

## 12. Final Recommendation

**READY_WITH_CURATION**, delivered as a **separate prerequisite feature** — *contingent on a product decision that the benefit (§11.1) justifies the work.*

1. **First, decide (§11.1).** Given **0 invisible stems** and a 483-row function-word-only surface, confirm the goal is still wanted. If not → **Strategy D (do nothing)**; Stems stays head-level (already correct).
2. **If approved:** build a **reviewed curated artifact** (`segment-stem-corrected-arabic.json`, ~483 entries / 14 forms) — **not** auto text-matching (Strategy B is deterministic but unsafe). Add nullable FK+indexed `segment.stem_id`, populate single-STEM from head + secondary from the artifact, replace `SEG-STEM-ID-ABSENT` with the `SEG-STEM-ID-*` checks, reseed locally.
3. **Then** (separate follow-up) switch Stems Explorer readers to `segment.stem_id` for segment-matched counts/ayahs/filter, mirroring the Lemmas segment-matched reader. No frontend contract change.

**Do not:** populate via production text-matching; mutate Quran/QUL/Corpus source; change head `quran_word_morphology.stem_id`; bundle this into the 017 polish branch; or proceed before the §11.1 product decision.

### Strict-safety determination
The source **cannot** safely map each STEM segment to a stem dimension row on its own: single-STEM is safe (head reuse), but the 483 multi-STEM secondary segments have **no source stem identity** and the only mechanical bridge (Arabic text-match) is **deterministic but not safe** (contextual rendering, artifact rows, invented provenance, 1 known miss). Therefore a **curated, reviewed artifact is mandatory** — auto-mapping is explicitly rejected.

---

*Report only. Read-only `SELECT` queries against the local dev DB (`quran_dashboard`). No code, schema, data, migration, importer, test, frontend, or commit changes. DB password was read from the existing local user-secrets store for this session only and is not reproduced here or written to any file.*
