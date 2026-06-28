# Lemmas Explorer — Comprehensive Implementation Plan

Feature: 017 Lexical Explorers Polish
Page: **الصيغ المعجمية** (Lemmas Explorer), route `/dashboard/words/lemmas`
Type: implementation plan only (no production code, no tests, no commits modified)
Branch: `017-lexical-explorers-polish`
Inputs: `docs/feature-017-lexical-explorers-polish/lemmas-page-focused-adjustments-report.md` (inspection)
+ current repository state.

> **Primary reference:** the **Roots Explorer** has already been migrated (uncommitted on this
> branch) to exactly the cleaned shapes this plan applies to Lemmas — cleaned ayah DTO with
> `isMatched`, marker exclusion in the reader, bare `{ surahs }` / `{ stems }` wrappers, and a
> per-page mapper that bridges the cleaned DTO back to the **unchanged** shared
> `qd-ayah-matches-list`. Lemmas mirrors Roots field-for-field.

---

## 1. Verdict

**READY_FOR_IMPLEMENTATION**

Every change has a concrete, file-level target and a complete sibling reference in Roots. The one
real bug (Issue 4 / R3 display source) is confirmed in the backend projection and proven-fixable.
No blocking clarification remains (§12).

---

## 2. Scope

### In scope
- Lemmas Explorer page, details panel, table, words list, type-distribution usage on Lemmas.
- Lemmas backend: controller DTO shapes, `EfLemmasReader`, `LemmasListDerivation`, query handlers
  (only where DTO shape/log fields change), response wrapper records.
- Lemmas frontend: `lemmas.models.ts`, `lemmas.labels.ts`, `lemmas.api.ts`, lemma facades/state,
  lemma components, a new `lemma-ayah-match.mapper.ts` (mirror of the Roots mapper).
- Lemmas backend + frontend tests/fixtures.

### Out of scope
- **Stems Explorer / `الأصول الصرفية` page — no direct work.** No Stems labels, no Stems backend
  bug fix (note: `EfStemsReader` shares the Issue-4 projection bug — *not* fixed here), no Stems
  response cleanup. Stems gets its own later pass.
- Word Types Explorer.
- The global `ApiResponse<T>` envelope — unchanged.
- Migrations, importers, Quran data mutation, new routes, DB/admin labels, localization system.

### Shared-component side effects (must be called out)
- `type-distribution-list` (Issue 3 / R8) is shared with the Stems panel. Removing the visible
  `السائد` text changes Stems too. This is the intended unified behavior; **no Stems-specific work**
  is added — just the shared component edit.
- `qd-ayah-matches-list` + `qd-highlighted-ayah` (the shared ayah renderer) stay on their current
  shape. Lemmas does **not** modify them; it adds a Lemmas mapper that adapts the cleaned Lemmas
  DTO to the shared shape — identical to how Roots already does it. No Stems/UniqueWords impact.
- `surah-occurrences-list` / `missing-surahs-list` take `[surahs]` arrays (item shape unchanged by
  this plan), so wrapper removal (R5/R6) does not affect them.

---

## 3. Current behavior summary

- **Long headers:** Lemmas table renders `كلمات بدون تشكيل` / `كلمات بالتشكيل` (visible) from
  `LEMMAS_COLUMN_HEADERS`. Roots already uses the short form for headers.
- **`النوع` column:** Lemmas table has a dedicated `النوع` column rendering `row.dominantType.arabicLabel`
  + a `+N` other-types badge (`#typeCell` template).
- **`السائد`:** `type-distribution-list` shows a hardcoded `dominantHeader = 'السائد'` twice (header
  cell + per-row dominant badge).
- **Simple/tashkeel bug:** `EfLemmasReader.LoadLemmaWordRowsAsync` projects `w.TextUthmani`
  (vowelled) for **both** kinds; only the grouping identity differs → `بدون تشكيل` shows vowelled glyphs.
- **Response shapes (current):**
  - List/summary: `LemmaListItemDto` / `LemmaSummaryDto` carry `lemmaBuckwalter`, `rootBuckwalter`,
    `dominantType`, `otherTypesCount`, `firstVerseKey` (+ summary `typeDistribution`).
  - Words: `LemmaWordItemDto { uniqueWordId, kind, displayTextUthmani, occurrencesCount, firstVerseKey }`.
  - Ayahs: `LemmaAyahMatchDto { ayahId, verseKey, surahNumber, surahNameArabic, ayahNumber,
    pageNumber, matchedQuranWordIds[], words[ AyahWordForHighlightDto{quranWordId,textUthmani,isAyahMarker} ] }`
    (markers **included**; frontend `LemmaAyahMatchDto extends AyahMatchDto`).
  - Surahs/missing/stems: wrapper records `{ id, lemmaText, …Count, surahs|stems[] }`.

---

## 4. Approved target behavior

- **Headers:** visible Lemmas table headers = `بدون تشكيل` / `بالتشكيل`; long form kept only for
  chip/mobile `aria-label` (count labels).
- **`النوع` column:** removed from the main table. Type distribution stays in the details panel.
- **`السائد`:** removed from visible UI; calm non-text emphasis (`aria-current`, selected styling,
  `data-testid="type-distribution-dominant"`) retained.
- **Simple/tashkeel:** `بدون تشكيل` → `TextUthmaniSimple` (unvowelled); `بالتشكيل` → `TextUthmani` (vowelled).
- **Response cleanup (R1–R8):** trimmed list/summary/words/ayah/surahs/missing/stems shapes per the
  approved decisions; `ApiResponse<T>` untouched; `typeDistribution` retained on summary.

Target conceptual shapes (from the task) — ayah / surahs / missing / stems:

```json
// ayah match
{ "ayahId": 1, "verseKey": "1:1", "surahNameArabic": "الفاتحة", "pageNumber": 1,
  "words": [ { "textUthmani": "كَلِمَة", "isMatched": true } ] }
// mentioned surahs
{ "surahs": [ { "surahNumber": 1, "nameArabic": "الفاتحة", "occurrencesInSurah": 2 } ] }
// missing surahs
{ "surahs": [ { "surahNumber": 2, "nameArabic": "البقرة" } ] }
// related stems
{ "stems": [ { "stemId": 200, "stemText": "كَلِم", "occurrencesCount": 10 } ] }
```

---

## 5. Backend implementation plan (file-level)

> Namespace: `QuranDashboard.Application.Abstractions.Quran.Words.Lemmas.Responses` for DTOs,
> `…Infrastructure.Persistence.Reads.Quran.Words.Lemmas` for the reader. `ApiResponse<T>` unchanged.

### 5.1 Lemma words display fix + R3 DTO trim
- **`EfLemmasReader.LoadLemmaWordRowsAsync`** — in the **simple** branch change the projected display
  from `w.TextUthmani` → **`w.TextUthmaniSimple`**. Tashkeel branch keeps `w.TextUthmani`.
  (Roots reference: `EfRootsReader` simple→`u.TextUthmaniSimple`, tashkeel→`u.TextUthmani`.)
- **`EfLemmasReader.GetLemmaWordsPageAsync`** — construct the trimmed DTO `(UniqueWordId, DisplayText,
  OccurrencesCount)`; drop the `kindKey` and `BuildFirstVerseKey(...)` arguments. Internal
  `LemmaWordGroupRow` keeps its ordering fields (`FirstSurah/Ayah/WordNumber`) for sort; only the
  emitted DTO shrinks. Rename internal `DisplayTextUthmani` field to `DisplayText` (optional, cosmetic).
- **`LemmaWordItemDto.cs`** → `record LemmaWordItemDto(int UniqueWordId, string DisplayText, int OccurrencesCount)`.
  Remove `Kind`, `DisplayTextUthmani`→`DisplayText`, remove `FirstVerseKey`.
- **`GetLemmaWordsHandler`** — `kind` stays a request/log field (path param `{wordKind}` still drives
  the simple/tashkeel branch and structured logging via `GetKindKey`); only the **response** drops it.
  No handler signature change.

### 5.2 R1 — Lemmas list DTO trim
- **`LemmaListItemDto.cs`** → keep `Id, LemmaText, RootId, RootText, OccurrencesCount, AyahsCount,
  SurahsCount, SimpleWordsCount, TashkeelWordsCount, StemsCount`. Remove `LemmaBuckwalter`,
  `RootBuckwalter`, `DominantType`, `OtherTypesCount`, `FirstVerseKey`.
- **`LemmasListDerivation.ToListItem`** — drop the removed ctor args. The `DominantType(...)` helper
  becomes unused for the list (still used by summary unless also dropped — see 5.3); prune if fully unused.

### 5.3 R2 — Lemma summary DTO trim (keep `typeDistribution`)
- **`LemmaSummaryDto.cs`** → keep `Id, LemmaText, RootId, RootText, counts…, TypeDistribution`.
  Remove `LemmaBuckwalter`, `RootBuckwalter`, `DominantType`, `OtherTypesCount`, `FirstVerseKey`.
- **`LemmasListDerivation.ToSummaryDto`** — drop removed ctor args; **retain** the `distribution`
  list + `NoType` fallback. `DominantType`/`OtherTypesCount` helpers may be removed if now unused.
- **`LemmasSummaryRow` + `EfLemmasReader.LoadWholeSummaryAsync`** *(optional minimal cleanup)*: with
  `FirstVerseKey` gone from both DTOs, the row's `FirstVerseKey` and the SQL
  `agg.first_surah_number/first_ayah_number` + their `ARRAY_AGG`/`BuildFirstVerseKey` become dead.
  Sort still uses `FirstWordOrderInMushaf` (unaffected). **Recommendation:** leave the aggregation in
  place for a minimal, low-risk diff, or remove the dead bits in the same pass — implementer's choice;
  not required for correctness.

### 5.4 R4 — Lemma ayah matches reshape + marker exclusion
- **`LemmaAyahMatchDto.cs`** → replace with the cleaned pair (mirror `RootAyahMatchDto.cs`):
  ```csharp
  public sealed record LemmaAyahMatchDto(
      int AyahId, string VerseKey, string SurahNameArabic, short PageNumber,
      IReadOnlyList<LemmaAyahWordDto> Words);
  public sealed record LemmaAyahWordDto(string TextUthmani, bool IsMatched);
  ```
  Remove `SurahNumber`, `AyahNumber`, `MatchedQuranWordIds`; stop using the shared
  `AyahWordForHighlightDto`.
- **`EfLemmasReader.GetLemmaAyahMatchesAsync`** — reshape to mirror `EfRootsReader.GetRootAyahMatchesAsync`:
  - Keep the existing existence check + `ReadPaging.CalculateSafeSkip` (Lemmas' safer paging — retain
    it; do **not** regress to Roots' raw `Skip`).
  - Build `matchedSet` per ayah from `WordMorphologies` where `LemmaId == id`.
  - Load page words with **`!w.IsAyahMarker`** in the `Where` (marker exclusion at the query).
  - Emit `LemmaAyahWordDto(w.TextUthmani, matchedSet.Contains(w.QuranWordId))`; page number from
    `ResolveAyahPageNumber(words)` (now over readable words only — keep the helper + fallback).
  - Drop `MatchedQuranWordIds` / `AyahWordForHighlightDto` projections and the `matchedIdsByAyah`
    list passed to the DTO (the set is now consumed only to compute `IsMatched`).
- **Marker exclusion must be test-asserted**, not just implemented (see §8).

### 5.5 R5 / R6 / R7 — wrapper metadata removal
- **`LemmaSurahsResponse.cs`** → `record LemmaSurahsResponse(IReadOnlyList<LemmaSurahItemDto> Surahs)`
  (drop `Id`, `LemmaText`, `SurahsCount`). `LemmaSurahItemDto` unchanged.
- **`LemmaMissingSurahsResponse.cs`** → `record LemmaMissingSurahsResponse(IReadOnlyList<MissingSurahItemDto> Surahs)`
  (drop `Id`, `LemmaText`, `MissingSurahsCount`).
- **`LemmaStemsResponse.cs`** → `record LemmaStemsResponse(IReadOnlyList<LemmaStemItemDto> Stems)`
  (drop `Id`, `LemmaText`, `StemsCount`). `LemmaStemItemDto` unchanged.
- **`EfLemmasReader`** `GetLemmaMentionedSurahsAsync` / `GetLemmaMissingSurahsAsync` / `GetLemmaStemsAsync`:
  - Keep the **not-found → `null`** guard (controller maps to 404).
  - Return the bare list (`new LemmaSurahsResponse(surahs)` etc.); empty case returns
    `new LemmaSurahsResponse([])` instead of the old `(id, text, 0, [])`.
  - The `lemma.LemmaText` lookups used only to fill wrapper fields can be reduced to a pure existence
    check (mirror Roots) — optional micro-cleanup.

### 5.6 R8 — type distribution
- No backend change beyond R2 (it already ships on `LemmaSummaryDto.TypeDistribution`). `TypeSummaryDto`
  and the per-type ordering in `EfLemmasReader.MaterializeTypeDistribution` stay as-is.

### 5.7 Controller / messages
- **`LemmasController`** — no route changes; action return types already reference the DTO records by
  name, so they recompile against the new shapes. XML summaries that mention "النوع الغالب" /
  "first verse key" should be reworded (doc-comment only). `ApiMessages` unchanged.

---

## 6. Frontend implementation plan (file-level)

### 6.1 Issue 1 — short headers (`lemmas.labels.ts`)
- `LEMMAS_COLUMN_HEADERS.simpleWords` → `'بدون تشكيل'`; `…tashkeelWords` → `'بالتشكيل'`.
- Leave `LEMMAS_COLUMN_COUNT_LABELS` long (feeds chip/mobile `aria-label`). Mirrors Roots.

### 6.2 Issue 2 — remove `النوع` column (`lemmas-table.component.html` + `.ts`)
- Remove: the `النوع` `columnheader` cell; the `lemmas-table__type-cell` loading skeleton; the desktop
  `lemmas-table__type-cell--desktop` body cell; the `typeCell` outlet inside `lemmas-table__mobile-meta`;
  the `#typeCell` `ng-template`.
- `lemmas-table.component.ts`: remove the `additionalTypesAria` getter and the `headers.type` reference;
  drop unused `dominantType`/`otherTypesCount` reads. (`LEMMAS_COLUMN_HEADERS.type` const + the
  `lemmasAdditionalTypesAria` label helper become dead — prune.)
- Responsive scss: drop `…__type-cell` rules (cosmetic).

### 6.3 Issue 3 / R8 — remove `السائد` (`type-distribution-list.component.*`)
- `.ts`: remove the `dominantHeader = 'السائد'` constant.
- `.html`: remove the `…__header-dominant` `<span>` and the per-row dominant `qd-badge`
  `{{ dominantHeader }}`. Keep `[class.qd-is-selected]="row.dominant"`, `aria-current`, and
  `data-testid="type-distribution-dominant"`. Shrink the header grid track if it leaves an empty column.
- **Shared with Stems** — call out, no Stems-specific edits.

### 6.4 Models (`lemmas.models.ts`)
- `LemmaListItemDto` → drop `lemmaBuckwalter`, `rootBuckwalter`, `dominantType`, `otherTypesCount`,
  `firstVerseKey`. (Also drop the now-unused `TypeSummaryDto` import from the table if applicable;
  `TypeSummaryDto` stays for `LemmaSummaryDto.typeDistribution`.)
- `LemmaSummaryDto` → drop the same five; keep `typeDistribution`.
- `LemmaWordItemDto` → `{ uniqueWordId, displayText, occurrencesCount }` (drop `kind`,
  `displayTextUthmani`→`displayText`, drop `firstVerseKey`).
- Ayah: stop `LemmaAyahMatchDto extends AyahMatchDto`; define cleaned shape mirroring `roots.models.ts`:
  ```ts
  export interface LemmaAyahWordDto { textUthmani: string; isMatched: boolean; }
  export interface LemmaAyahMatchDto {
    ayahId: number; verseKey: string; surahNameArabic: string; pageNumber: number;
    words: LemmaAyahWordDto[];
  }
  ```
- `LemmaSurahsDto` → `{ surahs: LemmaSurahItemDto[] }`; `LemmaMissingSurahsDto` → `{ surahs: MissingSurahItemDto[] }`;
  `LemmaStemsDto` → `{ stems: LemmaStemItemDto[] }` (drop `id`/`lemmaText`/`*Count`).
- `LemmasPanelState.ayahs` stays `PagedResultDto<LemmaAyahMatchDto>` (now cleaned shape).

### 6.5 API service (`lemmas.api.ts`)
- No endpoint/URL changes. Return-type generics already reference the model interfaces by name, so they
  follow the trimmed shapes automatically. `getLemmaWords` still takes `wordView` in the path (kind stays
  a request concern). No change to method signatures.

### 6.6 Facade / state
- **`lemmas-detail-panel.updates.ts`** — `buildMentionedSurahsPanelUpdate` / `buildMissingSurahsPanelUpdate`
  / `buildStemsPanelUpdate` already read `data.surahs.length` / `data.stems.length` → unaffected by wrapper
  removal. `buildAyahsPanelUpdate` / `buildWordsPanelUpdate` are shape-agnostic (store `data` as-is) → no change.
- **`lemmas-detail-view.loader.ts`**, **`lemmas-detail.facade.ts`**, **`lemmas-cache.ts`**,
  **`lemmas-url-sync.ts`** — no logic change (they pass typed responses through). They recompile against
  the trimmed models. Verify `lemmas-explorer.facade.ts` list→`LemmaListItemViewModel` mapping does not
  read any removed field (it should only spread + add `displayText`).

### 6.7 Words list (`lemma-words-list.component.*`) — R3
- `.ts`: replace `item.displayTextUthmani` → `item.displayText`; remove reliance on `item.kind`. Add an
  input for the active kind, e.g. `readonly kind = input.required<LemmaWordView>()`, and use it in
  `buildUniqueWordsDeepLink(this.kind(), { wordId: item.uniqueWordId, view: 'ayahs' })`.
- `.html`: `{{ row.item.displayText }}` for the visible glyphs.
- **`lemmas-explorer-page.component.html`** — pass `[kind]="panelState().wordView"` to both the loading
  and success `<qd-lemma-words-list>` instances.

### 6.8 Ayah matches — mapper + page wiring (R4)
- **New util `features/words/utils/lemma-ayah-match.mapper.ts`** — mirror `root-ayah-match.mapper.ts`:
  ```ts
  import { AyahMatchDto } from '../models/unique-words.models';
  import { LemmaAyahMatchDto } from '../models/lemmas.models';
  import { parseVerseKey } from './verse-key';            // reuse existing helper
  export function mapLemmaAyahMatchToShared(match: LemmaAyahMatchDto): AyahMatchDto { … }
  ```
  Synthesize `ayahNumber` via `parseVerseKey(verseKey)`, `matchedQuranWordIds` from word indices where
  `isMatched`, and `words[].{ quranWordId: index, textUthmani, isAyahMarker: false }`. The shared
  `qd-ayah-matches-list` / `qd-highlighted-ayah` stay **unchanged**.
- **`lemmas-explorer-page.component.ts`** — add `ayahsPageForView` computed (mirror Roots): map
  `panelState().ayahs.items` through `mapLemmaAyahMatchToShared`; retype `emptyAyahsPage` to
  `PagedResultDto<AyahMatchDto>` (shared). Import `AyahMatchDto` from `unique-words.models` and the mapper.
- **`lemmas-explorer-page.component.html`** — bind the ayahs success case to `[page]="ayahsPageForView()"`
  instead of the raw `panelState().ayahs`.
- **`verse-key.ts`** — reuse the existing `parseVerseKey` (already used by the Roots mapper); no new helper.

### 6.9 Surahs / missing / stems consumers
- `lemmas-explorer-page.component.html` already binds `surahs.surahs`, `missing.surahs`,
  `panelState().stems?.stems` → unaffected by wrapper removal. No template change beyond model types.

---

## 7. Implementation phases

> The DTO reshapes are an intentional **frontend⇄backend contract break** confined to this feature
> branch. Phase 1 is independently shippable. Phases 2–5 each pair a backend shape change with its
> frontend counterpart and **must land together per area** (a half-applied shape breaks the page).
> Recommended: **two PRs** — (A) Phase 1 UI-only; (B) Phases 2–6 as one coordinated backend+frontend PR
> (matching how Roots landed). A single all-in-one PR is also acceptable if review size is manageable.

### Phase 1 — Lemmas UI-only fixes (no contract change)
- Short headers (6.1); remove `النوع` column (6.2); remove visible `السائد` (6.3).
- Self-contained; green without backend changes.

### Phase 2 — Lemma words response + display fix
- Backend: simple→`TextUthmaniSimple` (5.1); trim `LemmaWordItemDto` (drop `kind`/`firstVerseKey`,
  rename `displayTextUthmani`→`displayText`).
- Frontend: model + `lemma-words-list` `displayText` + `kind` input (6.7).

### Phase 3 — List & summary cleanup
- Backend: trim `LemmaListItemDto` / `LemmaSummaryDto` + `LemmasListDerivation` (5.2/5.3).
- Frontend: trim models; ensure table/facade reference no removed field (6.4/6.6).

### Phase 4 — Ayah match cleanup
- Backend: cleaned `LemmaAyahMatchDto` + `LemmaAyahWordDto`, marker exclusion, `isMatched` (5.4).
- Frontend: cleaned model + new `lemma-ayah-match.mapper.ts` + page `ayahsPageForView` wiring (6.8).
- `pageNumber` retained for Mushaf deep links.

### Phase 5 — Surahs / missing-surahs / stems wrapper cleanup
- Backend: bare `{ surahs }` / `{ stems }` responses (5.5).
- Frontend: trim models; consumers already use `.surahs` / `.stems` (6.4/6.9).

### Phase 6 — Tests
- Update backend + frontend tests/fixtures per §8.

### Phase 7 — Verification
- Run focused tests then builds per §9.

---

## 8. Test plan

### Backend (`tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/…`)
- **`LemmasWordsReadTests`**: Simple `InlineData` expected display → **unvowelled** (`كَلِمَة`→`كلمة`,
  `كَلَّمَ`→`كلم`); Tashkeel stays vowelled. Replace `first.DisplayTextUthmani` assertions with
  `first.DisplayText`; remove `first.Kind` assertions (field gone). Add a guard that simple text ≠
  tashkeel text (no harakat). Counts/paging assertions unchanged.
  - Seed already supports this: `morphology-explorers-seed.sql` has `text_uthmani_simple='كلمة'/'كلم'`.
- **`LemmasAyahsReadTests`**: assert the cleaned shape — no `surahNumber`/`ayahNumber`/`matchedQuranWordIds`;
  `words[]` are `{ textUthmani, isMatched }`; **no ayah-marker word is returned**; `pageNumber` resolves
  from a readable (non-marker) word; `isMatched` is true for matched words and false otherwise. (Mirror the
  Roots ayah tests.) Seed an ayah containing a marker word to prove exclusion.
- **`LemmasListReadTests`**: drop assertions on `dominantType` / `otherTypesCount` / `lemmaBuckwalter` /
  `rootBuckwalter` / `firstVerseKey` for the **list**; keep `typeDistribution` assertions for the
  **summary** path. Verify counts + sort still hold.
- **`LemmasSurahsReadTests`** (mentioned + missing): assert against bare `.Surahs` (no
  `Id`/`LemmaText`/`*Count` wrapper); not-found still 404.
- **`MorphologyRelationshipsReadTests`** (lemma stems) + **`MorphologyExplorersCacheReadTests`**: update
  to bare `.Stems` wrapper; cache no-new-command assertions unchanged.

### Frontend
- **`lemmas-table.component.spec.ts`**: headers contain `بدون تشكيل` / `بالتشكيل` (not the long form);
  `النوع` **not** present; remove the dominant-type/additional-types test; trim the `row()` builder
  (remove `dominantType`/`lemmaBuckwalter`/`rootBuckwalter`/`firstVerseKey`); chip count + count-open
  mapping still pass.
- **`lemma-words-list.component.spec.ts`**: feed `displayText`; pass the `kind` input; assert the
  unique-word deep link still resolves (`/dashboard/words/unique/{kind}`, `word=`, `view=ayahs`) without
  the `kind` DTO field; badge count + pagination unchanged. Optionally assert a simple row renders the
  unvowelled string it is given.
- **`lemmas-explorer-page.component.spec.ts`**: rebuild the `ayahMatch` fixture to the cleaned shape
  (`{ ayahId, verseKey, surahNameArabic, pageNumber, words:[{textUthmani,isMatched}] }`); assert matched
  words highlight via `.highlighted-ayah__word--matched`; assert Mushaf open link uses `pageNumber`;
  mentioned/missing surahs + stems render from bare `{ surahs }` / `{ stems }`; selection/state/pagination
  intact.
- **`type-distribution-list.component.spec.ts`**: dominant still identified by
  `data-testid="type-distribution-dominant"` + `aria-current`; add `expect(textContent).not.toContain('السائد')`.

---

## 9. Verification commands

> Focused tests first, then builds. Use the exact runners in `Backend/CLAUDE.md` and
> `Frontend/quran-dashboard-ui/CLAUDE.md`. Keep the frontend `VITEST_MAX_FORKS` cap (uncapped
> `npm test` can OOM/freeze the machine).

### Backend
```bash
# focused Lemmas tests
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --filter "FullyQualifiedName~WordsMorphologyExplorers.Lemmas"
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --filter "FullyQualifiedName~WordsMorphologyExplorers.MorphologyRelationshipsReadTests"

# backend build
dotnet build Backend/QuranDashboard.sln
```

### Frontend
```bash
cd Frontend/quran-dashboard-ui
# focused Lemmas specs (keep the worker cap)
npm test -- lemmas-table.component
npm test -- lemma-words-list.component
npm test -- lemmas-explorer-page.component
npm test -- type-distribution-list.component

# frontend build
npm run build
```

---

## 10. Backward compatibility / risk notes

- **Intentional contract break:** the trimmed DTOs break the FE/BE contract, but only inside this
  feature branch and only for Lemmas endpoints. Backend + frontend for each area must ship together
  (§7). No external/public consumer exists for these endpoints.
- **Shared `type-distribution-list` (Stems side effect):** removing `السائد` also affects the Stems
  panel. Intended; no Stems-specific work added.
- **Ayah DTO alignment with Roots:** the cleaned Lemmas ayah DTO is intentionally identical in spirit
  to `RootAyahMatchDto`, but they remain **separate, Lemmas-namespaced** records and a separate
  `lemma-ayah-match.mapper.ts`. The shared `qd-ayah-matches-list` / `qd-highlighted-ayah` are not
  modified. The mapper synthesizes `quranWordId` from the word index (matched-set by index) exactly as
  Roots does — keep that convention so the shared highlighter keeps working.
- **`pageNumber` must survive:** it is required for Mushaf navigation/deep links and stays on the ayah DTO.
- **Marker exclusion is a correctness rule for Quran display:** it must be **tested**, not just coded —
  seed a marker word and assert it is absent from the response (§8).
- **No Quran text mutation:** the display fix only *reads* the already-imported `text_uthmani_simple`
  column; no source text is written.
- **Stems backend parity bug stays open:** `EfStemsReader` has the same simple/tashkeel projection bug.
  Out of scope here; it remains until the Stems pass. Flagged so it is not mistaken for fixed.
- **`firstVerseKey` aggregation:** if `firstVerseKey` is dropped but the SQL aggregation is left intact,
  there is dead computation (harmless). Removing it is optional; sort uses `FirstWordOrderInMushaf`.

---

## 11. Non-goals (explicit)

- No migrations.
- No importers.
- No Quran data mutation.
- No new routes.
- No Stems Explorer direct work (shared-component side effects only).
- No Word Types Explorer work.
- No DB/admin label system.
- No full localization system.
- No changes to the global `ApiResponse<T>` envelope.
- No commits, no staging, no destructive commands.

---

## 12. Open questions

`No blocking clarification required.`

Non-blocking implementer choices (safe defaults stated):
- Trim `LemmasSummaryRow`/SQL dead `firstVerseKey` aggregation, or leave it (default: **leave**, minimal diff).
- New words-list input named `kind` vs `wordView` (default: **`kind`**, fed from `panelState().wordView`).
- Rename internal reader field `DisplayTextUthmani`→`DisplayText` (default: **rename**, cosmetic).
- Ship as two PRs (Phase 1; Phases 2–6) vs one (default: **two**).
```
