# Tasks: Word Simple I‘rab Foundation

**Input**: Design documents from `specs/005-word-simple-i3rab-foundation/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: **INCLUDED.** `plan.md` defines an xUnit + Testcontainers suite, the Backend has a strong test
culture (Feature 004), and these tests are the cheaper implementer's safety harness. Write each test
**before** the code it covers and confirm it FAILS first.

**Branch**: `005-word-simple-i3rab-foundation` · **Solution root for `dotnet` commands**: `Backend/`

> **AUTHORITATIVE CONTENT SOURCE (read before coding the catalogue):**
> `Backend/report/feature-005-word-simple-i3rab-foundation/segment-pattern-rule-coverage-report.md`
> §3.4 lists all **142** `(segment signature → exact Arabic label)` rows and §4 the **67** families.
> **Transcribe labels verbatim from there — do not invent or translate any Arabic.**

> **HARD RULES for every task (from spec FR-020..FR-024):** write **only** the four `i3rab_*` columns on
> `quran_word_morphology_segments` and rows in `quran_i3rab_rules`. Never modify any other morphology
> column, `quran_words`, the Uthmani/QPC text, or the `quran_pos_tags` seed. Never insert/delete segment
> rows (count stays **128,219**). Never set a form for the **208** NULL-`form_arabic_normalized` rows.

## Format: `[ID] [P?] [Story?] Description`

- **[P]** = can run in parallel (different file, no dependency on an incomplete task).
- **[US#]** = the user story this task serves (story phases only).

## Path conventions (existing Backend Clean Architecture solution — no new project)

- Domain: `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/`
- Abstractions: `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/Irab/`
- Application: `Backend/application/QuranDashboard.Application/Quran/Words/GenerateI3rab/`
- Infrastructure: `Backend/infrastructure/QuranDashboard.Infrastructure/...`
- Console host: `Backend/tools/QuranDashboard.DataImporter/Program.cs`
- Tests: `Backend/tests/QuranDashboard.Tests/Quran/WordsSimpleI3rab/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Folders and references so the feature compiles.

- [X] T001 Create the feature folders (empty) per `plan.md` Project Structure: `Domain/Quran/Words/Morphology/Irab/`, `Application.Abstractions/Quran/Words/Morphology/Irab/`, `Application/Quran/Words/GenerateI3rab/`, `Infrastructure/Files/Quran/Morphology/Irab/`, `Infrastructure/Persistence/Repositories/Quran/Irab/`, `Infrastructure/Persistence/Configurations/Quran/Words/Morphology/Irab/`, `Infrastructure/Reports/Quran/Irab/`, and `tests/QuranDashboard.Tests/Quran/WordsSimpleI3rab/`.
- [X] T002 [P] Verify `Backend/tests/QuranDashboard.Tests` already references xUnit `2.9.3`, FluentAssertions `8.2.0`, Testcontainers.PostgreSql `4.4.0` (it does, from Feature 004) — no package changes needed; confirm `dotnet build Backend` is green before starting.

**Checkpoint**: solution builds; feature folders exist.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain types, schema, abstractions, DI, and the verb skeleton that ALL stories need.

**⚠️ CRITICAL**: No user story can run until this phase is complete.

- [X] T003 [P] Create `I3rabStatus` enum (`Approved`, `NeedsReview`, `Unsupported`) in `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/Irab/I3rabStatus.cs`. Add a helper to map enum→stored string (`approved`/`needs_review`/`unsupported`).
- [X] T004 [P] Create the `QuranI3rabRule` entity in `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/Irab/QuranI3rabRule.cs` with properties per `data-model.md` §5: `int Id`, `string SignatureKey`, `string RuleFamily`, `string I3rabArabic`, `string DefaultStatus`, `string? Description`, `short SortOrder`.
- [X] T005 Add four properties + one nav to the EXISTING `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/WordMorphologySegment.cs`: `string? I3rabArabic`, `int? I3rabRuleId`, `string? I3rabStatus`, `string? I3rabReviewReason`, `QuranI3rabRule? I3rabRule`. (Do not change existing properties.)
- [X] T006 [P] Create the Abstractions DTOs in `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/Irab/` per `contracts/i3rab-abstractions.md`: `I3rabSegmentInput.cs`, `I3rabRuleSeedRow.cs`, `I3rabSegmentLabel.cs`, `I3rabCheckResult.cs`, `I3rabGenerationResult.cs`.
- [X] T007 [P] Create the Abstractions interfaces in the same folder per `contracts/i3rab-abstractions.md`: `II3rabGenerationSource.cs`, `II3rabRuleCatalog.cs`, `II3rabAssembler.cs`, `II3rabGenerationWriter.cs`, `II3rabGenerationReportWriter.cs`.
- [X] T008 [P] Create `I3rabInvariants.cs` (static ids + expected counts) in the same Abstractions folder: `ExpectedSegmentCount=128219`, `ExpectedWordCount=77432`, `ExpectedRuleCount=142`, `ExpectedFamilyCount=67`, `ExpectedNullFormCount=208`, and the 9 hard-check id constants + 5 warning ids from `contracts/validation-report.schema.md`.
- [X] T009 Create `QuranI3rabRuleConfiguration.cs` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Morphology/Irab/`: table `quran_i3rab_rules`, `id` identity PK, **UNIQUE** `signature_key`, index on `rule_family`, `default_status` CHECK ∈ {approved,needs_review,unsupported}, all columns per `data-model.md` §2.
- [X] T010 Modify the EXISTING `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Morphology/WordMorphologySegmentConfiguration.cs`: map the 4 new columns — `i3rab_arabic text NULL`, `i3rab_rule_id int NULL` (FK → `quran_i3rab_rules.id`, `ON DELETE RESTRICT`, btree index), `i3rab_status text NOT NULL` with CHECK ∈ {approved,needs_review,unsupported}, `i3rab_review_reason text NULL`. Do not change existing mappings.
- [X] T011 Register `QuranI3rabRule` in the EF `DbContext` (DbSet + apply configuration), then generate the schema-only migration with EF tooling: `dotnet ef migrations add AddWordSimpleI3rab` from the Infrastructure project. The migration MUST add the new table + the 4 columns; `i3rab_status` is added `NOT NULL DEFAULT 'unsupported'` (transient backfill default, research R8). **Do not hand-write the migration; do not add `HasData`; do not run `database update` (that happens at deploy/run time).**
- [X] T012 Add the `generate-i3rab` verb skeleton to the EXISTING `Backend/tools/QuranDashboard.DataImporter/Program.cs`: parse `generate-i3rab [--report-out <path>] [--force]` (mirror `import-morphology` parsing), resolve `GenerateI3rabHandler` from DI, and call it. Reject unknown args with usage text. (Handler is wired in Phase 3.)

**Checkpoint**: schema + entities + abstractions + verb skeleton compile (`dotnet build Backend` green).

---

## Phase 3: User Story 1 — Every segment gets an approved label (Priority: P1) 🎯 MVP

**Goal**: Running `generate-i3rab` labels all 128,219 segments with status `approved`, each with a
non-null Arabic label and a resolvable rule id, and writes a PASS report.

**Independent Test**: run `dotnet run --project Backend/tools/QuranDashboard.DataImporter -- generate-i3rab`
on a DB with morphology loaded; then `SELECT i3rab_status, count(*) FROM quran_word_morphology_segments GROUP BY 1;`
returns `approved | 128219`.

### Tests for User Story 1 (write first, confirm they FAIL)

- [ ] T013 [P] [US1] `SegmentSignatureBuilderTests.cs` in `Backend/tests/QuranDashboard.Tests/Quran/WordsSimpleI3rab/`: data-driven cases mapping known morphology inputs → expected signature keys (e.g. STEM N+GEN → `STEM:N:GEN`; STEM V PERF ACT 3MS → `STEM:V:PERF:ACT:3MS`; SUFFIX PRON 3MP → `SUFFIX:PRON:3MP`; STEM PN Allah-lemma GEN → `STEM:PN:ALLAH:GEN`; STEM N GEN 1S → `STEM:N:GEN:1S`). Pure unit, no DB.
- [ ] T014 [P] [US1] `I3rabAssemblerTests.cs` (same folder): given a small in-memory catalogue + sample `I3rabSegmentInput`s, assert each output `I3rabSegmentLabel` has the looked-up label, the right `signature_key`, and `status = approved`. Pure unit, no DB.
- [ ] T015 [US1] `I3rabGenerationTests.cs` (same folder, Testcontainers `postgres:16-alpine`): **apply all EF migrations (incl. `AddWordSimpleI3rab`) to the container**, seed a small but representative morphology fixture, run the generator, assert every segment has `i3rab_status='approved'`, non-null `i3rab_arabic`, resolvable `i3rab_rule_id`; and the report verdict is PASS. Use the source-safe fixture pattern from Feature 004's `MorphologyImportTestFixture` (it already applies migrations).

### Implementation for User Story 1

- [ ] T016 [P] [US1] Implement `SegmentSignatureBuilder.cs` in `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/Morphology/Irab/`: build `kind:pos[:ALLAH][:case][:tense:voice:person][:1S]` from `kind`, `pos`, `case_feature`, verb tense/voice, person token (from `features_raw`), and the Allah-lemma flag — exactly the §3.4 signature format (research R5). Make it FR-013..FR-016 complete (case, tense, voice, person).
- [ ] T017 [US1] Implement `I3rabRuleCatalogSeed.cs` (implements `II3rabRuleCatalog`) in the same folder: hardcode **all 142** rows `{ SignatureKey, I3rabArabic, RuleFamily, DefaultStatus='approved', SortOrder }`. **Column mapping (see data-model.md §2):** `SignatureKey` = coverage report §3.4 column 1 (e.g. `STEM:N:GEN`, `SUFFIX:PRON:3MP`); `I3rabArabic` = §3.4 "i‘rab (Arabic)" column (verbatim); `RuleFamily` = the **§4 family (67 distinct)** the signature rolls up to (drop person/number/gender, e.g. `SUFFIX:PRON:3MP`/`SUFFIX:PRON:2MP` → `PRON.SUF`), **not** §3.4's finer "rule key". Provide `Rows()` and `TryGet(signatureKey, out row)`. **This is the single source of Arabic labels — do not invent or translate Arabic.**
- [ ] T018 [US1] Implement `I3rabAssembler.cs` (implements `II3rabAssembler`) in the same folder: for each `I3rabSegmentInput`, call `SegmentSignatureBuilder` → `II3rabRuleCatalog.TryGet`. On hit → `(I3rabArabic=row.I3rabArabic, SignatureKey, Status='approved', ReviewReason=null)`. On miss → `(I3rabArabic=null, Status='unsupported', ReviewReason='no catalogue match for signature <key>')` (expected count 0 in v1).
- [ ] T019 [US1] Implement `II3rabGenerationSource` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Irab/EfI3rabGenerationSource.cs`: `LoadSegments()` reads all segments joined to their word (for `case_feature`, verb tense/voice) and lemma (Allah flag via `lemma_buckwalter` match `{ll~ah}`, research R7), populating `I3rabSegmentInput` incl. `FormIsNull`. Read-only queries only.
- [ ] T020 [US1] Implement `EfI3rabGenerationWriter.cs` + `I3rabSql.cs` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Irab/`: in ONE transaction — (a) upsert the 142 catalogue rows by `signature_key`; (b) binary `COPY` the per-segment tuples into a TEMP table; (c) `UPDATE quran_word_morphology_segments t SET i3rab_arabic=s.i3rab_arabic, i3rab_rule_id=(SELECT id FROM quran_i3rab_rules WHERE signature_key=s.signature_key), i3rab_status=s.status, i3rab_review_reason=s.reason FROM temp s WHERE t.id=s.segment_id`. Reuse the Npgsql binary-COPY approach from `EfBulkMorphologyWriter`. Writes ONLY the 4 columns.
- [ ] T021 [US1] Implement `I3rabValidationRunner.cs` in the same Irab repo folder: implement all **9 hard checks** + **5 warnings** from `contracts/validation-report.schema.md`. For `I3RAB-SOURCE-COLUMNS-UNCHANGED` capture a before/after snapshot/hash of the non-i3rab segment columns **plus a row-count/hash of `quran_words` and `quran_pos_tags`** (FR-023); for `I3RAB-SEGMENT-ROWCOUNT-STABLE` the segment rowcount; for `I3RAB-NULL-FORM-NOT-INVENTED` the NULL-form id set. Return `IReadOnlyList<I3rabCheckResult>`.
- [ ] T022 [US1] Implement `MarkdownJsonI3rabReportWriter.cs` in `Backend/infrastructure/QuranDashboard.Infrastructure/Reports/Quran/Irab/`: emit the JSON + Markdown report per `contracts/validation-report.schema.md` (totals, per-status coverage, per-rule/family usage, checks, warnings, verdict). Default dir `resources/report/words-simple-i3rab/` (research R3).
- [ ] T023 [US1] Implement `GenerateI3rabCommand.cs`, `GenerateI3rabResult.cs`, and `GenerateI3rabHandler.cs` in `Backend/application/QuranDashboard.Application/Quran/Words/GenerateI3rab/`: orchestrate `source.LoadSegments()` → `assembler.Assemble()` → `writer.Write(rules, labels, force)` (which runs the gate inside its transaction and commits or rolls back) → `reportWriter.Write()`. Return a `GenerateI3rabResult` with counts + verdict + report path + exit code.
- [ ] T024 [US1] Wire DI in the Infrastructure DI extension and the console host: register `II3rabGenerationSource`, `II3rabRuleCatalog`, `II3rabAssembler`, `II3rabGenerationWriter`, `II3rabGenerationReportWriter`, `GenerateI3rabHandler`; finalize `Program.cs` so `generate-i3rab` runs end-to-end. Confirm T013–T015 now PASS.

**Checkpoint**: `generate-i3rab` runs green → 128,219 `approved`, 142 rules / 67 families, report verdict PASS. **This is the deployable MVP.**

---

## Phase 4: User Story 2 — Correct, curated Arabic labels (Priority: P2)

**Goal**: The labels are the curated correct Arabic (the 21 seed corrections + لفظ الجلالة), owned by the
catalogue, never the wrong `quran_pos_tags` seed values.

**Independent Test**: query the ر, T, RES, SUR, INL, P.SUFFIX, N.GEN.1S, and لفظ الجلالة labels and confirm
they equal the FR-011 corrected values; confirm every `i3rab_rule_id` resolves.

### Tests for User Story 2

- [ ] T025 [P] [US2] `I3rabRuleCatalogSeedTests.cs` (unit, no DB): assert the seed has exactly **142** rows and **67** distinct `RuleFamily`; every `DefaultStatus = approved`; `signature_key` is unique; and all **21** FR-011 corrections are present with the exact corrected Arabic (`T→ظرف زمان`, `SUB→حرف مصدري`, `RES→أداة حصر`, `STEM:INTG→اسم استفهام`, `PREFIX:INTG→همزة استفهام`, `AMD→حرف استدراك`, `SUP→حرف زائد`, `PREV→ما الكافّة`, `INC→حرف ابتداء/استفتاح`, `EXL→حرف تفصيل`, `INT→حرف تفسير`, `EXH→حرف تحضيض`, `SUR→حرف فجاءة`, `INL→حروف مقطّعة (فواتح السور)`, `EQ→همزة التسوية`, `VOC.SUFFIX→ميم عوض عن حرف النداء`, `COM→واو المعية`, `P.SUFFIX→لام الجر`, `N.GEN.1S→اسم مجرور مضاف إلى ياء المتكلم`, `REM→حرف استئناف`, `PREFIX:IMPV→لام الأمر`), plus `STEM:PN:ALLAH:{GEN,NOM,ACC}→لفظ الجلالة {مجرور,مرفوع,منصوب}`.
- [ ] T026 [US2] Implement/confirm the `I3RAB-LABEL-REVIEW` warning in `I3rabValidationRunner.cs` enumerates the rules whose label diverges from `quran_pos_tags.arabic_label` (the 21 corrections), and that `I3RAB-RULE-USAGE` reports per-family counts. Extend `MarkdownJsonI3rabReportWriter` to render them.
- [ ] T027 [P] [US2] `I3rabLabelCorrectnessTests.cs` (Testcontainers): after a run, assert for every segment `i3rab_arabic` equals its joined rule's `i3rab_arabic`; `i3rab_rule_id` always resolves (`I3RAB-RULE-RESOLVES` = 0 dangling); and no `i3rab_arabic` equals a known-wrong seed value (e.g. no segment shows `تاء تأنيث` for a `T`).

**Checkpoint**: labels verified correct, catalogue-owned, FK-resolved.

---

## Phase 5: User Story 3 — Word-level i‘rab composed at read time (Priority: P3)

**Goal**: Word summaries derive from ordered segment labels with «، »; nothing word-level is stored.

**Independent Test**: compose بِحَمْدِكَ (2:30:20) from its ordered segment labels and confirm
`حرف جر، اسم مجرور، ضمير متصل في محل جر مضاف إليه`; confirm no `quran_word_i3rab` / `quran_word_segment_i3rab` table.

### Tests for User Story 3

- [ ] T028 [P] [US3] `I3rabWordCompositionTests.cs` (Testcontainers): after a run, run `string_agg(i3rab_arabic, '، ' ORDER BY segment_number)` for بِحَمْدِكَ (`2:30:20`) and assert it equals `حرف جر، اسم مجرور، ضمير متصل في محل جر مضاف إليه`; assert `I3RAB-WORD-DISPLAYABLE` = 77,432 (every word composes).
- [ ] T029 [P] [US3] `I3rabSchemaShapeTests.cs` (Testcontainers): query `information_schema.tables` and assert **no** table named `quran_word_i3rab` and **no** `quran_word_segment_i3rab` exists, and that `quran_i3rab_rules` exists (spec SC-009, FR-018).

**Checkpoint**: read-time composition verified; no stored word/segment i‘rab table.

---

## Phase 6: User Story 4 — Safe, repeatable generation that never harms source (Priority: P3)

**Goal**: Refuses on stale morphology and on a non-empty target without `--force`; `--force` recomputes
identically; the gate rolls back on any hard-check failure; source data is untouched.

**Independent Test**: run on empty morphology → refuses; run twice → identical; force a check failure → rollback.

### Implementation + Tests for User Story 4

- [ ] T030 [US4] Implement `I3rabCommandExecutor.cs` in `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Repositories/Quran/Irab/`: preflight — if morphology is missing/stale (segment count ≠ 128,219 or `quran_word_morphology` empty) → refuse (exit non-zero, write a refusal report, no DB writes); if i‘rab already populated and `--force` absent → refuse; with `--force`, recompute and overwrite all rows (research R9). Wire it ahead of the handler's write step.
- [ ] T031 [P] [US4] `I3rabRefusalTests.cs` (Testcontainers): (a) empty/stale morphology → refusal + 0 writes; (b) second run without `--force` → refusal; (c) second run with `--force` → succeeds and overwrites.
- [ ] T032 [P] [US4] `I3rabSourceSafetyTests.cs` (Testcontainers): snapshot the non-i3rab segment columns + the segment row count + the set of NULL-`form_arabic_normalized` ids + a row-count/hash of `quran_words` and `quran_pos_tags` BEFORE a run; after a successful run assert all are byte/row identical (`I3RAB-SOURCE-COLUMNS-UNCHANGED` incl. **`quran_words` and the `quran_pos_tags` seed unchanged** — FR-023; `I3RAB-SEGMENT-ROWCOUNT-STABLE` = 128,219; `I3RAB-NULL-FORM-NOT-INVENTED` = the 208 stay NULL with a label).
- [ ] T033 [P] [US4] `I3rabValidationFailureTests.cs` (Testcontainers): inject a forced violation for each of the 9 hard checks (e.g. a tampered label set) and assert the run ROLLS BACK (no committed changes), writes a failure report, and returns a non-zero result.
- [ ] T034 [P] [US4] `I3rabIdempotencyTests.cs` (Testcontainers): run with `--force` twice; assert the second run's committed state (all 4 columns on all 128,219 segments + the 142 catalogue rows) is identical to the first.

**Checkpoint**: generation is safe, gated, idempotent, and source-preserving.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T035 [P] Run the clean-code self-check (`.claude/skills/engineering-review/references/clean-code-guard/`) and the test-code self-check (`.claude/skills/test-guard/`) per `CLAUDE.md`; fix naming/SOLID/DRY issues in the new files.
- [ ] T036 Run `dotnet build Backend` and `dotnet test Backend/tests/QuranDashboard.Tests` — confirm all unit + Testcontainers tests are green.
- [ ] T037 Execute `quickstart.md` end-to-end against a real (or Testcontainers) DB: run `generate-i3rab`, then run the 5 verification SQL spot-checks and confirm the expected results (128,219 approved · 142/67 · لفظ الجلالة مجرور · the بِحَمْدِكَ summary · 208 NULL forms).
- [ ] T038 Confirm the report artifact at `resources/report/words-simple-i3rab/simple-i3rab-generation-report.md` shows verdict PASS, 100% approved coverage, 0 needs-review/unsupported, and the 21-correction `I3RAB-LABEL-REVIEW` note.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (P1)** → no deps.
- **Foundational (P2)** → after Setup. **Blocks all user stories.**
- **US1 (P3)** → after Foundational. The MVP; builds the whole pipeline.
- **US2, US3, US4** → after US1 (they verify/harden the pipeline US1 builds). US2/US3/US4 are independent of each other and can proceed in parallel.
- **Polish (P7)** → after all desired stories.

### Critical path (MVP)

`T001→T002` → `T003..T012` → `T013..T024` → **working `generate-i3rab`**.

### Within a story

- Write the story's tests first (confirm FAIL) → implement → confirm PASS.
- Foundational: enum/entity/DTOs/interfaces ([P]) → EF configs → migration → verb skeleton.

### Parallel opportunities

- Foundational [P]: T003, T004, T006, T007, T008 (different files). T005/T009/T010/T011/T012 touch shared/sequential files — not [P].
- US1 tests [P]: T013, T014 (T015 needs the fixture). US1 impl: T016 is [P]; T017→T018→T019→T020→T021→T022→T023→T024 are mostly sequential (shared pipeline / DI).
- US2/US3/US4 are parallel **stories**; within them the [P] tests run together.

---

## Parallel Example: Foundational

```bash
# These five create independent new files and can run together:
Task: T003 I3rabStatus enum
Task: T004 QuranI3rabRule entity
Task: T006 Abstractions DTOs
Task: T007 Abstractions interfaces
Task: T008 I3rabInvariants
```

## Parallel Example: User Story 4 tests

```bash
Task: T031 I3rabRefusalTests
Task: T032 I3rabSourceSafetyTests
Task: T033 I3rabValidationFailureTests
Task: T034 I3rabIdempotencyTests
```

---

## Implementation Strategy

### MVP first (User Story 1)

1. Phase 1 Setup → Phase 2 Foundational → Phase 3 US1.
2. **STOP and VALIDATE**: run `generate-i3rab`; confirm 128,219 approved + PASS report.
3. This is a shippable data foundation (labels exist and display).

### Incremental delivery

- US1 = engine + full coverage (MVP).
- US2 = label-correctness verification (the 21 corrections + FK).
- US3 = read-time word composition verification (no stored word table).
- US4 = safety/rebuild hardening (refusal/force/idempotency/source-preservation).
- Each story is an independently testable increment that does not break the previous.

---

## Notes

- **No commit, no `database update`, no migration is hand-written** — `dotnet ef migrations add` only (T011).
- Tests use **source-safe** fixtures (individual derived labels, never assembled ayah text) — Quranic data safety.
- Every Arabic label comes from the catalogue seed (T017), transcribed verbatim from coverage report §3.4 — the cheaper model must **not** invent or translate Arabic.
- After T024 the feature is functionally complete; T025–T034 prove correctness/safety; T035–T038 polish & validate.
- Total: **38 tasks** — Setup 2 · Foundational 10 · US1 12 · US2 3 · US3 2 · US4 5 · Polish 4.
