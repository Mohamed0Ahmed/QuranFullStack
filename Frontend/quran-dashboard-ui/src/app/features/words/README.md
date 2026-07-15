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
- **Identity is clean imlaei-simple** (display Uthmani) — mirrors the backend read models.
- **Headline result-count stat** (Feature 026, US4) on the four "normal" explorers (Unique Words, Roots,
  Lemmas, Stems): the shared presentational `explorer-result-count` component renders the label-prefix
  phrasing **"عدد الـ…: N"** (عدد الكلمات / عدد الجذور / عدد الصيغ المعجمية / عدد الأصول الصرفية) from the
  page's existing `listState().totalCount` — no new backend read or aggregation. It sits in the toolbar
  recess beside search/sort. States: list loading → non-interactive skeleton; list error → renders nothing
  (the page's own error state owns the message); zero results → "0". Because the total is the filtered
  query's own count, the stat reflects search/filters by construction and never disagrees with pagination.
  Word Types uses the separate four-count scope summary, not this stat.
- **Count-range filters** (Feature 026, US5) on the four normal explorers: the shared
  `explorer-count-range-filter` component offers preset bucket chips (`aria-pressed`, RTL) plus a
  "مخصّص" min/max panel per metric — exactly the count columns each page already shows (Unique Words:
  occurrences/ayahs/surahs; Roots: + simple/tashkeel words + lemmas + stems; Lemmas: + simple/tashkeel
  + stems; Stems: + simple/tashkeel). Presets live in `models/words-filter-presets.ts` (config, not
  labels); per-page metric descriptors (`*_RANGE_METRICS`) map each metric to its URL key, backend API
  prefix, and bucket family. The **URL grammar is `min..max`** (either bound omissible), parsed
  **fail-closed** (malformed / min>max ⇒ that filter absent, page still loads) by the shared
  `parseCountRange`/`words-range-filters` helpers. URL keys per page: Unique Words / Lemmas / Stems /
  Roots share `occ`, `ayahs`, `surahs`; Roots/Lemmas/Stems add `simple`, `tashkeel`; Roots/Lemmas add
  `stems`; Roots adds `lemmas`. Changing any range resets the list `page`. The API sends
  `<prefix>Min`/`<prefix>Max` only for active bounds; frontend list cache keys gain a deterministic
  range fragment (absent ⇒ pre-feature key). The headline stat reflects the filtered `totalCount` by
  construction. `*_RANGE_METRICS` and the range-filter labels are read via **TDZ-safe getters**, never
  `readonly` fields (they resolve to `undefined` in the bundled test build otherwise).
- **Association filters** (Feature 026, US7) narrow three of the normal explorers by a related dimension,
  using the shared presentational `explorer-association-filter` search-select (an inline search field whose
  input opens a focus-driven popover holding the options list, with the current selection shown as a badge
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
  testids preserved. `explorer-association-filter`'s popover is now focus-driven rather than a `<details>`
  disclosure: it opens on field focus or typing and closes on Escape (focus restored to the field),
  outside-click, focus leaving the component (`focusout`), or selecting an option, with no focus trap and
  single-open behavior (focusing a sibling field closes the previous); `aria-expanded`/`aria-controls`/
  `aria-haspopup="true"` sit on the field, and options stay plain Tab-reachable `aria-pressed` buttons, not a
  listbox (no arrow-key/`aria-activedescendant` model, deliberate). The panel floats above
  `.uw-toolbar-recess` (unclipped, RTL-anchored under the field, viewport-aware height limit). **Unchanged:**
  every URL query key, the url-sync contract, all data-testids, search debounce/semantics, and the
  association-filter public inputs/outputs.
- Tests: obey the repo test-command rule (see `../../../../README.md`) — the vitest worker
  cap and jsdom observer guards apply here.
- **Word Types has table-view tabs** (`tableView=words|roots|stems|lemmas`, default `words`,
  RTL order كلمات | جذور | أصول | صيغ). Grouped views are grouped and counted server-side before
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
