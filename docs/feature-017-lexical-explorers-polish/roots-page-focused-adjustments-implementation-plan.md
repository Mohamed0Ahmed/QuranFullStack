# Roots Page — Focused Adjustments Implementation Plan

Feature: `017-lexical-explorers-polish`
Scope: Roots Explorer page only (backend read responses + Roots/shared Words frontend), plus two cross-cutting Words polish items (centralized labels, subtle shared table-header styling).
Status of source state: derived from the live repository on branch `017-lexical-explorers-polish`.

---

## 1. Verdict

`READY_FOR_IMPLEMENTATION`

All eight response-cleanup decisions, the simple/tashkeel display fix, the lemma/stem new-tab anchors, the label centralization, and the header-styling change are implementable against the current code with sensible, evidence-backed defaults. The one design decision that needed resolving — the ayah-match DTO/components are **shared** across Roots/Lemmas/Stems/Unique-Words, so the `isMatched` reshape (E4) cannot mutate the shared contract — is resolved below by adding a **Roots-scoped DTO plus a boundary adapter**, leaving the other three explorers byte-identical. See §11.

---

## 2. Scope

### Backend files expected to change
- `application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/Responses/RootListItemDto.cs`
- `application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/Responses/RootSummaryDto.cs`
- `application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/Responses/RootWordItemDto.cs`
- `application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/Responses/RootAyahMatchDto.cs` (restructure + add Roots-scoped word record)
- `application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/Responses/RootSurahsResponse.cs`
- `application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/Responses/RootMissingSurahsResponse.cs`
- `application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/Responses/RootLemmasResponse.cs`
- `application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/Responses/RootStemsResponse.cs`
- `infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/EfRootsReader.cs` (display fix + all response reshapes)
- `infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/RootsListDerivation.cs` and `RootSummaryRow` (drop `FirstVerseKey` mapping/column — same Roots reads folder)
- `infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/RootsWordsDerivation.cs` (only if it references the renamed field; pagination logic itself unchanged)
- `infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Roots/CachedRootsReader.cs` — **no field edits expected** (it passes DTOs through); verify it still compiles after type changes.

> Controllers, `GetRoot*Outcome`/`GetRoot*Handler`, and `IRootsReader` keep their **signatures**; they carry the response objects unchanged. Only the record shapes they wrap change. Verify each `switch` still compiles.

### Frontend files expected to change
- `src/app/features/words/models/roots.models.ts` (DTO reshapes, new Roots-scoped ayah word type, `toRootSummary`)
- `src/app/features/words/components/root-words-list/root-words-list.component.ts` + `.html` (`displayTextUthmani` → `displayText`)
- `src/app/features/words/components/root-lemmas-list/root-lemmas-list.component.ts` + `.html` (new-tab anchors)
- `src/app/features/words/components/root-stems-list/root-stems-list.component.ts` + `.html` (new-tab anchors)
- `src/app/features/words/utils/verse-key.ts` (new — `parseVerseKey` helper)
- `src/app/features/words/utils/root-ayah-match.mapper.ts` (new — Roots→shared `AyahMatchDto` adapter)
- `src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.ts` + `.html` (map Roots ayahs to shared shape before the two `qd-ayah-matches-list` bindings)
- `src/app/features/words/models/roots.labels.ts` (new anchor aria labels + any inline strings moved here)
- Label centralization (Phase 5) also touches `lemmas.labels.ts`, `stems.labels.ts`, `unique-words.labels.ts`, and the table/detail components of all four explorers + shared `ayah-matches-list` for any remaining inline Arabic strings.
- Header styling (Phase 6): the global theme tokens stylesheet (where `--qd-surface*` are declared) + the `__header` rules in `roots-table`, `lemmas-table`, `stems-table`, `unique-words-table`, and the list headers (`root-words-list`, `root-lemmas-list`, `root-stems-list`, `surah-occurrences-list`, `missing-surahs-list`).

### Tests expected to change
Backend (`tests/QuranDashboard.Tests/Quran/WordsRoots/`):
- `RootsListReadTests.cs`, `RootsWordsReadTests.cs`, `RootsAyahsReadTests.cs`, `RootsSurahsReadTests.cs`, `RootsLemmasStemsReadTests.cs`
- `RootsCacheReadTests.cs`, `RootsLoggingTests.cs` (only if they reference changed fields/types)
- `RootsExplorerTestFixture.cs` (seed distinct vowelled vs unvowelled unique-word text so the display fix is assertable)

Frontend (co-located `*.spec.ts`):
- `root-words-list.component.spec.ts`, `root-lemmas-list.component.spec.ts`, `root-stems-list.component.spec.ts`
- `roots-explorer-page.component.spec.ts`
- new specs: `verse-key.spec.ts`, `root-ayah-match.mapper.spec.ts`
- `roots-table.component.spec.ts` and any spec asserting `firstVerseKey`.

### Docs/report files expected to change
- This plan file only. No report folders are created by this task.

---

## 3. Current behavior summary (the five issue groups)

**A. Simple/tashkeel display bug.** `EfRootsReader.LoadGroupedRootWordsAsync` groups by `w.UniqueSimpleWordId` (simple) or `w.UniqueTashkeelWordId` (tashkeel) — the **id selection is correct** — but the displayed text is always `w.TextUthmani` (the raw `quran_words` Uthmani text). So both `بدون تشكيل` and `بالتشكيل` render identical vowelled text. Confirmed root cause matches the prior inspection.

**B. Lemma/stem navigation.** `root-lemmas-list` and `root-stems-list` render each item as a non-interactive `<div role="listitem">`. There is no navigation to the Lemmas/Stems explorers. (For comparison, `root-words-list` already renders real `<a target="_blank">` deep links to Unique Words, so the anchor pattern exists in-feature.)

**C. Labels.** Each explorer already has a `*.labels.ts` constants file and most headers read from it, but some Arabic strings are still inline in templates (e.g. pagination `ariaLabel="تصفّح كلمات الجذر"`, `ariaLabel="تصفّح الآيات"`, and the surah/missing list headers). No DB/admin text exists yet.

**D. Table headers.** Explorer table headers are styled per-component (`.roots-table__header`, list `__header` rules) over `--qd-surface`-family tokens; there is no shared "header background" token, and the current background reads too light.

**E. Roots responses.** Carry extra fields the page does not need:
- List/Summary items expose `firstVerseKey`.
- Root word items expose `firstVerseKey` and name the text field `displayTextUthmani`.
- Ayah matches expose `ayahNumber`, `surahNumber`, `matchedQuranWordIds`, and per-word `quranWordId` + `isAyahMarker`, and **include ayah-marker words** in `words`.
- Mentioned/missing surah, lemmas, and stems responses wrap their arrays in `{ id, rootText, <count> }` metadata.

---

## 4. Approved target behavior

### A. Simple/tashkeel display
- `بدون تشكيل` (simple) displays **unvowelled** text sourced from the unique **simple** word.
- `بالتشكيل` (tashkeel) displays **vowelled** text sourced from the unique **tashkeel** word.
- Display columns mirror the Unique Words explorer for consistency:
  - simple → `unique_simple_words.text_uthmani_simple` (unvowelled, Uthmani orthography)
  - tashkeel → `unique_tashkeel_words.text_uthmani` (vowelled)

### B. Lemma/stem anchors
- Lemma item → `<a href="/dashboard/words/lemmas?lemma={lemmaId}" target="_blank" rel="noopener noreferrer">`.
- Stem item → `<a href="/dashboard/words/stems?stem={stemId}" target="_blank" rel="noopener noreferrer">`.
- Built from existing `buildLemmasDeepLink({ lemmaId })` / `buildStemsDeepLink({ stemId })` + `deepLinkToHref` (these already emit exactly those paths/params). No new routes.

### C. Labels
- All Words-area table/header/anchor labels resolved from `*.labels.ts` constants; identical Arabic wording; no DB/admin layer, no i18n framework.

### D. Header styling
- A shared, subtle, slightly darker header background + clearer text, driven by one shared token, applied across roots/unique-words/lemmas/stems tables and the Roots detail list headers. Not navy, not heavy, no Roots-only hardcode.

### E. Response shapes (target)

Root list item / Root summary — `firstVerseKey` removed:
```json
{ "id": 1, "rootText": "...", "occurrencesCount": 0, "ayahsCount": 0, "surahsCount": 0,
  "simpleWordsCount": 0, "tashkeelWordsCount": 0, "lemmasCount": 0, "stemsCount": 0 }
```

Root word item — `firstVerseKey` removed, `displayTextUthmani` → `displayText`:
```json
{ "uniqueWordId": 10, "kind": "simple", "displayText": "بسم", "occurrencesCount": 3 }
```

Root ayah match — trimmed; marker words excluded; per-word `isMatched`:
```json
{
  "ayahId": 1,
  "verseKey": "1:1",
  "surahNameArabic": "الفاتحة",
  "pageNumber": 1,
  "words": [ { "textUthmani": "بِسْمِ", "isMatched": true } ]
}
```

Mentioned surahs — wrapper metadata removed:
```json
{ "surahs": [ { "surahNumber": 1, "nameArabic": "الفاتحة", "occurrencesInSurah": 3 } ] }
```

Missing surahs — wrapper metadata removed:
```json
{ "surahs": [ { "surahNumber": 2, "nameArabic": "البقرة" } ] }
```

Root lemmas — wrapper metadata removed:
```json
{ "lemmas": [ { "lemmaId": 100, "lemmaText": "رَحْمَة", "occurrencesCount": 2 } ] }
```

Root stems — wrapper metadata removed:
```json
{ "stems": [ { "stemId": 200, "stemText": "حَامَّة", "occurrencesCount": 1 } ] }
```

---

## 5. Implementation phases

- **Phase 1 — Backend response cleanup + simple/tashkeel display fix** (§6).
- **Phase 2 — Backend tests** (§8).
- **Phase 3 — Frontend models/API/facade/templates** (§7): DTO reshapes, `displayText` rename, ayah adapter + `verseKey` helper.
- **Phase 4 — Lemma/stem new-tab anchors** (§7.B).
- **Phase 5 — Centralized labels** (§7.D).
- **Phase 6 — Subtle shared header styling** (§7.E).
- **Phase 7 — Verification** (§9).

Recommended order: Phase 1 → 2 (backend green) → 3 → 4 (Roots functional) → 5 → 6 → 7. Phases 5 and 6 are independent and may be done in parallel after Phase 3.

---

## 6. Detailed backend plan

### 6.1 DTO/record edits (Application.Abstractions, Roots/Responses)

| Record | Remove | Rename | Keep |
|---|---|---|---|
| `RootListItemDto` | `FirstVerseKey` | — | id, rootText, occurrencesCount, ayahsCount, surahsCount, simpleWordsCount, tashkeelWordsCount, lemmasCount, stemsCount |
| `RootSummaryDto` | `FirstVerseKey` | — | (same eight counts as above) |
| `RootWordItemDto` | `FirstVerseKey` | `DisplayTextUthmani` → `DisplayText` | uniqueWordId, kind, occurrencesCount |
| `RootSurahsResponse` | `Id`, `RootText`, `SurahsCount` | — | `Surahs` (+ `RootSurahItemDto` unchanged) |
| `RootMissingSurahsResponse` | `Id`, `RootText`, `MissingSurahsCount` | — | `Surahs` |
| `RootLemmasResponse` | `Id`, `RootText`, `LemmasCount` | — | `Lemmas` (+ `RootLemmaItemDto` unchanged) |
| `RootStemsResponse` | `Id`, `RootText`, `StemsCount` | — | `Stems` (+ `RootStemItemDto` unchanged) |

`RootAyahMatchDto` — restructure and add a **Roots-scoped** word record (do **not** reuse the shared `AyahWordForHighlightDto`):
```csharp
public sealed record RootAyahMatchDto(
    int AyahId,
    string VerseKey,
    string SurahNameArabic,
    short PageNumber,
    IReadOnlyList<RootAyahWordDto> Words);

public sealed record RootAyahWordDto(
    string TextUthmani,
    bool IsMatched);
```
Removed from `RootAyahMatchDto`: `SurahNumber`, `AyahNumber`, `MatchedQuranWordIds`, and the shared-DTO `Words` element fields `QuranWordId` + `IsAyahMarker`. Kept: `AyahId` (row/track identity), `VerseKey`, `SurahNameArabic`, `PageNumber`.

> **`pageNumber` is retained deliberately** — it is required for Mushaf deep-link navigation without an extra lookup.

### 6.2 `EfRootsReader` edits

**Display fix (`LoadGroupedRootWordsAsync`).** Project the display text from the unique-word table that matches the kind, not from `quran_words.text_uthmani`:
- For `RootWordKind.Simple`: join `quran_words.unique_simple_word_id` → `unique_simple_words`, select `TextUthmaniSimple`.
- For `RootWordKind.Tashkeel`: join `quran_words.unique_tashkeel_word_id` → `unique_tashkeel_words`, select `TextUthmani`.

Keep the existing grouping/ordering by the unique-word id and first-occurrence (surah, ayah, word). The grouped row's display text now comes from the joined unique-word row. Emit `RootWordItemDto(uniqueWordId, kindKey, displayText, occurrencesCount)` (no `FirstVerseKey`). The internal `$"{surah}:{ayah}"` first-verse-key computation is dropped from the projection.

**Ayah matches (`GetRootAyahMatchesAsync`).**
- Continue computing the matched `quran_words` id set per ayah (`matchedIdsByAyah`) — but use it only to derive each word's `IsMatched`; do not emit it.
- When projecting `words`, **filter out ayah-marker words** (`where !w.IsAyahMarker`) so markers are never returned.
- Map each remaining word to `new RootAyahWordDto(w.TextUthmani, matchedSet.Contains(w.QuranWordId))`.
- `ResolveAyahPageNumber(...)` is unchanged and still feeds `PageNumber`.
- Build `RootAyahMatchDto(ayah.AyahId, ayah.VerseKey, ayah.SurahNameArabic, pageNumber, words)` — drop `SurahNumber`, `AyahNumber`, `MatchedQuranWordIds`.

**Surahs / missing / lemmas / stems readers.** Construct the trimmed responses directly: `new RootSurahsResponse(surahs)`, `new RootMissingSurahsResponse(missingSurahs)`, `new RootLemmasResponse(lemmas)`, `new RootStemsResponse(stems)`. The `root` existence lookup stays (it still gates `null`/NotFound), but `root.RootText` and the counts are no longer placed in the response.

**List/summary derivation.** In `RootsListDerivation.ToPage`/`ToSummary`, stop mapping `FirstVerseKey` into `RootListItemDto`/`RootSummaryDto`. Remove `FirstVerseKey` from `RootSummaryRow` and from the `LoadWholeSummaryAsync` SQL `SELECT` (the `a_first`/`w_first`/`first_m` joins exist only to compute `first_verse_key`; once the field is gone they can be dropped — **optional**, but preferred for cleanliness since nothing else uses them; if any other consumer references them, leave the joins and just drop the projected column).

### 6.3 Caching / handlers / controllers
- `CachedRootsReader`: passes the same response/DTO types through cache; no field edits. Recompile and confirm the `TryGetValue<...>` generic types still match the (same-named) records.
- `GetRoot*Handler` / `GetRoot*Outcome`: carry the response objects; no field references to removed members expected — recompile and confirm.
- `RootsController`: returns the responses verbatim inside `ApiResponse<T>.Ok(...)`; no shape logic. Recompile and confirm every `switch` arm still binds.

### 6.4 Backend non-actions
- No migrations, no importers, no Quran data mutation, no index changes.
- No edits to the **shared** `AyahWordForHighlightDto`, `UniqueWordAyahMatchDto`, `LemmaAyahMatchDto`, `StemAyahMatchDto`, or their readers — Lemmas/Stems/Unique-Words responses stay identical.

---

## 7. Detailed frontend plan

### 7.A Models (`roots.models.ts`)
- `RootListItemDto`, `RootSummaryDto`: remove `firstVerseKey`. Update `toRootSummary` to stop copying it. Drop `firstVerseKey` from `RootListItemViewModel` consumers if any.
- `RootWordItemDto`: rename `displayTextUthmani` → `displayText`; remove `firstVerseKey`.
- Replace `export interface RootAyahMatchDto extends AyahMatchDto {}` with a **Roots-scoped** shape (it must no longer inherit the shared `AyahMatchDto`, which keeps `ayahNumber`/`matchedQuranWordIds` for the other explorers):
  ```ts
  export interface RootAyahWordDto { textUthmani: string; isMatched: boolean; }
  export interface RootAyahMatchDto {
    ayahId: number;
    verseKey: string;
    surahNameArabic: string;
    pageNumber: number;
    words: RootAyahWordDto[];
  }
  ```
- `RootSurahsDto`, `RootMissingSurahsDto`, `RootLemmasDto`, `RootStemsDto`: remove `id`, `rootText`, and the `*Count` wrapper fields; keep only the arrays (`surahs` / `lemmas` / `stems`). Item interfaces unchanged.

`roots.api.ts` needs no signature change (generics flow the new shapes).

### 7.B Lemma/stem new-tab anchors
- `root-lemmas-list.component.ts`: import `deepLinkToHref` and `buildLemmasDeepLink`; add a `rows` computed mapping each `RootLemmaItemDto` to `{ item, href: deepLinkToHref(buildLemmasDeepLink({ lemmaId: item.lemmaId })) }`; add an `openLemmaLabel` from `roots.labels.ts`.
- `root-lemmas-list.component.html`: change each `<div role="listitem">` row into
  ```html
  <a class="root-lemmas-list__row qd-interactive-surface"
     [attr.href]="row.href" [attr.aria-label]="openLemmaLabel"
     target="_blank" rel="noopener noreferrer" data-testid="root-lemma-item"> … </a>
  ```
  (drop `role="listitem"`/`role="list"` since anchors are natively semantic; keep the same row inner markup).
- `root-stems-list.*`: identical pattern with `buildStemsDeepLink({ stemId })` and `openStemLabel`.
- Use `rel="noopener noreferrer"` (per decision B) — note this is stricter than the existing unique-word anchor's `rel="noopener"`, which is intentional and left untouched.

### 7.C Ayah matches: boundary adapter + verseKey helper
The shared `qd-ayah-matches-list` / `qd-highlighted-ayah` consume the shared `AyahMatchDto` (`matchedQuranWordIds` + per-word `quranWordId`/`isAyahMarker`). To keep those components and the other three explorers untouched, map the trimmed Roots response into the shared shape at the Roots boundary:

- New `utils/verse-key.ts`:
  ```ts
  export function parseVerseKey(verseKey: string): { surahNumber: number; ayahNumber: number } {
    const [s, a] = verseKey.split(':');
    return { surahNumber: Number(s), ayahNumber: Number(a) };
  }
  ```
- New `utils/root-ayah-match.mapper.ts` — `mapRootAyahMatchToShared(match: RootAyahMatchDto): AyahMatchDto`:
  - `ayahNumber` ← `parseVerseKey(match.verseKey).ayahNumber`
  - `words` ← `match.words.map((w, i) => ({ quranWordId: i, textUthmani: w.textUthmani, isAyahMarker: false }))` (markers already excluded by the backend)
  - `matchedQuranWordIds` ← indices where `w.isMatched`
  - carry `ayahId`, `verseKey`, `surahNameArabic`, `pageNumber`.
  - The highlight is therefore still driven by `isMatched`.
- `roots-explorer-page.component.ts`: add a computed that maps the panel's `RootAyahMatchDto` page to a `PagedResultDto<AyahMatchDto>` via the mapper, and bind that computed to both `qd-ayah-matches-list` usages (desktop + mobile). State (`RootsPanelState.ayahs`) can remain typed as the Roots shape; only the view binding is mapped. (Alternative: map inside `roots-detail-view.loader`/facade so state holds the shared shape — heavier type churn, not recommended.)

### 7.D Centralized labels (Phase 5)
Keep the existing per-feature `*.labels.ts` convention; do not introduce DB/admin text or i18n.
- `roots.labels.ts`: add `ROOTS_OPEN_LEMMA_LABEL`, `ROOTS_OPEN_STEM_LABEL`; move remaining inline Roots strings here, e.g. the pagination aria labels `'تصفّح كلمات الجذر'` (root-words-list) and `'تصفّح الآيات'` (ayah-matches-list), and the surah/missing list headers.
- Audit and move any inline table/header Arabic strings in the Lemmas, Stems, and Unique-Words tables/detail components into their respective `*.labels.ts`; promote genuinely shared strings (already partly done via `ROW_NUMBER_HEADER`, `AYAH_REF_LABEL`, `MUSHAF_PAGE_REF_LABEL`, `OPEN_AYAH_IN_MUSHAF_LABEL` in `unique-words.labels.ts`) to a clearly shared constants location and import them.
- **Constants structure**: one `*.labels.ts` per feature for feature-specific labels; shared cross-feature labels live in the existing shared labels module that the four features import. Each constant is a plain exported string (or a small typed record like `ROOTS_COLUMN_HEADERS`), so a future move to DB/admin text is a localized swap.
- Hard rule: identical Arabic wording before/after; this phase only relocates/reuses strings.

### 7.E Subtle shared header styling (Phase 6)
- Add shared tokens in the global theme stylesheet next to the `--qd-surface*` definitions, e.g.:
  ```css
  --qd-explorer-table-header-bg: color-mix(in oklch, var(--qd-primary) 6%, var(--qd-surface-elevated));
  --qd-explorer-table-header-fg: var(--qd-text-strong, currentColor);
  ```
  Subtle tint (≈6–8% primary), not navy, calm. Tune the percentage during review.
- Reference the tokens from each explorer table header rule (`.roots-table__header`, `.lemmas-table__header`, `.stems-table__header`, `.unique-words-table__header`) and the Roots detail list headers (`.root-words-list__header`, `.root-lemmas-list__header`, `.root-stems-list__header`, surah/missing list headers): `background: var(--qd-explorer-table-header-bg); color: var(--qd-explorer-table-header-fg);`.
- No per-component hardcoded colors; the change is centralized in the token so all four tables move together.

---

## 8. Test plan

### Backend (`tests/QuranDashboard.Tests/Quran/WordsRoots/`)
- **Simple words are unvowelled**: seed a unique simple word whose `text_uthmani_simple` has no harakat and whose `quran_words.text_uthmani` has harakat; assert `displayText` for `kind=simple` equals the unvowelled text and contains no tashkeel marks (`RootsWordsReadTests`). Requires `RootsExplorerTestFixture` to seed distinct values.
- **Tashkeel words are vowelled**: assert `displayText` for `kind=tashkeel` equals `unique_tashkeel_words.text_uthmani` (vowelled) (`RootsWordsReadTests`).
- **No `firstVerseKey`**: list, summary, and root-word items no longer expose `firstVerseKey` (compile-level field removal + assertion adjustments in `RootsListReadTests`, `RootsWordsReadTests`).
- **Ayah matches trimmed**: response no longer exposes `ayahNumber`, `surahNumber`, `matchedQuranWordIds`, or per-word `quranWordId`/`isAyahMarker` (`RootsAyahsReadTests`).
- **Ayah words expose `textUthmani` + `isMatched`**: at least one matched and one unmatched word with correct `isMatched` (`RootsAyahsReadTests`).
- **Marker words excluded**: an ayah whose raw words include a marker returns `words` with the marker absent (`RootsAyahsReadTests`).
- **`pageNumber` remains**: assert the kept, correct `pageNumber` on an ayah match (`RootsAyahsReadTests`).
- **Wrapper metadata removed**: mentioned/missing surahs, lemmas, stems responses contain only their arrays — no `id`/`rootText`/`*Count` (`RootsSurahsReadTests`, `RootsLemmasStemsReadTests`).
- Adjust `RootsCacheReadTests`/`RootsLoggingTests` only where they reference changed fields.
- Test-data safety: continue constructing real DTOs/entities from the fixture; keep Quranic text source-safe.

### Frontend (`*.spec.ts`)
- **Lemma anchor**: `root-lemmas-list.component.spec.ts` asserts the rendered anchor has `href="/dashboard/words/lemmas?lemma=100"`, `target="_blank"`, `rel="noopener noreferrer"`.
- **Stem anchor**: `root-stems-list.component.spec.ts` asserts `href="/dashboard/words/stems?stem=200"`, `target="_blank"`, `rel="noopener noreferrer"`.
- **verseKey helper**: `verse-key.spec.ts` — `parseVerseKey('2:255')` → `{ surahNumber: 2, ayahNumber: 255 }`.
- **Ayah mapper**: `root-ayah-match.mapper.spec.ts` — `isMatched` words map into `matchedQuranWordIds`; `ayahNumber` derived from `verseKey`; `pageNumber`/`verseKey`/`surahNameArabic` carried; markers absent (no `isAyahMarker:true` produced).
- **Display rename**: `root-words-list.component.spec.ts` renders `displayText` (no `displayTextUthmani` reference remains).
- **Labels unchanged**: assert the visible Arabic header/anchor text equals the centralized constants (value-equality), proving wording is unchanged after centralization.
- Update any spec asserting `firstVerseKey` on roots list/summary/word DTOs.
- Test-code self-check: behavior-focused, real DTOs constructed, no framework-guarantee tests, data-driven anchor variants where natural.

---

## 9. Verification commands

Focused first, then builds.

Backend (from repo root or `Backend/`):
```bash
# focused Roots tests
dotnet test Backend/QuranDashboard.sln --filter "FullyQualifiedName~WordsRoots"
# backend build
dotnet build Backend/QuranDashboard.sln -c Debug
```

Frontend (from `Frontend/quran-dashboard-ui/`):
```bash
# focused frontend tests — KEEP the worker cap (omitting it can OOM/freeze the machine)
VITEST_MAX_FORKS=2 npm test -- --run roots
# (also run the words utils/components specs: verse-key, root-ayah-match.mapper, root-lemmas-list, root-stems-list, root-words-list, roots-explorer-page)
# frontend build
npm run build
```

> Use the exact filter/command forms documented in `Backend/CLAUDE.md` and `Frontend/quran-dashboard-ui/CLAUDE.md`. The `VITEST_MAX_FORKS` cap is mandatory per the known frontend test-worker constraint.

---

## 10. Non-goals (explicit)

- **No migrations.**
- **No importers.**
- **No Quran data mutation.**
- **No new routes** (lemma/stem anchors reuse existing `/dashboard/words/lemmas` and `/dashboard/words/stems`).
- **No response cleanup beyond the approved Roots responses** — Lemmas/Stems/Unique-Words responses and the shared `AyahWordForHighlightDto`/`*AyahMatchDto` stay untouched.
- **No Word Types Explorer work.**
- **No DB/admin-editable label system and no i18n framework** — labels are only centralized into constants.
- No wider DTO cleanup; no Arabic wording changes; no dark-navy/heavy header restyle.

---

## 11. Risks / notes

- **Shared ayah contract (primary design decision).** `AyahMatchDto`, `AyahWordForHighlightDto`, `qd-ayah-matches-list`, and `qd-highlighted-ayah` are shared by all four explorers. E4 is Roots-only, so this plan adds a **Roots-scoped DTO** (backend) + a **frontend boundary adapter** (`mapRootAyahMatchToShared`) instead of changing the shared contract. Trade-off: the shared component still renders via `matchedQuranWordIds` (synthesized from `isMatched`) rather than reading `isMatched` directly. The rejected alternative — reshape the shared DTO/components to consume `isMatched` and migrate Lemmas/Stems/Unique-Words backends too — is out of scope ("Roots page only", "do not broaden"). If a later feature unifies all explorers on `isMatched`, the adapter becomes the natural deletion point.
- **`displayText` rename is a contract change.** Confirm no other consumer (mappers, specs, mushaf cross-links) reads `displayTextUthmani` before deleting it; the search scope above shows it is read only by `root-words-list`.
- **`firstVerseKey` removal SQL.** Dropping the `a_first`/`w_first`/`first_m` joins is optional cleanup; if any other derivation reads them, drop only the projected column to avoid behavioral risk to ordering (ordering uses `first_word_order_in_mushaf`, not the verse key).
- **Display-column choice.** Simple→`text_uthmani_simple`, tashkeel→`text_uthmani` mirrors the Unique Words reader so the two explorers render consistently; if product later wants imlaei-simple for "simple", it is a one-column swap plus a fixture/test update.
- **`rel` mismatch.** New lemma/stem anchors use `rel="noopener noreferrer"` (decision B) while the existing unique-word anchor uses `rel="noopener"`. This is intentional and limited to the new anchors.
- **Header token tuning.** The exact tint percentage for `--qd-explorer-table-header-bg` is a visual judgment; verify against `DESIGN.md` (calm, reverent, not enterprise-greige) during review.
- **Caching.** `CachedRootsReader` caches the response objects; after the reshape the cached payloads are smaller — no key/TTL change needed, but clear any persisted cache between manual checks so stale full-shape payloads are not served.
```
