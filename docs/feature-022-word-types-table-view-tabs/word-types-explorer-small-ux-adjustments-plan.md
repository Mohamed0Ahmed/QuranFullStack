# Word Types Explorer Small UX Adjustments Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: use `superpowers:executing-plans` to execute this plan in one pass. Keep one final commit boundary; do not create a worktree or reopen the approved product decisions.

**Goal:** Separate parent browsing, URL-backed list scope, and URL-backed detail scope; make statistics the only detail-opening actions in words/roots/stems/lemmas; keep the exact open-detail row visibly active; and remove the repeated detail summary card without changing backend contracts or the stable shell.

**Architecture:** Parent browsing stays local to the filter. The list query continues to own `type`, `childCode`, `case`, `tense`, and `voice`; selecting a child changes only those list fields plus list `page`. A separately parsed five-field detail-scope snapshot travels with every selected word/grouped identity, detail `view`, and `detailPage`. Detail loaders reconstruct requests exclusively from that snapshot, never from the current list scope; word loaders consume only its existing feature projection, while exact active-row restoration also consumes stored type/child. Every explicit statistic selection replaces the identity and copies the current list scope; a later list-scope change leaves those detail keys untouched. Angular Router merge/history then restores different list and detail scopes on refresh, direct loading, and Back/Forward without facade-memory exceptions.

**Tech stack:** Angular 20 standalone components, Signals/`linkedSignal`, Angular Router query params, RxJS facades, Vitest through the Angular unit-test builder, SCSS.

## Global constraints and locked behavior

- Frontend-only. Do not change Backend code, endpoints, DTOs, request shapes, cache contracts, migrations, packages, or Quran data. If implementation proves an API/contract change is necessary, stop instead of expanding this plan.
- A parent with children only changes the locally browsed child panel. It performs no URL navigation, list/detail request, table transition, prompt, or detail clear. The childless `inl` node remains the directly committable leaf.
- List scope is `type + childCode + case + tense + voice`. Selecting a child commits that scope, resets list `page` to 1, preserves `tableView`, and does not write or clear the selected identity, `view`, `detailPage`, or any detail-scope key.
- Detail scope is the exact query scope captured by the last explicit word/root/stem/lemma action. It remains unchanged when list scope changes. The detail facade must restore and load from the stored detail fields, with no inference or fallback to `type`, `childCode`, `case`, `tense`, or `voice`.
- Clicking a new statistic replaces the selected identity, target detail `view`, canonical `detailPage`, and detail-scope snapshot from the current list query. Clearing details clears all identity/view/page/location fields and all five detail-scope key names.
- Preserve native history entries, page-one omission, and the rule that `surahs` never serializes `detailPage`. Preserve existing stable-shell, skeleton, retry, cache, focus, selected styling, RTL, and accessibility behavior.
- Every words/root/stem/lemma row container is a non-focusable presentation/selection container. Click, Enter, and Space on the row itself do nothing; do not keep `rowSelected`. Only native statistic buttons open details. Grouped mapping is `occurrences -> words`, `ayahs -> ayahs`, `surahs -> surahs`; word mapping remains exactly `occurrences/ayahs -> ayahs`, `surahs -> surahs`.
- After any statistic action, the exact selected row uses the shared `qd-is-selected` explorer-row color for as long as its detail panel is open, independent of the active detail tab. A later statistic transfers selection; closing clears it; refresh/history restore it when identity and scope match. Matching is word ID + context + stored feature scope for words, or kind + numeric ID + stored grouped scope. A preserved detail from another list scope must not highlight a coincidentally equal row. Quiet hover must not override the selected color.
- Remove only the visible summary card. Keep summary requests/state because `displayText`, header labeling, not-found, loading, error, and retry orchestration still consume them. Grouped member-word rows remain display-only; skeleton/loading rows remain non-interactive.

## Final URL detail-scope contract

Add these key names in canonical order after `word`/`contextCode`/`root`/`stem`/`lemma` and before `view`:

- `detailType`
- `detailChildCode`
- `detailCase`
- `detailTense`
- `detailVoice`

Encoding is deliberately frontend-state-driven:

- A root/stem/lemma selection writes all five fields because `WordTypesApi.groupedScopeParams()` and `toGroupedRequest()` consume the full `WordTypeDetailScope`. `detailChildCode` is omitted/null only when the captured scope has no child (the valid `inl` leaf); `all` feature values remain explicit so the snapshot is complete.
- A word selection also writes all five fields. `getSummary()`, `getAyahMatches()`, and `getSurahs()` still consume only `contextCode + detailCase + detailTense + detailVoice`; `detailType/detailChildCode` are required solely to restore exact scoped active-row styling and never alter the API request.
- Missing, malformed, or type-incompatible required detail fields make the selection invalid: keep the normalized list query usable, expose no active detail selection, and issue no detail request. Legacy identity URLs without a complete detail snapshot fail closed; they must never borrow list-scope values.
- Example independent restoration: `type=noun&childCode=ADJ&tableView=roots&root=123&detailType=verb&detailChildCode=present&detailCase=all&detailTense=present&detailVoice=all&view=ayahs` loads the Noun/Adjective roots table while root 123 details query Verb/Present.

## Current-state findings

- `WordTypeFilterComponent.selectedNode` currently conflates the browsed parent with committed `selectedType`; parent clicks emit `typeSelected`, and `WordTypesExplorerFacade.selectType()` navigates, resets filters/page, and clears the selection.
- `WordTypesExplorerFacade.selectChild()` knows only `childCode`, cannot commit a child under a separately browsed parent, and calls `clearWordTypesSelection()`. Router navigation already uses `queryParamsHandling: 'merge'`, so a replacement `selectScope(type, childCode)` can preserve detail state simply by omitting its keys.
- `ParsedWordTypesQuery` and `WORD_TYPES_QUERY_KEYS` contain no independent detail-scope fields. `WordTypesDetailFacade.toSelection()` currently copies list `case/tense/voice` into word identity and `scopeFrom(parsed)` copies all five list fields into grouped selection, so refresh and history incorrectly re-scope an existing detail.
- `WordTypeDetailScope` already models the required fields exactly. `toGroupedRequest()` forwards all five to grouped summary/member-word/ayah/surah loaders and cache keys. Word summary/ayah/surah loaders use only identity `contextCode + case + tense + voice`; word selection must additionally retain the same scope for exact active-row matching. No backend or API-client change is required.
- Full scoped `isSameSelection()`/`isSamePanelUrlState()` are already correct once `toSelection()` uses stored detail fields. The prior facade-memory/identity-only comparator approach must not be implemented: route state becomes the source of truth for refresh, direct URLs, and Back/Forward.
- `WordTypesTableComponent.openCount()` already supplies the word-row statistic mapping, but word row containers also emit `rowSelected`; grouped rows use one whole-row `<button>` and default to `words`. Remove both whole-row action paths so `countOpened` is the sole detail-opening event in all four views.
- The table already accepts selected-row state and applies the shared `qd-is-selected` pattern. The page must derive that state from the route-restored detail selection and require exact identity plus equality between stored detail scope and current list scope; text equality and numeric ID alone are insufficient.
- `WordTypesExplorerPageComponent.activeSummary` feeds both the existing header and `<qd-word-type-detail-summary>`. The four summary-component files have no other consumers; summary data itself remains required.
- Existing URL sync already preserves positive numeric identities, table-view compatibility, page-one omission, and surah page removal. Extend those patterns with strict independent detail-scope parsing; do not create another URL model.
- There is no tracked `docs/feature-023-*` directory. This document stays in the existing Word Types table/details planning area; the current behavior contract is `specs/019-word-types-explorer/`.

## Exact implementation file set and symbols

### Modify

- `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types.models.ts`
  - Extend `ParsedWordTypesQuery`, `WORD_TYPES_QUERY_KEYS`, and `WORD_TYPES_SELECTION_QUERY_KEYS` with nullable parsed detail fields and the five key names.
- `Frontend/quran-dashboard-ui/src/app/features/words/models/word-types-detail.models.ts`
  - Store `WordTypeDetailScope` on word selections as well as grouped selections so route-restored highlighting can compare the full snapshot without changing word API inputs.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.ts`
  - Extend `parseWordTypesQueryParams()`, `WordTypesQueryChange`, `WORD_TYPES_QUERY_ORDER`, `buildWordTypesQueryParams()`, `clearWordTypesSelection()`, and `clearSelectionForTableView()`.
  - Add one `buildWordTypesDetailScopeQuery(selection)` encoder that emits all five fields for every selection. Strict detail parsing must not call list normalizers that default from the active list scope.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-url-sync.spec.ts`
  - Cover canonical detail encoding, independent parsing/history, fail-closed input, clearing, page-one omission, and surah paging.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.spec.ts`
  - Replace `selectedNode`, `typeSelected`, and `childSelected` with local browsed-parent state and a typed `{ type, childCode }` committed-scope event.
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-types-table/word-types-table.component.spec.ts`
  - Extend `WordTypeCountOpenedEvent`/`openCount()` to grouped rows, remove `rowSelected` and every whole-row click/keyboard action, and cover scoped selected styling for all row kinds.
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts`
  - Update `selectScope()`, `onCountOpened()`, `toGroupedSelection()`, `currentScope()`, exact scoped selected-row matching/focus restoration, `clearSelection()`, and title-only summary projection. Remove `selectRow()`/`selectWordRow()` actions. Every statistic writes `buildWordTypesDetailScopeQuery(selection)` with its identity/view fields.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-explorer.facade.spec.ts`
  - Replace `selectType()`/`selectChild()` with `selectScope(type, childCode)`; write list fields/list page only and never clear detail fields.
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail.facade.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail.facade.spec.ts`
  - Change `toSelection()`/`scopeFrom()` to require stored detail fields. Keep full `isSameSelection()` and `isSamePanelUrlState()` so route replay replaces details only when identity, detail scope, view, page, or location actually changes.
- `Frontend/quran-dashboard-ui/src/app/features/words/README.md`
- `specs/019-word-types-explorer/spec.md`
- `specs/019-word-types-explorer/contracts/frontend-routing-state.md`
- `specs/019-word-types-explorer/quickstart.md`

### Delete after removing the only page import/render

- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.scss`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-detail-summary/word-type-detail-summary.component.spec.ts`

### Guard only; no planned production edit
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.html`
- `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.spec.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-cache.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-view.loader.ts`
- `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-view.loader.spec.ts`

## Single-pass implementation sequence

1. **Write the URL and behavior regressions first.** In `word-types-url-sync.spec.ts`, lock the five-key namespace, conditional word/grouped encoding, stable order, complete independent parse, page-one/surah rules, and fail-closed malformed/incomplete snapshots. In facade/page tests, first encode the Verb/Present root-123/ayahs to Noun/Adjective sequence, explicit replacement, refresh, and history expectations. Replace superseded parent, whole-row, and summary-card assertions instead of keeping contradictory tests.

2. **Add independent route state.** Extend the model/key/change/order declarations and implement strict detail-field parsing plus `buildWordTypesDetailScopeQuery(selection)`. Required values must be present and enum-valid; grouped type/filter combinations use the same compatibility rules as list parsing but fail closed rather than defaulting from list fields. Add all detail keys to both selection-clear helpers. Preserve the existing positive-ID, table-view, `detailPage`, and surah normalization.

3. **Separate parent browsing and list commits.** Give `WordTypeFilterComponent` a `browsedType` `linkedSignal`, keep `aria-current`/selected styling tied to committed list scope, use `aria-expanded` for the browsed panel, and emit `{ type, childCode }` only for a child or `inl`. Route that event to `WordTypesExplorerFacade.selectScope()`, which normalizes cross-parent list filters and resets list page but omits all identity/detail keys so Router merge preserves them byte-for-byte.

4. **Restore details only from detail scope.** In `WordTypesDetailFacade.toSelection()`, build word identity from `detailCase/detailTense/detailVoice` and grouped `WordTypeDetailScope` from all five detail fields. Return `null` for incomplete/incompatible snapshots; do not consult list fields and do not add an in-memory preservation comparator. Existing full selection comparison, loaders, APIs, and cache keys then handle explicit replacement, refresh, direct loading, and Back/Forward correctly.

5. **Make statistics the only actions and restore exact active styling.** Render all four row kinds as non-focusable containers with no click/keydown handler, link, button wrapper, or `rowSelected` emission. Keep word statistic chips and render grouped counts with the same `WordCountChipComponent` markup/classes/ARIA. Route only `countOpened` through `onCountOpened()`: grouped rows use the approved `words/ayahs/surahs` mapping and exact numeric ID; words retain `ayahs/ayahs/surahs`. Write identity + view/page + the full encoded detail scope atomically; word API requests remain projected to their existing feature identity. Derive the table's selected row from the open panel selection, require exact kind/identity and stored-scope equality with current list scope, and retain the shared `qd-is-selected` color across tab changes, refresh, and history. Transfer it on another statistic, clear it on close, keep hover subordinate, and restore focus to the originating statistic button.

6. **Remove only the repeated summary presentation.** Remove the page import/render and delete the four summary-component files. Replace `activeSummary` only with the title text projection used by the panel header; retain summary/grouped-summary fetches, loading/error/retry/not-found behavior, tabs, lists, and paging.

7. **Update current contracts, verify once, and commit once.** Update only the README/spec/route contract/quickstart entries that describe parent commits, grouped activation, summary presentation, and URL restoration. Run the focused suites, Word Types suite, production build, diff guards, and clean/test-code self-checks. Stage only the listed files and create one final commit.

## Behavior-first regression matrix

- `word-type-filter.component.spec.ts`: browsing Noun while Verb/Present is committed changes only the expanded child panel; no scope event, URL, table, or details change. Selecting Noun/Adjective emits one committed scope; `inl`, focus, loading, and secondary-filter accessibility remain.
- `word-types-explorer.facade.spec.ts`: `selectScope('noun', 'ADJ')` writes only normalized list-scope/list-page keys, preserves `tableView`, and does not include any identity, `view`, `detailPage`, or `detail*` clearing key. Cross-parent feature normalization remains list-only.
- `word-types-url-sync.spec.ts`:
  - changing list keys while merging preserves the old five-key grouped snapshot;
  - every new statistic serializes all five current list-scope values; word API assertions remain unchanged because only the three feature fields feed its request identity;
  - clear helpers remove identity, view/page, and every detail key;
  - a URL with Noun/Adjective list scope and Verb/Present root scope parses both independently;
  - successive ParamMaps model Back/Forward with independently changing list and detail scopes;
  - missing/invalid detail type, feature, child/type compatibility, or incomplete word feature snapshot yields no valid detail selection and never falls back to list scope;
  - page 1 remains omitted and `surahs` removes `detailPage`.
- `word-types-detail.facade.spec.ts`: direct URL/refresh restores root 123 ayahs with the stored Verb/Present request while the parsed list is Noun/Adjective; Back/Forward restores the exact prior identity, detail scope, view, and page; an invalid snapshot leaves the panel closed and makes no API call; an explicit same-ID selection with a new detail scope reloads that scope.
- `word-types-table.component.spec.ts`: words/root/stem/lemma each expose three native, keyboard-operable count chips; approved mappings and exact identities are preserved; click/Enter/Space on every row container is inert and causes no duplicate event; skeletons are non-interactive. A selected input applies the shared active class/ARIA for each kind, changes to another row transfer it, null clears it, and hover selectors do not override the selected state.
- `word-types-explorer-page.component.spec.ts`:
  - Verb/Present table and populated root details remain unchanged while Noun is merely browsed;
  - selecting Noun/Adjective changes only the list request/rows and preserves root 123, ayahs, `detailPage`, the Verb/Present detail URL keys, title, active tab, content, and detail API call counts;
  - clicking a new statistic replaces identity/view/page and copies the current list query into the appropriate detail-key projection before loading;
  - word and grouped row-container click/Enter/Space cause no route navigation or detail request; only each statistic opens its mapped view once;
  - statistic selection activates the exact word/root/stem/lemma row, tab changes retain it, another statistic transfers it, close clears it, and refresh/Back/Forward restore it when list/detail scopes match;
  - a preserved detail under a different current list scope does not activate a same-text, same-word-ID/context, or same grouped numeric-ID row;
  - a fresh component/route bind restores different list/detail scopes; simulated Back/Forward restores both independently;
  - closing details clears identity/view/page/detail keys and restores focus;
  - the summary test id is absent while header, tabs, loading skeleton, error/retry, not-found, and successful detail content render.
- Diff guard: no file under `Backend/` and no frontend API/cache/loader file changes; this proves no backend/API change is introduced.

## Documentation updates

- `Frontend/quran-dashboard-ui/src/app/features/words/README.md`: record browsed parent versus list scope versus detail scope; exact conditional key usage; statistic-only actions for all four views; exact scoped active-row color behavior; removed summary card; unchanged shell/cache/loading/retry/paging invariants.
- `specs/019-word-types-explorer/spec.md`: update only parent/child behavior (`FR-007`), grouped actions (`US6`/`FR-048`), independent refresh/direct/history restoration (`FR-053`), and tabs without the summary card (`FR-055`).
- `specs/019-word-types-explorer/contracts/frontend-routing-state.md`: add the five key names, canonical order, word/grouped presence rules, strict fail-closed validation, child-commit preservation, explicit statistic replacement, clear behavior, and the differing-scope example URL.
- `specs/019-word-types-explorer/quickstart.md`: update acceptance/manual steps and focused-suite description/count only after the final run. Do not add a report or second plan.

## Exact verification commands

```bash
cd /projects/Dashboard/App/Frontend/quran-dashboard-ui
npm test -- --include=src/app/features/words/state/word-types-url-sync.spec.ts --include=src/app/features/words/state/word-types-explorer.facade.spec.ts --include=src/app/features/words/state/word-types-detail.facade.spec.ts
npm test -- --include=src/app/features/words/components/word-type-filter/word-type-filter.component.spec.ts --include=src/app/features/words/components/word-types-table/word-types-table.component.spec.ts --include=src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts
npm test -- --include=src/app/features/words/components/word-type-details-panel/word-type-details-panel.component.spec.ts --include=src/app/features/words/state/word-types-detail-view.loader.spec.ts
npm test -- --include='src/app/features/words/**/*word-type*.spec.ts'
npm run build
```

Then from the repository root:

```bash
cd /projects/Dashboard/App
git diff --check
rg -n "WordTypeDetailSummaryComponent|qd-word-type-detail-summary|word-type-detail-summary" Frontend/quran-dashboard-ui/src/app/features/words
git diff --name-only -- Backend
git diff --name-only -- Frontend/quran-dashboard-ui/src/app/features/words/data-access/word-types.api.ts Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-cache.ts Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-view.loader.ts
git status --short
```

Expected: all focused/full Word Types tests pass; the production build completes (existing documented non-fatal SCSS budget warnings may remain); `git diff --check` is clean; summary-component search, Backend diff, and API/cache/loader diff are empty; only the listed implementation/tests/docs and this plan are changed/deleted. Run the repository-required clean-code and test-code self-checks before delivery.

## One final commit boundary

After all verification, stage only the exact listed implementation, tests, documentation, deleted summary-component paths, and this plan, then create one commit:

```bash
git commit -m "fix(word-types): refine explorer interactions"
```

Do not create intermediate commits.

## Completion boundary

The pass is complete only when parent browsing is URL/data inert; a child changes only list scope while preserving identity/view/detail page and the stored detail scope; every row container is inert and only statistics atomically replace detail identity/view/scope; the exact scoped row keeps the shared active color across tabs/refresh/history while cross-scope details never falsely select; clear removes all detail and row-selection state; refresh/direct URLs/Back/Forward independently restore list and detail scopes; invalid snapshots fail closed; keyboard, skeleton, focus, and summary-card regressions pass; page-one/surah rules remain canonical; the production build passes; and no Backend/API/cache/loader file changes.
