# Design Preview — flat parchment + green (day mode)

High-fidelity, **static HTML design comps** showing the proposed restyle of the Quran
Dashboard (المنهج القرآني) **before any app code changes**. Every comp is a full screen
(top nav + content + navy footer), RTL, Arabic-first, **day mode only**, with **real
data baked in** — each file opens standalone in a browser (double-click the `.html`
file; no backend, no build step, no JavaScript).

These files are review artifacts only. Nothing under the Angular app was modified.

## Files

| File | What it shows |
|---|---|
| `design-language.html` | The shared system: palette, typography, buttons, fields, chips, tabs, badges, tables, detail lists, ayah cards, states, the one floating-layer shadow, navy footer. |
| `mushaf.html` | Mushaf reader — real page **440** (end of فاطر + start of يس), selected ayah **35:45**, selected word **بِعِبَادِهِۦ** with its real 3-segment analysis, morphology summary, occurrence cards, study tabs with التفسير الميسر content. |
| `roots.html` | Roots explorer — real first page (mushaf order), selected root **ا ل ه** + detail panel (words list, real tab counts). |
| `lemmas.html` | Lemmas explorer — selected lemma **ٱللَّه** + الآيات tab with type-filter chips and real ayah-match cards. |
| `stems.html` | Stems explorer — selected stem **ٱللَّهِ** + الصيغ tab (linked lemmas) and توزيع الأنواع. |
| `unique-words.html` | Unique words (بالتشكيل) — selected word **ٱللَّهِ** + الآيات drilldown with ayah cards. |
| `word-types.html` | Word types — full stack: main-type strip (اسم selected), child chips, presence filters, scope-counts strip (12364 / 1407 / 6968 / 3301), table-view tabs with جذور view, grouped detail for root **ا ل ه**. |
| `assets/preview.css` | The shared stylesheet (tokens + components) all comps link. |
| `fonts/` | Fonts copied verbatim from the app (see below). |

## The direction (locked for these comps)

- **Fully flat.** Hairline borders (`#e7e2d7`) carry all structure. Shadows exist
  **only** on floating layers (menus/popovers/modals — `.pv-pop`). No card shadows,
  no hover lifts, no gradients anywhere.
- **Warm parchment canvas + ink + ONE scholarly green.** Canvas `#f6f4ee`, surface
  `#fffdf8`, reading paper `#fbf8f0`, recess `#f0ede2`, ink `#2b2a26`, muted `#6f6b62`;
  the single accent `#2f6d5f` (AA text shade `#275c50`, tint `#eaf2ee`). Amber
  status `#fbf1dc / #96660f`; calm danger `#f9ece8 / #a44a3f`.
- **Navy is footer-only** (`#13253a`, flat, no gradient) — the one dark anchor, with
  warm off-white text and a sage `#a8c8ba` accent. No navy anywhere else.
- **The green thread** (signature): a 2px green line means "current" everywhere —
  active tab underline, selected table/list row's inline-start edge, the selected
  mushaf word's underline, matched words in ayah cards.
- **Type:** Amiri bold carries page/panel titles (the scholarly voice); IBM Plex Sans
  Arabic 400/700 carries the working chrome; Quran surfaces keep the app's own fonts,
  untouched and never animated.

## Quran font handling (copied from the app, not substituted)

Copied verbatim into `fonts/` from `Frontend/quran-dashboard-ui`:

| File | Family | Renders |
|---|---|---|
| `amiri-regular.woff2`, `amiri-bold.woff2` | Amiri | Quran words on the mushaf surface, ayah text in study/ayah cards, UI display titles (`public/fonts/`). |
| `UthmanicHafs_V22.ttf` | Uthmanic Hafs | **Ayah-end number medallions only** — never Quran words (repo invariant: V22 mis-renders U+06DF; source `src/assets/fonts/quran/`). |
| `quran-common.woff2` | Mushaf Common | Basmallah `﷽` (U+FDFD), the surah-title ornamental frame (ligature trigger `header`), juz-number glyphs (`juz022`). |
| `surah-name-v1.woff2` | Mushaf Surah Name | Page-chrome surah labels (ligature triggers `surah`, `surah035`, `surah036`). |
| `surah-name-v2.woff2` | Mushaf Surah Name V2 | The surah name overlaid on the title frame (`surah036`). |
| `ibm-plex-sans-arabic-*.woff2` | IBM Plex Sans Arabic | UI chrome. |

The mushaf ligature fonts substitute **literal ASCII trigger strings** (e.g.
`surah036`, `header`, `juz022`) into drawn glyphs via OpenType ligatures — exactly how
the app renders them (`features/mushaf/assets/*.ligatures.json`). The comp emits the
same trigger strings styled with the same families.

One comp-only compromise: the multi-color **segment-rendered word** in the word
analysis (`بِعِبَادِهِۦ`) uses per-segment `<span>`s joined with U+200D (ZWJ) to keep
Arabic letter joining; the real app uses the CSS Custom Highlight API. Visual result
matches; implementation will keep the app's approach.

## Real data — source and queries

All content was pulled READ-ONLY from the local dev Postgres (`quran_dashboard`, the
full imported corpus) and baked statically. No writes, no migrations.

- **Mushaf page 440**: `quran_mushaf_pages/_lines`, `quran_words` (15 lines, 121
  words incl. 13 ayah markers, exact `text_uthmani` with tashkeel), `quran_surahs`,
  `quran_juzs`; ligature triggers from the frontend's `*.ligatures.json`.
- **Ayah study 35:45**: `quran_tafsir_*` (التفسير الميسر shown; السعدي also extracted),
  `quran_translation_*` (Saheeh International), `quran_full_i3rab_*` (4 real sources
  extracted), `quran_similar_ayah_links` (16:61), `quran_mutashabihat_*`;
  word analysis from `quran_word_morphology(_segments)` + `quran_pos_tags`
  (بِ حرف جر + عِبَادِ اسم مجرور + هِۦ ضمير متصل للغائب؛ root ع ب د، lemma عَبْد، stem عِبَادِ).
- **Explorers**: lists mirror the backend read models under
  `Backend/infrastructure/.../Persistence/Reads/Quran/Words/` (EfRootsReader /
  EfLemmasReader / EfStemsReader / EfUniqueWordsReader / EfWordTypesReader —
  projections, count semantics, and the real UI default sorts). Each list is the real
  first page (30 rows, mushaf order; word-types: occurrences order, noun scope).
  Totals shown are real: roots 1642, lemmas 4817, stems 11843, unique-tashkeel words
  21294, noun-scope words 12364.
- Detail selections were chosen to be **visible on the rendered first page**, so the
  selected row and its open panel always agree (roots ا ل ه، lemmas ٱللَّه، stems
  ٱللَّهِ، unique ٱللَّهِ، word-types root ا ل ه).

## Divergences from DESIGN.md / PRODUCT.md / UI_STYLE_SYSTEM.md (to reconcile at implementation time)

The current docs lock a **navy + gold + parchment** identity with a soft-shadow
elevation ladder. These comps intentionally diverge; adopting them means updating the
docs on every point below:

1. **Accent color.** Gold (`--qd-accent` family, the One Voice Rule, the locked
   allowed-gold list — DESIGN.md §2, UI_STYLE_SYSTEM §16.3) → replaced by one
   scholarly green. Gold disappears entirely; the allowed-gold list becomes an
   allowed-green list (same discipline, new hue).
2. **Structural/primary color.** Navy `--qd-primary` (primary buttons, brand) →
   green primary buttons + green brand mark; **navy demoted to footer-only**.
3. **Elevation.** The Soft-Elevation Rule and shadow ladder (`--qd-shadow-sm/hover/lg`
   required on cards; hover `translateY(-2px)` lift — DESIGN.md §4, UI_STYLE_SYSTEM
   §15E/§17 card contract) → **fully flat**: no card shadows, no hover lift; the
   shadow ladder collapses to a single floating-layer shadow.
4. **Navbar.** Translucent `--qd-chrome-bg` + backdrop blur + shadow → opaque flat
   surface + hairline bottom border. Active item: gold tint + navy text → green tint
   + green ink pill.
5. **Footer.** Stays the navy anchor, but loses the radial `footer-bg-2` glow and the
   gold gradient top hairline (flat solid navy) and swaps `--qd-footer-accent` gold →
   sage green.
6. **Focus ring.** Gold `--qd-focus-ring`/`--qd-ring` → green ring.
7. **Selected/active doctrine (§16.1).** Same roles, recolored: `--qd-selected-bg`
   gold-tint → green-tint; `--qd-accent-text` navy → deep green; the 2px indicator /
   selected-row edge becomes the green thread.
8. **Mushaf word-selection indicator.** `--qd-mushaf-word-selection-indicator` gold →
   green; selected-ayah recolor uses green.
9. **Radii.** `--qd-radius-md 0.875rem / -lg 1.375rem` → crisper 10px cards / 7px
   controls / pill chips.
10. **Explorer table header.** Navy-tinted mix (`--qd-explorer-table-header-bg`) →
    plain parchment recess.
11. **Segment palette.** The six `--qd-segment-cat-*` data colors re-tuned to
    desaturated parchment-friendly values (function unchanged).
12. **PRODUCT.md Visual Identity.** The "Real Pages prototype (navy + gold +
    parchment) is the visual source of truth" section would be superseded by this
    direction; the register, principles, and anti-references stand unchanged.
13. **Gradients.** The two sanctioned exceptions (footer gradient hairline, optional
    navbar blur) are removed — zero gradients/blur.

**Unchanged and honored:** parchment canvas & warm-neutral rule, Arabic-first RTL,
Amiri + IBM Plex Sans Arabic roles, all Quran-font/rendering invariants (Amiri for
words, Uthmanic Hafs markers-only, ligature fonts, never animate Quran text), calm
motion, WCAG AA intent (green ink `#275c50` on tint passes AA), light+dark theming
remains a goal — these comps preview **day mode only**.

## Known comp limitations

- Static: no interactivity, no JS; hover/focus states exist in CSS, flows don't.
- Day mode only (per the brief); dark theme is future reconciliation work.
- The range-filter panel is shown open with no active bucket (truthful to the
  unfiltered totals shown); association filters are shown resting.
- Pagination last-page numbers appear only where they are real (e.g. 55 for roots);
  detail-list pagination omits totals the data didn't establish.
- Marker pills (juz/hizb/rub/sajda) are hidden — matching the app's current
  `mushaf-page-marker-visibility.ts` behavior.
