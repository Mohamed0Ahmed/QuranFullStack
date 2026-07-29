# Phase 3 — contract regeneration: evidence

Plan: `docs/feature-abwab-global-order/plan.md` §7 Phase 3 (T301).

## Commands run

```
Backend/scripts/export-swagger                 # dotnet build (Release) + swagger tofile
npm run generate:api   (Frontend/quran-dashboard-ui)
npm run docs:api       (Frontend/quran-dashboard-ui)
```

## Confirmed

- `openapi/swagger.json` regenerated from the phase-2 API build; contains `AbwabReorderScope`
  and 4 occurrences of `globalOrderValue`.
- `abwab-door-dto.ts` and `abwab-tree-door-dto.ts` both gained `globalOrderValue: number | null`.
- `abwab-reorder-scope.ts` (new) — `export type AbwabReorderScope = 1 | 2`.
- `reorder-door-body.ts` widened to `{ position, scope: AbwabReorderScope, version }`.
- `docs/api-reference/index.html` rebuilt via redocly + inline bundle.

## Changed files (8)

```
M  Frontend/quran-dashboard-ui/openapi/swagger.json
M  Frontend/quran-dashboard-ui/src/app/core/api/generated/models.ts
M  Frontend/quran-dashboard-ui/src/app/core/api/generated/models/abwab-door-dto.ts
M  Frontend/quran-dashboard-ui/src/app/core/api/generated/models/abwab-tree-door-dto.ts
M  Frontend/quran-dashboard-ui/src/app/core/api/generated/models/reorder-door-body.ts
?? Frontend/quran-dashboard-ui/src/app/core/api/generated/models/abwab-reorder-scope.ts
?? Frontend/quran-dashboard-ui/src/app/core/api/generated/models/abwab-reorder-scope-array.ts
M  docs/api-reference/index.html
```

## `npm run build` (required verification)

Fails with exactly the two breaks the plan names as phase 4's job, nothing else:

1. `abwab-page.component.ts:224` — `reorderDoor` call site omits the now-required `scope`
   (T405's job).
2. `abwab-page-overlays.controller.ts:38` — hand-built `AbwabDoorDto`-shaped object is missing
   `globalOrderValue` (T401's named ripple).

No other file breaks. This is the expected, plan-predicted state at the phase 2/4 handoff
boundary — phase 3 does not fix these; phase 4 does.
