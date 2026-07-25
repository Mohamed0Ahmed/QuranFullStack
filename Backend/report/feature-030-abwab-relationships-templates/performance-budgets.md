# Feature 030 — frozen performance budgets (§15.3)

**Task**: T075. **Status: US1 (relationships) half only.** The US2 (templates) half — template
application and the template-editor/application UI interaction — is **not** measured here and must be
appended before T075 is ticked.

These are **measured** numbers, not invented thresholds. §15.3 requires budgets frozen *before* a
writer or UI is accepted; this file is that freeze, and the assertions live in
`Backend/tests/QuranDashboard.Tests/Abwab/Relationships/RelationshipBudgetTests.cs`.

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

### 3. UI baseline (reused, not re-measured)

Per T075 the `029` large-tree browser budget is the UI baseline. It is recorded in
`Backend/report/feature-029-abwab-core/001-completion-validation-report.md`: at `nodeCount=2500` the
Abwab tree measured `abwabRenderMs=134` / `abwabScrollMs=124` against a reused `028` spike baseline of
`baselineRenderMs=60` / `baselineScrollMs=162`, asserted as `max(baseline × 5, 2000 ms)`.

The relationship slice does **not** introduce a new large-collection UI surface: it renders one
category's relationships (a short list), not a 2,000-node tree, so the `029` budget bounds it with
large headroom. The relationship browser suite
(`Frontend/quran-dashboard-ui/e2e/abwab/relationships-slice.spec.ts`) asserts **interaction
correctness** — explicit actions, no drag, exactly-one post-mutation reload, RTL focus order — rather
than re-deriving a render budget.

## Why the wall-clock budgets are floored at 250 ms

Both measured p95 values are single-digit milliseconds. Asserting `measured × 5` (14 ms / 34 ms)
would make the suite fail on any slower or busier machine while telling us nothing about a real
regression. The floor follows the shape `029` already used (`max(baseline × 5, 2000 ms)` for browser
interaction): the multiplier governs when the measurement is large, the floor governs when it is
small. At 250 ms, a genuine order-of-magnitude collapse — an N+1 reintroduced, an index dropped —
still fails the gate, which is what the gate is for.

## Re-measuring

```bash
cd /projects/Dashboard/App
dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj \
  --filter "FullyQualifiedName~RelationshipBudgetTests"
```

If a change moves a **query count**, that is a real regression: fix the query, do not raise the
budget. If a change moves only wall-clock p95 on different hardware, re-record the hardware
assumptions in this file rather than loosening a correctness assertion.
