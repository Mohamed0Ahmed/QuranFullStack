# Word Types Explorer (أنواع الكلمات) — Pre-Spec Planning Document

**Feature:** 019 — Word Types Explorer / أنواع الكلمات
**Document type:** PRE-SPEC PLAN (planning only — source document for the next `/speckit.specify`)
**Status:** No code, no Spec Kit run, no migration/importer/API/frontend/test/commit. Planning only.
**Primary input:** `docs/feature-019-word-types-explorer/word-types-explorer-capability-and-ui-report.md`
**Branch:** `018-segment-stems-and-stems-explorer`
**Date:** 2026-06-30

> This document encodes the **locked product decisions** for Feature 019 and the **recommended technical strategy** so that `/speckit.specify` can produce a clean spec. All product decisions below are locked; technical items marked _(recommendation)_ are proposals for the spec/plan phase to ratify.

---

## 1. Goal and Scope

### 1.1 Goal

A scholarly Words-hub explorer that lets an admin browse Quran words **by their main grammatical word type** (اسم / فعل / حرف وأداة / حروف مقطّعة) and drill into a selected word's occurrences, surahs, and analysis. The word type is the **main (head) word type only**, sourced from `quran_word_morphology.head_pos`.

### 1.2 In scope (v1)

- A **table-first** explorer page: top word-type filter picker, central words table, right selected-word details card.
- Four main types with parent-select + expandable child subtypes.
- Secondary filters: nominal **case** (for nominal types); verb **tense** + **voice** (for verbal type).
- Rows = **word + grammatical context** (§5.3); a word with multiple usages yields multiple rows (no mixed rows).
- Tree node counts = **distinct word-context rows**; table count columns = **occurrence-level** stats scoped to each row's exact context (occurrences / ayahs / surahs).
- Uthmani **with tashkeel** display; **no** Simple/Tashkeel toggle.
- Reuse of the existing lexical-explorer split-view + ayah/surah list components + per-word analysis endpoint.

### 1.3 Out of scope (v1) — see §12

Segment/prefix/suffix POS in counts; full per-segment i‘rab as a tree dimension; Simple/unvowelled display mode; secondary-STEM surfacing in multi-STEM compounds.

### 1.4 Sibling/template features

Feature 015 (Roots Explorer), 016 (Lemmas/Stems), 014 (Unique Words) — reuse read-abstraction, cache/logging, split-view, URL-state, and ayah/surah list patterns.

---

## 2. UI Contract (locked)

- **Page is table-first, not tree-first.** The type hierarchy is a filter picker only; the table + details card are the page.
- **Top section** = word-type filters (the picker).
- **Main area** = words table, styled like existing lexical explorer tables.
- **Right side** = selected-word details card. The card belongs to the **selected word**, not the selected type.
- The type hierarchy lives **only** inside the filter picker; it never becomes the main layout.
- Responsive behavior mirrors existing explorers (details collapses/stacks on narrow viewports; URL-restorable state).

---

## 3. Type Tree / Filter Contract (locked structure + recommended sources)

### 3.1 Main types and actions

Each main type exposes **two actions**: (a) clicking the **label** selects all words under that parent; (b) clicking the **expand arrow** opens the child subtype tree.

```
أنواع الكلمات
├─ اسم            select → all nominal words            expand → child subtypes
├─ فعل            select → all verbs                    expand → ماض / مضارع / أمر (tense)  [+ voice secondary]
├─ حرف وأداة      select → all particles/tools          expand → specific particle subtypes (optional v1)
└─ حروف مقطّعة    select → INL (leaf, no children)
```

### 3.2 Node → source rule (all word-level on `quran_word_morphology`, join `quran_pos_tags` for category)

| Node                          | Arabic label          | Predicate                                        | v1          |
| ----------------------------- | --------------------- | ------------------------------------------------ | ----------- |
| اسم (parent)                  | اسم                   | `pt.category = 'noun'`                           | ✅          |
| → اسم                         | اسم                   | `head_pos = 'N'`                                 | ✅          |
| → اسم علم                     | اسم علم               | `head_pos = 'PN'`                                | ✅          |
| → صفة                         | صفة                   | `head_pos = 'ADJ'`                               | ✅          |
| → ضمير                        | ضمير                  | `head_pos = 'PRON'`                              | ✅          |
| → اسم موصول / اسم إشارة / ظرف | from `quran_pos_tags` | `head_pos = 'REL'` / `'DEM'` / `IN ('T','LOC')`  | ✅ optional |
| فعل (parent)                  | فعل                   | `IsVerb = true` (≡ `head_pos = 'V'`)             | ✅          |
| حرف وأداة (parent)            | حرف وأداة             | `pt.category = 'particle' AND head_pos <> 'INL'` | ✅          |
| → specific particle           | from `quran_pos_tags` | `head_pos = '<code>'` (P / CONJ / NEG / …)       | ✅ optional |
| حروف مقطّعة (leaf)            | حروف مقطّعة           | `head_pos = 'INL'`                               | ✅          |

**Mandatory rule:** the particle parent **must exclude `INL`** (`head_pos <> 'INL'`), because `INL` is `category = 'particle'` in the seed but is promoted to its own main type — otherwise INL words double-count under both حرف وأداة and حروف مقطّعة.

**Type labels are API-sourced** from `quran_pos_tags.arabic_label`; the UI does not hardcode `اسم`/`فعل`/`حرف وأداة`. Only the four **main-type display strings** and the secondary-filter option strings are static UI labels.

### 3.3 Real types vs secondary filters

- **Real word types** (above) classify word occurrences by `head_pos`/category. The four main types partition matching non-marker word occurrences by head type, but tree counts count distinct **word-context rows** (§5.3), not distinct word texts. Therefore the four main-type tree counts are not required to sum to the number of unique displayed words. The integrity guard is: each selected node count must equal the table `totalCount` for the same filter/context.
- **Secondary filters** (§6) refine an already-typed set by a word-level head-STEM feature; they are **not** new node types and never cross type boundaries.

---

## 4. Count Semantics (locked)

Two distinct count families. **They must never be conflated in the UI or the API.**

### 4.1 Tree / filter node counts = WORD-CONTEXT (row) counts

- A node's count = **number of distinct word-context rows** (see §5.3) that match the node predicate — i.e. the number of rows the table would show for that node, **not** raw occurrences and **not** distinct word texts.
- Examples (locked): `فعل` count = distinct verb word-context rows under all verbs; `فعل أمر` count = distinct word-context rows under imperative verbs only; `اسم علم` count = distinct word-context rows under proper nouns only.
- These are **not** occurrence counts. A single displayed word that has two grammatical usages under the node counts as **two** rows.
- Recommended computation: `COUNT(DISTINCT <row grouping key>)` (§5.3) over `quran_word_morphology ⋈ quran_words` rows where `!IsAyahMarker` and the node predicate holds.
- Segment/prefix/suffix POS contribute **nothing** to these counts (the query reads only `quran_word_morphology`, one row per word occurrence).

### 4.2 Table count columns = OCCURRENCE-level stats for the exact row context

Each table row is a **word-context row** (§5.3). Its count columns are scoped to that exact row context (active type/subtype + active secondary feature), never to all usages of the displayed word:

- **المواضع** = count of occurrences belonging to that exact row context.
- **الآيات** = distinct ayahs (`verse_key`) containing those occurrences.
- **السور** = distinct surahs containing those occurrences.

(A homograph that is `اسم` in some ayahs and `صفة` in others produces **two** rows, each with its own المواضع/الآيات/السور counted only from its own context.)

### 4.3 Consistency guard

**Tree node count = table `totalCount`** for the same active node, because both count distinct **word-context rows** matching the node. The spec should assert this equality as an integrity test.

---

## 5. Word Identity, Display, and Row Grouping (LOCKED)

### 5.1 Locked product decisions

- **No Simple/Tashkeel toggle in v1.**
- Display words in **Uthmani with tashkeel**.
- "بدون تشكيل" is rejected for this page: the same unvowelled display form can carry different word-type usages, which would be misleading here.
- **A row is not the word text alone.** A row represents **`word + resolved grammatical context under the active filter`**. The same displayed word may appear in **more than one row** when it has different grammatical usages.
- Search may later accept unvowelled input internally, but **display stays tashkeel** in v1.

### 5.2 Identity table

Use the existing **`quran_words_unique_tashkeel`** identity (display = `text_uthmani`) as the word-text key. Do **not** introduce a new identity table. The Simple identity (`quran_words_unique_simple`) is not used for display in v1.

### 5.3 Row grouping — LOCKED: word + grammatical context (no mixed rows)

**Golden rule: NO mixed rows.** A row aggregates only occurrences that share the same displayed word **and** the same resolved grammatical context under the active filter. Never collapse two distinct matching usages of a word into one "dominant-subtype" row.

**Worked example (locked):** a word that occurs as both `اسم` and `صفة` under the active filter must render as **two rows** — one `اسم` row, one `صفة` row — each with its own occurrence/ayah/surah counts, its own details card, and its own ayah list.

**Grouping key by active filter context:**

| Active filter context                                                                    | Row grouping key                                                                                                                         |
| ---------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| Nominal **parent** (اسم) or particle **parent** (حرف وأداة) — multiple subtypes in scope | `UniqueTashkeelWordId + head_pos` (one row per distinct subtype usage)                                                                   |
| Verb **parent**, showing **tense** rows                                                  | `UniqueTashkeelWordId + VerbTense` (add `VerbVoice` to the key **only** when voice is part of the active context/filter)                 |
| Exact **child/leaf** filter (اسم علم / صفة / حرف جر / فعل أمر / INL …)                   | `UniqueTashkeelWordId` alone is sufficient — `head_pos`/feature is already pinned by the active filter                                   |
| Active **secondary filter** (e.g. nominal case مرفوع)                                    | scope the row's occurrences, counts, and details to that active feature value; include the feature in the key when it discriminates rows |

- Always apply `!IsAyahMarker`. Always restrict a row's occurrence set to occurrences matching the active node **and** active secondary predicate.
- The grouping key always includes whatever dimension is **not yet pinned** by the active filter but is in scope — so two in-scope usages of the same word never share a row.
- **النوع column** (§7) is therefore always exact for the row: it shows the row's own `head_pos` subtype (or, for the verb branch, the row's tense/voice context). There is **no** "dominant subtype" display — that pattern is explicitly rejected.
- Consequence: under a parent node, table row count ≥ distinct word texts; tree node count counts these same word-context rows (§4.3), so tree count = table `totalCount` still holds.

---

## 6. Data / Read-Model Strategy (recommendation)

### 6.1 Single source, word-level

All tree predicates, secondary filters, and table counts derive from **`quran_word_morphology`** (entity `WordMorphology`), joined to `quran_words` (marker filter + tashkeel identity) and `quran_pos_tags` (category/label). No segment table for tree/counts/filters. Confirmed columns (all indexed where filtered): `HeadPos` (FK, NOT NULL), `IsVerb`, `VerbTense`, `VerbVoice`, `CaseFeature`, plus `RootId`/`LemmaId`/`StemId`.

### 6.2 Secondary-filter feature values (locked sources)

| Filter       | Column        | Values                                                         |
| ------------ | ------------- | -------------------------------------------------------------- |
| nominal case | `CaseFeature` | `nominative` / `accusative` / `genitive` / `NULL` (→ غير محدد) |
| verb tense   | `VerbTense`   | `past` / `present` / `imperative`                              |
| verb voice   | `VerbVoice`   | `active` / `passive`                                           |

`CaseFeature`, `VerbTense`, `VerbVoice` are nullable and only populated from head-STEM features; verb tense/voice are only set when `IsVerb`.

### 6.3 Counts are computed, not read pre-aggregated

- The pre-aggregated `occurrences_count`/`ayahs_count`/`surahs_count` on `quran_words_unique_tashkeel` span **all** occurrences of a word and are **not** type-scoped, so they **cannot** be used directly for the table columns or tree counts (which are always filter-scoped). The read model must **recompute** occurrence-scoped counts via a grouped query over `quran_word_morphology ⋈ quran_words` with the active predicate.
- Tree node counts likewise computed as `COUNT(DISTINCT <row grouping key>)` (§5.3) per node predicate — i.e. counting word-context rows, not distinct word texts.
- Pages are small (e.g. 25 rows); `head_pos`/`CaseFeature`/`VerbTense` indexes + `AsNoTracking` + the existing caching layer keep this cheap.

### 6.4 Row enrichment (root / lemma / stem)

Per row, show the root / lemma / stem of the row's own context. Because a row is already pinned to one `head_pos` (and feature) context (§5.3), root/lemma/stem are usually constant; where they still vary, take the **dominant** value among that row's occurrences (winner pattern). Root winner query already exists (`LoadPrimaryRootsAsync`); **lemma and stem winner queries are new** (mirror the root one — low risk, optional v1; see §7).

### 6.5 Marker exclusion

Always filter `!IsAyahMarker` (join `quran_words`), consistent with every existing reader.

### 6.6 Caching / logging

Mirror `CachedLemmasReader` + `LemmasCacheKeys` shape, e.g. cache key `wordtypes:{type}:{childCode}:{case|tense|voice}:p{page}:s{pageSize}` and `wordtypes:tree`. Reuse the existing structured completion logging. No new infrastructure.

---

## 7. API Proposal (recommendation — shapes illustrative; conform to global `ApiResponse<T>`)

Add a **separate** read-only area `api/words/word-types` (new controller + cached reader + EF reader, layered like Lemmas/Stems). Do **not** overload the Unique-Words endpoints. No migration, importer, or `ApiResponse` change.

| #   | Endpoint                                                                                         | Purpose                                                                       | Counts                                        |
| --- | ------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------- | --------------------------------------------- |
| E1  | `GET api/words/word-types/tree`                                                                  | Main types + children + secondary-option metadata + **word counts** per node  | word counts                                   |
| E2  | `GET api/words/word-types/words?type=&childCode=&case=&tense=&voice=&page=&pageSize=&sort=`      | Paged **word-context** rows (§5.3)                                            | occurrence-scoped to each row's exact context |
| E3  | `GET api/words/word-types/words/{tashkeelWordId}?contextCode=&case=&tense=&voice=` (row context) | Details-card summary for **one word-context row**                             | occurrence-scoped to that row context         |
| E4  | `GET api/words/word-types/words/{tashkeelWordId}/ayahs?<row context>&page=&pageSize=`            | Ayah matches for that exact row context; highlights only matching occurrences | —                                             |
| E5  | `GET api/words/word-types/words/{tashkeelWordId}/surahs?<row context>`                           | Surah distribution for that exact row context (+ missing surahs)              | —                                             |
| —   | `GET api/mushaf/words/{location}/analysis`                                                       | **Reuse existing** for the details "التحليل" tab per chosen occurrence        | —                                             |

**Row identity in the API.** A table row is a **word-context row**, not a word. The response must carry an explicit, addressable row context so E3–E5 reproduce the _exact_ same row (no re-collapsing). Recommended: a `contextCode` capturing the unpinned dimension(s) of §5.3 (e.g. the row's `head_pos` under a parent node, or `tense`/`voice` under the verb branch), threaded through E3–E5 alongside `tashkeelWordId`.

Response sketch for E2 row:

```jsonc
{
  "tashkeelWordId": 1234,
  "contextCode": "PN", // the row's own resolved context (head_pos here); part of the row key
  "displayText": "…", // text_uthmani (Uthmani + tashkeel)
  "typeCode": "PN", // ALWAYS exact for this row — no dominant/mixed value
  "typeLabel": { "ar": "اسم علم" }, // from quran_pos_tags
  "broadLabel": { "ar": "اسم" },
  "caseOrFeature": "genitive", // the row's own case/tense/voice context (or null), never mixed
  "rootText": "…", // root for this row context
  "lemmaText": "…", // lemma for this row context (new winner query, optional v1)
  "stemText": "…", // stem for this row context (new winner query, optional v1)
  "occurrencesCount": 0,
  "ayahsCount": 0,
  "surahsCount": 0, // occurrence-scoped to THIS row context
}
```

The full row context (`tashkeelWordId` + `contextCode` + active `case`/`tense`/`voice`) is threaded through E3–E5 so the details card, surah list, and ayah highlighting reproduce the **exact row context**, never all usages of the displayed word.

---

## 8. Frontend Component / State Proposal (recommendation)

New feature area under `features/words` (or a dedicated `features/word-types`), mirroring the four existing explorers.

**Components**

- `word-types-explorer-page` — split-view shell (top filter, table left, details right), responsive + URL-restore (model on `stems-explorer-page` / `unique-words-page`).
- `word-type-filter` (the picker) — four main types with label-select + expand-arrow child tree; renders node **word counts**; shows the correct secondary-filter row based on the selected type. _(Only genuinely new component.)_
- `word-types-table` — model on `unique-words-table`; columns per §7/§4.2; reuse shared `explorer-table-*` utilities (keydown / focus-controller / column-nav / scroll / count-active / scrollbar-gutter-sync) **unchanged**.
- `word-type-details-panel` — model on `stem-details-panel` / `lemma-details-panel`; tabs/sections per §9; reuse `ayah-matches-list`, `highlighted-ayah`, `surah-occurrences-list`, `missing-surahs-list`, `word-count-chip`.

**State**

- `word-types-explorer.facade.ts`, `word-types-detail.facade.ts`, `word-types-cache.ts`, `word-types-url-sync.ts` — mirror the existing per-explorer facades/cache/url-sync.
- URL state encodes: `type`, `childCode`, `case` | `tense` | `voice`, the selected row's `tashkeelWordId` **and** its `contextCode` (so a deep link restores the exact word-context row, not just the word), page, sort, active details tab/column (reuse the deep-link column pattern already in the explorers).

**Labels**

- `word-types.labels.ts` for **static UI strings only** (main-type display strings, secondary-filter options مرفوع/منصوب/مجرور/غير محدد, ماض/مضارع/أمر, معلوم/مجهول, column headers). Use the TDZ-safe getter pattern. **POS type labels come from the API**, not this file.

**Mappers** — reuse `*-ayah-match.mapper` / `verse-key` patterns.

---

## 9. Details Card Behavior (locked)

The right-side card belongs to the **selected word-context row** (not to the displayed word in general) and shows:

- selected word (Uthmani + tashkeel);
- the row's exact type / subtype;
- the row's case **or** tense/voice when applicable (from head word features);
- root, lemma, stem of the row context;
- occurrences / ayahs / surahs **scoped to that exact row context**;
- tabs/sections:
  - **الآيات الخاصة بالكلمة** — ayah list, **highlighting only the occurrences belonging to that row context** (E4);
  - **السور** — surah distribution + missing surahs for that row context (E5);
  - **التحليل** — full analysis for a chosen occurrence, reusing `GET api/mushaf/words/{location}/analysis`.

The card and ayah list must use the **exact row context** (`tashkeelWordId` + `contextCode` + active case/tense/voice), never all usages of the displayed word. (e.g. for the `صفة` row of a word that is also used as `اسم`, show only its adjective occurrences; the `اسم` row is a separate card.)

---

## 10. I‘rab Behavior (locked)

- I‘rab is **secondary**, never a type-tree dimension.
- **Nominal case** = a secondary filter/display from the head word feature `CaseFeature`.
- **Verb tense/voice** = secondary filters/display from `VerbTense` / `VerbVoice`.
- **Full i‘rab** appears only as an ayah/word-level action in the details card (`التحليل` tab), reusing the existing per-word analysis (`WordAnalysisResponse` segments + simplified i‘rab) — not rebuilt.
- **Segment-level simple i‘rab must not affect tree counts** — structurally guaranteed (tree reads only `quran_word_morphology`).
- POS catalogue (`quran_pos_tags`) and i‘rab catalogue (`quran_i3rab_rules`) stay separate; this page uses POS for types + word-level feature columns for secondary filters.

---

## 11. Risks / Edge Cases

| #   | Risk                                                                          | Mitigation                                                                                                                                               |
| --- | ----------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| R1  | **`PRO` label not reseeded** into live DB → prohibitive لا mis-buckets as اسم | Pre-implementation gate G1 (§13); seed already corrected, may need `force` reseed or one-off `UPDATE`.                                                   |
| R2  | **Homograph across main types** (same tashkeel form noun + verb)              | Word-context grouping (§5.3) → one row per usage under each type, counts scoped to that context; never one mixed row.                                    |
| R3  | **Within-parent subtype mix** (e.g. اسم + صفة under اسم)                      | **LOCKED:** split into separate rows via `UniqueTashkeelWordId + head_pos` (§5.3). النوع is always the row's exact subtype; no dominant-subtype display. |
| R4  | **Multi-STEM compounds** — head only                                          | Accepted per product; document coverage gap; counts stay integrity-correct (one head type/word).                                                         |
| R5  | **Missing root/lemma/stem** (nullable)                                        | `—` fallback per existing convention; never block row (head_pos is NOT NULL).                                                                            |
| R6  | **Null features** (`CaseFeature`/`VerbTense`/`VerbVoice`)                     | `غير محدد` case option (`IS NULL`); show tense/voice only under verb; no misleading label for null.                                                      |
| R7  | **Particle/INL have no nominal case**                                         | Hide nominal-case filter for verbal/particle/INL types (locked §6 product rule).                                                                         |
| R8  | **INL double-count**                                                          | Particle parent excludes `INL` (§3.2 mandatory rule).                                                                                                    |
| R9  | **`ABR` / `other` / future code** outside 4 buckets                           | `ABR` has 0 occurrences today; map `other`/unmatched to safe fallback or omit; assert 4-bucket sum = total.                                              |
| R10 | **Count cost** (recompute occurrence-scoped, no pre-agg)                      | Indexed `head_pos`/`CaseFeature`/`VerbTense`; small page size; reuse caching; group subqueries.                                                          |
| R11 | **Reuse risk** — disturbing Unique-Words contract/cache                       | Separate `word-types` read path; reuse query _shapes_, not endpoints.                                                                                    |
| R12 | **Marker leakage** querying morphology directly                               | Always join `quran_words` + filter `!IsAyahMarker`.                                                                                                      |
| R13 | **Tree↔table count drift**                                                    | Both count distinct **word-context rows** (§5.3) so tree count = table totalCount; assert in tests (§4.3).                                               |
| R14 | **Row-context not addressable** → E3–E5 / deep links re-collapse usages       | Carry an explicit `contextCode` in the row + URL (§7/§8); E3–E5 require it to reproduce the exact row.                                                   |

---

## 12. Out of Scope / Deferred Scope

- Segment / prefix / suffix POS in any count (explicitly deferred; possible future feature).
- Simple/unvowelled **display** mode and the Simple/Tashkeel toggle.
- Internal unvowelled **search** input (may come later; display stays tashkeel).
- Full per-segment i‘rab as a tree dimension or filter.
- Surfacing secondary STEMs of multi-STEM compounds.
- الأصل (lemma) / الصيغة (stem) columns _may_ ship v1 via new winner queries, but are **deferrable** if scope must shrink (الجذر + counts already reuse-ready).

> Note: per-subtype row splitting (word + grammatical context) is **NOT deferred** — it is the locked v1 row model (§5.3).

---

## 13. Pre-Implementation Gates

**G1 — `PRO` POS label live-DB check (mandatory before implementation).**

```sql
SELECT code, arabic_label, category
FROM quran_pos_tags
WHERE code = 'PRO';
```

Expected: `PRO | حرف نهي | particle`. If the live row is stale (`ضمير منفصل` / `noun`), run a `force` morphology reseed (or a one-off `UPDATE`) before/with v1, otherwise prohibitive لا mis-buckets under اسم.

**G2 — Encode the locked row model** during `/speckit.specify` / `/speckit.plan` (the grouping is decided, not open):

1. Row = **word + resolved grammatical context** (§5.3); no mixed rows; per-context grouping key by active filter; addressable `contextCode`.
2. Counts: tree = distinct word-context rows; table = occurrence-scoped to each row's exact context; assert the §4.3 tree=table count-integrity test.

**G3 — Confirm v1 column set** (§7): الكلمة، النوع، الجذر، المواضع، الآيات، السور are reuse-ready; decide whether الأصل (lemma) + الصيغة (stem) ship v1 (new winner queries) or defer.

**G4 — Confirm optional child nodes** (REL / DEM / ظرف under اسم; specific particle codes under حرف وأداة) are in or out for v1.

---

## 14. Recommended Next Step

Run **`/speckit.specify`** using this document as the source. Suggested follow-on order:

1. `/speckit.specify` → `spec.md` (user stories: browse by type, drill into a word, secondary filters, details card).
2. `/speckit.plan` → technical plan + contracts (`word-types-read-model`, `word-types-api`, `word-types-frontend-routing-state`), reusing Feature 015/016 contracts as templates.
3. `/speckit.tasks` → dependency-ordered tasks, backend-first (read path + tests), then the table-first frontend explorer.
4. Execute G1 (the `PRO` live-DB check) before the first implementation task.

---

### Appendix — Locked decisions checklist (for the spec)

- [x] Table-first; type tree = filter picker only; details card belongs to the selected word.
- [x] Main types: اسم / فعل / حرف وأداة / حروف مقطّعة; label-select + expand-arrow children.
- [x] Row = word + grammatical context (§5.3); **no mixed rows**; same word may yield multiple rows (e.g. اسم + صفة → 2 rows); addressable `contextCode`.
- [x] Tree counts = distinct word-context rows; table counts = occurrence/ayah/surah scoped to each row's exact context.
- [x] No Simple/Tashkeel toggle; Uthmani-with-tashkeel display; tashkeel identity.
- [x] Main type source = `quran_word_morphology.head_pos` (first STEM by segment_number); segment POS out of scope.
- [x] Secondary filters: case (nominal) from `CaseFeature`; tense/voice (verbal) from `VerbTense`/`VerbVoice`; none for particle/INL.
- [x] Columns: الكلمة / النوع / الجذر / الصيغة / الأصل / المواضع / الآيات / السور (الأصل/الصيغة deferrable).
- [x] Details card: word, type/subtype, case|tense/voice, root/lemma/stem, occ/ayah/surah, tabs الآيات/السور/التحليل with filter-matched highlighting.
- [x] I‘rab secondary only; full i‘rab via existing analysis endpoint; segment i‘rab excluded from counts.
- [x] Pre-check gate G1 on `PRO` POS label.
