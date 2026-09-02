---
name: performance-backend-review
description: Use when explicitly asked for a backend/database performance review or when a Quran Dashboard query, endpoint, import, transaction, or backend test path is reported as slow.
---

# Performance Backend Review

## Responsibility

Evidence-based, review-only performance findings for the changed or reported backend
scope (.NET / ASP.NET Core / EF Core / PostgreSQL, including the DataPipelines
importers/generators and `tools/QuranDashboard.DataImporter`): query shape and N+1s,
projection vs full-entity materialization, tracking vs `AsNoTracking` on genuinely hot
paths, index fit for real query paths, pagination/result-size limits and payload
over-fetching, transaction scope/lock duration and async usage, importer/DataPipeline
runtime and memory behavior (streaming vs in-memory, batching, bulk insert), cache/reuse
of demonstrably repeated expensive work, and measured backend test runtime. Read-only
measurement (e.g. `EXPLAIN` in a read-only session, a timed run) is part of this
responsibility when a finding needs it.

**Not this skill's job:** general engineering/architecture/test-code-quality review;
speculative caching or indexing; mutating source, migrations, tests, or data; executing
fixes; or invoking another Skill. Explicit invocation only — the word "review" alone
never selects it, and frontend performance belongs to `performance-angular-review`.

## Evidence rules (anti-noise)

Every finding points at a real code/query path in the changed scope with a plausible
cost. Do not flag missing caches, batching, or indexes without a real repeated expensive
operation or a clear query path that would use them; do not rate `AsNoTracking` MAJOR on
tiny or cold reads. When impact is uncertain, mark it NOTE and recommend a measurement
(`EXPLAIN ANALYZE`, profiling, a timed import) rather than asserting a problem. Fewer,
well-evidenced findings beat a long list — a false positive erodes trust in every other
finding.

## Quran data safety overrides speed (hard constraint)

Correctness, atomicity, and provenance always win over speed. A recommendation that
weakens text integrity, source hashes/manifest checks,
source-unchanged checks, validation hard checks,
rollback/atomicity, report correctness, or provenance/traceability is itself the defect
— never propose it, and flag it if the diff already does it. "Slower but correct" is the
right answer for this product.

## Conditional context

- Immediate callers/callees of the changed path — only to judge whether it is hot or
  read-heavy; do not drift into auditing untouched subsystems.
- The EF Core entity configurations under
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/` —
  the authoritative index/key/relationship declarations, and therefore the only
  trustworthy input to an index or join-path finding.
- The implicated heading of `Backend/.architecture/BACKEND_STRUCTURE.md` or
  `CLEAN_ARCHITECTURE.md` — only so a recommendation does not violate structure.
- Live cardinality — there is deliberately no stored database-baseline report; if row
  counts matter, measure in a read-only session and state when you measured.

## Output

1. **Verdict** — PASS / PASS WITH NOTES / CHANGES REQUESTED (the last only with at
   least one MAJOR, evidence-backed finding), with one line of reasoning.
2. **Scope reviewed** — changed files/paths inspected and context consulted.
3. **Findings** — per finding: severity (MAJOR / MINOR / NOTE), file/path, why it
   affects runtime/memory/database/import/test time, the evidence (quote the
   line/loop/query), suggested direction (never implemented). "None." when clean.
4. **Quran data safety** — confirm no finding trades away integrity, atomicity,
   validation, reports, or provenance (or flag the diff if it already does).
5. **Next step** — proceed, fix first, or measure before trusting a verdict.
