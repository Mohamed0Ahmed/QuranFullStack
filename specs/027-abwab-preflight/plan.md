# Implementation Plan: Abwab Preflight — Documentation-Only Freeze

**Branch**: `027-abwab-preflight` | **Date**: 2026-07-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/027-abwab-preflight/spec.md`

## Summary

`027-abwab-preflight` is documentation-only. It records and freezes
`docs/feature-abwab-management/MASTER_PLAN.md` in Spec-Kit-ready form — frozen
vocabulary, normalization contract, permission catalogue, cross-cutting invariants,
domain/persistence model, registries, matrices, API/conflict contract, source contracts,
and the dependency DAG — plus a requirement-to-owner traceability catalogue for
`028`–`034` and automated documentation-consistency checks. It produces no code, package,
migration, seed, database, runtime, mock, or implementation task, introduces no new
product or architecture decision, reinterprets nothing, and owns none of the work
reserved for `028`–`034` (Master Plan §1, §17, §18.1, §20.1). The technical approach is
copy-with-fidelity: reproduce the canonical catalogues byte-for-code and verify with
mechanical comparison; assign every locked invariant exactly one implementation owner and
at least one acceptance owner.

## Technical Context

**Language/Version**: N/A — documentation only (Markdown); no source code produced (§1, §18.1 exit)

**Primary Dependencies**: None. Sole canonical input is `docs/feature-abwab-management/MASTER_PLAN.md` (§2)

**Storage**: N/A — no database, schema, migration, or seed (§18.1 exit)

**Testing**: Documentation-consistency comparison of copied direct-dependency sets and catalogue codes against the Master Plan (§18.1 exit); see `contracts/doc-consistency-checks.md`

**Target Platform**: N/A — planning/documentation artifact in `specs/027-abwab-preflight/`

**Project Type**: Documentation-only Spec Kit (preflight freeze)

**Performance Goals**: N/A — no runtime. Numeric performance budgets are frozen by each owning domain Spec Kit, not `027` (§15.3)

**Constraints**: Byte-for-code fidelity to the Master Plan; no new decisions; no content owned by `028`–`034`; no code/package/migration/seed/DB/runtime/mock/implementation task (§18.1, FR-017/FR-018/FR-021)

**Scale/Scope**: Eight-Spec-Kit portfolio (`027`–`034`); `027` freezes shared catalogues and produces one traceability catalogue with 29 locked invariant groups (§19)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is an unfilled template with no ratified principles, so
it imposes no project-specific gates. Governing rules for this feature come from the
Master Plan itself:

- **No new decisions** — there are no open Decision Gates; `027` records, never chooses (§5, §20.1). ✅ PASS
- **No implementation** — no code/package/migration/seed/DB/runtime/mock/task in `027` (§1, §18.1 exit). ✅ PASS
- **No downstream leak** — nothing owned by `028`–`034` is performed here (§17, §18.2–§18.8). ✅ PASS
- **Fidelity** — normalization and permission lists match byte-for-code (§18.1 exit). ✅ PASS (diff-verified IDENTICAL)

No violations. Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/027-abwab-preflight/
├── plan.md                          # This file (/speckit-plan output)
├── spec.md                          # Feature spec (frozen freeze + Appendix A/B)
├── research.md                      # Phase 0 output — decision-free confirmation
├── data-model.md                    # Phase 1 output — frozen-artifact + traceability schema
├── quickstart.md                    # Phase 1 output — freeze validation guide
├── contracts/
│   └── doc-consistency-checks.md    # Phase 1 output — required doc-check comparisons
└── checklists/
    └── requirements.md              # Spec quality checklist (from /speckit-specify)
```

### Source Code (repository root)

None. `027` writes no files outside `specs/027-abwab-preflight/`. No `src/`, `Backend/`,
or `Frontend/` change is part of this feature (§1, §18.1 exit). The canonical input
`docs/feature-abwab-management/MASTER_PLAN.md` is read-only for `027` and is not modified.

**Structure Decision**: Documentation-only. All artifacts live under
`specs/027-abwab-preflight/`. No application source tree is created or touched.

## Complexity Tracking

No Constitution Check violations. Section intentionally empty.
