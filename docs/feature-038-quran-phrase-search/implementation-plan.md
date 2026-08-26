# Quran Phrase Search — Implementation Plan

> Status: ready for implementation
>
> Planning format: normal implementation plan, not Spec Kit
>
> Working branch: `wordsSearchFeature`
>
> Scope: derived phrase index, read APIs, and the Quran phrase-search workspace
>
> Lifecycle: this file and the companion HTML prototype are temporary feature artifacts. They are removed after the engineering review passes and before the final merge, as required by `docs/README.md`.

## 1. Required outcome

Build a dedicated Quran phrase-search workspace with three connected capabilities:

1. **Exact repetitions:** list the most repeated adjacent two-word phrases, then three-word phrases, and any other supported length selected by the user.
2. **Manual phrase context:** find every occurrence of a user-entered phrase and progressively explore what appears before and after it, without losing the pairing between the two sides.
3. **Approximate similarity:** compare equal-length phrases position by position, explain matched and differing words, and support a minimum of 50% matching.

Every window, occurrence, context branch, and similarity comparison remains inside one ayah. Nothing crosses an ayah boundary.

## 2. Scope boundaries

### In scope

- Simple and tashkil-aware identity modes.
- Manual context search from one resolved Quran source token up to the current source maximum of 128 tokens.
- Exact repetition lists from two tokens upward.
- Manual approximate search from two tokens upward.
- The global approximate explorer from four tokens upward.
- Full Uthmani ayah rendering with word-ID-based highlighting.
- A derived index that can be built on the existing database without reimporting tafsirs, translations, Abwab, morphology, or i3rab.
- Staged, validated, atomic build activation with a compatible rollback generation.
- Public, read-only HTTP APIs that comply with the repository's unsafe-method policy.

### Out of scope for v1

- Semantic or morphological similarity.
- Insertions, deletions, or reordered words between compared phrases.
- Cross-ayah windows.
- Merging the existing `quran_mutashabihat_*` source-backed tables into this feature. They remain a separate data product.
- New Backend test classes or test methods, new `*.spec.ts` files, or new/expanded Playwright journeys without explicit owner approval.
- Applying a migration, mutating a database, staging Git changes, committing, pushing, opening a PR, formal review, or deployment without the separate authority required for that action.

## 3. Locked product contracts

### 3.1 Quran source-token grain

- The counting unit is a non-marker `QuranWord` row.
- Ordering is `word_number` within `ayah_id`.
- Quran windows are never built by splitting display text on whitespace.
- `ياأيها` is one source token. A user may type a helpful space, but query resolution must map the input back to the source token before counting.
- Every valid start position creates a window, including overlapping windows.
- Two occurrences in the same ayah are two independent occurrences.

### 3.2 Identity and display

| Concern | Contract |
|---|---|
| Display | Always render the original `text_uthmani` words returned by the API |
| Simple exact identity | `unique_simple_word_id`, built from the MASAQ-backed `word_key_imlaei_simple` value |
| Tashkil exact identity | `unique_tashkeel_word_id`, using the current `DisplayWordsSql.TashkeelIdentityCte` semantics |
| Ignored tashkil-identity marks | Tatweel, U+0653, U+06D6..U+06DC, U+06DE, U+06E9, and U+200F |
| Preserved tashkil distinctions | Harakat, shadda, tanween, sukun, and every distinction currently preserved by the display-word identity contract |

The current tashkil identity literal must be extracted into one Infrastructure-owned contract reused by both `rebuild-words` and PhraseSearch. Its behavior must not change during that extraction.

### 3.3 Exact identity versus input tolerance

The corpus identity remains exact. Hamza tolerance exists only while resolving user input:

- `ExactTokenIds` stores the exact corpus identity sequence.
- `SearchTokenIds` stores a secondary search-equivalence sequence.
- Query resolution may use `SearchTokenIds`, but repetition grouping, Quran-to-Quran similarity scoring, context branches, and selected context paths use `ExactTokenIds`.
- The displayed Quran text is never replaced by a normalized search value.
- Waw, fa, ba, and every clitic remain significant. `كان الله` is not `وكان الله`.
- This feature does not fold `ة` to `ه` or `ى` to `ي`.

Create a PhraseSearch-specific normalizer. Do not reuse `ArabicSearchQueryNormalizer` unchanged because its fold map is broader than this contract.

The PhraseSearch hamza map is:

- `أ`, `إ`, `آ`, and `ٱ` resolve through `ا`.
- `ؤ` resolves through `و`.
- `ئ` resolves through `ي`.
- A standalone `ء` does not create a search distinction when the user omits it.
- No other letters change.

The normalizer first applies Unicode canonical normalization. In simple mode it removes diacritics and the agreed ignorable marks. In tashkil mode it preserves every mark that participates in the exact tashkil identity.

### 3.4 Query resolution and one scoring basis

Approximate matching must not assign two different scores to the same corpus pair.

1. Raw input is normalized only to resolve each user token to one or more exact corpus token identities.
2. If one unambiguous exact token sequence is resolved, the server returns a build-scoped `resolutionRef` containing that exact sequence.
3. If folded spelling or tokenization allows multiple exact sequences, the server returns `ambiguous` with explicit candidates. The UI requires a choice; it never picks silently.
4. If a token cannot resolve to a Quran source identity, the result is `unresolved`. V1 does not invent an unknown identity and count it as a fuzzy mismatch.
5. Manual and global Hamming scores both compare `ExactTokenIds`.

This keeps hamza-tolerant typing while preserving one Quran-to-Quran similarity definition and the profiler baseline.

### 3.5 Exact repetition

- A repeated phrase is the same `ExactTokenIds` sequence at least twice.
- The primary count is occurrence count, not only distinct ayah count.
- The default list is simple mode, length 2, occurrence count descending.
- Single-token variants are stored for manual context resolution but are not exposed in the general repetition list.

### 3.6 Paired before/after context

- Every occurrence has one logical pair: `before | query | after` within its ayah.
- The previous path starts with the nearest token before the query and grows outward toward `[Start of ayah]`.
- The following path starts with the nearest token after the query and grows toward `[End of ayah]`.
- Query lookup uses the resolved exact query sequence. Surrounding branch grouping and path selection use the selected mode's `ExactTokenIds`, never `SearchTokenIds` and never individual `QuranWordId` values.
- Selecting a branch on either side recomputes both sides and the result set from the same filtered occurrence population.
- The two sides have independent pagination cursors. Loading more on one side must not reset the other side or the current context-results page.

Every branch option contains:

- An opaque build-scoped exact token reference.
- Original display text.
- `passesThroughCount`.
- `sideEndsHereCount` when that side reaches its ayah boundary.
- Boundary kind when applicable.

The branch invariant is:

```text
passesThroughCount = sideEndsHereCount + sum(child.passesThroughCount)
```

`sideEndsHereCount` is only a one-sided boundary count under the current two-sided filter. It must not be labelled as a complete exact context count. `exactFullContextCount` is returned only when both side boundaries are fixed, or as the occurrence count of one complete before/query/after context group.

### 3.7 Approximate similarity

- Compared phrases have the same length.
- Each position is either an exact match or a substitution.
- No insertion, deletion, reordering, semantic expansion, or cross-ayah matching is included in v1.
- The integer threshold rule is:

```text
matchedCount * 100 >= minimumMatchPercent * wordCount
```

- Global presets are 50%, 60%, 70%, 80%, and 90%.
- Nine words at 50% require at least five exact matches and allow at most four substitutions.
- The manual UI may let the user edit either percentage or maximum differences, but it sends one unambiguous `minimumMatchedWords` value. The server rejects anything below `ceil(wordCount / 2)`.
- A global "group" is one anchor phrase plus its directly verified neighbors. It is not a transitive connected component; A~B and B~C must not imply A~C.

## 4. Measurement baseline and storage decision

The profiler ran read-only against the current approved source. Its temporary script and result file were deleted afterward, so the values below are planning baselines that the first production builder report must independently reproduce. They are not a retained evidence artifact by themselves.

### 4.1 Source integrity

| Metric | Observed value |
|---|---:|
| Readable Quran words | 77,432 |
| Ayahs | 6,236 |
| MASAQ-to-Uthmani joins | 77,432 / 77,432 |
| Word-order gaps inside ayahs | 0 |
| Location mismatches | 0 |
| Longest ayah | 2:282, 128 source tokens |

### 4.2 Windows and variants

| Metric | Observed value |
|---|---:|
| Windows of length 2+ per mode | 718,523 |
| Windows of length 1+ per mode | 795,955 |
| Combined occurrence rows including singles | 1,591,910 |
| Distinct simple variants, length 2+ | 664,782 |
| Distinct tashkil variants, length 2+ | 669,643 |
| Combined distinct variants including singles | 1,368,351 |
| Combined exact repeated variants | 49,174 |
| Occurrences belonging to exact repeated variants | 151,795 |
| Longest repeated simple phrase | 24 tokens |
| Longest repeated tashkil phrase | 23 tokens |

### 4.3 Similarity edges

| Minimum match | All lengths | Length 4+ only |
|---|---:|---:|
| 50% | 7,928,564 | 1,115,977 |
| 60% | 521,234 | 236,650 |
| 70% | 100,789 | 100,789 |
| 80% | 33,091 | 33,091 |
| 90% | 1,682 | 1,682 |

- Lengths 2 and 3 contribute 6,812,587 edges at 50%, or 85.92% of all 50% edges.
- Distinct length-2 variants have no edge at 60%+, and distinct length-3 variants have no edge at 70%+.
- The raw engineering estimate for length-4+ edges at 50% is about 107–179 MB before actual row, index, and WAL overhead.
- Maximum non-zero edge lengths are 48 at 50%, 40 at 60%, 34 at 70%, 30 at 80%, and 26 at 90%.
- All 254 mode/length cells were counted exactly; no estimate was used.

### 4.4 Locked storage strategy

- Store phrase variants and occurrences for every source length from 1 through the observed maximum.
- Precompute and store similarity edges only for lengths 4+.
- Do not store length-2 or length-3 edges. Manual search for those lengths scans only the same-length variant partition.
- Store each edge once with matched count, difference count, and difference positions. Do not duplicate an edge for every threshold.
- Store small per-anchor threshold statistics separately so the global explorer does not group and count 1.1 million edges for every page request.

## 5. Backend placement

PhraseSearch is a separate Quran bounded context. It must not become a global utility folder and must not be placed inside the source-backed Mutashabihat feature.

### 5.1 Domain

```text
Backend/domain/QuranDashboard.Domain/Quran/PhraseSearch/
```

Planned types:

- `PhraseIndexBuild`
- `PhraseIndexState`
- `QuranPhraseSearchToken`
- `QuranPhraseVariant`
- `QuranPhraseOccurrence`
- `QuranPhraseSimilarityEdge`
- `QuranPhraseSimilarityAnchorStat`
- `PhraseTextMode`
- `PhraseIndexBuildStatus`

### 5.2 Application and Infrastructure

```text
Backend/application/QuranDashboard.Application.Abstractions/Quran/DataPipelines/PhraseSearch/
Backend/application/QuranDashboard.Application/Quran/DataPipelines/PhraseSearch/
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/PhraseSearch/
Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/DataPipelines/PhraseSearch/
Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/PhraseSearchDependencyInjection.cs
Backend/tools/QuranDashboard.DataImporter/Import/VerbRunners/BuildPhraseIndexRunner.cs
```

Read-side placement:

```text
Backend/application/QuranDashboard.Application.Abstractions/Quran/PhraseSearch/
Backend/application/QuranDashboard.Application/Quran/PhraseSearch/Queries/
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/PhraseSearch/
Backend/api/QuranDashboard.Api/Controllers/Quran/PhraseSearch/
```

Use separate repetition, context, and similarity reader interfaces and thin controllers. Do not create one oversized reader or controller.

## 6. Executable data model

IDs inside a generation are builder-assigned deterministic `bigint` values. A variant resource is always identified by the composite `(buildId, variantId)`; IDs are never assumed to identify the same phrase across builds.

### 6.1 `quran_phrase_index_builds`

- Primary key: `id uuid`, generated by the application.
- `status`: Building, Validated, Active, Superseded, Failed.
- `format_version`.
- `exact_ready` and `similarity_ready`.
- `builder_version`.
- `source_revision` and `source_fingerprint`.
- Started, validated, activated, failed, and completed timestamps.
- Compact totals, verdict, report path, and a redacted failure summary.
- A partial unique index ensures at most one row has Active status.

Final feature activation requires the current format version with both readiness flags true. No exact-only generation may be served as a completed feature build.

### 6.2 `quran_phrase_index_state`

A migration-seeded singleton row contains:

- `source_revision` owned by PhraseSearch source coordination.
- Current semantic `source_fingerprint`, nullable while foundation data lacks rebuilt identities.
- `active_build_id`.
- `previous_build_id` when compatible with the current fingerprint.
- `is_stale`, `stale_reason`, and `updated_at_utc`.

The active and previous foreign keys use `ON DELETE SET NULL`. A check prevents both pointers from referencing the same build. API readers use only the active pointer.

### 6.3 `quran_phrase_search_tokens`

- Composite primary key: `(build_id, mode, id)`.
- `search_text`, non-empty.
- `exact_token_ids integer[]`, non-empty, listing every exact corpus identity that resolves through this search spelling.
- Unique key: `(build_id, mode, search_text)`.

This dictionary supports explicit ambiguity rather than silently merging exact corpus identities.

### 6.4 `quran_phrase_variants`

- Composite primary key: `(build_id, id)`.
- `mode`, `word_count`.
- `exact_token_ids integer[]`.
- `search_token_ids integer[]`.
- `display_text`, derived from the first occurrence for list rendering only.
- `occurrence_count`, `ayah_count`, `surah_count`.
- `first_quran_word_id`.
- Alternate key: `(build_id, id, mode, word_count)` for child composite foreign keys.
- Unique key: `(build_id, mode, word_count, exact_token_ids)`.
- Non-unique exact-search index: `(build_id, mode, word_count, search_token_ids)`.
- Repetition-list index: `(build_id, mode, word_count, occurrence_count DESC, id)`.
- Checks require both arrays to have `word_count` elements.

### 6.5 `quran_phrase_occurrences`

- Composite primary key: `(build_id, id)`.
- `variant_id`, `mode`, `word_count`.
- `ayah_id`, `start_word_number`, `end_word_number`.
- `first_quran_word_id`, `last_quran_word_id`.
- Composite foreign key `(build_id, variant_id, mode, word_count)` to the variant alternate key.
- Unique key `(build_id, variant_id, ayah_id, start_word_number)`; end position is derived by length.
- Check: `end_word_number - start_word_number + 1 = word_count`.
- Index `(build_id, variant_id, ayah_id, start_word_number)`.
- Index `(build_id, ayah_id, start_word_number, end_word_number)`.
- Base Quran foreign keys are Restrict; source reset coordination removes incompatible phrase builds before foundation truncation.

Add or prove an existing readable index for context hydration on `quran_words(ayah_id, word_number)` filtered to non-markers. The current surah/ayah/word index is not treated as proof for the `ayah_id` access path; the final choice needs `EXPLAIN` evidence.

### 6.6 `quran_phrase_similarity_edges`

- `build_id`, `mode`, `word_count`.
- `left_variant_id`, `right_variant_id`, with `left < right`.
- `matched_count`, `difference_count`.
- `difference_positions smallint[]`, one-based.
- Composite foreign keys for both endpoints to `(build_id, id, mode, word_count)` on variants.
- Primary/unique key `(build_id, left_variant_id, right_variant_id)`.
- Checks: no self-edge, `matched + different = word_count`, difference-array cardinality equals difference count, and at least `ceil(word_count / 2)` matches.

Neighbor indexes:

- `(build_id, left_variant_id, matched_count DESC, right_variant_id)`.
- `(build_id, right_variant_id, matched_count DESC, left_variant_id)`.

Mode and length may be included columns when the measured query plan benefits from them. They must not precede the fixed anchor ID in the neighbor seek index.

### 6.7 `quran_phrase_similarity_anchor_stats`

- Composite primary key `(build_id, variant_id, threshold)`.
- `mode`, `word_count`, `neighbor_count`, and optional best matched count.
- Threshold check: one of 50, 60, 70, 80, 90.
- Composite foreign key to the variant alternate key.
- Global-list index `(build_id, mode, word_count, threshold, neighbor_count DESC, variant_id)`.

These rows are threshold aggregates, not duplicated relationships.

### 6.8 Delete and retention policy

| Relationship/state | Policy |
|---|---|
| Build to its search tokens, variants, occurrences, edges, and stats | Cascade as one generation |
| State active/previous pointer to build | Set null; state is locked and cleared before deletion |
| Variant to occurrences/edges/stats | Composite FK with generation-safe cascade semantics |
| Occurrence to Quran base rows | Restrict during ordinary deletes |
| Active and previous compatible builds | Never removed by automatic cleanup |
| Other superseded builds | Eligible after a grace interval longer than the maximum API timeout |
| Failed build child rows | Removed after the failure report is durable; compact build metadata is retained for the configured audit period |

The implementation must validate the generated migration's actual cascade graph. It must not rely on a prose-only cross-row CHECK.

### 6.9 EF placement and migration rule

```text
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs
Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/PhraseSearch/
```

- Add explicit DbSets and snake_case mappings.
- Generate the migration only with:

```bash
cd Backend
./scripts/add-mig AddQuranPhraseSearchIndex
```

- Never hand-write the migration.
- Stop after generation. Applying it to a named database requires separate explicit authority.

## 7. Source coordination and freshness

### 7.1 Phrase-owned source state

Introduce `PhraseSourceStateCoordinator` in Infrastructure. It owns:

- One documented PostgreSQL advisory-lock namespace/key used by `import-foundation`, `rebuild-words`, and `build-phrase-index`.
- The PhraseSearch `source_revision` and `source_fingerprint` fields in the singleton state row.
- Source invalidation and activation race prevention.

Morphology, i3rab, tafsirs, translations, navigation, Mutashabihat, and Abwab do not touch this revision.

### 7.2 Semantic fingerprint

Compute SHA-256 over the ordered non-marker tuple:

```text
quran_word.id
ayah_id
word_number
text_uthmani
word_key_imlaei_simple
canonical_tashkeel_identity
unique_simple_word_id
unique_tashkeel_word_id
```

Serialize every field with an unambiguous length prefix over canonical UTF-8 bytes. Including the original `text_uthmani` ensures that a display-only source change, including an ignored identity mark, cannot reuse a fingerprint while changing persisted `display_text`. The tuple definition is versioned with the builder format. The revision increments only when this semantic fingerprint changes.

### 7.3 First-run state initialization

The migration seeds `source_revision = 0` with a null fingerprint. On an already populated database,
`build-phrase-index` initializes that state before staging:

1. Acquire the PhraseSearch source-mutation lock and lock the singleton row.
2. Verify that every non-marker word has both exact identity links and that the current display-word invariants pass.
3. Compute the approved semantic fingerprint from the complete tuple above.
4. Persist the fingerprint, advance the PhraseSearch source revision, and commit.
5. If identity links are absent or inconsistent, refuse initialization and require `rebuild-words`; never infer a partial fingerprint.

This bootstrap changes PhraseSearch state only. It does not rebuild or mutate Quran source/display data.

### 7.4 Foundation and display-word behavior

`import-foundation --force` transaction order:

1. Take the shared PhraseSearch source-mutation advisory lock.
2. Lock the state singleton.
3. Clear active and previous pointers and mark the index stale.
4. Mark builds incompatible and delete generation children/builds in FK-safe order.
5. Perform the existing foundation truncate/copy.
6. Set the phrase source fingerprint to null and increment the phrase source revision.
7. Commit atomically.

`rebuild-words` transaction order after its existing validation succeeds:

1. Hold the same source-mutation lock.
2. Compute the new semantic fingerprint.
3. If it is unchanged, retain the active and previous phrase builds.
4. If it changed, increment the phrase source revision, update the fingerprint, clear incompatible pointers, and mark old builds stale/superseded.
5. Commit the display-word rebuild and phrase freshness change together.

A failed foundation import or display-word rebuild rolls all PhraseSearch state changes back.

### 7.5 Approved baseline handling

Separate fixed integrity invariants from observed baseline totals:

- Fixed invariants include no markers, no cross-ayah windows, contiguous word order, complete identity links, valid array lengths, and valid edge math.
- Observed counts in section 4 are keyed to the approved source fingerprint and builder version.
- The same approved fingerprint with different totals is a hard failure.
- An unknown fingerprint returns `SourceApprovalRequired`, writes a full non-activating report, and does not accept `--force` as a bypass.
- Approving a new fingerprint requires explicit source review, a fresh measurement, and an intentional update of the versioned baseline before rerunning.

## 8. Full index build

### 8.1 Importer command and dependency order

Add a source-free derived-data verb:

```text
build-phrase-index [--report-out <path>] [--force]
```

Canonical dependency order:

```text
import-foundation
  -> rebuild-words
    -> build-phrase-index
```

The builder depends on Quran words and both completed unique-word identity links. It does not depend on later import products.

Update:

- `Backend/tools/QuranDashboard.DataImporter/Program.cs`.
- `Backend/tools/QuranDashboard.DataImporter/Import/VerbRunners/BuildPhraseIndexRunner.cs`.
- `Backend/tools/QuranDashboard.DataImporter/Import/DefaultPaths/DataImporterDefaults.cs`.
- DataImporter DI.
- `Backend/tools/QuranDashboard.DataImporter/README.md`.
- `Backend/scripts/README.md`.

### 8.2 Source snapshot

1. Run the section 7.3 state bootstrap if the migration-seeded fingerprint is still null.
2. Take the PhraseSearch builder advisory lock so only one phrase build runs.
3. In a short read-only repeatable-read transaction, read the state revision/fingerprint and all 77,432 source-token tuples.
4. Close that source transaction after materializing the bounded source set.
5. Build and stage the new generation without holding a long source transaction.
6. Recheck revision and fingerprint under the source-state fence during activation.

### 8.3 Variants and occurrences

For each mode:

1. Group readable words by ayah and order by `word_number`.
2. Refuse any missing exact identity or non-contiguous sequence.
3. Generate every in-ayah window from length 1 through ayah length.
4. Group identical `ExactTokenIds` into deterministic local variant IDs.
5. Create one occurrence per start position.
6. Calculate occurrence, ayah, surah, and first-occurrence summaries.
7. Build the search-token dictionary and variant `SearchTokenIds`.

Use staging and PostgreSQL binary COPY or set-based inserts. Do not insert 1.5 million occurrences one EF entity at a time.

### 8.4 Similarity edges

- Do not generate length-2 or length-3 edges.
- For each mode and length 4+, represent a variant with positional features `(position, ExactTokenId)`.
- Order features globally by rarity and use an exact overlap-prefix candidate filter for the 50% floor.
- Deduplicate candidates, verify the complete Hamming distance, and persist only verified qualifying pairs.
- For small high-length partitions, switch to bounded brute force when the measured work estimate is lower.
- Hashes and signatures are candidate aids only; the full token arrays resolve collisions.
- Build anchor statistics from the verified edge set.

Record per mode/length: raw windows, variants, candidate emissions, unique candidates, verified pairs, edges, elapsed time, and peak memory when available.

### 8.5 Activation

1. Create a build row in Building state.
2. Write all generation rows under its `build_id` without touching active data.
3. Run every hard check.
4. Mark exact and similarity readiness only after their checks pass.
5. Start a short activation transaction and acquire the PhraseSearch source-state fence.
6. Re-read source revision and fingerprint.
7. If either changed, fail activation and retain the old active generation.
8. If unchanged, mark the new build Validated, flip state active/previous pointers, update statuses, and commit.
9. Run eligible cleanup after activation, never before it.

`--force` permits another build while an active generation exists. It never deletes the active build first and never bypasses source approval or integrity checks.

### 8.6 Reader snapshot and cleanup safety

- Each multi-query read request opens a read-only `REPEATABLE READ` transaction, reads one active build ID, and performs branch aggregation, paging, and ayah hydration in that snapshot.
- The response includes `activeBuildId`.
- Cleanup never removes active or previous builds.
- Other superseded builds have a minimum 15-minute grace by default, exceeding the 10-second request timeout. Both values are configuration-backed.
- PostgreSQL MVCC plus the reader snapshot prevents a request from mixing generations.

### 8.7 Build report

Default local path:

```text
resources/report/quran-phrase-search/<build-id>/
```

Write Markdown and JSON containing:

- Build ID, format/builder version, status, Persisted, Active, and readiness flags.
- Source revision and fingerprint before and at activation.
- Previous and new active build IDs.
- Per-mode/per-length counts and algorithms.
- Total variants, occurrences, edges, and anchor stats.
- Duration, peak memory when available, and disk preflight figures.
- Every hard check, warning, and redacted failure.

The first approved activation report must reproduce or explicitly stop on the planning baseline in section 4.

## 9. Read API

### 9.1 Why all public operations are GET

The repository classifies POST, PUT, PATCH, and DELETE as unsafe and forbids anonymous unsafe endpoints. PhraseSearch remains a public read feature and does not expand that security contract. Every endpoint is therefore GET and idempotent.

The only raw-query transfer uses base64url UTF-8 in a dedicated resolution request. The measured longest source ayah is 2,289 UTF-8 bytes and 3,052 base64url characters, versus 6,867 percent-encoded characters. Splitting resolution from later branch requests keeps the current maximum under the server request-line budget. The server caps decoded input at 4 KiB and validates the encoded length before decoding.

Application logs redact the `q64`, resolution, and path-reference parameter values.

### 9.2 Endpoints

Base route:

```text
/api/quran/phrase-search
```

| Method | Path | Purpose |
|---|---|---|
| GET | `/capabilities` | Active build, modes, supported lengths, readiness, and thresholds |
| GET | `/query-resolutions` | Resolve `mode` + `q64` to exact build-scoped candidate sequences |
| GET | `/repetitions` | Paged exact repeated phrases |
| GET | `/repetitions/{buildId}/{variantId}/occurrences` | Paged full-ayah occurrences |
| GET | `/contexts/branches` | Independently paged previous and following branch options |
| GET | `/contexts/groups` | Paged complete before/query/after context groups |
| GET | `/contexts/occurrences` | Paged ayah occurrences for one complete context reference |
| GET | `/similarities/search` | Manual exact-identity Hamming search for a resolved query |
| GET | `/similarity-groups` | Paged global anchor phrases for length 4+ |
| GET | `/similarity-groups/{buildId}/{variantId}/matches` | Paged direct verified neighbors |

Every route is added to `Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs` in the same change.

### 9.3 Build-scoped references

- `resolutionRef`, branch path refs, and full-context refs are compact base64url binary references containing a format byte, exact token IDs, and a checksum.
- They are not trusted identifiers. The server validates format, checksum, build ID, mode, length, and membership in the active generation.
- Variant routes always include both build ID and local variant ID.
- A stale build reference returns HTTP 409 with `Errors = ["phrase_index_changed"]` and an Arabic message.
- Missing/stale index state returns HTTP 503 with `Errors = ["phrase_index_unavailable"]` and an Arabic message.
- Invalid or oversized input returns HTTP 400 with a stable error code in `Errors`.
- Query resolution itself returns HTTP 200 with `status = resolved | ambiguous | unresolved` and candidate data, because ambiguity is a valid user decision state rather than a transport failure.

This uses the current `ApiResponse<T>` envelope and does not add a global error-envelope field.

### 9.4 Query parser

Resolution order is deterministic:

1. Apply Unicode and mode-specific search normalization.
2. Respect typed whitespace boundaries first.
3. Allow a boundary-spanning source alias when the concatenated input maps to an actual source token, such as `يا أيها` to `ياأيها`.
4. Prefer a fully attested exact source-token sequence over an unattested segmentation.
5. If multiple equal valid resolutions remain, return `ambiguous` candidates instead of choosing the fewest tokens silently.
6. If any segment has no Quran identity, return `unresolved`.

Candidate count is capped and stable-sorted. The UI displays exact token chips and requires a candidate selection before context or approximate requests.

### 9.5 Pagination and stable ordering

- Default page size: 25. Maximum: 100.
- Previous and following branch lists use separate opaque cursors and totals.
- Context groups have their own page.
- Occurrences inside a context group have their own page.
- Loading more in one surface does not alter the others.
- Repetition lists sort by the requested order, then stable variant ID.
- Branch options sort by count descending, then exact token ID.
- Occurrences sort by surah, ayah, then start word number.

### 9.6 Response contracts

Every page-ready response includes `activeBuildId`.

An ayah occurrence includes:

- Verse key, surah, ayah, and page metadata.
- Full readable `words[]` with `quranWordId`, word number, and original Uthmani text.
- Separate word-ID sets for each highlight role. Angular never locates matches by string comparison.

Context branch responses include:

- The resolved query and selected exact paths, both nearest-first.
- Independent previous/following options, cursor, and total metadata.
- `passesThroughCount` and `sideEndsHereCount`.
- `exactFullContextCount` only when both boundaries are complete; otherwise null.
- The total occurrence population after applying both selected paths.

Similarity responses include:

- Resolved/anchor phrase and compared phrase.
- Word count, matched count, difference count, and derived percentage.
- Matched and differing one-based positions.
- Full ayah occurrences and highlight word IDs.

### 9.7 Read strategy and limits

- Repetitions and global similarity read precomputed rows.
- Context branches are grouped on demand from the same filtered occurrence set; trees are not persisted.
- Manual similarity scans only the active build's variants of the same mode and length and compares `ExactTokenIds`.
- Length 2 and 3 are always on demand. Length 4+ may use stored edges when the resolved query is an existing variant, but the final score is still verified from exact arrays.
- Raw-query or selected-path results are never placed in unbounded memory-cache keys.
- Capabilities and common public pages may use bounded caching keyed by active build ID.

Apply a named `PhraseSearchCompute` policy to resolution, context, and manual-similarity endpoints:

- Configuration-backed default concurrency: 4.
- Queue limit: 8.
- Server timeout: 10 seconds.
- Maximum page size: 100.
- Maximum decoded raw query: 4 KiB and 128 resolved tokens.

Global listing endpoints continue to use the existing public-read/global limiter. Phase 8 may lower, but not silently raise, these limits based on measured plans.

## 10. Frontend architecture

### 10.1 Routes and navigation

Base route:

```text
/dashboard/words/phrases
```

Child routes:

```text
/dashboard/words/phrases/repetitions
/dashboard/words/phrases/context
/dashboard/words/phrases/similarity
```

- `/phrases` redirects to `/repetitions`.
- Add `words-phrases` labelled `البحث في القرآن` to Words navigation and the command launcher.
- Add a dedicated card to the Words Hub. Do not fold it into the existing morphology explanation chain.

Current integration points:

```text
Frontend/quran-dashboard-ui/src/app/core/navigation/route-paths.ts
Frontend/quran-dashboard-ui/src/app/core/navigation/words-nav-items.ts
Frontend/quran-dashboard-ui/src/app/features/words/words.routes.ts
Frontend/quran-dashboard-ui/src/app/features/words/components/words-local-nav/
Frontend/quran-dashboard-ui/src/app/features/words/pages/words-hub-page/
```

### 10.2 Feature placement

```text
Frontend/quran-dashboard-ui/src/app/features/words/quran-phrase-search/
  quran-phrase-search.routes.ts
  pages/
    quran-phrase-search-shell/
    phrase-repetitions-page/
    phrase-context-page/
    phrase-similarity-page/
  components/
    phrase-search-tabs/
    phrase-text-mode-toggle/
    phrase-repetitions-list/
    phrase-occurrence-list/
    phrase-query-resolution/
    phrase-context-explorer/
    phrase-context-branch-list/
    phrase-context-breadcrumb/
    phrase-full-context-list/
    phrase-similarity-list/
    phrase-highlighted-ayah/
  data-access/
    phrase-resolution.api.ts
    phrase-repetitions.api.ts
    phrase-context.api.ts
    phrase-similarity.api.ts
  state/
    phrase-repetitions.facade.ts
    phrase-repetitions-url-sync.ts
    phrase-context.facade.ts
    phrase-context-url-sync.ts
    phrase-context-selection.store.ts
    phrase-similarity.facade.ts
    phrase-similarity-url-sync.ts
  models/
```

- Provide each facade at its route page so state does not leak across tabs.
- Use explicit submit by button or Enter for query resolution. Do not request on every keystroke.
- Use `switchMap` to cancel superseded HTTP requests.
- Keep URL parsing/serialization pure and outside the facade.
- Split before a hard file-size limit: component TS/HTML 400 lines, SCSS 300, API service 350, facade 600. Treat the architecture soft limits as the point to plan the split, not as a target to exceed.

### 10.3 URL and restore contract

Common state:

- Repetitions: `build`, `mode`, `length`, `sort`, `page`, `phrase`, `occPage`.
- Context: `build`, `q`, `resolution`, `before`, `after`, `contextsPage`.
- Similarity: `build`, `source`, `q`, `resolution`, `mode`, `length`, `min`, `sort`, `page`.

Use compact base36/base64url references for IDs and paths. Before writing URL state, serialize the complete URL:

- At 1,800 characters or fewer, the state is shareable and compatible with the current 2,048-character navigation-resume guard.
- Above that limit, keep the full working state in route-scoped session state and place only the safe base state in the URL. Show a clear notice that this long state is session-only and that a copied link restores the base query, not every selected branch.
- Do not claim that every 128-token state is fully shareable.

When any response's `activeBuildId` differs from route state:

1. Cancel in-flight requests.
2. Clear build-scoped caches, variant IDs, resolution refs, and selected branch refs.
3. Preserve the raw user query when safe.
4. Show `تغير فهرس البحث، أعد اختيار النتيجة` instead of a misleading 404 or empty state.

### 10.4 Reuse and ayah rendering

Reuse:

- Shared tabs, loading, refreshing, empty, error, pagination, result-count, result-list, and ayah-card primitives.
- Words explorer toolbar/search-row where they fit.
- Session scroll state.
- Existing Mushaf deep-link helpers and `DetailOverlayAyahLinkDirective` patterns.

Do not reuse the current single-match-set highlighted ayah component unchanged. `phrase-highlighted-ayah` receives explicit word-ID roles from the API.

Context-page visual priority:

1. Query words.
2. Selected previous path.
3. Selected following path.
4. All remaining ayah words in the normal style.

Similarity-page visual priority:

1. Differing positions.
2. Matching positions.
3. All remaining words in the normal style.

Each role has a non-color cue and legend. Context and similarity roles are not rendered simultaneously in one card.

### 10.5 Progressive context interaction

Desktop wide layout:

```text
[Previous — right]  <-  [Fixed query — center]  ->  [Following — left]
```

- Show only the next level on each side, not a fully expanded horizontal tree.
- Each level is a single-select list of buttons.
- A boundary is an explicit selectable button.
- Selecting either side requests both updated option lists and the filtered occurrence total.
- Breadcrumbs allow one-level-at-a-time reversal.
- Full context groups and ayah occurrences appear below the explorer.

Mobile through 767px:

- Use a stacked stepper: path summary, previous, following, then results.
- Do not render a horizontal tree.

Use the current breakpoint contract for medium layout and start the wide layout at 1080px.

### 10.6 Accessibility details

- Routed tabs use the shared `QdTabs`/`QdTab` contract and support arrow-key movement.
- Branches are button lists, not an ARIA tree.
- Use `aria-current` for the selected path item. Use `aria-expanded` only when an actual disclosure exists; do not mix it casually with `aria-pressed`.
- `aria-busy` covers the updating region.
- One live region announces the final updated result count once, not every node count.
- Loading more preserves focus in the same side. A branch selection either retains focus on the selected item or moves to the first item of the new level according to one documented rule.
- Respect RTL, logical CSS properties, and `prefers-reduced-motion`.
- Every route has explicit loading, refreshing, empty, invalid-query, ambiguous-query, error, stale-build, and unavailable-index states.

### 10.7 Generated API contracts

During implementation:

```bash
Backend/scripts/export-swagger
cd Frontend/quran-dashboard-ui
npm run generate:api
```

- Review the intended `openapi/swagger.json` and generated-model diff.
- Never edit generated models manually.
- Re-run both generators and compare before/after hashes to prove deterministic output while the intended files are still uncommitted.
- Run `Backend/scripts/check-api-contract` only at the final Git-authorized gate after generated outputs are part of the staged/committed baseline; otherwise its clean-diff check will correctly fail on the intended uncommitted contract change.

## 11. Implementation phases

### Phase 0 — Preflight and authority checkpoints

Tasks:

- Confirm the branch is not `main` and inspect overlapping worktree changes.
- Record the current migration head and the named local database state.
- Lock the table, key, route, DTO, source-fingerprint, and public-GET contracts in this plan before production edits.
- Confirm which disposable/local database may later receive the migration and full build.

Exit gate:

- No open product decision.
- Explicit authority checkpoints are identified for migration generation, local migration application/build, canonical dump replacement, retained-test additions if any, and Git/release actions.

### Phase 1 — Domain, source state, and schema

Ownership:

- PhraseSearch Domain entities and enums.
- DbSets, EF configurations, indexes, composite keys, and FK delete behavior.
- Phrase source-state coordinator contract.
- Generated migration.

Tasks:

- Implement the executable model in sections 6 and 7.
- Generate the migration with `./scripts/add-mig` only.
- Inspect migration and snapshot for unintended changes.
- Build and run pending-model and migration lanes without applying the migration to a user database.

Code-complete gate:

- Backend build passes.
- Pending-model gate passes.
- Migration lane passes.
- No database was mutated without authority.

Operational checkpoint before runtime phases:

1. Receive explicit authority for one named local/disposable database.
2. Prove backup/rebuildability.
3. Apply `Backend/scripts/update-db`.
4. Only then run Phase 2–5 data and runtime gates.

Without that authority, implementation may remain code-complete but runtime acceptance is explicitly blocked.

### Phase 2 — Complete atomic builder

Ownership:

- Application command/handler/result.
- Infrastructure snapshot, staging, COPY, exact windows, similarity edges, validation, activation, cleanup, and report.
- DataImporter verb, DI, paths, and operational READMEs.
- Foundation/rebuild-words source-state integration.

Tasks:

- Extract and reuse the existing tashkil identity contract without behavior drift.
- Implement exact variants/occurrences for both modes and every source length.
- Implement length-4+ exact similarity edges and anchor statistics.
- Implement source approval, readiness, hard checks, report, and atomic activation.
- Implement the lock-protected first-run source-state initialization for an existing database with already-valid display identities.
- Implement compatible previous-generation retention and stale invalidation.
- Run two complete builds on the authorized clone and verify identical fingerprint/totals with distinct build IDs and only the newest active.

Runtime exit gate:

- The first report reproduces the approved baseline.
- Both readiness flags are true.
- No marker or cross-ayah window exists.
- A cancellation during staging on a disposable clone leaves the old active build intact.
- A source revision/fingerprint change before activation on a disposable clone prevents activation.
- No permanent production failpoint or unapproved retained test is added for those rehearsals.

### Phase 3 — Exact repetition read slice

Ownership:

- Capabilities, repetitions, and occurrence contracts/handlers/readers/controllers.
- Smoke route catalog and OpenAPI output.

Tasks:

- Implement active-build-scoped pagination, sorting, validation, and full-ayah hydration.
- Return 503 for unavailable/stale state and 409 for stale resource refs.
- Add all routes to the existing smoke route catalog.

Exit gate:

- The default result is simple, length 2, most occurrences first.
- Current maximum repeated lengths are reported as 24 simple and 23 tashkil.
- Opening a variant shows every occurrence with a full ayah and word-ID highlights.

### Phase 4 — Query resolution and paired context

Ownership:

- Phrase-specific normalizer and parser.
- Resolution, branches, context groups, and context-occurrence read slices.

Tasks:

- Resolve hamza-tolerant raw input to exact source-token candidates.
- Handle `resolved`, `ambiguous`, and `unresolved` states.
- Aggregate exact previous/following branches from one paired occurrence population.
- Implement independent cursors, side boundary counts, full-context groups, and paged ayahs.
- Redact query/reference values from application logs and avoid raw-query cache keys.

Exit gate:

- An input without the hamza can resolve the correct hamza-bearing exact Quran token while preserving original display.
- `كان الله` never resolves to `وكان الله`.
- `يا أيها` can resolve to the one source token.
- Boundary and duplicate-in-one-ayah cases are correct.
- Every branch response satisfies the count invariant.
- One-sided boundary counts are never labelled as complete-context counts.

### Phase 5 — Similarity read slice

Ownership:

- Manual same-length scan.
- Global anchor-stat list and direct-neighbor readers/controllers.

Tasks:

- Use one exact Hamming scoring basis for manual and global results.
- Read length-4+ global neighbors from stored edges.
- Scan same-length variants for manual queries and length 2/3.
- Explain matched and differing positions.
- Capture `EXPLAIN (ANALYZE, BUFFERS)` for the worst length-2/3 manual scan, global anchor page, and anchor-neighbor lookup.

Exit gate:

- Threshold totals reproduce section 4.
- Nine tokens at 50% require five matches.
- No length-2/3 edge rows exist.
- No self, cross-mode, cross-length, or cross-build edge exists.
- Neighbor and anchor queries use the intended indexes without an unbounded corpus scan.

### Phase 6 — Angular shell and repetitions

Ownership:

- Routes, navigation, Words Hub, shell, tabs, repetitions facade/API/page/components, and shared phrase ayah renderer.

Tasks:

- Add lazy child routes and build-aware URL state.
- Bootstrap capabilities.
- Add filters, sort, pagination, repetition list, and occurrence details.
- Implement every loading/empty/error/unavailable state.

Exit gate:

- Deep links, back/forward navigation, and route restore work for normal-length state.
- Desktop, mobile, and keyboard inspection pass.
- The client does no Arabic normalization or string-based highlight matching.

### Phase 7 — Angular context and similarity

Ownership:

- Query-resolution UI, context/similarity facades/APIs/pages/components, URL-sync files, and context selection store.

Tasks:

- Add explicit submit and exact candidate chips.
- Build the centered paired progressive explorer and independent pagination surfaces.
- Add full context groups and occurrence lists.
- Add manual/global similarity modes and linked percentage/difference controls.
- Implement build-change reset, session-only long-state notice, legends, and focus/live-region rules.

Exit gate:

- A branch selection updates both sides and results from one filter state.
- Side boundary and complete-context counts cannot be confused.
- Mobile uses the stacked interaction without horizontal overflow.
- Routed tabs, focus restoration, live announcement, and keyboard navigation pass.

### Phase 8 — Integrated verification and actual sizing

Tasks:

- Regenerate and review OpenAPI/client contracts.
- Run the pre-dump gates in section 12 sequentially. Record `canonical-data` as deliberately blocked until the Phase 9 dump-regeneration authority and do not run it against the known-stale manifest.
- Build the full phrase generation on the authorized local clone.
- Measure actual table/index size, temporary double-generation space, WAL, build time, and peak memory.
- Measure cold/warm request plans and latency for default lists, worst context, manual similarity, global anchors, and neighbor lookup.
- Run desktop and mobile browser walkthroughs.
- Inspect logs for query/reference redaction.
- Rehearse compatible pointer rollback on the disposable clone.

Exit gate:

- All hard checks and authorized pre-dump gates pass; the deferred canonical-data gate is not claimed as passed.
- No regression is observed in existing Words or Mushaf routes.
- No misleading partial/stale result can be served.
- Actual storage and latency are reported as measurements, not guessed claims.

### Phase 9 — Canonical dump, release, and artifact lifecycle

- The feature's large derived tables are intentionally excluded from the canonical smoke dump. They are deterministic, rebuildable, and would otherwise add active/previous generations and make smoke restore disproportionately large.
- Update `Backend/scripts/create-smoke-dump` so both `pg_dump` selection and manifest table enumeration exclude `public.quran_phrase_*` data.
- Update the existing canonical-data gate minimally to assert and document that exclusion. Do not create a new test method unless separately approved.
- After the migration is applied to the canonical local source and with explicit overwrite authority, run `Backend/scripts/create-smoke-dump --yes`, then the canonical-data lane. The migrated test schema retains the migration-seeded empty state row; no phrase generation is restored.
- Smoke expectations for PhraseSearch on the canonical dump use the honest unavailable-index response, while full-data behavior is covered by the authorized local runtime walkthrough.
- Do not request a formal engineering review unless the owner explicitly asks for it.
- Before release, run `pre-pr` only after all earlier gates.
- Do not stage, commit, push, or open a PR without explicit Git authority.
- After engineering review passes, the final pre-merge commit deletes this folder and any temporary `Backend/report/feature-038-*` review evidence after repointing inbound references.

## 12. Testing Decision and verification gates

### 12.1 Testing Decision

Under `TESTING_CONSTITUTION.md`:

- Do not add a Backend test class or test method by default.
- Do not add `*.spec.ts`.
- Do not create or expand a Playwright journey without explicit owner approval.
- Add every new route to the existing `SmokeRouteCatalog.cs`; this is required route parity maintenance, not a new per-endpoint test suite.
- Minimally update an existing canonical-data gate when the intentionally changed dump exclusion contract requires it.
- Prove builder integrity through production hard checks, reports, retained lanes, and authorized disposable-clone rehearsals.

### 12.2 Backend sequence

Build once, then reuse the output; do not run Backend lanes concurrently:

```bash
dotnet build Backend/QuranDashboard.sln
Backend/scripts/check-pending-model --no-build
Backend/scripts/test-backend migration --no-build
Backend/scripts/test-backend pipeline --no-build
Backend/scripts/test-backend smoke --no-build
Backend/scripts/test-backend tier-b --no-build
```

The canonical-data lane is intentionally deferred because a new migration makes the existing
data-only dump stale. After explicit canonical-dump overwrite authority and regeneration, run:

```bash
Backend/scripts/test-backend canonical-data --no-build
```

Before release only:

```bash
Backend/scripts/test-backend pre-pr
```

### 12.3 Frontend sequence

Run each command independently from `Frontend/quran-dashboard-ui`:

```bash
npm run check:no-unit-specs
npm run typecheck:app
npm run build:verify
```

Then from repository root:

```bash
git diff --check
```

### 12.4 API contract sequence

During implementation:

```bash
Backend/scripts/export-swagger
cd Frontend/quran-dashboard-ui
npm run generate:api
```

Review the intended diff and verify a second generation is byte-stable. At the final Git-authorized clean/staged baseline only:

```bash
Backend/scripts/check-api-contract
```

### 12.5 Manual/browser matrix

| Case | Expected result |
|---|---|
| Simple, length 2, descending | Highest occurrence count first |
| Switch to tashkil | Different identity/counts; original Uthmani renderer remains correct |
| `وكان الله` | Its positions only; never treated as `كان الله` |
| Hamza written/omitted | Resolves to exact candidate(s), preserving corpus spelling |
| Ambiguous folded spelling | Candidate choice is shown; no silent merge |
| `يا أيها` | Can resolve to one source token |
| Query at ayah start | Direct previous-side boundary |
| Query at ayah end | Direct following-side boundary |
| Select previous then following | Both filter the same paired occurrence set |
| Load more on one side | Other side and context page remain stable |
| One-sided terminal | Label is side boundary count, not complete-context count |
| Both boundaries fixed | `exactFullContextCount` is available |
| Nine-token 50% query | At least five exact matches |
| Length-2/3 similarity | On demand, with no stored edge |
| Long query | API accepts the bounded `q64`; UI honestly marks over-1,800-char state session-only |
| Build activation during an open page | Stale refs reset with an explicit index-changed message |
| Missing/stale index | 503 state, never a misleading empty list |
| Desktop RTL | Previous right, query center, following left |
| Mobile | Stacked flow with no horizontal tree/overflow |
| Keyboard | Routed tabs use arrows; branches and pagination remain reachable |

## 13. Operations and rollback

### 13.1 First authorized local rollout

```text
1. Name the local/disposable database and prove backup or rebuildability.
2. Apply the generated migration after explicit authority.
3. Confirm foundation data is current.
4. Run rebuild-words if either exact identity link set is missing or stale; otherwise let the builder perform the lock-protected PhraseSearch state bootstrap.
5. Run build-phrase-index, which must initialize a null PhraseSearch fingerprint before staging.
6. Require a PASS report with both readiness flags and an active build ID.
7. Start the API and verify capabilities, repetitions, context, and similarity.
8. Measure database and request behavior before any production decision.
```

### 13.2 Failure behavior

- A failure before activation leaves the old active generation untouched.
- A semantic source change invalidates incompatible generations; the API becomes unavailable until repair-forward rather than serving stale data.
- Migration down is not an operational rollback.
- Normal rollback is one source-fenced transaction. It revalidates source fingerprint, format version, and both readiness flags; changes the current Active build to Superseded; changes the compatible previous build to Active; swaps active/previous pointers in unique-index-safe order; and commits the statuses and pointers atomically.
- If no compatible previous build exists, repair-forward by building a new generation.

### 13.3 Production safety

- Migration application, a long production build, and large-generation cleanup each require separate operational authority.
- Disk preflight must cover active generation, staged generation, indexes, WAL, and a safety margin.
- Never delete the active generation to make room for its replacement.
- After activation, check `/api/health`, capabilities, representative exact reads, context, and similarity.

## 14. Definition of done

The feature is complete only when all of the following are true:

- Schema, source coordination, full builder, all read slices, and all three Angular routes implement this plan.
- A complete generation is atomic, source-compatible, ready for exact and similarity reads, and rollback-capable.
- The first builder independently reproduces the approved planning baseline or stops for source approval.
- No repetition, context, or similarity operation crosses an ayah.
- Exact identity remains separate from hamza-tolerant input resolution.
- Manual and global similarity use the same exact scoring basis.
- Context paths use exact identities, preserve before/after pairing, and satisfy the count invariant.
- One-sided boundary counts and complete-context counts are distinct in contracts and UI copy.
- Every occurrence renders a full original Uthmani ayah with server-provided word-ID roles.
- Loading, empty, invalid, ambiguous, error, stale-build, unavailable, pagination, RTL, mobile, keyboard, and reduced-motion behavior are complete.
- Actual build/storage/query measurements are recorded.
- Every authorized gate passes, and every unrun gate is reported honestly.
- The canonical dump excludes phrase-derived data by explicit tested contract.
- No commit, push, formal review, PR, or deployment is reported unless it was explicitly requested and actually performed.

## 15. Remaining authorities, not design decisions

No product decision or storage measurement remains open. Implementation still stops at the relevant boundary for:

1. Permission to generate the EF migration.
2. Separate permission to apply it to one named local/disposable database and run the full builder.
3. Permission to overwrite the canonical smoke dump after the schema change.
4. Permission for any new retained automated test outside the allowed existing-gate maintenance.
5. Git staging/commit/push/PR authority.
6. Formal review and production authority.
