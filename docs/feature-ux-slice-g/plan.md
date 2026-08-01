# Slice G — Templates (UX audit)

Source: `docs/abwab-ux-audit.md` "Slice G — Templates" (`:1116-1126`) — item 20
(`:716-834`) and item 21 (`:836-884`). Item 21(b) (`qd-context-menu` extraction) already
shipped in Slice A and is **composed**, not rebuilt; only 21(a) is open. The audit isolated
this slice because item 20 is not an alignment fix but a **contract change**: it replaces the
axiom of an open feature's plan, reshapes an exception payload, moves an existing route's
response semantics, and rewrites four recorded rationales.

**Mode when this plan was written:** plan-only. No code, no docs, no `plan.md` amendment, no
Git action. Everything below is scheduled, nothing is done.

**Slice F status at plan time:** merged. `ux-slice-f-sections` merged into `dev`; ancestry
checked at plan time — the section reorder route
(`Controllers/Abwab/AbwabSectionsController.cs:81`), `EfAbwabSectionsWriter.ReorderAsync`
(`Persistence/Writes/Abwab/EfAbwabSectionsWriter.cs:79`), and `abwab-toolbar`'s
`.qd-tabs__count` call-site (`abwab-toolbar.component.html:14,31`) are all present on `dev`.
This plan is measured
against `dev` (`79d3501c`, clean). **The F-DEPENDENT fact list is empty** — nothing in Slice G
consumes a Slice F primitive. One F precedent is reused as a *shape* and named where it lands
(§4.1-7: a slice that changes an existing route's contract runs the route-smoke tier even
though no route is added).

## Precondition — VERIFIED on `dev` (`79d3501c`, clean) at plan time

| Consumed primitive / mechanism | Where it lives | Verified |
|---|---|---|
| Slices A–F merged to `dev` | `dev` tip `79d3501c` | ✅ |
| **The open feature whose axiom this reverses** | `docs/feature-abwab-templates/plan.md` (1098 lines); root `CLAUDE.md` "Active Spec Kit Feature" is currently `None` — the feature's plan folder is live and unswept | ✅ amended here, **not** swept (§3) |
| … the axiom itself, §5.1 "The one sentence everything derives from" | `docs/feature-abwab-templates/plan.md:158-163` | ✅ verbatim, quoted in §5 below |
| … "Every matrix cell in §6 is a consequence of that sentence plus the doors' own write invariants" | same section, `:163` | ✅ **this is why Phase 1 re-derives §6 rather than patching cells** |
| … §4 locked decision "Apply" | `plan.md:116` | ✅ |
| … §4 locked decision "Apply collision" | `plan.md:123` | ✅ |
| … route table row 5 ("`201` created root doors") | `plan.md:140` | ✅ |
| … §5.5 and its conclusion "**the only collision an apply can hit is at the root**… and it is the only `409` the apply route can produce" | `plan.md:232-249`, conclusion at `:247-249` | ✅ **does not survive** — rewritten, not annotated |
| … §6.1 anchor cell keyed to the root's name | `plan.md:330` | ✅ re-keyed |
| … §6.3 deep-copy cells that inherit it (empty template, same-template-twice) | `plan.md:359-371` | ✅ |
| … §5.2 one-root-per-template index and "the template's name is the root node's name" | `plan.md:165-183` | ✅ **survives, untouched** (§5 ledger) |
| … "no template application at root level… `400`" — about **target** doors | `plan.md:87-88`, `:337` | ✅ **survives, untouched** |
| … §9 traps (§9 begins at `plan.md:878`): `AbwabTemplateRootNodeException` covers reorder **and** delete, do not split (`:896`); the `23505` helper is the inverse of the relations case, so still pre-check up front (`:904`); do not guard the concurrent-apply `order_value` race (`:912`) | `docs/feature-abwab-templates/plan.md:896`, `:904`, `:912` | ✅ all three honored (§4.1-3, §4.2-4, §4.2-9) |
| … §8 testing posture (no new tests, parity entries mandatory, existing suites run, one debt row per gap) | `plan.md:857-875` | ✅ the posture Slice G continues (§4.1-8) |
| **The apply writer, in full** — the one file item 20 rewrites | `Backend/infrastructure/…/Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs` (201 lines) | ✅ read end to end |
| … `<remarks>` claiming no global-order work / no resequencing / no per-node section resolution | `EfAbwabTemplateApplyWriter.cs:7-14` | ✅ **all three survive the reversal**; only the sentence introducing them changes (§5 ledger) |
| … `rootNode` located as the sole `ParentNodeId is null` node | `:42-43` | ✅ still located; it just stops being copied |
| … the "one created root per target" ordering comment | `:55-56` | ✅ amended |
| … the collision pre-check, keyed on `d.Name == rootNode.Name`, with its "Named up front so the 409 can say WHICH targets collided; 23505 names no row" comment | `:64-77` | ✅ the rule extended, not replaced |
| … `childrenByParentNode`, keyed by **node id**, children ordered `(OrderValue, Id)` | `:79-82` | ✅ **the BFS descent needs no change** — only its seed does (§4.2-6) |
| … `nextOrder` = the target's live child count + 1, computed per target | `:87-88,93-94` | ✅ becomes the base of the level-1 offset |
| … one `copiedRoot` per target, seeded into `createdRoots` | `:96-99` | ✅ the seed this slice replaces |
| … the level-by-level descent with children's **verbatim** `OrderValue` | `:107-134`, comment at `:118-120` | ✅ **survives verbatim at depth ≥ 2** (§4.2-5) |
| … `AddAliases(copied.Door.Id, copied.Node.Aliases, now)` — **per node**, called for every level including the seed | `:110` | ✅ the alias **rows** are already correct at any seed; see DRIFT-1 for the DTO |
| … the return block computing **one** `rootAliases` and stamping it onto every DTO | `:136-152`, `rootAliases` at `:137` | ✅ **correct today only because every seed wraps the same node** — DRIFT-1 |
| … `NewDoor` (section inherited from the target at every depth, `GlobalOrderValue` stays null) | `:154-170` | ✅ unchanged |
| … `SaveTranslatingDuplicateNameAsync` — the `23505` race backstop throwing with an empty list | `:174-201`, throw at `:197` | ✅ shape kept, payload type changes with the exception |
| … the enclosing transaction is what makes the batch all-or-nothing | `:29`, commit at `:136` | ✅ unchanged |
| `AbwabTemplateApplyCollisionException` — `IReadOnlyList<string>` of **target** names | `Application.Abstractions/Abwab/AbwabTemplateApplyCollisionException.cs:3-8` | ✅ payload reshaped (§4.2-9) |
| `AbwabTemplateRootNodeException` — one type, two refusals (reorder + delete) | `Application.Abstractions/Abwab/AbwabTemplateRootNodeException.cs:3-6` | ✅ **not split**; its rationale gains a second reason (§5 ledger) |
| `IAbwabTemplateApplyWriter` — "Returns the created ROOT door per target" | `Application.Abstractions/Abwab/IAbwabTemplateApplyWriter.cs:10-11` | ✅ comment amended; **signature and return type unchanged** |
| `ApplyTemplateOutcome` — six variants, `Collision(IReadOnlyList<string> DoorNames)` | `Application/Abwab/Commands/Templates/ApplyTemplate/ApplyTemplateOutcome.cs:5-15` | ✅ one variant added, one payload retyped |
| `ApplyTemplateHandler` — four `catch` arms mapping exceptions to outcomes | `…/ApplyTemplate/ApplyTemplateHandler.cs:33-52` | ✅ a fifth arm added |
| `ApplyTemplateBody(IReadOnlyList<int>? TargetDoorIds)` — **request shape unchanged by this slice** | `…/ApplyTemplate/ApplyTemplateCommand.cs:3-7` | ✅ (§4.2-2) |
| The apply route and its six status mappings | `Controllers/Abwab/AbwabTemplatesController.cs:84-112` (`[HttpPost("templates/{templateId:int}/apply")]` at `:84`) | ✅ one arm added, one message call retyped |
| Apply `ApiMessages`: `AbwabTemplateApplied`, `…ApplyNoTargets`, `…ApplyTargetArchived`, `…ApplyCollision`, the private prefix, and `AbwabTemplateApplyCollisionWith(...)` | `api/QuranDashboard.Api/Common/ApiMessages.cs:179-191` | ✅ two rewritten, one added (§4.2-10) |
| **`AbwabTemplateSummaryDto.NodeCount` excludes the root** — the DTO's own comment | `Application.Abstractions/Abwab/Responses/AbwabTemplateSummaryDto.cs:3-5` | ✅ |
| … and the reader that populates it counts **every live node with a parent, at every depth** | `Persistence/Reads/Abwab/EfAbwabTemplatesReader.cs:23-24` (`ParentNodeId != null && DeletedAtUtc == null`) | ✅ **this is the contradiction's resolution — §5.3** |
| … the frontend VM derives the same number independently, by walking the built tree | `models/abwab-templates.models.ts:67-88` (`descendantCount`, returned as `nodeCount` at `:88`); VM field documented at `:24-25` | ✅ same semantics, second source |
| … and the copy modal reads **that** VM number, not the summary row | `abwab-templates-page.component.html:224` (`facade.selectedTemplate()?.nodeCount ?? 0`); `abwab-templates.facade.ts:56-61` returns `buildAbwabTemplateTree(dto)` | ✅ |
| `SmokeRouteCatalog` apply entry — `POST`, `{templateId:int}`, expects `404`, `ParityOnly = true` | `Tests/Smoke/SmokeRouteCatalog.cs:356-359` | ✅ **the reversal moves none of its four fields** — DRIFT-3 |
| `SmokeCoverageParityTests` keys on `"<METHOD> <template>"` with route constraints part of the key, both directions asserted | `Tests/Smoke/SmokeCoverageParityTests.cs:10-35,63-68` | ✅ no route added ⇒ no new entry owed |
| `shared/ui/context-menu/` — the Slice A primitive item 21(a) composes | `shared/ui/context-menu/context-menu.component.ts` (28 lines), `.html` (10 lines) | ✅ |
| … its contract: `position: {x, y}`, `menuTestId`, `backdropTestId`, `dismissed`; items are projected `<ng-content>` | `context-menu.component.ts:14-18`; `.html:1-10` | ✅ **the `{x, y}` anchor contract already matches** `AbwabTemplateNodeMenuRequest` |
| … Escape is **document-level**, because no open path puts focus inside the menu | `context-menu.component.ts:20-27` | ✅ still true after 21(a) — the keyboard path leaves focus on the row's control (§4.2-13) |
| … the two gaps §17 deliberately left open: **no viewport clamping**, **no focus management into the menu** | `.architecture/UI_STYLE_SYSTEM.md` `qd-context-menu` entry, `:1046-1073`; recorded in `docs/feature-ux-slice-a/plan.md` phase 6 T604 | ✅ **both stay open** (§3) |
| The templates page already composes it, with the root-vs-node item swap kept page-side | `abwab-templates-page.component.html:233-255`; `contextMenuNodeId` / `contextMenuPosition` / `onMenuRequested` at `abwab-templates-page.component.ts:71-72,255-261` | ✅ **the page side of 21(a) is already built** — only the tree's two new emit paths are missing |
| `AbwabTemplateNodeMenuRequest { nodeId, x, y }` and `menuRequested` | `abwab-template-tree.component.ts:6-10`, `:44` | ✅ reused unchanged |
| … emitted from **one** path today: the `⋯` button's click | `abwab-template-tree.component.ts:104-106`; `.html:61-69` | ✅ the gap item 21(a) closes |
| … the template tree's rows are `<div role="listitem">` with **no `tabindex`**; only chevron / `＋` / `⋯` are focusable | `abwab-template-tree.component.html:3-10`, `:12-26`, `:52-69` | ✅ **there is no focus model to anchor to** — §4.2-13 names the mechanism |
| … each row already carries `[data-testid]="'abwab-template-tree-row-' + row.node.id"` | `abwab-template-tree.component.html:9` | ✅ the anchor handle, no new attribute needed |
| … and the component's own doc records why it is a list, not `role="tree"` | `abwab-template-tree.component.ts:26-28` | ✅ amended in the same change (§5 ledger) |
| **The doors tree's four menu paths — the parity target** | see the three rows below | ✅ |
| … right-click: `(contextmenu)` on the row, `onRowContextMenu` with `preventDefault` | `abwab-tree.component.html:17`; `abwab-tree.component.ts:193-199` | ✅ |
| … `⋯` click | `abwab-tree.component.ts` (`onMoreClick` → `openMenuFor`) | ✅ already present in the template tree |
| … keyboard `ContextMenu`/`Shift+F10`, anchored to the focused row's `getBoundingClientRect()`, with "a menu pinned at (0,0) is not a usable keyboard path" | `abwab-tree.component.ts:317-325`; `rowElement` at `:349-351` | ✅ **the anchor pattern copied**; the roving-tabindex model behind it is **not** (§3) |
| … the doors tree earns `role="tree"` with a full RTL keyboard model | `abwab-tree-keyboard.controller.ts`; recorded at `abwab-tree.component.ts:30-34` | ✅ the reason the workshop still does not claim the role |
| The copy modal — every string it renders and where each comes from | `abwab-template-copy-modal.component.ts:59-72`; `.html:18`, `:26-29`, `:56-59` | ✅ |
| … `templateCopyDescription` («بجذره وكل فروعه») | `models/abwab.labels.ts:351` | ✅ rewritten |
| … `templateCopyPreview(name, count)` («سيكسب ابنًا جديدًا… وبداخله N عنصرًا») | `models/abwab.labels.ts:352-353` | ✅ rewritten; **the number is kept** (§5.3) |
| … `templateCopyPreviewNoRoot`, `templateCopyPreviewDetached`, `templateCopyConfirmButton(count)` (counts **targets**) | `models/abwab.labels.ts:354`, `:357`, `:362-363` | ✅ **all three untouched** (§5 ledger) |
| … the recorded preview contract in the component's class doc | `abwab-template-copy-modal.component.ts:15-24` | ✅ amended |
| … the success path **ignores the response payload** | `abwab-template-copy-modal.component.ts:177-187` | ✅ so DRIFT-1 has no visible frontend symptom — which is why it must be fixed deliberately |
| … `ELEMENT_FORMS` + `countPhrase`, the Arabic counted-noun helper the preview already uses | `models/abwab.labels.ts:46`, `:24` | ✅ reused; no new form set needed |
| … the modal's spec asserts labels **by reference** (`ABWAB_LABELS.templateCopyEmptyDoors`, `…pickerNoMatches`) and never the preview or description strings | `abwab-template-copy-modal.component.spec.ts:145-177`; testids used listed at `:66-188` — `-preview` and the description appear in **neither** | ✅ **the copy rewrite cannot break it** (§7) |
| Frontend apply wiring: api → controller → page | `data-access/abwab-templates.api.ts:43-45`; `state/abwab-templates.controller.ts:71-76`, refresh at `:106,120` | ✅ unchanged by this slice |
| … and the templates controller is deliberately **not** `AbwabWriteController` | `features/abwab/README.md:629-635` | ✅ no 409 policy is forked or shared here |
| Existing gates for the touched surfaces | `abwab-template-copy-modal.component.spec.ts`, `abwab-templates.facade.spec.ts` (3 cases) | ✅ |
| **No spec exists** for `abwab-template-tree`, `abwab-templates-page`, `abwab-templates.api.ts`, or `abwab-templates.controller.ts`; **no templates e2e exists** | `components/abwab-template-tree/` (3 files, no `.spec.ts`); `pages/abwab-templates-page/` (3 files, no `.spec.ts`); `e2e/` has no templates flow | ✅ measured, not inherited — drives §7 |
| `TESTING_DEBT.md` row 7 **is this writer**, and is the file's self-declared highest-value row; its trigger is "the next change to the apply path" | `docs/TESTING_DEBT.md` (`abwab-templates` section, row 7) | ✅ **this slice fires that trigger** (§7) |
| `TESTING_DEBT.md` row 9 already names `abwab-template-tree/` and `pages/abwab-templates-page/` | same file, row 9 | ✅ widened, not duplicated (§7) |
| `TESTING_DEBT.md`'s own rule: parity entries are **not** debt-able; tiers the strategy requires are never deferred here | `docs/TESTING_DEBT.md:11-16` | ✅ |
| Route-smoke tier is required for **contract** changes on an existing route, with an explicit `Tests.Smoke.Data` RAN/SKIPPED statement | `TESTING_STRATEGY.md` §3 Tier A/C, §4, §10; `Backend/CLAUDE.md` "Backend Test Selection" | ✅ (§4.1-7) |
| The validated commands: no-pipeline (1,086 / ~21 s), smoke (140 / ~52 s), `Tests.Api` (60 / ~10 s) | `TESTING_STRATEGY.md` §5 | ✅ |
| The validated frontend commands and the Vitest fork cap (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`, baked into `npm test`) | `TESTING_STRATEGY.md` §6 | ✅ |
| Contract regeneration chain: `Backend/scripts/export-swagger` → `npm run generate:api` → `npm run docs:api`; `Backend/scripts/check-api-contract` runs all three and fails on `git diff --exit-code` | `Backend/scripts/README.md`; `Frontend/quran-dashboard-ui/package.json` | ✅ |
| Backend READMEs carrying apply claims | `Persistence/Writes/Abwab/README.md:20-23`, `:44-46`, `:47-49`, `:179-199`, `:224-229` | ✅ named per line in §5 ledger |
| … and the read README's descendant-count claim, which says nothing about apply | `Persistence/Reads/Abwab/README.md:99-101` | ✅ **verify-only, no amendment owed** |
| Frontend README paragraphs carrying apply / workshop-tree claims | `features/abwab/README.md:17-25`, `:657-666`, `:677-684`, `:733` | ✅ named per line in §5 ledger |
| The design-preview concept whose «بجذره» copy the reversal supersedes | `docs/design-preview/abwab-templates-concept.html:139`, `:145`; cited as a contract at `features/abwab/README.md:733` and at `plan.md:167-168` | ✅ **recorded as superseded, not edited** (§4.2-16) |

### DRIFT — where current code contradicts the audit or this commission

| # | The audit / commission says | `dev` at `79d3501c` says | This plan follows |
|---|---|---|---|
| DRIFT-1 | The audit's ripple list (`:781-820`) enumerates five ripples and stops. **Aliases are not among them.** | `EfAbwabTemplateApplyWriter.cs:137` computes **one** `rootAliases` and stamps it onto every returned DTO (`:151`). Correct today only because every entry in `createdRoots` wraps the same `rootNode`. Post-reversal that list holds **N distinct level-1 children**, each with its own `Node.Aliases`, so every DTO would report the root's aliases instead of its own. It compiles, and the modal ignores the payload (`abwab-template-copy-modal.component.ts:177-187`), so nothing breaks visibly. | **Scheduled as T304, not footnoted.** The fix is `copied.Node.Aliases` inside the `Select`; `CopiedNode` already carries `.Node` (`:17`). Stated precisely because half of it is a non-issue: the **alias rows** are already right — `AddAliases(copied.Door.Id, copied.Node.Aliases, now)` (`:110`) is per-node and runs for the seed level too, so the database is correct at any seed and only the response payload lies. This is the exact class decision 5 exists for: silent, compile-clean, contract-lying. |
| DRIFT-2 | Audit `:831` Fix/Size: the component needs `templateNodeCount - 1`. | `EfAbwabTemplatesReader.cs:23-24` counts every live node with `ParentNodeId != null` — **all depths, root excluded** — and `buildAbwabTemplateTree` (`abwab-templates.models.ts:67-88`) derives the VM's `nodeCount` the same way. Post-reversal, apply creates exactly that many doors per target. | **No arithmetic change. Subtracting one would introduce an off-by-one.** The audit contradicts itself here: its own `:765-770` says the reversal makes the existing number correct, and the code agrees. Only the prose around the number lies. Full resolution in §5.3. |
| DRIFT-3 | Commission decision 7: "the `SmokeRouteCatalog` entry exists but **its expectations move**". | `SmokeRouteCatalog.cs:356-359` is `new("api/abwab/templates/{templateId:int}/apply", "/api/abwab/templates/1/apply", HttpStatusCode.NotFound) { Method = HttpMethod.Post, ParityOnly = true }`. The reversal changes none of those four fields: same verb, same template, same constraint, and an **unknown** template id still answers `404` — the empty-template `400` is reachable only for a template that exists. | **The entry is verify-only; no catalog edit is scheduled.** The *tier* still runs and is not optional (§4.1-7) — the route's response semantics change even though its parity key does not. Recorded so nobody schedules a phantom edit, and so the reviewer does not read a missing catalog diff as a missing obligation. |
| DRIFT-4 | Commission: item 21(a) is "right-click + the `ContextMenu`/`Shift+F10` path **anchored to the focused row's `getBoundingClientRect()`**", mirroring the doors tree. | The doors tree anchors via `rovingId()` → `rowElement(id)` (`abwab-tree.component.ts:317-325,349-351`), which exists because that tree has a roving-tabindex focus model. The **template tree has no focus model at all**: rows are `<div role="listitem">` with no `tabindex` (`abwab-template-tree.component.html:3-10`); only the chevron, `＋`, and `⋯` buttons are focusable. There is no "focused row" to read. | **The anchor pattern is copied; the focus model is not.** `(keydown)` binds on the **row div**, catching the key as it bubbles from whichever of the row's own controls has focus, and `@for` supplies the row id; the anchor is `rowElement(id)?.getBoundingClientRect()` against the existing `[data-testid="abwab-template-tree-row-<id>"]` (`.html:9`). Row stays a non-tab-stop, `role="tree"` stays unclaimed, `{ nodeId, x, y }` is reused unchanged. Mechanism fixed in §4.2-13, README amendment in §5. |

## 0. Guard result

Task arithmetic: Phase 1 = 4, Phase 2 = 4, Phase 3 = 4, Phase 4 = 1, Phase 5 = 1,
Phase 6 = 3, Phase 7 = 3, Phase 8 = 3, Phase 9 = 3. **26 tasks — under the 30-task
threshold. One slice, no split.**

Recorded so a mid-execution split does not get drawn on task count: if this slice had split,
the seam is **after Phase 5** — "the contract" (Phases 1–5: the axiom amendment, the exception
payload, the writer, the route gate, the regeneration check) versus "the surfaces" (Phases 6–7:
the preview copy, the empty-template affordance, the two menu paths). The seam is **contract vs
UI**, deliberately *not* Slice F's who-can-be-hurt test: that test does not discriminate here,
because both halves are unpinned — the apply writer has no backend behavior test
(`TESTING_DEBT.md` row 7) and the workshop tree and page have no spec at all. Contract-vs-UI is
the honest seam: Phases 1–5 change what the API means, Phases 6–7 change what the user reads and
presses.

## 1. Objective

| # | Deliverable | Home | Audit item |
|---|---|---|---|
| 1 | The open feature's plan tells the truth: §5.1's axiom replaced, §6 **re-derived from the new sentence** rather than patched cell by cell, §5.5 rewritten, §4 and the route table amended | `docs/feature-abwab-templates/plan.md` | 20 (the reversal's primary document) |
| 2 | The apply copies the root's **direct children** as new children of each target, recursively, and never copies the root | `EfAbwabTemplateApplyWriter.cs` | 20 |
| 3 | Level-1 children append at `nextOrder + i`; depth ≥ 2 keeps its verbatim `OrderValue`; **every touched scope stays `1..N`** | same | 20 (ripple 2) |
| 4 | An empty-root template is **refused `400`** with «القالب لا يحتوي عناصر لنسخها» — writer-side, authoritative | new exception + outcome + `ApiMessages` constant | 20 (ripple 4) |
| 5 | Collision becomes **per-child-name under each target**: the pre-check compares the root's direct child names against each target's live child names, and the `409` names **(target, child)** pairs | exception payload, outcome, `ApiMessages`, writer pre-check | 20 (ripple 1) |
| 6 | Every returned DTO carries **its own node's** aliases — DRIFT-1, the ripple the audit missed | `EfAbwabTemplateApplyWriter.cs:137,151` | 20 (found here) |
| 7 | The response's **meaning** is written down where it can be read: one door per target becomes N per target, with an unchanged type | `IAbwabTemplateApplyWriter.cs`, `Writes/Abwab/README.md`, `features/abwab/README.md` | 20 (ripple 3, decision 5) |
| 8 | The copy modal states the new contract **before** the write: rewritten description and preview, the element count kept as-is, and an empty-template affordance that stops the confirm button promising copies it cannot produce | `models/abwab.labels.ts`, `abwab-template-copy-modal.component.*` | 20 (ripple 5) |
| 9 | The workshop tree emits `menuRequested` from **three** paths, not one: `⋯`, right-click, and `ContextMenu`/`Shift+F10` — composing the shipped `qd-context-menu` and reusing `{ nodeId, x, y }` unchanged | `abwab-template-tree.component.{ts,html}` | 21(a) |
| 10 | The README paragraph on the workshop tree's list role says **precisely** which keyboard path was added and why the role is still not claimed | `features/abwab/README.md:677-684`; component doc `abwab-template-tree.component.ts:26-28` | 21(a) |
| 11 | Route-smoke tier run with an explicit `Tests.Smoke.Data` RAN/SKIPPED statement, and `check-api-contract` clean | `docs/feature-ux-slice-g/evidence.md` | 20 (contract change on an existing route) |
| 12 | Docs true again in the same change: two backend READMEs, the frontend abwab README, §17's `qd-context-menu` entry (verify-only unless a gap moved), and the `TESTING_DEBT.md` rows this posture owes — including restating row 7, whose trigger this slice fires | seven files, named in §9 | repo law |

## 2. Scope

**In:**

- **Planning artifact (amended, not swept)**
  - `docs/feature-abwab-templates/plan.md` — §5.1, §4 (`:116`, `:123`), route table (`:140`), §5.5 (`:232-249`, rewritten), §6.1 (`:330`), the §6.3 cells that inherit it, and §9's apply traps.
- **Backend**
  - `application/QuranDashboard.Application.Abstractions/Abwab/AbwabTemplateApplyCollisionException.cs` — payload retyped.
  - `application/QuranDashboard.Application.Abstractions/Abwab/AbwabTemplateApplyCollisionPair.cs` — new, the `(TargetName, ChildName)` pair.
  - `application/QuranDashboard.Application.Abstractions/Abwab/AbwabTemplateEmptyException.cs` — new.
  - `application/QuranDashboard.Application.Abstractions/Abwab/IAbwabTemplateApplyWriter.cs` — the `//` contract comment only.
  - `application/QuranDashboard.Application/Abwab/Commands/Templates/ApplyTemplate/ApplyTemplateOutcome.cs` — one variant added, `Collision`'s payload retyped.
  - `…/ApplyTemplate/ApplyTemplateHandler.cs` — one `catch` arm.
  - `api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs` — one `switch` arm, one message call.
  - `api/QuranDashboard.Api/Common/ApiMessages.cs` — one constant added, two rewritten, the formatter retyped.
  - `infrastructure/…/Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs` — pre-check, empty guard, seed, level-1 offset, alias DTO fix, `<remarks>` and two comments.
- **Frontend**
  - `features/abwab/models/abwab.labels.ts` — two strings rewritten, one added.
  - `features/abwab/components/abwab-template-copy-modal/` — `.ts` / `.html` (+ `.scss` only if the empty-template block needs a rule; the `qd-state` primitive is preferred).
  - `features/abwab/components/abwab-template-tree/` — `.ts` / `.html`.
- **Docs (same change, repo law)** — `Backend/…/Persistence/Writes/Abwab/README.md`,
  `Backend/api/QuranDashboard.Api/Controllers/README.md` (verify-only: no route added or removed),
  `Backend/…/Persistence/Reads/Abwab/README.md` (verify-only), `features/abwab/README.md`,
  `.architecture/UI_STYLE_SYSTEM.md` §17 (verify-only unless a `qd-context-menu` gap moved),
  `docs/TESTING_DEBT.md`, `docs/feature-ux-slice-g/evidence.md`.
- **Contract artifacts** — `Frontend/quran-dashboard-ui/openapi/swagger.json`,
  `src/app/core/api/generated/`, `docs/api-reference/` — **regenerated and expected to be a
  no-op** (§4.2-3); if the diff is non-empty the diff is the deliverable.

**Out (named so nobody "finishes the thought"):**

- **Any migration, any schema change.** No column, index, or table is added, dropped, or backfilled. The `(template_id, parent_node_id, name)` and `(section_id, parent_id, name)` unique indexes are both consumed exactly as they stand.
- **The request shape.** `ApplyTemplateBody(IReadOnlyList<int>? TargetDoorIds)` is unchanged, and no `version` is added to any templates route (`plan.md` §5.4).
- **`role="tree"` on the workshop tree, and the RTL arrow-key navigation model.** The README's recorded reason stands (§3).
- **A row context menu for `abwab-cards`.** The audit flags its absence at `:882-884`; recorded as an open decision for a later slice (§4.2-15), not scoped.
- **Viewport clamping and focus management into `qd-context-menu`.** Slice A's two recorded open gaps stay open, and §17 keeps saying so.
- **Splitting `AbwabTemplateRootNodeException`.** `plan.md` §9 forbids it; the new empty-template refusal gets its own type instead (§4.2-8).
- **A guard on the concurrent-apply `order_value` race.** `plan.md` §6.4 and §9 both forbid it; the level-1 offset is a different, deterministic concern (§4.2-5).
- **A template↔copy link, a provenance column, a badge, or an "update all copies" path.** Detachment is untouched.
- **Unioning the target count.** `templateCopyConfirmButton` still counts targets.
- **Template-list reorder, `AbwabTemplate` order columns, template restore, an archived-templates view.**
- **`EfAbwabDoorsWriter`'s 816 > 600 refactor debt** — this slice adds no line to that file.
- **Caching, ETags, snapshot-version reuse** — Slice I owns it, last. §8 carries one forward-looking note; nothing here is designed *for* it.
- **Slice H (navbar) work of any kind.**
- **Any planning-artifact sweep or N-2 deletion** — deferred to the single cleanup pass after Slice I. `docs/feature-abwab-templates/` is **amended here and swept never** (§3).
- **Any `dev → main` merge.**

## 3. Non-goals

- **No planning-artifact sweep in this slice — standing user decision.** ALL planning-folder
  sweeps and N-2 evictions are deferred to one cleanup pass after Slice I. **The asymmetry is
  the point and must not be smoothed:** `docs/feature-abwab-templates/` is **amended** in this
  slice (Phase 1, mandatory) and **not deleted, not repointed, not evicted**. Amending a live
  planning document is repo law; deleting one is a deferred chore. Nothing here deletes any
  planning folder. **Not deferred either:** same-change README and §17 amendments for behavior
  this slice changes (§1 row 12, §9).
- **No new test suites, per the rush-period posture** (`plan.md` §8, continued) — existing
  suites still RUN before merge, parity one-liners stay mandatory, and every gap becomes a row
  in `docs/TESTING_DEBT.md` in the same change (§7). **The route-smoke tier is not optional
  here** and is not debt-able: `TESTING_DEBT.md:11-16` says so in its own words.
- **No `role="tree"`, no arrow-key model.** `features/abwab/README.md:677-684` records why:
  *"claiming the role without the arrow-key model would promise a navigation contract the
  workshop does not implement."* Adding a menu key does not license the role — the commission
  says so, the README says so, and §4.2-13's mechanism is chosen precisely so the row never
  becomes a tab stop.
- **No `qd-context-menu` changes.** The primitive is **composed**, not extended. Its two
  recorded gaps stay open; closing either would change keyboard behavior on the doors page too,
  which is not this slice's scope.
- **No caching design.** The apply still refreshes nothing on the doors side and the workshop
  still refetches its own list; §8 carries the one forward-looking note as a risk, not as a
  design accommodation.
- **No litigation of the reversal.** It is recorded and implemented. §5 states the old sentence
  and the new one side by side so the diff is legible; nowhere does this plan argue the case.

## 4. Locked decisions

### 4.1 Carried in from the audit / the commission / prior slices / standing rules

1. **Children-only apply.** The root is a naming/container row that is never copied. Stated as
   the new axiom in §5.1 and re-derived through §6 (Phase 1).
2. **Empty-root template ⇒ refuse `400`** with «القالب لا يحتوي عناصر لنسخها». Not a silent
   no-op. **Writer-side is authoritative**; any modal-side affordance is a courtesy (§4.2-11).
3. **Collision becomes per-child-name under each target**, with a `(target, child name)`
   payload. **Both halves of `plan.md` §9's locked shape survive:** the pre-check names the
   offenders, and `23505` stays the nameless race backstop. All-or-nothing inside one
   transaction.
4. **Level-1 ordering gets an offset** (`nextOrder + i`); deeper levels keep their verbatim
   `OrderValue`. Every touched scope stays `1..N` — the sections and doors reorder paths depend
   on it. `plan.md` §9's decision **not** to guard the concurrent-apply `order_value` race
   stands unchanged; this offset is a different, deterministic concern.
5. **Response type stays `IReadOnlyList<AbwabDoorDto>`**; its meaning changes from one door per
   target to N per target. Nothing breaks at compile time — which is exactly why it is written
   into the interface comment, the writer's `<remarks>`, and both READMEs.
6. **Item 21(a) is the menu key only.** `(contextmenu)` with `preventDefault`, plus
   `ContextMenu`/`Shift+F10`. No `role="tree"`, no arrow-key model.
7. **The route-smoke tier is required** — response semantics change on an existing route — and
   the evidence must state whether `QuranDashboard.Tests.Smoke.Data` RAN or SKIPPED. That the
   catalog entry itself does not move (DRIFT-3) does not weaken this.
8. **Rush-period testing posture:** no new suites; existing suites run before merge; parity
   one-liners mandatory; gaps become `docs/TESTING_DEBT.md` rows in the same change.
9. **Same-change README + §17 amendments are repo law and in scope**; all planning-artifact
   sweeps stay deferred to the post-Slice-I pass (§3).

### 4.2 Decided by this plan

1. **Phase 1 amends §5.1 first and then re-derives the whole of §6 — it does not patch cells.**
   The plan's own sentence (`plan.md:162`) makes §6 a function of §5.1, so re-running the
   derivation is the only method that finds cells the audit did not enumerate. Two such cells
   are already visible from the reads (§6a rows *empty-root template* and *single-child
   template with a colliding name*); the re-derivation exists to find the rest.
2. **The request shape does not change.** `ApplyTemplateBody` stays
   `IReadOnlyList<int>? TargetDoorIds`. Nothing about children-only apply needs a new field, and
   `plan.md` §5.4 forbids adding a version token.
3. **Contract regeneration is scheduled as a verification expecting a clean result, not as an
   edit.** The request shape is unchanged, the response *type* is unchanged, and the controllers
   carry no `[ProducesResponseType]`, so the new `400` adds nothing to the OpenAPI document. T501
   runs `Backend/scripts/check-api-contract` and expects it clean. **If it is not clean, the diff
   is the deliverable** and lands in that commit.
4. **The collision pre-check is one query, keyed on the child-name set.** Read the root's direct
   child names from `childrenByParentNode[rootNode.Id]` (already built at
   `EfAbwabTemplateApplyWriter.cs:79-82`), then one `AsNoTracking` read of live doors whose
   `ParentId` is in `targetIds` **and** whose `Name` is in that set, projecting
   `{ ParentId, Name }`. Map each hit onto its target's name. This extends the existing
   pre-check's rule (`:64-65`) rather than replacing it.
5. **The `409` message's ordering is fixed, not incidental.** The writer already pins target
   order to the **caller's** order (`:53-56`). With pairs, that is no longer sufficient — a
   target can contribute several names. The second rule: **caller's target order, then the
   template's own sibling order for the names under each target.** Without it the `409` text is
   nondeterministic across runs for the same input, which makes the message untestable and the
   bug report unreproducible.
6. **The BFS descent loop is not touched — only its seed.** `childrenByParentNode` is keyed by
   **node id** (`:79-82`), so the level-by-level walk (`:107-134`) is already generic over
   whatever seeds it. The change is: instead of `createdRoots` holding one `CopiedNode(copiedRoot,
   rootNode, …)` per target, it holds one per **(target, root's direct child)**, at
   `OrderValue = nextOrder + i` where `i` is the child's index in the `(OrderValue, Id)`-ordered
   list. Naming this precisely keeps the executor out of the descent loop, where the verbatim-
   `OrderValue` comment (`:118-120`) is still correct and must survive.
7. **The empty-root guard fires immediately after `childrenByParentNode` is built, before the
   target reads.** Consequence, and it is a matrix cell, not an accident: **an empty template
   applied to an archived target returns the empty-template `400`, not the archived-target
   `400`.** Both are `400` with different Arabic messages, and both defend. This order is chosen
   because the template's emptiness is a property of the template alone — it does not depend on
   which doors were picked, so refusing it before reading the targets is the cheaper and more
   honest refusal. Recorded as a cell in §6a.
8. **The empty-root refusal gets its own exception type, `AbwabTemplateEmptyException`.**
   `plan.md` §9 forbids *splitting* `AbwabTemplateRootNodeException` (which covers reorder and
   delete of the root) — it does not require overloading it with a third, unrelated refusal
   raised by a third handler. A new type is the clean call; stated explicitly so the executor
   does not read §9 as blocking it.
9. **`AbwabTemplateApplyCollisionException` carries
   `IReadOnlyList<AbwabTemplateApplyCollisionPair>`,** where `AbwabTemplateApplyCollisionPair`
   is a `sealed record (string TargetName, string ChildName)` in
   `Application.Abstractions/Abwab/`, and the exception's property renames `DoorNames` →
   `Collisions` (a list of pairs under a name meaning door names would lie). Two parallel
   `string` lists would let the pairing drift; a tuple would give the API message no names to
   read. The `23505` backstop still throws with an **empty** list, unchanged in spirit (`:197`).
   **The `Pair` suffix is load-bearing, not decoration:** `ApiMessages` already has a `const
   string AbwabTemplateApplyCollision` (`:182`) and keeps that name (§4.2-10), so a type of the
   same name used in a signature *inside that class* binds the field first — CS0118, "is a
   field but is used like a type". Verified: `ApiMessages.cs` declares no `using` directives
   and `Abstractions.Abwab` is not in the Api project's `GlobalUsings.cs`, so retyping the
   formatter adds the first `using` that file has ever carried. Both facts belong in T301's
   diff, not in a compile error.
10. **Arabic copy, exactly.** New constant
    `AbwabTemplateApplyEmpty = "القالب لا يحتوي عناصر لنسخها"` (the commission's words,
    verbatim). `AbwabTemplateApplyCollision` (the nameless backstop) becomes
    `"يوجد باب بنفس اسم أحد عناصر القالب داخل الباب المستهدف"`. The prefix becomes
    `"لم يتم النسخ — أسماء موجودة داخل الأبواب المستهدفة"`, and each pair renders
    `«{TargetName}» ← «{ChildName}»`, joined by «، ». English identifiers, Arabic user-facing
    text, all of it in `ApiMessages.cs` — `API_GUIDELINES.md` unchanged.
11. **The modal's empty-template affordance is a courtesy and says so.** When
    `templateNodeCount() === 0`, the preview block is replaced by
    `templateCopyEmptyTemplate` and the confirm button is disabled. **The `400` remains the
    guarantee** — the workshop can reach the empty state (create writes only the root), and a
    stale list would otherwise let a disabled button and a live template disagree. The modal
    never claims to be the check.
12. **The element count is not touched — DRIFT-2.** `templateCopyPreview` keeps
    `this.templateNodeCount()` unchanged. Only the sentence around it is rewritten. Full
    reasoning in §5.3.
13. **Item 21(a)'s mechanism, fixed:** `(contextmenu)` and `(keydown)` both bind on the **row
    `<div>`** in `abwab-template-tree.component.html:3-10`. The keydown handler catches
    `ContextMenu` and `Shift+F10` as they bubble from whichever of the row's own controls
    (chevron / `＋` / `⋯`) has focus, calls `preventDefault`, and anchors at
    `rowElement(nodeId)?.getBoundingClientRect()` → `{ left, bottom }`, falling back to `(0, 0)`
    only if the element is missing — the doors tree's exact fallback (`abwab-tree.component.ts:324`).
    `rowElement` is a private `querySelector` on `[data-testid="abwab-template-tree-row-<id>"]`,
    the handle the template already renders (`.html:9`), copying `abwab-tree.component.ts:349-351`.
    **No `tabindex` is added to any row.** `qd-context-menu`'s document-level Escape
    (`context-menu.component.ts:24-27`) keeps working because this path, like the other three,
    leaves focus outside the menu.
14. **Right-click has no bulk-mode guard here.** The doors tree returns early in bulk mode
    (`abwab-tree.component.ts:195-197`); the workshop tree has no bulk mode, so importing the
    guard would be a branch that can never be taken.
15. **`abwab-cards`' missing row menu is recorded as an open decision, not scoped.** The audit
    flags it at `:882-884` and notes no README records the asymmetry as deliberate. This plan
    records it in `features/abwab/README.md` as an open question for a later slice and does
    nothing else with it — building it here would be a third menu consumer landing in a slice
    whose test posture cannot cover the first two.
16. **The design-preview concept is superseded, not edited.** `abwab-templates-concept.html:139,145`
    say the template is copied «كاملًا بجذره». `docs/design-preview/` is historical mockup
    material and is not rewritten. The supersession is recorded once, in
    `features/abwab/README.md`, beside the line that cites the concept as a design contract
    (`:733`) — because that README is what a future agent reads before trusting the mockup.

## 5. The ground truth this plan is derived from

### 5.1 The sentence being replaced

The open feature's plan, §5.1 (`docs/feature-abwab-templates/plan.md:158-163`), currently reads:

> **Applying a template inserts a copy of its root node as a NEW CHILD of each target door,
> and recursively copies that node's subtree beneath it.**
>
> Every matrix cell in §6 is a consequence of that sentence plus the doors' own write invariants.

The replacement, stated once, here, and copied verbatim into §5.1 in T102:

> **Applying a template inserts copies of the template root's DIRECT CHILDREN as new children
> of each target door, recursively copying each of their subtrees. The template's root node is
> never copied.**
>
> Every matrix cell in §6 is a consequence of that sentence plus the doors' own write invariants.

The second sentence is unchanged and is what makes T103 mandatory: §6 is re-derived, not patched.

### 5.2 Units — every count in this plan declares one

The reversal makes four different numbers live in the same paragraphs. They are never
interchangeable and no obligation in this plan may be read across them:

| Unit | What it counts | Where it appears |
|---|---|---|
| **targets** | doors the user picked | `templateCopyConfirmButton(count)` (`abwab.labels.ts:362`), `templateCopySelectedSummary`, the handler's `targetCount` log (`ApplyTemplateHandler.cs:29-30`) |
| **template nodes** | live nodes under the root, **all depths, root excluded** | `AbwabTemplateSummaryDto.NodeCount`, `AbwabTemplateVm.nodeCount`, the list's «N عناصر» chip, `templateCopyPreview`'s count |
| **child names** | the root's **direct** children — the collision surface | the new pre-check, the `409`'s pair list. **Not** exposed to the frontend and not needed there |
| **created doors** | rows written per target = *template nodes* (post-reversal); total = targets × template nodes | the response payload's length, `Writes/Abwab/README.md` |

The identity **created doors per target = template nodes** is exactly what §5.3 turns on, and it
holds only after the reversal. Before it, the figure was *template nodes + 1*.

### 5.3 The `NodeCount` contradiction, resolved from code

The audit contradicts itself. At `:765-770` it says `NodeCount` already excludes the root, so the
reversal makes the existing number correct. At `:831` its Fix/Size line says the component needs
`templateNodeCount - 1`.

**Read from code, both ends:**

- `AbwabTemplateSummaryDto.cs:3-5` documents *"NodeCount counts the root's live descendants and
  excludes the root itself"*.
- `EfAbwabTemplatesReader.cs:23-24` implements it as
  `db.AbwabTemplateNodes.Count(n => n.TemplateId == t.Id && n.ParentNodeId != null && n.DeletedAtUtc == null)`
  — every live node with a parent, at **every** depth. Root excluded, nothing else excluded.
- `buildAbwabTemplateTree` (`abwab-templates.models.ts:67-88`) independently derives the VM's
  `nodeCount` by incrementing once per child reached from the root — the same set.
- The copy modal reads the **VM's** number (`abwab-templates-page.component.html:224` →
  `abwab-templates.facade.ts:56-61`), not the summary row's. Both agree.

Post-reversal, apply copies the root's direct children and every descendant beneath them — which
is precisely "every live node with a parent". **Created doors per target = `NodeCount`, exactly.**

**Resolution: no arithmetic change. `templateCopyPreview` keeps `templateNodeCount()` as it is.**
Subtracting one would make the preview under-report by one for every template — an off-by-one
introduced by "fixing" a number that the reversal makes correct. The audit's `:765-770` is right
and its `:831` is wrong; recorded as DRIFT-2 so the executor does not follow the Fix/Size line.

What *is* wrong today is the prose: `templateCopyPreview` promises `nodeCount` elements while the
apply creates `nodeCount + 1` doors. The reversal fixes the number by fixing reality, and T601
fixes the sentence.

### 5.4 The amendment ledger — every recorded statement, by file and line

**Amend (`docs/feature-abwab-templates/plan.md`):**

| Line(s) | What is there now | Treatment |
|---|---|---|
| `:158-163` (§5.1) | the axiom | **replaced** with §5.1 above; T102 |
| `:162` | "every matrix cell in §6 is a consequence of that sentence" | **unchanged** — it is what makes T103 mandatory |
| `:116` (§4 "Apply") | "The template root becomes a **new child** of each target door, full depth…" | rewritten: the root's **children** become new children of each target, each with its subtree; sibling order preserved with a level-1 offset |
| `:123` (§4 "Apply collision") | "If any target already has a live child named like the template root, the whole apply fails with one `409` naming every colliding target" | rewritten to the per-child-name rule and the `(target, child)` pairs |
| `:140` (route table, row 5) | "`201` created root doors" | rewritten: `201` created doors, **N per target**; refusals gain the empty-template `400` |
| `:232-249` (§5.5) | titled "Sibling-name uniqueness inside a template is what keeps the copy honest", concluding *"the only collision an apply can hit is at the root… and it is the only `409` the apply route can produce"* (`:247-249`) | **rewritten, not annotated.** The template's own `(template_id, parent_node_id, name)` index still guarantees the root's children are internally distinct — that half survives and is *why* the pre-check's name set has no duplicates — but it says nothing about the target's existing children, so the collision surface becomes N names per target. The conclusion does not survive; the section keeps its purpose (why the copy cannot fail on an invisible constraint) and loses its "only at the root" claim |
| `:330` (§6.1 anchor cell) | "Live door that already has a live child «أركان الإيمان» → `409`, nothing is created anywhere" | **re-keyed** from the root's name to the root's children's names; the all-or-nothing half survives verbatim |
| §6.3 (`:359-371`) | the deep-copy cells that inherit the root-keying — **empty template**, **same template applied twice**, **sibling order** | re-derived in T103; the empty-template cell **flips from "Legal" to `400`** (§6a, and §11) |
| §9 (`:889`, `:898-900`, `:906`) | the apply traps | `AbwabTemplateRootNodeException` not-split: **unchanged**. Pre-check-then-`23505`: **unchanged in shape**, restated for pairs. Concurrent-apply race unguarded: **unchanged**. A new trap is added: *do not subtract one from `templateNodeCount`* (DRIFT-2) |

**Explicitly NOT touched in `plan.md`:** `:87-88` and `:337` (rootless apply `400` is about
**target** doors and stays correct); §5.2's one-root-per-template index and "the template's name
is the root node's name" (`:165-183`) — both survive and read **better**, since the root becomes
purely a naming/container row; §5.3 (aliases as `text[]`); §5.4 (no version token); §5.6
(detachment, level-order saves); §5.7 (snapshot contract unchanged); §6.2 (template editing ×
existing copies); §6.4's concurrency cells other than the ones re-derived; §8 (the posture);
§10–§12.

**Amend (code and its recorded rationales):**

| File | Line(s) | Treatment |
|---|---|---|
| `EfAbwabTemplateApplyWriter.cs` | `:7-14` (`<remarks>`) | The three "deliberately does NOT do" clauses **all survive** — a copy is still never a root (no global-order work), every insert still lands in a scope it created or is newest in (no resequencing), the section is still read once per target (no per-node resolution). Only the sentence that frames them changes, plus one addition: the level-1 offset is what *keeps* the second clause true |
| same | `:55-56` ("one created root per target") | rewritten: N created children per target, in the caller's target order and the template's sibling order |
| same | `:64-65` (the pre-check's "name the offenders" comment) | extended to say the pre-check now names **which child name inside which target** |
| same | `:118-120` (verbatim `OrderValue` at depth) | **unchanged** — still exactly right for depth ≥ 2; the level-1 exception is documented at the seed |
| `IAbwabTemplateApplyWriter.cs` | `:10-11` ("Returns the created ROOT door per target") | rewritten: returns the created **top-level** doors, N per target |
| `AbwabTemplateRootNodeException.cs` | `:3-5` | rationale gains its **new** reason: *"deleting it would leave a template that cannot be applied"* is still true, now because a rootless template has no children to enumerate. **Do not split the type** — `plan.md` §9 |
| `abwab.labels.ts` | `:351` (`templateCopyDescription`, «بجذره وكل فروعه») | rewritten (§6 T601) |
| same | `:352-353` (`templateCopyPreview`) | prose rewritten, **`count` untouched** (§5.3) |
| `abwab-template-copy-modal.component.ts` | `:15-24` (the recorded preview contract) | rewritten to state children-only and the empty-template refusal |
| `abwab-template-tree.component.ts` | `:26-28` (why a list, not `role="tree"`) | amended to name the keyboard path that was added and why the role is still not claimed |
| `features/abwab/README.md` | `:657-666`, `:677-684`, `:733` | apply paragraph re-derived; workshop-tree list-role paragraph amended; the concept's «بجذره» recorded as superseded |
| `Writes/Abwab/README.md` | `:179-199` | "A template is a door subtree, and applying it is a plain door create repeated" and "Applying is all-or-nothing, and **the only collision is at the root**" — the second title is now false and the paragraph is rewritten |

**Record as superseded, do not edit:** `docs/design-preview/abwab-templates-concept.html:139,145`
(«كاملًا بجذره»), cited as a design contract at `features/abwab/README.md:733` and at
`plan.md:167-168`. §4.2-16 fixes where the supersession is recorded.

**Do not touch, and do not "fix" while here:**
`templateCopyConfirmButton(count)` — counts **targets**, never a union;
`templateCopyPreviewNoRoot` («لا يمكن النسخ كباب رئيسي») — correct, and *more* obviously so after
the reversal; `templateCopyPreviewDetached`; the copy's detachment at birth (no `templateId`, no
provenance); the fact that the apply refreshes nothing on the doors side; `NewDoor`'s section
cascade and null `GlobalOrderValue`; the level-order `SaveChanges` descent; the enclosing
transaction; `AbwabTreeDoorDto`.

## 6. Phases

Every phase is one commit. The tree builds at each commit boundary, and every commit is green.

### Phase 1 — Baseline, then the axiom and the re-derived matrix (4 tasks)

**Doc-amendment phase, first, non-negotiable.** No implementation task in this slice may start
before §6 has been re-derived, because the re-derivation is what produces the cells Phases 2–7
implement against.

**Files** — `CLAUDE.md`; `docs/feature-ux-slice-g/evidence.md` (new);
`docs/feature-abwab-templates/plan.md`.

- **T101 — Baseline, recorded before anything is touched.** Set the root `CLAUDE.md` Active Spec
  Kit Feature to `ux-slice-g` + this plan. Create `docs/feature-ux-slice-g/evidence.md` and
  record, as measured numbers: backend `dotnet build`; the no-pipeline tier (§5, expect 1,086);
  the route-smoke tier (§5, expect 140) **with an explicit `Tests.Smoke.Data` RAN or SKIPPED
  statement**; `Backend/scripts/check-api-contract` result; `npm test` file and test counts;
  `npm run build`. A baseline that is not green is a stop condition, not a starting point.
- **T102 — Replace the axiom.** Rewrite `plan.md` §5.1 (`:158-163`) to the sentence in §5.1
  above, verbatim, keeping `:162` unchanged. Amend `:116`, `:123`, and the route table's row 5
  (`:140`) per the §5.4 ledger. Add the DRIFT-2 trap to §9. **Record the reversal; do not argue
  it** — no "why" paragraph is added, and the old sentence is not preserved as a struck-through
  quote inside the plan (git history is the archive).
- **T103 — Re-derive §6 from the new sentence, cell by cell, and rewrite §5.5.** Do not patch
  §6.1's anchor cell in place; walk §6.1, §6.3, and the apply-touching cells of §6.4 and ask of
  each: *what does the new axiom make this?* Rewrite §5.5 (`:232-249`) to keep its purpose — why
  the copy cannot fail on a constraint the user never saw — and drop its "only at the root"
  conclusion. **The matrix produced here is the contract Phases 2–7 implement.** Every cell in
  §6a of this plan must appear in it; any cell the re-derivation finds that §6a does not have is
  a finding, and if its correct outcome is genuinely undetermined, that is a stop condition
  (§11).
- **T104 — Cross-check the ledger.** `grep -rn` the repo for the phrases the reversal falsifies
  — «بجذره», "root becomes a new child", "only collision is at the root", "created root", "one
  created root per target", "root door per target" — across code, READMEs, `.architecture/`,
  `docs/`, `specs/`, and `e2e/`. Every hit is either in §5.4's amend list, in its
  do-not-touch list, or is a **finding** to be added to the ledger before Phase 2 starts. Record
  the grep and its result in `evidence.md`. This task exists because §5.4 was assembled from
  targeted reads; a repo-wide sweep is what makes it complete.

### Phase 2 — The additive half of the contract surface (4 tasks)

**Purely additive, by design.** Everything in this phase compiles against the writer exactly as
it stands today, so the commit is green with no writer change. **The collision reshape is NOT
here** — see the note at the end of this phase.

**Files** — `Application.Abstractions/Abwab/AbwabTemplateApplyCollisionPair.cs` (new),
`AbwabTemplateEmptyException.cs` (new), `IAbwabTemplateApplyWriter.cs`,
`AbwabTemplateRootNodeException.cs`;
`Application/Abwab/Commands/Templates/ApplyTemplate/ApplyTemplateOutcome.cs`,
`ApplyTemplateHandler.cs`; `api/…/Common/ApiMessages.cs`;
`api/…/Controllers/Abwab/AbwabTemplatesController.cs`.

- **T201 — The collision pair type, and the two recorded rationales this slice owes.** New
  `public sealed record AbwabTemplateApplyCollisionPair(string TargetName, string ChildName);`
  in `Application.Abstractions/Abwab/`, beside the exception that will carry it in T301. It is
  added here and consumed in Phase 3; an unreferenced record is green. Same task, because both
  are one-comment amendments in the same folder and §5.4 owes them:
  - `IAbwabTemplateApplyWriter.cs:10-11` — "Returns the created ROOT door per target" becomes
    the created **top-level** doors, N per target. **Signature and return type unchanged.**
  - `AbwabTemplateRootNodeException.cs:3-5` — the rationale gains its new reason (a rootless
    template has no children to enumerate) and keeps its one-type-two-refusals note. **Do not
    split the type** (`plan.md:896`).
- **T202 — The empty-template exception.** New
  `public sealed class AbwabTemplateEmptyException : Exception;` with a `//` comment recording
  the decision and its boundary: the template's root has no live children, so there is nothing to
  copy; the refusal is a `400`, not a silent no-op, because the copy modal's confirm button would
  otherwise promise N copies and produce zero. **State in the comment that this is deliberately
  not `AbwabTemplateRootNodeException`** and why (`plan.md` §9 forbids splitting that type, and
  overloading it with a third refusal in a third handler is the thing §9 is protecting against) —
  otherwise the next reader re-litigates it.
- **T203 — The new outcome, its handler arm, and its Arabic copy.** `ApplyTemplateOutcome`
  gains `EmptyTemplate` (`Collision`'s payload is **not** touched here — T301).
  `ApplyTemplateHandler` gains a fifth `catch` arm mapping `AbwabTemplateEmptyException` →
  `EmptyTemplate`, logged with the existing
  `LogWarning("Rejected {feature} {operation} {reason}", …, "emptyTemplate")` shape — the four
  existing arms' pattern, not a new one. `ApiMessages` gains **only**
  `AbwabTemplateApplyEmpty` (§4.2-10). All Arabic user-facing text stays in `ApiMessages.cs`;
  identifiers stay English.
- **T204 — The route arm, and the catalog verification.** `AbwabTemplatesController.Apply`
  gains
  `ApplyTemplateOutcome.EmptyTemplate => BadRequest(ApiResponse<…>.Fail(ApiMessages.AbwabTemplateApplyEmpty))`.
  Its `Collision` arm is untouched here (T301). **No route, verb, template, or constraint
  changes**, so no `SmokeRouteCatalog` edit is owed (DRIFT-3) — verify the entry at
  `SmokeRouteCatalog.cs:356-359` still matches and record that verification in `evidence.md`
  rather than editing the file. The `_ => throw new InvalidOperationException` default arm stays;
  it is what makes a forgotten variant a loud failure.

**Why the collision reshape is not in this phase, stated so it is not "tidied" back in.**
`AbwabTemplateApplyCollisionException`'s only two throw sites are in the writer
(`EfAbwabTemplateApplyWriter.cs:76-77`, which passes a `List<string>` of target names, and
`:197`, the empty-list backstop), and its only reader is `ApplyTemplateHandler.cs:51`
(`ex.DoorNames`). Retyping the constructor here would break `:76-77` on `CS1503` and the handler
on the renamed property — a red commit in a plan whose §10 says every commit is green. The
retype and the throw sites can only be green **together**, which is T301. Everything above is
additive: a record nothing references yet, a new exception nothing throws yet, and an outcome
variant that compiles as an unreachable arm.

### Phase 3 — The writer, and the collision reshape it makes reachable (4 tasks)

**Files** — `infrastructure/…/Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs`;
`Application.Abstractions/Abwab/AbwabTemplateApplyCollisionException.cs`;
`Application/Abwab/Commands/Templates/ApplyTemplate/{ApplyTemplateOutcome,ApplyTemplateHandler}.cs`;
`api/…/Common/ApiMessages.cs`; `api/…/Controllers/Abwab/AbwabTemplatesController.cs`.

- **T301 — The collision reshape, atomically, with the writer's pre-check.** One task because
  these five edits are green only together (see the Phase 2 note):
  - `AbwabTemplateApplyCollisionException` takes
    `IReadOnlyList<AbwabTemplateApplyCollisionPair>` and exposes it as `Collisions`. Its `//`
    comment is rewritten to state the new key (a child name under a target) and to keep the
    recorded reason the payload exists at all: *the pre-check names them; `23505` names nothing,
    so the race backstop throws with an empty list*.
  - `ApplyTemplateOutcome.Collision`'s parameter renames `DoorNames` → `Collisions` and retypes.
  - `ApplyTemplateHandler.cs:51` reads `ex.Collisions`.
  - `ApiMessages`: the two collision strings rewritten and the formatter retyped to render pairs
    per §4.2-5 and §4.2-10. This adds the first `using QuranDashboard.Application.Abstractions.Abwab;`
    that file has carried (§4.2-9).
  - The writer's pre-check (`:66-77`) becomes the child-name-set query of §4.2-4 — one
    `AsNoTracking` read of live doors whose `ParentId` is in `targetIds` **and** whose `Name` is
    in the root's direct-child name set, projecting `{ ParentId, Name }` — throwing with pairs
    built in the order §4.2-5 fixes. The `23505` backstop (`:197`) still throws with an empty
    list. Amend the `:64-65` comment.
- **T302 — The empty guard.** After `childrenByParentNode` is built (`:79-82`), read
  `childrenByParentNode.TryGetValue(rootNode.Id, out var rootChildren)`; if absent or empty,
  throw `AbwabTemplateEmptyException` — **before** the target reads (§4.2-7), which is what
  makes §6a row 7 true. `rootChildren` is the same list T301's pre-check and T303's seed both
  read, so it is resolved once, here, and not re-queried.
- **T303 — The seed and the level-1 offset.** Replace the one-`copiedRoot`-per-target loop
  (`:87-99`) with: per target, `nextOrder` as today (`:93-94`), then for each `rootChildren[i]`
  a `NewDoor(child, target.SectionId, target.Id, nextOrder + i, now)` seeded into the list the
  BFS walks. **Do not touch the descent loop (`:107-134`)** — it is keyed by node id and is
  already generic; its verbatim-`OrderValue` comment (`:118-120`) stays correct for depth ≥ 2.
  Amend `:55-56` and the `<remarks>` (`:7-14`) per §5.4. Verify by reading, not by assuming, that
  every touched scope still ends `1..N`: the target's existing children occupy `1..nextOrder-1`
  and the N new ones occupy `nextOrder..nextOrder+N-1`.
- **T304 — DRIFT-1: per-node aliases in the response.** Replace the single `rootAliases`
  (`:137`) with `AbwabAliasNormalization.Normalize(copied.Node.Aliases)` inside the `Select`
  (`:151`); `CopiedNode` already carries `.Node` (`:17`). **State in the commit message that the
  alias *rows* were already correct** — `AddAliases` (`:110`) is per-node and runs for the seed
  level — so this is a response-payload fix, not a data fix, and nothing already written to the
  database is wrong.

### Phase 4 — The route gate (1 task)

- **T401 — Run the gate the contract change owes.** `dotnet build`, then `Tests.Api` (§5, expect
  60), then the route-smoke tier (§5, expect 140) **with the `Tests.Smoke.Data` RAN/SKIPPED
  statement written out in full**, then the no-pipeline tier (§5, expect 1,086). Record all four
  in `evidence.md`. `SmokeCoverageParityTests` passing here is the proof DRIFT-3 was read right:
  if it fails, the catalog entry did move and Phase 2's T204 verification was wrong.

### Phase 5 — Contract regeneration, expected clean (1 task)

- **T501 — Run `Backend/scripts/check-api-contract` and record the result.** The expectation is
  **clean** (§4.2-3): request shape unchanged, response type unchanged, no
  `[ProducesResponseType]` anywhere on the controller, so the new `400` adds nothing to the
  OpenAPI document. **If it is not clean, the regenerated `swagger.json`,
  `core/api/generated/`, and `docs/api-reference/` are this commit's deliverable** and the
  diff is described in `evidence.md`. Run `export-swagger` before `generate:api` if regenerating
  by hand — `plan.md` §9 records that the generator reads the spec off disk and will happily
  regenerate the previous contract while reporting success.

### Phase 6 — The copy modal tells the truth (3 tasks)

**Files** — `features/abwab/models/abwab.labels.ts`;
`components/abwab-template-copy-modal/abwab-template-copy-modal.component.{ts,html}` (+ `.scss`
only if T602 needs a rule).

- **T601 — The copy.** `templateCopyDescription` (`:351`) becomes
  `'اختر الأبواب المستهدفة — عناصر القالب (بدون جذره) ستُنسخ داخل كل باب تختاره.'`
  `templateCopyPreview` (`:352-353`) becomes
  `` `كل باب مستهدف سيكسب ${countPhrase(count, ELEMENT_FORMS)} من «${templateName}» بكامل تفرعها — جذر القالب نفسه لا يُنسخ.` ``
  — reusing `ELEMENT_FORMS` and `countPhrase` (`:46`, `:24`), **with `count` unchanged**
  (§5.3, DRIFT-2). New `templateCopyEmptyTemplate:
  'هذا القالب لا يحتوي عناصر — أضف عنصرًا واحدًا على الأقل قبل النسخ.'`
  `templateCopyPreviewNoRoot`, `templateCopyPreviewDetached`, and `templateCopyConfirmButton`
  are **not touched**. Amend the component's class doc (`:15-24`).
- **T602 — The empty-template affordance (courtesy, not guarantee).** A
  `hasElements = computed(() => this.templateNodeCount() > 0)`; when false, the preview block
  (`.html:26-29`) renders `templateCopyEmptyTemplate` in its place and the confirm button
  (`.html:56-59`) is disabled. Prefer the existing `qd-state` primitive the modal already
  imports over a new SCSS rule; add a rule only if the shape genuinely does not fit, and use
  tokens if so. **The component's own comment must say the writer's `400` is the guarantee** —
  a stale list can show a template that has since lost its last child, and a disabled button
  that disagrees with the server is worse than a refused write. RTL is inherited from the
  dialog's `dir="rtl"`; no new direction handling.
- **T603 — Verify the modal's spec passes unedited.** `npm test --include` on
  `abwab-template-copy-modal.component.spec.ts`. It asserts labels **by reference** and never
  the preview or description strings (Precondition table), so T601 cannot break it. The one
  cell at risk is the confirm-button case (`:109`), which T602's `disabled` binding could
  reach. **If it fails, that is T602's own regression, not spec maintenance** — fix the
  component, do not edit the assertion. Record the result in `evidence.md`.

### Phase 7 — Item 21(a): the workshop tree's two missing menu paths (3 tasks)

**Files** — `components/abwab-template-tree/abwab-template-tree.component.{ts,html}`.

- **T701 — Right-click.** `(contextmenu)="onRowContextMenu($event, row.node.id)"` on the row
  `<div>` (`.html:3-10`); the handler calls `event.preventDefault()` then emits
  `menuRequested` with `{ nodeId, x: event.clientX, y: event.clientY }` — the same payload
  `onMoreClick` (`:104-106`) already emits. **No bulk-mode guard** (§4.2-14). The `⋯` path is
  untouched.
- **T702 — The keyboard path.** `(keydown)="onRowKeydown($event, row.node.id)"` on the same
  row `<div>`. The handler acts on `event.key === 'ContextMenu'` and on
  `event.key === 'F10' && event.shiftKey`, calls `preventDefault`, and anchors at
  `rowElement(nodeId)?.getBoundingClientRect()` → `{ x: rect.left, y: rect.bottom }`, falling
  back to `(0, 0)` only when the element is missing — `abwab-tree.component.ts:323-324`'s exact
  shape. `rowElement` is a private `querySelector` on
  `[data-testid="abwab-template-tree-row-<id>"]` via an injected `ElementRef`, copying
  `abwab-tree.component.ts:349-351`. **No `tabindex` is added and no `role` changes.** A `//`
  comment records the mechanism: the key is caught as it bubbles from whichever of the row's own
  controls has focus, which is what makes this work without a roving-tabindex model — and the
  doors tree's own reason for anchoring at all (*"a menu pinned at (0,0) is not a usable
  keyboard path"*) is carried over rather than re-derived.
- **T703 — Verify the composition end to end, by hand, in a real browser.** jsdom cannot
  produce a `contextmenu` event with usable client coordinates or a meaningful
  `getBoundingClientRect`, and no spec exists for this component (Precondition table) — so this
  is the only check that exists. Walk, at `/abwab/templates` with a two-level template loaded:
  right-click a row (menu opens at the pointer, the browser's own menu does not); `Tab` to a
  row's `⋯` then press `ContextMenu` and `Shift+F10` (menu opens under that row's start edge,
  not at the viewport corner); the same from the chevron and `＋` (same result — this is what
  proves the bubbling mechanism); `Escape` dismisses (the document-level handler); backdrop
  click dismisses; the root row's menu still swaps delete-node for delete-template
  (`abwab-templates-page.component.html:247-255`); and the RTL near-edge overflow **still
  happens** — that is Slice A's recorded open gap, unchanged, and seeing it is confirmation, not
  a defect. Record each step's result in `evidence.md`, including the failures that are expected.

### Phase 8 — Docs true again (3 tasks)

- **T801 — Backend READMEs.** `Persistence/Writes/Abwab/README.md:179-199`: re-derive the "A
  template is a door subtree" paragraph (the three does-not-need clauses survive; the framing
  and the level-1 offset change), and rewrite "Applying is all-or-nothing, and **the only
  collision is at the root**" — its title is now false. State the response's new meaning (N doors
  per target, unchanged type) and the `(target, child)` pair message. Verify `:20-23`, `:44-46`,
  `:47-49`, and `:224-229` still read true and amend only what moved.
  `Persistence/Reads/Abwab/README.md:99-101` and `api/…/Controllers/README.md` are
  **verify-only** — no route was added or removed, and the descendant-count claim is unaffected
  — but the verification is a task, not an assumption.
- **T802 — Frontend README and §17.** `features/abwab/README.md`: re-derive the apply paragraph
  (`:657-666`); amend the workshop-tree list-role paragraph (`:677-684`) to name **exactly**
  which keyboard path was added (`ContextMenu`/`Shift+F10`, from the row's own controls, via
  bubbling) and **why the role is still not claimed** (no arrow-key navigation model, so claiming
  it would still promise a contract the workshop does not implement); record the concept's
  «بجذره» as superseded beside `:733` (§4.2-16); record `abwab-cards`' missing row menu as an
  open decision for a later slice (§4.2-15). Verify `:17-25`'s endpoint counts are unchanged —
  **no route was added, so twenty-one/twenty-five both stand**; amend only if the sweep proves
  otherwise. `.architecture/UI_STYLE_SYSTEM.md` §17's `qd-context-menu` entry (`:1046-1073`) is
  **verify-only**: the primitive is composed unchanged and both recorded gaps stay open. If a
  gap moved, §17 is amended in this commit.
- **T803 — `docs/TESTING_DEBT.md`.** Add the `ux-slice-g` section dated at execution, with the
  rows in §7. **Restate row 7 rather than leaving it untouched:** its trigger — "the next change
  to the apply path" — fires in this slice and was not paid, so the row must name the reversal's
  new surface (children-only enumeration, the level-1 offset, the per-child collision, the
  per-node alias DTO) instead of describing a writer that no longer exists. Widen row 9 to name
  the two new menu paths rather than relying on it to cover them silently.

### Phase 9 — Verification and close-out (3 tasks)

- **T901 — Tier C.** Backend build; the no-pipeline tier (expect 1,086 — unchanged, matching the
  zero-new-backend-tests posture); the route-smoke tier (expect 140) **with the
  `Tests.Smoke.Data` RAN/SKIPPED statement, again, at close**; `npm test` (expect the T101 file
  and test counts, **unchanged** — this slice writes no spec); `npm run build`. **Tier B fires
  independently** and is satisfied by the same run: this is a completed backend+frontend vertical
  slice (`TESTING_STRATEGY.md` §3 Tier B). No Tier D trigger: no `DataPipelines` code, no
  importer, no migration, no canonical resource, no shared persistence that can reach pipeline
  tables. Any count that moves against T101 is explained per-file or it is a finding.
- **T902 — The browser acceptance pass for item 20.** Against the local dev DB: apply a
  two-level template to two live doors (each target gains N top-level children, not one wrapper;
  the subtrees are complete; sibling order matches the template); apply the same template again
  to one of them (`409`, the message names **(target, child)** pairs, and **nothing is created
  anywhere** — verify the second target too); apply a template whose root has one child; apply an
  **empty** template (`400`, «القالب لا يحتوي عناصر لنسخها», and the modal's confirm was already
  disabled); apply to a section-less target (the whole copy inherits `section_id = NULL`); apply
  to a nested target; edit the template afterwards and confirm the copies do not change. Record
  every step in `evidence.md`. **This is the behavioral acceptance for the reversal** — under the
  §7 posture nothing else covers it.
- **T903 — Close-out sweep.** Re-run T104's grep across the whole repo; every remaining hit is
  either amended or explicitly recorded as superseded. Verify `git status --short` is empty after
  `check-api-contract`. Clear the root `CLAUDE.md` Active Spec Kit Feature back to `None`.
  **Do not sweep, delete, or repoint any planning folder** (§3) — including
  `docs/feature-abwab-templates/`, which this slice amended.

| Phase | Commit | Gate before the next phase starts |
|---|---|---|
| 1 | `docs(ux-slice-g): amend the templates axiom and re-derive the matrix` | T101 baseline green; T104 sweep clean or its findings folded into the ledger |
| 2 | `feat(ux-slice-g): add the empty-template refusal to the apply contract` | `dotnet build` (additive only — the writer is untouched) |
| 3 | `feat(ux-slice-g): copy the template root's children, never its root` | `dotnet build` (the collision retype and its throw sites land together) |
| 4 | `test(ux-slice-g): run the route gate the contract change owes` | Tier A + `Tests.Api` + smoke, all green, data tier stated |
| 5 | `chore(ux-slice-g): verify the generated contract` | `check-api-contract` clean (or its diff committed) |
| 6 | `feat(ux-slice-g): the copy modal states the children-only contract` | T603 spec green unedited |
| 7 | `feat(ux-slice-g): right-click and menu-key parity for the workshop tree` | T703 browser walk recorded |
| 8 | `docs(ux-slice-g): READMEs, §17 verification, and the debt this slice owes` | — |
| 9 | `docs(ux-slice-g): T901–T903 close-out evidence` | Tier B/C green; sweep clean |

## 6a. The re-derived interaction matrix — apply case × state

Re-derived from the new axiom, **not copied** from `plan.md` §6. "Live" = `deleted_at IS NULL`.
"Created" always means *doors written anywhere*, because the batch is all-or-nothing inside one
transaction. `N` = the root's direct-child count; `M` = template nodes (= created doors **per
target**, §5.2).

| # | Case | Outcome | What is created **anywhere** | Why |
|---|---|---|---|---|
| 1 | Live target, no name clash | `201` | `M` doors under that target; the root's `N` children at `nextOrder … nextOrder+N-1`, their subtrees at verbatim `OrderValue` | the new axiom + §4.2-6 |
| 2 | Target whose live children collide on **one** of the root's child names | `409` | **nothing, anywhere** | pre-check + all-or-nothing |
| 3 | Target colliding on **several** child names | `409`, message lists every pair for that target, in the template's sibling order | nothing | §4.2-5 |
| 4 | **Several targets** each colliding | `409`, pairs grouped by target in the **caller's** target order | nothing | §4.2-5 |
| 5 | Target whose colliding child is **archived** | `201` | as row 1 | the doors index filters `deleted_at IS NULL`, so an archived child does not occupy the name — unchanged from `plan.md` §6.1 |
| 6 | **Empty-root template** (root, no live children) | **`400`** «القالب لا يحتوي عناصر لنسخها» | nothing | §4.1-2. **This cell flips from `plan.md` §6.3's "Legal"** and is the loudest product consequence — see §11 |
| 7 | Empty-root template **and** an archived target | **`400` empty-template**, not `400` archived-target | nothing | §4.2-7 — the guard fires before the target reads |
| 8 | **Single-child template** | `201` | `M` doors per target; that one child lands at exactly `nextOrder`, so the offset is invisible in this case | the offset degenerates to the old behavior — which is why it must be tested at N ≥ 2 |
| 9 | **Deep template** (3+ levels) | `201` | full depth; only level 1 is offset, everything below is verbatim | §4.2-6; recursion has no depth limit, matching doors |
| 10 | **Archived target** (template non-empty) | `400` archived-target | nothing | unchanged from `plan.md` §6.1; unreachable from the UI (the picker lists live doors only) |
| 11 | **Root-level (rootless) apply** — empty `targetDoorIds` | `400` no-targets | nothing | **unchanged, and unrelated to the reversal** — `plan.md:87-88,337` is about *target* doors |
| 12 | **Same template applied twice** to the same target | second → `409` on **the children's** names, not the root's | nothing on the second | the first apply's children now occupy those names. The rule survives, its key changes |
| 13 | Same template applied twice, first apply's children since **archived** | `201` | as row 1 | row 5's rule, one apply later |
| 14 | **Section-less target** (`section_id IS NULL`) | `201` | whole copy inherits `section_id = NULL` | the cascade invariant, unchanged |
| 15 | **Nested target**, any depth | `201` | copy deepens the branch | no depth limit |
| 16 | Two targets, one an **ancestor** of the other | `201` | **both get their own copy** — no dedup, no union | `plan.md` §6.1's ancestor cell survives verbatim; the confirm count stays the number of targets |
| 17 | Unknown target id / unknown template id / soft-deleted template | `404` | nothing | unchanged |
| 18 | **Concurrent apply × apply** on the same target | both may compute the same `nextOrder`, producing duplicate `order_value`s | both copies | **unchanged and still unguarded** (`plan.md` §6.4, §9). The offset makes the collision wider (N rows, not one) but not different in kind; reads tolerate ties, ordering `(OrderValue, Id)` |
| 19 | **Apply × concurrent template edit** | nodes read once inside the transaction; the edit is either copied or not | one consistent outcome | unchanged — no token is offered because the caller holds no template version |
| 20 | **Concurrent apply of the *same* template** to the same target | one wins, the other `409`s on the children's names | only the winner's | the unique index is the arbiter; key changed, arbiter unchanged |
| 21 | A node with empty description / no ayah / no aliases | `201`, copied as `NULL` / `NULL` / `{}` | unchanged | the nullability doors already allow |
| 22 | A node with **aliases**, at level 1 | `201`; alias **rows** correct (already were), and the **response DTO** now reports that node's own aliases | unchanged rows, fixed payload | DRIFT-1 / T304 |
| 23 | Duplicate name **inside** the template | unrepresentable | — | `(template_id, parent_node_id, name)` refuses it at authoring time; this is what guarantees the pre-check's name set has no duplicates |

Rows 6, 7, 12, and 22 are the ones that did not exist or read differently before the reversal.
Row 8 is the trap: it is the case where a wrong offset still looks correct.

## 7. Testing posture and the debt it owes

**Posture (locked, §4.1-8):** no new suites. Existing suites RUN. Parity one-liners are
mandatory — and **none is owed here**, because no route is added (DRIFT-3); the existing entry is
verified instead. Gaps become `TESTING_DEBT.md` rows in this change.

**The gates that run, with the validated commands (`TESTING_STRATEGY.md` §5/§6):**

- `dotnet build` — every backend phase.
- `--filter "FullyQualifiedName~QuranDashboard.Tests.Api"` (60) — T401.
- `--filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` (140) — T401 **and** T901, each
  with an explicit `Tests.Smoke.Data` RAN or SKIPPED statement. An unqualified "smoke passed" is
  not acceptable evidence.
- The no-pipeline filter (1,086) — T101, T401, T901.
- `npm test` (fork cap preserved by the script) — T101, T603 (focused), T901 (full).
- `npm run build` — T101, T901.
- `Backend/scripts/check-api-contract` — T101, T501, T903.

**Not a gate:** Playwright. There is no templates e2e (Precondition table), so `npm run e2e` has
nothing to say about this slice. If it is run at all it is evidence, never a tier.

**Why Tier B fires:** this completes a backend+frontend vertical slice
(`TESTING_STRATEGY.md` §3 Tier B). Note the contrast with Slice F, honestly: **no `shared/` or
global-stylesheet file is touched here** — `qd-context-menu` is composed, not edited, and
`src/styles/_components.scss` is not opened — so the shared-infrastructure trigger does **not**
fire. Tier B fires on the vertical-slice ground alone, and Tier C's full suite satisfies it.

**What this posture leaves uncovered, and the rows it owes:**

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| **7 (restated, not new)** | **The deep copy, re-derived.** Row 7 already names this writer and calls itself the file's highest-value row; its trigger — "the next change to the apply path" — **fires in this slice and is not paid.** The row must be rewritten to the reversal's surface: children-only enumeration, the level-1 `nextOrder + i` offset with every touched scope still `1..N`, per-child-name collision with `(target, child)` pairs, the empty-template `400`, and the per-node alias DTO. Leaving it describing a writer that no longer exists is worse than having no row | `Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs` | The next change to the apply path **or** to `abwab_doors`' per-sibling unique index. Unchanged trigger, restated surface |
| G1 | **The level-1 offset specifically** — that N children land contiguously at `nextOrder … nextOrder+N-1` and the target's child scope stays `1..N`. Row 8 of §6a is why this needs its own line: at N = 1 a broken offset is invisible | same | Whoever fixes the concurrent-apply `order_value` race, **or** the next change to any doors reorder path — both depend on target scopes being `1..N` |
| G2 | **The empty-template refusal and its ordering** — the `400`, and that it fires **before** the archived-target check (§6a row 7) | `EfAbwabTemplateApplyWriter` + `ApplyTemplateHandler` | The next change to the apply refusal set, or the first time a second refusal wants to move ahead of the target reads |
| G3 | **Apply smoke** — the `201`/`400`/`404`/`409` status and envelope contract of the apply route, now including the new `400` and the re-shaped `409` message. Catalogued `ParityOnly`, i.e. listed but not dispatched | `Tests/Smoke/`, `SmokeRouteCatalog.cs:356-359` | When write protection lands and `/api/abwab` stops being `Open`: the auth cases force a dispatched test per route regardless. **This narrows the templates row 8, it does not replace it** |
| **9 (widened, not new)** | Row 9 already names `components/abwab-template-tree/` and `pages/abwab-templates-page/`. Widen it to name **the two new menu paths** — right-click with `preventDefault`, and the `ContextMenu`/`Shift+F10` path anchored via `getBoundingClientRect` — rather than letting "the tree editor's collapse/order-edit/quick-add" be read as covering them | `components/abwab-template-tree/` | Unchanged: the next time the workshop changes shape |
| G4 | **The copy modal's empty-template affordance** — that the confirm button disables at `nodeCount === 0` and the preview is replaced. The modal's spec exists and covers everything else it does; this one cell is new and unwritten | `abwab-template-copy-modal.component.spec.ts` | The next change to the copy modal. **Cheapest row in this table** — the spec is already stood up, so this is one `it` block, not a suite |

G4 is the honest one to flag in review: the posture's own logic (existing suites run, new cells
are debt) applies, but this is a component that *already has a spec*, so the marginal cost of
covering it is a fraction of the others. Deferring it is a choice, not a constraint.

## 8. Risk register

| # | Risk | Likelihood | Blast radius | Mitigation in this plan |
|---|---|---|---|---|
| 1 | The level-1 offset is wrong (off-by-one, or applied at depth ≥ 2 too), breaking `1..N` in a target's child scope | medium | The doors and sections reorder paths both assume contiguity; a broken scope surfaces later, elsewhere, as a reorder that moves the wrong row | §4.2-6 confines the change to the seed; T303 verifies contiguity by reading the arithmetic; §6a row 8 names the case where a bug hides; debt row G1 |
| 2 | The executor "fixes" `templateNodeCount - 1` per audit `:831` | **high** — the audit says it in its Fix/Size line | Every preview under-reports by one, permanently, and looks deliberate | DRIFT-2, §5.3, §4.2-12, and a new §9 trap in the amended `plan.md` (T102) — four places |
| 3 | DRIFT-1 ships unfixed: every DTO reports the root's aliases | medium — it compiles and nothing visible breaks | A contract that lies to whatever consumer reads the payload next | T304, named as its own task with the fix and the boundary (rows already correct) |
| 4 | The `409` message is nondeterministic across runs | medium | Untestable message, unreproducible bug reports | §4.2-5 fixes both ordering rules explicitly |
| 5 | §6 is patched cell-by-cell instead of re-derived, and a cell the audit did not enumerate ships wrong | medium | A contract cell nobody notices until a user hits it | T103 is a task, T104 sweeps the repo, and §11 makes an undetermined cell a stop condition |
| 6 | The empty-template guard's position is changed later "for symmetry" with the other `400`s, silently flipping §6a row 7 | low | A different Arabic message for the same input; no functional break, but a matrix cell quietly becomes false | §4.2-7 records the position **and its consequence** as a cell, so moving it is visibly a contract change |
| 7 | Someone splits `AbwabTemplateRootNodeException` to hold the empty refusal, or overloads it | medium — §9's "do not split" reads like "do not add either" | Either violates `plan.md` §9 or produces a type with three unrelated meanings | §4.2-8 states the distinction and T202 puts the reasoning in the new type's own comment |
| 8 | Item 21(a) grows a `tabindex` or a `role="tree"` mid-execution "since we're in there" | medium | Promises a navigation contract the workshop does not implement — the exact thing the README refuses | §3, §4.2-13's mechanism is chosen to make it unnecessary, T802 amends the README to say precisely what was added |
| 9 | The keyboard path is built and does not work, because nothing focusable exists on the row | **would have been high** — this is DRIFT-4 | A shipped, unreachable a11y control | The bubbling mechanism is fixed in §4.2-13 before any code is written, and T703 verifies from all three of the row's controls |
| 10 | `Escape` stops dismissing the menu after 21(a) | low | A shipped regression on the doors page too, if `qd-context-menu` were touched | The primitive is not touched (§3); the new path leaves focus outside the menu, which is the condition its document-level handler already assumes (`context-menu.component.ts:20-27`); T703 walks it |
| 11 | The copy modal's spec is edited to accommodate T602 rather than the component fixed | medium | A weakened assertion on the only specced surface in this slice | T603 states the rule: a failure there is T602's regression, not maintenance |
| 12 | Contract regeneration is skipped because "nothing changed" | medium | A stale `swagger.json` discovered three slices later | T501 is a task with an expectation, and `check-api-contract` runs again in T903 |
| 13 | The empty-template `400` surprises the user in production: every newly created template now refuses apply until it gets a child | **certain, by design** | A product behavior change beyond the audit's framing | §11 flags it as the plan's one product consequence worth a look before execution; §6a row 6 states it plainly; T602's affordance makes it legible in the UI before the write |
| 14 | A future cache (Slice I) assumes an apply creates one door per target | low now, higher later | Wrong invalidation granularity | Recorded here and in both READMEs (T801/T802). Slice I is deliberately last for exactly this reason |
| 15 | `docs/feature-abwab-templates/` is swept while being amended | low | Deletes the document this slice exists to correct, and git history becomes the only record of a live decision | §3 states the asymmetry twice; T903 forbids it explicitly |

## 9. Obligations checklist (all must be true at close)

- [ ] `plan.md` §5.1 carries the new axiom verbatim; `:162` is unchanged.
- [ ] `plan.md` §6 was **re-derived**, and every §6a row appears in it.
- [ ] `plan.md` §5.5 is **rewritten**, not annotated; its "only at the root" conclusion is gone.
- [ ] `plan.md` `:116`, `:123`, `:140`, `:330`, the inheriting §6.3 cells, and §9 are amended.
- [ ] `plan.md:87-88`, `:337`, `:165-183`, §5.3, §5.4, §5.6, §5.7, §6.2, §8, §10–§12 are **untouched**.
- [ ] The writer copies the root's direct children, never the root; the BFS descent loop is unchanged.
- [ ] Level 1 lands at `nextOrder + i`; depth ≥ 2 keeps verbatim `OrderValue`; every touched scope is `1..N`.
- [ ] Empty-root template → `400` «القالب لا يحتوي عناصر لنسخها», raised **writer-side**, before the target reads.
- [ ] Collision is per child name; the payload is `(TargetName, ChildName)` pairs; the pre-check names them and `23505` still throws with an empty list.
- [ ] Every returned DTO carries its own node's aliases (DRIFT-1).
- [ ] `AbwabTemplateRootNodeException` is **not** split; the empty refusal has its own type.
- [ ] No `version` on any templates route; the request shape is unchanged.
- [ ] `templateNodeCount` is **not** decremented anywhere (DRIFT-2).
- [ ] `templateCopyConfirmButton`, `templateCopyPreviewNoRoot`, `templateCopyPreviewDetached` are untouched.
- [ ] The workshop tree emits `menuRequested` from three paths; `{ nodeId, x, y }` is unchanged; `qd-context-menu` is untouched.
- [ ] No `tabindex`, no `role="tree"` on any workshop row; §17's two `qd-context-menu` gaps are still recorded as open.
- [ ] `SmokeRouteCatalog` verified unchanged and the verification recorded (DRIFT-3); route-smoke tier ran at T401 **and** T901, each with a `Tests.Smoke.Data` RAN/SKIPPED statement.
- [ ] `check-api-contract` clean at close; `git status --short` empty after it.
- [ ] `Writes/Abwab/README.md`, `features/abwab/README.md` amended; `Reads/Abwab/README.md`, `Controllers/README.md`, §17 verified.
- [ ] `TESTING_DEBT.md` carries the `ux-slice-g` rows; **row 7 restated**, **row 9 widened**.
- [ ] The concept's «بجذره» is recorded as superseded; `docs/design-preview/` is not edited.
- [ ] `abwab-cards`' missing row menu is recorded as an open decision, not built.
- [ ] `evidence.md` records T101 baseline, T104 and T903 sweeps, T401/T901 tiers, T603, T703's browser walk, and T902's acceptance pass.
- [ ] **No planning folder deleted, swept, evicted, or repointed** — including `docs/feature-abwab-templates/`.
- [ ] Root `CLAUDE.md` Active Spec Kit Feature back to `None`.
- [ ] No migration, no schema change, no package install, no `dev → main` merge.

## 10. Execution note

**Every commit is green.** Phase 2 lands the new outcome variant and the retyped payload before
the writer can produce either — the union stays exhaustive, the controller's mapping compiles,
and no behavior changes, so the commit is green with an unreachable arm. Phase 3 makes it
reachable. That ordering is deliberate: the alternative (writer first) would have the handler
failing to compile against an exception type it cannot map.

The commit boundary is the bisection mechanism, so the phases are not merged for convenience.
Phase 1 in particular stays its own commit even though it touches no code: if the reversal turns
out to be wrong, the axiom amendment is the thing to revert, and it should revert alone.

**Branch:** off `dev`, PR into `dev`. Never `main`.

## 11. Stop conditions

Stop and ask if any of these is true:

1. **T103's re-derivation produces a cell whose correct outcome is genuinely undetermined.**
   Not "needs a judgment call" — undetermined, meaning two defensible outcomes with no
   precedent, invariant, or locked decision that picks between them. Bring the cell, both
   readings, and what each would cost.
2. **The writer's current behavior differs from the audit's description in a way that changes
   the design.** DRIFT-1 through DRIFT-4 are already resolved and are *not* stop conditions —
   they are the DRIFT rule's normal case. A fifth divergence that invalidates §4.2-6's "the
   descent loop needs no change" or §4.2-4's one-query pre-check is a stop.
3. **T101's baseline is not green.** A baseline failure is not a starting point.
4. **`check-api-contract` produces a non-empty diff at T501** *and* the diff touches
   `AbwabTreeDoorDto` or any request shape. A response-message or description-only diff is
   handled in-phase (§4.2-3); a shape change means something upstream is wrong.

**Flagged, not a stop — the user should see it before execution:** §6a row 6 means **every
newly created template refuses apply until it gets at least one child.** `plan.md` §6.3 records
that root-only is *"the default state of every newly created template"*, so the empty-root
refusal is not an edge case — it is the state every template passes through. This follows
directly from locked decision 2 and this plan implements it as locked; it is surfaced here
because it is the loudest product consequence of the reversal and it is easier to see stated
plainly than derived from a matrix cell. T602's affordance is what makes it legible in the UI
rather than a surprise at the confirm button.
