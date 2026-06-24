# Contract: Frontend Routing, State, and UI Behavior

## Route

```text
/dashboard/words/roots
```

- One child route under the existing `words` feature (sibling of `unique/:mode`).
- Add `WORDS_ROOTS_SEGMENT = 'roots'` + `rootsRoutePath()` to `core/navigation/route-paths.ts`; add a lazy `WORDS_ROOTS_ROUTE` to `words.routes.ts`.
- Stable route key, not a translated label. Generic fallback must not handle `words/roots`.

## Query Params

List state:

| Param | Values | Default |
|---|---|---|
| `search` | Arabic root text | empty |
| `sort` | `mushaf-order`, `occurrences`, `alpha` | `mushaf-order` |
| `page` | positive integer | `1` |

Selected-root / panel state:

| Param | Values | Meaning |
|---|---|---|
| `root` | root id | Selects a root; drives the panel. Valid only as a positive int. **URL-only — never displayed.** |
| `view` | `words`, `ayahs`, `surahs`, `lemmas`, `stems` | Active panel tab; only meaningful when `root` is set. |
| `wordView` | `simple`, `tashkeel` | Sub-view; only when `view=words` (default `simple`). |
| `surahView` | `mentioned`, `missing` | Sub-view; only when `view=surahs` (default `mentioned`). |
| `detailPage` | positive integer | Detail page; only for paginated views (`ayahs`, `words`). |

Rules:

- Unknown `sort` → default; non-positive/NaN `page`/`detailPage` → default; `view` ignored unless `root` is a valid positive int; sub-views ignored unless their parent `view` is active; `detailPage` ignored outside paginated views.
- Clearing the selection clears `root`, `view`, `wordView`, `surahView`, `detailPage`; preserves `search`, `sort`, `page`.
- Invalid/unknown `root` → controlled not-found state in the panel; list stays usable; the bad selection is not retried on later list navigation.
- `pageSize` / `detailPageSize` are fixed defaults (not URL params).

Example URLs:

```
/dashboard/words/roots?search=رحم&sort=occurrences&page=1&root=55&view=ayahs&detailPage=1
/dashboard/words/roots?root=55&view=words&wordView=simple
/dashboard/words/roots?root=55&view=words&wordView=tashkeel
/dashboard/words/roots?root=55&view=surahs&surahView=missing
```

Row-select default (when `root` is set without an explicit `view`): `view=words&wordView=simple`.

## Table columns & count-click mapping

Grid header labels (short, for scanability): الجذر · المواضع · الآيات · السور · بدون تشكيل · بالتشكيل · الصيغ · الأصول.
Semantic column meaning (counts / aria): الجذر · المواضع · الآيات · السور · كلمات بدون تشكيل · كلمات بالتشكيل · الصيغ المعجمية · الأصول الصرفية.
Show summary numbers only; UI row numbers; no backend ids. Tashkeel column semantics are `كلمات بالتشكيل` (never `الصيغ بالتشكيل`).

| Count cell | Opens |
|---|---|
| المواضع | `view=ayahs` |
| الآيات | `view=ayahs` |
| السور | `view=surahs&surahView=mentioned` |
| كلمات بدون تشكيل | `view=words&wordView=simple` |
| كلمات بالتشكيل | `view=words&wordView=tashkeel` |
| الصيغ المعجمية | `view=lemmas` |
| الأصول الصرفية | `view=stems` |
| (row select) | `view=words&wordView=simple` (default) |

Each count cell is a real keyboard-operable button (reuse `word-count-chip`). Zero-count cells remain clickable and open an empty-state view.

## Details panel

Tabs: الكلمات (sub-views بدون تشكيل / بالتشكيل) · الآيات · السور (sub-views ورد فيها / لم يذكر فيها) · الصيغ المعجمية · الأصول الصرفية. No نظرة عامة tab.

Layout: desktop split-screen with the panel on the inline-end side, having its **own scroll container** (table and panel scroll independently); narrow screens use a dismissible drawer (focus-trap, `Esc`, focus return) — **not** a modal as the desktop default.

## Data loading

| UI state | Behavior |
|---|---|
| List open | Load one cached/paged roots list for search/sort/page. **No detail calls on table render.** |
| Panel `ayahs` | Lazy-load paged ayah matches (reuse `highlighted-ayah` + `ayah-matches-list`). |
| Panel `words/*` | Lazy-load paged word list for the sub-view. |
| Panel `surahs/mentioned` / `missing` | Lazy-load whole list (≤114). |
| Panel `lemmas` / `stems` | Lazy-load whole list (bounded). |

Rules:

- Lazy-load only the active tab/sub-view; reuse already-loaded views within the same selected-root session (frontend `ApiResponseCache`, `roots:` keys).
- Child components do not call API services directly; the facade owns orchestration and maps `ApiResponse<T>` into page-ready loading/empty/error/not-found state.
- Use the shared `qd-pagination` for the list and paginated detail views (not the old words-only pagination).

## Navigation to existing word details

- Word items carry `uniqueWordId` + `kind`. Clicking builds a Feature 014 deep link via `buildUniqueWordsDeepLink(kind, { wordId })`: `simple` → Unique Words simple flow, `tashkeel` → tashkeel flow. Navigation uses ids, not text.
- Lemmas/stems are **static, non-interactive** list items (ids retained in the model for future linking; no fake buttons/links now).

## Highlighting rules

- `highlighted-ayah` receives `words` + `matchedQuranWordIds`; a word is highlighted iff its `quranWordId` ∈ matched set.
- No string replacement / text-fragment matching; no Quran-text mutation.
- Highlight must not rely on color alone (class/marker/accessible label). Quran text rendering/fonts stable and not animated.

## Accessibility & RTL

- Count cells, tabs, sub-view toggles, pagination, and word links are keyboard-operable.
- Tab strip uses `role="tablist"/tab/tabpanel` with `aria-selected` and RTL-aware arrow keys; sub-views are a nested tablist; selected row uses `aria-current`; panel load status via `role="status" aria-live="polite"`.
- Arabic-first RTL using logical CSS properties (`inline-start/inline-end`); no fake interactive elements.

## States

Loading, empty, no-results (empty search), error (safe backend message), and not-found (invalid `root`) are explicit and calm; missing data is never fabricated.
