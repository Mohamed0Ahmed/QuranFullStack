# Feature 030 — frozen performance budgets (§15.3)

**Task**: T075. **Status: both halves measured** — US1 (relationships) and US2 (templates).

These are **measured** numbers, not invented thresholds. §15.3 requires budgets frozen *before* a
writer or UI is accepted; this file is that freeze, and the assertions live in
`Backend/tests/QuranDashboard.Tests/Abwab/Relationships/RelationshipBudgetTests.cs` and
`Backend/tests/QuranDashboard.Tests/Abwab/Templates/TemplateBudgetTests.cs`.

A budget is a **measurement gate**. It is never permission to weaken a correctness assertion.

## Reference machine and stack (the assumptions these numbers depend on)

| | |
|---|---|
| CPU | Intel Core i7-6820HQ @ 2.70 GHz — 4 physical cores / 8 threads |
| RAM | 14 GiB total (~4 GiB available at measurement time) |
| OS | Linux 7.0.0-28-generic (x64) |
| .NET SDK | 10.0.110 |
| PostgreSQL | `postgres:16-alpine` via Testcontainers (`Abwab/_Fixtures/PostgresFixture.cs`, collection `AbwabDbCollection`) |
| DB locality | container on the same host — **no** network latency is included |
| Run mode | Debug build, tests run alongside other local load; single measurement run |

**These numbers are hardware-specific.** They were taken on a loaded developer laptop, not on
dedicated CI hardware. That is precisely why the wall-clock assertions are floored rather than fitted
tightly to the measurement (see below).

## Data assumptions (the shape of the fixture each number was measured against)

| Path | Fixture |
|---|---|
| Directional cycle validation — deep | Chain of **40** categories joined by 40 active `BroaderNarrower` edges; the probe closes the chain, forcing the BFS to walk every level |
| Directional cycle validation — wide | Star at depth **1**: one category pointing at **40** leaves |
| Subtree dormancy | **200** affected categories, **100** active mutual relationships, **50** of them dormant (every 4th endpoint soft-deleted) |
| Template application — small | Template of **2** roots × depth 1 = **4** nodes, applied to one empty target category |
| Template application — large | Template of **10** roots × depth 1 = **20** nodes, applied to one empty target category |
| Template history | **106** real audited template-history events (one create + 105 edits) on one template — deliberately past the 100-entry cap |

## Measured results

### 1. Directional cycle validation

| Metric | Measured | Frozen budget | Rationale |
|---|---|---|---|
| Queries, depth-40 chain | **40** | `depth × 1 + 2` = 42 | One batched query per BFS **level**, never one per edge |
| Queries, depth-1 star (40 edges) | **2** | measured 2 + 1 = **3** | Cost tracks **depth**, not edge count — this is the N+1 guard |
| p95 latency (20 iterations, warm) | **2.82 ms** | **250 ms** | `max(measured × 5, 250 ms floor)` |

The query count is the load-bearing assertion: it is deterministic and hardware-independent, so it is
what actually catches a regression to per-edge querying. The two query assertions together pin the
complexity class — *proportional to depth, independent of width*.

### 2. Subtree-dormancy query

| Metric | Measured | Frozen budget | Rationale |
|---|---|---|---|
| Queries, 2 affected categories | **2** | — | Baseline |
| Queries, 200 affected categories | **2** | measured 2 + 1 = **3** | **Constant**: attached rows, then a single batched endpoint load |
| p95 latency (20 iterations, warm) | **6.72 ms** | **250 ms** | `max(measured × 5, 250 ms floor)` |

`EfAbwabRelationshipReadPort.GetDormantCountsAsync` is constant-query by construction — it batches the
endpoint load instead of resolving endpoints per row. The test asserts the large and small affected
sets produce the **identical** count, which is the real anti-N+1 statement.

### 3. Template application (US2)

| Metric | Measured | Frozen budget | Rationale |
|---|---|---|---|
| Queries, 4-node template | **30** | — | Baseline |
| Queries, 20-node template | **86** | — | Baseline |
| Queries **per created category** | **3.50** | **4** | `(86 − 30) / (20 − 4)` — constant per node, so cost tracks rows created, not round-trips per row |
| p95 latency (10 iterations, warm, 4-node template) | **61 ms** | **305 ms** | `max(measured × 5, 250 ms)` |

The per-node figure is the load-bearing assertion. Application is inherently O(nodes) — it creates one
real category per node — so the budget cannot be "constant total"; the meaningful statement is that
the **per-node** cost is constant. A regression that re-resolved manual protection or the destination
name guard per created row would add at least one whole query per node and fail the gate.

The remaining ~16 queries are fixed per-application overhead: barrier lock, revision lock, target and
ancestor load, template + node + alias load, the acyclicity read, the ChangeSet/audit-event insert,
and the commit.

### 4. Template-history read (US2)

| Metric | Measured | Frozen budget | Rationale |
|---|---|---|---|
| Queries, 106-event history | **2** | measured 2 + 1 = **3** | **Constant**: the capped page, then the timeline generation |
| Entries returned | **100** (`HasMore = true`) | `IAbwabTemplateReadPort.MaxHistoryEntries` = 100 | The response is bounded regardless of how long a template has existed |
| p95 latency (10 iterations, warm) | **6 ms** | **250 ms** | `max(measured × 5, 250 ms floor)` |

This path carries a **known, recorded cost**: both predicates are substring matches over the
append-only `abwab_audit_events.payload` text column, so the *scan* is unindexable and degrades as the
audit log grows. Fixing that needs first-class indexed action-kind and aggregate-id columns on the
audit event — `028` kernel substrate, and the audit read model is `033`'s, so neither is `030`'s to
reshape. What `030` **does** own and has fixed is the **response size**: the projection is capped at
`MaxHistoryEntries` and reports truncation through `TemplateHistoryDto.HasMore`, so no single read can
return an unbounded payload. Handed to `033` with that split stated.

### 5. UI baseline (reused, not re-measured)

Per T075 the `029` large-tree browser budget is the UI baseline. It is recorded in
`Backend/report/feature-029-abwab-core/001-completion-validation-report.md`: at `nodeCount=2500` the
Abwab tree measured `abwabRenderMs=134` / `abwabScrollMs=124` against a reused `028` spike baseline of
`baselineRenderMs=60` / `baselineScrollMs=162`, asserted as `max(baseline × 5, 2000 ms)`.

Neither `030` slice introduces a new large-collection UI surface. The relationship page renders one
category's relationships (a short list); the template editor renders one template's node tree, which
is authored by hand a node at a time and is nowhere near a 2,000-node tree. The `029` budget bounds
both with large headroom. The two browser suites
(`e2e/abwab/relationships-slice.spec.ts`, `e2e/abwab/templates-slice.spec.ts`) assert **interaction
correctness** — explicit actions, no drag, explicit save with no edit-session lock, exactly-one
post-mutation reload, RTL focus order — rather than re-deriving a render budget.

## Why the wall-clock budgets are floored at 250 ms

Three of the four measured p95 values are single-digit milliseconds. Asserting `measured × 5`
(14 ms / 34 ms / 30 ms) would make the suite fail on any slower or busier machine while telling us
nothing about a real regression. The floor follows the shape `029` already used
(`max(baseline × 5, 2000 ms)` for browser interaction): the multiplier governs when the measurement is
large, the floor governs when it is small. Template application is the one path where the multiplier
wins (61 ms × 5 = 305 ms), because it writes a whole subtree rather than running a single query. At
these budgets a genuine order-of-magnitude collapse — an N+1 reintroduced, an index dropped — still
fails the gate, which is what the gate is for.

## Re-measuring

```bash
cd /projects/Dashboard/App
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --filter "FullyQualifiedName~RelationshipBudgetTests|FullyQualifiedName~TemplateBudgetTests"
```

If a change moves a **query count**, that is a real regression: fix the query, do not raise the
budget. If a change moves only wall-clock p95 on different hardware, re-record the hardware
assumptions in this file rather than loosening a correctness assertion.
