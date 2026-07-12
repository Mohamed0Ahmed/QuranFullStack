# Word Types Explorer — Details & Stability Inspection Report

Feature area: **019/021/022 — Word Types Explorer** (table-view tabs: كلمات | جذور | أصول | صيغ)
Branch: `023-quran-search-and-words-ux` (current)
Type: **read-only inspection.** No production/test/spec/config/migration/package change is proposed as
done here. This report scopes the *next additive* work; it does **not** implement it and does **not**
constitute the implementation plan.

Every conclusion below was verified against the current repository at the paths cited. Line numbers are
as-read at inspection time.

---

## 1. Executive verdict

**READY_WITH_NOTES.**

The next iteration (grouped-view details + layout stability + row numbering + hover cleanup) can be done
**entirely additively**, on the **current branch**, **migration-free**, **importer-free**, and
**package-free** — with two caveats that must be decided before coding:

1. **Feature 022 deliberately shipped the opposite of the new requirement in three places.** Locked
   decisions 9–11 of the 022 plan (`docs/feature-022-word-types-table-view-tabs/word-types-table-view-tabs-plan.md:38`)
   explicitly say: grouped-row **details are out of MVP**, grouped rows are **noninteractive**, and the
   details panel is **hidden** in grouped views (and the tabs strip is **hidden** without a leaf scope —
   locked 12 / §12.3). The new work **reverses** decisions 9, 10, 11, the strip-visibility rule, and the
   current `tableView`→`words` reset on type/subtype change (022 plan §11.3) — the new requirement is to
   **preserve** the selected `tableView`. This is legitimate forward progress, but it is a **contract-level
   change to a documented, tested MVP**, so the README + the 019 spec set must be updated in the same change
   (they currently assert the opposite).

2. **The grouped details require a new Backend read surface.** No existing endpoint returns the
   words/ayahs/surahs of a *root/stem/lemma scoped to the active grammatical context*. The
   Roots/Lemmas/Stems Explorer endpoints exist but are **globally scoped** and **count-family-incompatible**
   (022 plan §3.6) — reusing them would show misleading global Quranic data. New read-only,
   schema-free API additions are required (details in §5 / §7).

Nothing in the inspection surfaced a blocker: the scoped `base` CTE already produces exactly the columns
grouped details need, and the paging/caching/URL machinery generalises cleanly.

---

## 2. Current architecture map

### 2.1 Route & page shell
- Route segment `types` → `wordTypesRoutePath()` (`core/navigation/route-paths.ts`).
- Smart page: `features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts`
  (standalone, `OnPush`). It binds two facades to the route and renders filter → sort → tabs → table →
  panel.

### 2.2 Frontend component tree (all under `features/words/`)
| Concern | File |
|---|---|
| Page (smart) | `pages/word-types-explorer-page/word-types-explorer-page.component.{ts,html,scss}` |
| Type/subtype/secondary filter | `components/word-type-filter/` |
| Four-view strip | `components/word-type-table-view-tabs/` |
| Table (word + grouped rows) | `components/word-types-table/` |
| Details panel chrome | `components/word-type-details-panel/` |
| Ayah list / surah lists (reused) | `components/ayah-matches-list/`, `surah-occurrences-list/`, `missing-surahs-list/` |

### 2.3 Frontend state
| Concern | File |
|---|---|
| List orchestration | `state/word-types-explorer.facade.ts` |
| Detail orchestration | `state/word-types-detail.facade.ts` |
| Detail view loading | `state/word-types-detail-view.loader.ts` |
| Detail panel state reducers | `state/word-types-detail-panel.updates.ts` |
| Client cache | `state/word-types-cache.ts` (extends `core/caching/api-response-cache`) |
| URL ⇄ state | `state/word-types-url-sync.ts` |
| Models / labels | `models/word-types.models.ts`, `models/word-types.labels.ts` |
| API | `data-access/word-types.api.ts` |

### 2.4 Backend (`Backend/`)
| Concern | File |
|---|---|
| Controller (6 actions) | `api/.../Controllers/Words/WordTypesController.cs` |
| Reader interface | `application/.../Quran/Words/WordTypes/IWordTypesReader.cs` |
| EF reader | `infrastructure/.../Reads/Quran/Words/WordTypes/EfWordTypesReader.cs` |
| Raw SQL builder | `infrastructure/.../Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs` |
| Cache decorator + keys | `infrastructure/.../Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs`, `WordTypesCacheKeys.cs` |
| Handlers | `application/.../Quran/Words/WordTypes/Queries/GetWordType{Tree,Rows,Table,Summary,Ayahs,Surahs}/` |
| DTOs | `application/.../Quran/Words/WordTypes/Responses/` |

### 2.5 Data flow (today)
```
route queryParams ──► parseWordTypesQueryParams ──► ExplorerFacade.loadList
                                                     ├─ GET /tree            (always, cached)
                                                     └─ GET /table           (only when leaf scope)
                                                           └─ words | roots | stems | lemmas rows

route queryParams ──► DetailFacade.syncFromUrlState (only when tableView==='words' AND word set)
                        ├─ GET /words/{id}          (summary)
                        ├─ GET /words/{id}/ayahs
                        └─ GET /words/{id}/surahs
```
The detail pipeline has **no branch** for root/stem/lemma selections; grouped rows never reach it.

---

## 3. Current-state inventory (§1 of brief)

### 3.1 Table-view switcher
`components/word-type-table-view-tabs/`. Buttons for `WORD_TYPE_TABLE_VIEW_OPTIONS`
(`models/word-types.labels.ts:20`) in RTL order كلمات | جذور | أصول | صيغ. Emits `viewSelected`.
Rendered **conditionally** by `hasTableScope()` — page HTML `word-types-explorer-page.component.html:39`.

### 3.2 Main type / subtype filters
`components/word-type-filter/`. Type ∈ {noun, verb, particle, inl}; subtype (`childCode`) is a catalogue
POS code (noun/particle) or a tense literal (verb). Handled by facade `selectType`
(`word-types-explorer.facade.ts:108`) and `selectChild` (`:131`).

### 3.3 Secondary/contextual filters
- **noun case** — `WordTypeCase = all|nominative|accusative|genitive|null` (`models:2`); facade
  `selectCase` (`:154`).
- **verb tense** — `all|past|present|imperative` (`models:3`); `selectTense` (`:161`).
- **verb voice** — `all|active|passive` (`models:4`); `selectVoice` (`:168`).
- No other active contextual filters. (Sort is a control, not a scope filter: `changeSort` `:175`.)
- The tree exposes the secondary-filter shape per node (`WordTypeSecondaryFilterDto`, `models:24`);
  case applies to nouns, tense+voice to verbs, none to particle/inl.

### 3.4 Table rendering & loading
`components/word-types-table/word-types-table.component.html`. A CSS-grid `role="table"`. Word view:
8 columns (`الكلمة · النوع · الجذر · الأصل · الصيغة · المواضع · الآيات · السور`). Grouped views: 4
columns (`<dimension> · المواضع · الآيات · السور`). Loading renders 5 skeleton rows
(`loadingRowPlaceholders`, `.ts:47`). Word rows are `<button>` with count-chip drilldowns; grouped rows
are **plain `<div>`, noninteractive** (`.html:128–139`).

### 3.5 Row selection
Word-only. `selectedRow` computed (`page.ts:81`) resolves to a `WordTableRowDto` **only when
`tableView === 'words'`** and the URL identity matches. `word-types-table.isSelected` (`.ts:83`) compares
the full composite identity. There is **no** selection model for grouped rows.

### 3.6 Details panel
`components/word-type-details-panel/`. Two tabs: `ayahs` (الآيات الخاصة بالكلمة) + `surahs` (السور).
Rendered by the page **only** inside `@if (listState().query.tableView === 'words')`
(`page.html:87`). It is inline (desktop split-screen) or modal (mobile), driven by `isDesktop`.

### 3.7 URL state
Keys (`WORD_TYPES_QUERY_KEYS`, `models:189`): `type, childCode, tableView, case, tense, voice, sort,
page, word, contextCode, view, detailPage, location, column`. Parsing normalises invalid values and, in
grouped views, **drops all selection params** (`word-types-url-sync.ts:40–62`). Selection params are the
six in `WORD_TYPES_SELECTION_QUERY_KEYS` (`models:206`).

### 3.8 Facade / orchestration
- **ExplorerFacade** owns `listState` (tree + rows + query). It nulls rows only when `tableView` itself
  changed (`:193–202`), otherwise keeps prior rows visible while loading. `loadList` short-circuits to a
  **tree-only** branch when no leaf is selected (`:204–222`) → `selectPrompt`/`empty`/`success` via
  `handleTreeOnlyResponse` (`:247`).
- **DetailFacade** owns `panelState`. Binds to the URL, loads summary then the active view
  (`syncFromUrlState:182`, `loadActiveView:257`). Word-only.

### 3.9 API services (`data-access/word-types.api.ts`)
`getTree` · `getRows` (`/words`) · `getTableRows` (`/table`) · `getSummary` (`/words/{id}`) ·
`getAyahMatches` (`/words/{id}/ayahs`) · `getSurahs` (`/words/{id}/surahs`). Detail calls take a
word identity (`WordTypeIdentityParams`: tashkeelWordId + contextCode + case/tense/voice).

### 3.10 Cache keys (`state/word-types-cache.ts`)
`tree` · `rows(filter,sort,page)` · `table(filter,tableView,sort,page)` (includes `tableView`, `:11`) ·
`summary(identity)` · `ayahs(identity,page)` · `surahs(identity)`. All detail keys are **word identity**
only — no dimension id, no dimension kind.

### 3.11 Backend (controller/handler/reader/DTO/query)
- Controller: `tree`, `words`, `table`, `words/{id}`, `words/{id}/ayahs`, `words/{id}/surahs`. The three
  detail actions are **word-only** (`WordTypesController.cs:102–179`).
- Reader: `GetTableRowsAsync` dispatches word vs grouped (`EfWordTypesReader.cs:106`). Grouped path groups
  the scoped `base` CTE by `dimension_id` (`EfWordTypesReader.Sql.cs:187`). Details (`GetSummaryAsync`,
  `GetAyahMatchesAsync`, `GetSurahsAsync`) all key on `MatchedMorphologyQuery(identity)` where identity is
  `tashkeelWordId + contextCode (+case/tense/voice)` (`EfWordTypesReader.cs:404`). **No grouped-detail
  reader method exists.**

### 3.12 Focused tests (current)
Frontend `features/words/` word-types specs (test-case counts):
`word-types-url-sync.spec.ts` (31) · `word-types-explorer-page.component.spec.ts` (22) ·
`word-types.api.spec.ts` (10) · `word-types-explorer.facade.spec.ts` (9) ·
`word-type-filter.component.spec.ts` (14) · `word-type-details-panel.component.spec.ts` (4) ·
`word-types-table.component.spec.ts` (4) · `word-types-cache.spec.ts` (3) ·
`word-type-table-view-tabs.component.spec.ts` (2) · `word-type-ayah-match.mapper.spec.ts` (1).

Backend `tests/.../Quran/WordsWordTypes/` (Fact/Theory counts):
`WordTypesTableReadTests.cs` (14) · `WordTypesSecondaryFilterReadTests.cs` (9) ·
`WordTypesSubtypeReadTests.cs` (9) · `WordTypesDetailsReadTests.cs` (8) · `WordTypesMainReadTests.cs` (7) ·
`WordTypesLoggingTests.cs` (4) · `WordTypesCacheReadTests.cs` (3) · `WordTypesChildCatalogueDriftTests.cs`
(2) · `WordTypesRowIdentityTests.cs` (2) · `WordTypesFixtureSmokeTests.cs` (1). No grouped-detail test.

### 3.13 What exists only for `words`, and what is missing for roots/stems/lemmas
| Capability | words | roots | stems | lemmas |
|---|:--:|:--:|:--:|:--:|
| Grouped table rows (paged, counted, sorted) | ✅ | ✅ | ✅ | ✅ |
| Stable numeric identity | ✅ (composite) | ✅ `rootId` | ✅ `stemId` | ✅ `lemmaId` |
| Row is selectable | ✅ | ❌ | ❌ | ❌ |
| Details panel rendered | ✅ | ❌ (hidden) | ❌ | ❌ |
| Summary/counts detail read | ✅ | ❌ | ❌ | ❌ |
| Related words / ayahs / surahs read | ✅ | ❌ | ❌ | ❌ |
| Detail cache keys | ✅ | ❌ | ❌ | ❌ |
| Row-number column | ❌ (none of them) | ❌ | ❌ | ❌ |

---

## 4. Confirmed problems (with file/symbol evidence)

### P1 — Details card disappears for roots/stems/lemmas
`word-types-explorer-page.component.html:87` wraps the whole `<qd-word-type-details-panel>` in
`@if (listState().query.tableView === 'words')`. In grouped views the panel is removed from the DOM
entirely. Compounded by two upstream facts: grouped rows are **noninteractive** (no `<button>`,
`word-types-table.component.html:128`) and the URL parser **drops selection params** in grouped views
(`word-types-url-sync.ts:40`). So there is no selection, no panel, and no path to either.

### P2 — Table disappears on filter/view changes
The table area is gated by `@if (listState().rows || listState().status === 'loading')`
(`page.html:64`). Two transitions leave that condition false:
- **Change main type** (`selectType`, facade `:108`) resets `childCode:null`. Today it **also** resets
  `tableView:words` — this reset is a **defect to remove** (the new requirement is to preserve `tableView`;
  §16). With no leaf, `loadList` takes the tree-only branch (`:206`) and `handleTreeOnlyResponse` sets
  `selectPrompt` with `rows:null` (`:263`). Result: `hasTableScope()` false → strip gone; `rows` null and
  not loading → **table gone**, replaced by the select-subtype prompt (`page.html:49`). Note: the no-leaf
  state has no rows regardless of `tableView`, so even with `tableView` preserved a subtype prompt is
  correct — but it must render **inside** the mounted shell, not replace it.
- **Clear subtype to parent** (`selectChild(null)`, `:131`) also resets `tableView:words` (same defect) and
  lands in the same no-leaf state.
Leaf→leaf subtype changes and secondary-filter changes **do** keep the table (rows retained while loading,
`:194`), so the disappearance is specifically the **no-leaf collapse** and the **tableView-switch null-out**
(`tableViewChanged` path `:193` sets `rows:null`; during the subsequent load the shell only survives because
`status==='loading'` keeps `page.html:64` true — a genuinely blank frame can appear between navigation and
the loading state landing).

### P3 — Four-view strip not consistently visible
`hasTableScope()` (`page.ts:76`) = `childCode !== null || type === 'inl'`. The strip
(`page.html:39`) is therefore **absent** on first load (noun, no child), on every parent selection, and
during the no-leaf collapse in P2. This is by-design per 022 locked decision 12 / plan §12.3, and it
**directly contradicts** the new requirement "strip always visible after the page loads."

### P4 — Poor hover with unwanted movement/lift animation
Word rows carry `class="word-types-table__row qd-interactive-surface"` (`word-types-table.component.html:72`).
`qd-interactive-surface:hover` applies `transform: translateY(-2px)` + shadow + border
(`styles/_components.scss:17–22`) with a `transform` transition (`:14`). That lift is the "movement/row
lift/animation" complaint. The **accepted** explorer-table row hover is quiet — `background: var(--qd-section-bg)`
only, no transform (`styles/_explorer-tables.scss:169–176`, which *already includes*
`.word-types-table__row`). The other explorer tables (roots/lemmas/stems/unique-words) put
`qd-explorer-table__row` on the row and reserve `qd-interactive-surface` for small inner elements only
(e.g. `unique-words-table.component.html:95`, `roots-table.component.html:66/78`). Word Types is the sole
table that applied the card-lift class to the whole row.

### P5 — No row-number column
`WORD_TYPES_TABLE_HEADERS` (`models/word-types.labels.ts:94`) has no `rowNumber`; the table renders none.
Every other words page has one (`ROW_NUMBER_HEADER`, `models/words-shared.labels.ts:18`; e.g.
`roots.labels.ts:17`, `lemmas.labels.ts:25`, `stems.labels.ts:30`, `unique-words.labels.ts`).

### P6 — Grouped totals shown but not explorable (dead-end counts)
Grouped rows show occurrences/ayahs/surahs as plain numbers (`word-types-table.component.html:134–136`)
with no drilldown — an information dead-end that the new details work resolves.

---

## 5. Root-cause analysis for every reported UX problem (§2 of brief)

For each reported symptom, the cause and the smallest coherent fix:

| Symptom | Root cause | Category | Smallest coherent fix |
|---|---|---|---|
| Details card disappears (roots/stems/lemmas) | Panel gated by `tableView==='words'` (`page.html:87`); grouped rows noninteractive; URL drops selection | conditional template + selection clearing + missing detail model | **Always mount the panel region**; render a **discriminated** details kind by `tableView`; make grouped rows selectable; keep the active-view selection key (`root`/`stem`/`lemma`) instead of dropping it — see §7.2/§9 |
| Table disappears on **type/subtype-to-parent** change | `selectType`/`selectChild(null)` reset `tableView:words` (defect) **and** no-leaf → tree-only branch → `selectPrompt` with `rows:null` (`facade:108`, `:131`, `:206`, `:263`) | facade state replacement + prompt/empty state gate | **Preserve `tableView`** (remove the `words` reset); keep the **table shell mounted**; render the select-subtype prompt **inside** the table body, not instead of the layout; do not null the layout on no-leaf |
| Table disappears on **view switch** | `tableViewChanged` nulls `rows` (`facade:193`); brief window before `loading` lands | cache/loading transition + component recreation window | Drive a **busy/skeleton** state on the persistent shell; keep `rows` visible (dimmed) or immediately show skeleton so no blank frame appears |
| Table disappears on **secondary filter** | Only real when combined with a no-leaf reset; leaf→leaf keeps rows | (mostly a non-issue today) | Covered by the shell-always-mounted fix |
| Four-view strip not always visible | `hasTableScope()` gate hides it without a leaf (`page.html:39`, `page.ts:76`) — 022 locked 12 | conditional template (deliberate) | Render the strip **unconditionally after tree load** with the **active `tableView` highlighted** (never forced to `words`); when no leaf scope, keep the active tab highlighted and let the empty table region prompt for a subtype. **This reverses 022 locked decision 12 — flag + update spec.** |
| Poor hover / lift animation | Row uses `qd-interactive-surface` (card-lift), not the quiet shared row hover (`html:72` vs `_explorer-tables.scss:169`) | shared style misuse | Drop `qd-interactive-surface` from the row; add `qd-explorer-table__row` so it inherits the quiet shared hover — reuse, no new variant |

Confirmed **not** the causes: incompatible row unions (the discriminated union already renders correctly),
stale request races (guarded by `distinctUntilChanged` on the request key + cache), and URL reconciliation
loops (the parser is idempotent). The blank-frame window on view switch is the only race-adjacent factor,
and it is a state-transition ordering issue, not a subscription leak.

### Required target behavior (restated, verified achievable)
- Strip always visible after the page loads → render unconditionally post-tree-load.
- Table area stays mounted during filter/view transitions → keep the shell; use skeleton/busy, never a
  blank layout.
- Type/subtype/view change refreshes data without collapsing → prompt/empty render *inside* the shell.
- Selection may be reconciled/cleared when invalid, but the **details area must not disappear** → panel
  region always mounted, content switches by kind (empty-selection state when nothing is selected).

---

## 6. Backend capability / gap table for words/roots/stems/lemmas (§5 of brief)

| Read | words | roots | stems | lemmas | Source of truth |
|---|:--:|:--:|:--:|:--:|---|
| Grouped list row (scoped, paged) | ✅ | ✅ | ✅ | ✅ | `EfWordTypesReader.Sql.cs:187` (grouped from scoped `base`) |
| Summary (label + scoped counts) | ✅ | ❌ | ❌ | ❌ | `GetSummaryAsync` word-only (`EfWordTypesReader.cs:176`) |
| Related words (paged) | ➖ n/a¹ | ❌ | ❌ | ❌ | none |
| Related ayahs (paged) | ✅ | ❌ | ❌ | ❌ | `GetAyahMatchesAsync` word-only (`:242`) |
| Related surahs (+ missing) | ✅ | ❌ | ❌ | ❌ | `GetSurahsAsync` word-only (`:354`) |
| Detail cache keys | ✅ | ❌ | ❌ | ❌ | `WordTypesCacheKeys` word identity only |

¹ For the word view, the "related words" concept collapses to the single selected word; for grouped
views it is the set of member words of that root/stem/lemma **within the active scope**.

**Confirmed gap:** all root/stem/lemma detail reads are missing. The grouped SQL already computes each
group's identity (`dimension_id`) and scoped counts, but there is no reader that, given
`(dimension kind, dimension id, WordTypeReadContext)`, returns that group's member words / ayahs / surahs.

**Reuse assessment (Roots/Lemmas/Stems Explorers) — do NOT directly reuse.** Their endpoints
(`/api/words/roots/{id}/words|ayahs|surahs`, `data-access/roots.api.ts:53–99`) are keyed by the
dimension id **alone** and are **globally scoped** over the whole Quran, using a **different count family**
(segment / `words_count` aggregates), as the 022 plan states explicitly (§3.6, plan lines 158–167). Serving
those under a filtered grammatical context (e.g. "genitive nouns whose root is ك-ت-ب") would show
**global root data**, not the scoped subset — a real risk of **misleading Quranic data**. The safe reuse
is at the **lower level**: share `BaseRowsSql`/`WordTypeReadContext` and the paging helper, but keep
dedicated Word-Types-scoped reads and contracts.

---

## 7. Recommended API and Frontend contracts

### 7.1 Backend — recommended approach: **dedicated Word-Types grouped-detail reads sharing low-level helpers** (option 3)
Evaluated:
1. *Reuse existing explorer endpoints* — **rejected** (global scope, wrong count family, §6).
2. *Dedicated grouped-detail endpoints* — **recommended shape**, but…
3. *Share `BaseRowsSql`/context + paging while keeping dedicated contracts* — **recommended
   implementation** of option 2. It preserves scope correctness, reuses proven SQL, and avoids N+1.

The correct scoping key for every grouped detail read is
`(WordTypeReadContext = type + childCode + case + tense + voice) + dimension kind + dimension id`, i.e.
`BaseRowsSql(context)` filtered by `WHERE {dimension}_id = @dimensionId`. That guarantees the details are
the **same population** that produced the row's counts.

**Proposed routes** (mirroring the existing `words/{id}/…` conventions; final names to be confirmed
against `Backend/.architecture/API_GUIDELINES.md` at implementation time — do not finalise arbitrarily):

```
GET /api/words/word-types/table/{kind}/{dimensionId}            → summary  (single-shot)
GET /api/words/word-types/table/{kind}/{dimensionId}/words      → member words (server-side paged)
GET /api/words/word-types/table/{kind}/{dimensionId}/ayahs      → ayah matches (server-side paged)
GET /api/words/word-types/table/{kind}/{dimensionId}/surahs     → surah occurrences (+ missing) (single-shot, NOT paged)
    kind ∈ {roots|stems|lemmas}
    &type &childCode &case &tense &voice     ← the SAME active scope as the row (required)
    &page &pageSize                          ← words + ayahs ONLY; summary and surahs are single-shot
```
Every request **must carry the full active scope** so the read filters the same `base`. Missing scope
params default exactly as the list endpoint does. **Paging policy is locked:** related words paged,
related ayahs paged, related surahs single-shot, missing surahs single-shot. `detailPage` therefore
applies only to the paged views (words, ayahs), never to surahs.

**Proposed response shapes** (read-only; reuse existing DTOs where identical):
- **summary** — `{ kind, dimensionId, displayText, occurrencesCount, ayahsCount, surahsCount }` (the
  grouped-row fields, recomputed authoritatively server-side for the scope).
- **words** — `PagedResult<GroupedMemberWordDto>` where each member word is a **word-context row**, grouped
  on the **same key the Words table uses** — `(unique_tashkeel_word_id, context_code)` over the
  dimension-filtered scoped `base` — **not** distinct unique words. Each row is
  `{ tashkeelWordId, contextCode, case?, tense?, voice?, displayText, occurrencesCount, ayahsCount,
  surahsCount }` (same composite word-row identity as `WordTableRowDto`). **These rows are display-only in
  this iteration** — the identity fields carry the word-context data and its scoped counts for rendering,
  they are **not** an interaction affordance: a member word does **not** open the word details panel and
  does **not** change URL/selection state (§10). The `PagedResult.TotalCount` = **distinct word-context
  rows** under the selected dimension + active scope (see §8). Reuse `RowsSql`/`RowsCountSql` grouping
  semantics verbatim, adding only `WHERE {root|stem|lemma}_id = @id` to the shared `base`.
- **ayahs** — `PagedResult<WordTypeAyahMatchDto>` (reuse the existing DTO + canonical Uthmani highlight
  projection from `GetAyahMatchesAsync`, `EfWordTypesReader.cs:311–348`).
- **surahs** — `WordTypeSurahsResponse` (reuse; occurrences + missing surahs).

**Reader**: add scoped overloads to `IWordTypesReader`, e.g.
`GetGroupedSummaryAsync(kind, dimensionId, filter, ct)`, `…AyahMatchesAsync(…, page, pageSize, ct)`,
`…SurahsAsync(…, ct)`, `…MemberWordsAsync(…, page, pageSize, ct)`. Each builds `WordTypeReadContext` and
adds `WHERE {root|stem|lemma}_id = @dimensionId` to the shared `base`. **`…MemberWordsAsync` must reuse the
Words-table grouping** — `GROUP BY (unique_tashkeel_word_id, context_code)` with
`context_code = ContextExpression(context)` (`RowsSql`/`RowsCountSql`, `:50`/`:71`/`:373`) — so its rows and
`TotalCount` are byte-for-byte the word-context rows the Words table returns under the same scope, restricted
to the selected dimension. Ayah paging must mirror the existing "page the distinct ayah ids, then hydrate
words for the page" pattern (`:258–349`) to avoid N+1 and oversized payloads.

**Cache**: extend `WordTypesCacheKeys` with grouped-detail keys that include `kind + dimensionId + the
full scope hash` (+ page for the paged words/ayahs views only; summary/surahs keys carry no page),
mirroring `Table(...)`.

Preserved guarantees: current grammatical scope (via `base`), stable numeric identity (`dimensionId`),
canonical Quran text (reuse the existing Uthmani highlight projection + `quran_words`/`quran_ayahs`
sources), server-side paging (member words + ayahs), no N+1, no oversized payloads, **no migration, no
importer, no schema change** (all reads over existing tables).

### 7.2 Frontend contracts
- **URL**: use the project's **explicit, canonical per-view identity keys** (not a generic `dim`) so a
  grouped detail is shareable/restorable, matching the existing word identity convention:
  - `tableView=words`  → existing `word`/`contextCode` (+ case/tense/voice)
  - `tableView=roots`  → `root={rootId}`
  - `tableView=stems`  → `stem={stemId}`
  - `tableView=lemmas` → `lemma={lemmaId}`

  Examples: `?tableView=roots&root=123&view=words` · `?tableView=stems&stem=456&view=ayahs&detailPage=2`
  · `?tableView=lemmas&lemma=789&view=surahs`. (`view=surahs` never carries `detailPage` — surahs are
  single-shot.) `kind` is implied by `tableView`, so the id key alone is unambiguous; no generic `dim` key
  is introduced (the repo already uses explicit named identities everywhere, so an explicit key is the
  stronger, consistent convention). These keys are **additive** to `WORD_TYPES_QUERY_KEYS`. On a
  table-view change, clear only the **incompatible** selection keys and preserve the active view's
  selection key when it is still valid; the parser must stop unconditionally dropping selection in grouped
  views (`word-types-url-sync.ts:40`) and instead validate the **active-view** selection there.
- **Detail state**: extend `WordTypesDetailState` (or introduce a discriminated `selectedDetail`) to carry
  `{ kind: 'word'|'root'|'stem'|'lemma', id }`, plus a `words` sub-view alongside `ayahs`/`surahs`
  (`detailPage` applies to `words`/`ayahs` only; `surahs` is single-shot).
- **API service**: add `getGroupedSummary/Words/Ayahs/Surahs(kind, id, scope[, page])` — `page` for the
  Words/Ayahs calls only.
- **Cache**: add grouped-detail keys including `kind + id + scope` (+ `page` for words/ayahs only).

---

## 8. Critical semantic scope (§4 of brief)

The grouped rows are produced **inside the active grammatical context** and at a **head/word-level grain**
— verified from code. The grouped SQL reads `FROM quran_word_morphology m` (`EfWordTypesReader.Sql.cs:172`),
i.e. the **`WordMorphology`** entity, whose primary key is `QuranWordId` — **one row per Quran word**
(`domain/QuranDashboard.Domain/Quran/Words/Morphology/WordMorphology.cs`). Each such row carries a
**single head** `root_id`/`stem_id`/`lemma_id` (`m.RootId`/`m.StemId`/`m.LemmaId`) plus the head
`head_pos`/`is_verb`/`verb_tense`/`verb_voice`/`case_feature`. The separate `WordMorphologySegment` table
(`quran_word_morphology_segment`, one row per segment) is **not** read by the grouped SQL. So a word
contributes **exactly once** to one root, one stem, and one lemma — **its head** — with **no per-segment
expansion**. The grouped table is therefore **word-level, keyed on the head-level dimension id**, not
segment-level.

**Grain-parity lock (locked rule).** Grouped details **must use exactly the same membership grain and
dimension identity as the grouped table row**: head-level `quran_word_morphology`, filtered by the same
`WordTypeReadContext` (type/childCode/case/tense/voice) with `WHERE {root|stem|lemma}_id = @id` over the
scoped `base`. Do **not** join `quran_word_morphology_segment` to add secondary roots/stems/lemmas the row
never represented; do **not** turn a head-level grouped row into a segment-level explorer inside the panel.

Per-view semantics (all over the scoped head-level `base`, `EfWordTypesReader.Sql.cs:150`):

| Semantic | roots (`rootId`) | stems (`stemId`) | lemmas (`lemmaId`) |
|---|---|---|---|
| **Membership** | words in scope whose **head** `root_id` = id | head `stem_id` = id | head `lemma_id` = id |
| **Morphology occurrence count** | `COUNT(*)` = head morphology rows (word occurrences) in scope | same | same |
| **Word-context row count** | `COUNT(*)` over `GROUP BY (unique_tashkeel_word_id, context_code)` — the **same** grouping the Words table uses (see below), **not** `COUNT(DISTINCT unique_tashkeel_word_id)` | same | same |
| **Ayah count** | `COUNT(DISTINCT ayah_id)` | same | same |
| **Surah count** | `COUNT(DISTINCT surah_number)` | same | same |
| **Search affects** | catalogue/list only (there is no search box in Word Types today) | — | — |
| **Filters sent to detail** | type, childCode, case, tense, voice (all active) — required | same | same |

**Member-word row identity = the Words-table word-context row (verified).** The existing Words table groups
its rows on **`(tashkeel_word_id, context_code)`** — verified in `EfWordTypesReader.Sql.cs`: `RowsCountSql`
`GROUP BY tashkeel_word_id, {ContextExpression(context)}` (`:50`) and `RowsSql`
`GROUP BY tashkeel_word_id, {ContextExpression(context)}, {TypeCodeExpression(context)}` (`:71`, where
`TypeCodeExpression == ContextExpression`). `context_code = ContextExpression(context)` is **`head_pos`**
for non-verb types and **`COALESCE(verb_tense, 'unspecified')`** for verbs (`:373`). The active secondary
filters (case/tense/voice) and the selected `childCode` **scope the `base` first** (`SecondaryFilterPredicate`,
`ChildCodePredicate`); they are **not** extra GROUP BY columns. Grouped member-words for a selected
root/stem/lemma **must reuse this exact grouping** over the dimension-filtered `base` — i.e. group by
`(unique_tashkeel_word_id, context_code)`, **never** by `unique_tashkeel_word_id` alone. Grouping by the
unique word alone would wrongly **merge two legitimate usages** of the same Quranic word whose resolved
grammatical context differs (e.g. the same spelling used as two different POS, or a verb in past vs present),
because `context_code` (head_pos / verb_tense) is precisely what distinguishes them.

The measures are **distinct**, never conflated:
- **Morphology occurrence count** — `COUNT(*)` of head morphology rows (word occurrences) in scope.
- **Word-context row count** — distinct `(unique_tashkeel_word_id, context_code)` groups = the member-words
  list length and its `totalCount`; ≤ occurrence count.
- **Distinct ayah count** — `COUNT(DISTINCT ayah_id)`.
- **Distinct surah count** — `COUNT(DISTINCT surah_number)`.

**Confirmed Words-table word-context grouping key, per active context** (member-words must match exactly,
after scoping `base` by the active filters + the selected dimension id):

| Active context | `context_code` value | Effective member-word grouping key |
|---|---|---|
| Noun/particle **parent** (no `childCode`) | `head_pos` (varies per word) | `(unique_tashkeel_word_id, head_pos)` |
| Verb **parent** (`type=verb`, no `childCode`) | `COALESCE(verb_tense, 'unspecified')` | `(unique_tashkeel_word_id, verb_tense)` |
| **Exact child** selected (noun/particle POS, or verb tense) | pinned constant (= the child) | `(unique_tashkeel_word_id, context_code)` → effectively `(unique_tashkeel_word_id)` (context already pinned) |
| `inl` | `head_pos` = `'INL'` (constant) | effectively `(unique_tashkeel_word_id)` |
| **Active secondary filter** (case / tense / voice) | unchanged (`head_pos` or `verb_tense`) | scope `base` by the filter **first**, then the same `(unique_tashkeel_word_id, context_code)` key — case/tense/voice are carried in the row identity but are **not** grouping columns; voice narrows the population only when a voice filter is active |

**Detail-summary parity (must hold):** for the selected row under the same active scope, the detail
`occurrencesCount`/`ayahsCount`/`surahsCount` must **equal** the row's counts — both derive from the
identical scoped head-level `base` filtered to that one dimension id. The member-words `totalCount` means
**distinct word-context rows** under the selected grouped dimension and active grammatical scope — i.e. the
count the existing Words table would return under the same filters.

**Occurrence-count vs `totalCount` (preserve the existing rule):** grouped `totalCount` = distinct
**non-null** dimension ids; it is **not** comparable to the words-view `totalCount` (different population).
Null coverage is an **occurrence-sum identity**, not a count identity (022 plan §8, lines 462–478).

**Null grouped dimensions stay excluded** (022 locked 12): head morphology rows with null
`root_id/stem_id/lemma_id` never form a row and must never appear in details. Ayah markers stay excluded
(`!IsAyahMarker`, `BaseRowsSql`).

**Quranic-data safety warning.** Because grouped counts and memberships are head-level and scope-bound,
**any change of grain** — expanding to segments, or dropping the active grammatical scope — would present
counts and memberships that do not match the row the user selected, i.e. **misleading Quranic data**. Keep
the grain and scope identical between the row and its details; assert it in tests (§18).

---

## 9. State / URL / cache interaction matrix (§9 of brief)

Target behaviour after the additive work (current gaps annotated). "Shell" = the mounted table layout +
strip + panel region.

| Action | Resets | Preserved | Table visible | Selection valid after | URL keys changed | Cache key changed | Backend request |
|---|---|---|---|---|---|---|---|
| Change **main type** | childCode→null, case/tense/voice→all, page→1, invalid selection | **tableView** (preserved) | **Yes (shell; prompt inside)** ¹ | cleared | type, childCode, case, tense, voice, page, selection | table (new scope) | tree (cached) + table when leaf² |
| Change **subtype** | page→1, selection | type, **tableView**, secondary | Yes | cleared/reconciled | childCode, page, selection | table | table |
| Change **secondary filter** (case/tense/voice) | page→1, selection | type, childCode, **tableView**, sort | Yes | reconciled | case/tense/voice, page, selection | table | table |
| Change **table view** | page→1, selection | scope filters | **Yes (skeleton, no blank frame)** ¹ | cleared | tableView, page, selection | table (view differs) | table |
| Change **sort** | page→1, selection | scope, tableView | Yes | reconciled | sort, page, selection | table | table |
| **Search** | n/a (no search control) | — | — | — | — | — | — |
| Change **page** | — | everything else | Yes | preserved | page | table (page) | table |
| Select **row** (word or grouped) | detail sub-state → defaults | list state | Yes | valid | word/contextCode (words) · root/stem/lemma (grouped), view, detailPage | detail key | summary + active view |
| Change **details tab** (words/ayahs/surahs) | detailPage→1 (paged views only) | selection | Yes | valid | view, detailPage (words/ayahs) | detail view key | that view |
| Change **details sub-page** (words/ayahs only) | — | selection, tab | Yes | valid | detailPage | detail key (page) | that view page |

¹ Currently **fails** (P2/P3): main-type/no-leaf collapses the layout; view switch has a blank-frame
window. ² Today the strip and table only appear once a leaf/`inl` scope exists; target is strip always
visible. `tableView` is **preserved** on every type/subtype/secondary-filter/sort change — the user returns
to `words` only by selecting the Words tab; `surahs` never carries `detailPage` (single-shot).

---

## 10. Frontend design & component reuse (§6 of brief)

**Recommended structure — a discriminated details state keyed by `tableView`, sharing the panel chrome
and the three list sub-views.**

- **Panel region always mounted.** Move `<qd-word-type-details-panel>` outside the `tableView==='words'`
  guard (`page.html:87`). The panel chrome (header, tabs, empty/notFound/loading/error states) is
  kind-agnostic and reusable as-is.
- **Shared sub-sections / tabs:** الكلمات (grouped only) · الآيات · السور. Word view keeps its current two
  tabs; grouped views add a "الكلمات" (member words) tab. Independent paging per sub-view (words + ayahs
  paged; surahs single-shot, matching today).
- **Reuse:** `ayah-matches-list`, `surah-occurrences-list`, `missing-surahs-list` are already generic and
  are reused for the grouped ayah/surah tabs unchanged. For member-words, reuse the **Words-table
  word-context row** projection (a lightweight list) — each member word is a `(tashkeelWordId, contextCode)`
  word-context row (`WordTableRowDto`-shaped), **not** a bare distinct unique word and **not** the standalone
  Roots/Lemmas/Stems explorer list components (those are wired to global explorer facades/URLs; embedding
  them here would drag global scope + separate URL state). The TS model for a member word is the existing
  composite word identity (`{ tashkeelWordId, contextCode, case, tense, voice, … }`) used purely for
  **display**.
- **Member-word rows are display-only (locked, this iteration).** They **must not** be clickable, **must
  not** open the existing word details panel, **must not** navigate or modify URL/selection state, and only
  **display** the word-context row plus its scoped counts (occurrences · ayahs · surahs). Render them as
  non-interactive elements (plain `<div>`/`<li>`, no `<button>`, no `qd-interactive-surface`, no row/count
  click handlers, no `qd-is-selected`). Prefer a **dedicated lightweight, non-interactive member-words
  list** (`word-type-grouped-words-list`) to keep scope and cache local. (Deep-linking a member word to the
  word details reads is an explicit **future follow-up**, not part of this iteration.)
- **State:** a discriminated `selectedDetail` (`{kind,id}` internally, surfaced in the URL as the explicit
  `root`/`stem`/`lemma` key per §7.2; existing composite identity for word) in `WordTypesDetailFacade`,
  with `view ∈ {words?,ayahs,surahs}`. Loading/empty/error/retry/not-found reuse the existing panel states
  (`word-type-details-panel.component.html:58–68`, `word-types-detail-panel.updates.ts`).
- **Cache-key composition:** `kind + id + full scope + tab (+ page for words/ayahs only)` — mirrors
  `WordTypesCacheKeys.table`. Summary and surahs are single-shot, so their keys carry no page.
- **Stale-request protection:** keep the `distinctUntilChanged` + `switchMap` pattern already used in both
  facades (`explorer.facade.ts:84`, `detail.facade.ts:74`); unsubscribe prior detail subs on new selection
  (already done, `detail.facade.ts:110`). Extend the identity comparison to include `kind`.

**Likely files affected (frontend):** `models/word-types.models.ts`, `models/word-types.labels.ts`,
`state/word-types-url-sync.ts`, `state/word-types-cache.ts`, `state/word-types-detail.facade.ts`,
`state/word-types-detail-view.loader.ts`, `state/word-types-detail-panel.updates.ts`,
`data-access/word-types.api.ts`, `components/word-types-table/*` (make grouped rows selectable + row
numbers + hover), `components/word-type-details-panel/*` (kind-aware content, member-words tab), a new
`components/word-type-grouped-words-list/*` (member words), `pages/word-types-explorer-page/*` (mount panel
always, strip always, shell-stable states).

---

## 11. Row numbering (§7 of brief)

- **Header:** reuse `ROW_NUMBER_HEADER` (`models/words-shared.labels.ts:18`); add `rowNumber` to
  `WORD_TYPES_TABLE_HEADERS` (`models/word-types.labels.ts:94`) exactly as the sibling label files do
  (`roots.labels.ts:17`, etc.). Accessible label already provided by the shared header pattern.
- **Formula:** reuse `pageRelativeRowNumber(page, pageSize, index)` = `(page-1)*pageSize + index + 1`
  (`utils/unique-words-pagination-display.ts:1`). Word Types table receives `rows` (a `PagedResultDto`)
  which carries `.page`/`.pageSize`, so it can compute the number without a new input (or accept
  `currentPage`/`pageSize` inputs like `roots-table`, `.ts:87`).
- **Virtual scrolling:** not used by the Word Types table (it renders `page.items` directly). If a virtual
  viewport is ever added, mirror `unique-words-table`'s `rowNumber(index)` inside `*cdkVirtualFor`
  (`unique-words-table.component.html:89`). No change needed now.
- **No visible DB id:** the number is purely `page/pageSize/index`-derived; never render `rootId`/`stemId`/
  `lemmaId`/`tashkeelWordId` (they stay identity-only, as the README invariant requires).
- **Stable after sort/search/page:** the number is position-in-page, so it re-derives correctly on every
  server-sorted/paged response; it is not tied to row identity.
- **Styling:** reuse shared `qd-explorer-table__header-cell--row-number` / `--cell--row-number`
  (`styles/_explorer-tables.scss:86–104`) — no new styles. Add the column to both word and grouped grid
  templates (`word-types-table.component.scss:13–26`).

---

## 12. Table hover & visual consistency (§8 of brief)

**Responsible CSS:** the row's `qd-interactive-surface` class
(`word-types-table.component.html:72`) resolves to `styles/_components.scss:11–22`:
```scss
.qd-interactive-surface { transition: border-color …, box-shadow …, transform var(--qd-t-fast); }
.qd-interactive-surface:hover { border-color: …; box-shadow: var(--qd-shadow); transform: translateY(-2px); }
```
That `transform: translateY(-2px)` + shadow is the lift/movement/animation. (A `prefers-reduced-motion`
block neutralises the transform at `_components.scss:438`, but that only helps reduced-motion users.)

**Accepted pattern (reuse):** `styles/_explorer-tables.scss:169–176` already defines the quiet row hover
for **every** explorer table, including `.word-types-table__row:hover` →
`background: var(--qd-section-bg)` (no transform, no lift), and the selected row style at
`:160–167` → `background: var(--qd-accent-tint)`. The other tables get this by using
`qd-explorer-table__row` on the row and keeping `qd-interactive-surface` only on small inner elements
(`unique-words-table.component.html:95`, `roots-table.component.html:78`).

**Required result → smallest fix:**
- Remove `qd-interactive-surface` from the word row (`word-types-table.component.html:72`) and add
  `qd-explorer-table__row` so the row inherits the shared quiet hover. → no movement, no transform, no
  lift, quiet background emphasis only.
- Selected row stays distinct via the existing `.word-types-table__row.qd-is-selected` shared rule.
- Loading rows already carry `--loading` and are excluded from the shared hover
  (`_explorer-tables.scss:174` `:not(…, .word-types-table__row--loading)`) and never had
  `qd-interactive-surface`; keep that.
- Grouped rows are plain `<div>`s; once they become selectable they should also use
  `qd-explorer-table__row` (not `qd-interactive-surface`).

**Shared-component side-effect check:** `qd-interactive-surface` is a **global utility** used by cards,
tabs, chips, and inner buttons across the app. The fix **removes** it only from the Word Types *row*; it
touches no shared file and cannot regress other components. No shared component authored the bad hover —
it was a local misuse of the shared class. (Keeping `focus-visible` outline: the row already has its own
`:focus-visible` rule at `word-types-table.component.scss:126`, so accessibility is unaffected.)

---

## 13. Reuse-versus-new-code decisions

| Concern | Decision | Why |
|---|---|---|
| Grouped list SQL / scope | **Reuse** `BaseRowsSql` + `WordTypeReadContext` | Already scoped correctly; details filter it by `dimension_id` |
| Ayah highlight projection | **Reuse** `GetAyahMatchesAsync` projection + `WordTypeAyahMatchDto` | Canonical Uthmani source, no N+1, paged |
| Surahs response | **Reuse** `WordTypeSurahsResponse` + surah/missing logic | Identical shape |
| Roots/Lemmas/Stems **explorer** readers/endpoints | **Do NOT reuse** | Global scope + wrong count family (§6, plan §3.6) → misleading data |
| Roots/Lemmas/Stems explorer **list components** | **Do NOT reuse in-panel** | Wired to global facades + own URL state |
| Ayah/surah list components | **Reuse** (`ayah-matches-list`, `surah-occurrences-list`, `missing-surahs-list`) | Already generic |
| Member-words list | **New lightweight, display-only component** | Keep scope + cache local; non-interactive rows (no selection/nav) |
| Details panel chrome | **Reuse**, make content kind-aware | Chrome is identity-agnostic |
| Row hover | **Reuse** shared `qd-explorer-table__row` | Kills the lift; one-line class swap |
| Row number | **Reuse** `pageRelativeRowNumber` + shared cell classes | Zero new styles |
| Detail cache | **Extend** `WordTypesCacheKeys` with grouped keys | Mirror `Table(...)` |

---

## 14. Exact likely changed-file inventory

**Backend (all read-only, schema-free):**
- `application/.../WordTypes/IWordTypesReader.cs` — add grouped-detail reader methods.
- `infrastructure/.../WordTypes/EfWordTypesReader.cs` — grouped-detail implementations.
- `infrastructure/.../WordTypes/EfWordTypesReader.Sql.cs` — grouped-detail SQL (member words/ayahs/surahs
  from scoped head-level `base` + `WHERE {root|stem|lemma}_id = @id`; no segment join). Member-words reuse
  the Words-table `GROUP BY (unique_tashkeel_word_id, context_code)` semantics verbatim.
- `application/.../WordTypes/Queries/GetWordTypeGroupedSummary|Words|Ayahs|Surahs/` — new query/handler/
  outcome trios (mirror existing detail handlers).
- `application/.../WordTypes/Responses/` — a `GroupedMemberWordDto` (**a word-context row** carrying the
  full composite word identity `tashkeelWordId + contextCode + case/tense/voice`, not a bare unique word)
  (+ reuse existing ayah/surah/summary DTOs where identical).
- `api/.../Controllers/Words/WordTypesController.cs` — new grouped-detail actions.
- `api/.../Common/ApiMessages.cs` — new Arabic message constants.
- `infrastructure/.../Caching/.../WordTypesCacheKeys.cs` + `CachedWordTypesReader.cs` — grouped-detail keys.
- `application/.../DependencyInjection.cs` + `infrastructure/.../WordTypesDependencyInjection.cs` — register
  new handlers.

**Frontend:**
- `models/word-types.models.ts`, `models/word-types.labels.ts` (row-number header, tab/section labels).
- `state/word-types-url-sync.ts` (explicit `root`/`stem`/`lemma` selection keys; preserve `tableView` on
  scope changes), `word-types-cache.ts` (grouped keys),
  `word-types-explorer.facade.ts` (**remove the `tableView`→`words` reset** in `selectType`/`selectChild(null)`),
  `word-types-detail.facade.ts`, `word-types-detail-view.loader.ts`, `word-types-detail-panel.updates.ts`.
- `data-access/word-types.api.ts` (grouped detail calls).
- `components/word-types-table/*` (selectable grouped rows, row-number column, hover class swap).
- `components/word-type-details-panel/*` (kind-aware content, member-words tab).
- `components/word-type-grouped-words-list/*` (**new**, **display-only** — non-interactive rows).
- `components/word-type-table-view-tabs/*` + `pages/word-types-explorer-page/*` (strip always visible;
  shell-stable prompt/empty/loading; panel always mounted).
- `pages/word-types-explorer-page/word-types-explorer-page.component.scss` (full-width vs split layout by
  view — already partly present at `:10`).

**Docs/specs (same change, mandatory):**
- `Frontend/.../features/words/README.md` (reverses the "details panel hidden / grouped noninteractive"
  statements at lines 46–52).
- `specs/019-word-types-explorer/contracts/word-types-api.md`, `frontend-routing-state.md`,
  `backend-read-abstractions.md`, `data-model.md` (add grouped-detail endpoints + grouped selection param).
- `Backend/.../Reads/Quran/Words/README.md` (grouped-detail reads from scoped `base`).

---

## 15. Risks & Quranic-data safety notes

1. **Global-vs-scoped confusion (highest risk).** Serving root/stem/lemma details from the global explorer
   readers would present whole-Quran data under a filtered grammatical context. Mitigation: all grouped
   details filter the scoped `base` by `dimension_id`; never call `EfRootsReader`/segment aggregates
   (plan §3.6).
2. **Count-family mixing.** Grouped occurrence counts are morphology-occurrence based; explorer counts are
   segment/`words_count` based. The detail summary must recompute from the scoped `base` so its counts
   equal the row's counts. Assert equality in tests.
3. **Grain parity (grouped row ↔ its details) — highest data-safety risk.** The grouped table is
   **head-level** (`quran_word_morphology`, one row per `QuranWordId`, single head `root_id`/`stem_id`/
   `lemma_id`; verified §8). Details **must** use the same head-level grain and dimension identity — never
   join `quran_word_morphology_segment` to add secondary roots/stems/lemmas the row never represented.
   Changing grain between the row and its details would produce **misleading counts and memberships**.
   Keep the four measures distinct: morphology occurrence count (`COUNT(*)` head rows) ≠ word-context row
   count ≠ distinct ayahs ≠ distinct surahs (§8).
3b. **Member-word identity must match the Words table (data-safety).** Member-word rows must group on
   `(unique_tashkeel_word_id, context_code)` — the exact Words-table word-context key (`head_pos` for
   non-verb, `COALESCE(verb_tense,'unspecified')` for verb; secondary filters + childCode scope `base`
   first, §8) — **never** `COUNT(DISTINCT unique_tashkeel_word_id)` alone. Grouping by the unique word alone
   would **merge two legitimate usages** of the same Quranic word whose resolved grammatical context differs
   (same spelling as different POS; past vs present verb), understating the count and hiding a valid usage.
4. **Null dimensions.** Must stay excluded from rows and details (locked 12).
4b. **Paging policy.** Related words + ayahs are server-side paged; related surahs (and missing surahs) are
   single-shot. `detailPage` must never be applied to the surahs view.
5. **Canonical text provenance.** Ayah highlighting must reuse the existing Uthmani projection over
   `quran_words`/`quran_ayahs` with markers excluded (`EfWordTypesReader.cs:311`, `.313` `!IsAyahMarker`).
   Do not introduce a second text source.
6. **Contract reversal visibility.** Reversing 022 locked decisions 9–12 is a user-visible + spec change;
   apply README/spec updates in the same change to avoid documentation drift.
7. **Blank-frame race on view switch.** The `rows:null` window (`facade:193`) must be replaced by an
   immediate busy/skeleton on the persistent shell, or a brief blank layout persists.
8. **Member-word interactivity scope creep.** Member-word rows inside root/stem/lemma details are
   **display-only** this iteration: not clickable, no word-details hand-off, no URL/selection mutation. The
   member word carries the full composite identity for display only; wiring it to open the word panel or
   change URL state is **out of scope** (future follow-up). Guard against accidentally reusing an
   interactive words-table row component that emits selection/navigation.

---

## 16. Recommended target behavior (consolidated)

1. After the tree loads, the **four-view strip is always visible** with the **active `tableView`
   highlighted**; grouped tabs are enabled once a leaf scope exists (or the table region prompts for a
   subtype otherwise).
2. The **selected `tableView` is preserved** across main-type, subtype, case, tense, and voice changes.
   The user returns to `words` **only** by selecting the Words tab. A scope change resets only page→1 and
   any now-invalid selection; it never silently switches to `words`.
3. The **table shell stays mounted** through every type/subtype/view/filter change; prompt/empty/loading
   render **inside** the shell with a skeleton/busy treatment — never a blank page. The last successful
   rows stay visible until replacement data (or an in-shell prompt/skeleton) is available.
4. The **details region is always mounted**; its content is a discriminated kind (word vs root/stem/lemma
   vs empty-selection). Selection may be reconciled or cleared, but the region never disappears.
5. Grouped **table** rows (roots/stems/lemmas) are **selectable** and open **scoped, same-grain** details:
   member words (paged, as **display-only word-context rows** grouped exactly like the Words table) + ayahs
   (paged) + surahs (single-shot) + scoped counts consistent with the row and all active filters. The
   member-word rows **inside** the details panel are **display-only** — not clickable, no panel hand-off, no
   URL/selection change.
6. A **row-number column** appears on all four views, matching the sibling pages.
7. Row hover is **quiet** (background/border only), with no transform/lift/animation; selected row stays
   distinct; loading rows get no hover.

Worked example (matches the brief): state `type=noun, childCode=N, tableView=roots`. User switches to
`verb` → `tableView` stays `roots`, the strip stays visible with جذور highlighted, the table shell stays
mounted; with no verb subtype yet, the subtype prompt shows **inside** the table region; once a verb
subtype is chosen, root rows load for the verb scope. The view never silently drops to `words`.

---

## 17. Recommended implementation phases (not a full plan)

1. **Backend grouped-detail reads** — reader methods + SQL over scoped `base` + `dimension_id`; queries/
   handlers/outcomes; controller actions; messages; cache keys; DI. Tests §18.
2. **Frontend contracts** — models (discriminated detail + row-number header), URL grouped-selection param,
   cache keys, API calls.
3. **Detail facade + loader** — kind-aware selection, member-words/ayahs/surahs loading, stale-request
   guard extended with `kind`.
4. **Layout stability** — strip always visible with the active `tableView` highlighted; **preserve
   `tableView`** on type/subtype/filter changes (remove the `selectType`/`selectChild(null)`
   `tableView`→`words` reset); table shell always mounted; prompt/empty/loading inside the shell; panel
   region always mounted.
5. **Grouped selection + details panel** — selectable grouped rows; explicit `root`/`stem`/`lemma` URL
   identity; kind-aware, same-grain panel content; member-words list component; reuse ayah/surah lists.
6. **Row numbering + hover cleanup** — shared row-number column; swap `qd-interactive-surface` →
   `qd-explorer-table__row` on rows.
7. **Docs/specs/README** updates (§14) — reverse the 022 MVP statements; add grouped-detail contracts.
8. **Full verification** — backend `--filter WordsWordTypes`; frontend word-types specs + `ng build`.

---

## 18. Test & acceptance gaps (§10 of brief)

**Current coverage** (§3.12): word-view list/detail/url/cache well covered; grouped **list** covered
(`WordTypesTableReadTests.cs`, 14); grouped **details** entirely uncovered on both stacks; **no** tests for
row numbering, hover class, layout stability, or strip-always-visible.

**Proposed missing coverage (minimum):**

Frontend:
- View strip always visible after page load (parent + leaf scopes), active `tableView` highlighted.
- **`tableView` preserved** on main-type / subtype / case / tense / voice changes (never reset to `words`);
  only the Words tab returns to `words`.
- Table shell remains visible during type/subtype/view/filter transitions (no blank-page assertion).
- Changing parent/subtype does not produce a blank page (prompt renders inside the shell).
- Switching roots/stems/lemmas views does not hide the details region.
- Row numbering present + correct (`(page-1)*pageSize + index + 1`), stable after sort/page.
- No animated hover class/style on rows (assert absence of `qd-interactive-surface` on the row; presence of
  `qd-explorer-table__row`).
- Selecting a root/stem/lemma opens the correct details **kind** via the explicit `root`/`stem`/`lemma` key.
- Related words render for all three grouped views as **word-context rows** (a word with two POS/tense
  usages under the dimension shows two rows, not one).
- **Member-word rows are display-only:** assert no clickable element/`<button>`, no `qd-interactive-surface`
  and no `qd-is-selected` on member rows; clicking a member row triggers **no** panel open, **no** router
  navigation, and **no** change to URL/selection query params.
- **Word + ayah paging** for grouped details; **surahs are single-shot** (assert no `detailPage` on the
  surahs view).
- Scoped grammatical filters (type/childCode/case/tense/voice) reach the detail requests.
- Detail counts **match the selected filtered row** (summary vs row parity, same head-level grain).
- URL restore + browser back/forward for a grouped selection via the explicit `root`/`stem`/`lemma` key.
- Cache separation by table view, selected dimension, active grammatical context, tab, and page
  (page component only for the words/ayahs views).
- Stale-response protection when switching selection/view quickly.

Backend:
- Grouped summary/words/ayahs/surahs return only the scoped members of the given dimension id.
- **Grain parity:** grouped-detail reads use the head-level `quran_word_morphology` grain (no
  `quran_word_morphology_segment` join); a word contributes once per head root/stem/lemma.
- Markers excluded from grouped ayah highlighting.
- Canonical Uthmani provenance for highlighted words.
- Null grouped dimensions never appear in details.
- Scoped detail `occurrencesCount`/`ayahsCount`/`surahsCount` **equal** the selected grouped-row counts
  under the same scope.
- **Member-word parity (explicit):** for a selected root/stem/lemma, the returned member-word rows **and
  their `TotalCount`** equal the rows the existing Words table (`GetRowsAsync`/`/words`) returns under (a)
  the same active grammatical filters, (b) restricted to the same selected dimension id, using (c) the same
  `(unique_tashkeel_word_id, context_code)` word-row grouping. Assert row-for-row equality (identity +
  per-row counts), not just count equality.
- **Usage-split assertion:** a unique word with two resolved contexts under the dimension (e.g. two
  head_pos, or past+present) yields **two** member-word rows — not one — proving grouping is not
  `DISTINCT unique_tashkeel_word_id`.
- **Four measures kept distinct:** morphology occurrence count, word-context row count, distinct ayah count,
  distinct surah count are asserted separately and not conflated.
- Surahs read is single-shot (no page parameter accepted/required).
- Cache-key isolation for grouped details (kind + id + scope; + page for words/ayahs only).

---

## 19. Branch / additivity / migration / importer / package confirmation (§14 of brief)

| Question | Answer | Basis |
|---|---|---|
| Can the work stay on the **current branch** (`023-quran-search-and-words-ux`)? | **Yes** | It extends the same feature already committed on this branch (022 table-view tabs). |
| **Additive** only? | **Yes** | New reader methods/queries/controller actions/components/URL param; existing `/words` + `/table` + word detail reads untouched. Two documented **reversals** of 022 MVP UI decisions (panel hidden → shown; grouped **table** rows noninteractive → selectable; strip hidden → always visible) are UI/spec changes, not deletions of capability. Member-word rows inside details stay **display-only** this iteration. |
| **Migration-free**? | **Yes** | All grouped details read existing tables (`quran_word_morphology`, `quran_words`, `quran_roots/lemmas/stems`, `quran_ayahs`, `quran_surahs`) via `BaseRowsSql`; no schema change. |
| **Importer-free**? | **Yes** | No source-data or pipeline change; identity/counts derive from existing read models. |
| **Package-free**? | **Yes** | No new NuGet/npm dependency; reuses Angular Signals/RxJS, STJ polymorphism, and existing shared styles/components. |

**Default posture confirmed:** read-only API additions only, no schema change. The only non-code
obligations are documentation/spec updates that must ship in the same change because they currently assert
the opposite MVP behaviour.

---

## 20. Change summary

### Revision 4 — grouped member-word rows are display-only

- **Sections corrected:** §7.1 (member-words DTO note), §10 (frontend design — new display-only bullet),
  §13 (reuse table), §14 (grouped-words-list marked display-only), §15 (new risk 8), §16 (target #5),
  §18 (new display-only test), §19 (additivity note).
- **Locked decision:** grouped member-word rows inside root/stem/lemma details are **display-only** this
  iteration — **not clickable**, do **not** open the existing word details panel, do **not** navigate or
  modify URL/selection state, and only **display** the word-context row and its scoped counts. Every
  "selecting a member word hands off to the existing word detail reads" statement was removed. Deep-linking
  a member word to the word details is an explicit **future follow-up**, not part of this iteration.

### Revision 3 — member-word row identity = the Words-table word-context row

- **Sections corrected:** §8 (semantic scope: measures table + new member-word grouping paragraph +
  confirmed per-context key table), §7.1 (`GroupedMemberWordDto` + reader/member-words note), §10 (frontend
  member-words model), §14 (DTO + SQL notes), §15 (new risk 3b), §16 (target #5), §18 (member-word parity +
  usage-split + four-measures tests).

- **Corrected definition.** Removed `COUNT(DISTINCT unique_tashkeel_word_id)` as the member-row measure.
  Related member-word rows now use the **exact Words-table word-row grouping** — `(unique_tashkeel_word_id,
  context_code)` over the dimension-filtered scoped `base` — so grouped root/stem/lemma details return the
  same word-context rows the Words table would return under the same active filters + selected dimension.
  `GroupedMemberWordDto` is a **word-context row** (full composite word identity), and its `TotalCount` =
  **distinct word-context rows**, not distinct unique words. Added: do-not-merge-usages risk, explicit
  parity + usage-split acceptance tests, and the four kept-distinct measures (morphology occurrence count,
  word-context row count, distinct ayah count, distinct surah count).

- **Exact confirmed Words-table grouping key, per supported context** (verified in
  `EfWordTypesReader.Sql.cs` — `RowsCountSql :50`, `RowsSql :71`, `ContextExpression :373`):
  - Noun/particle **parent** (no `childCode`): `(unique_tashkeel_word_id, head_pos)`.
  - Verb **parent** (`type=verb`, no `childCode`): `(unique_tashkeel_word_id, COALESCE(verb_tense,'unspecified'))`.
  - **Exact child** selected (noun/particle POS, or verb tense): `(unique_tashkeel_word_id, context_code)`
    with `context_code` pinned constant by the child → effectively `(unique_tashkeel_word_id)`.
  - `inl`: `context_code = 'INL'` (constant) → effectively `(unique_tashkeel_word_id)`.
  - **Active secondary filter** (case/tense/voice): scope `base` by the filter **first**, then the same
    `(unique_tashkeel_word_id, context_code)` key; case/tense/voice are carried in the row identity but are
    **not** grouping columns (voice narrows the population only when a voice filter is active).

### Revision 2

This revision corrected four items across the whole report (executive summary, root causes, target
behavior, API/frontend contracts, interaction matrix, risks, phases, and acceptance tests):

- **Sections corrected:** §1 (executive caveat), §4 (P2 root cause), §5 (root-cause table), §7.1 (API
  routes + paging note), §7.2 (URL identity keys + detail-state/API/cache bullets), §8 (semantic scope —
  full rewrite), §9 (interaction matrix), §10 (cache-key composition + selected-detail), §14 (changed-file
  inventory), §15 (risks), §16 (target behavior + worked example), §17 (phases), §18 (tests).

- **Preserve the selected table view.** Removed every statement that main-type / subtype / secondary-filter
  changes reset `tableView`→`words`. Target locked: `tableView` is **preserved** on type/subtype/case/
  tense/voice changes; the user returns to `words` **only** via the Words tab; scope changes reset only
  page→1 and invalid selection, showing an in-shell subtype prompt when the new scope has no leaf. The
  current `selectType`/`selectChild(null)` reset is documented as a **defect to remove**, not endorsed.

- **Final grouped-membership grain (confirmed from code):** **head/word-level** `quran_word_morphology`
  (PK `QuranWordId`, one row per word), grouped on the **single head** `root_id`/`stem_id`/`lemma_id`
  (`EfWordTypesReader.Sql.cs:172` → `BaseRowsSql`; `domain/.../Morphology/WordMorphology.cs`). The
  `WordMorphologySegment` table is **not** used by the grouped SQL — **no per-segment expansion**. Grouped
  details are locked to the **same head-level grain and dimension identity** as the row; joining segments is
  forbidden. The prior multi-segment claim was removed as unverified. A Quranic-data safety warning about
  grain drift was added.

- **Final URL identity decision:** explicit per-view keys — `root={rootId}` / `stem={stemId}` /
  `lemma={lemmaId}` (word view keeps `word`/`contextCode`). The generic `dim` key was removed; the repo's
  existing explicit-identity convention makes named keys the stronger, consistent choice. On table-view
  change, clear only incompatible selection keys and preserve the active view's key when valid.

- **Final paging policy:** related words — **paged**; related ayahs — **paged**; related surahs —
  **single-shot**; missing surahs — **single-shot**. `detailPage` applies only to words/ayahs, never
  surahs. All surah-paging statements, tests, contracts, and cache rules were removed.

- **Verdict unchanged:** **READY_WITH_NOTES**, and the work remains **current-branch / additive /
  migration-free / importer-free / package-free** on the corrected evidence.

---

*End of inspection report. No implementation performed; no implementation plan produced.*
