# Feature Specification: Words Explorers Enhancements (Word Types Parity, Filters, Statistics)

**Feature Branch**: `026-words-explorers-enhancements`
**Created**: 2026-07-14
**Status**: Draft
**Input**: Derived from the authoritative implementation plan at
`docs/feature-026-words-explorers-enhancements/plan.md` (intentional plan → spec order;
the plan is the decision record — this spec restates its WHAT/WHY and must not be read
as overriding any Locked Decision A1–D, Non-Goal, or acceptance criterion there).

## Clarifications

### Session 2026-07-14

- Q: Which preset bucket thresholds should the count-range filter chips use? → A: Plan
  defaults with disjoint boundaries — occurrences 1 · 2–10 · 11–100 · 101–1000 · 1001+;
  ayahs/surahs 1 · 2–10 · 11–50 · 51+ (surahs ≤ 114); word/lemma/stem sub-counts
  1 · 2–5 · 6–20 · 21+; every
  metric row also offers "مخصّص" (custom min/max). URL stores the actual range, so
  thresholds remain tunable later without breaking links.
- Q: Where should the Word Types four-count summary strip be placed? → A: Between the
  type-filter strip and the table-view tabs (filters → scope summary → tabs → table);
  it must not break the page's mounted-shell behavior.
- Q: How is the headline result-count phrased in Arabic? → A: Label-prefix form
  "عدد الـ…: N" — عدد الكلمات: N / عدد الجذور: N / عدد الصيغ المعجمية: N /
  عدد الأصول الصرفية: N. Sidesteps تمييز number-agreement with dynamic
  digits; dimension named explicitly per terminology lock D.

## Context & Why

The dashboard has five read-only Words explorers: Unique Words (بالتشكيل/بدون تشكيل),
Roots (الجذور), Lemmas (الصيغ المعجمية), Stems (الأصول الصرفية), and Word
Types (أنواع الكلمات). Four of them already give researchers a fast, searchable,
1000-row browsing experience with 100-item detail lists. Word Types — the page
researchers use to study grammatical categories — lags behind: no search, 25-row pages,
25-item detail lists. Separately, none of the explorers can narrow by the counts they
already display (e.g. "show only roots that appear in more than 100 ayahs"), and none
answers the researcher's first question — "how many results am I looking at?" — without
reading the pagination control.

This feature (1) brings Word Types to parity with its siblings, (2) adds filters built
only on data the pages already show, and (3) surfaces honest counts: a headline result
total on the four normal explorers, and a four-count scope summary on Word Types.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Search Word Types by word text (Priority: P1)

A researcher on the Word Types page wants to find a specific word inside the currently
selected grammatical scope (e.g. nouns in the accusative case) by typing part of the
word, exactly as they already do on the other four explorers.

**Why this priority**: Search is the largest parity gap; without it, locating one word
among tens of thousands of rows is effectively impossible.

**Independent Test**: Open Word Types, select any type scope, type a word fragment —
the table narrows to matching words; clear it — the full scope returns.

**Acceptance Scenarios**:

1. **Given** an active type scope on the words view, **When** the researcher types a
   fragment, **Then** after a short pause the table shows only word rows whose
   *word identity text* (the clean, tashkeel-insensitive form) contains the fragment,
   and the list returns to its first page.
2. **Given** an active search, **When** the researcher switches to the roots, stems, or
   lemmas view, **Then** those views show only the roots/stems/lemmas *of the matching
   words* — the search narrows every view of the same scope (CONFIRMED decision; not
   open).
3. **Given** any view, **Then** the search input is visible and its placeholder names
   the word grain (it searches words, e.g. "ابحث في الكلمات"), so grouped views stay
   honest about what was searched.
4. **Given** a search that matches nothing, **Then** the table shows its existing empty
   state and the result totals show zero.
5. **Given** a shared link containing the search text, **When** it is opened, **Then**
   the same narrowed result appears; **Given** browser Back/Forward, **Then** the
   previous search state restores exactly.
6. Search never matches root/stem/lemma display text — only word identity text.

---

### User Story 2 - Browse Word Types in 1000-row pages (Priority: P1)

A researcher browsing a grammatical scope wants to scan the whole result in a few large
pages instead of clicking through dozens of 25-row pages, matching the other explorers.

**Why this priority**: Paging fatigue is the second parity gap; the other four
explorers already serve 1000 rows per page.

**Independent Test**: Select a large scope (e.g. all verbs); the first page holds up to
1000 rows and scrolls smoothly; pagination reflects the larger page size.

**Acceptance Scenarios**:

1. **Given** a scope with more than 25 entries, **When** the list loads, **Then** up to
   1000 rows are served on one page for all four views (words/roots/stems/lemmas).
2. **Given** a 1000-row page, **When** the researcher scrolls, **Then** scrolling stays
   responsive (no jank) and row numbering, selection highlight, and the
   statistic-buttons behavior are unchanged.
3. **Given** the existing page invariants (table shell, tabs strip, and details host
   staying mounted through transitions), **Then** they all still hold at the new page
   size.

---

### User Story 3 - Read 100 detail items per page in Word Types (Priority: P1)

A researcher who opened a word's ayahs (or a grouped root/stem/lemma's member words or
ayahs) wants 100 items per detail page — the same depth the other explorers give.

**Why this priority**: Completes parity; 25-item detail pages force four times the
clicks during verification work.

**Independent Test**: Open any detail list from a Word Types row; one page shows up to
100 items with working pagination.

**Acceptance Scenarios**:

1. **Given** a selected word, **When** its ayahs view opens, **Then** up to 100 ayahs
   appear per page, each with its canonical verse reference (e.g. 2:255), highlighted
   matched words, and the existing per-ayah actions.
2. **Given** a selected grouped root/stem/lemma, **When** its member-words or ayahs
   view opens, **Then** up to 100 items appear per page.
3. **Given** existing shared links with a detail page number, **Then** they keep
   working (page numbering semantics unchanged).

---

### User Story 4 - See the result total on the four normal explorers (Priority: P2)

A researcher on Unique Words, Roots, Lemmas, or Stems wants one visible headline
number: how many entities the current search/filters produce.

**Why this priority**: Cheapest high-value addition — the number already exists in the
paged result; it only needs to be surfaced.

**Independent Test**: Open any of the four pages; a labeled count appears; type a
search — the count changes to the filtered total.

**Acceptance Scenarios**:

1. **Given** no search or filters, **Then** the headline stat equals the unfiltered
   total of that page's entities.
2. **Given** an active search and/or filters, **Then** the stat equals exactly the
   active filtered result total (the same total the pagination is built from) and
   updates whenever search or filters change.
3. **Given** the list is loading, **Then** the stat shows a non-interactive placeholder;
   **Given** the list errored, **Then** the stat does not show a stale or misleading
   number (the page's error state owns the message); **Given** zero results, **Then**
   the stat shows 0.
4. The stat's Arabic phrasing (decided — see Clarifications) is the label-prefix form
   "عدد الـ…: N", naming the entity the page actually counts (terminology lock below):
   عدد الكلمات: N / عدد الجذور: N / عدد الصيغ المعجمية: N /
   عدد الأصول الصرفية: N.

---

### User Story 5 - Narrow the four normal explorers by their own counts (Priority: P2)

A researcher wants to filter Unique Words, Roots, Lemmas, and Stems by the count
columns those pages already display — e.g. "roots occurring more than 100 times",
"unique words appearing in exactly one surah" — using quick preset buckets or a custom
min/max.

**Why this priority**: Turns the explorers from browsing tools into query tools using
only data already on screen.

**Independent Test**: On Roots, pick an occurrences bucket — the table and headline
stat narrow; enter a custom min/max — same behavior; share the URL — the filter
restores.

**Acceptance Scenarios**:

1. **Given** any of the four pages, **When** the researcher picks a preset bucket for a
   metric, **Then** the list shows only entities whose count falls in that range, the
   list returns to page one, and the headline stat updates. Preset buckets (decided —
   see Clarifications): occurrences 1 · 2–10 · 11–100 · 101–1000 · 1001+;
   ayahs/surahs 1 · 2–10 · 11–50 · 51+ (surahs capped at 114); word/lemma/stem
   sub-counts 1 · 2–5 · 6–20 · 21+. The URL stores the actual range, not the bucket
   identity, so threshold tuning later is not a contract change.
2. **Given** the "custom" option (مخصّص), **When** min and/or max are entered, **Then**
   the same narrowing applies; either bound may be left open.
3. Filterable metrics per page are exactly the counts that page already shows:
   Unique Words — occurrences, ayahs, surahs; Roots — occurrences, ayahs, surahs,
   simple words, tashkeel words, lemmas, stems; Lemmas — occurrences, ayahs, surahs,
   simple words, tashkeel words, stems; Stems — occurrences, ayahs, surahs, simple
   words, tashkeel words.
4. **Given** a shared/bookmarked URL with filters, **Then** the identical filtered view
   restores; **Given** a malformed filter value in the URL (e.g. min greater than max,
   non-numeric), **Then** the filter is treated as absent (fails closed) and the page
   still loads; a directly submitted invalid range is rejected with a clear Arabic
   message.
5. **Given** active filters combined with search and sort, **Then** all compose (AND
   semantics) and the stat reflects the combined result.
6. Range filters describe the entity grain of their page honestly: on Unique Words they
   filter unique word identities; on Roots/Lemmas/Stems they filter dimension entries.

---

### User Story 6 - Narrow Word Types by root/stem/lemma presence (Priority: P2)

A researcher on Word Types wants to show only word entries that have (or lack) an
associated root, stem, or lemma — e.g. to isolate particles without roots or to study
only lemma-bearing entries.

**Why this priority**: The only new Word Types filter possible from existing data
without new derivations; complements the parity work.

**Independent Test**: Select "has root = missing" — only rows without a root remain;
grouped views and (later) the four-count summary reflect the same narrowed scope.

**Acceptance Scenarios**:

1. **Given** an active scope, **When** a has-root/has-stem/has-lemma choice is made
   (each tri-state: any / has / missing), **Then** the words view shows only matching
   word entries and the list returns to page one.
2. **Given** the same choice, **When** the researcher switches to grouped views,
   **Then** those views are built from the same narrowed scope (the presence flags are
   part of the scope, like the case/tense/voice sub-filters).
3. **Given** a shared URL with presence flags, **Then** the state restores; malformed
   values fail closed.

---

### User Story 7 - Filter by associated type, root, or lemma (Priority: P3)

A researcher wants deeper association filters: Unique Words by primary word type or
primary root; Lemmas by their root; Stems by their primary root or primary lemma.

**Why this priority**: Highest research value but the heaviest work; depends on the
filter UI from Story 5 existing.

**Independent Test**: On Unique Words, filter by a word type — every visible row's
displayed type chip matches the filter; on Lemmas, filter by a root — only that root's
lemmas remain.

**Acceptance Scenarios**:

1. **Given** Unique Words, **When** filtering by primary word type or primary root,
   **Then** every row shown displays exactly that type/root as its primary association
   — the filter and the displayed chip never disagree.
2. **Given** Lemmas, **When** filtering by a root, **Then** only lemmas belonging to
   that root remain (a true belonging relation).
3. **Given** Stems, **When** filtering by root or lemma, **Then** the filter uses the
   stem's *primary* association only, and the filter label says so honestly
   ("الجذر الأساسي" / "الصيغة المعجمية الأساسية" — a stem may co-occur with others; the filter
   is by the primary one, and this is documented).
4. All association filters are URL-shareable, restore on refresh/Back, and fail closed
   on invalid identifiers.

---

### User Story 8 - See the four scoped counts on Word Types (Priority: P4)

A researcher on Word Types wants an at-a-glance summary of the currently selected
scope: how many words, roots, stems, and lemmas it contains — updating with every
scope change (type, sub-filters, presence flags, search).

**Why this priority**: The flagship statistic; depends on search (Story 1) and presence
flags (Story 6) being part of the scope first.

**Independent Test**: Select a scope — four labeled counts appear; each count equals
the total of the corresponding table view for the identical scope; type a search — all
four counts narrow.

**Acceptance Scenarios**:

1. **Given** any active scope, **Then** the summary shows exactly four counts — words,
   roots, stems, lemmas — each labeled per the terminology lock, in the same
   right-to-left order as the existing view tabs
   (كلمات | جذور | أصول صرفية | صيغ معجمية).
2. **Given** the identical scope, **Then** each of the four counts EQUALS the total the
   corresponding table view (words/roots/stems/lemmas) reports — always, for every
   combination of type, child type, case, tense, voice, presence flags, and search.
3. **Given** a scope change of any kind (type selection, sub-filter, presence flag,
   search), **Then** all four counts update; **Given** only a view-tab switch or a page
   change, **Then** the counts do not reload (they describe the scope, not the page).
4. The four counts use ONLY the Word Types scoped counting family (word-context grain;
   distinct roots/stems/lemmas within the active scope). They are NEVER the global
   whole-Quran aggregates shown on the Roots/Lemmas/Stems explorers, and the two
   families never appear mixed in one surface.
5. **Given** the counts are loading, **Then** the summary shows a non-interactive
   placeholder; **Given** the counts failed while the table succeeded, **Then** the
   table stays fully usable and the summary shows its own compact error with a retry
   that refetches only the counts; **Given** an all-zero scope, **Then** zeros render.
6. Placement (decided — see Clarifications): the strip sits between the type-filter
   strip and the table-view tabs (filters → scope summary → tabs → table), and must
   not break the page's mounted-shell behavior.

---

### Edge Cases

- Search text that normalizes to empty (whitespace, diacritics only) → treated as no
  search.
- Search plus a scope with zero matches → empty state everywhere; four counts show
  zeros; no error.
- Malformed URL values (ranges with min > max, non-numeric bounds, unknown presence
  flag values, invalid identifiers) → fail closed: the offending state is treated as
  absent, the page loads, nothing crashes.
- Excessively long search input → rejected politely (defensive bound) without breaking
  the page.
- A filter change that excludes the currently selected detail row → the list narrows;
  detail behavior follows the page's existing selection rules (selection is
  identity-loaded; the list scope and the detail selection remain independent).
- Back/Forward across search/filter/page/scope changes → every state restores exactly.
- The four-count summary failing while the table succeeds (and vice versa) → each
  surface degrades independently; no blocking.
- 1000-row page on a slow machine → the list must stay scrollable and interactive; the
  loading state is non-interactive but the shell never unmounts.
- Requests that ask for more than the allowed page size → rejected with a clear Arabic
  message (existing behavior, unchanged semantics).

## Requirements *(mandatory)*

### Functional Requirements

**Word Types parity**

- **FR-001**: Word Types MUST offer a text search that matches the clean,
  tashkeel-insensitive word identity text only — never root/stem/lemma display text.
- **FR-002** (CONFIRMED, fixed): the search narrows the shared scope — all four table
  views AND the four-count summary reflect it; the search input is visible on all
  views with a placeholder naming the word grain.
- **FR-003**: Word Types search behavior MUST match the other explorers' feel: brief
  typing pause before applying (same debounce behavior), URL-reflected, list page
  resets on change.
- **FR-004**: The Word Types list MUST serve up to 1000 rows per page (default and
  maximum) across all four views, with smooth scrolling and unchanged row semantics.
- **FR-005**: Word Types detail lists (word ayahs, grouped member words, grouped ayahs)
  MUST serve up to 100 items per page; the single-shot surah views stay single-shot.
- **FR-006**: Search text MUST never be recorded in logs (only the fact that a search
  was present).

**Filters**

- **FR-007**: Unique Words, Roots, Lemmas, Stems MUST offer count-range filters on
  exactly the count metrics each page already displays (see Story 5, scenario 3), via
  preset buckets plus a custom min/max; open-ended bounds allowed.
- **FR-008**: Word Types MUST offer tri-state has-root / has-stem / has-lemma presence
  filters that participate in the scope (grouped views and the four-count summary
  included).
- **FR-009**: Unique Words MUST support filtering by primary word type and by primary
  root; the filter result MUST agree exactly with the primary type/root the rows
  display.
- **FR-010**: Lemmas MUST support filtering by their root (a true belonging relation);
  Stems MUST support filtering by their primary root and primary lemma, labeled as
  primary ("الأساسي/الأساسية") and documented as primary-not-sole.
- **FR-011**: All filters compose with each other and with search/sort (AND semantics);
  changing any filter resets the list to its first page.
- **FR-012**: Invalid filter input submitted directly MUST be rejected with a clear
  Arabic message; invalid filter state arriving via URL MUST fail closed (treated as
  absent).
- **FR-013**: No filter may require schema, migration, importer, or source-data
  changes. If one turns out to, work STOPS and the conflict is reported (locked B3).

**Statistics**

- **FR-014**: Each of the four normal explorers MUST show ONE headline statistic: the
  total count of the current result set — identical to the total the page's pagination
  is built from — updating with every search/filter change. No new aggregation is
  introduced for it.
- **FR-015**: Word Types MUST show a scoped four-count summary (words, roots, stems,
  lemmas) for the active scope (type, child type, case, tense, voice, presence flags,
  search).
- **FR-016**: Each of the four counts MUST equal the total of the corresponding table
  view for the identical scope — this equality is the correctness contract.
- **FR-017**: The four counts MUST come only from the scoped word-context counting
  family. Scoped counts and the global whole-Quran aggregate family MUST never be
  conflated or mixed in one surface (hard invariant).
- **FR-018**: Statistic surfaces MUST have distinct loading / empty / error states;
  loading is non-interactive; a statistics failure MUST NOT block the table (and vice
  versa); the four-count summary offers its own retry.

**State & sharing**

- **FR-019**: Every new piece of state (search, ranges, presence flags, association
  filters) MUST be URL-shareable, restore exactly on refresh/Back/Forward and via
  shared links, and parse fail-closed.
- **FR-020**: Existing URLs (without the new keys) MUST keep working unchanged; all new
  state is optional and additive. Behavior defaults preserve today's semantics except
  the locked page-size increases.

**Terminology (lock D — applies to every new label)**

- **FR-021**: root = **"الجذر"** (plural "الجذور"); stem's canonical user-facing label =
  **"الأصل الصرفي"** (plural "الأصول الصرفية"); lemma's canonical user-facing label =
  **"الصيغة المعجمية"** (plural "الصيغ المعجمية") — the app's live terms (verified in
  the words feature label files). "الجذع" and "اللمّة" remain internal reference terms
  only and MUST NOT appear in user-facing labels. Every Arabic label MUST name the
  dimension it actually counts; view-tab labels and count labels stay mutually
  consistent.

### Key Entities

- **Word (unique identity)**: a Quran word identified by its clean,
  tashkeel-insensitive text form; displayed in Uthmani script. Carries occurrence,
  ayah, and surah counts. Two modes: with tashkeel / without.
- **Root (جذر)**: morphological root dimension; carries global usage counts and
  relations to lemmas/stems.
- **Lemma (الصيغة المعجمية; internal reference "اللمّة")**: lexical form dimension;
  belongs to at most one root.
- **Stem (الأصل الصرفي; internal reference "الجذع")**: morphological stem dimension;
  has *primary* (not sole)
  associations to a root and a lemma.
- **Word-context entry**: the Word Types row grain — a word identity in a specific
  grammatical context; the unit the scoped counting family counts.
- **Scope (Word Types)**: the combination of main type, child type, case, tense,
  voice, presence flags, and search that defines what the page currently shows; the
  four-count summary describes exactly this.
- **Count families (invariant)**: scoped word-context counts (Word Types) vs global
  whole-Quran aggregates (Roots/Lemmas/Stems explorers) — related but never
  interchangeable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A researcher can locate a specific word inside any Word Types scope by
  typed search in under 10 seconds, where previously it required paging through 25-row
  pages.
- **SC-002**: Word Types serves up to 1000 rows per page (40× today's 25) and detail
  lists up to 100 items per page (4× today's 25), matching the other four explorers.
- **SC-003**: Scrolling a full 1000-row Word Types page remains smooth and interactive
  (no perceptible jank) on the same hardware where the other explorers' 1000-row
  tables are smooth today.
- **SC-004**: On all four normal explorers, the headline stat equals the paged result
  total for the active query in 100% of cases, including after every search/filter
  change.
- **SC-005**: On Word Types, each of the four scope counts equals the corresponding
  table view's total for the identical scope in 100% of tested scope combinations
  (types × sub-filters × presence flags × search).
- **SC-006**: 100% of new state round-trips through URLs: share, refresh, Back/Forward
  all restore the exact view; 100% of malformed URL states fail closed without errors.
- **SC-007**: Zero instances of mixed count families in any single surface (audited
  against the invariant).
- **SC-008**: All existing explorer behavior not named here — ordering, selection,
  detail navigation, existing URL contracts — is unchanged (existing test suites stay
  green).
- **SC-009**: The scoped four-count summary answers "what does this scope contain?"
  in one glance — no additional round-trip cost perceptible to the user versus the
  table load beside it.

## Out of Scope (carried from the plan — verbatim non-goals)

- No row-cap change on the already-1000 explorers (Unique/Roots/Lemmas/Stems lists stay
  1000/1000).
- Unique-word ayahs cap stays **100**.
- No SUM/average/occurrence aggregations in any stat area — counts only (the result
  count on normal pages; the four dimension counts on Word Types).
- `TypeDistributionListComponent` is NOT deleted here (separate cleanup).
- No importer, no Quran text change, no schema/migration, no new packages, no unrelated
  refactors.

## Assumptions

- The implementation plan at `docs/feature-026-words-explorers-enhancements/plan.md` is
  the authoritative decision record; its Locked Decisions (A1–D), phase ordering, stop
  conditions, and acceptance criteria govern. This spec adds no scope beyond it.
- All work is read-only over existing data; the word identity rule (clean
  imlaei-simple identity, Uthmani display) and existing ordering contracts are
  unchanged and out of discussion.
- The existing count data (occurrence/ayah/surah counts and dimension associations) is
  correct as imported; this feature surfaces and filters it, never recomputes or
  corrects it.
- Arabic-first RTL presentation, scholarly-calm visual register, and the existing
  explorer interaction patterns (search feel, chips, pagination, detail panels) are
  the design baseline; nothing introduces a new visual system.
- Accessibility expectations: filter chips expose pressed state to assistive
  technology; loading placeholders are non-interactive; keyboard and focus behavior of
  the tables and detail panels remains as it is today.
- Sub-second-feel performance intent: the 1000-row list and the four-count summary are
  bounded reads that keep the page responsive; a hard performance failure at the
  locked defaults is a stop condition (user sign-off required), not a silent redesign.
