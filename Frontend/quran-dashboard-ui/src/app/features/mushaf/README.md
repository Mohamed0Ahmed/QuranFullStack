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
- **Mushaf font is Amiri** (`public/fonts/` + `assets/fonts/quran/`) — **not**
  `UthmanicHafs_V22`, which mis-renders mark **U+06DF** as baseline dots. Do not swap the
  Mushaf font.
- **Display text is Uthmani**; segment slicing/highlighting operates on the Uthmani string.
  Search/normalize is a separate concern (`arabic-search-normalize`).
- **jsdom lacks `matchMedia` / `ResizeObserver`** under the vitest builder — guard them and
  default to desktop (many components use responsive/observer logic).
- URL-state (`mushaf-url-sync`) is a shareable contract — keep params stable.

## Related

- Backend: `Backend/.../Persistence/Reads/Quran/MushafReader/*`, MushafReader handlers.
- Specs: `specs/011-mushaf-reader-study-context/`, `012-mushaf-ayah-similarities/`.
  (Prior docs evidence reports for feature 011/012 were purged.)
