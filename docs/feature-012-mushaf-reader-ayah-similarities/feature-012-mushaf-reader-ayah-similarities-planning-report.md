# Feature 012 — Mushaf Reader Ayah Similarities: Planning Report

**Project:** Quran Dashboard / المنهج القرآني  
**Feature:** 012 — Mushaf Reader Ayah Similarities  
**Report type:** Planning only; no Spec Kit artifacts yet  
**Date:** 2026-06-21  
**Status:** Finalized planning source for the next `/speckit.specify` step

## 1. Feature Summary

Feature 012 exposes the already-imported Feature 006 similar-ayah and mutashabihat data inside the existing Feature 011 Dashboard Mushaf Reader selected-ayah study area.

The selected ayah study area currently offers three study actions/tabs:

- `التفسير`
- `الترجمة`
- `الإعراب الكامل`

Feature 012 should add two more ayah-level actions:

- `آيات قريبة في المعنى` — flat similar-meaning ayah links.
- `المتشابهات اللفظية للحفظ` — grouped phrase/word-span mutashabihat.

The core product distinction must stay visible in both API and UI:

- Similar meaning ayahs are ayah-to-ayah links and may render as a flat list.
- Mutashabihat are phrase-group records. A selected ayah can appear in multiple groups, and each group contains multiple occurrences across ayahs. They must render grouped by phrase/group, never flattened into one list of ayahs.

This feature is read-only. It should not import, edit, normalize, or persist Quran text. Ayah display text should continue to come from canonical Quran tables such as `quran_ayahs` and `quran_words`, not from mutashabihat storage.

Locked v1 API posture:

- Do not add similarity counters or relationship details to the Mushaf page response.
- Add lightweight similarity counts to the selected `AyahStudyResponse` only.
- Lazy-load full similar-ayah and mutashabihat detail payloads only when the user opens the corresponding selected-ayah action.

## 2. Scope

In scope:

- Add selected-ayah similarity counts to the existing ayah study experience, preferably without loading full detail payloads up front.
- Add lazy-loaded details for similar meaning ayahs when the user selects `آيات قريبة في المعنى`.
- Add lazy-loaded grouped mutashabihat details when the user selects `المتشابهات اللفظية للحفظ`.
- Preserve the existing dashboard Mushaf Reader route and selected-ayah study placement.
- Reuse the existing `ApiResponse<T>` API envelope and Arabic-default user-facing messages.
- Reuse the existing frontend data-access/facade/cache pattern under `features/mushaf/`.
- Support URL-restorable ayah action state if the selected action is represented as an ayah tab/action.
- Show empty states clearly when an ayah has no similar links or no mutashabihat groups.
- Keep all reads over the existing HTTPS backend configuration from Feature 011.

Recommended minimal data scope:

- Existing selected ayah study API should include counts only in `similaritySummary`:
  - `similarAyahCount`
  - `mutashabihatGroupCount`
  - `mutashabihatOccurrenceCount`
- Full detail payloads should be fetched only when the user clicks the corresponding action.
- The Mushaf page response stays focused on page layout, lines, words, and basic ayah metadata. It must not add `similarAyahCount`, `mutashabihatGroupCount`, or `mutashabihatOccurrenceCount` to page ayah DTOs in v1.

## 3. Out Of Scope

Out of scope:

- No database schema changes unless later implementation discovers a proven query limitation not visible in the current database report.
- No migrations.
- No new importers and no re-import of Feature 006 data.
- No Mushaf page response changes for similarity counts or detail payloads.
- No editing, curation, approval, hiding, or annotation of similarity records.
- No public reader feature.
- No audio, bookmarks, memorization scheduling, quizzes, progress tracking, or spaced repetition.
- No graph visualization or cross-surah exploration mode.
- No all-source browsing of tafsir/translation/i3rab beyond Feature 011 behavior.
- No copying Quran text into mutashabihat-derived tables or denormalized similarity tables.
- No persisted reverse similar-ayah edges.
- No attempt to merge similar meaning links and mutashabihat into one polymorphic relationship model.

## 4. Existing Data Sources And Tables Expected To Be Used

Primary source reports:

- `docs/feature-006-quran-mutashabihat-foundation/feature-006-quran-mutashabihat-foundation-planning-report.md`
- `Backend/report/feature-006-quran-mutashabihat-foundation/005-final-completion-report.md`
- `docs/feature-011-mushaf-reader-study-context/feature-011-ayah-word-data-capability-report.md`
- `Backend/report/database/current-database-tables-and-relationships-report.md`

Current database baseline confirms these Feature 006 tables exist:

| Table | Rows | Purpose |
| --- | ---: | --- |
| `quran_mutashabihat_groups` | 814 | Repeated-phrase group headers with representative ayah and representative word range. |
| `quran_mutashabihat_occurrences` | 3,557 | Occurrences of each group in ayahs, including `word_from` / `word_to` spans. |
| `quran_similar_ayah_links` | 3,552 | Directed source-to-target similar ayah links with score, coverage, matched words count, and raw matched-word ranges. |

Related canonical tables:

| Table | Use |
| --- | --- |
| `quran_ayahs` | Resolve selected `verseKey`, target ayah identity, canonical `text_uthmani`, surah/ayah/page/juz/hizb/rub metadata. |
| `quran_surahs` | Arabic surah names for target/occurrence labels. |
| `quran_words` | Derive occurrence phrase text or word-span display from canonical word rows when a phrase preview/highlight is needed. |
| `quran_mushaf_pages` / `quran_mushaf_lines` | Optional navigation labels and page/line context for off-page occurrences. |

Important table semantics:

- `quran_similar_ayah_links` is stored directed. The source data is asymmetrically pruned; some reverse links are absent. A reader-facing list should normally query both `source_ayah_id = selected` and `target_ayah_id = selected` to avoid hiding relevant incoming links.
- `quran_mutashabihat_occurrences` is the ayah-to-group lookup table. Query selected ayah occurrences first, then load sibling occurrences for each group.
- Mutashabihat tables store references and word indices, not Quran text. Any ayah text or phrase text displayed by the feature should be read from `quran_ayahs` / `quran_words` at request time.
- The current indexes support the expected reads: `quran_mutashabihat_occurrences.ayah_id`, `quran_mutashabihat_occurrences(group_id, ayah_id, word_from, word_to)`, `quran_similar_ayah_links(source_ayah_id, target_ayah_id)`, and `quran_similar_ayah_links.target_ayah_id`.

Schema-change assessment:

- No schema change is justified by the current reports.
- Existing indexes are aligned with both lazy endpoints.
- A migration should be rejected unless implementation proves a concrete performance problem with measured queries on the real dataset.

## 5. Backend API Options And Recommended Contract

### Option A — Extend Existing Ayah Study With Full Details

Shape:

- Add `similarAyahs` and `mutashabihatGroups` directly to `GET /api/mushaf/ayahs/{verseKey}/study`.

Pros:

- One request after ayah selection.
- Simple frontend integration.

Cons:

- Violates the preferred lazy-loading constraint.
- Makes the existing ayah study response heavier for users who only need tafsir/translation/i3rab.
- Couples two very different data shapes to the existing three-source study response.

Verdict: not recommended.

### Option B — Counts In Ayah Study, Two Lazy Detail Endpoints

Shape:

- Keep `GET /api/mushaf/pages/{pageNumber}` unchanged for similarity purposes.
- Keep `GET /api/mushaf/ayahs/{verseKey}/study` focused on selected ayah study, but add lightweight similarity counts.
- Add a flat lazy endpoint for similar ayahs.
- Add a grouped lazy endpoint for mutashabihat.

Pros:

- Keeps the initial Mushaf page response focused on page layout, lines, words, and basic ayah metadata.
- Keeps initial selected-ayah study payload light.
- Lets action cards show meaningful counts before details load.
- Keeps flat and grouped concepts separate.
- Fits existing Feature 011 lazy-loading and cache patterns.

Cons:

- Adds two endpoints and two frontend loading states.

Verdict: recommended.

### Option C — One Combined Ayah Relationships Endpoint

Shape:

- Add `GET /api/mushaf/ayahs/{verseKey}/similarities` returning both flat similar links and grouped mutashabihat.

Pros:

- One lazy request for both relationship types.
- Avoids adding detail payloads to the existing ayah study response.

Cons:

- Still loads both detail types even when the user clicks only one.
- Encourages UI and model conflation between flat links and grouped phrase occurrences.

Verdict: acceptable fallback, but less precise than Option B.

### Recommended Backend Contract

Use Option B.

The Mushaf page endpoint remains unchanged in v1:

```text
GET /api/mushaf/pages/{pageNumber}
```

Do not add similarity counters or relationship details to page, line, word, or page-ayah DTOs. This response should stay limited to page navigation/layout, Mushaf lines, words, markers, and basic page/ayah metadata already needed to render the page.

#### Existing endpoint extension

Endpoint:

```text
GET /api/mushaf/ayahs/{verseKey}/study?tafsirSource=...&translationSource=...&fullI3rabSource=...
```

Add a lightweight `similaritySummary` block to `AyahStudyResponse`:

```json
{
  "ayah": {},
  "selectedSources": {},
  "tafsir": null,
  "translation": null,
  "fullI3rab": null,
  "similaritySummary": {
    "similarAyahCount": 4,
    "mutashabihatGroupCount": 2,
    "mutashabihatOccurrenceCount": 9
  }
}
```

Notes:

- `similarAyahCount` should count distinct related ayahs after combining outgoing and incoming directed links.
- `mutashabihatGroupCount` should count distinct groups containing the selected ayah.
- `mutashabihatOccurrenceCount` should count occurrences across the selected ayah's groups. If implementation excludes selected-ayah occurrences from the count, the API field or UI copy must make that semantics explicit; otherwise count all occurrences in those groups.
- The count block belongs in the selected ayah study response because the frontend already requests selected ayah study details when an ayah is selected.
- These counts do not authorize eager loading of full detail payloads. Similar-ayah and mutashabihat details remain separate lazy reads.

#### Similar meaning ayahs endpoint

Endpoint:

```text
GET /api/mushaf/ayahs/{verseKey}/similar-ayahs
```

Response under `ApiResponse.data`:

```json
{
  "verseKey": "2:25",
  "count": 4,
  "items": [
    {
      "targetVerseKey": "2:26",
      "surahNumber": 2,
      "surahNameArabic": "...",
      "ayahNumber": 26,
      "pageNumber": 5,
      "juzNumber": 1,
      "hizbNumber": 1,
      "rubNumber": 1,
      "textUthmani": "...",
      "score": 91,
      "coverage": 100,
      "matchedWordsCount": 8,
      "relationshipDirection": "outgoing",
      "hasReverseLink": true
    }
  ]
}
```

Contract notes:

- `textUthmani` must be read from `quran_ayahs`, not copied from similarity data.
- `relationshipDirection` can be `outgoing`, `incoming`, or `bidirectional` after deduplication.
- Query both outgoing and incoming directed rows for reader-facing display unless a later clarification explicitly chooses strict source direction.
- If both directions exist, return one item per related target ayah with `relationshipDirection = "bidirectional"` and `hasReverseLink = true`; use the highest score/coverage for sorting or expose both directional score fields only if needed.
- Sort by strongest score first, then natural Mushaf order.
- Return `200 OK` with an empty `items` array when the selected ayah exists but has no links.
- Return `400 Bad Request` for malformed verse keys and `404 Not Found` when the selected ayah does not exist.

#### Mutashabihat grouped endpoint

Endpoint:

```text
GET /api/mushaf/ayahs/{verseKey}/mutashabihat
```

Response under `ApiResponse.data`:

```json
{
  "verseKey": "2:25",
  "groupCount": 2,
  "groups": [
    {
      "groupKey": "mutashabihat:1234",
      "sourceGroupId": 1234,
      "representativeVerseKey": "2:25",
      "representativeWordFrom": 3,
      "representativeWordTo": 6,
      "phraseTextUthmani": "...",
      "occurrenceCount": 5,
      "distinctAyahCount": 5,
      "distinctSurahCount": 3,
      "selectedOccurrences": [
        {
          "verseKey": "2:25",
          "wordFrom": 3,
          "wordTo": 6,
          "isRepresentative": true,
          "phraseTextUthmani": "..."
        }
      ],
      "occurrences": [
        {
          "verseKey": "2:25",
          "surahNumber": 2,
          "surahNameArabic": "...",
          "ayahNumber": 25,
          "pageNumber": 5,
          "wordFrom": 3,
          "wordTo": 6,
          "isSelectedAyah": true,
          "isRepresentative": true,
          "textUthmani": "...",
          "phraseTextUthmani": "..."
        }
      ]
    }
  ]
}
```

Contract notes:

- Preserve `groups[]` as the top-level detail shape. Do not return one flat occurrence list.
- `groupKey` should be stable and non-database-internal. A derived key from `source_group_id` is acceptable because `source_group_id` is a source/provenance key, not a database surrogate id.
- `textUthmani` should be read from `quran_ayahs`.
- `phraseTextUthmani`, if returned, should be derived at read time from canonical `quran_words` using `ayah_id + word_from..word_to`.
- Include `wordFrom` / `wordTo` even if phrase text is returned, so the UI can display a range label and later support highlighting without a contract break.
- A selected ayah can appear in multiple groups. Each group must remain distinct and contain its own occurrence list across ayahs.
- Sort groups by selected occurrence position in the selected ayah, then by `sourceGroupId`.
- Sort occurrences inside each group by Mushaf order.
- Return `200 OK` with an empty `groups` array when the selected ayah exists but belongs to no groups.

#### Backend placement

Recommended backend shape if implemented later:

- Application abstractions under `Quran/MushafReader/` or a nested `Quran/MushafReader/AyahSimilarities/` feature folder.
- Application queries:
  - `GetSimilarAyahs`
  - `GetAyahMutashabihat`
- Infrastructure read repositories near existing Mushaf Reader read repositories.
- API controllers under existing Mushaf Reader ayah controllers, preserving routes under `api/mushaf/ayahs`.
- Cache decorators can be added after read tests pass, with keys such as:
  - `mushaf:similar-ayahs:{verseKey}`
  - `mushaf:mutashabihat:{verseKey}`

## 6. Frontend UX Behavior

### Placement

Add two additional actions under the existing selected-ayah study actions/cards:

- `التفسير`
- `الترجمة`
- `الإعراب الكامل`
- `آيات قريبة في المعنى`
- `المتشابهات اللفظية للحفظ`

The exact visual treatment should follow the Feature 011 selected-ayah section rather than creating a new public-reader layout. The UI should remain calm, RTL-first, and suitable for long study sessions.

### Labels

Recommended Arabic labels:

| UI element | Arabic label |
| --- | --- |
| Similar meaning action | `آيات قريبة في المعنى` |
| Similar meaning short tab | `آيات قريبة` |
| Mutashabihat action | `المتشابهات اللفظية للحفظ` |
| Mutashabihat short tab | `المتشابهات` |
| Empty similar state | `لا توجد آيات قريبة في المعنى لهذه الآية في البيانات الحالية.` |
| Empty mutashabihat state | `لا توجد متشابهات لفظية مسجلة لهذه الآية في البيانات الحالية.` |
| Similar loading state | `جارٍ تحميل الآيات القريبة...` |
| Mutashabihat loading state | `جارٍ تحميل المتشابهات اللفظية...` |

### Similar Meaning Ayahs UX

Behavior:

- Clicking `آيات قريبة في المعنى` lazy-loads the flat list if not already loaded/cached.
- Each row/card shows target verse reference, Arabic surah name, page number, optional score/coverage metadata, and canonical ayah text.
- Clicking a target ayah should navigate/select that ayah in the Mushaf Reader when feasible, preserving existing page/ayah URL behavior.
- Incoming-only records should not be visually treated as lower quality solely because direction is incoming; the source data is directed for dataset reasons, not necessarily product meaning.

Recommended display order:

- Highest score first.
- Bidirectional links before one-way links when scores tie.
- Natural Mushaf order as final tie-breaker.

### Mutashabihat UX

Behavior:

- Clicking `المتشابهات اللفظية للحفظ` lazy-loads grouped data if not already loaded/cached.
- Render one group card per mutashabihat group.
- The group header should show a phrase/range summary, occurrence count, and distinct ayah/surah count.
- Inside each group, render occurrences as a list of ayah references with canonical ayah text and phrase range.
- Highlight or visually mark the selected ayah occurrence within each group.
- Keep all occurrences for the same phrase group together. Do not intermix occurrences from different groups.
- If a selected ayah has multiple selected occurrences in the same group, show them under the same group rather than duplicating the group card.

Recommended group card copy:

- Header: `موضع متشابه` or `مجموعة متشابهة`
- Count line: `تظهر في {n} مواضع ضمن {m} سور`
- Selected occurrence label: `موضع الآية المحددة`
- Representative label when useful: `الموضع الممثل للمجموعة`

### Loading And Caching

- Do not load either detail list during initial page load.
- Do not load detail lists merely because an ayah is selected. The selected ayah study response may return counts, but similar-ayah details load only when `آيات قريبة في المعنى` is active, and mutashabihat details load only when `المتشابهات اللفظية للحفظ` is active.
- Reuse the existing bounded frontend cache/dedupe approach.
- Cache by selected `verseKey` only; no user-specific state is involved.
- Empty successful responses can be cached on the frontend if the existing cache policy allows successful empty data, but failed HTTP responses should not be cached.

### URL State

Feature 011 currently limits ayah tabs to `tafsir`, `translation`, and `full-i3rab`. Feature 012 should intentionally widen this set if these actions become tabs.

Recommended ayah tab values:

- `tafsir`
- `translation`
- `full-i3rab`
- `similar-ayahs`
- `mutashabihat`

Alternative:

- Keep the existing three source tabs and add a separate `ayahAction` URL key for the two relationship actions.

Recommendation:

- Use widened `ayahTab` values. The five actions all represent selected-ayah study content and belong in the same conceptual control.
- Do not introduce a separate URL key unless implementation finds a concrete conflict with the existing tab/source-selector model. No such conflict is identified in this planning report.

## 7. Suggested Phases

### Phase 1 — Contract And Query Design

- Encode the finalized decision that counts are added only to `AyahStudyResponse.similaritySummary`, not to the Mushaf page response.
- Finalize endpoint names and DTO fields.
- Encode the finalized recommendation that similar links combine outgoing and incoming rows for reader display, deduplicating bidirectional results unless a later clarification changes this.
- Encode the rule that phrase preview text, if returned, comes from canonical `quran_words` at read time.

### Phase 2 — Backend Read APIs

- Add application query handlers for similar ayahs and mutashabihat groups.
- Add read repository methods using `AsNoTracking` and canonical joins.
- Add thin API controller actions under `api/mushaf/ayahs`.
- Add Arabic message keys close to the Mushaf Reader feature.
- Add cache decorators only after read behavior tests pass.

### Phase 3 — Frontend Data Access And State

- Add DTO models for `SimilarAyahsResponse` and `AyahMutashabihatResponse`.
- Add API services for the two lazy endpoints.
- Extend reader state with separate load states for similar ayahs and mutashabihat.
- Extend cache keys and facade loading methods.
- Widen ayah tab URL parsing/normalization if using the recommended tab approach.

### Phase 4 — Frontend UI

- Add two selected-ayah actions/tabs under the existing study controls.
- Add flat similar-ayah card/list component.
- Add grouped mutashabihat group component.
- Add loading, empty, and error states in Arabic.
- Ensure mobile/drawer behavior preserves the active ayah action.

### Phase 5 — Verification And Review

- Run backend integration tests with seeded data.
- Run frontend unit tests for state, URL parsing, and rendering groups.
- Manually verify ayahs with no data, one similar link, multiple mutashabihat groups, and multiple occurrences in a group.
- Perform engineering review focused on data safety, grouping correctness, and payload size.

## 8. Acceptance Criteria

Functional acceptance:

- Selecting an ayah still shows the existing tafsir, translation, and full i3rab actions unchanged.
- The selected-ayah study area includes `آيات قريبة في المعنى` and `المتشابهات اللفظية للحفظ`.
- The Mushaf page response does not include `similarAyahCount`, `mutashabihatGroupCount`, `mutashabihatOccurrenceCount`, similar-ayah details, or mutashabihat details.
- The selected ayah study response includes `similaritySummary.similarAyahCount`, `similaritySummary.mutashabihatGroupCount`, and `similaritySummary.mutashabihatOccurrenceCount`.
- Similar meaning ayahs load only when the user clicks their action or opens a URL with that action active.
- Similar meaning ayahs render as a flat list of ayah-to-ayah relationships.
- Similar meaning ayahs combine incoming and outgoing directed links for reader-facing display and deduplicate bidirectional results unless a later clarification changes this.
- Mutashabihat load only when the user clicks their action or opens a URL with that action active.
- Mutashabihat render grouped by phrase/group, with each group containing its occurrences.
- A selected ayah that belongs to multiple mutashabihat groups shows multiple separate group cards.
- A selected ayah with no similar links shows a clear Arabic empty state.
- A selected ayah with no mutashabihat groups shows a clear Arabic empty state.
- Invalid verse keys return controlled `400` responses; unknown ayahs return controlled `404` responses.

Data acceptance:

- No Quran text is read from mutashabihat tables.
- Ayah text comes from `quran_ayahs.text_uthmani`.
- Phrase/word-span display, if present, is derived from canonical `quran_words` at read time.
- No database writes occur.
- No migrations are created.
- No importers are created or run for this feature.
- Similar link deduplication handles bidirectional rows without showing duplicate target ayah cards.
- Mutashabihat grouping preserves `source_group_id`/group identity and does not flatten occurrences.

UX acceptance:

- Arabic labels are clear for end users and distinguish semantic similarity from memorization/wording similarity.
- Loading states are scoped to the clicked action, not the entire Mushaf page.
- Long lists scroll inside stable study cards/panels without breaking the Mushaf Reader layout.
- The selected ayah occurrence is visually identifiable in mutashabihat groups without relying on color alone.
- On mobile/tablet, the active ayah action remains reachable and URL-restorable.

Performance acceptance:

- Initial Mushaf page load remains unchanged and does not include similarity details.
- Initial ayah study load includes at most lightweight counts, not full details.
- Detail responses are cached/deduplicated consistently with Feature 011 patterns.

## 9. Test Plan

Backend tests:

- Mushaf page endpoint contract does not expose similarity counters or detail payloads.
- Ayah study endpoint returns `similaritySummary` with `similarAyahCount`, `mutashabihatGroupCount`, and `mutashabihatOccurrenceCount`.
- Similar ayahs endpoint returns `400` for malformed verse key.
- Similar ayahs endpoint returns `404` for well-formed unknown verse key.
- Similar ayahs endpoint returns `200` with empty `items` for an existing ayah with no links.
- Similar ayahs endpoint combines outgoing and incoming rows and deduplicates bidirectional target ayahs.
- Similar ayahs endpoint sorts by score and stable natural order.
- Similar ayahs endpoint maps target ayah text from `quran_ayahs`.
- Mutashabihat endpoint returns `400` for malformed verse key.
- Mutashabihat endpoint returns `404` for well-formed unknown verse key.
- Mutashabihat endpoint returns `200` with empty `groups` for an existing ayah outside all groups.
- Mutashabihat endpoint returns one group object per distinct mutashabihat group.
- Mutashabihat endpoint includes all sibling occurrences for each selected group.
- Mutashabihat endpoint preserves multiple selected occurrences in one group when present.
- Mutashabihat endpoint derives phrase text from `quran_words` if phrase text is included.
- Count summary tests verify `similarAyahCount`, `mutashabihatGroupCount`, and `mutashabihatOccurrenceCount` semantics.
- Cache tests verify identical successful reads are cached and not-found/failure responses are not cached unless explicitly justified.

Frontend tests:

- URL normalization accepts the new ayah tab/action values.
- Selecting `آيات قريبة في المعنى` triggers only the similar ayahs API.
- Selecting `المتشابهات اللفظية للحفظ` triggers only the mutashabihat API.
- Concurrent duplicate clicks dedupe requests through the existing cache layer.
- Similar ayah list renders flat items and empty/error states.
- Mutashabihat component renders grouped cards and does not flatten occurrences.
- Selected ayah occurrence has an accessible label/marker.
- Mobile state preserves the active ayah action.

Manual verification:

- Verify one ayah known to have similar links.
- Verify one ayah known to be present in multiple mutashabihat groups.
- Verify one ayah with no similar links and no mutashabihat groups.
- Verify a target similar ayah click navigates/selects the target ayah without breaking page state.
- Inspect browser network requests to confirm detail endpoints are lazy-loaded over HTTPS only.

## 10. Risks And Review Points

Risks:

- Similar ayah links are directed in storage, while users may expect a relationship to work both ways. The read API should deliberately combine incoming and outgoing links for dashboard study unless the product chooses a strict source-direction view.
- `coverage` can exceed 100 in known source rows. Do not silently clamp unless the UI label explicitly says a display percentage is normalized. Prefer showing raw source coverage only if meaningful, or hide coverage in v1.
- Mutashabihat word ranges depend on imported word indices. Feature 006 treated upper-bound mismatches as warnings. The read layer should not invent phrase text if a range cannot resolve cleanly; it should return range metadata and a controlled missing phrase state.
- Provenance/license was noted as unresolved in the Feature 006 completion report. This does not block internal dashboard planning, but it should be reviewed before public exposure or publishing.
- Adding five ayah actions into the current selected-ayah section may crowd the UI on smaller screens. Prefer compact tabs/actions with internal scrolling or segmented grouping rather than large equal-width cards.
- Returning full ayah text for many mutashabihat occurrences can grow payloads. If payloads are larger than expected, return phrase/range summaries first and load full occurrence ayah text on group expansion.

Review points before implementation:

- Treat incoming plus outgoing similar links, deduplicated, as the implementation default unless `/speckit.specify` records a later clarification changing it.
- Confirm whether the UI should show score/coverage, or hide technical scoring behind a simple ordering.
- Phrase text previews are allowed in v1 only when derived from canonical `quran_words`; otherwise show word ranges and canonical ayah text.
- Counts are locked to `AyahStudyResponse.similaritySummary`, not the Mushaf page response.
- URL strategy is locked as a recommendation to widen `ayahTab` unless implementation finds a concrete conflict requiring a separate key.

## 11. Spec Kit Recommendation

Spec Kit is recommended before implementation, but not required for this planning-report-only step.

Why recommended:

- The feature touches both backend and frontend.
- It adds API contracts where grouping semantics matter.
- It widens frontend URL state and selected-ayah UI behavior.
- It has Quran data-safety constraints that should be encoded as explicit requirements.
- Implementation may be delegated, so a precise `spec.md`, `plan.md`, contracts, and `tasks.md` would reduce ambiguity.

Recommended Spec Kit posture:

- Use this report as the source input for Feature 012.
- The next step after this report update is `/speckit.specify`, not `/speckit.plan`.
- Keep the Spec Kit scope small: two lazy endpoints, required counts in selected ayah study, frontend selected-ayah actions, grouped mutashabihat rendering, tests.
- Do not include migrations, import work, public reader scope, audio, bookmarks, or editing in the Spec Kit feature.

If the team chooses not to use Spec Kit, the report is still sufficient for a small senior-engineer implementation, but the API contracts and grouped mutashabihat acceptance criteria should be copied into implementation tickets verbatim.

## 12. Changelog

- Finalized that the Mushaf page response must not include similarity counters or similarity detail payloads in v1.
- Finalized that selected `AyahStudyResponse` includes `similaritySummary.similarAyahCount`, `similaritySummary.mutashabihatGroupCount`, and `similaritySummary.mutashabihatOccurrenceCount`.
- Reaffirmed Option B as the recommended API direction: selected ayah study returns counts only, with separate lazy endpoints for similar meaning ayahs and grouped mutashabihat details.
- Locked similar meaning ayahs as a flat reader-facing list that combines incoming and outgoing directed links and deduplicates bidirectional results unless a later clarification changes this.
- Locked mutashabihat display as grouped by phrase/group, never flattened, with phrase text derived from canonical `quran_words` if returned.
- Reaffirmed Quran text safety: ayah text from `quran_ayahs.text_uthmani`, phrase/word-span text from `quran_words`, no database writes, no migrations, and no importers.
- Reaffirmed frontend URL recommendation to widen `ayahTab` with `similar-ayahs` and `mutashabihat` unless a concrete implementation conflict requires a separate URL key.
- Clarified that the next workflow step is `/speckit.specify`; `/speckit.plan` comes later only after the specification exists.
