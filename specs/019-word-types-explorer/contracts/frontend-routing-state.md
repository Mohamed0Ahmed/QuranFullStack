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
| `tableView` | `words`, `roots`, `stems`, `lemmas` (Feature 022 — table-view tabs) | `words` |
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
- Changing `type`, `childCode`, `case`, `tense`, `voice`, `sort`, or `tableView` resets list `page` to `1` and clears selection.
- Missing or invalid `tableView` defaults to `words`; existing URLs without `tableView` keep working unchanged.
- When `tableView !== 'words'`, `word`/`contextCode`/`view`/`detailPage`/`location`/`column` are dropped
  from parsed state even if a stale or foreign deep link supplies them — grouped views have no word-row
  selection concept, so a URL like `?tableView=roots&word=123&contextCode=PN` renders the roots view
  with no selection instead of attempting to select a nonexistent row.
- Clearing `childCode` back to the parent (`selectChild(null)`) and switching `type` (`selectType`) both
  reset `tableView` to `words` — a grouped tab never lingers on a no-leaf scope that has nothing to
  aggregate.
- A valid positive selected row that the backend no longer resolves renders a controlled panel not-found state; the table remains usable.
- Page sizes are implementation constants, not URL params.

## Deep-Link Builder

```text
buildWordTypesDeepLink({
  type,
  childCode,
  tableView,
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

## Table-View Tabs (Feature 022)

When a table scope is selected (a leaf/`childCode`, or `type=inl`), a tab row above the table switches
the same filtered scope between four aggregation levels via the `tableView` query param, in RTL order:

```text
كلمات (words) | جذور (roots) | أصول (stems) | صيغ (lemmas)
```

- Tabs are hidden when no table scope is selected (parent node, no leaf chosen) — there is nothing to
  aggregate.
- The list always loads from `GET .../word-types/table` (E2b); `GET .../word-types/words` (E2) stays
  reserved for existing shareable deep links.
- Selecting a tab resets `page` to `1`, clears any word-row selection, and preserves the active
  `type`/`childCode`/`case`/`tense`/`voice` filters.
- Outside `tableView=words`, the details panel is hidden and the table expands to full width; grouped
  rows and their counts are **noninteractive** (no row click, no count-chip drilldown, no selected
  state) — grouped-row detail views are out of MVP.
- Rows whose `kind` does not match the active `tableView` are never rendered (defense-in-depth against
  a stale response painting under the wrong tab).

## Table and Selection Behavior

Columns (`tableView=words`):

```text
الكلمة · النوع · الجذر · الأصل · الصيغة · المواضع · الآيات · السور
```

`الأصل` (stem) and `الصيغة` (lemma) render the neutral placeholder when the backend returns null or the
v1 scope defers those winner queries.

Grouped-view columns (`tableView=roots|stems|lemmas`): `<dimension> · المواضع · الآيات · السور`, where
`<dimension>` is `الجذر` (roots), `الأصل` (stems), or `الصيغة` (lemmas) — a single dimension column,
no root/type/stem/lemma meta columns, since the row itself *is* that dimension.

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
- `tableView` URL parse/build: missing → `words`; invalid → `words`; valid round-trips; appears in the
  documented param order; a direct grouped URL with stale `word`/`contextCode` parses with no selection.
- Table-view tab switching resets page to `1`, clears selection, and changes the request/cache key
  (triggers reload); `selectType`/`selectChild(null)` reset `tableView` to `words`.
- Grouped rendering: dimension column + three counts per view, noninteractive (no row button, no count
  click, no selected state); rows whose `kind` mismatches the active `tableView` are skipped.
- Hidden/restored details panel and full-width table layout across `tableView` transitions.
- Table-view tabs hidden without a table scope; RTL roving-tab keyboard behavior (`ArrowLeft`=next,
  `ArrowRight`=previous, `Home`/`End`).
- Corrected stem/lemma header and tab-label terminology.
