# Slice G — Evidence

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
