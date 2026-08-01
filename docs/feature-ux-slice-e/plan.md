# Slice E — Restorable overlays (UX audit)

Source: `docs/abwab-ux-audit.md` "Slice E — Restorable overlays" (`:1102-1107`) — item 11
(`:381-416`) whole and alone. The audit isolated this slice deliberately: it changes the
abwab URL contract (a seventh query key with its own fail-closed parse and
scope-invalidation rule) and refines a recorded README invariant about why overlay state
is page-scoped.

**Mode when this plan was written:** plan-only. No code, no Git, nothing amended.

**Slice D status at plan time:** merged. `ux-slice-d-tree` merged via PR #56 and the
follow-up perf PR #57; `git merge-base --is-ancestor ux-slice-d-tree dev` confirms
ancestry. This plan is measured against `dev` (`67a4afc9`, clean). **The D-DEPENDENT
fact list is empty** — every row in §5 was verified on `dev` itself.

## Precondition — VERIFIED on `dev` (`67a4afc9`, clean) at plan time

| Consumed primitive / mechanism | Where it lives | Verified |
|---|---|---|
| Slices A–D merged to `dev` | D via PR #56 + #57 (`2f58a506`, `67a4afc9`) | ✅ |
| The six-key URL contract, fail-closed parse, defaults | `abwab-url-sync.ts:12-32`, `models/abwab.models.ts:151-178` | ✅ |
| Scope-invalidation clear + explicit-`door`/`card` override | `abwab-url-sync.ts:43-78`; pinned at `abwab-url-sync.spec.ts:58-68` | ✅ |
| Single `queryParamMap` subscription parsing all six keys; `door=null` clears selection; reveal handshake | `abwab-page.component.ts:235-268` | ✅ |
| Deferred `door=` deep-link restore (URL + snapshot settle, `untracked` select) | `abwab-page.component.ts:218-233` | ✅ — the ordering discipline the seventh key joins |
| `AbwabPageOverlaysController`: page-provided, **no `Router`/`ActivatedRoute` dependency**, URL side-effects stay the page's job | `abwab-page-overlays.controller.ts:13-30` | ✅ |
| Words-feature precedent end to end: `isOpen`/`isRetainedClosed`, close-retains (replace), restore (push), `NavigationEnd` re-parse, restore button, focus-to-restore | `detail-overlay-history.service.ts:46-48,125-141,52-64`; `detail-modal-shell.component.html:70-82`, `.ts:91-95` | ✅ |
| Global overlay query keys are `qdDetail`/`qdDetailOpen` — a bare `modal` key collides with nothing (repo-wide grep found no `modal` query param) | `detail-overlay.models.ts:5-9` | ✅ |
| The two gates the audit names are live suites | `abwab-url-sync.spec.ts` (69 lines, M26 + build blocks); `e2e/abwab-url-and-a11y.e2e.ts` (150 lines, 6 tests incl. reload/Back-Forward and fail-closed) | ✅ |
| Reveal-in-tree (Slice D): closes the relations modal, then **one** query patch; archived/missing guard; mark keyed off the param emission | `abwab-page.component.ts:379-423`; README `:236-264` | ✅ |

## 0. Guard result

Task arithmetic: Phase 1 = 2, Phase 2 = 2, Phase 3 = 3, Phase 4 = 2, Phase 5 = 3,
Phase 6 = 2, Phase 7 = 3. **17 tasks — under the 30-task threshold. One slice, no
split.**

Recorded so a mid-execution split does not get drawn on task count: if this slice had
split, the seam is **after Phase 4** — "the contract and its machinery" (Phases 2–4: the
seventh key's parse/build, the page wiring, the restore control — the NEW-PATTERN work,
gated by the url-sync spec and new page-spec cases) versus "integration and record"
(Phases 5–6: the reveal interaction, invalidation behaviors, e2e amendments, README/§17
refinement — every one lands on surfaces existing suites already pin). The seam is who
can be hurt: Phases 2–4 create behavior nothing pins yet; Phases 5–6 amend behavior that
is already under test.

## 1. Objective

| # | Deliverable | Home | Audit item |
|---|---|---|---|
| 1 | A seventh query key `modal` joins the abwab URL contract: closed value set, fail-closed parse (including cross-key consistency with `door`), its own scope-invalidation rule mirroring `door`/`card`'s, explicit-`modal` override | `abwab.models.ts`, `abwab-url-sync.ts` + spec | 11 route (a) |
| 2 | Overlay open/close/restore round-trips through the URL: open pushes `modal=<kind>`, Escape/backdrop/close-button retain `modal=<kind>-closed` (replace), restore pushes `<kind>` back, Back/Forward re-parse via the existing subscription | `abwab-page.component.*` | 11 (words pattern, faithfully) |
| 3 | A restore control in the closed state, focus moved to it on close, plus an explicit X that clears the key entirely | `abwab-page.component.*` + `abwab.labels.ts` | 11 ("render a restore control… add an X") |
| 4 | Restore ordering stated and implemented: the modal restores at the same settle point as `door=` (URL + snapshot), door-dependent kinds additionally require a **live** bound subject; archived/missing/out-of-scope ⇒ the key is inert, fail-closed | `abwab-page.component.ts` | 11 + commissioning ordering requirement |
| 5 | Reveal-in-tree and the seventh key reconciled: the reveal's single patch explicitly discards `modal`; the README's "the contract gains no seventh key" sentence amended | `abwab-page.component.ts` + README | 11 × D item 10 |
| 6 | The README page-scoped-overlay invariant **refined, not violated** — stated in the README's own words that a restorable-but-closed state is what makes re-entry safe; URL-contract table grows the seventh row; the `/abwab/templates` "no URL state" statement revisited and its outcome recorded | `features/abwab/README.md` | 11 ("Say that in the README") |
| 7 | Both named gates amended in the same change: `abwab-url-sync.spec.ts` (parse/build for the new key) and `e2e/abwab-url-and-a11y.e2e.ts` (modal survives reload/Back-Forward; restore/discard flow) | the two suites | 11 (slice-E gate list) |

## 2. Scope

**In:**

- `models/abwab.models.ts` — `ABWAB_QUERY_KEYS.modal`, the kind type + closed value set,
  `AbwabQueryState.modal`, its default.
- `state/abwab-url-sync.ts` + `abwab-url-sync.spec.ts` — parse (fail-closed, cross-key),
  build (invalidation + override).
- `pages/abwab-page/abwab-page.component.ts/.html/.scss` + its spec — open-path patches,
  param-emission reconciliation, close/retain/restore/discard navigation semantics,
  restore-ordering effect, the restore control and its focus behavior.
- `models/abwab.labels.ts` — the restore control's Arabic strings (restore label naming
  the overlay, discard `aria-label`).
- `state/abwab-page-overlays.controller.ts` — only if reconciliation needs an idempotent
  open-by-kind entry point; the controller keeps **zero** Router knowledge (its recorded
  boundary, `:19-21`).
- `e2e/abwab-url-and-a11y.e2e.ts` — the mandated gate amendments.
- Docs the above force: `features/abwab/README.md` (invariant `:444-450`, URL-contract
  table `:225-234`, reveal paragraph `:236-245`, templates statement `:266-269`),
  `UI_STYLE_SYSTEM.md` §17 only if the restore control becomes a reusable pattern
  (expected: it does not — it composes existing button/chip classes; record only if
  execution proves otherwise).

**Out (named so nobody "finishes the thought"):**

- **Bulk-subject overlays never write the key**: bulk move (`openBulkMovePicker`,
  controller `:189-196`) and bulk-anchor relations (`openBulkRelations`, `:269-276`)
  read `bulkSet`, which is deliberately not URL state — they stay session-transient.
  Fail-closed, not deferred: restoring a bulk overlay without its bulk set would open a
  lie.
- **The two archive confirms** — they are inline reserved-slot cards, not dialogs
  (`abwab-page.component.html:200-260`), and a destructive confirmation must be
  re-initiated, never URL-restored.
- **The row context menu** — positional and transient (`controller :292-305`).
- **Generalizing `DetailOverlayHistoryService`** (audit route (b)) — explicitly rejected
  by the audit's own recommendation; core navigation shared with words stays untouched.
- **`/abwab/templates` gains no URL state** (decision 4.2-10 below — the "revisit" the
  audit demands is answered and recorded, not silently skipped).
- Draft/form state inside a restored modal (decision 4.2-9: restore reopens the overlay,
  not its unsaved text).
- **Backend changes of any kind**; sections (F), templates (G), navbar (H), cache/ETag
  (I).
- Any planning-artifact deletion (§3) and any `dev → main` merge.

## 3. Non-goals

- **No planning-artifact sweep in this slice — standing user decision.** ALL
  planning-folder sweeps are deferred to one cleanup pass after Slice I. Nothing here
  deletes or repoints a planning folder. **Not deferred:** same-change README/§17
  amendments for behavior this slice changes — those stay mandatory (§1 rows 5, 6).
- **No backend changes** and **no `dev → main` merge**.
- **URL and cache identity must include every input that changes returned scope** —
  standing decision, answered explicitly here: the `modal` key changes **no** returned
  scope anywhere. The snapshot fetch is a single unparameterized tree GET
  (root-scoped facade); the relations read is keyed by door id and uncached — a fresh
  fetch per open (`abwab-relations.controller.ts:33-42`), so a restored relations modal
  re-fetches honestly. No cache key, no restore identity, no history identity carries
  the modal key. If execution finds any surface where it would (including Slice I's
  future cache design), that is a stop condition, not a quiet addition.
- No new E2E posture: the e2e amendment is the audit's mandated same-change gate update
  to an existing suite, not a new tier; no e2e run is cited as a gate in place of Vitest
  + build.
- No new visual vocabulary: the restore control composes existing button/hairline/chip
  classes and existing tokens — no new hue, no new z-band (it is an in-flow control, not
  a floating layer).

## 4. Locked decisions

### 4.1 Carried in from the audit / prior slices / standing rules

1. **Route (a) — URL-encoded overlay state — is the audit's recorded recommendation**
   (`:404-413`): a `modal=` key joining the six existing keys, fail-closed parse per the
   URL contract, a restore control in the closed state, an X that clears the key, and
   Back/Forward for free. Route (b) (generalizing the words service) is recorded as
   rejected there.
2. **The README invariant is being refined, not violated — and the README must say
   that** (`:413-416`): the reason overlay state was page-scoped was "a left-open modal
   would paint again on re-entry before any data loads"; a restorable-but-**closed**
   state is precisely the shape that makes re-entry safe. The amendment states the
   refinement in place (`README.md:444-450`).
3. **The words pattern is followed faithfully** (`:388-401`): close retains (replace
   semantics), restore reopens as a history push so Back returns to the closed state,
   state re-parses on every navigation so browser Back/Forward work, and the closed
   state renders a persistent restore control that receives focus on close
   (`detail-overlay-history.service.ts:125-141`; `detail-modal-shell.component.ts:91-95`).
4. **Fail-closed parse is the contract's own convention** (`abwab-url-sync.ts:20`,
   README `:271-273`): anything invalid parses to the default with **no URL rewrite** —
   a dead `door=` id today stays in the URL, inert. The seventh key inherits exactly
   that stance (no words-style canonicalization pass; that is the global overlay's
   convention, not abwab's).
5. **The URL is the single source of truth for the selection; every path that acts on a
   door writes `door=` before acting** (README `:275-280`). The modal key joins the
   same doctrine — and the **one-navigation rule** from D's reveal (every state folded
   into one `buildAbwabQueryParams` patch; README `:236-245`) governs every patch this
   slice writes.
6. **The overlays controller owns no URL** (`abwab-page-overlays.controller.ts:19-21`):
   URL side-effects stay the page's job. Slice E does not move that boundary.
7. **Same-change doc/spec amendments are never deferred**; the audit names
   `abwab-url-sync.spec.ts` and `e2e/abwab-url-and-a11y.e2e.ts` as this slice's gates
   (`:1106-1107`) — both are amended in the same change as the behavior.
8. **Planning-artifact sweeps are deferred to one pass after Slice I** (standing user
   decision, §3).

### 4.2 Decided by this plan

1. **The seventh key is `modal`, bare, matching the six existing bare keys**
   (`section`/`view`/`archive`/`door`/`card`/`q` — models `:151-159`). Collision check
   done at plan time: the app's only other overlay keys are the global `qdDetail`/
   `qdDetailOpen` (`detail-overlay.models.ts:5-9`), and no `modal` query param exists
   anywhere in `src/app`. Both key families ride the same URL without contact —
   `queryParamsHandling: 'merge'` (page `:437-439`) preserves foreign keys by
   construction.
2. **Value grammar — a closed set of six values encoding kind and visibility:**
   `create` (create root door), `child` (create child of `door=`), `edit` (edit
   `door=`), `move` (move `door=`), `sections`, `relations` (relations of `door=`) —
   each either bare (open) or with the suffix `-closed` (retained, restorable).
   Anything else — unknown kind, bare suffix, casing games — fails closed to `null`
   (absent). One key, per the audit's own sizing ("a `modal=` key"), with visibility in
   the value rather than a words-style second key.
3. **Cross-key consistency is part of the parse, and it fails closed:** the
   door-dependent kinds (`child`, `edit`, `move`, `relations`) parse to `null` when the
   same ParamMap's `door` fails its own parse. `create` and `sections` are
   door-independent. This is parse-level only — the URL is not rewritten (4.1-4); a
   `modal=edit` orphaned by a later `door=null` patch (e.g. archive-success's clear,
   page `:326-328`) simply goes inert and the restore control disappears.
4. **Restorable set = the four true modals, single-subject modes only:** door modal
   (`create`/`child`/`edit`), move picker single mode (`move`), sections modal
   (`sections`), relations modal door mode (`relations`). Every subject is derivable
   from `door=` + snapshot (`openEdit`/`openCreateChild`/`openMovePicker`/
   `openRelations` all read `selectedDoor()` — controller `:73-93,180-187,259-267`).
   Bulk modes and confirms are named outs (§2) — they never write the key, so
   navigation away discards them exactly as today.
5. **History semantics, mirroring words exactly:** opening writes `modal=<kind>` as a
   **push** — folded into the same single patch as the `door=` write on select-then-act
   paths (one navigation, no race; the `onRelationsRequested` shape, page `:356-364`).
   Escape/backdrop/the modal's own close button all **retain**: a replace-navigation to
   `modal=<kind>-closed` (words: "Close/Escape/backdrop: the stack stays"). The restore
   control's X **discards**: a replace clearing the key. Restore **pushes** `<kind>`
   back, so Back returns to the closed state. `updateQueryParams` grows a `replaceUrl`
   option; no second navigation path is invented.
6. **Reconciliation inverts through the existing subscription, idempotently.** The
   param emission (page `:237-266`) parses `modal` with the other six and reconciles the
   overlays controller to it: open kinds ensure the matching overlay is open (a no-op
   when the click already opened it synchronously), `-closed`/absent ensure everything
   URL-backed is closed. Restore ordering (§1 row 4): the reconciliation **effect**
   waits for the same settle point as the `door=` restore (URL + snapshot, whichever is
   second — page `:218-233`), and door-dependent kinds additionally require the bound
   node to exist **and be live** — `byId` holds archived nodes too
   (`abwab-tree.builder.ts:81` sets unconditionally), and the `door=` effect checks
   presence only (`:228-231`), so the modal guard is deliberately stricter, matching the
   reveal's archived/missing guard (`:381-385`). Fail-closed outcome for archived/
   missing/out-of-scope: **nothing opens, no restore control renders, the key sits
   inert in the URL** — the same non-action a dead `door=` produces today.
7. **The reveal's single patch explicitly discards `modal`** (`modal: null` folded into
   the one `buildAbwabQueryParams` call, page `:390-403`). Retaining `relations-closed`
   across a reveal would be a trap: the key's subject is always `door=` (it carries no
   id of its own, by 4.2-2), and the reveal rewrites `door=` to the target — a later
   restore would open the **target's** relations while the user expects the source's.
   Discard is the honest reading of the click's promise ("go to the tree"). Recorded as
   this plan's call — cheap to revisit if the user wants source-door retention, which
   would require the key to carry an id and is deliberately not built now. The README
   reveal paragraph's "**the contract gains no seventh key**" sentence (`:236`) is
   amended in the same change — it was true when D recorded it and stops being true
   here; the replacement states that the reveal *writes* the seventh key only to clear
   it.
8. **Scope invalidation mirrors `door`/`card` verbatim:** switching `section` or turning
   `archive` on clears `modal` alongside `door`/`card` (`abwab-url-sync.ts:65-69` grows
   one line); turning `archive` off restores nothing; an explicit `modal` in the same
   change overrides the clear (the `:71-76` shape). One uniform rule for all three
   dependent keys — including the door-independent kinds (`create`, `sections`), because
   a scope switch is a context change and a uniform rule beats a clever per-kind table.
   Pinned in the build spec beside the existing override case (`spec :66-68`).
9. **Restore restores the overlay, not a draft.** URL state encodes *which* overlay is
   restorable, never form contents — a reopened door modal is pristine from the
   snapshot, exactly as the words frames are entity views, not drafts. Stated in the
   README amendment so nobody later "fixes" it into serialized form state.
10. **`/abwab/templates` stays URL-state-free — the audit's "revisit" is answered, not
    ignored.** Item 11's route (a) requires the README's no-URL-state statement to be
    *revisited* (`:409-410`). Revisited outcome: item 11's "Where" is the doors page's
    overlay controller; the workshop has no deep-link demand, its overlays are
    template-editor working state whose subjects (selected template, editor node) are
    themselves not URL state, and the recorded split trigger ("a URL-state contract
    arriving on this route", README `:166-168`) would fire for zero user benefit. The
    README statement (`:266-269`) is amended to record that Slice E revisited it and
    retained it, so the next reader sees a decision, not an oversight.
11. **The restore control lives in the page header actions area**
    (`abwab-page.component.html:3-12`), rendered only when the parsed state is a valid
    retained-closed kind: one in-flow control composing existing button/hairline
    classes — Arabic label naming the overlay kind («استعادة …»), the X inside it with
    its own `aria-label` for discard, `data-testid`s for both. On every retain-close,
    focus moves to it (words precedent, `detail-modal-shell.component.ts:91-95` — a
    queued focus after the trap releases). No floating layer, no new z-token, no new
    hue. Exact placement/metrics settle at execution against `DESIGN.md`; the
    *existence, labels, focus rule, and testids* are locked here.
12. **Expected test-count delta, stated in advance: net increase, roughly +12 to +25**
    across `abwab-url-sync.spec.ts` (parse: six kinds × open/closed, fail-closed
    garbage, cross-key door dependency; build: invalidation + override),
    `abwab-page.component.spec.ts` (reconciliation idempotence, restore ordering incl.
    archived/missing inertness, retain/discard/restore navigation semantics, focus to
    restore control, reveal-discards-modal), and the e2e suite (modal key in the
    reload/Back-Forward tests + the restore/discard flow). Zero removals; no new spec
    files (every touched surface has one).
13. **One light branch off `dev`: `ux-slice-e-overlays`.** Per-phase commits, PR targets
    `dev`, never `main`.

## 5. The ground truth this plan is derived from

Read before executing; each row is a measured fact from `dev` at `67a4afc9`, not an
assumption. (D merged before planning — no row depends on unmerged work.)

| Fact | Where |
|---|---|
| The six keys and their defaults; keys are bare strings; `AbwabQueryState` is the parse's closed shape | `models/abwab.models.ts:151-178` |
| Parse fails closed per key (`parsePositiveId`, view whitelist, `archive === '1'`); the convention is documented at the function head | `abwab-url-sync.ts:12-32` (comment `:20`) |
| Build: only keys present in the change emit; `section` change or `archive: true` clears `door`+`card`; explicit `door`/`card` in the same change overrides the clear | `abwab-url-sync.ts:49-79`; pinned at `abwab-url-sync.spec.ts:39-68` |
| One `queryParamMap` subscription parses all six keys into page signals; `door=null` clears the selection store; the reveal handshake (`revealPending` + target match) rides the same emission | `abwab-page.component.ts:235-268` |
| The `door=` deep link restores in an `effect` gated on **both** URL and snapshot, `untracked` around the store write — the settle discipline the modal restore must join | `abwab-page.component.ts:218-233` |
| That effect checks `byId` **presence only** — and `byId` holds archived nodes (`byId.set` unconditional), so `door=` today binds an archived door; the modal restore therefore needs its own live-guard | `abwab-page.component.ts:228-231`; `abwab-tree.builder.ts:81` |
| The overlays controller is page-provided with a recorded no-Router boundary; URL side-effects are explicitly the page's job via callbacks | `abwab-page-overlays.controller.ts:13-30` (boundary `:19-21`) |
| Overlay inventory: door modal `:61-97` (three modes: create-root / create-child / edit — mode is which private signals are set); single archive confirm `:100-129`; bulk archive confirm `:132-151`; move picker `:154-220` (private `moveDoorIds`, single vs bulk); sections modal `:223-236`; relations modal `:240-289` (private `relationsAnchorId`, `anchorPickMode`); context menu `:292-305` | `abwab-page-overlays.controller.ts` |
| Every single-subject open path reads `selectedDoor()` — the subject is derivable from `door=` + snapshot; the bulk paths read `bulkSet`, which is not URL state | `abwab-page-overlays.controller.ts:73-93,180-187,259-267` vs `:189-196,269-276` |
| The four modals are composed as siblings with `(closed)` outputs wired to plain `overlays.closeX()`; the two archive confirms are **inline reserved-slot cards**, not dialogs | `abwab-page.component.html:267-306` vs `:200-260` |
| Select-then-act with a synchronous open beside the URL write — the shape modal-open patches extend | `abwab-page.component.ts:356-364` (`onRelationsRequested`) |
| The reveal handler: closes the relations modal first, guards archived/missing, then **one** patch (`door` always, conditional `section`/`view`/`q`); mark/scroll keyed off the param emission, `revealPending` prevents re-arming | `abwab-page.component.ts:379-423` |
| `updateQueryParams` is the page's single navigation choke point: `router.navigate([], { relativeTo, queryParams, queryParamsHandling: 'merge' })` — no `replaceUrl` option today | `abwab-page.component.ts:437-439` |
| Words precedent: `isOpen`/`isRetainedClosed` split `:46-48`; close retains with replace `:125-132`; restore is a push so Back returns to closed `:134-141`; state re-parses on every `NavigationEnd` `:52-64` (doctrine comment `:26-28`) | `detail-overlay-history.service.ts` |
| Words closed-state restore button (persistent, `aria-label`, testid) and the focus-moves-to-restore-on-close rule | `detail-modal-shell.component.html:70-82`; `.ts:91-95` |
| Global overlay keys: `qdDetail` / `qdDetailOpen` — no `modal` query param exists anywhere in `src/app` (plan-time grep, spec files included) | `detail-overlay.models.ts:5-9` |
| The relations read is a fresh uncached fetch keyed by door id — a restored relations modal re-fetches; no cache carries overlay identity | `abwab-relations.controller.ts:33-42` |
| README: the page-scoped-overlay invariant this slice refines | `features/abwab/README.md:444-450` |
| README: the URL-contract table (six rows) and the fail-closed + invalidation paragraph | `features/abwab/README.md:225-234,271-273` |
| README: the reveal paragraph opens "**Reveal-in-tree writes only the keys above — the contract gains no seventh key**" — true at D, amended by E (4.2-7) | `features/abwab/README.md:236-245` |
| README: `/abwab/templates` "carries no URL state at all" + the workshop's recorded split trigger ("a URL-state contract arriving on this route") | `features/abwab/README.md:266-269,162-168` |
| README: "The URL is the single source of truth for the selection" + select-then-act paths | `features/abwab/README.md:275-280` |
| Gate 1: `abwab-url-sync.spec.ts` — M26 parse block + build block, 10 tests, the exact blocks the new key extends | `abwab-url-sync.spec.ts:10-69` |
| Gate 2: `e2e/abwab-url-and-a11y.e2e.ts` — 6 tests: keys survive reload/Back-Forward (`:8`), archive round-trip (`:36`), invalid values fail closed (`:55`), cards drill (`:67`), alias search (`:84`), tree a11y (`:97`) | `e2e/abwab-url-and-a11y.e2e.ts` |
| The page header actions area the restore control joins | `abwab-page.component.html:3-12` |
| Archive success clears `door=` via callback — the patch that can orphan a retained door-dependent `modal` value (goes inert per 4.2-3) | `abwab-page.component.ts:326-328` |

## 6. Phases

### Phase 1 — Baseline and record (2 tasks)

- **T101** — Baseline on `dev`: full Vitest (`npm test`, fork cap preserved via the npm
  script) + `npm run build`; record file/test counts, timings, and the `dev` SHA into
  `docs/feature-ux-slice-e/evidence.md`. No CI exists (`TESTING_STRATEGY.md` §8); every
  later delta measures against this run only.
- **T102** — Record the slice in the root `CLAUDE.md` "Active Spec Kit Feature" section
  (slug `ux-slice-e`, this plan, plan-driven — no `specs/` workspace). Create branch
  `ux-slice-e-overlays` off `dev`. **Do not sweep any planning folder** (§3).

### Phase 2 — The seventh key: contract before consumers (2 tasks)

- **T201** — `models/abwab.models.ts`: `ABWAB_QUERY_KEYS.modal`, the kind union + closed
  value set (4.2-2), `modal` on `AbwabQueryState` (parsed shape: `{ kind, closed } |
  null`, or equivalent — pick the shape the page reads most cleanly and record it),
  default `null`. `abwab-url-sync.ts`: parse per 4.2-2/4.2-3 (closed-set match,
  `-closed` suffix, cross-key door dependency, everything else → `null`, **no URL
  rewrite**); build per 4.2-8 (`section`/`archive:true` clears `modal` beside
  `door`/`card`; explicit `modal` in the same change overrides; `modal: null` clears
  the key).
- **T202** — Spec first, same change (gate 1): extend M26's parse block — each kind
  open and closed, unknown kind, bare `-closed`, door-dependent kind with missing/
  invalid `door` fails closed, door-independent kinds survive without `door`; extend
  the build block — invalidation clears `modal`, explicit-`modal` override, `modal:
  null` clears. Existing 10 tests stay byte-identical. Verification: focused Tier A run
  on the url-sync spec.

### Phase 3 — Page wiring: URL round-trip (3 tasks)

- **T301** — Open paths write the key: every single-subject open folds `modal: <kind>`
  into its existing single patch (`onTreeSelected`-adjacent action handlers, the
  context-menu actions via their page wiring, `onRelationsRequested`, the toolbar's
  create-root and sections openers). One patch per gesture — the one-navigation rule
  (4.1-5); synchronous select + open stay exactly as today (reconciliation makes the
  echo a no-op). Bulk paths and confirms write nothing (4.2-4).
- **T302** — Close semantics: `updateQueryParams` gains a `replaceUrl` option (4.2-5);
  the four modals' `(closed)` outputs route through page handlers that close the signal
  **and** replace-navigate to `modal=<kind>-closed`; the restore control's X
  replace-navigates the key away; restore pushes `modal=<kind>`. Page spec: open patch
  contents per kind, retain-on-close writes `-closed` with replace semantics, discard
  clears, restore pushes (assert via router spy args — push vs replace is the
  contract).
- **T303** — Reconciliation + restore ordering (4.2-6): the param emission reconciles
  overlays to the parsed `modal` (idempotent both directions); a restore effect gated
  like the `door=` effect (URL + snapshot settle) opens a parsed **open** kind once its
  subject binds — door-dependent kinds require a live node (`!isArchived`), else
  nothing opens and the key sits inert. Page spec: deep-link `modal=edit&door=<live>`
  opens edit after snapshot arrival; `door=<archived>` and `door=<missing>` stay
  closed with no control shown; `modal=sections` opens without `door`; emission with
  `-closed` closes an open modal (Back/Forward path).

### Phase 4 — The restore control (2 tasks)

- **T401** — The control (4.2-11): rendered in the header actions area only when parse
  yields a valid retained-closed state; Arabic restore label naming the overlay kind +
  the X with its own `aria-label` (`abwab.labels.ts`, counted-noun rules n/a — plain
  labels); testids; composes existing button/hairline classes, both themes checked in
  the browser. Focus: every retain-close queues focus to the control after the trap
  releases (words precedent). Page spec: renders per state, hidden when the key is
  inert (dead subject), restore click round-trips to open, X click clears, focus lands
  on the control after Escape-close.
- **T402** — A11y pass on the pair: modal Escape → focus restore control → Enter
  reopens → focus returns into the modal (Slice C's `cdkTrapFocusAutoCapture` does the
  re-capture — verify, don't reimplement); discard X announced sensibly. Evidence into
  `evidence.md` (keyboard-only walkthrough, both themes).

### Phase 5 — Interactions with everything that already exists (3 tasks)

- **T501** — Reveal × modal (4.2-7): `onRevealRequested`'s patch adds `modal: null`;
  page spec pins reveal-discards-modal; the README reveal paragraph amendment lands
  with this task (same change).
- **T502** — Invalidation behaviors end to end: section switch / archive-on with an
  open or retained modal clears it (build-level rule from T201, but the page-level
  outcome — modal actually closes via reconciliation — is pinned in the page spec);
  archive-success orphaning a retained `edit` leaves it inert (4.2-3) — spec'd.
- **T503** — Gate 2 (mandated same-change): extend `e2e/abwab-url-and-a11y.e2e.ts` —
  the `:8` reload/Back-Forward test (or a sibling) covers `modal=`: open a modal →
  reload → modal is open; Escape → restore control visible → Back → returns per
  history semantics; invalid `modal` garbage joins the `:55` fail-closed test; the
  restore/discard flow asserted once. Run the amended suite once as evidence (not a
  gate).

### Phase 6 — Docs true again (2 tasks)

- **T601** — `features/abwab/README.md`, one coherent edit: the URL-contract table
  grows the `modal` row (values, absent-means, the `-closed` grammar, the cross-key
  door rule); the invalidation paragraph (`:271-273`) names `modal`; the page-scoped
  invariant (`:444-450`) refined per 4.1-2 — page-scoped is still true, *what changed*
  is that closed-restorable state now lives in the URL, and re-entry is safe precisely
  because restore is explicit (say it in the README's own voice); the no-draft rule
  (4.2-9); the templates statement (`:266-269`) records the revisit outcome (4.2-10).
- **T602** — Cross-check the record as a set: reveal paragraph (T501's edit) reads
  correctly against the new table; controller header comment (`:19-28`) still true
  (URL stays the page's job — verify, amend only if wording drifted); §17 untouched
  unless T401 created a reusable pattern (state the outcome in `evidence.md` either
  way); `docs/contracts/` pointer index needs no change (it defers to README — verify).

### Phase 7 — Verification and close-out (3 tasks)

- **T701** — Tier C against T101: full Vitest + `npm run build` (feature-scope slice —
  no `shared/`/`core/` touch expected; if T401 unexpectedly touches `shared/`, that
  promotes to Tier B — record which ran). Expected delta per 4.2-12: net +12–25, zero
  removed, zero new spec files — any other delta explained or fixed before proceeding.
  No backend change ⇒ no `dotnet test`, no route-smoke tier.
- **T702** — Browser acceptance matrix into `evidence.md`: per kind — open → reload →
  open; Escape → restore control (focus on it) → restore → open again → Back → closed;
  X → key gone; deep link with dead/archived door → inert; section switch → cleared;
  reveal → modal discarded; both themes; keyboard-only pass from T402. The five abwab
  e2e specs run once (single-worker project) as extraction-style evidence.
- **T703** — Close-out sweep: `grep -rn` (prose included) for "no seventh key",
  "gains no", "six keys", "six URL keys", "all six" across the repo — every hit either
  updated or verified still true (the D evidence folder keeps its historical wording —
  evidence is not rewritten); final `evidence.md` entry: baseline vs closing numbers,
  acceptance artifacts. The Active-Feature record clears at merge, not before.

## 7. Testing posture

- Per-phase: Tier A focused globs
  (`npm test -- --include="src/app/features/abwab/**/*.spec.ts"`), fork cap preserved
  (`VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` via the npm script). Suites + build stay
  green at every commit.
- **Expected delta direction, declared in advance: net increase (+12–25), zero
  removals, zero new spec files** — new specs only where this slice's surfaces demand
  them, and every touched surface already has a suite. The two audit-named gates
  (`abwab-url-sync.spec.ts`, `e2e/abwab-url-and-a11y.e2e.ts`) are **mandatory
  same-change amendments**, the primary correctness gates for this slice.
- Behavior-first per test-guard: parse/build tested as pure functions on real
  `ParamMap`s (the suite's existing harness); page behavior via router-spy navigation
  args and DOM assertions, real snapshot DTOs through the builder; no mocking of
  `buildAbwabQueryParams`.
- Pre-PR: Tier C = full Vitest + `npm run build` (frontend-only slice). Browser passes
  and the e2e single-worker run are extraction-style evidence — never cited in place of
  the Vitest suite or the build.

## 8. Risk register

| Risk | Why it is real | Mitigation |
|---|---|---|
| Reconciliation loop: open writes URL → emission reconciles → writes again | The subscription and the click handler both touch the same state | Reconciliation only *reads* URL → state and is idempotent (open-when-open is a no-op); only user gestures write URL; page spec pins one navigation per gesture |
| Push-vs-replace mistakes corrupt history (Back skips or traps) | Three navigation flavors (open push, retain replace, restore push) through one choke point | `updateQueryParams` grows one explicit option; specs assert the flag per flavor; the e2e Back/Forward flow is the end-to-end pin |
| Escape-close focus fight: trap releases while focus moves to the restore control | Slice C's `cdkTrapFocus` + queued explicit focus vs the new queued focus-to-restore | Words precedent is a `setTimeout` after visibility flips (`detail-modal-shell.component.ts:91-95`); T402 verifies the full keyboard round-trip |
| A crafted/stale `modal=` with a dead subject opens a broken overlay | `byId` holds archived nodes; `door=` restore today binds presence-only | 4.2-6's live-guard is stricter than the `door=` effect on purpose; dead states are spec'd inert (no open, no control) |
| The reveal leaves a retained relations key pointing at the wrong subject | The key carries no id; reveal rewrites `door=` | 4.2-7: reveal discards `modal` in its single patch; spec'd; README amended in the same task |
| Restore of a `child`/`edit` modal after the subject moved section | The retained key + `door=` both survive in-scope moves | The subject re-derives from the fresh snapshot at restore; scope *changes* clear the key (4.2-8); move-within-scope keeps a valid subject by construction |
| The templates split-trigger fires accidentally ("a URL-state contract arriving on this route") | README `:166-168` names it as a structural trigger | 4.2-10 keeps templates URL-state-free; nothing in scope touches the templates page |
| The `qdDetail` family and `modal` interleave on one URL | Both ride the same query string | Merge-handling preserves foreign keys (measured, page `:437-439`); the global overlay never opens over `/abwab` today, and no shared parsing exists — collision checked at plan time (4.2-1) |
| The seventh key leaks into scope/cache identity later (Slice I) | Standing decision 5 exists precisely because this is easy to miss | §3 states the measured no-scope-effect verdict; T703's sweep + the README table give Slice I one authoritative row to read |
| Restore control visual drift (new vocabulary) | It is the app's second restore affordance | 4.2-11: composes existing classes/tokens only; both-theme browser check in T401 |

## 9. Obligations checklist (all must be true at close)

- [ ] Baseline recorded (T101) before any change; closing Tier C compared against it (T701)
- [ ] Seventh key `modal`: closed value set, `-closed` grammar, fail-closed parse incl. cross-key door rule, no URL rewrite
- [ ] Invalidation: `section`/`archive:true` clears `modal`; explicit-`modal` override; pinned in the build spec
- [ ] History semantics: open push / retain replace / restore push / discard clear — spec'd via navigation args and e2e Back/Forward
- [ ] Restore ordering joins the `door=` settle discipline; archived/missing/out-of-scope subjects leave the key inert — spec'd
- [ ] Restore control: valid-retained-only render, Arabic labels, X discard, focus-on-close, both themes
- [ ] Bulk overlays, confirms, context menu never write the key (named outs honored)
- [ ] Reveal discards `modal` in its single patch; README's "gains no seventh key" sentence amended in the same change
- [ ] README: URL-contract table row, invalidation paragraph, refined page-scoped invariant (in the README's own words), no-draft rule, templates revisit outcome
- [ ] Both audit-named gates amended same-change; existing tests byte-identical where untouched
- [ ] Cache/scope identity untouched by the new key — stated in evidence, not assumed
- [ ] Test delta within 4.2-12's declared direction; zero tests removed; fork cap preserved; no e2e cited as a gate
- [ ] No backend change; no planning folder deleted or repointed; PR targets `dev`; no `dev → main`

## 10. Execution note

One light branch off `dev`: `ux-slice-e-overlays` (4.2-13). Commits per task or tight
task-pair, phases in order — the ordering is the discipline (contract before consumers;
machinery before the control; integration and record after both; the two gates amended
in the same change as the behavior they pin).

| Phase | Title | Items | Tasks |
|---|---|---|---|
| 1 | Baseline and record | — | T101–T102 (2) |
| 2 | The seventh key (contract + gate 1) | 11 | T201–T202 (2) |
| 3 | Page wiring: URL round-trip | 11 | T301–T303 (3) |
| 4 | The restore control | 11 | T401–T402 (2) |
| 5 | Interactions + gate 2 | 11 × 10 | T501–T503 (3) |
| 6 | Docs true again | 11 | T601–T602 (2) |
| 7 | Verification and close-out | — | T701–T703 (3) |

**17 tasks. Guard: under 30 — one slice, no split** (seam recorded in §0 in case
execution learns otherwise).
