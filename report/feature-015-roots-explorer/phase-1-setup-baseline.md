# Feature 015 — Roots Explorer
## Phase 1 (T001): Green Baseline Setup

**Date:** 2026-06-24
**Recorded by:** T001 baseline check

### 1. Branch confirmation

All three repos confirmed on branch `015-roots-explorer` with clean working
trees (branch confirmation handled by orchestrator prior to this check).

| Repo                        | Branch                | Status |
|-----------------------------|-----------------------|--------|
| App (workspace root)        | `015-roots-explorer`  | clean  |
| Backend                     | `015-roots-explorer`  | clean  |
| Frontend/quran-dashboard-ui | `015-roots-explorer`  | clean  |

### 2. Toolchain versions

- dotnet SDK: 10.0.109
- node: v20.20.2
- npm: 10.8.2

### 3. Backend build

**Command:** `dotnet build Backend/QuranDashboard.sln`
**Result:** PASS (exit 0)
**Warnings:** 0
**Errors:** 0
**Elapsed:** 26.82 s

All 8 projects built:
QuranDashboard.Domain, .Shared, .Application.Abstractions, .Application,
.Infrastructure, .DataImporter, .Api, .Tests.

### 4. Frontend install

`node_modules` already present; `package-lock.json` exists.
**Install needed:** No (skipped). Existing install used.

### 5. Frontend build

**Command:** `npm run build` (resolves to `ng build`) in `Frontend/quran-dashboard-ui`
**Result:** PASS (exit 0)
**Warnings:** 2 (both pre-existing CSS budget warnings, unrelated to Feature 015):
- `src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.scss` exceeded 4.00 kB budget by 211 bytes (4.21 kB total).
- `src/app/features/mushaf/components/source-selector/source-selector.component.scss` exceeded 4.00 kB budget by 185 bytes (4.18 kB total).

**Errors:** 0
**Bundle:** Initial total 362.40 kB (99.41 kB transfer). Output at `dist/quran-dashboard-ui`.

### 6. Baseline summary

| Check            | Result                    | Warnings                              |
|------------------|---------------------------|---------------------------------------|
| Branches         | PASS                      | —                                     |
| Backend build    | PASS                      | 0                                     |
| Frontend install | N/A (already present)     | —                                     |
| Frontend build   | PASS                      | 2 (pre-existing, budget only)         |

**Baseline is GREEN.** Any future build failure not introduced by Feature 015
work is attributable to a pre-existing issue. The only pre-existing warnings
are two Mushaf CSS budget warnings (F012 area), which predate this feature.
