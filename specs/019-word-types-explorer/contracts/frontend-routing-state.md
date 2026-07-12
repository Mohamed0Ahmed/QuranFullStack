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
| `root` | positive root ID | none |
| `stem` | positive stem ID | none |
| `lemma` | positive lemma ID | none |
| `view` | `words`, `ayahs`, `surahs` | `ayahs` for word selection; `words` for grouped selection |
| `detailPage` | positive integer | internal `1`; omitted from the canonical URL at page `1` |
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
- `word` without a valid positive `contextCode` does not select a row. `root`, `stem`, and `lemma`
  must each be positive integers when their table view is active.
- Identity keys are compatible only with the matching `tableView`:

  | `tableView` | Accepted identity | Default `view` |
  |---|---|---|
  | `words` | `word` + `contextCode` | `ayahs` |
  | `roots` | `root` | `words` |
  | `stems` | `stem` | `words` |
  | `lemmas` | `lemma` | `words` |

  The parser clears every incompatible identity key. `word`, `contextCode`, `location`, and `column`
  are retained only in `words`; grouped views ignore them. Display text is never identity, and the
  generic `dim` query key is forbidden.
- Clearing selection removes `word`, `contextCode`, `root`, `stem`, `lemma`, `view`, `detailPage`,
  `location`, and `column` while preserving list filters.
- Changing `type`, `childCode`, `case`, `tense`, or `voice` resets list `page` to `1` and clears every
  selection key. Changing `tableView` resets list `page` to `1`, clears incompatible selection keys,
  and leaves the target view's identity key untouched. Sorting and list pagination do not clear a
  compatible selection.
- Missing or invalid `tableView` defaults to `words`; existing URLs without `tableView` keep working unchanged.
- `view=words` normalizes to `ayahs` for word selection. `view=surahs` always keeps internal
  `detailPage=1` and removes `detailPage` from canonical URL writes. For `words` and `ayahs`, page `1`
  is represented internally but omitted from the URL; only pages above `1` serialize `detailPage`.
- Browser back/forward restores the identity key compatible with each emitted `ParamMap`, together with
  the complete filter scope and the canonical detail view/page state.
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
  root,
  stem,
  lemma,
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

Canonical URLs use explicit selection keys and omit default page `1`:

```text
/dashboard/words/types?type=noun&childCode=PN&word=1234&contextCode=PN&view=ayahs
/dashboard/words/types?tableView=roots&root=123&view=words
/dashboard/words/types?tableView=stems&stem=456&view=ayahs&detailPage=2
/dashboard/words/types?tableView=lemmas&lemma=789&view=surahs
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

Rules:

- No detail calls during initial table render.
- Only the active detail view loads.
- Already loaded detail views may be reused for the same row identity.
- Transport errors and backend-controlled failures are separate states.
- Missing API data is never replaced with invented Quranic content.

## Grouped Detail API and Cache Contract (Task 6)

Grouped detail reads use the selected numeric dimension identity and the complete active grammatical
scope. The frontend keeps the singular selection kind (`root`, `stem`, `lemma`) and translates it to
the backend's plural route segment (`roots`, `stems`, `lemmas`):

```ts
const groupedRequest: WordTypeGroupedRequestParams = {
  kind: 'root',
  dimensionId: 4210,
  type: 'noun',
  childCode: 'PN',
  case: 'nominative',
  tense: 'all',
  voice: 'all',
};

api.getGroupedSummary(groupedRequest);
api.getGroupedMemberWords(groupedRequest, page, pageSize);
api.getGroupedAyahMatches(groupedRequest, page, pageSize);
api.getGroupedSurahs(groupedRequest);
```

- Every call sends `type`, sends `childCode` when present, and sends only concrete grammatical
  filters through the shared identity-parameter policy.
- Only member words and ayahs send `page` and `pageSize`; summary and surahs never send paging or
  `detailPage` query parameters.
- Grouped cache keys use the stable shape
  `wordtypes:grouped:{kind}:{dimensionId}:{type}:{childCode|all}:{case}:{tense}:{voice}:view:{view}`.
  The `words` and `ayahs` variants append `:p{page}`; `summary` and `surahs` do not. This isolates
  kind, ID, full scope, and view without putting API loading behavior into the cache service.

## Kind-Aware Detail Orchestration (Task 7)

`WordTypesDetailFacade` restores and loads details for all four selection kinds from the URL. It parses
the explicit selection key compatible with the active `tableView` (`word`+`contextCode`, `root`, `stem`,
or `lemma`) into a discriminated `WordTypeDetailSelection` carrying the full grammatical scope, then
loads a kind-appropriate summary and the active view:

- **Summary dispatch by kind.** A `word` selection loads the word summary/cache; a `root`/`stem`/`lemma`
  selection loads the grouped summary/cache. Word summaries populate `summary`, grouped summaries
  populate `groupedSummary` (exactly one is non-null for an active selection).
- **View dispatch by kind and view.** `WordTypesDetailViewLoader` routes `words` to grouped member words
  (grouped selection only — the word selection has no `words` view), `ayahs` to the word or grouped ayah
  endpoint, and `surahs` to the single-shot word or grouped surah endpoint. The `surahs` load ignores
  `detailPage`.
- **Default view.** A newly restored/selected word defaults to `ayahs`; a grouped selection defaults to
  `words`. `isPaginatedWordTypeView` is true for `words` and `ayahs`, false for `surahs`.
- **Internal page vs URL.** A paged view restored without a URL `detailPage` is internal page `1`; URL
  omission is never a null internal page. Only pages above `1` serialize.
- **Restoration and history.** Refresh and browser back/forward replace the selection kind, its summary,
  and the active view from the explicit URL key alone; a scope-only change for the same dimension ID
  loads a new scoped summary.
- **Stale protection.** Route-driven loads use `switchMap` to cancel superseded work, and every state
  write is additionally gated by a monotonic generation counter, so a late non-cancellable summary or
  detail response can never overwrite a newer kind/scope/view/page.
- **Not-found vs error vs retry.** An absent scoped dimension (null data or `404`) becomes a kind-aware
  `notFound` that preserves the selection without clearing list state; a transport/backend failure
  becomes a retryable `error`. `retry()` reloads the summary when it never arrived, otherwise reloads the
  active view. Failed reads are never cached (`ApiResponseCache` stores only successful responses), so a
  retry always re-issues the request.

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
- Selecting a tab resets `page` to `1`, clears incompatible selection keys, and preserves the active
  `type`/`childCode`/`case`/`tense`/`voice` filters.
- A grouped row uses its explicit numeric `root`/`stem`/`lemma` URL identity; display text is never
  serialized as selection identity.
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
| Row select | `view=ayahs` |
| المواضع | `view=ayahs` |
| الآيات | `view=ayahs` |
| السور | `view=surahs` |

Rows are selected by `tashkeelWordId + contextCode + active feature`, not by displayed text.
Zero-count destinations remain keyboard-operable when they are valid and show an empty state.

## Details Panel

Tabs/sections:

- `الآيات الخاصة بالكلمة` (`view=ayahs`)
- `السور` (`view=surahs`)

The panel summary shows the exact row's word, subtype, case or tense/voice where applicable,
root/lemma/stem placeholders, and occurrence/ayah/surah counts. The ayah tab highlights only matched
occurrences for the row context.

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
- Deep link restores exact compatible `word + contextCode` or grouped ID, including same
  spelling/different-context word cases and full grouped scope.
- No eager detail calls on list render.
- Row/count interactions map to the expected detail view.
- Ayah highlighting receives context-scoped match data without string replacement.
- Transport error vs backend failure/not-found states.
- Missing root/lemma/stem placeholders.
- Secondary filters do not trigger scoped tree-count expectations; only row total and active UI chips reflect the secondary scope.
- Keyboard/ARIA behavior for filter picker, table rows, tabs, and narrow-screen panel.
- `tableView` URL parse/build: missing → `words`; invalid → `words`; compatible explicit grouped keys
  round-trip with scope; incompatible keys are removed; the canonical order includes `root`, `stem`, and
  `lemma` and has no generic `dim` key.
- Detail paging: words/ayahs retain internal page `1` while omitting it from URLs, pages above `1`
  serialize it, and surahs always remove it. Browser back/forward replays the compatible identity.
- Table-view tab switching resets page to `1`, clears incompatible selection keys, and changes the
  request/cache key (triggers reload); `selectType`/`selectChild(null)` reset `tableView` to `words`.
- Grouped rendering: dimension column + three counts per view, noninteractive (no row button, no count
  click, no selected state); rows whose `kind` mismatches the active `tableView` are skipped.
- Hidden/restored details panel and full-width table layout across `tableView` transitions.
- Table-view tabs hidden without a table scope; RTL roving-tab keyboard behavior (`ArrowLeft`=next,
  `ArrowRight`=previous, `Home`/`End`).
- Corrected stem/lemma header and tab-label terminology.
