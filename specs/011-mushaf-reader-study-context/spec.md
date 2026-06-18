# Feature Specification: Mushaf Reader Study Context

**Feature Branch**: `011-mushaf-reader-study-context`
**Created**: 2026-06-17
**Status**: Draft
**Input**: User description: "Both Backend API and Frontend Angular dev server must run over HTTPS locally, and all normal Frontend API calls must target the HTTPS Backend URL only. Read plan - and according to the best practices of Github's speckit, create the spec, Generation Only. The implementation will be done using a cheaper model, so the specification and everything should be super clear."

<!--
  CONTEXT FOR THE IMPLEMENTER (read this first)
  - This spec turns the locked planning report into requirements. The planning report is:
      docs/feature-011-mushaf-reader-study-context/feature-011-mushaf-reader-study-context-planning-report.md
    and the data-capability report is:
      docs/feature-011-mushaf-reader-study-context/feature-011-ayah-word-data-capability-report.md
  - All product/UX decisions are LOCKED (see "Locked decisions" below). Do not re-open them.
  - This feature only READS the already-seeded local database. It does NOT import, migrate,
    seed, or modify any data, and it does NOT change Quranic source text.
  - The concrete technical design (exact endpoints, response shapes, cache keys, component
    names) lives in the planning report and will be finalized in the plan phase (plan.md).
    This spec states WHAT must be true and WHY, in language a non-specialist can verify.
-->

<!--
  GLOSSARY (plain-language; read this first)
  - Mushaf: the printed-page form of the Quran. A "Mushaf page" is one physical page (there
    are exactly 604 pages in the standard layout used here).
  - Page line: one printed line on a Mushaf page. A page is a stack of lines.
  - Word: one printed word on a line. Each readable word can be studied.
  - Ayah: one Quran verse. Its reference is written "surah:ayah" (e.g., "2:25" = surah 2,
    ayah 25). This is called the verse reference / verse key.
  - Ayah-end marker: the small numbered glyph that ends an ayah. It is shown but is NOT a
    studyable word.
  - Surah: a chapter of the Quran (114 total), each with an Arabic name.
  - Juz / Hizb / Rub: nested reading divisions (30 juz, 60 hizb, 240 rub). Used for context
    and markers.
  - Sajda: one of 15 ayahs marked for prostration.
  - Tafsir: scholarly explanation of an ayah's meaning. Many sources exist; one is shown.
  - Translation: an ayah rendered in another language. Many sources exist; one is shown.
  - Full i3rab: a detailed grammatical analysis of an ayah, stored as formatted (HTML)
    content. Several sources exist; one is shown.
  - Morphology: the grammatical make-up of a word (its type, root, lemma, stem, tense, etc.).
  - Segment: a piece of a single word (e.g., a prefix + stem + suffix glued together).
  - Simple i3rab: a short, simplified grammatical label attached to a segment.
  - Word location: the "surah:ayah:word" reference of a word (e.g., "2:25:3").
  - Segment location: the "surah:ayah:word:segment" reference of a segment (e.g., "2:25:3:1").
  - HTTPS: a secure (encrypted) web connection. "Over HTTPS" = served at an https:// address.
-->

## Locked decisions (authoritative for this feature)

These were decided in the planning report and are fixed inputs to this spec:

1. **Secure local environment**: the dashboard application and its backend data service both run over **HTTPS** in local development, and every normal data request from the dashboard targets the **HTTPS backend address only** (never a non-secure/HTTP address).
2. **Default sources** (configuration-driven; these are the v1 configured values): tafsir `ar-muyassar`, translation `en-sahih-international`, full i3rab `muyassar`.
3. **Translation is included in v1**; the selected-ayah study always offers tafsir, translation, and full i3rab.
4. **Layout**: the Mushaf page area is anchored on the **right** (Arabic-first, right-to-left); a single wide **study area on the left** is split top/bottom — **selected-word analysis on top, selected-ayah study on bottom** — both visible together on wide desktop.
5. **Ayah study loads the three selected/default sources together** in one on-demand load (no per-source/per-tab splitting in v1).
6. **Markup content (tafsir / full i3rab / any translation markup) renders sanitized by default**; raw/unsafe markup injection is not the default path; source content in the database is never altered.
7. **Caching is part of this feature but added only after the read services and their tests are stable**.

## User Scenarios & Testing *(mandatory)*

The primary actor is an **Arabic-speaking dashboard admin / teacher** who studies the Quran by reading a Mushaf page and inspecting an ayah and a word in depth. A secondary actor is the **developer/operator** who runs the dashboard and its data service locally and needs the secure-connection guarantees. The Quran data already exists in the local database from earlier features; this feature presents it for study and does not change it.

### User Story 1 - Read and navigate a Mushaf page (Priority: P1)

An admin opens the Mushaf Reader and sees one real Mushaf page rendered exactly as the data defines it: lines in order, words in order, surah-name and basmallah lines where they belong, ayah-end markers, and the page's juz/hizb/rub/sajda markers beside the right ayahs. A header shows the page's context (surah(s), juz, hizb, rub, page number) and lets the admin move to the previous/next page or jump by surah.

**Why this priority**: This is the core of the feature. Delivered alone, it is already a usable Mushaf reading surface inside the dashboard. Everything else (ayah study, word analysis) hangs off being able to see and navigate a page.

**Independent Test**: Open the reader at a known page (e.g., page 1, page 5, page 604), confirm the lines/words match the data and read correctly right-to-left, confirm the header context is correct, and confirm previous/next/surah navigation moves to the expected page and stays within pages 1–604.

**Acceptance Scenarios**:

1. **Given** the seeded database is available, **When** the admin opens the reader at page 5, **Then** the page's lines render in order, each line's words render in order using the authoritative Uthmani text, and the page text is never rebuilt from word-segment pieces.
2. **Given** a page that contains a surah start, **When** it renders, **Then** the surah-name line and basmallah line appear in their correct places and ayah lines appear as ayah lines.
3. **Given** a page where a juz/hizb/rub/sajda begins, **When** it renders, **Then** the corresponding marker appears beside the related ayah; if that ayah spans multiple lines on this page, the marker appears on the **first** line where that ayah appears on this page.
4. **Given** any page, **When** it loads, **Then** the header shows the surah name(s) on the page, the juz/hizb/rub number(s), and the page number, and offers previous/next-page and jump-by-surah controls.
5. **Given** page 1 (or page 604), **When** the admin tries to go before page 1 (or after page 604), **Then** navigation does not move out of the valid 1–604 range.
6. **Given** an invalid page request (e.g., page 0, page 605, or a non-number), **When** it is opened, **Then** the admin sees a clear "page not found / invalid" state and the app does not crash.

---

### User Story 2 - Secure local environment: HTTPS everywhere, HTTPS-only data calls (Priority: P1)

A developer runs the dashboard and its backend data service locally. Both are served over secure HTTPS connections. The dashboard makes all of its normal data requests to the secure HTTPS backend address only — it never calls a non-secure HTTP address and never produces mixed-content (insecure) requests.

**Why this priority**: This is an explicit, non-negotiable requirement from the feature input and it underpins every data load in User Stories 1, 3, 4, and 5. If data calls could go over an insecure or wrong-scheme address, the whole feature is non-compliant. It is independently testable and gates the rest.

**Independent Test**: Start both apps locally; confirm each is reachable only at an `https://` address; exercise page load, ayah study, and word analysis; observe that every data request the dashboard makes targets the secure HTTPS backend address and that there are zero non-secure (HTTP) or mixed-content data requests.

**Acceptance Scenarios**:

1. **Given** the dashboard is running locally, **When** it is opened, **Then** it is served over a secure HTTPS connection.
2. **Given** the backend data service is running locally, **When** the dashboard requests data, **Then** the request targets the secure HTTPS backend address.
3. **Given** any normal dashboard data request (page, ayah study, word analysis), **When** it is sent, **Then** it uses the HTTPS backend address only — no non-secure HTTP address and no mixed-content request occurs.
4. **Given** the secure backend address is unreachable, **When** the dashboard tries to load data, **Then** it shows a clear Arabic error/empty state and does **not** silently retry over a non-secure connection.

---

### User Story 3 - Study a selected ayah (tafsir + translation + full i3rab together) (Priority: P2)

After selecting an ayah (from the page or via the URL), the admin sees that ayah's study details in the lower part of the left study area: its identity (verse reference, surah name and number, ayah number, text, word count, page/line presence, juz/hizb/rub, and sajda if any), and — loaded together on demand — the selected/default tafsir, the selected/default translation, and the selected/default full i3rab. The admin can switch any of the three sources.

**Why this priority**: This is the first half of the "study context" value. It is independently testable and valuable once a page can be read (US1) over the secure environment (US2).

**Independent Test**: Select an ayah with no source chosen and confirm the configured defaults are used (tafsir `ar-muyassar`, translation `en-sahih-international`, full i3rab `muyassar`) and that all three appear together; then switch each source and confirm the displayed content and the "source used" label update.

**Acceptance Scenarios**:

1. **Given** an ayah is selected with no source specified, **When** its study details load, **Then** the system loads only the configured default source for each of tafsir, translation, and full i3rab, and shows all three together.
2. **Given** an ayah study response, **When** it is shown, **Then** it states which source was actually used for each of tafsir, translation, and full i3rab.
3. **Given** an ayah is selected, **When** study details load, **Then** the system loads only the one selected/default source per kind — it does **not** load all available sources.
4. **Given** the admin switches the tafsir (or translation, or full i3rab) source, **Then** the corresponding content reloads on demand and the "source used" label updates.
5. **Given** a displayed tafsir or full-i3rab entry that actually covers several ayahs (a grouped/ranged entry), **When** it is shown, **Then** the view makes clear which verses it covers and that it is a grouped entry.
6. **Given** ayah study content that contains markup (formatted full i3rab, or markup in tafsir/translation), **When** it renders, **Then** it renders safely (sanitized) and never executes unsafe embedded content.
7. **Given** a configured default source key that does not exist in the data, **When** that kind is requested, **Then** the system shows a clear empty/error state for that kind and does **not** silently substitute a different source.

---

### User Story 4 - Analyze a selected word and its segments (Priority: P2)

After selecting a **readable** word, the admin sees, in the upper part of the left study area: the word's location and identity, its Uthmani text and simple/imlaei forms, its morphology (word type/head part-of-speech, root, lemma, stem, case, verb tense, voice), occurrence/identity counts, and the word rendered as **glued colored segments**. Each segment is color-linked to its data row and its simple i3rab label. Ayah-end markers are not selectable.

**Why this priority**: This is the second half of the "study context" value. It is independently testable once a page can be read (US1) over the secure environment (US2), and complements ayah study (US3).

**Independent Test**: Select a normal multi-segment word and confirm morphology, identity counts, and the glued colored segments all appear with matching colors between each segment, its data row, and its simple i3rab; then attempt to select an ayah-end marker and confirm it is rejected; then open a word that has an empty segment form and confirm the fallback behavior.

**Acceptance Scenarios**:

1. **Given** a readable word is selected, **When** its analysis loads, **Then** it shows the word location, verse reference, surah/ayah/word numbers, page/line/line-word-order, Uthmani text, simple/imlaei forms, morphology (word type/head POS, root, lemma, stem, case, verb tense, voice), and ordered/unique identity counts.
2. **Given** a selected word, **When** the segment view renders, **Then** the word appears as inline segments glued together with no inserted spaces, and each segment uses a color slot that is the same color in the glued word, in the segment's data row, and in the segment's simple i3rab label.
3. **Given** segment colors, **When** they are shown, **Then** they are visual-linking colors only (they do not encode grammatical category meaning).
4. **Given** a segment that has no display form, **When** the word renders, **Then** the system does not invent text: it shows a placeholder for that segment, keeps the segment's raw data visible, and preserves the full word using the authoritative Uthmani text.
5. **Given** an ayah-end marker, **When** the admin tries to select it for analysis, **Then** the system does not treat it as a studyable word and returns/show a clear "not analyzable" result.
6. **Given** any selected word, **When** it is analyzed, **Then** the glued colored segment rendering appears only in the analysis panel and never in the Mushaf reading area.

---

### User Story 5 - Reproduce any view from its URL (Priority: P3)

The admin can share or bookmark a link that reopens the exact same view: the same page, the same selected ayah, the same selected word, the same selected segment, the same active panel/tabs, and the same chosen sources. Reopening the link restores that state.

**Why this priority**: This makes study work shareable and repeatable across sessions and teammates. It is valuable but secondary to actually reading and studying.

**Independent Test**: Set up a specific view (a page, a selected ayah, a selected word, a selected segment, chosen sources, and active tabs), copy the URL, open it in a fresh session, and confirm the same view is restored.

**Acceptance Scenarios**:

1. **Given** a chosen page, selected ayah, selected word, selected segment, active panel/tabs, and chosen sources, **When** the admin copies the URL and reopens it, **Then** the same view is restored.
2. **Given** the URL, **When** it represents references, **Then** it uses natural Quran keys (ayah like `2:25`, word like `2:25:3`, segment like `2:25:3:1`) and not internal database numbers.
3. **Given** a wide desktop view, **When** the active-panel value changes, **Then** both the word section and the ayah section remain visible (the active-panel value drives focus/drawer behavior, not exclusive hiding, on wide desktop).
4. **Given** a tablet or mobile layout, **When** the view is restored from a URL, **Then** the same state is preserved even though panels may collapse to a stacked or drawer/bottom-sheet form.

---

### Edge Cases

- **Invalid / out-of-range page**: page 0, page > 604, or non-numeric → clear not-found/invalid state, no crash, navigation never leaves 1–604.
- **Ayah spanning multiple lines on a page**: division/sajda marker appears on the first line where that ayah appears on the current page.
- **Selecting an ayah-end marker as a word**: rejected as not analyzable.
- **Word with an empty/missing segment form**: no invented text; placeholder + raw data shown; full word preserved from Uthmani text.
- **Grouped/ranged tafsir or full-i3rab entry**: the view explains which verses it covers.
- **Missing configured default source**: clear empty/error state for that kind; no silent substitution.
- **Backend unreachable / secure address down**: clear Arabic error/empty state; never falls back to a non-secure connection.
- **Very long tafsir/full-i3rab content**: stays inside stable card bounds and scrolls internally; layout does not break.
- **Repeat access to the same page/ayah/word**: served faster from cache without showing stale user-specific data.
- **Narrow screens**: study sections collapse to stacked/drawer form; reading and study remain usable; URL state preserved.

## Requirements *(mandatory)*

### Functional Requirements

#### Secure local environment & connectivity

- **FR-001**: The dashboard application MUST run over a secure HTTPS connection in local development.
- **FR-002**: The backend data service MUST run over a secure HTTPS connection in local development.
- **FR-003**: All normal dashboard data requests (page, ayah study, word analysis) MUST target the HTTPS backend address **only**; the dashboard MUST NOT issue normal data requests to a non-secure HTTP address and MUST NOT produce mixed-content (insecure) requests.
- **FR-004**: If the secure backend address is unreachable, the dashboard MUST show a clear Arabic error/empty state and MUST NOT silently fall back to a non-secure connection.

#### Mushaf page reading & navigation

- **FR-010**: The system MUST provide a dashboard reader view that displays one Mushaf page selected by page number, valid range **1–604**.
- **FR-011**: The page MUST render its lines in order, and within each line its words in order, using the authoritative Uthmani word text from the database.
- **FR-012**: The system MUST NOT reconstruct Mushaf page text from morphology segment forms; Mushaf text always comes from the authoritative Uthmani word text.
- **FR-013**: The system MUST correctly render the three line kinds: surah-name lines, basmallah lines, and ayah lines.
- **FR-014**: The system MUST display ayah-end markers within the page.
- **FR-015**: The system MUST display sajda, rub, hizb, and juz markers beside the ayah they belong to. When an ayah spans multiple lines on the current page, the marker MUST be placed on the **first line where that ayah appears on the current page**.
- **FR-016**: A header above the Mushaf MUST show the surah name(s) present on the page, the juz number(s), the hizb number(s), the rub number(s), and the page number.
- **FR-017**: The header MUST provide controls to go to the previous page, go to the next page, and jump by surah, never navigating outside the 1–604 range.
- **FR-018**: The initial page load MUST be lean: it MUST NOT include tafsir, translation, full-i3rab content, or word morphology.

#### Selected ayah study

- **FR-020**: Selecting an ayah MUST load that ayah's study details **on demand** (not as part of the initial page load).
- **FR-021**: Ayah study MUST show the core ayah identity: verse reference, surah number and Arabic surah name, ayah number, ayah text, word count, page/line presence, juz/hizb/rub, and sajda when present.
- **FR-022**: Ayah study MUST load and show, **together in one on-demand load**, the selected/default tafsir, the selected/default translation, and the selected/default full i3rab.
- **FR-023**: For each of tafsir, translation, and full i3rab, the system MUST load only the **one** selected/default source — never all available sources.
- **FR-024**: Default sources MUST be configuration-driven. The v1 configured defaults are tafsir `ar-muyassar`, translation `en-sahih-international`, and full i3rab `muyassar`.
- **FR-025**: The ayah study result MUST report which source was actually used for each of tafsir, translation, and full i3rab.
- **FR-026**: When a displayed tafsir or full-i3rab entry covers more than one ayah (a grouped/ranged entry), the result MUST expose enough metadata for the view to show which verses are covered and that it is a grouped entry.
- **FR-027**: The admin MUST be able to switch the tafsir, translation, and full-i3rab source independently, after which the corresponding content reloads on demand.
- **FR-028**: If a configured/selected source does not exist in the data, the system MUST show a clear empty/error state for that kind and MUST NOT silently substitute a different source.

#### Selected word analysis

- **FR-030**: The admin MUST be able to select a **readable** word; ayah-end markers MUST NOT be selectable for analysis and MUST be rejected as not analyzable.
- **FR-031**: Word analysis MUST load **on demand** (not as part of the initial page load).
- **FR-032**: Word analysis MUST show: word location, verse reference, surah/ayah/word numbers, page/line/line-word-order, Uthmani text, simple/imlaei forms, word type/head part-of-speech, root, lemma, stem, case feature, verb tense, voice, and ordered/unique identity counts.
- **FR-033**: Word analysis MUST render the selected word as glued segments — inline pieces with **no inserted spaces** between them.
- **FR-034**: Each segment MUST use a color slot such that the segment in the glued word, the segment's data row, and the segment's simple i3rab label share the same color.
- **FR-035**: Segment colors MUST be visual-linking colors only; they MUST NOT encode grammatical category meaning in v1.
- **FR-036**: If a segment has no display form, the system MUST NOT invent text. It MUST show a placeholder for that segment, keep the segment's raw data visible, and preserve the full word from the authoritative Uthmani text.
- **FR-037**: The glued colored-segment rendering MUST appear only in the word analysis panel, never in the Mushaf reading area.

#### URL-encoded view state

- **FR-040**: The page URL MUST be able to represent: current page, selected ayah, selected word, selected segment (when present), active panel/focus, active tabs, and the selected tafsir/translation/full-i3rab sources.
- **FR-041**: URL references MUST use natural Quran keys (e.g., ayah `2:25`, word `2:25:3`, segment `2:25:3:1`) and MUST NOT expose internal database numeric ids.
- **FR-042**: Opening a URL that carries view state MUST restore the same view (deep-link and reload reproducibility).
- **FR-043**: The active-panel value MUST control focus / responsive-drawer behavior; on wide desktop it MUST NOT hide the other study section (word and ayah sections both remain visible). The v1 panel value set is `ayah`, `word`, or `none` (a `sources` panel is out of scope for v1).

#### Content safety

- **FR-050**: Tafsir, translation, and full-i3rab content that contains markup MUST be rendered safely (sanitized) by default; unsafe/raw markup injection MUST NOT be the default rendering path, and embedded content MUST NOT be able to execute unsafely.
- **FR-051**: The system MUST NOT alter, strip, or rewrite the source content stored in the database; safety handling applies only at display time.

#### Layout, responsiveness, and stability

- **FR-060**: The reader MUST place the Mushaf page area on the **right** and a single wide study area on the **left**; the left study area MUST be split with selected-word analysis on **top** and selected-ayah study on **bottom**, both visible together on wide desktop.
- **FR-061**: The reader MUST use the available dashboard width and MUST NOT constrain itself to a narrow public-reader container.
- **FR-062**: Cards/panels MUST keep stable outer dimensions and scroll **internally** when their content is long.
- **FR-063**: On tablet the study area MAY stack/collapse; on mobile the study sections MUST become drawer/bottom-sheet form with tabs for word and ayah; URL view state MUST be preserved across all responsive modes.
- **FR-064**: This view is a dashboard page for admins/teachers and MUST NOT be built as a public visitor Mushaf.

#### Performance via on-demand loading and caching

- **FR-070**: The initial page view MUST stay light by deferring all heavy content to the on-demand loads already required by FR-018 (lean page), FR-020 (ayah study on selection), and FR-031 (word analysis on selection); no tafsir/translation/full-i3rab/morphology is fetched until the relevant selection occurs. (This requirement names the resulting performance behavior; it does not add a separate loading trigger.)
- **FR-071**: After the read services and their tests are stable, the system MUST cache successful, immutable read responses (page, ayah study, word analysis) to make repeat access faster.
- **FR-072**: The cache MUST NOT store user-specific state, and MUST NOT cache failed/not-found responses (unless a specific case is explicitly justified).
- **FR-073**: The dashboard MUST avoid duplicate concurrent identical data requests, MAY prefetch the previous/next page after the current page loads, and MUST keep its cache bounded.

#### Read-only data & localization

- **FR-080**: This feature MUST only read the existing seeded database; it MUST NOT import, migrate, seed, edit, or delete any data, and MUST NOT change Quranic source text.
- **FR-081**: User-facing messages MUST be Arabic by default; internal identifiers/keys remain in their canonical (English/natural-key) form.

### Key Entities *(read-only; already present in the seeded database)*

- **Mushaf Page**: one of 604 printed pages; the unit the reader displays. Knows its page number and which surah(s)/ayah(s) appear on it.
- **Page Line**: one printed line on a page, in order, with a kind (surah-name, basmallah, or ayah).
- **Word**: one printed item on a line; either a **readable word** (studyable) or an **ayah-end marker** (shown, not studyable). Carries its Uthmani text and its location.
- **Ayah**: one verse, identified by its verse reference; carries text, word count, page/line presence, and juz/hizb/rub context.
- **Surah**: a chapter (114), with an Arabic name.
- **Navigation Division (Juz / Hizb / Rub)**: nested reading divisions used for header context and page markers.
- **Sajda**: a marked prostration ayah (15 total), with a type.
- **Tafsir Source & Entry**: a scholarly explanation source and its per-ayah (or grouped) explanation content.
- **Translation Source & Entry**: a translation source and its per-ayah translated text.
- **Full i3rab Source & Entry**: a grammatical-analysis source and its per-ayah (or grouped) formatted content.
- **Word Morphology**: the grammatical make-up of a readable word (type/head POS, root, lemma, stem, case, verb tense, voice).
- **Morphology Segment**: a piece of a word, in order, with its own grammatical data and a simple i3rab label.
- **Simple i3rab**: a short, simplified grammatical label attached to a segment.
- **Display Identity (ordered / unique)**: precomputed occurrence and identity counts for a word.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin can open the reader and see a correct, fully rendered Mushaf page for any page from 1 to 604, with lines and words in the right order and reading right-to-left.
- **SC-002**: From any page, the admin can reach the previous page, the next page, or a chosen surah in at most **2** clicks/taps (for a surah jump: one to open the surah/page selector, one to choose), and navigation never leaves the 1–604 range.
- **SC-003**: Selecting an ayah shows its tafsir, translation, and full i3rab **together** (no separate manual step per source), each clearly labeled with the source used.
- **SC-004**: Selecting a readable word shows its morphology and its color-linked segments with matching colors across the glued word, the data rows, and the simple i3rab labels; selecting an ayah-end marker never produces a word analysis.
- **SC-005**: Any reader view can be reproduced exactly by copying its URL and reopening it in a fresh session (same page, ayah, word, segment, panel/tabs, and sources).
- **SC-006**: Both the dashboard and its data service are reachable **only** over secure HTTPS connections locally, and across a full session (page load, ayah study, word analysis) there are **zero** non-secure (HTTP) or mixed-content data requests.
- **SC-007**: The initial page view carries **no** tafsir, translation, full-i3rab, or word-morphology content; those appear only after the admin selects an ayah or a word.
- **SC-008**: Repeated access to the same page, ayah, or word is faster than the first access, with no stale user-specific data shown.
- **SC-009**: Long tafsir/full-i3rab content stays within stable card bounds and scrolls internally; the page layout does not break or overflow.
- **SC-010**: In every case, the Mushaf reading area shows the authoritative Uthmani text and never shows word-segment forms.
- **SC-011**: Markup content (formatted full i3rab and any tafsir/translation markup) renders without executing any unsafe embedded content.
- **SC-012**: For an ayah that spans multiple lines on a page, its division/sajda marker appears on the first line where that ayah appears on that page in 100% of checked cases.
- **SC-013**: The reader remains usable on tablet and mobile (reading plus opening ayah/word study), and URL state is preserved when switching between desktop, tablet, and mobile layouts.

## Assumptions

- The local database seeded by Features 002–010 is present and complete (604 pages; lines and words; full morphology, segments, and simple i3rab; tafsir, translation, and full-i3rab sources with full ayah coverage; juz/hizb/rub/sajda metadata). This feature reads it and does not change it.
- The configured default source keys exist in the data: tafsir `ar-muyassar`, translation `en-sahih-international`, full i3rab `muyassar`. The translation default is English because the catalogue contains no Arabic translation source.
- Default sources are supplied through configuration; the configured values above are the v1 defaults.
- Local development uses trusted development HTTPS certificates for both the dashboard and the backend data service; the exact certificate setup is an implementation detail resolved in the plan phase.
- The dashboard runs inside the existing dashboard shell/layout from earlier feature work; this feature adds the reader view, not a new application shell.
- The detailed technical design (exact endpoints, response shapes, cache keys, and component breakdown) is captured in the planning report and will be finalized in `plan.md`; this spec intentionally states behavior, not implementation.

## Out of Scope

- Audio / recitation playback.
- Bookmarks, last-reading position, and any user-preference persistence.
- Mutashabihat (similar-ayah) and similar-ayah study panels.
- Gates / ayah doors (أبواب).
- Advanced source browser and multi-source comparison.
- Public visitor Mushaf and glyph/page-font perfect public reader.
- Database cleanup, schema changes, migrations, or new importers.
- Editing or correcting any Quranic data.
- Semantic part-of-speech color systems (segment colors are visual-linking only in v1).

## Dependencies

- The seeded local Quran database (read-only) from Features 002–010.
- The existing dashboard application shell/layout and its Arabic-first right-to-left conventions.
- The existing backend API response and localization conventions (Arabic-default user-facing messages; English/natural-key identifiers).
- Trusted local HTTPS development certificates for both applications.
