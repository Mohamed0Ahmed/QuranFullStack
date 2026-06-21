# Phase 0 Research: Words Hub + Unique Words Explorer

## Decision 1: Use Deterministic Unique-Word ID As Selection And URL Identity

**Decision:** Use the stable unique-word `id` as the selected-word key in API routes, frontend state, and restored URLs.

**Rationale:** Feature 013 acceptance verified that both unique tables use deterministic IDs equal to the first Quran word ID and remain stable across rebuilds. IDs are URL-safe, compact, and avoid ambiguity between Uthmani display text, simplified keys, and representative display forms.

**Alternatives considered:**
- Use Uthmani display text: rejected because text is long, encoding-heavy, and not unique across modes in a URL-friendly way.
- Use simple technical keys: rejected because they are not primary user-facing labels and do not apply uniformly to both modes.
- Use both ID and display text: rejected because it adds duplicate identity sources and reconciliation complexity.

## Decision 2: Introduce Minimal `PagedResult<T>`

**Decision:** Add a small reusable paged result contract with `page`, `pageSize`, `totalCount`, and `items`.

**Rationale:** Feature 014 is the first read API that needs traditional page metadata. A minimal shared contract avoids repeated per-feature pagination shapes while staying small enough not to become a dumping abstraction.

**Alternatives considered:**
- Return arrays only: rejected because UI needs total count and page metadata.
- Add a large pagination framework with cursors, links, and filters: rejected as YAGNI for v1.
- Make paging DTO feature-specific: rejected because future list features are expected and the shape is generic.

## Decision 3: Use Two Unique-Word Modes With Stable Keys

**Decision:** Support `tashkeel` and `simple` as the only v1 mode keys. Default to `tashkeel`.

**Rationale:** These map directly to validated Feature 013 unique-word tables and to the product labels `بالتشكيل` and `إملائي (بدون تشكيل)`. `tashkeel` is closest to visible Quran text, making it the safest default.

**Alternatives considered:**
- Add roots/lemma/stem/POS modes now: rejected because they are explicitly out of scope and shown only as coming-soon hub cards.
- Use Arabic labels as route keys: rejected because route keys must be stable and label-independent.

## Decision 4: Use Normalized Contains Search

**Decision:** Search matches when the normalized query appears anywhere in normalized unique-word text, for both modes.

**Rationale:** Arabic users may search by remembered fragments and may omit tashkeel or use common letter variants. Contains matching favors discoverability and was confirmed during clarification. Results still display Uthmani text, never raw technical keys.

**Alternatives considered:**
- Prefix matching: rejected because it hides useful matches when users remember a middle fragment.
- Exact matching: rejected because it is too strict for exploratory browsing.

## Decision 5: Keep List Reads On Unique Tables

**Decision:** The unique-word list reads directly from unique tables and their precomputed counts. It does not group occurrences per card.

**Rationale:** Feature 013 already validates `occurrences_count`, `ayahs_count`, and `surahs_count`. Reading those columns avoids N+1 and avoids unnecessary aggregation over `quran_words` for every visible card. `missingSurahsCount` is derived as `114 - surahsCount`.

**Alternatives considered:**
- Live group `quran_words` for every list item: rejected because it is slower and duplicates validated data-foundation work.
- Store missing surah count separately: rejected because it is a trivial derivation from a fixed Quran surah count.

## Decision 6: Use Separate Read Resources For List, Summary, Surahs, Missing Surahs, And Ayahs

**Decision:** Expose five read resources: paged list, selected word summary, mentioned surahs, missing surahs, and paged ayah matches.

**Rationale:** The frontend needs a lightweight list, a summary for restored modal state, and three distinct drill-down views. Keeping these reads separate avoids loading ayah word payloads when the user only opens surahs or missing surahs.

**Alternatives considered:**
- One combined word-detail endpoint: rejected because it loads unused drill-down data and can over-fetch large ayah payloads.
- Put all drill-down data in the list response: rejected because list cards must remain lightweight.

## Decision 7: Use Modal Drill-Downs With Query-Param State

**Decision:** Drill-downs open as modals over the current list and are restorable via query-param state.

**Rationale:** This preserves the user's list context while supporting refresh, share, and browser navigation. The clarification session confirmed modal behavior.

**Alternatives considered:**
- Dedicated full pages: rejected because they would make returning to list context more disruptive for the v1 study workflow.
- Inline expansion: rejected because long ayah lists would make the list visually unstable and harder to paginate.

## Decision 8: Use ID-Based Highlighting Only

**Decision:** Highlight ayah matches by `quranWordId` membership in `matchedQuranWordIds`, never by text replacement.

**Rationale:** Quran words can repeat or look visually similar. String replacement risks highlighting the wrong word and visually modifying Quranic text incorrectly. Occurrence IDs are exact and already stable.

**Alternatives considered:**
- Highlight by matching displayed text: rejected because it can over-highlight repeated or similar strings.
- Highlight by word number only: rejected because it is not enough without ayah context and matched occurrence identity.

## Decision 9: No Schema Changes Or New Indexes In V1

**Decision:** Use existing tables and indexes. Do not add migrations, tables, columns, imports, or indexes for v1.

**Rationale:** Existing unique tables and filtered `quran_words.unique_*_word_id` indexes support the required reads at current data scale. Optional composite indexes may be considered only after measured profiling shows a real bottleneck.

**Alternatives considered:**
- Add composite drill-down indexes now: rejected because current scale and existing indexes are adequate and no measured problem exists.
- Add denormalized read tables: rejected because counts are already precomputed and drill-downs are straightforward filtered reads.

## Decision 10: Use Existing Feature-First Frontend Pattern

**Decision:** Add `features/words/` with routeable pages, child components, data-access, state/facade, models, and feature routes.

**Rationale:** The feature has real routes, URL state, API-backed loading/error/empty behavior, modal state, and many child visual areas. Feature-first structure keeps files focused and avoids a giant routeable page.

**Alternatives considered:**
- Extend a generic fallback page: rejected because the Words area is now a real feature.
- Put API calls in components directly: rejected because the page has pagination, search, modal state, and URL sync that belong in a facade/store.
