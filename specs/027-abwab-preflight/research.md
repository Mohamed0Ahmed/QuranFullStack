# Phase 0 Research: Abwab Preflight — Documentation-Only Freeze

**Feature**: `027-abwab-preflight` | **Date**: 2026-07-22

`027` introduces no new product or architecture decision (Master Plan §5, §20.1). There
are therefore no `NEEDS CLARIFICATION` items and no design alternatives to weigh — a
freeze copies decisions already made. "Research" here is confirmation of the preconditions
and mechanics of the copy, each recorded as a decision/rationale/alternatives triple for
traceability.

## R1 — Canonical source

- **Decision**: `docs/feature-abwab-management/MASTER_PLAN.md` is the sole canonical
  product and architecture source; every `027` artifact traces to it by `§` reference.
- **Rationale**: The Master Plan passed independent adversarial review and superseded
  planning sources were removed, making it self-contained (§1, §2, §18.1 entry).
- **Alternatives considered**: Consulting historical planning/remediation/review
  documents — rejected; they are not normative inputs (§2).

## R2 — No open decisions

- **Decision**: `027` records; it never chooses a restore class, protection target,
  permission code, notification recipient, ownership boundary, normalization rule, Quran
  identity model, note/group behavior, write-enforcement layer, concurrency meaning,
  adapter dependency, frontend-transport ownership, or top-level dependency.
- **Rationale**: There is no open Decision Gate; a genuine change returns to an
  independent Master Plan amendment, not a local decision (§17, §20.1).
- **Alternatives considered**: Adding "provisional"/"if needed" placeholders — rejected;
  `027` exit forbids them (§18.1 exit, SC-004).

## R3 — Copy fidelity mechanism

- **Decision**: Reproduce the compact frozen catalogues byte-for-code (vocabulary,
  normalization algorithm, permission catalogue, HTTP-409 conflict codes, DAG edge list +
  predecessor table, supersessions) and the full §19 traceability matrix; reference the
  larger behavioral matrices (§6, §7, §8, §9, §10, §12, §13) by `§` pointer with a
  copy-without-reinterpretation requirement. Verify with mechanical text comparison.
- **Rationale**: Byte-for-code fidelity is an explicit exit criterion; the Master Plan
  remains the single authoritative source, so the spec need not physically duplicate every
  large matrix to be faithful — but the identity-bearing catalogues must be exact (§18.1,
  §2).
- **Alternatives considered**: (a) Paraphrasing catalogues — rejected; risks synonym drift
  (§5.2). (b) Duplicating the entire 195 KB plan into the spec — rejected; adds no fidelity
  over the authoritative source and increases divergence risk.
- **Evidence**: Mechanical diff of conflict codes, permission codes, DAG edges, and
  Unicode ranges against the Master Plan returns IDENTICAL; §19 traceability reproduces 29
  rows → 29 rows (see `quickstart.md`).

## R4 — Ownership assignment rule

- **Decision**: Every locked invariant group is assigned exactly one implementation owner
  set and at least one primary acceptance owner, copied from §19 without reinterpretation.
- **Rationale**: `027` exit requires this exact assignment so no invariant is orphaned or
  double-implemented (§18.1 exit, §19).
- **Alternatives considered**: Deriving owners from the DAG heuristically — rejected; §19
  is authoritative and must be copied, not recomputed.

## R5 — Scope boundary against 028–034

- **Decision**: `027` records ownership and acceptance for `028`–`034`; it performs none
  of their implementation (foundations, core, relationships/templates, links, workspace/
  review/notifications, audit/restore, realtime/hardening).
- **Rationale**: The eight-Kit ownership is fixed in §17 and §18.2–§18.8; `027`'s cohesive
  exit condition is a documentation freeze only (§17, FR-021).
- **Alternatives considered**: Pre-staging any `028` foundation artifact — rejected;
  `027 → 028` is a hard DAG edge and no implementation precedes acceptance (§16).

## Outcome

All Technical Context items are resolved (N/A or decision-free). No `NEEDS CLARIFICATION`
remains. Proceed to Phase 1.
