# Segment Dimension IDs Prerequisite — Implementation Plan

Feature: 017 Lexical Explorers Polish
Task type: implementation plan only
Scope: backend data model + morphology importer prerequisite only
Branch: `017-lexical-explorers-polish`
Date: 2026-06-27

Inputs:
- `docs/feature-017-lexical-explorers-polish/lemma-details-matching-segment-pos-report.md`
- `docs/feature-017-lexical-explorers-polish/segment-dimension-ids-feasibility-report.md`
- `docs/feature-017-lexical-explorers-polish/segment-dimension-ids-db-verification-report.md`
- current backend entity/importer/validation/test structure

---

## 1. Verdict and Scope

**READY_FOR_IMPLEMENTATION**

This plan implements only the prerequisite segment-level dimension IDs:

- Add nullable indexed `lemma_id` to `quran_word_morphology_segments`.
- Add nullable indexed `root_id` to `quran_word_morphology_segments`.
- Populate those columns during morphology import.
- Add hard validation checks and focused tests proving the imported IDs follow the approved policy.

This plan does **not** implement the Lemma Details reader fix yet. The later reader fix remains a separate task after this prerequisite is migrated, imported, and verified.

This plan does **not** add `stem_id` to segments. `segment.stem_id` is rejected because segments have no stem source and `quran_stems` has no Buckwalter bridge. Stems Explorer continues to use word-level `quran_word_morphology.stem_id`.

This plan does **not** change Quran source text, morphology source files, QUL source files, staged import packages, or corpus data. It changes only how the existing source facts are represented in the database after import.

---

## 2. Schema Changes

Migration name suggestion: `AddSegmentDimensionIds`

### Table changes

Add to `quran_word_morphology_segments`:

- `lemma_id INT NULL`
- `root_id INT NULL`

### Foreign keys

- `lemma_id -> quran_lemmas.id`
- `root_id -> quran_roots.id`
- Delete behavior: `NO ACTION` / restrict equivalent, mirroring the current head morphology dimension references on `quran_word_morphology`.

### Indexes

- `IX_quran_word_morphology_segments_lemma_id`
- `IX_quran_word_morphology_segments_root_id`

### Entity/configuration changes

- `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/WordMorphologySegment.cs`
  - Add nullable `int? LemmaId`.
  - Add nullable `int? RootId`.
  - Add optional navigation properties only if consistent with local EF patterns; current `WordMorphology` has `Root`, `Lemma`, and `Stem` navigation properties, so segment `Root` / `Lemma` navigations are acceptable.

- `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Morphology/WordMorphologySegmentConfiguration.cs`
  - Map `lemma_id` and `root_id`.
  - Add indexes with the exact names above.
  - Add `HasOne(...).WithMany().HasForeignKey(...).OnDelete(DeleteBehavior.Restrict)` for both nullable FKs.

### Migration location

- Generate through EF Core tooling under:
  - `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/`
- Do not hand-write the migration, designer, or model snapshot. Backend instructions require EF tooling for migrations.
- Do not run `dotnet ef database update` unless the implementation task explicitly asks for it.

---

## 3. Importer Population Algorithm

The importer should populate `segment.root_id` and `segment.lemma_id` from already-loaded source facts during the morphology import. It must never invent IDs and must fail clearly on unsafe ambiguity.

### `root_id`

For each segment:

1. If `segment.root_buckwalter` is null, empty, or whitespace:
   - Set `root_id = null`.
2. Otherwise resolve by `quran_roots.root_buckwalter`.
   - In implementation, this can use the in-memory `ResolvedRootDto` collection before bulk copy, because those rows are copied into `quran_roots` immediately before morphology rows.
3. If no root matches:
   - Fail import with a clear validation/report error.
4. If more than one root matches:
   - Fail import with a clear validation/report error.

The DB verification report says this resolves 100% cleanly today.

### `lemma_id`

Only `kind = 'STEM'` segments can receive `lemma_id`.

For each segment:

1. If segment kind is not `STEM`:
   - Set `lemma_id = null`, even if the source unexpectedly carries `lemma_buckwalter`.
2. If `segment.lemma_buckwalter` is null, empty, or whitespace:
   - Set `lemma_id = null`.
3. If the word has exactly one `kind = 'STEM'` segment:
   - Assign that single STEM segment `lemma_id = quran_word_morphology.lemma_id`.
   - This deliberately avoids fragile global Buckwalter matching for unresolved or ambiguous single-STEM Buckwalter cases.
   - This is valid because the single STEM segment is the word's head lemma segment.
4. If the word has more than one `kind = 'STEM'` segment:
   - Resolve each STEM segment independently.
   - Prefer direct match by `segment.lemma_buckwalter`.
   - If the segment Buckwalter matches the word head lemma Buckwalter, assign the word head `lemma_id`.
   - Otherwise resolve to `quran_lemmas` by `lemma_buckwalter`.
   - If duplicate lemma Buckwalter candidates exist:
     - Prefer the candidate whose `lemma_text` matches or safely normalizes to the segment Arabic form.
     - Otherwise use a deterministic tie-break only if the implementation documents and tests why it is safe for the observed corpus.
     - If the tie-break is unsafe, fail import with a clear report.
   - Every STEM segment with non-null `lemma_buckwalter` must end with exactly one `lemma_id`.

### Null-safe policy

- Null/empty `segment.lemma_buckwalter` -> `lemma_id = null`.
- Null/empty `segment.root_buckwalter` -> `root_id = null`.
- Never invent IDs.
- Never assign `lemma_id` to prefixes, suffixes, particles, or any non-STEM segment.
- Never add or populate `segment.stem_id`.

### Implementation shape

The current assembler creates head-level dimensions first:

- `MorphologyAssembler.Assemble(...)`
- `AlignedWordDto(..., RootId, LemmaId, StemId)`
- `ResolvedRootDto`
- `ResolvedLemmaDto`
- `ResolvedStemDto`

The safest implementation is a second in-memory segment dimension pass inside `MorphologyAssembler` after `ResolvedRoots` and `ResolvedLemmas` are known:

1. Build `rootsByBuckwalter` from `ResolvedRootDto.RootBuckwalter`.
2. Build `lemmasByBuckwalter` from `ResolvedLemmaDto.LemmaBuckwalter`.
3. For each `AlignedWordDto`, derive `AlignedSegmentDto.RootId` and `AlignedSegmentDto.LemmaId`.
4. Return `MorphologySourceData` whose segment DTOs already contain the nullable IDs used by bulk copy.

This keeps DB writes simple and avoids read-after-copy queries during binary import.

---

## 4. Where to Implement

| File/class | Expected change | Risk | Test coverage needed |
| --- | --- | --- | --- |
| `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/WordMorphologySegment.cs` | Add nullable `LemmaId` and `RootId`; optionally add `QuranLemma? Lemma` and `QuranRoot? Root` navigations. | Low. Entity shape change only. | Model/migration shape assertions if the project has them; import tests should prove values materialize. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Morphology/WordMorphologySegmentConfiguration.cs` | Map `lemma_id`/`root_id`, add indexes, add restrict/no-action FKs to `QuranLemma` and `QuranRoot`. | Medium. FK delete behavior and index names must match migration expectations. | Migration SQL/model snapshot review; EF model tests if available. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/` | Generate `AddSegmentDimensionIds` migration and snapshot update using EF tooling. | Medium. Generated migration must not backfill incorrectly or add `stem_id`. | Build plus migration inspection; optional migration SQL review. |
| `Backend/application/QuranDashboard.Application.Abstractions/Quran/DataPipelines/Words/MorphologyImporting/MorphologySourceData.cs` | Extend `AlignedSegmentDto` with nullable `RootId` and `LemmaId`. | Medium. Record constructor changes touch tests and assembler call sites. | Assembler unit tests for single-STEM, multi-STEM, non-STEM, nulls, and duplicates. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyAssembler.cs` | Add segment dimension resolver after `ResolvedRoots`/`ResolvedLemmas` are available; preserve current head-level dimension behavior. | High. This is the core correctness logic and must avoid global Buckwalter pitfalls. | Focused unit tests for every population policy and failure mode. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyBulkCopier.cs` | Add `root_id` and `lemma_id` to `CopySegmentsAsync` column list and binary writes, in exact column order. | High. Binary COPY column order mistakes can corrupt/import-fail the run. | Import integration test proving rows persist with expected IDs; hard check coverage. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologySql.cs` | Add SQL constants for `SEG-*` validation checks. | Medium. Checks must match the approved policy and avoid naive all-case Buckwalter equality. | Validation tests and real import checks. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyValidationRunner.cs` | Add segment dimension hard checks to `RunAllHardChecksAsync`. | Medium. False failures are likely if checks are too strict. | Tests for failing unresolved/ambiguous cases and passing happy path. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyImportReportBuilder.cs` | Include any new warning/error details if the resolver reports ambiguity/unresolved segment dimensions; totals may not need new fields unless useful. | Low to Medium. Report should stay concise but actionable. | Failure-result tests should assert clear `SEG-*` messages. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/DataPipelines/Words/MorphologyImporting/MarkdownJsonMorphologyReportWriter.cs` | Ensure new checks are emitted in JSON/Markdown reports through existing check list plumbing. | Low if reports already render all checks generically. | Existing report tests or a smoke assertion that new check IDs appear. |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyAssemblerTests.cs` | Add direct algorithm tests for segment `lemma_id`/`root_id` population and failure paths. | Medium. Fixtures must use source-safe synthetic morphology. | Required unit coverage for the policy. |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyDimensionTests.cs` | Extend dimension behavior tests to cover segment-level dimensions. | Medium. Must preserve existing word-level dimension semantics. | Assertions for resolved IDs and no `stem_id`. |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTests.cs` | Add persisted DB assertions for segment IDs, 128,219 real segment count in real-import coverage, and no segment `stem_id`. | Medium. Integration tests may be slower but are the best coverage for COPY + FKs. | Testcontainers/local integration coverage. |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyValidationFailureTests.cs` | Add failure tests for unresolved multi-STEM lemma and duplicate tie-break failures where unsafe. | Medium. Needs carefully controlled synthetic source. | Failure result includes clear check/error ID. |
| `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTestFixture.cs` | Extend synthetic seed helpers to create multi-STEM, duplicate Buckwalter, null source, and root resolution cases. | Medium. Fixture changes can affect many import tests. | Keep additions additive and explicit. |

---

## 5. Validation Hard Checks

Add hard checks after segment copy and before transaction commit.

### Required check IDs

- `SEG-LEMMA-ID-STEM-ONLY`
  - `lemma_id` is non-null only on `kind = 'STEM'` segments.

- `SEG-LEMMA-ID-REQUIRED-FOR-STEM`
  - Every STEM segment with non-null/non-empty `lemma_buckwalter` has non-null `lemma_id`.

- `SEG-LEMMA-ID-SINGLE-STEM-HEAD-CONSISTENT`
  - For words with exactly one STEM segment, that STEM segment's `lemma_id` equals `quran_word_morphology.lemma_id`.

- `SEG-LEMMA-ID-MULTI-STEM-RESOLVES`
  - For words with more than one STEM segment, every STEM segment with non-null/non-empty `lemma_buckwalter` resolves to exactly one `lemma_id`.
  - This check should validate the imported result and any resolver-produced ambiguity details, not require all single-STEM rows to match raw Buckwalter.

- `SEG-LEMMA-ID-NO-FANOUT`
  - One segment row maps to at most one lemma. In the physical schema this should always be true because `lemma_id` is scalar; the check should also guard resolver diagnostics so ambiguous candidates never silently pick multiple meanings.

- `SEG-ROOT-ID-RESOLVES`
  - Every segment with non-null/non-empty `root_buckwalter` has non-null `root_id`.

- `SEG-ROOT-ID-CONSISTENT`
  - Referenced root row has matching `root_buckwalter`.

- `SEG-DIM-NULL-SAFE`
  - Null/empty source values remain null IDs:
    - null/empty `root_buckwalter` -> `root_id IS NULL`
    - null/empty `lemma_buckwalter` -> `lemma_id IS NULL`
    - non-STEM rows -> `lemma_id IS NULL`

- `SEG-STEM-ID-ABSENT`
  - `quran_word_morphology_segments` has no `stem_id` column.

- Existing `MORPH-SOURCE-UNCHANGED` remains green.

### Important check boundary

Do **not** add a naive consistency check requiring every `segment.lemma_buckwalter` to equal the referenced `quran_lemmas.lemma_buckwalter` in all cases.

The approved policy intentionally assigns single-STEM words from the already-resolved word head `lemma_id` to avoid unresolved/ambiguous global Buckwalter matching. A strict all-row Buckwalter equality check would reject the approved policy and regress the point of this prerequisite.

For multi-STEM words, Buckwalter-based consistency is appropriate because each STEM segment is independently resolved; even there, duplicate Buckwalter candidates must go through the documented safe tie-break or fail import.

---

## 6. Test Plan

### Migration/model shape tests

If the project has EF model/migration shape tests, add or extend them to prove:

- `quran_word_morphology_segments.lemma_id` exists and is nullable.
- `quran_word_morphology_segments.root_id` exists and is nullable.
- FK targets are `quran_lemmas.id` and `quran_roots.id`.
- Delete behavior is restrict/no-action equivalent.
- Indexes are named:
  - `IX_quran_word_morphology_segments_lemma_id`
  - `IX_quran_word_morphology_segments_root_id`
- No `quran_word_morphology_segments.stem_id` column exists.

### Importer unit tests

Add focused tests for:

- Single-STEM word assigns the segment `lemma_id` from the word head `lemma_id`.
- Multi-STEM word assigns each STEM segment's own `lemma_id`.
- Non-STEM segment keeps `lemma_id = null`.
- `root_id` resolves from `root_buckwalter`.
- Null/empty `lemma_buckwalter` remains `lemma_id = null`.
- Null/empty `root_buckwalter` remains `root_id = null`.
- Duplicate lemma Buckwalter tie-break behavior:
  - safe Arabic form / normalized form match chooses the documented candidate.
  - unsafe ambiguity fails import clearly.
- Unresolved multi-STEM lemma fails clearly.
- Unresolved or ambiguous root fails import clearly, even though the live DB currently resolves roots 100%.

### Real import / Testcontainers tests

Add or extend real import coverage to prove:

- All 128,219 segments import.
- All STEM segments with non-null/non-empty `lemma_buckwalter` have non-null `lemma_id`.
- `root_id` resolves for all non-null/non-empty `root_buckwalter`.
- No `stem_id` column exists on `quran_word_morphology_segments`.
- DB report expectations still hold after import:
  - bug surface remains multi-STEM words, not no-STEM fallback.
  - every readable word has a STEM segment.
  - `head_pos` equals the first STEM segment POS.
  - the newly populated `segment.lemma_id` can identify the 272 affected occurrences across the 5 lemmas without relying on head POS.

### Regression tests

- Existing morphology import tests still pass.
- Existing morphology refusal/force behavior still passes.
- Existing morphology render/provenance checks still pass.
- Existing simple i'rab generation still passes after reseed.
- Existing Lexical Explorer read tests are expected to remain unchanged until the later Lemma Details reader task.

---

## 7. Reset/Reseed Sequence

Use this sequence after implementation, not during planning.

1. Generate the migration with EF tooling:
   - suggested migration name: `AddSegmentDimensionIds`
2. Apply the migration to the local dev DB only when the implementation task authorizes DB writes.
3. Reset/drop the local DB if appropriate for a clean import.
4. Run Quran foundation import if the reset removed `quran_words` and related foundation rows:
   - `QuranDashboard.DataImporter import-foundation --source <path> [--report-out <path>] [--force]`
5. Rebuild display words if needed:
   - `QuranDashboard.DataImporter rebuild-words [--report-out <path>] [--force]`
6. Run morphology import with new segment IDs:
   - `QuranDashboard.DataImporter import-morphology [--source <path>] [--report-out <path>] [--force]`
7. Run simple i'rab generation after morphology:
   - `QuranDashboard.DataImporter generate-i3rab [--report-out <path>] [--force]`
8. Run relevant backend tests:
   - build
   - morphology import tests
   - morphology validation tests
   - simple i'rab generation tests
   - affected read tests only after the later Lemma Details reader change
9. Flush API/cache/restart if doing manual smoke testing, because cached reader values can survive a reseed.

---

## 8. Explicit Non-Scope

- No Lemma Details reader changes in this prerequisite.
- No `EfLemmasReader.LoadWholeSummaryAsync` changes in this prerequisite.
- No `EfLemmasReader.GetLemmaAyahMatchesAsync` changes in this prerequisite.
- No frontend changes.
- No Stems Explorer changes.
- No `segment.stem_id`.
- No POS label changes.
- No simplified i'rab label changes.
- No Quran source text changes.
- No morphology source corpus mutation.
- No QUL source mutation.
- No broad refactor.
- No DB writes during planning.
- No commits during planning.

---

## 9. Risks and Mitigations

| Risk | Why it matters | Mitigation |
| --- | --- | --- |
| Duplicate `lemma_buckwalter` values | The feasibility report identifies duplicate lemma Buckwalter keys, so global matching can be ambiguous. | Single-STEM words use head `lemma_id`; multi-STEM duplicates must use safe Arabic form matching or fail. |
| Unresolved `lemma_buckwalter` values | Some Buckwalter values do not resolve globally. | Single-STEM rows bypass global matching; unresolved multi-STEM rows fail clearly. |
| Multi-STEM ambiguity | The live bug is caused by multi-STEM words where the head lemma belongs to a different STEM than the first STEM used for `head_pos`. | Resolve each STEM independently and require every lemma-bearing STEM segment to have exactly one `lemma_id`. |
| Hard checks too strict | Naive Buckwalter equality would fail valid single-STEM assignments. | Split checks by single-STEM and multi-STEM policy; document why single-STEM uses head `lemma_id`. |
| Binary COPY column order mistakes | `CopySegmentsAsync` uses positional binary import; a column/write mismatch can fail or corrupt import shape. | Update COPY column list and writes together; add persisted import assertions for known segment IDs. |
| Migration/backfill risk | Existing environments may have old segment rows with null new IDs until morphology is reseeded or backfilled. | Keep columns nullable; document reseed path. If shared DB needs backfill, implement as a separate authorized data operation. |
| Cache invalidation | Cached lexical reader responses may continue showing old head-POS-derived behavior after reseed and later reader fix. | Restart API/flush cache during smoke testing and after deploy. |
| Downstream `generate-i3rab` dependency | Simple i'rab generation reads morphology segments after morphology import. | Run `generate-i3rab --force` after morphology reseed and include its tests in verification. |
| Root future-proofing scope creep | `root_id` is not required for the immediate Lemma Details bug. | Include only because DB verification proves 100% clean resolution and it avoids a second migration; do not add reader changes here. |

---

## 10. Final Implementation Phases

### Phase 1 — Schema/entity/config/migration

Scope:
- Add `LemmaId` and `RootId` to `WordMorphologySegment`.
- Map columns, FKs, delete behavior, and indexes in `WordMorphologySegmentConfiguration`.
- Generate EF migration `AddSegmentDimensionIds`.

Acceptance criteria:
- Migration adds only `lemma_id` and `root_id` to `quran_word_morphology_segments`.
- Migration adds the two requested indexes.
- Migration adds the two requested FKs.
- Migration does not add `stem_id`.
- Build succeeds.

Tests/checks to run:
- Backend build.
- EF migration/model shape checks if available.
- Inspect generated migration and snapshot.

Rollback notes:
- Revert the generated migration files plus entity/config changes before applying the migration.
- If already applied locally, drop/recreate the local dev DB or apply an explicit down migration only with authorization.

### Phase 2 — Importer DTO/bulk copy population

Scope:
- Extend `AlignedSegmentDto` with nullable `RootId` and `LemmaId`.
- Add the segment dimension resolver in `MorphologyAssembler`.
- Update `CopySegmentsAsync` to include `root_id` and `lemma_id`.
- Preserve existing head-level `quran_word_morphology.root_id`, `lemma_id`, and `stem_id` behavior.

Acceptance criteria:
- Single-STEM rows use head `lemma_id`.
- Multi-STEM rows resolve each STEM independently.
- Non-STEM rows keep `lemma_id = null`.
- Root IDs resolve by `root_buckwalter`.
- Null sources remain null.
- No source file mutation.

Tests/checks to run:
- Focused assembler tests.
- Synthetic import test proving persisted segment IDs.
- Existing morphology import tests.

Rollback notes:
- Revert DTO/assembler/copier changes; schema can remain unused because columns are nullable.

### Phase 3 — Validation hard checks

Scope:
- Add `SEG-*` SQL constants to `MorphologySql`.
- Add hard checks in `MorphologyValidationRunner`.
- Ensure report output includes all new hard checks.

Acceptance criteria:
- All required `SEG-*` checks run before commit.
- Failures rollback the import and report clear errors.
- Checks match approved population policy and do not enforce naive all-row Buckwalter equality.
- `MORPH-SOURCE-UNCHANGED` remains part of the run.

Tests/checks to run:
- Validation failure tests for unresolved multi-STEM lemma and unsafe duplicate tie-break.
- Passing import tests confirming new checks are green.

Rollback notes:
- Revert validation runner and SQL constants. Nullable columns can remain until the full change is reverted.

### Phase 4 — Tests

Scope:
- Add/extend morphology assembler tests.
- Add/extend morphology dimension tests.
- Add/extend morphology import tests.
- Add validation failure tests.
- Add real import/Testcontainers coverage where the suite supports it.

Acceptance criteria:
- Tests cover the approved algorithm, not implementation details.
- Synthetic Quranic/morphology fixtures remain source-safe and clearly synthetic.
- Existing morphology and i'rab generation tests still pass.

Tests/checks to run:
- Targeted `QuranDashboard.Tests.Quran.WordsMorphology` tests.
- Relevant simple i'rab tests.
- Backend build.

Rollback notes:
- Revert only the new/changed tests if they are wrong; do not weaken production validation to satisfy over-strict tests.

### Phase 5 — Local reset/reseed and verification report

Scope:
- Apply migration locally if authorized.
- Reset/reseed local DB if appropriate.
- Run foundation/rebuild/morphology/simple-i'rab sequence as needed.
- Produce a backend verification report under `Backend/report/feature-017-lexical-explorers-polish/` if requested by the implementation task.

Acceptance criteria:
- Morphology import persists all 128,219 segments.
- All segment dimension hard checks pass.
- `SEG-STEM-ID-ABSENT` passes.
- `MORPH-SOURCE-UNCHANGED` passes.
- Simple i'rab generation passes after morphology.

Tests/checks to run:
- Import command report review.
- SQL verification for the required DB-backed checks.
- Targeted backend tests.

Rollback notes:
- For local DB only, drop/recreate or restore from backup if reseed fails.
- Do not attempt production/shared DB rollback without an explicit deployment plan.

### Phase 6 — Review/commit

Scope:
- Run clean-code self-check against the changed implementation.
- Run test-code self-check for added tests.
- Review generated migration carefully.
- Commit only after implementation and verification are complete and authorized.

Acceptance criteria:
- Scope remains limited to schema/entity/config/importer/validation/tests for segment `lemma_id` and `root_id`.
- No Lemma Details reader/frontend/Stems changes are included.
- Git diff contains no source corpus mutations.
- Commit message mentions the segment dimension IDs prerequisite.

Tests/checks to run:
- Final backend build.
- Targeted backend tests.
- Import verification if DB work was authorized.

Rollback notes:
- Revert implementation commit before deploy if verification fails.
- If migration has been applied to a shared database, use an explicit migration rollback plan.

---

## 11. Follow-Up After This Prerequisite

After this prerequisite is complete and verified, create a separate plan/task for Lemma Details Option A.

Only that later task should update:

- `EfLemmasReader.LoadWholeSummaryAsync`
- `EfLemmasReader.GetLemmaAyahMatchesAsync`

The later reader task should use `segment.lemma_id + segment.pos` instead of `head_pos` to classify Lemma Details type distribution, type filtering, and highlights.

Do not include that implementation in this prerequisite.

---

## Final Answer

Is this plan ready for implementation?

Yes. The DB-backed decision is clear, the schema/importer scope is narrow, and the implementation targets are concrete.

Any blocking unknowns?

No blocking unknowns. The only implementation-time detail to verify is whether any duplicate `lemma_buckwalter` appears inside the multi-STEM resolution set in a way that cannot be safely tied to the segment Arabic form. If unsafe, the importer must fail with a clear `SEG-LEMMA-ID-MULTI-STEM-RESOLVES`/resolver error rather than guessing.

Exact next prompt to implement Phase 1 only:

```text
Implement Phase 1 of docs/feature-017-lexical-explorers-polish/segment-dimension-ids-implementation-plan.md only: add segment lemma_id/root_id entity and EF configuration changes, generate the AddSegmentDimensionIds migration, do not update importers/tests/frontend/readers, do not update the database, then run the backend build and report the generated files.
```

Exact next prompt to implement the whole prerequisite:

```text
Implement the Segment Dimension IDs prerequisite from docs/feature-017-lexical-explorers-polish/segment-dimension-ids-implementation-plan.md through Phase 4 only: schema/entity/config/migration, importer population, validation hard checks, and tests. Do not implement the Lemma Details reader fix, do not update frontend, do not add stem_id, do not mutate source corpus files, and do not update the database unless explicitly needed for authorized verification.
```
