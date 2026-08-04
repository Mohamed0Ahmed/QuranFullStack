# Plan — Abwab UX slice K: relations (client cache, honest modal states, picker exclusion fix)

- **Status:** planned, not started. Normal implementation plan, NOT Spec Kit. **Frontend-only.**
- **Base branch:** `dev`; feature branch off `dev`, PR into `dev`.
- **Path rationale:** the slice series lived at `docs/feature-ux-slice-<letter>/`, one `plan.md`
  each; hence `docs/feature-ux-slice-k/plan.md`. (Slices a–j were swept per the
  planning-artifact lifecycle rule on 2026-08-04 and are in git history; this folder and
  slice-l's are the N-2 buffer.)
- **Planning basis:** the slice-K inspection of 2026-08-02 (post-PR-#60 tree); every file:line
  below re-verified against the working tree on that date.
- **Locked decisions:** K1 (client-side relations cache keyed by door id, invalidated by tree
  identity), K2 (count-discriminated modal state machine), K3 (disabled anchor row + children
  at depth+1) — as given.

## 0. Scope confirmations, non-goals, and the deferred decision

**Contract confirmation (checked, not assumed):** this slice changes **no request or response
DTO**, adds **no route**, and touches **no backend file**. Therefore: no route-smoke gate, no
`SmokeRouteCatalog` change, no migration, no `check-api-contract` run. If any task below turns
out to need a backend or contract change, that breaks the slice's premise — **STOP and report.**
(The template-apply check in §1 was a read of existing code, performed at planning time.)

**Non-goals (stated):** the fat snapshot (deferred, below); any backend change at all —
including a backend relations cache decorator, ETag/304 on the relations route, and unifying
the duplicated dormancy predicate; removing the per-door GET; slice L's search work; the
planning-artifact cleanup pass.

**Deferred decision — the fat snapshot (record with trigger):** rejected for now because its
decisive payload number is unmeasurable — the local `abwab_door_relations` table has zero rows
(sequence never fired) and the local corpus is e2e residue after feature M's wipe. **Trigger to
revisit:** real relation data exists to measure. **Method on file:** the inspection's
SQL-reconstructed payload (compact JSON, field-for-field per DTO, raw + gzip) with the measured
unit cost **~90–152 B per list entry** (name-length- and escaping-dependent; Arabic escapes 3.0×
raw under the unconfigured `JavaScriptEncoder.Default`, mostly vanishing under gzip), **each
relation serialized twice** (once per endpoint). **The cost scales with the number of RELATIONS,
not doors** — a door count says nothing about it.

## 1. Verified anchors (re-checked 2026-08-02)

| Fact | Where |
|---|---|
| **Template APPLY bumps the tree generation** (STOP-gate: PASSED) | `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Abwab/InvalidatingAbwabTemplateApplyWriter.cs:27` (`_invalidator.InvalidateTree()` in `finally`; class doc says it bumps TREE, not templates). No pre-existing tree-refresh defect; the frontend cache may layer on this identity |
| Every list-affecting write bumps the tree generation (incl. door rename) | decorators `InvalidatingAbwabRelationsWriter.cs:17-44`, `InvalidatingAbwabDoorsWriter` (all methods), `InvalidatingAbwabSectionsWriter`, apply above; bypass-free (no code references the concrete `EfAbwab*` writers) |
| ETag = `bootId + tree generation`, body-agnostic; CORS-exposed | `AbwabCacheGeneration.cs:22`; `Backend/api/.../Extensions/ServiceCollectionExtensions.cs:84` (`WithExposedHeaders(HeaderNames.ETag)`) |
| Facade holds the validator beside the value — currently a **private field** | `state/abwab-snapshot.facade.ts:34` (`private etagState: string \| null = null`), written at `:68` from the response header; 304 keeps value+validator (`:79` area) |
| Relations controller is app-scoped, fetch + mapping only | `state/abwab-relations.controller.ts:29-30` (`providedIn: 'root'`), `loadFor` `:34-43` |
| Modal state machine today: no loading signal; `[]` reset before fetch; empty branch on `groups().length === 0`; sticky load error | `abwab-relations-modal.component.ts:109-113`, `:241-244`, `:329-337`; `component.html:29-31` (error, no action), `:40-42` (empty) |
| Modal fetch chain | `abwab-page.component.html:253` → `abwab-page-overlays.controller.ts:387` → `abwab-relations.controller.ts:34` → `abwab.api.ts:110-112` |
| Count already on every node; snapshot `byId` available to the overlays controller | `abwab.models.ts:176-178`, `abwab-tree.builder.ts:79`; `abwab-page-overlays.controller.ts:48, 212, 227` (`this.byId()`) |
| Picker row builder + the exclusion hoist | `abwab-door-picker.component.ts:90-116`; hoist `:109` (`isExcluded \|\|` forces descent) and `:110` (`isExcluded ? depth : depth + 1`); suppressed anchor row `:106-108`; `expandedIds` `:73`, toggle `:140-144`, `reset()` `:168-171`; `disabledTag` input `:55`; depth → CSS var `component.html:19` + `component.scss:34` |
| Pinned product requirement: excluded door's subtree stays **visible at open** | `abwab-relations-modal.component.spec.ts:236-243` (presence-only — depth/expansion unpinned) |
| Picker has **no spec file**; consumers: relations modal (`excludedIds` bound, `html:123`), template-copy modal (**no** `excludedIds` → immune); move picker is a **separate component** (own builder drops excluded subtrees — its cycle guard) | `find`-verified; `abwab-move-picker.component.ts:79-103` |
| §17 loading doctrine: compose `qd-skeleton-rows`/`qd-state`; a new loading state is a `shape`/`rowTemplate` input, not a new component; skeletons `role="status"` `aria-busy` non-interactive; error may carry **exactly one** action | `UI_STYLE_SYSTEM.md:791-833` (`qd-state`), `:957-982` (skeletons) |
| Existing labels to reuse | `abwab.labels.ts:183` (`retryButton: 'إعادة المحاولة'`), `:245` (`relationsEmpty`), `:246` (`relationsLoadError`) |
| README paragraphs invalidated by K1 | `Frontend/.../features/abwab/README.md:310-317` ("the relations read is … uncached", "still uncached and unconditional"); `Backend/.../Persistence/Reads/Abwab/README.md:125-127` (same claim, backend side — **frontend-only slice: flag, do not edit backend? NO — see §7**: root `CLAUDE.md` demands README truth in the same change; a doc-only edit to a backend README is not a backend *behavior* change and stays in scope) |
| Housekeeping drift | `abwab-door-picker.component.ts:29` (dangling "row 4" back-reference); "five abwab specs" at `TESTING_STRATEGY.md:418`, `e2e/README.md:31, 59, 83` — seven exist today, **eight after this slice** |
| Zero existing coverage named | no picker spec; no relations e2e (`grep relation e2e/` = menu-presence only); no facade validator spec (TESTING_DEBT I3); relations writer/reader untested (rows 1–2); debt row 5 at `docs/TESTING_DEBT.md:31` |

## 2. Design

### 2.1 K1 — the client-side relations cache and its identity

**Identity source — decided: the snapshot ETag validator, exposed by the facade.** The facade
already stores the server validator beside the snapshot as one unit (`etagState`,
`abwab-snapshot.facade.ts:34/:68`); it becomes a private *signal* with a public
`readonly snapshotValidator: Signal<string | null>` view (value semantics unchanged: written on
200, kept on 304, kept on failure). Why not `AbwabTreeDto.version`: **doubly disqualified** —
(a) it is documented diagnostics-only and must not acquire an identity job
(`features/abwab/README.md:536-541`; `Reads/Abwab/README.md:102-108`); (b) it is *factually
blind* to relation writes — `GetSnapshotVersionAsync` reads sections/doors/aliases only, so a
relation add/delete moves the ETag but not `version`, and a version-keyed cache would serve
stale lists on exactly the writes that matter most. The ETag is the same truth the backend's
invalidation uses: `bootId + tree generation`, bumped by **every** write that can alter a
relation list, rename included.

**Cache shape** — lives in `AbwabRelationsController` (root-provided, `:29`): a
`Map<number, readonly AbwabRelationVm[]>` plus the `string | null` validator it was built
under. `loadFor(doorId)` becomes cache-aware:
1. If the facade's current validator differs from the stored one (or either is null) → clear
   the whole map, adopt the current validator. (Null validator = no snapshot identity held —
   never serve from cache under it.)
2. Hit → return the stored list synchronously (`of(...)`), **no HTTP**.
3. Miss → fetch as today; on success store under the adopted validator; on error store nothing.
A post-write `reload` bypasses the hit path (forced refetch) and overwrites the entry.

**Invalidation matrix — every path, how covered, and the assertion that proves it.** "Identity"
means: the write triggers the refresh-after-write snapshot refetch (or the route-entry
unconditional `load()`), the response carries a bumped-generation ETag, the validator signal
changes, and step 1 clears the map before any read is served.

| Path | Cache survives? | Covered by | Assertion (spec, phase 3) |
|---|---|---|---|
| Relation add | NO | identity (write → refresh → new ETag) + the forced post-write refetch | `add-then-reopen serves the refetched list, never the stale entry` |
| Relation delete | NO | same | `delete-then-reopen never serves the removed row` |
| **Door rename** | NO | identity — same mechanism as every other row | **regression guard, not a discriminating test**: `renaming a door evicts cached lists that merely MENTION it`. Under today's clear-all-on-identity-change implementation this spec is **indistinguishable from the source-agnostic case** — it passes under any implementation that clears on validator change. It exists to fail the day a finer-grained invalidation forgets the rename→partner-list dependency; the spec's own comment must say exactly that. The README pin (below) is the real artifact |
| Archive / restore (single) | NO | identity | `archive-then-open falls to a fetch (dormancy)` |
| Bulk archive / bulk move | NO | identity | one bulk case, same shape |
| Template apply | NO | identity (apply bumps tree — §1 STOP-gate) + route-entry `load()` on returning to `/abwab` | `a validator change from any source clears every entry` (source-agnostic case) |
| Section create/rename/delete/reorder | NO (over-eager, harmless) | identity | covered by the source-agnostic case |
| Snapshot refetch → **200** (new ETag) | NO | step 1 | covered by the source-agnostic case |
| Snapshot refetch → **304** | **YES** | validator unchanged — nothing changed server-side | `a 304 leaves the cache serving hits` |
| Snapshot refetch → failure | YES | validator kept with the kept value (`facade README` contract) | `a failed refresh does not wipe the cache` |
| Route leave / re-enter | YES until the re-entry `load()` resolves; then per its ETag | root-provided controller + step 1 | implicit in the 200/304 cases |

**A stale list is a correctness defect** — the assertions above are phase-3 gating specs, not
nice-to-haves. **README pin (same commit):** one sentence added to the frontend README's
relations paragraph and to `Reads/Abwab/README.md`'s caching section: *any future
finer-grained invalidation of relation lists must still evict on door rename — partner names
and list ordering embed the name, and no count changes when it does.* The rename spec is a
regression guard for that future change, not present-tense proof (see the matrix row); the
README sentence is what makes the requirement binding today.

### 2.2 K2 — the modal state machine, count-discriminated

New modal input `anchorRelationCount: number`, bound by the page from a new overlays
computed `relationsAnchorCount` — **reactive on the current anchor id AND the snapshot**
(`computed(() => byId().get(relationsAnchorId())?.relationCount ?? 0)`, beside
`restoreTarget`, `abwab-page-overlays.controller.ts:235` area) — never a value captured at
open. New `status` signal: `'idle' | 'loading' | 'ready' | 'error'`.

**Mode scoping (verified against the template):** the relations list exists ONLY in door
mode — `abwab-relations-modal.component.html:33-66` renders the bulk-target strip in
anchor-pick mode and the groups/empty area in its `@else`. Therefore the discriminator, the
skeleton, the empty state, and the fetch are all door-mode-only; anchor-pick mode issues no
fetch (the existing `!anchorPickMode()` guard in the open effect stays) and holds
`status = 'ready'` throughout — picking an anchor inside the modal consults neither the
count nor the cache.

**Anchor change while open (door mode):** the open effect already tracks `anchorDoorId()`
(`:232-254`), so re-pointing the anchor re-fires it. The re-fire re-runs the FULL
discriminator for the new anchor: reset draft + error, then zero-count → `'ready'` + empty,
no request; non-zero → cache consulted for the **new** id, hit → instant list, miss →
`'loading'` + fetch. A stale count or a stale list surviving an anchor change is a
correctness bug in the discriminator; the matrix row and assertion below close it.

The open effect (`:232-254`) becomes:

- **count === 0** → `status = 'ready'`, `relations = []`, **no request is issued at all** —
  the fetch is skipped, not hidden. Empty state «لا توجد علاقات لهذا الباب بعد — أضف أول
  علاقة من الأسفل.» renders immediately.
- **count > 0, cache hit** → `status = 'ready'`, list rendered synchronously. **No skeleton
  flash**: the hit path resolves before change detection paints, and the template's loading
  branch is gated on `status() === 'loading'`, which a hit never enters.
- **count > 0, cache miss** → `status = 'loading'`; the body renders `qd-skeleton-rows`
  (composed per §17 — a `rowTemplate` mirroring the relation group rows, no new component,
  `role="status"`/`aria-busy` inherited) with sr label `relationsLoading` (§5). The empty
  message and the `0` count chip are unreachable in this window (both branches now require
  `status() === 'ready'`).

**Resolution rule (locked): once a list is fetched, the LIST wins over the count** for what is
displayed — the count only decides whether to fetch and what to show while waiting. A
count > 0 door whose fetch returns `[]` shows the empty state after resolution (`'ready'` +
empty list); no reconciliation with the flag is attempted.

**Deliberate trade-off (recorded):** the discriminator is snapshot data, so a stale snapshot
with a zero count means the modal asserts "no relations" without asking the server. Accepted
because the app is single-instance, single-operator, and the refresh-after-write invariant
(`features/abwab/README.md:496-508`) keeps the snapshot current after every write.
**What would invalidate this reasoning:** a second concurrent operator, or any future write
path that does not refresh the snapshot. Either forces the zero-count branch back to a fetch.

**Error handling:** `reload`'s success branch now clears `errorMessage` (the sticky-error fix,
`:330-336`). The load-error `qd-state` gains the single §17-permitted action —
`[actionLabel]="retryLabel"` (`retryButton`, labels:183) wired to a re-fetch of the anchor.
**Justification for the third retry site:** §17 reserves the action for transient transport
failures where retry is the honest recovery (`UI_STYLE_SYSTEM.md:797-801`); a modal-open load
failure is exactly the class the two existing sites cover (both are doors-snapshot load
failures), and without it the only recovery is closing and reopening the modal.
**Write-path errors keep today's surfacing and gain NO retry**: the add/remove buttons are
themselves the retry affordance — a second action on the same error line would violate the
one-action rule and duplicate an existing control.

**State matrix (each cell = expected UI + whether a request is issued; all asserted in phase 2/3 specs):**

| State | UI | Request? |
|---|---|---|
| Open, count = 0 | empty state immediately; count chip shows 0 honestly | **NO** |
| Open, count > 0, cache miss | skeleton rows + sr label; no empty text, no 0 chip | YES (GET) |
| Open, count > 0, cache hit | list immediately; no skeleton | **NO** |
| Load error | `qd-state error` + message + «إعادة المحاولة»; groups hidden | was YES, failed |
| Retry pressed | back to skeleton; previous error cleared | YES |
| Empty after fetch (count>0, server says `[]`) | empty state (list wins) | already resolved |
| Write in flight | existing behavior (buttons disabled per current logic) | YES (POST/DELETE) |
| Write error | error line via `errorMessage`; **no retry action**; picker/draft intact | was YES, failed |
| Post-write refresh | forced refetch overwrites cache; list re-renders; snapshot refresh runs (existing invariant) | YES (GET + snapshot) |
| **Anchor changed while open (door mode)** | full discriminator re-run for the new anchor: previous list/error never shown; zero-count new anchor → empty immediately; non-zero → cache hit instant / miss skeleton | **per the new anchor**: NO on zero-count or cache hit; YES on miss |
| Anchor-pick mode (any anchor picked in-modal) | bulk-target strip + picker only — no list, no skeleton, no empty state | **NO** (existing guard) |

### 2.3 K3 — the disabled anchor row

Fix in `abwab-door-picker.component.ts:90-116` (the only touched builder; move picker is a
separate component and untouched; template-copy passes no `excludedIds` and is unaffected —
both re-verified §1):

- Excluded nodes are **no longer suppressed**: they render as a **disabled, non-selectable
  row** at their true depth — no checkbox/radio control at all (not a disabled control),
  `aria-disabled="true"` on the row, name + a muted **tag** naming why (§5), muted text +
  absent control + tag = three non-color signals. The row is not focusable-as-selectable; its
  **chevron is a real, focusable control** and behaves normally.
- Children recurse at **`depth + 1`** always — the `isExcluded ? depth : depth + 1` branch
  (`:110`) dies.
- Descent follows `expandedIds` for excluded rows like any other — the `isExcluded ||`
  disjunct (`:109`) dies. To keep the **pinned** open-state visibility
  (`abwab-relations-modal.component.spec.ts:236-243`: the excluded door's subtree is present
  at open), `reset()` (`:168-171`) seeds `expandedIds` with the current `excludedIds` — the
  subtree is visible at open AND collapsible thereafter, which is the whole fix.
- New picker input `excludedTag = input('')` (sibling of `disabledTag`, `:55`); the relations
  modal binds it per mode (§5). Empty string ⇒ no tag rendered (template-copy unaffected).
- `subtreeMatches` search behavior is unchanged (excluded rows already participate).

**A11y statement:** the disabled row is announced as its name + the tag text + disabled (the
tag is part of the row's text content; `aria-disabled` carries the state); it is skipped by
the picker's selection interaction but its chevron stays in the tab order; indentation uses
the existing logical-property CSS var (`--abwab-door-picker-depth`, `scss:34`) — RTL-safe by
construction.

## 3. Phases

### Phase 1 — K3 picker fix + the picker's first spec (least risk, no behavior dependency)

| # | Task | Files |
|---|---|---|
| 1.1 | Row builder per §2.3: excluded row rendered disabled, `depth + 1` children, `expandedIds`-governed descent, `reset()` seeding, `excludedTag` input | `components/abwab-door-picker/abwab-door-picker.component.{ts,html,scss}` |
| 1.2 | Relations modal: bind `[excludedTag]` per mode (door: «الباب المفتوح», anchor-pick: «هدف محدد») | `components/abwab-relations-modal/abwab-relations-modal.component.{ts,html}`; labels §5 |
| 1.3 | **Create `abwab-door-picker.component.spec.ts`** (first ever): excluded root renders disabled with tag and no control; children at depth 1 (CSS var asserted); chevron collapses/expands them; subtree visible at open (seed); search still reveals matches; non-excluded behavior unchanged; `excludedTag=''` renders no tag | new spec file |
| 1.4 | Re-run the pinned host spec unmodified — `:236-243` must stay green (presence preserved) | `abwab-relations-modal.component.spec.ts` |
| 1.5 | Fix the dangling "row 4" back-reference in the picker's doc comment | `abwab-door-picker.component.ts:29` |

Behavior change: excluded doors visible-but-disabled with real hierarchy; no more phantom roots.
**Verification (Tier A):** `npm test -- --include="src/app/features/abwab/components/abwab-door-picker/*.spec.ts" --include="src/app/features/abwab/components/abwab-relations-modal/*.spec.ts"`.
**Commit boundary:** one commit.

### Phase 2 — K2 state machine (fetch-always for count>0; the cache arrives in phase 3)

| # | Task | Files |
|---|---|---|
| 2.1 | `status` signal + count discriminator + zero-count no-fetch + skeleton branch per §2.2; `anchorRelationCount` input; overlays computed + page binding | `abwab-relations-modal.component.{ts,html}`, `state/abwab-page-overlays.controller.ts`, `pages/abwab-page/abwab-page.component.html` |
| 2.2 | Sticky-error fix (success clears `errorMessage`); load-error retry action (`retryButton`) | `abwab-relations-modal.component.{ts,html}` |
| 2.3 | Labels: `relationsLoading` (§5) | `models/abwab.labels.ts` + labels spec |
| 2.4 | Specs — the state matrix rows: zero-count open fires **no** HTTP (assert the `loadRelations` stub uncalled) and shows empty immediately; count>0 shows skeleton and never the empty text pre-resolve (async stub — replace the synchronous `of(...)` with a `Subject` so the in-flight window exists, fixing the untestability the inspection found at `spec:63-64`); error→retry→success clears the error; empty-after-fetch (list wins); **anchor-change-while-open: re-point the anchor input mid-open — the old list is never shown against the new anchor, a zero-count new anchor issues no fetch, a non-zero one fetches**; anchor-pick mode renders no skeleton/empty and fires no fetch regardless of counts; existing `:125` empty-state spec updated to the zero-count path | `abwab-relations-modal.component.spec.ts` |
| 2.5 | README: the loading/empty/error composition paragraph gains the relations modal's loading state; the retry-sites sentence goes from two to three | `features/abwab/README.md` (~L616-625) |

Behavior change: no more «لا توجد علاقات» flash; zero-count doors stop issuing requests entirely.
**Verification (Tier A):** relations-modal + labels focused globs.
**Commit boundary:** one commit.

### Phase 3 — K1 cache + invalidation assertions + rename pin

| # | Task | Files |
|---|---|---|
| 3.1 | Facade: `etagState` → private signal + public `snapshotValidator` readonly view (write points `:68` and the 304-keep path unchanged in semantics) | `state/abwab-snapshot.facade.ts` |
| 3.2 | Cache in the relations controller per §2.1 (map + validator adoption + hit/miss/forced-refetch); modal's post-write `reload` uses the forced path | `state/abwab-relations.controller.ts`, `abwab-relations-modal.component.ts` |
| 3.3 | Cache-hit UX: hit renders with no skeleton (status never enters `'loading'`) — matrix row asserted | `abwab-relations-modal.component.spec.ts` |
| 3.4 | Invalidation specs — every row of the §2.1 matrix: source-agnostic validator-change clears all; 304 keeps hits; failed refresh keeps cache; add/delete/archive rows; **the rename pin** (`renaming a door evicts cached lists that merely mention it`) | `state/abwab-relations.controller.spec.ts` (new — the controller has no spec today), facade spec |
| 3.5 | Facade validator spec cases — pays TESTING_DEBT **I3** for the snapshot facade half: 200 stores the header validator; 304 keeps value+validator, no error, loading ends (`throwError(() => new HttpErrorResponse({status: 304}))` per the row's own recipe) | `state/abwab-snapshot.facade.spec.ts` |
| 3.6 | READMEs in the same commit: frontend README relations paragraphs (~L310-317 "uncached and unconditional" → the client cache + identity + the rename-pin sentence); `Backend/.../Persistence/Reads/Abwab/README.md:125-127` (doc-only truth fix: the client now holds a prior value; the backend read stays uncached) + the rename-pin sentence; TESTING_DEBT: narrow I3 (snapshot-facade half paid; templates-facade half remains) | three docs |

Behavior change: second and later opens of any door render with zero network wait until any write.
**Verification (Tier A):** relations-controller + facade + relations-modal globs.
**Commit boundary:** one commit.

### Phase 4 — the relations e2e flow + housekeeping (debt row 5 fires)

**Decision, stated: TESTING_DEBT row 5's trigger ("the next time relations change shape")
FIRES here.** This slice rewrites the modal's read path and layers a cache whose staleness
class only a browser flow exercises; row 5 is the single check crossing read, write, count,
and flag in one pass.

| # | Task | Files |
|---|---|---|
| 4.1 | Fixture: relations helper (add/delete via API) on the abwab sandbox | `e2e/fixtures/abwab.ts` |
| 4.2 | New `e2e/abwab-relations.e2e.ts` — the row-5 flow: add a relation → chip on both doors → archive one endpoint → chip + tree flag vanish → restore → both return; plus one cache-honesty step asserted by **request interception, not network idle**: `page.route('**/api/abwab/doors/*/relations', …)` counting GETs across open → close → reopen — **exactly one**; then rename the partner via API, refresh, reopen → the new name renders (a second GET is expected here) | new e2e spec |
| 4.3 | Run `npm run e2e` (opt-in evidence, never Tier-C substitute) — the abwab project count grows to **eight** specs, single-worker rule unchanged | — |
| 4.4 | Housekeeping: "five abwab specs" → eight at `TESTING_STRATEGY.md:418` and `e2e/README.md:31, 59, 83`; delete TESTING_DEBT row 5 (`docs/TESTING_DEBT.md:31`) — paid | those three files |

**Commit boundary:** one commit.

### Pre-PR (Tier C)

`npm test` (full, fork cap preserved) + `npm run build`. No backend gates — nothing under
`Backend/` changed except two README paragraphs (doc-only). E2E cited as supplementary.

## 4. Arabic strings (verbatim)

New (`models/abwab.labels.ts`, TDZ getters):

| Key | Value | Surface |
|---|---|---|
| `relationsLoading` | `يتم تحميل العلاقات…` | skeleton sr-only label, relations modal |
| `pickerExcludedAnchorTag` | `الباب المفتوح` | disabled anchor row tag, door mode |
| `pickerExcludedTargetTag` | `هدف محدد` | disabled row tag, anchor-pick mode |

Reused unchanged: `retryButton` «إعادة المحاولة» (`:183`), `relationsEmpty` (`:245`),
`relationsLoadError` (`:246`). Removed: none.

## 5. Accessibility and RTL

- Skeleton: non-interactive, `role="status"` + `aria-busy` + the sr label — inherited from the
  §17 skeleton system; static under reduced motion.
- Retry: the single `qd-state` action, a real button, Arabic label, reachable in the modal's
  existing trap order.
- Disabled anchor row: per §2.3 — `aria-disabled`, tag text in the accessible name, no
  selection control, chevron focusable; three non-color signals.
- Logical properties only throughout (the picker's depth var already is); both themes
  unaffected (no new colors — muted/existing tokens only).

## 6. Risks, rollback, stop conditions

**Risks — named plainly: everything this slice touches is at or near zero existing coverage.**
No picker spec existed (phase 1 creates the first), no relations e2e (phase 4 creates it), no
facade validator spec (phase 3 pays I3's snapshot half), the relations write paths have no
spec of their own (unchanged here, still uncovered — debt rows 1–2 stay). The cache's failure
mode is silent staleness — which is why the §2.1 matrix rows are gating specs. The zero-count
no-fetch branch inherits snapshot staleness (recorded trade-off, §2.2) — its invalidating
conditions (second operator, non-refreshing write path) are written into the README pin.

**Rollback:** four independent commits; phase 3 (cache) reverts cleanly to phase 2's
fetch-always machine; phase 2 reverts to today's modal; phase 1 is self-contained.

**Stop conditions:** (1) any task turns out to require a backend behavior or contract change —
STOP (premise break, §0); (2) the pinned visibility spec (`:236-243`) cannot stay green with
the seeded-expansion approach — STOP and report before weakening a pinned product requirement;
(3) the e2e cache-honesty step's request interception cannot be made reliable — report and
drop that one step, not the flow; **network idle is not an acceptable substitute for a
"zero requests" claim**; (4) the facade validator refactor changes any observable 304 behavior
(`abwab-snapshot.facade.spec.ts` regressions) — STOP.

## 7. Acceptance criteria (each independently checkable)

1. Opening a zero-count door's relations modal issues **zero** HTTP requests and renders the
   empty state immediately (spec-asserted via uncalled stub; e2e network-idle step).
2. Opening a count>0 door on a cache miss shows skeleton rows — never the empty message, never
   a `0` chip — until the list resolves.
3. Reopening any door with an unchanged snapshot validator renders the list instantly with no
   skeleton and no request.
4. Every §2.1 invalidation matrix row has a green spec, including: a 304 preserves hits and a
   failed refresh preserves the cache. The rename case is present **as a labeled regression
   guard** (its comment states it is currently indistinguishable from the source-agnostic
   validator-change case), and the rename-pin sentence is present in both READMEs — the
   sentence, not the spec, is what binds today's behavior.
5. A failed load shows «إعادة المحاولة»; retry re-fetches; a later success clears the error
   (sticky-error fix). Write errors keep their surfacing and carry no retry action.
6. The picker renders an excluded door as a disabled, tagged, non-selectable row at its true
   depth, children indented one level below, collapsible via its chevron, subtree visible at
   open; `abwab-relations-modal.component.spec.ts:236-243` passes unmodified.
7. `abwab-door-picker.component.spec.ts` exists and covers the §2.3 behaviors directly.
8. Move picker and template-copy modal behavior byte-identical (their specs untouched, green).
9. `AbwabTreeDto.version` acquired no new job (facade change is validator-only; version
   paragraphs unedited except where quoted).
10. `e2e/abwab-relations.e2e.ts` passes in an actual run; TESTING_DEBT row 5 deleted; I3
    narrowed to its templates-facade half; the "five specs" drift is fixed to eight in both
    files; the picker's row-4 dangling reference is gone.
11. Both invalidated README paragraphs (frontend `:310-317`, backend Reads `:125-127`) state
    the new truth in the same commits as the behavior, each carrying the rename-pin sentence.
12. Full `npm test` + `npm run build` green pre-PR; no file under `Backend/` changed except
    the one README.
