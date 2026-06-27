# Lemmas Explorer — Focused Adjustments Inspection Report

Feature: 017 Lexical Explorers Polish
Page under review: **الصيغ المعجمية** (Lemmas Explorer), route `/dashboard/words/lemmas`
Type: inspection + report only (no production code, no tests, no commits modified)
Branch: `017-lexical-explorers-polish`

---

## 1. Verdict

**PLAN READY**

All five issue groups have a concrete, evidence-backed cause and a minimal change plan.
Issue 4 is a genuine backend projection bug (not a guess) and the correct fix is already
proven by two sibling readers (`EfUniqueWordsReader`, `EfRootsReader`). No blocking
clarification is required. One non-blocking note: the DTO field name `DisplayTextUthmani`
becomes slightly misleading after the fix; renaming it is a *future response-cleanup*
item and is intentionally out of scope here.

---

## 2. Scope inspected

### Backend files

- `api/QuranDashboard.Api/Controllers/Words/LemmasController.cs` — all 7 endpoints.
- `application/.../Quran/Words/Lemmas/ILemmasReader.cs`, `LemmaWordKind.cs`, `LemmaSort.cs`.
- `application/.../Quran/Words/Lemmas/Responses/` — `LemmaListItemDto`, `LemmaSummaryDto`,
  `LemmaWordItemDto`, `LemmaAyahMatchDto`, `LemmaSurahsResponse`, `LemmaMissingSurahsResponse`,
  `LemmaStemsResponse`.
- `application/.../Quran/Words/Lemmas/Queries/GetLemmaWords/GetLemmaWordsHandler.cs` (+ siblings:
  GetLemmasPage, GetLemmaSummary, GetLemmaAyahs, GetLemmaMentionedSurahs, GetLemmaMissingSurahs,
  GetLemmaStems).
- `infrastructure/.../Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs` (+ `LemmasListDerivation`,
  `LemmasSummaryRow`).
- Cross-reference readers (proof of correct pattern): `EfRootsReader.cs` (lines 44–113),
  `EfUniqueWordsReader.cs` (lines 378, 407), `EfStemsReader.cs` (lines 426, 438).
- Domain: `Quran/Words/QuranWord.cs`, `Quran/Words/Display/UniqueSimpleWord.cs`,
  `UniqueTashkeelWord.cs`.

### Frontend files

- `features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.html` (+ `.ts`).
- `features/words/components/lemmas-table/lemmas-table.component.html` (+ `.ts`, responsive scss).
- `features/words/components/lemma-details-panel/lemma-details-panel.component.html` (+ `.ts`).
- `features/words/components/lemma-words-list/lemma-words-list.component.html` (+ `.ts`).
- `features/words/components/type-distribution-list/type-distribution-list.component.ts` (+ `.html`).
- `features/words/components/lemma-stems-list/…` (referenced).
- `features/words/data-access/lemmas.api.ts`.
- `features/words/models/lemmas.models.ts`, `models/lemmas.labels.ts`.
- Cross-reference labels: `models/roots.labels.ts`, `models/stems.labels.ts`, `models/unique-words.labels.ts`.

### Tests / fixtures inspected

- Backend: `tests/.../WordsMorphologyExplorers/LemmasWordsReadTests.cs`,
  `morphology-explorers-seed.sql` (word rows 3001–3011, unique-tashkeel 31001/31002,
  unique-simple 32001/32002, morphology rows for lemma 500).
- Frontend: `lemmas-table.component.spec.ts`, `lemma-words-list.component.spec.ts`,
  `type-distribution-list.component.spec.ts`.

---

## 3. Current behavior summary

- **Headers (Issue 1):** The main Lemmas table renders the *long* labels
  `كلمات بدون تشكيل` / `كلمات بالتشكيل` as visible column headers (from
  `LEMMAS_COLUMN_HEADERS`). The sibling Roots table already uses the *short* form
  `بدون تشكيل` / `بالتشكيل` for headers and reserves the long form for the count/aria
  label. Lemmas (and Stems) have not adopted the short header form yet.
- **النوع column (Issue 2):** The main Lemmas table has a dedicated `النوع` column that
  renders `row.dominantType.arabicLabel` plus a `+N` other-types badge.
- **السائد (Issue 3):** The shared `type-distribution-list` component shows a hardcoded
  `السائد` string twice — as a header column and as a per-row badge on the dominant type.
  This list is embedded in the Lemma details panel.
- **Simple/tashkeel words (Issue 4):** The Lemma details → Words tab sub-tabs (`بدون تشكيل`
  / `بالتشكيل`) both render `displayTextUthmani`. The backend `EfLemmasReader` projects
  `w.TextUthmani` (vowelled) for **both** the simple and the tashkeel kind; only the grouping
  identity (`UniqueSimpleWordId` vs `UniqueTashkeelWordId`) differs. Therefore the
  `بدون تشكيل` view shows vowelled glyphs.
- **Responses (Issue 5):** 7 endpoints back the page; inventory in §8.

---

## 4. Issue 1 — Short unified headers

### Current labels found
`models/lemmas.labels.ts`:

```ts
export const LEMMAS_COLUMN_HEADERS = {
  …
  simpleWords: 'كلمات بدون تشكيل',   // long — VISIBLE header
  tashkeelWords: 'كلمات بالتشكيل',   // long — VISIBLE header
  …
};
export const LEMMAS_COLUMN_COUNT_LABELS = {
  …
  simpleWords: 'كلمات بدون تشكيل',   // long — used for chip/mobile aria-label
  tashkeelWords: 'كلمات بالتشكيل',
  …
};
export const LEMMAS_WORD_VIEW_LABELS = { simple: 'بدون تشكيل', tashkeel: 'بالتشكيل' }; // already short (detail sub-tabs)
```

### Where long labels are used (visible)
- `lemmas-table.component.html` header row: `{{ headers.simpleWords }}` / `{{ headers.tashkeelWords }}`.
- `lemmas-table.component.html` mobile stat badges: visible text `{{ … }} {{ headers.simpleWords }}` /
  `{{ headers.tashkeelWords }}`.

### Where short labels are already used
- Detail panel words sub-tabs use `LEMMAS_WORD_VIEW_LABELS` (`بدون تشكيل` / `بالتشكيل`) — already short.
- Sibling **Roots** table (`roots.labels.ts`): `ROOTS_COLUMN_HEADERS.simpleWords = 'بدون تشكيل'`
  (short header) while `ROOTS_COLUMN_COUNT_LABELS.simpleWords = 'كلمات بدون تشكيل'` (long, aria only).
  This is the approved target pattern.

### Centralized or hardcoded?
**Centralized.** All header strings live in `lemmas.labels.ts` and are consumed via the table's
`headers` / `countLabels` getters. No per-template hardcoding. Good.

### Minimal plan
1. In `lemmas.labels.ts`, change **`LEMMAS_COLUMN_HEADERS.simpleWords` → `'بدون تشكيل'`** and
   **`LEMMAS_COLUMN_HEADERS.tashkeelWords` → `'بالتشكيل'`**.
2. Leave `LEMMAS_COLUMN_COUNT_LABELS` long (these feed `qd-word-count-chip [label]` and the
   mobile-stat `aria-label`, i.e. descriptive/aria only) — mirrors Roots.
3. (Related, for cross-page consistency) apply the same change to `stems.labels.ts`
   `STEMS_COLUMN_HEADERS`. Stems is out of the primary Lemmas scope; flag, do not bundle unless desired.

### Exact files likely to change
- `Frontend/.../models/lemmas.labels.ts` (2 constants).
- (optional consistency) `Frontend/.../models/stems.labels.ts`.

### Tests likely to change
- `lemmas-table.component.spec.ts` → header assertions currently expect
  `'كلمات بدون تشكيل'` / `'كلمات بالتشكيل'`; update to `'بدون تشكيل'` / `'بالتشكيل'`.
- (if Stems updated) `stems-table.component.spec.ts` similarly.

---

## 5. Issue 2 — Remove `النوع` column from main Lemmas table

### Current column source
`lemmas-table.component.html`:
- Header cell: `<div role="columnheader">{{ headers.type }}</div>` (`headers.type = 'النوع'`).
- Loading skeleton: one `lemmas-table__type-cell` placeholder.
- Desktop body cell: `lemmas-table__type-cell--desktop` → `*ngTemplateOutlet="typeCell"`.
- Mobile meta: `*ngTemplateOutlet="typeCell"` inside `lemmas-table__mobile-meta`.
- `#typeCell` template renders `row.dominantType.arabicLabel` + `+{{ row.otherTypesCount }}` badge.

### Backend field / source
- `LemmaListItemDto.DominantType` (`TypeSummaryDto`) + `OtherTypesCount`, produced by
  `EfLemmasReader.LoadWholeSummaryAsync` → `MaterializeTypeDistribution` (count desc, earliest
  Mushaf occurrence). This is a single "dominant" type per lemma — exactly the misleading
  single-type-per-lemma the product decision rejects for the table.

### Frontend display source
The `#typeCell` template, bound to `row.dominantType` / `row.otherTypesCount`.

### Minimal plan (frontend-only, table view)
Remove from `lemmas-table.component.html`:
1. The `النوع` `columnheader` cell.
2. The `lemmas-table__type-cell` loading skeleton placeholder.
3. The desktop `lemmas-table__type-cell--desktop` body cell.
4. The `typeCell` outlet inside `lemmas-table__mobile-meta`.
5. The `#typeCell` `ng-template` definition (now unused).
`lemmas-table.component.ts`: the `additionalTypesAria` getter and `LEMMAS_COLUMN_HEADERS.type`
usage become dead; remove the now-unused references (optional `lemmasAdditionalTypesAria`
helper in labels can be left or pruned).

### Response fields to keep untouched for now
**Keep** `LemmaListItemDto.DominantType`, `OtherTypesCount`, and `LemmaSummaryDto.TypeDistribution`
exactly as-is. The type distribution still drives the details-panel `type-distribution-list`.
No DTO/field removal in this work (response cleanup is a future, separate decision — §9).

### Tests likely to change
- `lemmas-table.component.spec.ts`:
  - `renders the nine semantic column headers` → drop `expect(headers).toContain('النوع')`.
  - `shows dominant type and an additional-types indicator …` → **remove** (asserts
    `lemmas-table-type` + `lemmas-table-additional-types`, both gone).
  - `renders counts and UI row numbers` (`qd-word-count-chip` length 12) is **unaffected**
    (type cell was never a chip).

---

## 6. Issue 3 — Remove `السائد`

### Where it appears
`components/type-distribution-list/type-distribution-list.component.ts`:

```ts
protected readonly dominantHeader = 'السائد';   // hardcoded constant
```

Rendered twice in `type-distribution-list.component.html`:
- Header: `<span class="…__header-dominant">{{ dominantHeader }}</span>`.
- Per-row dominant badge: `@if (row.dominant) { <span class="qd-badge …__dominant">{{ dominantHeader }}</span> }`.

This component is embedded by the Lemmas details panel
(`lemmas-explorer-page.component.html` → `<qd-type-distribution-list>`). It is a **shared**
component (also used by the Stems explorer), so removing `السائد` is consistent across both.

### Hardcoded or constant-driven?
**Hardcoded** local component constant `dominantHeader` (not from `lemmas.labels.ts`, not from any
backend response). Removal is purely presentational.

### Minimal plan
1. Remove the `…__header-dominant` `<span>` (and, if it leaves an empty grid track, drop the
   matching header column from the component scss/grid).
2. Remove the per-row dominant `qd-badge` `<span>{{ dominantHeader }}</span>`.
3. Keep the non-text dominant marker: the row still carries
   `[attr.aria-current]="row.dominant ? 'true' : null"`, `data-testid="type-distribution-dominant"`,
   and `[class.qd-is-selected]="row.dominant"` for the calm, color-free emphasis.
4. Remove the now-unused `dominantHeader` constant.

### Tests likely to change
- `type-distribution-list.component.spec.ts` asserts the dominant row via
  `data-testid="type-distribution-dominant"` + `aria-current="true"` — **not** the word `السائد`,
  so it stays green. No test change strictly required; optionally add a guard
  `expect(root.textContent).not.toContain('السائد')`.

---

## 7. Issue 4 — Lemma words simple/tashkeel display bug

### Current endpoint / API flow
`GET /api/words/lemmas/{id}/words/{wordKind}` (`wordKind` ∈ `simple|tashkeel`)
→ `LemmasController.GetWords` → `GetLemmaWordsHandler` (`LemmaWordKindParser` → `LemmaWordKind`)
→ `ILemmasReader.GetLemmaWordsAsync` → `EfLemmasReader.GetLemmaWordsPageAsync`
→ `LoadLemmaWordRowsAsync(id, useSimpleWordIds, …)`.
Frontend: `LemmasApi.getLemmaWords(id, wordView, …)` → `lemma-words-list` renders
`row.item.displayTextUthmani`.

### Does the frontend send the correct wordView?
**Yes.** `wordView` (`simple`/`tashkeel`) is taken from panel state and placed in the path:
`/lemmas/${id}/words/${encodeURIComponent(wordView)}`. The handler parses it correctly and
selects the simple vs tashkeel branch. The request side is not the bug.

### Does the backend select the right simple/tashkeel identity?
**Grouping identity: yes. Display text: no.** In `EfLemmasReader.LoadLemmaWordRowsAsync`:

```csharp
// simple branch
select new LemmaWordOccurrenceRow(
    w.UniqueSimpleWordId, w.TextUthmani, … )   // ← TextUthmani (VOWELLED)
// tashkeel branch
select new LemmaWordOccurrenceRow(
    w.UniqueTashkeelWordId, w.TextUthmani, … ) // ← TextUthmani (VOWELLED)
```

Both branches project `w.TextUthmani`. The simple branch groups by the *simple* identity but
still displays the *vowelled* text. → `بدون تشكيل` shows tashkeel. **Confirmed bug location:
backend projection** (not frontend binding, not a mapper, not the request).

### Does display text come from the wrong source?
**Yes.** The unvowelled column already exists and is populated:
`quran_words.text_uthmani_simple` (`QuranWord.TextUthmaniSimple`). Seed proof
(`morphology-explorers-seed.sql`):

```
quran_words   id=3001 : text_uthmani='كَلِمَة'  text_uthmani_simple='كلمة'
unique simple id=32001: text_uthmani_simple='كلمة'   (kind=simple identity)
unique tashk. id=31001: text_uthmani='كَلِمَة'        (kind=tashkeel identity)
```

### Proven-correct sibling pattern
- `EfUniqueWordsReader`: simple list → `text_uthmani_simple AS DisplayText` (line 407);
  tashkeel list → `text_uthmani AS DisplayText` (line 378).
- `EfRootsReader.LoadGroupedRootWordsAsync`: simple → `u.TextUthmaniSimple` (line 68);
  tashkeel → `u.TextUthmani` (line 80).

(Note: this aligns with the recorded "display stays Uthmani" decision — the *unvowelled*
display is `text_uthmani_simple`, i.e. Uthmani script without tashkeel, **not** imlaei.)

### Minimal fix plan
- **Backend (the fix):** In `EfLemmasReader.LoadLemmaWordRowsAsync`, change the **simple** branch
  projection from `w.TextUthmani` to **`w.TextUthmaniSimple`**. Leave the tashkeel branch on
  `w.TextUthmani`. One-field change; no join needed (the per-occurrence
  `quran_words.text_uthmani_simple` equals the unique-simple text). DTO/field names unchanged.
- **Frontend:** **No change required** for the bug — `lemma-words-list` faithfully renders whatever
  the DTO returns; after the backend fix, `بدون تشكيل` shows `كلمة`, `بالتشكيل` shows `كَلِمَة`.
- **Out of scope / future:** renaming `LemmaWordItemDto.DisplayTextUthmani` → a neutral
  `DisplayText` (it now holds simple text for the simple kind). Response-cleanup; not part of this plan.

### Backend tests needed (to prove the fix)
- Update `LemmasWordsReadTests.GetLemmaWords_returns_correct_unique_ids_display_text_and_counts_for_each_kind`:
  - `Simple` `InlineData` expected display: `كَلِمَة`→**`كلمة`**, `كَلَّمَ`→**`كلم`** (unvowelled).
  - `Tashkeel` `InlineData` stays `كَلِمَة` / `كَلَّمَ` (vowelled).
- Add an assertion that the simple display contains no Arabic harakat (e.g. simple text ≠ the
  tashkeel text for the same lemma word), so the regression is locked.

### Frontend tests needed
- Optional/light: `lemma-words-list.component.spec.ts` already feeds explicit
  `displayTextUthmani` and asserts render; add one case feeding an unvowelled string for a
  `simple` row and a vowelled string for a `tashkeel` row to document the expectation
  (pure pass-through; the real proof is the backend test).

> Related observation (not in Lemmas scope): `EfStemsReader` has the **identical** bug — both
> word branches project `w.TextUthmani` (lines 426/438). Flag for the Stems polish pass; do not
> bundle here unless the scope is widened.

---

## 8. Issue 5 — Lemmas response inventory (inventory only, no removals)

> "Visible" = rendered text the admin reads. "Routing/state" = used for selection, query-string,
> deep links, pagination. "Pass-through" = present in the DTO but not currently rendered or used
> by the Lemmas page.

### R1 — Lemmas list / catalogue
- **Endpoint:** `GET /api/words/lemmas?search&sort&page&pageSize`
- **Reader/handler:** `GetLemmasPageHandler` → `EfLemmasReader.GetLemmasPageAsync` → `LemmasListDerivation.ToPage`
- **Backend DTO:** `PagedResult<LemmaListItemDto>`
- **Frontend method/model:** `LemmasApi.getLemmasList` → `PagedResultDto<LemmaListItemDto>` → `LemmaListItemViewModel`

| Field | FE consumption |
|---|---|
| `id` | routing/state (selection, `lemma=` query) |
| `lemmaText` | visible (→ `displayText`, lemma cell) |
| `lemmaBuckwalter` | pass-through |
| `rootId` | routing (root deep-link href) |
| `rootText` | visible (root link / dash) |
| `rootBuckwalter` | pass-through |
| `dominantType` (`TypeSummaryDto`) | visible **today** in النوع column (removed by Issue 2; field retained) |
| `otherTypesCount` | visible **today** as `+N` badge (removed by Issue 2; field retained) |
| `occurrencesCount` | visible chip + mobile stat; opens `ayahs` |
| `ayahsCount` | visible chip; opens `ayahs` |
| `surahsCount` | visible chip; opens `surahs/mentioned` |
| `simpleWordsCount` | visible chip; opens `words/simple` |
| `tashkeelWordsCount` | visible chip; opens `words/tashkeel` |
| `stemsCount` | visible chip; opens `stems` |
| `firstVerseKey` | pass-through (not rendered in table) |

- **Pagination/list:** `LEMMAS_LIST_PAGE_SIZE = 1000`; sort ∈ `mushaf-order|occurrences|alpha`.

### R2 — Lemma summary / details
- **Endpoint:** `GET /api/words/lemmas/{id}`
- **Reader/handler:** `GetLemmaSummaryHandler` → `EfLemmasReader.GetLemmaSummaryAsync` → `LemmasListDerivation.ToSummary`
- **Backend DTO:** `LemmaSummaryDto` (all `LemmaListItemDto` fields **+** `IReadOnlyList<TypeSummaryDto> TypeDistribution`)
- **Frontend method/model:** `LemmasApi.getLemmaSummary` → `LemmaSummaryDto`
- **Consumption:** `lemmaText` → panel `selectionTitle` (visible); `typeDistribution` →
  `qd-type-distribution-list` (visible, dominant marker per Issue 3); counts → used for
  state-restore from a shared link; other fields mirror R1.

### R3 — Lemma words (simple **and** tashkeel — same endpoint)
- **Endpoint:** `GET /api/words/lemmas/{id}/words/{wordKind}` (`wordKind` = `simple` | `tashkeel`)
- **Reader/handler:** `GetLemmaWordsHandler` → `EfLemmasReader.GetLemmaWordsPageAsync`
- **Backend DTO:** `PagedResult<LemmaWordItemDto>`
- **Frontend method/model:** `LemmasApi.getLemmaWords` → `PagedResultDto<LemmaWordItemDto>`

| Field | FE consumption |
|---|---|
| `uniqueWordId` | routing (unique-word deep link `word=`) |
| `kind` | routing (deep-link segment `/unique/{kind}`) |
| `displayTextUthmani` | visible (word glyphs) — **Issue 4 source field** |
| `occurrencesCount` | visible (badge) |
| `firstVerseKey` | pass-through |

- **Pagination:** `LEMMA_DETAIL_PAGE_SIZE = 100`; handler `MaxPageSize = 1000`.
- **Sample (test):** simple → `{uniqueWordId:32001, kind:'simple', displayTextUthmani:'كَلِمَة'→(fix)'كلمة', occurrencesCount:10}`;
  tashkeel → `{uniqueWordId:31001, kind:'tashkeel', displayTextUthmani:'كَلِمَة', occurrencesCount:10}`.

### R4 — Lemma ayah matches
- **Endpoint:** `GET /api/words/lemmas/{id}/ayahs?page&pageSize`
- **Reader/handler:** `GetLemmaAyahsHandler` → `EfLemmasReader.GetLemmaAyahMatchesAsync`
- **Backend DTO:** `PagedResult<LemmaAyahMatchDto>` (extends shared `AyahMatchDto` shape)
- **Frontend method/model:** `LemmasApi.getLemmaAyahMatches` → `PagedResultDto<LemmaAyahMatchDto>` → shared `qd-ayah-matches-list`

| Field | FE consumption |
|---|---|
| `ayahId` | routing/state |
| `verseKey` | visible |
| `surahNumber` | visible / routing |
| `surahNameArabic` | visible |
| `ayahNumber` | visible |
| `pageNumber` | visible (mushaf page) |
| `matchedQuranWordIds[]` | highlight logic |
| `words[]` (`AyahWordForHighlightDto{quranWordId,textUthmani,isAyahMarker}`) | visible (ayah render + highlight) |

### R5 — Mentioned surahs
- **Endpoint:** `GET /api/words/lemmas/{id}/surahs`
- **Reader/handler:** `GetLemmaMentionedSurahsHandler` → `EfLemmasReader.GetLemmaMentionedSurahsAsync`
- **Backend DTO:** `LemmaSurahsResponse { Id, LemmaText, SurahsCount, Surahs[ LemmaSurahItemDto{SurahNumber,NameArabic,OccurrencesInSurah} ] }`
- **Frontend method/model:** `LemmasApi.getLemmaMentionedSurahs` → `LemmaSurahsDto` → `qd-surah-occurrences-list`
- **Consumption:** `nameArabic` + `occurrencesInSurah` visible; `surahNumber` routing; `id`/`lemmaText`/`surahsCount` state/pass-through.

### R6 — Missing surahs
- **Endpoint:** `GET /api/words/lemmas/{id}/missing-surahs`
- **Reader/handler:** `GetLemmaMissingSurahsHandler` → `EfLemmasReader.GetLemmaMissingSurahsAsync`
- **Backend DTO:** `LemmaMissingSurahsResponse { Id, LemmaText, MissingSurahsCount, Surahs[ MissingSurahItemDto{SurahNumber,NameArabic} ] }`
- **Frontend method/model:** `LemmasApi.getLemmaMissingSurahs` → `LemmaMissingSurahsDto` → `qd-missing-surahs-list`
- **Consumption:** `nameArabic` visible; `surahNumber` routing; counts/identity pass-through.

### R7 — Related stems
- **Endpoint:** `GET /api/words/lemmas/{id}/stems`
- **Reader/handler:** `GetLemmaStemsHandler` → `EfLemmasReader.GetLemmaStemsAsync` (`MorphologyRelatedItemsOrdering.OrderLemmaStems`)
- **Backend DTO:** `LemmaStemsResponse { Id, LemmaText, StemsCount, Stems[ LemmaStemItemDto{StemId,StemText,OccurrencesCount} ] }`
- **Frontend method/model:** `LemmasApi.getLemmaStems` → `LemmaStemsDto` → `qd-lemma-stems-list`
- **Consumption:** `stemText` + `occurrencesCount` visible; `stemId` routing; identity/count pass-through.

### Type distribution (no separate endpoint)
- Not its own response. The **dominant** type ships on R1/R2 (`DominantType` + `OtherTypesCount`);
  the **full ordered** distribution ships on R2 (`LemmaSummaryDto.TypeDistribution`). Rendered by
  the shared `qd-type-distribution-list` (Issue 3 target).

### Related roots (no dedicated lemma→roots response)
- The page exposes only the lemma's **single owned root** via R1/R2 (`rootId`/`rootText`),
  rendered as a new-tab link to the Roots explorer. There is **no** "related roots list" endpoint
  for a lemma. (Recorded for completeness; no action.)

---

## 9. Proposed implementation phases (do **not** implement here)

- **Phase 1 — Headers:** `lemmas.labels.ts` → short `LEMMAS_COLUMN_HEADERS.simpleWords` /
  `tashkeelWords`; keep `LEMMAS_COLUMN_COUNT_LABELS` long for aria. (Optional: mirror in `stems.labels.ts`.)
- **Phase 2 — Remove النوع column:** strip header cell, skeleton cell, desktop cell, mobile-meta
  outlet, and `#typeCell` template from `lemmas-table.component.html`; prune dead `additionalTypesAria`.
  Keep `DominantType`/`OtherTypesCount`/`TypeDistribution` DTO fields untouched.
- **Phase 3 — Remove السائد:** delete the `dominantHeader` header + dominant badge in
  `type-distribution-list`; retain `aria-current` + testid + selected styling; remove the constant.
- **Phase 4 — Fix simple/tashkeel words:** `EfLemmasReader.LoadLemmaWordRowsAsync` simple branch
  → `w.TextUthmaniSimple`. No frontend change.
- **Phase 5 — Tests:** update `lemmas-table.component.spec.ts` (headers, drop dominant-type test),
  `LemmasWordsReadTests.cs` (simple expected display → unvowelled + harakat-free assertion);
  optional `type-distribution-list.component.spec.ts` guard and `lemma-words-list.component.spec.ts` case.
- **Phase 6 — Verification:** focused tests, then builds (§11).

**Future, separate phase (out of scope):** response-cleanup decisions — e.g. removing
`DominantType`/`OtherTypesCount` from `LemmaListItemDto` if the table no longer needs them, and
renaming `LemmaWordItemDto.DisplayTextUthmani` → `DisplayText`. Not part of this plan.

---

## 10. Test plan

| What to prove | Layer | Test |
|---|---|---|
| Short headers `بدون تشكيل` / `بالتشكيل` used | FE | `lemmas-table.component.spec.ts` header assertion |
| `النوع` column removed from main table | FE | `lemmas-table.component.spec.ts` — drop `toContain('النوع')`; remove dominant-type test |
| `السائد` no longer visible | FE | `type-distribution-list.component.spec.ts` — dominant still via testid/aria-current; optional `not.toContain('السائد')` |
| Simple words are unvowelled | BE | `LemmasWordsReadTests` Simple `InlineData` → `كلمة`/`كلم`; assert no harakat |
| Tashkeel words are vowelled | BE | `LemmasWordsReadTests` Tashkeel `InlineData` stays `كَلِمَة`/`كَلَّمَ` |
| Links/state/pagination still work | FE | existing `lemmas-table` chip→view/sub-view mapping, `lemma-words-list` deep-link + pagination, root link target/rel — must stay green |

---

## 11. Verification commands

> Run focused tests first, then full builds. Use the exact runners from `Backend/CLAUDE.md` and
> `Frontend/quran-dashboard-ui/CLAUDE.md`. Per recorded constraint, keep the frontend
> `VITEST_MAX_FORKS` cap in place (uncapped `npm test` can OOM/freeze the machine).

### Backend
```bash
# focused Lemmas tests
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --filter "FullyQualifiedName~WordsMorphologyExplorers.LemmasWordsReadTests"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --filter "FullyQualifiedName~WordsMorphologyExplorers.LemmasListReadTests"

# backend build
dotnet build Backend/QuranDashboard.sln
```

### Frontend
```bash
# focused Lemmas specs (keep the worker cap)
cd Frontend/quran-dashboard-ui
npm test -- lemmas-table.component
npm test -- lemma-words-list.component
npm test -- type-distribution-list.component

# frontend build
npm run build
```

---

## 12. Non-goals (explicit)

- **No migrations.**
- **No importers.**
- **No Quran data mutation** (display now reads the already-imported `text_uthmani_simple`; no
  source text is altered).
- **No new routes.**
- **No response cleanup decisions** in this report (no DTO/field removals; inventory only).
- **No Word Types Explorer work.**
- **No DB/admin label system** yet.
- **No full localization system.**

---

## 13. Risks / notes

- **Shared `type-distribution-list` (Issue 3):** removing `السائد` affects every consumer
  (Lemmas **and** Stems panels). This is the intended unified behavior; just be aware the change
  is not Lemmas-isolated. The component's scss grid likely has a 3rd "dominant" track that should
  shrink to 2 to avoid an empty column.
- **Issue 4 field-name drift:** after the fix, `DisplayTextUthmani` carries *unvowelled* text for
  the simple kind. Acceptable short-term; flagged for the future response-cleanup phase.
- **Stems parity (Issue 4):** `EfStemsReader` shares the identical simple/tashkeel projection bug.
  Out of Lemmas scope, but the page-level inconsistency will remain until Stems is patched.
- **`النوع` two meanings:** Issue 2 removes the **main table** `النوع` column only. The
  `type-distribution-list` keeps its own `النوع` header (meaningful in the distribution context) —
  do not remove that one.
- **Mobile stat badges (Issue 1):** they reuse `headers.*` for visible text, so the short headers
  also shorten the mobile badge labels — desired and consistent; aria stays long via `countLabels`.
```
