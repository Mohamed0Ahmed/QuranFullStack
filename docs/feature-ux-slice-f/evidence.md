# Slice F — Evidence

Plan: `docs/feature-ux-slice-f/plan.md`. Branch: `ux-slice-f-sections`, off `dev` @ `7b0e8fba`.

## T101 — Baseline (dev @ `7b0e8fba`, clean)

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Backend/QuranDashboard.sln` | Build succeeded, 0 warnings, 0 errors, 35.8s |
| No-pipeline regression | `dotnet test … --filter "...!~QuranDashboard.Tests.Smoke."` (§5 catalog) | 1086 passed, 0 failed, 0 skipped, 22s |
| Route-smoke tier | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` | 140 passed, 0 failed, 0 skipped, 49s |
| **`Tests.Smoke.Data` — RAN** | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke.Data"` | 13 passed (subset of the 140 above). `resources/db-dumps/quran-canonical/quran-canonical.dump` is present, so the fixture did not self-skip. |
| `check-api-contract` | `Backend/scripts/check-api-contract` | "API contract up to date." — clean at baseline; `git status --short` empty after the run. Run so a later staleness report can be attributed to this slice's own changes, not pre-existing drift. |
| Frontend tests | `npm test` | 193 files, 2316 tests passed, 0 failed. 202.32s (TESTING_STRATEGY.md's catalog figure of 191 files / 2161 tests is the prior measurement point — both counts have since grown; no contradiction). |
| Frontend build | `npm run build` | Succeeded, 17.9s. Pre-existing budget warnings (initial bundle +69.54 kB over 500 kB budget; two mushaf SCSS files over their 4 kB budget) — none introduced by this slice, carried forward as-is. |

**Baseline verdict:** both stacks green. Nothing outstanding to disentangle from this slice's own changes.

## T102 — Branch and feature record

- Branch `ux-slice-f-sections` created off `dev` @ `7b0e8fba`.
- Plan committed to the branch (not to `dev`).
- Root `CLAUDE.md` "Active Spec Kit Feature" section updated: `ux-slice-e` entry (merged) replaced with `ux-slice-f`. `docs/feature-ux-slice-e/` left untouched — no planning-artifact sweep in this slice (§3).

## Phase 2 — T201-T205 (backend route/writer/wiring/catalog)

- `dotnet build Backend/QuranDashboard.sln` — succeeded, 0 warnings, 0 errors (after retrying past one transient MSB3883 file-lock error from a lingering build-server process, unrelated to this change).
- `Tests.Abwab` — 46 passed, 0 failed (matches plan's precondition count — unchanged, no new backend tests per the rush-period posture).
- `Tests.Api` — 60 passed, 0 failed (matches §5's catalog figure).

## Phase 3 — T301 (route gate)

- `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` — **140 passed, 0 failed, 0 skipped**, 47s. Same total as the T101 baseline: `SmokeCoverageParityTests` asserts both parity directions (every registered route has a catalog entry, every catalog entry maps to a registered route) as a fixed handful of tests over the whole catalog, not one test per route, so an unchanged total together with a passing run is the expected signal that the new `POST api/abwab/sections/{id:int}/order` entry landed correctly in both directions.
- **`Tests.Smoke.Data` — RAN** (subset of the above; same `resources/db-dumps/quran-canonical/` dump as baseline, unchanged).

## Phase 5 — T501-T502 (frontend write path)

- `AbwabSectionsModalComponent` now declares `reorderSection` as a required input (T601's button/editor UI is the consumer; declaring the signature now is what T501's "bound into the modal" step requires — Angular's `strictTemplates` rejects a binding to an unknown property, so the input had to land in this phase, not deferred whole to Phase 6). The existing 15-case modal spec's shared `render()` helper got a matching stub so no existing case broke.
- Focused glob `npm test -- --include="src/app/features/abwab/**/*.spec.ts"` — **371 passed** (366 baseline-for-this-glob + 5 new: 1 in `abwab.api.spec.ts`, 4 in `abwab-write.controller.spec.ts`), 0 failed, 24 files. `abwab.api.spec.ts` asserts the POST verb, URL, and `{ position, version }` body; `abwab-write.controller.spec.ts` asserts success refreshes the snapshot, 409 maps to `conflict` with the backend message, 400 maps to `invalid`, and the door selection is untouched (a section write invalidates no door selection, matching `renameSection`).

## Phase 6 — T601-T603 (sections-modal order editor)

- Editor state machine per §T601's table: idle (a real `<button>` chip, Arabic `aria-label` naming the section and its order), editing (`<input type="number" min="1">` seeded from the live `sections()` row), submitting (`editingOrderId` cleared before the call, same ordering as the tree's `commitOrderEdit`), error (the existing single error strip). `onOrderKeydown` opens with `event.stopPropagation()` (§4.2-9, mandatory — the dialog binds `(keydown.escape)="requestClose()"`). Focus-in uses the plan's named mechanism (`#orderInput` + `viewChild` + `afterNextRender({ injector })`); focus-out (back to the row's own button) is scoped by this row's `data-testid` through the host `ElementRef`, since a bare `viewChild('orderButton')` would be ambiguous — every row not being edited renders that ref simultaneously. `editingOrderId` is a signal separate from `editingId`, and `isDirty` does not read it — an open order edit is not unsaved work (§4.2-15).
- One existing spec cell's expectation moved deliberately: "traps focus… with the first control inside the dialog" now points at `abwab-sections-modal-order-1`, because the order trigger is a real `<button>` (§4.2-16, not the tree's `<span>`) rendered before rename/delete in DOM order — it is now genuinely the row's first focusable control.
- Focused glob — **378 passed** (371 + 7 new cells: click opens seeded; Enter commits `(id, position, liveVersion)`; Escape cancels and the modal stays open; blur cancels without submitting; a non-integer/zero input submits nothing; a 409 renders the error strip; `resetDraft` on reopen clears an open order edit), 0 failed, 24 files.

## Phase 7 — T701-T703 (tab count badges)

- T701 ripple check re-run at execution time: `grep -rln AbwabTreeSnapshotVm src/app` still resolves to exactly the same 5 files the plan measured (model, builder, facade, selection store, README) — no drift, delta holds.
- `rootCountBySectionId` built in the same pass as `liveRoots` in `buildAbwabTreeSnapshot`; carried on `AbwabTreeSnapshotVm`. Builder spec: 22 passed (18 + 4 new — counts roots only, excludes archived roots, omits rather than zero-defaults a section with no roots, and pins the Σ-can-be-less-than-`liveRoots.length` non-identity).
- `.qd-tabs__count` rendered at the call-site in `abwab-toolbar.component.html` on both the «كل الأبواب» button and each section tab (`qdTab` itself untouched — DRIFT-4); `.qd-tabs__count--empty { opacity: 0.5; }` added beside the shipped selected-state block in `_components.scss`, composing rather than forking because that rule sets only `background`/`color`. Two existing tab-label assertions (`abwab-toolbar.component.spec.ts`, `abwab-page.component.spec.ts`) moved from reading a tab's full `textContent` to its leading text node, since the count `<span>` is now a sibling in the DOM.
- `ROOT_DOOR_FORMS` (four Arabic forms, no zero form — the zero-state is a visual dim, not a distinct phrase) plus `tabRootCountAriaLabel`/`allDoorsTabRootCountAriaLabel` in `abwab.labels.ts`; the badge's visible digits are `aria-hidden`, the tab's own `aria-label` carries the counted-noun phrase. `abwab-page.component.ts` derives `rootCountBySectionId` and `totalRootCount` (`liveRoots.length`, **not** a sum over the map) from the facade snapshot and binds both into the toolbar.
- Focused glob — **393 passed** (378 + 15 new: 4 builder, 4 toolbar, 7 labels), 0 failed, 24 files. `npm run build` — succeeded, 19.3s, same two pre-existing budget warnings as baseline, none new.
