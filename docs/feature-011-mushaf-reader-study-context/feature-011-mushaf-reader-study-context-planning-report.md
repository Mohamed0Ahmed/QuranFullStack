# Feature 011 — Mushaf Reader Study Context: Planning Report

> Status: Planning / documentation only. No code, no migrations, no Spec Kit files, no database changes, no commits.
> Decisions status: **all v1 decisions are now locked** (see §14). This report is the pre-Spec-Kit lock-in.
> Companion inputs:
> - `Backend/report/database/current-database-tables-and-relationships-report.md` (database baseline, inspected `2026-06-17`).
> - `docs/feature-011-mushaf-reader-study-context/feature-011-ayah-word-data-capability-report.md` (data capability report).

## 1. Executive summary

Feature 011 adds a **dashboard Mushaf Reader page** to the Quran Dashboard (المنهج القرآني). It is a curator/study surface, not a public visitor reader.

The page renders a single real Mushaf page from the seeded `quran_dashboard` database, line by line, using the authoritative Uthmani word text, and lets an admin/teacher:

- **Read a Mushaf page** built from `quran_mushaf_lines` and `quran_words`, with surah-name lines, basmallah lines, ayah lines, and ayah-end markers.
- **Navigate** between pages (1..604) and by surah, with page/surah context shown in a header above the reader.
- **Study a selected ayah** — identity, page/line presence, juz/hizb/rub/sajda, plus one selected/default tafsir, one translation, and one full i3rab entry.
- **Analyse a selected word** — occurrence identity, display forms, root/lemma/stem/head POS, ordered/unique identity counts, and segment-level morphology with simple i3rab, all **lazy-loaded**.
- **See segment structure** rendered as glued, color-linked inline segments inside the analysis panel only — never in the Mushaf page text.

**Final locked shape of v1:**

- **Layout is right-Mushaf / left-study-area.** The Mushaf page area is anchored on the **right** (Arabic-first RTL), and a single **wide study area** sits on the **left**, vertically split: **selected word / segment analysis on top, selected ayah study on bottom**, both visible together on wide desktop. This replaces the earlier center-Mushaf / two-side-panel idea.
- **Translation is included in v1.** The selected-ayah study area always includes tafsir, translation, and full i3rab.
- **Default sources are locked** (configuration-driven, with v1 configured defaults): tafsir `ar-muyassar`, translation `en-sahih-international`, full i3rab `muyassar`.
- **The ayah study API returns the three selected/default sources together in v1** (tafsir + translation + full i3rab in one response); per-tab/per-section loading is deferred.
- **HTML rendering is sanitized by default** (no default `bypassSecurityTrustHtml`; no DB mutation/stripping).
- **Caching is part of Feature 011, but added after the APIs and tests stabilize** — minimal backend `IMemoryCache` (no Redis) and a bounded/simple frontend cache.

The design relies on data that already exists in the database (confirmed by the capability report): 604 pages, 9,046 lines, 83,668 `quran_words` (77,432 readable + 6,236 markers), full morphology and segment i3rab coverage, 84 tafsir sources, 167 translation sources, and 4 full-i3rab HTML sources, all with 6,236-ayah coverage. The word/page responses stay lean and heavy details are lazy-loaded; all important UI state is encoded in the URL using natural Quran keys.

No new data, importers, migrations, or schema changes are required. After review, Feature 011 is ready to proceed to `/speckit.specify`.

## 2. Locked product/UX decisions

The following are decided and not open in v1.

### Page nature and width

- **Dashboard, not public.** This is a dashboard page for Arabic-speaking admins/teachers, not a public visitor Mushaf page.
- **Use available dashboard width.** Lay the page out across the dashboard content width. Do **not** constrain it inside a narrow public-website reading container.

### Final layout (locked)

- **Right: Mushaf page area.** The Mushaf is anchored on the right because this is an Arabic-first RTL dashboard.
- **Left: one wide study area.** A single wide study column on the left, not separate scattered panels.
- **Top of left study area: selected word / segment analysis.**
- **Bottom of left study area: selected ayah study.**
- **Word and ayah sections are visible together on wide desktop** — the user studies a word *inside* its ayah, so both contexts stay on screen.
- **Do not use three independent narrow columns**, and **do not put the Mushaf in the center with separate right/left panels**.
- **`panel` controls focus / responsive drawer state, not exclusive visibility on wide desktop.** On wide desktop both study sections are visible; `panel` drives focus and, on smaller screens, which drawer/tab is active.
- The left study area must be wide enough for fixed-size cards, internal scroll, source selectors, tafsir, translation, full i3rab, word morphology, and segment analysis.

### Header

- **Header above the Mushaf page.** A context header sits above the reader and shows page/surah context:
  - surah name, or the surahs present on the page;
  - juz number(s);
  - hizb number(s);
  - rub number(s);
  - page number;
  - surah/page navigation controls.
- **Navigation lives in the page-number/navigation area.** The header's page/navigation region lets the user move between pages and jump by surah.

### Mushaf text and markers

- **Lines visually align as much as possible** (justified Arabic line layout) as far as CSS/font/shaping allow. The database supplies line/word ordering; pixel-perfect glyph justification is not a database concern (see Risks).
- **Mushaf text comes from `quran_words.text_uthmani`.**
- **Never reconstruct Mushaf text from morphology segments.**
- **Markers sit beside the related ayah.** Sajda/rub/hizb/juz markers appear beside the ayah they belong to.
- **First-line marker rule.** If an ayah spans multiple lines on the current page, the marker is placed on the **first line where that ayah appears on the current page** (`MIN(line_number)` for that ayah on that page).

### Source defaults, loading, rendering, cache (locked)

- **Default source keys (configuration-driven, v1 configured defaults):**
  - Tafsir: `ar-muyassar` — `MushafReader:DefaultTafsirSourceKey`
  - Translation: `en-sahih-international` — `MushafReader:DefaultTranslationSourceKey`
  - Full i3rab: `muyassar` — `MushafReader:DefaultFullI3rabSourceKey`
- **Translation card is in v1.** The selected-ayah study area must include tafsir, translation, and full i3rab.
- **Ayah study API returns the three selected/default sources together in v1** (no per-tab split in v1).
- **HTML rendering is sanitized by default**; no default `bypassSecurityTrustHtml`; never mutate/strip source text in the DB.
- **Cache is part of Feature 011 but added after the APIs and tests stabilize**; backend `IMemoryCache` only (no Redis); frontend cache bounded/simple.

### State and cards

- **Stable card/panel dimensions.** Cards/panels have stable outer dimensions and scroll internally when content is long.
- **URL holds important UI state.** Representable in the URL: `page`, selected ayah, selected word, selected segment (if any), active panel/focus, active tabs, selected tafsir source, selected translation source, selected full i3rab source.

## 3. Locked selected ayah behavior

When an ayah is selected, the ayah study area supports:

**Core ayah identity** (lightweight, loaded with the study request):

- verse key (`quran_ayahs.verse_key`);
- surah number and Arabic surah name (`quran_surahs.name_arabic`);
- ayah number;
- ayah text (`quran_ayahs.text_uthmani`);
- word count (`words_count_real`, with `words_count_source` as fallback);
- page/line presence (`page_from`, `page_to`, plus first line on the current page derived from `quran_words`);
- juz / hizb / rub (denormalized on `quran_ayahs`, backed by division tables);
- sajda when present (`quran_sajdas`).

**Loading model (locked):** the v1 selected-ayah study response **loads the three selected/default sources together** — tafsir, translation, and full i3rab — in one response. Only the selected/default source per kind is loaded (never all sources). Source switching is supported later through selectors.

**Tafsir:**

- load only the selected/default source — never all 84 sources;
- **v1 configured default: `ar-muyassar`** (configuration-driven via `MushafReader:DefaultTafsirSourceKey`).

**Translation:**

- **In v1 the translation card is in scope.**
- load only the selected/default source — never all 167 sources;
- **v1 configured default: `en-sahih-international`** (configuration-driven via `MushafReader:DefaultTranslationSourceKey`). The default is English because **no Arabic translation source exists in the current catalogue** — this is expected, not a blocker.

**Full i3rab:**

- load only the selected/default source;
- HTML can be heavy (and grouped entries can cover multiple ayahs), so this content is sanitized at render time;
- expose grouped/ranged entry metadata so the UI can explain coverage: `sourceValueKind`, `sourceLeaderVerseKey`, `isGroupLeader`, `coveredAyahCount`, `coveredAyahKeys`;
- **v1 configured default: `muyassar`** (configuration-driven via `MushafReader:DefaultFullI3rabSourceKey`; one of `daas`, `darwish`, `jadwal`, `muyassar`).

**Mutashabihat and similar ayahs:**

- data exists (`quran_mutashabihat_*`, `quran_similar_ayah_links`);
- **deferred from v1**; not in the v1 study panel.

## 4. Locked selected word behavior

When a user selects a **readable** word:

- **Marker rows are not selectable** for word analysis. Ayah-end marker rows (`is_ayah_marker = true`) are display glyphs only; the analysis API must reject them.
- **Lazy load** the word analysis only after selection.
- The analysis shows:
  - word location (`quran_words.location`, e.g. `2:25:3`);
  - verse key;
  - surah number;
  - ayah number;
  - word number;
  - page number;
  - line number;
  - line word order;
  - `text_uthmani`;
  - simple/imlaei forms (`text_uthmani_simple`, `text_imlaei_simple`) when useful;
  - word type / head POS (`quran_word_morphology.head_pos` + POS labels);
  - root (`quran_roots`);
  - lemma (`quran_lemmas`);
  - stem (`quran_stems`);
  - case feature (`case_feature`);
  - verb tense (`verb_tense`);
  - voice (`verb_voice`);
  - ordered/unique identity counts (occurrences/ayahs/surahs) when useful.

**Segment display decision (analysis panel only):**

- Render the selected word as **glued colored segments** in the analysis panel.
- Each segment is an inline span with **no inserted spaces** between spans.
- Each segment uses a **visual color slot**.
- Segment color links three places: the segment text in the glued word, the segment data row, and the segment's simple i3rab label.
- Segment colors are **visual-linking colors in v1, not semantic POS colors**.
- If a segment form is empty/null/fragile, **do not invent text**. (The capability report found 208 of 128,219 segment rows with empty/null `form_arabic_normalized`.)
- Fallback must preserve the full word from `quran_words.text_uthmani`: show a placeholder/marker for the empty segment, keep the raw segment metadata visible, and if needed fall back to the whole-word Uthmani text rather than a broken glued render.
- Segment rendering is **only for the analysis panel, never for Mushaf page text**.

## 5. Proposed layout (locked)

The final locked desktop layout anchors the Mushaf on the right and a single wide, vertically-split study area on the left.

```
┌──────────────────────────────────────────────────────────────┐
│ Header: surah/page/juz/hizb/rub context + navigation          │
├──────────────────────────────┬───────────────────────────────┤
│ Left wide study area          │ Right Mushaf page area         │
│                              │                               │
│ ┌──────────────────────────┐ │  Mushaf lines, words, markers  │
│ │ Top: selected word        │ │                               │
│ │ word text                 │ │                               │
│ │ glued colored segments    │ │                               │
│ │ POS/root/lemma/stem       │ │                               │
│ │ segment rows + i3rab      │ │                               │
│ └──────────────────────────┘ │                               │
│ ┌──────────────────────────┐ │                               │
│ │ Bottom: selected ayah     │ │                               │
│ │ tafsir                    │ │                               │
│ │ translation               │ │                               │
│ │ full i3rab                │ │                               │
│ └──────────────────────────┘ │                               │
└──────────────────────────────┴───────────────────────────────┘
```

### Why this layout

- **RTL reading flow favors the Mushaf on the right.** This is an Arabic-first dashboard; the reading surface belongs where the eye starts.
- **The user studies a word inside an ayah**, so the selected-word context and the selected-ayah context should remain **visible together** rather than swapping in and out.
- **It avoids three narrow columns**, which would crush long tafsir/full-i3rab content and the glued segment render.
- **It gives long tafsir/full-i3rab content enough width** in a single wide study column with internal scroll.
- **It suits a dense dashboard workspace** better than a public-reader layout: one reading area, one study area, clear vertical separation of word vs ayah.

### Suggested proportions (design guidance, not backend constraints)

- Right Mushaf page area: **55%–60%** of available content width.
- Left study area: **40%–45%**.
- Inside the left study area:
  - selected word analysis (top): **~35%–40%** of study-area height;
  - selected ayah study (bottom): **~60%–65%** of study-area height.

### Responsive behavior

- **Wide desktop:** right Mushaf + left vertically-split study area (word on top, ayah on bottom), both visible.
- **Tablet:** the study area may collapse or stack; prefer a single visible study section with a toggle/tabs between word and ayah.
- **Mobile:** drawer / bottom-sheet with tabs for word and ayah, opened from the selection.
- **Do not over-polish mobile in v1, but do not break it.**
- **Preserve URL state across all responsive modes.** `panel` drives focus/drawer state on smaller screens but is not exclusive-visibility on wide desktop.

All cards/panels keep stable outer dimensions and scroll internally in every mode.

## 6. Backend API planning

Three lean, read-only endpoints. All responses sit under the standard `ApiResponse.data` envelope per `Backend/.architecture/API_GUIDELINES.md` (Arabic default user-facing messages; English identifiers/property names). DTO shapes below are **proposals**, not implementations.

### 6.1 Mushaf page — `GET /api/mushaf/pages/{pageNumber}`

Returns lean page data only:

- page metadata (page number);
- previous/next page (derived within 1..604);
- surahs on page;
- ayah range on page;
- navigation summary (juz/hizb/rub numbers present on page);
- lines (line number, type, centered flag);
- words per line (location, verse key, word number, line word order, `text_uthmani`, `is_ayah_marker`, qpc glyph for markers);
- ayah markers (`is_ayah_marker = true` rows);
- sajda/rub/hizb/juz markers with first-line placement metadata;
- **no** tafsir / translation / full-i3rab text or HTML;
- **no** word morphology.

Proposed DTO shape (draft):

```jsonc
{
  "pageNumber": 5,
  "previousPageNumber": 4,
  "nextPageNumber": 6,
  "surahs": [
    { "surahNumber": 2, "nameArabic": "البقرة", "firstAyahOnPage": 25, "lastAyahOnPage": 29 }
  ],
  "ayahRange": { "firstVerseKey": "2:25", "lastVerseKey": "2:29" },
  "navigation": { "juzNumbers": [1], "hizbNumbers": [1], "rubNumbers": [1, 2] },
  "lines": [
    {
      "lineNumber": 1,
      "lineType": "ayah",          // ayah | surah_name | basmallah
      "isCentered": false,
      "surahNumber": null,          // populated for surah_name lines
      "words": [
        {
          "wordLocation": "2:25:1",
          "verseKey": "2:25",
          "wordNumber": 1,
          "lineWordOrder": 1,
          "textUthmani": "...",
          "isAyahMarker": false
        }
      ]
    }
  ],
  "markers": [
    { "markerType": "rub", "markerNumber": 2, "verseKey": "2:26", "lineNumber": 4, "wordLocation": "2:26:1" }
  ]
}
```

Marker placement is derived server-side: resolve the related ayah, then place at `MIN(quran_words.line_number)` for that ayah on that page (first-line rule).

### 6.2 Selected ayah study — `GET /api/mushaf/ayahs/{verseKey}/study?tafsirSource=...&translationSource=...&fullI3rabSource=...`

**v1 loading model (locked):** returns the **three selected/default sources together** — tafsir, translation, and full i3rab — in a single response.

- Source query parameters are **optional**; when omitted, the API resolves the **locked config defaults** (`ar-muyassar`, `en-sahih-international`, `muyassar`) via `MushafReader:Default*SourceKey`.
- The response **echoes the resolved source keys** back in `selectedSources` so the UI shows exactly what was used.
- **Per-tab / per-section loading is deferred.** Future work can split this if payload size becomes a real issue, but v1 does not.

Returns:

- core ayah (identity, text, word count, page/line presence, juz/hizb/rub, sajda);
- selected tafsir (single source);
- selected translation (single source) — **included in v1**;
- selected full i3rab (single source, HTML);
- selected-source metadata (`selectedSources` with resolved keys);
- grouped/ranged metadata when relevant (`sourceValueKind`, `sourceLeaderVerseKey`, `isGroupLeader`, `coveredAyahCount`, `coveredAyahKeys`).

Draft shape follows the capability report's "Selected ayah study response" example (`ayah`, `selectedSources`, `tafsir`, `translation`, `fullI3rab`), with all three content blocks populated in v1.

### 6.3 Selected word analysis — `GET /api/mushaf/words/{wordLocation}/analysis`

Returns:

- word occurrence (location, verse key, surah/ayah/word numbers, page/line/line-word-order, `text_uthmani`, simple/imlaei forms, qpc glyph);
- display identity (ordered tashkeel/simple, unique tashkeel/simple, occurrence/ayah/surah counts);
- morphology (head POS + labels, root, lemma, stem, is_verb, verb tense, voice, case feature, head features);
- head POS / word type;
- root / lemma / stem;
- segments with simple i3rab per segment;
- rendered segment metadata for the frontend (`segmentColorSlot`, display text + `displayTextStatus`, POS + label, features, i3rab text + rule signature/family/status).

Must **reject ayah marker rows** (`is_ayah_marker = true`) with a clear not-analyzable response. Draft shape follows the capability report's "Selected word analysis response" example (`word`, `identity`, `morphology`, `renderedWordSegments`).

### DTO placement note

These are read DTOs at the API boundary and should live with the Mushaf reader feature in the appropriate Application/Api layers per `Backend/.architecture/BACKEND_STRUCTURE.md` (feature/bounded-context grouping, not global DTO dumping folders). This report proposes shapes only; exact namespacing is a Spec Kit / implementation decision.

## 7. Frontend planning

Angular dashboard route, Arabic-first RTL, per `Frontend/quran-dashboard-ui/CLAUDE.md`, `PRODUCT.md`, and `DESIGN.md`.

### Route

`/dashboard/mushaf`

### URL query parameters

- `page` — current Mushaf page (e.g. `5`);
- `ayah` — selected ayah verse key (e.g. `2:25`);
- `word` — selected word location (e.g. `2:25:3`);
- `segment` — selected segment location (e.g. `2:25:3:1`);
- `panel` — active focus / responsive drawer state (`ayah | word | sources | none`);
- `ayahTab` — active selected-ayah tab (`tafsir | translation | full-i3rab | links`);
- `wordTab` — active selected-word tab (`morphology | segments | i3rab | identity`);
- `tafsirSource` — selected tafsir source key;
- `translationSource` — selected translation source key;
- `fullI3rabSource` — selected full-i3rab source key.

Use **natural Quran keys** in the URL, never database numeric ids:

- `ayah=2:25`
- `word=2:25:3`
- `segment=2:25:3:1`

`panel` controls **focus and responsive drawer state**, not exclusive desktop visibility — on wide desktop both the word section and the ayah section are visible.

### Component architecture (final layout)

- **`mushaf-reader-shell`** — smart page/shell component. Orchestrates the **right Mushaf area** and the **left study area**, route/query state, API calls, selected ayah, selected word, selected segment, and source selections. It owns the URL↔state mapping and triggers lazy loads. It must stay thin: orchestration logic belongs in a reader state service + per-resource data services, not in the shell file.

Presentational / child components:

- **`mushaf-header-navigation`** — surah/page/juz/hizb/rub context + page/surah navigation.
- **`mushaf-page-area`** — right-side Mushaf container.
- **`mushaf-page-view`** — the rendered page (lines).
- **`mushaf-line`** — one line.
- **`mushaf-word`** — one word.
- **`mushaf-marker`** — sajda/rub/hizb/juz/ayah-end marker display.
- **`study-area-card`** — left study-area container/card wrapper with stable dimensions + internal scroll.
- **`selected-word-section`** — top section of the left study area.
  - **`segment-rendered-word`** — glued colored segments.
  - **`word-morphology-summary`** — POS/root/lemma/stem/identity.
  - **`segment-data-rows`** — per-segment data rows (color-linked to the glued render).
- **`selected-ayah-section`** — bottom section of the left study area.
  - **`tafsir-card`**
  - **`translation-card`**
  - **`full-i3rab-card`**
- **`source-selector`** — reusable selector for tafsir / translation / full-i3rab sources.

Clarifications:

- The shell orchestrates the **right Mushaf area** and the **left study area**.
- The **selected word is the top section**; the **selected ayah is the bottom section**.
- Both are **visible together on wide desktop**.
- `panel` controls **focus/drawer state**, especially on tablet/mobile responsive layouts.

Each card/section owns stable dimensions and internal scroll; segment color slot assignment is shared between `segment-rendered-word` and `segment-data-rows` so colors stay linked.

## 8. Caching strategy

Caching is **part of Feature 011**, but **not the starting point**. Stabilize the APIs and tests first.

### Recommended ordering

1. Implement read APIs **without cache**.
2. Validate DTOs and tests.
3. Add **backend cache**.
4. Implement frontend.
5. Add **frontend request cache** and optional prefetch.

### Backend cache

- Use minimal **`IMemoryCache`** in v1.
- **No Redis** in v1.
- Cache only **successful, immutable read responses** (Quran data does not change at runtime).
- **Do not cache user-specific state.**
- **Do not cache failed/not-found responses** unless explicitly justified later.

Backend cache keys:

- `mushaf:page:{pageNumber}`
- `mushaf:ayah-study:{verseKey}:taf:{tafsirSource}:tr:{translationSource}:i3rab:{fullI3rabSource}`
- `mushaf:word-analysis:{wordLocation}`

The ayah-study cache key **includes all three source keys** because v1 loads the three selected/default sources together; the resolved (default-applied) keys are used in the key so cache entries stay deterministic.

### Frontend cache

- Cache successful page responses by page number.
- Cache ayah study responses by verse key + source keys.
- Cache word analysis responses by word location.
- **Deduplicate concurrent identical requests** (share the in-flight observable/promise).
- Optional **prefetch** of previous/next page after the current page loads.
- Keep the frontend cache **bounded and simple** (e.g. small LRU or capped map); no elaborate eviction in v1.

## 9. HTML/content safety (locked policy)

- **Full i3rab entries are HTML** (`quran_full_i3rab_entries.i3rab_html`, `markup_format = html`).
- **Tafsir/translation may include markup** depending on source.
- **Rendering is sanitized by default.** HTML is sanitized on the way to the DOM (allowlist of tags/attributes); unsafe rendering is not permitted.
- **No default `bypassSecurityTrustHtml`.** Angular's trust-bypass must not be the default rendering path.
- **A controlled trusted-HTML allowlist may be considered later, only if explicitly documented** (specific known-safe source families, deliberate and reviewed).
- **Do not strip or mutate source text in the database.** Sanitization is a render-time concern.
- **Sanitization is an API/frontend concern, not an import-layer concern.** The import layer keeps source HTML intact.

## 10. Testing strategy

### Backend

- page 1, page 5, page 604 (boundaries + a representative interior page);
- page not found / invalid page (e.g. 0, 605, non-numeric);
- line/word ordering (correct `line_number`, `line_word_order`);
- ayah marker rows present and flagged;
- marker placement first-line rule (multi-line ayah → marker on first line on that page);
- **default source resolution uses the locked config defaults** (`ar-muyassar`, `en-sahih-international`, `muyassar`) when source params are omitted;
- ayah study with default sources and with explicitly selected sources;
- **ayah study response includes selected/default tafsir, translation, and full i3rab together** (all three blocks populated in v1);
- grouped/ranged tafsir/full-i3rab metadata exposed correctly (`isGroupLeader`, `coveredAyahKeys`, etc.);
- word analysis for a normal readable word;
- word analysis rejects an ayah marker row;
- word with segments (segments ordered, i3rab present);
- word with empty/null segment form → fallback flag, no invented text, whole-word Uthmani preserved;
- **sanitized HTML policy is represented at the boundary or rendering-contract level**, as appropriate;
- cache hit behavior after the first request (second identical request served from cache; user state never cached).

### Frontend

- route query-state sync (URL ↔ view state both directions);
- page load;
- selected ayah from URL (deep link);
- selected word from URL (deep link);
- selected segment from URL (deep link);
- source selection updates the URL (and re-triggers the right lazy load);
- **desktop layout places the Mushaf on the right and the study area on the left**;
- **the selected word section appears above the selected ayah section**;
- **both sections can be visible together on wide desktop**;
- **`panel` controls focus / responsive drawer, not exclusive desktop visibility**;
- **translation card is present in v1**;
- **selected ayah study loads tafsir + translation + full i3rab together**;
- **HTML rendering uses the sanitized-by-default policy**;
- fixed card dimensions with internal scroll on long content;
- segment colors link the rendered segment and its data row (same slot → same color);
- no segment reconstruction for Mushaf page text (Mushaf text always from `text_uthmani`);
- **responsive fallback preserves URL state** across desktop/tablet/mobile modes.

Test-code self-check (per workspace rules / `test-guard`): test behavior not implementation, mocks target real boundaries only, variants are data-driven, real DTOs/entities constructed not mocked, persistence/query correctness uses real infrastructure, and Quranic test data stays source-safe.

## 11. Scope boundaries

### In scope (Feature 011)

- Dashboard Mushaf Reader route (`/dashboard/mushaf`).
- Backend read APIs (page, ayah study, word analysis).
- Lean page response.
- Selected ayah study (one tafsir + one translation + one full i3rab, returned together in v1).
- Selected word analysis lazy load.
- Segment-colored analysis rendering (visual-linking colors).
- URL state for page/ayah/word/segment/panel/tabs/source keys.
- Minimal backend (`IMemoryCache`) and bounded frontend cache.
- Fixed-size cards/panels with internal scroll.
- Right-Mushaf / left-study-area layout with initial responsive behavior (tablet stack/collapse, mobile drawer).

### Out of scope

- Audio.
- Bookmarks.
- Last-reading persistence.
- User-preference persistence.
- Mutashabihat / similar-ayah panels.
- Gates / ayah doors (أبواب).
- Advanced source browser.
- Multi-source comparison.
- Public visitor Mushaf.
- Glyph/page-font perfect public reader.
- Database cleanup / nullable analysis.
- New importers.
- Editing Quranic data.

## 12. Implementation phase proposal

Phased for phase-by-phase implementation and review. APIs and tests precede cache; cache precedes frontend; frontend caching/polish last.

- **Phase 1 — Backend contracts / read models.** DTO shapes, read-model query strategy, and the **locked source-default configuration keys** (`MushafReader:DefaultTafsirSourceKey` = `ar-muyassar`, `…DefaultTranslationSourceKey` = `en-sahih-international`, `…DefaultFullI3rabSourceKey` = `muyassar`); no endpoints yet.
- **Phase 2 — Mushaf page API.** `GET /api/mushaf/pages/{pageNumber}`, lean response, marker first-line rule, page validation. Tests.
- **Phase 3 — Ayah study API.** `GET /api/mushaf/ayahs/{verseKey}/study`, returning **tafsir + translation + full i3rab together in v1**, with default resolution from locked config and grouped/ranged metadata. Tests.
- **Phase 4 — Word analysis API.** `GET /api/mushaf/words/{wordLocation}/analysis`, morphology + segments + i3rab, marker rejection, empty-segment fallback. Tests.
- **Phase 5 — Backend cache.** `IMemoryCache` over the three endpoints with the agreed keys, **after the APIs and tests stabilize**; cache-hit tests.
- **Phase 6 — Frontend route and page shell.** `/dashboard/mushaf`, **right-Mushaf / left-study `mushaf-reader-shell`**, URL ↔ state mapping, data services skeleton.
- **Phase 7 — Right Mushaf page rendering and navigation.** Lines/words from `text_uthmani`, markers, header context, page/surah navigation in the right Mushaf area.
- **Phase 8 — Bottom selected ayah section.** Inside the left study area: core ayah + tafsir/translation/full-i3rab cards, source selectors.
- **Phase 9 — Top selected word / segment section.** Inside the left study area: morphology, identity, glued colored segments, segment data rows, color linking, fallback.
- **Phase 10 — Frontend cache and URL polish.** Bounded request cache, request dedupe, optional prefetch, deep-link correctness.
- **Phase 11 — Impeccable layout refinement.** Refinement for **this exact final layout** (right Mushaf + left vertically-split study area), Arabic line alignment, stable cards with internal scroll, responsive drawer/tab behavior, RTL polish per DESIGN.md.
- **Phase 12 — Final tests / review / docs.** Full test pass, engineering review, completion docs.

Optional ordering note: Phases 3 and 4 are independent and could be parallelized after Phase 2; Phase 5 should follow whichever of 3/4 lands last.

## 13. Risks and mitigations

- **Right-Mushaf + left-vertical-study density.** Two stacked study sections plus a reading area in one viewport can feel dense. *Mitigation:* enforce the suggested proportions (Mushaf 55–60%, study 40–45%; word ~35–40% / ayah ~60–65% of study height), stable cards, clear vertical separation.
- **Vertical split may squeeze long tafsir/full-i3rab.** The bottom ayah section holds three content cards. *Mitigation:* internal scroll within stable card dimensions; tabs (`ayahTab`) to switch between tafsir/translation/full-i3rab when height is tight.
- **Word section internal scroll.** Words with many segments can overflow the top section. *Mitigation:* internal scroll in the word section; glued render wraps/scrolls without breaking color linking.
- **Responsive drawer/tab behavior must preserve URL state.** Collapsing to drawers/tabs must not lose `page/ayah/word/segment/panel/tabs/source` state. *Mitigation:* single URL↔state source of truth; responsive tests assert state survives mode changes.
- **Payload size.** Tafsir/full-i3rab HTML (especially grouped entries) can be large, and v1 returns all three sources together. *Mitigation:* lean page response; single selected source per kind; per-section loading available as a deferred follow-up if payloads become a real problem.
- **Slow joins.** Ayah study joins span tafsir/translation/full-i3rab tables (hundreds of thousands to ~1M rows). *Mitigation:* always filter by source key + indexed ayah/source joins; never load all sources; rely on existing indexes documented in the baseline.
- **Cache invalidation.** Cached data is immutable Quran content. *Mitigation:* cache only successful immutable reads; in-memory cache clears on restart; never cache user-specific or failed responses.
- **Route-state complexity.** Many URL params. *Mitigation:* single state service in the shell; explicit URL↔state mapping; deep-link tests per param.
- **Arabic line alignment.** Database gives line/word order, not pixel justification. *Mitigation:* frontend concern; test Arabic shaping; accept "as aligned as CSS/font allow" for v1; precomputed layout aids deferred.
- **HTML rendering safety.** i3rab/tafsir markup. *Mitigation:* sanitized-by-default; no default trust-bypass; documented trusted-HTML allowlist only as a future option; never mutate source in DB.
- **Fixed card layout with long content.** Long tafsir/i3rab in stable-size cards. *Mitigation:* internal scroll within stable outer dimensions; verified by test.
- **Segment empty-form fallback.** 208 segment rows have empty/null forms. *Mitigation:* never invent text; placeholder + raw metadata; whole-word Uthmani fallback; explicit test.
- **Smart component size.** The shell can balloon. *Mitigation:* thin shell + state/data services + presentational children; size/responsibility checked in review per backend/frontend structure rules.

## 14. Final recommendation

### Final locked decisions before Spec Kit

- **Tafsir default:** `ar-muyassar` (`MushafReader:DefaultTafsirSourceKey`).
- **Translation default:** `en-sahih-international` (`MushafReader:DefaultTranslationSourceKey`).
- **Full i3rab default:** `muyassar` (`MushafReader:DefaultFullI3rabSourceKey`).
- **Translation card:** in v1.
- **Desktop layout:** right Mushaf page area, left wide study area.
- **Left study area top:** selected word / segment analysis.
- **Left study area bottom:** selected ayah study.
- **Ayah study API:** returns the three selected/default sources together in v1.
- **HTML rendering:** sanitized by default.
- **Cache:** inside Feature 011, after APIs/tests stabilize.

All defaults are configuration-driven; the values above are the v1 configured defaults. The database baseline and capability report confirm all required data exists with full coverage; no new data, importers, migrations, or schema changes are needed.

**Feature 011 is ready for `/speckit.specify`.** No clarification questions remain open; the items previously flagged (default source keys, translation in v1, layout side assignment, ayah study loading granularity, HTML rendering policy) are now locked above.

### Recommended Spec Kit feature title

**Dashboard Mushaf Reader — Right Mushaf, Left Study Area (Ayah Study + Word Analysis)**

(Feature 011, branch `011-mushaf-reader-study-context`.)

### Recommended Spec Kit scope wording

> Add a dashboard-only Mushaf Reader page (`/dashboard/mushaf`) that renders a real seeded Mushaf page from the database using `quran_words.text_uthmani`, with a context header (surah/page/juz/hizb/rub) and page/surah navigation. Use a right-Mushaf / left-study layout: the **Mushaf page area is anchored on the right** (Arabic-first RTL) and a **single wide study area on the left** is vertically split into a **top selected-word / segment analysis** section and a **bottom selected-ayah study** section, both visible together on wide desktop. Provide three lean read APIs: a lean Mushaf page response (lines, words, ayah and division/sajda markers with first-line placement); a selected-ayah study response that **returns the selected/default tafsir, translation, and full i3rab together** (defaults configuration-driven: `ar-muyassar`, `en-sahih-international`, `muyassar`; grouped/ranged metadata exposed; the **translation card is included in v1**); and a lazy-loaded selected-word analysis response (occurrence identity, ordered/unique identity counts, root/lemma/stem/head POS morphology, and segment-level morphology with simple i3rab). Render selected-word segments as glued, color-linked inline spans in the analysis panel only — never in Mushaf text — with safe fallback for empty segment forms. Encode all important UI state in the URL using natural Quran keys (`page`, `ayah=2:25`, `word=2:25:3`, `segment=2:25:3:1`, `panel`, `ayahTab`, `wordTab`, source keys), where `panel` controls focus/responsive-drawer state rather than exclusive desktop visibility. Render tafsir/translation/full-i3rab HTML **sanitized by default** (no default `bypassSecurityTrustHtml`, no DB mutation). Add minimal backend (`IMemoryCache`) caching after the APIs and tests stabilize, with keys `mushaf:page:{pageNumber}`, `mushaf:ayah-study:{verseKey}:taf:{tafsirSource}:tr:{translationSource}:i3rab:{fullI3rabSource}`, and `mushaf:word-analysis:{wordLocation}`, plus a bounded/simple frontend cache with request dedupe and optional prev/next prefetch. Use stable-dimension cards/panels with internal scroll and initial responsive behavior (tablet stack/collapse, mobile drawer/tabs) that preserves URL state. Out of scope: audio, bookmarks, persistence, user preferences, mutashabihat/similar-ayah panels, gates, advanced source browser, multi-source comparison, public visitor reader, glyph-perfect rendering, database cleanup, new importers, and editing Quranic data.

---

## Planning update verification

- Documentation-only update.
- No source code changed.
- No database changes.
- No migrations.
- No Spec Kit artifacts created.
- No imports run.
- No commits made.

*This report is documentation only. No code, Backend/Frontend source, migrations, Spec Kit files, importers, or database changes were created or run, and nothing was committed.*
