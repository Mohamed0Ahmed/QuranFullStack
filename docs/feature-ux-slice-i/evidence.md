# Slice I — Evidence

Plan: `docs/feature-ux-slice-i/plan.md`. Branch: `ux-slice-i`, off `dev` @ `5adc9bc0`
(clean). Plan committed to the branch as `65b664b4`, not to `dev`.

## T101 — Baseline (dev @ `5adc9bc0`, clean)

Commands taken verbatim from `TESTING_STRATEGY.md` §5 `:341-358` / §6 `:401-414`.

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Backend/QuranDashboard.sln` | Succeeded. 0 warnings, 0 errors. 36.87 s. |
| No-pipeline regression | `dotnet test … --no-build --filter "…!~ the ten pipeline namespaces …&FullyQualifiedName!~QuranDashboard.Tests.Smoke."` | **1,086 passed**, 0 failed, 0 skipped. 22 s. Matches the strategy's expected count exactly. |
| `Tests.Api` | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Api"` | **60 passed**, 0 failed, 0 skipped. 14 s. |
| Route-smoke tier | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` | **140 passed**, 0 failed, **0 skipped**. 1 m 2 s. |
| **`Tests.Smoke.Data`** | — | **RAN.** The data tier skips per-test via `SmokeDumpFactAttribute`/`SmokeDumpTheoryAttribute` when the canonical dump is absent (`Smoke/Data/SmokeDumpGate.cs`); 0 skipped of 140 means the dump was present and every data case executed. |
| Frontend tests | `npm test` | **193 files, 2,343 tests** passed, 0 failed. 218.70 s. |
| Frontend build | `npm run build` | Succeeded, 18.264 s. Three pre-existing budget warnings (initial bundle +71.29 kB over the 500 kB budget; two mushaf SCSS files over their 4 kB budgets) — carried forward, none introduced here. |

**Frontend count discrepancy, recorded not rounded off:** `TESTING_STRATEGY.md` §6 `:410-411`
still says 191 files / 2,161 tests. The tree measures **193 / 2,343**, which is exactly what
Slice H's own T101 measured (`docs/feature-ux-slice-h/evidence.md`) — the strategy's frontend
line went stale before this slice and no slice has repaid it. This slice writes no spec, so
**193 / 2,343 is the number T502 must reproduce unchanged**; the plan's "191 / 2,161" is
inherited from the same stale line and is not the gate.

### The wire measurement (the number Phase 5 compares against)

Kestrel on the `http` launch profile (`http://localhost:5014`), local dataset. `curl -w`:

| Request | Status | Body bytes | Time |
|---|---|---|---|
| `GET /api/abwab/tree` (cold, first query of the process) | `200` | **140,187** | 3.434 s |
| `GET /api/abwab/tree` (repeat 1 / 2 / 3) | `200` | 140,187 | 0.135 s / 0.051 s / 0.032 s |
| `GET /api/abwab/templates` | `200` | 226 | 0.082 s |

Response headers on every one of them: `Content-Type`, `Date`, `Server`, `Transfer-Encoding`
— **no `ETag`, no `Cache-Control`**, confirming on the wire the plan's grep-based claim that
the backend has zero HTTP caching today. Each repeat re-queries the database; the falling
times are warm-EF/warm-Postgres effects, not caching.

**Baseline verdict: green.** Stop condition 5 does not fire. T501/T502 must reproduce
1,086 / 60 / 140 / 191 files / 2,161 tests unchanged — this slice writes no test.

## T102 — Sweep for recorded statements this slice falsifies

`grep -rn` across `Backend/`, `Frontend/quran-dashboard-ui/src/`, `docs/`,
`Backend/.architecture/`, and `Frontend/quran-dashboard-ui/.architecture/` for:
`No caching`, `no invalidation`, `ETag`, `If-None-Match`, `304`, `Not Modified`,
`unconditional`, `diagnostics only`, `diagnostics-only`. (`docs/abwab-ux-audit.md` and the
closed slices' own plans are historical record and excluded from the amendment obligation.)

Result — the plan-time prediction plus **one new hit**:

| Hit | Status |
|---|---|
| `Persistence/Reads/Abwab/README.md:106-108` — "**No caching.** … no invalidation story yet …" | In the §5.4 ledger. Replaced at T601. |
| `Persistence/Reads/Abwab/README.md:88-92` — `Version` is diagnostics-only, ignores relations | In the ledger. Amended (one clause) at T601. |
| `features/abwab/README.md:507-508` — the `version`-is-diagnostics-only gotcha | In the ledger. Amended to distinguish, not weakened, at T601. |
| **`features/abwab/README.md:285-296` — "`modal` … is not part of any cache key, restore identity, history identity or **ETag** … This is the one row of this table a future caching design must **not** pick up"** | **New — not in the plan-time sweep.** It is a *constraint this slice must honor*, not a falsified statement: the tree validator is a server-side generation counter keyed on nothing from the URL, the snapshot read stays one unparameterized root-scoped tree GET, and the relations read stays uncached (§4.2-9). All three clauses of the paragraph remain literally true. Folded into the ledger as a **confirming clause** on the `features/abwab/README.md` amendment (T601), so the next reader sees the constraint was met rather than merely unbroken by accident. |
| `models/abwab.models.ts:190` — "Diagnostics only — never used for conflict detection" | Still true; the validator consumes no DTO field. No edit. |
| `Domain/Abwab/AbwabDoorRelation.cs:35` — relation `Version` diagnostics-only | Still true. No edit (do-not-touch list). |
| `LOGGING_GUIDELINES.md:17`, enriched-morphology test string, controller "retag" comments | Unrelated senses of the search terms. No edit. |

Zero hits for `ETag` / `If-None-Match` / `304` / `Not Modified` in **code** on either end,
confirming the plan's "NEW PATTERN on both ends" premise at execution time.
