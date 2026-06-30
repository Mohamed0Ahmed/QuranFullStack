# Contract: Frontend Routing, State, and UI Behavior

## Route

```text
/dashboard/words/types
```

- Add `WORDS_TYPES_SEGMENT = 'types'` and `wordTypesRoutePath()` to
  `src/app/core/navigation/route-paths.ts`.
- Add a lazy `WORDS_TYPES_ROUTE` sibling in `features/words/words.routes.ts`.
- Add a Words hub card/link labelled for أنواع الكلمات.
- Route paths and query keys are stable technical values; Arabic labels may evolve independently.

## Query State

| Param | Values | Default |
|---|---|---|
| `type` | `noun`, `verb`, `particle`, `inl` | `noun` |
| `childCode` | parent-specific code (noun POS code such as `N`, `PN`, `ADJ`, `PRON`, `REL`, `DEM`, `T`, `LOC`, `TIM`, `IMPN`; or verb tense `past`, `present`, `imperative`) | none |
| `case` | `all`, `nominative`, `accusative`, `genitive`, `null` | `all` |
| `tense` | `all`, `past`, `present`, `imperative` | `all` |
| `voice` | `all`, `active`, `passive` | `all` |
| `sort` | `occurrences`, `ayahs`, `surahs`, `mushaf-order`, `alpha` | `occurrences` |
| `page` | positive integer | `1` |
| `word` | positive `tashkeelWordId` | none |
| `contextCode` | selected row context code | none |
| `view` | `ayahs`, `surahs`, `analysis` | `ayahs` when row selected |
| `detailPage` | positive integer | `1` |
| `location` | Quran word location for per-occurrence analysis | none |
| `column` | focused/active table column key | none |

Rules:

- Missing or invalid `type` defaults to `noun`; this is the no-query default page state.
- Invalid `childCode` is ignored and removed from the normalized state.
- `case` is meaningful only when `type=noun`; it is ignored for other types.
- `tense` and `voice` are meaningful only when `type=verb`; they are ignored for other types.
- `particle` and `inl` hide secondary filters.
- `inl` is a leaf: `childCode` is ignored.
- Non-positive/malformed `page` and `detailPage` normalize to `1`.
- `word` without a valid positive `contextCode` does not select a row.
- Clearing selection removes `word`, `contextCode`, `view`, `detailPage`, `location`, and `column` while preserving list filters.
- Changing `type`, `childCode`, `case`, `tense`, `voice`, or `sort` resets list `page` to `1` and clears selection.
- A valid positive selected row that the backend no longer resolves renders a controlled panel not-found state; the table remains usable.
- Page sizes are implementation constants, not URL params.

## Deep-Link Builder

```text
buildWordTypesDeepLink({
  type,
  childCode,
  case,
  tense,
  voice,
  sort,
  page,
  word,
  contextCode,
  view,
  detailPage,
  location,
  column,
})
```

Return the existing deep-link target shape:

```ts
{ path: wordTypesRoutePath(), queryParams }
```

The canonical selected-row URL includes both `word` and `contextCode`:

```text
/dashboard/words/types?type=noun&childCode=PN&word=1234&contextCode=PN&view=ayahs
```

## Page and Component State

```text
word-types-explorer-page
  -> word-types-explorer.facade
    -> word-types.api.ts
  -> word-types-detail.facade
    -> word-types.api.ts
```

The page shell owns route parsing and delegates actions. Facades own API orchestration, loading,
empty/error/not-found states, pagination, selected filters, selected row, and URL updates. Child
components are presentational and never call backend services.

## Data Loading

| UI state | Behavior |
|---|---|
| Page open | Load tree and first rows for normalized filters. |
| Filter change | Reload rows. Reload the static tree only when type/child catalogue data is stale; secondary filter changes do not request scoped tree counts. |
| Row select | Load summary for exact `word + contextCode + active feature` identity. |
| `view=ayahs` | Lazy-load paged ayah matches for exact row context. |
| `view=surahs` | Lazy-load surah distribution + missing surahs for exact row context. |
| `view=analysis` | Reuse `GET api/mushaf/words/{location}/analysis`; no request until a location is selected. |

Rules:

- No detail calls during initial table render.
- Only the active detail view loads.
- Already loaded detail views may be reused for the same row identity.
- Transport errors and backend-controlled failures are separate states.
- Missing API data is never replaced with invented Quranic content.

## Table and Selection Behavior

Columns:

```text
الكلمة · النوع · الجذر · الصيغة · الأصل · المواضع · الآيات · السور
```

`الصيغة` and `الأصل` render the neutral placeholder when the backend returns null or the v1 scope
defers those winner queries.

Secondary filters narrow the table `totalCount` and any active UI count chips derived from the current rows. They do not change the static E1 tree counts.

Count mapping:

| Interaction | Destination |
|---|---|
| Row select | `view=ayahs&detailPage=1` |
| المواضع | `view=ayahs&detailPage=1` |
| الآيات | `view=ayahs&detailPage=1` |
| السور | `view=surahs` |
| analysis action for a listed occurrence | `view=analysis&location={location}` |

Rows are selected by `tashkeelWordId + contextCode + active feature`, not by displayed text.
Zero-count destinations remain keyboard-operable when they are valid and show an empty state.

## Details Panel

Tabs/sections:

- `الآيات الخاصة بالكلمة` (`view=ayahs`)
- `السور` (`view=surahs`)
- `التحليل` (`view=analysis`)

The panel summary shows the exact row's word, subtype, case or tense/voice where applicable,
root/lemma/stem placeholders, and occurrence/ayah/surah counts. The ayah tab highlights only matched
occurrences for the row context. The analysis tab displays one selected occurrence's existing
`WordAnalysisResponse`.

Desktop uses the existing Words explorer split-view pattern with an inline-end details panel. Narrow
screens stack/collapse consistently with existing explorers. Quran/Mushaf text is never animated.

## Accessibility and RTL

- Filter buttons, expand arrows, row controls, count chips, tabs, pagination, and analysis links are
  keyboard-operable.
- The filter picker distinguishes label select from expand-arrow behavior.
- Rows expose selected state beyond color (`aria-current` or equivalent).
- Tabs use tablist semantics and RTL-aware keyboard behavior.
- Loading states use polite live regions where appropriate.
- Use logical CSS properties and existing `qd-*` tokens/classes.
- Backend IDs are not visible UI labels.

## Required Frontend Tests

- Route helper and route registration for `/dashboard/words/types`.
- URL parse/build/normalize for default noun state and all filter classes.
- Secondary filter visibility by main type.
- Filter changes reset page and clear selection.
- Deep link restores exact `word + contextCode`, including same spelling/different context cases.
- No eager detail calls on list render.
- Row/count interactions map to the expected detail view.
- Ayah highlighting receives context-scoped match data without string replacement.
- Transport error vs backend failure/not-found states.
- Missing root/lemma/stem placeholders.
- Secondary filters do not trigger scoped tree-count expectations; only row total and active UI chips reflect the secondary scope.
- Keyboard/ARIA behavior for filter picker, table rows, tabs, and narrow-screen panel.
