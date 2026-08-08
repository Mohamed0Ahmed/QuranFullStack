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

**The Phase 5 Abwab smoke payoff (2026-08-06)** moved the due relation, template, section-reorder,
template-apply, and conditional-read cases into the route-smoke suite. Their rows are deleted rather
than marked done. Going forward, tests come before the feature, so a row like these should not be
openable again.

## abwab-relations (branch `abwab-relations`, 2026-07-29)

Posture: **no new tests in the feature**, by explicit decision. Verification was the existing
suites staying green plus a manual pass over the feature's own interaction checklist. Nothing in
this feature's evidence claims behavioral coverage.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| 1 | Backend write behavior — canonical pair ordering (`door_a_id < door_b_id` for all three types), `broader_door_id` direction storage, all-or-nothing multi-target add, self/unknown/archived refusals, soft delete with no revive | `Persistence/Writes/Abwab/EfAbwabRelationsWriter.cs` | The next change to the relations writer, **or** adding a fourth relation type — both have to re-derive these rules anyway |
| 2 | Backend read behavior — the dormancy join (relation visible iff its own `deleted_at` is null **and** both endpoints are live) and `RelationCount`'s live-endpoint-only counting. Also the negative side: no door **or section** write path may touch `abwab_door_relations`, so move / reorder / rename / section create-rename-delete must leave every row and count alone | `Persistence/Reads/Abwab/EfAbwabRelationsReader.cs`, `EfAbwabTreeReader.GetLiveRelationCountsAsync`, `Persistence/Writes/Abwab/` | The next change to the archive / restore / bulk-archive paths **or to either section/door writer** — dormancy rides entirely on the former, and the "structure never touches relations" invariant is enforced by nothing but the absence of code in the latter |

Rows 1 and 2 are the ones with no cover **anywhere** — not a spec, not a smoke case, not an e2e
flow. The e2e row that used to sit here is gone: `e2e/abwab-relations.e2e.ts` (slice K) now crosses
the read, the write, the count, and the row flag in one pass. It does not touch these three, which
are about the writer's own rules and the routes' status contract, not about what a browser sees.

## abwab-templates (branches `abwab-templates-a` / `abwab-templates-b`, 2026-07-29)

Posture: **no new tests in the feature**, the second consecutive feature under it. Verification
was the existing Frontend suite staying green, with no spec file added or removed, plus a
manual pass over the feature's own interaction checklist. Nothing in this feature's evidence
claims behavioral coverage.

**One exception, added by the Slice B review-fix round:** `abwab-templates.facade.spec.ts`
pins the selected template's identity — a
failed switch shows no template rather than the previous one, and a failed refresh of the same
template keeps it on screen. It exists because the round fixed a defect that let the copy modal
preview one template while apply sent another; a correctness fix of that shape is not deferrable
into this file. Row 9 is narrowed accordingly, not deleted.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| 6 | Backend template/node write behavior — one root per template, sibling-name uniqueness inside a template, node delete taking its whole subtree, sibling resequencing to `1..N`, the root's refusal to reorder or delete, template delete touching one row | `Persistence/Writes/Abwab/EfAbwabTemplatesWriter.cs` | The next change to the templates writer — it has to re-derive every one of these rules anyway |
| 7 | **The deep copy — restated for ux-slice-g's children-only reversal, same row, new surface.** The root's direct children enumerated and copied recursively (never the root itself); the level-1 `nextOrder + i` offset with every touched scope staying `1..N`; depth ≥ 2 keeping verbatim `OrderValue`; ~~`section_id` inheritance at every depth~~ (**paid** by `AbwabTemplateApplyBehaviorTests.ApplyAsync_CopiesCarryTheTargetsSectionAtEveryDepth`, abwab-mandatory-section); alias rows and each DTO reporting its own node's aliases; all-or-nothing across N targets; the empty-root-template `400` raised before the target reads; and the per-`(target, child)`-name `409` | `Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs` | The next change to the apply path **or to `abwab_doors`' per-sibling unique index**. Unchanged trigger — still the only place in the repo where door rows are created by something other than `CreateAsync` |
| 9 | Frontend workshop behavior — the flat→tree build for nodes and the tree editor's collapse and quick-add paths. **The node modal is no longer in this row** — `abwab-template-node-modal.component.spec.ts` covers its dirty-close confirm and its submit/validation path. **The order editor is no longer in this row either** — `abwab-template-tree.component.spec.ts` pins Enter-commit/Escape/cancel-on-blur and the chip's keyboard route. **The copy modal's picker is no longer in this row** — `abwab-template-copy-modal.component.spec.ts` covers it, and the picker itself is now the shared `abwab-door-picker`. **The facade's selected-template identity is not in this row either** — `abwab-templates.facade.spec.ts` covers it. **Narrowed again by the review fixes:** the *placement math* is unit-pinned in `shared/ui/context-menu/context-menu-placement.spec.ts` (RTL/LTR default, flip, clamp), so what remains browser-walk-only is exactly the tree's two row-menu **paths** — right-click with `preventDefault`, and `ContextMenu`/`Shift+F10` anchored via `getBoundingClientRect` — because jsdom cannot produce a usable `contextmenu` event or a meaningful `getBoundingClientRect`; the only shipped in-browser placement assertion is on the doors page (`e2e/abwab-operations.e2e.ts`) | `features/abwab/components/abwab-template-tree/`, `abwab-template-node-modal/`, `pages/abwab-templates-page/` | The next time the workshop changes shape |
| 10 | One e2e flow — author a two-level template, copy it into two doors, see the subtree under both, then edit the template and watch the copies **not** change | `Frontend/quran-dashboard-ui/e2e/` | Same trigger as row 9; it is the only check that would catch a detachment regression end to end, and detachment is the cell this feature is most likely to have misunderstood |

Row 7 is the one with no cover **anywhere**. Row 10 is the cheapest thing that would cover the
most: it crosses the template writes, the deep copy, the doors read, and detachment in one pass.

**Not debt, and not deferrable:** the `abwab-door-fields-form` extraction is covered by
`abwab-door-modal.component.spec.ts`, which still exercises the extracted fields end to end —
the extraction preserved every `data-testid` through a `testIdPrefix` input precisely so that
spec keeps pinning the behavior it always pinned. (That spec has since grown its own cases, so
this is an invariant about *what it covers*, not a claim that it went unedited.) The form has no
spec of its own, and does not need one while that remains true.

## ux-slice-f (branch `ux-slice-f-sections`, 2026-08-01)

Posture: **no new test suites**, rush-period decision (plan §4.1-6). Existing suites ran before
merge; the route-smoke tier is exempt from the posture and ran regardless (not debt-able, see
above). Nothing in this feature's evidence claims backend behavioral coverage for the new writer
method — the frontend cells in Phases 5-7 do claim coverage for what they assert.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| F1 | **The section reorder writer's behavior** — contiguous `1..N` across every live section, first→last and last→first, single-section no-op, out-of-range refusal, the stale-token 409, and the sibling-token 409 that makes the resequence all-or-nothing | `Persistence/Writes/Abwab/EfAbwabSectionsWriter.ReorderAsync` | The next change to the sections writer, **or** the fix for the `CountAsync + 1` / non-resequencing-delete gap (F2) — both have to re-derive these rules anyway. `AbwabDoorWriteBehaviorTests.cs` (`ReorderAsync_ProducesContiguousOrderValues`) is the shape it copies |
| F2 | **The duplicate-`OrderValue` condition itself** — create assigns `count(live) + 1` while delete resequences nothing, so two live sections can share an `OrderValue`; nothing anywhere asserts the reorder stays correct under it, and nothing asserts the heal | `EfAbwabSectionsWriter.cs` (`CreateAsync`, `DeleteAsync`) | Whoever fixes the create/delete gap. Until then the correctness rests entirely on the `(OrderValue, Id)` tie-break (`Writes/Abwab/README.md`), which is documented and untested |

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
| G4 | **The copy modal's empty-template affordance** — that the confirm button disables at `templateNodeCount() === 0` and the preview swaps to the empty state. **Cheapest row in this table**: the modal's spec already exists and covers everything else it does, so this is one `it` block, not a suite | `abwab-template-copy-modal.component.spec.ts` | The next change to the copy modal |

## ux-slice-h (branch `ux-slice-h`, 2026-08-01)

Posture: **no new test suites**, rush-period decision (plan §4.1-7). Existing suites ran before
merge; no route-smoke tier runs — no `Backend/` file is in scope for this slice (§4.1-8), so
there is nothing catalogued `ParityOnly` to narrow here either. `core/layout/top-navbar/` had no
unit spec before this slice and gains none — the rows below are as much about the surface's
pre-existing thinness as about what this slice adds to it.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| H1 | **The navbar itself, wholesale** — no unit spec exists at all: menu open/close state, `openMenuKey` mutual exclusion (now load-bearing for three menus instead of two), Escape/outside-click dismissal, `aria-expanded`, the inert-under-lock binding. This predates the slice; collapsing the state machine to one shared field raises the cost of it staying unpinned | `core/layout/top-navbar/` | The next change to the navbar or the nav model — auth-gated entries, a fourth dropdown, or Slice I if caching adds any nav affordance |
| H2 | **The §6a active-state matrix** — the query-param cells (`/abwab?archive=1` and its neighbors) are exactly where a `routerLinkActiveOptions` regression hides, they are assertable in jsdom with a router harness, and nothing asserts them | `top-navbar.component.html`, `core/navigation/nav-menu.ts` | The next nav-entry addition, or any change to the abwab URL contract's `archive` key |
| H3 | **The mobile flattened children** — nesting, indentation, parent-row navigability, per-row active state | `top-navbar.component.{html,scss}` | The next mobile-nav change |
| H4 | **One e2e flow for the new dropdown** — hover «الأبواب», click «الأرشيف», land on `/abwab?archive=1` with the archive view open. `shell-nav.e2e.ts` is the shipped template; this is one ~10-line test in an existing file, the cheapest row here | `e2e/shell-nav.e2e.ts` | The next time the navbar or the abwab URL contract changes shape |

H4 is the honest one to flag: the posture's logic applies to it same as the rest, but the file,
fixture, and pattern all exist, so the marginal cost is a fraction of H1-H3.

## ux-slice-i (branch `ux-slice-i`, 2026-08-02)

Posture at the time: **no new test suites**, rush-period decision (plan §4.1-8). Every existing
suite ran before merge, including the route-smoke tier — required here because response semantics
changed on three existing routes. No `SmokeRouteCatalog` entry was owed: no route was added, and
the smoke client then sent no `If-None-Match`, so every catalogued expectation still held.

That posture left the series' highest-risk correctness work — the backend's first invalidation
machinery and the frontend's first conditional request — signed off on a browser walk whose record
has since been swept.

**The conditional-request half is no longer uncovered.** Authorization Phase 5 paid row I2 across
two Smoke classes, both catalogued in the Smoke lane
(`TestSupport/Execution/test-gates.tsv:246` and `:249`):

- `SmokeAbwabConditionalReadTests` drives the matching validator to a bodiless `304` and asserts it
  issues no database command (`:33`), the non-matching validator to a fresh `200` (`:53`), and the
  same pair on template detail (`:67`, `:88`);
- `SmokeAbwabTemplateReadTests` drives an unknown id carrying a crafted `If-None-Match` to a `404`
  that returns no validator (`:13`).

Row I2 and the F-34 cross-reference were deleted when those tests landed. The rows that remain below
are the parts those classes do not reach.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| I1 | **The generation lifecycle** — capture-before-load never serving a validator newer than its data; the `finally` bump firing on the partially-committed implicit-transaction paths; boot-scoped validators never colliding across restarts. All three hold by construction and are asserted by nothing | `Infrastructure/Caching/Abwab/` | The next cached resource, **or** the multi-instance migration (`Persistence/Reads/Abwab/README.md`) — a shared-generation implementation has to prove exactly these properties anyway |
| I3 | **The templates facade's `304` path** — keeps value and validator, sets no error, ends loading; the id-keyed selected validator never travels across templates and is dropped by `clearSelection`. Assertable today with no new harness: these specs stub the api object, so a `304` is `throwError(() => new HttpErrorResponse({ status: 304 }))` and a validator round-trip is `of(new HttpResponse({ body, headers }))` — **not** `HttpTestingController.flush`, which these specs do not use. **The snapshot-facade half is PAID** (slice K): `abwab-snapshot.facade.spec.ts` now covers the 200 round-trip, the 304 keep-value-and-validator, the failed-refresh keep, and the headerless-null case, because the validator became the relations cache's identity and stopped being an internal detail | `abwab-templates.facade.spec.ts` | The next change to the templates facade or to the api layer's response shape |
| I4 | **The just-wrote invariant end to end** — load `/abwab`, rename a door, assert the refetch was a `200` (not a `304`) and the new name renders. The whole design exists for this row and only a browser proves it | `e2e/` | The next write path added to abwab, or the multi-instance migration — both re-open the ordering question |

I3 is the honest one to flag: unlike H's rows it needs no browser and no new harness. Its
snapshot-facade half was paid in slice K, when the validator became the relations cache's identity;
what remains is the templates facade, and deferring that is a choice, not a constraint.

## abwab-mandatory-section (branch `feature/abwab-mandatory-section`, 2026-08-02)

Posture: the feature's own behavior is covered — the writer's rejections, the restore-destination
resolution including both cross-operation defects, the `NOT NULL` column and its `23502`, the tree
snapshot's `sectionRetired`, the confirm primitive, the restore modal, and the rewritten e2e all
have tests. Row 7's `section_id` obligation above is paid outright. What follows is debt this work
**surfaced or inherited**, not debt it created.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| J1 | **`EfAbwabDoorsWriter` is past the 600-line file threshold** — and has been since before this feature (`wc -l` gives the current figure; the review-fix branch's dead-code removals shrank it without clearing the breach). Not a coverage gap but a structural one, and the reason every change to it is harder to review than it should be. A split (create/move/order/archive-restore) is a dedicated slice; it was an explicit non-goal here, since mixing a refactor into a semantics change makes both unreviewable | `Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs` | Blocked on nothing. The next substantial change to the writer — do it first, not alongside |

## Enumeration drift tests (doc-contradiction sweep, 2026-08-04)

The sweep found that **every hand-maintained enumeration in the long-lived docs had drifted** —
scroll-lock surfaces, `.qd-modal-backdrop` consumers, the `--qd-z-*` scale, frontend feature
lists, skill counts, test counts. The one enumeration that had **not** drifted is
`SmokeRouteCatalog`, because `SmokeCoverageParityTests` fails by name when it does.

Those docs have been rewritten to state the rule and point at the source of truth instead of
counting (so nothing below is a stale-doc bug any more). What is owed is the mechanism that made
the route catalog trustworthy, for the three enumerations that are genuinely **binding** — where
a reader needs the membership set, not just the rule. Each is a parity test modelled on
`SmokeCoverageParityTests`: assert both directions between the declared set and the live set.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| E1 | **The `--qd-z-*` layer scale** — that every `--qd-z-*` token declared in `_tokens.scss` appears in the §4 rung order, and that no SCSS in `src/` writes a bare `z-index` outside the scale. The scale is binding (a rung inversion breaks a real surface, as the sticky-navbar case proved) and nothing asserts it | `src/styles/_tokens.scss`, `.architecture/UI_STYLE_SYSTEM.md` §4 | The next `--qd-z-*` token added or reordered — the change that would otherwise silently break the rung order is exactly the one that should carry the test |
| E2 | **The chrome-inert blast radius** — that every template applying `qdModalScrollLock` makes `.qd-navbar` inert, and that no dialog renders `.qd-modal-backdrop` without acquiring the lock. `qd-confirm-dialog` composing the directive means any new confirm silently joins this set, which is precisely why it needs a test rather than a list | `shared/ui/modal-scroll-lock/`, `core/layout/top-navbar/` | The next surface that acquires the scroll lock — including any new `qd-confirm-dialog` consumer |
| E3 | **The `.qd-modal-backdrop` consumer set** — that every modal/dialog composes the shared backdrop rather than rolling its own, so the phone padding rule cannot be forked per consumer | `src/styles/`, `shared/ui/confirm-dialog/`, the abwab and words modals | The next modal or dialog added anywhere in `src/app/` |

Writing these three is the **next** pass; this section records that they are owed, not that they
exist. Until they land, the rule-plus-pointer wording in the docs is the whole safeguard.

## Carried out of deleted artifacts (fold-then-delete pass, 2026-08-04)

The reports, reviews, and audits under `docs/` and `Backend/report/feature-*/` were deleted on the
doctrine that a stale review is worth less than a fresh one, which can be run on demand. Deleting
them is only safe if the items still **open** move somewhere live. These are those items. Each was
re-checked against current code on 2026-08-04; anything already fixed was dropped rather than
carried, and the check is named so the next reader can redo it cheaply.

| # | Uncovered / open area | Where | Pays it |
|---|---|---|---|
| C1 | **`BulkMoveAsync`'s validation ordering** (sweep finding F07, still undecided). Two test comments (`AbwabDoorWriteBehaviorTests.cs:332`, `SmokeAbwabWriteTests.cs:771`) still state the destination section is resolved before the doors are loaded; the code does the opposite, `Persistence/Writes/Abwab/README.md` now documents the code's actual order and names both comments, and no test discriminates the two orderings. Deliberately left as-is by three consecutive passes — it is a code question, not a doc question | `Persistence/Writes/Abwab/EfAbwabDoorsWriter.BulkMoveAsync`, the two test comments above | The next change to the bulk-move path. Whoever touches it must pick an ordering, write the test that tells the two apart, and fix the two test comments to match |
| C2 | **The remaining open performance findings.** B1/B2/B3/B6 and frontend F1/F4 are **paid** — `WordTypesRedundancyReadTests`, `LemmaStemSummaryOverfetchReadTests`, `LemmaStemWordsPagingRedundancyReadTests` pin the backend three, the two mushaf runners now retain and unsubscribe an `activeSubscription`, and neither Quran-text card still animates `color`. Still open, all Low: the ayah-study/word-analysis point-query fan-out, the eight-command cold mushaf page, and `GET /api/dashboard/info` refetching on every dashboard mount | `Persistence/Reads/Quran/MushafReader/EfAyahStudyReader.cs`, `EfWordAnalysisReader.cs`, `EfMushafPageReader.cs`, `features/dashboard/pages/dashboard-home/` | The next change to any of those three readers, or the next remote-database latency complaint — the fan-out only matters when the round trip stops being loopback |
| C3 | **Unique Words drilldown work outliving its page.** `unique-words-page.component.ts` `ngOnDestroy` unbinds the list facade but never closes the root drilldown facade, so a summary or detail request in flight at navigation time still lands and updates offscreen state. Low: finite, at most one request, and the response is cached | `features/words/pages/unique-words-page/`, `state/unique-words-drilldown.facade.ts` | The next change to the drilldown facade or the page's teardown. `mushaf-*-load.runner.ts` is the shape it copies, since that is how the equivalent mushaf finding was closed |
| C4 | **No engineering review has been run since 2026-07-18.** That review's two BLOCKING findings and every MAJOR re-checked on 2026-08-04 are closed in current code (the route-spec module-cache pollution, the morphology fixture overwriting canonical import evidence, the health endpoint's 200-on-unhealthy, the post-auth rate limiter, the plaintext production credential, Application-layer file-system access, the swallowed `InvalidDataException` message). Its remaining MINOR/NOTE items were **not** individually adjudicated and were not carried — they are clarity items from before three merged slice series | whole repo | Re-run the review rather than reading the old one. This row exists so "no review since" is a recorded fact instead of an assumption |
| C5 | **The canonical import evidence has no drift guard.** `Backend/report/feature-008-*/` and `feature-009-*/` hold the only surviving record of source verification, per-source hashes, exclusions, and provenance warnings for the translations and navigation-metadata imports. Those reports were **kept, not folded**, because the counts have nowhere to be asserted: the canonical smoke dump pins only the five morphology baseline tables (`create-smoke-dump`), so no existing tier can see a translations or navigation row count. Folding the numbers into a README without a guard would turn evidence into rumour | `Backend/report/feature-008-*/`, `feature-009-*/`, `Backend/scripts/create-smoke-dump` | Whoever extends the canonical dump beyond the morphology baseline, **or** the next translations/navigation re-import. Either one makes the assertion cheap; until then the reports stay |

## Comment purge follow-ups (branch `comment-purge`, 2026-08-04)

The purge deleted comments that asserted things the code does not do. Two of the facts it
touched cannot be settled by reading code, so they are recorded here instead of being guessed at.

| # | Uncovered / open area | Where | Pays it |
|---|---|---|---|
| P1 | **The `'Uthmanic Hafs'` trigger mechanism is UNVERIFIED.** Its `font-display: block` comment states the face "renders via ligature substitution, keyed off ASCII trigger strings (ayah-marker glyphs)". The face's only production consumer is `--qd-font-quran-ayah-marker` (`_tokens.scss:70`) applied at `mushaf-word.component.scss:29`, and unlike the three QCF packs it has **no ligature key map** in `features/mushaf/assets/` — so nothing in the repo confirms or refutes the mechanism. The comment was deliberately LEFT IN PLACE: an unverified claim is not a false one, and the `font-display: block` decision it carries is Quran-safety and correct regardless | `src/styles/_typography.scss` ('Uthmanic Hafs' block), `mushaf-word.component.scss:29` | Someone with font knowledge inspecting `UthmanicHafs_V22.ttf`'s GSUB table, **or** the next change to the ayah-marker rendering path. Confirm the mechanism and trim the comment to what is true, or correct it |
| P2 | **The measured contrast ratios have nothing asserting them.** Seven token/surface pairings carry measured WCAG ratios that were folded out of `_tokens.scss` comments into `src/styles/README.md` Invariants. Nothing recomputes them, so re-tuning a token by eye silently drops a pairing below target — exactly the drift a fold turns from a comment into a rumour. **The test must assert**, for each row of that README table, that the WCAG 2.x contrast computed from the two resolved OKLCH token values meets its floor: `--qd-ayah-card-bg` vs Quran text >= 12.7:1 and vs muted meta >= 4.5:1 (AA); `--qd-accent-text` on light surfaces >= 7:1 (AAA); `--qd-warning` on `--qd-warning-tint` >= 4.5:1 and as a non-text dot on the navy footer >= 3:1 (AA non-text); danger text on `--qd-danger-tint` >= 4.5:1; `--qd-success-tint` vs `--qd-success` >= 4.5:1. Assert **floors, not exact equality** — the stated figures are measurements and will drift with rounding. Both themes must be covered: `_themes.scss` overrides `--qd-ayah-card-bg` to `--qd-surface` in dark, so the dark pairing is a different computation, not the same one | Test to live at `src/styles/token-contrast.spec.ts` (Vitest already globs `*.spec.ts` under `src/`), reading `src/styles/_tokens.scss` and `_themes.scss`; ratios listed in `src/styles/README.md` Invariants | The next change to any `--qd-*` colour token, or the next theme/palette pass. Whoever re-tunes a colour is already holding the contrast question |

## abwab-review-fixes (branch `abwab-review-fixes`, 2026-08-04)

The whole-feature engineering review's test-coverage findings. **Most of them were not new debt.**
Six findings named an untested behavior; adjudicated against this ledger and against the code they
resolve to one new row, two behaviors that gained real specs during the fix branch, and three
areas that already had rows here. The review's own F-34 entry flagged the double-count risk; it was
right, and it applied to two more.

- **F-65** (`abwab-template-node-modal` had no spec) and **F-69** (`qd-context-menu` had no spec)
  are **paid, not deferred**, in two installments each. F-65: the dirty-close half landed with the
  accessibility fixes; the submit/validation half (empty-name refusal, failure keeps the modal
  open, success closes) landed with the independent-review fixes in the same spec. F-69: the
  naming/focus spec landed first; the RTL-placement/flip/clamp math became testable when it was
  extracted into the pure `shared/ui/context-menu/context-menu-placement.ts`, and
  `context-menu-placement.spec.ts` pins all four branches — placement no longer rides on the
  opt-in e2e tier. No row.
- **F-13** (relations canonical pair, broader-door direction, derived dormancy) is already rows 1
  **and 2** of *abwab-relations* above — the derived-dormancy leg is row 2. **F-14** (apply copies
  children never root, the `(target, child)` collision key, the empty-root `400`) is already row 7
  of *abwab-templates*. **F-34** (the ETag/generation/`304` mechanism) now remains only under row
  **I1** of *ux-slice-i*: the generation lifecycle remains unpaid under its existing trigger. The
  conditional-GET contract was paid by the Phase 5 route-smoke suite, so I2 was deleted. Duplicating
  I1 here would inflate the ledger and split its obligation across two triggers.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| R1 | **The tree snapshot's alias projection.** `EfAbwabTreeReader` builds every door's alias list in one `GroupBy` + `ToDictionaryAsync` over the live alias rows, and nothing asserts it: that each door receives its own aliases and no other door's, that a door with no aliases gets an empty list rather than being absent from the dictionary, and that soft-deleted alias rows are excluded. This is the hottest read path in the feature — every tree GET runs it — and it is the one part of the snapshot projection with no cover of any kind. Row 2 of *abwab-relations* covers `GetLiveRelationCountsAsync` on the same reader; the alias half has nothing | A read-behavior test in `Backend/tests/QuranDashboard.Tests/Abwab/` over `Persistence/Reads/Abwab/EfAbwabTreeReader.cs` | The next change to the tree reader's projection or to `abwab_door_aliases` — including the alias-normalization path, which already has to re-derive what a door's alias set is |

## test-runtime prerequisite (branch `feature/security-authorization-permissions`, 2026-08-06)

Recorded by the Phase 9 formal review of the nine-commit test-runtime prerequisite
(`7aba2f98`…`9ed3a5d8`). The prerequisite deleted or moved test code under a documented
replacement rule, and every deletion had named replacement coverage. One residue survived that
rule with no assertion anywhere, so it is recorded here rather than left as folklore. It is not a
reason to hold the prerequisite and is cheap once someone is already in the file.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| TR1 | **Invalid email vectors are no longer proved to declare a null `ExpectedNormalized`.** Phase 7 deleted `EmailIdentityContractTests.Vectors_CoverValidInvalidAndNormalizedDuplicateCases`, whose three arms asserted the shape of the shared vector table. Two arms gained real production-behavior survivors — the valid arm through `EmailIdentityNormalizerTests.ValidVector_NormalizesThroughOneSharedImplementation`, and the duplicate-group arm through the new `DuplicateVectorGroup_NormalizesToOneSharedIdentity` theory, which also re-asserts the `>= 2` group size. The **invalid** arm has no survivor: `InvalidVectors` projects only `vector.Input`, so an invalid row that carried a non-null `ExpectedNormalized` would now be silently ignored rather than caught. **The test must assert** that every `EmailIdentityContractVectors.Invalid` entry has `ExpectedNormalized == null`, in the consuming theory rather than in a data self-test | `Backend/tests/QuranDashboard.Tests/Api/Access/EmailIdentityNormalizerTests.cs`, over `TestSupport/Access/EmailIdentityContractVectors.cs` | The next change to the email-identity vector table or to `EmailIdentityNormalizer` — both already have to re-derive what "invalid" means |

## authorization Phase 9 — Abwab write E2E (branch `feature/security-authorization-permissions`, 2026-08-07)

Posture: **the Abwab write E2E specs cannot run and are not claimed as passing.** Phase 5 closed the
21 Abwab write routes, and `e2e/fixtures/abwab.ts` seeds its world through four of them anonymously
(`POST /api/abwab/sections`, `POST /api/abwab/doors`, `POST /api/abwab/doors/{id}/relations`, and a
cleanup `DELETE /api/abwab/sections/{id}`). Those calls now correctly receive `401`, so 39 cases in
the `abwab` Playwright project fail during setup, before reaching any assertion. This is enforcement
working, not a regression: the specs encode a scenario — an anonymous visitor authoring Abwab
content — that the product no longer permits.

The specs were **not** deleted, skipped, or rewritten to assert less, and Backend enforcement was
**not** weakened to revive them. The 28 non-Abwab E2E cases and the new Phase 9 permission E2E cases
pass, including anonymous public browsing, URL-restored write overlays staying closed, and a
handcrafted anonymous write receiving the Backend denial envelope.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| A1 | **Every Abwab authoring flow that only a browser proves** — create/edit/move/reorder/archive/restore of doors, section create/rename/reorder/delete, relation add and remove, template create/delete/apply, and template-node add/edit/reorder/delete, each driven end to end through the real UI against a real Backend. Unit and component specs pin the dispatch wiring and the route smoke suite pins the HTTP contract, but nothing currently walks an authorized human through an Abwab write in a browser. **The harness must provide** an authenticated E2E persona whose access token the Backend test host actually validates — a signing key the API trusts plus a local user row that is `Active` and holds the exact permission under test — so that `e2e/fixtures/abwab.ts` seeds as that persona instead of anonymously, and so a permission-denied persona can be asserted against the same flow. It must not seed by bypassing HTTP authorization, because the point of the flow is that an authorized caller succeeds where an anonymous one is refused | `Frontend/quran-dashboard-ui/e2e/fixtures/abwab.ts` and `e2e/fixtures/logto.ts` (which today stubs only OIDC discovery and returns an empty JWKS, so it can mint nothing), over the `abwab` project in `playwright.config.ts` | The first explicitly approved authenticated browser-authoring expansion, **or** the first authenticated E2E persona added for any feature. This remains opt-in under `TESTING_STRATEGY.md`; it is not a standing authorization acceptance gate |

## access-admin catalogue readiness (branch `feature/034-access-catalogue-readiness`, 2026-08-08)

Posture: the fail-closed behaviour this work adds is covered — the catalogue-request failure
isolating one region, the unready-catalogue read-only editor, the absent save path, the
`expectNone` on any permissions `PUT`, and the severity routing of the operator message all have
specs. Row **AC1** is not a gap in that coverage. It is the opposite: **two passing tests now
assert that a permission code the served catalogue does not list is dropped in silence**, and the
plan's later catalogue-deduplication work has to change both of them, not one.

**The page redesign that followed added a different gap, and row AC2 records it.** `/settings/access`
is now a desktop-first master/detail workspace, and **no browser ever rendered it** — the route is
Owner-only behind a guard and this repository still has no authenticated Playwright persona (see row
A1 above), so every check on the redesign was a jsdom one. Stated plainly because it is a mitigation
and not a substitute: the layout is a near-verbatim copy of the shipped, browser-proven
`Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.scss:22-60`
— the same flex split, the same `flex: 1; min-inline-size: 0` main column, the same sticky
fixed-width aside — which is exactly what the implementation plan directed. That makes the risk
small; it does not make it observed.

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| AC1 | **The silently-understated permission diff is enshrined by two tests, not one.** `permissionCodesForSubmission` intersects the draft with the rendered catalogue, so a value that `isPermissionCode` accepts but the served catalogue omits is removed from the submitted set **and** from `permissionDiff()` — the operator ticks a box, the confirmation reports "no change", and the save omits it. Both halves are currently pinned as intended: `access-admin-permissions.spec.ts:68` (*drops unknown and group-like values from a request payload*) at the pure-function level, and `access-admin.facade.spec.ts:558` (*keeps a draft code that the catalogue no longer offers out of the submitted set*), which asserts `permissionDiff()` equals `{ granted: [], revoked: [] }` while the draft holds the extra code. **What the assertions must become:** separate the two populations. A group sentinel or a non-`PermissionCode` string must keep being dropped — that half of `:68` stays. A real `PermissionCode` absent from the catalogue must stop being dropped in silence: the facade case must assert that it either reaches `permissionDiff().granted` and the submitted set, or that the save is refused with a message the operator can see, and must no longer accept a diff that disagrees with the draft. `core/auth/permission-code.ts`, the hand-maintained allowlist the whole filter rests on, still has **no spec file at all** | `Frontend/quran-dashboard-ui/src/app/features/access-admin/models/access-admin-permissions.spec.ts`, `state/access-admin.facade.spec.ts`, over `models/access-admin-permissions.ts` (`permissionCodesForSubmission`) and `state/access-admin.facade.ts` (`permissionDiff`, `permissionCodesForAssignment`) | The catalogue-deduplication work that removes the hand-duplicated allowlist in `core/auth/permission-code.ts` — it has to decide what an uncatalogued code means before it can generate the list. Nothing else in the feature may quietly widen the drop in the meantime |
| AC2 | **Three redesign claims that only a browser can judge, asserted nowhere.** (a) **The sticky aside** — `.access-admin-page__users` carries `position: sticky` with `inset-block-start: calc(var(--qd-navbar-block-size) + var(--qd-space-4))`, and sticking needs a real scroll container and a real resolved navbar height. (b) **The single-column collapse** at `bp.$qd-bp-tablet-max`, where the layout becomes `flex-direction: column` and the aside becomes `inline-size: 100%; position: static` — jsdom evaluates no media query, so the tablet layout is unreachable from a spec. (c) **The no-1px-shift selection thread** — the unselected row reserves `border-inline-start-width: 2px` in `transparent` and `.qd-is-selected` only recolours it, so proving "no shift" means comparing two *resolved* border widths, which jsdom does not compute. The jsdom-checkable half of the same redesign **is** asserted and is not in this row: `role="listitem"` per row, `.qd-truncate` plus `[title]` on name and email, `[title]` on the permission code, and the `role="status"` reserved mutation region in each mutating panel. **What a browser check must confirm**, signed in as an **Active Owner** on `/settings/access`: the aside stays pinned while the detail panel scrolls; at ≤ tablet width the two columns become one with the aside full-width and unpinned; and selecting a list row moves no adjacent pixel | `Frontend/quran-dashboard-ui/src/app/features/access-admin/pages/access-admin-page/access-admin-page.component.scss:25-30` and `:148-157`, `components/access-user-list/access-user-list.component.scss:23-39` | **Row A1's harness — do not build a second one.** A1 already owes the authenticated E2E persona, and its "**or** the first authenticated E2E persona added for any feature" clause is the trigger that unblocks this row too. The only addition this route makes to A1's requirement is that the persona must be an **Active Owner**, since `/settings/access` is guarded on Owner membership rather than on a permission code |
