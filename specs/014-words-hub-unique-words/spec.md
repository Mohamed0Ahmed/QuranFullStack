# Feature Specification: Words Hub + Unique Words Explorer

**Feature Branch**: `014-words-hub-unique-words`  
**Created**: 2026-06-21  
**Status**: Draft  
**Input**: User description: "Generate a Spec Kit specification from docs/feature-014-words-hub-unique-words/feature-014-words-hub-unique-words-planning-report.md. Generation only; implementation will be delegated, so the specification must be clear."

## Clarifications

### Session 2026-06-21

- Q: Should unique-word search use contains, prefix, or exact matching after Arabic normalization? → A: Contains matching.
- Q: Should word distribution drill-downs open as modal, dedicated page, or inline expansion? → A: Modal over the current list.
- Q: What identifier should restored selected-word state use in URLs? → A: Stable unique-word ID.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Open The Words Hub (Priority: P1)

An Arabic-speaking dashboard admin or teacher opens the Words area and sees a calm hub for Quran word-study sections. The hub makes the available section, `الكلمات الفريدة`, clear and reachable, while showing future sections as disabled coming-soon cards.

**Why this priority**: This gives the Words area a real entry point and prevents users from landing on an empty section. It also bounds v1 by showing which word-study sections are available now and which are future scope.

**Independent Test**: Open the Words area from dashboard navigation and confirm the hub shows one active Unique Words card plus four disabled coming-soon cards, all with Arabic labels.

**Acceptance Scenarios**:

1. **Given** the admin opens the Words area, **When** the hub appears, **Then** it shows the active card `الكلمات الفريدة` and the future cards `الجذور`, `الصيغة المعجمية`, `الأصل الصرفي`, and `أنواع الكلمة`.
2. **Given** the admin activates `الكلمات الفريدة`, **When** the card is selected, **Then** the Unique Words explorer opens in the with-tashkeel mode by default.
3. **Given** the admin focuses or clicks a future card, **When** the card is disabled, **Then** it does not navigate and clearly communicates `قريبًا`.
4. **Given** the hub is viewed on desktop, tablet, or mobile, **When** the cards are displayed, **Then** the layout remains RTL-first, readable, and not visually crowded.

---

### User Story 2 - Browse And Search Unique Quran Words (Priority: P1)

The admin browses unique Quran words in two modes: words distinguished by tashkeel and simplified imlaei words without tashkeel. Each listed word shows an authoritative Uthmani display word and four study counts: occurrences, ayahs, surahs, and surahs where the word is not mentioned.

**Why this priority**: This is the main value of the feature. It lets teachers and reviewers inspect Quran word distribution without needing raw data tables or technical keys.

**Independent Test**: Open each unique-word mode, browse the list, search using Arabic input with and without diacritics, and confirm each result shows the display word and all four counts.

**Acceptance Scenarios**:

1. **Given** the admin opens the Unique Words explorer, **When** the page loads, **Then** it defaults to `بالتشكيل` and offers a second mode `إملائي (بدون تشكيل)`.
2. **Given** the admin views a unique word row or card, **When** the item is displayed, **Then** the main label is an Uthmani Quran word display, not a raw technical key.
3. **Given** the admin searches with Arabic text containing tashkeel or common hamza/alef/ya/waw variants, **When** the normalized query appears anywhere in a normalized unique-word text, **Then** matching results can be found without requiring exact diacritic entry.
4. **Given** the admin changes sorting, **When** a sort is selected, **Then** the list can be ordered by Mushaf order, by occurrence frequency, or alphabetically.
5. **Given** there are many unique words, **When** the admin pages through results, **Then** the list remains usable and does not require loading every result at once.
6. **Given** a search has no matches, **When** results load, **Then** the page shows the Arabic empty state `لا توجد نتائج` rather than an error.

---

### User Story 3 - Inspect Word Distribution Drill-Downs (Priority: P2)

The admin selects a unique word and inspects where it appears in a modal over the current list: which surahs mention it, which surahs do not mention it, and which ayahs contain it. In the ayah view, exact matched word occurrences are highlighted without relying on text replacement.

**Why this priority**: Counts are useful only if users can verify and study the underlying distribution. The three drill-downs turn summary counts into reviewable Quran study context.

**Independent Test**: Choose a known unique word, open each drill-down view, and confirm the surah lists, missing-surah lists, and ayah matches agree with the displayed counts.

**Acceptance Scenarios**:

1. **Given** a unique word is visible in the list, **When** the admin opens `السور`, **Then** the interface lists the surahs where the word is mentioned and shows the occurrence count per surah.
2. **Given** a unique word is visible in the list, **When** the admin opens `لم يذكر في`, **Then** the interface lists the surahs where the word is not mentioned.
3. **Given** a unique word is visible in the list, **When** the admin opens `الآيات`, **Then** the interface lists ayahs containing that unique word and highlights the exact matching word occurrences in each ayah.
4. **Given** an ayah contains the same displayed text in more than one position or a visually similar word, **When** matches are highlighted, **Then** only the stored matching word occurrences for the selected unique word are highlighted.
5. **Given** the admin closes a drill-down modal, **When** control returns to the list, **Then** the previous search, sort, mode, and page context remain unchanged.
6. **Given** the selected word has many matching ayahs, **When** the ayah drill-down is opened, **Then** the ayah list is paginated and remains readable.

---

### User Story 4 - Restore A Shared Explorer State (Priority: P3)

The admin can refresh, bookmark, or share a Unique Words explorer state that restores the selected mode, search, sort, page, selected word by stable unique-word ID, and active modal drill-down view.

**Why this priority**: Reviewers and teachers often need to return to the same word distribution later or share it with another reviewer. This is secondary to the core browse and drill-down behavior but important for real study workflows.

**Independent Test**: Set a mode, search term, sort, page, selected word, and drill-down view; refresh or reopen the shared link; confirm the same state is restored.

**Acceptance Scenarios**:

1. **Given** the admin is viewing the simplified unique-word mode with a search and sort applied, **When** the page is refreshed, **Then** the same mode, search, sort, and list page are restored.
2. **Given** the admin has a selected word drill-down modal open, **When** the link is copied and reopened, **Then** the same selected word is restored by stable unique-word ID when the word still exists.
3. **Given** the restored state references an unknown word, **When** the page loads, **Then** the system shows a controlled Arabic not-found state and keeps the rest of the list usable.

---

### Edge Cases

- Search input includes tashkeel, missing tashkeel, hamza/alef variants, ya/alef-maqsura variants, or waw-hamza variants.
- A search returns no unique words.
- A requested page number is beyond the available result pages.
- A selected unique word exists in very many ayahs.
- A selected unique word appears multiple times in the same ayah.
- An ayah contains visually similar text that is not an occurrence of the selected unique word.
- A unique word appears in all 114 surahs, so the missing-surahs list is empty.
- A unique word appears in only one surah, so the missing-surahs list is large.
- A restored link references an invalid mode, invalid selected word, or invalid drill-down view.
- Future hub cards are keyboard-focused or clicked by assistive-technology users.
- Quran ayah markers or non-readable markers must never appear as unique-word occurrences or highlighted matches.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a Words hub for dashboard users.
- **FR-002**: The Words hub MUST show `الكلمات الفريدة` as the only active v1 section.
- **FR-003**: The Words hub MUST show `الجذور`, `الصيغة المعجمية`, `الأصل الصرفي`, and `أنواع الكلمة` as disabled coming-soon sections.
- **FR-004**: Disabled coming-soon sections MUST be visibly disabled, keyboard-safe, and non-navigable.
- **FR-005**: Opening `الكلمات الفريدة` MUST show the Unique Words explorer.
- **FR-006**: The Unique Words explorer MUST support two modes: `بالتشكيل` and `إملائي (بدون تشكيل)`.
- **FR-007**: The Unique Words explorer MUST default to `بالتشكيل` when no mode is specified.
- **FR-008**: Each unique word item MUST show an authoritative Uthmani display word as its primary label.
- **FR-009**: The simplified mode MUST NOT use a raw technical key as the primary user-facing label.
- **FR-010**: Each unique word item MUST show `المواضع`, `الآيات`, `السور`, and `لم يذكر في` counts.
- **FR-011**: `لم يذكر في` MUST equal 114 minus the number of surahs where the word is mentioned.
- **FR-012**: The list MUST support contains-style Arabic search after normalization, tolerating missing tashkeel and common Arabic letter-form variants.
- **FR-013**: Search MUST work in both unique-word modes while preserving Uthmani display text in results.
- **FR-014**: The list MUST support sorting by Mushaf order, occurrence frequency, and alphabetical order.
- **FR-015**: The default list sort MUST be Mushaf order.
- **FR-016**: The list MUST be paginated so users are not required to load all unique words at once.
- **FR-017**: The system MUST show Arabic loading, empty, and error states for the hub, list, and drill-down views.
- **FR-018**: The occurrence count `المواضع` MUST be displayed as summary information in v1 and MUST NOT open a separate drill-down unless a later specification explicitly adds that scope.
- **FR-019**: Selecting `السور` for a unique word MUST open a modal view showing the surahs where the word is mentioned, including occurrence count per surah.
- **FR-020**: Selecting `لم يذكر في` for a unique word MUST open a modal view showing the surahs where the word is not mentioned.
- **FR-021**: Selecting `الآيات` for a unique word MUST open a modal view showing ayahs containing the selected unique word.
- **FR-022**: The ayah drill-down MUST highlight exact matching word occurrences for the selected unique word.
- **FR-023**: Matching MUST be based on stored word occurrence identity and MUST NOT rely on replacing matching text strings inside ayah text.
- **FR-024**: If a selected unique word appears multiple times in the same ayah, all exact matching occurrences in that ayah MUST be highlighted.
- **FR-025**: Words that merely look similar or share displayed text fragments MUST NOT be highlighted unless they are exact occurrences of the selected unique word.
- **FR-026**: The ayah drill-down MUST paginate ayahs when the selected word appears in many ayahs.
- **FR-027**: Closing a drill-down modal MUST preserve the user's current mode, search, sort, and list page.
- **FR-028**: The explorer MUST support refreshable and shareable state for mode, search, sort, list page, selected word by stable unique-word ID, drill-down view, and ayah drill-down page.
- **FR-029**: Restored state with invalid mode, unknown word, or invalid drill-down view MUST produce controlled Arabic feedback and keep the list usable.
- **FR-030**: Quran ayah markers and non-readable markers MUST be excluded from unique-word counts, occurrence lists, and highlighted matches.
- **FR-031**: The feature MUST NOT change Quran source text, unique-word identity generation, counts, occurrences, or source data.
- **FR-032**: The feature MUST NOT introduce roots exploration, lemma exploration, stem exploration, word-type exploration, audio, global search, editing, importing, or data curation in v1.
- **FR-033**: The interface MUST be Arabic-first and RTL-first, with visible focus, sufficient contrast, accessible controls, and no color-only meaning.
- **FR-034**: Quran word and ayah text shown by this feature MUST come from the existing canonical Quran word and ayah data.

### Key Entities *(include if feature involves data)*

- **Words Hub**: The dashboard entry point for Quran word-study sections. It contains one active v1 section and four future disabled sections.
- **Unique Word Mode**: The selected way to group unique words: with tashkeel or simplified imlaei without tashkeel.
- **Unique Word**: A stable Quran word identity used for browsing, counts, restored views, and occurrence drill-downs. Restored selected-word state uses this stable ID rather than display text.
- **Word Counts**: The four visible distribution counts for a unique word: occurrences, ayahs, surahs, and missing surahs.
- **Mentioned Surah**: A surah where the selected unique word appears, including how many times it appears in that surah.
- **Missing Surah**: A surah where the selected unique word does not appear.
- **Ayah Match**: An ayah that contains one or more exact occurrences of the selected unique word.
- **Word Occurrence**: A specific Quran word position inside an ayah that may be highlighted when it matches the selected unique word.
- **Drill-Down View**: The focused study view for a selected word: surahs, missing surahs, or ayahs.
- **Explorer State**: The restorable user context: mode, search, sort, list page, selected word, drill-down view, and ayah drill-down page.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In 100% of tested dashboard sessions, users can reach the Unique Words explorer from the Words hub in no more than two interactions.
- **SC-002**: In 100% of tested hub views, `الكلمات الفريدة` is active and the four future sections are visibly disabled and non-navigable.
- **SC-003**: In 100% of sampled unique word items, the item shows an Uthmani display word and all four counts: occurrences, ayahs, surahs, and missing surahs.
- **SC-004**: In 100% of sampled unique word items, `لم يذكر في` equals 114 minus the displayed surah count.
- **SC-005**: In 100% of sampled simplified-mode results, the primary label is readable Quran display text rather than a raw technical key.
- **SC-006**: At least 95% of common Arabic searches with omitted tashkeel or common letter-form variants return expected matching results when such words exist.
- **SC-007**: In 100% of sampled words, mentioned surahs and missing surahs are disjoint and together account for all 114 surahs.
- **SC-008**: In 100% of sampled ayah drill-downs, only exact stored matching word occurrences are highlighted, including multiple matches in the same ayah.
- **SC-009**: In 100% of tested restored links, valid mode, search, sort, page, selected word, and drill-down state are restored.
- **SC-010**: In 100% of tested invalid restored links, users receive controlled Arabic feedback and can continue using the list.
- **SC-011**: In 100% of implementation verification checks, no Quran source text, word identities, counts, or occurrence data are changed by using the feature.
- **SC-012**: At least 90% of review participants can distinguish the two modes, `بالتشكيل` and `إملائي (بدون تشكيل)`, after reading the page labels and seeing example results.

## Assumptions

- The target users are the existing Arabic-speaking dashboard admins and teachers.
- The Words hub belongs inside the existing dashboard context for `المنهج القرآني`.
- Stable unique-word identities and validated word distribution counts already exist before this feature begins.
- Stable unique-word IDs are safe for restored URLs and selected-word state.
- The Quran has 114 surahs; missing-surah counts are derived from that fixed total.
- `الكلمات الفريدة` is the only active Words hub section in v1.
- Roots, lemmas, stems, and word-type exploration are future sections represented only as disabled coming-soon cards.
- The with-tashkeel mode is the default because it is closest to the visible Quran text.
- Simplified unique words still use representative Uthmani display text for users.
- Search uses normalized contains matching to favor discoverability for Arabic users over exact technical matching.
- Page sizes are chosen to keep browsing and drill-downs responsive on desktop, tablet, and mobile.
