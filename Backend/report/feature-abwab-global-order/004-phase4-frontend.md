# Phase 4 — frontend: evidence

Plan: `docs/feature-abwab-global-order/plan.md` §7 Phase 4 (T401–T407).

## What changed

- `AbwabNode` gains `globalOrderValue: number | null`; `abwab.models.ts` adds the
  `AbwabOrderScope` (`'global' | 'section'`) domain type plus
  `ABWAB_ORDER_SCOPE_TO_WIRE`, the only place that maps it to the backend's numeric
  `AbwabReorderScope` (`Section = 1, Global = 2` — confirmed against
  `Backend/application/QuranDashboard.Application.Abstractions/Abwab/AbwabReorderScope.cs`
  before committing).
- `abwab-tree.builder.ts`: `liveRoots` sorts by `globalOrderValue, id`;
  `archivedRoots` is unchanged (`orderValue, id` — archived doors carry no global value).
  `filterAbwabRootsBySection` re-sorts a specific section's roots back to
  `orderValue, id` on a filtered copy, since the shared `liveRoots` array is now in the
  superset's order, not any one section's.
- `abwab-page-overlays.controller.ts`: the hand-built `AbwabDoorDto` in `selectedDoor`
  carries `globalOrderValue` (T401's named ripple).
- `abwab-tree.component.ts/.html`: new `orderScope` input. Depth-0 rows render/edit
  `globalOrderValue` under `'global'`, `orderValue` otherwise; depth > 0 always stays on
  `orderValue`. `orderCommitted` now carries the scope the row used.
- `abwab-cards.component.ts/.html`: same rule, applied only when `cardId() === null`
  (top level) — every drilled-in level stays on `orderValue` regardless of scope. Cards
  has no inline editor, so this is display-only.
- `abwab-page.component.ts/.html`: derives `orderScope` from `activeSectionId()`
  (`null` ⇒ `'global'`), passes it to both `qd-abwab-tree` and `qd-abwab-cards`, and
  `onOrderCommitted` maps the emitted scope through `ABWAB_ORDER_SCOPE_TO_WIRE` before
  dispatch.
- `abwab-write.controller.ts`: **no code change** — `reorderDoor(id, body)` already
  forwards `body` verbatim, and `ReorderDoorBody`'s widened shape flows through by
  construction. `abwab.labels.ts`: **no new entries** — the outcome→message mapping
  already prefers the backend's own Arabic message for every 400/409
  (`toFailureOutcome`), so a fallback string for the two new 400s would be the same dead
  code the README already calls out for the section-delete conflict.
- `abwab-move-picker.component.ts`: **no behavior change**, decision recorded in a
  class-doc comment — `destinationRows` walks `liveRoots` as given, so a section's
  destination list now follows the superset's global order rather than the section's
  own `orderValue`. Deliberate (plan §7 T402's audit obligation): the picker is a
  destination list, not an ordered outline, and pinned by a new spec case.

## Tests

- `npm test -- --include="src/app/features/abwab/**/*.spec.ts"` → **215 passed** (was
  ~200 before this phase; +15 net new/changed cases across the builder, tree, cards,
  move-picker, page, and write-controller specs).
- `npm test` (full suite) → **190 files / 2157 passed, 0 failed** (was 190/2142 —
  +15 matches the focused run).
- `npm run build` → clean, no errors (pre-existing bundle-budget warnings only,
  unrelated to this feature).

Every spec file across the feature that builds an `AbwabTreeDoorDto`, `AbwabDoorDto`,
or `AbwabNode` literal needed a `globalOrderValue` default to keep compiling under the
new required field — not just the six files T407 names. Found via `grep -rl` for
`directChildCount`/`liveChildCount`/`representativeAyahText` across `src/`, not by
waiting for the compiler to surface them one at a time.

## Self-review before commit (via `advisor`)

- **Confirmed the wire mapping against the backend enum directly** (see above) — the
  generated `AbwabReorderScope` type is `1 | 2` with no names, so an inverted mapping
  would have compiled clean and every test written in this phase would still have
  passed, since they assert against the same assumption. This is the one check that
  matters most in this phase and nothing here would have caught it on its own.
- **Checked `e2e/` for a break**, since `ng build`/`npm test`'s Vitest glob
  (`*.spec.ts`) never touches `*.e2e.ts`. `abwab-operations.e2e.ts`'s inline-reorder
  flow navigates directly into a sandbox section (`/abwab?section=<id>`), so
  `orderScope` resolves to `'section'` there and both the displayed numbers and the
  dispatched scope are unchanged — no fixture or assertion needed updating. No e2e file
  reads a superset order number, so the new global-vs-section display split does not
  surface there yet; that is T501/T502's job (Phase 5), not a phase-4 regression.
- **Added the missing tie-break case** `abwab-tree.builder.spec.ts` — two live roots
  with equal `globalOrderValue`, asserting the lower `id` sorts first, matching plan §4
  ("`id` as tie-break hardening") and §6's no-unique-index trap.

## Not yet done

Phase 5 (e2e independence flow, R1 parallelism-hazard decision, README updates,
re-measured `TESTING_STRATEGY.md` counts, root `CLAUDE.md` Active-Feature line) is
unstarted. `features/abwab/README.md` is now stale in three places this phase touched
(refresh-after-write's "in that scope" wording, the builder's ordering description, and
the move-picker's flat-list rationale) — tracked by plan §7 T504, not fixed here.
