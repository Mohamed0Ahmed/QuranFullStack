# Words feature (الكلمات) — explorers

**HOW rules:** `.architecture/FRONTEND_STRUCTURE.md`, `.architecture/API_INTEGRATION_GUIDELINES.md`
(project root). This file is the WHAT (current truth + shared pattern).

## What this feature does

Five read-only explorers over the Quran word data — **Roots, Lemmas, Stems, WordTypes,
Unique Words** — plus the Words hub. Each is a table-first split-screen page: a paginated
table on one side, a detail panel (summary + related lists + ayah matches) on the other,
with all selection/filter/paging state reflected in the URL.

## Shared pattern (each explorer repeats it)

Per explorer `X` in {roots, lemmas, stems, word-types, unique-words}:

- `pages/X-explorer-page/` — routed smart component (unique-words: `unique-words-page`).
- `state/X-explorer.facade.ts` (+ `X-detail.facade.ts`) — orchestrates load/select.
- `state/X-cache.ts` — client cache of fetched pages/details.
- `state/X-url-sync.ts` — URL ⇄ state (the URL-state contract; keep params stable).
- `state/X-detail-view.loader.ts` — loads the detail panel for a selection.
- `data-access/X.api.ts` — `ApiResponse<T>` calls.
- `models/X.models.ts` + `models/X.labels.ts` — view models + Arabic labels. Wire DTOs are
  re-exported from `core/api/generated/` (aliased to the historical `*Dto` names); UI-only
  unions, request params, and view models stay hand-written, and closed backend vocabularies
  (e.g. `kind`, table-row discriminators) are narrowed via `Omit`-overlays over the generated
  types.
- `utils/X-ayah-match.mapper.ts` — maps API ayah matches to view rows.

Shared across explorers: `utils/explorer-table-*` (focus/keyboard-nav/scroll/column-nav),
`utils/explorer-keyboard-nav.scheduler.ts`, `utils/verse-key.ts`, and the
`components/` table + list + panel set.

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
- **Labels use the TDZ getter pattern.** Read `*.labels.ts` consts via **getters**, not
  `readonly` fields — otherwise they resolve to `undefined` (temporal dead zone) in the
  test bundle. **Do not revert the getters.**
- **URL-state is a contract.** `*-url-sync.ts` param names/shape are user-facing (shareable
  links) and spec'd; changing them is a contract change — update the spec and tests too.
- **`sort` is one param with a suffix grammar** (Feature 030, N8 — cross-stack; the backend
  half is the authority, see the reads README's ordering contract). `token := column |
  column "-asc" | column "-desc"`. A **bare token means the column's natural direction** —
  counts descend, text ascends — so every pre-030 token keeps its exact meaning as an alias:
  `occurrences` ≡ `occurrences-desc`, `alpha` ≡ `alpha-asc`. The **bare form is canonical**
  for the natural direction and the suffixed form only for the opposite one, so a count
  column's canonical set is `{ occurrences, occurrences-asc }` and `occurrences-desc`
  canonicalizes **out** on the way in — one ordering can never be cached or shared under two
  spellings. `mushaf-order` is ascending-only and **bare-only** (`mushaf-order-asc/-desc` are
  rejected here and 400 on the backend). **The default is the param's ABSENCE** — never
  `sort=mushaf-order` — and releasing a header cycle writes `{ sort: null, page: null }`;
  changing the ordering always resets `page`. There is **no `dir` param and no new query
  key** (the `column` key is unrelated — it is detail focus). Client list cache keys keep the
  token in the same opaque slot (`roots:list:<sort>:…`, `wordtypes:table:…:sort:<sort>:…`) —
  no key-format change. The grammar, the 3-state cycle, `aria-sort`, the glyph and the
  aria-label live in `models/explorer-sort.ts` + `utils/explorer-table-sort.controller.ts`;
  each explorer owns its column allowlist (`*_SORT_COLUMNS`) and a `normalize*Sort` guard that
  **fails closed to the default** on anything unknown. Matching is **exact** — unlike the
  backend parser the frontend does not trim or case-fold, so `?sort=ALPHA` falls back to the
  default (pre-existing, spec'd). Sortable columns: Roots `alpha` + all 7 counts; Lemmas
  `alpha` + 6 counts; Stems `alpha` + 5 counts; Unique Words / Word Types `alpha`,
  `occurrences`, `ayahs`, `surahs`. **Related-entity text columns are deliberately NOT
  sortable** (lemmas' الجذر, stems' dominant الجذور/الصيغ, unique-words' نوع الكلمة/الجذر,
  word-types' النوع/الجذر/الأصل/الصيغة) and neither is unique-words' لم يذكر فيها (computed
  post-page; ordering by it is just the inverse of السور) — they render as plain headers.
  **Word Types is the exception on defaults**: it defaults to `occurrences` desc, so its
  المواضع header renders actively sorted in the default state and its cycle collapses to
  desc(default) ⇄ asc with no release step, while `mushaf-order` stays an ordinary offered
  ordering there rather than the release state. Its `alpha` column is the **dimension text
  column**, whose header text follows `tableView` (الكلمة / الجذر / الأصل الصرفي / الصيغة
  المعجمية) even though the token is identical across all four views.
- **Sorting is column-headers at ≥1024px, a `<select>` at ≤1023px** (Feature 030, N8). The
  top ترتيب dropdown is gone from every layout where the table header row is visible. Because
  all five table SCSS files set the header row to `display: none` at ≤1023px, a compact
  fallback select stays under `.qd-explorer-sort-fallback` (CSS-hidden ≥1024px) on **phone AND
  tablet** — deleting it would make sorting unreachable below desktop. It offers the default
  order plus every sortable column × both directions and drives the **same** URL contract, so
  picking the explorer's default releases the param instead of spelling it out. The
  `*-sort-select` testids are preserved. Visuals/a11y for the headers: `UI_STYLE_SYSTEM.md`
  §17 (`.qd-explorer-table` → column-header sorting).
- **Identity is clean imlaei-simple** (display Uthmani) — mirrors the backend read models.
- **Headline result-count stat** (Feature 026, US4) on the four "normal" explorers (Unique Words, Roots,
  Lemmas, Stems): the shared presentational `explorer-result-count` component renders the label-prefix
  phrasing **"عدد الـ…: N"** (عدد الكلمات / عدد الجذور / عدد الصيغ المعجمية / عدد الأصول الصرفية) from the
  page's existing `listState().totalCount` — no new backend read or aggregation. It sits in the toolbar
  recess beside search/sort. States: list loading → non-interactive skeleton; list error → renders nothing
  (the page's own error state owns the message); zero results → "0". Because the total is the filtered
  query's own count, the stat reflects search/filters by construction and never disagrees with pagination.
  Word Types uses the separate four-count scope summary, not this stat.
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
- **Association filters** (Feature 026, US7) narrow three of the normal explorers by a related dimension,
  using the shared presentational `explorer-association-filter` search-select (an inline search field whose
  input opens a popover holding the options list, with the current selection shown as a badge
  plus a clear affordance; RTL, `aria`; options stay plain `aria-pressed` buttons, not a listbox — see the
  Feature 027 controls-layout bullet below for the popover interaction). URL keys (all optional,
  additive, parsed **fail-closed**): Unique Words `primaryType` (POS code) + `rootId`; Lemmas `rootId`;
  Stems `rootId` + `lemmaId`. `primaryType` keeps a well-formed catalogue token else absent; ids keep a
  positive integer else absent (`state/words-association-filters.ts` `parsePosCodeParam` /
  `parsePositiveIntParam`). Changing any association resets the list `page`. The API sends each param only
  when set; frontend list cache keys gain an `assoc(...)` fragment (absent ⇒ pre-feature key). Options are
  loaded by `WordsAssociationOptionsService`, **reusing existing reads with no new endpoint**: the
  root/lemma pickers server-search `roots.api`/`lemmas.api` (cached under a `*:picker:` namespace in the
  shared caches); the Unique Words type select is fed from the word-types **tree** read, flattening the
  noun and particle POS-leaf children (the "POS child catalogue"). Verb and muqatta'at are represented
  non-granularly in the tree (by tense / as a main type) and are therefore not offered as granular
  primary-type options — use the Word Types explorer for those. Labels (lock D): Unique Words
  "النوع الأساسي" / "الجذر الأساسي"; Lemmas "الجذر" (owned root, a true belonging relation); Stems
  "الجذر الأساسي" / "الصيغة المعجمية الأساسية" (**primary, not sole** — the filter matches the displayed
  dominant association). Every association filter agrees with the value the row displays, so the chip and
  the filter can never disagree.
- **Explorer controls layout** (Feature 027) restructures the four normal explorers' toolbars: the shared
  presentational `qd-explorer-search-row` (`role="search"`) holds the main search `<input type="search">`
  plus each page's projected association-filter fields (`<ng-content>`) side-by-side at ≥ tablet (≥1024px),
  stacking full-width on phone (≤767px). A secondary controls row (`.qd-explorer-controls-secondary`) holds
  the sort `<select>` plus `explorer-count-range-filter`; the headline result-count stat stays visible. The
  former `qd-unique-words-search-bar` is retired — its input is now the shared row's main input, its sort
  select a page-level secondary-row `<select>` — with the `unique-words-search-input`/`unique-words-sort-select`
  testids preserved. `explorer-association-filter`'s popover is field-driven rather than a `<details>`
  disclosure (Feature 030, N5): it opens on typing, on `ArrowDown`/`Alt+ArrowDown` (which also moves focus
  to the first option), or on field focus **only when the field already carries a selection or a query** —
  an empty, unselected field stays closed on focus, and `ArrowDown` is the keyboard route in. It closes on
  Escape (focus restored to the field, with the reopen guard), outside-click, focus leaving the component
  (`focusout`), or selecting an option, with no focus trap and single-open behavior (focusing a sibling
  field closes the previous); `aria-expanded`/`aria-controls`/`aria-haspopup="true"` sit on the field, and
  options stay plain Tab-reachable `aria-pressed` buttons, not a listbox (no roving arrow-key/
  `aria-activedescendant` model, deliberate). Re-opening on a selection never re-fetches, so a URL-restored
  server-searched picker may open with no options until the user types (accepted; `searchChange` stays
  typing-driven). The panel floats above
  `.uw-toolbar-recess` (unclipped, RTL-anchored under the field, viewport-aware height limit). **Unchanged:**
  every URL query key, the url-sync contract, all data-testids, search debounce/semantics, and the
  association-filter public inputs/outputs.
- **Ayah match cards use the shared `qdAyahCard` frame** (Feature 029, Change A): loaded and
  loading cards in `ayah-matches-list` compose `shared/ui/ayah-card` (no `qd-card`, no
  alternating row fill, no per-context recolors in `_explorer-detail-lists.scss`). Rows are
  **tracked by `verseKey`**, never `ayahId` — Word Type ayah rows all carry `ayahId: 0`.
  `HighlightedAyahComponent` (marker filtering, matched-ID set, untouched `textUthmani` spans,
  Quran font) stays feature-owned and unchanged.
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
- **Word Types search** (`search` URL key, Feature 026) matches the clean, tashkeel-insensitive **word
  identity text only** (never root/stem/lemma display text). It is part of the **list scope**: the one
  toolbar input is visible on ALL four table views, and changing it narrows the words view AND the grouped
  roots/stems/lemmas views together (the placeholder names the word grain — "ابحث في الكلمات"). It follows
  the shared explorer feel: page-owned `Subject` + `debounceTime(300)` → router merge
  `{ search, page: null }`; parsed fail-closed (trim, empty → absent); URL-shareable and restored into the
  input on refresh/Back. Frontend and backend list cache keys gain the search component (empty ⇒
  pre-feature key). Search resets the list page but keeps the identity-loaded detail selection.
- **Word Types presence flags** (`hasRoot`/`hasStem`/`hasLemma` URL keys, Feature 026, US6) are
  tri-state (`true`/`false`, absent = any; parsed fail-closed to null). Like search they are part of the
  **list scope**: the words view AND the grouped roots/stems/lemmas views (and their totals) reshape
  together, so a "missing" choice keeps a rootless word row but collapses that grouped view. The
  toolbar exposes a three-option chip group per dimension (labels per lock D: الجذر / الأصل الصرفي /
  الصيغة المعجمية; options الكل / موجود / غير موجود). Changing a flag resets the list page and clears any
  open detail selection (like the case/tense/voice sub-filters). Frontend and backend list cache keys
  gain a compact flag segment (all-absent ⇒ pre-feature key); grouped-detail reads never carry the
  flags. The scope threads through `word-types-url-sync.ts` → `word-types-explorer.facade.ts` →
  `word-types.api.ts` (`hasRoot=true|false` sent only when set) → `word-types-cache.ts`.
- **Word Types scoped four-count summary** (Feature 026, US8) is the non-interactive `word-type-scope-counts`
  strip between the filter strip and the view tabs (order: filters → scope summary → tabs → table). It shows
  how many **كلمات / جذور / أصول / صيغ** the active scope contains, reusing the view tabs' SHORT labels
  **verbatim** (`WORD_TYPE_TABLE_VIEW_OPTIONS`, same RTL order كلمات | جذور | أصول | صيغ — the tabs are NOT
  renamed). Each count equals the corresponding tab's pagination `TotalCount` for the identical scope. It is
  served by **one** new read `GET api/words/word-types/scope-counts` (→ `WordTypeScopeCountsDto`). The strip
  is a self-contained widget that reads `WordTypesExplorerFacade.scopeCountsState` and triggers a
  counts-only refetch (`retryScopeCounts`) itself, so the page class stays thin. Counts **load on scope
  change only** — type/childCode/case/tense/voice/search/flags — and **NOT** on a `tableView` or `page`
  change (they describe the scope, not a page); the facade keys them off a `scopeKey` that omits
  tableView/sort/page, and both the frontend (`word-types-cache.ts` `scopeCounts(...)`) and backend
  (`WordTypesCacheKeys.ScopeCounts`) cache keys use the full scope and nothing view/page. States: a leaf
  scope confirmed → loading skeleton → four counts (0 renders as 0); a **counts** failure shows a compact
  error + **إعادة المحاولة** (refetches counts only) and **never blocks the table**; a **table** failure
  hides the strip's numbers (scope unconfirmed); a parent/prompt scope shows nothing. The strip host stays
  mounted through every transition (mounted-shell invariant). These are the scoped word-context counts only,
  never the global `words_count`-backed family.
- **Word Types page sizes** (Feature 026): the list serves up to **1000 rows/page**
  (`WORD_TYPES_PAGE_SIZE`, default + cap 1000) across all four views with `CdkVirtualScrollViewport`
  virtual scrolling (mirrors the other explorer tables; guarded on `ResizeObserver`); detail lists (word
  ayahs, grouped member words, grouped ayahs) serve up to **100 items/page**
  (`WORD_TYPES_DETAIL_PAGE_SIZE`). The mounted table-view strip / shell / details-host invariant survives
  the virtual-scroll change.
- The data-access client also exposes the grouped-detail contract under
  `.../word-types/table/{roots|stems|lemmas}/{dimensionId}`. Every grouped request carries the
  full active scope (`type`, optional `childCode`, and concrete `case`/`tense`/`voice` values);
  member words and ayahs are paged, while surahs are a single-shot read with no page parameter.
  `WordTypesCacheKeys.grouped*` keys isolate kind, numeric ID, scope, view, and (for paged views)
  page, so future detail loading cannot cross-serve a different grouped selection.

## Related

- Backend read models: `Backend/.../Persistence/Reads/Quran/Words/README.md`.
- Specs: `specs/015-roots-explorer/`, `016-lemmas-stems-explorer/`, `019-word-types-explorer/`,
  `014-words-hub-unique-words/`.
  (Prior frontend/docs evidence reports were purged — recover from git history if needed.)
