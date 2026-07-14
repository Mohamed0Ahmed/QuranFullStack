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

- **Labels use the TDZ getter pattern.** Read `*.labels.ts` consts via **getters**, not
  `readonly` fields — otherwise they resolve to `undefined` (temporal dead zone) in the
  test bundle. **Do not revert the getters.**
- **URL-state is a contract.** `*-url-sync.ts` param names/shape are user-facing (shareable
  links) and spec'd; changing them is a contract change — update the spec and tests too.
- **Identity is clean imlaei-simple** (display Uthmani) — mirrors the backend read models.
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
