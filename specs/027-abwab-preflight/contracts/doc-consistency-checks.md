# Contract: Documentation-Consistency Checks

**Feature**: `027-abwab-preflight` | **Date**: 2026-07-22

`027` exposes no external runtime interface (no API, CLI, or schema) — so there is no
service contract. Its only "contract" is the **automated documentation check** required
at acceptance: the copied direct-dependency sets and catalogue codes MUST match the
canonical Master Plan (Master Plan §18.1 exit, FR-020, SC-003). This document specifies
**what** the checks compare and their pass condition. It does not implement them — `027`
produces no code (§1, §18.1 exit); the check is a mechanical text comparison a reviewer or
CI step runs against the two Markdown files.

## Inputs

- **Canonical**: `docs/feature-abwab-management/MASTER_PLAN.md`
- **Frozen copy**: `specs/027-abwab-preflight/spec.md`

## Required comparisons

| Check | Compares | Source (§) | Pass condition |
|---|---|---|---|
| DC-1 Permission codes | Set of catalogue permission codes | §5.2 ↔ Appendix A.3 | Identical set; no synonym, addition, or omission |
| DC-2 Conflict codes | Set of `abwab.*` conflict/error codes | §11 ↔ Appendix A.4 | Identical set |
| DC-3 DAG edges | Set of `NNN -> NNN` direct edges | §16.1 ↔ Appendix A.5 | Identical set (17 edges); `027` has no predecessor; only successor `027 -> 028` |
| DC-4 Predecessor table | Per-Kit direct predecessors | §16.2 ↔ Appendix A.5 | Identical rows |
| DC-5 Normalization ranges | Set of `U+XXXX` mark ranges + 8 steps | §5.1 ↔ Appendix A.2 | Identical ranges; `ة`-not-normalized rule present; no "all marks" predicate |
| DC-6 Supersessions | Six frozen supersessions | §2.1 ↔ Appendix A.6 | All six present, unaltered in meaning |
| DC-7 Traceability completeness | Every §19 invariant group | §19 ↔ Appendix B | Same row count; each row has exactly one implementation owner and ≥1 acceptance owner |
| DC-8 No-decision guard | Absence of provisional language | spec.md | No "provisional", "if needed", "TBD", "TODO", or future product/architecture decision |
| DC-9 No-implementation guard | Absence of implementation output | feature dir | No code, package, migration, seed, DB, runtime, or mock file produced by `027` |
| DC-10 No-downstream-leak guard | Absence of `028`–`034` work | spec.md | Ownership recorded, not performed (§17, §18.2–§18.8) |

## Output

A pass/fail result per check. Acceptance requires **all checks pass with zero
mismatches** (SC-001, SC-003). Any mismatch blocks `027` acceptance — and therefore
blocks `028` authorization — until the frozen copy is corrected to match the Master Plan
(§16, §18.1 exit).

## Non-goals

- The checks do not modify the Master Plan (read-only canonical input, §2).
- The checks do not validate downstream implementation — that is owned by `028`–`034`
  acceptance (§17).
- The checks introduce no product or architecture decision (§20.1).
