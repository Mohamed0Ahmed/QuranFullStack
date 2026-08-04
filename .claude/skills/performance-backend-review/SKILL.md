---
name: performance-backend-review
description: >-
  Deep, review-only PERFORMANCE audit for the Quran Dashboard .NET / ASP.NET Core /
  EF Core / PostgreSQL backend. Explicit-invocation only — use this skill when the user
  explicitly asks for a backend or database performance review, e.g. running
  "performance-backend-review", slow backend endpoints, N+1 queries, EF Core query
  performance, AsNoTracking vs tracking, missing indexes or query plans, transaction or
  lock cost, pagination / result-size cost, over-fetching or large response payloads,
  streaming vs loading into memory, caching or reuse of expensive reads, Quran importer /
  DataPipeline runtime cost, or slow backend tests (Testcontainers, repeated real-source
  imports). It inspects only the changed backend scope unless a wider audit is requested,
  and produces evidence-based findings, severities, and recommendations — never code fixes.
  Do NOT use this skill for general code review, engineering review, PR review, clean-code
  review, architecture or structure review, Spec Kit / phase review, or "is this safe to
  merge" — those belong to engineering-review (and backend-structure-review). It triggers
  only on explicit backend performance intent, not on the word "review" by itself.
---

# Performance Backend Review

A deep, **review-only** performance audit of the Quran Dashboard backend
(.NET / ASP.NET Core / EF Core / PostgreSQL, Clean Architecture: `domain`,
`application`, `application.abstractions`, `infrastructure`, `api`, `tests`, plus the
`DataPipelines` importers/generators/rebuilders and the `tools/QuranDashboard.DataImporter`).

The job is to find **real, evidence-backed** runtime, memory, database, import-time, and
test-time costs in the changed code — and to recommend how to fix them without ever
weakening Quran data integrity. You report; you do not implement.

## When to use / when NOT to use

This is an **explicit-invocation** skill. It exists alongside `engineering-review`, which
owns *general* code review, architecture, clean-code, Spec Kit compliance, and
merge-readiness. To avoid stepping on that skill, stay in your lane:

**Use this skill only when the request carries explicit backend performance intent** —
the user says "performance", "slow", "too slow", "latency", "scaling", "N+1", "query
plan", "index", "AsNoTracking", "over-fetching", "import takes too long", "tests are
slow", or names this skill directly.

**Do NOT use this skill** (defer to `engineering-review` / `backend-structure-review`) for:
"review this PR", "engineering review", "is this safe to merge", "architecture review",
"clean code review", "review the implementation", "phase review", or any general backend
review with **no** performance intent. If the user wants both a general review and a
performance pass, do the performance pass here and say the general review is a separate
skill.

## Review-only guardrails

- Do **not** modify application source code, migrations, or tests.
- Do **not** refactor, "quickly fix", or rewrite anything.
- Produce findings, severities, and recommendations only. If the user wants the fixes
  applied, that is a separate, explicitly requested task.

## Evidence-based findings only (anti-noise)

Performance review is worthless if it becomes a generic checklist of things that *might*
matter. Every finding must point at a **real code path in the diff** with a plausible cost.
Hold yourself to these rules so the report stays trustworthy and short:

- Do **not** flag missing caching, batching, or indexes unless there is a real **repeated
  expensive operation** or a **clear query path** that would use them. "Could add a cache
  here" with no evidence of repeated cost is not a finding.
- Do **not** rate `AsNoTracking` as MAJOR on tiny or simple read queries. Use MINOR or
  NOTE unless it is on a genuinely **hot or read-heavy** path.
- Do **not** recommend premature abstraction, speculative configurability, or broad
  rewrites in the name of performance.
- If the performance impact is **uncertain**, mark it **NOTE** and recommend a measurement
  (profiling, `EXPLAIN ANALYZE`, a timed import run, a benchmark) instead of asserting a
  problem.
- Keep the report focused. Do not pad it with generic advice that does not apply to the
  changed code.

The cost of a false-positive finding is real: it sends engineers to "optimize" code that
was fine and erodes trust in the next finding. Prefer fewer, well-evidenced findings.

## Scope discipline

Review **only the changed backend scope** (the diff / the files the user points at) unless
the user explicitly asks for a wider audit. When you need to judge whether a change is hot
or read-heavy, you may read the immediate callers/callees and the relevant entity/DbContext
configuration, but do not drift into auditing untouched subsystems.

## Quranic data safety overrides speed (hard constraint)

This product curates Quran source data. **Correctness, atomicity, and provenance always
win over speed.** The general Quran data-safety rules apply in full — see the shared
reference: `.claude/skills/engineering-review/references/quran-data-safety.md`. In a
backend performance context specifically, a recommendation that weakens any of the
following is itself the defect — never propose it, and flag it if the diff already does it:

- Quran text integrity (ayah / word / root / morphology / tafsir / translation text).
- Source hashes, manifest checks, and **source-unchanged** checks.
- Validation **hard checks** and report gates.
- Rollback / atomicity behavior (no partial-state imports).
- Report correctness.
- Provenance / license / traceability warnings.

If you cannot make something faster without touching one of these, say so plainly and stop
there. "Slower but correct" is the right answer for this product.

## Context you may consult (optional, only when it sharpens a finding)

- `Backend/.architecture/BACKEND_STRUCTURE.md`, `CLEAN_ARCHITECTURE.md`,
  `API_GUIDELINES.md` — layer boundaries and the `ApiResponse` contract, so a perf
  recommendation does not violate architecture.
- The relevant EF Core entity configurations under
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/`
  (`Quran/`, `Abwab/`, `Access/`) — the authoritative declaration of every index, key, and
  relationship, and therefore the only trustworthy input to an index / join-path check.
  `Migrations/` records how the live schema got there; `QuranDashboardDbContext` records
  what is mapped.
- There is deliberately **no database-baseline report** to read. One existed and was deleted:
  a snapshot of table shapes and row counts is stale the moment a migration lands, and it was
  being consulted as if current. If you need live cardinality, measure it in a read-only
  session and say when you measured it.

If a referenced document is missing, say so rather than inventing its contents.

## What to inspect

The sections below define what to evaluate. They map one-to-one onto the output sections
4–9. Walk them against the changed code; skip a section with "Not applicable in this diff"
when nothing in scope touches it (do not invent material to fill it).

### API / query performance
- **N+1 queries** — a query inside a loop, or lazy navigation accessed per row.
- **Query shape & projection** — `Select` into a DTO vs materializing whole entities/graphs.
- **Materializing full entities** when a projection would carry only the needed columns.
- **EF tracking vs `AsNoTracking`** on read-only paths (severity per the anti-noise rule).
- **Lazy-loading surprises** — navigations triggering hidden round-trips after the query.
- **Repeated DB round-trips** — several queries where one (or a join / batched load) suffices.
- **Pagination & result-size limits** — unbounded list endpoints; missing `Skip/Take`/limits.
- **Sorting / filtering cost** — ordering or filtering in memory that the DB should do, or
  on columns with no supporting index.
- **Response payload size / over-fetching** — returning more fields/rows than the client uses.
- **Lazy-loading heavy details** — large/optional detail returned eagerly when it could be
  fetched on demand.

### PostgreSQL / index
- New read paths have **suitable indexes** for their joins, filters, and ordering.
- New **migrations** introduce indexes matching the expected query patterns of the change.
- Unique / check constraints support **correctness** without harming expected write paths.
- Do **not** demand an index without a real query path that would use it.

### Transaction / concurrency
- **Transaction scope and lock duration** — is the transaction as short as correctness allows?
- **Work done inside transactions** — heavy CPU, parsing, network, or file IO holding a tx open.
- **Report writing / hashing / file IO inside transactions** — move outside the tx where
  atomicity does not require it (but never at the cost of integrity).
- **Atomicity / partial-state safety** — failure leaves no half-applied import.
- **Connection lifetime & async usage** — `async`/`await` all the way; no sync-over-async;
  connections not held longer than needed.
- **Long-running locks** avoided where the design allows.

### Importer / DataPipeline performance
The `DataPipelines` importers/generators/rebuilders and `tools/QuranDashboard.DataImporter`
process large Quran source files; this is where import-time and memory cost concentrate.
- **Large-file parsing strategy** — appropriate for the file size.
- **Repeated parsing / hashing / loading** of the same source within one run.
- **Streaming vs loading fully into memory** when the file is large and processed sequentially.
- **Bulk insert / `COPY`** usage instead of row-by-row inserts for big loads.
- **Batch size & memory behavior** — batching that bounds memory without thrashing.
- **`--force` truncate/reload cost and safety** — expensive, but must stay atomic and gated.
- **Validation gates & report-generation cost** — note the cost, but **never** recommend
  weakening source-integrity checks to reduce it (see the data-safety constraint).

### Cache / reuse
- **Cache key correctness** — keys capture every input that changes the result.
- **Duplicate expensive reads** — the same costly query/parse executed repeatedly in one path.
- **Reusing already-loaded data safely** within a request/import instead of re-querying.
- **Cache invalidation cost** — invalidation is correct and not more expensive than the win.
- Avoid **speculative caching** — only recommend a cache where evidence shows repeated
  expensive work.

### Backend test runtime
- **Testcontainers startup cost** — containers spun up more often than necessary.
- **Real-source import repetition** — full imports/rebuilds re-run per test when a shared
  fixture would do.
- **Shared fixture opportunities** — collection/class fixtures to amortize expensive setup.
- **Synthetic vs real-source balance** — heavy real-source runs where synthetic data proves
  the behavior just as well.
- **Slow tests caused by unnecessary full imports/rebuilds.**
- Do **not** recommend weakening assertion quality or Quran data safety to speed up tests.

## Required review output

Produce the report in **exactly** this structure. Keep it focused; sections 4–9 should
contain only real findings, not restated checklists.

```
# Performance Backend Review

## 1. Verdict
One of: PASS / PASS WITH NOTES / CHANGES REQUESTED
(One line of reasoning. CHANGES REQUESTED only when there is at least one MAJOR,
blocking, evidence-backed finding.)

## 2. Scope Reviewed
- Backend files / components inspected (the changed scope).
- Any context files read.

## 3. Performance Findings
For each finding:
- **Severity:** MAJOR (likely real runtime/scaling issue before merge) /
  MINOR (useful improvement, not merge-blocking) /
  NOTE (watch item, future scaling concern, or measurement suggestion)
- **File / path & code area**
- **Why it affects** runtime / memory / database / import time / test time
- **Evidence** from the diff or code path (quote the line/loop/query)
- **Suggested fix** (describe it; do not implement it)
- **Blocking?** yes / no
If none: "None."

## 4. API / Query Performance Check
Findings on N+1, projection/query shape, full-entity materialization, tracking vs
AsNoTracking, lazy-loading surprises, repeated round-trips, pagination/limits,
sort/filter cost, payload size/over-fetching, lazy-loading heavy details.
If nothing in scope: "Not applicable in this diff."

## 5. PostgreSQL / Index Check
Indexes for new joins/filters/ordering; migration index coverage; constraints supporting
correctness without harming writes. No index demands without a real query path.
If nothing in scope: "Not applicable in this diff."

## 6. Transaction / Concurrency Check
Transaction scope/lock duration; work inside transactions; report/hash/file IO inside
transactions; atomicity/partial-state safety; connection lifetime & async usage; long locks.
If nothing in scope: "Not applicable in this diff."

## 7. Importer / DataPipeline Performance Check
Large-file parsing; repeated parse/hash/load; streaming vs in-memory; bulk insert/COPY;
batch size/memory; --force truncate-reload cost & safety; validation/report cost.
(Never recommend weakening source-integrity checks for speed.)
If nothing in scope: "Not applicable in this diff."

## 8. Cache / Reuse Check
Cache key correctness; duplicate expensive reads; safe reuse of loaded data; invalidation
cost; no speculative caching without evidence.
If nothing in scope: "Not applicable in this diff."

## 9. Backend Test Runtime Check
Testcontainers startup cost; real-source import repetition; shared fixture opportunities;
synthetic vs real-source balance; slow tests from unnecessary full imports/rebuilds.
(Never weaken assertions or Quran data safety for speed.)
If nothing in scope: "Not applicable in this diff."

## 10. Quranic Data Safety Performance Rule
State explicitly that performance improvements must never compromise: Quran text integrity,
source hashes / manifest checks, source-unchanged checks, validation hard checks, rollback
behavior, report correctness, or provenance/license warnings. Confirm none of the findings
above ask for such a trade-off (or flag it if the diff already makes one).

## 11. Final Recommendation
Whether the diff can proceed as-is, needs fixes first, or needs measurement/profiling
before a verdict can be trusted. One short, direct next step.
```

## Guardrails

- Be direct and practical; prefer fewer, well-evidenced findings over a long list.
- Do not invent costs. If the diff or file tree is unavailable, request it.
- If you cannot tell whether a path is hot/read-heavy, say so and mark the finding NOTE
  with a measurement suggestion — do not inflate it to MAJOR.
- Never recommend trading Quran data integrity, atomicity, or provenance for speed.
- Do not implement fixes unless the user explicitly asks.
- This is a performance skill only. General code quality, architecture, clean-code, Spec
  Kit compliance, and merge-readiness belong to `engineering-review`; test-*code* quality
  belongs to `test-guard`. Stay in the performance lane.
