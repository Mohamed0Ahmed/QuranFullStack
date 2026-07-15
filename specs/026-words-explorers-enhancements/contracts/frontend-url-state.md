# Contract: Frontend URL state & cache identity (Feature 026)

URL params are user-facing, shareable contracts (words feature README rule). Every
key below: optional, additive, parsed **fail-closed** (malformed ⇒ treated as
absent), serialized only when active, and covered by url-sync spec tests in the same
commit. Existing keys and their semantics are unchanged; existing URLs keep working.

## 1. Range grammar (shared)

- Value: `min..max` — `5..`, `..10`, `5..10`, `5..5` all valid; integers ≥ 0;
  `min <= max` when both present; anything else ⇒ fail-closed (filter absent).
- Bucket chips are presentation only: selecting a bucket writes its canonical range;
  مخصّص writes the entered range. Changing preset thresholds later never breaks links.
- Any range change resets the list `page` key (removed = page 1).

## 2. New keys per page

### Unique Words (`/dashboard/words/unique/:mode`)

| Key | Form | Meaning |
|---|---|---|
| `occ`, `ayahs`, `surahs` | range | count-range filters |
| `primaryType` | POS code | primary word-type filter |
| `rootId` | positive int | primary-root filter |

### Roots (`/dashboard/words/roots`)

| Key | Form |
|---|---|
| `occ`, `ayahs`, `surahs`, `simple`, `tashkeel`, `lemmas`, `stems` | range |

### Lemmas (`/dashboard/words/lemmas`)

| Key | Form |
|---|---|
| `occ`, `ayahs`, `surahs`, `simple`, `tashkeel`, `stems` | range |
| `rootId` | positive int (FK belonging) |

### Stems (`/dashboard/words/stems`)

| Key | Form |
|---|---|
| `occ`, `ayahs`, `surahs`, `simple`, `tashkeel` | range |
| `rootId`, `lemmaId` | positive int (primary association) |

### Word Types (`/dashboard/words/types`)

| Key | Form | Meaning |
|---|---|---|
| `search` | trimmed non-empty string | word-identity search; part of the LIST SCOPE (all tableViews + scope counts inherit it); change resets `page` |
| `hasRoot`, `hasStem`, `hasLemma` | `true`/`false` (absent = any) | presence flags; part of the list scope |

Detail-selection snapshot keys (`word/contextCode/root/stem/lemma`,
`detailType/...`, `view`, `detailPage`, `location`, `column`) are **unchanged**;
list-scope changes keep following the page's existing selection rules.

## 3. Interaction contract

- Search inputs: page-owned `Subject` + `debounceTime(300)` → router merge
  `{ search, page: null }` (existing explorer pattern; Word Types adopts it).
- Word Types search input is visible on ALL tableViews; placeholder names the word
  grain ("ابحث في الكلمات").
- Filter row: chips are `<button>`s with `aria-pressed`; مخصّص reveals min/max numeric
  inputs; RTL layout; active state visible; clearing a filter removes its key.
- Restore matrix (spec SC-006): refresh, direct URL, Back/Forward, shared link — all
  restore identical list + stat + (Word Types) scope-counts state.

## 4. Cache identity (frontend)

- Every list cache key gains every new param of its page (normalized; absent ⇒
  pre-feature key).
- Word Types adds a scope-counts cache key = full scope
  `(type, childCode, case, tense, voice, search, hasRoot, hasStem, hasLemma)` — same
  inputs as the backend key; NOT keyed by `tableView` or `page`.
- Scope counts refetch on scope change only; tableView/page changes must hit neither
  the counts cache-invalidation path nor the network.

## 5. New presentational surfaces

| Surface | States |
|---|---|
| `explorer-result-count` (4 normal pages, toolbar) | value = `listState().totalCount`, phrasing "عدد الـ…: N" (عدد الكلمات / عدد الجذور / عدد الصيغ المعجمية / عدد الأصول الصرفية); loading → non-interactive skeleton; list error → hidden; empty → "0" |
| `explorer-count-range-filter` (4 normal pages + Word Types flags row) | idle/active chips (`aria-pressed`), مخصّص expanded, disabled while list loading |
| `word-type-scope-counts` (Word Types, between filter strip and view tabs) | four counts reusing the existing tabs' SHORT labels verbatim — كلمات \| جذور \| أصول \| صيغ (RTL, same order AND same text as the tabs; tabs not renamed — spec Clarifications); own loading skeleton (non-interactive) / compact error + إعادة المحاولة (refetches counts only) / zeros for empty scope; counts failure never blocks the table; table failure hides the strip's numbers (scope unconfirmed); mounted-shell invariant preserved |

## 6. Terminology (lock D, app terms — binding for every new label)

root "الجذر/الجذور" · stem "الأصل الصرفي/الأصول الصرفية" · lemma
"الصيغة المعجمية/الصيغ المعجمية" · words "الكلمات". "الجذع"/"اللمّة" must not appear
in user-facing labels. Exception scope: surfaces sitting directly beside the existing
view tabs (the four-count strip) reuse the tabs' established short forms
(أصول / صيغ) verbatim. Labels follow the existing TDZ-getter pattern in
`*.labels.ts` (do not revert the getters).
