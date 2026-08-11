# UI Polish Audit — living report

**Status: OPEN.** This is a running audit, not a final report. Pages are added as they are
reviewed; each page gets its own `##` section and ends with its own status line. No
implementation plan is written here, and no production code has been changed by this audit.

## Method and measurement environment

All numbers below were measured in a real browser against the running local stack, not derived
from reading CSS alone. Where a value is derived rather than measured, it is labelled
**(derived)**.

| Item | Value |
| --- | --- |
| Frontend | `ng serve --ssl` on `https://localhost:4200`, **development** configuration |
| Backend | `https://localhost:5015` (local, warm) |
| Browser | Chrome, viewport `1920×937`, `document.documentElement.clientWidth = 1905`, DPR 1 |
| Route | `/dashboard/mushaf`, page 1, ayah `1:3` and `1:5`, word `1:3:1` / `1:5:1` |

Caveat on wall-clock timings: the automated tab reported `document.visibilityState === "hidden"`,
so `requestAnimationFrame` did not run and `setTimeout`/`setInterval` were subject to background
clamping. **Resource-timing values (`PerformanceResourceTiming`) and geometry values
(`getBoundingClientRect`) are unaffected and are the numbers relied on below.** Timer-derived
durations are flagged where they appear.

---

## Mushaf Reader

Route: `/dashboard/mushaf` → `Frontend/quran-dashboard-ui/src/app/features/mushaf/`.
Feature truth: `features/mushaf/README.md`.

### Finding M-1 — The study tabs row shifts horizontally during loading

#### Current behavior

In the five-tab study row (`التفسير` / `الترجمة` / `الإعراب` / `آيات قريبة` / `المتشابهات`), the
count badges on the last two tabs are **removed** while ayah study data is loading and
**reinserted** when it arrives. Because the tabs are intrinsically sized, the two affected tabs
shrink, and every tab after them slides along the inline axis. The row container itself does not
move; the shift is entirely inside the row.

The section already reserves *vertical* geometry while loading (the N3 row 10 `ResizeObserver`
reservation). Nothing reserves *inline* geometry — the same observer actively **discards** the
reservation when the inline size changes by more than 1px.

#### Evidence

Measured on ayah `1:3` (similar-ayah count `5`, mutashabihat count `0`) at viewport 1905px, by
recording `getBoundingClientRect()` for every tab, then removing exactly the two
`.qd-tabs__count` spans that Angular removes while loading, re-measuring, and restoring them:

| Tab | Loaded `x` / `w` | Loading `x` / `w` | Δx | Δw |
| --- | --- | --- | --- | --- |
| التفسير | 985.8 / 65.7 | 985.8 / 65.7 | 0 | 0 |
| الترجمة | 915.8 / 65.9 | 915.8 / 65.9 | 0 | 0 |
| الإعراب | 846.8 / 65.0 | 846.8 / 65.0 | 0 | 0 |
| آيات قريبة | 733.0 / 109.8 | 761.0 / 81.8 | **+28.0** | **−28.0** |
| المتشابهات | 611.2 / 117.8 | 667.2 / 89.8 | **+56.0** | **−28.0** |

Confirmed live during a real ayah switch (programmatic click on `[data-word-location="1:5:1"]`,
polled DOM state):

```
t≈899ms   loading=true   badges=0   lastTab.x=667.2   lastTab.w=89.8
t≈2899ms  loading=false  badges=2   lastTab.x=611.2   lastTab.w=117.8
```

Measured row geometry in the loaded state:

```
.qd-tabs class list : "qd-tabs qd-tabs--scrollable"
display             : flex        justify-content: normal (= flex-start)
direction           : rtl         gap: 4px        overflow-x: auto
.qd-tabs__tab       : flex: 0 0 auto ; gap: 8px
container           : x=285.5  w=766  scrollWidth=766  clientWidth=766  scrollLeft=0
sum of tab widths   : 424.3px  (row does not overflow at Wide)
.qd-tabs__count     : width 20px, min-inline-size 20px, padding-inline 5.6px/5.6px,
                      font-variant-numeric: tabular-nums
```

The 28.0px per-tab delta is exactly `20px` (badge `min-inline-size: 1.25rem`) + `8px`
(`.qd-tabs__tab { gap: var(--qd-space-2) }`).

Affected files:

- `features/mushaf/components/selected-ayah-section/selected-ayah-section.component.html:44-49` —
  `@if (tabCount(tab.key) !== null) { <span class="qd-tabs__count"> … }`
- `features/mushaf/components/selected-ayah-section/selected-ayah-section.component.ts` —
  `tabCount()` returns `null` when `this.loadState().isLoading || !this.study()`
- `styles/_components.scss:298-305` (`.qd-tabs`), `:331-350` (`.qd-tabs__tab`),
  `:391-412` (`.qd-tabs__count`, `.qd-tabs__count--empty`)
- `shared/ui/tabs/tabs.component.ts` — `scrollable()` is true because the row has 5 tabs
  (`QD_TABS_SEGMENTED_MAX = 3`), so the row is `display: flex; overflow-x: auto`, **not**
  `--segmented` (which would have given tabs `flex: 1 1 0`)

#### Root cause

Three facts compose:

1. **The badge is conditionally rendered, so it is genuinely removed from the DOM during
   loading.** `tabCount()` short-circuits on `loadState().isLoading`. Note that
   `_ayahStudy` is *not* cleared when switching ayahs (`AyahStudyLoadRunner.schedule()` leaves
   the previous view model in place), so `study()?.similaritySummary` still holds usable counts
   at that moment — the badge is dropped because of the `isLoading` guard alone, not because
   the data is gone.
2. **Tabs are intrinsically sized.** `.qd-tabs__tab { flex: 0 0 auto }` and the row is
   `flex-start`-packed with a 766px container against 424.3px of content — there is 341.7px of
   free space, so nothing absorbs the 56px the row loses. The items simply re-pack toward the
   RTL start.
3. **Nothing reserves the badge slot.** The count is not a fixed-width element: even between two
   *loaded* ayahs the badge width changes with digit count — measured `"13"` → tab width
   `115.4px` vs `"5"` → `109.8px`, a further **5.6px** shift per badge, despite `tabular-nums`,
   because `min-inline-size` is only wide enough for one digit.

Secondary contributor **(derived, not measured — the automated window could not be resized)**: at
Compact/Medium the study column is narrower than the 424.3px + 16px of gaps the row needs, so
`overflow-x: auto` engages and `QdTabsComponent`'s `effect(() => selected?.scrollIntoView())`
becomes live. There, the 56px content change also changes the row's scroll offset, compounding
the visible jump.

#### User-visible impact

On every ayah change and on every study reload, two tab labels visibly jump sideways (up to 56px
for the last tab), numbers vanish and reappear, and the row's rhythm changes twice per
interaction. Because the debounce plus dev latency holds the loading state for roughly a second
(see M-3), the jump is slow enough to read as a defect rather than a repaint.

#### Recommended direction

The goal is a tab row whose geometry is fixed from first render through the loaded state, for any
count value.

1. **Keep the count element mounted at all times for the two count-bearing tabs, and vary only
   its appearance.** This is the pattern already used elsewhere in the repo:
   `features/abwab/components/abwab-toolbar/abwab-toolbar.component.html:14-15` keeps the span and
   toggles `qd-tabs__count--empty`. Applied here, the badge would render in an "unknown" state
   while loading rather than unmounting.
2. **Give the count slot a stable inline size independent of its digits** — a `min-inline-size`
   sized for the widest count the endpoint can return (`tabular-nums` is already set, so a `ch`
   based floor is exact). This removes the residual 5.6px digit shift that survives fix (1).
3. **Structural alternative that makes the geometry unconditional:** switch this one tablist to
   the existing shared `layout="grid"` contract
   (`styles/_components.scss:325-329` — `repeat(var(--qd-tabs-grid-columns, 5), minmax(0, 1fr))`,
   whose default of 5 already matches this row). Tab widths then stop depending on content
   entirely, so a badge appearing or disappearing changes nothing. Precedents exist
   (`word-type-table-view-tabs`, `abwab-move-picker`). Trade-off: it changes visual density —
   tabs stretch across the full 766px instead of hugging their labels — and it replaces the
   `--scrollable` overflow behavior with a 5-column grid that needs checking at Compact.
4. **Optional, for continuity rather than geometry:** let `tabCount()` fall through to the
   already-retained `study()?.similaritySummary` while loading instead of returning `null`, so
   the number stays visible across the transition. This alone does **not** fix the problem (digit
   width still varies), and it shows the outgoing ayah's counts during the load, which conflicts
   with the feature's existing "`null` = unknown, `0` = known empty" rule for the child cards
   (`features/mushaf/README.md`). Treat it as a separate decision from the geometry fix.

Recommended combination: **(1) + (2)**, or **(3)** if the density change is acceptable. Adding a
spinner is explicitly not the answer here — the row must not resize at all.

#### Page-specific or shared

**Both, split by which fix is chosen.**

- The conditional `@if` on the badge is **page-specific** (`selected-ayah-section`). Fixing it
  there is contained.
- `.qd-tabs__count`'s intrinsic width and `.qd-tabs__tab`'s `flex: 0 0 auto` are **shared**
  (`styles/_components.scss`, consumed by 19 templates across `mushaf`, `words`, `abwab`, and
  `access-admin`; the other count consumer is `abwab-toolbar`). Widening the count's floor
  globally affects those.
- Switching this instance to `layout="grid"` is **page-specific** in effect (an input on one
  `<qd-tabs>`), even though it exercises a shared contract.

#### Risk of the change

- Fix (1) — **Low.** Feature-local template change; the loading-state semantics of the child
  cards are untouched.
- Fix (2) — **Low to Medium.** Low if the floor is applied to this tablist only; Medium if
  `.qd-tabs__count` is changed globally, because `abwab-toolbar` and the Words tabs would shift
  width too and would need re-verification.
- Fix (3) — **Medium.** Changes the visual proportions of the row at every band and needs
  re-checking at Compact where 5 equal tracks may be too tight; it also removes the horizontal
  scroll affordance the row relies on today at narrow widths.
- Fix (4) — **Medium.** It touches the count semantics the similar-ayahs/mutashabihat placeholder
  reservation depends on; the README is explicit that `null` and `0` are not interchangeable.

---

### Finding M-2 — The route leaves ~1/3 of the viewport unused at Wide-plus

#### Current behavior

At 1920px the Mushaf Reader paints inside a 1440px shell centred in a 1905px content area, and
the Quran text itself occupies 448px — 23.5% of the viewport. The layout stops responding to
viewport width entirely at 1440px: every additional pixel above that becomes dead outer margin.

#### Evidence

Measured at `clientWidth = 1905`:

```
--qd-page-gutter                    : 2.5rem  (40px, Wide-plus)
--qd-page-measure-protected-mushaf  : 90rem   (1440px)
--qd-split-mushaf                   : minmax(0, 40%) minmax(0, 60%)
--qd-split-gap                      : 1.5rem  (24px)
--qd-mushaf-text-column-width       : 28rem   (448px)

.qd-page-shell.mushaf-reader        x = 232.5   w = 1440   padding-inline = 40px
  resolved grid-template-columns    "544px 792px"
  .mushaf-reader__page  (reader)    x = 1088.5  w = 544
  .mushaf-reader__study (study)     x = 272.5   w = 792
  .mushaf-page-view__text-column    x = 1144    w = 448
```

Derived from those measurements:

| Band of horizontal space | px at 1905 | share |
| --- | --- | --- |
| Dead outer margin (shell centring) | 232.5 × 2 = **465** | 24.4% |
| Route gutter (`--qd-page-gutter` × 2) | 80 | 4.2% |
| Unused inside the reader column (544 − 448) | 96 | 5.0% |
| Split gap | 24 | 1.3% |
| Quran text column | 448 | 23.5% |
| Study column | 792 | 41.6% |

**641px (33.6%) of the viewport carries no content.**

Study-side readability, measured on the loaded tafsir body:

```
.study-card__body   width = 700px   font-size = 16.2px   line-height = 31.59px
                    max-inline-size = none   ≈ 72ch
--qd-measure-prose  = 68ch  (token exists, unused here)
```

Affected files:

- `styles/_tokens.scss:211` — `--qd-page-measure-protected-mushaf: 90rem`
- `styles/_tokens.scss:218-219` — `--qd-split-mushaf`, `--qd-split-gap`
- `styles/_tokens.scss:161` — `--qd-mushaf-text-column-width: 28rem`
- `styles/_tokens.scss:286-296` — the `--qd-page-gutter` ramp (32px at Wide, 40px at Wide-plus)
- `styles/_layout.scss:63-65, 89-93, 185-187` — `.qd-page-shell--protected-mushaf`,
  `.qd-page-split`, `.qd-page-split--mushaf`
- `features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.html:2` — the single
  declaration of the page intent and the split
- `features/mushaf/components/mushaf-page-view/mushaf-page-view.component.scss` — `padding: 1rem`
  and `&__text-column { width: min(100%, var(--qd-mushaf-text-column-width)); margin-inline: auto }`
- `features/mushaf/components/_study-card.shared.scss` — `.study-card__body`, no inline-size cap

#### Root cause

Two independent facts, and it matters that they are independent:

1. **The page intent caps at 90rem.** `protected-mushaf` is the narrowest of the four named
   intents (`capped-reading` 72rem, `full-data` 100rem, `split-workspace` 100rem,
   `protected-mushaf` 90rem). Above 1440px viewport the shell is frozen at 1440px, so all extra
   width becomes centring margin.
2. **The split is percentage-based while the Quran column is hard-capped.**
   `minmax(0, 40%) minmax(0, 60%)` means any width the shell gains is divided 40/60 — but the
   reader side cannot use its share, because `.mushaf-page-view__text-column` is
   `min(100%, 28rem)`. Today that already leaves 96px unused inside the reader column.

Consequence: **raising the measure alone does not fix this.** It converts outer whitespace into
reader-column whitespace at a 40% rate. Conversely, changing the split alone leaves the 465px of
dead margin untouched. Both have to move together, and the study side needs a prose cap or the
reclaimed width degrades tafsir readability (already at ~72ch against a 68ch token).

#### User-visible impact

On a 1920px display the reader feels like a narrow strip in the middle of the screen: a quarter
of the window is empty on the sides, the Quran page has visible slack around it inside its own
column, and the study column is simultaneously the widest element on screen and the one whose
prose is longest per line. The imbalance reads as an unfinished layout rather than a deliberate
reading measure.

#### Recommended direction

Ordered, because the order is load-bearing:

1. **Make the reader track content-sized rather than percentage-sized at Wide-plus.** Derive it
   from `--qd-mushaf-text-column-width` plus the page-view padding instead of `40%`, so the Quran
   measure stays byte-for-pixel what it is today and every reclaimed pixel flows to the study
   side. This is the step that makes step 2 useful.
2. **Then let the shell use more viewport above 1440** — either raise
   `--qd-page-measure-protected-mushaf` toward the `full-data`/`split-workspace` 100rem, or make
   it a width-responsive `clamp()`. Without step 1 this only inflates reader slack.
3. **Cap the study prose independently** — apply `--qd-measure-prose` (or an equivalent) to
   `.study-card__body`, so the extra study width goes to the card/list surfaces and the ayah
   frames rather than to ~100ch tafsir lines.
4. **Leave Compact and Medium alone.** `features/mushaf/README.md` records measured geometry for
   those bands, including `.mushaf-reader__page`'s deliberate negative-margin cancellation of the
   Compact gutter (which exists to stop a Madani line from wrapping). Any change here must be
   re-measured at 390 / 768 / 1080 / 1440 / 1920 with the invariant that the page stays at 15
   non-wrapping lines.

#### Page-specific or shared

**Effectively page-specific, but the edits land in global files.**
`--qd-page-measure-protected-mushaf` and `--qd-split-mushaf` are declared in `styles/_tokens.scss`
and `.qd-page-split--mushaf` in `styles/_layout.scss`, but all three are consumed only by the
Mushaf route, and `FRONTEND_UI_RULES.md` §5 names `protected-mushaf` explicitly as
"feature-owned". The prose cap on `.study-card__body` is feature-local. `--qd-page-gutter` is
genuinely shared and should **not** be touched for this. Any change must pass
`npm run check:golden-ui` (single gutter owner, no raw responsive thresholds, band vocabulary).

#### Risk of the change

**Medium.** The reader column is the protected Quran boundary: line count, line heights, word
rects, markers and ligatures are documented as measured constants at 1080 and 1440, and a track
type change can silently invalidate them. The README also warns that the reserved page-area
height baseline invalidates silently if the column-width token or font metrics move. The prose
cap (step 3) is **Low** risk on its own. Step 2 alone, without step 1, is **Low risk but
ineffective** — it would move whitespace rather than remove it.

---

### Finding M-3 — Perceived initial latency is frontend, not API

#### Current behavior

The reader feels slow to fill in on open, and noticeably slow when switching ayah/word. Measured,
the backend is not involved in either.

#### Evidence

**Initial open, cold-ish dev-server cache** (deep link `?word=1:3:1&panel=word&ayah=1:3`,
`PerformanceResourceTiming`, `startTime +duration`):

```
 817ms +  6ms   /api/health                          (footer health widget)
1142ms +  6ms   /api/mushaf/study-sources
1145ms +  7ms   /api/mushaf/pages/1
1148ms +  7ms   /api/mushaf/words/1:3:1/analysis
1150ms +  8ms   /api/mushaf/ayahs/1:3/study
1992ms +  9ms   /api/mushaf/pages/2                  (adjacent-page prefetch)
domContentLoadedEventEnd = 879ms   loadEventEnd = 935ms
```

**Initial open, warm dev-server cache, bare entry `/dashboard/mushaf`** (session restore fired and
rewrote the URL to `?ayah=1:3&word=1:3:1&panel=word`):

```
 492ms +  5ms   /api/health
 780ms + 13ms   /api/mushaf/study-sources
 787ms + 18ms   /api/mushaf/pages/1
 789ms + 17ms   /api/mushaf/words/1:3:1/analysis
 790ms + 17ms   /api/mushaf/ayahs/1:3/study
1294ms +  4ms   /api/mushaf/pages/2
domContentLoadedEventEnd = 540ms
```

**Backend measured directly** (`curl`, warm, `time_starttransfer`):

```
/api/mushaf/pages/1              200   9.0ms / 8.5ms
/api/mushaf/study-sources        200   8.7ms / 9.9ms
/api/mushaf/ayahs/1:5/study      200   9.0ms / 8.9ms
/api/mushaf/words/1:5:1/analysis 200   7.4ms / 8.0ms
```

**Ayah switch, live** (programmatic click on `[data-word-location="1:5:1"]`, `t` relative to the
click):

```
t = 905ms   /api/mushaf/words/1:5:1/analysis   +21ms
t = 907ms   /api/mushaf/ayahs/1:5/study        +25ms
```

**Dev-server shape:** 230 resources on the initial load, of which **220 are individual unbundled
ES modules** served by Vite; last script response at 1727ms. Fonts are not on the critical path
(`amiri-bold.woff2` at 881ms +32ms).

Affected files:

- `src/environments/environment.development.ts:7` — `devApiLatencyMs: 450`
- `src/app/core/data-access/dev-api-latency.ts`, `dev-latency.interceptor.ts`
- `src/app/app.config.ts:42-45` — `withInterceptors([secureUrlInterceptor, authInterceptor(), devLatencyInterceptor])`
- `src/app/app.config.ts:46` — `provideAuth({ config: oidcConfig }, withAppInitializerAuthCheck())`
- `src/app/app.config.ts:39` — `provideZoneChangeDetection({ eventCoalescing: true })`
- `features/mushaf/state/mushaf-ayah-study-load.runner.ts:14` — `AYAH_STUDY_SWITCH_DELAY_MS = 700`
- `features/mushaf/state/mushaf-word-analysis-load.runner.ts:11` — `WORD_ANALYSIS_SWITCH_DELAY_MS = 700`
- `features/mushaf/state/mushaf-reader.facade.ts` — `hydrateFromUrl`, `applyUrlState`,
  `syncSimilarAyahsDetail`, `syncMutashabihatDetail`
- `core/caching/api-response-cache.ts`, `features/mushaf/state/mushaf-reader-cache.ts`

#### Root cause

Attributed against the four categories asked for:

**Backend/API latency — not a contributor.** 5–25ms on the wire from the browser, 7–10ms measured
server-side. Every Mushaf endpoint is single-digit milliseconds warm.

**Frontend orchestration — the dominant real contributor, in three separate places.**

1. **A deliberate 450ms is added to every response in dev.** `devLatencyInterceptor` is registered
   for all HTTP traffic and `environment.development.ts` sets `devApiLatencyMs: 450`. Against a
   7–25ms wire time this is ~95% of the observed wait per request. It is invisible to
   resource timing (it is applied by an RxJS `delay` after the response lands), which is exactly
   why the page can feel slow while the network panel looks instant. It is **dev-only**
   (`environment.ts` sets `0`), so it does not affect production — but it is almost certainly the
   main source of the "slightly slow" impression while developing.
2. **A 700ms unconditional debounce on every switch.** `AyahStudyLoadRunner.schedule()` and
   `WordAnalysisLoadRunner.schedule()` set `isLoading: true` immediately and then wait 700ms
   before issuing the request. Measured: click → request leaves the browser at **905ms**. The
   debounce exists to coalesce rapid keyboard word-stepping
   (`MushafReaderPageComponent.onDocumentKeydown → facade.moveSelectedWord`), but it applies
   identically to a single deliberate click on a new ayah. Note the cache path is *not* penalised:
   `schedule()` calls `applyCached()` before arming the timer, so a previously-loaded target
   resolves instantly.
3. **Similarity data is loaded only when its tab is opened.** `syncSimilarAyahsDetail` /
   `syncMutashabihatDetail` return early unless `ayahTab` matches, so first opening
   `آيات قريبة` / `المتشابهات` always costs a round trip — even though the *counts* already
   arrived with the study payload. Re-opening is instant, because `SimilarAyahsLoadRunner.runLoad`
   consults `MushafReaderCache` first.

**Hypotheses checked and found NOT to be causes** (these were specifically asked about):

- *Unnecessary sequential fetching* — **no.** The four initial requests start within 8ms of one
  another (1142/1145/1148/1150, and 780/787/789/790 warm). They are genuinely parallel.
- *Mushaf rendering waiting on Study data* — **no.** `mushaf-page-area` binds only
  `facade.page()` / `facade.pageLoadState()`, and `hydrateFromUrl` issues `loadPage()` **before**
  `applyUrlState()` schedules the study/word loads. The reader is free to paint first; it simply
  doesn't appear to, because all four responses are held by the same 450ms dev delay and land
  together.
- *Duplicate or redundant requests* — **none observed**, including on the bare-entry
  session-restore path. `bindToRoute` returns early on the restoring emission, so the restore
  navigation does not double-fetch. `MushafStudySourceCatalogStore` guards its own load with
  `loaded`/`loading` flags and fired once.
- *Caching not being used* — **used correctly overall.** `ApiResponseCache` de-duplicates
  in-flight requests via `shareReplay`, has a 48-entry LRU, and adjacent-page prefetch is working
  (`/pages/2` at 1992ms / 1294ms). One asymmetry, noted for completeness rather than as a defect:
  `AyahStudyLoadRunner.runLoad()` does not call `applyCached()` (only `schedule()` does), unlike
  `SimilarAyahsLoadRunner.runLoad()` which does. No visual flash results, because
  `getOrLoad` replays a cached response synchronously within the same change-detection tick.

**Rendering / loading-state delay — a structural contributor, not measured as a bottleneck here.**
The app uses Zone-based change detection. Of the 20 Mushaf components, only 7 are `OnPush`
(`mushaf-page-view`, `mushaf-line`, `mushaf-word`, `mushaf-marker`, `mushaf-page-area`,
`similar-ayahs-card`, `mutashabihat-groups-card`). The three components on the hot path of every
load-state flip — `mushaf-reader-page`, `study-context-section`, `selected-ayah-section` — are all
Default, so each signal write in the facade re-checks that whole subtree. Page 1 renders 37 word
buttons; a dense page renders several times that.

**Dev-environment noise that must be excluded before drawing production conclusions.** ~780–1145ms
elapses before the first data request. Under `ng serve` the app pulls 220 separate ES modules from
the Vite dev server (last one at 1727ms). This does not exist in a production build. What *does*
survive into production from that window is `withAppInitializerAuthCheck()`: the OIDC check runs as
an `APP_INITIALIZER`, so component bootstrap — and therefore every Mushaf request — waits on it.
The measured warm gap from `domContentLoadedEventEnd` (540ms) to the first Mushaf request (780ms)
is ~240ms.

#### User-visible impact

On open, roughly a second passes with skeletons before any real content appears, and all four
regions fill at once rather than the Quran page arriving first. On every ayah/word switch, the
study area sits in its loading state for around a second before the request is even sent —
which is also the window during which the M-1 tab shift is visible.

#### Recommended direction

No performance change should be made from these numbers alone. In order:

1. **Re-measure against a production build** before changing anything, so the 450ms interceptor
   and the 220 dev modules are out of the picture. A large part of the felt latency is expected to
   disappear; what remains is the app-initializer auth check plus the 700ms debounce.
2. **Make the 700ms debounce conditional instead of unconditional.** It exists to coalesce
   keyboard word-stepping; a discrete click on a new ayah does not need it. Either scope it to
   repeated/keyboard-driven changes or shorten it materially.
3. **Start the similarity requests from the already-known counts** rather than on tab open — when
   `study.similaritySummary` reports a non-zero count, the data is going to be needed and the
   cards already reserve count-driven placeholders for it.
4. **Promote `mushaf-reader-page`, `study-context-section` and `selected-ayah-section` to
   `OnPush`.** Their inputs are already signals; this stops a load-state flip from re-checking the
   whole reader.
5. **Decide whether the OIDC app-initializer must block first paint** on a read-only Quran route.
6. `/api/health` (footer widget) is issued ahead of the Mushaf requests but costs 5–6ms and does
   not block anything — no action.

#### Page-specific or shared

- **Page-specific:** the 700ms switch debounce, the tab-lazy similarity loads, and the `OnPush`
  promotions — all inside `features/mushaf/`.
- **Shared/global:** the dev latency interceptor and `devApiLatencyMs`, the OIDC app-initializer,
  and the Zone change-detection provider — all in `app.config.ts` / `core/data-access/` /
  `src/environments/`, affecting every route.

#### Risk of the change

- Debounce change — **Medium.** It is a real guard against a request storm during arrow-key word
  stepping, and it interacts with the F1 stranded-load recovery contract in
  `state/mushaf-url-hydration.ts` (which keys off `isLoading` at rebind time). The coalescing
  behavior on the keyboard path must be preserved.
- `OnPush` promotion — **Low to Medium.** Mechanical, but `selected-ayah-section` writes a signal
  from a `ResizeObserver` callback; under `OnPush` the loading reservation must still schedule
  change detection. Requires visual re-verification of the N3 row 10 reservation.
- Eager similarity loading — **Low**, at the cost of up to two extra requests per ayah selection.
- Touching the dev latency value — **Low** (dev-only), but it is deliberate instrumentation, so
  removing it rather than making it easy to toggle would lose a testing capability.
- Touching `app.config.ts` auth initialization — **High.** It affects every route and the auth
  contract; out of scope for a UI polish pass without a separate decision.

---

## Locked Decisions After Audit

These are **product decisions recorded after the audit above**. They are **planning inputs only** —
none of them has been implemented, and this report does not contain an implementation plan.
Where a decision contradicts a "Recommended direction" written earlier in this report, **the
decision below wins**.

1. **Remove the artificial development API latency completely.**
   `devApiLatencyMs = 450` (`src/environments/environment.development.ts:7`, applied through
   `core/data-access/dev-latency.interceptor.ts` and registered in `app.config.ts`) is no longer
   part of the intended frontend behavior. It is to be removed outright, not merely made
   configurable. Supersedes the softer wording in M-3 "Risk of the change" about preserving it as
   a testing capability.

2. **Remove the unconditional 700ms switch debounce completely.**
   `AYAH_STUDY_SWITCH_DELAY_MS = 700` (`features/mushaf/state/mushaf-ayah-study-load.runner.ts:14`)
   and `WORD_ANALYSIS_SWITCH_DELAY_MS = 700`
   (`features/mushaf/state/mushaf-word-analysis-load.runner.ts:11`) are to be removed, not
   shortened and not made conditional. Supersedes M-3 recommendation 2. Note for whoever plans
   this: the debounce is currently the only coalescing guard on the keyboard word-stepping path
   (`MushafReaderPageComponent.onDocumentKeydown → MushafReaderFacade.moveSelectedWord`), and it
   interacts with the F1 stranded-load recovery contract in `state/mushaf-url-hydration.ts`. Both
   need to be re-examined when the timers go, but the debounce itself is not retained.

3. **Do NOT eagerly preload Similar Ayahs or Mutashabihat.**
   `syncSimilarAyahsDetail` / `syncMutashabihatDetail`
   (`features/mushaf/state/mushaf-reader.facade.ts`) stay lazy: the data is fetched only when its
   tab is opened, because the user may never open those tabs. **Reverses M-3 recommendation 3**,
   which proposed starting those requests from the already-known `similaritySummary` counts. The
   existing `MushafReaderCache` behavior (a re-opened tab resolves from cache) is unchanged.

4. **Do not change OIDC / bootstrap / auth initialization as part of this UI polish work.**
   `withAppInitializerAuthCheck()` in `app.config.ts` stays as-is. **Withdraws M-3
   recommendation 5** from this scope. The ~240ms it costs before the first Mushaf request is
   accepted for now and, if revisited, belongs to a separate decision outside UI polish.

Findings M-1 and M-2 are unaffected by these decisions.

---

Status: AUDITED — AWAITING MORE PAGES
