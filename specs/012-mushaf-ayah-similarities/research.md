# Phase 0 Research: Mushaf Reader Ayah Similarities

## Decision 1: Keep Mushaf Page Response Unchanged

**Decision:** Do not add similarity counters or detail payloads to the Mushaf page response in v1.

**Rationale:** The page response is responsible for page layout, lines, words, markers, and basic ayah metadata. Adding similarity counts to every ayah on the page would make the initial reading payload heavier and shift selected-study data into the page load. The selected ayah study request already runs when the user selects an ayah, so counts belong there.

**Alternatives considered:**
- Add counters to each page ayah: rejected because it bloats initial page load and violates the locked planning report.
- Add counters only to visible selected ayah on page response: rejected because page response should not depend on selected study state.

## Decision 2: Add `similaritySummary` To Selected Ayah Study

**Decision:** Extend selected `AyahStudyResponse` with `similaritySummary` containing `similarAyahCount`, `mutashabihatGroupCount`, and `mutashabihatOccurrenceCount`.

**Rationale:** This gives the UI enough information to label the two new actions while keeping detail payloads lazy. It also aligns with the existing user flow: selecting an ayah already loads selected ayah study details.

**Alternatives considered:**
- Return counts only from detail endpoints: rejected because the action cards would not know availability until clicked.
- Add full details to ayah study: rejected because it violates lazy loading and couples three unrelated payload shapes.

## Decision 3: Use Two Lazy Detail Endpoints

**Decision:** Add separate lazy reads for flat similar ayahs and grouped mutashabihat details.

**Rationale:** The two data families have different shapes and UX: similar meaning ayahs are flat ayah-to-ayah links, while mutashabihat are grouped phrase/word-span similarities. Separate reads keep contracts clear and avoid loading unused detail data.

**Alternatives considered:**
- One combined similarities endpoint: rejected because it loads both detail types even when the user opens only one.
- Embed details in selected ayah study: rejected for payload and conceptual coupling reasons.

## Decision 4: Combine Incoming And Outgoing Similar Links

**Decision:** Reader-facing similar ayahs should combine outgoing and incoming directed links and deduplicate bidirectional relationships.

**Rationale:** Feature 006 stored the source faithfully as directed links, but the source is asymmetrically pruned. A user studying an ayah expects relevant semantic neighbors, not only records where the selected ayah happened to be the source. Combining both directions avoids hiding useful links.

**Alternatives considered:**
- Outgoing only: rejected because incoming-only links would be invisible.
- Persist reverse links: rejected because Feature 006 explicitly avoided generated reverse edges and no schema change is justified.

## Decision 5: Preserve Mutashabihat Grouping

**Decision:** Mutashabihat details must return and render as groups, each with its own occurrences.

**Rationale:** Mutashabihat are phrase/word-span group data. Flattening occurrences would erase the core meaning: which occurrences belong to the same repeated phrase group. A selected ayah can appear in multiple groups, and each group must remain distinct.

**Alternatives considered:**
- Flat occurrence list: rejected because it violates the spec and destroys group semantics.
- Merge similar ayahs and mutashabihat into one relation model: rejected because their grains and user purposes differ.

## Decision 6: Derive Quran Text From Canonical Tables Only

**Decision:** Ayah text comes from canonical ayah text; phrase/word-span text, if returned, is derived from canonical word rows at read time.

**Rationale:** Mutashabihat storage contains references and ranges, not authoritative Quran display text. Quran data safety requires that no Quran text be copied from or invented by similarity tables.

**Alternatives considered:**
- Store denormalized phrase text: rejected because it would require schema changes and duplicate Quran text.
- Reconstruct from source JSON: rejected because Feature 006 data foundation is already imported and the source package should not drive runtime display.

## Decision 7: Widen Existing `ayahTab` URL State

**Decision:** Extend selected ayah tab/action values to `tafsir`, `translation`, `full-i3rab`, `similar-ayahs`, and `mutashabihat`.

**Rationale:** The two new actions are selected-ayah study content and fit the same conceptual control as tafsir, translation, and full i3rab. A separate URL key would add complexity without a known conflict.

**Alternatives considered:**
- Add `ayahAction`: rejected unless implementation finds a concrete conflict with the existing tab/source-selector model.

## Decision 8: No Schema Change Or Migration

**Decision:** Use existing Feature 006 tables and existing indexes; do not create migrations.

**Rationale:** The database baseline shows the required tables and indexes already exist for selected ayah lookups: similar links have source/target indexes, and mutashabihat occurrences have ayah/group indexes. Current row counts are small enough for straightforward read queries.

**Alternatives considered:**
- Add denormalized read tables or materialized summaries: rejected as YAGNI until measured query performance proves a need.
