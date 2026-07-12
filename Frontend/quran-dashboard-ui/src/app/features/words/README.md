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
- `models/X.models.ts` + `models/X.labels.ts` — view models + Arabic labels.
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
  RTL order كلمات | جذور | أصول | صيغ). Grouped views (`roots`/`stems`/`lemmas`) are backed by
  `GET .../word-types/table` and are grouped + counted server-side **before** pagination;
  `GET .../word-types/words` stays unchanged for existing deep links. Rows are a
  discriminated union keyed by `kind: 'word'|'root'|'stem'|'lemma'` — grouped identity is
  the numeric `rootId`/`stemId`/`lemmaId`, **never** Arabic display text. The **table-view strip,
  table shell, and details host stay mounted** through every parent/child/filter/sort/view/loading/
  empty/error transition (the strip appears once the tree loads, including parent scopes); the split
  table/details layout is kept for grouped views, and the **table owns its own prompt/loading/empty/
  error (with retry) inside its body** instead of swapping the shell out. `tableView` **survives**
  type/child/case/tense/voice/sort/page changes — only the Words tab returns a grouped view to
  `words`. Grouped rows/counts are still display-only in this iteration (grouped-row selection and
  grouped detail content land with US6). Switching a `tableView` clears only the incompatible
  selection keys, even if a stale deep link supplies one. Both backend and
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
