# Testing debt

Deliberately skipped test coverage, and what pays it back.

This file exists because a testing posture can be a legitimate decision but must never be an
invisible one. Every line below names a **concrete future trigger** — a change that will already
be touching that code, at which point writing the missing tests costs almost nothing extra.
"Later" is not a trigger.

**What does not belong here:**

- **`SmokeRouteCatalog` parity entries are not debt-able.** `SmokeCoverageParityTests` fails by
  name when a registered route has no catalog entry, so an entry is a build-level gate, not
  coverage. A route added without one fails the suite; it cannot be deferred into this file.
- Tiers `TESTING_STRATEGY.md` requires. This file records what was *not written*, never a reason
  to skip a run that document mandates.

Rows stay until they are paid. Delete a row when its tests land — do not mark it done.

## abwab-relations (branch `abwab-relations`, 2026-07-29)

Posture: **no new tests in the feature**, by explicit decision. Verification was the existing
suites staying green plus a manual pass over the feature's own interaction checklist. Nothing in
this feature's evidence claims behavioral coverage.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| 1 | Backend write behavior — canonical pair ordering (`door_a_id < door_b_id` for all three types), `broader_door_id` direction storage, all-or-nothing multi-target add, self/unknown/archived refusals, soft delete with no revive | `Persistence/Writes/Abwab/EfAbwabRelationsWriter.cs` | The next change to the relations writer, **or** adding a fourth relation type — both have to re-derive these rules anyway |
| 2 | Backend read behavior — the dormancy join (relation visible iff its own `deleted_at` is null **and** both endpoints are live) and `RelationCount`'s live-endpoint-only counting. Also the negative side: no door **or section** write path may touch `abwab_door_relations`, so move / reorder / rename / section create-rename-delete must leave every row and count alone | `Persistence/Reads/Abwab/EfAbwabRelationsReader.cs`, `EfAbwabTreeReader.GetLiveRelationCountsAsync`, `Persistence/Writes/Abwab/` | The next change to the archive / restore / bulk-archive paths **or to either section/door writer** — dormancy rides entirely on the former, and the "structure never touches relations" invariant is enforced by nothing but the absence of code in the latter |
| 3 | Relations smoke — the `200` / `201` / `204` / `400` / `404` / `409` status and envelope contract of the three routes, including the archived-anchor read that must answer `200 []` rather than `404` (all three routes are catalogued `ParityOnly`, i.e. listed but not dispatched) | `Tests/Smoke/`, `SmokeRouteCatalog.cs` | When write protection lands and `/api/abwab` stops being `Open`: the auth cases force a dispatched test per route regardless |
| 5 | One e2e flow — add a relation, see the chip on both doors, archive one endpoint, watch the chip and the tree flag vanish, restore, watch them return | `Frontend/quran-dashboard-ui/e2e/` | The next time relations change shape. Slice C's unit specs and its manual reproduction (`docs/feature-ux-slice-c/evidence.md`) both stop at the seam; this is the only check that would catch dormancy end to end, and it is the §6.3 cell most likely to break silently |

Rows 1 and 2 are the ones with no cover **anywhere** — not a spec, not a smoke case, not an e2e
flow. Row 5 is the single cheapest thing that would cover the most: it crosses the read, the
write, the count, and the flag in one pass.

## abwab-templates (branches `abwab-templates-a` / `abwab-templates-b`, 2026-07-29)

Posture: **no new tests in the feature**, the second consecutive feature under it. Verification
was the existing suites staying green (Frontend: 190 spec files / 2,158 tests, unchanged) plus a
manual pass over the feature's own interaction checklist. Nothing in this feature's evidence
claims behavioral coverage.

**One exception, added by the Slice B review-fix round:** `abwab-templates.facade.spec.ts`
(3 cases, Frontend now **191 files / 2,161 tests**) pins the selected template's identity — a
failed switch shows no template rather than the previous one, and a failed refresh of the same
template keeps it on screen. It exists because the round fixed a defect that let the copy modal
preview one template while apply sent another; a correctness fix of that shape is not deferrable
into this file. Row 9 is narrowed accordingly, not deleted.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| 6 | Backend template/node write behavior — one root per template, sibling-name uniqueness inside a template, node delete taking its whole subtree, sibling resequencing to `1..N`, the root's refusal to reorder or delete, template delete touching one row | `Persistence/Writes/Abwab/EfAbwabTemplatesWriter.cs` | The next change to the templates writer — it has to re-derive every one of these rules anyway |
| 7 | **The deep copy — restated for ux-slice-g's children-only reversal, same row, new surface.** The root's direct children enumerated and copied recursively (never the root itself); the level-1 `nextOrder + i` offset with every touched scope staying `1..N`; depth ≥ 2 keeping verbatim `OrderValue`; `section_id` inheritance at every depth; alias rows and each DTO reporting its own node's aliases; all-or-nothing across N targets; the empty-root-template `400` raised before the target reads; and the per-`(target, child)`-name `409` | `Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs` | The next change to the apply path **or to `abwab_doors`' per-sibling unique index**. Unchanged trigger — still the only place in the repo where door rows are created by something other than `CreateAsync` |
| 8 | Templates smoke — the `200`/`201`/`204`/`400`/`404`/`409` status and envelope contract of all nine routes (all nine are catalogued `ParityOnly`, i.e. listed but not dispatched) | `Tests/Smoke/`, `SmokeRouteCatalog.cs` | When write protection lands and `/api/abwab` stops being `Open`: the auth cases force a dispatched test per route regardless |
| 9 | Frontend workshop behavior — the flat→tree build for nodes, the tree editor's collapse/order-edit/quick-add, and the node modal over the shared authoring form. **The copy modal's picker is no longer in this row** — `abwab-template-copy-modal.component.spec.ts` covers it, and the picker itself is now the shared `abwab-door-picker`. **The facade's selected-template identity is not in this row either** — `abwab-templates.facade.spec.ts` covers it. **Widened by ux-slice-g:** the tree's two new row-menu paths — right-click with `preventDefault`, and `ContextMenu`/`Shift+F10` anchored via `getBoundingClientRect` — are also uncovered here; jsdom cannot produce a usable `contextmenu` event or a meaningful `getBoundingClientRect`, so a browser walk (`docs/feature-ux-slice-g/evidence.md`, T703) is the only check that exists today | `features/abwab/components/abwab-template-tree/`, `abwab-template-node-modal/`, `pages/abwab-templates-page/` | The next time the workshop changes shape |
| 10 | One e2e flow — author a two-level template, copy it into two doors, see the subtree under both, then edit the template and watch the copies **not** change | `Frontend/quran-dashboard-ui/e2e/` | Same trigger as row 9; it is the only check that would catch a detachment regression end to end, and detachment is the cell this feature is most likely to have misunderstood |

Row 7 is the one with no cover **anywhere**. Row 10 is the cheapest thing that would cover the
most: it crosses the template writes, the deep copy, the doors read, and detachment in one pass.

**Not debt, and not deferrable:** the `abwab-door-fields-form` extraction is covered by
`abwab-door-modal.component.spec.ts` running green **unchanged** (11/11) — the extraction
preserved every `data-testid` through a `testIdPrefix` input precisely so that spec keeps
pinning the behavior it always pinned. The form has no spec of its own, and does not need one
while that remains true.

## ux-slice-f (branch `ux-slice-f-sections`, 2026-08-01)

Posture: **no new test suites**, rush-period decision (plan §4.1-6). Existing suites ran before
merge; the route-smoke tier is exempt from the posture and ran regardless (not debt-able, see
above). Nothing in this feature's evidence claims backend behavioral coverage for the new writer
method — the frontend cells in Phases 5-7 do claim coverage for what they assert.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| F1 | **The section reorder writer's behavior** — contiguous `1..N` across every live section, first→last and last→first, single-section no-op, out-of-range refusal, the stale-token 409, and the sibling-token 409 that makes the resequence all-or-nothing | `Persistence/Writes/Abwab/EfAbwabSectionsWriter.ReorderAsync` | The next change to the sections writer, **or** the fix for the `CountAsync + 1` / non-resequencing-delete gap (F2) — both have to re-derive these rules anyway. `AbwabDoorWriteBehaviorTests.cs` (`ReorderAsync_ProducesContiguousOrderValues`) is the shape it copies |
| F2 | **The duplicate-`OrderValue` condition itself** — create assigns `count(live) + 1` while delete resequences nothing, so two live sections can share an `OrderValue`; nothing anywhere asserts the reorder stays correct under it, and nothing asserts the heal | `EfAbwabSectionsWriter.cs` (`CreateAsync`, `DeleteAsync`) | Whoever fixes the create/delete gap. Until then the correctness rests entirely on the `(OrderValue, Id)` tie-break (`Writes/Abwab/README.md`), which is documented and untested |
| F3 | **Section reorder smoke** — the `200`/`400`/`404`/`409` status and envelope contract of the new route (catalogued `ParityOnly`, i.e. listed but not dispatched). The doors cases at `SmokeAbwabWriteTests.cs` are the template | When write protection lands and `/api/abwab` stops being `Open`: the auth cases force a dispatched test per route regardless |

## ux-slice-g (branch `ux-slice-g`, 2026-08-01)

Posture: **no new test suites**, rush-period decision (plan §4.1-8, continued from ux-slice-f).
Existing suites ran before merge; the route-smoke tier is exempt from the posture and ran
regardless (not debt-able, see above). Row 7 and row 9 of the `abwab-templates` section above
were **restated and widened**, not left describing a writer/tree that no longer matches reality
— their trigger and pay-off are unchanged, only their surface moved. The rows below are new debt
this slice itself introduces.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| G1 | **The level-1 offset specifically** — that N children land contiguously at `nextOrder … nextOrder+N-1` and the target's child scope stays `1..N`. At N = 1 a broken offset is invisible, which is exactly why this needs its own line rather than folding into row 7 | `Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs` | Whoever fixes the concurrent-apply `order_value` race, **or** the next change to any doors reorder path — both depend on target scopes being `1..N` |
| G2 | **The empty-template refusal and its ordering** — the `400`, and that it fires **before** the archived-target check | `EfAbwabTemplateApplyWriter.cs`, `ApplyTemplateHandler.cs` | The next change to the apply refusal set, or the first time a second refusal wants to move ahead of the target reads |
| G3 | **Apply smoke, narrowed** — the route's status/envelope contract now includes the new `400` and the re-shaped `409` message; still catalogued `ParityOnly` (listed but not dispatched) | `Tests/Smoke/`, `SmokeRouteCatalog.cs:356-359` | When write protection lands and `/api/abwab` stops being `Open`: the auth cases force a dispatched test per route regardless. Narrows row 8 above, does not replace it |
| G4 | **The copy modal's empty-template affordance** — that the confirm button disables at `templateNodeCount() === 0` and the preview swaps to the empty state. **Cheapest row in this table**: the modal's spec already exists and covers everything else it does, so this is one `it` block, not a suite | `abwab-template-copy-modal.component.spec.ts` | The next change to the copy modal |
