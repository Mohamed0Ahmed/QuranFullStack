# Word Types Explorer (أنواع الكلمات) — Capability & UI Implementation-Readiness Report

**Feature:** 019 — Word Types Explorer / أنواع الكلمات
**Task type:** REPORT ONLY (no code, seed, migration, importer, API, frontend, or test changes)
**Branch inspected:** `018-segment-stems-and-stems-explorer`
**Date:** 2026-06-30
**Verdict (summary):** **READY_WITH_NOTES** — the word-level data needed to build the type tree and all secondary filters already exists, is indexed, and is reused by existing readers. No migration or importer change is required. The remaining work is one new read path (a type-filtered word list) plus a small filter-picker component; the only data caveat is confirming the corrected `PRO` POS label has been reseeded into the live DB.

---

## 0. Executive Summary

- **All four main word types and every requested secondary filter are derivable word-level from a single table, `quran_word_morphology` (entity `WordMorphology`).** It carries, per word occurrence: `HeadPos` (FK → `quran_pos_tags.code`, NOT NULL, indexed), `IsVerb`, `VerbTense`, `VerbVoice`, `CaseFeature` (all indexed), plus `RootId`/`LemmaId`/`StemId`. No segment join is needed for the tree or the case/tense/voice filters.
- **`head_pos` reliably builds the main type tree.** It is defined and DB-validated as *"first STEM POS by segment_number"* (`MorphologyValidationRunner.cs:104`), and `IsVerb ⇔ head_pos = 'V'` is enforced as an import invariant (`MorphologySql.cs:116-119`). Word types are already correct for the head-stem model; the only known gap is that secondary STEMs in multi-STEM compounds are invisible (a coverage gap, not a correctness bug).
- **Type labels come from one source of truth:** `PosTagSeed.cs` (49 codes) → copied into `quran_pos_tags` → joined by every reader. Each tag has `ArabicLabel`, `EnglishLabel`, `Category` (`noun` / `verb` / `particle` / `other`), and `SortOrder`. The frontend hard-codes **no** Arabic POS labels.
- **The one historically-wrong label (`PRO`) is already corrected at the seed** (`PRO` → `حرف نهي`, category `particle`). The residual risk is purely operational: the live DB mirrors the seed and only updates on a `force` morphology reseed, so v1 must confirm the reseed ran (otherwise `head_pos = 'PRO'` words still mis-bucket as `اسم`).
- **The four broad classes the product wants already exist as a reader rule:** `EfUniqueWordsReader.ResolvePrimaryWordTypeBroadLabel(code, category)` maps `noun → اسم`, `verb → فعل`, `particle → حرف`, special-casing `INL → حروف مقطّعة`. The product's `حرف وأداة` main type = `category = 'particle'` (excluding `INL`).
- **Ayah markers are already excluded consistently.** Every word read path filters `!IsAyahMarker`, and the unique-word aggregate tables are built from non-marker words only.
- **Strong frontend reuse base.** The Roots / Lemmas / Stems / Unique-Words explorers already implement the exact split-screen pattern this page wants (left table + right details panel), shared keyboard-nav utilities, URL-state sync, per-feature facades/caches, and reusable `ayah-matches-list` / `surah-occurrences-list` / `*-details-panel` components.
- **The page should be table-first, not tree-first.** The type hierarchy is a compact filter-picker; the main surface is the existing explorer table + right details card.
- **Main new backend work:** the Unique-Words list reader does **not** currently accept a type filter — a new type-filtered word-list read path is the principal backend addition (mirrors the existing winner-type and primary-root queries plus a `WHERE`). Everything else is reuse.

---

## 1. Current Data Capability

### 1.1 Tables / columns that support this page now

| Table | Entity / file | Columns used by this page | Role |
| --- | --- | --- | --- |
| `quran_word_morphology` | `WordMorphology.cs` | `QuranWordId`, `HeadPos`, `SegmentCount`, `RootId`, `LemmaId`, `StemId`, `IsVerb`, `VerbTense`, `VerbVoice`, `CaseFeature`, `HeadFeaturesJson` | **Primary source.** One row per word occurrence. Carries the head type + all secondary grammatical features word-level. |
| `quran_pos_tags` | `PosTag.cs` / `PosTagSeed.cs` | `Code`, `ArabicLabel`, `EnglishLabel`, `Category`, `SortOrder`, `Description` | Type labels + broad-class grouping (join target of `head_pos`). |
| `quran_words` | `QuranWord.cs` | `Id`, `IsAyahMarker`, `UniqueSimpleWordId`, `UniqueTashkeelWordId`, location/text fields | Links occurrences to surah/ayah/word location, the two unique-word identities, and the marker flag. |
| `quran_words_unique_simple` / `quran_words_unique_tashkeel` | (read via `EfUniqueWordsReader`) | `Id`, `text_uthmani_simple` / `text_uthmani`, `occurrences_count`, `ayahs_count`, `surahs_count`, `first_word_order_in_mushaf` | **Pre-aggregated** word identity + occurrence/ayah/surah counts. Reusable directly for the table's count columns. |
| `quran_roots` / `quran_lemmas` / `quran_stems` | `QuranRoot.cs` / `QuranLemma.cs` / `QuranStem.cs` | `Id`, text fields | Source for the الجذر / الأصل / الصيغة columns. |
| `quran_word_morphology_segments` | `WordMorphologySegment.cs` | `Kind`, `Pos`, features, i‘rab | **Out of scope for v1 counts** (segment/prefix/suffix POS). Only relevant later for full i‘rab detail in the card. |

**Indexes confirmed** (`20260610155434_AddQuranWordMorphology`): `IX_quran_word_morphology_head_pos`, plus indexes on `CaseFeature` and `VerbTense`. `head_pos`, `pos` (segments) are FKs into `quran_pos_tags`. → the type tree, the case filter, and the tense filter are all index-backed.

### 1.2 Can `head_pos` reliably build the main word type tree?

**Yes.**

- **Definition is deterministic and DB-validated:** `head_pos` = *"first STEM pos by segment_number"* (`MorphologyValidationRunner.cs:104`); the head is always the first STEM segment, with a documented fallback to the first segment POS when no STEM exists (`MorphologyAssembler.cs` head resolution).
- **`IsVerb` is exactly `head_pos = 'V'`** — an import invariant the validation runner fails closed on (`MorphologySql.cs:116-119`: `m.is_verb IS DISTINCT FROM (m.head_pos = 'V')`). So the verb parent can use either `IsVerb` or `head_pos = 'V'` interchangeably.
- **Every code the corpus emits is covered by the seed**, enforced by a fail-closed import test (`MorphologyPosResolutionTests.PosTagSeed_covers_all_observed_corpus_pos_codes`). There is no risk of a head word whose `head_pos` has no label row.
- **Known limitation (not a blocker):** in multi-STEM compound words the head is the first STEM only; secondary STEMs are not represented at the word level. This is the documented head policy and is acceptable for a *main word type* tree (the product decision explicitly adopts it).

### 1.3 Where are type labels sourced from?

Single source of truth chain:

```
PosTagSeed.cs  (49 PosTag rows: Code, ArabicLabel, EnglishLabel, Category, SortOrder, Description)
   │  MorphologyBulkCopier.CopyPosTagsAsync  (binary COPY, during force morphology import)
   ▼
quran_pos_tags  (DB mirror)
   │  join head_pos = code
   ▼
readers (EfUniqueWordsReader, EfWordAnalysisReader, …) → API LocalizedLabel{Ar,En}
   ▼
frontend renders API label verbatim (no hardcoded Arabic POS strings)
```

The frontend has **no** hardcoded Arabic POS labels; it renders whatever the API returns (`headPosLabel.ar`, `primaryWordTypeBroadArabicLabel`). Fixing labels at the seed + reseeding propagates to the UI with zero frontend change.

### 1.4 Are POS labels corrected and safe to display?

**Yes, at the seed.** The single historically-confirmed defect — `PRO` mislabeled `ضمير منفصل` / category `noun` — **is already corrected in `PosTagSeed.cs`**: `PRO` = `حرف نهي`, English `Prohibition Particle`, category `particle` (`PosTagSeed.cs` SortOrder 20). All other 48 codes are CORRECT/ACCEPTABLE per the Feature-017 POS label review, with a few low-priority NEEDS_REVIEW notes (`SUB`, `EXL`, `TIM` duplicate, `INTG` context-dependent) that do not affect the four broad classes.

**Operational caveat (the one real data note):** `quran_pos_tags` is **not** seeded by migration; it is (re)written only by a `force` morphology import (truncate + binary COPY of all six morphology tables). The DB faithfully mirrors whatever seed was last imported. **v1 must verify a reseed has run after the `PRO` correction**, otherwise the live DB still holds `PRO = ضمير منفصل / noun` and ~327 head occurrences of prohibitive `لا` will mis-bucket under `اسم` instead of `حرف وأداة`. This is verifiable with a read-only query (`SELECT code, arabic_label, category FROM quran_pos_tags WHERE code='PRO';`) — recommended as the first pre-implementation check.

### 1.5 How should multi-STEM words be handled?

Use the **existing head policy unchanged**: the word's type is its `head_pos` = first STEM by `segment_number`. Secondary STEMs are deliberately not surfaced (out of scope for v1, consistent with the product decision). This keeps the tree counts equal to the word-occurrence counts (one type per word, no double counting). Segment/prefix/suffix POS must **not** contribute to tree counts — and with this model they structurally cannot, because the tree reads only `quran_word_morphology` (one row per word), never the segments table.

### 1.6 Are ayah markers excluded consistently?

**Yes.**

- All word-level read queries filter `!w.IsAyahMarker` (e.g. `EfUniqueWordsReader.ReadableMatchesQuery`, lines 270-273).
- The pre-aggregated unique-word tables (`quran_words_unique_simple/tashkeel`) are built from non-marker words, so their `occurrences_count` / `ayahs_count` / `surahs_count` already exclude markers.
- `quran_word_morphology` rows correspond to real words (row count ≈ Quran word count without markers); when querying it directly, **defensively join `quran_words` and filter `!IsAyahMarker`** to remain consistent with the rest of the app.

---

## 2. Proposed Type Tree

The tree has **real word types** (nodes that filter on `head_pos` / its category) and **secondary grammatical filters** (case / tense / voice that filter on word-level features *within* a selected type). They are kept strictly separate below.

### 2.1 Tree diagram

```
أنواع الكلمات (Word Types)
│
├─ ① اسم            ── parent: category(head_pos) = 'noun'           [head_pos] ✔ v1
│     ├─ اسم            ── head_pos = 'N'                              [head_pos] ✔ v1
│     ├─ اسم علم        ── head_pos = 'PN'                             [head_pos] ✔ v1
│     ├─ صفة            ── head_pos = 'ADJ'                            [head_pos] ✔ v1
│     ├─ ضمير           ── head_pos = 'PRON'                          [head_pos] ✔ v1
│     ├─ اسم موصول      ── head_pos = 'REL'                           [head_pos] ✔ v1 (optional)
│     ├─ اسم إشارة      ── head_pos = 'DEM'                           [head_pos] ✔ v1 (optional)
│     └─ ظرف            ── head_pos IN ('T','LOC')                    [head_pos] ✔ v1 (optional)
│        └── secondary (nominal case): الكل / مرفوع / منصوب / مجرور / غير محدد   ── m.CaseFeature
│
├─ ② فعل            ── parent: IsVerb = true  (≡ head_pos = 'V')      [head_pos] ✔ v1
│        ├── secondary (tense): الكل / ماض / مضارع / أمر              ── m.VerbTense ∈ {past,present,imperative}
│        └── secondary (voice): الكل / معلوم / مجهول                  ── m.VerbVoice ∈ {active,passive}
│
├─ ③ حرف وأداة       ── parent: category(head_pos) = 'particle' AND head_pos <> 'INL'   [head_pos] ✔ v1
│     ├─ حرف جر         ── head_pos = 'P'                             [head_pos] ✔ v1 (optional)
│     ├─ حرف عطف        ── head_pos = 'CONJ'                          [head_pos] ✔ v1 (optional)
│     ├─ حرف نفي        ── head_pos = 'NEG'                           [head_pos] ✔ v1 (optional)
│     ├─ حرف شرط        ── head_pos = 'COND'                          [head_pos] ✔ v1 (optional)
│     └─ … (other particle head codes by count)                      [head_pos] ✔ v1 (optional)
│        └── NO nominal case filter, NO tense/voice filter
│
└─ ④ حروف مقطّعة     ── leaf: head_pos = 'INL'                        [head_pos] ✔ v1
         └── no children, no secondary filters
```

`[head_pos]` = node filters on the word-level `head_pos` (or its `category`). Secondary filters (`m.CaseFeature`, `m.VerbTense`, `m.VerbVoice`) are word-level head-STEM features, **not** new node types.

### 2.2 Node definitions

#### Main word types (real types — based on `head_pos` / its category)

| Node | Arabic label | Source rule | Based on | Safe v1? |
| --- | --- | --- | --- | --- |
| اسم (parent) | اسم | `category = 'noun'` (join `head_pos → quran_pos_tags`) | head_pos category | ✅ |
| → اسم | اسم | `head_pos = 'N'` | head_pos | ✅ |
| → اسم علم | اسم علم | `head_pos = 'PN'` | head_pos | ✅ |
| → صفة | صفة | `head_pos = 'ADJ'` | head_pos | ✅ |
| → ضمير | ضمير | `head_pos = 'PRON'` | head_pos | ✅ |
| → اسم موصول | اسم موصول | `head_pos = 'REL'` | head_pos | ✅ optional |
| → اسم إشارة | اسم إشارة | `head_pos = 'DEM'` | head_pos | ✅ optional |
| → ظرف | ظرف زمان/مكان | `head_pos IN ('T','LOC')` | head_pos | ✅ optional |
| فعل (parent) | فعل | `IsVerb = true` (≡ `head_pos = 'V'`) | head_pos | ✅ |
| حرف وأداة (parent) | حرف وأداة | `category = 'particle' AND head_pos <> 'INL'` | head_pos category | ✅ |
| → specific particle (حرف جر، حرف عطف، …) | from `ArabicLabel` | `head_pos = '<code>'` | head_pos | ✅ optional |
| حروف مقطّعة (leaf) | حروف مقطّعة | `head_pos = 'INL'` | head_pos | ✅ |

> Note on the broad label: the product's `حرف وأداة` corresponds to the reader's broad class `حرف` (`category = 'particle'`). `INL` is technically `category = 'particle'` in the seed but is intentionally promoted to its own main type `حروف مقطّعة` (matching `ResolvePrimaryWordTypeBroadLabel`'s `INL` special-case). Therefore the particle parent **must exclude `INL`**, or `INL` words would be double-counted under both `حرف وأداة` and `حروف مقطّعة`.

#### Secondary grammatical filters (NOT word types — features within a type)

| Filter | Shown when selected type is… | Options (Arabic) | Source column | Value mapping | Safe v1? |
| --- | --- | --- | --- | --- | --- |
| Nominal case | nominal (اسم / اسم علم / صفة / ضمير / …) | الكل / مرفوع / منصوب / مجرور / غير محدد | `m.CaseFeature` | `nominative` / `accusative` / `genitive` / `NULL` | ✅ |
| Verb tense | verbal (فعل) | الكل / ماض / مضارع / أمر | `m.VerbTense` | `past` / `present` / `imperative` | ✅ |
| Verb voice | verbal (فعل) | الكل / معلوم / مجهول | `m.VerbVoice` | `active` / `passive` | ✅ |

- `غير محدد` (case) is required because `CaseFeature` is nullable: it is populated only when the head STEM features contain `NOM` / `ACC` / `GEN` (`MorphologyAssembler.MapCaseFeature`, lines 636-654). Many nominal words (and most particles) have `NULL` case.
- For **particle / tool** and **حروف مقطّعة** types: **no** nominal case filter and **no** tense/voice filter (confirmed by the product rule; these features are null/meaningless there).
- `VerbTense` / `VerbVoice` are populated only when `IsVerb` (lines 170-171), so they are only meaningful under the verb branch — exactly where they are shown.

### 2.3 Real types vs secondary filters — the clean separation

- **Real word types** change *which rows* are in the table by `head_pos` (a single categorical word attribute). They are mutually exclusive and partition all words: every word has exactly one `head_pos`, hence exactly one main type (`اسم` | `فعل` | `حرف وأداة` | `حروف مقطّعة`). The sum of the four main-type counts = total non-marker words → a natural count-integrity assertion.
- **Secondary filters** do **not** introduce new types; they refine an already-typed set by a word-level head-STEM feature (`CaseFeature` / `VerbTense` / `VerbVoice`). They never apply across type boundaries (no case filter on verbs, no tense filter on nouns).

---

## 3. Word Selection Logic

All selection is **word-level**: every predicate below targets exactly one row per word in `quran_word_morphology` (joined to `quran_words` for the marker filter and to `quran_pos_tags` for the category). **No segment-level query is involved** for the tree or the secondary filters. This mirrors the existing `typeCode` filter precedent, which filters on `m.HeadPos` (word-level), already shipped for the Lemmas and Stems ayah lists.

Base predicate (always applied): `qw.IsAyahMarker = false`.

### 3.1 Parent types

| Parent | Predicate (word-level) |
| --- | --- |
| all nouns (اسم) | `pt.Category = 'noun'` (`JOIN quran_pos_tags pt ON pt.code = m.head_pos`) |
| all verbs (فعل) | `m.IsVerb = true` (equivalently `m.head_pos = 'V'`) |
| all particles (حرف وأداة) | `pt.Category = 'particle' AND m.head_pos <> 'INL'` |
| disjoint letters (حروف مقطّعة) | `m.head_pos = 'INL'` |

### 3.2 Child types

| Child | Predicate |
| --- | --- |
| proper noun (اسم علم) | `m.head_pos = 'PN'` |
| pronoun (ضمير) | `m.head_pos = 'PRON'` |
| adjective (صفة) | `m.head_pos = 'ADJ'` |
| plain noun (اسم) | `m.head_pos = 'N'` |
| relative / demonstrative / adverb (optional) | `m.head_pos = 'REL'` / `'DEM'` / `IN ('T','LOC')` |
| specific particle (optional) | `m.head_pos = '<code>'` (e.g. `'P'`, `'CONJ'`, `'NEG'`) |

### 3.3 Verb tense (within فعل)

| Option | Predicate |
| --- | --- |
| الكل | `m.IsVerb = true` |
| ماض | `m.IsVerb = true AND m.VerbTense = 'past'` |
| مضارع | `m.IsVerb = true AND m.VerbTense = 'present'` |
| أمر | `m.IsVerb = true AND m.VerbTense = 'imperative'` |

(Note: imperative verbs are coded `head_pos = 'V'` with `VerbTense = 'imperative'`; the `IMPV` code is the *prefixed lām of command*, not the verb, and is segment-only/out of scope.)

### 3.4 Nominal case (within a nominal type)

| Option | Predicate |
| --- | --- |
| الكل | (no extra predicate beyond the nominal type) |
| مرفوع | `m.CaseFeature = 'nominative'` |
| منصوب | `m.CaseFeature = 'accusative'` |
| مجرور | `m.CaseFeature = 'genitive'` |
| غير محدد | `m.CaseFeature IS NULL` |

### 3.5 Word-level confirmation

Confirmed **word-level, not segment-level**. `quran_word_morphology` is keyed by `QuranWordId` (one row per word). `HeadPos`, `IsVerb`, `VerbTense`, `VerbVoice`, `CaseFeature` are all columns on that row. The segments table (`quran_word_morphology_segments`) is only needed for full per-segment i‘rab in the details card — never for the tree, counts, or secondary filters.

---

## 4. UI Feasibility

### 4.1 Intended UI vs existing explorer patterns

| Intended element | Existing equivalent | Reuse verdict |
| --- | --- | --- |
| Top filter section | New (the type-picker). Closest existing: `unique-words-search-bar`, `lemma-ayah-type-filters` / `stem-ayah-type-filters` chip rows | **Mostly new**, but chip-filter UX + responsive chip layout already exist to copy. |
| Main word table | `unique-words-table`, `roots-table`, `lemmas-table`, `stems-table` (+ shared `explorer-table-*` keyboard-nav / scroll / focus utilities) | **Reuse pattern directly.** |
| Right-side selected details card | `root-details-panel`, `lemma-details-panel`, `stem-details-panel` | **Reuse pattern directly.** |
| Selected-word ayahs | `ayah-matches-list`, `highlighted-ayah`, `lemma-ayah-match.mapper` / `stem-ayah-match.mapper` | **Reuse.** |
| Selected-word surahs | `surah-occurrences-list`, `missing-surahs-list`, `unique-words-surahs` util | **Reuse.** |
| Counts chip | `word-count-chip` | **Reuse.** |
| Type-hierarchy-as-filter (parent select + expand arrow for children) | None exactly; conceptually a small tree/disclosure list | **New, but small** — a presentational picker over static metadata. |
| URL/state sync, facade, cache | `*-explorer.facade.ts`, `*-detail.facade.ts`, `*-cache.ts`, `*-url-sync.ts` per feature | **Reuse pattern directly.** |

### 4.2 Recommended components/patterns to reuse

- **Page shell & split view:** copy `unique-words-page` / `stems-explorer-page` structure (left table + right details, responsive collapse, restore-from-URL).
- **Table:** model the new `word-types-table` on `unique-words-table` (it already renders display text + type label + root + occurrence/ayah/surah counts — the closest column set to the proposed columns).
- **Shared utils (no change):** `explorer-table-keydown`, `explorer-table-focus-controller`, `explorer-table-column-nav`, `explorer-table-scroll`, `explorer-count-active`, `table-scrollbar-gutter-sync`.
- **Details card:** model on `stem-details-panel` / `lemma-details-panel` (tabs for words / ayahs / surahs, chip filters inside the Ayahs tab).
- **State:** new `word-types-explorer.facade.ts` + `word-types-detail.facade.ts` + `word-types-cache.ts` + `word-types-url-sync.ts` mirroring the existing four explorers.
- **Labels:** new `word-types.labels.ts` for the *static UI strings* (filter button labels, secondary-filter option labels like `مرفوع`/`ماض`/`معلوم`). **POS type labels themselves must continue to come from the API** (do not hardcode `اسم`/`فعل`/`حرف وأداة` derived from `head_pos`; let the metadata endpoint supply them). Follow the established TDZ-safe getter pattern for label consts.

### 4.3 Table-first, not tree-first?

**Confirmed: table-first.** The product decision already states the type hierarchy is only a filter picker, not the page layout. This matches every existing explorer (the dimension list/tree is a navigation aid; the table + details are the page). Build the type tree as a compact left-rail or top-bar picker; keep the table the primary surface.

### 4.4 Right-side details card shows selected-word details + ayahs?

**Confirmed and fully supported.** The selected row is a word (unique-word identity). The details card should show: the word's counts (occurrences/ayahs/surahs) via the existing summary shape, its ayah occurrences via `ayah-matches-list`, its surah distribution via `surah-occurrences-list`, and — as an action — its full grammatical analysis. A per-word full-analysis endpoint already exists (`GET api/mushaf/words/{location}/analysis` → `WordAnalysisResponse`, with morphology + segments + simplified i‘rab), so the "analysis" action can reuse it for a chosen occurrence.

---

## 5. API / Read Model Proposal (no implementation)

### 5.1 Reuse vs new

| Capability | Status |
| --- | --- |
| Per-word full analysis (for the details "analysis" action) | **Exists** — `GET api/mushaf/words/{location}/analysis`. |
| Word-level `head_pos` type filter precedent | **Exists** — `typeCode` on Lemmas/Stems ayah endpoints filters `m.HeadPos` word-level. |
| Winner (dominant) `head_pos` per unique word | **Exists** — `EfUniqueWordsReader.LoadPrimaryWordTypesAsync`. |
| Primary root per unique word | **Exists** — `LoadPrimaryRootsAsync`. |
| Pre-aggregated occurrence/ayah/surah counts per unique word | **Exists** — on `quran_words_unique_*` tables. |
| **Type-filtered word *list*** (filter the unique-words list by primary type code/category + optional secondary feature) | **NEW** — the Unique-Words list reader signature (`GetUniqueWordsPageAsync(kind, search, sort, page, pageSize)`) has **no** type parameter. This is the principal backend addition. |
| Type tree metadata + per-node counts | **NEW** — a small read returning the 4 main types + children + counts. |

**Recommendation:** add a thin new read path (new controller area, e.g. `api/words/word-types`, mirroring the Lemmas/Stems controller + cached-reader + EF-reader layering) rather than overloading the Unique-Words endpoints, so the existing endpoints' contracts and caches stay untouched. The new reader can reuse the same winner-type and primary-root query shapes already in `EfUniqueWordsReader`. No migration, importer, or `ApiResponse` change is required.

### 5.2 Proposed response shapes (illustrative; conform to the global `ApiResponse<T>` envelope)

**(a) Type filter metadata / tree** — static-ish, cacheable:

```jsonc
// GET api/words/word-types/tree?mode=simple
{
  "mainTypes": [
    {
      "key": "noun", "code": null, "label": { "ar": "اسم", "en": "Noun" },
      "wordsCount": 0, "occurrencesCount": 0,
      "secondaryFilter": "nominalCase",
      "children": [
        { "code": "PN",  "label": { "ar": "اسم علم", "en": "Proper Noun" }, "wordsCount": 0 },
        { "code": "ADJ", "label": { "ar": "صفة",     "en": "Adjective"   }, "wordsCount": 0 },
        { "code": "PRON","label": { "ar": "ضمير",    "en": "Pronoun"     }, "wordsCount": 0 }
      ]
    },
    { "key": "verb", "code": "V", "label": { "ar": "فعل", "en": "Verb" },
      "secondaryFilter": "verbTenseVoice", "children": [] },
    { "key": "particle", "code": null, "label": { "ar": "حرف وأداة", "en": "Particle" },
      "secondaryFilter": null, "children": [ /* optional specific particle codes */ ] },
    { "key": "inl", "code": "INL", "label": { "ar": "حروف مقطّعة", "en": "Quranic Initials" },
      "secondaryFilter": null, "children": [] }
  ],
  "secondaryOptions": {
    "nominalCase": [
      { "value": "all",        "label": { "ar": "الكل" } },
      { "value": "nominative", "label": { "ar": "مرفوع" } },
      { "value": "accusative", "label": { "ar": "منصوب" } },
      { "value": "genitive",   "label": { "ar": "مجرور" } },
      { "value": "unset",      "label": { "ar": "غير محدد" } }
    ],
    "verbTense": [ {"value":"all","label":{"ar":"الكل"}}, {"value":"past","label":{"ar":"ماض"}},
                   {"value":"present","label":{"ar":"مضارع"}}, {"value":"imperative","label":{"ar":"أمر"}} ],
    "verbVoice": [ {"value":"all","label":{"ar":"الكل"}}, {"value":"active","label":{"ar":"معلوم"}},
                   {"value":"passive","label":{"ar":"مجهول"}} ]
  }
}
```

Type **labels come from `quran_pos_tags`** (do not hardcode in the API or UI). Per-node counts are optional in v1 (see §6 for the count-source decision).

**(b) Filtered word table** — the main grid (paged):

```jsonc
// GET api/words/word-types/words?mode=simple&type=noun&childCode=PN&case=genitive&page=1&pageSize=25&sort=occurrences
{
  "page": 1, "pageSize": 25, "totalCount": 0,
  "items": [
    {
      "id": 1234,                       // unique-word id (mode-scoped identity)
      "displayText": "…",               // text_uthmani_simple (simple) / text_uthmani (tashkeel)
      "primaryTypeCode": "PN",
      "primaryTypeLabel": { "ar": "اسم علم", "en": "Proper Noun" },
      "primaryTypeBroadLabel": { "ar": "اسم" },
      "caseFeature": "genitive",        // winner-occurrence feature OR null; see §7 caveat
      "rootId": 10, "rootText": "…",    // primary root (reuse LoadPrimaryRoots)
      "lemmaText": "…",                 // primary lemma (NEW winner query, optional v1)
      "stemText": "…",                  // primary stem (NEW winner query, optional v1)
      "occurrencesCount": 0, "ayahsCount": 0, "surahsCount": 0
    }
  ]
}
```

**(c) Selected word summary / details:** reuse the existing `UniqueWordSummaryDto` shape (`id, kind, displayText, occurrencesCount, ayahsCount, surahsCount, missingSurahsCount`).

**(d) Selected word ayah matches:** reuse the existing unique-word readable-matches → ayah-list shape (paged), optionally accepting the same secondary feature filter so the ayah list narrows with the table.

**(e) Selected word surah distribution:** reuse `GetMentionedSurahsAsync` → `UniqueWordSurahsResponse` (+ `missing-surahs-list`).

### 5.3 Caching / logging

Mirror `CachedLemmasReader` / `LemmasCacheKeys` (e.g. `wordtypes:{mode}:{type}:{childCode}:{feature}:p{page}:s{pageSize}`) and the existing structured completion logging. No new infra.

---

## 6. Counts and Columns

### 6.1 How to calculate the counts

- **occurrences / المواضع, ayahs / الآيات, surahs / السور:** when the row identity is the **unique word** and no secondary feature filter is active, use the **pre-aggregated** `occurrences_count` / `ayahs_count` / `surahs_count` columns directly (zero computation, already marker-free). This is the cheapest, most consistent path and is exactly what the Unique-Words explorer already returns.
- **When a secondary feature filter (case/tense/voice) is active:** those features are per-occurrence, so the pre-aggregated counts (which span all occurrences of the word) no longer match the filtered set. Two options:
  - **(Recommended v1)** Recompute counts over the matching occurrences: `occurrences = COUNT(*)`, `ayahs = COUNT(DISTINCT verse_key)`, `surahs = COUNT(DISTINCT surah)` over `quran_word_morphology ⋈ quran_words` rows where the word's unique id = row id **and** the feature predicate holds (markers excluded). Small grouped query, well-defined.
  - (Simpler fallback) Keep showing the word's full counts and treat the secondary filter as *membership only* ("words that have ≥1 occurrence with this feature"). Less precise; note it explicitly if chosen.
- **unique simple/tashkeel identity:** already exists as the two `quran_words_unique_*` tables. Per the established identity rule, the **statistical/identity key is the clean imlaei-simple** word (Simple mode); **display stays Uthmani**. Tashkeel mode uses the full-tashkeel identity. The page should reuse both identities rather than invent a new grouping.

### 6.2 Row identity — the one genuine design decision

`head_pos` (and case/tense/voice) is a property of a **word occurrence**, but the count columns imply **aggregated rows**. Three coherent models:

| Model | Row = | Type column | Secondary-filter fit | Cost | Reuse |
| --- | --- | --- | --- | --- | --- |
| **A. Unique-word + winner type (recommended)** | a unique word (simple/tashkeel) whose **dominant** `head_pos` matches the type | dominant type (stable) | needs occurrence-scoped recount (§6.1) | low | maximal — reuses winner-type, primary-root, pre-aggregated counts |
| B. Distinct morphological form | distinct `(displayText, head_pos, case/tense/voice)` | exact per row | exact, native | medium (new aggregation) | partial |
| C. Raw occurrence list | one word occurrence | exact per row | exact, native | low query, but المواضع/الآيات/السور are trivially 1 | low |

**Recommendation:** ship **Model A** for v1 (it reuses the most and gives a familiar Unique-Words-style table), with the §6.1 occurrence-scoped recount when a secondary filter is active. Keep Model B documented as the "scientifically exact per-feature" upgrade if the dominant-type approximation proves confusing for words whose occurrences split across types.

### 6.3 Proposed columns — verdict

| Column | Source | v1 verdict |
| --- | --- | --- |
| الكلمة | unique word `displayText` (Uthmani / Uthmani-simple by mode) | ✅ ready (reuse) |
| النوع | primary `head_pos` label (`quran_pos_tags.arabic_label`; or broad label) | ✅ ready (reuse winner-type) |
| الإعراب / السمة | `m.CaseFeature` / tense / voice of the word | ⚠️ **only stable per-row in Model B/C, or as the winner-occurrence feature in Model A.** Recommend showing it in the **details card**, and in the table only when a secondary filter is active (then it is constant). |
| الجذر | primary root (`LoadPrimaryRootsAsync`) | ✅ ready (reuse) |
| الصيغة (stem/form) | primary stem | ➕ NEW winner query (mirror primary-root); low risk; optional v1 |
| الأصل (lemma) | primary lemma | ➕ NEW winner query (mirror primary-root); low risk; optional v1 |
| المواضع | `occurrences_count` (or recount) | ✅ ready |
| الآيات | `ayahs_count` (or recount) | ✅ ready |
| السور | `surahs_count` (or recount) | ✅ ready |

**Recommended v1 column set:** الكلمة، النوع، الجذر، المواضع، الآيات، السور (all reuse). Add الأصل / الصيغة via the same winner-query pattern if desired (low risk). Treat الإعراب/السمة as a details-card field (or a conditional column under an active secondary filter).

### 6.4 Simple vs tashkeel display

Reuse the existing **two-mode toggle** (the Unique-Words explorer already exposes Simple ↔ Tashkeel). **Default to Simple** (clean imlaei-simple identity; Uthmani-simple display), with a toggle to Tashkeel — consistent with the rest of the Words hub. No new toggle mechanism needed.

---

## 7. I‘rab Integration

- **v1 i‘rab surface is the secondary filters only**, all sourced from word-level head-STEM features already on `quran_word_morphology`:
  - **Nominal types:** case filter/display from `m.CaseFeature` (`nominative`/`accusative`/`genitive`/null) → مرفوع/منصوب/مجرور/غير محدد.
  - **Verbal types:** tense from `m.VerbTense`, voice from `m.VerbVoice`.
- **Full i‘rab** (per-segment parsed grammar) is **not** part of the tree or counts in v1. It should appear only as an **ayah-/word-level detail action** in the details card, served by the existing `GET api/mushaf/words/{location}/analysis` (`WordAnalysisResponse.RenderedWordSegments[].SegmentI3rabArabic` + rule signature/family/status). Reuse, do not rebuild.
- **Segment-level simple i‘rab must not affect tree counts.** It structurally cannot in this design: the tree and counts read only `quran_word_morphology` (one row/word); the segments table is touched only when rendering the full-analysis detail for a single chosen occurrence.
- Keep POS labels and i‘rab labels separate (two seeds: `quran_pos_tags` vs `quran_i3rab_rules`); this page uses the POS catalogue for types and the word-level feature columns for the secondary filters — it does not need the i‘rab rule catalogue except inside the reused analysis action.

---

## 8. Risks and Edge Cases

| # | Risk / edge case | Impact | Mitigation |
| --- | --- | --- | --- |
| R1 | **`PRO` label not yet reseeded** into the live DB | ~327 prohibitive-`لا` head words mis-bucket under `اسم` instead of `حرف وأداة`; scholar-facing error | **Pre-implementation read-only check** of `quran_pos_tags` for `PRO`; if stale, run a `force` morphology reseed (or one-off `UPDATE`). Seed is already correct. |
| R2 | **Multi-STEM compound words** | Only the head (first STEM) type is shown; secondary STEMs invisible | Accepted per product decision; document as a known coverage gap, not a bug. Counts stay integrity-correct (one type/word). |
| R3 | **Missing root / lemma / stem** (`RootId`/`LemmaId`/`StemId` nullable) | الجذر/الأصل/الصيغة columns empty for some words | Render an empty/`—` fallback (existing convention); never block the row. Type/counts are unaffected (head_pos is NOT NULL). |
| R4 | **Unknown / null features** (`CaseFeature`/`VerbTense`/`VerbVoice` null) | Case filter must handle "no case"; tense/voice null for non-verbs | Provide `غير محدد` case option (`IS NULL`); only show tense/voice under the verb branch; never show a misleading label for null. |
| R5 | **Words with no meaningful nominal case** (particles, INL, some nouns) | Case filter irrelevant | Hide the nominal-case filter entirely for verbal / particle / INL types (per product rule). |
| R6 | **INL / disjoint letters** | Double-count risk: `INL` is `category='particle'` in the seed but is its own main type | Particle parent predicate **must** exclude `INL` (`head_pos <> 'INL'`). |
| R7 | **`ABR` (category `other`)** + any future non-noun/verb/particle code | Falls outside the 4 main buckets | `ABR` has 0 corpus occurrences today; map `other`/unmatched to a safe fallback (or omit). Validate the 4-bucket sum = total non-marker words as a guard. |
| R8 | **Performance** | Type-filtered list + winner-type/root subqueries over ~77k morphology rows | `head_pos`, `CaseFeature`, `VerbTense` are indexed; reuse `AsNoTracking` + paging + the existing caching layer; pre-aggregated counts avoid recomputation in the common (no-secondary-filter) case. |
| R9 | **Count semantics drift** when secondary filter active vs not | Inconsistent المواضع between filtered/unfiltered views | Decide §6.1 semantics up front (recommended: occurrence-scoped recount under an active feature filter) and assert it in tests. |
| R10 | **Existing API/page reuse risk** | Overloading Unique-Words endpoints could disturb its contract/cache and tests | Add a **separate** `word-types` read path; do not mutate the Unique-Words list signature. Reuse query *shapes*, not endpoints. |
| R11 | **Ayah-marker leakage** if querying `quran_word_morphology` directly | Inflated counts | Always join `quran_words` and filter `!IsAyahMarker`, matching every other reader. |

---

## 9. Recommendation

### 9.1 Verdict

**READY_WITH_NOTES.**

The data model fully supports the Word Types Explorer **today**: the main type tree, the nominal-case filter, and the verb tense/voice filters are all derivable word-level from indexed columns on `quran_word_morphology`; the four broad classes already exist as a reader rule; POS labels are corrected at the seed; ayah markers are excluded throughout; and the split-screen table + details UI is a direct reuse of four shipped explorers. No migration, importer, schema, or `ApiResponse` change is required. The "notes" are: confirm the `PRO` reseed (R1), decide row identity + secondary-filter count semantics (§6.2 / §6.1), and add one new read path (no Unique-Words endpoint changes).

### 9.2 Suggested v1 scope

- **Main types:** اسم / فعل / حرف وأداة / حروف مقطّعة (parent select + expand).
- **Children:** at minimum اسم علم / صفة / ضمير under اسم (the product's named set); optionally REL/DEM/ظرف and specific particle codes if cheap.
- **Secondary filters:** nominal case (`m.CaseFeature`) for nominal types; tense (`m.VerbTense`) + voice (`m.VerbVoice`) for verbs; none for particle/INL.
- **Table (Model A):** unique-word rows filtered by dominant `head_pos`; columns الكلمة / النوع / الجذر / المواضع / الآيات / السور; Simple↔Tashkeel toggle, default Simple.
- **Details card:** counts + ayah list (`ayah-matches-list`) + surah distribution (`surah-occurrences-list`) + full-analysis action reusing `GET api/mushaf/words/{location}/analysis`.
- **Backend:** new `api/words/word-types` controller + cached reader + EF reader (tree metadata, filtered word list, summary/ayahs/surahs) reusing winner-type/primary-root query shapes and the existing caching/logging layers.

### 9.3 Suggested deferred scope

- الأصل (lemma) / الصيغة (stem) columns (need new winner queries — low risk, but optional).
- Model B (distinct morphological-form rows) for scientifically-exact per-feature counts, if dominant-type approximation proves confusing.
- Segment/prefix/suffix POS as a separate future feature (explicitly out of scope per product decision).
- Specific-particle child tree beyond the obvious high-count codes.
- Surfacing secondary STEMs of multi-STEM compounds.

### 9.4 Exact next step after this report

1. **Run the read-only `PRO` check** (`SELECT code, arabic_label, category FROM quran_pos_tags WHERE code='PRO'`); if stale, schedule a `force` reseed before/with v1.
2. **Confirm the two product/data decisions:** row identity (recommend Model A) and secondary-filter count semantics (recommend occurrence-scoped recount).
3. **Brainstorm + spec the feature** (Spec Kit: `spec.md`, `plan.md`, contracts for the new `word-types` read model), reusing the Feature 015/016 read-abstraction and frontend-routing-state contracts as templates.
4. Only then implement, backend-first (new read path + tests), then the frontend explorer (table-first split view).

---

## Appendix — Evidence Index (files inspected, read-only)

- `Backend/domain/.../Quran/Words/Morphology/WordMorphology.cs` — word-level type + feature columns.
- `Backend/domain/.../Morphology/{PosTag,MorphologicalCase,VerbTense,VerbVoice,SegmentKind,WordMorphologySegment,QuranStem}.cs` — entity shapes/enums.
- `Backend/infrastructure/.../MorphologyImporting/PosTagSeed.cs` — 49 POS codes incl. corrected `PRO` = `حرف نهي`/`particle`; categories.
- `Backend/infrastructure/.../MorphologyImporting/MorphologyAssembler.cs` (head-pos resolution L166-177; `MapVerbTense`/`MapVerbVoice`/`MapCaseFeature` L612-654) — feature value strings.
- `Backend/infrastructure/.../MorphologyImporting/MorphologyValidationRunner.cs:104`, `MorphologySql.cs:116-119` — head-policy + `IsVerb ⇔ head_pos='V'` invariants.
- `Backend/infrastructure/.../Reads/Quran/Words/EfUniqueWordsReader.cs` — winner-type (L281-325), primary-root (L327-371), marker exclusion (L270-273), pre-aggregated counts (L373-400).
- `Backend/application/.../Quran/Words/Responses/UniqueWordListItemDto.cs` — existing list item shape (`PrimaryWordTypeCode`, broad label, counts, root).
- `Backend/application/.../Quran/MushafReader/Responses/WordAnalysisResponse.cs` — per-word full-analysis shape (reused for the details "analysis" action).
- `Backend/api/.../Controllers/Words/{LemmasController,StemsController}.cs` — existing word-level `typeCode` filter precedent.
- `Backend/infrastructure/.../Caching/Quran/Words/Lemmas/{CachedLemmasReader,LemmasCacheKeys}.cs` — caching pattern to mirror.
- `Frontend/.../features/words/{components,pages,state,data-access,models,utils}` — explorer reuse base (tables, details panels, ayah/surah lists, facades, url-sync, shared keyboard-nav utils).
- `docs/feature-017-lexical-explorers-polish/pos-tag-arabic-labels-review-report.md` — prior POS-label correctness review (source of the `PRO` finding now fixed).
- Prior in-repo DB inventory (via the 017 review): `quran_pos_tags` = 49 rows, `quran_word_morphology` = 77,432 rows, `quran_word_morphology_segments` = 128,219 rows.

> **Constraints honored:** report only. No source, seed, DB, migration, importer, API, frontend, or test changes were made. DB counts are taken from prior in-repo read-only inventory (not re-queried live this session); the single live check recommended before implementation is the `PRO` row in `quran_pos_tags`.
