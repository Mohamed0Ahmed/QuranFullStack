# Phase 1 Data Model: Word Types Explorer

**Feature**: 019 — Word Types Explorer (أنواع الكلمات)
**Date**: 2026-06-30
**Nature**: **Read-only**. No new tables, columns, indexes, or migrations. This document describes (a) the existing entities/columns the feature reads, (b) the derived read-model concepts (tree node, word-context row), and (c) the exact grouping keys and count semantics the implementation must satisfy.

---

## 1. Existing entities consumed (read-only)

### 1.1 `WordMorphology` → table `quran_word_morphology` (PRIMARY SOURCE)

One row per **word occurrence**. All feature predicates, filters, and counts read this table.

| Column | Type | Used for | Notes |
|--------|------|----------|-------|
| `HeadPos` | string FK → `quran_pos_tags.code`, **NOT NULL** | main type + subtype classification | head policy = first STEM by segment_number; indexed |
| `IsVerb` | bool | verb parent predicate | `IsVerb ⇔ head_pos = 'V'`; indexed |
| `VerbTense` | string? (`past`/`present`/`imperative`) | verb subtype + secondary filter | populated only when `IsVerb`; indexed |
| `VerbVoice` | string? (`active`/`passive`) | verb secondary filter | populated only when `IsVerb` |
| `CaseFeature` | string? (`nominative`/`accusative`/`genitive`/null) | nominal secondary filter | null → غير محدد |
| `RootId` | int? FK → `quran_roots` | الجذر enrichment | nullable |
| `LemmaId` | int? FK → `quran_lemmas` | الأصل enrichment (deferrable) | nullable |
| `StemId` | int? FK → `quran_stems` | الصيغة enrichment (deferrable) | nullable |
| (word FK) | → `quran_words` | marker filter + tashkeel identity join | always join + filter `!IsAyahMarker` |

### 1.2 `quran_words` (join — identity + marker filter)

Provides `IsAyahMarker` (always exclude) and the link to the **tashkeel unique-word identity**.

### 1.3 `quran_words_unique_tashkeel` (display identity)

- Key: `UniqueTashkeelWordId`. Display text: `text_uthmani` (Uthmani + tashkeel).
- Pre-aggregated `occurrences_count`/`ayahs_count`/`surahs_count` exist but span **all** usages and are **NOT type-scoped** → **must not** be used for this feature's columns or tree counts (see §4).
- The Simple identity (`quran_words_unique_simple`) is **not** used in v1.

### 1.4 `quran_pos_tags` (catalogue — labels + category)

- `code`, `arabic_label`, `category` ∈ {`noun`,`verb`,`particle`,`other`}.
- Source of **all POS type labels** (API-sourced; UI does not hardcode them).
- Note the data gate: row `PRO` must read `حرف نهي / particle` (G1 / FR-044).
- POS codes outside the four v1 bucket predicates are excluded from Word Types Explorer counts and rows; they are never silently counted under another bucket.

### 1.5 Enrichment catalogues (read-only)

`quran_roots`, `quran_lemmas`, `quran_stems` — for الجذر / الأصل / الصيغة display text via winner resolution (§5).

---

## 2. Derived concept: Tree Node

A tree node is a **filter predicate over `quran_word_morphology`**, not a stored entity.

| Node | Arabic label (source) | Predicate (head-level) | v1 |
|------|----------------------|------------------------|----|
| اسم (parent) | static main-type string | `pt.category = 'noun'` | ✅ |
| → اسم | `pt.arabic_label` | `head_pos = 'N'` | ✅ |
| → اسم علم | `pt.arabic_label` | `head_pos = 'PN'` | ✅ |
| → صفة | `pt.arabic_label` | `head_pos = 'ADJ'` | ✅ |
| → ضمير | `pt.arabic_label` | `head_pos = 'PRON'` | ✅ |
| → other noun-category subtypes | `pt.arabic_label` | `pt.category = 'noun'` child codes (currently `REL`, `DEM`, `T`, `LOC`, `TIM`, `IMPN`, plus the core four above) | ✅ |
| فعل (parent) | static | `IsVerb = true` | ✅ |
| → ماض / مضارع / أمر | static | `VerbTense = 'past'/'present'/'imperative'` | ✅ |
| حرف وأداة (parent) | static | `pt.category = 'particle' AND head_pos <> 'INL'` | ✅ |
| → specific particle | `pt.arabic_label` | `head_pos = '<code>'` | deferred |
| حروف مقطّعة (leaf) | static | `head_pos = 'INL'` | ✅ |

**Mandatory invariant**: the particle parent predicate **must include `head_pos <> 'INL'`** (FR-009), otherwise INL words double-count under both حرف وأداة and حروف مقطّعة.

Every node predicate is additionally conjoined with `!IsAyahMarker`.

The E1 tree is intentionally **unscoped by secondary filters** in v1: it returns static type/child row counts for the selected dataset, not counts filtered by case/tense/voice.

---

## 3. Derived concept: Word-Context Row (THE row model — LOCKED)

> **Golden rule: NO mixed rows.** A row aggregates only occurrences that share the **same displayed word** AND the **same resolved grammatical context** under the active filter. The same displayed word may appear in **more than one row** (e.g. used as both اسم and صفة → two rows). "Dominant subtype" collapsing is **rejected**.

### 3.1 Row grouping key by active filter context

| Active filter context | Row grouping key | Reason |
|-----------------------|------------------|--------|
| Nominal **parent** (اسم) or particle **parent** (حرف وأداة) — multiple subtypes in scope | `UniqueTashkeelWordId + head_pos` | subtype not yet pinned → one row per distinct subtype usage |
| Verb **parent**, showing **tense** rows | `UniqueTashkeelWordId + VerbTense` (+ `VerbVoice` **only** when voice is in the active context) | tense not yet pinned |
| Exact **child/leaf** filter (اسم علم / صفة / فعل أمر / حرف جر / INL …) | `UniqueTashkeelWordId` alone | head_pos/feature already pinned by the active filter |
| Active **secondary filter** (e.g. case = مرفوع) | scope the row's occurrence set to that feature value; **include the feature in the key when it discriminates rows** | feature pins/segments the context |

**Rule of thumb**: the grouping key always includes whatever in-scope dimension is **not yet pinned** by the active filter, so two in-scope usages of the same word never share a row.

### 3.2 Row fields (E2 / `WordTypeRowDto`)

| Field | Meaning | Constraint |
|-------|---------|------------|
| `tashkeelWordId` | unique-tashkeel identity | display key |
| `contextCode` | the row's resolved unpinned dimension(s) — e.g. its `head_pos` under a parent, or `tense`/`voice` under verb | **part of the row key**; addressable (R6) |
| `displayText` | `text_uthmani` (Uthmani + tashkeel) | — |
| `typeCode` / `typeLabel` | the row's **exact** subtype code + Arabic label | **always exact** — never dominant/mixed |
| `broadLabel` | the row's main-type label (اسم/فعل/حرف وأداة/حروف مقطّعة) | — |
| `caseOrFeature` | the row's own case/tense/voice context, or null | never a union across usages |
| `rootText` / `lemmaText` / `stemText` | enrichment for the row context (root required where source data provides it; lemma/stem winners deferrable) | API returns null when unavailable/deferred; UI shows `—` |
| `occurrencesCount` / `ayahsCount` / `surahsCount` | occurrence-scoped counts (§4.2) | scoped to THIS row context only |

### 3.3 Worked example (locked)

A word occurring as both اسم and صفة under the اسم parent renders as **two rows**:

| displayText | contextCode | typeLabel | occurrences | ayahs | surahs |
|-------------|-------------|-----------|-------------|-------|--------|
| (word) | `N` | اسم | (its noun occurrences) | … | … |
| (word) | `ADJ` | صفة | (its adjective occurrences) | … | … |

Each row has its own details card and its own ayah list; neither row shows the other's occurrences.

---

## 4. Count semantics (TWO FAMILIES — must never be conflated)

### 4.1 Tree / filter node counts = **word-context ROW** counts

- Node count = `COUNT(DISTINCT <row grouping key>)` over `quran_word_morphology ⋈ quran_words` where `!IsAyahMarker` and the node predicate holds.
- **Not** occurrence counts and **not** distinct word-text counts. A word with two usages under the node counts as **two**.
- Therefore the four main-type node counts are **not required to sum to** the number of unique displayed words (FR-026 / pre-spec §3.3).

### 4.2 Table column counts = **occurrence-level** stats for the exact row context

For each word-context row, scoped to its exact context (active type/subtype + active secondary feature + `contextCode`):

- **المواضع** `occurrencesCount` = count of occurrences in that row context.
- **الآيات** `ayahsCount` = distinct `verse_key` among those occurrences.
- **السور** `surahsCount` = distinct surahs among those occurrences.

### 4.3 Integrity invariant (assert in tests)

**Tree node count == table `totalCount`** only for the same active main type/child node when no secondary feature filter is applied, because both count distinct word-context rows matching the unscoped node (FR-027 / pre-spec §4.3). When case/tense/voice is applied, the table `totalCount` is filtered and is not expected to equal the static E1 tree node count.

---

## 5. Enrichment winner resolution

For a row, الجذر/الأصل/الصيغة display text is the row context's value when available. Because the row is pinned to one head-POS (and feature) context, these are usually constant; where they vary across the row's occurrences, take the **dominant (winner)** value.

- الجذر: reuse existing root-winner query (`LoadPrimaryRootsAsync`) and return it when source data provides a root.
- الأصل (lemma) / الصيغة (stem): **new** winner queries mirroring the root one — low risk, **deferrable** for v1. If not implemented in v1, return null.
- Null or deferred root/lemma/stem → `—` fallback in the UI; null enrichment never blocks the row (head_pos is NOT NULL).

---

## 6. Validation rules (from requirements)

- **Marker exclusion**: every query filters `!IsAyahMarker` (FR-025; risk R12).
- **INL exclusion** from particle parent (FR-009; risk R8).
- **Out-of-bucket POS exclusion**: POS codes outside noun/verb/particle-without-INL/INL are excluded from v1 buckets, not reclassified.
- **No mixed rows** / no dominant subtype (FR-017/018; risk R3).
- **Row addressability**: `contextCode` required on E2 rows and threaded through E3–E5 (FR-018a/018b; risk R14).
- **Counts recomputed**, never read from pre-aggregated unique-word columns (FR-024; pre-spec §6.3).
- **Static tree counts**: E1 counts are not scoped by secondary filters; secondary filters narrow E2/E3/E4/E5 and active UI count chips only.
- **Secondary-filter visibility**: nominal case shown only for nominal types; verb tense/voice only for the verb type; none for particle/INL (FR-019–023; risk R7).
- **Display**: Uthmani + tashkeel only; no Simple/Tashkeel toggle (FR-029).

---

## 7. State transitions

None. This is a stateless read-only explorer; "state" is purely the URL-encoded view selection (type / childCode / case|tense|voice / selected row `tashkeelWordId` + `contextCode` / page / sort / active tab), described in `contracts/frontend-routing-state.md`.
