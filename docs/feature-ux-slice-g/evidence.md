# Slice G — Evidence

> **Retained evidence — 2026-08-04.** This file survived the planning-artifact lifecycle sweep
> because a live document cites it as the only existing check for the work it records
> (`docs/TESTING_DEBT.md` row 9, the T703 browser walk). Its sibling `plan.md` and the other slice folders it references were swept and
> resolve in **git history** only. The citations are left as written: this is a dated record of
> what was verified and when, and repointing it at today's READMEs would falsify it.

Plan: `docs/feature-ux-slice-g/plan.md`. Branch: `ux-slice-g`, off `dev` @ `79d3501c`.

## T101 — Baseline (dev @ `79d3501c`, clean)

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Backend/QuranDashboard.sln` | First invocation hit a transient `Internal CLR error (0x80131506)` (dotnet host crash, unrelated to this repo); re-run succeeded — Build succeeded, 0 warnings, 0 errors, 40.5s |
| No-pipeline regression | `dotnet test … --filter "...!~QuranDashboard.Tests.Smoke."` (§5 catalog) | 1086 passed, 0 failed, 0 skipped, 22s |
| Route-smoke tier | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` | 140 passed, 0 failed, 0 skipped, 52s |
| **`Tests.Smoke.Data` — RAN** | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke.Data"` | 13 passed (subset of the 140 above). `resources/db-dumps/quran-canonical/quran-canonical.dump` is present, so the fixture did not self-skip. |
| `check-api-contract` | `Backend/scripts/check-api-contract` | "API contract up to date." — clean at baseline; `git status --short` empty after the run. |
| Frontend tests | `npm test` | 193 files, 2343 tests passed, 0 failed. 220.27s. |
| Frontend build | `npm run build` | Succeeded, 20.4s. Pre-existing budget warnings (initial bundle +69.58 kB over 500 kB budget; two mushaf SCSS files over their 4 kB budget) — none introduced by this slice, carried forward as-is. |

**Baseline verdict:** both stacks green. Nothing outstanding to disentangle from this slice's own changes.

## T102 (of this evidence record) — Branch and feature record

- Branch `ux-slice-g` created off `dev` @ `79d3501c`.
- Plan committed to the branch (not to `dev`), commit `e5f46f6b`.
- Root `CLAUDE.md` "Active Spec Kit Feature" section updated: entry added for `ux-slice-g`.
  `docs/feature-abwab-templates/`, `docs/feature-ux-slice-e/`, `docs/feature-ux-slice-f/` left
  untouched — no planning-artifact sweep in this slice (§3).

## T103 — §6 re-derivation

`docs/feature-abwab-templates/plan.md` §5.1 axiom replaced verbatim (children-only; root never
copied), `:162` unchanged. §4 "Apply" and "Apply collision" rows rewritten. Route table row 5
rewritten (N created doors per target, empty-template `400` added to refusals). §5.5 rewritten —
dropped the "only collision is at the root" conclusion, kept the section's purpose (why the
copy cannot fail on an invisible constraint); the `(template_id, parent_node_id, name)` index's
role is now stated as guaranteeing the *pre-check's own name set* has no duplicates, not as
capping the collision surface at one name. §6.1 re-derived: children-only anchor case, per-
`(target, child)` collision rows (single and multi), the new empty-root-template `400` row and
its ordering ahead of the archived-target `400`, all pre-existing rows (archived collision,
section-less, nested, ancestor/descendant, unknowns) carried forward. §6.3 re-derived: empty
template flips Legal → `400`; same-template-applied-twice re-keyed to the children's names; an
archived-since row added; sibling-order cell states the level-1 offset; alias cell cross-
references DRIFT-1. §6.4: the two apply-touching cells (apply×apply same target, apply×apply
same template same target) restated for the offset and the re-keyed collision; the other three
cells (concurrent template edit, node edit×delete, template delete×apply) are unaffected and
untouched. Every row of ux-slice-g `plan.md` §6a (23 rows) is represented in the re-derived
matrix — verified by walking the re-derived tables against §6a row by row.

DRIFT-2 trap added to `plan.md` §9 (do not subtract one from `templateNodeCount`); the existing
`23505`/pre-check trap restated for pairs.

## T104 — Repo-wide grep sweep

Command (per phrase, repeated for each of the six):

```
grep -rn "<phrase>" .
```

Phrases run: «بجذره», `root becomes a new child`, `only collision is at the root`,
`created root`, `one created root per target`, `root door per target`.

| Hit | Disposition |
|---|---|
| `abwab.labels.ts:351` (`templateCopyDescription`) | In §5.4 amend list — T601 |
| `Writes/Abwab/README.md:194` ("only collision is at the root") | In §5.4 amend list — T801 |
| `IAbwabTemplateApplyWriter.cs:10` ("created ROOT door per target") | In §5.4 amend list — T201 |
| `EfAbwabTemplateApplyWriter.cs:55` ("one created root per target") | In §5.4 amend list — T303 |
| `docs/feature-abwab-templates/plan.md:140,:116,:123,:158-163,:232-249,:330` | The plan's own contract sections — amended in this phase (T102/T103) |
| `docs/design-preview/abwab-templates-concept.html:139,145` («كاملًا بجذره») | Explicitly do-not-edit — recorded as superseded instead (§4.2-16, T802) |
| **`IAbwabTemplatesWriter.cs:18-19`, `AbwabTemplateNode.cs:8-10`, `AbwabTemplateNodeConfiguration.cs:88-90`** | **New finding, not in the original §5.4 ledger** — all three justify the one-root-per-template invariant by quoting the old axiom sentence. The invariant itself is untouched (§5.2 survives), but the justification text is stale. Fixed in this task; rows added to the ledger's amend table |
| `docs/abwab-ux-audit.md:764,819,1147,726,753` | The audit document that originated this slice. **Reviewed, not edited** — it is a historical snapshot of what was found at audit time; the resolution is this slice's own plan/evidence, not a rewrite of the audit. Consistent with the "git history is the archive" rule already applied to the design-preview concept |
| `docs/feature-abwab-templates/plan.md:634` and `:629` (Phase 4 / T402-T404, "Returns the created root doors…") | **Reviewed, not edited.** These are historical execution-record prose for already-shipped Slice A/B tasks, inside a section §5.4 deliberately does not touch (only §5.1/§4/§5.5/§6/§9/route-table are amended). The live contract is those amended sections, not the phase narrative. Treated the same as "Explicitly NOT touched" §5.6/§5.7/§8/§10-§12 |
| `Frontend/quran-dashboard-ui/e2e/fixtures/abwab.ts:116` ("UI-created roots") | False positive — about UI-created door roots in general, unrelated to template apply |
| `words-explainer.content.ts:78`, `words-pages-hero.html:160` («بجذره») | False positive — Arabic word-morphology copy ("its root and paradigm"), unrelated homonym |

No hit was left undisposed. Three findings outside the original ledger were fixed in this
phase; two categories (audit doc, historical phase narrative) were reviewed and deliberately
left unedited with rationale recorded above.

## Phase 2 — T204: SmokeRouteCatalog verification

`SmokeRouteCatalog.cs:356-359` entry read and compared against DRIFT-3's expectation:

```
new("api/abwab/templates/{templateId:int}/apply", "/api/abwab/templates/1/apply", HttpStatusCode.NotFound)
{
    Method = HttpMethod.Post, ParityOnly = true,
}
```

Matches exactly — same route template, same probe URL, same `NotFound` constraint, same
`ParityOnly = true`. No edit made, per DRIFT-3. `dotnet build Backend/QuranDashboard.sln`
green after T201-T204 (additive only, writer untouched): 0 warnings, 0 errors, 29.9s.

## Phase 3 — T301-T304: the writer rewrite

All five pieces (collision exception retype, outcome/handler, `ApiMessages` formatter, the
writer's pre-check, the empty guard, the seed/offset, and the per-node alias fix) landed
together, per the plan's note that they are green only as one commit.

- `AbwabTemplateApplyCollisionException` now carries `IReadOnlyList<AbwabTemplateApplyCollisionPair>`
  as `Collisions`. `ApplyTemplateOutcome.Collision` retyped to match; handler reads `ex.Collisions`.
- `ApiMessages.cs` gained its first `using` (`QuranDashboard.Application.Abstractions.Abwab`) — no
  CS0118 collision, because the pair type keeps its `Pair` suffix distinct from the
  `AbwabTemplateApplyCollision` constant, exactly as §4.2-9 anticipated.
- Writer reordered: `childrenByParentNode` now builds immediately after `rootNode` is found (moved
  ahead of the target reads), the empty-root guard reads `rootChildren` off it and throws before any
  target is read, the collision pre-check queries the child-name set instead of the root's name and
  builds `(target, child)` pairs in caller-target-order then template-sibling-order, and the seed
  loop replaces the single `copiedRoot` with one door per `rootChildren[i]` at `nextOrder + i`. The
  BFS descent loop (`:107-134` pre-rewrite) is untouched. The response `Select` now normalizes each
  `copied.Node.Aliases` instead of a single `rootAliases` (DRIFT-1).
- `dotnet build Backend/QuranDashboard.sln`: 0 warnings, 0 errors, 28.9s.
- `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab"`: 46 passed, 0 failed,
  0 skipped, 7s — no regression in the existing Abwab suite (no dedicated apply-writer behavior
  test exists yet; that gap is `TESTING_DEBT.md` row 7, restated in Phase 8).

## Phase 4 — T401: the route gate the contract change owes

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Backend/QuranDashboard.sln` | 0 warnings, 0 errors, 28.8s |
| `Tests.Api` | `--filter "FullyQualifiedName~QuranDashboard.Tests.Api"` | 60 passed, 0 failed, 0 skipped, 11s — matches catalog |
| Route-smoke tier | `--filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` | 140 passed, 0 failed, 0 skipped, 47s |
| **`Tests.Smoke.Data` — RAN** | subset of the 140 above, 0 skipped | dump present, fixture did not self-skip |
| No-pipeline regression | `--filter "...!~QuranDashboard.Tests.Smoke."` (§5 catalog) | 1086 passed, 0 failed, 0 skipped, 18s — unchanged from T101 baseline |

`SmokeCoverageParityTests` passed inside the 140 — the proof DRIFT-3 was read right: the apply
route's `SmokeRouteCatalog` entry needed no edit, and the parity gate agrees.

## Phase 5 — T501: contract regeneration, expected clean

`Backend/scripts/check-api-contract` → **"API contract up to date."** — clean, as expected
(§4.2-3): the request shape is unchanged, the response type is unchanged, and the controller
carries no `[ProducesResponseType]`, so the new `400` adds nothing to the OpenAPI document.
`git status --short` empty after the run — nothing to commit for this phase.

## Phase 6 — T601-T603: the copy modal tells the truth

- `templateCopyDescription` and `templateCopyPreview` rewritten to children-only wording;
  `count` in the preview is untouched (§5.3/DRIFT-2). New `templateCopyEmptyTemplate`.
  `templateCopyPreviewNoRoot`/`Detached`/`templateCopyConfirmButton` untouched.
- `hasElements = computed(() => templateNodeCount() > 0)` added; the preview block swaps to a
  `qd-state variant="empty"` when false, and the confirm button's `[disabled]` gains the
  `!hasElements()` term alongside the existing `pickedIds().size === 0` term. No new SCSS rule —
  `qd-state` fits the existing flex-column preview container.
- `npm test -- --include="…/abwab-template-copy-modal/**/*.spec.ts"`: 11 passed, 0 failed —
  unedited. The spec's default `templateNodeCount` fixture (4) keeps every existing case's
  `hasElements()` true, so T602 could not have touched their outcomes.

## Phase 7 — T701-T703: item 21(a), browser-walked

Backend started with Kestrel serving the frontend's mkcert PEM
(`ASPNETCORE_Kestrel__Certificates__Default__Path`/`KeyPath` → `Frontend/quran-dashboard-ui/
localhost.pem`/`localhost-key.pem`), `dotnet run --project api/QuranDashboard.Api --launch-profile
https`; frontend `npm run start:https`. Local Postgres, template «الثمرات» (root + 4 children),
one child («اهدافه») given a grandchild («هدف فرعي») to make it genuinely two-level for this walk
— left in place afterwards and reused as the T902 acceptance-pass fixture.

Walked at `/abwab/templates`, jsdom cannot reach any of this (Precondition table — no spec exists
for this component):

| Step | Result |
|---|---|
| Right-click a row | Custom `qd-context-menu` opens at the pointer; the browser's native menu does not appear |
| `Escape` | Dismisses |
| Focus `⋯`, then `Shift+F10` | Menu opens anchored under the row's start edge (not the viewport origin) |
| Focus the chevron (via click, toggling collapse), then `Shift+F10` | Same menu, same anchor — confirms the keydown bubbles from whichever control has focus, not just `⋯` |
| Tab from the chevron to `＋`, then `Shift+F10` | Same again — third control confirmed |
| Click outside (backdrop) | Dismisses |
| `⋯` and right-click on the **root** row («الثمرات») | Both open the menu with **«حذف القالب»** in place of «حذف العنصر» — the root-vs-node swap (page-side, already built in Slice A/21(a) precondition) is untouched by the two new emit paths |

**Not forced this walk:** the RTL near-edge overflow gap. No row in this template's tree
happened to render near the viewport edge, and the primitive itself is untouched (§3), so there
is no reason to expect it regressed — recorded as not directly observed rather than claimed.

Two Chrome-DevTools-MCP screenshot calls timed out transiently mid-walk (`Page.captureScreenshot`
timeout) with no console errors and no loss of app state across the retry — treated as an
automation-channel hiccup, not an application defect.

## Phase 8 — T801-T803: docs true again

- `Persistence/Writes/Abwab/README.md:179-199` re-derived: the "template is a door subtree"
  paragraph now states children-only copy, the response's new meaning (N doors/target,
  `IReadOnlyList<AbwabDoorDto>` type unchanged), and the level-1 offset; the descend-one-level
  paragraph is untouched (BFS loop unchanged); "the only collision is at the root" rewritten to
  the per-`(target, child)` rule plus the new empty-template `400` and its ordering ahead of the
  archived-target check. `:20-23`, `:44-46`, `:47-49`, `:224-229` verified still true, untouched.
  `Persistence/Reads/Abwab/README.md:99-101` and `api/…/Controllers/README.md` verified
  verify-only — neither claims anything the reversal falsifies.
- `features/abwab/README.md`: new bullet stating the children-only reversal (no such claim
  existed pre-slice to amend); workshop-tree list-role paragraph amended to name the
  `ContextMenu`/`Shift+F10` path and why the role stays unclaimed; the design-preview concept's
  «كاملًا بجذره» recorded as superseded beside its citation (not edited, §4.2-16);
  `abwab-cards`' missing row menu recorded as an open decision for a later slice. `:17-25`
  endpoint counts verified unchanged (no route added). `.architecture/UI_STYLE_SYSTEM.md` §17's
  `qd-context-menu` entry verified verify-only: both recorded gaps (no viewport clamping, no
  focus management into the menu) stay open, matching T703's empirical walk — the keyboard path
  leaves focus outside the menu, same as the other three paths.
- `docs/TESTING_DEBT.md`: row 7 (`abwab-templates` section) restated to the reversal's surface;
  row 9 widened to name the two new menu paths. New `## ux-slice-g` section added with G1-G4.

## Phase 9 — T901: Tier C

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Backend/QuranDashboard.sln` | 0 warnings, 0 errors, 35.4s |
| No-pipeline regression | `--filter "...!~QuranDashboard.Tests.Smoke."` | 1086 passed, 0 failed, 0 skipped, 20s — **unchanged from T101** |
| Route-smoke tier | `--filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` | 140 passed, 0 failed, 0 skipped, 49s |
| **`Tests.Smoke.Data` — RAN (again, at close)** | subset of the 140, 0 skipped | dump present |
| Frontend tests | `npm test` | 193 files, 2343 tests passed — **identical to T101**; this slice wrote no spec |
| Frontend build | `npm run build` | Succeeded, 19.6s; same three pre-existing budget warnings, none new |

Tier B is satisfied by this same run: a completed backend+frontend vertical slice
(`TESTING_STRATEGY.md` §3 Tier B). No Tier D trigger fires — no `DataPipelines` code, no
importer, no migration, no canonical resource, no shared persistence reaching pipeline tables.
No count moved against the T101 baseline; nothing to explain as a finding.

## Phase 9 — T902: browser acceptance pass for item 20

Against the local dev DB, both through the UI and (for the two cases the UI deliberately makes
unreachable) directly against the live API on `https://localhost:5015`:

| # | Scenario | Result |
|---|---|---|
| 1 | Apply «الثمرات» (root + 4 children, one with a grandchild) to two live root doors («الجهاد», «الحمد والثناء») via the copy modal | `201`. Each target gained **4 top-level children** (اهدافه/واجباته/انواعه/مكانته at order `1..4`) — not one wrapper — and اهدافه's own grandchild «هدف فرعي» came through intact at order 1. Door count `13 → 23` (+10 = 5 nodes × 2 targets) |
| 2 | Apply the same template again to «الجهاد» alone | `409`: `لم يتم النسخ — أسماء موجودة داخل الأبواب المستهدفة: «الجهاد» ← «اهدافه»، «الجهاد» ← «واجباته»، «الجهاد» ← «انواعه»، «الجهاد» ← «مكانته»` — all four pairs named, in template sibling order. Door count stayed **23** — nothing created anywhere |
| 3 | Apply an empty template («العبرة», `nodeCount = 0`) | Modal: empty-state block (`templateCopyEmptyTemplate`) replaces the preview, confirm button stays disabled even with a target picked. Direct `POST /api/abwab/templates/5/apply` (bypassing the disabled control): `400` `القالب لا يحتوي عناصر لنسخها` — the writer-side refusal, verified as the actual guarantee behind the UI courtesy |
| 4 | Apply to a target that is **both nested and section-less** («المصطفى صل الله عليه وسلم», id 438, `parentId=338`, `sectionId=null`) | `201`, 4 created doors, every one `"sectionId":null,"parentId":438`, `orderValue` `1..4` — cascade inheritance and depth both correct in one case |
| 5 | Single-child template (new template, one child) applied to «الحمد والثناء» (already had 4 live children from case 1) | `201`, the one child lands at `orderValue: 5` = `nextOrder + 0` — the offset degenerates to the pre-reversal shape exactly as §6a row 8 predicts |
| 6 | Edit the live template afterward (renamed node 11, «اهدافه» → «اهدافه المعدلة») | Every already-copied door across all three prior targets still reads **«اهدافه»**, unchanged — detachment confirmed |

All seven `§6a`-anchored scenarios in the plan's T902 checklist are covered by the six rows
above (case 1 covers two of the checklist's items — two-target apply and sibling-order/subtree
completeness — in one pass).

**DRIFT-1, discriminating check.** Every template node touched above had empty aliases, so none
of the responses so far could tell a per-node alias apart from the old single-`rootAliases`
bug (the root itself never had aliases either, so both behaviors would return `[]`). Set two
*different* children of «الثمرات» to two *different* aliases (`اهدافه المعدلة` →
`["علامة الأهداف"]`, `واجباته` → `["علامة الواجبات"]`) and applied to a fresh target
(«قصص الانبياء», door 537): the resulting doors report **`["علامة الأهداف"]`** and
**`["علامة الواجبات"]`** respectively — each its own node's aliases, not one shared value.
DRIFT-1 confirmed fixed, not just built.

## Phase 9 — T903: close-out sweep

Re-ran T104's six-phrase grep across the whole repo. Every hit from T104 is unchanged in
disposition (still in the amend list, still do-not-touch, or already a recorded finding) except
one new one, found because this pass came after Phase 8's own edits landed:

| Hit | Disposition |
|---|---|
| `docs/feature-abwab-templates/plan.md:171` (§5.2, "a template with two roots makes 'the template root becomes a new child' undefined") | **Covered by an explicit, locked plan decision — not amended.** ux-slice-g's own precondition table and §5.4 "Explicitly NOT touched" list both name §5.2 (`:165-183`) as surviving untouched, reasoned as "the root becomes purely a naming/container row." Unlike the three T104 code-comment findings (which were plain omissions from the original ledger), this line sits inside a section the plan's authors explicitly considered and chose to leave alone — overriding that via a grep sweep would re-litigate a locked decision, which `plan.md` §3 forbids ("no litigation of the reversal"). The one-root invariant this paragraph justifies is still correct; only the axiom-quote inside its own historical reasoning is dated, and it reads as history, not as a claim about current apply behavior |

`Backend/scripts/check-api-contract`: **"API contract up to date."** — clean. `git status --short`
after the run showed only this evidence file's own in-progress edit — no stray generated-contract
diff.

Root `CLAUDE.md` "Active Spec Kit Feature" cleared back to `None`.

**No planning folder deleted, swept, evicted, or repointed.** `docs/feature-abwab-templates/`
was amended in Phase 1 and T104/T903, and is otherwise untouched — still live, per §3.
