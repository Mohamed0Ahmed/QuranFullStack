# Phase 1 Data Model: Abwab Preflight — Documentation-Only Freeze

**Feature**: `027-abwab-preflight` | **Date**: 2026-07-22

`027` has **no runtime data model** — no entities, tables, migrations, or persistence
(Master Plan §1, §18.1 exit). The "entities" below are the frozen **documentation
artifacts** the freeze produces and the **records** inside the traceability catalogue.
Fields are Markdown content, not columns; "validation rules" are fidelity requirements
verified by the doc-consistency checks (`contracts/doc-consistency-checks.md`). All
content is copied from the canonical Master Plan without reinterpretation (§2).

## Frozen documentation artifacts

Each artifact lives in `spec.md` (compact catalogues verbatim in Appendix A; larger
matrices referenced by `§`) and is validated for byte-for-code fidelity.

| Artifact | Source (§) | Location | Fidelity rule |
|---|---|---|---|
| Frozen Vocabulary & Labels | §5 | spec.md Appendix A.1 | Every term/label exact; entity term `باب`/`أبواب`, never `تصنيف` |
| Arabic Normalization Contract | §5.1 | spec.md Appendix A.2 | All 8 steps + full Unicode-16 mark set byte-for-code; `ة` not→`ه`; no "all marks" predicate |
| Canonical Permission Catalogue | §5.2 | spec.md Appendix A.3 | Only listed codes; assignability metadata; no synonyms; no non-existent codes |
| Frozen Supersessions | §2.1 | spec.md Appendix A.6 | All six supersessions preserved |
| HTTP-409 Conflict-Code Contract | §11 | spec.md Appendix A.4 | Every `abwab.*` code + condition exact; 400/403/404/503 mappings preserved |
| Dependency DAG | §16 | spec.md Appendix A.5 | 17 edges + predecessor table + parallelism; `027` no predecessor, only `027→028` |
| Aggregate/Restore Registry | §8 | referenced by § (copy-without-reinterpretation) | One restore class per state; owner/adapter prerequisite preserved |
| Action & Protection Matrix | §9 | referenced by § | Ordinary-24h/manual/stabilization columns preserved |
| Notification Event & Recipient Matrix | §10 | referenced by § | Recipients/exclusions/no-Outbox preserved |
| Attribution-Source & Note Contracts | §13, §13.1 | referenced by § | Per-source contracts + current-door no-copy + mutashabihat deferral preserved |
| Cross-cutting invariants & domain model | §6, §7 | referenced by §, owner-assigned in Appendix B | Recorded, assigned, not reinterpreted |
| In/out scope + operational fluency | §3.1, §3.2, §3.3 | spec.md Requirements/Overview | Preserved unchanged |
| Verified repository-reality constraints | §4 | spec.md Assumptions/FR-014 | Recorded as facts, not decisions |

**Artifact-level invariants**

- No artifact introduces a provisional/if-needed/future decision (§18.1 exit, SC-004).
- No artifact contains code, package, migration, seed, DB, runtime, mock, or task (§18.1
  exit, SC-005).
- No artifact performs work owned by `028`–`034` (§17, SC-006).

## Traceability Catalogue record

The traceability catalogue (`spec.md` Appendix B, copied from §19) is a list of records
with this fixed shape:

| Field | Meaning | Validation |
|---|---|---|
| `invariant_group` | Name of the locked invariant group | Non-empty; one row per §19 group |
| `canonical_clauses` | Master Plan `§` clauses defining it | Non-empty; each resolves to a real Master Plan section |
| `implementation_owner(s)` | Spec Kit(s) that implement it | **Exactly one owner set** (§18.1 exit) |
| `acceptance_owner(s)` | Spec Kit(s)/gate that accept it | **At least one** acceptance owner (§18.1 exit) |

**Record-set invariants**

- Row count equals the §19 group count (currently 29).
- Every group has exactly one implementation owner set and ≥1 acceptance owner (SC-002).
- Owners are drawn only from `{027, 028, 029, 030, 031, 032, 033, 034, planning workflow,
  independent review}` as fixed in §19 — no invented owner.

## State transitions

None. Documentation artifacts have no lifecycle. The only feature-level state is its Spec
Kit status: `Draft` → accepted once the doc-consistency checks pass and the traceability
record-set invariants hold (§18.1 exit). Acceptance of `027` is the sole DAG precondition
for `028` (§16).
