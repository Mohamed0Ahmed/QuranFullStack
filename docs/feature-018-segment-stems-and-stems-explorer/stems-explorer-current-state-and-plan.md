# Stems Explorer — Current State Inspection & Implementation Plan

**Feature:** 018 — Segment Stems & Stems Explorer
**Route:** `/dashboard/words/stems`
**Arabic name:** `الأصول الصرفية`
**Scope:** Stems Explorer only (backend + frontend). Report + plan only — no production edits, no commit.
**Date:** 2026-06-29
**Branch:** `018-segment-stems-and-stems-explorer`

---

## 0. Verdict

> **`READY_WITH_NOTES`**

The core semantic change — make Stems Explorer **segment-level** using
`quran_word_morphology_segments.kind = 'STEM' AND quran_word_morphology_segments.stem_id = selectedStemId`
— is fully feasible with **no migration, no importer, no packages**. The column,
indexes, entity, `DbSet`, and segment→`PosTag` POS relationship already exist.

It is `READY_WITH_NOTES` (not clean `READY_FOR_PLAN`) because of these
non‑blocking decisions, all resolved in the plan below:

- **N1.** Every occurrence read in `EfStemsReader` is currently word/head‑level
  (`quran_word_morphology.stem_id`). The conversion is mechanical but touches
  **all** stem detail/summary methods, so catalogue counts will shift for stems
  whose words carry a non‑head (secondary) STEM segment. **This shift is intended.**
- **N2.** `type-distribution-list` is a **shared** component (Lemmas/Roots reuse
  it). Adding ayah‑filter interactivity + a stems title must be done via
  optional, default‑off inputs (or a stems‑local wrapper) so Lemmas/Roots are
  not regressed.
- **N3.** Backend tests need the **committed seed slice** extended: STEM segments
  are currently seeded **without** `stem_id` (the column is absent from the seed
  `INSERT`). Seed extension is test‑support, not production/import code.
- **N4.** `الصيغة المعجمية` legitimately appears in the **related‑lemmas tab** and
  must not be blanket‑removed; only the **selected stem identity** must remain
  `الأصل الصرفي` (already the case).
- **N5.** Visible `السائد` does **not** appear anywhere; a spec already guards it.
  Req 6 "remove visible السائد" is already satisfied — keep the guard.

---

## 1. Backend stem membership

### 1.1 Methods that still use `quran_word_morphology.stem_id` (word/head‑level) — ALL of them

File: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs`

| Method | Membership predicate today | Output that must become segment‑driven |
|---|---|---|
| `GetStemAyahMatchesAsync` | `_db.WordMorphologies.Where(m => m.StemId == id)` joined to `QuranWords` | matched ayah set + `MatchedQuranWordIds` highlight set |
| `GetStemMentionedSurahsAsync` | `from m in WordMorphologies … where m.StemId == id group w by w.SurahNumber` | per‑surah `occurrencesInSurah` |
| `GetStemMissingSurahsAsync` | `where m.StemId == id select w.SurahNumber` (distinct) | mentioned vs missing surah split |
| `GetStemLemmasAsync` | `where m.StemId == id && m.LemmaId != null` | related‑lemmas list |
| `GetStemWordsPageAsync` → `LoadStemWordRowsAsync` | `where m.StemId == id` (simple + tashkeel) | words tab list + occurrence counts |

File: `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.Summary.cs`

| Method | Membership today | Output |
|---|---|---|
| `LoadWholeSummaryAsync` — raw SQL `agg` CTE | `FROM quran_word_morphology m JOIN quran_words w … WHERE m.stem_id IS NOT NULL GROUP BY m.stem_id` | catalogue + summary counts: `occurrences/ayahs/surahs/simple/tashkeel`, first verse |
| `LoadWholeSummaryAsync` — `occurrenceRows` LINQ | `from m in WordMorphologies join t in PosTags on m.HeadPos … where m.StemId != null` | **type distribution (HEAD POS)**, dominant lemma, dominant root |

`MaterializeTypeDistribution`, `BuildDominantLemma`, `BuildDominantRoot` (in `EfStemsReader.cs`)
consume `StemTypeOccurrenceRow`, which is built from **`m.HeadPos`** — i.e. word/head POS,
not segment POS. These feed `StemsListDerivation.DominantType` / `OtherTypesCount`.

### 1.2 Methods already using segment‑level data

**None.** No Stems reader touches `_db.WordMorphologySegments`. (Confirmed by the
committed test seed comment: *"Stems: `quran_word_morphology m (m.stem_id = X)`"*
vs *"Lemmas: `quran_word_morphology_segments s`"* — Lemmas already went
segment‑level, Stems did not.)

### 1.3 Segment infrastructure available (no migration needed)

- Entity `WordMorphologySegment` (`Domain/.../Morphology/WordMorphologySegment.cs`):
  `Kind` (string, stored `'STEM'`), `Pos` (string), `StemId` (`int?`), `LemmaId`,
  `RootId`, `QuranWordId`.
- `DbSet<WordMorphologySegment> WordMorphologySegments` — `QuranDashboardDbContext.cs:28`.
- Config `WordMorphologySegmentConfiguration.cs`: indexes on `StemId`
  (`IX_quran_word_morphology_segments_stem_id`), on `Pos`, and a filtered
  `kind = 'STEM'` index; **`Pos` is an FK to `PosTag`** → segment POS Arabic/English
  labels are joinable exactly like `HeadPos` is today.

**Canonical membership predicate** (LINQ):
`from s in _db.WordMorphologySegments where s.Kind == "STEM" && s.StemId == id join w in _db.QuranWords on s.QuranWordId equals w.Id`.

---

## 2. Catalogue / list (`GetStemsPageAsync` → `LoadWholeSummaryAsync` → `StemsListDerivation`)

- **Counts are word/head stem**, from the raw‑SQL `agg` CTE keyed on
  `quran_word_morphology.stem_id`. Must become STEM‑segment driven.
- **Multi‑STEM secondary stems are NOT included.** A word contributes only via its
  single head `m.stem_id`; its secondary STEM segment's `stem_id` is invisible to
  the catalogue. The verified 483 secondary STEM segments (479 mapped) are absent.
- **Search/sort/paging are safe and reusable.** `StemsListDerivation` is pure
  in‑memory (normalized Arabic contains‑search, deterministic sort, safe paging
  via `ReadPaging.CalculateSafeSkip`). Sort `mushaf-order` uses
  `quran_stems.first_word_order_in_mushaf` (precomputed stem‑identity column) — keep
  as identity metadata; do **not** re‑derive it from segments (see N1/§7 decision).

---

## 3. Details / summary (`GetStemSummaryAsync` → same `LoadWholeSummaryAsync`)

All of `occurrencesCount`, `ayahsCount`, `surahsCount`, `simpleWordsCount`,
`tashkeelWordsCount` are **word‑level** (the `agg` CTE).

Target segment‑driven definitions (matching STEM segments `s.Kind='STEM' AND s.StemId=id`):

- `occurrencesCount` = `COUNT(*)` of matching STEM segments (**not** distinct words).
- `ayahsCount` = `COUNT(DISTINCT w.ayah_id)` of those segments' words.
- `surahsCount` = `COUNT(DISTINCT w.surah_number)`.
- `simpleWordsCount` = `COUNT(DISTINCT w.unique_simple_word_id)`.
- `tashkeelWordsCount` = `COUNT(DISTINCT w.unique_tashkeel_word_id)`.

Yes — occurrences must count matching **STEM segments**, so a word with a secondary
STEM segment for this stem counts, and a word whose head stem differs is still
captured through its segment.

---

## 4. Words tab — product/UI decisions

**Frontend files:** `components/stem-words-list/` + `pages/stems-explorer-page/stems-explorer-page.component.html` + `components/stem-details-panel/stem-details-panel.component.scss`.

- **Type must NOT appear in words tab.** Today `<qd-type-distribution-list>` is rendered
  at the top of `#stemsPanelContent` for **every** view (words/ayahs/surahs/lemmas)
  — see `stems-explorer-page.component.html` (`@if (panelState().summary …)` block
  above the `@if (activeView()==='words')`). So it currently shows in the words tab. Fix:
  render type distribution **only** when `activeView() === 'ayahs'`.
- **Internal scroll is missing (UI bug — confirmed).** Root cause:
  - `stem-details-panel.component.scss`: `.explorer-detail-panel__body` (the panel
    surface) is `flex:1 1 auto; min-block-size:0; overflow:hidden`.
  - `stem-words-list.component.scss`: `.stem-words-list__viewport` has **no**
    `block-size` and **no** `overflow` — so the list grows unbounded and is clipped
    by the surface's `overflow:hidden`; nothing scrolls.
  - Working reference (ayahs): `ayah-matches-list.component.scss`
    `.ayah-matches-list__viewport { block-size: min(58vh, 30rem); overflow: auto; scrollbar-gutter: stable; }`.
  - Fix: give `.stem-words-list__viewport` the same bounded scroll
    (`block-size: min(58vh, 30rem); overflow: auto; scrollbar-gutter: stable;`) and let
    `:host` / `.stem-words-list` be column‑flex so header + viewport + pagination stack
    with the viewport scrolling.
- **Keep only the `بدون تشكيل` / `بالتشكيل` switch.** Already correct: the words sub‑tabs
  in `stems-explorer-page.component.html` render `wordViewOptions = ['simple','tashkeel']`
  → labels `بدون تشكيل` / `بالتشكيل` (`WORDS_SHARED_WORD_VIEWS`). No other control to remove
  besides moving the type distribution out (above).

---

## 5. Ayahs tab — product/UI decisions

**Today:** no type filter exists. `GetStemAyahsQuery(Id, Page, PageSize)` and
`StemsApi.getStemAyahMatches(id, page, pageSize)` carry no type. The type
distribution is shown but is **non‑interactive** (`type-distribution-list` rows are
display‑only; `dominant=index===0`).

**Target behaviour:**

- Type appears **only** in the Ayahs tab and is used to **filter** ayahs.
- Filter predicate (backend): matching segment
  `s.Kind = 'STEM' AND s.StemId = selectedStemId AND s.Pos = selectedTypeCode`.
- **> 1 type:** show `عرض الكل` + every type option; clicking a type filters; `عرض الكل`
  clears the filter (all member ayahs).
- **Exactly 1 type:** keep the single type visible as **information**; do **not** show
  `عرض الكل`; do not present it as a multi‑choice filter.
- **Invalid/unknown type in URL/state:** handle safely. `parseStemsQueryParams`
  (`stems-url-sync.ts`) must validate the `type` param against the summary's type
  codes; an unknown code falls back to "all" (no filter) without breaking the page —
  mirror the existing `isStemView`/`isStemWordView` guard pattern.

State plumbing required (see §B plan): new `type` query key, `typeFilter` in
`StemsPanelState`/`ParsedStemsQuery`, threading through facade → loader → cache key →
`StemsApi` → backend query/handler/reader.

---

## 6. Type distribution

- **Must use matching STEM segment POS** (`s.Pos`), not `m.HeadPos`. Today it is
  built from `m.HeadPos` in `LoadWholeSummaryAsync`'s `occurrenceRows` join.
- **Wording must distinguish Stems from Lemmas.** Today the section title
  (`STEMS_TYPE_DISTRIBUTION_LABEL = 'توزيع الأنواع'`) is used **only as `aria-label`**,
  not visible. Add a visible title `توزيع أنواع الكلمات المرتبطة بالأصل` for the Stems/ayahs
  context (via optional input on the shared component — see N2).
- **Visible `السائد`:** does not appear. The "dominant" row is conveyed via
  `qd-is-selected` + `aria-current` + `data-testid="type-distribution-dominant"` only.
  `type-distribution-list.component.spec.ts` already asserts
  `textContent … not.toContain('السائد')`. Keep subtle selected styling/aria for the
  **active filter** row; keep the guard.

---

## 7. Surahs tab / mentioned & missing surahs

- **Mentioned counts are word‑level** (`GetStemMentionedSurahsAsync`:
  `group w by w.SurahNumber … g.Count()` over `m.StemId == id`).
- **Missing is word‑level** (`GetStemMissingSurahsAsync`: surahs not in the
  word‑level mentioned set).
- Target: `occurrencesInSurah` = `COUNT` of matching **STEM segments** per surah;
  mentioned set = distinct surahs of matching STEM segments; **missing = 114 surahs −
  segment‑level mentioned set**.
- **Decision (N1):** `quran_stems.first_word_order_in_mushaf` stays as stem‑identity
  metadata for the catalogue sort (it is stem identity, not occurrence membership).
  All **occurrence/membership** outputs go segment‑level.

---

## 8. Multi‑STEM correctness

- A word may belong to **more than one stem** when it has multiple STEM segments
  (e.g. compound forms). Segment‑level membership makes such a word appear under
  **each** of its STEM segments' stems.
- Secondary STEMs with mapped `segment.stem_id` (479 of 483) **must** appear in their
  stem's results — they only become visible after the conversion.
- The **4** unresolved secondary exceptions with `segment.stem_id = null` **must not**
  be invented or force‑mapped in read code. `s.StemId == id` with a non‑null `id`
  already excludes `NULL` rows — no `COALESCE`, no fallback to `m.stem_id`.
- Ayah markers remain excluded: STEM segments only attach to real morphology words;
  markers have no STEM segment, so they never enter the match set. Highlight set
  (`MatchedQuranWordIds`) = `quran_word_id` of matching STEM segments.

---

## 9. Frontend wording & UI polish

- **Selected stem** already uses `الأصل الصرفي` (panel label/title/surface labels in
  `stems.labels.ts`). No `الصيغة المعجمية` is applied to the stem identity. ✔
- `الصيغة المعجمية` occurrences in Stems context are **legitimate** (related‑lemmas tab:
  `STEMS_LEMMA_LINK_PREFIX`, `stem-lemmas-list`) — keep (N4).
- **Short headers** (`stems-table.component.html` via `STEMS_COLUMN_HEADERS`):
  - `بدون تشكيل` / `بالتشكيل` — already short ✔.
  - dominant‑lemma column header is `الصيغة المعجمية` (`WORDS_SHARED_HEADERS.lemma`) →
    change to **`الصيغ`** in the Stems table.
  - dominant‑root column header is `الجذر` → change to **`الجذور`** in the Stems table.
  - (Both are Stems‑scoped via `STEMS_COLUMN_HEADERS`; `stems-table.component.spec.ts`
    header assertions must be updated accordingly.)
- No redesign — focused semantic + layout fixes only.

---

# IMPLEMENTATION PLAN (strict)

Membership rule everywhere occurrence/membership is computed:
`s.Kind == "STEM" && s.StemId == id` over `_db.WordMorphologySegments` joined to
`_db.QuranWords` (and to `_db.PosTags` on `s.Pos` for type labels).

## A. Backend phases

### A1 — Contract & wiring (type filter)
1. `IStemsReader.GetStemAyahMatchesAsync` — add optional `string? pos` (type code) param.
2. `GetStemAyahsQuery` — add `string? Type`.
3. `GetStemAyahsHandler` — pass `query.Type` through; treat null/empty as "all";
   an unknown code is allowed (yields empty page, not an error) — keep existing
   `InvalidId`/`InvalidPaging` outcomes unchanged.
4. `StemsController.GetAyahs` — add `[FromQuery] string? type`; forward to the query.
5. `StemsApi.getStemAyahMatches` (frontend) — see B.

### A2 — Segment‑level membership conversion (`EfStemsReader.cs`)
Replace the `from m in _db.WordMorphologies where m.StemId == id` source with the STEM
segment source in:
- `GetStemAyahMatchesAsync` — matched ayah ids + per‑ayah `MatchedQuranWordIds` from
  matching STEM segments' `QuranWordId`.
- `GetStemMentionedSurahsAsync` — group matching STEM segments by `w.SurahNumber`,
  `occurrencesInSurah = COUNT(segments)`.
- `GetStemMissingSurahsAsync` — mentioned = distinct surahs of matching STEM segments;
  missing = all surahs not in that set.
- `GetStemLemmasAsync` — related lemmas from matching STEM segments
  (`s.LemmaId` where non‑null), keep `MorphologyRelatedItemsOrdering.OrderStemLemmas`.
- `LoadStemWordRowsAsync` (both simple/tashkeel) — drive from matching STEM segments'
  words; group by `unique_simple/tashkeel_word_id`.

### A3 — Type filter conversion (`GetStemAyahMatchesAsync`)
When `pos` is non‑null/non‑empty, add `&& s.Pos == pos` to the matching‑segment
predicate (count, page, and highlight queries all filter identically).

### A4 — Summary / counts conversion (`EfStemsReader.Summary.cs`)
Rewrite the `agg` CTE to aggregate over `quran_word_morphology_segments` filtered to
`kind = 'STEM' AND stem_id IS NOT NULL`, joined to `quran_words`:
- `occurrences_count = COUNT(*)`,
- `ayahs_count = COUNT(DISTINCT w.ayah_id)`, `surahs_count = COUNT(DISTINCT w.surah_number)`,
- `simple_words_count = COUNT(DISTINCT w.unique_simple_word_id)`,
  `tashkeel_words_count = COUNT(DISTINCT w.unique_tashkeel_word_id)`,
- first‑occurrence ordering keys from those segments' words.
Rewrite `occurrenceRows` to join `_db.WordMorphologySegments` (STEM, non‑null `StemId`)
→ `QuranWords` → `PosTags` on **`s.Pos`** → lemmas/roots on **`s.LemmaId`/`s.RootId`**.
`MaterializeTypeDistribution` then groups by **segment POS**; `BuildDominantLemma` /
`BuildDominantRoot` rank by segment lemma/root. `StemsListDerivation` unchanged (pure
derivation over `StemSummaryRow`). Keep `first_word_order_in_mushaf` from
`quran_stems` (identity metadata, N1/§7).

> Out of scope: do **not** change `quran_word_morphology.stem_id` itself or any other
> reader (Lemmas/Roots). Word/head `stem_id` may remain referenced only as explicit
> word/head metadata, never as the Stems occurrence source.

## B. Frontend phases

### B1 — Words tab internal scroll fix (UI bug)
- `stem-words-list.component.scss`: add to `.stem-words-list__viewport`
  `block-size: min(58vh, 30rem); overflow: auto; scrollbar-gutter: stable;` and make
  `:host` / `.stem-words-list` column‑flex with `min-block-size:0` so the viewport is the
  single scroll region (parity with `ayah-matches-list`). No template/markup change.

### B2 — Type out of words tab; into ayahs tab as filter
- `stems-explorer-page.component.html`: render `<qd-type-distribution-list>` **only**
  inside the `activeView() === 'ayahs'` branch (remove the always‑on block). Wire its
  selection to a `type` filter.
- `stems-explorer-page.component.ts`: add `onTypeFilterChange(code|null)` →
  `updateQueryParams(buildStemsQueryParams({ type, detailPage: 1 }))`; expose current
  `typeFilter` from `panelState`.

### B3 — Single‑type vs multi‑type behaviour
- Derive type options from `panelState().summary?.typeDistribution`.
- `> 1` type → render `عرض الكل` + all options; selected option drives the filter.
- exactly `1` type → render it as info only, **no** `عرض الكل`, no multi‑choice affordance.
- Make `type-distribution-list` interactive via **optional, default‑off** inputs
  (e.g. `selectable`, `selectedCode`, `showAllOption`, `title`) + a `typeSelected`
  output, so Lemmas/Roots keep the current read‑only rendering (N2). Alternatively a
  thin stems‑local wrapper — prefer optional inputs to avoid duplication.

### B4 — State / data‑flow plumbing for `type`
- `stems.models.ts`: add `type` to `STEMS_QUERY_KEYS` + `STEMS_SELECTION_QUERY_KEYS`;
  add `typeFilter: string | null` to `StemsPanelState` and `ParsedStemsQuery`.
- `stems-url-sync.ts`: parse `type` only when `view === 'ayahs'`; validate against the
  summary's type codes; unknown → null (all). Build `type` in `buildStemsQueryParams`.
- `stems-detail.facade.ts` / `stems-detail-panel.updates.ts`: track `typeFilter`,
  refetch ayahs on change, reset to page 1.
- `stems-detail-view.loader.ts` + `stems-cache.ts`: thread `typeFilter` into the ayahs
  load and include it in `StemsCacheKeys.ayahs(...)`.
- `stems.api.ts`: `getStemAyahMatches(id, page, pageSize, type?)` → set `type` param when present.

### B5 — Wording cleanup
- `stems.labels.ts`: add `عرض الكل` label; set the visible type‑distribution title
  `توزيع أنواع الكلمات المرتبطة بالأصل`; change Stems table headers
  `STEMS_COLUMN_HEADERS.lemma → 'الصيغ'`, `STEMS_COLUMN_HEADERS.root → 'الجذور'`.
- Keep `السائد` absent (no change); keep related‑lemmas `الصيغة المعجمية` (N4).

## C. Tests

### C0 — Test support (seed extension; not production)
Extend `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/morphology-explorers-seed.sql`:
add `stem_id` to STEM segment rows and new fixtures covering —
- a **primary** STEM segment whose `stem_id` = the word's head `m.stem_id`;
- a **secondary** STEM segment on a word whose head stem differs, mapped to a
  different `stem_id` (multi‑STEM word);
- a STEM segment with `stem_id = NULL` (the exception);
- segment `pos` distinct from `m.head_pos` (to prove type uses segment POS);
- reuse the existing ayah‑marker exclusion rows (8201/8202).

### C1 — Backend tests (`WordsMorphologyExplorers/*`, Testcontainers fixture)
Prove:
- secondary STEM segment word **is included** in its stem's ayahs/words/surahs/summary
  (`StemsAyahsReadTests`, `StemsWordsReadTests`, `StemsSurahsReadTests`, `StemsListReadTests`);
- word/head `m.stem_id` is **not** the membership source (a word whose head stem ≠
  selected stem still matches via its STEM segment; a word whose head stem = selected
  stem but has no STEM segment for it does not leak);
- `segment.stem_id = NULL` exception is **excluded**;
- type distribution uses matching **STEM segment POS** (segment POS ≠ head POS asserted);
- ayah type filter (`pos`) returns only ayahs whose matching segment has that POS;
- ayah markers remain **excluded** from match/highlight sets.

### C2 — Frontend tests (`stems-explorer-page.component.spec.ts`, `stem-words-list.component.spec.ts`, `type-distribution-list.component.spec.ts`)
Prove:
- type **not** shown in words tab; type **is** shown in ayahs tab;
- `عرض الكل` appears only when `> 1` type; single type renders without `عرض الكل`;
- selecting a type calls the ayahs API / sets `type` query param and filters the list;
- words tab list has an internal scroll container (assert the scroll class/style on
  `.stem-words-list__viewport`);
- Stems labels use `الأصل الصرفي` (selected stem) and the new short headers `الصيغ`/`الجذور`;
- visible `السائد` remains absent (keep existing guard).

## D. Verification commands

Backend (focused; requires Docker for Testcontainers):
```bash
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --filter "FullyQualifiedName~WordsMorphologyExplorers"
```
Backend build (if signatures change):
```bash
dotnet build Backend/Backend.sln
```
Frontend (focused; **keep the VITEST_MAX_FORKS cap** — uncapped `npm test` OOMs/freezes the machine):
```bash
cd Frontend/quran-dashboard-ui
VITEST_MAX_FORKS=2 npm test -- stems-explorer-page stem-words-list type-distribution-list
```
Frontend build (if touched scope requires it):
```bash
cd Frontend/quran-dashboard-ui && npm run build
```

## E. Explicit out‑of‑scope

- Lemmas Explorer and Roots Explorer behaviour/data (only **shared‑component
  backward compatibility** for `type-distribution-list` is in scope).
- Any database **migration** (the `stem_id` column already exists).
- Any **importer** / DataPipeline / segment‑stem curation change.
- New **packages** / dependencies.
- **Design tokens** / visual redesign.
- **Quran text** mutation of any kind.
- Changing `quran_word_morphology.stem_id` semantics or the **4 null‑exception**
  secondary stems (never invented or force‑mapped in read code).
- `quran_stems.first_word_order_in_mushaf` recomputation (kept as identity metadata).
