# Feature Specification: Quran Roots Explorer

**Feature Branch**: `015-roots-explorer`
**Created**: 2026-06-23
**Status**: Draft
**Input**: Combined implementation plan `docs/feature-015-roots-explorer/feature-015-roots-explorer-combined-implementation-plan.md` (built from the capability analysis, read-only verification, and frontend UX contract reports). Read-only feature; no Quran data is changed.

## Overview

The Roots Explorer is a new read-only screen in the dashboard's Words area that lets Arabic-speaking
admins browse every Quranic root (الجذر) and explore, for any selected root, the words, verses,
chapters, lemmas, and stems associated with it. It is a sibling of the existing Unique Words Explorer
and reuses the same calm, scholarly, Arabic-first (RTL) interaction model.

The screen is a **split view**: a roots table on the main area and a **persistent details panel**
beside it that scrolls independently, so an admin can keep the roots list in place while browsing a
root's details. Clicking any numeric count in the table jumps the panel straight to the matching
detail. The feature is strictly read-only over data that already exists.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse and find roots with their summary numbers (Priority: P1)

An admin opens the Roots Explorer and sees a table of all Quranic roots. Each row shows the root text
and eight summary numbers (occurrences, ayahs, surahs, simple words, tashkeel words, lemmas, stems).
The admin can search by root text, sort the list, and page through it. This is the MVP: a usable,
searchable roots overview.

**Why this priority**: The table is the entry point and the summary surface for the whole feature. It
delivers standalone value (a browsable root catalogue with counts) even before any detail panel
exists.

**Independent Test**: Open `/dashboard/words/roots`, confirm the table lists roots with all eight
counts, then search, sort, and paginate and confirm the list updates correctly and the URL reflects
the state (so a refresh restores it).

**Acceptance Scenarios**:

1. **Given** the Roots Explorer is open, **When** the page loads, **Then** a table of roots is shown where every row displays the root text, a UI row number, and the eight summary counts, with no backend identifiers visible.
2. **Given** the table is shown, **When** the admin types Arabic root text into search, **Then** only roots whose text matches are listed (Arabic-normalized contains), and an empty match shows a clear "no results" state.
3. **Given** the table is shown, **When** the admin chooses a sort (mushaf order, occurrences, or alphabetical), **Then** the rows reorder accordingly; the default order is mushaf order.
4. **Given** more roots exist than fit one page, **When** the admin moves to another page, **Then** the next set of roots is shown using the shared pagination control.
5. **Given** any list state (search, sort, page), **When** the admin refreshes or uses browser back/forward, **Then** the same list state is restored from the URL.
6. **Given** the table is shown, **When** the page first loads the list, **Then** no per-root detail data (words, ayahs, surahs, lemmas, stems) is fetched — only the summary counts.

---

### User Story 2 - Inspect the verses where a root appears, with the root's words highlighted (Priority: P2)

The admin clicks a root's المواضع or الآيات count and the details panel opens on the الآيات tab,
listing the verses where the root appears. (Row select alone opens الكلمات per FR-014; count cells
open their mapped tab.) In each verse, the words derived from that
root are visually highlighted. The list is paginated because some roots appear in many verses.

**Why this priority**: Seeing a root in context across the Quran, with its words highlighted, is the
single most valuable insight the feature provides.

**Independent Test**: Click the المواضع or الآيات count of a root and confirm the panel opens on
الآيات, showing a paginated list of verses with the root's words highlighted and distinguishable
without relying on color alone.

**Acceptance Scenarios**:

1. **Given** the table is shown, **When** the admin clicks a root's المواضع count, **Then** the details panel opens on the الآيات tab for that root.
2. **Given** the الآيات tab is open, **When** the verses render, **Then** only the words that belong to the selected root are highlighted in each verse, and the highlight is conveyed by more than color alone (e.g. a marker/label/accessible text).
3. **Given** a root that appears in many verses, **When** the admin opens الآيات, **Then** the verses are paginated and the panel remains responsive (the whole page does not freeze or load every verse at once).
4. **Given** the الآيات tab is open at a given detail page, **When** the admin refreshes or shares the link, **Then** the same root, tab, and detail page are restored.
5. **Given** the details panel is open, **When** the admin scrolls the panel, **Then** the panel scrolls independently and the roots table stays in place.
6. **Given** the URL references a root that does not exist, **When** the page loads, **Then** a controlled "not found" message is shown in the panel and the roots table remains fully usable.

---

### User Story 3 - Explore a root's words and open the existing word details (Priority: P3)

From a selected root, the admin opens the الكلمات tab and switches between two sub-views: بدون تشكيل
(simple, no-diacritics word forms) and بالتشكيل (Quranic/tashkeel word forms). Each word shows its
display text and how many times it occurs within this root. Clicking a word opens that word in the
existing Unique Words detail flow (simple words open the simple flow; tashkeel words open the
tashkeel flow).

**Why this priority**: Connects roots to the existing Unique Words feature and lets admins move from a
root down to a specific word's full profile.

**Independent Test**: Open الكلمات, toggle بدون تشكيل / بالتشكيل, confirm each shows the correct word
list with per-root counts, then click a word and confirm it opens the existing Unique Words detail in
the matching mode.

**Acceptance Scenarios**:

1. **Given** a root is selected, **When** the admin clicks the كلمات بدون تشكيل count, **Then** the الكلمات tab opens on the بدون تشكيل sub-view listing the root's distinct simple word forms.
2. **Given** a root is selected, **When** the admin clicks the كلمات بالتشكيل count, **Then** the الكلمات tab opens on the بالتشكيل sub-view listing the root's distinct tashkeel word forms.
3. **Given** a word list is shown, **When** there are many words, **Then** the list is paginated.
4. **Given** the بدون تشكيل sub-view is shown, **When** the admin clicks a word, **Then** the existing Unique Words simple detail opens for that word.
5. **Given** the بالتشكيل sub-view is shown, **When** the admin clicks a word, **Then** the existing Unique Words tashkeel detail opens for that word.
6. **Given** a word row, **When** it is displayed, **Then** it shows the word's display text and its occurrence count within the selected root (the destination word detail shows the word's overall counts across the whole Quran).

---

### User Story 4 - See which surahs contain (or do not contain) a root (Priority: P3)

From a selected root, the admin opens the السور tab and switches between ورد فيها (surahs where the
root appears, with per-surah occurrence counts) and لم يذكر فيها (surahs where it never appears).

**Why this priority**: Gives a quick distributional picture of a root across the 114 chapters.

**Independent Test**: Click the السور count, confirm ورد فيها lists the surahs with counts, toggle to
لم يذكر فيها, and confirm the two lists together account for all 114 surahs.

**Acceptance Scenarios**:

1. **Given** a root is selected, **When** the admin clicks the السور count, **Then** the السور tab opens on the ورد فيها sub-view listing each surah (by Arabic name) where the root appears, with its occurrence count in that surah.
2. **Given** the السور tab is open, **When** the admin switches to لم يذكر فيها, **Then** the surahs where the root never appears are listed.
3. **Given** both sub-views for a root, **When** their entries are counted, **Then** ورد فيها count plus لم يذكر فيها count equals 114.
4. **Given** a root that appears in every surah, **When** the admin opens لم يذكر فيها, **Then** an empty state is shown (no missing surahs).
5. **Given** the السور sub-views, **When** they load, **Then** each is loaded as a whole list (not paginated), because there are at most 114 entries.

---

### User Story 5 - View a root's lemmas and stems (display only) (Priority: P4)

From a selected root, the admin opens the الصيغ المعجمية (lemmas) tab or the الأصول الصرفية (stems)
tab and sees the list of lemmas / stems associated with that root, each with its text and occurrence
count within the root. These lists are display-only for now (their own detail pages are a future
feature).

**Why this priority**: Completes the morphological picture of a root. Lower priority because the items
are not yet navigable.

**Independent Test**: Open الصيغ المعجمية and الأصول الصرفية for a root, confirm each lists the
expected items with counts, and confirm the items are not interactive (no clickable buttons/links
that do nothing) while still retaining identity for future linking.

**Acceptance Scenarios**:

1. **Given** a root is selected, **When** the admin clicks the الصيغ المعجمية count, **Then** the الصيغ المعجمية tab opens listing the root's lemmas with each lemma's text and its occurrence count within the root.
2. **Given** a root is selected, **When** the admin clicks the الأصول الصرفية count, **Then** the الأصول الصرفية tab opens listing the root's stems with each stem's text and its occurrence count within the root.
3. **Given** the lemmas list for a root, **When** its items are counted, **Then** that count equals the الصيغ المعجمية number shown for the same root in the table (the two must always agree).
4. **Given** the lemmas or stems list, **When** items are displayed, **Then** they are shown as static (non-interactive) list items — there are no fake buttons or links that lead nowhere.

---

### Edge Cases

- **Root in every surah**: لم يذكر فيها is empty (missing-surahs count = 0); the empty state is shown clearly.
- **Very high-frequency root**: A root that appears in roughly two thousand verses must stay responsive via pagination; the verse list never loads in full at once.
- **Shared word form across roots**: A simple word form may belong to more than one root. The word row count is scoped to the selected root; the destination Unique Words detail shows the word's whole-Quran counts. This difference must not corrupt either number.
- **Invalid/unknown selected root in the URL**: The panel shows a controlled "not found" message; the table stays usable; a stale selection is not retried on every later navigation.
- **Empty search**: A search with no matches shows a calm "no results" state, not a blank or error screen.
- **Zero-count cell**: Clicking a count that is zero opens the corresponding tab in a clear empty state (rather than doing nothing silently).
- **Words without a root**: Words that have no root are simply absent from every root view; they are never invented or attributed to a root.
- **Narrow screen**: On small screens the details panel becomes a drawer/sheet that can be dismissed, returning focus to the control that opened it; the experience is not a desktop modal.
- **Refresh during a deep detail state**: Refreshing on a specific root + tab + sub-view + detail page restores exactly that state.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Navigation and layout

- **FR-001**: The system MUST provide a Roots Explorer screen at the route `/dashboard/words/roots` within the existing Words area.
- **FR-002**: On wide screens the system MUST present a split view: a roots table in the main area and a persistent details panel beside it.
- **FR-003**: The details panel MUST scroll independently of the roots table (its own scroll region), so browsing details does not move the table.
- **FR-004**: The system MUST NOT use a modal/dialog as the primary desktop detail experience. On narrow screens the details panel MAY be presented as a dismissible drawer/sheet.
- **FR-005**: The system MUST NOT include an "نظرة عامة" (overview) tab; the table is the summary surface.
- **FR-047**: The Words hub MUST provide a navigation entry — the `الجذور` card — that links to the Roots Explorer at `/dashboard/words/roots`, so admins can reach the screen from the existing Words landing page.

#### Roots table

- **FR-006**: The table MUST display, for each root, these eight columns in this order (semantic meaning): الجذر, المواضع, الآيات, السور, كلمات بدون تشكيل, كلمات بالتشكيل, الصيغ المعجمية, الأصول الصرفية. The visible grid header row MAY use shortened labels for scanability (`بدون تشكيل`, `بالتشكيل`, `الصيغ`, `الأصول` for the last four columns); full semantic meaning MUST remain available via context, `aria-label`, tooltips, or detail-panel labels where applicable.
- **FR-007**: The table MUST show summary numbers only; it MUST NOT load or display any per-root detail data (word lists, verse lists, surah lists, lemma lists, stem lists) as part of listing the roots.
- **FR-008**: The system MUST NOT display backend technical identifiers (root ID, word ID, lemma ID, stem ID, etc.) anywhere in the visible UI. UI row numbers MAY be used to identify rows.
- **FR-009**: The system MUST let the admin search roots by Arabic root text using diacritic-insensitive "contains" matching; a search with no matches MUST show a clear no-results state.
- **FR-010**: The system MUST let the admin sort the roots list by these stable options: `mushaf-order` (default), `occurrences`, and `alpha` (alphabetical by root text).
- **FR-011**: The system MUST paginate the roots list using the shared pagination component/pattern already used elsewhere in the app (it MUST NOT reintroduce a roots-only or words-only pagination control).
- **FR-012**: Each numeric count cell in the table MUST be an actual interactive control (keyboard-operable button), not an inert element.
- **FR-013**: Clicking a count cell MUST open the details panel on the matching tab/sub-view per this mapping: المواضع → الآيات; الآيات → الآيات; السور → السور / ورد فيها; كلمات بدون تشكيل → الكلمات / بدون تشكيل; كلمات بالتشكيل → الكلمات / بالتشكيل; الصيغ المعجمية → الصيغ المعجمية; الأصول الصرفية → الأصول الصرفية.
- **FR-014**: Selecting a root row (other than via a specific count) MUST open the details panel on the الكلمات tab, defaulting to `view=words&wordView=simple` (بدون تشكيل sub-view).

#### Meaning of the counts (data rules — must be testable)

- **FR-015**: المواضع MUST equal the total number of word occurrences derived from the root across the Quran (the root's occurrence count).
- **FR-016**: الآيات MUST equal the number of distinct verses that contain at least one word derived from the root.
- **FR-017**: السور MUST equal the number of distinct surahs that contain at least one word derived from the root.
- **FR-018**: كلمات بدون تشكيل MUST equal the number of distinct simple (no-diacritics) word identities among the root's words.
- **FR-019**: كلمات بالتشكيل MUST equal the number of distinct tashkeel (Quranic-shaped) word identities among the root's words.
- **FR-020**: الصيغ المعجمية (lemmas) MUST count every distinct lemma that appears among the root's words (co-occurrence meaning: a lemma is counted for a root if it appears on any word of that root). The system MUST NOT define this count as "lemmas owned by / dominant to the root", which can differ.
- **FR-021**: الأصول الصرفية (stems) MUST equal the number of distinct stems that appear among the root's words.
- **FR-022**: The الصيغ المعجمية number shown in the table for a root MUST equal the number of items in that root's lemmas tab (the column and the tab MUST always agree).

#### Details panel: tabs and loading

- **FR-023**: The details panel MUST provide these tabs: الكلمات (with sub-views بدون تشكيل and بالتشكيل), الآيات, السور (with sub-views ورد فيها and لم يذكر فيها), الصيغ المعجمية, الأصول الصرفية.
- **FR-024**: The system MUST lazy-load a tab's (or sub-view's) data only when that tab/sub-view becomes active; inactive tabs MUST NOT trigger data loads.
- **FR-025**: The الآيات (verse matches) list MUST be paginated.
- **FR-026**: The الكلمات word lists (both sub-views) MUST be paginated.
- **FR-027**: The السور sub-views (ورد فيها and لم يذكر فيها), the الصيغ المعجمية list, and the الأصول الصرفية list MAY each be loaded as a whole list (no pagination), as they are bounded (surahs ≤ 114; lemmas and stems are small per root).
- **FR-028**: Re-opening a tab/sub-view or returning to a previously loaded detail page within the same selected-root session SHOULD reuse already-loaded data rather than re-fetching it.

#### Verse highlighting

- **FR-029**: In the الآيات tab, the system MUST highlight exactly the words that belong to the selected root within each verse, identified by word identity (word tokens/IDs), NOT by string replacement or text-fragment matching.
- **FR-030**: The system MUST NOT alter, re-spell, or fabricate Quran text when rendering or highlighting; words are shown as stored.
- **FR-031**: The highlight MUST be perceivable without relying on color alone (e.g. an additional marker, style, or accessible label).

#### Words sub-views and navigation to existing word details

- **FR-032**: Each word item in الكلمات MUST show the word's display text and its occurrence count within the selected root.
- **FR-033**: Clicking a word in the بدون تشكيل sub-view MUST open that word in the existing Unique Words simple detail flow; clicking a word in the بالتشكيل sub-view MUST open it in the existing Unique Words tashkeel detail flow. Navigation MUST use the word's identity, not its text.

#### Lemmas and stems (display only)

- **FR-034**: Each lemma item MUST show the lemma text and its occurrence count within the selected root; each stem item MUST show the stem text and its occurrence count within the selected root.
- **FR-035**: Lemma and stem items MUST be presented as static, non-interactive list items for now (no clickable buttons/links that lead nowhere); their identity MUST be retained internally so they can become navigable when their detail screens exist later.

#### URL state and restoration

- **FR-036**: The system MUST represent the following in the URL so refresh, browser back/forward, and shared links restore the exact state: search text, sort, list page, selected root, active tab (view), active sub-view (word sub-view and surah sub-view), and the detail page for paginated detail views.
- **FR-037**: Sub-view state MUST only apply within its parent tab (word sub-view only under الكلمات; surah sub-view only under السور); the detail page MUST only apply to paginated views (الآيات and الكلمات).
- **FR-038**: Clearing the selected root MUST preserve the list state (search, sort, page) and clear only the root/tab/sub-view/detail-page state.
- **FR-039**: When the URL references an unknown or invalid root, the system MUST show a controlled "not found" state in the panel while keeping the roots table usable, and MUST NOT repeatedly retry the invalid selection on subsequent list navigation.

#### States, accessibility, RTL, and data safety

- **FR-040**: The system MUST provide clear, calm states for loading, empty, no-results (empty search), error, and not-found; it MUST NOT show blank screens or fabricate Quran data when data is missing.
- **FR-041**: All interactive elements (count cells, tabs, sub-view toggles, pagination, word links) MUST be keyboard-operable, and the active tab/sub-view and the selected row MUST be perceivable beyond color (e.g. via state markers / accessible labels).
- **FR-042**: The interface MUST be Arabic-first and right-to-left; Quran text rendering MUST remain stable and MUST NOT be animated.
- **FR-043**: The feature MUST be strictly read-only; it MUST NOT modify, import, or regenerate any Quran/morphology data.

#### Backend behavior (observable contract — not implementation)

- **FR-044**: The system MUST serve the roots list (with all eight counts, search, sort, and pagination) and each detail view (words, verse matches, mentioned surahs, missing surahs, lemmas, stems, and a root summary for deep-link restoration) as read-only operations.
- **FR-045**: Stable read responses (the roots summary list without an active search, and per-root detail reads) SHOULD be cached so repeated views are fast; free-text searches MUST NOT create unbounded cached entries. Caching MUST NOT change behavior for any existing cached features.
- **FR-046**: The system MUST emit structured operational logs for roots list and detail requests, recording at least: root identifier, active view, active sub-view, page and page size, sort, whether a search was present, result counts, and elapsed time where measured. Logs MUST NOT contain Quran text, word text, root text, raw search text, or large payloads.

### Key Entities *(include if feature involves data)*

- **Root (الجذر)**: A Quranic Arabic root. Has root text and aggregate relationships to its word occurrences, the verses and surahs it appears in, and the lemmas and stems found among its words. Carries the summary counts shown in the table.
- **Word occurrence**: A single Quran word that derives from a root; located in a specific verse/surah and mapped to a simple word identity and a tashkeel word identity.
- **Simple word identity / Tashkeel word identity**: The distinct word forms a root's words map to (no-diacritics and Quranic-shaped, respectively). These are the link targets into the existing Unique Words Explorer.
- **Verse (آية)**: A Quran verse that contains one or more of the root's words; the unit highlighted in the الآيات tab.
- **Surah (سورة)**: A Quran chapter; for a given root it is either "mentioned" (ورد فيها, with an occurrence count) or "missing" (لم يذكر فيها). There are 114 surahs.
- **Lemma (الصيغة المعجمية)**: A dictionary form that appears among a root's words; counted by co-occurrence; display-only for now, with retained identity for future linking.
- **Stem (الأصل الصرفي)**: A morphological stem that appears among a root's words; display-only for now, with retained identity for future linking.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin can open the Roots Explorer and see the full root catalogue with all eight summary counts per row; the first page of roots becomes visible within 1 second under normal operating conditions.
- **SC-002**: For 100% of roots, every one of the eight counts is shown and matches its defined meaning (occurrences, distinct ayahs, distinct surahs, distinct simple words, distinct tashkeel words, lemmas by co-occurrence, stems).
- **SC-003**: For 100% of roots, the الصيغ المعجمية number in the table equals the number of items shown in that root's lemmas tab.
- **SC-004**: Clicking any count cell opens the correct details tab/sub-view per the defined mapping in 100% of cases, and the requested detail becomes visible within 1 second under normal operating conditions.
- **SC-005**: For a high-frequency root (appearing in roughly two thousand verses), the الآيات tab remains responsive via pagination and never loads all verses at once or freezes the page.
- **SC-006**: In the الآيات tab, exactly the selected root's words are highlighted in each verse (no missed and no extra words), verified against known roots.
- **SC-007**: Refreshing the page, using browser back/forward, or opening a shared link restores the exact state (search, sort, page, selected root, tab, sub-view, detail page) in 100% of cases.
- **SC-008**: From a word in either الكلمات sub-view, the admin reaches the correct existing Unique Words detail (simple from بدون تشكيل, tashkeel from بالتشكيل) in 100% of cases.
- **SC-009**: A keyboard-only admin can reach and activate every count cell, tab, sub-view toggle, pagination control, and word link, and can perceive the active tab/sub-view and selected row without relying on color.
- **SC-010**: No backend technical identifier is visible anywhere in the UI.
- **SC-011**: Listing roots performs no per-root detail data loading; detail data is loaded only when its tab/sub-view is activated.
- **SC-012**: Operational logs for list and detail requests contain the required diagnostic fields and contain no Quran text, word text, root text, or raw search text.

## Assumptions

- The feature is **read-only** over morphology and word data that already exists; no new data import, no data pipeline, no database schema change, and no change to Quran text are required (confirmed by the capability analysis and read-only verification reports).
- The existing **Unique Words Explorer** (Feature 014) exists and is the destination for word clicks; its simple and tashkeel detail flows are reused unchanged.
- The data is **immutable at runtime**; any reseed happens offline and is followed by a restart, so caching stable read responses is safe and needs no runtime invalidation.
- A **shared pagination** component/pattern already exists in the app and is reused for the roots list and paginated detail lists.
- **Lemma and stem detail pages do not exist yet**; lemmas and stems are display-only here, with identity retained so they can become navigable later.
- The **lemmas count uses co-occurrence semantics** (every distinct lemma appearing among a root's words), which equals the precomputed per-root distinct-lemma figure for all roots (verified); the alternative "dominant/owned lemma" count is explicitly not used.
- **Sortable list options are the three keys** `mushaf-order`, `occurrences`, `alpha`; individual numeric columns are not separately sortable in this version.
- **Word-row occurrence counts are scoped to the selected root**; the destination Unique Words detail shows the word's whole-Quran counts (the difference is expected and acceptable).
- **Zero-count cells remain clickable** and open the relevant tab in an empty state, for consistency.
- On wide screens the details panel sits on the reading-end (inline-end) side; on narrow screens it becomes a dismissible drawer. Exact breakpoint and side placement follow the app's existing responsive/RTL conventions.
- Detail page sizes (for verses and word lists) use fixed sensible defaults and are not exposed as user-adjustable settings or URL parameters in this version.
- Users are Arabic-speaking dashboard admins; Arabic is the default UI language.

## Dependencies

- Existing Quran morphology and word data (roots, word morphology links, simple/tashkeel word identities, verses, surahs, lemmas, stems) — read-only.
- Existing Unique Words Explorer (Feature 014) flows and deep-link mechanism.
- Existing shared UI building blocks: pagination control, highlighted-verse rendering, list/loading/empty/error/not-found state primitives, and the app's caching and structured-logging conventions.

## Out of Scope

- Any write, edit, import, or regeneration of Quran/morphology data.
- Lemma detail pages and stem detail pages (future features); here they are display-only.
- An overview ("نظرة عامة") tab.
- Searching roots by anything other than root text (e.g. by lemma, stem, or meaning).
- Part-of-speech, verb-feature, or morphology-segment exploration.
- Any change to the existing Unique Words Explorer behavior (it is only reused as a navigation target).
- Database migrations or new indexes (verification confirmed none are required).
