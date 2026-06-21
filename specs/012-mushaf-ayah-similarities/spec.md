# Feature Specification: Mushaf Reader Ayah Similarities

**Feature Branch**: `012-mushaf-reader-ayah-similarities`  
**Created**: 2026-06-21  
**Status**: Draft  
**Input**: User description: "Read our plan - and according to the best practices of Github's speckit, create the spec, Generation Only. The implementation will be done using a cheaper model, so the specification and everything should be super clear. But create new branch for all repos."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See Similarity Availability For A Selected Ayah (Priority: P1)

An Arabic-speaking dashboard admin or teacher selects an ayah in the existing Mushaf Reader and sees, inside the selected ayah study area, whether that ayah has similar meaning ayahs and mutashabihat groups available. The initial Mushaf page reading experience remains focused on page layout and reading; similarity counts appear only after an ayah is selected and its study context is loaded.

**Why this priority**: This is the smallest useful slice. It preserves the reader's calm page view while letting the user know, at the moment of ayah study, whether deeper similarity data exists.

**Independent Test**: Select ayahs that have similarity data and ayahs that do not. Confirm the page itself does not show similarity counters, then confirm the selected ayah study area shows a similarity summary with counts for similar meaning ayahs, mutashabihat groups, and mutashabihat occurrences.

**Acceptance Scenarios**:

1. **Given** the admin opens a Mushaf page, **When** the page appears before any ayah study is loaded, **Then** the page shows reading layout, lines, words, markers, and basic ayah metadata only, with no similar-ayah or mutashabihat counters on the page ayah data.
2. **Given** the admin selects an ayah, **When** the selected ayah study area loads, **Then** it includes a similarity summary with `similarAyahCount`, `mutashabihatGroupCount`, and `mutashabihatOccurrenceCount`.
3. **Given** the selected ayah has no similar meaning ayahs and no mutashabihat groups, **When** the selected ayah study area loads, **Then** the similarity summary shows zero counts without treating the ayah as an error.
4. **Given** the selected ayah has similarity data, **When** the similarity summary appears, **Then** no full similar-ayah list or mutashabihat group details are loaded or displayed until the user opens the matching action.

---

### User Story 2 - Review Similar Meaning Ayahs As A Flat List (Priority: P2)

After selecting an ayah, the admin opens `آيات قريبة في المعنى` and reviews related ayahs as a simple flat list. The list combines relevant directed relationships in both directions so the user does not miss relationships merely because of how the source data was stored.

**Why this priority**: Similar meaning ayahs are useful for semantic comparison and teaching. They are simpler than mutashabihat because they are ayah-to-ayah links, so they should remain easy to scan.

**Independent Test**: Select an ayah with outgoing links, incoming links, and bidirectional links. Open `آيات قريبة في المعنى` and confirm all related ayahs appear once in a flat list, with bidirectional duplicates deduplicated.

**Acceptance Scenarios**:

1. **Given** a selected ayah has outgoing similar meaning links, **When** the admin opens `آيات قريبة في المعنى`, **Then** the related target ayahs appear in a flat list.
2. **Given** a selected ayah has incoming similar meaning links, **When** the admin opens `آيات قريبة في المعنى`, **Then** those related ayahs also appear in the same flat list.
3. **Given** the same related ayah is linked in both directions, **When** the flat list is shown, **Then** that related ayah appears once, marked or ordered as bidirectional if such metadata is shown.
4. **Given** a listed similar ayah, **When** the admin reads the item, **Then** it shows the target ayah reference, Arabic surah name, page context, canonical ayah text, and available source metrics such as score, coverage, and matched-word count when they support ordering or confidence without distracting from Quran study.
5. **Given** a selected ayah has no similar meaning ayahs, **When** the admin opens `آيات قريبة في المعنى`, **Then** the interface shows the Arabic empty state `لا توجد آيات قريبة في المعنى لهذه الآية في البيانات الحالية.`

---

### User Story 3 - Review Mutashabihat As Phrase Groups (Priority: P2)

After selecting an ayah, the admin opens `المتشابهات اللفظية للحفظ` and sees mutashabihat grouped by phrase or word-span group. The selected ayah may belong to more than one group, and each group lists its occurrences across ayahs without flattening unrelated groups together.

**Why this priority**: Mutashabihat support memorization and wording comparison. Their value depends on preserving phrase grouping; flattening occurrences would remove the meaning of the data.

**Independent Test**: Select an ayah known to belong to multiple mutashabihat groups. Open `المتشابهات اللفظية للحفظ` and confirm each group is separate, each group contains its own occurrence list, and the selected ayah occurrence is visible inside every relevant group.

**Acceptance Scenarios**:

1. **Given** a selected ayah belongs to multiple mutashabihat groups, **When** the admin opens `المتشابهات اللفظية للحفظ`, **Then** the interface shows multiple separate group cards or sections.
2. **Given** a mutashabihat group contains occurrences in several ayahs, **When** the group is displayed, **Then** the group contains its occurrences together under that group and does not mix them with occurrences from other groups.
3. **Given** the selected ayah appears in a mutashabihat group, **When** that group is displayed, **Then** the selected ayah occurrence is visibly identified without relying on color alone.
4. **Given** phrase or word-span text is shown for a group or occurrence, **When** the text appears, **Then** it is derived from the canonical Quran word text for that ayah and word range, not copied from mutashabihat storage.
5. **Given** a selected ayah has no mutashabihat groups, **When** the admin opens `المتشابهات اللفظية للحفظ`, **Then** the interface shows the Arabic empty state `لا توجد متشابهات لفظية مسجلة لهذه الآية في البيانات الحالية.`

---

### User Story 4 - Reopen A Similarity Study View From The URL (Priority: P3)

The admin can share or bookmark a Mushaf Reader URL that restores the selected page, selected ayah, and active selected-ayah action, including the two new similarity actions.

**Why this priority**: Teachers and reviewers need repeatable study links, but this is secondary to displaying the similarity data correctly.

**Independent Test**: Open the selected ayah study area, switch to each of the two new similarity actions, copy the URL, reopen it in a new session, and confirm the same selected ayah action is restored.

**Acceptance Scenarios**:

1. **Given** the admin is viewing `آيات قريبة في المعنى`, **When** the URL is copied and reopened, **Then** the reader restores the same selected ayah and opens the similar meaning action.
2. **Given** the admin is viewing `المتشابهات اللفظية للحفظ`, **When** the URL is copied and reopened, **Then** the reader restores the same selected ayah and opens the mutashabihat action.
3. **Given** the URL represents the selected ayah action, **When** it is read by the system, **Then** the accepted action values are `tafsir`, `translation`, `full-i3rab`, `similar-ayahs`, and `mutashabihat`.

---

### Edge Cases

- Selected ayah has no similar meaning ayahs but has mutashabihat groups.
- Selected ayah has similar meaning ayahs but no mutashabihat groups.
- Selected ayah has neither similarity type.
- Selected ayah belongs to multiple mutashabihat groups.
- Selected ayah has multiple occurrences within one mutashabihat group.
- A related similar ayah is linked in both directions and must not appear twice.
- A related similar ayah exists only as an incoming relationship and should still appear by default.
- A phrase word range cannot be resolved cleanly from canonical word text; the system must not invent phrase text and must still show usable range metadata.
- Very long similar-ayah or mutashabihat lists must remain contained inside the selected ayah study area without breaking the Mushaf Reader layout.
- Invalid or unknown ayah references must produce clear Arabic error or empty states without changing Quranic data.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST keep the Mushaf page view focused on page layout, lines, words, markers, and basic ayah metadata.
- **FR-002**: The system MUST NOT add `similarAyahCount`, `mutashabihatGroupCount`, `mutashabihatOccurrenceCount`, similar-ayah details, or mutashabihat details to the initial Mushaf page data in v1.
- **FR-003**: The selected ayah study area MUST show five ayah study actions: `التفسير`, `الترجمة`, `الإعراب الكامل`, `آيات قريبة في المعنى`, and `المتشابهات اللفظية للحفظ`.
- **FR-004**: When an ayah is selected, the selected ayah study data MUST include a `similaritySummary` containing `similarAyahCount`, `mutashabihatGroupCount`, and `mutashabihatOccurrenceCount`.
- **FR-005**: `similarAyahCount` MUST represent distinct related ayahs after combining incoming and outgoing directed relationships and deduplicating bidirectional matches.
- **FR-006**: `mutashabihatGroupCount` MUST represent the number of distinct mutashabihat groups that contain the selected ayah.
- **FR-007**: `mutashabihatOccurrenceCount` MUST represent the total occurrences across the selected ayah's mutashabihat groups, including selected-ayah occurrences.
- **FR-008**: The system MUST load similar meaning ayah details only when the user opens `آيات قريبة في المعنى` or reopens a URL with that action active.
- **FR-009**: The system MUST load mutashabihat group details only when the user opens `المتشابهات اللفظية للحفظ` or reopens a URL with that action active.
- **FR-010**: Similar meaning ayahs MUST render as a flat list of ayah-to-ayah relationships.
- **FR-011**: Similar meaning ayahs MUST combine incoming and outgoing directed relationships for reader-facing display.
- **FR-012**: Similar meaning ayahs MUST deduplicate bidirectional relationships so one related ayah appears once.
- **FR-013**: Each similar meaning ayah item MUST show enough information for the admin to identify it: ayah reference, Arabic surah name, ayah number, page context, and canonical ayah text.
- **FR-014**: Mutashabihat details MUST render grouped by phrase/group and MUST NOT flatten all occurrences into one list.
- **FR-015**: A selected ayah that belongs to multiple mutashabihat groups MUST show each group separately.
- **FR-016**: Each mutashabihat group MUST show its own occurrences across ayahs.
- **FR-017**: Each mutashabihat occurrence MUST show enough information for the admin to identify it: ayah reference, Arabic surah name, ayah number, page context, word range, and canonical ayah text.
- **FR-018**: The selected ayah occurrence inside a mutashabihat group MUST be visually identifiable without relying on color alone.
- **FR-019**: If phrase or word-span text is shown, the system MUST derive it from canonical Quran word text for the relevant ayah and word range.
- **FR-020**: Ayah text displayed by this feature MUST come from canonical ayah text.
- **FR-021**: The system MUST NOT copy Quran text from mutashabihat storage.
- **FR-022**: The system MUST NOT write, edit, import, normalize, delete, or migrate Quranic similarity data as part of this feature.
- **FR-023**: The system MUST show clear Arabic empty states for selected ayahs with no similar meaning ayahs or no mutashabihat groups.
- **FR-024**: The system MUST preserve the active selected-ayah action in shareable reader URLs, using the accepted values `tafsir`, `translation`, `full-i3rab`, `similar-ayahs`, and `mutashabihat`.
- **FR-025**: The system MUST keep long similarity lists inside stable selected-ayah study panels so the Mushaf Reader layout remains usable on desktop, tablet, and mobile.
- **FR-026**: The system MUST NOT introduce public-reader behavior, audio, bookmarks, memorization scheduling, editing, approval workflows, or graph exploration in this feature.

### Key Entities *(include if feature involves data)*

- **Selected Ayah**: The ayah currently chosen in the Mushaf Reader. It has an ayah reference, surah identity, canonical ayah text, page context, and existing study content.
- **Similarity Summary**: Lightweight counts attached to the selected ayah study area: similar meaning ayah count, mutashabihat group count, and mutashabihat occurrence count.
- **Similar Meaning Ayah**: A related ayah linked to the selected ayah by semantic similarity. It is displayed as one flat item in a list and may be discovered from either direction of the stored relationship.
- **Mutashabihat Group**: A phrase or word-span group representing repeated wording useful for memorization comparison. One selected ayah can belong to multiple groups.
- **Mutashabihat Occurrence**: One appearance of a mutashabihat group's phrase or word span in a specific ayah, including word range and whether it is the selected ayah occurrence.
- **Canonical Quran Text**: The authoritative ayah and word text used for display. Similarity storage is treated as references and ranges, not as a source for Quran text.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In 100% of sampled Mushaf page loads, no similar-ayah or mutashabihat counts or detail lists appear before an ayah is selected.
- **SC-002**: In 100% of sampled selected ayahs, the selected ayah study area shows a similarity summary with all three count fields, including zero-count cases.
- **SC-003**: In 100% of sampled ayahs with bidirectional similar meaning relationships, each related ayah appears only once in the similar meaning list.
- **SC-004**: In 100% of sampled ayahs with multiple mutashabihat groups, groups remain visually and structurally separate.
- **SC-005**: In 100% of sampled mutashabihat displays that include phrase text, phrase text is traceable to canonical word text for the displayed ayah and word range.
- **SC-006**: Admins can open either new selected-ayah action from an already selected ayah in no more than two user interactions.
- **SC-007**: In manual network or interaction inspection, full similar-ayah details and mutashabihat group details are not loaded until their corresponding action is opened.
- **SC-008**: At least 90% of review participants can correctly describe the difference between `آيات قريبة في المعنى` and `المتشابهات اللفظية للحفظ` after using the UI labels and layout.
- **SC-009**: Reopening a shared URL restores the selected ayah and active similarity action for both new actions in 100% of tested cases.

## Assumptions

- The target users are the same Arabic-speaking dashboard admins and teachers served by the existing Mushaf Reader.
- Feature 011's Mushaf Reader and selected ayah study area already exist and remain the host experience for this feature.
- Feature 006's similar meaning ayah and mutashabihat data already exists and is available for read-only use.
- Similar meaning ayahs are reader-facing relationships, so incoming and outgoing directed records are both considered.
- Mutashabihat are phrase/group-based and must be understood through their groups, not as independent flat ayah links.
- `mutashabihatOccurrenceCount` counts all occurrences across the selected ayah's groups, including selected-ayah occurrences, unless a later product clarification changes the displayed count label and semantics.
- Similar ayah source metrics such as score, coverage, and matched-word count may be returned and shown quietly when they support ordering or confidence without distracting from Quran study.
- Existing secure local reader behavior and Arabic RTL design rules continue to apply.
