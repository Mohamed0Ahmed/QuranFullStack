---
description: "Task list for 027-abwab-preflight (documentation-only freeze)"
---

# Tasks: Abwab Preflight — Documentation-Only Freeze

**Input**: Design documents from `/specs/027-abwab-preflight/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: None (no code). Verification is the documentation-consistency check suite
(DC-1…DC-10) defined in `contracts/doc-consistency-checks.md`; those check tasks live in
User Story 5, not as TDD test tasks.

**Organization**: Tasks grouped by user story (spec.md) for independent completion.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: parallelizable — touches a *different file* with no dependency on an incomplete task.
- **[Story]**: US1…US5 map to the user stories in `spec.md`.
- Every task names its **Source §** (Master Plan clause), its **Output file**, and a **Done-when** condition.

## Ground rules (apply to EVERY task)

1. **Canonical source (read-only)**: `docs/feature-abwab-management/MASTER_PLAN.md`. Never modify it. `§` references point into it.
2. **Documentation-only**: produce NO code, package, migration, seed, database, runtime, mock, or implementation task (Master Plan §1, §18.1 exit). Every output is Markdown under `specs/027-abwab-preflight/`.
3. **No new decisions**: copy/record only; there are no open Decision Gates (§5, §20.1). No "provisional"/"if needed"/"TBD"/"TODO"/future-decision language.
4. **No downstream leak**: record ownership/acceptance for `028`–`034`; perform none of their implementation (§17, §18.2–§18.8).
5. **Byte-for-code fidelity**: copied catalogues must match the Master Plan exactly — no synonym, addition, or omission (§18.1 exit).

## Path conventions

- Feature dir: `specs/027-abwab-preflight/`
- Spec file: `specs/027-abwab-preflight/spec.md`
- Canonical source (read-only): `docs/feature-abwab-management/MASTER_PLAN.md`

---

## Phase 1: Setup (Shared)

**Purpose**: Confirm inputs and read the exact source clauses before copying.

- [x] T001 Confirm environment: run `git branch --show-current` (expect `027-abwab-preflight`), `test -f docs/feature-abwab-management/MASTER_PLAN.md && echo OK`, and `test -d specs/027-abwab-preflight && echo OK`. **Done-when**: branch is `027-abwab-preflight` and both files/dirs exist.
- [x] T002 [P] Read canonical clauses that `027` freezes — §1–§5.2, §6–§13.1, §16, §17, §18.1, §19, §20 of `docs/feature-abwab-management/MASTER_PLAN.md` — as read-only source notes. Do NOT edit the Master Plan. **Done-when**: each frozen catalogue/matrix/DAG location (§2.1, §5, §5.1, §5.2, §8, §9, §10, §11, §13, §16, §19) is located and quotable.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the spec skeleton and guardrails every story writes into.

**⚠️ CRITICAL**: No user-story task may begin until Phase 2 is complete.

- [x] T003 Create the `spec.md` skeleton in `specs/027-abwab-preflight/spec.md`: H1 title "Feature Specification: Abwab Preflight — Documentation-Only Freeze"; header fields (`Feature Branch: 027-abwab-preflight`, `Created: 2026-07-22`, `Status: Draft`, `Input:` = verbatim user description); a canonical-source blockquote naming `docs/feature-abwab-management/MASTER_PLAN.md` (§2); and the mandatory empty section headings in order — `## Overview`, `## User Scenarios & Testing`, `## Requirements` (with `### Functional Requirements`, `### Key Entities`), `## Success Criteria` (`### Measurable Outcomes`), `## Assumptions`, `## Appendix A — Frozen Reference`, `## Appendix B — Requirement-to-Owner Traceability Catalogue`. **Done-when**: all headings exist in order; no placeholder tokens left from the template.
- [x] T004 Write the `## Overview` in `spec.md` stating `027` is documentation-only, produces no code/package/migration/seed/DB/runtime/mock/task, introduces no new decision, and owns none of `028`–`034` (cite §1, §17, §18.1, §20.1). **Done-when**: Overview asserts all five ground rules with § citations.

**Checkpoint**: Skeleton + guardrails ready — user stories can proceed.

---

## Phase 3: User Story 1 - Freeze vocabulary, normalization, and permission catalogue (Priority: P1) 🎯 MVP

**Goal**: Record the identity-bearing catalogues (vocabulary, normalization, permissions, supersessions) byte-for-code so every later Kit inherits identical strings.

**Independent Test**: Diff each recorded vocabulary entry, normalization step, and permission code against the Master Plan — valid when all match byte-for-code with zero synonyms/additions/omissions (verified by DC-1/DC-2/DC-5 in US5).

- [x] T005 [US1] Copy the §5 "Frozen vocabulary, labels, and catalogue inputs" table into `spec.md` Appendix A.1 verbatim, including entity term `باب`/`أبواب` (never `تصنيف`), `Category`/`Section`, `/gates` + `الأبواب`, `أبواب غير مصنفة`, `كل الأبواب` (view, never a Section), `قوالب الأبواب`, `أسماء البحث`, `ملاحظات الرابط`, `مساحة إعداد الطلبات`, the audit changed-value row, and the Reactive-Forms/Signals row. **Source §**: §5. **Output**: `spec.md`. **Done-when**: every §5 row present unaltered.
- [x] T006 [US1] Copy the §5.1 Arabic normalization algorithm into `spec.md` Appendix A.2 byte-for-code: all 8 steps, the full frozen Unicode-16 Arabic-mark set (`U+0610–U+061A`, `U+064B–U+065F`, `U+0670`, `U+06D6–U+06DC`, `U+06DF–U+06E4`, `U+06E7–U+06E8`, `U+06EA–U+06ED`, `U+0897–U+089F`, `U+08CA–U+08E1`, `U+08E3–U+08FF`, `U+10EFC–U+10EFF`), `أ/إ/آ/ٱ → ا`, `ى → ي`, `ة` NOT normalized to `ه`, display-preservation, and the shared fixture-corpus requirement. Include the "no runtime 'all marks' predicate" rule. **Source §**: §5.1. **Output**: `spec.md`. **Done-when**: all ranges/steps reproduced; DC-5 will diff IDENTICAL.
- [x] T007 [US1] Copy the §5.2 permission catalogue table and frozen rules into `spec.md` Appendix A.3: all nine domain rows and their exact codes only; `SystemOwnerOnly` metadata on `permission.*`/`audit.restore`/`safetyPoint.*`; `DashboardAdminBaseline` on `attribution.view`; the non-existent codes (`category.copy`, Owner-bypass, SystemOwner-direct-link); the SystemOwner automatic-policy rule; the aggregate-subresource mapping (aliases → `category.edit`; template verbs); and "backend enforcement authoritative, frontend UX only". Forbid synonyms (create/add, remove/delete). **Source §**: §5.2. **Output**: `spec.md`. **Done-when**: DC-1 will diff IDENTICAL.
- [x] T008 [US1] Copy the §2.1 supersessions into `spec.md` Appendix A.6 (all six): plain-string `RepresentativeQuranExcerpt`; relationship/reorder outside the 24h gate; grouped-link ≥2 members + delete-whole; permissions & owner membership outside Product Restore; canonical highlights use `QuranWord.Id`; ordered notes replace source-description wording. **Source §**: §2.1. **Output**: `spec.md`. **Done-when**: all six present unaltered in meaning.
- [x] T009 [US1] Add FR-001…FR-005 and FR-019 under `### Functional Requirements` in `spec.md`, each citing its `§` (vocabulary §5; no-drag §3.2/§9/§14.2; normalization §5.1; permission catalogue §5.2; supersessions §2.1; byte-for-code fidelity §18.1 exit). **Output**: `spec.md`. **Done-when**: six FRs written, each testable and § cited.
- [x] T010 [US1] Add "User Story 1" (Priority P1) under `## User Scenarios & Testing` in `spec.md` with Why-this-priority, Independent Test, and ≥3 Given/When/Then acceptance scenarios covering entity term, mark-removal set, and non-existent permission codes. **Output**: `spec.md`. **Done-when**: story is independently testable as written.

**Checkpoint**: Identity catalogues frozen and verifiable.

---

## Phase 4: User Story 2 - Copy registries, matrices, contracts, and the DAG (Priority: P1)

**Goal**: Reproduce the ownership/behavior matrices, conflict codes, source contracts, and the dependency DAG so nothing downstream is ambiguous.

**Independent Test**: Each registry/matrix/contract/DAG entry matches its Master Plan source (DC-2/DC-3/DC-4 in US5); no changed, added, or dropped entry.

- [x] T011 [US2] Copy the §11 HTTP-409 conflict-code catalogue into `spec.md` Appendix A.4 verbatim (every `abwab.*` code + condition), plus the 400/403/404/503 mappings (`abwab.validation_failed`, `abwab.permission_denied`/`system_owner_required`/`ownership_denied`, `abwab.not_found`, `abwab.restore_executing`) and the "no add/rename/remap without amendment" rule. **Source §**: §11. **Output**: `spec.md`. **Done-when**: DC-2 will diff IDENTICAL.
- [x] T012 [US2] Copy the §16 DAG into `spec.md` Appendix A.5: the §16.1 renderer-independent edge list (17 edges), the §16.2 direct-predecessor table, and the §16.3 safe-parallelism rules; state explicitly that `027` has no predecessor and `027 -> 028` is its only successor edge. **Source §**: §16. **Output**: `spec.md`. **Done-when**: DC-3/DC-4 will diff IDENTICAL.
- [x] T013 [US2] In `spec.md`, record §8 (aggregate/restore registry), §9 (action & protection matrix), §10 (notification event/recipient matrix), and §13/§13.1 (attribution-source & note contracts) by `§` reference with an explicit "copy-without-reinterpretation" requirement, and record §6 (cross-cutting invariants) + §7 (domain/persistence model) as frozen reference to be owner-assigned in Appendix B. Preserve: one restore class per state ("No adapter" explicit), the "last editor/Owner never overrides manual/stabilization" rule, no-Outbox, mutashabihat word-extraction deferral, and current-door no-copy/no-reorder. **Source §**: §6–§10, §13. **Output**: `spec.md`. **Done-when**: each area referenced with its § and the preservation rule stated.
- [x] T014 [US2] Add FR-006…FR-011, FR-013, FR-014 under `### Functional Requirements` in `spec.md`, each citing its § (registry §8; action matrix §9; notification matrix §10; conflict codes §11; source contracts §13/§13.1; DAG §16; scope §3.1/§3.2; operational fluency §3.3; repo-reality §4). **Output**: `spec.md`. **Done-when**: FRs written, each testable and § cited.
- [x] T015 [US2] Add "User Story 2" (Priority P1) under `## User Scenarios & Testing` with Why/Independent-Test and ≥3 acceptance scenarios covering one-restore-class, one conflict-code lookup, and the DAG predecessor/successor rule. **Output**: `spec.md`. **Done-when**: independently testable.

**Checkpoint**: Matrices, conflict codes, contracts, and DAG frozen.

---

## Phase 5: User Story 3 - Produce the requirement-to-owner traceability catalogue (Priority: P1)

**Goal**: Map every locked invariant group to its clauses, exactly one implementation owner, and ≥1 acceptance owner.

**Independent Test**: Each row has exactly one implementation-owner set and ≥1 acceptance owner; row count equals the §19 group count (DC-7).

- [x] T016 [US3] Copy the §19 "Locked-invariant coverage and acceptance ownership" matrix into `spec.md` Appendix B verbatim: all rows, four columns (invariant group | canonical plan clauses | implementation owner(s) | primary acceptance owner). Preserve every owner exactly; invent no owner. **Source §**: §19. **Output**: `spec.md`. **Done-when**: Appendix B row count == §19 row count (currently 29).
- [x] T017 [US3] Verify the record-set invariant: every Appendix B row has exactly one implementation-owner set and at least one acceptance owner; owners drawn only from `{027,028,029,030,031,032,033,034, planning workflow, independent review}`. **Output**: verification (no file change unless a defect is found, then fix `spec.md`). **Done-when**: 0 rows with zero/multiple implementation-owner sets, 0 rows missing an acceptance owner.
- [x] T018 [US3] Add FR-012, FR-015, FR-022 (traceability catalogue §18.1 step 4/§19; documentation-only §1/§18.1; entry preconditions §1/§16.2/§18.1) and "User Story 3" (Priority P1) with acceptance scenarios covering implementation-owner and acceptance-owner completeness. **Output**: `spec.md`. **Done-when**: FRs + story written and § cited.
- [x] T019 [P] [US3] Populate `specs/027-abwab-preflight/data-model.md` with the frozen-artifact table (artifact | source § | location | fidelity rule) and the traceability-record schema (fields `invariant_group`, `canonical_clauses`, `implementation_owner(s)` = exactly one, `acceptance_owner(s)` = ≥1) plus the "no runtime data model / no lifecycle" statement. **Source §**: §8, §18.1, §19. **Output**: `data-model.md` (separate file). **Done-when**: schema + artifact table present; consistent with Appendix B.

**Checkpoint**: Traceability catalogue complete and owner-verified.

---

## Phase 6: User Story 4 - Record purely-visual tokens within the locked presentation (Priority: P2)

**Goal**: Capture visual tokens (non-color diff indicator) without changing behavior/ownership/scope/data contracts.

**Independent Test**: Each recorded token changes only presentation and touches no behavior/ownership/scope/data contract.

- [x] T020 [US4] Record the purely-visual changed-value diff indicator in `spec.md` (FR-016 + the Appendix A.1 audit-changed-value row): `--qd-accent-text`/allowed green plus a textual or icon marker, never color alone; state it stays within the locked scholarly/RTL presentation and changes no behavior/ownership/scope/data contract. **Source §**: §5, §6.3, §18.1 step 3. **Output**: `spec.md`. **Done-when**: FR-016 present with the neutrality constraint.
- [x] T021 [US4] Add "User Story 4" (Priority P2) with Independent Test and an acceptance scenario asserting the diff indicator is `--qd-accent-text`/green + marker, never color alone. **Output**: `spec.md`. **Done-when**: independently testable.

**Checkpoint**: Visual tokens captured, behavior untouched.

---

## Phase 7: User Story 5 - Pass automated documentation-consistency checks (Priority: P2)

**Goal**: Provide and run the DC-1…DC-10 checks that prove the freeze is faithful and no implementation leaked.

**Independent Test**: Running the check suite reports zero mismatch across copied dependency sets and catalogue codes, and the no-decision/no-implementation/no-leak guards hold.

- [x] T022 [P] [US5] Author `specs/027-abwab-preflight/contracts/doc-consistency-checks.md` defining inputs (canonical vs frozen copy), checks DC-1…DC-10 (permission codes, conflict codes, DAG edges, predecessor table, normalization ranges, supersessions, traceability completeness, no-decision guard, no-implementation guard, no-downstream-leak guard), each with a pass condition, plus non-goals. **Source §**: §18.1 exit, §11, §16, §19. **Output**: `contracts/doc-consistency-checks.md` (separate file). **Done-when**: all ten checks specified with pass conditions.
- [x] T023 [P] [US5] Author `specs/027-abwab-preflight/quickstart.md` as a runnable validation guide containing the exact shell commands for DC-1/DC-2/DC-3/DC-5 diffs, DC-7 row-count check, and DC-8/DC-9/DC-10 guards, with expected outputs and the acceptance statement (all checks pass ⇒ `027` accepted ⇒ `028` authorized). **Source §**: §16, §18.1 exit. **Output**: `quickstart.md` (separate file). **Done-when**: every DC check has a copy-paste command + expected result.
- [x] T024 [P] [US5] Author `specs/027-abwab-preflight/research.md` recording decision-free confirmations R1–R5 (canonical source; no open decisions; copy-fidelity mechanism; ownership-assignment rule; scope boundary vs `028`–`034`) as Decision/Rationale/Alternatives triples, and state "0 NEEDS CLARIFICATION". **Source §**: §1, §2, §5, §16, §17, §18.1, §20.1. **Output**: `research.md` (separate file). **Done-when**: R1–R5 present, no open question remains.
- [x] T025 [US5] Run DC-1/DC-2/DC-3/DC-5 from repo root: diff permission codes, `abwab.*` conflict codes, `NNN -> NNN` DAG edges, and `U+XXXX` ranges between `docs/feature-abwab-management/MASTER_PLAN.md` and `specs/027-abwab-preflight/spec.md` (commands in `quickstart.md`). **Done-when**: all four report IDENTICAL (empty diff). If any differ, fix `spec.md` and re-run.
- [x] T026 [US5] Run DC-7: confirm §19 row count equals Appendix B row count and re-confirm the T017 owner-assignment invariant. **Done-when**: counts equal (29 == 29) and invariant holds.
- [x] T027 [US5] Run DC-8/DC-9/DC-10 guards: grep `spec.md` for `provisional|if needed|to be decided|TBD|TODO|NEEDS CLARIFICATION` (expect none); confirm `git diff --name-only HEAD -- Backend Frontend` is empty (docs-only); manual-review that Appendix B records `028`–`034` ownership but performs none of it. **Done-when**: DC-8 PASS, DC-9 PASS, DC-10 PASS.
- [x] T028 [US5] Add SC-001…SC-007 under `### Measurable Outcomes` in `spec.md` (byte-for-code 0 diffs; every invariant 1 impl owner + ≥1 acceptance owner; 0 doc-check mismatches; 0 provisional decisions; 0 code/artifacts; 0 downstream-leak items; DAG 17 edges + predecessor table with `027`→`028` only) and "User Story 5" (Priority P2) with an acceptance scenario for zero mismatch. **Source §**: §16, §17, §18.1 exit, §19, §20.1. **Output**: `spec.md`. **Done-when**: seven measurable, technology-agnostic SCs + story written.

**Checkpoint**: Freeze proven faithful; guards green.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Finish supporting sections and run the full acceptance gate.

- [x] T029 Write the `## Assumptions` section in `spec.md`: Master Plan passed independent adversarial review and is the sole canonical source with superseded sources removed (§1, §2, §16.2, §18.1 entry); no other document is a normative input (§2); repo source authoritative only for verified current facts (§4); `028`–`034` generated later per the DAG, not by `027` (§16); post-`034` review is review-only (§18.8, §20.2); constitution file is an unfilled template. **Output**: `spec.md`. **Done-when**: all six assumptions present with § citations.
- [x] T030 [P] Re-validate the spec quality checklist `specs/027-abwab-preflight/checklists/requirements.md` against the final `spec.md`; toggle only changed `[ ]`/`[x]` markers; add a Notes bullet explaining that Appendix A/B code strings are frozen product identifiers (required by the freeze mandate), not new implementation detail. **Output**: `checklists/requirements.md` (separate file). **Done-when**: checklist reflects final spec; all items pass or exceptions are noted.
- [x] T031 Final consistency scan of `spec.md`: no contradictory statements, heading hierarchy intact, one canonical term per concept (no synonyms), and every FR/SC/US cross-referenced to a `§`. **Output**: `spec.md`. **Done-when**: 0 contradictions, 0 synonym drift, every requirement § cited.
- [x] T032 Run the full `quickstart.md` end-to-end from repo root; confirm all DC-1…DC-10 checks pass with zero mismatch. This is the `027` acceptance gate and the sole DAG precondition for authorizing `028` (§16, §18.1 exit). **Done-when**: every check passes; `027` is acceptance-ready.

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: after Setup — BLOCKS all user stories (skeleton + guardrails).
- **User Stories (Phase 3–7)**: after Foundational.
  - US1, US2, US3 (all P1) and US4, US5 mostly write into the single file `spec.md`, so their `spec.md` tasks are **sequential** (same-file). Separate-file tasks (T019 data-model, T022 contracts, T023 quickstart, T024 research) are **parallel**.
- **Polish (Phase 8)**: after all desired stories; T032 runs last.

### User-story dependencies

- **US1 (P1)** — independent; MVP.
- **US2 (P1)** — independent of US1; both write `spec.md` appendices.
- **US3 (P1)** — copies §19; T019 (data-model) depends only on §19 content, not on US1/US2 file writes.
- **US4 (P2)** — independent; references the A.1 audit row from US1 (content), not a hard ordering.
- **US5 (P2)** — the check-running tasks (T025–T027, T032) depend on US1–US4 `spec.md` content being written; the authoring tasks (T022–T024) do not.

### Parallel opportunities

- **T002** [P] (read-only source extraction).
- **T019, T022, T023, T024** [P] — four different files, authorable concurrently once Phase 2 is done.
- **T030** [P] — separate checklist file.
- All same-file (`spec.md`) tasks are sequential to avoid write conflicts.

---

## Parallel Example: separate-file authoring (after Phase 2)

```bash
# Different files → run together:
Task: "T019 Populate data-model.md traceability schema + frozen-artifact table"
Task: "T022 Author contracts/doc-consistency-checks.md (DC-1…DC-10)"
Task: "T023 Author quickstart.md runnable validation guide"
Task: "T024 Author research.md R1–R5 decision-free confirmations"
```

---

## Implementation Strategy

### MVP first

1. Phase 1 Setup → Phase 2 Foundational (skeleton + guardrails).
2. Phase 3 US1 (identity catalogues) → **STOP & VALIDATE** via DC-1/DC-2/DC-5.
3. US1 alone is a demonstrable freeze of the identity-bearing catalogues.

### Full acceptance (required to authorize 028)

1. Complete US1 + US2 + US3 (all P1) — vocabulary/normalization/permissions, matrices/conflict-codes/DAG, and the traceability catalogue.
2. Complete US4 (visual tokens) + US5 (checks).
3. Phase 8 Polish → run T032 (full quickstart). All DC checks pass ⇒ `027` accepted ⇒ `028` authorized (§16, §18.1 exit).

### Guardrails (never violate)

- No code/package/migration/seed/DB/runtime/mock/task (§1, §18.1 exit).
- No new/provisional decision; no open Decision Gate (§5, §20.1).
- No `028`–`034` implementation; record ownership only (§17, §18.2–§18.8).
- Byte-for-code fidelity on every copied catalogue (§18.1 exit).

---

## Notes

- `[P]` = different file, no incomplete-task dependency.
- `[Story]` maps each task to a user story for traceability.
- Copy tasks are **transcription**, not authorship — the Master Plan wording governs; on any doubt, the Master Plan is authoritative (§2).
- Commit after each phase or logical group; stop at any checkpoint to validate independently.
- Total: 32 tasks (T001–T032).
