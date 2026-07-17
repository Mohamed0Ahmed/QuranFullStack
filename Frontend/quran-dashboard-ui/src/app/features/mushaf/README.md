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
  `mushaf-reader-view-mappers.ts` + `segment-color-palette.ts`.
- `utils/`: `segment-uthmani-slices`, `segment-word-highlights`, `mushaf-word-display-text`,
  `morphology-display.labels`, `arabic-search-normalize`, `study-source-catalog.*`,
  `mushaf-verse-key-display`, `mushaf-location-keys`, `surah-jump-catalog.helpers`.
- `models/mushaf.models.ts` — the reader/segment/study view models. Wire DTOs are
  re-exported from `core/api/generated/` (aliased, e.g. `MushafPageDto` ↔ generated
  `MushafPageResponse`); closed vocabularies (line/marker types, text direction,
  source-value kinds, relationship directions) are narrowed via `Omit`-overlays, and
  view models stay hand-written.

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
- **Selected-word loading reserves its natural size** (Feature 029, U1): while word
  analysis loads, `selected-word-section` holds `min-block-size: max(baseline, last
  natural)` so the divider/next section below it never moves. The last successful
  block size and segment count are recorded by a guarded `ResizeObserver` (numeric
  geometry only — old text/Quran DOM is never retained), the skeleton renders that
  segment count (fallback 3 on first load) with geometry matched to the loaded
  cells, and the responsive baseline is measured, not invented (333px wide bands /
  495px under the 768px morphology-grid breakpoint). Reservation clears on
  success/error/empty; loaded content always sizes itself.
- **Ayah hover is component-local, never URL-synced** (Feature 030, N7): hovering (or
  keyboard-focusing) any word paints `--qd-mushaf-ayah-hover-bg` behind every word of that
  ayah. The `hoveredVerseKey` signal is owned by `mushaf-page-view` and flows down the
  existing input chain (page-view → line → word) with an `ayahHover` output back up — it is
  **never** in the facade and **never** in the URL; `focusAyah` (the click/URL-synced
  `--highlighted-ayah` text-color state) keeps its own semantics and cache identity. The
  signal **resets on page change** (verse keys are global, so a stale key would paint a
  phantom wash on the next page). The wash is **background-color + radius only** — it never
  touches glyphs, fonts, padding, or line metrics — is gated behind `@media (hover: hover)`
  so a touch tap can't stick it, excludes the ayah-marker glyph (mirrors the
  `isHighlightedAyahWord` exclusion), and always loses to the selected-word wash.
- **Mushaf font is Amiri** (`public/fonts/` + `assets/fonts/quran/`) — **not**
  `UthmanicHafs_V22`, which mis-renders mark **U+06DF** as baseline dots. Do not swap the
  Mushaf font.
- **Display text is Uthmani**; segment slicing/highlighting operates on the Uthmani string.
  Search/normalize is a separate concern (`arabic-search-normalize`).
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
- **jsdom lacks `matchMedia` / `ResizeObserver`** under the vitest builder — guard them and
  default to desktop (many components use responsive/observer logic).
- URL-state (`mushaf-url-sync`) is a shareable contract — keep params stable. The global
  detail overlay's `qdDetail*` keys are a different owner riding the same URL (Feature
  029, B7): `isBareMushafEntry` treats a URL whose only params are overlay keys as bare
  (session restore still fires), and the facade's session-restore navigation merges query
  params so a retained overlay stack survives restoration.

## Related

- Backend: `Backend/.../Persistence/Reads/Quran/MushafReader/*`, MushafReader handlers.
- Specs: `specs/011-mushaf-reader-study-context/`, `012-mushaf-ayah-similarities/`.
  (Prior docs evidence reports for feature 011/012 were purged.)
