# Feature Specification: Quran Lemmas & Stems Explorer

**Feature Branch**: `016-lemmas-stems-explorer`
**Created**: 2026-06-25
**Status**: Draft
**Input**: Combined implementation plan `docs/feature-016-lemmas-stems-explorer/feature-016-lemmas-stems-explorer-combined-implementation-plan.md`, supported by the Feature 016 capability and linking report. Generation only; no implementation is included.

## Overview

Feature 016 adds two read-only study screens to the dashboard's existing Words area:

- **Lemmas Explorer** (`الصيغ المعجمية`) at `/dashboard/words/lemmas`
- **Stems Explorer** (`الأصول الصرفية`) at `/dashboard/words/stems`

Each screen gives Arabic-speaking admins and teachers a searchable, sortable, paginated catalogue with
scholarly summary counts and a persistent details panel. From a selected lemma or stem, users can study
its words, ayahs, surah distribution, related morphology, and part-of-speech distribution without
changing Quran or morphology data.

The two explorers are sibling experiences, not one mixed catalogue. They share the established
Arabic-first Roots Explorer interaction model while keeping lemma and stem terminology, relationships,
counts, and restored URL state explicit.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse and find Quran lemmas (Priority: P1)

An admin opens the Lemmas Explorer and sees the Quran's dictionary forms in a paginated table. Each
lemma row shows its display text, related root when one exists, dominant type, and the counts needed to
understand its Quran-wide distribution. The admin can search, sort, and page through the catalogue.

**Why this priority**: The lemma catalogue is one of the two primary products of this feature and
delivers standalone value before any detail view is opened.

**Independent Test**: Open `/dashboard/words/lemmas`, verify the required columns and counts, then
search, sort, paginate, refresh, and confirm the list state is restored.

**Acceptance Scenarios**:

1. **Given** the Lemmas Explorer is opened, **When** its first list page loads, **Then** each row shows the lemma, related root or a calm empty value, dominant type, occurrences, ayahs, surahs, simple words, tashkeel words, and related stems.
2. **Given** the lemma table is shown, **When** the admin searches by Arabic lemma text, **Then** matching lemmas are shown using normalized contains matching and a no-results state appears when nothing matches.
3. **Given** the lemma table is shown, **When** the admin changes sort or list page, **Then** the rows update and the chosen list state is represented in the URL.
4. **Given** a lemma has no owned root, **When** its row is rendered, **Then** the root value is a non-clickable calm empty value and the row remains fully usable.
5. **Given** the lemma list is loaded, **When** no lemma is selected, **Then** no lemma word, ayah, surah, or related-stem detail data is loaded.

---

### User Story 2 - Browse and find Quran stems (Priority: P1)

An admin opens the Stems Explorer and sees the Quran's morphological stems in a paginated table. Each
stem row shows its display text, dominant co-occurring lemma and root when available, dominant type, and
distribution counts. The admin can search, sort, and page through the catalogue.

**Why this priority**: The stem catalogue is the second primary product of the feature and must remain
independently useful even when some stems do not have a related lemma or root.

**Independent Test**: Open `/dashboard/words/stems`, verify the required columns and null-safe values,
then search, sort, paginate, refresh, and confirm the list state is restored.

**Acceptance Scenarios**:

1. **Given** the Stems Explorer is opened, **When** its first list page loads, **Then** each row shows the stem, dominant related lemma and root or calm empty values, dominant type, occurrences, ayahs, surahs, simple words, and tashkeel words.
2. **Given** the stem table is shown, **When** the admin searches by Arabic stem text, **Then** matching stems are shown using normalized contains matching.
3. **Given** a stem co-occurs with more than one lemma or root, **When** its summary row is shown, **Then** the dominant related value is chosen by occurrence count and ties are resolved by earliest Mushaf occurrence.
4. **Given** a stem has no related lemma or root, **When** its row is rendered, **Then** the missing relationship is shown as a non-clickable calm empty value rather than an error.
5. **Given** the stem list is loaded, **When** no stem is selected, **Then** no stem word, ayah, surah, or related-lemma detail data is loaded.

---

### User Story 3 - Study exact ayah occurrences (Priority: P2)

The admin opens the ayahs view for a selected lemma or stem and sees the Quran verses where it occurs.
The exact matching Quran words are highlighted by stored word identity, and each ayah can be opened in
the existing Mushaf Reader in a new browser tab.

**Why this priority**: Quran context is the strongest verification and study surface for morphological
data. It lets users confirm that counts and classifications correspond to actual word occurrences.

**Independent Test**: Select a representative lemma and stem, open each ayahs view, verify exact
highlights and pagination, then follow an ayah link to the correct Mushaf location.

**Acceptance Scenarios**:

1. **Given** a lemma or stem row is visible, **When** the admin activates its occurrences or ayahs count, **Then** the details panel opens on the ayahs view for that exact selection.
2. **Given** an ayah contains one or more matching words, **When** the ayah is rendered, **Then** all and only the stored word occurrences associated with the selected lemma or stem are highlighted.
3. **Given** a high-frequency lemma or stem, **When** the ayahs view is opened, **Then** the ayahs are paginated and the interface does not load the full result set at once.
4. **Given** an ayah result is shown, **When** the admin activates its link, **Then** the matching ayah opens and is focused in the existing Mushaf Reader in a new browser tab.
5. **Given** an invalid selected lemma or stem is restored from the URL, **When** the page loads, **Then** the panel shows a controlled not-found state while the catalogue remains usable.

---

### User Story 4 - Explore related Quran word forms (Priority: P2)

For a selected lemma or stem, the admin opens the words view and switches between simple
no-diacritics forms and tashkeel forms. Each word shows how often it occurs within the selected lemma
or stem and links to the corresponding existing Unique Words detail in a new browser tab.

**Why this priority**: Word forms connect abstract morphology to readable Quran words and reuse the
established Unique Words study flow.

**Independent Test**: Open both word sub-views for one lemma and one stem, verify scoped counts and
pagination, then follow simple and tashkeel links to the correct Unique Words mode.

**Acceptance Scenarios**:

1. **Given** a lemma or stem is selected, **When** its row is selected without activating a count, **Then** the details panel opens on words / simple.
2. **Given** the words view is open, **When** the admin switches between simple and tashkeel, **Then** the matching distinct word identities and their selection-scoped occurrence counts are shown.
3. **Given** a selected lemma or stem has many related words, **When** a word sub-view is opened, **Then** the list is paginated.
4. **Given** a simple word is shown, **When** the admin activates it, **Then** that stable word identity opens in the existing simple Unique Words detail in a new browser tab.
5. **Given** a tashkeel word is shown, **When** the admin activates it, **Then** that stable word identity opens in the existing tashkeel Unique Words detail in a new browser tab.
6. **Given** a word count is shown inside a lemma or stem, **When** it is compared with the destination Unique Words detail, **Then** the local count remains scoped to the selected lemma or stem while the destination may show the word's Quran-wide count.

---

### User Story 5 - Review surah distribution (Priority: P3)

For a selected lemma or stem, the admin sees the surahs where it appears and the surahs where it does
not appear. Mentioned surahs show occurrence counts, and the two views account for all 114 surahs.

**Why this priority**: Surah distribution gives a concise Quran-wide perspective after the core list,
ayah, and word experiences are available.

**Independent Test**: Open mentioned and missing surahs for a representative lemma and stem and verify
that the two sets are disjoint and total 114.

**Acceptance Scenarios**:

1. **Given** a lemma or stem row is visible, **When** the admin activates its surahs count, **Then** the details panel opens on surahs / mentioned.
2. **Given** mentioned surahs are shown, **When** the list renders, **Then** every item shows the Arabic surah name and occurrence count for the selected lemma or stem.
3. **Given** the admin switches to missing surahs, **When** the list renders, **Then** it contains exactly the surahs where the selection does not occur.
4. **Given** both surah sub-views, **When** their unique entries are combined, **Then** they account for all 114 surahs without overlap.
5. **Given** the selection occurs in every surah, **When** missing surahs is opened, **Then** a clear empty state is shown.

---

### User Story 6 - Understand type and morphology relationships (Priority: P3)

The admin sees a consistent definition of `النوع` based on the dominant part of speech and can inspect
the complete type distribution in details. Lemmas link to their related stems; stems link to their
related lemmas; available roots link to the existing Roots Explorer.

**Why this priority**: This completes the scholarly morphology view and prevents a misleading
single-type interpretation when a lemma or stem appears with multiple parts of speech.

**Independent Test**: Use selections with multiple types and relationships, verify the dominant type
and tie-break, inspect the full distribution, and follow root/lemma/stem links.

**Acceptance Scenarios**:

1. **Given** a lemma or stem appears with multiple part-of-speech types, **When** its table row is shown, **Then** the type with the greatest occurrence count is shown as dominant and an additional-types indicator is shown.
2. **Given** two types have the same occurrence count, **When** the dominant type is chosen, **Then** the type whose matching occurrence appears first in Mushaf order wins.
3. **Given** a selected lemma or stem, **When** its full type distribution is shown, **Then** every associated type is listed with its controlled Arabic label and occurrence count, and the totals agree with the selection's occurrences.
4. **Given** a lemma has related stems, **When** the related stems view is opened, **Then** each stem shows its text and occurrence count within the lemma and links to that stem's explorer state in a new browser tab.
5. **Given** a stem has related lemmas, **When** the related lemmas view is opened, **Then** each lemma shows its text and occurrence count within the stem and links to that lemma's explorer state in a new browser tab.
6. **Given** a row has an available root, **When** the admin activates the root, **Then** that root opens in the existing Roots Explorer in a new browser tab.

---

### User Story 7 - Move between Mushaf and morphology explorers (Priority: P3)

While studying a selected word in the Mushaf Reader, the admin can open its root, lemma, or stem in the
corresponding explorer when a stable identity exists. Missing relationships remain readable but
non-interactive.

**Why this priority**: Bidirectional linking turns separate screens into one coherent study workflow
without changing the Mushaf Reader's primary purpose.

**Independent Test**: Select Mushaf words with and without root, lemma, and stem relationships; verify
available links open the correct explorer in new tabs and missing links remain non-interactive.

**Acceptance Scenarios**:

1. **Given** a selected Mushaf word has a lemma identity, **When** the admin activates the lemma, **Then** the Lemmas Explorer opens with that lemma selected on words / simple.
2. **Given** a selected Mushaf word has a stem identity, **When** the admin activates the stem, **Then** the Stems Explorer opens with that stem selected on words / simple.
3. **Given** a selected Mushaf word has a root identity, **When** the admin activates the root, **Then** the existing Roots Explorer opens with that root selected on words / simple.
4. **Given** a root, lemma, or stem identity is unavailable, **When** the morphology summary is shown, **Then** its display value remains non-clickable and no fabricated link is created.
5. **Given** any cross-page morphology or study link, **When** it is rendered, **Then** it is a normal new-tab link with a destination that can be inspected, copied, and opened independently.

---

### User Story 8 - Restore and navigate exact explorer state (Priority: P4)

The admin can refresh, bookmark, share, and navigate backward or forward through a lemma or stem study
state. List filters, selected identity, active detail view, active sub-view, and detail page restore
without confusing stale state.

**Why this priority**: Reliable state restoration supports long research sessions and collaboration,
but it depends on the primary browsing and detail flows.

**Independent Test**: Build deep states on both explorers, refresh them, reopen copied links, and use
browser back/forward while verifying exact restoration and safe handling of invalid values.

**Acceptance Scenarios**:

1. **Given** a valid lemma explorer state, **When** the page is refreshed or reopened, **Then** search, sort, list page, selected lemma, detail view, relevant sub-view, and detail page are restored.
2. **Given** a valid stem explorer state, **When** the page is refreshed or reopened, **Then** search, sort, list page, selected stem, detail view, relevant sub-view, and detail page are restored.
3. **Given** the admin changes an internal list or detail state, **When** the URL updates, **Then** the change remains in the same browser tab.
4. **Given** irrelevant sub-view or detail-page state is present, **When** the URL is interpreted, **Then** it is ignored or normalized without breaking the valid state.
5. **Given** the selected lemma or stem is cleared, **When** the panel closes, **Then** search, sort, and list page remain unchanged while selection-specific state is removed.

### Edge Cases

- A lemma has no owned root.
- A stem has no co-occurring lemma, no co-occurring root, or neither.
- A stem co-occurs with multiple lemmas or roots and the dominant relationship needs a deterministic tie-break.
- A lemma or stem appears with multiple part-of-speech types, including an occurrence-count tie.
- A selected lemma or stem has a very high ayah count or many word forms.
- A selected lemma or stem occurs multiple times in the same ayah.
- An ayah contains visually similar text that does not belong to the selected lemma or stem.
- A mentioned-surahs list contains all 114 surahs, leaving the missing-surahs list empty.
- A search returns no results.
- A requested list page or detail page exceeds the available pages.
- A restored URL contains an unknown identity, invalid sort, invalid view, invalid sub-view, or non-positive page.
- A zero count is activated and must open a clear empty detail state.
- A related root, lemma, stem, word, or ayah link is opened in a new tab.
- A narrow screen cannot preserve the full desktop split view.
- Quran ayah markers or non-readable markers must never appear as morphology occurrences or highlights.

## Requirements *(mandatory)*

### Functional Requirements

#### Navigation and layout

- **FR-001**: The system MUST provide a Lemmas Explorer at `/dashboard/words/lemmas` and a Stems Explorer at `/dashboard/words/stems` within the existing Words area.
- **FR-002**: The Words hub MUST provide active navigation entries for `الصيغ المعجمية` and `الأصول الصرفية` that lead to their respective explorers.
- **FR-003**: On wide screens, each explorer MUST use a split view with the catalogue table in the main area and a persistent details panel beside it.
- **FR-004**: The details panel MUST scroll independently from the catalogue table. On narrow screens it MAY adapt to the existing dismissible drawer/sheet pattern and MUST return focus to the control that opened it.
- **FR-005**: A modal/dialog MUST NOT be the primary desktop details experience, and neither explorer MUST add an overview tab because the table is the summary surface.
- **FR-006**: Both explorers MUST remain Arabic-first and right-to-left and MUST follow the existing calm Words/Roots visual language without introducing a new palette or redesign.

#### Lemmas catalogue

- **FR-007**: The Lemmas table MUST present these semantic columns in this order: الصيغة المعجمية, الجذر, النوع, المواضع, الآيات, السور, كلمات بدون تشكيل, كلمات بالتشكيل, الأصول الصرفية.
- **FR-008**: The Lemmas list MUST support normalized Arabic contains search by lemma display text, stable sorting by Mushaf order (default), occurrences, or alphabetical order, and pagination.
- **FR-009**: Each lemma row MUST use the lemma's stable numeric identity for selection and restoration while keeping that technical identity hidden from the visible interface.
- **FR-010**: The lemma root column MUST use the lemma's existing owned-root relationship. If no root exists, the interface MUST show a non-clickable calm empty value and MUST NOT infer a root.
- **FR-011**: Selecting a lemma row outside a specific count MUST open words / simple for that lemma.

#### Stems catalogue

- **FR-012**: The Stems table MUST present these semantic columns in this order: الأصل الصرفي, الصيغة المعجمية, الجذر, النوع, المواضع, الآيات, السور, كلمات بدون تشكيل, كلمات بالتشكيل.
- **FR-013**: The Stems list MUST support normalized Arabic contains search by stem display text, stable sorting by Mushaf order (default), occurrences, or alphabetical order, and pagination.
- **FR-014**: Each stem row MUST use the stem's stable numeric identity for selection and restoration while keeping that technical identity hidden from the visible interface.
- **FR-015**: When a stem co-occurs with multiple lemmas or roots, its table summary MUST show the dominant related lemma and root by occurrence count, with ties resolved by earliest Mushaf occurrence.
- **FR-016**: If a stem has no related lemma or root, the missing relationship MUST be shown as a non-clickable calm empty value and MUST NOT be treated as an error.
- **FR-017**: Selecting a stem row outside a specific count MUST open words / simple for that stem.

#### Count meanings and type meanings

- **FR-018**: For a selected lemma or stem, المواضع MUST equal the total number of matching word-morphology occurrences.
- **FR-019**: الآيات MUST equal the number of distinct Quran verses containing at least one matching occurrence.
- **FR-020**: السور MUST equal the number of distinct surahs containing at least one matching occurrence.
- **FR-021**: كلمات بدون تشكيل MUST equal the number of distinct simple word identities among matching occurrences.
- **FR-022**: كلمات بالتشكيل MUST equal the number of distinct tashkeel word identities among matching occurrences.
- **FR-023**: For a lemma, الأصول الصرفية MUST equal the number of distinct stems co-occurring with that lemma.
- **FR-024**: The lemma's الأصول الصرفية table count MUST equal the number of items in its related-stems view.
- **FR-025**: `النوع` MUST mean the dominant controlled part-of-speech type among matching occurrences, selected by highest occurrence count and then by earliest Mushaf occurrence when counts tie.
- **FR-026**: If more than one part-of-speech type is associated with a lemma or stem, the table MUST indicate that additional types exist and the details panel MUST expose the complete distribution with controlled labels and occurrence counts.
- **FR-027**: The complete type-distribution occurrence counts for a lemma or stem MUST total that selection's المواضع count.

#### Count activation and details

- **FR-028**: Every displayed numeric count that has a corresponding detail view MUST be a keyboard-operable control.
- **FR-029**: Lemma count activation MUST map as follows: المواضع or الآيات → ayahs; السور → surahs / mentioned; كلمات بدون تشكيل → words / simple; كلمات بالتشكيل → words / tashkeel; الأصول الصرفية → related stems.
- **FR-030**: Stem count activation MUST map as follows: المواضع or الآيات → ayahs; السور → surahs / mentioned; كلمات بدون تشكيل → words / simple; كلمات بالتشكيل → words / tashkeel.
- **FR-031**: Activating a zero count MUST open the corresponding detail view with a clear empty state rather than silently doing nothing.
- **FR-032**: Detail data MUST be loaded only when its view or sub-view becomes active; loading a catalogue MUST NOT trigger per-row detail reads.
- **FR-033**: Word and ayah detail views MUST be paginated. Mentioned surahs, missing surahs, related stems, related lemmas, and type distributions MAY be loaded as bounded whole lists.
- **FR-034**: Reopening already loaded detail state during the same selected-identity session SHOULD reuse it when safe rather than unnecessarily repeating the read.

#### Words, ayahs, and surahs

- **FR-035**: The words view MUST provide simple and tashkeel sub-views and show each word's display text plus its occurrence count scoped to the selected lemma or stem.
- **FR-036**: Simple and tashkeel word items MUST link by stable word identity to the matching existing Unique Words detail mode in a new browser tab.
- **FR-037**: The ayahs view MUST highlight exact matching Quran word occurrences by stored word identity and MUST NOT use string replacement or text-fragment matching.
- **FR-038**: The system MUST NOT alter, re-spell, fabricate, or animate Quran text while rendering or highlighting it.
- **FR-039**: Highlighted matches MUST remain perceivable without relying on color alone.
- **FR-040**: Each ayah result MUST provide a new-tab link to the existing Mushaf Reader using the ayah's stable location and page context.
- **FR-041**: Mentioned surahs MUST show Arabic surah names and occurrence counts; missing surahs MUST show exactly the complementary set.
- **FR-042**: For every lemma and stem, mentioned and missing surahs MUST be disjoint and together account for all 114 surahs.

#### Morphology and cross-page links

- **FR-043**: A lemma's related-stems view MUST list each distinct stem with its text, occurrence count within the lemma, and stable new-tab link to the Stems Explorer.
- **FR-044**: A stem's related-lemmas view MUST list each distinct lemma with its text, occurrence count within the stem, and stable new-tab link to the Lemmas Explorer.
- **FR-045**: Available root values in lemma and stem summaries MUST link by stable root identity to the existing Roots Explorer in a new browser tab.
- **FR-046**: The existing Mushaf selected-word morphology summary MUST make root, lemma, and stem values new-tab links to their explorers when their stable identities exist.
- **FR-047**: Missing root, lemma, or stem identities MUST remain non-clickable, and the system MUST NOT build canonical links from Arabic display text, Buckwalter text, or other display values.
- **FR-048**: Cross-page links to roots, lemmas, stems, unique words, ayahs, and Mushaf morphology destinations MUST be normal inspectable links that open safely in a new browser tab. Same-page search, sort, pagination, selection, tabs, and sub-views MUST remain same-tab state changes.

#### URL state and restoration

- **FR-049**: The Lemmas Explorer URL MUST be able to restore search, sort, list page, selected lemma, active detail view, applicable word or surah sub-view, and applicable detail page.
- **FR-050**: The Stems Explorer URL MUST be able to restore search, sort, list page, selected stem, active detail view, applicable word or surah sub-view, and applicable detail page.
- **FR-051**: Stable numeric lemma and stem identities MUST be the canonical URL lookup values. Arabic display text and Buckwalter text MUST NOT be canonical URL identity.
- **FR-052**: The default restored state for either explorer MUST be Mushaf-order list sorting, list page 1, words view, simple word sub-view, mentioned-surahs sub-view, and detail page 1.
- **FR-053**: Word sub-view state MUST apply only to words; surah sub-view state MUST apply only to surahs; detail-page state MUST apply only to paginated words and ayahs views.
- **FR-054**: Changing search or sort MUST reset the catalogue page to 1 while preserving the current selected lemma or stem and its active detail state. Clearing a selected lemma or stem MUST preserve search, sort, and list page while clearing selection-specific detail state.
- **FR-055**: Frontend malformed or non-positive `page` and `detailPage` query values MUST normalize to 1. Backend non-positive `page` or `pageSize` values MUST return the controlled validation outcome. A valid positive catalogue or detail page beyond the available results MUST return a successful empty page and render a controlled empty state without changing the requested URL page. An unknown positive lemma or stem identity MUST render the panel not-found state while leaving the catalogue usable.
- **FR-056**: Browser refresh, back/forward navigation, and reopening a copied valid link MUST restore the same meaningful explorer state.

#### States, accessibility, safety, and operations

- **FR-057**: Both explorers MUST provide clear Arabic loading, empty, no-results, error, and not-found states and MUST NOT show blank or fabricated data.
- **FR-058**: Count controls, rows, tabs, sub-view controls, pagination, and links MUST be keyboard-operable, and selected/active state MUST be perceivable without color alone.
- **FR-059**: Every new-tab link MUST expose an accurate destination, preserve visible focus, and use safe new-tab behavior.
- **FR-060**: The feature MUST be strictly read-only and MUST NOT edit, import, regenerate, or otherwise mutate Quran, word, or morphology data.
- **FR-061**: The feature MUST use the existing populated morphology relationships and MUST NOT require a database schema change or speculative index as part of the approved scope.
- **FR-062**: Read operations MUST provide the two catalogue summaries, selection summaries for deep-link restoration, and all defined detail views with controlled invalid-input and not-found outcomes.
- **FR-063**: Stable catalogue and detail reads SHOULD be reusable so repeated study navigation is fast, while free-text searches MUST NOT create unbounded retained entries.
- **FR-064**: Operational diagnostics MUST record safe identifiers, operation/view, applicable page and sort values, whether search was present, result counts, and elapsed time where measured. They MUST NOT contain Quran text, lemma/stem/root text, raw search text, or large payloads.

### Key Entities *(include if feature involves data)*

- **Lemma (الصيغة المعجمية)**: A Quranic dictionary form with a stable numeric identity, Arabic display text, optional Buckwalter display, optional owned root, morphology occurrences, distribution counts, related stems, and part-of-speech distribution.
- **Stem (الأصل الصرفي)**: A Quranic morphological stem with a stable numeric identity, Arabic display text, morphology occurrences, optional co-occurring lemmas and roots, distribution counts, and part-of-speech distribution.
- **Dominant related lemma/root**: The lemma or root most frequently co-occurring with a stem, with earliest Mushaf occurrence as the deterministic tie-break.
- **Type distribution**: The controlled part-of-speech types associated with a lemma or stem, including counts; its leading item defines the table's `النوع`.
- **Word occurrence**: A specific Quran word position associated with a lemma or stem and located in one ayah and surah.
- **Simple word identity / Tashkeel word identity**: Stable distinct word forms used in the words sub-views and as destinations in the existing Unique Words Explorer.
- **Ayah match**: A Quran verse containing one or more exact word occurrences associated with the selected lemma or stem.
- **Mentioned surah / Missing surah**: The complementary surah distributions for a selection; together they cover all 114 surahs.
- **Related stem / Related lemma**: A morphology relationship established by co-occurrence on Quran word morphology, with a count scoped to the selected lemma or stem.
- **Explorer state**: The restorable list and detail context for one explorer: search, sort, list page, selected identity, view, relevant sub-view, and relevant detail page.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In 100% of tested sessions, users can reach either explorer from the Words area in no more than two interactions.
- **SC-002**: The first catalogue page becomes visible within 1 second under normal operating conditions for at least 95% of measured page openings.
- **SC-003**: In 100% of sampled lemma rows, all nine required semantic columns are present and their counts match the definitions in this specification.
- **SC-004**: In 100% of sampled stem rows, all nine required semantic columns are present; missing lemma/root relationships render as controlled empty values rather than errors.
- **SC-005**: In 100% of tested count activations, the correct detail view and sub-view open according to the defined lemma/stem mapping.
- **SC-006**: In 100% of sampled lemmas, the displayed related-stems count equals the number of distinct items in its related-stems view.
- **SC-007**: In 100% of sampled lemma/stem type distributions, the dominant type follows the required count and Mushaf-order tie-break, and the full distribution totals the occurrence count.
- **SC-008**: In 100% of sampled ayah results, exactly the stored matching word occurrences are highlighted, including multiple matches in one ayah, with no text-based false matches.
- **SC-009**: For representative high-frequency selections, paginated word and ayah views remain responsive and do not load their entire result sets at once.
- **SC-010**: In 100% of sampled selections, mentioned and missing surah sets are disjoint and together contain exactly 114 surahs.
- **SC-011**: In 100% of tested root, lemma, stem, unique-word, and ayah links, the correct stable-identity destination opens in a new browser tab.
- **SC-012**: In 100% of tested valid shared links, the meaningful lemma/stem explorer state is restored after refresh or reopening; invalid state produces controlled feedback while preserving catalogue usability.
- **SC-013**: A keyboard-only user can reach and activate every count, row-selection control, tab, sub-view, pagination control, and cross-page link, and can identify selected/active state without color alone.
- **SC-014**: In 100% of verification checks, using either explorer or the new Mushaf links leaves Quran text, word identities, morphology data, and database structure unchanged.
- **SC-015**: Operational diagnostics for the feature contain the required safe fields and contain no Quran text, lexical display text, or raw search text.

## Assumptions

- The target users are existing Arabic-speaking dashboard admins, supervisors, and teachers.
- The feature is read-only over the populated Feature 004 morphology data. Verified baseline counts are 4,793 lemmas and 12,108 stems, with complete Arabic display values and readable-word connectivity.
- No database migration, importer, data-pipeline run, Quran text change, or speculative index is required.
- Lemmas and stems are separate explorer pages inside one combined feature because they share a study pattern but have different identities, columns, relationships, and detail views.
- Arabic display text is the required v1 search surface. Lemma Buckwalter remains display/supporting metadata and is not required as a separate v1 search mode or canonical identity.
- Stable numeric identities are available for roots, lemmas, stems, and unique words. Ayah navigation uses the existing stable Mushaf location contract.
- A lemma's table root uses its existing owned-root relationship; a stem's table lemma/root uses dominant morphology co-occurrence.
- Controlled part-of-speech Arabic labels already exist and are reused; the feature does not invent new type labels.
- Word occurrence counts inside detail lists are scoped to the selected lemma or stem; destination Unique Words details may show Quran-wide totals.
- Zero-count controls remain activatable and lead to clear empty states for consistency.
- Detail page sizes use fixed sensible defaults and are not user-configurable or independently encoded in the URL.
- On wide screens the panel follows the existing Roots Explorer inline layout; on narrow screens it follows the existing dismissible adaptation.
- Runtime Quran/morphology data is stable enough for safe reuse of read responses; any offline reseed is followed by the established refresh/restart process.

## Dependencies

- Existing populated Quran morphology, word, ayah, surah, root, lemma, stem, part-of-speech, and unique-word data.
- Existing Words hub and Feature 014 Unique Words Explorer.
- Existing Feature 015 Roots Explorer and its split-view, URL-state, pagination, highlighting, and deep-link behavior.
- Existing Mushaf Reader selected-word analysis and ayah-focus navigation.
- Existing shared loading, empty, error, not-found, pagination, and highlighted-ayah interaction patterns.

## Out of Scope

- Editing, importing, regenerating, or curating Quran, word, lemma, stem, root, or morphology data.
- Database migrations or new indexes without a separately evidenced and approved requirement.
- A combined generic morphology explorer that replaces the explicit Lemmas and Stems experiences.
- Canonical lookup or restored selection by Arabic display text, Buckwalter text, normalized text, or other display values.
- New lemma/stem creation, correction, merging, classification editing, or manual relationship management.
- Filtering or browsing by every morphology feature, segment, or grammatical property beyond the defined part-of-speech summary.
- A new design language, color palette, dashboard layout, or animated Quran text.
- Changes to the meaning or existing behavior of the Roots Explorer, Unique Words Explorer, or Mushaf Reader beyond the additive stable-identity links defined here.
