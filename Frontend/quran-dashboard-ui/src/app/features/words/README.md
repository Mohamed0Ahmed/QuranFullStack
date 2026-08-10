# Words feature (الكلمات) — explorers

**HOW rules:** `.architecture/FRONTEND_STRUCTURE.md`, `.architecture/API_INTEGRATION_GUIDELINES.md`
(project root). This file is the WHAT (current truth + shared pattern).

## What this feature does

Five read-only explorers over the Quran word data — **Roots, Lemmas, Stems, WordTypes,
Unique Words** — plus the Words hub. Each is a table-first split-screen page: a paginated
table on one side, a detail panel (summary + related lists + ayah matches) on the other,
with all selection/filter/paging state reflected in the URL.

## Shared pattern (each explorer repeats it)

For each `X` in `roots`, `lemmas`, `stems`, `word-types`, and `unique-words`:

- `pages/X-explorer-page/` is the routed smart component
  (`unique-words-page/` is the naming exception).
- `state/X-explorer.facade.ts` and `state/X-detail.facade.ts` orchestrate list and selection
  state; `state/X-cache.ts` caches pages/details; `state/X-url-sync.ts` owns the stable
  URL-to-state contract; and `state/X-detail-view.loader.ts` loads the selected detail view.
- `data-access/X.api.ts` owns `ApiResponse<T>` calls.
- `models/X.models.ts` and `models/X.labels.ts` own view models and Arabic labels.
  Generated wire DTOs come from `core/api/generated/` under the historical `*Dto` aliases;
  UI-only unions/request/view models remain local, and closed backend vocabularies are narrowed
  with `Omit` overlays.
- `utils/X-ayah-match.mapper.ts` maps API ayah matches to view rows.

Word Types additionally keeps
`pages/word-types-explorer-page/word-types-detail-panel.view-model.ts` as the pure derivation
layer over `WordTypesDetailFacade.panelState()`: active summary, word/ayah pages (including
empty-while-loading pages), the ayah parent frame, and mentioned/missing surah rows. The page
only wires those results into computed signals.

Shared explorer mechanics stay in `utils/explorer-table-*`,
`utils/explorer-keyboard-nav.scheduler.ts`, `utils/verse-key.ts`, and the feature's shared
`components/` table/list/panel set.

Every explorer root composes the feature-neutral `qd-page-shell qd-page-shell--split-workspace`
owned by `styles/_layout.scss` — the single route-gutter owner. The `qd-container` /
`qd-page-frame` / `qd-explorer-frame` aliases stay on their own rules for the call sites that have
not migrated yet; do not remove them as if they were a second layout contract. See
[`styles/README.md`](../../../styles/README.md) and
[`UI_STYLE_SYSTEM.md`](../../../../.architecture/UI_STYLE_SYSTEM.md).
## Global entity-detail overlay (Feature 029, Change B)

- `entity-detail-overlay/` owns the persistent global detail overlay: the host component
  (mounted once beside `qd-app-shell` in `app.ts`) binds the URL-authoritative
  `core/navigation/detail-overlay` coordinator to the shared `qd-detail-modal-shell` and
  mounts one lazy adapter per top-frame kind inside `@defer` blocks. Adapters must stay
  out of the eager bundle — the production build must show one lazy chunk per adapter.
- **Route-independent detail controllers**: `state/roots-detail.controller.ts` is the
  reference pattern — an `@Injectable()` (NOT root-provided) controller with
  constructor-injected api/cache/view-loader that owns the panel signal state, all loads,
  and stale-response cancellation, with a route-free `applyUrlState(...)` entry point.
  `RootsDetailFacade` stays the page's thin route adapter over its own private controller
  instance; each overlay adapter provides its OWN component-scoped controller, so overlay
  activity can never mutate the page panel. The root-scoped API/cache/view-loader services
  stay shared, so the side panel and the overlay de-duplicate the same reads
  (`RootsCacheKeys` unchanged).
- **Only the top layer traps focus**: the four mobile detail drawers
  (`root`/`lemma`/`stem`/`word-type-details-panel`) bind `[cdkTrapFocus]` to
  `!DetailOverlayHistoryService.isOpen()`, and `word-drilldown-modal` passes the same expression
  as `[trapFocus]` to its `qd-modal-shell`. While the global dialog is open they sit inside the
  inert app shell, so their traps stand down and exactly one trap is enabled
  (`app.nested-layers.spec.ts`). Never re-add an unconditional `cdkTrapFocus`.
- **The Unique-Words drilldown modal is an F14 `overlay` shell** (Golden UI Phase 7): its Wide
  modal branch renders `qd-modal-shell variant="overlay"` with `flushBody` (the projected
  `qd-details-workspace` brings its own header, tabs and body scroller, so the shell contributes no
  second padding ring) and no shell title or close of its own — the workspace already carries both.
  The shell owns the backdrop, the named width, the Compact `94dvh` sheet, the scroll lock and the
  Escape/backdrop dismissal; the `word-drilldown-modal`/`word-drilldown-backdrop` test ids and the
  `inline` and `frameless` branches are unchanged.
- **Overlay adapters never call the Router** and never push view/page changes into the
  controller directly: every tab/sub-view/pagination change goes through
  `DetailOverlayHistoryService.replaceTopFrame(...)`; the URL sync feeds the new frame back
  into the adapter's `frame` input, which re-drives the controller (`applyUrlState` runs
  `untracked` so panel-state reads don't retrigger the effect).
- All five entity panels expose a `frameless` input that renders only the view tablist +
  tabpanel body (no card, no header/close, no dialog/backdrop) for composition inside the
  global shell, which owns all dialog chrome. Overlay content testids are prefixed
  `overlay-<entity>-*` (page testids unchanged). All five adapters are fully implemented:
  root/lemma/stem (controllers extracted from their facades — lemma/stem identity includes
  the ayahs `typeCode`), unique (drilldown controller extracted; `(mode, wordId, view,
  ayahPage)` is one identity), and wordType (word-kind-only controller sharing
  `WordTypesCacheKeys`; a frame `view` of `words` is clamped to `ayahs` since member-words
  exists only for grouped selections — the page facade keeps grouped logic and was not
  refactored).
- The shell heading uses the host-provided `EntityDetailOverlayTitleStore`: the active
  adapter publishes its loaded entity title and clears it on destroy; while empty the host
  falls back to the generic kind label.
- **Cross-entity links** (plan §5.2): the seven detail-list link components
  (root-words/lemmas/stems, lemma-words/stems, stem-words/lemmas) render real
  `a[qdDetailLink]` anchors carrying fully-explicit frames instead of forced-new-tab
  explorer deep links. Context decides the click semantics via the
  `DETAIL_OVERLAY_LINK_MODE` token: overlay adapters provide `'append'` (push onto the
  stack), side panels get the `'start'` default (new one-frame stack that never touches
  the panel's own selection — proven by `entity-detail-overlay-invariant.spec.ts`).
  Modifier clicks/copy-link keep native browser behavior.
- **Ayah continuity** (plan §5.2, B7): `ayah-matches-list` renders its Mushaf link as
  `a[qdAyahOverlayLink]` (core `detail-overlay-ayah-link.directive.ts`) instead of a
  forced new tab. With the overlay open the click is a replace-navigation that carries
  the whole frame stack onto the Mushaf base; from a side panel the render site passes
  the panel's own typed frame as `parentFrame`, which is promoted to a one-frame stack
  over the Mushaf (all 5 overlay adapters pass `frame()`; the four explorer pages and
  `word-drilldown-modal` build frames from their own state — the Word Types page passes
  `null` for grouped root/stem/lemma selections, which have no frame grammar).
  `word-type-grouped-words-list` (display-only) is unchanged. Table links outside a
  detail surface keep page navigation (locked invariant).

## Gotchas / invariants (read before changing)

- **All five explorers now consume the shared Words architecture.** Every table selector delegates
  to `qd-data-table`: Roots, Lemmas, Stems and Unique Words render `standard`; Word Types renders
  `wide-columns` in its words view and `grouped-rows` in the roots/stems/lemmas views. All five pages
  use `qd-page-shell qd-page-shell--split-workspace`, the feature-local `qd-explorer-toolbar`, and
  `qd-page-split qd-page-split--data`. The pager is projected into the table's own footer slot
  (`rootsTablePagination`, `lemmasTablePagination`, `stemsTablePagination`,
  `uniqueWordsTablePagination`, `wordTypesTablePagination`). The Phase 4 shared TypeScript API was
  consumed unchanged; the old `utils/table-scrollbar-gutter-sync.ts` re-export is deleted and every
  table imports `shared/ui/data-table/table-scrollbar-gutter-sync` directly.
- **Row identity is the frozen F09 `data-row-id` attribute.** Word Types' former
  `data-word-types-row` hook is gone: `rowId` returns the same `word/root/stem/lemma` DOM identity
  and `focusStatistic` resolves it through `[data-row-id]`. Grouped rows stay display-only — no
  `tabindex`, no row activation, no link or button on the row itself; the three statistic chips are
  the only interactive elements.
- **The five explorers share one responsive composition contract.** Compact uses semantic list cards
  (`role="list"/"listitem"`) at the preserved `5.5rem` Roots, `6.5rem` Lemmas, `6.75rem` Stems,
  `4.25rem` Unique and `5rem` grouped Word Type heights, with content-driven Word Type word rows;
  Medium `768–1079` keeps table semantics with identity, three priority counts, and an explicit
  disclosure (`كل الأعداد`, or `كل التفاصيل` for the Word Types words view, which hides its four
  related-entity columns there); Wide begins at `1080` with the `1.25fr/1fr` table/details split,
  `44px` sticky header, `40px` rows, internal table scrolling, and a fixed table pager. The page
  shell is the only route-gutter owner (`16/24/32/40px`), measured at 390/767/768/1024/1079/1080/1440.
- **All five details use the shared F07/F10/F11 anatomy.** Root/Lemma/Stem/Word Type panels and the
  Unique drilldown compose `qd-details-workspace` + `qd-tabs`, keep their `5/4/4/2-or-3/3` tab sets
  and labeled tabpanels mounted, receive collision-free per-instance IDs, keep `notFound` inside the
  selected tabpanel (the other tabs are disabled while it holds), and use `.qd-details__body` as the
  sole details scroller. Ordinary linked/display-only results use `qdResultList`/`qdResultItem`;
  Quran results keep `qdAyahCard` and the existing highlighted-Quran renderer unchanged.
- **Zero-count detail triggers remain visible but inert across all five explorers.** Identity
  actions, count chips, mobile stat badges and Lemma/Stem ayah-type controls use
  `لا كلمات مرتبطة بهذا النوع، لذا لا تفاصيل لعرضها.` as the visible reason, reference it through
  `aria-describedby`, retain native `disabled` plus `aria-disabled="true"`, and never open details.
  Each table renders that reason once, in its pagination slot, under a per-instance id.
  `word-count-chip` keeps all prior inputs and adds only the optional disabled-reason ID contract.
- **Words has zero `qd-state` consumers.** `word-drilldown-modal` and the Root/Lemma/Stem/Word Type
  overlay adapters read errors through the F12 owner `qd-error-state severity="read"`; their
  `data-testid` hooks are unchanged. Nothing under `src/app/features/words/` may reference
  `<qd-state>` or `QdStateComponent` again.
- **The explorer toolbar is feature-local and semantic-only.** Its primary, result, secondary,
  applied-summary, and action zones stay mounted while each page continues to own field meaning,
  draft/applied state, Submit/Enter/Clear behavior, URL serialization, and Back/Forward restoration.
  Word Types uses its `taxonomy` variant and keeps the type tree as a sibling slot above the
  toolbar, never a second shell. `uw-toolbar-recess` / `uw-toolbar-rise` are gone.

- **Table/list visuals are centralized** (`UI_STYLE_SYSTEM.md` §17): all 5 explorer
  tables compose `.qd-explorer-table` (root class + `.qd-explorer-table__*` BEM
  elements, `styles/_explorer-tables.scss`) and all 10 detail-list panels compose
  `.qd-detail-list__*` BEM elements (`styles/_explorer-detail-lists.scss`) alongside
  their own component root class. These shared class families own row/header/hover/
  selected visuals **and** density (row `2.5rem`, cell `6×10px`, header `2.75rem`) —
  per-component SCSS keeps only `grid-template-columns` and genuine column extras
  (e.g. `stem-lemmas` 4-col, `type-distribution` 2-col). No table/list needed a
  partial-collapse exception. **Selected state** (a table row, a `qd-is-selected`
  list row, a chip, a tab) is the one doctrine visual app-wide (§16.1):
  `--qd-selected-bg` (accent-tint) background + `--qd-accent-text` label + a
  hairline `--qd-border-accent` edge or indicator — **never** a solid gold fill.
  Hover is always `--qd-surface-hover`. A component needing a visual rule beyond
  columns/selected-state is a signal to extend the shared base, not fork it.
- **List states render in the table shell, selection states in the detail panel** (Feature 030, N3
  row 5 — the §17 mounted-shell doctrine). The four normal explorers used to insert `error` /
  `empty` / `notFound` as page-level banners **above** the fixed table+panel grid, which pushed the
  whole grid down ~4.5rem whenever one appeared. They are now placed by **owner**, not by
  convenience: `error` and `empty` come from `listState()` — the table genuinely has no rows — so
  they render **inside the table shell** (`.<x>-table__state`), standing in for the body and keeping
  the shell's footprint (`min-block-size: min(70vh, 40rem)` in the ≤1023px band, matching the body
  it replaces; `flex: 1 1 auto` inside the desktop card). `notFound` comes from `panelState()` — a
  restored deep-link selection is missing **while the list is fine and populated** — so it renders
  **inside the details panel**, which is a fixed-height aside on desktop and the fixed
  `.qd-modal.explorer-detail-modal` at ≤1023px. Putting `notFound` in the table shell would hide a
  populated table and is **not** an option. Testids follow the new homes: the list states keep
  `<x>-list-error` / `<x>-list-no-results` (Unique Words: `unique-words-error` /
  `unique-words-empty`) inside the table; not-found is the panel's own `<x>-details-not-found`, and
  the former page-level `<x>-restored-not-found` testids are **gone** on Roots/Lemmas/Stems (on
  Lemmas/Stems they had been double-rendering the same message alongside the panel's). Overlay
  adapters keep owning their own `overlay-<x>-not-found` branch inside the projected content and
  leave the panel's `notFound` input **unbound** — binding it would make the panel swallow the
  adapter's branch.
- **Unique Words is the exception for `notFound`**: its drilldown builds restored-not-found with
  `isOpen: false` (`utils/unique-words-drilldown.state.ts`), so at ≤1023px the drilldown modal does
  not render at all and on desktop the inline panel shows its select-a-word prompt. Its
  `unique-words-restored-not-found` and `unique-words-restored-error` banners therefore **stay at
  page level** (below Wide the drilldown modal does not render at all) — the panel would drop the message below desktop and the table shell would hide a
  populated table — until that state contract is revisited. They no longer shift the grid: they
  live in `.unique-words-restored-slot`, rendered in **every** drilldown state, which reserves one
  compact banner row from first paint (the two states are mutually exclusive). Keep the banners
  inside that slot and keep them one line; a banner taller than the reservation grows it. Only the
  list states moved into the table shell.
- **Labels use the TDZ getter pattern.** Read `*.labels.ts` consts via **getters**, not
  `readonly` fields — otherwise they resolve to `undefined` (temporal dead zone) in the
  test bundle. **Do not revert the getters.**
- **URL-state is a contract.** `*-url-sync.ts` param names/shape are user-facing (shareable
  links) and spec'd; changing them is a contract change — update the spec and tests too.
- **`sort` is one exact, fail-closed URL token.** Its client grammar is
  `token := column | column "-asc" | column "-desc"`. A bare token means the column's
  natural direction (counts descending, text ascending); the suffix is used only for the
  opposite direction, so natural aliases normalize back to the bare canonical spelling.
  Matching is exact: the frontend neither trims nor case-folds, and an unknown token falls
  back to the explorer default.
- `mushaf-order` is ascending and bare-only; the client rejects its suffixed forms. The
  canonical default is absence of `sort`, never `sort=mushaf-order`. Releasing a header cycle
  writes `{ sort: null, page: null }`, every order change resets `page`, and there is no
  `dir` key (`column` belongs to detail focus).
- List cache keys retain the canonical token in their existing opaque sort slot; do not create a
  second cache spelling. The three-state header cycle, `aria-sort`, glyph, and label live in
  `models/explorer-sort.ts` and `utils/explorer-table-sort.controller.ts`.
- Frontend column allowlists stay local in `*_SORT_COLUMNS` and `normalize*Sort`:
  Roots offers `alpha` plus `occurrences/ayahs/surahs/simple/tashkeel/lemmas/stems`; Lemmas
  removes `lemmas`; Stems removes `lemmas/stems`; Unique Words and Word Types offer
  `alpha/occurrences/ayahs/surahs`. Related-entity text columns are not sortable (Lemma root;
  Stem dominant root/lemma; Unique Words type/root; Word Types type/root/stem/lemma), nor is
  Unique Words' computed missing-surahs column.
- Word Types is the default/cycle exception: `occurrences` descending is default, so that header
  cycles only default-descending ↔ ascending, while `mushaf-order` remains an offered order.
  Its `alpha` header follows `tableView` while the token stays the same.

The existing
[Backend Words reads README](../../../../../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md)
owns server parsing, deterministic ordering/tie-breaks, query construction, and count invariance;
do not duplicate those implementation formulas here.
- **Sorting is column-headers at ≥1024px, a `<select>` at ≤1023px** (Feature 030, N8). The
  top ترتيب dropdown is gone from every layout where the table header row is visible. Because
  all five table SCSS files set the header row to `display: none` at ≤1023px, a compact
  fallback select stays under `.qd-explorer-sort-fallback` (CSS-hidden ≥1024px) on **phone AND
  tablet** — deleting it would make sorting unreachable below desktop. It offers the default
  order plus every sortable column × both directions and drives the **same** URL contract, so
  picking the explorer's default releases the param instead of spelling it out. The
  `*-sort-select` testids are preserved. Visuals/a11y for the headers: `UI_STYLE_SYSTEM.md`
  §17 (`.qd-explorer-table` → column-header sorting).
- **Identity is clean imlaei-simple; Uthmani is display-only.** The
  [Backend Words reads README](../../../../../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md)
  owns the corresponding read-model identity.
- **Headline result count** appears on Unique Words, Roots, Lemmas, and Stems through the shared
  `ExplorerResultCountComponent` (`qd-result-count`, with the live
  `qd-explorer-result-count` compatibility alias). It renders «عدد الـ…: N» from the current
  `listState().totalCount` beside the search/sort controls: list loading shows a
  non-interactive skeleton, list error renders nothing because the table shell owns the error,
  and zero renders `0`. Because it uses the filtered list total, it stays aligned with filters
  and pagination. Word Types uses its separate four-count scope summary. Component ownership,
  its TDZ-safe label getter, and the alias rationale live in
  [`shared/README.md`](../../shared/README.md).
- **Count-range filters** (Feature 026, US5; chips reshaped in Feature 030, N4) on the four normal
  explorers: the shared `explorer-count-range-filter` component offers exactly **three chips per
  metric** — `أكثر من N` / `أقل من N` / `مخصّص` (`aria-pressed`, RTL) — over exactly the count columns each
  page already shows (Unique Words: occurrences/ayahs/surahs; Roots: + simple/tashkeel words + lemmas +
  stems; Lemmas: + simple/tashkeel + stems; Stems: + simple/tashkeel). **N is per metric**: المواضع 100,
  الآيات 100, السور **50** (a surah count can never exceed 114, so the family's 100 would be a dead chip),
  and every sub-count metric (كلمات بدون تشكيل/بالتشكيل, الصيغ المعجمية, الأصول الصرفية) 10. It resolves as a
  family default (`RANGE_FAMILY_THRESHOLDS`) **plus** an optional per-metric `RangeMetric.threshold`
  override — required because ayahs and surahs share the `ayahsSurahs` family and would otherwise be
  forced onto one N; السور reads the shared `SURAHS_RANGE_THRESHOLD` const so its chips cannot drift apart
  across the four explorers. Both chip bounds are **strict** (`أكثر من 100` ⇒ `101..`, `أقل من 100` ⇒
  `..99`), leaving exactly N reachable only through مخصّص. Chips are **presentation-only**: the URL stores
  the actual range, never a chip identity, so a shared link carrying any other range — including a
  pre-030 bucket link such as `occ=11..100` — still parses and simply reopens as an active مخصّص. Chip
  testids are stable slugs (`range-filter-chip-<metric>-gt|lt`), never derived from the Arabic label or
  its digits. Presets live in `models/words-filter-presets.ts` (config, not labels — the chip copy lives
  in `WORDS_RANGE_FILTER_LABELS`); per-page metric descriptors (`*_RANGE_METRICS`) map each metric to its
  URL key, backend API prefix, family, and optional threshold. The **URL grammar is `min..max`** (either
  bound omissible), parsed
  **fail-closed** (malformed / min>max ⇒ that filter absent, page still loads) by the shared
  `parseCountRange`/`words-range-filters` helpers. URL keys per page: Unique Words / Lemmas / Stems /
  Roots share `occ`, `ayahs`, `surahs`; Roots/Lemmas/Stems add `simple`, `tashkeel`; Roots/Lemmas add
  `stems`; Roots adds `lemmas`. Changing any range resets the list `page`. The API sends
  `<prefix>Min`/`<prefix>Max` only for active bounds; frontend list cache keys gain a deterministic
  range fragment (absent ⇒ pre-feature key). The headline stat reflects the filtered `totalCount` by
  construction. `*_RANGE_METRICS` and the range-filter labels are read via **TDZ-safe getters**, never
  `readonly` fields (they resolve to `undefined` in the bundled test build otherwise). Layout
  (Feature 029, U2): the shared filter host is a **full-width second row** of
  `.qd-explorer-controls-secondary` (`flex: 1 1 100%` on the component host) below the sort control
  on all four pages, so expanding the `<details>` panel grows its own row and never moves the sort.
- **مخصّص commits on Enter, never per keystroke** (Feature 030, N4): typing in the min/max inputs writes
  only component-local draft signals — no emit, therefore **no navigation, no history entry and no
  fetch** (a range used to cost one of each per keystroke). `Enter` (preventing the default) or the
  touch-friendly `تطبيق` button commits the normalized draft through the ordinary emit path; `Escape`
  reverts the draft to the last committed value; blur is a no-op (the draft persists). Drafts re-sync
  from `ranges()` whenever it changes outside the component (URL restore, Back/Forward, clear-all). The
  `parseBound`/`normalize` guards (non-numeric or negative ⇒ open bound; min > max ⇒ fail-open by
  dropping the max) run at **commit** time, not per keystroke.
- **Ayah type chips** (lemmas and stems only — roots have none, Word Types detail is per-type by
  construction, and `type-distribution-list` is display-only) narrow the ayahs tab by `typeCode`, and
  render at four sites: the two explorer pages and their two overlay adapters. **Clicking a chip that
  already renders active (`aria-pressed="true"`) is a complete no-op** (Feature 030, N1): the guard sits
  in the chip components' `selectTypeCode`, ahead of the emit, so no state call, no URL write, and no HTTP
  request happen from any of the four sites. The downstream page/adapter guards are kept as defense in
  depth. **The single-type chip and `عرض الكل` render active while `selectedTypeCode()` is `null`, and
  that active state is VISUAL ONLY** — the only-type code is deliberately never written to state or the
  URL, because shared-URL identity and the `aria-pressed` contract tests depend on the convention; do not
  "normalize" it away. Accepted consequence (N1-a): re-clicking the active type while on detail page > 1
  no longer resets to page 1 — pagination owns page navigation.
- **Association filters** use the shared presentational
  `explorer-association-filter` search-select. Their optional URL keys are: Unique Words
  `primaryType` and `rootId`; Lemmas `rootId`; Stems `rootId` and `lemmaId`.
  `primaryType` accepts a well-formed catalogue token and ids accept positive integers;
  malformed input fails closed to absence through
  `state/words-association-filters.ts`. A change resets list `page`, sends only active API
  params, and adds the deterministic `assoc(...)` fragment to the frontend list cache key.
- `WordsAssociationOptionsService` reuses the existing roots/lemmas server-search reads under
  their `*:picker:` cache namespaces. Unique Words types come from the Word Types tree:
  noun and particle POS-leaf children are offered; verb and muqatta'at are not granular there
  and must be explored through Word Types.
- User-facing labels remain: Unique Words «النوع الأساسي» / «الجذر الأساسي»; Lemmas «الجذر»;
  Stems «الجذر الأساسي» / «الصيغة المعجمية الأساسية». Each filter describes the association
  displayed by its row, so the selected filter and visible chip cannot disagree.

The
[Backend Words reads README](../../../../../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md)
owns primary/dominant association selection, winner ordering, and server query semantics.
- **Explorer controls layout** (Feature 027) restructures the four normal explorers' toolbars: the shared
  presentational `qd-explorer-search-row` (`role="search"`) holds the main search `<input type="search">`
  plus each page's projected association-filter fields (`<ng-content>`) side-by-side at ≥ tablet (≥1024px),
  stacking full-width on phone (≤767px). A secondary controls row (`.qd-explorer-controls-secondary`) holds
  the sort `<select>` plus `explorer-count-range-filter`; the headline result-count stat stays visible. The
  former `qd-unique-words-search-bar` is retired — its input is now the shared row's main input, its sort
  select a page-level secondary-row `<select>` — with the `unique-words-search-input`/`unique-words-sort-select`
  testids preserved. `explorer-association-filter`'s popover is field-driven rather than a `<details>`
  disclosure (Feature 030, N5): it opens on typing, on `ArrowDown`/`Alt+ArrowDown`, or on field focus
  **only when the field already carries a selection or a query** — an empty, unselected field stays
  closed on focus, and `ArrowDown` is the keyboard route in. Since Golden UI Phase 7 the panel itself
  is the shared F15 layer (`qdFloatingLayer="searchable-picker"`, `shared/ui/floating-layer/`), so
  placement, block-axis flip, inline clamp, the `min(60vh, 24rem)` cap and the Escape/Tab/outside
  dismissal script are the same code the Mushaf and Access pickers run (D33/D34) — the component no
  longer measures or positions anything. The field is the **combobox**
  (`role="combobox"`, `aria-expanded`/`aria-controls`/`aria-haspopup="listbox"`) and keeps DOM focus
  while the layer moves an `aria-activedescendant` cursor over `role="option"` items, so the list is
  navigable *without* interrupting typing; `Enter` activates the cursor option, and `Home`/`End`
  deliberately stay with the text caret. Escape still restores focus to the field and arms the reopen
  guard; `focusout` past the component and selecting an option still close it, with no focus trap and
  single-open behavior (focusing a sibling field closes the previous). Re-opening on a selection never
  re-fetches, so a URL-restored server-searched picker may open with no options until the user types
  (accepted; `searchChange` stays typing-driven). The panel floats above `.uw-toolbar-recess`
  (unclipped, RTL-anchored under the field, viewport-aware height limit). **Unchanged:**
  every URL query key, the url-sync contract, all data-testids, search debounce/semantics, and the
  association-filter public inputs/outputs.
- **Ayah match cards use the shared `qdAyahCard` frame** (Feature 029, Change A): loaded and
  loading cards in `ayah-matches-list` compose `shared/ui/ayah-card` (no `qd-card`, no
  alternating row fill, no per-context recolors in `_explorer-detail-lists.scss`). Rows are
  **tracked by `verseKey`**, never `ayahId` — Word Type ayah rows all carry `ayahId: 0`.
  `HighlightedAyahComponent` (marker filtering, matched-ID set, untouched `textUthmani` spans,
  Quran font) stays feature-owned and unchanged.
- **Words explainer hero + hub** (Feature 031, presentation-only, no backend/URL/cache change): each
  explorer page mounts the shared `qd-words-explainer` **inside `.uw-intro-band`, after
  `.qd-page-header` and above `.uw-toolbar-recess`**. It renders ordinal + eyebrow + tagline + body +
  `الفائدة` benefit callout from the single approved content source
  `models/words-explainer.content.ts` and projects each page's `مثال` example region via
  `<ng-content>`; it does **not** re-render the page title (the existing `<h1>` owns it; the section
  is named by `aria-label`). The hero is **static-height prose** — it never conditions on
  `listState()`/`panelState()`, has no loading/skeleton, and renders **above and outside** every
  mounted shell the invariants govern, so it cannot move the table+panel grid. Collapse is **per-page
  memory** (`state/words-explainer-preference.ts`, storage key `qd-words-explainer`, value =
  comma-joined collapsed keys), restored **synchronously** (a field-initialiser read, the
  `ThemeService` pattern) so the **first paint already reflects stored state** — never read the
  preference in an `effect()`/`ngOnInit`, which reintroduces the Feature 030 N3 expand-then-collapse
  shift. No height animation. The hub (`words-hub-page`) reads the same five content records for its
  numbered nav cards (card description = the page's tagline, so the two cannot drift) plus the
  orientation chain (`WORDS_HUB_CHAIN`, all-neutral nodes); card testids are **stable slugs**
  `words-hub-card--<key>` (never Arabic-label-derived), and the coming-soon scaffolding is removed
  (every explorer has shipped). The example words are illustrative `مثال` morphology in the Amiri
  face, never Quran data or queryable counts. The green callout is the one tinted-green panel,
  sanctioned in the allowed-green list (DESIGN.md §2 / UI_STYLE_SYSTEM.md §16.3, item 8).
- Tests: obey the repo test-command rule (see `../../../../README.md`) — the vitest worker
  cap and jsdom observer guards apply here.
- **Word Types has table-view tabs** (`tableView=words|roots|stems|lemmas`, default `words`,
  RTL order كلمات | جذور | أصول | صيغ). Placement (Feature 029, U3): the tab strip is the **first
  child of the split layout**, directly above the table column only (desktop pins tabs/table to
  grid column 1 rows 1/2 and the details panel to column 2 row 2, so the panel top aligns with the
  table; below desktop DOM flow gives tabs → table → panel). The semantic order
  filters → scope summary → tabs → table and the mounted-shell invariant are unchanged. Grouped views are grouped and counted server-side before
  pagination, and their identity is the numeric `rootId`/`stemId`/`lemmaId`, never display text. The
  **table-view strip, table shell, and details host stay mounted** through every browse/list/filter/
  sort/view/loading/empty/error transition; the table owns prompt/loading/empty/error-with-retry.
  A parent with children is **browse-only local state**: clicking it changes only the displayed child
  choices and performs no URL, list, or detail change. Selecting a child commits the list scope
  (`type`, `childCode`, `case`, `tense`, `voice`) and resets list page; the `inl` leaf commits directly.
  `tableView` survives list changes, and rows whose `kind` mismatches it are never rendered.

  All four views render quiet, non-focusable row containers with page-relative row numbers. The row
  container has no click/Enter/Space action. Only the three native statistic buttons open details:
  word `occurrences/ayahs → ayahs`, word `surahs → surahs`; grouped `occurrences → words`, grouped
  `ayahs → ayahs`, grouped `surahs → surahs`. Skeleton rows remain non-interactive. The exact open-detail
  row carries the shared `qd-is-selected`/`aria-selected`/`aria-current` treatment until details close;
  identity and the complete stored grammatical scope must match the current list, so preserved details
  from another list scope never highlight a coincidentally equal row. Focus returns to the originating
  statistic button, and hover never overrides the active color.

  URL state separates the list scope from the detail selection's snapshot:
  `detailType`, `detailChildCode`, `detailCase`, `detailTense`, `detailVoice`. Every statistic writes all
  five with identity/view/page; committing a child under the same main type preserves them while switching
main type clears them (the snapshot belonged to the previous type), refresh/direct URLs/Back/Forward restore
  both scopes independently, malformed/incomplete snapshots fail closed, and closing details clears
  identity/view/page plus all five detail keys. Detail tabs remain kind-aware (word → آيات/سور الكلمة;
  root → كلمات/آيات/سور الجذر; stem → كلمات/آيات/سور الأصل الصرفي; lemma → كلمات/آيات/سور الصيغة المعجمية),
  and content begins directly with the tabs and active list—there is no repeated
  summary card. Row-driven selections seed summary state from the table row and load the chosen detail
  immediately; refresh/direct URLs still fetch the summary because no table-row payload is available for
  the panel title and loading/error/retry/not-found orchestration.

  After a successful tree read, rows-only and later tree failures retain the last valid tree/strip.
  Grouped **member-word rows are strictly display-only** — no button/link/tabindex/`qd-interactive-surface`/
  selected state and no Router; only their pagination emits. Grouped words and ayahs are server-paged with
  internal page 1, the canonical URL omits `detailPage` at page 1 and serializes only pages `> 1`, and the
  surahs view always removes `detailPage`. Switching `tableView` changes only the displayed table and
  list page: an open detail identity/scope/view/page remains loaded even when its kind differs from the
  current table. Returning to the matching table kind and scope restores the exact row highlight without
  reloading details. Both backend and
  frontend cache keys (`WordTypesCacheKeys.table` / `word-types-cache.ts`'s `table(...)`)
  include `tableView`, so tab switches never cross-serve another view's rows. Stem/lemma
  terminology follows the Roots/Lemmas/Stems explorers: **stem = الأصل / الأصول الصرفية**,
  **lemma = الصيغة / الصيغ المعجمية**.
- **Word Types search** uses the optional `search` URL key and matches clean,
  tashkeel-insensitive word identity only, never root/stem/lemma display text. The single toolbar
  input is present on all four views and narrows words plus grouped roots/stems/lemmas together;
  its placeholder therefore names the word grain («ابحث في الكلمات»). The page debounces for
  300 ms, merges `{ search, page: null }`, trims and fails closed from empty to absence, restores
  from shared URLs/Back/Forward, sends search through `word-types.api.ts` only when present,
  and includes it in the frontend list cache identity (absence preserves the unfiltered key).
  Changing search resets the list page but preserves an identity-loaded detail selection.
- **Word Types presence flags** are the optional tri-state `hasRoot`/`hasStem`/`hasLemma`
  keys (`true`/`false`; absent means any) and fail closed to absence. They reshape words,
  grouped views, and their totals as one list scope. Each dimension exposes
  «الكل / موجود / غير موجود» under «الجذر / الأصل الصرفي / الصيغة المعجمية».
  A change resets list page and clears detail selection. The client threads the values through
  `word-types-url-sync.ts` → `word-types-explorer.facade.ts` → `word-types.api.ts` →
  `word-types-cache.ts`, sends a flag only when set, and adds a compact cache segment only when
  active. Grouped-detail reads carry neither these flags nor search; their numeric selection is
  already scoped by the grammatical fields.
- **Word Types scoped four-count summary** is the mounted, non-interactive
  `word-type-scope-counts` strip between filters and view tabs. It reuses the exact
  `WORD_TYPE_TABLE_VIEW_OPTIONS` short labels and order «كلمات / جذور / أصول / صيغ».
  Each count equals the corresponding tab's `TotalCount` for the identical scope and belongs to
  the scoped word-context family, never the global `words_count` family.
  - The client reads `GET api/words/word-types/scope-counts` as
    `WordTypeScopeCountsDto`. The widget owns `scopeCountsState` and counts-only
    `retryScopeCounts` so it never blocks the table.
  - Counts reload only when type/child/case/tense/voice/search/flags change, not on
    `tableView`, sort, or page. The frontend `scopeCounts(...)` cache identity contains the full
    scope and omits view/sort/page.
  - A confirmed leaf shows loading then four values (including zero); counts failure shows a
    compact retry; table failure hides unconfirmed numbers; parent/prompt scope shows nothing.
    The strip host remains mounted through all states.
- **Word Types page sizes** remain 1000 rows per list page
  (`WORD_TYPES_PAGE_SIZE`) with `CdkVirtualScrollViewport`, and 100 items per detail page
  (`WORD_TYPES_DETAIL_PAGE_SIZE`). The mounted tabs/table/details contract remains intact.
- The data-access client exposes grouped detail at
  `.../word-types/table/{roots|stems|lemmas}/{dimensionId}`. Every request includes numeric
  kind/id plus the active grammatical scope (type, optional child code, and concrete
  case/tense/voice). Member words and ayahs are paged; surahs are single-shot.
  `WordTypesCacheKeys.grouped*` isolates kind, numeric id, scope, view, and page for paged views,
  so grouped selections cannot cross-serve one another.

The
[Backend Words reads README](../../../../../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md)
owns server search/presence predicates, SQL count formulas, grouping/order, command counts, and
grouped-detail hydration mechanics.
## Related

- This README and the local components/services own current frontend Words behavior.
- Backend ordering, association, query, count, and hydration owner:
  [`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md`](../../../../../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md).
- Thin contract index:
  [`docs/contracts/words-explorers.md`](../../../../../../docs/contracts/words-explorers.md).
- Planning-artifact lifecycle: [`docs/README.md`](../../../../../../docs/README.md).
