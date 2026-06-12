# Implementation Plan: Word Simple I‘rab Foundation

**Branch**: `005-word-simple-i3rab-foundation` | **Date**: 2026-06-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/005-word-simple-i3rab-foundation/spec.md`

> **Companion documents (long-form rationale, source of truth — read these):**
> - `docs/feature-005-word-simple-i3rab-foundation/feature-005-word-simple-i3rab-foundation-planning-report.md` (locked v1 plan)
> - `Backend/report/feature-005-word-simple-i3rab-foundation/segment-pattern-rule-coverage-report.md` (the finalized 142-signature / 67-family catalogue, exact Arabic labels, 100% approved coverage — **the authoritative label source for the catalogue seed**)
> - `Backend/report/feature-005-word-simple-i3rab-foundation/simple-i3rab-label-inventory-report.md` (superseded sibling; historical evidence)
>
> **Governance:** `Backend/.architecture/{BACKEND_STRUCTURE,CLEAN_ARCHITECTURE,API_GUIDELINES}.md`,
> `CODING_PRINCIPLES.md`, `Backend/CLAUDE.md` (EF migration policy).

## Summary

Add a **simplified Arabic i‘rab label to every morphology segment**, derived deterministically from the
completed Feature 004 morphology data. For each of the **128,219** segments (across **77,432** readable
words), compute a stable **segment signature** from its existing morphology features
(`kind:pos[:ALLAH][:case][:tense:voice][:person]`), **look that signature up** in a curated catalogue,
and store the catalogue's exact Arabic label + provenance on the segment row. The catalogue is a new
table `quran_i3rab_rules` with **142 rows** (one per signature, grouped into **67 families** via a
`rule_family` column). The labels live **inline** on `quran_word_morphology_segments` via four new
columns; **no** `quran_word_segment_i3rab` and **no** `quran_word_i3rab` table are created — word
summaries are composed at read time. In v1 every segment resolves to status `approved` (100% coverage);
`needs_review`/`unsupported` are schema-reserved.

**Technical approach:** unlike Feature 004 (source-driven from JSON), Feature 005 is **DB-to-DB** (like
Feature 003's rebuild): it reads the populated morphology, assembles labels in memory (signature → catalogue
lookup — **no Arabic-label composition in code**), then **bulk-loads via Npgsql binary `COPY` into a temp
staging table + a single `UPDATE … FROM`** of the four `i3rab_*` columns, and **seeds the 142 catalogue
rows**, all inside **one transaction**, committing only if a hard-check gate passes (else rollback). It is
exposed as a **fourth console verb, `generate-i3rab`**, on the existing `tools/QuranDashboard.DataImporter`
host (operator/CI only — never HTTP). It **never** mutates the original morphology columns, `quran_words`,
or the `quran_pos_tags` seed; it adds an i‘rab label only and **never** invents an Arabic form for the 208
NULL-form segments. The catalogue is a curated **in-code seed** (the same pattern as `PosTagSeed.cs`),
seeded idempotently — **not** EF `HasData`. **No API, UI, stored word summary, or syntactic roles.**

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0`)
**Primary Dependencies**: EF Core `10.0.8`, `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.0` (binary
`COPY` of the per-segment i‘rab tuples into a temp table, then `UPDATE … FROM` — mirrors the
`EfBulkMorphologyWriter` COPY usage), `Microsoft.Extensions.Hosting` `10.0.0` (console host + DI). No new
package. Feature parsing reuses the existing `features_raw` / `features_json` already on each segment.
**Storage**: PostgreSQL. **Extends** `quran_word_morphology_segments` with four new columns
(`i3rab_arabic`, `i3rab_rule_id`, `i3rab_status`, `i3rab_review_reason`); **adds one new table**
`quran_i3rab_rules` (142 importer-seeded rows). Reads the existing morphology tables read-only; writes
**only** the four columns + the new rules table. `quran_words`, the original morphology columns, and the
`quran_pos_tags` seed are **never** mutated.
**Testing**: xUnit `2.9.3` + FluentAssertions `8.2.0` + Testcontainers.PostgreSql `4.4.0`
(`postgres:16-alpine`) in the existing `tests/QuranDashboard.Tests` project; plus pure unit tests for the
`SegmentSignatureBuilder` and the catalogue seed (no DB).
**Target Platform**: Linux server, .NET 10 runtime
**Project Type**: Existing Backend Clean Architecture solution (7 projects incl. the `DataImporter`
console host) — **no new project**; one new verb (`generate-i3rab`) + new feature folders under
`Quran/Words/…/Irab/` and `Quran/Words/GenerateI3rab/`.
**Performance Advisory**: read ~128,219 segments + features, build signatures, look up in the 142-row
catalogue, `COPY` the tuples to a temp table + one `UPDATE … FROM`, seed 142 rules, validate — in **one
transaction**. Operator-run timing target/observation is seconds, not a user-facing path and not a hard
gate (`CommandTimeout` ~600 s).
**Constraints**: **DB-to-DB** (no source files); schema-only migration (no `HasData`; catalogue seeded by
the generator idempotently); writes **only** the four `i3rab_*` columns + `quran_i3rab_rules`; original
morphology columns / `quran_words` / `quran_pos_tags` seed **never** mutated; segment row count **stable
(128,219)** — no insert/delete/truncate of segments; the **208** NULL `form_arabic_normalized` rows stay
NULL (label only, never an invented form); **per-occurrence** grain keyed to segment id / `quran_word_id`;
runs **after** a completed morphology import and **refuses** on missing/stale morphology; refuses a
non-empty i‘rab target without `--force`; idempotent; atomic + hard-gated validation + Markdown/JSON
report; rule layer owns Arabic labels (the 21 seed corrections), `quran_pos_tags` stays a technical
dictionary; Quranic data safety (derived labels keyed by id, no ayah text, source-safe fixtures);
**no API / UI / stored word summary (`quran_word_i3rab`) / separate segment table
(`quran_word_segment_i3rab`) / syntactic roles**.
**Scale/Scope**: 1 new table (**142 rows / 67 families**) + **4** new columns; updates **128,219**
segments to `approved`; **9** hard checks + **5** warnings; repeatable operator-run generation.

*No unresolved NEEDS CLARIFICATION items — the spec (with its 2026-06-12 Clarifications), the planning
report, and the finalized coverage report are exhaustive; the two deferred items (verb name, report path)
are settled in `research.md`.*

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution (`.specify/memory/constitution.md`) is an **unratified template** (placeholders
only). In its absence the governance gates are the workspace architecture docs (same posture as Features
002/003/004). Status:

| Gate (source) | Status | Notes |
|---|---|---|
| Clean Architecture dependency direction (`CLEAN_ARCHITECTURE.md`) | ✅ Pass | Domain holds the new `QuranI3rabRule` entity + `I3rabStatus` enum (pure) and four new properties on the existing `WordMorphologySegment`. Application orchestrates `GenerateI3rab` via Application.Abstractions interfaces. Infrastructure implements the catalogue seed, the signature builder, the assembler, the bulk writer, the validation runner, and the report writer. The console host is composition only. Infra → Application.Abstractions + Domain (not Application). |
| Domain/feature foldering, no dumping folders (`BACKEND_STRUCTURE.md`) | ✅ Pass | All new types under `Quran/Words/Morphology/Irab/` (Domain, Abstractions, Infrastructure) and `Quran/Words/GenerateI3rab/` (Application). No `Enums/Models/Helpers/DTOs` dumping folders. |
| EF migration policy (`Backend/CLAUDE.md`) | ✅ Pass (deferred to implementation) | One **tooling-generated** schema-only migration (`AddWordSimpleI3rab`: new table + 4 columns + FK + CHECK + indexes). **No** hand-written migration, **no** `HasData`. The migration is created during `/implement`, only when explicitly requested; `database update` is not run by the plan. |
| API guidelines / `ApiResponse` (`API_GUIDELINES.md`) | ✅ N/A | No API/controllers/HTTP in this feature (operator/CI console verb only). |
| Quranic data safety (`PRODUCT.md`, planning report) | ✅ Pass | Derived grammatical labels keyed by identifier only; **no** ayah text stored; original morphology columns + `quran_words` + `quran_pos_tags` seed untouched; the 208 NULL forms preserved; unsupported recorded with a reason, never guessed; labels are explicitly **not** authoritative scholarly i‘rab; source-safe test fixtures. |

**Post-Phase-1 re-check:** ✅ Pass — the design adds one Domain entity + one enum + four properties, one
new table, one console verb, and feature-scoped Infrastructure; no layer boundary is crossed and no
dumping folder is introduced. No complexity deviations to track.

## Project Structure

### Documentation (this feature)

```text
specs/005-word-simple-i3rab-foundation/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output — decisions & rationale
├── data-model.md        # Phase 1 output — entities, columns, migration shape, validation rules
├── quickstart.md        # Phase 1 output — how to run & verify the generator
├── contracts/           # Phase 1 output
│   ├── cli-verb.md                  # the `generate-i3rab` console verb contract
│   ├── i3rab-abstractions.md        # Application.Abstractions interfaces & DTOs
│   └── validation-report.schema.md  # hard checks, warnings, report artifact schema
├── checklists/
│   └── requirements.md  # spec quality checklist (from /speckit-specify)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created by /speckit-plan)
```

### Source Code (repository root)

Existing Backend Clean Architecture solution. **No new project.** New files are feature-scoped under
`Quran/Words/…/Irab/` (mirroring the Feature 004 `Morphology/` layout). `(modify)` marks the few existing
files that change.

```text
Backend/
├── domain/QuranDashboard.Domain/Quran/Words/Morphology/
│   ├── WordMorphologySegment.cs                         # (modify) + I3rabArabic, I3rabRuleId, I3rabStatus, I3rabReviewReason, nav I3rabRule
│   └── Irab/
│       ├── QuranI3rabRule.cs                            # new entity (catalogue row)
│       └── I3rabStatus.cs                               # new enum: Approved / NeedsReview / Unsupported
│
├── application/QuranDashboard.Application.Abstractions/Quran/Words/Morphology/Irab/
│   ├── II3rabGenerationSource.cs                        # reads morphology segments + features for generation
│   ├── II3rabRuleCatalog.cs                             # supplies the 142-row seed + signature→rule lookup
│   ├── II3rabGenerationWriter.cs                        # COPY-staged UPDATE + rule seed, one transaction
│   ├── II3rabGenerationReportWriter.cs                  # Markdown+JSON report writer
│   ├── I3rabSegmentInput.cs                             # DTO: segment id, kind, pos, features, lemma flag
│   ├── I3rabRuleSeedRow.cs                              # DTO: signature key, arabic, family, default status, sort
│   ├── I3rabGenerationResult.cs                         # outcome (counts, checks, persisted, report path)
│   └── I3rabInvariants.cs                               # hard-check ids + thresholds (mirrors MorphologyInvariants)
│
├── application/QuranDashboard.Application/Quran/Words/GenerateI3rab/
│   ├── GenerateI3rabCommand.cs                          # options: ReportOut, Force
│   ├── GenerateI3rabHandler.cs                          # orchestrates load → assemble → validate → write/rollback → report
│   └── GenerateI3rabResult.cs
│
├── infrastructure/QuranDashboard.Infrastructure/
│   ├── Files/Quran/Morphology/Irab/
│   │   ├── I3rabRuleCatalogSeed.cs                      # the 142-signature catalogue (exact Arabic labels) — single label source
│   │   ├── SegmentSignatureBuilder.cs                  # features → signature key (kind:pos[:ALLAH][:case][:tense:voice][:person])
│   │   └── I3rabAssembler.cs                            # per-segment: signature → catalogue lookup → (label, ruleId, status, reason)
│   ├── Persistence/Repositories/Quran/Irab/
│   │   ├── EfI3rabGenerationWriter.cs                   # COPY tuples → temp; UPDATE … FROM; seed quran_i3rab_rules; one txn
│   │   ├── I3rabSql.cs                                  # SQL constants (temp table, UPDATE…FROM, seed upsert, check queries)
│   │   ├── I3rabValidationRunner.cs                     # the 9 hard checks + 5 warnings
│   │   ├── I3rabCommandExecutor.cs                      # stale/missing-morphology detection, refusal/--force, txn boundary
│   │   └── I3rabGenerationConstants.cs
│   ├── Persistence/Configurations/Quran/Words/Morphology/Irab/
│   │   ├── QuranI3rabRuleConfiguration.cs               # table quran_i3rab_rules, unique signature_key, columns
│   │   └── (modify) ../WordMorphologySegmentConfiguration.cs  # 4 columns + FK + CHECK + index on i3rab_rule_id
│   ├── Reports/Quran/Irab/
│   │   └── MarkdownJsonI3rabReportWriter.cs
│   └── Migrations/
│       └── <timestamp>_AddWordSimpleI3rab.cs           # tooling-generated during /implement (schema-only)
│
└── tools/QuranDashboard.DataImporter/
    └── Program.cs                                       # (modify) register the `generate-i3rab` verb

Backend/tests/QuranDashboard.Tests/Quran/WordsSimpleI3rab/
├── SegmentSignatureBuilderTests.cs                      # pure unit (no DB)
├── I3rabRuleCatalogSeedTests.cs                         # 142 rows, 67 families, the 21 corrections, label correctness (no DB)
├── I3rabAssemblerTests.cs                               # signature → lookup correctness (no DB)
├── I3rabGenerationTests.cs                              # full run → 100% approved, idempotency, --force (Testcontainers)
├── I3rabValidationFailureTests.cs                       # each hard check fails ⇒ rollback (Testcontainers)
├── I3rabSourceSafetyTests.cs                            # source columns unchanged, rowcount stable, 208 NULL forms preserved
└── I3rabRefusalTests.cs                                 # refuse on stale/missing morphology + non-empty without --force
```

**Structure Decision**: Reuse the existing 7-project Clean Architecture solution and the established
`Quran/Words/Morphology/` feature area. Feature 005 adds an `Irab/` sub-area in Domain / Abstractions /
Infrastructure and a `GenerateI3rab/` use-case folder in Application, plus a fourth verb on the existing
`DataImporter` console host. This matches Feature 004's foldering exactly and introduces no new project,
no dumping folder, and no API surface.

## Complexity Tracking

> No Constitution Check violations — this section is intentionally empty.
