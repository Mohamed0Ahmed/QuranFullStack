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
| 4 | Frontend modal behavior — the four-group derivation, type-switch clearing picks, already-linked disabling per (pair, type), anchor-pick mode's inverted picker and its add-count, and the **mode-dependent direction-pill copy** (the pill names the targets in door mode and the anchor in anchor-pick mode; one shared pair of strings was inverted in the second until review caught it) | `features/abwab/components/abwab-relations-modal/` | The next time the modal changes shape — a component with no spec cannot be refactored safely twice |
| 5 | One e2e flow — add a relation, see the chip on both doors, archive one endpoint, watch the chip and the tree flag vanish, restore, watch them return | `Frontend/quran-dashboard-ui/e2e/` | Same trigger as row 4; this is the only check that would catch dormancy end to end, and it is the §6.3 cell most likely to break silently |

Rows 1 and 2 are the ones with no cover **anywhere** — not a spec, not a smoke case, not an e2e
flow. Row 5 is the single cheapest thing that would cover the most: it crosses the read, the
write, the count, and the flag in one pass.

## abwab-templates (branches `abwab-templates-a` / `abwab-templates-b`, 2026-07-29)

Posture: **no new tests in the feature**, the second consecutive feature under it. Verification
was the existing suites staying green (Frontend: 190 spec files / 2,158 tests, unchanged) plus a
manual pass over the feature's own interaction checklist. Nothing in this feature's evidence
claims behavioral coverage.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| 6 | Backend template/node write behavior — one root per template, sibling-name uniqueness inside a template, node delete taking its whole subtree, sibling resequencing to `1..N`, the root's refusal to reorder or delete, template delete touching one row | `Persistence/Writes/Abwab/EfAbwabTemplatesWriter.cs` | The next change to the templates writer — it has to re-derive every one of these rules anyway |
| 7 | **The deep copy** — depth, sibling-order preservation, `section_id` inheritance at every depth, alias rows, all-or-nothing across N targets, and the root-name collision that is the only `409` it can produce | `Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs` | The next change to the apply path **or to `abwab_doors`' per-sibling unique index**. This is the highest-value row in the file: it is the only place in the repo where door rows are created by something other than `CreateAsync`, so the doors' own write invariants are enforced here by a second, separately-written path that nothing compares against the first |
| 8 | Templates smoke — the `200`/`201`/`204`/`400`/`404`/`409` status and envelope contract of all nine routes (all nine are catalogued `ParityOnly`, i.e. listed but not dispatched) | `Tests/Smoke/`, `SmokeRouteCatalog.cs` | When write protection lands and `/api/abwab` stops being `Open`: the auth cases force a dispatched test per route regardless |
| 9 | Frontend workshop behavior — the flat→tree build for nodes, the tree editor's collapse/order-edit/quick-add, the node modal over the shared authoring form, and the copy modal's picker (search auto-expand, multi-select, the targets-not-union count, and the selection surviving a `409`) | `features/abwab/components/abwab-template-tree/`, `abwab-template-node-modal/`, `abwab-template-copy-modal/`, `pages/abwab-templates-page/` | The next time the workshop changes shape. It shares the relations modal's trigger for the picker specifically: both pickers unify when either gets its specs |
| 10 | One e2e flow — author a two-level template, copy it into two doors, see the subtree under both, then edit the template and watch the copies **not** change | `Frontend/quran-dashboard-ui/e2e/` | Same trigger as row 9; it is the only check that would catch a detachment regression end to end, and detachment is the cell this feature is most likely to have misunderstood |

Row 7 is the one with no cover **anywhere**. Row 10 is the cheapest thing that would cover the
most: it crosses the template writes, the deep copy, the doors read, and detachment in one pass.

**Not debt, and not deferrable:** the `abwab-door-fields-form` extraction is covered by
`abwab-door-modal.component.spec.ts` running green **unchanged** (11/11) — the extraction
preserved every `data-testid` through a `testIdPrefix` input precisely so that spec keeps
pinning the behavior it always pinned. The form has no spec of its own, and does not need one
while that remains true.
