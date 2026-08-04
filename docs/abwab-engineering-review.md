# Abwab — whole-feature engineering review

Read-only review of the Abwab area as a whole. Thirteen slices (A–M plus the
navigation-progress fix) were each reviewed individually; this is the first pass over the
feature as a system. Method: the project's `engineering-review` skill. Authority order:
current code and tests first, then the nearest `README.md`, `.architecture/**`,
`docs/contracts/`, and the root / Backend / Frontend `CLAUDE.md`.

Severity vocabulary is the one this review was commissioned with — **HIGH / MEDIUM / LOW** —
not the skill's BLOCKING/MAJOR/MINOR/NOTE.

- **HIGH** — wrong data, a broken invariant, a security or Quran-safety problem, or something
  a user can reach that corrupts state.
- **MEDIUM** — wrong behavior in a reachable but bounded case, or a contract inconsistency.
- **LOW** — naming, clarity, minor drift.

**Working artifact.** This file lives only until its findings are adjudicated, and is deleted
in the last commit before the fix branch merges — the same lifecycle as a plan.

---

## 0. Adjudication log (fix branch `abwab-review-fixes`)

Every finding ends in one of three states: **fixed**, **converted** to a `docs/TESTING_DEBT.md`
row, or **closed** with a written reason. Nothing is left merely noted.

### Bundle 1 — F-35, the HIGH

| Finding | State | Where |
|---------|-------|-------|
| F-35 | **fixed** | `state/abwab-selection.store.ts:48-54` (new `setSectionScope`), `pages/abwab-page/abwab-page.component.ts:264` (wiring) |

**What changed.** `AbwabSelectionStore` gained `setSectionScope(sectionId)`, which clears the bulk
set when the section scope actually changes. It is the exact generalisation of the existing
`setArchiveViewActive` precedent in the same file — one scope change was wired and the other was
not. The page pushes the parsed section into it from the URL subscription
(`abwab-page.component.ts:264`), immediately after the existing
`setArchiveViewActive(parsed.archive)` call.

**Why the store-side rule rather than a call-site fix.** Both entry paths — the section tabs via
`onSectionChanged`, and `onRevealRequested`, which writes `section` itself when the revealed door
lives elsewhere — reach the store through the same URL subscription. Placing the rule in the store
and feeding it from the URL closes both at once and keeps the invariant from being re-derivable per
call site, which is what the finding's "prefer the store-side rule" recommended.

**Bulk mode is not exited, only the set is cleared.** `setArchiveViewActive` exits bulk mode
entirely because bulk is *forbidden* in the archive view (`setBulkMode` refuses while it is
active). No such prohibition exists for sections, so clearing the set is the smaller change that
satisfies the invariant while preserving the mode the user deliberately turned on.

**Regression tests — two, one per entry path**, in
`pages/abwab-page/abwab-page.component.spec.ts`: a section switch clears the bulk set, and a reveal
into another section clears it through the same rule. The reveal test feeds the actual
`router.navigate` arguments back into the URL rather than a hand-written query string, so it pins
the real handler output. **Both were confirmed to fail without the fix** (`expected '2' to be '0'`
and `expected '1' to be '0'`) and pass with it; the page spec goes 99 → 101 with no existing test
changed.

**Any other scope-changing write? One found, deliberately not fixed here.** The tree *marks*
search matches in place and hides nothing (`[matchedIds]`, `abwab-page.component.html:154`), so
`q` is not a scope change there. But the **cards view filters** on the same query
(`abwab-page.component.html:129`) and also supports bulk mode (`:134`), so in cards a search can
hide a bulk-selected door. This is deliberate design, not drift — the spec records it at
`abwab-page.component.spec.ts:490-493`: "the tree marks matches in place; cards and archive still
filter, deliberately, from the same box." Clearing bulk on `q` would therefore be wrong for the
tree, which is the majority surface. Flagged here and carried into bundle 2, where F-55 changes
cards' filtering depth; the interaction should be settled there rather than by a rule in the store.

### Bundle 2 — missing guards and missing states (11 findings)

| Finding | State | Where |
|---------|-------|-------|
| F-53 | **fixed** | `components/abwab-cards/abwab-cards.component.html:28`, `.ts:76` |
| F-54 | **fixed** | `pages/abwab-page/abwab-page.component.ts:197` |
| F-55 | **fixed** (user ruling) | `components/abwab-cards/abwab-cards.component.ts:67` |
| F-56 | **fixed** (user ruling) | `components/abwab-cards/abwab-cards.component.html:34` |
| F-57 | **fixed** (accepted consequence) | `components/abwab-template-tree/abwab-template-tree.component.ts:133-138` |
| F-61 | **fixed** | `components/abwab-template-copy-modal/abwab-template-copy-modal.component.ts:111-116` |
| F-63 | **fixed** | `components/abwab-door-modal/abwab-door-modal.component.ts:130`, `abwab-sections-modal` |
| F-66 | **fixed** | `components/abwab-door-picker/abwab-door-picker.component.ts:62` |
| F-91 | **fixed** | `pages/abwab-templates-page/abwab-templates-page.component.html:28-35` |
| F-92 | **fixed, scenario partly refuted** | `pages/abwab-templates-page/abwab-templates-page.component.ts:308-314` |
| F-97 | **fixed** | `components/abwab-door-picker/abwab-door-picker.component.html:11` |

**Deliberate deviation on F-53, accepted.** The finding suggested a page-level
`@if (displayRoots().length === 0)` wrapper around `<qd-abwab-cards>`. That guard removes the
component *including its breadcrumb*, so a drilled user (`card=5`) typing a zero-match query would
lose the only in-page way back out. The empty state was therefore placed inside the cards
component, below the breadcrumb, still composing the shared `qd-state`. After F-55, `displayRoots()`
is also no longer the right emptiness signal — the emptiness that matters is the level being
viewed, which only the cards component can derive from `cardId`.

**F-92's stated failure scenario is REFUTED, though the fix still landed.** The dialog was never
dismissible mid-flight: `ConfirmDialogComponent.cancel()` already guards. The mirror of
`cancelNodeDelete()` was applied anyway because the *error-clearing* half of the finding was real.
Recorded so the finding is not cited later as evidence of a hole that did not exist.

**Both new "no results" states distinguish filtering from emptiness via `AbwabSearchResult.isFiltering`,
not `q !== ''`** — `searchAbwabNodes` trims and returns `EMPTY_SEARCH_RESULT` for a whitespace-only
query, so `q='   '` correctly still reads «لا توجد أبواب مؤرشفة.» instead of falsely claiming a
filter ran.

**Verification (run by the parent, not the implementing agents).**
`tsc --noEmit -p tsconfig.app.json` → 0; `-p tsconfig.spec.json` → 0; `npm run build` → success with
exactly the three known budget warnings; abwab suite **538 → 566**, 29 files, 0 failures. Diff audit
confirms the rise is purely additive: no `it(`, `expect(` or `describe(` line was deleted from any
existing spec, and `abwab.labels.ts` gained two strings with no edits to existing values. All 27
touched files fall inside the four agents' declared ownership lists; no README was touched.

> **Correction to the Bundle 1 entry above.** Bundle 1 reported `tsc --noEmit -p tsconfig.json`
> exit 0. That check is **vacuous**: the root `tsconfig.json` is `"files": []` plus project
> references, and `--noEmit` does not follow references, so it type-checks nothing. Bundle 1's real
> evidence is its passing specs, plus the `npm run build` above, which AOT-compiles the committed
> bundle-1 code and succeeded. The leaf configs (`tsconfig.app.json`, `tsconfig.spec.json`) are the
> correct targets and are what every later bundle uses.

**Carried to bundle 6 — README claims these fixes newly falsified** (docs are deliberately last,
so they are tracked here rather than patched twice):

- `features/abwab/README.md:83-85` — «In **cards** and the **archive** the same query still
  *filters* (`pruneAbwabNodesToVisible`)». Cards no longer use `pruneAbwabNodesToVisible` at all.
  The archive half is still true.
- `README.md:79-83` — the "lie about the data" reasoning now applies to all three surfaces.
- `README.md:157-166` — the `abwab-cards/` description predates the component owning its own
  empty state and the card being a real `<button>`.
- `README.md:794-798` — «Only three error sites carry the single `actionLabel` retry»; F-91 makes
  it four (the templates-list read wired to `AbwabTemplatesFacade.loadList()`).
- `README.md:~804-805` — the double-dispatch closure is documented only for the relation-delete
  confirm; template apply, create/edit-door and create-section now share it.

**Noticed, not fixed** (no finding behind them; recorded rather than silently swept):

- The door picker's retry gives no in-flight feedback over stale rows — residue of F-97's
  error-only hoist; the skeleton is still trapped in `@empty`.
- `abwab-cards.component.ts:96` `onCheckboxClick`'s `stopPropagation()` is now redundant, the
  checkbox being a sibling rather than nested.
- `abwab-templates-page.component.ts:352-358` `closeOverlays()` bypasses the new busy guard, exactly
  as its `deletingNodeId` twin does; left symmetric.
- F-58 confirmed still open — the workshop's order chip remains a click-only `<span>`. It is
  bundle 3 work.

---

## 1. Areas covered / remaining

| # | Area | Status |
|---|------|--------|
| 1 | Backend — controllers, DTOs, contracts, routes | **done** |
| 2 | Backend — handlers / application layer | **done** |
| 3 | Backend — persistence: writers, readers, EF configurations, migrations | **done** |
| 4 | Backend — ETag / conditional GET / caching and invalidation | **done** |
| 5 | Frontend — abwab state layer | **done** |
| 6 | Frontend — abwab components | **done** |
| 7 | Frontend — shared, core, styles, and the five words surfaces | **done** |
| 8 | **Cross-slice pass** | **done** |

---

## 2. Findings

| ID | Area | Severity | Ownership | Summary | Citation |
|----|------|----------|-----------|---------|----------|
| F-01 | 7 / docs | MEDIUM | Abwab-owned | 33 references to deleted Abwab planning artifacts (`plan.md`, `plan-slice-b.md`, `plan-slice-b2.md`, "abwab UX audit") survive in long-lived docs and tests; repo law calls a dangling link a defect | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:72`, `.architecture/UI_STYLE_SYSTEM.md:758` |
| F-02 | 1 | MEDIUM | Abwab-owned | The five Abwab DELETE actions return 204 with no body, but the exported OpenAPI spec documents them as 200 with an `ObjectApiResponse` body — the frontend generates payload types from that spec | `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:190` |
| F-03 | 1 | MEDIUM | Abwab-owned | The reorder-scope enum guard lives in the controller, which constructs an Application-layer outcome (`ReorderDoorOutcome.InvalidScope`) that no handler can ever produce | `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:112` |
| F-04 | 1 | MEDIUM | Abwab-owned | `DELETE api/abwab/sections/{id}` is the only Abwab door/section write that carries no version token, yet the controller maps a `StaleVersion` outcome to 409 that no stale client can trigger — and the README says the opposite | `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:63` |
| F-05 | 1 | MEDIUM | Abwab-owned | The Controllers README asserts the Abwab surface "must not reach production before a write policy attaches" — production is live and unauthenticated, so a developer trusting the README believes a constraint that has already been broken | `Backend/api/QuranDashboard.Api/Controllers/README.md:19` |
| F-06 | 1 | MEDIUM | Abwab-owned | `AbwabTemplateSummaryDto.NodeCount` counts live NON-ROOT descendants, but neither its name nor any README says so — it is the one Abwab count field whose scope is stated nowhere | `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Responses/AbwabTemplateSummaryDto.cs:3` |
| F-07 | 3 | MEDIUM | Abwab-owned | The Writes README states BulkMoveAsync resolves the target section before loading the doors, and calls that ordering load-bearing; the code does the opposite | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:221` |
| F-08 | 1-4 docs | MEDIUM | Abwab-owned | Writes README states as law that EVERY SaveChangesAsync in the folder goes through a translating helper; eight bare saves exist, and two of them can reach the global handler as a 500 exactly as that sentence warns | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplatesWriter.cs:173` |
| F-09 | 1-4 docs | MEDIUM | Abwab-owned | Writes README claims door create is the only path needing an explicit transaction and the only one with two SaveChangesAsync calls; three other paths do both, one of them in the same class, and the same README contradicts itself 77 lines later | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:77` |
| F-10 | 1-4 docs | MEDIUM | Abwab-owned | Writes README says the template apply writer has no tests of either kind; the test file exists and docs/TESTING_DEBT.md records the obligation it pays as PAID — two long-lived documents state opposite facts about the same file | `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTemplateApplyBehaviorTests.cs:16` |
| F-11 | 1-4 docs | MEDIUM | Abwab-owned | Both the Writes README and the Controllers README present the relations-add refusal set as exhaustive, and both omit three reachable 400s — including the fact that `direction` is REQUIRED for a Comprehensiveness relation | `Backend/application/QuranDashboard.Application/Abwab/Commands/Relations/AddDoorRelations/AddDoorRelationsHandler.cs:125` |
| F-12 | 3 | LOW | Abwab-owned | The section_id SET NOT NULL migration has no backfill and no guard against the exact NULL-row condition its own commit records as having existed, and the local remedy (wipe-abwab) does not exist for production's 785 doors | `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260802062011_RequireAbwabDoorSection.cs:13` |
| F-13 | 3 | MEDIUM | Abwab-owned | Invariant 4 (canonical pair + unique index + derived dormancy) has zero test coverage anywhere in the repository — no behavior test, no schema test, no smoke test | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabRelationsWriter.cs:45` |
| F-14 | 3 | MEDIUM | Abwab-owned | Invariant 5's reversed decision — apply copies the root's CHILDREN, never the root — is pinned by no test, and neither is the (target, child) collision key nor the empty-root 400 | `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTemplateApplyBehaviorTests.cs:16` |
| F-15 | 2 | MEDIUM | Abwab-owned | CreateDoorHandler has no catch for AbwabStaleVersionException, but the door-create save path can throw it — the caller gets a 500 instead of the documented 409 | `Backend/application/QuranDashboard.Application/Abwab/Commands/Doors/CreateDoor/CreateDoorHandler.cs:37` |
| F-16 | 1 | LOW | Abwab-owned | All six `Created(...)` calls pass a relative URI with no leading slash, so the `Location` header resolves to a wrong path per RFC 3986 | `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:34` |
| F-17 | 1 | LOW | Abwab-owned | The Controllers README calls the template reads "admin-only" in the same paragraph that states all Abwab routes are Open; they carry no authorization | `Backend/api/QuranDashboard.Api/Controllers/README.md:17` |
| F-18 | 1 | LOW | Abwab-owned | `AbwabDoorsController.cs` is 227 lines, over the 200-line soft threshold for controllers, with no recorded justification | `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:227` |
| F-19 | 3 | LOW | Abwab-owned | Both READMEs say the relation row's xmin is never read; EF reads it on every UPDATE, which is precisely why DeleteAsync has a concurrency catch | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md:212` |
| F-20 | 3 | LOW | Abwab-owned | The tree snapshot's alias projection — a GroupBy/ToDictionaryAsync on the hottest read path — is asserted by no test | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTreeReader.cs:19` |
| F-21 | 3 | LOW | Abwab-owned | A template's flat node list comes back root-last from the reader but root-first from the create response, because OrderBy on a nullable parent id is NULLS LAST in Postgres | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTemplatesReader.cs:105` |
| F-22 | 3 | LOW | Abwab-owned | Two live sections can share an OrderValue because create assigns count(live)+1 while section delete resequences nothing | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabSectionsWriter.cs:12` |
| F-23 | 3 | LOW | Abwab-owned | ResequenceSiblingsExcludingAsync still takes a nullable section id, dead nullability left behind by the section_id NOT NULL reversal | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:651` |
| F-24 | 1-4 docs | LOW | Abwab-owned | Four load-bearing file:LINE citations in the two Persistence READMEs point at the wrong lines or a wrong count, in an area whose own repo law requires facts to be proven from code with a file:LINE | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md:152` |
| F-25 | 1-4 docs | LOW | Abwab-owned | Two Abwab controllers carry `//` comment blocks in production API code, and one of them restates a README paragraph verbatim — the one comment form the root CLAUDE.md forbids with no exception | `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTreeController.cs:16` |
| F-26 | 1-4 docs | LOW | Abwab-owned | Reads README describes the snapshot relation count as 'One grouped query per snapshot'; the query does no grouping — it materializes every live pair and counts in memory | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTreeReader.cs:62` |
| F-27 | 3 | LOW | Abwab-owned | createdRoots in the apply writer is residue of the reversed root-copy decision — it holds the copied CHILDREN, and the name says the opposite of the invariant the file enforces | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs:87` |
| F-28 | 3 | LOW | Abwab-owned | ResolveBroaderDoorId silently defaults a null direction on a Comprehensiveness relation to 'target is broader'; the guard that makes this unreachable lives in the handler, not at the writer seam, and nothing tests either | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabRelationsWriter.cs:137` |
| F-29 | 2 | LOW | Abwab-owned | AbwabDuplicateNameException.Name and its two-branch message are dead — every catch site discards the exception, so neither is ever read | `Backend/application/QuranDashboard.Application.Abstractions/Abwab/AbwabDuplicateNameException.cs:8` |
| F-30 | 2 | LOW | Abwab-owned | The empty-doors rule has two answers: the bulk handlers refuse it as 400, the writer returns an empty success — the writer branch is unreachable dead defensive code | `Backend/application/QuranDashboard.Application/Abwab/Commands/Doors/BulkMoveDoors/BulkMoveDoorsHandler.cs:16` |
| F-31 | 2 | LOW | Abwab-owned | AddDoorRelationsHandler's three request-shape refusals and GetDoorRelationsHandler's NotFound are the only Abwab refusals that log nothing | `Backend/application/QuranDashboard.Application/Abwab/Commands/Relations/AddDoorRelations/AddDoorRelationsHandler.cs:20` |
| F-32 | 4 | LOW | Abwab-owned | The '816 lines' figure for EfAbwabDoorsWriter is wrong in two long-lived docs, and TESTING_DEBT additionally says the file got larger when it got smaller | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md:53` |
| F-33 | 4 | LOW | Abwab-owned | GET templates/{id} compares the validator before the existence check, so a crafted If-None-Match makes a nonexistent template answer 304 instead of 404 | `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:47` |
| F-34 | 4 | LOW | Abwab-owned | Nothing anywhere asserts the ETag/generation/304 mechanism — already recorded as debt, flagged so the parent does not double-count it | `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTreeReadTests.cs:5` |
| F-35 | 5 | HIGH | Abwab-owned | The bulk selection set survives a section switch, so bulk move / bulk archive / bulk relations operate on doors that are not in the visible tree | `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-selection.store.ts:37` |
| F-36 | 5 | MEDIUM | Abwab-owned | `pendingRequest?.unsubscribe()` cannot cancel an in-flight request because `shareReplay(1)` keeps the source subscribed, so an older tree response can overwrite a newer one along with its ETag | `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-snapshot.facade.ts:42` |
| F-37 | 5 | MEDIUM | Abwab-owned | A restored URL carrying a `section` id that no longer exists produces a permanently empty tree with a `0` stat and no active tab — the parse validates shape but not existence, contradicting the README's "fails closed to the defaults" | `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-url-sync.ts:64` |
| F-38 | 7 core/styles | MEDIUM | Abwab-owned | The chrome-inert "blast radius" membership test documented in two places is wrong: the global detail-overlay shell holds the scroll lock imperatively and never applies `qdModalScrollLock`, so the prescribed grep under-reports the radius | `src/app/shared/ui/detail-modal-shell/detail-modal-shell.component.ts:63` |
| F-39 | 7 core/styles | MEDIUM | Abwab-owned | The Chrome-inert rule states a hard count ("these nine") in the same paragraph that forbids stating a count, and the number is wrong — there are 12 `qdModalScrollLock` holders plus 1 imperative holder | `.architecture/UI_STYLE_SYSTEM.md:1503` |
| F-40 | 7 core/styles | MEDIUM | pre-existing | A click on a dropdown trigger cannot open its menu once `mouseenter` has already fired — the `<li>`'s hover-open and the button's click-toggle fight each other; Slice H doubled the affected surface from one dropdown to two | `src/app/core/layout/top-navbar/top-navbar.component.html:19` |
| F-41 | 7 core/styles | MEDIUM | Abwab-owned | The nav dropdown drops focus to `<body>` when it closes — Escape, outside-click and link-click all destroy the `<ul>` via `@if` with no focus return to the trigger | `src/app/core/layout/top-navbar/top-navbar.component.html:51` |
| F-42 | 7 core/styles | MEDIUM | Abwab-owned | The `more` dropdown is a hand-rolled parallel branch keyed on the magic string `'more'` — it is the surviving pre-dropdown remnant, duplicating the entire dropdown markup (including the chevron SVG verbatim) and behaving differently from the data-driven ones | `src/app/core/layout/top-navbar/top-navbar.component.html:85` |
| F-43 | 7 docs | MEDIUM | Abwab-owned | UI_STYLE_SYSTEM.md §17 asserts all six abwab modals carry an "unconditional cdkTrapFocus"; the sections modal's trap is conditional, and that conditionality is load-bearing | `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:1143` |
| F-44 | 7 docs | MEDIUM | Abwab-owned | Reversal #3's code citation points at lines that state nothing, and claims a "class comment" that does not exist and could not exist under the workspace comment ban | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:1006` |
| F-45 | 7 docs | MEDIUM | Abwab-owned | The Browser-e2e section enumerates five abwab specs as the complete set; eight exist and the Playwright abwab project captures all eight — including the one this same README elsewhere cites as the pin for the reveal/Back contract | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:965` |
| F-46 | 7 docs | MEDIUM | Abwab-owned | README points at docs/TESTING_DEBT.md for the untested relation-delete dispatch; no such ledger row exists, so a real uncovered branch is scheduled by nothing | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:960` |
| F-47 | 7 a11y | MEDIUM | Abwab-owned | abwab-templates-page implements no focus return at all, so every overlay it opens from the row context menu drops focus to <body> on close — the doors page solved exactly this and the workshop did not | `src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.ts:1` |
| F-48 | 7 a11y | MEDIUM | Abwab-owned | A successful door restore drops focus to <body>: the archive row that invoked the modal is removed by the refresh, and neither the page nor the overlays controller restores focus | `src/app/features/abwab/pages/abwab-page/abwab-page.component.html:227` |
| F-49 | 7 a11y | MEDIUM | Abwab-owned | Escape becomes a dead key once a dirty-discard strip is open in the door, sections and template-node modals — it neither dismisses the strip nor closes the modal | `src/app/features/abwab/components/abwab-door-modal/abwab-door-modal.component.ts:109` |
| F-50 | 7 a11y | MEDIUM | Abwab-owned | The archive view's expand chevron has no accessible name and, on leaf rows, is an empty focusable button — three sibling components name and guard the same control | `src/app/features/abwab/components/abwab-archive-view/abwab-archive-view.component.html:15` |
| F-51 | 7 a11y | MEDIUM | Abwab-owned | Every write failure is announced twice — once by qd-state's role="alert" and once by the polite abwab-announcer — because both are fed the same outcome.message | `src/app/features/abwab/state/abwab-write.controller.ts:204` |
| F-52 | 7 a11y | MEDIUM | Abwab-owned | Successful writes announce nothing for doors but everything for templates — one announcer region, two opposite policies | `src/app/features/abwab/state/abwab-write.controller.ts:187` |
| F-53 | 6 | MEDIUM | Abwab-owned | The cards view has no empty state and no no-results state at all — a zero-match search or an empty section renders a bare breadcrumb over a blank grid | `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html:126` |
| F-54 | 6 | MEDIUM | Abwab-owned | A zero-match search in the archive view collapses the archive into «لا توجد أبواب مؤرشفة.» — the exact "lie about the data" ux-slice-l removed from the tree, never applied to the other two filtering surfaces | `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html:116` |
| F-55 | 6 | MEDIUM | Abwab-owned | Cards search filtering is applied to the root level but not below it: a matching root whose descendants do not match renders as an unreachable leaf, and drilled levels ignore the query entirely | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-cards/abwab-cards.component.ts:58` |
| F-56 | 6 | MEDIUM | Abwab-owned | Cards are non-focusable `<div>`s with a click handler and a dead `:focus-visible` rule — the whole cards view is unreachable by keyboard | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-cards/abwab-cards.component.html:30` |
| F-57 | 6 | MEDIUM | Abwab-owned | The template tree's inline order editor still COMMITS on blur — the exact behavior the doors tree and the sections modal both reversed to cancel-on-blur | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-tree/abwab-template-tree.component.html:39` |
| F-58 | 6 | MEDIUM | Abwab-owned | The inline order chip is a click-only `<span>` in both trees, so no keyboard path to reorder exists anywhere in the feature — and the README asserts the opposite invariant | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.html:71` |
| F-59 | 6 | MEDIUM | Abwab-owned | The toolbar's tree/cards view toggle exposes no selected state to assistive tech and reuses the section tabs' aria-label as its group name | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-toolbar/abwab-toolbar.component.html:61` |
| F-60 | 6 | MEDIUM | Abwab-owned | The side panel's bulk count interpolates a bare number into Arabic copy — «3 باب محدد» — against the feature's own counted-noun rule | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-side-panel/abwab-side-panel.component.html:88` |
| F-61 | 6 | MEDIUM | Abwab-owned | Template apply has no in-flight guard: a second click on «انسخ» re-issues the whole apply and duplicates the template's children under every selected door | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-copy-modal/abwab-template-copy-modal.component.ts:108` |
| F-62 | 6 | MEDIUM | Abwab-owned | The relations modal keeps its focus trap unconditionally live while its nested delete-confirm dialog is open — the exact two-live-traps case the README declares forbidden and the sections modal explicitly avoids | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-relations-modal/abwab-relations-modal.component.html:10` |
| F-63 | 6 | MEDIUM | Abwab-owned | Create-door and create-section submit with no in-flight guard and no version token, so a double click creates two doors / two sections | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-modal/abwab-door-modal.component.ts:156` |
| F-64 | 6 | MEDIUM | Abwab-owned | The move picker's chosen destination row is conveyed by colour/weight only — the pick button carries no `aria-pressed`/`aria-current`, so a screen-reader user cannot tell which parent the move will use | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-move-picker/abwab-move-picker.component.html:93` |
| F-65 | 6 | MEDIUM | Abwab-owned | `abwab-template-node-modal` has no spec file at all, so its dirty-close confirm and its submit/validation path are untested — the same shape the door modal covers in a 15 KB spec | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-node-modal/abwab-template-node-modal.component.ts:89` |
| F-66 | 7 shared | MEDIUM | Abwab-owned | `qd-state`'s reserved error box can render as an empty danger box: the door picker keys its reserve error branch off `status()`, not off a non-empty message, and `errorMessage` defaults to `''` | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-picker/abwab-door-picker.component.html:67` |
| F-67 | 7 shared | MEDIUM | Abwab-owned | `.qd-tabs__count--empty` dims an ACTIVE, clickable tab's zero count with `opacity: 0.5`, computing to roughly 1.9:1 against the surface it sits on — no measured ratio is recorded anywhere | `Frontend/quran-dashboard-ui/src/styles/_components.scss:230` |
| F-68 | 7 shared | MEDIUM | Abwab-owned | The templates tree's `⋯` button is keyboard-focusable and feeds `event.clientX/clientY` straight into `qd-context-menu`'s `position`, so a keyboard activation opens the menu clamped to the viewport's top-left corner instead of at the row | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-tree/abwab-template-tree.component.ts:88` |
| F-69 | 7 shared | MEDIUM | Abwab-owned | `qd-context-menu` has no unit spec at all; its RTL placement, viewport flip and clamp math is covered only by opt-in E2E, which is not a required test tier | `Frontend/quran-dashboard-ui/src/app/shared/ui/context-menu/context-menu.component.ts:63` |
| F-70 | 7 shared | MEDIUM | Abwab-owned | `qd-context-menu` exposes `role="menu"` with `role="menuitem"` children but manages no focus and carries no accessible name; the projected items sit at the very end of the page DOM, so the keyboard open path the series added leads to a menu the user must Tab through the whole page to reach | `Frontend/quran-dashboard-ui/src/app/shared/ui/context-menu/context-menu.component.html:6` |
| F-71 | 5 | LOW | Abwab-owned | `handleSuccess` casts a possibly-null payload to `T`, so `AbwabWriteOutcome<AbwabDoorDto>` can carry `data: null` while its type says otherwise | `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-write.controller.ts:182` |
| F-72 | 5 | LOW | Abwab-owned | `abwab-page-overlays.controller.ts` is 416 lines, over the 400-line soft threshold for state services, and the README never acknowledges the threshold | `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-page-overlays.controller.ts:416` |
| F-73 | 5 | LOW | Abwab-owned | The `state/` layer imports a type from a `components/` file, inverting the feature's layering | `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-page-overlays.controller.ts:11` |
| F-74 | 5 | LOW | Abwab-owned | Two adjacent counts on the doors page are both labelled «كل الأبواب» while counting different scopes; only the tab badge's aria-label names its scope | `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-tree.builder.ts:167` |
| F-75 | 7 core/styles | LOW | Abwab-owned | `.more-dropdown` is a dead class — Slice H kept it "additive" when `.words-dropdown` was deleted, and nothing in the app now selects it | `src/app/core/layout/top-navbar/top-navbar.component.html:86` |
| F-76 | 7 core/styles | LOW | pre-existing | The navbar template hardcodes the `/dashboard` path twice, against `core/README.md`'s stated invariant that route strings come from `route-paths.ts` | `src/app/core/layout/top-navbar/top-navbar.component.html:77` |
| F-77 | 7 core/styles | LOW | Abwab-owned | The chrome-inert binding covers `<nav class="qd-navbar">` only; the navbar's own full-screen `.mobile-menu` overlay is rendered outside that element and is never inerted | `src/app/core/layout/top-navbar/top-navbar.component.html:234` |
| F-78 | 7 docs | LOW | Abwab-owned | Six code citations in the README are stale, one of them past end-of-file; five of the six sit in the "Decisions that reversed mid-series" section | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:1003` |
| F-79 | 7 docs | LOW | Abwab-owned | Reversal #4 quotes the nav dropdown's middle item as «القوالب»; the shipped label is «قوالب الأبواب» — «القوالب» is a different control | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:1011` |
| F-80 | 7 docs | LOW | Abwab-owned | The stats-reconciliation entry gives a reason that does not entail the conclusion, so a developer would think the sum is structurally guaranteed when it is guaranteed by two write guards | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:826` |
| F-81 | 7 docs | LOW | Abwab-owned | Two stale counts: the doors API is described as fifteen endpoints 340 lines after the same README says sixteen, and the page is described as composing fifteen children where it composes seventeen | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:377` |
| F-82 | 7 docs | LOW | Abwab-owned | The bulk-conflict message is built from the live bulk set, not the attempted refs, so it can name a door the request never carried | `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-write.controller.ts:236` |
| F-83 | 7 docs | LOW | Abwab-owned | Three Arabic strings are quoted in guillemets in the README but are not the shipped strings, two of them truncated mid-sentence | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:802` |
| F-84 | 7 docs | LOW | Abwab-owned | docs/contracts/ has no Abwab pointer page, so the feature's 1,024-line README is unreachable from the index the workspace declares as the way to find contract truth | `docs/contracts/README.md:20` |
| F-85 | 7 docs | LOW | Abwab-owned | The sections-controller bullet lists three forwarded writes; it forwards four, and the omitted one backs a feature the same README documents at length | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:346` |
| F-86 | 7 a11y | LOW | Abwab-owned | The archive view's row controls and the doors tree's bulk checkbox are real tab stops inside role="treeitem" rows that also carry a roving tabindex | `src/app/features/abwab/components/abwab-archive-view/abwab-archive-view.component.html:5` |
| F-87 | 6 | LOW | Abwab-owned | The relations flag is a pressable strip that does nothing in bulk mode — it neither opens relations nor toggles the row's bulk checkbox — while the README says a row click in bulk mode means "toggle this door" | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.ts:146` |
| F-88 | 6 | LOW | Abwab-owned | The relations flag's has-relations / no-relations state is conveyed to sighted users by colour alone | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.scss:184` |
| F-89 | 6 | LOW | Abwab-owned | Cards render a bare unlabeled digit for a count whose scope is undeclared, while every count badge in the tree carries a full Arabic aria-label naming its scope | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-cards/abwab-cards.component.html:53` |
| F-90 | 6 | LOW | Abwab-owned | A hand-entered `?archive=1&door=<live id>` leaves the side panel offering edit/move/archive/add-child while the archive view is on screen | `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.ts:263` |
| F-91 | 6 | LOW | Abwab-owned | The templates list's load-error state offers no retry, leaving a full-page browser reload as the only recovery — while the copy modal nested inside the same page does offer one | `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.html:28` |
| F-92 | 6 | LOW | Abwab-owned | `cancelTemplateDelete()` has no in-flight guard and does not clear its error, unlike the node-delete confirm three methods above it | `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.ts:307` |
| F-93 | 6 | LOW | Abwab-owned | Three different breakpoint conventions inside one feature: the shared SCSS variable, a raw 900px, and 60rem | `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.scss:94` |
| F-94 | 6 | LOW | Abwab-owned | One file is over its hard threshold and three more are over soft without the README mention FRONTEND_STRUCTURE requires | `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.ts:1` |
| F-95 | 6 | LOW | Abwab-owned | `AbwabDoorPickerComponent.onRowChange` is unreachable dead code — the checkbox's own click handler cancels activation, so `change` never fires | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-picker/abwab-door-picker.component.ts:136` |
| F-96 | 6 | LOW | Abwab-owned | The door picker's excluded/disabled reasons are visual-only: `aria-disabled` sits on a role-less `<div>` and the «…» tag text is not part of any accessible name | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-picker/abwab-door-picker.component.html:18` |
| F-97 | 6 | LOW | Abwab-owned | The door picker's loading/error/empty states live inside `@empty`, so a doors-load failure that arrives while rows are already rendered shows nothing at all | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-picker/abwab-door-picker.component.html:59` |
| F-98 | 6 | LOW | Abwab-owned | `pickerStatus` in the template-copy modal can never evaluate to `'ready'`, so the picker's four-state type is really three states at this call site | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-copy-modal/abwab-template-copy-modal.component.ts:62` |
| F-99 | 7 shared | LOW | Abwab-owned | UI_STYLE_SYSTEM.md's `reserve` entry undercounts the `[reserve]` call-sites (four claimed, eight in code) and describes all of them as message-guarded when the door picker is not | `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:866` |
| F-100 | 7 shared | LOW | Abwab-owned | The context-menu placement contract's labels ("extends toward the inline-start", "cross the inline-start viewport edge") are inverted relative to the mechanics stated in the same sentence and implemented in code | `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:1225` |
| F-101 | 7 shared | LOW | Abwab-owned | §17's `qd-chip` backing-class list omits `.qd-chip--disabled`, which is the class that actually delivers the non-interactive disabled state on the removable and anchor branches | `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:791` |
| F-102 | 7 shared | LOW | Abwab-owned | §17 describes `.qd-checkbox` as sizing "a native `<input type="checkbox">`", but the door picker composes it on an `<input type="radio">` in single-pick mode | `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:1035` |
| F-103 | 7 shared | LOW | Abwab-owned | `abwab-door-restore-modal` is the one `qd-confirm-dialog` on the doors page that does not pass `testIdPrefix`, against §17's stated rule for pages hosting more than one confirm | `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-restore-modal/abwab-door-restore-modal.component.html:1` |
| F-104 | 7 shared | LOW | Abwab-owned | The two comments the purge kept in shared/ui both restate facts already written into `.architecture/UI_STYLE_SYSTEM.md` §17, so they do not clear the root CLAUDE.md exception bar | `Frontend/quran-dashboard-ui/src/app/shared/ui/chip/chip.component.html:1` |

---

## 3. Finding detail

### F-01 — Dangling references to the deleted planning artifacts (MEDIUM, Abwab-owned)

**What the code/docs do.** The Abwab planning artifacts were deleted, as the lifecycle rule
requires. Thirty-three citations of them survive across fifteen files. None of the referenced
files exists anywhere in the repository (`find` for `plan-slice-*` and `*ux-audit*` returns
nothing; `specs/` contains only `README.md`).

Two of the fifteen are **long-lived documents**, which is what makes this a finding rather than
housekeeping:

- `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md` — 8 references, at lines
  `:72`, `:166`, `:173`, `:174`, `:327`, `:722`, `:734`, `:757`.
- `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` — 2 references, at `:758`
  ("item 19 of the abwab UX audit") and `:762` ("since Abwab Slice B (plan-slice-b.md T412)").

The remaining 23 are in `e2e/**` and `*.spec.ts` files (11 files; `e2e/abwab-operations.e2e.ts`
alone has 4). Test files are outside the comment policy's scope, but not outside the repoint rule.

The most consequential instance is `README.md:757`, which asserts that the shipped Arabic string
**differs from** what the plan locked — an assertion whose authority is a file that no longer
exists, so a reader cannot tell whether the difference was deliberate or a defect.

**What it should do, and on whose authority.** Root `CLAUDE.md`, the planning-artifact lifecycle
rule: *"**Repoint before you delete.** `grep -rn` the whole repo — code, tests, `.claude/`,
`.agents/`, `.specify/`, scripts, manifests, every README — for each path being removed. A
dangling link blocks the delete; it is a defect, not an acceptable cost."* The same rule names
`README.md` files and `.architecture/**` as long-lived documentation, so these two files were
required to be repointed before the deletion commit ran.

**Smallest correction — to the documentation.** For each reference, either fold the cited fact
into the sentence itself (so the claim stands on its own and is provable from code) or drop the
parenthetical citation. No claim needs to be re-derived: in every instance the surrounding
sentence already states the fact; only the now-unresolvable provenance pointer is dangling.
`README.md:757` is the one that needs a decision rather than an edit — see §6.

**Severity rationale.** MEDIUM, not LOW: repo law explicitly classifies a dangling link as a
defect, and the affected files include an `.architecture/**` document that governs future work
across the whole frontend. It is not HIGH — no data is wrong and no invariant is broken.

---

### F-02 — The five Abwab DELETE actions return 204 with no body, but the exported OpenAPI spec documents them as 200 with an `ObjectApiResponse` body — the frontend generates payload types from that spec (MEDIUM, Abwab-owned)

**Citation.** `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:190`

**What the code does.** `DeleteDoorOutcome.Success => NoContent()` (and the four siblings at AbwabSectionsController.cs:69, AbwabDoorRelationsController.cs:71, AbwabTemplatesController.cs:95, AbwabTemplateNodesController.cs:97) send `204 No Content`. Because the action is declared `Task<ActionResult<ApiResponse<object>>>` and carries no `[ProducesResponseType]`, Swashbuckle infers the declared `T` and writes `"200": {... "$ref": "#/components/schemas/ObjectApiResponse"}` for all five routes in Frontend/quran-dashboard-ui/openapi/swagger.json. No 204 is documented anywhere.

**What it should do, and on whose authority.** The spec should document the success status the server actually sends. Authority: Controllers/README.md:136-140 excuses only *non-200* error schemas as a recorded follow-up ("Typed non-200 response schemas ([ProducesResponseType] for 400/404/500) are a recorded follow-up") — it does not excuse a wrong *success* code; and Controllers/README.md:132-134 states the frontend generates payload types from this spec, so it is a consumed contract. API_GUIDELINES.md:91 sanctions 204 but the spec must reflect it.

**Smallest correction — to the code.** Add `[ProducesResponseType(StatusCodes.Status204NoContent)]` to the five DELETE actions (and re-run `Backend/scripts/export-swagger` + `check-api-contract`). If the team prefers not to start typing responses, the alternative smallest correction is a sentence in Controllers/README.md owning the fact that the five DELETEs are documented 200 but answer 204 — but that leaves the generated client wrong.

---

### F-03 — The reorder-scope enum guard lives in the controller, which constructs an Application-layer outcome (`ReorderDoorOutcome.InvalidScope`) that no handler can ever produce (MEDIUM, Abwab-owned)

**Citation.** `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:112`

**What the code does.** `var outcome = Enum.IsDefined(body.Scope) ? await reorderHandler.HandleAsync(...) : new ReorderDoorOutcome.InvalidScope();` — the API layer validates the enum and manufactures the Application result type. `ReorderDoorHandler.HandleAsync` (ReorderDoorHandler.cs:12-43) contains no scope check at all and has no code path returning `InvalidScope`; a grep for `InvalidScope` across application/, api/, infrastructure/ returns only the record declaration (ReorderDoorOutcome.cs:12) and the two controller lines (:112, :122).

**What it should do, and on whose authority.** Use-case validation belongs in the Application layer, and the same area already does it that way: `AddDoorRelationsHandler.cs:23` (`if (!Enum.IsDefined(command.Type)) return new AddDoorRelationsOutcome.InvalidType();`) and `:28` validate the relation enums inside the handler and return the outcome from there. Authority: API_GUIDELINES.md §7 line 187 ("Controllers/endpoints must not contain business validation logic"), §1 lines 44-51 (the API layer maps results, it does not own rules), plus same-area precedent.

**Smallest correction — to the code.** Move the `Enum.IsDefined(command.Scope)` check into `ReorderDoorHandler.HandleAsync` as the first guard returning `new ReorderDoorOutcome.InvalidScope()`, and reduce the controller to the plain `await reorderHandler.HandleAsync(new ReorderDoorCommand(...))` call. The two smoke tests that pin this (SmokeAbwabWriteTests.cs:575 missing scope, :590 unknown scope) keep passing unchanged.

**Also reported independently at.** `Backend/application/QuranDashboard.Application/Abwab/Commands/Doors/ReorderDoor/ReorderDoorOutcome.cs:12` — 2 of the six backend agents reached this finding separately.

---

### F-04 — `DELETE api/abwab/sections/{id}` is the only Abwab door/section write that carries no version token, yet the controller maps a `StaleVersion` outcome to 409 that no stale client can trigger — and the README says the opposite (MEDIUM, Abwab-owned)

**Citation.** `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs:63`

**What the code does.** `public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken cancellationToken)` takes no body. Its two siblings do: `RenameSectionBody(string Name, uint Version)` and `ReorderSectionBody(int Position, uint Version)`. `DeleteSectionCommand(int Id)` carries no version, and `EfAbwabSectionsWriter.DeleteAsync` (lines 47-70) never sets `db.Entry(section).Property(s => s.Version).OriginalValue` the way `ReorderAsync` does at :91 — so the 409 mapped at AbwabSectionsController.cs:74-75 is reachable only from an EF race between the reader's load and `SaveChangesAsync`, never from a client holding a stale version. A client with a days-old view can delete a section and be told nothing.

**What it should do, and on whose authority.** Either the route carries `{ version }` like its two siblings and the writer pins `OriginalValue`, or the README states that section delete is version-free. Authority: Controllers/README.md:20-21 asserts "Optimistic concurrency is `uint xmin`, surfaced as `409` in the shared envelope" and then names ONLY the relation family (:34-36) and the template family (:38-39) as carrying no version token — section delete is an undocumented third exception. Damage is bounded because `DeleteAsync` re-checks `hasLiveDoors` at :56-61, so this is a contract/documentation defect rather than a data-loss one.

**Smallest correction — to the code.** Add a `DeleteSectionBody(uint Version)` and thread it through `DeleteSectionCommand` → `EfAbwabSectionsWriter.DeleteAsync` with `OriginalValue` pinned, matching `ReorderAsync`. If the team prefers to keep the route body-free, delete the unreachable `DeleteSectionOutcome.StaleVersion` branch and add section-delete to the README's list of version-free writes.

---

### F-05 — The Controllers README asserts the Abwab surface "must not reach production before a write policy attaches" — production is live and unauthenticated, so a developer trusting the README believes a constraint that has already been broken (MEDIUM, Abwab-owned)

**Citation.** `Backend/api/QuranDashboard.Api/Controllers/README.md:19`

**What the code does.** README:18-20 reads: "All routes are `Open` — this is the repository's first write surface and it ships without authentication in Slice A (see feature plan §9/§10); it must not reach production before a write policy attaches." No `[Authorize]` attribute exists on any of the six Abwab controllers or any of their 25 actions (verified by reading all six files in full). Meanwhile docs/TESTING_DEBT.md:21-23 records the opposite as fact: "They were keyed to 'when write protection lands'; `/api/abwab` shipped to production still `Open`, so that trigger was overtaken by the release rather than met."

**What it should do, and on whose authority.** The nearest README is the current truth of its area (root CLAUDE.md, "Local README Context": "It states the current truth"). Two long-lived documents currently state contradictory facts about whether an unauthenticated write surface is in production; the TESTING_DEBT one is correct.

**Smallest correction — to the documentation.** Replace the "must not reach production before a write policy attaches" clause in Controllers/README.md:19 with the actual posture — that it IS in production Open, and that closing it is the next feature's acceptance criterion — pointing at docs/TESTING_DEBT.md:20-25 rather than restating it.

---

### F-06 — `AbwabTemplateSummaryDto.NodeCount` counts live NON-ROOT descendants, but neither its name nor any README says so — it is the one Abwab count field whose scope is stated nowhere (MEDIUM, Abwab-owned)

**Citation.** `Backend/application/QuranDashboard.Application.Abstractions/Abwab/Responses/AbwabTemplateSummaryDto.cs:3`

**What the code does.** `public sealed record AbwabTemplateSummaryDto(int Id, string Name, int NodeCount);`. It is populated from `DescendantCount = db.AbwabTemplateNodes.Count(n => n.TemplateId == t.Id && n.ParentNodeId != null && n.DeletedAtUtc == null)` (EfAbwabTemplatesReader.cs:21-22), projected as the third positional argument at EfAbwabTemplatesReader.cs:28. So the wire name says "nodes", the query counts live non-root nodes: a template whose root has two children reports 2, not 3. The internal anonymous type is honestly named `DescendantCount`; only the public contract name loses that.

**What it should do, and on whose authority.** The other three Abwab counts all declare their scope in a README that is provable from code: `DirectChildCount` and `DoorsInScopeCount` at Persistence/Reads/Abwab/README.md:52-56 ("count LIVE rows only", "regardless of nesting depth") with tests pinning both (AbwabTreeReadTests.cs:102, :109, :129-136), and `RelationCount` at README.md:96-101. `NodeCount` appears in no README statement and has no test. Authority: same-area precedent plus root CLAUDE.md's rule that a fact not recoverable from a name must live in the nearest README with a `file:LINE` proof.

**Smallest correction — to the documentation.** Add one line to Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/README.md beside the other three counts, stating that `AbwabTemplateSummaryDto.NodeCount` is the live descendant count excluding the root, proved by `EfAbwabTemplatesReader.cs:21-22`. Renaming the property to `DescendantCount` is the truer fix but flows into `Frontend/.../core/api/generated/` via the exported spec, so it is the larger change.

**Corroborated.** 2 of the six backend agents reached this finding independently.

---

### F-07 — The Writes README states BulkMoveAsync resolves the target section before loading the doors, and calls that ordering load-bearing; the code does the opposite (MEDIUM, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:221`

**What the code does.** `BulkMoveAsync` loads and validates the doors first — `db.AbwabDoors.Where(d => ids.Contains(d.Id) && d.DeletedAtUtc == null)` at :221-223 followed by `throw new AbwabNotFoundException()` at :226 — and only then calls `ResolveTargetSectionAsync` at :229. `MoveAsync` genuinely does load the door first (:95 before :101), so the README's sentence is half true, which is why it reads as a deliberate contrast rather than an error.

**What it should do, and on whose authority.** `Persistence/Writes/Abwab/README.md:143-145` says: "Check ORDER differs on purpose and is load-bearing: `MoveAsync` loads the door first (unknown id stays a `404`), while `BulkMoveAsync` resolves the target first (request-shape validation before entity checks)." Under the actual code a bulk move naming an unknown door AND an unstated root section returns 404, not the 400 the README promises. Already recorded as `docs/TESTING_DEBT.md` row C1, which notes no test discriminates the two orderings — so the contradiction is confirmed but neither side is pinned.

**Smallest correction — to the documentation.** Either move the `ResolveTargetSectionAsync` call above the `loaded` query, or delete the second half of that README sentence. Whichever is chosen must land with the discriminating test C1 asks for; leaving both unpinned for a third pass is the actual defect.

**Also reported independently at.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:229` — 2 of the six backend agents reached this finding separately.

---

### F-08 — Writes README states as law that EVERY SaveChangesAsync in the folder goes through a translating helper; eight bare saves exist, and two of them can reach the global handler as a 500 exactly as that sentence warns (MEDIUM, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplatesWriter.cs:173`

**What the code does.** `ReorderNodeAsync` (`EfAbwabTemplatesWriter.cs:173`) and `DeleteNodeAsync` (`:211`) call `await db.SaveChangesAsync(cancellationToken);` with no try/catch. `AbwabTemplateNode.Version` is `IsRowVersion()` (`Persistence/Configurations/Abwab/AbwabTemplateNodeConfiguration.cs:68-69`), so EF puts `xmin` in the UPDATE's WHERE clause and raises `DbUpdateConcurrencyException` when a concurrent write moved a sibling row. `GlobalExceptionHandler.cs:54` maps every exception except `UserProvisioningEmailConflictException` to `500`. Six further bare saves: `EfAbwabTemplatesWriter.cs:21`, `:37`, `:58`; `EfAbwabDoorsWriter.cs:47`, `:81`; `EfAbwabRelationsWriter.cs:82` (the last two shapes are partly described elsewhere in the README, the templates ones are not).

**What it should do, and on whose authority.** The Writes README's own rule — 'Every `SaveChangesAsync` in this folder goes through a translating helper — a bare save is how a raw EF exception reaches the global handler as a 500 instead of a 409' (`Writes/Abwab/README.md:33-35`). This file explicitly declares itself the precedent for every later write feature (`:9-11`), so the rule is repo law for the area, not a description.

**Smallest correction — to the code.** Code: wrap `EfAbwabTemplatesWriter.ReorderNodeAsync` (:173) and `DeleteNodeAsync` (:211) in a concurrency-translating save so a lost race surfaces as a defined outcome rather than `500`. Because no templates route carries a version token (`Writes/Abwab/README.md:44-45`), a stale-token `409` has no wire meaning here — the honest mapping is a retry or a defined 409/404, decided by whoever fixes it. If instead the decision is that bare saves are acceptable on the paths that only touch `order_value`/`deleted_at`, then README:33-35 must stop saying 'Every' and name the exceptions with their file:LINEs.

**Corroborated.** 2 of the six backend agents reached this finding independently.

**Reviewer verification — the defect is wider than reported.** I read
`EfAbwabDoorsWriter.cs` directly. The two translating helpers are
`SaveTranslatingWriteExceptionsAsync` (`:740-754`) and `SaveTranslatingConcurrencyAsync`
(`:756-766`), and eight call sites use them. But **the doors writer itself also saves bare, on
its two most-used paths**: `CreateAsync` calls the translating helper at `:44` and then a bare
`await db.SaveChangesAsync(cancellationToken)` at `:47` for the alias write inside the same
transaction; `EditAsync` does the identical pair at `:78` and `:81`. So the README's absolute
rule is broken in the very class the README holds up as the exemplar, not only in
`EfAbwabTemplatesWriter`. Reachability differs between the two: on `CreateAsync:47` the alias
rows are inserts, so `DbUpdateConcurrencyException` is not realistically reachable; on
`EditAsync:81` `ReplaceAliasesAsync` issues UPDATEs to soft-delete existing aliases, which is a
genuine concurrency surface. Severity stays MEDIUM; the correction is the same helper call, in
two more places than the finding names.

---

### F-09 — Writes README claims door create is the only path needing an explicit transaction and the only one with two SaveChangesAsync calls; three other paths do both, one of them in the same class, and the same README contradicts itself 77 lines later (MEDIUM, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:77`

**What the code does.** `EfAbwabDoorsWriter.EditAsync` opens `await using var transaction = await db.Database.BeginTransactionAsync(...)` at `:77` and issues two saves (`:78` translating, `:81` bare) before `:83` commits. `EfAbwabTemplatesWriter.CreateAsync:20` does the same (saves at `:21` and `:37`, commit `:38`). `EfAbwabTemplateApplyWriter.ApplyAsync:18` opens a transaction spanning one save per tree level (`:103`, `:126`, commit `:130`).

**What it should do, and on whose authority.** `Writes/Abwab/README.md:169-171` — '**Create needs an explicit transaction**; nothing else does. It is the only path with two `SaveChangesAsync` calls…'. The same file already contradicts it at `:246`: '**The copy descends one level per `SaveChanges`, inside one transaction.**' The code is right on all three counts: Edit must be atomic across the door row and its alias diff, template create across the template and its root node, apply across all levels.

**Smallest correction — to the documentation.** Documentation: rewrite README:169-171 to state the rule that is actually true — any write whose result spans more than one `SaveChangesAsync` needs an explicit transaction — and cite the four paths (`EfAbwabDoorsWriter.cs:43`, `:77`; `EfAbwabTemplatesWriter.cs:20`; `EfAbwabTemplateApplyWriter.cs:18`). Do not change the code.

---

### F-10 — Writes README says the template apply writer has no tests of either kind; the test file exists and docs/TESTING_DEBT.md records the obligation it pays as PAID — two long-lived documents state opposite facts about the same file (MEDIUM, Abwab-owned)

**Citation.** `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTemplateApplyBehaviorTests.cs:16`

**What the code does.** `AbwabTemplateApplyBehaviorTests.cs` exists (74 lines) with `ApplyAsync_CopiesCarryTheTargetsSectionAtEveryDepth` at `:16-17`, running against the real `AbwabSchemaFixture`. `docs/TESTING_DEBT.md:61` (row 7) strikes the `section_id` inheritance obligation through and marks it '**paid** by `AbwabTemplateApplyBehaviorTests.ApplyAsync_CopiesCarryTheTargetsSectionAtEveryDepth`'.

**What it should do, and on whose authority.** `Writes/Abwab/README.md:286-288` asserts '**`EfAbwabRelationsWriter`, `EfAbwabTemplatesWriter`, and `EfAbwabTemplateApplyWriter` have none of either** — both features wrote no tests by decision'. That is true for the first two (grep finds no test reference to either) and false for the third. `docs/TESTING_DEBT.md` is repo law as the live ledger (root `CLAUDE.md`, long-lived-documentation list), so the README is the side that drifted.

**Smallest correction — to the documentation.** Documentation: amend README:286-291 to exclude `EfAbwabTemplateApplyWriter` from the 'none of either' claim and name `AbwabTemplateApplyBehaviorTests.cs:16`, keeping the rest of the paragraph (the apply writer is still the highest-value gap — row 7's other obligations remain open).

**Also reported independently at.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md:286` — 3 of the six backend agents reached this finding separately.

---

### F-11 — Both the Writes README and the Controllers README present the relations-add refusal set as exhaustive, and both omit three reachable 400s — including the fact that `direction` is REQUIRED for a Comprehensiveness relation (MEDIUM, Abwab-owned)

**Citation.** `Backend/application/QuranDashboard.Application/Abwab/Commands/Relations/AddDoorRelations/AddDoorRelationsHandler.cs:125`

**What the code does.** `AddDoorRelationsHandler.IsDirectionValidFor` (`:125-128`) requires `direction is not null && Enum.IsDefined(...)` for `Comprehensiveness` and requires `direction is null` for the other two types; a violation returns `InvalidDirection` → `400` (`AbwabDoorRelationsController.cs:47-49`). An undefined `type` returns `InvalidType` → `400` (`:82-85`, controller `:45-47`), and an empty `targetDoorIds` returns `InvalidRequest` → `400` (`:77-80`, controller `:43-45`).

**What it should do, and on whose authority.** `Writes/Abwab/README.md:215-217` says the call 'carries the anchor, the type, an **optional** direction, and N targets; **any refusal** — self (`400`), unknown id (`404`), archived endpoint (`400`), duplicate pair (`409`) — fails the whole batch'. `Controllers/README.md:37-38` says 'Self-relation and an archived endpoint are `400`; an unknown door id is `404`'. `Controllers/README.md:138-140` establishes that these files are the ONLY description of a route's failure statuses (the exported spec documents none), which makes an incomplete enumeration a real client-facing contract gap — and `docs/TESTING_DEBT.md:163-166` already records that every hand-maintained enumeration in the long-lived docs had drifted.

**Smallest correction — to the documentation.** Documentation: in `Writes/Abwab/README.md:215-217` change 'an optional direction' to state the actual rule (mandatory for `Comprehensiveness`, forbidden otherwise) and add the invalid-type and empty-target `400`s to the refusal list; mirror the same three in `Controllers/README.md:37-38`. Code is correct.

---

### F-12 — The section_id SET NOT NULL migration has no backfill and no guard against the exact NULL-row condition its own commit records as having existed, and the local remedy (wipe-abwab) does not exist for production's 785 doors (LOW, Abwab-owned — downgraded from the agent's MEDIUM, see below)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260802062011_RequireAbwabDoorSection.cs:13`

**What the code does.** Up() is a single AlterColumn to nullable:false — one statement, `ALTER TABLE abwab_doors ALTER COLUMN section_id SET NOT NULL`, with no UPDATE, no DEFAULT, and no pre-flight check. Commit 896585e0's own message records the local database held 529 doors of which 39 carried a NULL section, and states verbatim that 'SET NOT NULL would have failed without it [wipe-abwab]'. The NULL rows are explainable: the writes README (Writes/Abwab/README.md:115) records a prior 'detach-to-section_id = null' restore behavior that this slice removed. There is no auto-migrate and no pending-migration boot guard anywhere in the repo (grep for Migrate()/MigrateAsync/GetPendingMigrations over Backend/api and Backend/infrastructure returns nothing), so applying this to the production database is a manual `./scripts/update-db` step whose outcome is recorded nowhere in the repo.

**What it should do, and on whose authority.** On the authority of Backend/scripts/README.md:141-142, which names this exact hazard — 'a schema change that cannot survive existing abwab rows (a column becoming NOT NULL, say) has a sanctioned local reset' — and names only a LOCAL reset. Production abwab rows are, by that same README's line 142, 'authored curation data' that nothing restores, so the sanctioned remedy is unavailable there. A migration whose success depends on data the author destroyed to make it pass owes an explicit guard.

**Smallest correction — to the code.** Do not backfill (inventing a section id is what the commit correctly refused). Add a fail-closed pre-flight to the release procedure: `SELECT count(*) FROM abwab_doors WHERE section_id IS NULL` against production before the dev→main release that carries this migration, and if it is non-zero decide each row explicitly. If the migration has already been applied to production successfully, this finding is closed by fact and the correction is to record that in Backend/scripts/README.md so the next NOT NULL migration inherits the check rather than the luck.

**Reviewer verification — severity downgraded.** I read the migration directly
(`20260802062011_RequireAbwabDoorSection.cs:13-20`): its `Up` is a single bare
`AlterColumn<int>(... nullable: false, oldNullable: true)` with no backfill and no guard, exactly
as reported. Two facts the agent did not weigh, both of which lower the severity:

1. **PostgreSQL `ALTER COLUMN … SET NOT NULL` fails loud.** It does not default, coerce or drop
   NULL rows — it aborts the transaction and the whole migration rolls back. The failure mode is
   a halted deploy, not corrupted data, so this cannot be the "actively corrupting production"
   class the review's stop condition is about.
2. **It has already been applied successfully to production.** The Abwab release is live with 785
   doors, so at apply time no NULL rows existed and none can appear now that the column is
   `NOT NULL`.

The residual risk is therefore narrow and forward-looking: restoring a pre-2026-08-02 backup, or
standing up an environment from one, halts on this migration with no remedy in the repo
(`Backend/scripts/wipe-abwab` is a local-only tool). Real, bounded, fails safe → **LOW**.
I am flagging the downgrade explicitly rather than silently, because the finding is correct on
the facts and only its consequence changed.

---

### F-13 — Invariant 4 (canonical pair + unique index + derived dormancy) has zero test coverage anywhere in the repository — no behavior test, no schema test, no smoke test (MEDIUM, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabRelationsWriter.cs:45`

**What the code does.** The invariant itself HOLDS today — `DoorAId = Math.Min(doorId, targetId)` / `DoorBId = Math.Max(...)` at :45-46, CHECK `door_a_id < door_b_id` at AbwabDoorRelationConfiguration.cs:11-13, partial unique index `(door_a_id, door_b_id, relation_type) WHERE deleted_at IS NULL` at :82-84. But nothing pins it. AbwabSchemaTests.cs covers abwab_sections / abwab_doors / abwab_door_aliases only (:90, :103, :113) and never touches abwab_door_relations; SmokeAbwabWriteTests.cs contains no relations test (grep for relations/templates/apply returns nothing across its 1236 lines); the three relations routes are catalogued ParityOnly at SmokeRouteCatalog.cs:289,293,297, i.e. listed but never dispatched. The failure is not hypothetical-only: if a regression dropped Math.Min/Math.Max, the CHECK would raise SqlState 23514, which SaveTranslatingDuplicateAsync (:117-127) does not catch — it filters on 23505 alone — so the route would answer 500 rather than 409, with every existing test still green.

**What it should do, and on whose authority.** docs/TESTING_DEBT.md rows 1-3 already name this obligation in the ledger's own words: canonical pair ordering for all three types, broader_door_id direction storage, all-or-nothing multi-target add, the dormancy join, and the three routes' status/envelope contract. Repo law (root CLAUDE.md, 'Evidence worth keeping becomes a test that fails on drift, not a report') makes the ledger the agenda, not the exemption.

**Smallest correction — to the code.** Pay TESTING_DEBT row 1 first and narrowly: one behavior test asserting that adding (A→B) then (B→A) of the same type is refused as a duplicate rather than stored as a second row, plus one asserting door_a_id < door_b_id on a stored Comprehensiveness row. Both are cheap and both are the pair that makes 'delete from either side deletes the row' structural.

**Corroborated.** 2 of the six backend agents reached this finding independently.

---

### F-14 — Invariant 5's reversed decision — apply copies the root's CHILDREN, never the root — is pinned by no test, and neither is the (target, child) collision key nor the empty-root 400 (MEDIUM, Abwab-owned)

**Citation.** `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTemplateApplyBehaviorTests.cs:16`

**What the code does.** The only test the apply writer has is ApplyAsync_CopiesCarryTheTargetsSectionAtEveryDepth, and its own header comment (:5-7) states the scope explicitly: 'Only the section-inheritance obligation is paid here — the rest of row 7 (offsets, aliases, all-or-nothing, 409 collisions) stays open.' The code is correct today: rootChildren is enumerated at EfAbwabTemplateApplyWriter.cs:39 and rootNode is never passed to NewDoor anywhere in the file; the empty-root throw is at :39-42 and precedes the target read at :44; the collision pre-check keys on (ParentId ∈ targetIds, Name ∈ rootChildNames) at :62-68 and emits pairs of (target.Name, hit.Name) at :80. None of the three is asserted. The apply writer is also, per the writes README's own words, 'the only path in the repository that creates door rows outside EfAbwabDoorsWriter.CreateAsync' — which I confirmed: grep for AbwabDoors.Add/AddRange over all .cs returns exactly EfAbwabDoorsWriter.cs:36 and EfAbwabTemplateApplyWriter.cs:98,121, and grep for raw `abwab_doors` outside Migrations/ and Configurations/ returns only tests and the wipe script.

**What it should do, and on whose authority.** docs/TESTING_DEBT.md row 7 states the obligation verbatim and flags why it matters: 'The deep copy — restated for ux-slice-g's children-only reversal, same row, new surface. The root's direct children enumerated and copied recursively (never the root itself)…'. A reversal that only a comment and a README record is one refactor away from being reverted silently.

**Smallest correction — to the code.** One test on the reversal alone: apply a template whose root is named R with children C1, C2 to a target T, then assert T's direct children are exactly {C1, C2} and that no door named R exists under T. That single assertion is what a re-reversal would fail.

---

### F-15 — CreateDoorHandler has no catch for AbwabStaleVersionException, but the door-create save path can throw it — the caller gets a 500 instead of the documented 409 (MEDIUM, Abwab-owned)

**Citation.** `Backend/application/QuranDashboard.Application/Abwab/Commands/Doors/CreateDoor/CreateDoorHandler.cs:37`

**What the code does.** CreateDoorHandler wraps writer.CreateAsync in a try with five catch arms (ParentNotFound, SectionRequired, SectionNotFound, SectionParentMismatch, DuplicateName — lines 37-61). AbwabStaleVersionException is not among them. EfAbwabDoorsWriter.CreateAsync calls MaintainGlobalOrderAsync at :40 for every ROOT create; that helper loads every live root TRACKED (EfAbwabDoorsWriter.cs:682-685, no AsNoTracking) and ResequenceGlobal (:670-677) assigns GlobalOrderValue to them, so the save at :44 is an INSERT plus N UPDATEs on pre-existing rows. AbwabDoor.Version is a concurrency token (Persistence/Configurations/Abwab/AbwabDoorConfiguration.cs:66-67 — .IsRowVersion()), so each of those UPDATEs is concurrency-checked, and SaveTranslatingWriteExceptionsAsync (:746-749) converts DbUpdateConcurrencyException into AbwabStaleVersionException. Nothing catches it: GlobalExceptionHandler.cs:43-58 maps no Abwab exception and answers 500 / ApiMessages.UnexpectedError. Reachability chain: a root create whose ResequenceGlobal actually emits UPDATEs (i.e. live roots are not already contiguous in (GlobalOrderValue, Id) order, which two concurrent root creates both computing count+1 can produce) plus a concurrent write to one of those rows between the read and the save. Blast radius is bounded — CreateAsync holds an explicit transaction (:43) so nothing commits, and the invalidating decorator bumps in finally — so this is a wrong status code, not corrupted data.

**What it should do, and on whose authority.** On the authority of the write-side README, which states the exception set of exactly this save helper: Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md:36-37 — 'SaveTranslatingWriteExceptionsAsync — writes that put a row into the unique index's live scope (create, edit, move, bulk-move, restore): both a stale token and a duplicate name are reachable.' Create is named in that list. Same-area precedent: every other handler on that list catches it — EditDoorHandler.cs:43, MoveDoorHandler.cs:50, BulkMoveDoorsHandler.cs:55, RestoreDoorHandler.cs:50. The API contract for a stale token is 409 (Backend/api/QuranDashboard.Api/Controllers/README.md:19-20: 'Optimistic concurrency is uint xmin, surfaced as 409 in the shared envelope').

**Smallest correction — to the code.** Add a catch (AbwabStaleVersionException) arm to CreateDoorHandler mirroring EditDoorHandler.cs:43-47, add a CreateDoorOutcome.StaleVersion variant next to CreateDoorOutcome.cs:15, and map it to Conflict(ApiMessages.AbwabDoorStaleVersion) in AbwabDoorsController.Create. (CreateSectionHandler has the same missing arm at CreateSectionHandler.cs:29 but EfAbwabSectionsWriter.CreateAsync:9-26 inserts one row and updates none, so there it is genuinely unreachable — either add the arm for symmetry or narrow the README sentence to exclude section create.)

**Reviewer verification — mechanism confirmed, reachability narrowed.** Confirmed directly:
`CreateDoorHandler.cs:37-57` catches five exception types and `AbwabStaleVersionException` is not
among them, while its sibling `EditDoorHandler.cs:43` does catch it. The throw is real —
`CreateAsync` calls `SaveTranslatingWriteExceptionsAsync` at `EfAbwabDoorsWriter.cs:44`, which
converts `DbUpdateConcurrencyException` into `AbwabStaleVersionException` at `:748`.
The path is narrower than "the door-create save path can throw it" suggests, and worth stating
precisely so the fix is not mis-scoped: the door itself is an INSERT and cannot raise a
concurrency exception. The reachable route is `MaintainGlobalOrderAsync` at `CreateAsync:40`,
which is called for every ROOT door creation and issues tracked UPDATEs against sibling rows; if
a concurrent archive or move makes one of those updates match zero rows, EF raises
`DbUpdateConcurrencyException`, `:748` translates it, and the handler has no catch — so the
client gets a 500 where every other door write returns the documented 409. Creating a CHILD door
skips `:40` entirely and is not exposed. MEDIUM stands.

---

### F-16 — All six `Created(...)` calls pass a relative URI with no leading slash, so the `Location` header resolves to a wrong path per RFC 3986 (LOW, Abwab-owned)

**Citation.** `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:34`

**What the code does.** `Created($"api/abwab/doors/{success.Door.Id}", ...)` emits `Location: api/abwab/doors/5`. Resolved against a request URI of `/api/abwab/doors` the base is `/api/abwab/`, giving `/api/abwab/api/abwab/doors/5`. The same pattern is at AbwabSectionsController.cs:27, AbwabDoorRelationsController.cs:41, AbwabTemplatesController.cs:78, AbwabTemplatesController.cs:112, AbwabTemplateNodesController.cs:29. AbwabTemplatesController.cs:112 additionally points a multi-resource creation at the bare collection `api/abwab/doors`. These six are the only `Created(`/`CreatedAt*` calls in the entire Controllers tree, so there is no pre-existing precedent being followed.

**What it should do, and on whose authority.** A `Location` should be a resolvable URI. Authority: API_GUIDELINES.md:90 (`201 Created` — successful creation when a resource is created) plus the general "keep API contracts stable and intentional" rule at §8:194. Impact is bounded: no consumer reads it — grepping the frontend for `headers.get` finds only `ETag` reads (abwab-templates.facade.ts:89, :126; abwab-snapshot.facade.ts:52) and `Authorization`, and no smoke test asserts `Location`.

**Smallest correction — to the code.** Prefix each URI with `/`, or switch to `CreatedAtAction`. Six one-character edits.

---

### F-17 — The Controllers README calls the template reads "admin-only" in the same paragraph that states all Abwab routes are Open; they carry no authorization (LOW, Abwab-owned)

**Citation.** `Backend/api/QuranDashboard.Api/Controllers/README.md:17`

**What the code does.** README:16-18 describes `api/abwab/templates` + `api/abwab/templates/{templateId}` as "(the admin-only door templates and one template's flat node list)", and README:18-19 then says "Twenty-five routes in all. All routes are `Open`". `AbwabTemplatesController` carries `[ApiController]` and `[Route("api/abwab")]` only (AbwabTemplatesController.cs:11-12) — no `[Authorize]`, no policy. The same wording is mirrored at Persistence/Reads/Abwab/README.md:13.

**What it should do, and on whose authority.** API_GUIDELINES.md §11:238 — "Admin-only behavior must not be exposed as public endpoints later without authorization" — and §9:204 ("Do not expose sensitive/internal-only endpoints accidentally"). Calling a route admin-only in the contract index while it is anonymously reachable is the drift that rule exists to prevent. This is the same known-Open posture as the write routes, so the exposure itself is accepted; the wording is what misleads.

**Smallest correction — to the documentation.** Change "admin-only door templates" to "admin-authored door templates" in Controllers/README.md:17 and Reads/Abwab/README.md:13, so the phrase describes who writes them, not who may read them.

---

### F-18 — `AbwabDoorsController.cs` is 227 lines, over the 200-line soft threshold for controllers, with no recorded justification (LOW, Abwab-owned)

**Citation.** `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:227`

**What the code does.** 227 lines, eight actions, eight injected handlers (:16-23). BACKEND_STRUCTURE.md:416-417 sets controllers at soft 200 / hard 300. It is the only Abwab controller over soft; the others are 136, 105, 100, 77, 36.

**What it should do, and on whose authority.** BACKEND_STRUCTURE.md:410-411 — a soft threshold means "review and justify", not split. The area already shows awareness of the number: Controllers/README.md:39-40 explains the templates split as avoiding "nine actions on one [that] would sit at the 200-line threshold", which makes the doors controller's silent 227 the odd one out rather than a genuine size problem. Every line is `outcome switch` mapping; there is no logic to move.

**Smallest correction — to the documentation.** One sentence in Controllers/README.md recording that AbwabDoorsController sits at 227 lines of pure status mapping across eight door writes and is deliberately not split — same shape the templates split already documents.

---

### F-19 — Both READMEs say the relation row's xmin is never read; EF reads it on every UPDATE, which is precisely why DeleteAsync has a concurrency catch (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md:212`

**What the code does.** README:210-214: "The relation row still has its own `xmin` for symmetry with the two other abwab tables, but **nothing reads it** — delete addresses a row by id, add creates." In fact `AbwabDoorRelationConfiguration.cs:69-70` maps `Version` as `IsRowVersion()`, so EF puts `WHERE xmin = @original` on the soft-delete UPDATE; `EfAbwabRelationsWriter.cs:84-87` catches the resulting `DbUpdateConcurrencyException` and returns `false`. The catch is unexplainable if nothing read the token.

**What it should do, and on whose authority.** The intended statement — *no relation route carries a client-supplied token, and no `OriginalValue` is ever overridden* — is true and is what the surrounding paragraph needs. "Nothing reads it" is false and misdescribes the one place the token changes behavior.

**Smallest correction — to the documentation.** Replace "nothing reads it" with "no route carries it and no writer overrides its `OriginalValue`; EF still compares it on the soft-delete UPDATE, which is what `DeleteAsync`'s concurrency catch answers".

---

### F-20 — The tree snapshot's alias projection — a GroupBy/ToDictionaryAsync on the hottest read path — is asserted by no test (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTreeReader.cs:19`

**What the code does.** `:19-23` builds `aliasesByDoor` with `.Where(a => a.DeletedAtUtc == null).OrderBy(a => a.Id).GroupBy(a => a.DoorId).ToDictionaryAsync(...)` and `:52` projects it onto every door. `AbwabTreeReadTests.cs`'s seven tests (`:11, :35, :56, :86, :116, :141, :174`) assert counts, ordering, `SectionRetired`, and `Version`; none mentions an alias.

**What it should do, and on whose authority.** Reads README:66-68 states "**Aliases are live-only**, matching the write side's own DTO projection … a soft-deleted alias is gone from every read, not just the write response." That live-only rule is a stated invariant of the area with nothing asserting it, and `AbwabDoorWriteBehaviorTests.EditAsync_ReplacingAliases_SoftDeletesTheDroppedOnes` (:825) proves it only on the write response.

**Smallest correction — to the code.** Extend one existing tree-read test to assert that a door's snapshot aliases contain the live values and not a soft-deleted one. This also becomes the only coverage that the grouped projection translates at all.

---

### F-21 — A template's flat node list comes back root-last from the reader but root-first from the create response, because OrderBy on a nullable parent id is NULLS LAST in Postgres (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTemplatesReader.cs:105`

**What the code does.** `GetAsync` orders `.OrderBy(n => n.ParentNodeId).ThenBy(n => n.OrderValue).ThenBy(n => n.Id)` (:105-107); Postgres sorts ASC NULLS LAST, so the root node (`parent_node_id IS NULL`) is the **last** element of `AbwabTemplateDto.Nodes`. `EfAbwabTemplatesWriter.CreateAsync:40` returns the same DTO type as `[ToDto(root)]` — root first. The reader still finds the root correctly by predicate (`:118`).

**What it should do, and on whose authority.** Reads README:109-115 says templates are flat and each node carries `ParentNodeId`, so a consumer assembles the tree and list order is not contractual — which makes this benign today. But nothing states the order, and the two producers of the same DTO disagree, so any consumer that ever assumes `Nodes[0]` is the root is right half the time.

**Smallest correction — to the documentation.** Either state in the Reads README that `AbwabTemplateDto.Nodes` has no contractual order, or make the reader's ordering root-first explicitly (`OrderBy(n => n.ParentNodeId != null)` first). Do not fix it by having consumers index position 0.

---

### F-22 — Two live sections can share an OrderValue because create assigns count(live)+1 while section delete resequences nothing (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabSectionsWriter.cs:12`

**What the code does.** `CreateAsync:12` sets `OrderValue = CountAsync(s => s.DeletedAtUtc == null) + 1`; `DeleteAsync:47-70` soft-deletes without renumbering the survivors. Delete the middle of three sections and create a fourth: live orders are `{1, 3, 3}`. Doors do not have this hole — every door archive path calls `ResequenceSiblingsExcludingAsync`.

**What it should do, and on whose authority.** Writes README:82 states the rule for the whole folder: "**Every write leaves its sibling scope at `1..N`.**" Sections are the one writer that does not. The README is honest about it at :99-103 ("That duplicate-`OrderValue` condition is not fixed here (`docs/TESTING_DEBT.md` rows F1/F2); it is worked around"), and the workaround — the `(OrderValue, Id)` tie-break shared with the reader — is real and correct.

**Smallest correction — to the code.** Nothing to change in this pass; it is correctly ledgered as F1/F2 with an owner condition. Recorded here only so a cross-slice reader does not rediscover it as new: the `1..N` invariant in the README's opening rule has exactly one documented exception.

---

### F-23 — ResequenceSiblingsExcludingAsync still takes a nullable section id, dead nullability left behind by the section_id NOT NULL reversal (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:651`

**What the code does.** The helper's signature is `ResequenceSiblingsExcludingAsync(int? sectionId, int? parentId, ...)` and its predicate is `d.SectionId == sectionId` (:654), comparing a now-non-nullable column against an `int?`. Every one of its four call sites (:132, :285, :338, :367) passes a plain `int`. `AbwabDoor.SectionId` is `int` (AbwabDoor.cs:7) and the column is NOT NULL since migration 20260802062011.

**What it should do, and on whose authority.** Sibling precedent in the same file: `ResolveCreateSectionAsync`, `ResolveTargetSectionAsync` and `ResolveRestoreSectionAsync` all return a non-nullable `int` precisely so "no write path can reach `SaveChanges` with a section-less door" (Writes README:141-143). This one parameter is the last place in the writer where a section-less door is still expressible in the type system.

**Smallest correction — to the code.** Change the first parameter to `int`. Nothing else changes; all four call sites already pass non-null.

---

### F-24 — Four load-bearing file:LINE citations in the two Persistence READMEs point at the wrong lines or a wrong count, in an area whose own repo law requires facts to be proven from code with a file:LINE (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md:152`

**What the code does.** (1) `Writes/README.md:152` cites `EfAbwabDoorsWriter.cs:699-701` for `LoadChildrenByParentAsync`; that method is at `:614-624` (the unfiltered select at `:616-618`), while `:699-701` is inside `ReplaceAliasesAsync`. (2) `Writes/README.md:53` says `EfAbwabDoorsWriter` 'is already 816 lines'; it is 767 (`docs/TESTING_DEBT.md:159` row J1 carries the same 816). (3) `Reads/README.md:147-149` cites `AbwabCacheGeneration.cs:11` for the per-instance `Guid` and `:22` for the interpolation; the `Guid` is at `:7` and the interpolations at `:16`, `:18`, `:20-21` (the file is 26 lines). (4) `Reads/README.md:155` cites `AbwabDependencyInjection.cs:19-21` for the single-instance generation registration; that is at `:14-16` — `:19-21` is the sections-writer decorator.

**What it should do, and on whose authority.** Root `CLAUDE.md` planning-artifact lifecycle: a fact folded into a README must 'prove it from code with a `file:LINE`'. A citation that lands on unrelated code is a broken proof; the next reader either follows it into the wrong method or stops trusting the citations wholesale. Every underlying claim is substantively CONFIRMED — only the pointers rotted (most likely in the `comment-purge` pass, which shortened `EfAbwabDoorsWriter`).

**Smallest correction — to the documentation.** Documentation: repoint the four citations to `EfAbwabDoorsWriter.cs:614-624`, `AbwabCacheGeneration.cs:7` and `:16`, `AbwabDependencyInjection.cs:14-16`; replace '816 lines' with the rule ('past the 600-line hard threshold') rather than a number nothing asserts — the same treatment `docs/TESTING_DEBT.md:163-172` prescribes for drifting counts. Update `docs/TESTING_DEBT.md:159` in the same pass.

**Also reported independently at.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/README.md:148`; `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md:153` — 4 of the six backend agents reached this finding separately.

---

### F-25 — Two Abwab controllers carry `//` comment blocks in production API code, and one of them restates a README paragraph verbatim — the one comment form the root CLAUDE.md forbids with no exception (LOW, Abwab-owned)

**Citation.** `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTreeController.cs:16`

**What the code does.** `AbwabTreeController.cs:16-18` carries a three-line `//` block explaining that the validator is captured before the load. That is the same fact `Persistence/Reads/Abwab/README.md:169-171` already states ('**Capture before load.** … the failure direction is one extra query, never a stale hit.'). `AbwabDoorsController.cs:108-109` carries a second two-line `//` block about the reorder scope default, a fact `AbwabReorderScope.cs:254` already carries at the enum and `Controllers/README.md:30-32` already states.

**What it should do, and on whose authority.** Root `CLAUDE.md`, *Comments are forbidden by default*: `.cs` under `api/` is in scope; 'comments that repeat a README' are listed under 'Forbidden, with no exception'; and the three-part exception requires that the fact cannot be solved by a sentence in the nearest README — both facts already live in a README. `Controllers/README.md:55-57` invokes that policy for this folder but scopes its own claim narrowly to `///` XML docs, so the `//` blocks slipped through.

**Smallest correction — to the code.** Code: delete `AbwabTreeController.cs:16-18` and `AbwabDoorsController.cs:108-109`. Both facts are already in a README; nothing needs folding.

**Also reported independently at.** `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabDoorsController.cs:108` — 3 of the six backend agents reached this finding separately.

---

### F-26 — Reads README describes the snapshot relation count as 'One grouped query per snapshot'; the query does no grouping — it materializes every live pair and counts in memory (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTreeReader.cs:62`

**What the code does.** `GetLiveRelationCountsAsync` (`:62-70`) issues one LINQ query with two joins and `.ToListAsync()`, projecting `{ DoorAId, DoorBId }` for every visible relation row, then counts both endpoints in a C# loop at `:72-79`. There is no `GroupBy` and no SQL aggregate.

**What it should do, and on whose authority.** `Reads/Abwab/README.md:99-101` — 'One grouped query per snapshot (`GetLiveRelationCountsAsync`), incrementing **both** endpoints of each visible row — never one query per door, which would turn the snapshot into an N+1.' The substantive half (one query, not one per door, both endpoints incremented) is CONFIRMED; only the word 'grouped' is wrong, and it matters because the neighbouring templates paragraph (`:116-117`) genuinely does aggregate in SQL, so a reader comparing the two is told they use the same technique when they do not.

**Smallest correction — to the documentation.** Documentation: change 'One grouped query per snapshot' to 'One query per snapshot, counted in memory'. Whether it should aggregate in SQL is a performance question and explicitly out of this review's scope.

---

### F-27 — createdRoots in the apply writer is residue of the reversed root-copy decision — it holds the copied CHILDREN, and the name says the opposite of the invariant the file enforces (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs:87`

**What the code does.** `var createdRoots = new List<CopiedNode>(targets.Count * rootChildren.Count);` is populated at :99 with one entry per (target, rootChild) — copied children, each of which has target.Id as its ParentId (:97 passes target.Id to NewDoor's parentId). It is also the value returned as the response at :132. No code path copies the root; only the name survives from before the reversal.

**What it should do, and on whose authority.** CODING_PRINCIPLES.md / the clean-code-guard naming rules, and root CLAUDE.md's 'Comments are forbidden by default' remedy order — 'a better name' is the first remedy, and here the name is actively contradicting the one invariant (children, never the root) that a later slice reversed into place.

**Smallest correction — to the code.** Rename `createdRoots` to `copiedChildren` at :87, :99, :105, :132. Pure rename, no behavior change.

---

### F-28 — ResolveBroaderDoorId silently defaults a null direction on a Comprehensiveness relation to 'target is broader'; the guard that makes this unreachable lives in the handler, not at the writer seam, and nothing tests either (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabRelationsWriter.cs:137`

**What the code does.** `return direction == AbwabRelationDirection.AnchorMoreComprehensive ? anchorDoorId : targetDoorId;` — a null direction falls to the else branch and stores targetDoorId as broader. Both CHECK constraints are satisfied by that row, so it would commit a direction the caller never stated. It is unreachable through the API today: AddDoorRelationsHandler.cs:81-84 rejects it (`type == AbwabRelationType.Comprehensiveness ? direction is not null && Enum.IsDefined(direction.Value) : direction is null`) → InvalidDirection → 400 at AbwabDoorRelationsController.cs:132-133. The mirror case (a direction supplied for Similarity/Opposition) is discarded at :132-135 and also rejected upstream.

**What it should do, and on whose authority.** Writes/Abwab/README.md:220-225 makes the writer the owner of the pair-and-direction contract — 'The canonical pair is the writer's job… `broader_door_id` carries the direction (`NOT NULL` exactly for `Comprehensiveness`)'. A contract the writer owns should not depend for its correctness on a validation two layers up that the writer's own signature (`AbwabRelationDirection? direction`) invites callers to skip.

**Smallest correction — to the code.** Either tighten the ternary to throw on a null direction for Comprehensiveness, or leave the code and record in Writes/Abwab/README.md that direction validity is the handler's obligation, citing AddDoorRelationsHandler.cs:81-84 — so the next caller of IAbwabRelationsWriter.AddAsync knows it inherits that duty. Do not silently keep the default.

**Also reported independently at.** `Backend/application/QuranDashboard.Application/Abwab/Commands/Relations/AddDoorRelations/AddDoorRelationsHandler.cs:66` — 2 of the six backend agents reached this finding separately.

---

### F-29 — AbwabDuplicateNameException.Name and its two-branch message are dead — every catch site discards the exception, so neither is ever read (LOW, Abwab-owned)

**Citation.** `Backend/application/QuranDashboard.Application.Abstractions/Abwab/AbwabDuplicateNameException.cs:8`

**What the code does.** The type carries 'public string? Name { get; } = name;' and builds one of two English messages depending on whether name is null. All seven catch sites are exception-type-only with no binding — CreateDoorHandler.cs:57, EditDoorHandler.cs:48, MoveDoorHandler.cs:55, RestoreDoorHandler.cs:55, BulkMoveDoorsHandler.cs:60, CreateSectionHandler.cs:29, RenameSectionHandler.cs:40 — and each returns a variant carrying no payload, so the controller answers a fixed Arabic constant (ApiMessages.AbwabDoorDuplicateName / AbwabSectionDuplicateName). The handlers log the name from their own local variable, not from the exception. BulkMoveAsync even passes null deliberately (EfAbwabDoorsWriter.cs:296). Contrast the two sibling exceptions whose payloads ARE consumed: AbwabRelationDuplicateException.DoorNames read at AddDoorRelationsHandler.cs:62, AbwabTemplateApplyCollisionException.Collisions read at ApplyTemplateHandler.cs:56.

**What it should do, and on whose authority.** CODING_PRINCIPLES / the clean-code-guard AI-failure-modes pack (dead code, speculative configurability): a carried payload nothing reads is dead weight, and it invites a future reader to believe the duplicate name reaches the user when it cannot.

**Smallest correction — to the code.** Either delete the Name property and collapse the message to the single constant string, or bind it at the catch sites and surface it the way AbwabDoorRelationDuplicateWith(doorNames) does (ApiMessages.cs:159-162). Deleting is the smaller change; surfacing is the better product answer, since the Arabic duplicate-name message currently names no door.

---

### F-30 — The empty-doors rule has two answers: the bulk handlers refuse it as 400, the writer returns an empty success — the writer branch is unreachable dead defensive code (LOW, Abwab-owned)

**Citation.** `Backend/application/QuranDashboard.Application/Abwab/Commands/Doors/BulkMoveDoors/BulkMoveDoorsHandler.cs:16`

**What the code does.** BulkMoveDoorsHandler.cs:16-20 and BulkArchiveDoorsHandler.cs:16-20 both refuse 'command.Doors.Count == 0 || command.Doors.Any(door => door is null)' with InvalidRequest, which the controller maps to 400 (AbwabDoorsController.cs:142-143, :172-173). The implementations of the same operation answer differently: EfAbwabDoorsWriter.BulkMoveAsync:215-218 and BulkArchiveAsync:303-306 both 'if (doors.Count == 0) return [];' — a silent empty success. Since the handlers are the only callers of IAbwabDoorsWriter, the writer branches are unreachable today.

**What it should do, and on whose authority.** clean-code-guard ai-failure-modes: defensive guards for impossible cases, and DRY on the knowledge (one rule, one place). The interface IAbwabDoorsWriter.cs:34-40 states no contract for an empty list, so a second caller added later gets the writer's answer, not the handlers'.

**Smallest correction — to the code.** Delete the two unreachable early returns in EfAbwabDoorsWriter (:215-218, :303-306) and let the handler guard be the single statement of the rule; or, if the writer must stay callable stand-alone, state the empty-list contract on IAbwabDoorsWriter and make both sides agree.

---

### F-31 — AddDoorRelationsHandler's three request-shape refusals and GetDoorRelationsHandler's NotFound are the only Abwab refusals that log nothing (LOW, Abwab-owned)

**Citation.** `Backend/application/QuranDashboard.Application/Abwab/Commands/Relations/AddDoorRelations/AddDoorRelationsHandler.cs:20`

**What the code does.** AddDoorRelationsHandler returns InvalidRequest (:20), InvalidType (:25) and InvalidDirection (:30) with no logger call, while its own later refusals do log ('Rejected {feature} {operation} {reason} {doorId}' at :46, :51, :56, :61). GetDoorRelationsHandler.cs:20 returns NotFound with no log while every other NotFound in the feature logs 'Not found {feature} {operation} …' (EditDoorHandler.cs:36, MoveDoorHandler.cs:23, ReorderDoorHandler.cs:21, RestoreDoorHandler.cs:21, DeleteDoorHandler.cs:21, RenameSectionHandler.cs:28, ReorderSectionHandler.cs:21, DeleteSectionHandler.cs:33, DeleteDoorRelationHandler.cs:20).

**What it should do, and on whose authority.** Same-area precedent: all fourteen Abwab handlers use the identical structured-logging shape with FeatureName/OperationName constants, and the relations routes are the ones with NO dispatched smoke tests (docs/TESTING_DEBT.md abwab-relations rows 1-3), so the log line is the only observability those three refusals have in production.

**Smallest correction — to the code.** Add the matching logger.LogWarning('Rejected {feature} {operation} {reason} {doorId}', …) to the three early returns in AddDoorRelationsHandler and a 'Not found' line to GetDoorRelationsHandler.cs:20.

---

### F-32 — The '816 lines' figure for EfAbwabDoorsWriter is wrong in two long-lived docs, and TESTING_DEBT additionally says the file got larger when it got smaller (LOW, Abwab-owned)

**Citation.** `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md:53`

**What the code does.** Writes/Abwab/README.md:53 states '`EfAbwabDoorsWriter` is already 816 lines against that section's 600-line hard threshold'. docs/TESTING_DEBT.md:159 (row J1) states '816 lines before this feature and larger after'. `wc -l` on Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs returns 767 — smaller, not larger.

**What it should do, and on whose authority.** Root CLAUDE.md: 'a canonical count, source hash, or measured budget with nothing asserting it is a rumour.' Nothing asserts this count, and it has already drifted. The argument both docs are making (the file is past the 600-line hard threshold and a split is owed) survives at 767 without any number.

**Smallest correction — to the documentation.** Drop the number from both places rather than updating it — 'past the 600-line hard threshold' is the durable claim; delete 'and larger after' from TESTING_DEBT.md:159, which is now false in the opposite direction.

**Corroborated.** 2 of the six backend agents reached this finding independently.

---

### F-33 — GET templates/{id} compares the validator before the existence check, so a crafted If-None-Match makes a nonexistent template answer 304 instead of 404 (LOW, Abwab-owned)

**Citation.** `Backend/api/QuranDashboard.Api/Controllers/Abwab/AbwabTemplatesController.cs:47`

**What the code does.** Lines 47-53 compute `validators.TemplateETag(templateId)` and return 304 on a match before `getTemplateHandler` ever runs. The validator embeds only the template id, the boot id and the shared templates generation — no row data and no existence check. A caller who has the templates-list ETag `"abwab-templates-{boot}-{gen}"` can trivially derive `"abwab-template-{anyId}-{boot}-{gen}"` and receive 304 for a template id that has never existed.

**What it should do, and on whose authority.** API_GUIDELINES.md:163 — 'A 404 carries no validator headers: an absence has no representation to validate.' The intent is that an absence is not a validatable resource; answering it 304 contradicts that. Note the reachability honestly: no normal client can hit this, because every template delete bumps `_templatesGeneration` (InvalidatingAbwabTemplatesWriter.cs:39), so a legitimately-held ETag for a deleted template is always stale by the time it is sent. It requires a hand-crafted header, and a 304 carries no body, so nothing leaks.

**Smallest correction — to the code.** Either run the handler first and only take the 304 branch on `GetTemplateOutcome.Success` (the 404 branch already sets no validator headers, so nothing else changes), or state the deviation in API_GUIDELINES.md §Conditional GETs alongside the existing `*` fail-open paragraph.

---

### F-34 — Nothing anywhere asserts the ETag/generation/304 mechanism — already recorded as debt, flagged so the parent does not double-count it (LOW, Abwab-owned)

**Citation.** `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTreeReadTests.cs:5`

**What the code does.** `grep -rn "NotModified|IfNoneMatch|If-None-Match|ETag|IAbwabCacheValidators|IAbwabCacheInvalidator|AbwabCacheGeneration" Backend/tests/ --include=*.cs` returns ZERO hits. The five decorators, the generation counter, the boot id, the capture-before-load stamp, the 304 branch and the 21 bump sites are all unexercised; every one of them could be deleted and the whole suite would stay green. Backend/tests/QuranDashboard.Tests/Abwab/ contains only AbwabDoorWriteBehaviorTests, AbwabSchemaTests, AbwabTemplateApplyBehaviorTests and AbwabTreeReadTests, none of which touch caching.

**What it should do, and on whose authority.** Root CLAUDE.md: 'Evidence worth keeping becomes a test that fails on drift, not a report... If the assertion has nowhere to live yet, keep the file and record in docs/TESTING_DEBT.md what the test must assert and where it must go.' That ledger entry EXISTS and is precise — docs/TESTING_DEBT.md rows I1 (generation lifecycle), I2 (conditional-GET contract of the three reads), I3 (templates facade 304 path) and I4 (the just-wrote invariant end to end). So the process was followed; this is recorded debt, not a silent gap.

**Smallest correction — to the code.** None required now — I1–I4 already state what must be asserted and where. Recorded here only so a cross-slice pass does not raise it as a new finding, and because I2's trigger ('acceptance criterion of the auth feature') makes it due with the next feature.
---

### F-35 — The bulk selection set survives a section switch, so bulk move / bulk archive / bulk relations operate on doors that are not in the visible tree (HIGH, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-selection.store.ts:37`

**What the code does.** `buildAbwabQueryParams` invalidates only `door`, `card` and `modal` when `section` changes (`abwab-url-sync.ts:100-105`). The page reacts by calling `AbwabSelectionStore.clearSelection()` when `parsed.door === null` (`abwab-page.component.ts:264-266`), and `clearSelection()` touches only `selectedDoorId`/`selectedVersion` (`abwab-selection.store.ts:37-39`) — it does not touch `bulkSet`. The only thing that ever clears `bulkSet` is `setBulkMode(false)` (`abwab-selection.store.ts:57`) and `clearBulk()` (`:73-75`), and the archive toggle reaches the former via `setArchiveViewActive(true)` (`:41-46`). Nothing in the state layer or the page clears it on a section change, and the section tabs are NOT disabled in bulk mode — `abwab-toolbar.component.html:20-37` renders every tab with an unconditional `(click)="selectSection(section.id)"`, gated only by `hideSectionControls` = `archiveParam()` (`abwab-page.component.html:76`). `AbwabWriteController.currentBulkRefs()` then filters only on `isArchived` (`abwab-write.controller.ts:164-172`), so every id from the previous section is submitted.

**What it should do, and on whose authority.** A scope switch must invalidate the bulk set exactly as it invalidates the single selection. The authority is the feature README's own rule, which the code half-implements: `abwab/README.md:544-546` — «**The URL is the single source of truth for the selection.** … Without that, the invalidation above would hold in the URL and silently fail in the store — leaving the side panel offering edit/move/archive on a door that is no longer in scope, which is exactly what §6.2's M22 cell forbids.» That reasoning is scope-general; the implementation applies it only to the single selection. Same-area precedent: the archive toggle already drops bulk (`abwab-selection.store.ts:41-46`), proving a scope change is understood to invalidate bulk — the section change is the same class of event.

**Smallest correction — to the code.** Give `AbwabSelectionStore` a section-scope input the way it already has `setArchiveViewActive`, and clear `bulkSet` (or the whole bulk mode) when it changes; or, minimally, have `AbwabPageComponent.onSectionChanged` call `this.selection.clearBulk()` alongside the query patch. Prefer the store-side rule so the invariant is not re-derivable per call site. Add a spec case in `abwab-selection.store.spec.ts` pinning it.

**Reviewer verification — CONFIRMED end to end.** This is the finding the cross-slice pass
exists to catch, so I traced every link myself rather than accepting the report:

1. `abwab-selection.store.ts:37-39` — `clearSelection()` sets `selectedDoorId`/`selectedVersion`
   to null and **does not touch `bulkSet`**. Confirmed by reading the method.
2. `abwab-selection.store.ts:41-46` — `setArchiveViewActive(true)` calls `setBulkMode(false)`,
   which at `:48-58` resets `bulkSet` to a new `Map()`. **So the archive scope change DOES clear
   bulk.** This is the asymmetry that makes the defect legible.
3. `grep` for every `clearBulk()` / `setBulkMode(` call site in the feature returns exactly two
   outside the store: `abwab-page.component.ts:317` (the bulk-mode toggle) and `:329` (the
   explicit clear button). **Neither is reachable from a section change.**
4. `abwab-page.component.ts:278-280` — `onSectionChanged` is three lines: it patches the query
   params and nothing else.
5. `abwab-toolbar.component.html:20-37` — the section tabs render with an unconditional
   `(click)="selectSection(section.id)"`, gated only by `hideSectionControls()`, which is the
   archive flag. **The tabs stay live in bulk mode**, so the user reaches this with one click.
6. `abwab-write.controller.ts:164-172` — `currentBulkRefs()` filters on `!node.isArchived`
   **only**. Section membership is never consulted, so every stale id is submitted by
   `bulkMoveDoors` (`:141`) and `bulkArchiveDoors` (`:149`).

**Why this is HIGH and not MEDIUM.** The user selects doors in section A, clicks section B, and
presses bulk archive. The confirm dialog counts the still-live bulk set, so it names a number the
user cannot reconcile with what is on screen, and the request archives doors that are not
visible. That is state corruption reachable in three clicks with no error and no undo — squarely
the review's HIGH definition. The archive path proving the rule is understood makes this an
omission at a seam between slices, not a missing concept.

**A second entry path, found in the cross-slice pass — the user need not click a tab.**
`onRevealRequested` (`abwab-page.component.ts:413-415`) writes `section` itself whenever the
revealed door lives outside the active section:
`...(this.activeSectionId() !== null && node.sectionId !== this.activeSectionId()
? { section: node.sectionId } : {})`. Reveal is reached from the relations modal — a slice-L
path — and it silently moves the user's scope through exactly the code that clears `door`, `card`
and `modal` but not `bulkSet`. So the stale-bulk state is reachable without the user ever
choosing a new section, which both widens the exposure and explains why per-slice review missed
it: the slice that added reveal and the slice that added bulk never appear in the same diff.

---

### F-36 — `pendingRequest?.unsubscribe()` cannot cancel an in-flight request because `shareReplay(1)` keeps the source subscribed, so an older tree response can overwrite a newer one along with its ETag (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-snapshot.facade.ts:42`

**What the code does.** `fetch()` calls `this.pendingRequest?.unsubscribe()` (`:42`), builds `request$` ending in `shareReplay(1)` (`:69`), then subscribes it into `pendingRequest` (`:72`) and also returns it (`:73`). `shareReplay(1)` with no config is `{ bufferSize: 1, refCount: false }` — the internal ReplaySubject subscribes to the HttpClient source on the first subscriber and NEVER unsubscribes, so unsubscribing every downstream subscriber does not tear down the HTTP request. The previous fetch's `tap` (`:47-57`) therefore still runs when it lands, writing `rawTree` and `etagState`. If refresh A is issued, then refresh B, and A resolves last, the UI shows A's tree and holds A's ETag. `AbwabWriteController.refreshAndRebind()` (`:259-265`) then calls `selection.rebindTo(stale snapshot)`, rebinding every cached `version` token off the older tree. The identical pattern is in `AbwabTemplatesFacade.fetchList` (`:79,106,109`) and `fetchSelected` (`:114,144,147`). No spec covers concurrency: `grep -n "unsubscribe|pendingRequest|shareReplay|race|cancel" state/abwab-snapshot.facade.spec.ts state/abwab-templates.facade.spec.ts` returns nothing.

**What it should do, and on whose authority.** Either the cancellation must actually work, or the guard must go and be replaced by a real last-write-wins discipline. Authority: `abwab/README.md:672-681` states refresh-after-write is «an invariant, not an optimization … Skipping the refresh reproduces spurious `409`s on the very next write» — rebinding off a stale tree is functionally the same as skipping it. `CODING_PRINCIPLES.md` §2 («Code should read clearly without hidden surprises») and the clean-code-guard AI-failure-mode «defensive guards that do not guard» both bear on a line that reads as protection and is inert.

**Smallest correction — to the code.** Use `shareReplay({ bufferSize: 1, refCount: true })` so unsubscribing the last subscriber tears the source down, or drop the manual `Subscription` bookkeeping and switch the fetch trigger to a `Subject` + `switchMap`. Add a spec asserting that an out-of-order response does not overwrite a newer `rawTree`/`etagState`.

---

### F-37 — A restored URL carrying a `section` id that no longer exists produces a permanently empty tree with a `0` stat and no active tab — the parse validates shape but not existence, contradicting the README's "fails closed to the defaults" (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-url-sync.ts:64`

**What the code does.** `parseAbwabQueryParams` runs `section` through `parsePositiveId` (`abwab-url-sync.ts:64`, `:17-23`), which checks only `Number.isInteger(value) && value > 0` (`abwab.models.ts:95-97`). Nothing reconciles the id against `snapshot.sections`. Downstream: `filterAbwabRootsBySection` returns `roots.filter((root) => root.sectionId === sectionId)` (`abwab-tree.builder.ts:103`) → empty; `countAbwabDoorsInOpenScope` returns `sections.find(...)?.doorsInScopeCount ?? 0` (`abwab-tree.builder.ts:185`) → `0`; and the toolbar renders one tab per existing section with `[selected]="activeSectionId() === section.id"` plus the all-doors tab with `[selected]="activeSectionId() === null"` (`abwab-toolbar.component.html:7,24`) — so with a dead id NO tab is selected. The user sees an empty tree, a `0` count, and no highlighted tab, with no indication why. Deleting a section the user had bookmarked (or had open in a second tab) reaches this.

**What it should do, and on whose authority.** `abwab/README.md:530` states «Fails closed to the defaults on anything invalid», and the `door`/`modal` keys implement exactly that — a `door` for a missing node yields no selection, and `restorableModal` refuses a subject that is not in `byId` (`abwab-modal-url.controller.ts:31-34`). `section` gets shape validation only, so it is the one key whose invalid value produces a silent dead state rather than a default. The README asserts a uniform rule the code does not honor for this key.

**Smallest correction — to the code.** In the page, once the snapshot has landed, fall back to `null` («كل الأبواب») when `activeSectionId()` is not in `snapshot.sections` — mirroring the existing `door=` settle-gated effect (`abwab-page.component.ts:231-242`) — and rewrite the URL by replace. Alternatively narrow the README claim to say `section` is validated for shape only and a dead id shows an empty scope. Fix the code; the README statement is the correct contract.

---

### F-38 — The chrome-inert "blast radius" membership test documented in two places is wrong: the global detail-overlay shell holds the scroll lock imperatively and never applies `qdModalScrollLock`, so the prescribed grep under-reports the radius (MEDIUM, Abwab-owned)

**Citation.** `src/app/shared/ui/detail-modal-shell/detail-modal-shell.component.ts:63`

**What the code does.** `DetailModalShellComponent` injects `ScrollLockService` (`:27`) and calls `this.scrollLock.acquire()` inside an `effect()` when `visibility() === 'open'` (`:63`), releasing at `:66` and `:104`. Its template renders `.qd-modal-backdrop` (`detail-modal-shell.component.html:2`) with no `qdModalScrollLock` attribute. Because it holds the lock, `ScrollLockService.isLocked` is true and `.qd-navbar` goes inert for every entity detail overlay in the app — a surface the documented grep does not return.

**What it should do, and on whose authority.** `.architecture/UI_STYLE_SYSTEM.md` §17 "Chrome-inert rule" states: "the rule is the membership test, not a list. Any surface that applies `qdModalScrollLock` makes the chrome inert, so **the directive's usages ARE the blast radius** — `grep -rn qdModalScrollLock src/app/` answers it". `src/app/shared/README.md:99-100` repeats it: "Which surfaces hold the lock is not a list to maintain here — it is whatever applies `qdModalScrollLock`, so `grep -rn qdModalScrollLock src/app/` is the answer." Authority: both are long-lived docs (architecture doc + nearest README) and both are provably false against `detail-modal-shell.component.ts:63`.

**Smallest correction — to the documentation.** Correct the membership test in both places to "whatever holds `ScrollLockService`'s lock — the directive's usages plus `detail-modal-shell.component.ts:63`, which acquires it imperatively", OR (cleaner, and makes the stated grep true) have `detail-modal-shell.component.html:2` apply `qdModalScrollLock` and delete the imperative effect. `docs/TESTING_DEBT.md:177` row E2 should assert whichever wording survives.

---

### F-39 — The Chrome-inert rule states a hard count ("these nine") in the same paragraph that forbids stating a count, and the number is wrong — there are 12 `qdModalScrollLock` holders plus 1 imperative holder (MEDIUM, Abwab-owned)

**Citation.** `.architecture/UI_STYLE_SYSTEM.md:1503`

**What the code does.** `grep -rn qdModalScrollLock src/app/` returns 12 template holders: 6 abwab modals (`abwab-door-modal`, `abwab-move-picker`, `abwab-relations-modal`, `abwab-sections-modal`, `abwab-template-copy-modal`, `abwab-template-node-modal`, each at `.component.html:9`), 5 words surfaces (`lemma-details-panel:96`, `root-details-panel:97`, `stem-details-panel:97`, `word-type-details-panel:97`, `word-drilldown-modal:111`) and `shared/ui/confirm-dialog/confirm-dialog.component.html:9`. `detail-modal-shell.component.ts:63` is a 13th holder. The doc says nine.

**What it should do, and on whose authority.** The same bullet says "no count belongs here because every new dialog moves it", and root `CLAUDE.md` repo law says "a canonical count, source hash, or measured budget with nothing asserting it is a rumour". Authority: root CLAUDE.md lifecycle rule + the paragraph's own stated policy.

**Smallest correction — to the documentation.** Delete "nine" from the sentence: "The navbar is keyboard-unreachable while any of these is open." No number in prose; `docs/TESTING_DEBT.md:177` row E2 is already the place the invariant gets asserted.

---

### F-40 — A click on a dropdown trigger cannot open its menu once `mouseenter` has already fired — the `<li>`'s hover-open and the button's click-toggle fight each other; Slice H doubled the affected surface from one dropdown to two (MEDIUM, pre-existing)

**Citation.** `src/app/core/layout/top-navbar/top-navbar.component.html:19`

**What the code does.** The `<li class="nav-dropdown">` binds `(mouseenter)="openMenu(item.key)"` (`:19`) and `(mouseleave)="closeMenu(item.key)"` (`:20`), while the trigger `<button>` binds `(click)="toggleMenu(item.key)"` (`:26`). `toggleMenu` is `this.openMenuKey = this.openMenuKey === key ? null : key` (`top-navbar.component.ts:75`). For any pointer type that dispatches `mouseenter` before `click` on the same interaction, the menu is already open when the click lands, so the click closes it. The repo states this in its own words: `e2e/shell-nav.e2e.ts:15-16` — "Hover, not click: the words item opens on `mouseenter` and the button's own click handler toggles it shut again, so a Playwright click would open and close the menu in one action" — and commit `d7a9c0fb` calls it "the hover-then-click-closes quirk". The `more` dropdown (`:90-98`) has no hover handlers and is therefore unaffected, so two visually identical triggers in the same navbar answer a click differently.

**What it should do, and on whose authority.** Per `DESIGN.md` / `PRODUCT.md` ("trustworthy structure", "calm for long focus") and ordinary disclosure semantics, an `aria-expanded` toggle button must open on activation. Authority: same-area precedent — the `more` trigger in the same template is click-only and works; `.architecture/UI_STYLE_SYSTEM.md` §12 Accessibility.

**Smallest correction — to the code.** Make the trigger button's click a pure open (`openMenu(item.key)`) when the pointer is hovering, or drop `(mouseenter)`/`(mouseleave)` from the `<li>` and make all three dropdowns click-only — matching the `more` trigger and letting `e2e/shell-nav.e2e.ts:17` use `.click()` like the other two tests.

---

### F-41 — The nav dropdown drops focus to `<body>` when it closes — Escape, outside-click and link-click all destroy the `<ul>` via `@if` with no focus return to the trigger (MEDIUM, Abwab-owned)

**Citation.** `src/app/core/layout/top-navbar/top-navbar.component.html:51`

**What the code does.** The menu is `@if (openMenuKey === item.key) { <ul …> }` (`:51`), so closing removes it from the DOM. `onEscape()` (`top-navbar.component.ts:42-49`), `onDocumentClick()` (`:52-62`) and `closeMenu()` (`:68-72`) only null `openMenuKey`; nothing calls `.focus()` on the trigger. A keyboard user who has tabbed into the dropdown and presses Escape loses focus to `<body>` and must re-tab from the top of the page. There is also no `ArrowDown`/`ArrowUp`/`Home`/`End` handling anywhere in the component — the only key handled is Escape.

**What it should do, and on whose authority.** Restore focus to the trigger button on Escape-close. Authority: same-app precedent — `shared/ui/detail-modal-shell/detail-modal-shell.component.ts:84` explicitly restores focus on close (`setTimeout(() => this.restoreButton()?.nativeElement.focus(), 0)`), and `features/words/README.md:287-288` documents the association-filter combobox restoring focus to the field on Escape. `.architecture/UI_STYLE_SYSTEM.md` §12 requires keyboard operability.

**Smallest correction — to the code.** In `onEscape()`, capture the open menu's trigger (`[data-testid]='nav-' + key + '-trigger'`) before nulling `openMenuKey` and `.focus()` it after. Arrow/Home/End can stay out of scope as long as the popup is not announced as a menu (see the `aria-haspopup` finding).

**Also reported independently at.** `src/app/core/layout/top-navbar/top-navbar.component.html:27`; `src/app/core/layout/top-navbar/top-navbar.component.ts:41` — merged from 3 separate agent findings covering the same defect.

---

### F-42 — The `more` dropdown is a hand-rolled parallel branch keyed on the magic string `'more'` — it is the surviving pre-dropdown remnant, duplicating the entire dropdown markup (including the chevron SVG verbatim) and behaving differently from the data-driven ones (MEDIUM, Abwab-owned)

**Citation.** `src/app/core/layout/top-navbar/top-navbar.component.html:85`

**What the code does.** Lines 13-84 render primary items generically — `@for (item of primaryItems)` + `@if (item.children)` — with interpolated `id`, `aria-controls`, `data-testid` and `track child.key`; that half is genuinely data-driven, no key switch. Lines 85-136 then hand-roll a second dropdown for `moreItems` with the literal key `'more'` hardcoded five times (`:87`, `:88`, `:94`, `:97`, `:119`, `:128`), a literal `id="more-menu"` and `aria-controls="more-menu"`, a literal Arabic label `المزيد` (`:100`) that lives in no `NavItem`, and a byte-identical copy of the chevron `<svg>` from `:33-49`. It also opens on click only, with no hover handlers.

**What it should do, and on whose authority.** `src/app/core/README.md:65-66` asserts the dropdown "is `@if (item.children)`, data-driven, not a per-key template branch" — true of the primary items, not of the `more` group, which the README does not mention. The `NavItem` model already carries everything needed (`nav-items.ts:1-9`); a synthetic parent `NavItem` with `children: moreItems` would collapse the branch. Authority: core README's own claim + `CODING_PRINCIPLES.md` DRY / no-magic-strings.

**Smallest correction — to the code.** Build the `more` entry as a `NavItem` (`key: 'more'`, `labelAr: 'المزيد'`, `children: moreItems`) in `nav-menu.ts` and render it through the same `@if (item.children)` branch; delete lines 85-136. If the group must stay separate, at minimum lift `'more'` and `المزيد` out of the template into `nav-menu.ts`.

---

### F-43 — UI_STYLE_SYSTEM.md §17 asserts all six abwab modals carry an "unconditional cdkTrapFocus"; the sections modal's trap is conditional, and that conditionality is load-bearing (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:1143`

**What the code does.** abwab-sections-modal.component.html:10 binds `[cdkTrapFocus]="deleteConfirmId() === null"` — the host trap yields while the nested delete-confirm dialog is open. The other five authoring modals do carry a bare `cdkTrapFocus` (abwab-door-modal:10, abwab-move-picker:10, abwab-relations-modal:10, abwab-template-copy-modal:10, abwab-template-node-modal:10).

**What it should do, and on whose authority.** The abwab feature README, the nearer and more specific authority for this area, states the opposite of §17 and states the reason: "the sections modal binds `[cdkTrapFocus]="deleteConfirmId() === null"` so its delete confirm's own trap is the only live one … Two live traps fight over focus" (features/abwab/README.md:602-608). Root CLAUDE.md's Local README Context rule makes the nearest README the current truth; §17 is the stale copy.

**Smallest correction — to the documentation.** In UI_STYLE_SYSTEM.md:1143, change "an unconditional `cdkTrapFocus`" to "`cdkTrapFocus` — unconditional in five of the six; `abwab-sections-modal` yields its trap while its nested delete confirm is open (see `features/abwab/README.md`)".

**Also reported independently at.** `src/app/features/abwab/components/abwab-relations-modal/abwab-relations-modal.component.html:10` — merged from 2 separate agent findings covering the same defect.

---

### F-44 — Reversal #3's code citation points at lines that state nothing, and claims a "class comment" that does not exist and could not exist under the workspace comment ban (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:1006`

**What the code does.** EfAbwabTemplateApplyWriter.cs:16-18 is `var targetIds = targetDoorIds.Distinct().ToList();`, a blank line, and `await using var transaction = await db.Database.BeginTransactionAsync(...)`. The copy-the-children rule is actually implemented at :31 (`var rootNode = nodes.Find(n => n.ParentNodeId is null)`), :39 (`childrenByParentNode.TryGetValue(rootNode.Id, out var rootChildren)`) and :87-99 (one copied door per `rootChildren[i]` per target). The file contains zero comments: `grep -c "//"` returns 0 and `grep "/\*"` returns nothing.

**What it should do, and on whose authority.** Per root CLAUDE.md ("Comments are forbidden by default" … "No `///` XML-doc anywhere in scope", Backend/CLAUDE.md), a class comment restating the rule is forbidden in `Backend/infrastructure/`. The README's own second pointer is correct: `Persistence/Writes/Abwab/README.md:232` does hold the axiom ("**A template is a door subtree, and applying it copies the root's DIRECT CHILDREN — never the root**"). Only the code citation and the comment claim are wrong. Behavior itself is CONFIRMED — the reversal holds.

**Smallest correction — to the documentation.** README.md:1006-1008: replace "`EfAbwabTemplateApplyWriter.cs:16-18` states the current rule, and `Persistence/Writes/Abwab/README.md` holds the axiom. The consequence…" with "`EfAbwabTemplateApplyWriter.cs:31-39` and `:87-99` implement it, and `Persistence/Writes/Abwab/README.md:232` holds the axiom." Delete the phrase "and the writer's own class comment restates it".

---

### F-45 — The Browser-e2e section enumerates five abwab specs as the complete set; eight exist and the Playwright abwab project captures all eight — including the one this same README elsewhere cites as the pin for the reveal/Back contract (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:965`

**What the code does.** `e2e/` holds eight `abwab-*.e2e.ts` files: abwab-archive, abwab-global-order, abwab-operations, abwab-relations, abwab-slice-j-widths, abwab-structure, abwab-tree-row-budget, abwab-url-and-a11y. playwright.config.ts:38 defines `{ name: 'abwab', testMatch: /abwab-.*\.e2e\.ts$/ }`, which matches all eight; single-worker comes from package.json:10 (`playwright test --project=abwab --workers=1`), not from the project block.

**What it should do, and on whose authority.** `e2e/README.md:41` ("then `abwab` (the eight `abwab-*.e2e.ts` specs, 1 worker") and `:69` ("**The eight Abwab specs run single-worker…**") are correct. The abwab README declares itself "the current record" (:10-11, :1019-1020), so its own inventory must match. The consequence is not the DB-race hazard — the `abwab-` naming convention and e2e/README already handle that — it is that a reader taking :965-976 as the complete inventory would conclude `abwab-relations.e2e.ts` does not exist, while README:522 relies on it as the pin for the reveal/Back-restores-the-source contract.

**Smallest correction — to the documentation.** README.md:965-976: extend the list to the eight actual files (or say "the eight `abwab-*.e2e.ts` specs — see `e2e/README.md` for the inventory") and change "these five specs" to "these specs".

---

### F-46 — README points at docs/TESTING_DEBT.md for the untested relation-delete dispatch; no such ledger row exists, so a real uncovered branch is scheduled by nothing (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:960`

**What the code does.** `AbwabWriteController.deleteRelation` (abwab-write.controller.ts:160-162) dispatches `api.deleteRelation`, a route typed `ApiResponse<unknown> | null` (abwab.api.ts:102-104) that answers 204, so it rides the null-envelope branch at abwab-write.controller.ts:183. `abwab-write.controller.spec.ts` contains no case for it (grep for "relation" returns only line 25, a `relationCount: 0` fixture field). `docs/TESTING_DEBT.md`'s abwab-relations table (:35-37) has exactly three rows, all backend: the relations writer, the relations reader, and the relations route smoke. None covers this. (The modal-level delete UX *is* covered — abwab-relations-modal.component.spec.ts:609-694 exercises confirm, cancel, busy and error — but against a mocked `deleteRelation` function input, which never reaches the controller's null-envelope branch.)

**What it should do, and on whose authority.** Root CLAUDE.md makes `docs/TESTING_DEBT.md` "a live ledger and the agenda of the next feature", and requires that evidence with nowhere to live be recorded there rather than asserted. A README sentence citing a ledger row that does not exist is the failure mode that rule exists to prevent: the branch that once shipped a real defect for single-door archive (README:952-957) is untested for relation delete and unrecorded.

**Smallest correction — to the documentation.** Add a row to `docs/TESTING_DEBT.md`'s abwab-relations table: "`AbwabWriteController.deleteRelation`'s null-envelope (204) success branch — no spec; the archive half of the same `handleSuccess` branch is pinned, this one is not | `abwab-write.controller.spec.ts` | the next change to the 204 handling or the relations routes". Alternatively add the two-line spec case and delete the README's pointer.

---

### F-47 — abwab-templates-page implements no focus return at all, so every overlay it opens from the row context menu drops focus to <body> on close — the doors page solved exactly this and the workshop did not (MEDIUM, Abwab-owned)

**Citation.** `src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.ts:1`

**What the code does.** grep for focus|activeElement|ElementRef over abwab-templates-page.component.ts returns nothing (exit 1). ctxEdit() (:337-343), ctxAddChild() (:344-352), requestNodeDelete() (:247-249) and requestTemplateDelete() (:282-283) each call closeContextMenu() (:243-245), destroying the menuitem button that cdkTrapFocusAutoCapture recorded as its restore target, then open the node modal or a qd-confirm-dialog. On close CDK calls focus() on a detached element and focus lands on <body>. AbwabPageComponent solves the same shape three ways — focusTreeRovingItem() with a header fallback (abwab-page.component.ts:356-363), modalRestoreControl.focusRestore() after a URL-backed close (:558), headerFallbackFocus on discard (:546).

**What it should do, and on whose authority.** Same-area precedent: abwab-page.component.ts:356-363 exists precisely because "the ctx menu that opened the dialog is gone in both outcomes, so auto-restore has no target" (abwab-page.component.spec.ts:300-303). The two pages compose the same qd-context-menu, the same qd-confirm-dialog and the same modal shell; one handles the detached-trigger case and one ignores it.

**Smallest correction — to the code.** Give the templates page the doors page's fallback: a viewChild on a stable header/editor control, and a queued focus() on it whenever an overlay closes after having been opened from the context menu.

---

### F-48 — A successful door restore drops focus to <body>: the archive row that invoked the modal is removed by the refresh, and neither the page nor the overlays controller restores focus (MEDIUM, Abwab-owned)

**Citation.** `src/app/features/abwab/pages/abwab-page/abwab-page.component.html:227`

**What the code does.** <qd-abwab-door-restore-modal (closed)="overlays.closeRestoreModal()" (restored)="overlays.closeRestoreModal()" /> (abwab-page.component.html:227-228). closeRestoreModal() (abwab-page-overlays.controller.ts:286) only clears the id. The invoking element is the archive row's restore button (abwab-archive-view.component.html:29-37); on success the door leaves archivedRoots, the row is destroyed, and qd-confirm-dialog's cdkTrapFocusAutoCapture restore target is detached. The page's three focus-return sites (abwab-page.component.ts:356-363, :546, :558) cover the URL-backed modals and the archive confirms; none covers the restore modal.

**What it should do, and on whose authority.** The page already treats "the trigger vanished, so place focus deliberately" as its own responsibility — abwab-page.component.spec.ts:305-320 asserts exactly that for the archive confirm ("success moves focus to the roving item once the archived row disappears", expect(document.activeElement).not.toBe(document.body)). The restore path is the mirror case and has no equivalent handler and no equivalent spec.

**Smallest correction — to the code.** On (restored), queue focus onto the archive view's roving row (or headerFallbackFocus when the archive empties), reusing the existing focusQueued/focusTreeRovingItem shape; add the matching spec assertion.

---

### F-49 — Escape becomes a dead key once a dirty-discard strip is open in the door, sections and template-node modals — it neither dismisses the strip nor closes the modal (MEDIUM, Abwab-owned)

**Citation.** `src/app/features/abwab/components/abwab-door-modal/abwab-door-modal.component.ts:109`

**What the code does.** All three modals bind (keydown.escape)="requestClose()" on the dialog element (abwab-door-modal.component.html:14; abwab-sections-modal.component.html:14; abwab-template-node-modal.component.html:14). requestClose() is `if (dirty) { confirmingDiscard.set(true); return; } closed.emit()` (abwab-door-modal.component.ts:109-115; abwab-sections-modal.component.ts:277-283; abwab-template-node-modal.component.ts:72-78). Once the strip is up the form is still dirty, so Escape re-sets an already-true signal and returns. The strip is a role="alertdialog" (abwab-door-modal.component.html:57) and is a DOM descendant of the element carrying the Escape binding, so no other handler sees the key either. cancelDiscard() is reachable only by clicking its button.

**What it should do, and on whose authority.** Every other interrupting surface in this feature answers Escape: qd-confirm-dialog cancels (confirm-dialog.component.html:14), the context menu dismisses (context-menu.component.ts:58-61), the sections modal's order editor cancels and stops propagation so it does not close the modal (abwab-sections-modal.component.ts:224-231 — README:248-251 calls that guard mandatory). A role="alertdialog" that ignores Escape is the one interrupting surface in the set that does not.

**Smallest correction — to the code.** In all three, make Escape route to cancelDiscard() when confirmingDiscard() is true and to requestClose() otherwise — one branch, three files, keeping the three shells identical.

---

### F-50 — The archive view's expand chevron has no accessible name and, on leaf rows, is an empty focusable button — three sibling components name and guard the same control (MEDIUM, Abwab-owned)

**Citation.** `src/app/features/abwab/components/abwab-archive-view/abwab-archive-view.component.html:15`

**What the code does.** The archive chevron binds no aria-label, no [attr.aria-hidden] and no leaf tabindex guard; its only content is <span aria-hidden="true">⌄/‹</span> rendered under @if (row.hasChildren) (:22-24). On a leaf the button renders empty, keeps its default tabindex 0, and is a tab stop with no accessible name and nothing to activate. abwab-door-picker.component.html:26-30, abwab-move-picker.component.html:82-86 and abwab-template-tree.component.html:17-21 all bind the same three attributes ([attr.tabindex] leaf guard, [attr.aria-hidden] leaf guard, [attr.aria-label]="expandAriaLabel(row)"). The doors tree chevron (abwab-tree.component.html:46-57) is also unnamed, but is at least tabindex="-1".

**What it should do, and on whose authority.** UI_STYLE_SYSTEM.md:367-374 requires visible focus states and forbids meaning carried without a text equivalent; an icon-only control needs an accessible name. Three of the five chevron implementations in this feature already carry exactly the right bindings, so the correct shape is settled precedent, not a judgement call.

**Smallest correction — to the code.** Copy the three attribute bindings from abwab-door-picker.component.html:26-30 onto abwab-archive-view.component.html:15-21 and add the aria-label to abwab-tree.component.html:46-51.

---

### F-51 — Every write failure is announced twice — once by qd-state's role="alert" and once by the polite abwab-announcer — because both are fed the same outcome.message (MEDIUM, Abwab-owned)

**Citation.** `src/app/features/abwab/state/abwab-write.controller.ts:204`

**What the code does.** handleFailure sets announcementState to outcome.message (:204); handleSuccess sets it on an isSuccess:false envelope (:193); handleBulkFailure sets it on conflict/transport/vanished (:215,:219,:228). That signal renders in qd-abwab-announcer, a role="status" aria-live="polite" region (abwab-page.component.html:51; abwab-announcer.component.html:1-7). The SAME string is simultaneously handed to the component surface: AbwabPageOverlaysController.archiveError.set(outcome.message) (:125,:142) → <qd-state variant="error"> in both confirm dialogs (abwab-page.component.html:270,:287), and AbwabDoorModalComponent errorMessage.set(outcome.message) (:175) → <qd-state variant="error"> (abwab-door-fields-form.component.html:1-2). qd-state's error variant is role="alert" (state.component.html:15). A 409 on archive therefore fires an assertive alert and a polite status carrying identical text.

**What it should do, and on whose authority.** UI_STYLE_SYSTEM.md:992-993 states the repo's own principle for exactly this: the count sits "outside both polite live regions — the title live region already re-announces on load, so inlining the count into either would double-announce it." abwab/README.md:88-92 applies the same care to the toolbar's settled match count, deliberately keeping it off the announcer channel. The write-failure pair was not given the same treatment.

**Smallest correction — to the code.** Pick one channel per failure: either stop setting announcementState when the outcome is also surfaced inline (the modal/confirm owns it), or drop the inline qd-state to a non-live presentation where the announcer already speaks.

---

### F-52 — Successful writes announce nothing for doors but everything for templates — one announcer region, two opposite policies (MEDIUM, Abwab-owned)

**Citation.** `src/app/features/abwab/state/abwab-write.controller.ts:187`

**What the code does.** handleSuccess: `if (onData && data !== null) { onData(data) } else { this.announcementState.set(null) }` (:184-188). Only restoreDoor passes an onData (:130-132, setting ABWAB_LABELS.restoreAnnouncement 'استُرجع الباب', labels.ts:171). Every other door/section/relation write — create, edit, move, reorder, archive, bulk move, bulk archive, restore-excepted, the four section commands, relations add and delete — actively CLEARS the announcement on success, so a screen-reader user gets silence. AbwabTemplatesController does the opposite: it announces every success (templates.controller.ts:28,:33,:63,:85 → templateCreatedAnnouncement 'أُنشئ القالب', templateDeletedAnnouncement 'حُذف القالب', templateAppliedAnnouncement, labels.ts:323-325), into the identical qd-abwab-announcer (abwab-templates-page.component.html:11).

**What it should do, and on whose authority.** abwab/README.md:326-327 describes qd-abwab-announcer as "one aria-live=\"polite\" role=\"status\" region for operation messages" — one region, one contract. Two controllers feeding one region with opposite success policies means the same class of action (a write completing) is confirmed on /abwab/templates and silent on /abwab. Whichever policy is right, it should be one policy.

**Smallest correction — to the code.** Decide the policy once and apply it in both controllers — most cheaply by giving AbwabWriteController's dispatch/handleSuccess an optional success message per command, as AbwabTemplatesController.dispatch already takes one (templates.controller.ts:85).

---

### F-53 — The cards view has no empty state and no no-results state at all — a zero-match search or an empty section renders a bare breadcrumb over a blank grid (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html:126`

**What the code does.** The tree branch guards `@if (visibleRoots().length === 0)` and renders `<qd-state variant="empty">` (`:144-145`); the archive branch does the same (`:116-117`). The cards branch renders `<qd-abwab-cards [roots]="displayRoots()">` unconditionally with no guard, and `abwab-cards.component.html:28-57` is a bare `@for` over `level()` with no `@empty` and no fallback. When `displayRoots()` is empty (no live doors in the section, or a query that matches nothing) the user sees the «كل الأبواب» breadcrumb and nothing else.

**What it should do, and on whose authority.** Loading / empty / error / no-results must be genuinely distinct, visible states on every surface. Authority: the same-area precedent two branches up in the same template (`abwab-page.component.html:116-117` and `:144-145`) and `README.md:785-787` («Loading/empty/error surfaces are composed, not hand-rolled. Every text-only loading, empty, and error site across abwab-page … now composes qd-skeleton-rows/qd-panel-skeleton (loading) or qd-state (empty/error)») — the cards branch is a site that README claims is covered and is not.

**Smallest correction — to the code.** Mirror the tree branch inside the cards branch: wrap `<qd-abwab-cards>` in `@if (displayRoots().length === 0) { <qd-state variant="empty" …/> } @else { … }`, using the existing `emptyLabel` for the no-doors case and a distinct no-results message when `searchQueryParam() !== ''`.

---

### F-54 — A zero-match search in the archive view collapses the archive into «لا توجد أبواب مؤرشفة.» — the exact "lie about the data" ux-slice-l removed from the tree, never applied to the other two filtering surfaces (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.html:116`

**What the code does.** `@if (displayArchivedRoots().length === 0) { <qd-state variant="empty" [message]="archiveEmptyLabel" …/> }`. `displayArchivedRoots()` is `pruneAbwabNodesToVisible(archivedRoots(), result.visibleIds)` when a query is present (`abwab-page.component.ts:192-197`), and `archiveEmptyLabel` is `ABWAB_LABELS.archiveEmptyMessage` = «لا توجد أبواب مؤرشفة.» (`abwab.labels.ts:168`). So typing a query that matches no archived door tells the user there are no archived doors.

**What it should do, and on whose authority.** README.md:79-83 states the rule for exactly this case: «a zero-match query leaves the full tree with a zero count rather than collapsing into «لا توجد أبواب بعد», which was a lie about the data». The tree half genuinely holds (the tree is fed `visibleRoots()`, not `displayRoots()`, at `abwab-page.component.html:144,148`). The archive is a filtering surface by the same README's own account (`:83-85`, «In cards and the archive the same query still filters») and inherited the defect the tree was fixed for.

**Smallest correction — to the code.** Split the archive branch's zero condition: when `searchQueryParam() !== ''` and `archivedRoots().length > 0`, render a no-match message instead of `archiveEmptyLabel`; keep `archiveEmptyLabel` for a genuinely empty archive.

---

### F-55 — Cards search filtering is applied to the root level but not below it: a matching root whose descendants do not match renders as an unreachable leaf, and drilled levels ignore the query entirely (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-cards/abwab-cards.component.ts:58`

**What the code does.** `level()` returns `path[path.length-1].children` when drilled, else `this.roots()`. `roots` is fed the pruned `displayRoots()` (`abwab-page.component.html:129`) but `path()` walks the **unpruned** `byId()` map (`abwab-cards.component.ts:41,49`, fed `facade.snapshot()?.byId` at `abwab-page.component.ts:119`). Two consequences: (a) at the root level, `pruneAbwabNodesToVisible` gives a matching-but-childless-match root `children: []`, so `--leaf` applies (`abwab-cards.component.html:32`), the meta count renders `''` (`:53`), `cursor: default` (`abwab-cards.component.scss:70-72`) and `onCardClick` never emits `drilled` (`abwab-cards.component.ts:74-76`) — that root's real children become unreachable in cards view with no indication; (b) once drilled, `level()` comes from the unpruned node, so the filter silently stops applying.

**What it should do, and on whose authority.** README.md:83-85 states cards is a filtering surface («In cards and the archive the same query still filters (pruneAbwabNodesToVisible)»), which implies one consistent filtered view, not a filter that inverts a branch into a leaf and evaporates one level down.

**Smallest correction — to the code.** Resolve the drill path against the same pruned tree the grid renders (walk `roots()` for the path instead of `byId()`), or stop pruning for cards and mark matches the way the tree does; either way one source of truth for a card's children.

---

### F-56 — Cards are non-focusable `<div>`s with a click handler and a dead `:focus-visible` rule — the whole cards view is unreachable by keyboard (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-cards/abwab-cards.component.html:30`

**What the code does.** `<div class="abwab-cards__card" … (click)="onCardClick(node)">` carries no `tabindex`, no `role`, and no `keydown` handler; select and drill are mouse-only. `abwab-cards.component.scss:65-68` defines `.abwab-cards__card:focus-visible { outline: 2px solid var(--qd-focus-ring); }`, a rule nothing can ever match — evidence the affordance was meant to be focusable.

**What it should do, and on whose authority.** The sibling surface for the same data is fully keyboard-operable: `abwab-tree.component.html:19-34` gives every row `role="treeitem"`, a roving `tabindex`, and a keydown model. `view` is a first-class URL key (`README.md:388`), so cards is a peer view, not a decoration. UI_STYLE_SYSTEM.md:374 and the README's «every affordance that looks pressable is» (`README.md:854-855`) both bind here.

**Smallest correction — to the code.** Make the card a real `<button type="button">` (the breadcrumbs at `abwab-cards.component.html:15-24` already are), or add `tabindex="0"`, a role, and Enter/Space handling; the focus style already exists.

---

### F-57 — The template tree's inline order editor still COMMITS on blur — the exact behavior the doors tree and the sections modal both reversed to cancel-on-blur (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-tree/abwab-template-tree.component.html:39`

**What the code does.** `(blur)="commitOrderEdit(row.node.id, $event.target)"` — clicking away from a half-typed number dispatches `orderCommitted`, which the page turns into a real `reorderNode` write (`abwab-templates-page.component.ts:234-236`). The doors tree binds the opposite: `(blur)="cancelOrderEdit(node.id)"` (`abwab-tree.component.html:68`), and Escape there cancels via the same path (`abwab-tree.component.ts:221-231`).

**What it should do, and on whose authority.** README.md:116-118 states the rule and its reason: «Enter is the only commit — blur and Escape both cancel. Blur used to commit; that made clicking away from a half-typed number resequence a scope the user never confirmed, and it is the one grammar in this feature where an unconfirmed value could be written.» README.md:245-247 applies the same grammar to the sections modal's order editor. The workshop's node reorder is the third instance of the identical editor and is the only one still on the abandoned semantics.

**Smallest correction — to the code.** Change the blur binding to a cancel that clears `editingOrderId` without emitting, mirroring `abwab-tree.component.ts:226-231`; leave Enter and Escape as they are.

**Also reported independently at.** `src/app/features/abwab/components/abwab-template-tree/abwab-template-tree.component.html:39` — merged from 2 separate agent findings covering the same defect.

---

### F-58 — The inline order chip is a click-only `<span>` in both trees, so no keyboard path to reorder exists anywhere in the feature — and the README asserts the opposite invariant (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.html:71`

**What the code does.** `<span class="abwab-tree__order qd-chip qd-chip--pill" … (click)="onOrderClick($event, node.id)">` — a pressable affordance that is not a button, has no `tabindex` and no key handler. Same shape in the workshop at `abwab-template-tree.component.html:42-47`. The tree's keyboard model (`abwab-tree-keyboard.controller.ts:89-113`) maps ArrowUp/Down/Left/Right/Home/End/Enter/Space/ContextMenu/F10 and has no key that opens the order editor.

**What it should do, and on whose authority.** README.md:174-175 states the side panel has «No reorder button — the tree's own inline number editor is the one reorder affordance», and README.md:848-856 states «Zero dead controls, and now no exception … every affordance that looks pressable is, and the only things this feature renders as inert are pure data: the row's count badges.» Both cannot be true: the one reorder affordance is unreachable by keyboard, and the order chip is a second inert-but-pressable element. README.md:248-250 already recognises the problem, calling the tree's element «the tree's dead `<span>`» while requiring a real `<button>` in the sections modal.

**Smallest correction — to the code.** Promote both order chips to `<button type="button">` with `[attr.tabindex]="-1"` in the doors tree (preserving the roving-tabindex invariant the row actions already use, `abwab-tree.component.html:119,133,143`) plus a key in the keyboard controller that opens the editor for the focused row.

**Also reported independently at.** `src/app/features/abwab/components/abwab-tree/abwab-tree.component.html:71` — merged from 2 separate agent findings covering the same defect.

---

### F-59 — The toolbar's tree/cards view toggle exposes no selected state to assistive tech and reuses the section tabs' aria-label as its group name (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-toolbar/abwab-toolbar.component.html:61`

**What the code does.** `<div class="abwab-toolbar__view-toggle" role="group" [attr.aria-label]="sectionTabsAriaLabel">` — the group is named «أقسام الأبواب» («door sections»), which is the section tab strip's name (`abwab.labels.ts:77`, also used at `:3`). The two buttons (`:62-79`) carry only `[class.abwab-toolbar__view-btn--active]`; no `aria-pressed`, `aria-current`, or `aria-selected`. The active state is styled at `abwab-toolbar.component.scss:70-74` (bg + accent text + `font-weight: 700`) and is announced nowhere.

**What it should do, and on whose authority.** The shared primitive one element above already does this correctly: `shared/ui/tabs/tab.directive.ts:6-13` host-binds `role: 'tab'` and `[attr.aria-selected]`, and the section strip composes it (`abwab-toolbar.component.html:3-38`). UI_STYLE_SYSTEM.md:374 forbids conveying meaning by colour alone; here the state is not conveyed to AT at all. The pattern existed in-repo and was not used.

**Smallest correction — to the code.** Add `[attr.aria-pressed]="view() === 'tree'"` / `'cards'` to the two buttons and give the group its own label constant instead of reusing `sectionTabsAriaLabel`.

---

### F-60 — The side panel's bulk count interpolates a bare number into Arabic copy — «3 باب محدد» — against the feature's own counted-noun rule (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-side-panel/abwab-side-panel.component.html:88`

**What the code does.** `<span …>{{ bulkCount() }}</span>` followed by `{{ bulkCountSuffix }}` (`:89`), where `bulkCountSuffix` is the invariant string `'باب محدد'` (`abwab.labels.ts:119`). The rendered phrase is «2 باب محدد» / «3 باب محدد» / «11 باب محدد» — singular noun for every count. Every other counted door surface in the feature goes through `countPhrase` + `DOOR_FORMS` (`abwab.labels.ts:11-22,32-38`), e.g. `archiveConfirm` (`:183`) and `movePickerTitleBulk` (`:150`).

**What it should do, and on whose authority.** README.md:841-844: «Counted door labels go through the Arabic number forms … Do not interpolate a bare count into new copy — «سيتم أرشفة 1 بابًا» is wrong Arabic and this product is Arabic-first.» The bulk bar is a counted-door label and is the one that escaped the rule. It is not the `qd-result-count` "label: N" data-display exemption README.md:834-836 carves out — this is a sentence, not a stat tile.

**Smallest correction — to the code.** Replace `bulkCountSuffix` with a `bulkSelectedCount(count)` label built on `countPhrase(count, SELECTED_DOOR_FORMS)` and render one interpolation instead of number-plus-suffix.

---

### F-61 — Template apply has no in-flight guard: a second click on «انسخ» re-issues the whole apply and duplicates the template's children under every selected door (MEDIUM, Abwab-owned — downgraded from the agent's HIGH, see below)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-copy-modal/abwab-template-copy-modal.component.ts:108`

**What the code does.** `confirm()` calls `applyTemplate()(targets).subscribe(...)` with no busy signal and returns nothing that disables the button. The button's `[disabled]="!hasElements() || pickedIds().size === 0"` (abwab-template-copy-modal.component.html:59) depends only on inputs that do not change while the request is out, the modal stays open until the response lands, and the request body `{ targetDoorIds: [...targetDoorIds] }` (state/abwab-templates.controller.ts:61) carries NO version/If-Match token, so the server cannot reject the duplicate. Two clicks = the template subtree copied twice under each of the N selected doors.

**What it should do, and on whose authority.** Same-area precedent plus the feature README's own words. The nested `qd-confirm-dialog` already models this: `confirm()`/`cancel()` return early on `busy()` and both buttons are `[disabled]` while busy (shared/ui/confirm-dialog/confirm-dialog.component.ts:41-53, .component.html:30,41), and README.md:804-805 states the relation-delete confirm "stays open, busy, until the write resolves — which is also what closes the double-dispatch hole the bare chip had." The hole is closed on the confirm paths and left open on this one.

**Smallest correction — to the code.** Add a `busy` signal set before `applyTemplate()(...)` and cleared in the subscribe callback; fold it into the confirm button's `[disabled]` and make `confirm()` return early when it is set — the exact shape `ConfirmDialogComponent` already uses.

**Reviewer verification — severity downgraded from HIGH.** The missing guard is real and I
confirmed it: `abwab-template-copy-modal.component.ts:113` subscribes with no pending flag, and
the confirm button's only `[disabled]` binding is
`!hasElements() || pickedIds().size === 0` (`abwab-template-copy-modal.component.html:59`) —
nothing about an in-flight request. A second click does re-issue the apply.

But the claimed consequence — "duplicates the template's children under every selected door" —
does not survive checking the backend, and two mechanisms bound it:

1. **The apply is transactional.** `EfAbwabTemplateApplyWriter.cs:18` opens a transaction and
   `:130` commits it, so a partial subtree cannot be left behind.
2. **A unique index blocks the duplicate.** `AbwabDoorConfiguration.cs:86-89` declares
   `HasIndex(d => new { d.SectionId, d.ParentId, d.Name }).IsUnique().AreNullsDistinct(false)
   .HasFilter("deleted_at IS NULL")`. Two applies of the same template to the same target
   collide on it, and the loser is rejected (`23505` → duplicate-name) rather than committed.

So the realistic outcome of the double-click is a confusing failure message on the second
request, not duplicated structure and not partial state. Reachable, bounded, no data corruption →
**MEDIUM**. The correction is unchanged and still worth making; only the stated consequence and
the priority change. I am recording the downgrade explicitly because the finding is correct about
the missing guard and wrong only about what the guard prevents.

---

### F-62 — The relations modal keeps its focus trap unconditionally live while its nested delete-confirm dialog is open — the exact two-live-traps case the README declares forbidden and the sections modal explicitly avoids (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-relations-modal/abwab-relations-modal.component.html:10`

**What the code does.** The dialog `<section>` carries a bare `cdkTrapFocus` (always enabled). When `pendingDelete()` is set, a `qd-confirm-dialog` — itself `cdkTrapFocus cdkTrapFocusAutoCapture` — renders as a SIBLING of the relations backdrop (abwab-relations-modal.component.html:173-191). Both traps are live at once: the outer trap's tab anchors still bracket the relations `<section>`, so focus arriving there is pulled back into the modal underneath the confirm, and focus that leaves the confirm is not returned to it.

**What it should do, and on whose authority.** `features/abwab/README.md:603-608` is explicit: "The one permitted nesting is a confirmation dialog above exactly one authoring modal, and the host yields while it is open: the sections modal binds `[cdkTrapFocus]="deleteConfirmId() === null"` so its delete confirm's own trap is the only live one … Two live traps fight over focus." The same-area precedent implements it (abwab-sections-modal.component.html:10) and is pinned by a test asserting `trapOf(fixture).enabled === false` while the confirm is open (abwab-sections-modal.component.spec.ts:440-453). Here the README is RIGHT and the code is WRONG — this must not be documented away.

**Smallest correction — to the code.** Change `cdkTrapFocus` to `[cdkTrapFocus]="pendingDelete() === null"` on abwab-relations-modal.component.html:10, mirroring the sections modal, and add the sections modal's yield/take-back assertion to the relations spec.

---

### F-63 — Create-door and create-section submit with no in-flight guard and no version token, so a double click creates two doors / two sections (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-modal/abwab-door-modal.component.ts:156`

**What the code does.** `submit()` calls `this.writeController.createDoor({...})` with a body carrying no `version` (abwab-door-modal.component.ts:156-165); the save button has no `[disabled]` and no busy binding (abwab-door-modal.component.html:80-82). Identically, `abwab-sections-modal.component.ts:149` calls `this.createSection()(name)` with the add button unguarded (abwab-sections-modal.component.html:98). The contrast that proves this is the axis: `updateDoor` sends `version: door.version` (abwab-door-modal.component.ts:144) and `saveRename` sends `current.version` (abwab-sections-modal.component.ts:175), so a second submit on those paths 409s as a stale version and surfaces an error instead of duplicating.

**What it should do, and on whose authority.** Same-area precedent: `ConfirmDialogComponent` guards every write it fronts with `busy` (shared/ui/confirm-dialog/confirm-dialog.component.ts:41-53), which README.md:804-805 names as what "closes the double-dispatch hole". The version-less create paths are the ones that actually corrupt and are the ones left unguarded.

**Smallest correction — to the code.** Add a `busy` signal in each modal, set it before the `createDoor`/`createSection` call and clear it in `handleOutcome`/the subscribe callback, bind it to the save/add button's `[disabled]`, and return early from `submit()`/`add()` while set.

---

### F-64 — The move picker's chosen destination row is conveyed by colour/weight only — the pick button carries no `aria-pressed`/`aria-current`, so a screen-reader user cannot tell which parent the move will use (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-move-picker/abwab-move-picker.component.html:93`

**What the code does.** `<button class="abwab-move-picker__pick" (click)="pickParent(row.node.id)">` has no state attribute; selection is expressed solely by `[class.abwab-move-picker__row--picked]` on the wrapper (abwab-move-picker.component.html:75) which resolves to `background` + `color` + `font-weight: 700` (abwab-move-picker.component.scss:110-115). The «كباب رئيسي» row is the same shape (abwab-move-picker.component.html:61-70). Confirm then emits `{ targetParentId: this.pickedParentId(), targetSectionId }` (abwab-move-picker.component.ts:171) — a destructive structural move whose target the user cannot verify non-visually.

**What it should do, and on whose authority.** Same-modal precedent, one element up: the section strip is `qd-tabs` at `layout="grid"` and its cells announce state through the primitive (`'[attr.aria-selected]': 'selected()'`, shared/ui/tabs/tab.directive.ts:10). README.md:196-198 pins the visible half of this rule for the strip — "The active cell is marked by the primitive's tint/accent border plus bold, never colour alone" — and the destination list is the same decision made in the same modal with the accessible half missing. `abwab-relations-modal.component.html:109,118` also sets `[attr.aria-pressed]` on its direction pill.

**Smallest correction — to the code.** Add `[attr.aria-pressed]="pickedParentId() === row.node.id"` to the destination pick button and `[attr.aria-pressed]="pickedParentId() === null"` to the «كباب رئيسي» button.

---

### F-65 — `abwab-template-node-modal` has no spec file at all, so its dirty-close confirm and its submit/validation path are untested — the same shape the door modal covers in a 15 KB spec (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-node-modal/abwab-template-node-modal.component.ts:89`

**What the code does.** The component owns real branching behaviour with no test: `submit()` trims and rejects an empty name into `errorMessage` (lines 89-97), then calls the injected `submitNode()` function input and closes only on success (99-106); `requestClose()` raises the discard-confirm strip when the child form is dirty (72-78). The directory listing for `components/abwab-template-node-modal/` contains only `.ts`, `.html` and `.scss` — no `.component.spec.ts` — while every sibling authoring modal has one (`abwab-door-modal.component.spec.ts`, `abwab-sections-modal.component.spec.ts`, `abwab-relations-modal.component.spec.ts`, `abwab-template-copy-modal.component.spec.ts`, `abwab-door-restore-modal.component.spec.ts`).

**What it should do, and on whose authority.** Same-area precedent: `abwab-door-modal.component.spec.ts` covers the identical dirty-close-confirm + name-required + submit-outcome triad for the modal this one was copied from, and README.md:590-594 treats all six authoring modals as one contract that must not diverge per modal.

**Smallest correction — to the code.** Add `abwab-template-node-modal.component.spec.ts` covering: empty-name submit sets the error and does not call `submitNode`; a dirty form's close raises the discard strip rather than emitting `closed`; a failing outcome keeps the modal open with the message.

---

### F-66 — `qd-state`'s reserved error box can render as an empty danger box: the door picker keys its reserve error branch off `status()`, not off a non-empty message, and `errorMessage` defaults to `''` (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-picker/abwab-door-picker.component.html:67`

**What the code does.** `@else if (status() === 'error') { <qd-state variant="error" [reserve]="true" [message]="errorMessage()" [actionLabel]="retryLabel" ... /> }`. `errorMessage = input('')` (abwab-door-picker.component.ts:42) and `status` (`:41`) are independent inputs with no coupling. With `status='error'` and the default empty message, `qd-state` renders `<div class="qd-error-state qd-state--reserve" role="alert">` (state.component.html:15) whose backing class carries `background: var(--qd-danger-tint)` (_components.scss:364), `color: var(--qd-danger)` (:564) and `padding: var(--qd-space-6)` (:547-549), plus the reserved `min-block-size: var(--qd-control-block-size)` message row at `opacity: 0` (state.component.scss:9-14) — a ~105px danger-tinted box with no text under `role="alert"`, announcing nothing, with a retry button under it. This is exactly the failure UI_STYLE_SYSTEM.md:866-869 records Slice C shipping and reverting. NOT reachable today: the only caller that sets `'error'` derives it from a truthy message (`abwab-template-copy-modal.component.ts:62-63` — `this.doorsLoading() ? 'loading' : this.doorsError() ? 'error' : 'empty'`), and `abwab-relations-modal.component.html:135-147` never passes `status` at all, so its default `'ready'` makes the branch dead there.

**What it should do, and on whose authority.** UI_STYLE_SYSTEM.md §17 `qd-state` (:866-869): the unguarded reserve-error rendering "shipped a 105px empty danger box on every open of both modals ... reverted to match the other three", and the seven other `[reserve]` error sites in the feature are all guarded on a non-empty message (abwab-sections-modal.component.html:21, abwab-template-copy-modal.component.html:22, abwab-relations-modal.component.html:29, abwab-door-fields-form.component.html:1, abwab-page.component.html:97, abwab-templates-page.component.html:27 and :76). Same-area precedent is unanimous; the door picker is the one exception.

**Smallest correction — to the code.** Change the guard to `@else if (status() === 'error' && errorMessage() !== '')` in abwab-door-picker.component.html:67, so the branch cannot render without the text that justifies the danger framing.

---

### F-67 — `.qd-tabs__count--empty` dims an ACTIVE, clickable tab's zero count with `opacity: 0.5`, computing to roughly 1.9:1 against the surface it sits on — no measured ratio is recorded anywhere (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/styles/_components.scss:230`

**What the code does.** `.qd-tabs__count--empty { opacity: 0.5; }` (:230-232) composes over `.qd-tabs__count { background: var(--qd-surface-recessed); color: var(--qd-text-muted); font-size: 0.75rem; }` (:211-223). Applied by abwab-toolbar.component.html:15 and :32 whenever a section's root-door count is 0, on a tab that is fully enabled and clickable. Computed (not browser-measured) from the light tokens — `--qd-text-muted: oklch(0.529 …)` (_tokens.scss:21), `--qd-surface-recessed: oklch(0.945 …)` (:8), composited at α=0.5 over `--qd-surface: oklch(0.994 …)` (:6) — the digit-to-badge contrast lands near 1.9:1 at 12px. WCAG 1.4.3's exemption for "inactive user interface components" does not apply: unlike `.qd-chip:disabled` (:266-271) and `.qd-tabs__tab[aria-disabled='true']` (:199-203), which use the identical `opacity: 0.5` idiom on genuinely disabled controls, this tab is live. Partially mitigated: the digits are `aria-hidden="true"` and the count is carried in the tab's own `aria-label` (abwab-toolbar.component.html:8, :16, :25, :34), so screen-reader users still get it — sighted low-vision users do not.

**What it should do, and on whose authority.** UI_STYLE_SYSTEM.md §17 `qd-tabs` (:753-758): the count is "**always** rendered, dimmed at zero via `.qd-tabs__count--empty` (opacity only, so it composes with the selected-state rule instead of forking a second one)". §12 Accessibility and the engineering-review frontend checklist both require sufficient contrast on shared `qd-` classes, and styles/README.md:103-121 makes measured contrast ratios a recorded invariant for every deliberately-low-contrast pairing in the token file — this pairing has no row in that table.

**Smallest correction — to the code.** Either replace the opacity dim with an explicit token colour step whose ratio can be measured and recorded, or measure the composited ratio in-browser and add a row for it to the styles/README.md contrast table so it stops being an unrecorded assumption. Do not widen the opacity idiom to more live controls.

---

### F-68 — The templates tree's `⋯` button is keyboard-focusable and feeds `event.clientX/clientY` straight into `qd-context-menu`'s `position`, so a keyboard activation opens the menu clamped to the viewport's top-left corner instead of at the row (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-tree/abwab-template-tree.component.ts:88`

**What the code does.** `protected onMoreClick(event: MouseEvent, nodeId: number): void { this.menuRequested.emit({ nodeId, x: event.clientX, y: event.clientY }); }`, wired at abwab-template-tree.component.html:67 on a `<button type="button">` that carries no `tabindex`, so it is in the tab order. A click synthesised by Enter/Space reports `clientX === clientY === 0`, which `qd-context-menu`'s `place()` then clamps to `[VIEWPORT_MARGIN, …]` (context-menu.component.ts:79-80, `VIEWPORT_MARGIN = 8`) — the menu lands at (8, 8), the top-left corner of the viewport, with no relationship to the focused row. The doors tree does NOT have this hole: its `⋯` carries `[attr.tabindex]="-1"` (abwab-tree.component.html:143) and its keyboard path goes through `onKeydown`'s `openMenu` intent, which measures the row rect and anchors on its inline-start edge (abwab-tree.component.ts:283-286). The templates tree has the equivalent `ContextMenu`/Shift+F10 path too (abwab-template-tree.component.ts:97-105) — it just left the button path unguarded beside it.

**What it should do, and on whose authority.** UI_STYLE_SYSTEM.md §17 `qd-context-menu` placement contract (:1237): "Both trees' keyboard paths anchor at the focused row's inline-start edge to match." That holds for the `ContextMenu`-key path in both trees but not for the templates tree's focusable `⋯`. Same-area precedent (abwab-tree.component.html:143) is the fix already written.

**Smallest correction — to the code.** Add `[attr.tabindex]="-1"` to the templates tree's `⋯` button (abwab-template-tree.component.html:63-70), matching abwab-tree.component.html:143, so the keyboard path is the row-anchored one only.

---

### F-69 — `qd-context-menu` has no unit spec at all; its RTL placement, viewport flip and clamp math is covered only by opt-in E2E, which is not a required test tier (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/shared/ui/context-menu/context-menu.component.ts:63`

**What the code does.** `place()` (:63-82) does the direction resolution, the inline preferred-side choice, the inline flip, the block flip and the two-axis clamp — the whole correctness surface of the component — and `shared/ui/context-menu/` contains only `.ts`, `.html` and `.scss`: there is no `context-menu.component.spec.ts` (every sibling primitive has one: chip, confirm-dialog, detail-modal-shell, state, tabs, ayah-card, pagination, skeleton). The only coverage is `e2e/abwab-operations.e2e.ts:148-196` (inline-start extension, the 900px inline flip, the 420px block flip).

**What it should do, and on whose authority.** Root CLAUDE.md, Test selection: "a browser E2E layer exists (`Frontend/quran-dashboard-ui/e2e/`, `npm run e2e`), but it is **opt-in and not a required tier** — do not present an E2E run as a Tier C or release gate". So the flip/clamp logic of an app-wide shared primitive currently has zero required-tier coverage. UI_STYLE_SYSTEM.md:1232-1236 acknowledges jsdom cannot measure boxes, but that argues only against testing the *measurement*, not against testing the arithmetic.

**Smallest correction — to the code.** Extract the body of `place()` into a pure helper module (e.g. `context-menu-placement.ts`) taking `(anchor, size, viewport, direction)` and returning `{left, top}` — the same shape as `shared/ui/skeleton/grid-template-columns.ts` and `shared/ui/pagination/pagination-window.ts`, both of which have their own specs — then unit-test the four branches. No jsdom measurement needed.

---

### F-70 — `qd-context-menu` exposes `role="menu"` with `role="menuitem"` children but manages no focus and carries no accessible name; the projected items sit at the very end of the page DOM, so the keyboard open path the series added leads to a menu the user must Tab through the whole page to reach (MEDIUM, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/shared/ui/context-menu/context-menu.component.html:6`

**What the code does.** The menu box declares `role="menu"` (:6) with no `aria-label`/`aria-labelledby`, and the component never moves focus into it — the only focus-adjacent behaviour is the document-level Escape dismissal (context-menu.component.ts:58-61). Both consumers render the menu as the last block of their page template (abwab-page.component.html:291-306; abwab-templates-page.component.html:206-239) with correct `role="menuitem"` buttons inside. Because Slice L added keyboard opening (abwab-tree.component.ts:283-286; abwab-template-tree.component.ts:97-105), a keyboard user can now reach a state where a `role="menu"` is open, focus is still on the tree row, and the items are reachable only by tabbing past every remaining focusable element on the page. ARIA's menu pattern assumes focus is inside the menu and arrow keys traverse it; neither holds here.

**What it should do, and on whose authority.** UI_STYLE_SYSTEM.md §17 `qd-context-menu` gap 2 (:1243-1246) names this deliberately and defers it: "Neither prior copy moved focus into it on open; adding that changes keyboard behavior on a shipped surface and belongs to Slice G's row-menu keyboard-path work, not this extraction." The deferral was written when the menu was mouse-only; the keyboard open path shipped afterwards, which is what turns a documented gap into a reachable one. §12 and the engineering-review frontend checklist require correct roles and keyboard reachability.

**Smallest correction — to the code.** Focus the first `[role="menuitem"]` when the menu mounts (an `afterRenderEffect` beside the existing placement one), and add a required `menuAriaLabel` input rendered as `aria-label` on the `role="menu"` box. If the focus move is genuinely out of scope for now, the honest smaller step is to drop `role="menu"`/`role="menuitem"` down to a plain labelled group of buttons so the markup stops promising a keyboard contract it does not implement — and update §17 to say so.

**Also reported independently at.** `src/app/shared/ui/context-menu/context-menu.component.ts:29` — merged from 2 separate agent findings covering the same defect.

---

### F-71 — `handleSuccess` casts a possibly-null payload to `T`, so `AbwabWriteOutcome<AbwabDoorDto>` can carry `data: null` while its type says otherwise (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-write.controller.ts:182`

**What the code does.** `const data = (response?.data ?? null) as T;` (`:182`) then `return { kind: 'success', data };` (`:190`). The `null` branch exists to serve the documented `204 No Content` case (`README.md:947-960`), but the same cast applies to the typed writes: `createDoor`/`updateDoor`/`moveDoor`/`reorderDoor` all declare `AbwabWriteOutcome<AbwabDoorDto>` (`:108-122`). A `200` whose envelope has `isSuccess: true, data: null` yields `outcome.data === null` typed as `AbwabDoorDto`. `AbwabDoorModalComponent.handleOutcome` does `this.saved.emit(outcome.data)` into `output<AbwabDoorDto>()` (`abwab-door-modal.component.ts:33,171`). Today this is latent: the only page handler is `(saved)="onDoorModalSaved()"` (`abwab-page.component.html:208`), which takes no argument. The templates side guards correctly — `if (outcome.data) { this.selectTemplate(outcome.data.id); }` (`abwab-templates-page.component.ts:179-180`) — and `AbwabTemplatesController` types its outcomes as `T | null` honestly (`abwab-templates.controller.ts:71,82`).

**What it should do, and on whose authority.** `CODING_PRINCIPLES.md` §6 Strong Typing: explicit types, no unjustified casts. Same-area precedent is the templates controller, which models the same 204 reality as `AbwabWriteOutcome<T | null>` instead of lying with a cast. The doors controller should do the same rather than hand consumers a type that permits a crash the compiler will not catch.

**Smallest correction — to the code.** Change `AbwabWriteController.handleSuccess` to return `AbwabWriteOutcome<T | null>` and let the payload-less methods (`archiveDoor`, `deleteSection`, `deleteRelation`) declare `unknown` as they already do, so the typed writes surface the nullability to callers — mirroring `abwab-templates.controller.ts:71`. Remove the `as T`.

---

### F-72 — `abwab-page-overlays.controller.ts` is 416 lines, over the 400-line soft threshold for state services, and the README never acknowledges the threshold (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-page-overlays.controller.ts:416`

**What the code does.** 416 lines owning seven distinct overlay concerns: the door create/child/edit modal (`:47-83`), single/bulk archive confirm (`:85-176`), the move picker (`:178-251`), the restore modal (`:253-288`), the sections modal (`:290-305`), the relations modal (`:307-362`), and the row context menu (`:364-415`). `FRONTEND_STRUCTURE.md:136-140` sets facade/store/state services at ideal 200-350, soft 400, hard 600, and `:145-146` warns to «Avoid oversized stores that own unrelated modals, filters, data loading, selection, drag/drop, and persistence all in one file.» The README documents what the file owns (`README.md:63-70`) and even records the page component's own threshold status explicitly (`README.md:45`, `README.md:290`), but says nothing about this file's size.

**What it should do, and on whose authority.** `FRONTEND_STRUCTURE.md:165-169`: a file expected to exceed its soft threshold must have the size «mention[ed]» and justified. Two sibling files in this feature carry exactly that acknowledgement in the README, so the omission here is inconsistent with the feature's own practice, not with an abstract rule. It is under the hard threshold (600) and the concerns are cohesive (all page overlays), so this is a documentation gap rather than a split mandate.

**Smallest correction — to the documentation.** Add one sentence to `README.md:63-70` stating the file sits just over the 400-line soft threshold and why the seven overlays are one cohesive unit — matching the wording already used for `abwab-page.component.ts` at `README.md:45` and the templates page at `README.md:290`. Record the next split seam (the relations block is the natural one) the same way `README.md:49-53` does.

---

### F-73 — The `state/` layer imports a type from a `components/` file, inverting the feature's layering (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-page-overlays.controller.ts:11`

**What the code does.** `import { AbwabMoveDestination } from '../components/abwab-move-picker/abwab-move-picker.component';` — the only state→components import in the feature. It is used in `confirmMove(destination: AbwabMoveDestination)` (`:233`). Every other type the state layer consumes comes from `../models/` or `core/api/generated/models/`.

**What it should do, and on whose authority.** `FRONTEND_STRUCTURE.md` Frontend Review Checklist §1 assigns `models/` ownership of «feature DTOs/view models/types» and `state/` ownership of «facade/store/state services»; a state service reaching into a component file for a contract type makes the component the owner of a state contract. Same-area precedent: `AbwabMoveDestination`'s peers (`AbwabRelationVm`, `AbwabNode`, `AbwabModalState`) all live in `models/abwab.models.ts`.

**Smallest correction — to the code.** Move the `AbwabMoveDestination` interface into `models/abwab.models.ts` and have both the picker component and the overlays controller import it from there. Pure move, no behavior change.

---

### F-74 — Two adjacent counts on the doors page are both labelled «كل الأبواب» while counting different scopes; only the tab badge's aria-label names its scope (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-tree.builder.ts:167`

**What the code does.** `countLiveAbwabDoors` counts every non-archived node in `byId` — live doors at ALL depths (`abwab-tree.builder.ts:167-175`), surfaced as `totalLiveDoorsCount` (`abwab-page.component.ts:128`) and rendered with `labelPrefix = ABWAB_LABELS.allDoorsTab` = «كل الأبواب» (`abwab-page.component.ts:227`, `abwab.labels.ts:76`, `abwab-page.component.html:55-61`). Separately `rootCountBySectionId`/`totalRootCount` count ROOT doors only (`abwab-tree.builder.ts:81-84`; `abwab-page.component.ts:124` = `liveRoots.length`), rendered as the badge inside the tab also labelled «كل الأبواب» (`abwab-toolbar.component.html:12-18`). Sighted users therefore see «كل الأبواب 3» in the tab strip and «كل الأبواب: 12» in the stat bar. The aria-label does carry the scope — `allDoorsTabRootCountAriaLabel: (count) => \`كل الأبواب: ${countPhrase(count, ROOT_DOOR_FORMS)}\`` (`abwab.labels.ts:80`) — so the discrepancy is visual only. The state-layer names (`countLiveAbwabDoors`, `rootCountBySectionId`, `totalRootCount`) are all correctly scoped.

**What it should do, and on whose authority.** Repo counting-scope law: a badge whose visible label does not describe the same scope as the query behind it is a finding. `README.md:832-836` explicitly ratifies the coexistence («no test or doc may treat them as the same number reused twice») but stops at forbidding a test — it does not require the visible labels to differ, which is the part a user experiences.

**Smallest correction — to the code.** Either give the stat a scope-bearing prefix distinct from the tab name (the sibling stat already does: «أبواب هذا التبويب», `abwab.labels.ts:81`), or add a `title`/visible qualifier to the tab badge. No code in `state/` changes — `abwab.labels.ts:76` and `abwab-toolbar.component.html:12-18` are the touch points.

---

### F-75 — `.more-dropdown` is a dead class — Slice H kept it "additive" when `.words-dropdown` was deleted, and nothing in the app now selects it (LOW, Abwab-owned)

**Citation.** `src/app/core/layout/top-navbar/top-navbar.component.html:86`

**What the code does.** `class="nav-item more-dropdown nav-dropdown"`. `grep -rn "more-dropdown\|words-dropdown" src/` returns exactly this one line: no SCSS rule, no spec, no TypeScript selector. `top-navbar.component.scss` styles only `.nav-item`, `.nav-dropdown` and `.dropdown-menu`; the outside-click handler queries `.nav-dropdown[data-menu-key=…]` (`top-navbar.component.ts:58`), not `.more-dropdown`.

**What it should do, and on whose authority.** Dead selectors are removed with the change that orphans them. Authority: `CODING_PRINCIPLES.md` / clean-code-guard "dead code" AI-failure-mode; commit `d7a9c0fb` retained it deliberately as "additive" but its last consumer (`.words-dropdown`'s sibling rule) went away in the same commit.

**Smallest correction — to the code.** Delete `more-dropdown` from the class list.

---

### F-76 — The navbar template hardcodes the `/dashboard` path twice, against `core/README.md`'s stated invariant that route strings come from `route-paths.ts` (LOW, pre-existing)

**Citation.** `src/app/core/layout/top-navbar/top-navbar.component.html:77`

**What the code does.** `[routerLinkActiveOptions]="{ exact: item.route === '/dashboard' }"` appears at `:77` (desktop non-dropdown item) and `:306` (mobile parent link). `DASHBOARD_ROUTE_PATH` exists at `route-paths.ts:19` and is derived from `NAV_ITEMS`. Both predate the Abwab series; Slice H restructured the surrounding branches without lifting them out.

**What it should do, and on whose authority.** `src/app/core/README.md:104-105`: "**Route strings live in `route-paths.ts`** — reference the constants, don't hardcode paths in components/routes." Authority: core README invariant.

**Smallest correction — to the code.** Expose `DASHBOARD_ROUTE_PATH` as a protected field on `TopNavbarComponent` and compare against it at both call-sites; or reuse the same `item.route === item.route`-free shape the child links use.

---

### F-77 — The chrome-inert binding covers `<nav class="qd-navbar">` only; the navbar's own full-screen `.mobile-menu` overlay is rendered outside that element and is never inerted (LOW, Abwab-owned)

**Citation.** `src/app/core/layout/top-navbar/top-navbar.component.html:234`

**What the code does.** `[attr.inert]`/`[attr.aria-hidden]` are bound on the `<nav>` at `:5-6`. The mobile menu is `@if (mobileOpen) { <div class="mobile-menu" …> }` at `:234`, after `</nav>` at `:232` — it is a sibling, not a descendant, so `locked()` does not reach it. It is `position: fixed; inset: 0; z-index: var(--qd-z-mobile-nav)` (`top-navbar.component.scss:135-140`). Not currently reachable: the overlay covers the viewport and any click on it calls `closeMobile()` (`:235`), so no in-page control that opens a modal can be reached while it is open. A fourth entry point (a confirm fired from inside the mobile menu, a route-driven modal, a deep link) would make it reachable.

**What it should do, and on whose authority.** `.architecture/UI_STYLE_SYSTEM.md` §17 "Chrome-inert rule" states the intent as "app chrome is not reachable while a modal dialog is open"; the mobile menu is chrome. Authority: that rule's stated intent.

**Smallest correction — to the code.** Bind `[attr.inert]="locked() ? '' : null"` on `.mobile-menu` too, or set `mobileOpen = false` whenever `locked()` turns true.

---

### F-78 — Six code citations in the README are stale, one of them past end-of-file; five of the six sit in the "Decisions that reversed mid-series" section (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:1003`

**What the code does.** README:1003 cites `abwab-tree.component.scss:257` for the `--empty` modifier; that file is 228 lines and the rule is at :184. README:997 cites `abwab-tree.component.html:70` for `(blur)="cancelOrderEdit(node.id)"`; it is at :68 (:70 is `} @else {`). README:998 cites `abwab-tree.component.ts:263-266` for the cancel guard; :263-266 is the `'focus'` case of `onKeydown`, and the guard is at :226-231. README:1002 cites `abwab-tree.component.html:115-123` for the flag button; the `<button` opens at :113. README:1014 cites `core/navigation/nav-menu.ts:9-29`; `ABWAB_MENU_ITEMS` is :5-22. README:551-552 cites `state/abwab-page-overlays.controller.ts:202` for `moveSectionIds`; it is at :196.

**What it should do, and on whose authority.** Root CLAUDE.md's folding rule requires each folded fact to be "prove[n] from code with a `file:LINE`". Every behavioural claim above is CORRECT — only the anchors drifted. The `scss:257` one is the tell: it cannot ever have been valid against this file, so the block was written against an earlier revision and re-anchored by nobody.

**Smallest correction — to the documentation.** Re-anchor the six citations to :184, :68, :226-231, :113-123, :5-22 and :196 respectively. Prefer symbol names over line numbers in this section, since it is the section most likely to outlive its line numbers.

**Also reported independently at.** `.architecture/UI_STYLE_SYSTEM.md:1471`; `.architecture/UI_STYLE_SYSTEM.md:177`; `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:551`; `src/app/features/abwab/README.md:997`; `src/styles/README.md:39` — merged from 7 separate agent findings covering the same defect.

---

### F-79 — Reversal #4 quotes the nav dropdown's middle item as «القوالب»; the shipped label is «قوالب الأبواب» — «القوالب» is a different control (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:1011`

**What the code does.** `core/navigation/nav-menu.ts:9` sets `labelAr: 'قوالب الأبواب'` for the `abwab-templates` child. «القوالب» is `ABWAB_LABELS.templatesButton` (abwab.labels.ts:265), the doors-page header button rendered at abwab-page.component.html:37.

**What it should do, and on whose authority.** README:1011 states the dropdown's contents as a locked triple, "الرئيسية / القوالب / الأرشيف". The other two match (nav-menu.ts:6 `'الرئيسية'`, :16 `'الأرشيف'`). Arabic-first is a product rule (root CLAUDE.md, PRODUCT.md), so a quoted user-facing string is a checkable assertion; this one names the wrong control.

**Smallest correction — to the documentation.** README.md:1011: change «القوالب» to «قوالب الأبواب».

---

### F-80 — The stats-reconciliation entry gives a reason that does not entail the conclusion, so a developer would think the sum is structurally guaranteed when it is guaranteed by two write guards (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:826`

**What the code does.** `EfAbwabTreeReader.cs:29-32` builds `liveSectionCounts` by grouping every live door on `SectionId`, and :42-45 emits `DoorsInScopeCount` only for sections with `DeletedAtUtc == null` (:10-11). So Σ `doorsInScopeCount` equals `countLiveAbwabDoors` (abwab-tree.builder.ts:167-175) only while no live door sits in a retired section. Two guards enforce that: section delete is refused while live doors remain (ApiMessages.cs:117), and restoring a root whose section was retired demands a live destination (README:222-226, abwab.labels.ts:174). `AbwabNode.sectionId` being `number` rather than `number | null` (abwab.models.ts:129) is not what makes the sum hold.

**What it should do, and on whose authority.** The conclusion the entry draws is CORRECT and its operative instruction ("Do not fix this by summing sections") is right. Only the stated reason is a non-sequitur, and it is the kind that makes an invariant look structural — someone loosening the section-delete guard would not know they had broken the sum.

**Smallest correction — to the documentation.** README.md:826-828: replace "(`AbwabNode.sectionId` is a plain `number`, so Σ `doorsInScopeCount` over every section equals the total)" with "(no live door can sit in a retired section — delete is refused while live doors remain and a retired-section restore demands a live one — so Σ `doorsInScopeCount` over the listed sections equals the total)".

---

### F-81 — Two stale counts: the doors API is described as fifteen endpoints 340 lines after the same README says sixteen, and the page is described as composing fifteen children where it composes seventeen (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:377`

**What the code does.** `abwab.api.ts` declares sixteen methods (getTree :39, createSection :46, renameSection :50, deleteSection :54, reorderSection :58, createDoor :62, updateDoor :66, moveDoor :70, reorderDoor :74, bulkMoveDoors :78, bulkArchiveDoors :82, archiveDoor :86, restoreDoor :90, getDoorRelations :94, addDoorRelations :98, deleteRelation :102). README:37 already says "(sixteen + nine)". Separately, README:49-50 says "the fifteen children it composes"; abwab-page.component.ts:69-88 imports seventeen components besides `RouterLink`.

**What it should do, and on whose authority.** These fail differently and both should be fixed. "Fifteen endpoints" contradicts the same README at :37 and the code — a reader can catch it and will not know which side to believe. "Fifteen children" contradicts only the code, so a reader cannot catch it at all; it also underpins the file-size argument at :41-54 that justifies keeping the page over the 400-line threshold.

**Smallest correction — to the documentation.** README.md:377 "the fifteen doors/sections/relations endpoints" → "the sixteen". README.md:49 "the fifteen children it composes" → "the seventeen children it composes".

**Also reported independently at.**  — merged from 2 separate agent findings covering the same defect.

---

### F-82 — The bulk-conflict message is built from the live bulk set, not the attempted refs, so it can name a door the request never carried (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-write.controller.ts:236`

**What the code does.** `handleBulkFailure(err, attempted)` (:208) receives the exact refs that were sent, but `bulkConflictMessage()` (:234-240) ignores them and maps `this.selection.bulkSet().keys()`. `currentBulkRefs()` (:164-172) filters out any ref whose node is archived, so `attempted` is a strict subset of `bulkSet` whenever a selected door was archived between selection and submit — and the 409 message then names a door that was never in the request. The sibling `bulkVanishedMessage` (:242-257) does use `attempted`.

**What it should do, and on whose authority.** README:751-753 states the contract as "the locked conflict message names every door in the *attempted* selection". I believe the README is the better spec here and the code is the side to correct: a conflict message that names a door the server never saw is misleading at exactly the moment the user needs an accurate list, and the adjacent vanished-message path already does it right. Consequence is bounded to message text, hence LOW.

**Smallest correction — to the code.** Give `bulkConflictMessage` the `attempted` refs: change :234 to `private bulkConflictMessage(attempted: readonly AbwabBulkDoorRef[]): string` mapping `attempted.map((ref) => … byId.get(ref.doorId)?.name ?? String(ref.doorId))`, and pass `attempted` from :214.

---

### F-83 — Three Arabic strings are quoted in guillemets in the README but are not the shipped strings, two of them truncated mid-sentence (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:802`

**What the code does.** README:802 writes «تُحذف من الطرفين»; abwab.labels.ts:212 ships `relationDeleteConfirmSides: 'ستُحذف العلاقة من الطرفين معًا.'`. README:180/184/215/218/742 write «كباب رئيسي»; abwab.labels.ts:152 ships `asMainDoorOption: 'كباب رئيسي (أعلى الشجرة)'`. README:274 writes «لا يوجد باب مطابق»; abwab.labels.ts:244 ships `pickerNoMatches: 'لا يوجد باب مطابق لبحثك.'`.

**What it should do, and on whose authority.** This README is the folded home of the feature's locked copy, and Arabic-first is a product rule (root CLAUDE.md Design Context, PRODUCT.md). Guillemets in this file otherwise mark exact shipped strings — «استرجع الأب أولًا» (:168 = labels.ts:170), «استُرجع الباب» (:229 = :171), «اسم القالب… (Enter)» (:876 = :272), «إضافة عنصر… (Enter)» (:877 = :289) all match byte-for-byte — so the three above read as locked strings and are not. A developer grepping for them finds nothing.

**Smallest correction — to the documentation.** Quote the shipped strings, or drop the guillemets where the README is paraphrasing (the :802 case reads as prose and only needs the marks removed).

---

### F-84 — docs/contracts/ has no Abwab pointer page, so the feature's 1,024-line README is unreachable from the index the workspace declares as the way to find contract truth (LOW, Abwab-owned)

**Citation.** `docs/contracts/README.md:20`

**What the code does.** `docs/contracts/README.md:22-27` lists six pages: http-api, response-envelope, words-explorers, mushaf-reader, import-pipelines, frontend-shell. `docs/contracts/frontend-shell.md:11-14` names core, shared, styles and response-envelope as its authoritative sources — not `features/abwab/README.md`. `grep -rn -i abwab docs/contracts/` returns exactly one hit, `http-api.md:12`, which lists `Abwab/` among controller folders and points at backend controllers only.

**What it should do, and on whose authority.** Root CLAUDE.md calls `docs/contracts/` "the pointer index that makes this rule workable" and Frontend CLAUDE.md tells agents to "use `docs/contracts/frontend-shell.md` / `words-explorers.md` / `mushaf-reader.md` to find the authoritative README/code". Words and Mushaf each got a page; Abwab — now the largest frontend feature, with seven URL keys, twenty-five endpoints and two root-scoped caches — got none, so the index cannot route anyone to it.

**Smallest correction — to the documentation.** Add `docs/contracts/abwab.md` as a pointer page (authoritative sources: `features/abwab/README.md`, `Persistence/Writes/Abwab/README.md`, `Persistence/Reads/Abwab/README.md`) and list it in `docs/contracts/README.md:22-27`. Restate no content, per that file's own rule.

---

### F-85 — The sections-controller bullet lists three forwarded writes; it forwards four, and the omitted one backs a feature the same README documents at length (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:346`

**What the code does.** `abwab-sections.controller.ts` exposes four writes: `createSection` :16, `renameSection` :20, `deleteSection` :24 and `reorderSection` :28-30. README:346-347 says it "forwards create/rename/delete to the shared write controller above".

**What it should do, and on whose authority.** The same README says the modal "Takes its four write functions as inputs (bound by the page to `state/abwab-sections.controller.ts`)" (:237) and devotes :241-251 to the order editor those four include. The three-item list is the stale one.

**Smallest correction — to the documentation.** README.md:346: "forwards create/rename/delete" → "forwards create/rename/reorder/delete".

---

### F-86 — The archive view's row controls and the doors tree's bulk checkbox are real tab stops inside role="treeitem" rows that also carry a roving tabindex (LOW, Abwab-owned)

**Citation.** `src/app/features/abwab/components/abwab-archive-view/abwab-archive-view.component.html:5`

**What the code does.** Archive rows are role="treeitem" with [attr.tabindex]="rovingId() === node.id ? 0 : -1" (:10) and contain a chevron (:15-25) and a restore button (:29-37) with no tabindex override, so Tab walks 2N controls through the tree. The doors tree keeps every in-row control out of the tab order — chevron (:50), flag (:119), ＋ (:133), ⋯ (:143) all [attr.tabindex]="-1" — except the bulk checkbox (:37-44), which has none.

**What it should do, and on whose authority.** README:118-121 states the doors tree keeps its row actions "out of the tab order so the roving-tabindex invariant holds", and README:107-109 says the archive view and the doors tree share the same tree contract. The archive view reuses the same keyboard controller (abwab-archive-view.component.ts:76-83) and the same roving model (:46-56), so the invariant should carry with it. The checkbox is redundantly reachable — Space already toggles it (abwab-tree-keyboard.controller.ts:104-106).

**Smallest correction — to the code.** Add [attr.tabindex]="-1" to the archive chevron and restore button and give the archive keyboard model a key that activates restore, mirroring the doors tree's Enter/menu handling; add [attr.tabindex]="-1" to the bulk checkbox.

---

### F-87 — The relations flag is a pressable strip that does nothing in bulk mode — it neither opens relations nor toggles the row's bulk checkbox — while the README says a row click in bulk mode means "toggle this door" (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.ts:146`

**What the code does.** `onFlagClick(event, id)` calls `event.stopPropagation()` first and then returns early when `bulkMode()` is true (`:147-150`). Because propagation is already stopped, the row's `(click)="onRowClick(node.id)"` (`abwab-tree.component.html:30`) never runs either, so the flag is a dead rectangle inside the row's bulk hit area. This is pinned as intended by `abwab-tree.component.spec.ts:420` («is inert in bulk mode, and the click never reaches the row's bulk toggle»).

**What it should do, and on whose authority.** README.md:133-134 says «it is inert in bulk mode, where a row click means "toggle this door"» — the second clause is exactly what the flag prevents on its own strip. The code is deliberate and pinned; the sentence is self-contradictory.

**Smallest correction — to the documentation.** Reword README.md:133-134 to state that the flag swallows the click in bulk mode rather than falling through to the row's toggle.

---

### F-88 — The relations flag's has-relations / no-relations state is conveyed to sighted users by colour alone (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-tree/abwab-tree.component.scss:184`

**What the code does.** `.abwab-tree__flag` is `background: var(--qd-accent-tint); color: var(--qd-accent-text); border-color: var(--qd-border-accent)` (`:178-182`) and `.abwab-tree__flag--empty` is `background: transparent; color: var(--qd-text-muted); border-color: var(--qd-border)` (`:184-188`). The visible label is the constant «علاقات» in both states (`abwab-tree.component.html:122`, `abwab.labels.ts:191`); no digit, glyph, or weight change distinguishes them. The count reaches AT only through `aria-label` (`abwab-tree.component.html:117`).

**What it should do, and on whose authority.** UI_STYLE_SYSTEM.md:374: «Do not rely on color alone to convey meaning». The same file's §752 states an active state «must not rest on colour alone», and the toolbar's own view toggle honours it with `font-weight: 700` (`abwab-toolbar.component.scss:73`).

**Smallest correction — to the code.** Add one non-chromatic differentiator at zero — e.g. render the count beside the label, or a weight/border-style change — so the two states differ by more than hue.

**Also reported independently at.** `src/app/features/abwab/components/abwab-tree/abwab-tree.component.html:113` — merged from 2 separate agent findings covering the same defect.

---

### F-89 — Cards render a bare unlabeled digit for a count whose scope is undeclared, while every count badge in the tree carries a full Arabic aria-label naming its scope (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-cards/abwab-cards.component.html:53`

**What the code does.** `{{ node.children.length > 0 ? node.liveChildCount : '' }}` — the gate reads `children.length` (which, under a search, is the PRUNED child list) while the printed number is `liveChildCount` (the unpruned live direct-child count from `abwab-tree.builder.ts:55`). No `aria-label`, no unit, no scope word. The order chip on the same card (`:49`) is likewise a bare digit.

**What it should do, and on whose authority.** Counting-scope discipline is repo law: a count must declare its scope. The same value in the tree is `[attr.aria-label]="childCountAriaLabel(node.liveChildCount)"` → «N باب تحته مباشرة» (`abwab-tree.component.html:86`, `abwab.labels.ts:99`). Under a search the gate and the value also disagree about which set they describe.

**Smallest correction — to the code.** Reuse `ABWAB_LABELS.rowChildCountAriaLabel` on the cards meta span and gate it on `liveChildCount > 0` so the condition and the number describe the same set.

---

### F-90 — A hand-entered `?archive=1&door=<live id>` leaves the side panel offering edit/move/archive/add-child while the archive view is on screen (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.ts:263`

**What the code does.** `this.selection.setArchiveViewActive(parsed.archive)` only turns bulk mode off (`abwab-selection.store.ts:41-46`); it never clears the single selection. `parseAbwabQueryParams` (`abwab-url-sync.ts:59-72`) accepts `archive` and `door` together — the door/card/modal invalidation lives only in `buildAbwabQueryParams` (`:100-105`), i.e. it fires on in-app navigation, not on URL entry. With both keys present the `door=` effect selects the door (`abwab-page.component.ts:231-241`), and `abwab-side-panel.component.html:41,50,59,68,77` only disable on `selectedDoor() === null || bulkMode()`; `archiveViewActive()` gates the bulk toggle alone (`:29`).

**What it should do, and on whose authority.** README.md:714-717 («Archived doors are read-only … Any other control on an archived door would be dead by definition») and the URL contract's «Fails closed to the defaults on anything invalid» (`README.md:507-512`). The nav dropdown's archive entry replaces rather than merges query params (`core/layout/top-navbar/top-navbar.component.html:58`), so this is only reachable via a bookmarked or hand-edited URL — but the contract claims a fail-closed parse.

**Smallest correction — to the code.** Clear the selection in the queryParamMap subscription when `parsed.archive` is true (or pass `archiveViewActive` into the ops' disabled expressions in the side panel).

---

### F-91 — The templates list's load-error state offers no retry, leaving a full-page browser reload as the only recovery — while the copy modal nested inside the same page does offer one (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.html:28`

**What the code does.** `<qd-state variant="error" [reserve]="true" [message]="facade.errorMessage() ?? ''" />` with no `actionLabel` and no `(action)`. `AbwabTemplatesFacade.loadList()` is available and is called from `ngOnInit` (`abwab-templates-page.component.ts:150`); the page already wires a retry for the doors read (`retryDoorsLoad()`, `:320-322`, bound at `abwab-templates-page.component.html:202`).

**What it should do, and on whose authority.** README.md:794-798 enumerates the three retry sites and justifies them as «the transport reads abwab otherwise offers no recovery from at all» — the templates list is a fourth read with exactly that property and no recovery, so the stated criterion selects it and the implementation does not.

**Smallest correction — to the code.** Add `[actionLabel]="retryLabel" (action)="facade.loadList()"` to that one `qd-state`, or record in the README why this read is exempt.

---

### F-92 — `cancelTemplateDelete()` has no in-flight guard and does not clear its error, unlike the node-delete confirm three methods above it (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-templates-page/abwab-templates-page.component.ts:307`

**What the code does.** `cancelTemplateDelete()` sets `confirmingTemplateDelete` to false unconditionally. Its sibling `cancelNodeDelete()` (`:274-280`) returns early when `nodeDeleteBusy()` and clears `nodeDeleteError`. So the template-delete dialog can be dismissed while its DELETE is in flight; if the write then fails, `templateDeleteError.set(outcome.message)` (`:299`) writes to a closed dialog and the failure is never shown, and the stale message survives until the next `requestTemplateDelete()` resets it (`:284`).

**What it should do, and on whose authority.** Same-area precedent: the node-delete pair in the same class, and `AbwabPageOverlaysController.cancelArchiveConfirm` (`state/abwab-page-overlays.controller.ts:171-175`), both guard on busy.

**Smallest correction — to the code.** Add `if (this.templateDeleteBusy()) { return; }` and `this.templateDeleteError.set(null);` to `cancelTemplateDelete()`.

---

### F-93 — Three different breakpoint conventions inside one feature: the shared SCSS variable, a raw 900px, and 60rem (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.scss:94`

**What the code does.** `@media (max-width: 900px)` collapses the doors page's side panel. `abwab-tree.component.scss:158` uses `@media (max-width: bp.$qd-bp-tablet-max)` (1023px) to drop the two wide badge columns, and `abwab-templates-page.component.scss:153` uses `@media (max-width: 60rem)` (960px). All three describe the same tablet-ish reflow of the same feature at three different widths.

**What it should do, and on whose authority.** `styles/_breakpoints.scss` defines `$qd-bp-phone-max: 767px; $qd-bp-tablet-max: 1023px; $qd-bp-desktop-min: 1024px;` as the vocabulary, and the same feature's own tree stylesheet already imports and uses it (`abwab-tree.component.scss:1`). Same-area precedent, and the tokens exist precisely so the reflow points line up.

**Smallest correction — to the code.** Import `styles/breakpoints` in both page stylesheets and replace `900px` / `60rem` with the intended `bp.$qd-bp-*` token.

---

### F-94 — One file is over its hard threshold and three more are over soft without the README mention FRONTEND_STRUCTURE requires (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/pages/abwab-page/abwab-page.component.ts:1`

**What the code does.** `abwab-page.component.ts` is 593 lines against a 400-line hard threshold; `abwab-page.component.html` 306 and `abwab-templates-page.component.ts` 358 against a 300-line soft threshold; `abwab-tree.component.ts` 324 (soft 300) and `abwab-tree.component.scss` 228 (soft 200). README.md:45-54 acknowledges the page TS and names the next seam, and README.md:290-300 acknowledges the templates page TS and names its split trigger; the page HTML, the tree TS and the tree SCSS are acknowledged nowhere.

**What it should do, and on whose authority.** `.architecture/FRONTEND_STRUCTURE.md:85-86,99-100,111-112` set the thresholds and `:163-175` requires a soft exceedance to be mentioned and justified, and a hard exceedance to be split or explicitly proposed. Two of five are documented; three are silent.

**Smallest correction — to the documentation.** Add the three unacknowledged files to the README's size note (or split them); no new document — the README already carries this ledger for the other two.

---

### F-95 — `AbwabDoorPickerComponent.onRowChange` is unreachable dead code — the checkbox's own click handler cancels activation, so `change` never fires (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-picker/abwab-door-picker.component.ts:136`

**What the code does.** The input binds both `(click)="$event.preventDefault()"` and `(change)="onRowChange(row)"` (abwab-door-picker.component.html:48-49). Cancelling the click event skips the control's activation behaviour, so neither `input` nor `change` is dispatched — for mouse clicks and for keyboard Space alike. Every real selection therefore flows through the row wrapper's `(click)="togglePicked(row)"` (abwab-door-picker.component.html:20). `onRowChange` has no coverage in `abwab-door-picker.component.spec.ts` (no `change`/`onRowChange`/`single` match in that file).

**What it should do, and on whose authority.** `CODING_PRINCIPLES.md` / the clean-code-guard AI-failure-modes list treat dead code as a defect; the `single()` early-return inside `onRowChange` also reads as a live single-vs-multi rule that does not exist.

**Smallest correction — to the code.** Delete `onRowChange` and the `(change)` binding, leaving the row's `(click)` as the one selection path.

---

### F-96 — The door picker's excluded/disabled reasons are visual-only: `aria-disabled` sits on a role-less `<div>` and the «…» tag text is not part of any accessible name (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-picker/abwab-door-picker.component.html:18`

**What the code does.** `[attr.aria-disabled]="row.isExcluded ? true : null"` is set on `<div class="abwab-door-picker__row qd-check-row">`, which carries no `role` — `aria-disabled` on a generic element is not exposed as a state. The reason chips at lines 53-57 (`excludedTag()` / `disabledTag()`, e.g. «مرتبط بالفعل») are bare `<span>`s outside the checkbox's `[attr.aria-label]="row.node.name"` (line 46), so a screen-reader user hears only the door name and, for disabled rows, the native disabled state — never why.

**What it should do, and on whose authority.** `UI_STYLE_SYSTEM.md` §`.qd-checkbox`: "Accessible name (contract, not optional): every checkbox composing `.qd-checkbox` MUST carry a real `<label for>` or an `aria-label` naming what it selects", and the "Header over badge columns" entry's standing rule that visible text is a hint while "meaning stays on each badge's own `aria-label`". Same-area precedent: `abwab-archive-view` renders the same kind of reason text (`restoreParentFirstHint`) beside a natively `[disabled]` button (abwab-archive-view.component.html:32,38-42) — also visual-only, so this is the feature's pattern, not a one-off.

**Smallest correction — to the code.** Fold the tag into the checkbox's accessible name (`[attr.aria-label]="row.node.name + (row.isDisabled ? ' — ' + disabledTag() : '')"`) and drop `aria-disabled` from the role-less row, or give the row a role that supports it.

---

### F-97 — The door picker's loading/error/empty states live inside `@empty`, so a doors-load failure that arrives while rows are already rendered shows nothing at all (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-picker/abwab-door-picker.component.html:59`

**What the code does.** The `@empty { … }` block is the only place `status() === 'loading'`, `status() === 'error'`, `searchFoundNothing()` and `emptyMessage()` render. In the copy modal, `retryDoors` re-runs `AbwabSnapshotFacade.load()` (abwab-templates-page.component.ts:320-322); if that retry fails while the previous snapshot's roots are still bound, `rows()` is non-empty, `@empty` never runs, and the error + retry control the modal passes in (`[status]`, `[errorMessage]`, `(retry)`, abwab-template-copy-modal.component.html:38-46) are silently swallowed.

**What it should do, and on whose authority.** `.architecture/API_INTEGRATION_GUIDELINES.md` / UI_STYLE_SYSTEM §17 treat loading / empty / error as distinct surfaces, and README.md:786-796 names the copy modal's doors-load failure as one of only three sites permitted a retry action — a retry that cannot render in the stale-data case is not that guarantee.

**Smallest correction — to the code.** Hoist the `status() === 'error'` branch out of `@empty` so the error banner renders above the list whenever `status()` is `'error'`, regardless of row count.

---

### F-98 — `pickerStatus` in the template-copy modal can never evaluate to `'ready'`, so the picker's four-state type is really three states at this call site (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-template-copy-modal/abwab-template-copy-modal.component.ts:62`

**What the code does.** `computed<AbwabDoorPickerStatus>(() => this.doorsLoading() ? 'loading' : this.doorsError() ? 'error' : 'empty')` — the loaded-and-populated case resolves to `'empty'`, which happens to be harmless only because the picker consults `status()` exclusively inside `@empty`. The declared union `'ready' | 'loading' | 'error' | 'empty'` (abwab-door-picker.component.ts:8) advertises a state this consumer can never produce, and the relations modal (the only other consumer) passes no `status` at all, taking the `'ready'` default.

**What it should do, and on whose authority.** Strong-typing / KISS in `CODING_PRINCIPLES.md`: a state machine whose consumers can only reach a subset of its states is misleading. Either the ternary returns `'ready'` when doors are present, or the picker's union drops the member no call site sets.

**Smallest correction — to the code.** Make the final branch `this.liveRoots().length === 0 ? 'empty' : 'ready'`, so the value read matches the state it names.

---

### F-99 — UI_STYLE_SYSTEM.md's `reserve` entry undercounts the `[reserve]` call-sites (four claimed, eight in code) and describes all of them as message-guarded when the door picker is not (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:866`

**What the code does.** §17 states "**`reserve` under an `@if` reserves nothing, and abwab does exactly that — knowingly.** All four abwab modal error surfaces render as `@if (message; as m) { <qd-state variant="error" [reserve]="true" [message]="m" /> }`" and closes "Do not 'fix' abwab's four sites by deleting the `@if`." `grep -rn '\[reserve\]' src/app/` returns eight sites, all `variant="error"`: abwab-door-picker.component.html:70, abwab-page.component.html:103, abwab-sections-modal.component.html:22, abwab-door-fields-form.component.html:2, abwab-template-copy-modal.component.html:23, abwab-relations-modal.component.html:32, abwab-templates-page.component.html:28 and :79. Four are the modals the doc means; two more (abwab-page, abwab-templates-page:28) are page-level and guarded by `errorMessage() && …`; abwab-templates-page:79 is a seventh guarded site; and the door picker is guarded on `status()` instead. A reader repointing this rule from the doc will miss half the call-sites and will not learn that one behaves differently.

**What it should do, and on whose authority.** Root CLAUDE.md: a fact folded into long-lived documentation must be provable from the code it describes, and `shared/README.md:43` explicitly tells the reader to `grep -rn '[reserve]' src/app/` for the current consumers — the doc should not then state a count that the grep contradicts.

**Smallest correction — to the documentation.** In UI_STYLE_SYSTEM.md:866-880, say "every abwab `[reserve]` error surface" rather than "all four", and add one clause noting the door picker guards on `status()` (and, once the code finding above is fixed, on a non-empty message too).

---

### F-100 — The context-menu placement contract's labels ("extends toward the inline-start", "cross the inline-start viewport edge") are inverted relative to the mechanics stated in the same sentence and implemented in code (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:1225`

**What the code does.** §17: "The menu extends toward the **inline-start** of the anchor point: under RTL its right edge sits at `x` and the box grows leftward" and "It **flips** … inline when the preferred side would cross the inline-start viewport edge". In RTL, inline-start IS the right side, so a box whose right edge sits at `x` and grows leftward extends toward inline-END; and the edge it can cross while doing so is the left/inline-end viewport edge. The code matches the mechanical clauses exactly and is correct RTL behaviour: `let left = rtl ? anchor.x - width : anchor.x;` then `if (rtl ? left < VIEWPORT_MARGIN : left + width > viewportWidth - VIEWPORT_MARGIN)` (context-menu.component.ts:68-71) — start edge pinned at the pointer, box grows in reading direction, flip when the far edge would leave the viewport. `shared/README.md:27-28` repeats the same inverted phrasing ("extends toward inline-start, flips on either viewport edge").

**What it should do, and on whose authority.** §8 (RTL and direction) and §17 are the authority for this primitive; a directional term used with the opposite of its CSS meaning in a contract that a future reader will implement against is a defect in the doc, not the code. The code is right — do not "fix" the placement to match the label.

**Smallest correction — to the documentation.** Reword both to the mechanics: "the menu's inline-START EDGE is pinned at the anchor and the box grows in the reading direction; it flips when its trailing (inline-end) edge would cross the viewport". Two sentences: UI_STYLE_SYSTEM.md:1225-1227 and shared/README.md:27-28.

---

### F-101 — §17's `qd-chip` backing-class list omits `.qd-chip--disabled`, which is the class that actually delivers the non-interactive disabled state on the removable and anchor branches (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:791`

**What the code does.** §17 lists "`.qd-chip`, `.qd-chip--pill`, `.qd-chip--static`, `.qd-chip.qd-is-selected`, `.qd-chip__count`, `.qd-chip__remove`, `.qd-chip__label`, `.qd-chip__label--clickable`. Compose, do not re-style." The template also binds `[class.qd-chip--disabled]="disabled()"` on the removable `<span>` (chip.component.html:29) and the anchor (`:46`), and `_components.scss:266-271` is `.qd-chip:disabled, .qd-chip.qd-chip--disabled { cursor: not-allowed; opacity: 0.5; pointer-events: none; }` — the only thing making a disabled removable chip or disabled anchor chip non-interactive, since neither element supports the native `disabled` attribute.

**What it should do, and on whose authority.** §17 declares itself the live contract and the class list is what a consumer composes against; a class that carries a stated behaviour ("disabled is visually muted and non-interactive", :780) must appear in the list that enumerates the behaviour's backing.

**Smallest correction — to the documentation.** Add `.qd-chip--disabled` to the backing-class list at UI_STYLE_SYSTEM.md:791-793.

---

### F-102 — §17 describes `.qd-checkbox` as sizing "a native `<input type="checkbox">`", but the door picker composes it on an `<input type="radio">` in single-pick mode (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:1035`

**What the code does.** §17 `.qd-checkbox` / `.qd-check-row`: "`.qd-checkbox` sizes and colors a native `<input type="checkbox">`", and its "Consumers" line (:1053-1054) names abwab-door-picker, abwab-tree, abwab-cards. abwab-door-picker.component.html:40-48 applies `class="qd-checkbox"` with `[attr.type]="single() ? 'radio' : 'checkbox'"` — reached whenever `[single]="anchorPickMode()"` is true (abwab-relations-modal.component.html:143). The styling works for both (fixed square, `accent-color`), so this is wording drift, not a rendering defect.

**What it should do, and on whose authority.** §17 is the composition contract; a consumer reading it would not know a radio is a sanctioned composition, and a future reviewer might "fix" the radio call-site off the class.

**Smallest correction — to the documentation.** At UI_STYLE_SYSTEM.md:1035-1036, say "a native `<input type="checkbox">` or `<input type="radio">`", and note the door picker's single-pick mode as the radio precedent.

---

### F-103 — `abwab-door-restore-modal` is the one `qd-confirm-dialog` on the doors page that does not pass `testIdPrefix`, against §17's stated rule for pages hosting more than one confirm (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/features/abwab/components/abwab-door-restore-modal/abwab-door-restore-modal.component.html:1`

**What the code does.** The restore modal renders `<qd-confirm-dialog [open]="open()" [titleText]="title" …>` with no `testIdPrefix`, so it answers to the default ids `qd-confirm-dialog`, `-backdrop`, `-confirm`, `-cancel` (confirm-dialog.component.ts:24). It is hosted on the doors page (abwab-page.component.html:223) alongside two other confirms that DO pass distinct prefixes — `abwab-page-archive-confirm` (:258) and `abwab-page-bulk-archive-confirm` (:275). Not ambiguous today, because it is the only default-prefixed confirm on that page; the ambiguity appears the moment a second one is added. Every other confirm in the app passes one (abwab-sections-modal.component.html:140, abwab-relations-modal.component.html:175, abwab-templates-page.component.html:143 and :161).

**What it should do, and on whose authority.** UI_STYLE_SYSTEM.md §17 `qd-confirm-dialog` (:803-806): "**Pass it whenever a page can host more than one confirm** — otherwise two dialogs on one page answer the same selector and every assertion against them is ambiguous." The doors page hosts three.

**Smallest correction — to the code.** Add `testIdPrefix="abwab-door-restore-confirm"` at abwab-door-restore-modal.component.html:1, matching the naming of the page's other two.

---

### F-104 — The two comments the purge kept in shared/ui both restate facts already written into `.architecture/UI_STYLE_SYSTEM.md` §17, so they do not clear the root CLAUDE.md exception bar (LOW, Abwab-owned)

**Citation.** `Frontend/quran-dashboard-ui/src/app/shared/ui/chip/chip.component.html:1`

**What the code does.** chip.component.html:1-2 ("Declared once and rendered through an outlet, because two `<ng-content>` elements sharing one selector would leave the second slot permanently empty") is stated at UI_STYLE_SYSTEM.md:786-789; context-menu.component.scss:21-23 (measure-for-one-frame, `visibility` not `display: none`) is stated at UI_STYLE_SYSTEM.md:1232-1234. Both were deliberately kept by commit c597a3f3 ("47 of 49 comments deleted, 2 kept"), so this is a judgement call rather than an oversight.

**What it should do, and on whose authority.** Root CLAUDE.md, "Comments are forbidden by default": the exception requires all three of (1) not derivable, (2) omitting it lets a competent developer make a WRONG change, and (3) it cannot be solved by a sentence in the nearest README/doc. Condition 3 fails for both — the sentence already exists in the area's authoritative doc, and "comments that repeat a README" is on the forbidden-with-no-exception list. "The burden of proof lies with the comment, never with its deletion."

**Smallest correction — to the code.** Delete both, since the knowledge already lives in §17 and neither line adds anything the doc does not carry. Flagged for the record; the cost of leaving them is near zero.
---

## 3b. Area 8 — the cross-slice pass

The reason this review exists. Areas 1–7 each looked at one surface; this section looks at the
interactions between them, which is what thirteen per-slice reviews structurally could not see.

Everything below is adjudicated against evidence harvested during areas 1–7 plus the direct
checks recorded inline. Where a suspicion was checked and **did not** hold, that is recorded as a
result rather than dropped — a cleared hypothesis is worth as much here as a confirmed one.

---

### 8.1 The four reversals — all four are implemented; three retain remnants

| # | Reversal | Implemented coherently? | Surviving remnant |
|---|----------|------------------------|-------------------|
| 1 | Reveal-in-tree no longer clears the search query `q` | **Yes — clean** | None found |
| 2 | The relations flag is always rendered, dimmed at zero, clickable | Yes | Dead in bulk mode (F-87); zero/non-zero is colour-only (F-88) |
| 3 | Template apply copies the root's CHILDREN, never the root | Yes | `createdRoots` misnamed (F-27); README cites lines that state nothing (F-44) |
| 4 | The navigation entry is a dropdown | Yes | Four defects (F-40 *pre-existing*, F-41, F-42, F-79) plus a dead class (F-75) |

**Reversal 1 — verified clean, end to end.** This is the only one with no remnant, and it is worth
stating how it was proven rather than asserted. `onRevealRequested`
(`abwab-page.component.ts:395-416`) patches `door`, `modal`, conditionally `section` and
conditionally `view` — and never `q`. That is sufficient because `buildAbwabQueryParams`
(`abwab-url-sync.ts:84-118`) writes **only** keys present in the change object, and its
scope-invalidation rule at `:100-105` clears `door`, `card` and `modal` — never `q`. A repo-wide
grep for writes to the `q` key returns exactly one site, `:97`, which is the search box itself.
There is no surviving path by which a reveal can clear the query.

**Reversal 3 — the code is right and the README is wrong, which is the direction that matters.**
`EfAbwabTemplateApplyWriter` copies children only; the invariant holds in code. What survives is
naming and documentation: the variable holding the copied *children* is called `createdRoots`
(`EfAbwabTemplateApplyWriter.cs:87`, F-27), which says the opposite of the invariant the file enforces,
and the README's citation for the reversal points at lines that assert nothing. Both are the
residue of the pre-reversal decision. Neither changes behaviour today; both are exactly what would
mislead the next person to touch the file.

---

### 8.2 URL and cache identity — **PASS**, and the apparent violation is not one

This is the check most likely to be misread, so the reasoning is recorded in full.

**The two key sets.**

- **Frontend URL — seven keys** (`abwab.models.ts:153-161`): `section`, `view`, `archive`,
  `door`, `card`, `q`, `modal`.
- **Backend tree cache — zero inputs.** The cache key has no inputs and the tree route has no
  parameters.

A set difference of seven against zero looks like a flat violation of the rule that both must
include every input that changes the returned scope. **It is not**, because none of the seven is
a *server-side* scope input. The tree read is one unparameterized, root-scoped GET returning the
whole snapshot, and every one of the seven keys scopes **client-side** over that single payload:

| URL key | What it scopes | Server input? |
|---------|----------------|---------------|
| `section` | `filterAbwabRootsBySection` (`abwab-tree.builder.ts:103`) | No — client filter |
| `archive` | A partition of the same snapshot (`abwab-tree.builder.ts:76-79`); toggling issues **no request** — verified | No |
| `q` | Client-side search over the built tree | No |
| `view`, `door`, `card` | Presentation and selection only | No |
| `modal` | Overlay selection only | No — see below |

So the set difference is empty *by construction*: there is nothing for the backend key to include,
and zero inputs is the correct design rather than a gap. The `304` cannot be served across a scope
change because no scope change reaches the server.

**The one key that could have broken it is `modal`, and it does not.** The README pins this
explicitly (`README.md:433`): «`modal` selects an overlay, never a data scope, and it enters no
*caching* identity.» Verified against code: the relations read is keyed on door id and the tree
validator only, and the `-closed` suffix carries a restore subject, not a cache input. This is the
row a future caching design must not pick up — adding `modal` to a cache key would be a contract
change, not an optimisation.

**Does a restored URL reproduce the same state?** Mostly, with one hole: a `section` id that no
longer exists is validated for shape but never for existence, producing an empty tree, a `0` count
and no selected tab (**F-37**, MEDIUM). Every other key fails closed to its default.

**Can a selection or an open detail survive into a scope it no longer belongs to?** This is the
question that produced the review's only HIGH, and the answer differs by subject:

| Subject | Survives a scope change? | Evidence |
|---------|--------------------------|----------|
| Single selection | **No** — invalidated | `abwab-url-sync.ts:100-105` clears `door`/`card`/`modal` |
| Open detail (the `-closed` restore chip) | **No** — refused | `abwab-modal-url.controller.ts:31-34` |
| **Bulk selection** | **YES — defect** | **F-35 (HIGH)** |

**A suspicion checked that did not hold.** The `-closed` restore subject looked like the sharpest
remaining interaction: `onRevealRequested:412` writes the *anchor* door as the restore subject
while the archived-door guard at `:397` tests the *revealed* door, so the anchor is never checked
at write time — and archived doors remain present in `byId`, since that is how the archive view
partitions them. A membership-only restore guard would therefore offer to reopen relations for a
door the user has since archived. **The guard is not membership-only.**
`abwab-modal-url.controller.ts:31-34` reads
`return !!node && !node.isArchived ? modal : null` — it tests the archived flag explicitly, so the
stale chip is refused at read time and the interaction is safe. Recording this as verified clean
because the write-side asymmetry is real and a future refactor that "simplifies" this guard to a
membership test would silently open the hole.

---

### 8.3 Cache invalidation — **PASS**, the strongest result in the review

Every write that changes what a reader would see bumps the generation. This was the highest-risk
backend check and it came back clean on every axis:

- **Per-method completeness: 21/21.** Every method on all five writer interfaces both forwards and
  bumps — doors 8/8, relations 2/2, sections 4/4, template-apply 1/1, templates 6/6. The classic
  "forwards eight, bumps six" defect is absent.
- **Registration: 5/5.** All five decorators are genuinely in the DI chain; the interface resolves
  to the decorator, never the bare writer. An unwired decorator would have been invisible.
- **Write-set ⊆ bumped read-set, proven both directions.** Tree read-set is
  `{Sections, Doors, DoorAliases, DoorRelations}`; templates read-set is
  `{Templates, TemplateNodes}`. The apply writer only *reads* templates (`AsNoTracking`) and
  writes doors/aliases, so it correctly bumps the tree and correctly does **not** bump templates.

Each write class the review asked about individually — a relation write, a section change, a
restore, each bulk operation, a template apply — is covered by that closure.

**Two caveats that do not change the verdict but bound it.**

1. **The counter is per-process** (see **Q-02**). It is correct for exactly one instance and
   serves stale `304`s the moment a second exists. Nothing in the repo pins the deployment to one
   instance.
2. **The relations cache's "rename pin" holds only by luck of granularity.** A cached relation list
   embeds the partner's *name*, so a door rename must evict it. It does today only because every
   door write bumps the whole tree generation. Any future finer-grained invalidation that stops
   bumping on rename breaks the relations cache silently — the README flags this as binding on
   future work, and that flag is correct and worth keeping.

---

### 8.4 Counting-scope discipline — **MIXED**

Repo law: every count declares whether it means doors, live doors, descendants, or relation rows.
Most counts comply; four do not, and they cluster at the seams between slices.

| Count | Declares its scope? | Verdict |
|-------|--------------------|---------|
| Tree badge counts | Yes | PASS |
| Toolbar tab counts (`rootCountFor`, `totalRootCount`) | Yes — roots in scope | PASS |
| `countLiveAbwabDoors` (`abwab-tree.builder.ts`) | Yes — live doors | PASS |
| `AbwabTemplateSummaryDto.NodeCount` | **No** — counts live **non-root** descendants | **F-06** |
| Cards view count | **No** — bare unlabeled digit | **F-89** |
| Two adjacent counts both labelled «كل الأبواب» | **No** — same label, different scopes | **F-74** |
| Bulk-conflict message | **No** — built from the live bulk set, not the attempted refs | **F-82** |

The pattern is that the *feature-internal* counts are disciplined and the counts that cross a
boundary — into a DTO, into a card, into an error message — lose their scope declaration.

---

### 8.5 The invariants the slices established — consolidated verdict

This table is area 8's verified-clean deliverable. The 314 individual entries in §5 are the
appendix behind it.

| # | Invariant | Verdict | Evidence |
|---|-----------|---------|----------|
| 1 | `section_id` NOT NULL; writer rejects at root scope only; children derive from parent | **PASS** | Column NOT NULL via `20260802062011`; frontend half confirmed — `abwab.api.ts:29-32` destructures `sectionId` out of the body entirely when `parentId != null`, so the key is genuinely **absent**, not `undefined` |
| 2 | Restore resolves a detached door; re-sectioning cascades to descendants **including archived rows** | **PASS** | Cascade query carries no `!IsArchived` filter |
| 3 | Independent global root ordering, `global ⟺ root ∧ live` | **PASS — all transitions** | create root `:36-41`; create child (never touched); move both directions `:137-145`; bulk move `:262-275, :291-294`; archive `:585-592`; delete `:369-372`; bulk archive `:328, :341-344`; restore `:430-433` |
| 4 | Relations stored as canonical pair + broader-door ref; dormancy **derived, never stored** | **PASS (code) / FAIL (coverage)** | Unique index `(DoorAId, DoorBId, RelationType)` filtered on `deleted_at IS NULL`; no dormancy column anywhere. **Zero tests** — **F-13** |
| 5 | Template apply keyed on (target, child); empty-root 400; copies children never root | **PASS (code) / FAIL (coverage)** | Transactional `:18`/`:130`; unique index blocks collisions. **No test pins the reversal** — **F-14** |
| — | URL ⟺ cache identity | **PASS** | §8.2 |
| — | Invalidation completeness | **PASS** | §8.3 |

Invariant 3 deserves emphasis: it is the one whose failure would be *wrong ordering data*, and
every one of its eight transitions has an enforcing line. It holds.

The pattern across 4 and 5 is consistent and worth naming: **the two invariants that encode a
scholarly claim rather than a mechanical rule are exactly the two with no test.** Invariant 4
persists a claim about which of two doors is broader; invariant 5 encodes a reversed structural
decision. Both are correct today and neither would fail loudly if a future change broke them.

---

### 8.6 State completeness — **MIXED**

Loading / empty / error / retry / partial-failure / success are genuinely distinct in the state
layer and in most modals, but three surfaces collapse states that must stay separate:

- **The cards view has no empty state and no no-results state at all** (**F-53**) — a zero-match
  search is indistinguishable from a load that returned nothing.
- **A zero-match search in the archive view renders «لا توجد أبواب مؤرشفة.»** (**F-54**) — the
  no-results state is reported as the empty state, telling the user the archive is empty when it
  is not.
- **`qd-state`'s reserved slot can render as an empty danger box** (**F-66**). This is a direct hit
  on the rule that a reserved slot must never render as an empty error box: the door picker keys
  its reserve such that an error box can appear with no message inside it. Of the seventy
  component findings this is the one that belongs in the cross-slice verdict, because the reserve
  is a *shared* mechanism and the defect is visible only where a feature meets it.

Skeleton rows are non-interactive everywhere checked — that half holds.

---

### 8.7 RTL, accessibility, keyboard and focus as one system — **MIXED**

Reviewed as a system rather than per component, the divergences are the finding. Each surface was
acceptable in its own slice review; together they are inconsistent.

- **Focus return diverges.** Most overlays return focus to their invoker. Three do not: the nav
  dropdown drops to `<body>` (**F-41**), `abwab-templates-page` implements no focus return at all
  (**F-47**), and a successful door restore drops focus to `<body>` because the invoking archive
  row is removed (**F-48**). "Focus returns on close" is therefore not a property of the feature —
  it is a property of most of it, which is the harder kind of bug to notice.
- **Escape becomes a dead key** once a dirty-discard strip is open in the door, sections and
  template-node modals (**F-49**) — the nested-overlay case the per-modal reviews could not see.
- **Two authority documents give opposite rules** for gating a modal's focus trap under a nested
  confirm (**F-43**), which is why the modals diverge: each slice followed a different document.
- **Inline reorder is mouse-only in both trees** (**F-58**), and the templates tree still commits
  on blur — the exact behaviour the doors tree deliberately abandoned (**F-57**). One tree kept the
  reversed behaviour.
- **The context menu declares `role="menu"`/`role="menuitem"` but manages no focus** (**F-70**),
  so the roles promise keyboard semantics the implementation does not provide.
- **Announcements are inconsistent in both directions**: every write failure is announced twice
  (**F-51**), while successful writes announce nothing for doors and everything for templates
  (**F-52**).

---

### 8.8 README-vs-code fidelity — the systemic finding

Treated as one result rather than thirty: the two recent documentation passes folded facts out of
artifacts that were then deleted, and **the folding was not verified against code**. The evidence
is that roughly a third of all findings in this review are documentation defects, and they share
one shape — a claim that was true of the plan, or true at fold time, asserted as current truth.

Three sub-patterns, each with its own risk:

1. **Stale `file:LINE` citations** — the repo's own folding rule requires every folded fact to be
   proven from code with a citation, and a large number of those citations no longer resolve, one
   past end-of-file (**F-78**, **F-24**). The proof mechanism the rule depends on has decayed,
   which makes every *other* folded claim harder to trust.
2. **Absolute rules with counter-examples in the same folder** — e.g. the Writes README's "every
   `SaveChangesAsync` goes through a translating helper" (**F-08**), which is violated in the very
   class the README holds up as the exemplar.
3. **Claims that document away a real defect** — the most dangerous class, because the README makes
   the code look intentional. `Controllers/README.md:19` asserts the Abwab surface "must not reach
   production before a write policy attaches" while that surface is live and unauthenticated
   (**F-05**).

Only one README claim needs a human decision rather than an edit: **Q-01**.

**On the review's own instruction to say when the code is right and the README is wrong:** that is
the majority verdict here. Of the documentation findings, the correction is to the documentation
in nearly every case — the Abwab code is in materially better shape than its documentation. The
two exceptions where the *code* should move are **F-37** (the README states the correct contract —
"fails closed to the defaults" — and `section` is the one key that does not honour it) and
**F-05**, where neither side is right: the README states a constraint that was already broken and
the code lacks the protection, so the honest correction is to record the real posture and let the
next feature close it.
---

### 8.9 The five `features/words/` surfaces — **PASS**, stated explicitly because they were named in scope

The review's scope list named "the five `features/words/` surfaces deliberately affected by the
sticky-navbar change" as an item in its own right, so the verdict is recorded here rather than
left among the appendix entries in §5.

The five are the explorer detail panels — `lemma-details-panel`, `root-details-panel`,
`stem-details-panel`, `word-type-details-panel` and their sibling — identified by grepping
`features/words/` for the properties the sticky navbar could have disturbed. All three checks
came back clean:

- **No sticky-offset or scroll collision.** `grep -rn "sticky|100dvh|100vh|scroll-margin|
  scroll-padding" src/app/features/words/` returns **zero hits** — none of the five declares a
  sticky offset, a viewport-height budget or a scroll margin, so there is nothing for the sticky
  navbar to collide with.
- **No z-index collision.** None of the five declares a z-index of its own; the single
  `--qd-z-popover` consumer in the area resolves through the scale.
- **Chrome-inerting and focus trapping are correct in all five.** Each binds `qdModalScrollLock`
  and each binds its focus trap *conditionally*, as `features/words/README.md` requires.

One related result worth stating alongside them: the sticky navbar did **not** invalidate the
explorer pages' viewport-height budget, because `mushaf` is the only route that opts into the
page-scroll shell layout (`app-shell.component.ts:41` reads `shellLayout`;
`features/mushaf/mushaf.routes.ts:14` is its only writer).

The Abwab-owned defects found in this area are in the navbar and z-scale themselves (F-40 through
F-42, F-75 through F-77), not in the words surfaces the change reached into.

---

## 4. CROSS notes (raw input to area 8 — now consumed)

These are the one-line suspicions recorded while working areas 1–7, kept verbatim as the audit
trail behind §3b. **They are raw material, not findings**: the ones that survived adjudication are
written up in area 8 above, several were checked and cleared there (the `-closed` restore subject,
the URL⟺cache set difference), and a few are performance observations parked for the separate
performance review. Do not read this section as a second findings list.

- **C-00** — The review scope lists `conditional-request.ts` under `core/`. Only one copy
  exists, at `Frontend/quran-dashboard-ui/src/app/features/abwab/data-access/conditional-request.ts`
  (5 lines); there is no `core/` copy. Scope-list drift, not yet a code finding — confirm in
  area 8 that no second conditional-request path exists under another name.

### From the frontend pass

- **[AREA 5]** PERFORMANCE (out of scope, one line as instructed): `AbwabSnapshotFacade.snapshot` rebuilds the whole tree via `buildAbwabTreeSnapshot` on every `rawTree` change (abwab-snapshot.facade.ts:26-29), and `AbwabPageOverlaysController.selectedDoor` allocates a fresh `AbwabDoorDto` on every `byId()` change (abwab-page-overlays.controller.ts:27-45) — hand to the performance pass, not measured here.
- **[AREA 5]** COMPONENTS AREA: the bulk-count chip interpolates a bare count into Arabic copy — `<span>{{ bulkCount() }}</span> {{ bulkCountSuffix }}` (abwab-side-panel.component.html:88-90) with `bulkCountSuffix: 'باب محدد'` (abwab.labels.ts:119) renders «3 باب محدد». The feature README's own rule at README.md:838-841 says «Do not interpolate a bare count into new copy — «سيتم أرشفة 1 بابًا» is wrong Arabic and this product is Arabic-first», and `countPhrase` already exists for exactly this. Belongs to the components reviewer.
- **[AREA 5]** COMPONENTS/PAGES AREA: `AbwabDoorModalComponent.saved` is typed `output<AbwabDoorDto>()` (abwab-door-modal.component.ts:33) but emits `outcome.data`, which the write controller's cast permits to be null (see my LOW finding on abwab-write.controller.ts:182). Today the page handler ignores the payload (`(saved)="onDoorModalSaved()"`, abwab-page.component.html:208) so nothing dereferences it — a future consumer would.
- **[AREA 5]** PAGES AREA: `AbwabPageComponent` is over the 400-line hard threshold for component TS (FRONTEND_STRUCTURE.md:84-86), which README.md:45-53 acknowledges explicitly and names the next split seam. Recorded so the pages reviewer does not re-derive it; not a finding of mine.
- **[AREA 5]** TEST COVERAGE GAP (feeds my MEDIUM on the shareReplay race): no spec anywhere exercises overlapping fetches. `grep -n "unsubscribe|pendingRequest|shareReplay|race|cancel" state/abwab-snapshot.facade.spec.ts state/abwab-templates.facade.spec.ts` returns nothing, and there is no spec covering a section switch while bulk mode is on in abwab-selection.store.spec.ts either — the two riskiest behaviors in this area are both untested.
- **[AREA 5]** BACKEND NOTE (informational, no finding): `TemplatesListETag()` and `TemplateETag(id)` share one `_templatesGeneration` counter (AbwabCacheGeneration.cs:18,21), so editing template A invalidates template B's held validator too. Conservative, correct, costs one extra 200 — recording it because the frontend holds a per-id validator (abwab-templates.facade.ts:22) and a reader could wrongly infer per-id server-side generations.
- **[AREA 5]** No Quran-data-safety concern found anywhere in this area. `representativeAyahText` is carried verbatim through both builders (abwab-tree.builder.ts:45; abwab-templates.models.ts:69), is never searched, normalised, trimmed, or defaulted, and no fallback anywhere fabricates ayah text — `toWireFields` trims only the authoring fields and nulls empties (abwab-templates.controller.ts:116-121), which is user-authored metadata, not source data.
- **[Area 6b]** For the state-area reviewer, NOT mine: `state/abwab-tree.builder.ts:35-38` builds the archived partition with `build(d, 0, true)`, and the `includeArchivedChildren || !child.isArchived` filter therefore applies NO filter at all on that branch. Today it is safe only because the backend cascades archiving over the whole subtree (`EfAbwabDoorsWriter.cs:331-334`). If any path ever leaves a live door under an archived parent, the archive view will render it as archived. The component under my review is correct; the latent hole is in the builder.
- **[Area 6b]** For the state/URL reviewer: `README.md:530-533` states that a URL-driven close bypasses the door and sections modals' unsaved-changes confirm. I confirmed the gesture side (`requestClose()` at abwab-door-modal.component.ts:109 and abwab-sections-modal.component.ts:277) but did not review the URL path, which lives in `state/abwab-modal-url.controller.ts`.
- **[Area 6b]** Performance suspicion only, not measured and not pursued per scope: `abwab-door-picker.rows()` and `abwab-move-picker.destinationRows()` re-walk the whole live tree on every keystroke of their search inputs (abwab-door-picker.component.ts:68-92, abwab-move-picker.component.ts:70-101).
- **[Area 6b]** Not filed as a finding — the dirty-close guard is deliberately scoped to the three modals with text fields (door / template-node / sections `requestClose()`), while the copy, relations and move pickers discard a picked-doors draft on backdrop click or Escape with no confirm (abwab-template-copy-modal.component.html:2,14; abwab-relations-modal.component.html:2,14; abwab-move-picker.component.html:2,14). All six close on backdrop click, so the contract-inconsistency trigger is not met, and README.md:530-533 names door+sections specifically. Recording it so the cross-slice pass can decide whether losing a multi-door selection deserves the same guard.
- **[Area 6b]** Reversal watch results for this area: #3 (template apply copies children, never the root) is CLEAN end to end — label, description, preview, the gating `hasElements`, and the request body all agree; no remnant of root-copying survives in code, copy, styles or the labels file. #2 (relations flag always rendered, dimmed at zero, clickable) is a tree/cards concern outside my dir, but its consequence lands here correctly: opening the modal at `anchorRelationCount() === 0` issues NO request and goes straight to the empty state (abwab-relations-modal.component.ts:249). #1 (reveal does not clear `q`) and #4 (nav dropdown) touch nothing in this directory.
- **[Area 6b]** No Quran-data safety issue found in this area. None of these ten components touches Quran text, morphology, identity, alignment, or counting scope over ayat; `representativeAyahText` is carried as an opaque authoring string through `abwab-door-fields-form` and is never parsed, normalized, or rendered with a Quran font by any component under review.
- **[Area 6a]** CROSS-SLICE: three near-identical inline order editors, two blur semantics. abwab-tree.component.html:68 cancels on blur, the sections modal cancels on blur (README.md:245-247), abwab-template-tree.component.html:39 still commits on blur. The doors tree's reversal was applied to two of the three surfaces; the workshop was missed. A per-slice review could not see this because the workshop's editor shipped in a different slice from the reversal.
- **[Area 6a]** CROSS-SLICE: three count-badge surfaces, two labelling disciplines. The tree gives every badge a scope-declaring Arabic aria-label (abwab-tree.component.html:86,96,106) and the toolbar gives every tab count one (abwab-toolbar.component.html:8,25); the cards ship two bare digits with no accessible name and no scope word (abwab-cards.component.html:49,53), and the side panel interpolates a bare count into Arabic prose (abwab-side-panel.component.html:88-89). Whichever surface a future count lands on decides which discipline it inherits.
- **[Area 6a]** CROSS-SLICE: the ux-slice-l "a zero-match query must not read as no data" fix was applied to the tree only. Archive (abwab-page.component.html:116-117) still collapses into «لا توجد أبواب مؤرشفة.» and cards (abwab-page.component.html:126-141) renders no state at all. Same defect class, two surviving instances, both listed above as separate findings because they need different corrections.
- **[Area 6a]** CROSS-SLICE: keyboard parity between the two views of the same data is uneven. The tree carries a full ARIA tree with roving tabindex and an RTL-mirrored key model (abwab-tree.component.html:15-34, abwab-tree-keyboard.controller.ts:89-113); the cards view of the same doors has no focusable element at all (abwab-cards.component.html:30) despite `view` being a URL-level peer (README.md:388).
- **[Area 6a]** PERFORMANCE (out of scope, one line as instructed): `displayRoots()` and `archiveSearchResult()` are computed on every snapshot/query change even when the active view never reads them (abwab-page.component.ts:140-143,191-197), and `AbwabTreeComponent.nodesById()` rebuilds a full Map of the visible subtree on every `roots()` identity change (abwab-tree.component.ts:71-79). Not measured, not recommended — for the performance pass.
- **[Area 6a]** STYLE-ONLY (not filed as a finding): abwab-page.component.scss:31 uses the physical `min-width: 0` where the rest of that file uses logical `inline-size`/`min-block-size`; in a horizontal writing mode this is behaviourally identical in RTL, so it is a vocabulary inconsistency rather than an RTL defect.
- **[Area 6a]** LEDGERED ELSEWHERE, not re-reported: `features/abwab/components/abwab-template-tree/` and `pages/abwab-templates-page/` carry no `.spec.ts`, which is why the blur-commit reorder above is unpinned — already recorded as row 9 of docs/TESTING_DEBT.md (`abwab-templates`). The template tree's right-click / ContextMenu / Shift+F10 paths are on the same row.
- **[AREA 7b]** THE FIVE WORDS SURFACES — identified as `root-details-panel`, `lemma-details-panel`, `stem-details-panel`, `word-type-details-panel`, `word-drilldown-modal`. Method: two independent corroborating statements. (1) `.architecture/UI_STYLE_SYSTEM.md` §17 "Chrome-inert rule": "This is an intentional behavior change on five shipped words surfaces nobody asked about, accepted deliberately" — and the inert half shipped in the same commit as the sticky navbar (`674653e9 feat(ux-slice-b2): stick the navbar and make it inert under dialogs`). (2) `features/words/README.md:63-65` names the same five by file: "the five mobile detail drawers (`root`/`lemma`/`stem`/`word-type-details-panel`, `word-drilldown-modal`)". Confirmed by `grep -rn qdModalScrollLock src/app/features/words/`, which returns exactly those five. ANSWER TO THE FOUR FAILURE MODES: none of them has a wrong sticky offset (none declares `position: sticky` or any navbar-derived offset), none has a z-index collision (all render `.qd-modal-backdrop` at rung 50 vs the navbar's 45), none has an obscured focus target (all five bind `[cdkTrapFocus]` conditionally and all five hold the lock, so the navbar is inert behind them), and none has a broken scroll-into-view (their explorer tables scroll inside a CDK virtual viewport / an internal `body.scrollTop`, never the document — `roots-table.component.ts:224-233` and siblings).
- **[AREA 7b]** A SECOND set of five words surfaces exists in the same slice and is easy to confuse with the above: the five `.qd-explorer-frame` call-sites (`lemmas-`/`roots-`/`stems-`/`word-types-explorer-page.component.html:2` + `unique-words-page.component.html:2`), named by `styles/README.md:36-39` and `features/words/README.md:41-45` as "the five existing explorer call-sites" kept working by the `.qd-page-frame` alias (Slice B2 T701/T702). They are unaffected by the sticky navbar for the reasons in verifiedClean. If a later pass sees "five words surfaces" in a doc, check which five is meant — the discriminator is whether the sentence is about the chrome-inert rule or the page-frame rename.
- **[AREA 7b]** `--qd-z-modal: 51` (`_tokens.scss:146`) has NO consumer: `grep -rn -- '--qd-z-' src/` returns it only at its declaration. `.qd-modal` (`_components.scss:580-586`) stacks inside its backdrop with no z-index of its own. §4 already acknowledges this ("`--qd-z-modal` has no consumer yet") — recorded here so a later pass does not re-discover it as a defect.
- **[AREA 7b]** PERFORMANCE (out of scope, one line as instructed): `openMenuKey` and `mobileOpen` are plain mutable fields, not signals (`top-navbar.component.ts:31-32`), and the component has no `ChangeDetectionStrategy.OnPush`; it works only because `app.config.ts:40` still uses `provideZoneChangeDetection`. A zoneless migration would silently break the dropdown.
- **[AREA 7b]** `--qd-explorer-chrome-block-size: 14rem` (`_words-explorer-layout.scss:77`, and `12rem` at `:143`) is a hand-measured viewport budget that does NOT reference `--qd-navbar-block-size`, unlike every other viewport-relative figure the sticky-navbar work re-based. It is correct today (sticky does not change the navbar's flow box), but it is the one remaining magic number in that family and will need re-measuring by hand if the navbar height ever changes.
- **[AREA 7b]** `.qd-shell-viewport { min-height: 100vh }` (`_layout.scss:10`) uses `100vh` while every other viewport budget in the app uses `100dvh` (`_tokens.scss:77`, `_words-explorer-layout.scss:116,145`, `_components.scss` modal `92dvh`, `abwab-page.component.scss:2`). Pre-existing and untouched by the Abwab series, but it is the shell that all of them sit inside, and on mobile browsers with a collapsing toolbar the two units disagree — exactly where the five words drawers switch to their modal form.
- **[AREA 7b]** No remnants found of the pre-dropdown SINGLE-LINK «الأبواب» nav entry: `nav-items.ts:16` still declares the flat item (correctly — `route-paths.ts:22` derives `ABWAB_ROUTE_PATH` from it), and `nav-menu.ts:24-32` layers children on top without mutating it. The only surviving pre-dropdown remnants are the hand-rolled `more` branch and the dead `.more-dropdown` class, both filed as findings.
- **[AREA 7b]** Reversals 1–3 (reveal-in-tree no longer clearing `q`; the always-rendered dimmed relations flag; template apply copying children not the root) have no footprint in this area — no core, styles, or words file in scope mentions them.
- **[AREA 7b]** No Quran text, morphology, identity, alignment, or counting-scope surface appears anywhere in this area. The only counted things in scope are nav item groups (`primaryItems`/`moreItems`/`actionItems`, `top-navbar.component.ts:27-29`), which are filters over `NAV_MENU`'s `group` field and carry no user-facing count label. Counting-scope discipline is not exercised here.
- **[Area 7a]** REVERSAL #2 (relations flag always rendered, dimmed at zero, clickable) — the shared-surface analogue is `.qd-tabs__count--empty` (_components.scss:230-232, added by 30f35b9b `feat(ux-slice-f)`), used only by abwab-toolbar.component.html:15 and :32. There is NO equivalent `--empty` modifier for `.qd-chip__count` (_components.scss:211-223 vs the chip's :280-292), so whoever reviews the tree's relations flag should check whether it hand-rolls its dimming or reuses the tabs modifier — and should note my contrast finding on the tabs one applies identically to any copy of the `opacity: 0.5` idiom on a live control.
- **[Area 7a]** COUNTING SCOPE, for the abwab-toolbar reviewer: the tab badge is named `totalRootCount()` / `rootCountFor(section.id)` (abwab-toolbar.component.html:15, :32) while the accessible label beside it is `allDoorsCountAriaLabel` / `sectionCountAriaLabel` (:8, :25). "Root doors" and "all doors" are different scopes; UI_STYLE_SYSTEM.md:753 calls it "item 19's root-count badge" and abwab/README.md:98 calls it "root-count badge". Verify the Arabic label text actually says roots, not doors.
- **[Area 7a]** PERFORMANCE (out of scope, one line as instructed): `qd-context-menu` does not reset `placement()` to null when `position` changes, so a reopen paints one frame at the previous placement before `afterRenderEffect` re-measures (context-menu.component.ts:42-55); and the placement is not recomputed on window resize/scroll while the menu is open. Not measured, not pursued.
- **[Area 7a]** For the abwab-door-picker reviewer: the pick input carries `(click)="$event.preventDefault()"` alongside `(change)="onRowChange(row)"` (abwab-door-picker.component.html:48-50). Because preventDefault on a checkbox/radio click cancels the state change, the `change` event never fires, so `onRowChange` (abwab-door-picker.component.ts:136-141) appears to be dead — toggling actually happens via the click bubbling to the row `<div (click)="togglePicked(row)">` (:20). Worth confirming the single/radio path really works before someone deletes the row handler.
- **[Area 7a]** For the abwab-relations-modal reviewer: that modal never passes `[status]` to `qd-abwab-door-picker` (abwab-relations-modal.component.html:135-147), so the picker sits on its default `'ready'` and its loading/error/empty branches (abwab-door-picker.component.html:60-80) are unreachable there — a doors-load failure inside the relations modal shows the picker's plain empty state, not an error. Check whether that is intended.
- **[Area 7a]** `qd-tabs` `layout='grid'` (tabs.component.ts:31, _components.scss:165-169) and `qd-chip`'s `labelClickable` (chip.component.ts:28-32) each exist for exactly one Abwab consumer today (abwab-move-picker's section strip; the relation chips). Neither leaks a feature concept into the primitive's API, so I did not raise them — but a cross-slice reviewer counting single-consumer additions to shared surfaces should know they are the two.
- **[AREA 7c]** COVERAGE, stated honestly. features/abwab/README.md lines 1–545 (header, status note, render chain, URL contract) audited exhaustively — every checkable assertion has a confirming file:LINE or a finding. Lines 546–724 (first half of Gotchas) audited exhaustively. Lines 725–961 (second half of Gotchas) audited on ~28 named claims and spot-checked elsewhere; the claims I did NOT independently verify are listed in openQuestions. Lines 963–1024 (e2e + reversals + related) audited exhaustively. UI_STYLE_SYSTEM.md audited only for the named sections (§ qd-modal/--fixed/--wide :1067-1150, Header over badge columns :1152-1195, Reveal highlight :1320-1345, Viewport reservation :1372-1410, Sticky app chrome :1411-1466, z-scale :168-185, qd-tabs count meta :753-755, qd-state reserve :854-876). shared/README.md, core/README.md and styles/README.md audited only on their abwab-relevant lines (shared :17, :42, :56, :63; core :65-67, :108-110; styles :40) — all confirmed. docs/contracts/frontend-shell.md is a 16-line pointer index with no abwab assertions to audit; the finding there is the absence of an Abwab page, not a wrong claim.
- **[AREA 7c]** THE CITATION BLOCK IN 'Decisions that reversed mid-series' (README:987-1014) SHOULD BE TREATED AS UNVERIFIED AS A WHOLE. Five of its six code citations are stale, one (`abwab-tree.component.scss:257`) points past end-of-file in a 228-line file, so it cannot ever have been valid against the current file. The behaviour each entry asserts is correct in every case — I verified all four reversals independently — but the anchors were written against an earlier revision and re-anchored by nobody. Any later pass that treats those line numbers as evidence will be reading the wrong lines.
- **[AREA 7c]** ALL FOUR KNOWN REVERSALS HOLD IN CODE, with no surviving remnant of the old behaviour found anywhere in components, styles, labels, tests or READMEs. Specifically: (1) no `q: null` or `q: ''` appears in the reveal patch (abwab-page.component.ts:409-418); (2) the relations flag is outside every `@if` (abwab-tree.component.html:112-124) and no `relationCount > 0` guard survives; (3) no code path copies the template root itself (EfAbwabTemplateApplyWriter.cs:87-99); (4) `abwab.routes.ts` carries no 'not the sidebar' text and nav-menu.ts:5-22 is the live source. The one stale remnant is textual: README:1011 quotes «القوالب» where the dropdown ships «قوالب الأبواب».
- **[AREA 7c]** PERFORMANCE — one suspicion, not pursued per scope: `AbwabSnapshotFacade.snapshot` (abwab-snapshot.facade.ts:26-29) recomputes the entire `buildAbwabTreeSnapshot` walk, including every `byId` entry and all three memoized per-node counts, on any `rawTree` change; several consumers (`countLiveAbwabDoors` over `byId.values()`, the relations modal's own `nodesById` walk at abwab-relations-modal.component.ts:175-183, the tree's `nodesById` at abwab-tree.component.ts:71-79) then re-walk that output. Left for the performance pass.
- **[AREA 7c]** COUNTING-SCOPE DISCIPLINE PASSES in this feature and is unusually well handled. Four distinct scopes are kept apart with distinct label vocabularies: live doors at any depth (`countLiveAbwabDoors`, «كل الأبواب» stat), live doors in one section at any depth (`doorsInScopeCount`, «أبواب هذا التبويب»), live ROOT doors per section (`rootCountBySectionId`, `ROOT_DOOR_FORMS` aria phrase), and per-row direct/total/depth badges (`rowChildCountAriaLabel` «تحته مباشرة» vs `rowDescendantCountAriaLabel` «تحته في كل المستويات» vs `rowDepthAriaLabel`). README:837-840 explicitly forbids asserting agreement between the tab badge and the stats. The archive-view and cards deliberately render no relation flag and no child badge, both derived (backend hides relations whose endpoint is archived — Reads/Abwab/README.md; archived doors have zero live children by construction).
- **[AREA 7c]** NO QURAN-DATA SAFETY ISSUE FOUND. Nothing in this feature touches Quran text, morphology, identity, alignment or counting scope. `representativeAyahText` is free admin-authored text explicitly disclaimed in the UI (abwab.labels.ts:134), carries no surah/ayah identity, and is never validated against or joined to canonical data.
- **[AREA 7c]** README:364-366's 'rename pin' is genuinely double-bound as claimed: the client sentence is at features/abwab/README.md:360-366 and the server half really does exist at Backend/infrastructure/.../Persistence/Reads/Abwab/README.md:132-134 ('Relation lists must be evicted when a door is **renamed**…'). This is the one cross-repo binding in the audit that survived verification intact — worth noting because the same paragraph's neighbour (the TESTING_DEBT pointer at :960) did not.
- **[AREA 7c]** A dangling reference to the deleted design comp `abwab-tree-concept.html` survives at README:118 and :138 (with line numbers :114, :436-443, :107), and in three spec/e2e describe strings. Per the brief this class of finding is already recorded elsewhere and is not re-reported here; noted only so a later pass knows those two README lines cite a file that `find` cannot locate anywhere in the repo.
- **[AREA 7d]** Quran-data safety: NOT APPLICABLE to this surface set. Nothing reviewed here touches Quran text, morphology, ayah identity, alignment, or ayah-counting scope — the only Quranic surface in range is the ayah-text authoring field inside abwab-door-fields-form, and this pass changed nothing about how it renders. No fabricated counts, no silent corrections, no hidden unknowns found.
- **[AREA 7d]** PERFORMANCE (out of scope, one line as instructed): abwab-tree.component.ts:313 and abwab-archive-view.component.ts:118-120 resolve the focus target by querySelector on a data-testid inside a queueMicrotask on every arrow key. Not measured, not recommended on — flagged for the performance pass only.
- **[AREA 7d]** For the performance pass: shared/ui/context-menu/context-menu.component.ts:42-55 measures the menu box in an afterRenderEffect every time position() changes. Noted, not evaluated.
- **[AREA 7d]** MISSING TEST COVERAGE, consolidated for whoever schedules it: (a) the ContextMenu/Shift+F10 keyboard path is opened by e2e/abwab-url-and-a11y.e2e.ts:230-243 but nothing asserts where focus goes after opening or dismissing; (b) app.nested-layers.spec.ts:222 pins 'exactly one focus trap enabled' for the words drawer/dialog pair only — there is no abwab case, so the relations-vs-sections trap divergence is unpinned in either direction; (c) no spec covers focus after a successful door restore, though abwab-page.component.spec.ts:305-320 covers the identical shape for archive; (d) no spec covers Escape while a dirty-discard strip is open; (e) abwab-template-tree has no spec file at all, so its blur-commits order editor is unpinned.
- **[AREA 7d]** Not re-reported per the task's exclusion: README:118 cites abwab-tree-concept.html:114 and :436-443, a deleted design artifact. Already recorded elsewhere.
- **[AREA 7d]** Two smaller ARIA slips not worth a finding each: abwab-move-picker.component.html:28 points aria-controls at destinationsId, but when no section is picked that id lands on a role="region" (:42-44) rather than the role="tabpanel" it carries otherwise (:50) — a tab controlling a non-tabpanel. And abwab-door-picker.component.html:18 puts aria-disabled="true" on a role-less <div>, where it is inert; the real signal for excluded rows is the excludedTag chip at :53-54, which does carry text.
- **[AREA 7d]** The abwab-templates-page composes qd-abwab-announcer (abwab-templates-page.component.html:11) and its controller announces every write success — so the workshop is the surface that gets announcement right and /abwab is the one that does not. Worth knowing which way to converge.

### From the backend pass

- **[Area 4]** FRONTEND KEY-SET COMPARISON (the one the cross-slice pass must run): the backend tree cache key has ZERO inputs and the tree route has ZERO parameters. If the frontend varies its cached snapshot by ANY key — archive on/off, section filter, search term, sort, paging — that key is invisible to the server and the ETag will 304 across it. See the evidence section's key-composition block for the exact expressions.
- **[Area 4]** Reads/Abwab/README.md:125-137 says the frontend caches per-door relation lists keyed on the TREE ETag and skips the request entirely when a door's snapshot RelationCount is 0. Two things for the frontend area to confirm against code: (a) that the key really is the tree ETag and not something narrower, and (b) the 'rename pin' at :132-137 — a door rename must evict relation lists because a list embeds the partner's name; it works today only because every door write bumps the tree generation.
- **[Area 4]** Cache-Control: no-store (ConditionalGet.cs:15) means the browser HTTP cache never stores these responses, so If-None-Match can ONLY ever be sent by the app's own facade explicitly. If any frontend code relies on the browser revalidating automatically, it silently never revalidates. Deliberate and documented at API_GUIDELINES.md:149-152.
- **[Area 4]** ConditionalGet.cs:18-20 treats If-None-Match: '*' as a NON-match and answers 200, deviating from RFC 9110 (where '*' matches any current representation). Deliberate fail-open, documented at API_GUIDELINES.md:161-162, safe direction (costs a body, never a stale representation). Not raised as a finding.
- **[Area 4]** PERFORMANCE (out of scope, one line as instructed): a deleted template's abwab:template:{id} entry is never removed from IMemoryCache — it is never served again (generation mismatch, CachedAbwabTemplatesReader.cs:37) but it is also never evicted, since Reads/Abwab/README.md:165-168 states there is no expiration on any entry. Bounded by template count.
- **[Area 4]** PERFORMANCE (out of scope, one line): there is no single-flight on the abwab:tree key — Reads/Abwab/README.md:172-175 says CacheLoadGate is deliberately not reused, so N concurrent cold requests each run the full tree query.
- **[Area 4]** ANOTHER AREA'S FINDING, one line, not chased: AbwabSectionDeleteResult (IAbwabSectionsWriter.cs:16-21) has no StaleVersion member and IAbwabSectionsWriter.DeleteAsync takes no expectedVersion, yet AbwabSectionsController.cs:74-75 has a DeleteSectionOutcome.StaleVersion branch — it looks unreachable.
- **[Area 4]** ANOTHER AREA'S FINDING, one line, not chased: AbwabDoorsController.cs:108-109 carries a two-line comment that is subject to the same workspace comment rule as the ones I did report.
- **[AREA 1]** ETag normalization drift: ConditionalGet.cs:40-42 strips a `W/` weak-validator prefix before comparing, but API_GUIDELINES.md:161 describes the rule as "exact ordinal match against a member of the request's list" with no mention of weak-validator handling. The code is arguably more correct than the doc; the doc is what a second implementer would follow.
- **[AREA 1]** Six of the thirteen Abwab request bodies live in their own `*Body.cs` file (DeleteDoorBody, EditDoorBody, MoveDoorBody, ReorderDoorBody, RestoreDoorBody, RenameSectionBody, ReorderSectionBody), while the other six are declared inside their `*Command.cs` (AddDoorRelationsCommand.cs:6, CreateTemplateCommand.cs:9, ApplyTemplateCommand.cs:5, AddTemplateNodeCommand.cs:11, EditTemplateNodeCommand.cs:10, ReorderTemplateNodeCommand.cs:5). Purely a placement inconsistency — worth one line for whoever reviews Application-layer structure.
- **[AREA 1]** Four Abwab actions bind an Application command directly off the wire (`[FromBody] CreateDoorCommand` AbwabDoorsController.cs:27, `BulkMoveDoorsCommand` :134, `BulkArchiveDoorsCommand` :164, `CreateSectionCommand` AbwabSectionsController.cs:19) while the other nine bind a `*Body` and construct the command. Not forbidden by API_GUIDELINES §8 (which only bars Domain types), but it makes the Application command the public wire contract for those four.
- **[AREA 1]** `RepresentativeAyahText` has no length cap or format validation anywhere on the write path — it is unbounded admin-authored text. Not a Quran-safety issue (nothing generates or corrects it), but a payload-size / input-hardening note for whoever owns validation next.
- **[AREA 1]** `DELETE api/abwab/doors/{id}` requires a JSON request body (`[FromBody] DeleteDoorBody` AbwabDoorsController.cs:184). Legal, and the .NET client sends it, but DELETE bodies are dropped by some proxies/CDNs — a deployment-topology risk rather than a code defect. Its sibling `DELETE api/abwab/sections/{id}` takes none, which is the asymmetry behind finding 3.
- **[AREA 1]** Performance (out of scope, one line as instructed): `EfAbwabTreeReader.GetTreeAsync` loads every door and alias row and runs three separate `MaxAsync` queries for the snapshot version (:84-92) on a table already at 785 production rows.
- **[AREA 1]** Four `plan §N` citations point at deleted planning artifacts and survived the documentation passes: Controllers/README.md:19, Reads/Abwab/README.md:32, Writes/Abwab/README.md:161, and AbwabDoorsController.cs:108. The Writes and Reads ones are outside this area but are the same defect class as finding 7.
- **[AREA 1]** No Abwab action carries `[ProducesResponseType]` of any kind, so the exported spec documents exactly one status per route (the inferred 200/201) and no error codes at all — acknowledged for errors at Controllers/README.md:136-140, but it is also why the 204 mismatch in finding 1 went unnoticed.
- **[AREA 3c]** PERFORMANCE (out of scope, one line as instructed): LoadChildrenByParentAsync (EfAbwabDoorsWriter.cs:614-624) and MaintainGlobalOrderAsync (:679-688) each read the whole abwab_doors table on every root-affecting write; the writes README:183-185 declares this an accepted cost, so it is a stated posture, not a discovery — the performance pass owns whether it still holds at 785 doors.
- **[AREA 3c]** Two empty leftover directories exist with no files and no tracked content: Backend/infrastructure/QuranDashboard.Infrastructure/Files/Abwab/LegacySeed/ and Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Abwab/, alongside a staged resources/import-sources/abwab-legacy-export. They contain no door-creating code (verified), so the 'only two door-creating paths' invariant is unaffected — but the empty scaffolding suggests a legacy import path that was planned and dropped, which is worth confirming is genuinely dead rather than pending.
- **[AREA 3c]** The section-resolution contract is deliberately three-way inconsistent by design and the README forbids harmonizing it (create REJECTS a disagreeing section, restore REJECTS it, move/bulk-move silently IGNORE targetSectionId when a parent is set — EfAbwabDoorsWriter.cs:557-563 does not even validate that the ignored section exists). This is correct per Writes/Abwab/README.md:160-163, but it means MoveDoorOutcome has no SectionParentMismatch variant while CreateDoorOutcome and RestoreDoorOutcome do — a shape difference a cross-slice contract pass should see stated rather than rediscover.
- **[AREA 3c]** A relation CHECK-constraint violation (SqlState 23514) is caught by no save helper in EfAbwabRelationsWriter.cs — SaveTranslatingDuplicateAsync (:117-127) filters on 23505 alone — so any future write path that bypasses Math.Min/Math.Max surfaces as a 500. Unreachable today; folded into the invariant-4 coverage finding rather than reported separately.
- **[AREA 3b]** PERFORMANCE (out of scope, one line as instructed): EfAbwabTreeReader.GetLiveRelationCountsAsync (:62-70) materializes every visible relation row per snapshot read rather than aggregating in SQL, and EfAbwabDoorsWriter.ToDtosAsync (:729-738) issues one alias query per door in a bulk-move response — both bounded by the cache and by admin scale, both for the performance pass, not this one.
- **[AREA 3b]** QURAN-DATA SAFETY: NOT APPLICABLE and verified so — nothing in the Abwab area touches Quran text, morphology, identity, alignment, or counting scope. AbwabDoor.RepresentativeAyahText is free-text authored by the admin (AbwabDoorConfiguration.cs:176-177), never joined to any Quran table, never validated against one, and never fabricated by any code path; the template copy carries it verbatim (EfAbwabTemplateApplyWriter.cs:155). No finding here is a Quran-safety finding.
- **[AREA 3b]** PRODUCTION-CORRUPTION: none found. The three write-side conditions that could in principle corrupt state are all already recorded rather than latent — the duplicate-OrderValue window in EfAbwabSectionsWriter (CreateAsync's count(live)+1 at :12 alongside DeleteAsync's non-resequencing delete at :63-67) is docs/TESTING_DEBT.md rows F1/F2 and Writes/README.md:99-103; the bulk-move check order is C1. The mandatory-section invariant, the archive/restore claim symmetry, and the global-order invariant all hold under code reading.
- **[AREA 3b]** CROSS-AREA (Frontend): Reads/README.md:127-131 and :132-137 make binding claims about Frontend/quran-dashboard-ui/src/app/features/abwab/state/abwab-relations.controller.ts (per-door cache keyed on the tree ETag, request skipped when RelationCount is 0, and the rename-pin requirement). The file exists and does adopt a validator (:33-36), but the frontend half of those claims is outside this area's scope — flag for whoever audits the Abwab frontend README.
- **[AREA 3b]** CROSS-AREA (Tests): docs/TESTING_DEBT.md:159 row J1 carries the same wrong 816-line figure as Writes/README.md:53. Whoever fixes one must fix the other, or the ledger re-seeds the stale number into the next README.
- **[AREA 3b]** CROSS-AREA (Controllers README, non-Abwab): Controllers/README.md:127-131 records that the committed swagger.json 'stayed stale for several commits' after the XML-doc strip. I did not verify whether it is current now — that is a check-api-contract question for whoever audits the API contract artifacts, not a README-fidelity one.
- **[AREA 3b]** PATTERN worth naming for the cross-slice pass: every MEDIUM here is a universally-quantified sentence — 'Every SaveChangesAsync', 'nothing else does', 'have none of either', 'any refusal'. The Abwab READMEs are accurate wherever they describe a specific mechanism and unreliable wherever they close an enumeration. That is the same failure docs/TESTING_DEBT.md:163-166 already diagnosed for the frontend docs ('every hand-maintained enumeration in the long-lived docs had drifted'); the backend Abwab READMEs were not swept by that pass and carry the same defect.
- **[AREA 3]** PERF (do not pursue here): EfAbwabTreeReader.cs:15-17 loads every door row unfiltered on every snapshot; production has 785 doors.
- **[AREA 3]** PERF: EfAbwabDoorsWriter.cs:682-685 ResequenceGlobal reads and rewrites every live root on each root-affecting write; the Writes README:183-185 accepts this explicitly.
- **[AREA 3]** PERF: EfAbwabDoorsWriter.cs:729-738 ToDtosAsync issues one alias query per door, so BulkMoveAsync's response is N+1 in the batch size.
- **[AREA 3]** PERF: EfAbwabTemplateApplyWriter.cs:91-92 runs one CountAsync per target inside the copy loop.
- **[AREA 3]** PERF: EfAbwabDoorsWriter.cs:616-618 LoadChildrenByParentAsync reads (id, parent_id) for the whole table on every move/archive/restore.
- **[AREA 3]** CACHING (Area covering Infrastructure/Caching/Abwab): the whole invalidation scheme is per-process memory and is documented as single-instance-only (Reads README:181-189). Whoever reviews Railway scaling owns that, not this area.
- **[AREA 3]** CONCURRENCY (cross-area): docs/TESTING_DEBT.md row G1 records a known concurrent-apply order_value race in EfAbwabTemplateApplyWriter; the per-target CountAsync at :91-92 is that race's site and it is still open.
- **[AREA 3]** API (Area 2): AddDoorRelationsHandler.cs:66 is the only thing standing between an omitted direction and EfAbwabRelationsWriter.cs:137 silently storing 'target is broader'. If anyone ever adds a second caller of IAbwabRelationsWriter.AddAsync, that guard does not travel with it.
- **[AREA 3]** DOC LIFECYCLE: three of the drifted README facts (test-coverage claim, line cites, line count) are all the same failure — folded facts whose proofs were never re-checked after later slices moved the code. Worth one sweep rather than three fixes.
- **[Area 2]** AREA 3 (Infrastructure writes) — README-vs-code contradiction: Persistence/Writes/Abwab/README.md:33-35 asserts 'Every SaveChangesAsync in this folder goes through a translating helper — a bare save is how a raw EF exception reaches the global handler as a 500 instead of a 409.' Two bare saves exist: EfAbwabDoorsWriter.cs:47 (alias flush inside CreateAsync) and EfAbwabDoorsWriter.cs:81 (alias flush inside EditAsync). Harmless TODAY only because abwab_door_aliases has no unique index (AbwabDoorAliasConfiguration.cs:55 is a plain HasIndex(a => a.DoorId)) and AbwabDoorAlias carries no version token. A developer trusting the README who adds a per-door unique alias index would ship 500s where 409s are the contract.
- **[Area 2]** AREA 3 — stale file:LINE citation in a long-lived README: Persistence/Writes/Abwab/README.md:152 cites 'LoadChildrenByParentAsync (EfAbwabDoorsWriter.cs:699-701)'; the method is actually at EfAbwabDoorsWriter.cs:614-624, and lines 699-701 sit inside ReplaceAliasesAsync. The claim it supports (no DeletedAtUtc filter on the parent map) is TRUE at :616-618 — only the pointer is wrong.
- **[Area 2]** AREA 3 — stale measurement used as an architecture justification: Persistence/Writes/Abwab/README.md:53 says 'EfAbwabDoorsWriter is already 816 lines against that section's 600-line hard threshold, so hanging it there was never available.' The file is 767 lines (wc -l). Still over the threshold, so the conclusion survives, but the number is not recoverable from the code it describes.
- **[Area 2]** AREA 1 (API) — Controllers/README.md:27-28 states 'a stated section that no longer exists is a 404' for restore without qualification. That holds only for a ROOT restore (ResolveRestoreSectionAsync:461-465 → EnsureSectionExistsAsync → AbwabSectionNotFoundException → 404). For a CHILD restore, a stated section is compared against the parent's and never existence-checked (:451-459), so a non-existent stated section yields 400 SectionParentMismatch, never 404.
- **[Area 2]** AREA 1 — production comment citing a deleted planning artifact: AbwabDoorsController.cs:108-109 'An omitted scope lands on the enum's unmapped default (0, plan §6)'. Same pattern in long-lived READMEs: Reads/Abwab/README.md:31-32 ('feature plan §4'), :53 ('not stated verbatim in the feature plan'), Writes/Abwab/README.md:161 ('plan §4, §13.5'), Controllers/README.md:19 ('see feature plan §9/§10'). Every one of those facts IS provable from code; the citations are not.
- **[Area 2]** AREA 1 — Controllers/README.md:18-19 says the open write surface 'must not reach production before a write policy attaches'. Documentation only: the state itself is the known, accepted, out-of-scope item; the README sentence is what no longer describes reality and will mislead the next reader.
- **[Area 2]** PERFORMANCE (one line, not investigated per scope exclusion): EfAbwabDoorsWriter.MaintainGlobalOrderAsync:682-685 loads and TRACKS every live root door on every root-affecting write, including plain door creation (:38-41); Writes/Abwab/README.md:183-185 records this as an accepted cost.
- **[Area 2]** CROSS-SLICE LOGGING: BulkArchiveDoorsHandler.cs:25 logs '{count}' = archivedIds.Count, which is the whole archived SUBTREE (EfAbwabDoorsWriter.ArchiveSubtreeAsync:579-612 returns door + live descendants), while BulkMoveDoorsHandler.cs:27 logs '{count}' = doors.Count, the number of REQUESTED doors. Same field name, different denominators. Not a defect; a log-analysis trap.
- **[Area 2]** TEMPLATES (outside this assignment, recorded for whoever owns them): AbwabTemplate has no Name property (Domain/Abwab/AbwabTemplate.cs) yet IAbwabTemplatesWriter.CreateAsync takes a name and AbwabTemplateDto exposes one — the name is the root NODE's name (EfAbwabTemplatesReader.cs:56-57), documented at Reads/Abwab/README.md:109-114.
- **[Area 2]** NO ACTIVE PRODUCTION DATA CORRUPTION FOUND in this area. The two MEDIUMs are a wrong status code and a contract/placement inconsistency; neither writes bad rows. The 785 seeded doors are not at risk from anything in Application/Abwab or Application.Abstractions/Abwab.

---

## 5. Verified clean

Invariants and contracts checked that **held**. Recorded as results, not omissions.

**Read §8.5 first** — it is the consolidated verdict table (five invariants, URL/cache identity,
invalidation completeness), and it is the actual deliverable of this section. Everything below is
the per-area appendix behind it.

### Backend (areas 1–4)

- **[Area 4]** WRITE-SET / READ-SET COMPLETENESS (the headline check). For every writer, the tables it WRITES intersect only read-sets whose generation it bumps — proven both directions, no gap and no missing bump. Tree read-set = {AbwabSections, AbwabDoors, AbwabDoorAliases, AbwabDoorRelations}. Templates read-set = {AbwabTemplates, AbwabTemplateNodes}. Doors writer writes {AbwabDoors, AbwabDoorAliases} → bumps tree, correctly not templates. Sections writer writes {AbwabSections} → tree. Relations writer writes {AbwabDoorRelations} → tree. Apply writer writes {AbwabDoors, AbwabDoorAliases} (it only READS templates, AsNoTracking) → tree, correctly NOT templates. Templates writer writes {AbwabTemplates, AbwabTemplateNodes} → templates, correctly NOT tree. — Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Abwab/EfAbwabTreeReader.cs:10,15,19,63 (read-set); EfAbwabTemplatesReader.cs:10,17,21,34,41 (read-set); Writes/Abwab/EfAbwabTemplateApplyWriter.cs:20,27 (templates READ AsNoTracking) vs :91,98,121,165 (doors/aliases WRITE); EfAbwabTemplatesWriter.cs:18,36,111,217; EfAbwabDoorsWriter.cs:36,707; EfAbwabSectionsWriter.cs:22; EfAbwabRelationsWriter.cs:54
- **[Area 4]** All 21 interface methods across the five writer interfaces are forwarded AND bump. 21/21 forward, 21/21 bump. No method forwards without bumping. — InvalidatingAbwabDoorsWriter.cs bumps at :29,48,65,82,98,110,122,134 (8/8); InvalidatingAbwabRelationsWriter.cs:28,40 (2/2); InvalidatingAbwabSectionsWriter.cs:22,34,46,58 (4/4); InvalidatingAbwabTemplateApplyWriter.cs:25 (1/1); InvalidatingAbwabTemplatesWriter.cs:27,39,58,76,88,100 (6/6)
- **[Area 4]** All five decorators are actually REGISTERED and in the request chain — the interface resolves to the decorator, never the bare Ef writer. — Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/AbwabDependencyInjection.cs:19 (Sections), :24 (Doors), :29 (Relations), :34 (Templates), :39 (TemplateApply); AddAbwab() is invoked at DependencyInjection.cs:26
- **[Area 4]** No second registration anywhere overrides a decorated interface (MS DI is last-wins), and registration ORDER is irrelevant because each service type is registered exactly once. — grep of AddScoped/AddSingleton/AddTransient over IAbwab* across all of Backend/ returns only AbwabDependencyInjection.cs:15,16,19,24,29,34,39,44,50,55 — one registration per service type
- **[Area 4]** Lifetimes compose correctly: no captive dependency. Singleton AbwabCacheGeneration injected into Scoped decorators/readers is legal; the Scoped decorators wrap Scoped inners; IMemoryCache is the singleton from AddMemoryCache(). — AbwabDependencyInjection.cs:12 (AddMemoryCache), :14 (AddSingleton<AbwabCacheGeneration>), :18-19,:23-24,:28-29,:33-34,:38-39,:43-44,:49-50 (AddScoped pairs)
- **[Area 4]** AbwabCacheGeneration is ONE object behind both interfaces — the failure the README warns about (two counters, writers bump one, controllers read the other, permanent 304 with a green build) does not exist. — AbwabDependencyInjection.cs:14-16 registers the concrete type once as a singleton and forwards both interfaces via GetRequiredService; AbwabCacheGeneration.cs:5 implements both IAbwabCacheInvalidator and IAbwabCacheValidators on the one class
- **[Area 4]** NO WRITE PATH BYPASSES THE DECORATORS. Every Abwab command handler depends on the interface, never on the concrete Ef writer, even though the concrete types are DI-registered and therefore injectable. — grep of EfAbwab*Writer across Backend/ shows references only inside Caching/Abwab/ decorators and AbwabDependencyInjection.cs — zero handler references; e.g. CreateDoorHandler.cs:7 (IAbwabDoorsWriter), ApplyTemplateHandler.cs:7 (IAbwabTemplateApplyWriter), RenameSectionHandler.cs:7 (IAbwabSectionsWriter), DeleteTemplateNodeHandler.cs:7 (IAbwabTemplatesWriter)
- **[Area 4]** No runtime seed/import path writes Abwab tables outside the five writers (the 785 production doors would otherwise be a live staleness source). `Application.Abstractions/Abwab/LegacySeed/` is an EMPTY directory. — `ls Backend/application/QuranDashboard.Application.Abstractions/Abwab/LegacySeed/` returns nothing; grep for AbwabDoors/AbwabSections/AbwabDoorAliases/AbwabDoorRelations outside tests and migrations hits only QuranDashboardDbContext.cs, the five Writes/Abwab writers and the two Reads/Abwab readers
- **[Area 4]** THE BUMP FIRES ON FAILURE TOO. Every one of the 21 bumps is in `finally`, not on the success path — so a translated exception thrown after a partial commit still invalidates. — InvalidatingAbwabDoorsWriter.cs:27-30 (try/finally shape, repeated identically at :46-49,:63-66,:80-83,:96-99,:108-111,:120-123,:132-135) and the same shape in all four other decorators; matches Writes/Abwab/README.md:59-62
- **[Area 4]** NO READ/BUMP RACE CAN PRODUCE A STALE CACHE HIT. Both cached readers capture the generation BEFORE querying and stamp the entry with the captured value, and an entry is served only on exact stamp equality. Every interleaving (write commits before the query, after the query, or between two concurrent readers overwriting each other's entry) lands on stamp mismatch → miss → refetch. Failure direction is one extra query, never a stale hit. — CachedAbwabTreeReader.cs:20 (capture), :22 (`cached.Generation == generation` equality gate), :28 (stamp with captured value); CachedAbwabTemplatesReader.cs:20,22,28 and :35,37,46
- **[Area 4]** THE ETAG CAN NEVER BE NEWER THAN THE BODY IT LABELS. The controller captures the validator before anything else and never re-reads it, so a client can only ever be handed a body at generation ≥ its ETag's generation — which costs one refetch, never a false 304. — AbwabTreeController.cs:19 (etag captured first), :27 (handler awaited after); AbwabTemplatesController.cs:25 then :33, and :47 then :55
- **[Area 4]** The 304 path returns NO BODY and runs ZERO queries, and carries both required headers (ETag + Cache-Control), per API_GUIDELINES.md:92-95 and :153-155. — AbwabTreeController.cs:20 (headers) then :24 `return StatusCode(StatusCodes.Status304NotModified)` — a bodiless StatusCodeResult returned before the handler at :27; AbwabTemplatesController.cs:26+:30 and :51+:52
- **[Area 4]** Every 200 from a conditional read carries ETag + Cache-Control: no-store, per API_GUIDELINES.md:149-152. — ConditionalGet.cs:14-15 sets both; called at AbwabTreeController.cs:20, AbwabTemplatesController.cs:26, and AbwabTemplatesController.cs:133 (OkWithValidator, used by the 200 at :60)
- **[Area 4]** A 404 carries no validator headers, per API_GUIDELINES.md:163. — AbwabTemplatesController.cs:61-62 returns NotFound without calling ConditionalGet.SetValidatorHeaders — the only Abwab conditional read that can 404
- **[Area 4]** WEAK-COMPARISON SEMANTICS ARE CORRECT. The server issues STRONG ETags (no W/ prefix); If-None-Match requires the weak comparison function per RFC 9110, and the matcher strips a leading W/ from each member before an ordinal compare — so W/"x" correctly matches "x". Comma-separated lists are split and each member trimmed. — AbwabCacheGeneration.cs:16,18,20-21 emit `"abwab-…"` with no W/ prefix; ConditionalGet.cs:36 (split on ','), :38 (Trim), :40-43 (W/ strip), :45 (Ordinal equality)
- **[Area 4]** A 500 does NOT leak the ETag set before the await. GlobalExceptionHandler calls Response.Clear(), which drops headers as well as body. — Backend/api/QuranDashboard.Api/Middleware/GlobalExceptionHandler.cs:35 and :53 (Response.Clear() before writing the error), guarded by the HasStarted check at :14
- **[Area 4]** Cache keys are namespaced and cannot collide with the other tenants of the shared IMemoryCache (notably CachedUserRoleResolver). — grep of `abwab:` across Backend/ returns exactly three sites, all in Caching/Abwab/: CachedAbwabTreeReader.cs:12, CachedAbwabTemplatesReader.cs:12 and :34
- **[Area 4]** A template MISS is never cached, so a caller-supplied id cannot grow the key space without bound (README Reads/Abwab:176-177 holds). — CachedAbwabTemplatesReader.cs:44 `if (template is not null)` guards the Set at :46; the null is returned uncached at :49
- **[Area 4]** README claim 'sections / doors / relations / apply bump the tree, the templates writer bumps templates' is EXACTLY what the code does. — Writes/Abwab/README.md:55-58 vs InvalidatingAbwabSectionsWriter.cs:22, InvalidatingAbwabDoorsWriter.cs:29, InvalidatingAbwabRelationsWriter.cs:28, InvalidatingAbwabTemplateApplyWriter.cs:25 (all InvalidateTree) and InvalidatingAbwabTemplatesWriter.cs:27 (InvalidateTemplates)
- **[Area 4]** README claim 'The compile error is the guard' is NOT overstated on close reading — it scopes the compile guard to interface growth and explicitly hands the bump to review and names the unregistered-decorator failure mode. I checked this expecting a documentation finding and it holds. — Writes/Abwab/README.md:67-70: 'an interface cannot grow without the decorator failing to build — and the `finally` bump is the line to check in review. A writer registered without its decorator would silently reintroduce stale reads with every test still green.'
- **[Area 4]** The multi-instance staleness risk is DOCUMENTED AND ACCEPTED with a migration path, not an undiscovered defect — and the README's specific claim that instance B can serve a stale 304 is correct despite the per-process boot id (the client's prior response can have come from B itself, so B's own ETag matches while B's counter never moved). — Reads/Abwab/README.md:181-189 (CONSTRAINT + migration path behind the existing interfaces); API_GUIDELINES.md:164-167 defers to it; API_GUIDELINES.md:291-293 records the same per-instance posture for the rate limiter; Backend/README.md:57-59 confirms Railway Hobby single-container deployment
- **[Area 4]** Quranic data safety: NOTHING in this area touches Quran text, morphology, identity, alignment or counting scope. The cached payloads are admin-authored gate (أبواب) names, descriptions, aliases and a free-text RepresentativeAyahText field; no Quran table is read or written, and no fallback fabricates data (a cache miss always calls the real reader). — AbwabTreeDto.cs:15-29 (the whole cached tree payload); EfAbwabTreeReader.cs:10-57 reads only abwab_* tables; CachedAbwabTreeReader.cs:27 and CachedAbwabTemplatesReader.cs:27,42 delegate every miss to the inner reader
- **[AREA 1]** Route-parity gate holds at 25/25 — every Abwab route has a SmokeRouteCatalog entry and every catalog entry is a live route, in both directions, with HTTP method and route constraints part of the key. — Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:228-359 (25 abwab entries) vs the 25 actions I enumerated across the six controllers; the bidirectional assertions are SmokeCoverageParityTests.cs:11-21 and :23-35, keyed by `$"{method} {template.TrimStart('/')}"` at :67-68.
- **[AREA 1]** ApiResponse envelope is consistent across all 25 Abwab actions — no action returns a bare object. Every 200/201/400/404/409 carries ApiResponse<T>; the only bodiless responses are 204 (five deletes) and 304 (three conditional reads), both sanctioned. — Every non-204/304 return in AbwabDoorsController.cs:34-46, AbwabSectionsController.cs:27-32, AbwabDoorRelationsController.cs:24-57, AbwabTreeController.cs:32, AbwabTemplatesController.cs:38-126, AbwabTemplateNodesController.cs:30-101 wraps in ApiResponse<T>.Ok/.Fail; API_GUIDELINES.md:91-95 sanctions 204 and 304 as the two bodiless statuses.
- **[AREA 1]** Model-binding failures keep the shared envelope instead of leaking English ValidationProblemDetails — so a malformed Abwab body still returns the Arabic failure shape. — Backend/api/QuranDashboard.Api/Extensions/ServiceCollectionExtensions.cs:24-32 replaces InvalidModelStateResponseFactory with `new BadRequestObjectResult(ApiResponse<object>.Fail(ApiMessages.ValidationFailed, errors))`; pinned by SmokeAbwabWriteTests.cs:123 `RenameSection_WithNullName_ReturnsBadRequestBindingLevel` and :198, :622.
- **[AREA 1]** No user-facing string is hardcoded in any Abwab controller — all 60+ messages are Arabic constants centralized in one file next to the API boundary, with English identifiers. — Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:112-192 (every Abwab message); zero string literals appear in the six controllers apart from route templates and the Location URIs. Satisfies API_GUIDELINES.md §10:208-227.
- **[AREA 1]** No internal detail leaks: no stack trace, SQL text, connection string, or filesystem path reaches a client. The two dynamic messages interpolate only door/template names the caller already owns. — ApiMessages.cs:159-162 (`AbwabDoorRelationDuplicateWith` joins `doorNames`) and :189-192 (`AbwabTemplateApplyCollisionWith` joins target/child names). Postgres exceptions are translated to domain exceptions inside the writer (EfAbwabSectionsWriter.cs:113-127) and never surfaced; the `_ => throw new InvalidOperationException(...)` default arms carry only a type name and flow to the global handler (ServiceCollectionExtensions.cs:63).
- **[AREA 1]** A false 304 is impossible: validators are opaque server-side generations, never derived from row data, and are monotonic within a process with a per-boot id preventing cross-restart collision. — Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Abwab/AbwabCacheGeneration.cs:7 (`_bootId` fresh Guid per instance), :12-14 (Interlocked.Increment only), :16-21 (ETags interpolate boot id + counter). Satisfies API_GUIDELINES.md:158-160.
- **[AREA 1]** Every Abwab write invalidates the cache generation its read depends on — including template apply, which creates door rows and therefore must bump the TREE generation, not the templates one. — InvalidatingAbwabTemplateApplyWriter.cs:25 calls `_invalidator.InvalidateTree()`; InvalidatingAbwabDoorsWriter.cs:29,48,65,82,98,110,122,134 and InvalidatingAbwabSectionsWriter.cs:22,34,46,58 and InvalidatingAbwabRelationsWriter.cs:28,40 all bump the tree; InvalidatingAbwabTemplatesWriter.cs:27,39,58,76,88,100 bump templates. All are in `finally` blocks, so a partial failure still invalidates.
- **[AREA 1]** 304 responses carry the ETag and Cache-Control headers API_GUIDELINES requires, and the 304 path never runs the query. — AbwabTreeController.cs:20 then :22-25 (headers set before the match check, `return StatusCode(304)` before `treeHandler.HandleAsync` at :27); AbwabTemplatesController.cs:26 then :28-31; AbwabTemplatesController.cs:49-53 sets headers inside the 304 branch. ConditionalGet.cs:12-16 sets both ETag and `Cache-Control: no-store`. Satisfies API_GUIDELINES.md:92-95 and :149-155.
- **[AREA 1]** A 404 from a conditional read carries no validator headers — an absence has no representation to validate. — AbwabTemplatesController.cs:61-62 returns `NotFound(...)` on the only Abwab conditional read that has a NotFound branch, and validator headers are set only inside the 304 branch (:51) and the success branch via `OkWithValidator` (:60, :131-135). Satisfies API_GUIDELINES.md:162.
- **[AREA 1]** ETag comparison is fail-open: an absent, blank, or `*` If-None-Match earns a full 200 rather than a stale 304. — ConditionalGet.cs:23-27 (empty header → false), :31-34 (blank member skipped), :45-48 (only an exact ordinal equality returns true, so `*` never matches a quoted generation string). Satisfies API_GUIDELINES.md:161-165.
- **[AREA 1]** The relations read distinguishes "unknown door" (404) from "archived door with no visible relations" (200 []) — an archived anchor is not a 404. — EfAbwabRelationsReader.cs:12-17 tests existence with `.AnyAsync(d => d.Id == doorId)` WITHOUT a DeletedAtUtc filter, so an archived door exists; the dormancy join at :23-25 then filters its relations to empty. Handler maps null→NotFound only. Matches Controllers/README.md:15-16 and Reads/Abwab/README.md:22-24.
- **[AREA 1]** No minimal-API endpoints exist in the Api project, so nothing is exposed outside the catalogued controller routes. — A grep for `MapGet(|MapPost(|MapPut(|MapDelete(|MapPatch(|MapMethods(` across Backend/api/QuranDashboard.Api/ returns zero hits (exit 1); SmokeCoverageParityTests.cs:52-61 additionally enumerates the live `EndpointDataSource` and would fail on any uncatalogued endpoint.
- **[AREA 1]** HTTP verbs are correct throughout: no GET mutates state; PUT is used for full replacement, POST for non-idempotent commands and named actions, DELETE for archive/delete. — The only GETs are AbwabDoorRelationsController.cs:15, AbwabTreeController.cs:13, AbwabTemplatesController.cs:21 and :43 — all four delegate to Queries/ handlers with no writer dependency (AbwabTreeController.cs:9-11 injects `GetAbwabTreeHandler` + `IAbwabCacheValidators` only). Satisfies API_GUIDELINES.md §3:77-83.
- **[AREA 1]** Route naming is resource-oriented, stable, plural, kebab-cased consistently, and exposes no table or file names. — `api/abwab/sections`, `/doors`, `/relations`, `/templates`, `/template-nodes`, `/tree`, with kebab-case sub-actions `bulk-move` (AbwabDoorsController.cs:132), `bulk-archive` (:162), `template-nodes` (AbwabTemplateNodesController.cs:45). Satisfies API_GUIDELINES.md §2:59-65.
- **[AREA 1]** Controllers stay thin — no EF Core, no Infrastructure type, no business rule in any of the six files; every action is bind → call handler → map outcome to status. — The `using` blocks of all six files reference only `Application.Abstractions.Abwab.Responses`, `Application.Abwab.Commands.*`, and `Application.Abwab.Queries.*` (e.g. AbwabDoorsController.cs:1-9, AbwabTemplatesController.cs:1-7); no `Microsoft.EntityFrameworkCore` or `QuranDashboard.Infrastructure` import appears. Satisfies API_GUIDELINES.md §1:44-56. The one exception is the scope guard at AbwabDoorsController.cs:110-112, reported as a finding.
- **[AREA 1]** Doors and sections write routes ARE dispatched smoke-tested for their status/envelope contract — the coverage gap is confined to relations and templates, and that gap is already recorded as debt rather than being silent. — Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs is 1236 lines covering create/rename/delete/reorder on sections and create/edit/move/reorder/bulk-move/bulk-archive/delete/restore on doors, including 400/404/409 arms (e.g. :53 duplicate→409, :94 stale version→409, :138 delete→204 no body, :509 out-of-range→400, :560 scope-not-applicable→400). The relations and templates routes have no dispatched test and are recorded at docs/TESTING_DEBT.md:37 (row 3) and :62 (row 8) as acceptance criteria of the auth feature.
- **[AREA 1]** Quran-data safety: NOT APPLICABLE. No Abwab controller or DTO touches Quran text, morphology, identity, alignment, or counting scope. — The only Quran-adjacent field on the surface is `RepresentativeAyahText` (AbwabDoorDto.cs:9, AbwabTemplateNodeDto.cs:8, AbwabTreeDto.cs:22) — admin-authored free text carried verbatim from body to row to response with no server-side normalization, correction, or generation (EditDoorBody.cs:6 → EditDoorCommand → EfAbwabDoorsWriter). The four Abwab counts count door/relation/node rows, never ayahs or words.
- **[AREA 3c]** INVARIANT 1a — section_id is NOT NULL at the column, in the model, and in the database — Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoor.cs:7 (`public int SectionId`); Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Abwab/AbwabDoorConfiguration.cs:16-18 (`.IsRequired()`); Migrations/QuranDashboardDbContextModelSnapshot.cs:84 (`b.Property<int>("SectionId")`, not int?); Migrations/20260802062011_RequireAbwabDoorSection.cs:13-20 (nullable:false). Pinned by Backend/tests/QuranDashboard.Tests/Abwab/AbwabSchemaTests.cs:372 (information_schema says 'NO') and :387 (raw INSERT with NULL is rejected by Postgres, deliberately bypassing EF).
- **[AREA 3c]** INVARIANT 1b — the writer rejects a missing section at ROOT SCOPE ONLY, never under a parent — EfAbwabDoorsWriter.cs:477-486 — the AbwabSectionRequiredException throw at :481 sits inside `if (!parentId.HasValue)`; the parent branch at :488-503 never throws it. Same shape for move/bulk-move at :546-555 (throw at :550 inside `if (!targetParentId.HasValue)`). Same for restore at :461-470 — the throw at :469 is reached only after the `parent is not null` early return at :451-459. Pinned three ways: AbwabDoorWriteBehaviorTests.cs:290 (create at root without section throws), :319 (MoveAsync), :335 (BulkMoveAsync); and the negative side at :741 (`CreateAsync_UnderAParent_DerivesTheSectionWhenNoneIsStated`), whose header comment at :729-731 names the rule — 'the one place the root-scope rejection deliberately does not reach'.
- **[AREA 3c]** INVARIANT 1c — a child's section is DERIVED from its parent, and a caller-supplied section that disagrees is REFUSED, not honoured — Create: EfAbwabDoorsWriter.cs:494-503 validates the stated section exists, throws AbwabSectionParentMismatchException at :499 if it disagrees, and returns `parent.SectionId` at :503 regardless. Restore: :451-459, mismatch throw at :455, `return parent.SectionId` at :458. Move/bulk-move: :557-563 ignores targetSectionId entirely and returns `parent.SectionId` at :563 — a documented deliberate asymmetry (Writes/Abwab/README.md:160-163), and safe because the derived value still wins. Template apply: EfAbwabTemplateApplyWriter.cs:97 and :120 pass target.SectionId / copied.SectionId, never a caller value. No path anywhere accepts a caller section that disagrees with a parent. Pinned by AbwabDoorWriteBehaviorTests.cs:756 (create mismatch throws), :527 (child restore derives the live parent's section when the body is null), :547 (child restore with a conflicting section throws), :604 (child restored after an ancestor re-section derives the parent's CURRENT section, with the stale value written straight onto the row to make it discriminating), and AbwabTemplateApplyBehaviorTests.cs:16 (copies carry the target's section at every depth).
- **[AREA 3c]** INVARIANT 2a — re-sectioning cascades to descendants INCLUDING archived rows (the one the brief predicted would break) — EfAbwabDoorsWriter.cs:530-532 — `var descendants = await db.AbwabDoors.Where(d => descendantIds.Contains(d.Id)).ToListAsync(cancellationToken);`. There is NO DeletedAtUtc predicate. The parent map it walks is equally unfiltered: :616-618 `await db.AbwabDoors.Select(d => new { d.Id, d.ParentId }).ToListAsync(...)`. Called from all three re-section paths — MoveAsync :127, BulkMoveAsync :280, RestoreAsync :427. Pinned by AbwabDoorWriteBehaviorTests.cs:695, whose assertion at :754-755 of the file is on an ARCHIVED grandchild ('an archived descendant keeps its parent_id through soft-delete, so it must follow too'), and again at :570 for the restore path ('the re-section reaches the rows the restore did not'). Both test headers state explicitly that a live-only cascade passes every assertion that ignores the archived row.
- **[AREA 3c]** INVARIANT 2b — restore resolves a detached door rather than leaving it section-less — EfAbwabDoorsWriter.cs:448-473 ResolveRestoreSectionAsync returns non-nullable int on every branch: parent's section (:458), the stated live section (:463-464), or the stored section only when IsSectionArchivedAsync says it is still live (:467-472), otherwise AbwabSectionRequiredException at :469 → RestoreDoorOutcome.SectionRequired (RestoreDoorHandler.cs:30-33) → 400 (AbwabDoorsController.cs:214-215). The old detach-to-null behavior is gone and the column now forbids it. Pinned by AbwabDoorWriteBehaviorTests.cs:460, :482, :508.
- **[AREA 3c]** INVARIANT 2c — restore claims exactly what THIS archive took, matched on the archive's own timestamp — EfAbwabDoorsWriter.cs:399 captures `var archivedAt = door.DeletedAtUtc;` before clearing it at :402, and :416 matches descendants on `d.DeletedAtUtc == archivedAt.Value` — not on `DeletedAtUtc != null`. Pinned by AbwabDoorWriteBehaviorTests.cs:434 (RestoreAsync_DoesNotResurrectIndependentlyArchivedDescendant). Correctly paired with 2a: the cascade at :427 is unbounded by what the restore gave back, so a separately-archived descendant is re-sectioned without being resurrected — the exact combination AbwabDoorWriteBehaviorTests.cs:570 asserts in one test.
- **[AREA 3c]** INVARIANT 3 — global_order_value IS NOT NULL ⟺ (parent_id IS NULL AND deleted_at IS NULL), across all six transitions — Create root → arrival, EfAbwabDoorsWriter.cs:38-41. Create child → never touched, so int? stays null. Root→nested move → :137-141 sets `door.GlobalOrderValue = null` AND departs. Nested→root move → :142-145 arrival. Root→root move (section change only) → neither branch fires, value preserved. Archive → ArchiveSubtreeAsync :589-592 nulls it when ParentId is null, then DeleteAsync :369-372 / BulkArchiveAsync :328,341-344 depart it. Restore → :430-433 arrival, gated on archivedAt.HasValue at :405. Bulk move → the per-door classification at :262-275 mirrors MoveAsync exactly. The departure/arrival asymmetry is correct against the pre-SaveChanges read at :682-687: departing rows still come back from the database and are dropped via excludeIds; arriving rows do not come back and are appended in code. Pinned by AbwabDoorWriteBehaviorTests.cs:184 (root→root leaves it unchanged), :204 (nested→root appends to end), :225 (root→nested nulls it and shifts later roots down), :247 (archive same), :269 (restore appends to end), :137/:154 (Section scope never touches Global and vice versa), :303 (roots in different sections share one sequence), :669 (a live-root restore leaves the sequence intact). The index is partial to exactly the biconditional's right-hand side — AbwabDoorConfiguration.cs:83-84 `HasFilter("parent_id IS NULL AND deleted_at IS NULL")` — and AbwabSchemaTests.cs:141 asserts that filter against pg_indexes, :158 asserts it is NOT unique.
- **[AREA 3c]** INVARIANT 4a — relations stored as a canonical pair, enforced on write AND at the database — EfAbwabRelationsWriter.cs:45-46 `DoorAId = Math.Min(doorId, targetId), DoorBId = Math.Max(doorId, targetId)` — applied unconditionally to all three types, directional included. Backed by CHECK `door_a_id < door_b_id` (AbwabDoorRelationConfiguration.cs:11-13, shipped in Migrations/20260729135714_AddAbwabDoorRelations.cs:38 and carried in the snapshot at its ToTable block). Because both orderings canonicalize to the same (min,max), (A,B) and (B,A) are literally the same row and the unique index rejects the second.
- **[AREA 3c]** INVARIANT 4b — the unique index actually enforces it, and is scoped so soft-deleted rows do not block re-adding — AbwabDoorRelationConfiguration.cs:82-84 `HasIndex(r => new { r.DoorAId, r.DoorBId, r.RelationType }).IsUnique().HasFilter("deleted_at IS NULL")`; identical in Migrations/20260729135714_AddAbwabDoorRelations.cs:59-64 and in the model snapshot. The writer's up-front GuardAgainstExistingAsync (:99-104) checks BOTH sides (`r.DoorAId == doorId || r.DoorBId == doorId`) so the 409 can name the colliding doors, with the 23505 catch at :123-126 as the race backstop.
- **[AREA 3c]** INVARIANT 4c — dormancy is DERIVED and never stored: no column, no flag, anywhere — Case-insensitive grep for dormant|dormancy over Backend, Frontend/quran-dashboard-ui/src and docs returns only prose (two README lines, one frontend spec name, one TESTING_DEBT row) — zero code identifiers. AbwabDoorRelation.cs:1-25 has no such property. AbwabDoorRelationConfiguration.cs maps eleven columns, none of them a dormancy flag. QuranDashboardDbContextModelSnapshot.cs's AbwabDoorRelation block lists exactly Id/ApprovedAtUtc/ApprovedBy/BroaderDoorId/CreatedAtUtc/CreatedBy/DeletedAtUtc/DeletedBy/DoorAId/DoorBId/RelationType/UpdatedAtUtc/UpdatedBy/Version. It is computed at read time in both readers: EfAbwabRelationsReader.cs:21-25 joins abwab_doors on both endpoints and requires `relation.DeletedAtUtc == null && doorA.DeletedAtUtc == null && doorB.DeletedAtUtc == null`; EfAbwabTreeReader.cs:63-68 does the same for RelationCount. And no door write touches a relation row — grep for AbwabDoorRelations over EfAbwabDoorsWriter.cs and EfAbwabSectionsWriter.cs returns nothing.
- **[AREA 3c]** INVARIANT 5a — apply copies the root's DIRECT CHILDREN and never the root itself; no surviving reversed path — EfAbwabTemplateApplyWriter.cs:39 `if (!childrenByParentNode.TryGetValue(rootNode.Id, out var rootChildren) || rootChildren.Count == 0)` — rootChildren is the copy set; the level-1 loop at :94-100 iterates it. `rootNode` (bound at :31) is used at :39 to look up its children and nowhere else. NewDoor is called exactly twice, :97 and :120, and neither is ever passed rootNode. There is no third door-creating path in the repository: AbwabDoors.Add/AddRange appears only at EfAbwabDoorsWriter.cs:36 and EfAbwabTemplateApplyWriter.cs:98,121 (plus tests), and raw `abwab_doors` outside Migrations/ and Configurations/ appears only in tests and scripts/wipe-abwab.
- **[AREA 3c]** INVARIANT 5b — the collision is keyed on (target, child), not on root name — EfAbwabTemplateApplyWriter.cs:61-68 — `rootChildNames` is the root's direct child names; the query filters `d.ParentId != null && targetIds.Contains(d.ParentId!.Value) && rootChildNames.Contains(d.Name) && d.DeletedAtUtc == null` and projects `{ ParentId, Name }`. The exception carries `new AbwabTemplateApplyCollisionPair(target.Name, hit.Name)` at :80, ordered by caller target order then the template's own sibling order (:76-81) → AbwabTemplateApplyCollisionException at :83 → ApplyTemplateOutcome.Collision (ApplyTemplateHandler.cs:58-62) → 409 (AbwabTemplatesController.cs:188-190).
- **[AREA 3c]** INVARIANT 5c — the empty-ROOT template is a 400, raised before any target is read, and is neither a 500 nor a silent no-op — EfAbwabTemplateApplyWriter.cs:41 `throw new AbwabTemplateEmptyException();` sits at :39-42, above the target read at :44-47 — exactly as Writes/Abwab/README.md:259-261 claims. Caught at ApplyTemplateHandler.cs:53-57 → EmptyTemplate → AbwabTemplatesController.cs:182-183 `BadRequest(...AbwabTemplateApplyEmpty)`. Distinct from the target-archived 400 (:180-181) and the no-targets 400 (:178-179).
- **[AREA 3c]** INVARIANT 5d — the empty TARGET LIST 400 is real, but it lives in the handler, not the writer — ApplyTemplateHandler.cs:22-27 — `var targetDoorIds = command.TargetDoorIds ?? []; if (targetDoorIds.Count == 0) { ... return new ApplyTemplateOutcome.InvalidRequest(); }` → 400 at AbwabTemplatesController.cs:178-179. This matters because the writer alone would NOT refuse it: with an empty targetIds, EfAbwabTemplateApplyWriter.cs:49 `targets.Count != targetIds.Count` is 0 != 0 (false), the loop at :89 does nothing, the while at :106 never runs, and :130 commits a no-op transaction returning []. The guard is correctly placed but is single-layered — worth knowing before anyone moves validation around.
- **[AREA 3c]** Relation direction validity is rejected upstream, so ResolveBroaderDoorId's fallthrough is unreachable through the API — AddDoorRelationsHandler.cs:81-84 `type == AbwabRelationType.Comprehensiveness ? direction is not null && Enum.IsDefined(direction.Value) : direction is null`, checked at :43-46 before the writer is called at :50 → InvalidDirection → 400 at AbwabDoorRelationsController.cs:132-133. Type validity likewise: `Enum.IsDefined(command.Type)` at :38-41, which also catches the JSON-absent 0 that AbwabRelationType.cs:7-12 starts at 1 to reserve. (Reported separately as a LOW because the guard is not at the writer seam.)
- **[AREA 3c]** No route is exposed beyond the known, accepted-unauthenticated Abwab write set — SmokeRouteCatalog.cs:224-356 enumerates the full Abwab surface — sections (4), doors (9), relations (3), tree (1), templates (4), template nodes (5), apply (1). Each of the six Abwab controllers' [Http*] attributes maps 1:1 onto that catalogue, and SmokeCoverageParityTests is the gate that keeps it so. Nothing outside the catalogued set exists.
- **[AREA 3b]** Route counts declare their scope and are exact: twenty-one write routes and four reads, twenty-five in all (Controllers/README.md:9-17) — Counted from the attributes: AbwabSectionsController.cs:17,37,62,80 (4 writes); AbwabDoorsController.cs:25,51,75,104,132,162,182,199 (8 writes); AbwabDoorRelationsController.cs:31,62 (2 writes) + :15 (1 read); AbwabTemplatesController.cs:67,86,102 (3 writes) + :21,:43 (2 reads); AbwabTemplateNodesController.cs:17,45,67,88 (4 writes); AbwabTreeController.cs:13 (1 read). 4+8+2+3+4 = 21 writes, 4 reads, 25 total
- **[AREA 3b]** 'their twelve routes are catalogued ParityOnly' (Writes/README.md:287-288) — the relations and templates route families are all ParityOnly, and the tree read deliberately is not — Backend/tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs:289,293,297 (relations, 3) + :320,324,328,332 (templates, 4) + :340,344,348,352 (nodes, 4) + :356 (apply, 1) = 12, each with `ParityOnly = true`; SmokeRouteCatalog.cs:311 registers api/abwab/tree with no ParityOnly flag
- **[AREA 3b]** AbwabTreeDoorDto.SectionRetired can only be true for an ARCHIVED door (Reads/README.md:44-45) — no live door can point at a section this reader filters out — Induction over every write path: EfAbwabSectionsWriter.cs:56-61 refuses to archive a section holding live doors; EfAbwabDoorsWriter.cs:508 (EnsureSectionExistsAsync requires DeletedAtUtc == null) gates create :484/:496, move :553, and restore-with-stated-section :463; EfAbwabDoorsWriter.cs:467-470 refuses an unstated restore into a retired section; child paths derive from a live parent (:503, :563, :458). Pinned by AbwabTreeReadTests.cs:56 GetTreeAsync_FlagsSectionRetired_OnlyForDoorsWhoseSectionIsArchived
- **[AREA 3b]** DirectChildCount, DoorsInScopeCount and RelationCount all count LIVE rows only, and each declares its scope (Reads/README.md:52-56, :96-98) — EfAbwabTreeReader.cs:25-28 (live children by ParentId), :29-32 (live doors by SectionId at any depth), :66-68 (relation visible iff its own deleted_at is null and both endpoint doors are live). DTO field names carry the scope: AbwabTreeDto.cs:13 DoorsInScopeCount, :27 DirectChildCount, :28 RelationCount. Pinned by AbwabTreeReadTests.cs:86 and :116
- **[AREA 3b]** Archived sections are excluded from the snapshot; archived doors are included and flagged via DeletedAtUtc != null (Reads/README.md:34-37) — EfAbwabTreeReader.cs:10-11 filters sections on DeletedAtUtc == null; :15-17 loads AbwabDoors with no deleted filter; :50 projects `d.DeletedAtUtc != null` into IsArchived. Pinned by AbwabTreeReadTests.cs:35
- **[AREA 3b]** Snapshot Version is max(updated_at, deleted_at) across sections, doors and aliases only — one query per table — and deliberately ignores abwab_door_relations (Reads/README.md:68-71, :102-104) — EfAbwabTreeReader.cs:84-86, :87-89, :90-92 (three MaxAsync calls, per-row greatest of the two columns), :94-95 combines them. No AbwabDoorRelations term anywhere in GetSnapshotVersionAsync. Pinned by AbwabTreeReadTests.cs:11 (null on empty schema) and :174
- **[AREA 3b]** The relations reader returns null for an unknown door (→404) and an empty list for a door with nothing visible; the two are not interchangeable (Reads/README.md:22-24) — EfAbwabRelationsReader.cs:12-17 returns null when no row with that id exists; :40-47 returns a materialized list otherwise. Mapped at AbwabDoorRelationsController.cs:23-26
- **[AREA 3b]** A relation's direction is resolved per viewer and never stored twice; broader_door_id is NOT NULL exactly for Comprehensiveness and must be one of the pair — EfAbwabRelationsReader.cs:50-61 and EfAbwabRelationsWriter.cs:140-151 both derive AbwabRelationDirection from broader_door_id relative to the anchor. Enforced in the schema by AbwabDoorRelationConfiguration.cs:17-20 CHECK `(relation_type = 3) = (broader_door_id IS NOT NULL) AND (broader_door_id IS NULL OR broader_door_id IN (door_a_id, door_b_id))`
- **[AREA 3b]** The canonical pair is the writer's job — every row stored door_a_id < door_b_id for all three types (Writes/README.md:220-225) — EfAbwabRelationsWriter.cs:45-46 `Math.Min`/`Math.Max` unconditionally; backed by AbwabDoorRelationConfiguration.cs:11-13 CHECK `door_a_id < door_b_id`. Three types confirmed at AbwabRelationType.cs:246-248
- **[AREA 3b]** The relations partial unique index is (door_a_id, door_b_id, relation_type) filtered on the relation's own deleted_at, which is what makes a dormant row still occupy its pair (Reads/README.md:87-89, Writes/README.md:42) — AbwabDoorRelationConfiguration.cs:82-84 — `HasIndex(r => new { r.DoorAId, r.DoorBId, r.RelationType }).IsUnique().HasFilter("deleted_at IS NULL")`
- **[AREA 3b]** Dormancy is a read-time join, never a stored column; no door or section write touches abwab_door_relations (Reads/README.md:81-89, Writes/README.md:202-209) — Join expressed at EfAbwabTreeReader.cs:63-68 and EfAbwabRelationsReader.cs:20-26. AbwabDoorRelation has no is_dormant column (AbwabDoorRelationConfiguration.cs:23-70). grep of EfAbwabDoorsWriter.cs and EfAbwabSectionsWriter.cs finds no AbwabDoorRelations reference at all
- **[AREA 3b]** Optimistic concurrency is applied as OriginalValue, never CurrentValue, and bulk writes set it per row (Writes/README.md:78-81) — EfAbwabDoorsWriter.cs:69, :112, :196, :260 (per row in the bulk-move loop), :320 (per row in bulk-archive), :359, :397; EfAbwabSectionsWriter.cs:37, :91. No `.CurrentValue` assignment anywhere in Persistence/Writes/Abwab/
- **[AREA 3b]** Archive claims only live descendants; restore matches descendants on the archive's own deleted_at timestamp captured before the door's is cleared (Writes/README.md:104-108) — EfAbwabDoorsWriter.cs:600-602 filters `d.DeletedAtUtc == null` in ArchiveSubtreeAsync; RestoreAsync captures `var archivedAt = door.DeletedAtUtc;` at :399 before clearing at :402, and matches `d.DeletedAtUtc == archivedAt.Value` at :416
- **[AREA 3b]** The cycle guard and the section cascade both walk ALL rows, archived included — parent_id survives soft-delete (Writes/README.md:146-155) — EfAbwabDoorsWriter.cs:616-618 selects every AbwabDoors row with no DeletedAtUtc filter; EnsureNotCycle (:566-577) walks that map; CascadeSectionToDescendantsAsync loads descendants with no deleted filter at :530-532
- **[AREA 3b]** Restore of an already-live door is a no-op on destination, sectioning, and both sequences — a sectionId in the body is ignored (Writes/README.md:128-133) — EfAbwabDoorsWriter.cs:405 gates the whole resolution/cascade/global/resequence block on `if (archivedAt.HasValue)`; everything at :407-441 sits inside it
- **[AREA 3b]** Two independent root orders: global_order_value IS NOT NULL ⟺ (parent_id IS NULL AND deleted_at IS NULL), and every root-affecting write maintains it (Writes/README.md:172-182) — Nulled on root→nested at EfAbwabDoorsWriter.cs:139 and :269, on archive at :591; re-granted on arrival at :40 (create), :144 (nested→root), :432 (restore); ResequenceGlobal at :670-677 reads only live roots via :682-685. Reorder scope split confirmed at :160-172 (Global → ResequenceGlobal) vs :175-180 (Section → Resequence). Schema mirror: AbwabDoorConfiguration.cs:229-230 index filtered `parent_id IS NULL AND deleted_at IS NULL`
- **[AREA 3b]** No UNIQUE index on global_order_value or order_value (Writes/README.md:192-194) — AbwabDoorConfiguration.cs:225 (SectionId, ParentId, OrderValue — non-unique) and :229-230 (GlobalOrderValue — non-unique, filtered). The only unique door index is :232-235 on (SectionId, ParentId, Name)
- **[AREA 3b]** The global-order backfill is hand-written SQL inside the EF-generated migration, at the cited line — Migrations/20260729105806_AddAbwabGlobalOrderValue.cs:28 — `migrationBuilder.Sql("""` appended after the generated CreateIndex; exactly the file:LINE Writes/README.md:187 claims
- **[AREA 3b]** AbwabCacheGeneration is registered as ONE object behind both interfaces (Reads/README.md:153-158) — AbwabDependencyInjection.cs:14 registers the concrete singleton; :15 and :16 forward IAbwabCacheInvalidator and IAbwabCacheValidators via `sp.GetRequiredService<AbwabCacheGeneration>()`. Both cached readers take the concrete type (:47, :53)
- **[AREA 3b]** The ETag mixes a per-process boot id with the generation counter (Reads/README.md:146-152) — AbwabCacheGeneration.cs:7 `Guid.NewGuid().ToString("N")[..8]` per instance, interpolated at :16 (tree), :18 (templates list), :20-21 (single template)
- **[AREA 3b]** Capture-before-load, no expiration on any entry, and a miss on abwab:template:{id} is never cached (Reads/README.md:166-171, :176-177) — CachedAbwabTreeReader.cs:20 captures before :27 loads, stamps at :28 with no MemoryCacheEntryOptions; CachedAbwabTemplatesReader.cs:20/:27-28 and :35/:42; :44-47 sets the entry only `if (template is not null)`
- **[AREA 3b]** Every writer interface is DI-wrapped by an invalidating decorator — none registered bare (Writes/README.md:56-70) — AbwabDependencyInjection.cs:19-21, :24-26, :29-31, :34-36, :39-41 — all five interfaces resolve to an Invalidating*Writer over the concrete Ef* type registered on the preceding line. The relations reader at :55 is registered bare and uncached, exactly as Reads/README.md:125-131 states
- **[AREA 3b]** Templates: the list is one query aggregating in SQL, and a rootless template is treated as not-found by both reads (Reads/README.md:109-117) — EfAbwabTemplatesReader.cs:14-23 — RootName and DescendantCount are correlated subqueries inside one Select; :27 drops rows whose RootName is null; GetAsync returns null at :57 when no live root node exists
- **[AREA 3b]** Template node uniqueness is (template_id, parent_node_id, name), and one live root per template is a schema constraint (Writes/README.md:44-45, :253-255) — AbwabTemplateNodeConfiguration.cs:85-88 unique on (TemplateId, ParentNodeId, Name) filtered `deleted_at IS NULL` with AreNullsDistinct(false); :81-83 unique on TemplateId filtered `parent_node_id IS NULL AND deleted_at IS NULL`
- **[AREA 3b]** Template deletion is soft and touches one row, while the three nodeId-keyed writes still join the template's flag and answer 404 (Writes/README.md:265-273) — EfAbwabTemplatesWriter.cs:52-54 sets only the template's own deleted_at/updated_at; FindLiveNodeAsync at :216-221 joins `db.AbwabTemplates.Any(t => t.Id == n.TemplateId && t.DeletedAtUtc == null)` and is the entry point for EditNodeAsync (:125), ReorderNodeAsync (:145) and DeleteNodeAsync (:181)
- **[AREA 3b]** Apply copies the root's DIRECT CHILDREN, never the root; empty-root template is refused before any target row is read; collisions are per (target, child) name (Writes/README.md:232-261, Controllers/README.md:46-54) — EfAbwabTemplateApplyWriter.cs:39-42 throws AbwabTemplateEmptyException before the target read at :44; :94-97 copies rootChildren at `nextOrder + i`; :120 keeps verbatim OrderValue below level 1; :62-84 builds AbwabTemplateApplyCollisionPair ordered by caller target order (:54) then template sibling order (:79). No MaintainGlobalOrder, no Resequence, no per-node section resolution anywhere in the file
- **[AREA 3b]** Aliases are normalized once at the write seam by the single shared helper, and every alias write goes through it (Writes/README.md:71-77) — AbwabAliasNormalization.cs:5-6 (trim, drop empties, distinct); called at EfAbwabDoorsWriter.cs:692 (ReplaceAliasesAsync), EfAbwabTemplatesWriter.cs:30, :105, :134 (node writes), EfAbwabTemplateApplyWriter.cs:143, :163 (apply). No other Trim/Distinct alias path exists in the folder
- **[AREA 3b]** Aliases are soft-deleted, never hard-deleted, and AbwabDoorAlias deliberately has no xmin (Writes/README.md:164-165) — EfAbwabDoorsWriter.cs:699-703 sets DeletedAtUtc/UpdatedAtUtc; no Remove call on AbwabDoorAliases anywhere. AbwabDoorAliasConfiguration.cs:96-145 declares no Version property and no IsRowVersion — unlike AbwabDoorConfiguration.cs:212-213
- **[AREA 3b]** Aliases are live-only on both sides (Reads/README.md:66-67) — EfAbwabTreeReader.cs:20 filters `a.DeletedAtUtc == null`; EfAbwabDoorsWriter.ToDtoAsync:720 filters the same way
- **[AREA 3b]** Reads tolerate gaps — ordering is by raw OrderValue with Id as tie-break, never assuming contiguity (Reads/README.md:72-74) — EfAbwabTreeReader.cs:12 (sections) and :16 (doors) both `.ThenBy(... .Id)`; EfAbwabSectionsWriter.cs:83 tie-breaks the same way, as Writes/README.md:97-98 claims. Pinned by AbwabTreeReadTests.cs:141 GetTreeAsync_OrdersDoorsByOrderValue_EvenWithAGap
- **[AREA 3b]** GlobalOrderValue is projected verbatim and the reader does not order by it (Reads/README.md:75-80) — EfAbwabTreeReader.cs:50 passes `d.GlobalOrderValue` straight into the DTO; the OrderBy chain at :16 is SectionId/ParentId/OrderValue/Id with no GlobalOrderValue term
- **[AREA 3b]** AbwabReorderScope is 1 = Section, 2 = Global, and an omitted/unrecognised value is a 400 (Controllers/README.md:30-33) — AbwabReorderScope.cs:255-256; refused at AbwabDoorsController.cs:110-112 via `Enum.IsDefined(body.Scope)` → InvalidScope → BadRequest at :122-123. Global on a nested door throws at EfAbwabDoorsWriter.cs:162-165 → BadRequest at AbwabDoorsController.cs:124-125
- **[AREA 3b]** Restore's body is { sectionId?, version }, it returns the plain AbwabDoorDto, and carries its own parent-still-archived 409 (Controllers/README.md:24-30) — RestoreDoorBody.cs:3 `record RestoreDoorBody(int? SectionId, uint Version)`; AbwabDoorsController.cs:200 returns ActionResult<ApiResponse<AbwabDoorDto>>; :220-221 maps ParentStillArchived to Conflict, thrown at EfAbwabDoorsWriter.cs:391-394. Retired-origin root → 400 at controller :214-215, stated-but-missing section → 404 at :212-213
- **[AREA 3b]** No controller in the tree carries /// XML docs (Controllers/README.md:55-57, :126-127) — `grep -rn "///" Backend/api/QuranDashboard.Api/Controllers/` returns only three hits, all inside Controllers/README.md itself (:56, :125, :126) — zero in any .cs file
- **[AREA 3b]** The relations and templates readers have no tests (Reads/README.md:205-206) — `grep -rn "AbwabRelationsReader|AbwabTemplatesReader|IAbwabRelationsReader|IAbwabTemplatesReader" Backend/tests/` returns zero matches; Backend/tests/QuranDashboard.Tests/Abwab/ contains only AbwabTreeReadTests.cs, AbwabDoorWriteBehaviorTests.cs, AbwabTemplateApplyBehaviorTests.cs, AbwabSchemaTests.cs, AbwabSchemaFixture.cs
- **[AREA 3b]** docs/contracts/http-api.md and response-envelope.md are pointer-only and every link target resolves — All eight targets exist: Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs, Backend/.architecture/API_GUIDELINES.md, Frontend/quran-dashboard-ui/src/app/core/data-access/api-response.model.ts, Frontend/quran-dashboard-ui/src/app/core/api/generated/ (models/, models.ts), Frontend/quran-dashboard-ui/openapi/swagger.json, Backend/scripts/export-swagger, Backend/scripts/check-api-contract, docs/contracts/README.md. Neither page restates a single Abwab route, status code or field, so neither can drift
- **[AREA 3b]** Backend/README.md and Backend/tests/QuranDashboard.Tests/README.md make no Abwab-specific claims that could drift — grep -i abwab over both files returns zero hits. Backend/README.md:4 does still say the backend is '**Read-only** over curated Quran data at the API; writes happen only through the import/generate CLI' — accurate as a statement about *Quran* data (no Abwab write touches a Quran table), and the twenty-one Abwab write routes are curation metadata, not Quran data; the sentence is scoped by 'over curated Quran data' and holds
- **[AREA 3b]** Relation writes carry no version token and nothing reads the relation row's xmin (Writes/README.md:210-214) — AddDoorRelationsHandler.cs:94-95 and DeleteDoorRelationCommand carry no version; EfAbwabRelationsWriter.cs has no `.OriginalValue` assignment anywhere; the row still declares one at AbwabDoorRelationConfiguration.cs:69-70 IsRowVersion()
- **[AREA 3b]** Relation delete is soft, returns bool, and a missing/already-deleted row is false rather than an exception (Writes/README.md:226-230) — EfAbwabRelationsWriter.cs:69-74 returns false when no live row matches; :77-78 sets deleted_at/updated_at; :84-87 catches DbUpdateConcurrencyException and returns false; mapped to 204/404 at AbwabDoorRelationsController.cs:71-73
- **[AREA 3b]** MoveAsync and BulkMoveAsync both read the destination scope with the moving door(s) excluded, then renumber destination-plus-door together (Writes/README.md:87-90) — EfAbwabDoorsWriter.cs:114-118 (`d.Id != id`) then :135 `Resequence(destinationSiblings.Append(door))`; :243-247 (`!movedIds.Contains(d.Id)`) then :288-289. Pinned by AbwabDoorWriteBehaviorTests.cs per Writes/README.md:90
- **[AREA 3b]** MaintainGlobalOrderAsync handles departures via excludeIds and arrivals in code, because its read still shows pre-SaveChanges state (Writes/README.md:195-199) — EfAbwabDoorsWriter.cs:679-688 — the read at :682-685 filters live roots from the database, and :687 applies `.Where(d => !excludeIds.Contains(d.Id)).Concat(arrivals)`. Callers supply departures at :140, :293, :343, :371 and arrivals at :40, :144, :432
- **[AREA 3]** RequireAbwabDoorSection does NOT drop, default, or silently rewrite pre-existing NULL section_id rows. Its Up() is a single ALTER … SET NOT NULL with no UPDATE and no DEFAULT, so a live database still holding NULLs aborts the migration loudly rather than corrupting curation data. The fail-closed shape is deliberate: commit 896585e0 records that EF's generated `defaultValue: 0` was removed by hand because it emitted `UPDATE abwab_doors SET section_id = 0 WHERE section_id IS NULL` onto a section id that does not exist. — Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/20260802062011_RequireAbwabDoorSection.cs:13-20 (whole Up body); `git show --stat 896585e0` commit message
- **[AREA 3]** The NOT NULL migration is already released — `git merge-base --is-ancestor 896585e0 main` returns YES, with `b666cb38 chore: trigger Railway redeploy of the abwab release` after it. Nothing in this area is actively corrupting production data. — git merge-base --is-ancestor 896585e0 main → YES; git log --oneline -3 main
- **[AREA 3]** EF configuration, migration, and model snapshot agree on every Abwab table — column names/nullability, all unique and filtered indexes, NULLS-NOT-DISTINCT annotations, both CHECK constraints, and every OnDelete behavior. No drift between the three. — AbwabDoorConfiguration.cs:79-89 vs 20260728144026_AddAbwabDoorsAndSections.cs:120-131 vs QuranDashboardDbContextModelSnapshot.cs:104-118; AbwabDoorRelationConfiguration.cs:9-21/82-87 vs 20260729135714_AddAbwabDoorRelations.cs:38-39/54-69 vs snapshot :244-257; AbwabTemplateNodeConfiguration.cs:81-92 vs 20260729162330_AddAbwabTemplates.cs:76-104 vs snapshot :542-552 and :472
- **[AREA 3]** section_id is non-nullable in all three representations after the late migration — entity `int`, configuration `IsRequired()`, snapshot `b.Property<int>("SectionId")` with `.IsRequired()` on the FK. — AbwabDoor.cs:7; AbwabDoorConfiguration.cs:16-18; QuranDashboardDbContextModelSnapshot.cs:84-86 and :2690
- **[AREA 3]** Re-sectioning cascades to descendants INCLUDING archived rows on all three paths (move, bulk-move, restore) — the descendant query carries no DeletedAtUtc filter, and the parent map it walks is built from every row. — EfAbwabDoorsWriter.cs:530-532 (`.Where(d => descendantIds.Contains(d.Id))`, no deleted filter); EfAbwabDoorsWriter.cs:616-618 (`db.AbwabDoors.Select(d => new { d.Id, d.ParentId })`, no filter); pinned by AbwabDoorWriteBehaviorTests.cs:695 and :570
- **[AREA 3]** The cycle guard walks the same all-rows map, so a move cannot nest a door under its own descendant through an archived connecting node. — EfAbwabDoorsWriter.cs:566-577 `EnsureNotCycle` over the map from :614-624; AbwabDoorWriteBehaviorTests.cs:15 and :30
- **[AREA 3]** Restore gives back exactly what its archive claimed — descendants are matched on the archive's own deleted_at timestamp captured before the door's is cleared, so an independently archived descendant is not resurrected. — EfAbwabDoorsWriter.cs:399 (`var archivedAt = door.DeletedAtUtc;`) and :415-417 (`d.DeletedAtUtc == archivedAt.Value`); AbwabDoorWriteBehaviorTests.cs:434
- **[AREA 3]** Restore of an already-live door is fully gated: no destination resolution, no re-section, no per-scope or global renumber. The whole body is inside `if (archivedAt.HasValue)`. — EfAbwabDoorsWriter.cs:405-441; AbwabDoorWriteBehaviorTests.cs:638 and :669
- **[AREA 3]** A restored child derives its section from its live parent's CURRENT section, read fresh, never from the value stored on the archived row; a stated section that disagrees is refused. — EfAbwabDoorsWriter.cs:388-395 (parent loaded fresh) and :451-459 (`return parent.SectionId;` after the mismatch throw); AbwabDoorWriteBehaviorTests.cs:604 and :547
- **[AREA 3]** The `global_order_value IS NOT NULL ⟺ (parent_id IS NULL AND deleted_at IS NULL)` biconditional holds across all seven write paths: departures are dropped by excludeIds because the read still shows pre-SaveChanges state, arrivals are appended in code, and archive nulls the column for a root. — EfAbwabDoorsWriter.cs:38-41, :137-145, :262-275, :328/341-344, :364/369-372, :430-433, :589-592, with the read at :682-687
- **[AREA 3]** Section-scope reorder never touches GlobalOrderValue and Global-scope reorder never touches OrderValue — the two resequencers each write exactly one column and ReorderWithinAsync takes which one as a parameter. — EfAbwabDoorsWriter.cs:661-668 (`Resequence` sets OrderValue only) vs :670-677 (`ResequenceGlobal` sets GlobalOrderValue only), dispatched at :172 and :180; AbwabDoorWriteBehaviorTests.cs:137 and :154
- **[AREA 3]** Relations are stored as a canonical pair for all three types and dormancy is derived, never stored — there is no is_dormant/is_visible column on the entity, in the configuration, or in the migration's column list. — EfAbwabRelationsWriter.cs:45-46; AbwabDoorRelation.cs:1-24 (full entity, no such property); AbwabDoorRelationConfiguration.cs:28-70; 20260729135714_AddAbwabDoorRelations.cs:19-33
- **[AREA 3]** Both readers express dormancy identically as a join on abwab_doors for door_a_id and door_b_id plus the relation's own deleted_at; the partial unique index filters on the relation's deleted_at only, so a dormant row still occupies its pair. — EfAbwabTreeReader.cs:62-70; EfAbwabRelationsReader.cs:19-26; AbwabDoorRelationConfiguration.cs:82-84
- **[AREA 3]** No door or section write touches abwab_door_relations. `EfAbwabDoorsWriter` and `EfAbwabSectionsWriter` contain no reference to AbwabDoorRelations at all. — EfAbwabDoorsWriter.cs:1-767 and EfAbwabSectionsWriter.cs:1-143 — the only DbSets used are AbwabDoors, AbwabSections, AbwabDoorAliases
- **[AREA 3]** A relation direction is never silently defaulted: the handler rejects a Comprehensiveness add with no direction and a non-Comprehensiveness add that carries one, before the writer's `ResolveBroaderDoorId` (whose null branch would otherwise mean "target is broader") can run. — AddDoorRelationsHandler.cs:28-30 and :66 `IsDirectionValidFor`; AbwabDoorRelationsController.cs:47-48 maps it to 400
- **[AREA 3]** Template apply copies the root's direct children only, never the root itself, and refuses an empty-root template before any target row is read. — EfAbwabTemplateApplyWriter.cs:39-42 (empty-root throw) precedes :44-47 (target read); :94-100 iterates `rootChildren`, and `rootNode` is only ever used as a key at :39
- **[AREA 3]** Apply's collision key is (target, child name), computed against the target's live children before any insert, and the pairs are ordered by caller target order then template sibling order. — EfAbwabTemplateApplyWriter.cs:61-84, with the caller order restored at :54 (`targets.OrderBy(t => targetIds.IndexOf(t.Id))`)
- **[AREA 3]** Apply is atomic across all levels: one explicit transaction spans the level-order saves, so a partial subtree cannot commit. — EfAbwabTemplateApplyWriter.cs:18 (`BeginTransactionAsync`) … :130 (`CommitAsync`), with the per-level saves at :103 and :126
- **[AREA 3]** Every bulk write is a single SaveChanges, so one stale row fails the whole batch — bulk move, bulk archive, move, delete and restore each save exactly once; only create/edit (two saves: door then aliases) and apply take an explicit transaction. — EfAbwabDoorsWriter.cs:296, :346, :147, :374, :443 (single saves) vs :43-49 and :77-83 (explicit transactions); AbwabDoorWriteBehaviorTests.cs:773
- **[AREA 3]** The optimistic token is always applied as OriginalValue, never CurrentValue, on every path that carries one, including per row in the two bulk paths. — EfAbwabDoorsWriter.cs:69, :112, :196, :260, :320, :359, :397; EfAbwabSectionsWriter.cs:37, :91
- **[AREA 3]** Aliases go through one normalization helper on all three write surfaces — the doors diff, the template node writes, and the apply inserts. — AbwabAliasNormalization.cs:5-6; EfAbwabDoorsWriter.cs:692; EfAbwabTemplatesWriter.cs:30, :105, :134; EfAbwabTemplateApplyWriter.cs:163
- **[AREA 3]** The tree reader includes archived doors and flags them, and excludes archived sections — no filter on the doors query at all, `DeletedAtUtc == null` on sections. — EfAbwabTreeReader.cs:15-17 (no Where) and :11; the flag at :50 (`d.DeletedAtUtc != null`)
- **[AREA 3]** All three counts on the snapshot are live-only and correctly scoped: DirectChildCount over live children, DoorsInScopeCount over every live door with that section at any depth, RelationCount over live-endpoint relations counted for both endpoints in one grouped pass. — EfAbwabTreeReader.cs:25-28, :29-32, :60-80
- **[AREA 3]** Nothing in this area invents, derives, or corrects Quran data. RepresentativeAyahText is free text on both the door and the template node, is copied verbatim by apply, and is never an FK or a verified reference. — AbwabDoor.cs:13-14; AbwabTemplateNode.cs:14-15; AbwabDoorConfiguration.cs:30-31 (plain text column); EfAbwabTemplateApplyWriter.cs:155
- **[AREA 3]** No Abwab table or column carries a HasDefaultValue anywhere in the six configurations — which is exactly what makes the NOT NULL migration fail-closed rather than back-filling. — AbwabDoorConfiguration.cs, AbwabSectionConfiguration.cs, AbwabDoorAliasConfiguration.cs, AbwabDoorRelationConfiguration.cs, AbwabTemplateConfiguration.cs, AbwabTemplateNodeConfiguration.cs — zero occurrences of HasDefaultValue
- **[AREA 3]** Persistence exposes nothing beyond the known 21 write and 4 read Abwab routes; the DbContext's six Abwab DbSets are reached only through the five writer and three reader interfaces, each writer DI-wrapped by its invalidating decorator. — QuranDashboardDbContext.cs:56-61; AbwabDependencyInjection.cs:18-30; Controllers/Abwab/ = 21 write attributes + 4 HttpGet
- **[Area 2]** Domain does not depend on Application or Infrastructure. AbwabRelationDirection lives in Application.Abstractions and is never persisted; the domain entity stores an id instead. — Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoorRelation.cs:12 ('public int? BroaderDoorId { get; set; }') — no reference to AbwabRelationDirection anywhere in Backend/domain/QuranDashboard.Domain/Abwab/*.cs; the direction is derived at read time at Backend/infrastructure/.../Reads/Abwab/EfAbwabRelationsReader.cs:50-61.
- **[Area 2]** Application and Application.Abstractions do not depend on EF Core, Npgsql or Infrastructure. — grep -rn 'EntityFrameworkCore|Npgsql|Infrastructure' --include='*.cs' over Backend/application/QuranDashboard.Application/Abwab/ and Backend/application/QuranDashboard.Application.Abstractions/Abwab/ returns zero hits. Every handler depends only on an interface: e.g. CreateDoorHandler.cs:5-7 (ILogger + IAbwabDoorsWriter), GetAbwabTreeHandler.cs:5-7 (IAbwabTreeReader).
- **[Area 2]** No exception type in Abstractions/Abwab is dead: all 21 are thrown at least once, and all 21 have at least one handler catch site. — Full throw/catch table in the evidence section below; throw sites enumerated by grep -rn 'throw new Abwab' over Backend/ (41 sites across EfAbwabDoorsWriter, EfAbwabSectionsWriter, EfAbwabRelationsWriter, EfAbwabTemplatesWriter, EfAbwabTemplateApplyWriter). The only mapping gap is the CreateDoorHandler.cs:37 finding above.
- **[Area 2]** Every Outcome union variant across the fourteen Abwab handlers is producible by its own handler — with exactly one exception. — Swept all fourteen unions against their handlers. Only ReorderDoorOutcome.InvalidScope (ReorderDoorOutcome.cs:12) is constructed outside its handler, at AbwabDoorsController.cs:112. Every other variant has a construction site inside its handler; e.g. RestoreDoorOutcome's seven non-Success variants map 1:1 to RestoreDoorHandler.cs:22,33,38,43,48,53,58.
- **[Area 2]** global_order_value IS NOT NULL ⟺ (parent_id IS NULL AND deleted_at IS NULL) is maintained by every root-affecting write. — EfAbwabDoorsWriter.cs:38-41 (create root → MaintainGlobalOrderAsync arrival), :137-145 (move root→nested nulls it + departs; nested→root arrives; root→root untouched), :267-275 (bulk-move, same two branches), :369-372 (delete/archive departs) with :589-592 nulling it in ArchiveSubtreeAsync, :328 and :341-344 (bulk-archive departures), :430-433 (restore root arrives). The Global reorder read itself filters exactly on that predicate: :167-170 'Where(d => d.ParentId == null && d.DeletedAtUtc == null)'.
- **[Area 2]** The tree reader's three counts really do count LIVE rows only, as its README claims. — EfAbwabTreeReader.cs:25-28 (liveChildCounts filters DeletedAtUtc == null && ParentId.HasValue), :29-32 (liveSectionCounts filters DeletedAtUtc == null), :60-80 (GetLiveRelationCountsAsync joins both endpoint doors on DeletedAtUtc == null). Matches Reads/Abwab/README.md:52-56 and :96-101.
- **[Area 2]** The deliberate create-rejects / move-ignores section asymmetry documented in the write README matches the code exactly — correctly NOT a finding. — EfAbwabDoorsWriter.ResolveCreateSectionAsync:494-500 throws AbwabSectionParentMismatchException when a stated section disagrees with the parent's; ResolveTargetSectionAsync:544-563 (used by MoveAsync:101 and BulkMoveAsync:229) ignores targetSectionId entirely once targetParentId is set. Declared deliberate at Persistence/Writes/Abwab/README.md:160-163 ('create rejects a disagreeing section, move ignores targetSectionId whenever targetParentId is set … Do not "harmonize" one into the other') and consistent with MoveDoorOutcome.cs having no SectionParentMismatch variant.
- **[Area 2]** Relation reads distinguish 'unknown door' (null → 404) from 'door with nothing visible' (empty list → 200), as the README requires. — EfAbwabRelationsReader.cs:12-17 returns null only when no AbwabDoors row with that id exists (note: no DeletedAtUtc filter, so an ARCHIVED door correctly answers 200 []); GetDoorRelationsHandler.cs:18-21 maps null → NotFound; AbwabDoorRelationsController.cs:25-26 maps that to 404. Matches Reads/Abwab/README.md:22-24.
- **[Area 2]** Optimistic concurrency really is an xmin concurrency token, so the version-carrying handlers' StaleVersion arms are load-bearing rather than decorative. — Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Abwab/AbwabDoorConfiguration.cs:66-67 — 'builder.Property(d => d.Version).IsRowVersion();'. Applied as OriginalValue (never CurrentValue) at EfAbwabDoorsWriter.cs:69, :112, :196, :260, :320, :359, :397.
- **[Area 2]** No handler swallows an exception with a catch-all, hardcodes a success, or contains a defensive guard for an impossible case (beyond the LOW finding above). — Every catch in Backend/application/QuranDashboard.Application/Abwab/**/*.cs names a specific Abwab exception type; grep shows zero 'catch (Exception', zero 'catch {', zero 'catch (Exception ex) when'. The one non-outcome throw is a genuine unreachable-variant assertion: DeleteSectionHandler.cs:38-39 'default: throw new InvalidOperationException($"Unhandled {nameof(AbwabSectionDeleteResult)} variant.")'.
- **[Area 2]** Handler file sizes are far inside BACKEND_STRUCTURE thresholds; no handler carries more than one responsibility. — Largest handler in the area is AddDoorRelationsHandler.cs at 70 lines; BulkMoveDoorsHandler.cs 66, CreateDoorHandler.cs 63, MoveDoorHandler.cs 61, RestoreDoorHandler.cs 61. Every one is validate → call one writer/reader → map exceptions to an outcome.
- **[Area 2]** No Quran text, morphology, identity, alignment or counting-scope logic exists anywhere in this area. — The only Quran-adjacent field is free text and is declared as such at Backend/domain/QuranDashboard.Domain/Abwab/AbwabDoor.cs:13-14 ('Free text only — never an FK or a verified Quran reference') and carried verbatim through AbwabDoorDto.cs:9 / AbwabTreeDto.cs:22. No handler parses, normalises, counts or validates it.

### Frontend (areas 5–7)

- **[AREA 5]** README claim CONFIRMED: `snapshotValidator` is `bootId + tree generation` (README.md:353). — Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Abwab/AbwabCacheGeneration.cs:7 (`_bootId = Guid.NewGuid().ToString("N")[..8]`) and :16 (`TreeETag() => $"\"abwab-tree-{_bootId}-{Interlocked.Read(ref _treeGeneration)}\""`); frontend side `abwab-snapshot.facade.ts:52` (`this.etagState.set(response.headers.get('ETag'))`) and :24 (`snapshotValidator = this.etagState.asReadonly()`).
- **[AREA 5]** README claim CONFIRMED: relation writes move the tree validator, so the relations cache keyed on `snapshotValidator` cannot serve a stale list after a relation add/delete (README.md:355-357). — Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Abwab/InvalidatingAbwabRelationsWriter.cs:28 and :40 both call `_invalidator.InvalidateTree()`; `AbwabCacheGeneration.cs:12` increments `_treeGeneration`; frontend eviction at abwab-relations.controller.ts:44-51.
- **[AREA 5]** Reversal #1 has NO surviving remnant: reveal-in-tree does not clear `q`. — abwab-url-sync.ts:96-98 writes `q` only when `changes.q !== undefined`; `q` is absent from the scope-invalidation block at abwab-url-sync.ts:100-105 (which clears only `door`, `card`, `modal`); the reveal patch at abwab-page.component.ts:410-413 sets `door`/`modal`/`section`/`view` and never `q`.
- **[AREA 5]** Reversal #2 has NO surviving remnant in the state layer: the relations flag count is exposed ungated at zero. — abwab-page-overlays.controller.ts:318-321 — `relationsAnchorCount = computed(() => { const id = this.relationsAnchorId(); return id === null ? 0 : (this.byId().get(id)?.relationCount ?? 0); })` — no `> 0` guard; `abwab-tree.builder.ts:64` copies `relationCount` onto every node unconditionally.
- **[AREA 5]** Reversal #3 has NO surviving remnant in the state layer: template apply sends only target door ids, no root-copy flag or root-inclusive wording. — abwab-templates.controller.ts:56-65 — `applyTemplate(templateId, targetDoorIds)` posts `{ targetDoorIds: [...targetDoorIds] }` and refreshes `'none'`; abwab-templates.api.ts:45-47 posts to `/templates/{id}/apply`. `AbwabTemplateVm.nodeCount` counts descendants only, excluding the root (abwab-templates.models.ts:58,62 — `descendantCount` is incremented only inside the children map, never for `rootDto`).
- **[AREA 5]** `ApiResponse<T>` is unwrapped exclusively in the state/facade layer; no component or page ever touches `isSuccess`. — `grep -rn "isSuccess" pages components --include="*.ts" --include="*.html" | grep -v '\.spec\.'` returns ZERO hits. The only unwrap sites are abwab-snapshot.facade.ts:50, abwab-templates.facade.ts:87 and :124, abwab-relations.controller.ts:56, abwab-write.controller.ts:183, abwab-templates.controller.ts:84.
- **[AREA 5]** Loading is never conflated with empty in either facade — the pre-load state is distinguishable from a genuinely empty result. — abwab-snapshot.facade.ts:31 — `isEmpty = computed(() => (this.rawTree()?.doors.length ?? -1) === 0)` (null tree → -1 → false). abwab-templates.facade.ts:38 — `isEmpty = computed(() => (this.rawList()?.length ?? -1) === 0)`. Consumers gate on `facade.isLoading() && !facade.snapshot()` (abwab-page.component.html:64, :58-59).
- **[AREA 5]** Both transport errors AND backend-controlled failures (`isSuccess === false`) are handled distinctly on every read and write path. — Reads: abwab-snapshot.facade.ts:50-56 (envelope branch) + :59-68 (`catchError`, with the 304 carve-out at :62-64); abwab-templates.facade.ts:87-93 + :96-105 and :124-131 + :134-143. Writes: abwab-write.controller.ts:183-194 (`isSuccess === false` → `invalid`, no refresh) vs :197-206 → `toAbwabWriteFailure` (:31-44); relations reads abwab-relations.controller.ts:56-58 vs :65.
- **[AREA 5]** A `304 Not Modified` keeps the value and the validator and sets NO error — a stale-data banner cannot appear on a successful revalidation. — abwab-snapshot.facade.ts:62-64 — `if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.NotModified) { return of(this.snapshot()); }` placed BEFORE `this.errorState.set(...)` at :66. Same shape at abwab-templates.facade.ts:11-13 (`isNotModified`), :99-101 and :137-139.
- **[AREA 5]** No `computed` reads a signal it also writes, and no `effect` writes state it depends on. — Both page effects route their writes through `untracked`: abwab-page.component.ts:241 (`untracked(() => this.selection.select(doorId, node.version))`) and :248 (`untracked(() => this.modalUrl.reconcileOpen())`). `restorableModal` (abwab-modal-url.controller.ts:26-36) reads `modalSignal`/`facade.snapshot`/`overlays.selectedDoor` and writes nothing. `moveExcludedIds` (:181-194), `moveSectionIds` (:196-201), `restoreAncestors` (:260-277) are pure reads. `AbwabSelectionStore`'s five computeds (abwab-selection.store.ts:24-28) all read `this.state()` only.
- **[AREA 5]** The `modal` key's fail-closed parse is complete: an id on the OPEN form, an id on any non-`relations` kind, a non-positive id, and a door-dependent kind without a valid `door=` all parse to `null`. — abwab-url-sync.ts:36 — `if (!closed || kind !== 'relations' || subjectDoorId === null) { return null; }`; :35 routes the id through `parsePositiveId` (:17-23 → `isPositiveId`, abwab.models.ts:95-97); :45-47 — `if (door === null && isDoorDependentAbwabModalKind(body)) { return null; }`; :42-44 rejects any unknown kind via `isAbwabModalKind` (abwab.models.ts:108-110).
- **[AREA 5]** The invalid serialised form `<non-relations-kind>-<id>-closed` is unreachable — no writer ever produces it. — Every `modal` write in the page passes `subjectDoorId: null` except one: abwab-page.component.ts:412 `modal: anchorId === null ? null : { kind: 'relations' as const, closed: true, subjectDoorId: anchorId }`. The other four writes are :539 (`{ kind: retained.kind, closed: false, subjectDoorId: null }`), :545 (`modal: null`), :556 (`{ kind, closed: true, subjectDoorId: null }`), :580 (`{ kind, closed: false, subjectDoorId: null }`).
- **[AREA 5]** An archived door can never become the single selection via the URL: the archive view writes no `door=` key. — `AbwabArchiveViewComponent` declares exactly one output — abwab-archive-view.component.ts:24 `readonly restoreRequested = output<number>();` — and the page binds only `(restoreRequested)="onRestoreRequested($event)"` (abwab-page.component.html:119-123). Turning archive ON clears `door`/`card`/`modal` (abwab-url-sync.ts:100-105, `changes.archive === true`), so no live-view selection survives into the archive view either.
- **[AREA 5]** A `relations-<id>-closed` restore control refuses a subject that is missing or archived, and a plain `-closed` refuses a door-dependent kind whose door is missing, archived, or no longer the selection. — abwab-modal-url.controller.ts:31-34 — `const node = this.facade.snapshot()?.byId.get(modal.subjectDoorId); return !!node && !node.isArchived ? modal : null;`; :102-112 `canOpen` — `return !!node && !node.isArchived && this.overlays.selectedDoor()?.id === doorId;`.
- **[AREA 5]** The relations cache never persists a response fetched under a validator that moved mid-flight, and never serves anything while the validator is null. — abwab-relations.controller.ts:34 — `const cached = validator === null ? undefined : this.cache.get(doorId);`; :60-62 — `if (requestValidator !== null && this.adoptCurrentValidator() === requestValidator) { this.cache.set(doorId, relations); }`; :44-51 `adoptCurrentValidator` clears the whole map on any validator change.
- **[AREA 5]** The templates facade's per-template ETag is keyed by template id, so a validator never travels to a different template (README.md:690-694 CONFIRMED). — abwab-templates.facade.ts:22 `private selectedEtagState: { id: number; etag: string } | null = null;`; :118 `const heldEtag = this.selectedEtagState?.id === templateId ? this.selectedEtagState.etag : null;`; :127 `this.selectedEtagState = etag ? { id: templateId, etag } : null;`; dropped with the value at :65 (`clearSelection`).
- **[AREA 5]** The workshop never names one template and writes to another (README.md:923-931 CONFIRMED). — abwab-templates.facade.ts:40-46 — `selectedTemplate = computed(() => { const dto = this.rawSelected(); if (dto === null || dto.id !== this.selectedIdState()) { return null; } return buildAbwabTemplateTree(dto); })`.
- **[AREA 5]** `byId` is complete — every door in the DTO lands in the map, so `countLiveAbwabDoors` cannot undercount and a `door=`/`modal` subject lookup cannot false-negative on an archived node. — abwab-tree.builder.ts:71-74 builds live roots (`!d.isArchived && d.parentId == null`) with `includeArchivedChildren=false`, and :76-79 builds `archivedRoots` from `d.isArchived && (d.parentId == null || !doorById.get(d.parentId)?.isArchived)` with `includeArchivedChildren=true` — so an archived child of a LIVE parent, excluded from the live subtree at :36-38, is picked up as an archived root. `build()` writes `byId.set(node.id, node)` at :67 for every node it constructs.
- **[AREA 5]** The bulk-set rebind drops archived ids while the single selection keeps the missing-only rule — the deliberate asymmetry the README documents (README.md:682-693) holds in code. — abwab-selection.store.ts:82-88 — `if (node && !node.isArchived) { nextBulk.set(doorId, node.version); }`; :92-93 — `selectedDoorId: selectedNode ? current.selectedDoorId : null, selectedVersion: selectedNode ? selectedNode.version : null`. Second-line defense at submit: abwab-write.controller.ts:164-172.
- **[AREA 5]** Bulk-archive's confirm count is a union of subtrees, not a sum — an ancestor+descendant pair is not double-counted (README.md:743-747 CONFIRMED). — abwab-write.controller.ts:70-90 `bulkLiveSubtreeCount` — `const counted = new Set<number>(); const walk = (node) => { if (counted.has(node.id)) return; counted.add(node.id); node.children.forEach(walk); }; ... return counted.size;`, contrasted with the single-door `countLiveSubtree` at :46-48.
- **[AREA 5]** No `any` and no fabricated fallback data anywhere in the assigned scope; every error path uses a locked Arabic label rather than invented content. — `grep -n ': any\|as any\| any>' state/*.ts data-access/*.ts models/*.ts` (excluding specs) returns nothing. Fallbacks are label constants only: abwab-snapshot.facade.ts:55,66 (`ABWAB_LABELS.loadErrorFallback`), abwab-relations.controller.ts:57,65 (`relationsLoadError`), abwab-write.controller.ts:37,40,43,192 (`writeConflictFallback`/`writeInvalidFallback`/`writeTransportFallback`). No Quran text is generated, mutated, normalised or searched — `representativeAyahText` is copied verbatim (abwab-tree.builder.ts:45; abwab-templates.models.ts:69) and is NOT a search field (`nodeMatchesQuery` reads `name` and `aliases` only, abwab-tree.builder.ts:120-125).
- **[AREA 5]** `urlBackedKind` reading a non-signal mutable field is safe: it is never read from a template. — `grep -rn "urlBackedKind" . --include="*.ts" --include="*.html" | grep -v '\.spec\.'` yields exactly two hits — the declaration at abwab-modal-url.controller.ts:93 and one imperative call inside `closeUrlBackedModal` at abwab-page.component.ts:550. The backing field `opened` (abwab-modal-url.controller.ts:24) is therefore never a change-detection dependency.
- **[AREA 5]** The two root order spaces stay separated in the builder — the superset sorts by `globalOrderValue`, every section tab by `orderValue`, and nested doors always by `orderValue` (README.md:694-705 CONFIRMED). — abwab-tree.builder.ts:71-73 `liveRoots ... .sort(byGlobalOrderThenId)` (:10-12); :96-104 `filterAbwabRootsBySection` returns `roots` untouched for `sectionId === null` and `.sort(byNodeOrderThenId)` (:14-16) otherwise; children sorted by `byOrderThenId` (:6-8) at :29-31.
- **[AREA 5]** `createDoor` omits `sectionId` from the wire body (rather than sending `undefined`) whenever `parentId` is set (README.md:738-742 CONFIRMED). — abwab.api.ts:29-32 — `function buildCreateDoorBody(command) { const { sectionId, ...rest } = command; return command.parentId != null ? rest : { ...rest, sectionId }; }`, applied at :63.
- **[AREA 5]** A `204 No Content` (null envelope) is treated as a payload-less success and still triggers the refresh-after-write invariant (README.md:947-960 CONFIRMED). — abwab-write.controller.ts:183 — `if (response === null || response.isSuccess) {` … :189 `this.refreshAndRebind();`. The four 204-answering routes are typed `ApiResponse<unknown> | null` at abwab.api.ts:54, :86, :102 and abwab-templates.api.ts:41, :61.
- **[AREA 5]** Refresh-after-write is unconditional across every write path — no scope-narrowed refresh exists (README.md:672-681 CONFIRMED). — abwab-write.controller.ts:259-265 `refreshAndRebind()` calls `this.facade.refresh()` (the full tree GET) and `this.selection.rebindTo(snapshot)` with no scope parameter; it is the single call site reached from `handleSuccess` (:189), which every doors/sections/relations write funnels through via `dispatch` (:174-179).
- **[AREA 5]** `conditionalHeaders` sends `If-None-Match` only when a validator is actually held, and both APIs default to no validator. — conditional-request.ts:3-5 — `export function conditionalHeaders(etag: string | null): HttpHeaders | undefined { return etag ? new HttpHeaders({ 'If-None-Match': etag }) : undefined; }`; call sites abwab.api.ts:39,42 and abwab-templates.api.ts:23,26 / :30,33, all with `etag: string | null = null`.
- **[AREA 5]** Bulk mode cannot be entered in the archive view, and entering the archive view drops it. — abwab-selection.store.ts:41-46 `setArchiveViewActive(active)` → `if (active) { this.setBulkMode(false); }`; :48-50 `setBulkMode(on)` → `if (on && this.archiveViewActive()) { return; }`. Driven from abwab-page.component.ts:263 on every param emission.
- **[AREA 5]** The templates controller does not fork the 409 policy — both controllers share one status→outcome mapping (README.md:891-897 CONFIRMED). — `toAbwabWriteFailure` is module-scope in abwab-write.controller.ts:31-44; imported and called by abwab-templates.controller.ts:6 and :95. The doors controller reaches it through the private `toFailureOutcome` at :267-269.
- **[AREA 5]** The 409/stale-version policy prefers the backend message and falls back to a locked label only when the backend supplies none. — abwab-write.controller.ts:33-37 — `const backendMessage = typeof body?.message === 'string' && body.message.length > 0 ? body.message : null; if (err.status === 409) { return { kind: 'conflict', message: backendMessage ?? ABWAB_LABELS.writeConflictFallback }; }`. Confirms README.md:753-762's account of the section-delete conflict copy.
- **[Area 6b]** Reversal #3 holds end to end: template apply copies the root's CHILDREN and never the root. The count fed to the preview is the descendant count with the root excluded. — models/abwab-templates.models.ts:58-79 (`let descendantCount = 0;` incremented only inside the children map, `return { …, nodeCount: descendantCount }`), consumed at pages/abwab-templates-page/abwab-templates-page.component.html:196 `[templateNodeCount]="facade.selectedTemplate()?.nodeCount ?? 0"` → abwab-template-copy-modal.component.ts:57
- **[Area 6b]** The template-copy confirmation copy says the same thing the request does — no surviving text implying the root is copied. — models/abwab.labels.ts:309 `templateCopyDescription: 'اختر الأبواب المستهدفة — عناصر القالب (بدون جذره) ستُنسخ داخل كل باب تختاره.'` and :310-311 `templateCopyPreview: … بكامل تفرعها — جذر القالب نفسه لا يُنسخ.`; request body is targets only, state/abwab-templates.controller.ts:61 `this.api.applyTemplate(templateId, { targetDoorIds: [...targetDoorIds] })`
- **[Area 6b]** A template holding only its root cannot be applied: it reaches the empty state and the confirm button stays disabled. — abwab-template-copy-modal.component.ts:60 `hasElements = computed(() => this.templateNodeCount() > 0)`; abwab-template-copy-modal.component.html:30 renders `templateCopyEmptyTemplate` and :59 `[disabled]="!hasElements() || pickedIds().size === 0"`
- **[Area 6b]** The archive view issues no request of its own — it is a pure partition of the cached snapshot. — abwab-archive-view.component.ts:21 `readonly roots = input<readonly AbwabNode[]>([])` is its only data source (no HttpClient, no facade injected); chain is pages/abwab-page/abwab-page.component.html:119-121 → abwab-page.component.ts:190-197 (`this.facade.snapshot()?.archivedRoots`) → state/abwab-tree.builder.ts:76-79
- **[Area 6b]** The archive view cannot show a live door, and cannot hide an archived one: `archivedRoots` selects exactly the archived doors whose parent is not archived, and archiving cascades server-side. — state/abwab-tree.builder.ts:76-79 `.filter((d) => d.isArchived && (d.parentId == null || !doorById.get(d.parentId)?.isArchived))`; cascade at Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:331-334 (`ArchiveSubtreeAsync` per loaded door). A restored subtree root leaves its children archived, and they re-enter as their own archived roots by the same filter.
- **[Area 6b]** Restore is offered only on the archived subtree root, matching the documented depth rule, and depth is read off the builder's partition rather than re-derived. — abwab-archive-view.component.html:32 `[disabled]="node.depth > 0"` with the «استرجع الأب أولًا» hint at :38-42, against README.md:165-169; depth is assigned by state/abwab-tree.builder.ts:79 `build(d, 0, true)`
- **[Area 6b]** Scroll lock is acquired and released symmetrically, is reference-counted, survives nesting, and cannot underflow. — shared/ui/modal-scroll-lock/modal-scroll-lock.directive.ts:12-18 (`ngOnInit` acquire / `ngOnDestroy` release) over shared/ui/modal-scroll-lock/scroll-lock.service.ts:12-31 (`lockCount` gate at 0 on both sides; `release()` returns early when `lockCount() === 0`). Nested case: relations modal `<section>` (abwab-relations-modal.component.html:9) + its confirm dialog (shared/ui/confirm-dialog/confirm-dialog.component.html:9) = 2 acquires, and both sections live inside `@if` blocks that destroy together.
- **[Area 6b]** All six authoring modals compose the shared fixed shell — none is a one-off — with `role="dialog"`, `aria-modal`, `dir="rtl"`, `aria-labelledby` pointing at its own `<h3>`, `qdModalScrollLock`, Escape-to-close and `cdkTrapFocusAutoCapture`. — `class="qd-modal qd-modal--fixed …"` at abwab-door-modal.component.html:4, abwab-template-node-modal.component.html:4, abwab-sections-modal.component.html:4, abwab-template-copy-modal.component.html:4 (+`--wide`), abwab-relations-modal.component.html:4 (+`--wide`), abwab-move-picker.component.html:4 (+`--wide`) — the exact three `--wide` consumers UI_STYLE_SYSTEM.md:1082-1084 names
- **[Area 6b]** No modal's own SCSS states a block-size or re-creates the §17 specificity trap the four deleted `max-block-size` caps were. — `grep -n 'max-block-size\|max-height\|block-size' ` over abwab-door-modal.component.scss, abwab-template-node-modal.component.scss, abwab-sections-modal.component.scss, abwab-template-copy-modal.component.scss, abwab-relations-modal.component.scss, abwab-move-picker.component.scss returns only `min-block-size: 1.1rem` (abwab-template-copy-modal.component.scss:30) and `min-block-size: 1.25rem` (abwab-relations-modal.component.scss:152) — reservations for the selected-summary line, not caps
- **[Area 6b]** Focus returns to the invoking element on close: every modal keeps `cdkTrapFocusAutoCapture`, which is the only thing that stores the previously focused element. — `cdkTrapFocusAutoCapture` at abwab-door-modal.component.html:11, abwab-template-node-modal.component.html:11, abwab-sections-modal.component.html:11, abwab-template-copy-modal.component.html:11, abwab-relations-modal.component.html:11, abwab-move-picker.component.html:11, shared/ui/confirm-dialog/confirm-dialog.component.html:11 — the rule stated at README.md:615-618
- **[Area 6b]** The queued `focusFirstField()` / `focusSearch()` calls are documented deliberate behaviour, not the "corrected after the fact" anti-pattern — the aimed target is a real `cdkFocusInitial`. — `cdkFocusInitial` at abwab-door-fields-form.component.html:9 (name input) and abwab-door-picker.component.html:2 (search input), serving all four modals per README.md:609-620; the queued calls sit at abwab-door-modal.component.ts:105, abwab-template-node-modal.component.ts:68, abwab-relations-modal.component.ts:252, abwab-template-copy-modal.component.ts:95
- **[Area 6b]** Escape closes every one of the six, and every backdrop click closes — the policy does not diverge on whether it closes. — `(keydown.escape)` + backdrop `(click)` pairs: door 14/2 `requestClose()`, template-node 14/2 `requestClose()`, sections 14/2 `requestClose()`, copy 14/2 `close()`, relations 14/2 `close()`, move-picker 14/2 `cancel()`. Every backdrop is `.qd-modal-backdrop` and every dialog stops propagation on its own click (line 13 in all six).
- **[Area 6b]** RTL is correct in the archive view's keyboard model: `resolveDirection()` finds a real `dir` host. — abwab-archive-view.component.ts:124-127 `closest('[dir]')` resolves against `src/index.html:2` `<html lang="ar" dir="rtl">`, so Arrow expand/collapse is mirrored correctly; the six modals additionally set `dir="rtl"` on their own `<section>` (line 7 in each).
- **[Area 6b]** The relations modal legitimately has no doors-load error/retry state: its picker consumes the already-loaded page snapshot, so there is no separate load to fail. — pages/abwab-page/abwab-page.component.ts:188 `pickerLiveRoots = computed(() => this.facade.snapshot()?.liveRoots ?? NO_ROOTS)` bound at abwab-page.component.html:236; only the templates page's copy modal owns a second load, and it is the one that passes `[status]`/`[errorMessage]`/`(retry)` (abwab-template-copy-modal.component.html:38-46)
- **[Area 6b]** Both pickers show live doors only — no archived door can be picked as a relation target, copy target, or move destination. — state/abwab-tree.builder.ts:72-74 builds `liveRoots` with `build(d, 0, false)`, whose child filter (`:35-38`) drops `child.isArchived`; that same array is what abwab-page.component.ts:188 and abwab-templates-page.component.ts:73 hand to both pickers
- **[Area 6b]** The move picker's client-side cycle guard cannot offer a door inside the moved subtree: excluding a node also removes everything beneath it, because the walk never descends into an excluded child. — abwab-move-picker.component.ts:84-99 (`const children = node.children.filter((child) => !excluded.has(child.id))`, and roots gated by `!excluded.has(root.id)`), matching README.md:219-221 "`excludedIds` is the moved door(s) plus every descendant, the client half of the cycle guard"
- **[Area 6b]** Every count in this area names the same scope its query computes. — `nodeCount` = descendants excluding root (abwab-templates.models.ts:58-79) under a label that says «بدون جذره» (abwab.labels.ts:309); the relations header chip renders `relations().length` = relation ROWS inside the `<h3>` whose text is «صلات «X»» (abwab-relations-modal.component.html:17-24); `templateCopyConfirmButton(count)` counts picked DOORS (abwab-template-copy-modal.component.ts:85 over `pickedIds().size`, label at abwab.labels.ts:319-320 «انسخ إلى N بابًا»); `templateAppliedAnnouncement(targetDoorIds.length)` counts the same doors (state/abwab-templates.controller.ts:64) and apply is all-or-nothing (README.md:302), so the announced number is in scope
- **[Area 6b]** Keyboard selection in the door picker works despite `(click)="$event.preventDefault()"` on the box — Space dispatches a synthetic click that bubbles to the row handler. — abwab-door-picker.component.html:20 `(click)="togglePicked(row)"` on the row wrapper receives the bubbled activation click from the input at :48; there is no keyboard-inaccessibility defect here (only the dead `(change)` path reported separately)
- **[Area 6b]** Nested confirm dialogs are exactly one level deep everywhere in this area — no confirmation above a confirmation. — The two nestings are abwab-sections-modal.component.html:138-155 and abwab-relations-modal.component.html:173-191, both a single `qd-confirm-dialog` above one authoring modal; `abwab-door-restore-modal` IS the confirm (abwab-door-restore-modal.component.html:1) and nests nothing
- **[Area 6b]** Door-restore only sends a destination when it genuinely changes one, and cannot be confirmed without one when the section was retired. — abwab-door-restore-modal.component.ts:100-105 spreads `sectionId` only when `needsDestination() && chosen !== null && chosen !== door.sectionId`; gate at :42-52 `destinationRequired = needsDestination && sectionRetired`, `confirmDisabled = destinationRequired && chosenSectionId() === null`, wired to the confirm dialog at abwab-door-restore-modal.component.html:7
- **[Area 6b]** The relations modal reads the gating relation count untracked, so a post-write snapshot refresh cannot reset an open draft. — abwab-relations-modal.component.ts:238-253 — `this.anchorRelationCount()` at :249 sits inside `untracked(() => { … })`, matching README.md:793-795
- **[Area 6b]** Each modal distinguishes error from empty from loading with the shared primitives rather than hand-rolled text. — `qd-state variant="error"` at abwab-sections-modal.component.html:22, abwab-template-copy-modal.component.html:23, abwab-relations-modal.component.html:30, abwab-door-restore-modal.component.html:14, abwab-door-picker.component.html:68; `variant="empty"` at abwab-template-copy-modal.component.html:30, abwab-relations-modal.component.html:56, abwab-door-picker.component.html:77,79; `qd-skeleton-rows` at abwab-relations-modal.component.html:47 and abwab-door-picker.component.html:61 — README.md:786-792
- **[Area 6a]** Reversal #1 — reveal-in-tree does NOT clear the search query `q`. There is no surviving q-clearing path anywhere in the feature. — abwab-page.component.ts:395-419 (`onRevealRequested` builds `door`, `modal`, conditional `section`, conditional `view` — no `q`); a repo-wide grep for writes of the key returns only `models/abwab.models.ts:159,179` (key name + default), `state/abwab-url-sync.ts:69` (parse) and `:97` (build), and the single writer `abwab-page.component.ts:286-288` (`onSearchQueryChanged`, bound to the toolbar's own output at abwab-page.component.html:81). `buildAbwabQueryParams`'s scope-invalidation block (abwab-url-sync.ts:100-105) clears door/card/modal and deliberately not `q`.
- **[Area 6a]** Reversal #2 — the relations flag is ALWAYS rendered, dimmed at zero, and clickable. No `@if (count > 0)` guard and no `disabled` binding survive. — abwab-tree.component.html:112-124 — the `<span class="abwab-tree__flags">` wrapper is unconditional (it is outside the `@if (row.hasChildren)` blocks that gate the three count badges) and contains a real `<button type="button">` with `[class.abwab-tree__flag--empty]="node.relationCount === 0"`, `(click)="onFlagClick($event, node.id)"`, `[attr.tabindex]="-1"` and an Arabic `aria-label`. No `disabled` attribute or binding appears on it. Pinned at abwab-tree.component.spec.ts:378-444.
- **[Area 6a]** Reversal #4 — the navigation entry is a dropdown, and `abwab.routes.ts` no longer records the abandoned "reached from the doors page header, not the sidebar" decision. — core/navigation/nav-menu.ts:5-27 defines `ABWAB_MENU_ITEMS` (الرئيسية / قوالب الأبواب / الأرشيف) and attaches them as `children` of the `abwab` nav item; features/abwab/abwab.routes.ts:1-23 contains only the two lazy route definitions and carries no such statement.
- **[Area 6a]** Skeleton rows are non-interactive — no skeleton row is focusable or clickable. — shared/ui/skeleton/skeleton-rows.component.html:5-14 — each row is a `<div … aria-hidden="true">` of `<span class="qd-skeleton">` children, with no tabindex, no role, no handlers; the only live element is a `qd-sr-only` `role="status"` label at `:2`. Both abwab consumers pass only `count`/`rowTemplate`/`loadingLabel` (abwab-page.component.html:88-93, abwab-templates-page.component.html:16-21,87-92).
- **[Area 6a]** A reserved slot can never render as an empty error box — every `[reserve]="true"` error site in scope is guarded by a truthy message. — abwab-page.component.html:97 (`@else if (facade.errorMessage() && !facade.snapshot())`) gating :101-107; abwab-templates-page.component.html:27 (`@else if (facade.errorMessage() && …)`) gating :28; abwab-templates-page.component.html:76 (`@if (facade.selectedErrorMessage(); as selectedError)`) gating :77-82; abwab-door-fields-form.component.html:1 (`@if (errorMessage(); as error)`) gating :2. `qd-state` itself only widens the box when `reserve()` is set (state.component.html:15) and hides an empty message via `qd-state__message--visible` (:16).
- **[Area 6a]** Tab-count scope agreement — the toolbar's badge label, the computed value, and the query behind it all mean *root doors*, and the per-section counts sum to the «كل الأبواب» count. — abwab-toolbar.component.html:13-18 / :30-36 render `totalRootCount()` and `rootCountFor(section.id)` with `aria-hidden` digits; the accessible name is `ABWAB_LABELS.tabRootCountAriaLabel` / `allDoorsTabRootCountAriaLabel` built on `ROOT_DOOR_FORMS` (abwab.labels.ts:65-70,78-80) — «باب رئيسي». The values come from `abwab-page.component.ts:121-124`: `rootCountBySectionId` from the builder and `totalRootCount = liveRoots.length`. `abwab-tree.builder.ts:81-84` builds `rootCountBySectionId` by iterating exactly `liveRoots`, so Σ over sections == `liveRoots.length` by construction, and `liveRoots` is the live-and-parentless filter at `:71-74`.
- **[Area 6a]** The tree's three count badges are all live-only and each carries a distinct, scope-declaring Arabic accessible name; the badge gate and the badge value describe the same set. — abwab-tree.builder.ts:40 computes `liveChildren = children.filter(c => !c.isArchived)`, and :55-63 derive `liveChildCount`, `liveDescendantCount`, `maxRelativeDepth` from it. In the live tree `build()` is called with `includeArchivedChildren = false` (:74), so `children.length > 0` ⇔ `liveChildCount > 0`, which is what `row.hasChildren` (abwab-tree-keyboard.controller.ts:17) gates all three badges on (abwab-tree.component.html:83,93,103). Labels: `rowChildCountAriaLabel` «… تحته مباشرة», `rowDescendantCountAriaLabel` «… تحته في كل المستويات», `rowDepthAriaLabel` «أعمق تفرّع تحته: …» (abwab.labels.ts:99-102).
- **[Area 6a]** The tree does NOT filter under a search — a zero-match query leaves the full tree standing with a zero count, and the tree's empty state is keyed to the unfiltered set. — abwab-page.component.html:144 guards on `visibleRoots()` (section-filtered, unpruned) and :148 feeds the tree `[roots]="visibleRoots()"`; the pruned `displayRoots()` (abwab-page.component.ts:140-143) is passed only to the cards branch (:129). Matches are conveyed by `[matchedIds]="treeMatchedIds()"` (:154) → `abwab-tree.component.html:22` `[class.abwab-tree__row--match]` → `abwab-tree.component.scss:98-100` (an inset 1px accent ring).
- **[Area 6a]** Enter-then-blur on the doors tree's order editor emits exactly once, and Escape emits nothing. — abwab-tree.component.ts:233-244 (`commitOrderEdit` returns early unless `editingId() === id`, and nulls it before emitting) and :226-231 (`cancelOrderEdit` guarded on the same id), bound at abwab-tree.component.html:67-68. Pinned by abwab-tree.component.spec.ts:247 (Escape) and :291 (blur after Enter).
- **[Area 6a]** No hardcoded colour palette and no physical left/right direction properties in any abwab component SCSS. — Basis is a repo-scoped grep over `src/app/features/abwab/**/*.scss` for `#rrggbb`/`rgb(`/`rgba(`/`hsl(`/named colours and for `left:`/`right:`/`margin-left`/`margin-right`/`padding-left`/`padding-right`/`border-left`/`border-right`/`text-align: left|right` — both returned zero matches. Spot-confirmed in the files read in full: abwab-tree.component.scss (every colour is `var(--qd-*)`, spacing via `inset-inline-start`/`padding-inline-*`), abwab-cards.component.scss:88-92 (`inset-inline-start`), abwab-toolbar.component.scss, abwab-door-fields-form.component.scss, abwab-page.component.scss.
- **[Area 6a]** Colour is not the sole carrier of the toolbar's active-view state (contrast case for the relations-flag finding). — abwab-toolbar.component.scss:70-74 — `.abwab-toolbar__view-btn--active { background: var(--qd-selected-bg); color: var(--qd-accent-text); font-weight: 700; }`. The weight change is the non-chromatic differentiator. Same for `.abwab-cards__crumb--current` (abwab-cards.component.scss:32-35).
- **[Area 6a]** Every abwab component in scope uses separate `.html` and `.scss` files; none inlines a template or styles in TypeScript. — `templateUrl`/`styleUrl` pairs at abwab-tree.component.ts:31-32, abwab-cards.component.ts:14-15, abwab-toolbar.component.ts:15-16, abwab-side-panel.component.ts:9-10, abwab-announcer.component.ts:13-14, abwab-template-tree.component.ts:21-22, abwab-door-fields-form.component.ts:12-13, abwab-page.component.ts:89-90, abwab-templates-page.component.ts:49-50.
- **[Area 6a]** No child component calls an API service; all seven presentational components take data via `input()` and report via `output()` only, and both pages own the injection. — abwab-tree.component.ts:36-53 injects only `ElementRef`; abwab-cards.component.ts:19-30, abwab-toolbar.component.ts:20-31, abwab-side-panel.component.ts:14-30, abwab-announcer.component.ts:18, abwab-template-tree.component.ts:26-34 (only `ElementRef`), abwab-door-fields-form.component.ts:17-21 inject nothing at all. Even the write-capable modals take their writes as function inputs from the page (abwab-page.component.html:239-253, abwab-templates-page.component.html:189,200). The facades/controllers are injected only in the two pages (abwab-page.component.ts:100-104, abwab-templates-page.component.ts:54-56).
- **[Area 6a]** The doors page's viewport-reservation chain in SCSS is exactly what the README claims — all five specific measurements hold. — abwab-page.component.scss:1-3 `.abwab-page__frame { min-block-size: calc(100dvh - var(--qd-navbar-block-size)); }`; :20-27 `.abwab-page__layout { flex: 1; min-block-size: 0; align-items: flex-start; }`; :29-35 `.abwab-page__main { align-self: stretch; }`; :37-40 `.abwab-page__tree-card { flex: 1; min-block-size: 0; }`; :48-49 `.abwab-page__side { position: sticky; top: calc(var(--qd-navbar-block-size) + var(--qd-space-4)); }`. The templates page has no `__frame` rule (abwab-templates-page.component.html:2 composes only `qd-container qd-page-frame`), matching README.md:566-577.
- **[Area 6a]** Quran-data safety — nothing in this area renders, derives, invents, or corrects Quran text, morphology, identity, alignment, or counting scope. — The single Quran-adjacent surface is the free-text `representativeAyahText` field in abwab-door-fields-form.component.html:30-41, whose hint explicitly disclaims verified status: `ayahHint: 'نص يكتبه المشرف، وليس مرجعًا قرآنيًا مُتحقَّقًا.'` (abwab.labels.ts:134). It is stored and echoed verbatim (abwab-door-fields-form.component.ts:79-81, :66-67) with no parsing, normalisation, or lookup. Every count in this area counts door rows (abwab-tree.builder.ts:55-63,81-84,167-186), never ayat or words.
- **[Area 6a]** The two routes are stable, lazy, and titled; no child/presentational component is given a route. — abwab.routes.ts:12-23 — `path: ''` → `AbwabPageComponent` with `title: navLabel('abwab')`, `path: 'templates'` → `AbwabTemplatesPageComponent` with `title: ABWAB_LABELS.templatesPageTitle`. Both pages link by path constant, not component class (abwab-page.component.ts:106 `/${ABWAB_ROUTE_PATH}/templates`, abwab-templates-page.component.ts:58).
- **[Area 6a]** The announcer is one always-mounted `role="status" aria-live="polite"` region that cannot become an error box. — abwab-announcer.component.html:1-7 — a single div with `role="status"`, `aria-live="polite"` and `[class.abwab-announcer--empty]="!message()"`; abwab-announcer.component.scss:7 keeps `min-height: 1.25rem` and :10-13 drops border and background when empty, so the reserved slot is invisible rather than an empty box. Mounted unconditionally on both pages (abwab-page.component.html:51, abwab-templates-page.component.html:11).
- **[AREA 7b]** Every stacking `z-index` in `src/` resolves through the `--qd-z-*` scale — zero raw numeric values anywhere (the assignment's primary z-scale question). — `grep -rn "z-index\|zIndex" src/` returns 12 SCSS declarations, all `z-index: var(--qd-z-*)`, plus 2 prose matches in READMEs. Full table in `evidence`. No literal, no `zIndex` in TS/HTML. Cross-checked against `docs/TESTING_DEBT.md:176` row E1, which is the ledger entry for asserting exactly this.
- **[AREA 7b]** The `--qd-z-*` scale is internally consistent and ascending, and modal > context menu > sticky navbar > floating > popover > in-page sticky holds by number. — src/styles/_tokens.scss:139-147 — sticky 5 < popover 30 < floating 40 < mobile-nav 45 < menu-backdrop 49 < menu 50 = modal-backdrop 50 < modal 51 < nav-progress 60. Consumers confirmed: navbar `_layout.scss:39` (45), context menu `context-menu.component.scss:4,10` (49/50), modal backdrop `_components.scss:571` (50), nav-progress `nav-progress.component.scss:11` (60).
- **[AREA 7b]** The one documented arithmetic tie in the scale (`--qd-z-menu` 50 == `--qd-z-modal-backdrop` 50) is not reachable — no context menu can be open while a modal backdrop renders. — src/app/features/abwab/state/abwab-page-overlays.controller.ts:403-404 — `runContextAction` reads the id then calls `this.closeContextMenu()` before invoking the action, and all five ctx entry points (`ctxEdit`/`ctxAddChild`/`ctxMove`/`ctxArchive`/`ctxRelations`, `:380-400`) route through it. Second consumer: `abwab-templates-page.component.ts:198` (`onAddChildRequested`), `:203` (`onEditRequested`), `:249` (`requestNodeDelete`), `:283` (`requestTemplateDelete`) each call `closeContextMenu()` before setting the modal signal. These are the only two `qd-context-menu` consumers in `src/app/`.
- **[AREA 7b]** The sticky navbar sits on the same rung its own dropdown and mobile menu declare, and the `:host { display: contents }` containing-block fix documented as load-bearing is actually present. — src/styles/_layout.scss:37-39 (`position: sticky; inset-block-start: 0; z-index: var(--qd-z-mobile-nav)`); src/app/core/layout/top-navbar/top-navbar.component.scss:1-3 (`:host { display: contents; }`), `:65` (`.dropdown-menu` → `--qd-z-mobile-nav`), `:138` (`.mobile-menu` → `--qd-z-mobile-nav`). All three on rung 45, exactly as `.architecture/UI_STYLE_SYSTEM.md` §17 "Sticky app chrome" claims.
- **[AREA 7b]** Every viewport-relative sticky offset the docs say was re-based onto `--qd-navbar-block-size` actually is, including the panel-height re-derivation. — src/styles/_tokens.scss:98 (`--qd-mushaf-sticky-top: calc(var(--qd-navbar-block-size) + var(--qd-space-3))`); `:77` (`--qd-mushaf-panel-height: calc(100dvh - var(--qd-mushaf-sticky-top))` — derived from the re-based offset, not the bare navbar token, as §17 requires); src/app/features/abwab/pages/abwab-page/abwab-page.component.scss:49 (`top: calc(var(--qd-navbar-block-size) + var(--qd-space-4))`); src/app/shared/ui/detail-modal-shell/detail-modal-shell.component.scss:82 (`inset-block-start: calc(var(--qd-space-4) + var(--qd-navbar-block-size))`). The last one is `--qd-z-floating` (40) — below the navbar's 45 — but it is offset a full navbar-height below the navbar's box, so the rungs never contest the same pixels.
- **[AREA 7b]** The navbar's total occupied block size equals the `--qd-navbar-block-size` token that every downstream offset is computed from — no 1px border drift. — src/styles/_layout.scss:30-36 — `box-sizing: border-box` with `height/min-height/max-height: var(--qd-navbar-block-size)` and `border-block-end: 1px`, so the border is inside the 3.5rem. A sticky element at `top: var(--qd-navbar-block-size)` lands flush.
- **[AREA 7b]** `NavItem` is genuinely data-driven for the two real dropdowns — no switch on hardcoded route strings — and the documented import-cycle/TDZ reason for attaching children outside `NAV_ITEMS` is real. — src/app/core/layout/top-navbar/top-navbar.component.html:14 (`@if (item.children)`, replacing the old `@if (item.key === 'words')` per commit d7a9c0fb); nav-menu.ts:24-32 attaches children via `childrenByParentKey`. Cycle verified: `nav-items.ts` has zero imports; `route-paths.ts:1` imports `NAV_ITEMS` and derives constants at module init (`:19-22`); `words-nav-items.ts:2-9` imports `route-paths`. Nesting children into `nav-items.ts` would close the loop nav-items → words-nav-items → route-paths → nav-items.
- **[AREA 7b]** The click-outside handler avoids the change-detection trap it documents — it keys on a static attribute, not the CD-lagged `.open` class. — src/app/core/layout/top-navbar/top-navbar.component.ts:58 — `el.querySelector('.nav-dropdown[data-menu-key="' + this.openMenuKey + '"]')`, matching the static `[attr.data-menu-key]` at `top-navbar.component.html:17` and the literal `data-menu-key="more"` at `:88`. Both dropdown kinds carry `.nav-dropdown` + `data-menu-key`, so click-outside covers `more` as well as the data-driven entries.
- **[AREA 7b]** `qd-nav-progress`'s settle rule is the documented inversion over in-flight events, never a terminal-event whitelist, and it cannot be stuck by route-preloading traffic. — src/app/core/layout/nav-progress/nav-progress.component.ts:68-73 — `if (event instanceof NavigationStart) { this.arm(); } else if (!IN_FLIGHT_EVENT_CLASSES.some((c) => event instanceof c)) { this.settle(); }`, with the 11 in-flight classes at `:29-41`. `RouteConfigLoadStart`/`RouteConfigLoadEnd` (which the preloader also emits on the same bus) are in that list, and `settle()` early-returns from `'idle'` (`:84-86`), so idle preloading never arms or clears the bar. Timers are cleared on destroy (`:74`).
- **[AREA 7b]** `qd-nav-progress` renders above the navbar in the shell, outside every router-outlet, and its sr-only status region is permanent rather than mounted with the bar. — src/app/core/layout/app-shell/app-shell.component.html:3-4 (`<qd-nav-progress />` immediately before `<qd-top-navbar />`, both inside `.qd-shell-viewport`, `<main><router-outlet/></main>` below at `:6-8`); nav-progress.component.html:9-11 — the `<span class="qd-sr-only" role="status">` sits outside the `@if (barVisible())` block and interpolates `statusMessage()`, which is `''` unless the bar is visible (`nav-progress.component.ts:60-62`).
- **[AREA 7b]** The idle route-preloading strategy is registered and is idle-bounded with a non-browser-safe feature test. — src/app/app.config.ts:41 — `provideRouter(routes, withPreloading(IdlePreloadStrategy))`; src/app/core/navigation/idle-preload.strategy.ts:8-16 — `requestIdleCallback(…, { timeout: 3000 })` guarded by `typeof requestIdleCallback === 'function'` (safe on an undeclared global) with a `setTimeout(resolve, 1500)` fallback; `:20-22` gates every `load()` behind it.
- **[AREA 7b]** `ScrollLockService` is reference-counted, SSR-guarded, and restores the caller's prior overflow rather than blanket-clearing it. — src/app/shared/ui/modal-scroll-lock/scroll-lock.service.ts:12-31 — `acquire()` snapshots `document.body.style.overflow` only on the 0→1 transition and `release()` restores it only on the 1→0 transition; both early-return under `!isPlatformBrowser(this.platformId)`; `release()` also guards `lockCount() === 0` against an unbalanced release. `isLocked` is a `computed` over the count (`:10`), which is what the navbar's inert binding reads (`top-navbar.component.ts:24`).
- **[AREA 7b]** The five words surfaces affected by the sticky-navbar change carry no sticky offset, no scroll-margin, and no z-index of their own — nothing for the sticky navbar to collide with. — `grep -rn "sticky|100dvh|100vh|scroll-margin|scroll-padding" src/app/features/words/` returns zero hits in `features/words/`; `grep -rn 'z-index' src/app/features/words/` returns exactly one, `explorer-association-filter.component.scss:71` (`var(--qd-z-popover)`, an explorer filter popover, not one of the five). All five render `.qd-modal-backdrop` (rung 50) — `lemma-details-panel.component.html:93`, `root-details-panel.component.html:94`, `stem-details-panel.component.html:94`, `word-type-details-panel.component.html:94`, `word-drilldown-modal.component.html:108` — above the sticky navbar's 45.
- **[AREA 7b]** All five carry the scroll lock and therefore do inert the chrome, and all five bind the focus trap conditionally as `features/words/README.md` requires. — `qdModalScrollLock` at lemma-details-panel.component.html:96, root-details-panel.component.html:97, stem-details-panel.component.html:97, word-type-details-panel.component.html:97, word-drilldown-modal.component.html:111. Conditional trap verified on the reference panel: root-details-panel.component.ts:45 — `drawerTrapEnabled = computed(() => !this.detailOverlayHistory.isOpen())`, bound at root-details-panel.component.html:101 (`[cdkTrapFocus]="drawerTrapEnabled()"`), matching `features/words/README.md:63-67`.
- **[AREA 7b]** No `.qd-modal-backdrop` consumer in the app renders without acquiring the scroll lock (`docs/TESTING_DEBT.md:177` row E2's second clause). — 13 consumers found by `grep -rn 'qd-modal-backdrop' src/app/`. 12 apply `qdModalScrollLock` in the same template; the 13th, `shared/ui/detail-modal-shell/detail-modal-shell.component.html:2`, acquires it imperatively at `detail-modal-shell.component.ts:63`. So the clause holds — it is the *membership test* wording, not the invariant, that is wrong (see findings).
- **[AREA 7b]** `.qd-page-frame` and its `.qd-explorer-frame` alias are one rule with the documented call-site split, and the doc's "five existing explorer call-sites" count is accurate. — src/styles/_layout.scss:49-60 declares both selectors on a single rule. `grep -rn 'qd-page-frame|qd-explorer-frame' src/` (excluding READMEs) returns exactly 7 call-sites: `abwab-page.component.html:2` and `abwab-templates-page.component.html:2` on the new name, and `lemmas-|roots-|stems-|word-types-explorer-page.component.html:2` + `unique-words-page.component.html:2` on the alias — the five `features/words/README.md:41-45` names.
- **[AREA 7b]** The sticky navbar did not invalidate the explorer pages' viewport height budget — `mushaf` is the only route that opts into the page-scroll shell layout, and `position: sticky` does not change the navbar's flow box. — `grep -rn 'shellLayout' src/app/` (non-spec) returns only `app-shell.component.ts:41` (the reader) and `features/mushaf/mushaf.routes.ts:14` (the single writer). The explorer budget `--qd-explorer-chrome-block-size: 14rem` → `min(calc(100dvh - …), 58rem)` (`_words-explorer-layout.scss:77,114-117`) sizes the card against the viewport at scroll-top, which is unchanged by the navbar becoming sticky — sticky affects paint/scroll, not the element's in-flow box (`_layout.scss:26-40`).
- **[AREA 7b]** The navbar spec gap and the z-scale assertion gap are recorded rather than silently missing, so they are not unlogged coverage holes. — docs/TESTING_DEBT.md:115-118 rows H1–H4 (H1: "**The navbar itself, wholesale** — no unit spec exists at all: menu open/close state, `openMenuKey` mutual exclusion …, Escape/outside-click dismissal, `aria-expanded`, the inert-under-lock binding"; H2 the active-state matrix incl. `/abwab?archive=1`; H3 mobile children; H4 the dropdown e2e) and `:176-177` rows E1 (the `--qd-z-*` scale) and E2 (the chrome-inert blast radius).
- **[AREA 7b]** The `/abwab?archive=1` nav entry — the app's first query-param nav item — resolves its active state against query params, so «الرئيسية» and «الأرشيف» cannot both light up. — nav-menu.ts:14-21 gives `abwab-archive` `route: ABWAB_ROUTE_PATH` + `queryParams: { archive: '1' }` while `abwab-home` (`:6`) has the same route and none. `top-navbar.component.html:58-60` binds `[queryParams]="child.queryParams ?? null"` and `[routerLinkActiveOptions]="{ exact: child.route === item.route }"` — both resolve to `exact: true` here, and Angular's `exact: true` is `{paths:'exact', queryParams:'exact', …}`, so the two entries discriminate on `archive=1`. (`docs/TESTING_DEBT.md:116` row H2 is the ledger entry for asserting this.)
- **[Area 7a]** Scroll-lock reference counting is correct for nested modals: the body locks on the first acquire, stays locked while any holder remains, restores the ORIGINAL overflow (not a hardcoded value) on the last release, and ignores unbalanced releases. — shared/ui/modal-scroll-lock/scroll-lock.service.ts:12-31 (`if (this.lockCount() === 0) { this.previousOverflow = document.body.style.overflow; … }`; release guards on `this.lockCount() === 0`), asserted by scroll-lock.service.spec.ts:26-36 (two simultaneous consumers) and :38-51 (unbalanced releases, including release-before-acquire and double-release).
- **[Area 7a]** Every scroll-lock acquire in the whole app is balanced by a release, including on component destroy — no path can leave the page unscrollable (and, since Slice B2, no path can leave `.qd-navbar` permanently inert). — All 11 `qdModalScrollLock` hosts sit inside `@if`/`@else if` control-flow blocks, so the directive's `ngOnInit` acquire (modal-scroll-lock.directive.ts:12-14) is always paired with its `ngOnDestroy` release (:16-18): confirm-dialog.component.html:9 under `@if (open())` (:1); abwab-door-modal:9, abwab-template-node-modal:9, abwab-move-picker:9, abwab-sections-modal:9, abwab-template-copy-modal:9, abwab-relations-modal:9 all under `@if (open())` (:1); word-drilldown-modal.component.html:111, lemma-details-panel:96, root-details-panel:97, stem-details-panel:97, word-type-details-panel:97 all under `@else if (…)` branches. The one direct-service consumer guards with a boolean AND a destroy hook: detail-modal-shell.component.ts:60-69 (`if (open && !this.holdsLock) … else if (!open && this.holdsLock) …`) plus :102-107 (`this.destroyRef.onDestroy(() => { if (this.holdsLock) { this.scrollLock.release(); … } })`).
- **[Area 7a]** `qd-state`'s reserve mechanism reserves the message SPAN, not the container, and default-off leaves existing call-sites untouched. — state.component.scss:9-14 (`.qd-state--reserve .qd-state__message { display: block; min-block-size: var(--qd-control-block-size); opacity: 0; transition: opacity var(--qd-t-fast); }`), `reserve = input(false)` at state.component.ts:18, asserted by state.component.spec.ts:101-106 and :118-126. `--qd-control-block-size` is the shared control-geometry token (_tokens.scss:123-127), so the reservation cannot drift from the control it stands in for, exactly as styles/README.md:13-17 requires.
- **[Area 7a]** `qd-state`'s three variants are mutually exclusive and carry the documented roles; `empty` and `loading` are never interactive. — state.component.html:1-29 is a single `@switch` with the action button rendered only inside the `error` case (:17-21); `role="status" aria-live="polite" aria-busy="true"` on loading (:5-7), `role="alert"` on error (:15). Asserted by state.component.spec.ts:32-44, :87-92 and :94-99.
- **[Area 7a]** `qd-context-menu` resolves direction from the DOM rather than hardcoding it, and the RTL branch pins the start edge at the pointer and grows the box in the reading direction — the behaviour is correct even though §17's label for it is not. — context-menu.component.ts:84-87 (`closest('[dir]')` → `'rtl'|'ltr'`) and :68-71 (`let left = rtl ? anchor.x - width : anchor.x;` with the mirrored overflow test). Physical `left`/`top` are used for the final write (context-menu.component.html:8-9) but the direction decision happens in JS, which §17:1226-1227 blesses.
- **[Area 7a]** `qd-context-menu` clamps both axes into the viewport with an 8px margin, so a menu opened near any edge cannot be clipped off-screen. — context-menu.component.ts:79-80 (`clamp(left, VIEWPORT_MARGIN, viewportWidth - width - VIEWPORT_MARGIN)`, same for top) with `VIEWPORT_MARGIN = 8` (:15) and `clamp` at :90-92; browser-tier assertions at e2e/abwab-operations.e2e.ts:178-196 (`menuBox.x + menuBox.width <= 900`, `menuBox.y + menuBox.height <= 420`).
- **[Area 7a]** Every item projected into `qd-context-menu`'s `role="menu"` carries `role="menuitem"` — the role pairing is not half-applied. — abwab-page.component.html:298-302 (five `<button type="button" role="menuitem" class="qd-context-menu__item">`) and abwab-templates-page.component.html:212-232 (same shape, including the two `--danger` variants).
- **[Area 7a]** `qd-confirm-dialog` puts initial focus on CANCEL, not confirm, and that is proven by a test rather than only asserted in the README. — confirm-dialog.component.ts:33-39 (`effect(() => { if (this.open()) setTimeout(() => this.cancelButton()?.nativeElement.focus()); })`), asserted after the macrotask at confirm-dialog.component.spec.ts:78-82 (`await new Promise(resolve => setTimeout(resolve)); expect(document.activeElement).toBe(cancelButton())`) — so it does in fact win the race against `cdkTrapFocusAutoCapture`.
- **[Area 7a]** `qd-confirm-dialog`'s `busy` disables BOTH buttons and suppresses Escape and backdrop dismissal, so a decision in flight cannot be double-fired or cancelled into an ambiguous state. — confirm-dialog.component.ts:41-53 (`confirm()` returns early on `busy() || confirmDisabled()`; `cancel()` returns early on `busy()`), and `cancel()` is what both `(keydown.escape)` (confirm-dialog.component.html:14) and the backdrop `(click)` (:2) call. `[disabled]="busy() || confirmDisabled()"` (:30) and `[disabled]="busy()"` (:41). Asserted at confirm-dialog.component.spec.ts:106-116.
- **[Area 7a]** `qd-confirm-dialog`'s `testIdPrefix` renames all four test ids and falls back to a default, so two confirms on one page can be told apart. — confirm-dialog.component.ts:24 (`testIdPrefix = input('qd-confirm-dialog')`) feeding all four ids at confirm-dialog.component.html:2, :12, :32, :42; asserted for both the default and a custom prefix at confirm-dialog.component.spec.ts:151-168.
- **[Area 7a]** No shared surface in `shared/ui/` carries any Abwab-specific input, class, branch, string or import — the extraction kept the primitives feature-agnostic. — `grep -rniE 'abwab|door|relation' src/app/shared/ui/` over `.ts`/`.html`/`.scss` returns exactly one hit, and it is test data: chip.component.spec.ts:8 (`const PROJECTED_LABEL = 'باب العلم بالله'`). `qd-context-menu` takes only `position`/`menuTestId`/`backdropTestId` (context-menu.component.ts:30-32) and projects items via `<ng-content />` (context-menu.component.html:11); `qd-tabs` owns no selection state (tabs.component.ts:29-31).
- **[Area 7a]** The chip's removable branch produces valid HTML — the nested remove and label buttons sit inside a static `<span>`, never inside another `<button>`/`<a>`. — chip.component.html:25-41 (removable renders `<span class="qd-chip qd-chip--pill qd-chip--static">` wrapping the nested `<button class="qd-chip__remove">`), and `labelIsButton` is gated on `labelClickable() && removable()` (chip.component.ts:32), asserted at chip.component.spec.ts:143-155 and :247.
- **[Area 7a]** Disabled state on the shared chip and tab is not conveyed by colour alone and is genuinely non-interactive on all three chip branches, including the two that have no native `disabled` attribute. — _components.scss:266-271 (`.qd-chip:disabled, .qd-chip.qd-chip--disabled { cursor: not-allowed; opacity: 0.5; pointer-events: none; }`) and :199-203 (same triple for `.qd-tabs__tab[aria-disabled='true']`); the anchor branch additionally drops `href` and sets `tabindex="-1"` + `aria-disabled` (chip.component.html:47-49), asserted at chip.component.spec.ts:106-131.
- **[Area 7a]** Every focusable shared surface has a visible focus ring drawn from the shared `--qd-focus-ring` token, not a suppressed outline. — _components.scss:261-264 (`.qd-chip:focus-visible`), :194-197 (`.qd-tabs__tab:focus-visible`), :647-650 (`.qd-context-menu__item:focus-visible`, `outline-offset: -2px` so the ring stays inside the menu box), _forms.scss:91-94 (`.qd-checkbox:focus-visible`).
- **[Area 7a]** `qd-tabs` roving tabindex skips disabled tabs entirely, and arrow-key direction is RTL-aware and driven by `orientation`, not `layout`. — tabs.component.ts:38-56 (`rovingIndex` filters `!tab.disabled()` in all three fallbacks) and :74-94 (`isRtl` flips the ArrowLeft/ArrowRight step; ArrowUp/ArrowDown return early when horizontal, and `layout` is never read in `onKeydown`); `[attr.tabindex]="roving() ? 0 : -1"` at tab.directive.ts:12. Asserted at tabs.component.spec.ts:66-115 and :152.
- **[Area 7a]** `.qd-truncate`'s mandatory-`[title]` contract holds at every single call-site in the app. — `.qd-truncate` is defined once at src/styles/_utilities.scss:58-63 (`min-inline-size: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap`). All 14 non-spec call-sites carry `[title]`: abwab-template-tree:50, abwab-archive-view:27, abwab-cards:5/:17/:50, abwab-door-picker:52, abwab-relations-modal:43, abwab-templates-page:41/:100, abwab-tree:79, abwab-sections-modal:67, abwab-move-picker:32/:99, abwab-side-panel:6.
- **[Area 7a]** `.qd-checkbox` lives in the styles layer as a utility class (not a component), is built from the `--qd-checkbox-size` token, and every consumer names its box. — src/styles/_forms.scss:84-95 (`.qd-checkbox { inline-size: var(--qd-checkbox-size); block-size: var(--qd-checkbox-size); flex: none; margin: 0; accent-color: var(--qd-accent); }` + `:focus-visible`) and :97-100 (`.qd-check-row`), token at _tokens.scss:135. All three consumers carry `[attr.aria-label]` naming the door: abwab-door-picker.component.html:46, abwab-tree.component.html:41, abwab-cards.component.html:43. `abwab-cards.component.scss:88-92` is placement-only (`position: absolute` + logical insets), stating neither size nor accent — exactly the boundary UI_STYLE_SYSTEM.md:1050-1052 draws.
- **[Area 7a]** `ScrollLockService.isLocked` is the single source of the chrome-inert rule — there is no second "any modal open" service. — scroll-lock.service.ts:10 (`readonly isLocked = computed(() => this.lockCount() > 0)`), consumed once at top-navbar.component.ts:24 (`protected readonly locked = this.scrollLock.isLocked`) and applied at top-navbar.component.html:5-6 (`[attr.inert]="locked() ? '' : null"`, `[attr.aria-hidden]="locked() ? true : null"`). `grep -rn 'ScrollLockService' src/` returns no other consumer.
- **[Area 7a]** `qd-detail-modal-shell` was NOT touched by the Abwab series — it is pre-existing (Feature 029/030) and only its scroll-lock dependency is shared with Abwab work. — `git log -- src/app/shared/ui/detail-modal-shell/` shows its last substantive commit as 26dcab9e (2026-07-17, Feature 029/030), before the series opened at 041f4935 (2026-07-29); the only later commits touching it are the 2026-08-04 comment purges (c597a3f3, 8481f2a7). Its count-reservation and focus-restore contracts (detail-modal-shell.component.ts:110-173, .html:33-39) are therefore out of Abwab scope.
- **[Area 7a]** The `.qd-context-menu__item` styling correctly lives in the global layer rather than the primitive's own stylesheet, because emulated encapsulation would otherwise never reach projected content. — context-menu.component.scss contains only the backdrop and box rules (:1-26) — no `__item` rule; the item family is global at _components.scss:629-654, and the documented templates-page override is a short page-scoped rule at abwab-templates-page.component.scss:145-151, matching UI_STYLE_SYSTEM.md:1247-1252's gap-3 note.
- **[AREA 7c]** HIGH-VALUE TARGET 1 — the relations cache identity is the snapshot ETag, and that ETag is bootId + tree generation (README:352-355). — Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Abwab/AbwabCacheGeneration.cs:7 (`_bootId = Guid.NewGuid().ToString("N")[..8]`) and :16 (`TreeETag() => $"\"abwab-tree-{_bootId}-{Interlocked.Read(ref _treeGeneration)}\""`); consumed at src/app/features/abwab/state/abwab-snapshot.facade.ts:52 (`this.etagState.set(response.headers.get('ETag'))`) and exposed at :24 (`snapshotValidator = this.etagState.asReadonly()`); adopted at state/abwab-relations.controller.ts:45.
- **[AREA 7c]** HIGH-VALUE TARGET 1b — the validator moves on every write that can alter a relation list, including relation add/delete, and a move clears every cache entry. — Backend .../Caching/Abwab/InvalidatingAbwabRelationsWriter.cs:28 and :40 both call `_invalidator.InvalidateTree()`; the doors writer calls it at :29,:48,:65,:82,:98,:110,:122,:134 and the sections writer at :22,:34,:46,:58. Client-side, state/abwab-relations.controller.ts:44-51 clears the whole Map when `snapshotValidator()` differs.
- **[AREA 7c]** HIGH-VALUE TARGET 1c — a null validator serves nothing from the cache; a 304 and a failed refresh both keep value and validator as one unit. — state/abwab-relations.controller.ts:34 (`const cached = validator === null ? undefined : this.cache.get(doorId)`) and :60 (`if (requestValidator !== null && this.adoptCurrentValidator() === requestValidator)`); state/abwab-snapshot.facade.ts:62-64 returns on `HttpStatusCode.NotModified` before the generic branch at :66, and neither branch touches `rawTree` or `etagState`.
- **[AREA 7c]** HIGH-VALUE TARGET 2 — `modal` enters no caching identity: the snapshot read is one unparameterized tree GET and the relations read is keyed by door id and the tree validator only (README:433-442). — data-access/abwab.api.ts:39-44 — `getTree` takes only an etag and issues `GET ${base}/tree` with no query params; :94-96 — `getDoorRelations(doorId)` issues `GET ${base}/doors/${doorId}/relations` with no headers and no other inputs. state/abwab-relations.controller.ts:29-30 — the cache is `Map<number, readonly AbwabRelationVm[]>` plus a single `cacheValidator: string | null`. `modal` appears nowhere in either file.
- **[AREA 7c]** HIGH-VALUE TARGET 3 — the archive view is a client-side partition of the cached snapshot and issues no request. — pages/abwab-page/abwab-page.component.ts:190 (`archivedRoots = computed(() => this.facade.snapshot()?.archivedRoots ?? [])`) and :299 (`onArchiveToggle` calls only `updateQueryParams(buildAbwabQueryParams({ archive: … }))`). The only `facade.load()` in the page is :253 in `ngOnInit`. `archivedRoots` is built inside `buildAbwabTreeSnapshot` at state/abwab-tree.builder.ts:76-79 from the same `dto.doors` array.
- **[AREA 7c]** HIGH-VALUE TARGET 4 — `createDoor` builds the wire body WITHOUT the `sectionId` key when `parentId` is set, not `sectionId: undefined`. — data-access/abwab.api.ts:29-32 — `function buildCreateDoorBody(command) { const { sectionId, ...rest } = command; return command.parentId != null ? rest : { ...rest, sectionId }; }`, used at :63. The destructure removes the key; the truthy branch returns `rest`, which never had it.
- **[AREA 7c]** HIGH-VALUE TARGET 4b — the door modal shell nulls `sectionId` for a child create as defence in depth, and the shared fields form has no section concept. — components/abwab-door-modal/abwab-door-modal.component.ts:163 (`sectionId: parentId != null ? null : sectionId`) with the selector/hint decision at :53 (`() => this.needsSection() && this.sections().length === 0`) and :150-152. `components/abwab-door-fields-form/abwab-door-fields-form.component.ts` contains no `section` identifier.
- **[AREA 7c]** HIGH-VALUE TARGET 5 — bulk-archive's confirm count is a union over one id set, not a sum of per-door subtrees. — state/abwab-write.controller.ts:67-90 — `bulkArchiveConfirmMessage` delegates to `bulkLiveSubtreeCount`, which builds `const counted = new Set<number>()`, walks with `if (counted.has(node.id)) return;` and returns `counted.size`. The single-door path uses the additive `countLiveSubtree` at :46-48, correctly a different function.
- **[AREA 7c]** HIGH-VALUE TARGET 6 — the «كل الأبواب» stat counts live doors via `countLiveAbwabDoors`, and the tab stat reads the backend's `doorsInScopeCount` rather than summing sections. — state/abwab-tree.builder.ts:167-175 (`countLiveAbwabDoors` iterates `byId.values()` counting `!node.isArchived`) and :177-186 (`countAbwabDoorsInOpenScope` returns `totalLiveDoors` for a null section, else `sections.find(...)?.doorsInScopeCount ?? 0`); wired at pages/abwab-page/abwab-page.component.ts:128-131. Backend source at Persistence/Reads/Abwab/EfAbwabTreeReader.cs:29-32 and :42-45.
- **[AREA 7c]** HIGH-VALUE TARGET 6b — the tab badge and the stats answer different questions and are never asserted equal (counting-scope discipline). — `rootCountBySectionId` counts liveRoots only (state/abwab-tree.builder.ts:81-84, iterating `liveRoots`), rendered as `.qd-tabs__count` at components/abwab-toolbar/abwab-toolbar.component.html:14 and :31 with the root-scoped aria phrase `ROOT_DOOR_FORMS` (models/abwab.labels.ts:65-70, :78-80). The stats use the any-depth counts at abwab-page.component.ts:128-131 with `allDoorsTab`/`statOpenScopeDoors` labels (:227-228). Two distinct scopes, two distinct label vocabularies, no test asserting agreement.
- **[AREA 7c]** HIGH-VALUE TARGET 7 — README:756-765's claim that the shipped section-delete conflict copy is the backend's, and that the plan's string exists nowhere. — Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:117 — `public const string AbwabSectionHasLiveDoors = "لا يمكن حذف القسم لاحتوائه على أبواب حالية";`, matched verbatim by e2e/abwab-structure.e2e.ts:34, abwab-sections-modal.component.spec.ts:208 and abwab-sections.controller.spec.ts:89. `grep -rn "القسم يحتوي أبوابًا نشطة"` over the whole repo returns nothing. The prefer-backend-message policy is at state/abwab-write.controller.ts:34-37.
- **[AREA 7c]** Reversal #1 holds — inline reorder commits on Enter only; blur and Escape both cancel, and the post-Enter blur is a no-op. — components/abwab-tree/abwab-tree.component.html:67-68 binds `(keydown)="onOrderKeydown($event, node.id)"` and `(blur)="cancelOrderEdit(node.id)"`; abwab-tree.component.ts:217-224 commits only on `'Enter'` and cancels on `'Escape'`; :226-231 guards `if (this.editingId() !== id) return;` and :237 clears `editingId` before emitting at :242.
- **[AREA 7c]** Reversal #2 holds — the relations flag renders on every row, is dimmed at zero, is a real button, keeps the roving-tabindex invariant, and is inert in bulk mode. — components/abwab-tree/abwab-tree.component.html:112-124 — the `<button>` is outside every `@if`, carries `[class.abwab-tree__flag--empty]="node.relationCount === 0"` (:116), an Arabic `[attr.aria-label]` (:117) and `[attr.tabindex]="-1"` (:119). The dim rule is abwab-tree.component.scss:184-188. Bulk inertness is abwab-tree.component.ts:146-153 (`if (this.bulkMode()) { return; }`). The two hover actions ARE hidden in bulk mode (html:126).
- **[AREA 7c]** Reversal #3 holds behaviourally — apply copies the template root's direct children, never the root. — Backend .../Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs:31 finds `rootNode` by `ParentNodeId is null`, :39 refuses when `rootChildren.Count == 0`, :87-99 creates one door per `rootChildren[i]` per target. Axiom at Persistence/Writes/Abwab/README.md:232. Frontend copy states it before the write: abwab.labels.ts:309 («… عناصر القالب (بدون جذره) ستُنسخ داخل كل باب تختاره») and :311 («… جذر القالب نفسه لا يُنسخ»).
- **[AREA 7c]** Reversal #4 holds — الأبواب is a data-driven hover dropdown with three children including the query-param archive entry. — core/navigation/nav-menu.ts:5-22 declares `abwab-home` (`الرئيسية`), `abwab-templates` (`قوالب الأبواب`) and `abwab-archive` (`الأرشيف`, `queryParams: { archive: '1' }`), attached via `childrenByParentKey` at :24-27. core/layout/top-navbar/top-navbar.component.html:14 (`@if (item.children)`), :19-20 (`(mouseenter)="openMenu(item.key)"` / `(mouseleave)="closeMenu(item.key)"`), :53 (`@for (child of item.children; …)`). abwab.routes.ts declares no guard on either route.
- **[AREA 7c]** Reveal-in-tree no longer clears `q`, and the whole patch is one navigation (README:455-474). — pages/abwab-page/abwab-page.component.ts:409-418 — a single `buildAbwabQueryParams({ door, modal, ...(section), ...(view) })`. No `q` key appears. `modal` is `{ kind: 'relations', closed: true, subjectDoorId: anchorId }` and only `null` when `anchorId === null` (:412); `section` only when `activeSectionId() !== null && node.sectionId !== activeSectionId()` (:413-415); `view: 'tree'` only when `viewParam() === 'cards'` (:416).
- **[AREA 7c]** The reveal pushes rather than replaces, and the ancestor chain is seeded (mergeable) rather than forced. — abwab-page.component.ts:409 calls `updateQueryParams(...)` with the default `replaceUrl = false` (:585); every retain/discard path passes `true` (:545, :556). `revealExpandSeedIds` (:160-174) feeds `expandSeedIds` (:176-186), which returns the module-scope `NO_IDS` (:60) when both sources are empty; the tree merges once into `manuallyExpandedIds` at components/abwab-tree/abwab-tree.component.ts:60-66 and hands the chevrons back via `setExpanded` (:300-310).
- **[AREA 7c]** The URL fail-closed table for `modal` holds exactly as documented (README:394-424). — state/abwab-url-sync.ts:36 — `if (!closed || kind !== 'relations' || subjectDoorId === null) return null;` (an id on the open form is invalid; an id on any other kind is invalid; the id must parse positive via :17-23). :45-47 — a door-dependent plain kind parses to nothing without a `door`. The id-carrying form deliberately does NOT consult `door` (:36-39). Kind set at models/abwab.models.ts:99-106; door-dependent set at :112-117.
- **[AREA 7c]** Scope invalidation clears door/card/modal on a section switch or archive-on, and an explicit key in the same change overrides it. — state/abwab-url-sync.ts:100-105 sets all three to `null` when `changes.section !== undefined || changes.archive === true`; the explicit assignments at :107-115 run afterwards and overwrite. Archive-off (`changes.archive === false`) does not trigger the clear.
- **[AREA 7c]** Restoring is stricter than parsing — a retained overlay needs a live subject, and the id-carrying form checks the carried id instead of `door=`. — state/abwab-modal-url.controller.ts:26-36 — `restorableModal` returns null unless `modal.closed`; for `subjectDoorId !== null` it requires `!!node && !node.isArchived` (:32-33), otherwise it defers to `canOpen` (:35), which at :110-111 requires `!!node && !node.isArchived && this.overlays.selectedDoor()?.id === doorId`.
- **[AREA 7c]** The modal URL controller is page-provided and touches no Router/ActivatedRoute. — state/abwab-modal-url.controller.ts:14 — bare `@Injectable()`; its imports (:1-5) are `@angular/core` plus three feature files. Provided at pages/abwab-page/abwab-page.component.ts:92 (`providers: [AbwabPageOverlaysController, AbwabModalUrlController]`), alongside the overlays controller.
- **[AREA 7c]** The URL is the single source of truth for the selection — a param emission with no `door` clears the store. — pages/abwab-page/abwab-page.component.ts:264-266 — `if (parsed.door === null) { this.selection.clearSelection(); }` inside the `queryParamMap` subscription; the selecting paths write `door=` first at :308 (`onTreeSelected`), :579 (`commitModalOpen`) and via the tree's `openMenuFor` emitting `selected` before `menuRequested` (components/abwab-tree/abwab-tree.component.ts:183-187).
- **[AREA 7c]** Both facades hold an If-None-Match validator beside its value, check 304 before the generic error branch, and drop them together. — state/abwab-snapshot.facade.ts:16 (`etagState`), :52 (written with the value), :62-64 (304 short-circuit before :66). state/abwab-templates.facade.ts:20 (`listEtagState`), :22 (`selectedEtagState: { id, etag } | null`), :118 (`heldEtag = this.selectedEtagState?.id === templateId ? … : null`), :127 (written with the value), :99-101 and :137-139 (both 304 short-circuits), :64-65 (`clearSelection` drops value and validator together). `isNotModified` at :11-13.
- **[AREA 7c]** A 204 null envelope is treated as a payload-less success by the shared write path, and every 204 route is typed to admit null. — state/abwab-write.controller.ts:181-190 — `handleSuccess` reads `response?.data ?? null` then `if (response === null || response.isSuccess)`. The five 204 routes are typed `Observable<ApiResponse<unknown> | null>`: abwab.api.ts:54 (deleteSection), :86 (archiveDoor), :102 (deleteRelation); abwab-templates.api.ts:41 (deleteTemplate), :61 (deleteNode).
- **[AREA 7c]** The 409 policy is one module-scope function shared by the doors and templates controllers, and 409s are surfaced, never auto-retried. — state/abwab-write.controller.ts:31-44 exports `toAbwabWriteFailure`, preferring the backend message at :34/:37 and falling back to `ABWAB_LABELS.writeConflictFallback`. The doors controller reaches it through :267-269; the relations controller forwards writes to it (state/abwab-relations.controller.ts:75, :83) rather than duplicating it. No `retry`/`retryWhen` operator appears in either file.
- **[AREA 7c]** Relation writes carry no version token and still refresh the doors snapshot. — data-access/abwab.api.ts:98-100 (`addDoorRelations(doorId, body: AddDoorRelationsBody)`) and :102-104 (`deleteRelation(relationId)`) — neither takes a version. Both go through `dispatch` at state/abwab-write.controller.ts:157 and :161, whose success path calls `refreshAndRebind()` (:189, :259-265).
- **[AREA 7c]** Refresh-after-write rebinds every cached version, and `rebindTo` drops archived ids from the bulk set while the single selection keeps the missing-only rule. — state/abwab-write.controller.ts:259-265 (`facade.refresh().subscribe(s => this.selection.rebindTo(s))`) plus the submit-time re-filter at :164-172 (`node !== undefined && !node.isArchived`). The builder does set archived doors into `byId` — state/abwab-tree.builder.ts:67 (`byId.set(node.id, node)`) is reached from the `archivedRoots` build at :76-79 with `includeArchivedChildren = true`.
- **[AREA 7c]** The relations modal issues no request for a zero-count door, and reads that count untracked so a post-write refresh cannot reset the draft. — components/abwab-relations-modal/abwab-relations-modal.component.ts:235-254 — the effect tracks `open()` (:236) and `anchorDoorId()` (:237) only; everything else, including `this.anchorRelationCount() > 0` at :249, is inside `untracked(() => { … })` (:238).
- **[AREA 7c]** "Already linked" is computed per (pair, type) with no direction term, and is empty in anchor-pick mode. — components/abwab-relations-modal/abwab-relations-modal.component.ts:193-199 — `linkedIds` returns `new Set<number>()` when `anchorPickMode()` (:194-196), else filters `relation.kind === kind` and maps `otherDoorId` (:198). `direction` is never read. `pickType` (:257-263) clears `pickedIds`; `pickDirection` (:265-267) does not.
- **[AREA 7c]** The direction pill genuinely has two copies, one per mode, and they state opposite sides. — components/abwab-relations-modal/abwab-relations-modal.component.ts:154-160 swaps on `anchorPickMode()`. models/abwab.labels.ts:238-239 — door mode: `relationDirectionAnchorMore: 'المحدد أقل شمولية'`, `relationDirectionAnchorLess: 'المحدد أكثر شمولية'`. :260-261 — anchor-pick: `relationsBulkDirectionAnchorMore: 'الباب المختار أكثر شمولية'`, `relationsBulkDirectionAnchorLess: 'الباب المختار أقل شمولية'`. «أعم»/«أخص» appear nowhere in the file.
- **[AREA 7c]** Exactly three error sites in the feature carry the single §17-permitted `actionLabel` retry, and the relations modal's write errors carry none. — `grep -rn actionLabel --include=*.html` over features/abwab returns three hits: pages/abwab-page/abwab-page.component.html:105 (`(action)="facade.load()"`), components/abwab-relations-modal/abwab-relations-modal.component.html:34 (`status() === 'error' ? retryLabel : null`), components/abwab-door-picker/abwab-door-picker.component.html:72 (`(action)="retry.emit()"`). Only the copy modal binds the picker's error channel (abwab-template-copy-modal.component.html:40-46); the relations modal leaves `status` at its `'ready'` default (abwab-door-picker.component.ts:41).
- **[AREA 7c]** All six authoring modals compose the shared shell; `cdkFocusInitial` appears in exactly two files serving four modals; no modal SCSS carries a max-block-size. — `qd-modal qd-modal--fixed` + `role="dialog"` + `qdModalScrollLock` + `cdkTrapFocusAutoCapture` on all six at line 4/5/9/11 of abwab-door-modal, abwab-template-node-modal, abwab-sections-modal, abwab-move-picker, abwab-relations-modal, abwab-template-copy-modal. `cdkFocusInitial` occurs exactly twice: abwab-door-fields-form.component.html:9 and abwab-door-picker.component.html:3. `grep max-block-size` over features/abwab component+page SCSS returns only abwab-side-panel.component.scss:115 (not a modal).
- **[AREA 7c]** §17's `--wide` consumer set of exactly three is accurate. — `grep qd-modal--wide` over features/abwab returns abwab-move-picker.component.html:4, abwab-relations-modal.component.html:4, abwab-template-copy-modal.component.html:4 — matching UI_STYLE_SYSTEM.md:1083-1084. The value is `width: min(100%, 52rem)` at styles/_components.scss:588-590, matching :1074.
- **[AREA 7c]** README:626-627's `min(92dvh, 44rem)` for `--fixed` is the base value and does not conflict with §17's phone rule. — styles/_components.scss:592-598 — `.qd-modal--fixed { display: flex; flex-direction: column; block-size: min(92dvh, 44rem); padding: 0; overflow: hidden; }`; the phone override is :615-617 (`block-size: min(94dvh, 44rem)`). UI_STYLE_SYSTEM.md:1091 states the base and :1108-1109 states the phone rule separately.
- **[AREA 7c]** §17's "Header over badge columns" three-level subgrid and its four dependent rules all hold in the doors tree. — components/abwab-tree/abwab-tree.component.html:2 (header, `aria-hidden="true"`, sibling of) :15 (`role="tree"`). abwab-tree.component.scss:3-7 (frame owns `grid-template-columns: minmax(0, 1fr) repeat(3, var(--abwab-tree-count-col)) auto auto`), :17-21 / :23-26 / :53-58 (three subgrids). Every row renders every badge cell: html:82, :92, :102 render the `.abwab-tree__count-cell` wrapper unconditionally. Inline insets live only on first/last cells (scss:42-45). Cells and tracks drop in one media query (scss:158-170). Column width is 1.75rem (scss:14), matching §17:1194.
- **[AREA 7c]** §17's "Reveal highlight" outline-not-tint rule and its duration coupling hold. — styles/_tokens.scss:37 — `--qd-selected-bg: var(--qd-accent-tint);` (light `--qd-accent-tint: oklch(0.954 0.010 164.9)` at :34, dark `oklch(0.250 0.030 281.2)` at styles/_themes.scss:32). components/abwab-tree/abwab-tree.component.scss:85-89 — `outline: 2px solid transparent; outline-offset: -2px; animation: abwab-tree-reveal 3s ease-out`, with the keyframe at :76-83; reduced-motion static hold at :91-96; `:focus-visible` declared after at :102-105. Host duration `REVEAL_HOLD_MS = 3000` at pages/abwab-page/abwab-page.component.ts:64.
- **[AREA 7c]** §17's "Viewport reservation" four-link chain and the sticky-aside caveat hold, and the templates page is correctly excluded. — pages/abwab-page/abwab-page.component.scss:1-3 (`.abwab-page__frame { min-block-size: calc(100dvh - var(--qd-navbar-block-size)); }`), :20-27 (`.abwab-page__layout { flex: 1; min-block-size: 0; align-items: flex-start; }`), :29-35 (`.abwab-page__main { flex: 1; align-self: stretch; }`), :37-40 (`.abwab-page__tree-card { flex: 1; min-block-size: 0; }`), :48-49 (`.abwab-page__side { position: sticky; top: calc(var(--qd-navbar-block-size) + var(--qd-space-4)); }`). Both pages compose `qd-container qd-page-frame` at html line 2; only the doors page adds `abwab-page__frame`. The templates editor keeps `min-block-size: 22rem` (abwab-templates-page.component.scss:92).
- **[AREA 7c]** §17's z-scale numbers and styles/README's navbar-rung claim are accurate. — styles/_tokens.scss:139-147 — sticky 5, popover 30, floating 40, mobile-nav 45, menu-backdrop 49, menu 50, modal-backdrop 50, modal 51, nav-progress 60. `.qd-navbar` uses `z-index: var(--qd-z-mobile-nav)` at styles/_layout.scss:39, matching styles/README.md:40, UI_STYLE_SYSTEM.md:1414 and abwab README:629-643 (which cites 45, 5, 40 and 30 correctly).
- **[AREA 7c]** The search split (mark in the tree, filter in cards/archive) and the 500 ms settled announcement hold. — state/abwab-tree.builder.ts:127-165 produces `matchedIds`/`visibleIds`/`autoExpandedIds` from one walk with a push/pop ancestor stack (:136, :141, :145); `pruneAbwabNodesToVisible` at :188-201 backs the filtering views (used at pages/abwab-page/abwab-page.component.ts:142 and :195). components/abwab-toolbar/abwab-toolbar.component.html:51-53 is the `aria-hidden` live count, :55-57 the always-mounted `role="status"` region; abwab-toolbar.component.ts:9 (`ANNOUNCE_SETTLE_MS = 500`), :52-54 (empty query clears immediately, announces nothing), :56-59 (settle timer).
- **[AREA 7c]** The move picker's open-reset keeps `open()` as its only tracked dependency. — components/abwab-move-picker/abwab-move-picker.component.ts:114-131 — the effect body reads `this.open()` (:115) then wraps everything else, including `const movedSectionIds = this.movedSectionIds();` (:119), in `untracked(() => { … })` (:118). The tracked-if-unwrapped source is `AbwabPageOverlaysController.moveSectionIds` at state/abwab-page-overlays.controller.ts:196-201, a `computed` rebuilding a fresh array via `.map().filter()` on every `byId()` change.
- **[AREA 7c]** `excludedIds` for the move is the moved door(s) plus every descendant — the client half of the cycle guard. — state/abwab-page-overlays.controller.ts:181-194 — `moveExcludedIds` seeds a `Set` from `moveDoorIds()` and walks `node.children` recursively with a visited guard.
- **[AREA 7c]** The workshop never names one template and writes to another. — state/abwab-templates.facade.ts:40-46 — `selectedTemplate` returns null unless `dto.id === this.selectedIdState()`. Every write in pages/abwab-templates-page/abwab-templates-page.component.ts takes its id off that object: :216-220 (`template.id` for addNode), :224-230 (quick-add), :290-296 (deleteTemplate), :324-334 (applyTemplate). `selectedTemplateId()` is never passed to a write.
- **[AREA 7c]** The apply refreshes nothing on purpose, and route entry is what makes copies visible. — pages/abwab-templates-page/abwab-templates-page.component.ts:324-334 calls only `this.controller.applyTemplate(template.id, targetDoorIds)`; pages/abwab-page/abwab-page.component.ts:253 calls `this.facade.load()` unconditionally in `ngOnInit`.
- **[AREA 7c]** The tree's RTL-mirrored keyboard model, flat rendering and roving tabindex hold. — components/abwab-tree/abwab-tree-keyboard.controller.ts:98-101 — `ArrowLeft` maps to `intoChildOrExpand` under `'rtl'` and `outOfChildOrCollapse` under `'ltr'`, `ArrowRight` the reverse; `ContextMenu` (:107-108) and `Shift+F10` (:109-110) both emit `openMenu`. abwab-tree.component.html:15-27 renders one flat `role="treeitem"` per visible row with `[attr.aria-level]="node.depth + 1"` and `[attr.tabindex]="rovingId() === node.id ? 0 : -1"`; every in-row control carries `[attr.tabindex]="-1"` (:51, :119, :133, :143).
- **[AREA 7c]** The archive view derives restorability from the builder's depth partition, not by walking byId. — components/abwab-archive-view/abwab-archive-view.component.html:32 (`[disabled]="node.depth > 0"`) and :38-40 (the `restoreParentFirstHint` shown for `node.depth > 0`), with the hint text at models/abwab.labels.ts:170 (`'استرجع الأب أولًا'`). No `byId` reference exists in the component.
- **[AREA 7c]** Counted door labels go through the Arabic number forms rather than bare interpolation. — models/abwab.labels.ts:11-22 (`countPhrase` with zero/one/two/few≤10/many), :32-38 (`DOOR_FORMS` — «لا أبواب» / «باب واحد» / «بابين» / «أبواب» / «بابًا»), used by `archiveConfirm` (:183) and `movePickerTitleBulk` (:150). The two stat labels deliberately bypass it: abwab-page.component.ts:227-228 read `allDoorsTab` (:76) and `statOpenScopeDoors` (:81), both plain strings.
- **[AREA 7c]** Labels are read through TDZ-safe getters, never readonly field initialisers. — components/abwab-tree/abwab-tree.component.ts:118-120, :138-140; pages/abwab-page/abwab-page.component.ts:207-228; components/abwab-relations-modal/abwab-relations-modal.component.ts:114-129 — all `protected get x(): string { return ABWAB_LABELS.y; }`. shared/README.md:63 states the same rule for `result-count.labels.ts`.
- **[AREA 7c]** The Quran-safety posture of this feature's copy holds — the ayah field is explicitly labelled as admin-authored, not verified scripture. — models/abwab.labels.ts:132-134 — `ayahFieldLabel: 'آية تمثل الباب'`, `ayahPlaceholder: 'نص حر — مقتطف يمثّل الباب'`, `ayahHint: 'نص يكتبه المشرف، وليس مرجعًا قرآنيًا مُتحقَّقًا.'`. `AbwabNode.representativeAyahText` is `string | null` (models/abwab.models.ts:127) with no surah/ayah identity, alignment or counting semantics anywhere in the feature.
- **[AREA 7c]** README:766-769's audit-seed claim — no createdAt/createdBy/approvedAt/approvedBy on the door wire model, and no surface renders one. — `grep -rn "createdBy\|approvedBy\|approvedAt\|createdAt" src/app/features/abwab/` returns nothing. `AbwabNode` (models/abwab.models.ts:123-142) carries no such field; the builder maps only the fields listed at state/abwab-tree.builder.ts:41-66.
- **[AREA 7c]** Endpoint totals at README:37-38 are correct: twenty-five across two files, sixteen plus nine, four of them reads, twenty-one writes. — data-access/abwab.api.ts declares sixteen methods (:39,:46,:50,:54,:58,:62,:66,:70,:74,:78,:82,:86,:90,:94,:98,:102); data-access/abwab-templates.api.ts declares nine (:23,:30,:37,:41,:45,:49,:53,:57,:61). Reads are the four `http.get` calls: abwab.api.ts:40, :95; abwab-templates.api.ts:24, :31. (README:377's "fifteen" is the outlier — reported as a finding.)
- **[AREA 7c]** docs/contracts/frontend-shell.md contains no abwab-specific assertion to audit. — docs/contracts/frontend-shell.md is 16 lines; :3 declares "Index only — defers to the linked code + README"; :11-14 names core/README.md, shared/README.md, styles/README.md and response-envelope.md. `grep -i abwab` over the file returns nothing. Its abwab-adjacent claims resolve through core/README.md:66 and :108-110, both of which I verified against nav-menu.ts:14-21 and abwab.routes.ts.
- **[AREA 7c]** The unauthenticated-writes status note at README:13-26 is accurate, not stale. — features/abwab/abwab.routes.ts declares two routes with no `canActivate`/`canMatch`; core/README.md:108-110 independently states "`/abwab` … same unguarded posture" and "Nothing is protected in this phase: the reusable `roleGuard` exists but is attached to no route". The README's own note is the correct record of a real production exposure, not a fidelity defect.
- **[AREA 7d]** No physical CSS properties anywhere in Abwab-touched SCSS. Zero hits for margin/padding/border-left|right, bare left:/right:, text-align:left|right, float:, and physical corner radii across 40 .scss files spanning app/features/abwab, ALL of app/shared/ui, ALL of app/core/layout, src/styles/ and src/styles.scss. Grep reach proved by a sanity control ('position' returns 5 hits in features/abwab) and every path verified present before scanning. — src/app/core/layout/top-navbar/top-navbar.component.scss:64 (inset-inline-start: 0 — logical); src/app/shared/ui/context-menu/context-menu.component.scss:11 (min-inline-size)
- **[AREA 7d]** dir="rtl" is on the document root, so every closest('[dir]') direction resolver in the system is live and the 'ltr' fallback is unreachable in this app — the tree's RTL arrow mirroring, the archive view's, qd-tabs' and the context menu's placement all resolve to rtl. — src/index.html:2 (<html lang="ar" dir="rtl">); abwab-tree.component.ts:320-323; abwab-archive-view.component.ts:124-127; shared/ui/tabs/tabs.component.ts:119-121; shared/ui/context-menu/context-menu.component.ts:84-87
- **[AREA 7d]** The tree keyboard model is genuinely RTL-mirrored, not LTR logic with a flipped label: ArrowLeft expands/enters and ArrowRight collapses/exits under rtl, with the LTR mirror preserved. Home/End/ArrowDown/ArrowUp walk visible rows only. — abwab-tree-keyboard.controller.ts:98-101; pinned by abwab-tree-keyboard.controller.spec.ts:104-179 and by the browser at e2e/abwab-url-and-a11y.e2e.ts:211-222
- **[AREA 7d]** Escape does NOT close two overlays at once in the one nesting the system permits. Every nested qd-confirm-dialog is a DOM SIBLING of its host modal's backdrop, not a descendant, so its (keydown.escape) cannot bubble to the host's (keydown.escape) binding. — abwab-sections-modal.component.html:135-138 (section closes :135, backdrop :136, confirm opens :138); abwab-relations-modal.component.html:170-173; confirm-dialog.component.html:14
- **[AREA 7d]** The sections modal's order editor stops Escape propagation, so cancelling an order edit does not close the whole modal (and does not write modal=sections-closed) — the README calls this guard mandatory and it is present. — abwab-sections-modal.component.ts:224-225 (onOrderKeydown opens with event.stopPropagation()); mirrored in the doors tree at abwab-tree.component.ts:218
- **[AREA 7d]** All six authoring modals carry byte-identical dialog semantics: role="dialog", aria-modal="true", dir="rtl", aria-labelledby pointing at their own <h3>, qdModalScrollLock, cdkTrapFocus + cdkTrapFocusAutoCapture, and (keydown.escape). No modal is missing a member of that set. — abwab-door-modal.component.html:5-14; abwab-sections-modal.component.html:5-14; abwab-relations-modal.component.html:5-14; abwab-move-picker.component.html:5-14; abwab-template-copy-modal.component.html:5-14; abwab-template-node-modal.component.html:5-14
- **[AREA 7d]** Initial focus is aimed, not corrected after the fact, and the two cdkFocusInitial markers serve all four modals that want one — exactly as the README describes. — abwab-door-fields-form.component.html (cdkFocusInitial on the name field, consumed by door + template-node modals); abwab-door-picker.component.html:3 (cdkFocusInitial on the search input, consumed by relations + copy modals)
- **[AREA 7d]** qd-confirm-dialog puts initial focus on CANCEL, not confirm, so a reflexive Enter produces the safe answer — and it is the same primitive behind all six confirmations in this feature. — confirm-dialog.component.html:37-46 (#cancelButton); confirm-dialog.component.ts places focus there; pinned at confirm-dialog.component.spec.ts:78-82
- **[AREA 7d]** Focus return to the modal-restore control after a URL-backed close is implemented, not left to CDK. — abwab-page.component.ts:558 (focusQueued(() => this.modalRestoreControl()?.focusRestore())) → abwab-modal-restore.component.ts:33 (restoreButton().nativeElement.focus())
- **[AREA 7d]** Skeletons are entirely non-interactive everywhere they appear in Abwab: no tabindex, no <button>, no (click) in shared/ui/skeleton, and every visual row is aria-hidden with a single sr-only role="status" carrying the loading label. — shared/ui/skeleton/skeleton-rows.component.html:1-15 (rows aria-hidden="true" at :9; grep for tabindex|<button|(click) over shared/ui/skeleton returns nothing)
- **[AREA 7d]** The tree's badge-column header sits OUTSIDE the role="tree" element and is aria-hidden, so it never reads as an unlabelled treeitem — exactly what UI_STYLE_SYSTEM §17 'Header over badge columns' demands. — abwab-tree.component.html:2 (header, aria-hidden="true") vs :15 (the role="tree" container opens after it)
- **[AREA 7d]** Counting-scope discipline holds in the accessible layer: each of the row's three badges names its own scope, and the toolbar tab's aria-label names root doors specifically while its visible digits are aria-hidden — so the tab count and the stats-row count cannot be mistaken for each other. — abwab.labels.ts:99 (rowChildCountAriaLabel: '… تحته مباشرة'), :100-101 (rowDescendantCountAriaLabel: '… تحته في كل المستويات'), :102 (rowDepthAriaLabel: 'أعمق تفرّع تحته: …'); abwab-toolbar.component.html:8 + :13-18 (aria-label carries the counted noun, the digits are aria-hidden)
- **[AREA 7d]** Bulk selection is keyboard-reachable: Space toggles the focused row's bulk membership, and only in bulk mode. — abwab-tree-keyboard.controller.ts:104-106; abwab-tree.component.ts:279-282
- **[AREA 7d]** Both trees' keyboard menu path anchors at the focused row's INLINE-START edge (row.right under RTL), matching the context menu's documented inline-start extension rather than hardcoding a physical side. — abwab-tree.component.ts:285-287; abwab-template-tree.component.ts:101-104; verified in the browser at e2e/abwab-url-and-a11y.e2e.ts:235-243
- **[AREA 7d]** The context menu's inline [style.left.px]/[style.top.px] are NOT a physical-property violation — they are JS-computed from a direction-aware placement that resolves RTL and flips on collision. Checked and held; do not re-file. — context-menu.component.html:8-9 bound from context-menu.component.ts:63-82 (const rtl = this.resolveDirection() === 'rtl'; left = rtl ? anchor.x - width : anchor.x)
- **[AREA 7d]** Accessible disabled state is real disabled, not visual-only, on every gated control checked: the side panel's six operations, the archive view's restore (with a visible textual reason, not colour), qd-confirm-dialog's busy state, and the door picker's already-linked rows. — abwab-side-panel.component.html:29,41,50,59,68,77 ([disabled]); abwab-archive-view.component.html:32 ([disabled]="node.depth > 0") plus the visible hint at :38-42; confirm-dialog.component.html:30-31 ([disabled] + aria-busy); abwab-door-picker.component.html:45 ([disabled]="row.isDisabled")
- **[AREA 7d]** The move picker's section strip is a real tablist with RTL-aware roving keyboard navigation, and its active cell is marked by tint/border PLUS bold rather than colour alone. — abwab-move-picker.component.html:20-35 (qd-tabs layout="grid", qdTab per cell, aria-controls) and :50 (role="tabpanel" aria-labelledby); shared/ui/tab.directive.ts:6-13 (role="tab", aria-selected, roving tabindex); shared/ui/tabs/tabs.component.ts:75-91 with RTL cases pinned at tabs.component.spec.ts:126-144
- **[AREA 7d]** The toolbar's search match count is deliberately kept off the announcer channel and speaks once on settle rather than per keystroke — the one place the system already reasons about double-announcement, and it is correct. — abwab-toolbar.component.html:51-57 (aria-hidden live digits + an always-mounted qd-sr-only role="status" bound to announcedCountText()); documented at abwab/README.md:86-92
- **[AREA 7d]** Every icon-only control OTHER than the two chevrons named in the findings carries an accessible name: the tree's ＋ and ⋯, the modal-restore discard ×, qd-chip's remove ×, the navbar theme toggle and hamburger, and the door/move/template-tree chevrons. — abwab-tree.component.html:131,141; abwab-modal-restore.component.html:14; shared/ui/chip/chip.component.html:36; top-navbar.component.html:145,212; abwab-door-picker.component.html:29; abwab-move-picker.component.html:85; abwab-template-tree.component.html:20
- **[AREA 7d]** The four reversals themselves hold in code: reveal-in-tree does not touch q; the relations flag is always rendered, dimmed at zero and clickable; template apply copies the root's children; the nav entry is a data-driven dropdown. — abwab-page.component.ts:397-406 (buildAbwabQueryParams patch carries door/modal/section/view and no q); abwab-tree.component.html:113-123; abwab-template-copy-modal.component.html:32-33 (previewNoRootText); top-navbar.component.html:14-70 driven by NAV_MENU

---

## 6. Open questions for the user

- **Q-01 — `README.md:757` asserts a deliberate divergence from a deleted plan.** The sentence
  states that the shipped Arabic string differs from what `plan-slice-b.md` §2 locked
  («القسم يحتوي أبوابًا نشطة»). The plan is gone, so the claim is unfalsifiable from the repo.
  Was the divergence deliberate (in which case the sentence should state the shipped string and
  its reason, dropping the comparison), or is it a defect the README has been documenting since
  the fold? Only you can answer this — see [F-01](#f-01--dangling-references-to-the-deleted-planning-artifacts-medium-abwab-owned).
- **Q-02 — Deployment shape, needed to grade the in-memory ETag generation counter.**
  `Backend/README.md` records no replica/scaling information (grepped for
  `replica|instance|scale|horizontal|in-memory`: no hits). The Abwab tree ETag is an in-memory
  generation counter, which is correct for exactly one process and serves stale `304`s the
  moment a second instance exists. Is the Railway deployment pinned to a single instance, and is
  that pinning recorded anywhere enforceable? Severity of the caching finding depends on the
  answer.
