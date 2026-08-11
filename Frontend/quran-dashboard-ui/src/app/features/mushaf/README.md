# Mushaf feature (المصحف) — reader

**HOW rules:** `.architecture/UI_STYLE_SYSTEM.md`, `.architecture/FRONTEND_STRUCTURE.md`,
`.architecture/API_INTEGRATION_GUIDELINES.md` (project root). This file is the WHAT.

## What this feature does

Renders the Mushaf page-by-page (Uthmani) and, on selection, a study context for the
chosen ayah/word: tafsir, translation, full/simple إعراب, morphology summary, similar
ayahs, and متشابهات groups. State (page, selected ayah/word, source selection) is URL-synced.

## Render chain & key pieces

- `components/`: `mushaf-page-view → mushaf-page-area → mushaf-line → mushaf-word`
  (+ `mushaf-marker`), plus study cards (tafsir, translation, full-i3rab,
  mutashabihat-groups, similar-ayahs, word-morphology-summary), `selected-ayah-section`,
  `selected-word-section`, `source-selector`, `surah-jump-picker`.
- Ligatures: `mushaf-common-ligature`, `mushaf-surah-name-ligature`,
  `mushaf-juz-number-ligature`, `mushaf-basmallah-display-text` (+ `assets/*.ligatures.json`).
- `state/`: `mushaf-reader.facade.ts` + `mushaf-reader-session.ts` +
  `mushaf-url-hydration.ts` / `mushaf-url-sync.ts` + per-concern load runners
  (ayah-study, mutashabihat, similar-ayahs, word-analysis) + `mushaf-reader-cache.ts` +
  `mushaf-reader-view-mappers.ts` + `segment-color-palette.ts` +
  `mushaf-study-source-catalog.store.ts`. The catalogue store owns the reader-wide
  tafsir/translation/full-إعراب option lists and their load-once guard — reference data
  outside the reader's URL state machine. The facade delegates
  `loadStudySourceCatalog()` / `*SourceOptions` to it and stays its only consumer, so the
  page keeps reading them off `MushafReaderFacade`.
- `utils/`: `segment-uthmani-slices`, `segment-word-highlights`, `mushaf-word-display-text`,
  `morphology-display.labels`, `study-source-catalog.*`,
  `mushaf-verse-key-display`, `mushaf-location-keys`, `surah-jump-catalog.helpers`.
- `models/mushaf.models.ts` — the reader/segment/study view models. Wire DTOs are
  re-exported from `core/api/generated/` (aliased, e.g. `MushafPageDto` ↔ generated
  `MushafPageResponse`); closed vocabularies (line/marker types, text direction,
  source-value kinds, relationship directions) are narrowed via `Omit`-overlays, and
  view models stay hand-written.

## Golden shell (Phase 9)

- **The route declares one page intent and one split.** `mushaf-reader-page.component.html` carries
  `qd-page-shell qd-page-shell--protected-mushaf qd-page-split qd-page-split--mushaf`, so the inline
  gutter (`16 / 24 / 32 / 40px`), the `--qd-page-measure-protected-mushaf` measure and the feature-owned
  split all come from the global layout layer. No component below it may add a second inline
  gutter. `protected-mushaf` and `--qd-split-mushaf` deliberately sit outside the `16/18/20rem` rail
  scale (G03) — the reader column is a page measure, not a rail.
- **The split token must stay gap-safe (Wide).** Through the Wide band `--qd-split-mushaf` is
  `minmax(0, 40%) minmax(0, 60%)`, not `40% 60%`. Two fixed percentage tracks already sum to 100% of
  the shell content box, so tracks plus `--qd-split-gap` overflow it by exactly the gap and push the
  study column outside the route gutter (measured at `40% 60%`: at 1080 the study started at
  `8.02px` against a content box starting at `32px`; at 1440, `16px` against `40px`). With
  `minmax()` the percentages become growth limits, so the reader takes 40% of the content box and
  the study absorbs the gap: 1080 → `400.391 + 576.609 + 24 = 1001`, exactly the content box. The
  reader — the protected side — is the track that does **not** move when `--qd-split-gap` or
  `--qd-page-gutter` is retuned.
- **At Wide-plus the reader track is content-sized, not percentage-sized.** `styles/_tokens.scss`
  re-declares two tokens inside the existing `≥1440` band block: the measure rises to `100rem` and
  `--qd-split-mushaf` becomes
  `minmax(0, calc(var(--qd-mushaf-text-column-width) + 2rem + var(--qd-mushaf-panel-chrome))) minmax(0, 1fr)`.
  The reader therefore asks for exactly what a Madani page needs and every further pixel goes to the
  study side instead of becoming reader slack — a `40%` track spends 40% of any reclaimed width on a
  column that is hard-capped at `--qd-mushaf-text-column-width` and cannot use it. **The
  three addends are a derivation, not a shape:** `28rem` text column + `2rem` `mushaf-page-view`
  inline padding + `--qd-mushaf-panel-chrome` (`1.5rem`) for the chrome that sits between
  the track edge and that padding box — the `mushaf-page-area` hairline pair (`2px`) and the Wide
  panel's own vertical scrollbar gutter (`15px` measured in Chrome/Linux; classic scrollbars run to
  `17px`, overlay platforms to `0`). **That last addend is load-bearing and was measured, not
  guessed:** the reader panel is a scroll container at Wide, so without it the scrollbar eats the
  protected column — a track of exactly `28rem + 2rem` measured `431px` of text column instead of
  `448px`, which is a Quran rendering delta. Anything that changes the page-view padding, the
  page-area border or the panel's scroller must re-measure this allowance. Measured at 1440 content
  box `1345`: `504 + 24 + 817`; at 1920 (content `1905`, shell `1600`): `504 + 24 + 992`. Below
  `1440` nothing moves — the base `90rem`/`40%–60%` values still resolve, and at every viewport under
  `1440` the shell was already viewport-bound rather than measure-bound.
- **Wide reading measure.** The Quran text column is `326px` at 390 (capped by the viewport), `351.39px`
  at 1080 (capped by the 40% reader track) and `448px` at 1440 **and** 1920 (capped by
  `--qd-mushaf-text-column-width`, `28rem`). The 1080 value was `377px` before the Golden shell; the
  `32px` Wide route gutter and the `24px` split gap account for the difference. Line count, line
  heights, word rects, fonts, markers and ligatures are unchanged at every width — the narrower Wide
  measure only removes slack inside the 15 fixed lines. Making the split gap-safe does not narrow it
  further: it takes the 24px off the study side only. Content-sizing the Wide-plus track does not
  narrow it either — it only removes the slack that used to sit *around* the column (`544 → 504` of
  track for the same `448` of text, re-verified word-rect by word-rect on pages 1, 2, 22, 50, 106
  and 604).
- **The study prose is capped independently of the study column.** `.study-card__body`
  (`components/_study-card.shared.scss`, the tafsir / translation / full-إعراب body) carries
  `max-inline-size: var(--qd-measure-prose)`, so widening the study track grows the card, list and
  ayah surfaces rather than the tafsir line length. Without it the body tracked the column: `700px`
  ≈ `72ch` at 1920 before this cap and ≈ `100ch` at the reclaimed width. It binds only where the
  column is wide enough to exceed the measure (1440 and 1920: `691px → 660.44px`, `68ch`); at 390,
  768 and 1080 the body is already narrower and nothing changes.
- **Compact declines the route gutter for the protected canvas.** §1.4 of the Golden geometry locks a
  `16px` Compact gutter, and `.mushaf-reader__page` cancels it —
  `inline-size: calc(100% + 2 * var(--qd-page-gutter))` with `margin-inline: calc(-1 * var(--qd-page-gutter))`,
  so its margin box equals the grid track exactly and cannot overflow at any Compact width. This is
  deliberate, not an oversight: a Madani page is a structural constant (15 non-wrapping lines over
  `--qd-mushaf-text-column-width`), and taking `2 × 16px` off the column at 390 wraps a line (measured:
  column `326px → 294px`, line 4 `42.61px → 84.2px`), which is a Quran rendering delta rather than a
  layout preference. The page shell stays the sole gutter owner; this only declines the gutter for the
  protected column, and the document still never scrolls horizontally. Medium and Wide are unaffected —
  there the column is capped by `--qd-mushaf-text-column-width` or by the 40% reader track, not by the
  viewport. Do not "restore" the gutter here.
- **Bands, not 1024.** Every Mushaf media query now resolves through `styles/_breakpoints.scss`:
  Wide (`≥1080`) is the sticky reader + independently scrolling study; Medium and Compact
  (`≤1079`) are reader-first, study-second in one column. The legacy `1023/1024` and `767` literals
  are gone; the reserved page-area height and the selected-word/ayah baselines follow the same bands.
  Note that the Wide split therefore starts at `1080`, not `1024` — 1024–1079 is a designed Medium
  composition, not a squeezed Wide one.
- **Shared owners around a protected renderer.** The reader/study chrome consumes F05 `qdAction`
  (page navigation, the mutashabihat disclosure), F07 `qd-tabs`/`qdTab` (the five ayah-study tabs),
  F10 `qdResultList` (`quran-result` on the similar-ayah and mutashabihat occurrence lists — the rows
  keep their own ayah-card renderer, G11), F12 `qd-empty-state`/`qd-error-state` and F15
  `qdFloatingLayer` (both pickers). Nothing shared reaches a Quran renderer descendant: `mushaf-line`,
  `mushaf-word`, `mushaf-marker` and `segment-rendered-word` were not touched.
- **Read failures are not alerts.** Every Mushaf failure is a *read* failure and renders through
  `qd-error-state severity="read"` (no `role="alert"`); loading keeps its own sr-only `role="status"`
  announcement, and empties render through `qd-empty-state`. The reader has never had a `qd-state`
  consumer and gained none.
- **D47 hit targets are overlays, not boxes.** The page-jump trigger keeps its printed `2.25rem`
  proportions and expands to `--qd-hit-target-min` through a transparent `::after`, and the
  previous/next actions carry `.qd-hit-target` for the same reason: growing the real box would
  change the Quran page measure and invalidate the measured N3 row 9 reservation below.
- **D37 — morphology segment rows are content.** `segment-data-rows` renders `div`s: no button or
  anchor, no `role`, no `tabindex`, no click/keydown output, no `qd-interactive-surface`, no hover,
  no pointer cursor and no focus ring. Only the morphology colour, number, part of speech and إعراب
  are carried. Do not "restore" an affordance here; there is no segment action to open.
- **D38 stays deferred.** `models/mushaf.models.ts`, `state/mushaf-url-sync.ts`,
  `state/mushaf-url-hydration.ts` and `state/mushaf-reader.facade.ts` were not modified in the Golden
  cycle. `panel`, `wordTab` and `segment` keep their parsing, normalization, hydration, serialization,
  session restore and cache identity byte-for-behavior; no visible consumer was added and no key retired.
- **The study tablist claims `aria-controls` only for the selected tab.** Only one
  `role="tabpanel"` is mounted at a time and its `id` tracks `activeTab()`, so binding every tab's
  `aria-controls` to its own panel id would leave four of five pointing at an element that does not
  exist. The template therefore sets `[attr.aria-controls]` directly (reactive) instead of feeding
  `qdTab`'s one-shot `panelId` input, and `QdTabDirective` leaves an already-present attribute
  alone. The Words detail panels are the opposite case — they mount all panels `hidden`, so every
  `panelId` there resolves.
- **Ids are per instance (D31/D44).** `selected-ayah-section` generates its own tab/panel ids,
  `surah-jump-picker` its listbox and option ids, and `source-selector` its listbox id. Nothing in the
  reader binds a module-level literal id any more, so an embedded study shell and the global detail
  overlay can never point at each other's panel.
- **`selected-ayah-section` owns two stylesheets.** `…component.scss` is the loaded study composition;
  `…states.scss` is the loading chrome (the N3 row 10 reservation, the source placeholder and the
  skeleton line boxes). They are both `styleUrls` of the same component, split by responsibility and to
  keep each file inside the component-SCSS threshold.

## Gotchas / invariants (read before changing)

- **Loading is a skeleton, never visible loading text** (`UI_STYLE_SYSTEM.md` §17
  loading/skeleton system). `mushaf-page-area` shows `qd-panel-skeleton` (`shape="panel"`,
  a neutral rounded block — chrome only) while `loadState().isLoading`; the Arabic
  string "جارٍ تحميل الصفحة..." is the sr-only `role="status"` label, not visible text.
  `selected-ayah-section` / `selected-word-section` render their own inline `.qd-skeleton`
  cells (sized to the study/analysis layout they load into) with the same sr-only
  `role="status"` pattern ("جارٍ تحميل دراسة الآية...", "جارٍ تحميل تحليل الكلمة..."). All
  three skeletons are **loading chrome only** — they never approximate or touch Quran
  text, ayah glyphs, word-segment rendering, or `--qd-font-quran`.
- **Loading never moves the layout** (Feature 030, N3). Every loading state in the reader
  repaints inside the box its loaded content will occupy; reservations apply **only while
  loading** — loaded content always sizes itself.
  - `mushaf-page-area` reserves a **static measured** block size below the 1024px panel
    breakpoint (above it the panel is already fixed), and the panel skeleton stretches to
    fill it instead of collapsing to a 3rem bar. The baseline is derived from the Madani
    page being a structural constant (15 non-wrapping lines over
    `--qd-mushaf-text-column-width`) — see the provenance comment in its `.scss`. Same
    accepted risk as the U1 baselines: it invalidates silently if the Quran font metrics
    or the column-width token change. Pages 1–2 over-reserve slightly.
  - `similar-ayahs-card` / `mutashabihat-groups-card` render **count-driven, card-shaped**
    placeholders (`expectedItemCount` / `expectedGroupCount` + `expectedOccurrenceCount`,
    fed from the already-loaded `study.similaritySummary`). The counts are `number | null`
    and the two states are **not** interchangeable: `null` = unknown (no summary yet, e.g.
    a deep link still resolving) ⇒ fixed fallback run, `0` = known empty ⇒ **no**
    placeholders, so a real zero cannot paint tall shimmer and then collapse into the
    short empty state. An absent study must pass `null`, never `0`.
    They compose the real `qdAyahCard` frame and the real meta/text classes, so
    the placeholder geometry cannot drift from the loaded geometry. Multi-line ayah text
    is unknowable before the load and still grows its card (accepted).
- **Both selected sections reserve their natural size while loading, through one shared
  utility.** Decision **N3-a** said "no shared utility until a third consumer"; the
  Words details content area (audit finding R-2) is that third consumer, so the
  threshold is reached and the contract now lives in
  `shared/layout/loading-size-reservation.ts` as `qdLoadingSizeReservation()`. It is the
  *whole* extraction and nothing more: hold the last known natural block size of a
  content region while it is loading, release it on settle, and invalidate it on an
  inline-size change, via the guarded `ResizeObserver` both Mushaf ports already used —
  plus a settle capture, because a section that settles at the height its skeleton already
  had never fires a second callback (`shared/README.md` holds the mechanism).
  It stores **numeric geometry only** — never prior text and never Quran DOM. What stays
  with the caller is what was always page-specific: which element is the reservation
  host, what "settled" means for that resource, and the per-band baseline floor the
  reservation is `max()`-ed against in CSS.
  - **Accepted trade-off, inherited from both original ports and now a property of the
    shared utility:** the reservation holds the **previous** entity's height while a
    **different** entity loads. The last successful size is the only honest predictor of
    the next one, so a section switching between two entities of very different heights
    holds stale geometry until the new one settles. This is deliberate — the alternative
    is the collapse-and-jump the reservation exists to prevent.
  - **`selected-word-section`** (Feature 029, U1): holds `min-block-size: max(baseline,
    last natural)` so the divider/next section below it never moves. Its loading skeleton
    still renders the **previous segment count** (fallback 3 on first load) with geometry
    matched to the loaded cells — that count is the component's own state, not the shared
    utility's, which carries geometry alone. The responsive baseline is measured, not
    invented (333px wide bands / 495px under the 768px morphology-grid breakpoint). Because
    the loaded section normally settles *on* that baseline, its recorded natural size and its
    floor coincide (measured: 333.05px natural, `333.046875px` reserved at 1440px), so the
    reservation is live but geometrically inert here — it earns its keep only when a word's
    analysis grows the section past the floor.
  - **`selected-ayah-section`** (Feature 030, N3 row 10): a loaded tafsir/translation/إعراب
    has an arbitrary height, so the same reservation applies with the same `--loading`
    class scoping — the scoping is load-bearing, because the reservation sits over three
    layered min-heights (the component's own, the `<1024px`/`<768px` embedded overrides in
    `styles/_components.scss`, and the reserved var). Its per-band baseline resolves
    through `--qd-ayah-study-min-height`, so it follows each band's study floor and can
    never reserve *less* than the loaded floor.
  - Reservation clears on success/error/empty; loaded content always sizes itself.
- **The study tab strip holds its inline geometry too, and never shows a stale count.**
  The strip is `qd-tabs layout="tracks"` with `--qd-tabs-track-floor: 7.75rem` on
  `.selected-ayah-section__tabs` — the floor clears the widest label (`المتشابهات`) plus its
  count slot, so the five tabs are equal grid tracks whose width never follows content, and a
  container too narrow for five wraps a tab instead of growing a scroller. The two
  count-bearing tabs keep their `.qd-tabs__count` **mounted at all times** and mark it
  `--unknown` while the study loads: `tabCount()` still returns `null` for `isLoading`, so the
  outgoing ayah's numbers are never repainted onto the incoming one, and the `null` = unknown /
  `0` = known empty semantics the similarity placeholders read stay honest. The slot's own
  two-digit floor lives in the golden layer (`.qd-tabs__count`), so a count crossing from one
  digit to two moves nothing either.
- **Word hover is CSS-only and word-scoped** (Feature 030 N7, rescoped by M1): hovering (or
  keyboard-focusing) a word paints `--qd-mushaf-word-hover-bg` behind **that one word** — it
  does **not** fan out across the ayah. There is deliberately **no hover state in TypeScript**:
  it is the word button's own `:hover` / `:focus-visible`, so there is no `hoveredVerseKey`
  signal, no `ayahHover` output, and nothing to reset on page change. Do not re-lift it into a
  signal — the ayah-wide version (030 N7) is exactly what M1 removed, because an 8%
  ayah-wide wash sat only ΔL 0.016 from the selected word and buried it among its neighbours.
  `focusAyah` (the click/URL-synced `--highlighted-ayah` text-color state) is a separate
  concern and keeps its own semantics and cache identity. The wash is **background-color +
  radius only** — it never touches glyphs, fonts, padding, or line metrics — is gated behind
  `@media (hover: hover)` so a touch tap can't stick it, excludes the ayah-marker glyph via
  `:not(:disabled)`, and always loses to the selected word.
- **The selected word is the page's one persistent mark** (M1): it paints
  `--qd-mushaf-word-selection-bg` + a `--qd-mushaf-word-selection-ring` hairline and is
  excluded from the hover wash, so it never dims or animates while the pointer moves. Its
  `transition: none` is load-bearing — a transition here reads as a flash.
- **The canvas < hover < selection ladder, and why its rungs cannot be read off its
  percentages.** Both washes tint the *same* indicator — `--qd-mushaf-word-selection-indicator`,
  which is `var(--qd-accent)` in both themes (`styles/_tokens.scss:78`, `styles/_themes.scss:63`),
  so the ladder resolves per theme automatically (gold-into-navy in dark). But the two washes mix
  that indicator into **different bases**: hover is `8%` into `--qd-bg`
  (`styles/_tokens.scss:93`) while selection is `28%` into `--qd-surface`
  (`styles/_tokens.scss:94`). The bases work against the gap, so comparing `8%` to `28%` tells
  you nothing about the resulting rungs — **re-measure in-browser rather than nudging the
  percentages by eye.**
  The light canvas is `--qd-bg` `oklch(0.967 …)` and the dark canvas `oklch(0.189 …)`
  (`styles/_tokens.scss:5`, `styles/_themes.scss:3`).
  **Why this ladder is allowed its own hover fill at all** (`.architecture/UI_STYLE_SYSTEM.md`
  §16.1 otherwise mandates one shared hover fill): the shared `--qd-surface-hover`
  `oklch(0.945 …)` (`styles/_tokens.scss:9`) sits only **ΔL 0.022** below the `--qd-bg` canvas
  `oklch(0.967 …)` — exact by subtraction, not a measurement — and that is imperceptible on
  parchment. **This 0.022 describes the SHARED token being displaced, not a rung of this
  ladder.** The ladder's own hover rung is `--qd-mushaf-word-hover-bg`, a `color-mix` result
  whose measured value sits further from the canvas; it is one of the numbers recorded in the
  `styles/_tokens.scss` comment. Do not reconcile the two figures — they measure different
  colours against the same canvas.
  Both washes are scoped to the ONE word under the pointer and never fan out across the ayah —
  hover is applied on the word element itself
  (`components/mushaf-word/mushaf-word.component.scss:39`), selection at `:52`.
  The measured rung values and the calibration history that produced them are **not derivable
  from code** and remain in the `styles/_tokens.scss` comment above the tokens.
- **Mushaf font is Amiri** (`public/fonts/` + `assets/fonts/quran/`) — **not**
  `UthmanicHafs_V22`, which mis-renders mark **U+06DF** as baseline dots. Do not swap the
  Mushaf font.
- **Display text is Uthmani**; segment slicing/highlighting operates on the Uthmani string.
  Search/normalize is a separate concern (`shared/quran/arabic-search-normalize`).
- **Selected-word identity links open the global detail overlay** (Feature 029, Change B):
  the root/lemma/stem/unique anchors in `selected-word-section` / `word-morphology-summary`
  and the new word-type link are real `a[qdDetailLink]` anchors carrying typed `v1~…`
  frames over the current Mushaf base (no more forced new tabs; modifier clicks keep
  browser behavior). The word-type frame comes from the pure
  `utils/word-type-detail-frame.adapter.ts` (locked §5.7 mapping: `contextCode` =
  verb tense (`'unspecified'` when null) for verbs / `headPos` otherwise, always
  `case=tense=voice=all`, view `ayahs` — the complete type row, never the clicked
  occurrence's narrowed features; underivable identity ⇒ plain non-interactive label).
- **Ayah-shaped list items use the shared `qdAyahCard` frame** (Feature 029, Change A):
  Similar Ayahs items and Mutashabihat occurrences compose `shared/ui/ayah-card` for the flat
  surface/hairline/radius/padding frame (the Mutashabihat selected occurrence layers a
  `--qd-border-accent` hairline on top). The frame is presentation-only — the
  `toStudyAyahDisplayText` display mapping, verse-key display, `ayahNavigate` outputs, and all
  Quran text rendering stay feature-owned and unchanged.
- Browser-only APIs such as `matchMedia` and `ResizeObserver` remain guarded, with a desktop
  fallback when they are unavailable.
- URL-state (`mushaf-url-sync`) is a shareable contract — keep params stable. The global
  detail overlay's `qdDetail*` keys are a different owner riding the same URL (Feature
  029, B7): `isBareMushafEntry` treats a URL whose only params are overlay keys as bare
  (session restore still fires), and the facade's session-restore navigation merges query
  params so a retained overlay stack survives restoration.

## Related

- Backend: `Backend/.../Persistence/Reads/Quran/MushafReader/*`, MushafReader handlers.
- Contracts: this README + the components/services here are the truth; the thin index is
  `docs/contracts/mushaf-reader.md`. The planning artifacts of the reader/similarities
  features were swept per the planning-artifact lifecycle rule and live in git history.
