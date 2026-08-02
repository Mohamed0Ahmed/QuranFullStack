# UX Slice L — search highlights, relation-delete confirms, the menu opens the right way, the relations modal comes back

**Mode:** normal plan, NOT Spec Kit — same path as every sibling (`docs/feature-ux-slice-a` … `-k/plan.md` all exist and are normal plans; confirmed against the tree).
**Scope:** frontend-only. The inspection's §8 verdict stands: no route, request/response contract, auth, middleware, or binding changes. This verdict is **void the moment any `Backend/` file enters scope** — that is stop condition 1.
**Branch (executor):** `feature/ux-slice-l` off `dev`. One commit per phase at the stated boundary. No push/PR without an explicit ask.

Everything cited here was re-verified on `dev` at `bf88de50` during planning. If a cited line
has drifted, re-locate by the quoted identifier before editing — do not edit by line number.

---

## 1. Locked decisions (user-confirmed; binding)

- **L1** — highlight, **tree only**; cards and archive keep filtering (the walk keeps producing `visibleIds` for them). Expansion via the **seed** mechanism — collapsible during search, survives clearing. The accumulate-on-broad-query consequence is **accepted**; record it in the README as intended, add no mitigation. **Reveal no longer clears `q`** — slice D's decision is formally reversed; README `:359-361` and page spec `:893` are decision records to rewrite. Match count beside the search input + announced; not in the stats row. Emphasis is an **outline-family persistent mark, never a tint** (measured reasons: `--qd-selected-bg` IS `--qd-accent-tint`; `_tokens.scss:85-100`'s flash lesson). B7-a (per-edge `[...ancestors, node]` allocation) folds in; output byte-identical. Per-keystroke history push is **surfaced as a named open decision (§12), not changed**.
- **L2** — relation delete gets `qd-confirm-dialog`, `tone: 'danger'`, full J6 wiring, layered over the open relations modal (sections-modal delete confirm is the nesting precedent — verified: it needed **no host focus-trap gating**, the dialog just renders inside the modal template with its own trap, `abwab-sections-modal.component.html:138-155`). The missing in-flight guard closes too. Body names **both doors**, the relation kind as the group label, and the two-sided consequence. New consumer of the primitive, not a retrofit.
- **L3** — `qd-context-menu` extends toward the **inline-start** of the anchor and **flips** on viewport collision; both consumers inherit it; the two keyboard call sites anchor at the row's inline-start edge. Test ids untouched. §17's recorded clamping gap is closed by this slice. Verification is browser-only + e2e.
- **L4** — the `modal` key gains its **own subject id** for the reveal's retained state, so the existing in-flow restore control reopens the **source** door's relations. URL-contract change, reload-surviving. Pin the Back-after-reveal path (zero coverage today). L1 and L4 both edit the reveal patch — L4 runs **after** L1; the reveal stays **one** router navigation.

Phase order (locked): **L2 → L3 → L1 → L4**.

---

## 2. Design

### 2.1 L2 — the delete confirm (Phase 1)

Today: chip `(remove)="remove(relation.id)"` (`abwab-relations-modal.component.html:56`) →
`remove()` (`abwab-relations-modal.component.ts:336-348`) dispatches immediately. No confirm, no
in-flight guard — a double-click dispatches twice. Anchor-pick/bulk mode renders the targets
strip instead of groups (`html:33-38`), so it has **no delete path**; one treatment suffices
(re-confirmed).

New component state, copying the sections-modal shape (`abwab-sections-modal.component.ts:80-81`,
`html:138-155`):

- `pendingDelete = signal<AbwabRelationVm | null>(null)` — the chip's remove now **opens** the
  confirm (`pendingDelete.set(relation)`), it never dispatches. Pass the whole VM, not the id:
  the body needs `otherDoorName` and `kind`/`direction` (the group), and the dialog must not
  re-derive them from a list a refetch could be rewriting.
- `deleteBusy = signal(false)`, `deleteError = signal<string | null>(null)` — the dialog stays
  open with both buttons disabled while the write is out (the primitive's `busy` also suppresses
  Escape/backdrop, `confirm-dialog.component.ts:58-70`); a failure lands **inside the dialog**
  as `qd-state variant="error"` (the archive/sections precedent), not in the modal's shared
  `errorMessage`. Success: clear `pendingDelete`, then `refetchAfterWrite(anchorId)` exactly as
  `remove()` does today.
- The double-dispatch hole closes structurally: the confirm's backdrop + trap make the chips
  unreachable while a delete is pending, and `confirmRemove()` guards on `deleteBusy()`.

Template: render the dialog inside the modal file, after the `</section>` sibling of the
backdrop, exactly the sections-modal pattern:

```html
@if (pendingDelete(); as relation) {
  <qd-confirm-dialog
    testIdPrefix="abwab-relations-delete-confirm"
    [open]="true"
    [titleText]="deleteConfirmTitle"
    [confirmLabel]="deleteConfirmLabel"
    [cancelLabel]="cancelLabel"
    tone="danger"
    [busy]="deleteBusy()"
    (confirmed)="confirmRemove()"
    (cancelled)="cancelRemove()"
  >
    <p>{{ deleteConfirmBody(relation) }}</p>
    <p>{{ deleteConfirmSides }}</p>
    @if (deleteError(); as message) {
      <qd-state variant="error" [message]="message" data-testid="abwab-relations-delete-confirm-error" />
    }
  </qd-confirm-dialog>
}
```

Arabic, verbatim (new entries in `abwab.labels.ts`, beside the relations block `:237-256`):

```ts
relationDeleteConfirmTitle: 'حذف العلاقة',
// The group label carries the direction for الشمولية — no separate direction sentence.
relationDeleteConfirmBody: (anchorName: string, otherName: string, group: AbwabRelationGroupKey): string => {
  switch (group) {
    case 'similarity':          return `سيتم حذف علاقة التشابه بين «${anchorName}» و«${otherName}».`;
    case 'opposition':          return `سيتم حذف علاقة التضاد بين «${anchorName}» و«${otherName}».`;
    case 'more-comprehensive':  return `سيتم حذف علاقة الشمولية: «${otherName}» أكثر شمولية من «${anchorName}».`;
    case 'less-comprehensive':  return `سيتم حذف علاقة الشمولية: «${otherName}» أقل شمولية من «${anchorName}».`;
  }
},
relationDeleteConfirmSides: 'ستُحذف العلاقة من الطرفين معًا.',
```

Confirm button reuses the existing `deleteConfirmButton: 'حذف'` (`abwab.labels.ts:369`) and
cancel reuses `cancelButton`. The group key for a VM is derivable the same way the list groups
derive it (`groupAbwabRelations` in `abwab.models.ts`); expose the small helper if it is not
already exported rather than duplicating the kind+direction→group rule.

Both doors are named because "deleted from both sides" is empty wording otherwise: the anchor is
`anchorDoorName()` (input), the partner is `relation.otherDoorName`. The two-sided fact is the
same one the modal header already states (`abwab.labels.ts:244`) and the backend enforces via the
canonical pair row (TESTING_DEBT, abwab-relations row 1).

### 2.2 L3 — menu placement (Phase 2)

Today `qd-context-menu` positions with physical `[style.left.px]/[style.top.px]`
(`context-menu.component.html:6-7`) from raw anchor coordinates, no bounds check — the recorded
gap (UI_STYLE_SYSTEM `:1176-1180`). Consumers pass `event.clientX/clientY` (mouse) or
`rect.left/rect.bottom` (keyboard): doors tree `abwab-tree.component.ts:197-203, :212-215,
:323-330`; template tree `abwab-template-tree.component.ts:111, :118, :130-131`.

**Placement contract (new, owned entirely by the primitive):**

- The menu extends toward the **inline-start** of the anchor point: under `dir="rtl"` its
  **right** edge sits at `x` and the box grows leftward; under `ltr` the current behavior
  (left edge at `x`, grows rightward) is the inline-start-relative mirror and is preserved.
  Direction is resolved from `closest('[dir]')`, the `abwab-tree.component.ts:361-364` pattern.
- **Inline flip:** if the preferred placement would cross the inline-start viewport edge
  (RTL: `x - width < margin`), flip to extend inline-end. **Block flip:** preferred below
  (`top = y`); if `y + height > innerHeight - margin`, open upward (`top = y - height`). After
  flipping, clamp into `[margin, viewport - margin]`. `margin = 8` (px, the `--qd-space-2` step).
- Implementation: the menu renders `visibility: hidden` for its first frame, measures itself
  (`getBoundingClientRect` via `afterNextRender`/effect keyed on `position()`), writes the
  adjusted coordinates to a signal, then becomes visible — no flash at the wrong side, no CDK
  dependency introduced. jsdom never runs this honestly (zero-sized rects), which is why the
  browser is the verification tier here.
- `menuTestId`/`backdropTestId` and the projected-item contract are untouched — the ids are
  load-bearing (§17 `:1153-1157`: 4 Vitest + ~8 Playwright assertions).

**Keyboard call sites (both trees):** anchor x becomes the row's inline-start edge —
`dir === 'rtl' ? rect.right : rect.left` — y stays `rect.bottom`. The doors tree already has
`resolveDirection()`; the template tree does **not** (verified) and mirrors it in.

### 2.3 L1 — search becomes highlight in the tree (Phase 3)

**The walk** (`abwab-tree.builder.ts:158-193`) keeps its result shape — `{isFiltering,
matchedIds, visibleIds, autoExpandedIds}` — because cards and archive still consume `visibleIds`
via `pruneAbwabNodesToVisible`. Rewrite the recursion with one shared push/pop stack (B7-a):
push the node before the children loop, pop after, mark ancestors off the live stack. Zero
per-edge allocation. **Byte-identical output is asserted by the existing M4/prune spec cases
passing unmodified** (`abwab-tree.builder.spec.ts:290-319, :351-380` pin exact set contents);
add one new deep-chain case (≥4 levels, match at the leaf) that would catch a mis-popped stack.

**The page** (`abwab-page.component.ts`):

- The tree branch binds `visibleRoots()` directly — never pruned. `displayRoots` (`:164-167`)
  remains and keeps feeding the **cards** branch; `displayArchivedRoots` (`:214-219`) keeps
  feeding the archive view. The tree's empty guard (`html:159`) switches to `visibleRoots()`.
  Consequence, stated as intended behavior: a no-match query leaves the full tree on screen with
  a zero count — the tree no longer collapses into the misleading «لا توجد أبواب بعد» empty state.
- New `treeMatchedIds = computed(() => this.searchResult().matchedIds)` bound to a new tree
  input.
- **Expansion switches from force to seed.** The tree's `forceExpandedIds` input and its union
  (`abwab-tree.component.ts:63, :99-101`) are **deleted** — the page was its only consumer
  (verified by grep; the tree spec has no case pinning it). The page's `expandSeedIds` binding
  becomes the union of the reveal chain and the search ancestors:
  `computed(() => union(revealExpandSeedIds(), searchResult().autoExpandedIds))`, preserving the
  shared `NO_IDS` identity when both are empty (`:60-62`'s reason). The tree's existing seed
  effect (`:89-97`) merges each new set into `manuallyExpandedIds` — collapsible immediately,
  surviving clear. Accumulation across keystrokes is the accepted behavior.
- The reveal patch **drops its `q` term** (`:498` deleted). The reveal comment block (`:451-463`)
  is rewritten: the pruning premise no longer exists in the tree.

**The tree** (`abwab-tree.component.ts/.html/.scss`): new input
`matchedIds = input<ReadonlySet<number>>(new Set())`; row class binding
`[class.abwab-tree__row--match]="matchedIds().has(node.id)"` beside the `--revealed` binding
(`html:23`). The mark:

```scss
/* Persistent match mark (slice L). Outline-family, never a tint: --qd-selected-bg IS
   --qd-accent-tint, so a tint is zero-delta on a matched row that is selected, and
   _tokens.scss:94 records a tint too close to hover reading as a flash. Inset box-shadow,
   NOT `outline`, on purpose: the reveal ring (--revealed, scss:128-132) and the focus ring
   (:focus-visible, scss:143-146) both own `outline`, and a third outline would make
   visibility depend on SCSS declaration order — a silent-reformat hazard. Different
   properties compose instead of competing. */
.abwab-tree__row--match {
  box-shadow: inset 0 0 0 1px var(--qd-accent);
}
```

**Property separation, not cascade order.** The match mark takes `box-shadow` and the reveal
ring keeps its existing `outline` animation untouched (`scss:119-140`, including its
reduced-motion static form) — the shipped §17 reveal contract does not change. The three states
therefore compose order-independently:

- **match + revealed:** both signals visible simultaneously — the 2px accent outline decaying
  over 3s around the persistent 1px inset shadow.
- **match + focused:** the focus ring plus the match shadow, both visible.
- **match + revealed + focused:** the focus ring wins the *outline* channel over the reveal
  (the existing, shipped "focus still wins over the reveal" rule, `scss:142-146` — untouched);
  the match shadow stays visible throughout.

The match mark never animates, so reduced-motion needs nothing new. The rows' `border-radius`
is followed by an inset shadow, so the mark hugs the same shape hover does.

**The count** (`abwab-toolbar.component.*`): new input `searchMatchCount = input(0)`. Two
elements, because the visible value and the spoken value must update at different rates — a
`role="status"` region updating per keystroke would announce once per typed character, a known
anti-pattern:

```html
<!-- Visible: live, per keystroke, silent to AT. -->
@if (searchQuery() !== '') {
  <span class="abwab-toolbar__search-count" aria-hidden="true" data-testid="abwab-toolbar-search-count">
    {{ matchCountText() }}
  </span>
}
<!-- Spoken: debounced. Always mounted (a status region must exist before it changes to be
     announced reliably), visually hidden, never carries a stale value. -->
<span class="qd-sr-only" role="status" data-testid="abwab-toolbar-search-count-announce">
  {{ announcedCountText() }}
</span>
```

**Debounce the announcement only** — the URL write stays per-keystroke (§12's open decision,
untouched). In the toolbar component: an effect watches `searchQuery()`/`searchMatchCount()`,
resets a plain `setTimeout` (the page's `revealTimer` idiom, cleared in `DestroyRef.onDestroy`),
and after **500 ms** of no change writes the settled phrase into the `announcedCountText`
signal. **On clear** (`searchQuery() === ''`): the pending timer is cancelled and the region is
emptied immediately — clearing announces nothing, and emptying prevents a stale count being
re-read later. `.qd-sr-only` is the existing utility (`styles/_utilities.scss:1`). Do **not**
route the count through `qd-abwab-announcer` (that channel is for one-shot reveal/write messages
and a per-keystroke stream would fight them). The page feeds the branch that actually ran:
`archiveParam() ? archiveSearchResult().matchedIds.size : searchResult().matchedIds.size`
(both computeds already exist; laziness keeps one walk per keystroke). The count is shown in all
views — in cards/archive it counts the same matched doors the filter kept, so it is honest there
too.

Arabic, verbatim (`abwab.labels.ts`; `countPhrase` shape verified at `:24-35`):

```ts
const RESULT_FORMS: ArabicCountForms = {
  zero: 'لا توجد نتائج',
  one: 'نتيجة واحدة',
  two: 'نتيجتان',
  few: 'نتائج',    // 3–10 → «5 نتائج»
  many: 'نتيجة',   // 11+ → «15 نتيجة»
};
searchMatchCount: (count: number): string => countPhrase(count, RESULT_FORMS),
```

**Placeholder** (`abwab.labels.ts:112`) — the parenthetical is now false for the tree and true
for cards/archive, so the copy stops claiming behavior entirely:

```ts
searchPlaceholder: 'ابحث في الأبواب…',
```

The per-view behavior split lives in the README (§8), not in a placeholder that would need to be
view-dependent.

**Search behavior matrix (tree view unless stated):**

| Gesture | Tree state | URL | Expansion |
|---|---|---|---|
| type a character | full tree; current matches carry the 1px mark; count updates | `q=<text>` merged, **history push** (`:326-328`, `:688-694`) | match ancestors seeded open (collapsible) |
| broad query («ال») then narrower | mark/count always reflect the **current** query only | per-keystroke pushes | seeds **accumulate** — branches opened by earlier keystrokes stay open (accepted) |
| clear via the native X (`type="search"`) | marks off, count hidden, full tree | `q` removed (push; empty → key dropped, `abwab-url-sync.ts:96-98`) | **unchanged — survives** (seeds are manual state now) |
| reveal while searching | reveal ring **beside** any match mark on the target for 3s (different properties, §2.3); marks/count untouched | `q` **survives**; patch = §2.4 table | target's ancestor chain seeded on top |
| Back through a typed query | marks/count follow each historical `q` value | steps back one keystroke per Back (named decision §12) | never rewinds — merged seeds stay |
| switch to cards mid-search | cards **filter** (pruned `displayRoots`) | `view=cards` | tree expansion untouched underneath |
| archive on mid-search | archive tree **filters**; `door/card/modal` cleared, `q` survives (`abwab-url-sync.ts:100-105`) | `archive=1` | live-tree expansion untouched |
| zero matches | full tree, no marks | `q` present | count reads «لا توجد نتائج» |

### 2.4 L4 — the modal key carries the reveal's anchor (Phase 4)

**Grammar, verbatim.** The serialized key becomes:

```
modal = <kind>                      open; subject is door= (unchanged)
modal = <kind>-closed               retained; subject is door= (unchanged — follows a later selection)
modal = relations-<id>-closed       retained; subject is door <id>, regardless of door=
```

`<id>` is a positive integer. **Fail-closed rules**, extending `parseModal`
(`abwab-url-sync.ts:31-44`):

- Strip `-closed` first; a remaining `relations-<digits>` yields kind `relations` +
  `subjectDoorId`. The id must pass `isPositiveId`, else the whole key parses to `null` (inert).
- An id on the **open** form (`relations-17` without `-closed`) is invalid → `null`. The open
  state's subject is always `door=`; an id-carrying open key would split the modal's subject
  from the selection, which `canOpen` (`abwab-modal-url.controller.ts:152-162`) exists to forbid.
- An id on any **other kind** (`edit-17-closed`) is invalid → `null`. Only the relations modal
  has a reveal, so only it can be retained with a diverged subject.
- The id-carrying form does **not** require a valid `door=` in the same ParamMap — its subject
  is the carried id. The plain door-dependent forms keep requiring it (unchanged, `:40-42`).

`AbwabModalState` (`abwab.models.ts`) gains `readonly subjectDoorId: number | null` (null =
"subject is `door=`", every existing state). `serializeAbwabModal` (`abwab-url-sync.ts:46-48`)
emits `relations-<id>-closed` when the id is present.

**When the carried door is dead:** `restorableModal`
(`abwab-modal-url.controller.ts:48-51`) gains the id-carrying branch — the state is restorable
iff `snapshot.byId.get(subjectDoorId)` exists and is not archived. Otherwise the key sits inert:
no control, no rewrite, the same fail-closed outcome a dead `door=` already produces (README
`:322-325`). Unlike the plain `-closed` forms, the carried subject is **pinned** — selecting
another door does not move what restore would reopen.

**The reveal patch (final shape, after L1 + L4 — still ONE `router.navigate` push):**

```ts
buildAbwabQueryParams({
  door: targetId,                                                  // always
  modal: { kind: 'relations', closed: true, subjectDoorId: anchorId },  // retained WITH the source id (was: null)
  ...(foreign section tab active ? { section: node.sectionId } : {}),  // unchanged (:494-496)
  ...(cards view open ? { view: 'tree' } : {}),                        // unchanged (:497)
  // no q term — removed in Phase 3
})
```

`anchorId` is `overlays.relationsAnchorDoorId()` read before the overlay closes; if it is
somehow null (door mode guarantees it isn't), fall back to `modal: null` — never emit a
malformed key. The dead-target guard branch (`:465-472`) keeps its discard unchanged (pinned at
spec `:1396`; it navigates nowhere, so there is no diverged subject to retain).

**The state-mutation table — one chip-name click, after both changes, in order:**

| # | Mutation | Where |
|---|---|---|
| 1 | relations overlay closes synchronously | `abwab-page.component.ts:474` |
| 2 | URL tracking released | `:477` |
| 3 | reveal announcement cleared | `:478` |
| 4 | `revealTargetId` ← target | `:479` |
| 5 | `revealSequence`++ (invalidates the expand-seed computed) | `:480` |
| 6 | `revealPending` ← true | `:481` |
| 7 | **one push navigation**: `door=<target>` · `modal=relations-<source>-closed` · `section=<target's>` iff foreign tab · `view=tree` iff cards — **no `q` term** | `:482-500` reshaped |
| 8 | param emission: seven keys re-parsed; `q`/search state **untouched by the reveal** | `:284-291` |
| 9 | selection rebinds to the target (source selection released) | `:256-266`, `:299-301` |
| 10 | `syncFromUrl` sees a retained key; `opened` is null → no-op close | `abwab-modal-url.controller.ts:66-80` |
| 11 | `restorableModal` becomes non-null → **the restore control renders, naming the source** | `:48-51` + page `html:9-15` |
| 12 | `startReveal`: mark set, 3s timer, `scrollIntoView` | `:506-519` |
| 13 | tree seed effect merges the target's ancestor chain (search seeds already merged, untouched) | `abwab-tree.component.ts:89-97` |

**Restore.** `onModalRestoreRequested` (`:627-634`): when the retained state carries an id, the
patch is `{ door: subjectDoorId, modal: { kind: 'relations', closed: false, subjectDoorId: null } }`
— one push. The emission then drives the **existing** deep-link machinery: the `door=` effect
selects the source, `reconcileOpen` opens the relations overlay on it (exactly the shape spec
`:1187` already pins). The open state never carries an id — after restore, subject = `door=`
again, every invariant intact. Back from the restored modal returns to the retained state
(restore is a push), matching the existing retain/restore history contract (README `:399-409`).

**Retained-key lifecycle — one key, one retained state (decided, not accidental).** The `modal`
key is single-valued, so whatever writes it next wins. That is the **intended** contract — the
URL records at most one overlay, open or retained; it already governs the plain `-closed` forms
today and the id-carrying form joins it, not an exception to it:

| Event while `relations-<id>-closed` is retained | Outcome |
|---|---|
| open **any** modal (edit/move/child/create/sections/relations) | `commitModalOpen` (`abwab-page.component.ts:678-686`) **overwrites** the key; the retained state and its restore control vanish, permanently (closing the new modal retains *that* modal's plain `-closed`, never resurrects the id-carrying one) |
| open the relations modal **fresh on another door** (the reverse order) | same overwrite: `door=<new>&modal=relations`; closing it retains plain `relations-closed` whose subject is the **new** door — the carried id is gone |
| a second reveal from the restored modal | overwrites with the new `relations-<newSource>-closed` |
| restore | rewrites to `door=<id>` + bare `relations` (§ above) |
| the control's X | discards by replace (existing path, unchanged) |
| section switch / archive on | cleared by the scope-invalidation rule (`abwab-url-sync.ts:100-105`, unchanged) |
| selecting another door (no modal) | **no-op** — the carried subject is pinned; only the plain `-closed` forms follow `door=` |

Both overwrite rows are **pinned in Phase 4** so this stays a decision: (a) reveal-retain →
open sections modal → key is `sections`, control gone; close it → `sections-closed`, the
relations state does not return; (b) `relations-7-closed` present → open relations on door 3 →
key is `relations` with `door=3`; close → plain `relations-closed` bound to door 3.

**Restore-control copy.** The control must say WHOSE relations it holds — that ambiguity is the
whole bug. `AbwabModalRestoreComponent` gains `subjectDoorName = input<string | null>(null)`;
the page passes the carried door's name from `byId`. Labels, verbatim:

```ts
// modalKindNames.relations stays 'علاقات الباب' for the plain retained form.
relationsOfDoorKindName: (doorName: string): string => `علاقات «${doorName}»`,
```

so the existing `modalRestoreLabel`/`modalDiscardAriaLabel` compose to
«استعادة علاقات «الصبر»» / «تجاهل علاقات «الصبر»».

**Cache honesty.** The relations list is cached by door id + tree validator
(`abwab-relations.controller.ts:42-68`); a reveal changes neither, so a **same-session** restore
reopens the source's list with **zero** additional relations GETs — asserted in e2e with the
passive `page.on('request')` counter that file already uses (`e2e/abwab-relations.e2e.ts:9-21`;
passive listener, never `page.route`, per its recorded reason). **After a reload** the cache is
empty (root-scoped service, fresh app), so restore issues exactly one GET — the reload case
asserts the control renders and the restore works, not zero requests. State both in the e2e.

**The Back pin (zero coverage today).** Back-after-reveal lands on the previous entry
(`modal=relations&door=<source>`) and the reconcile machinery reopens the modal on the source —
the README's designed path (`:399-409`), pinned by nothing. Phase 4 pins it twice: a page-spec
case driving the params subject through reveal-then-back emissions, and an e2e `page.goBack()`
assertion.

---

## 3. Phases

Vitest runs use the `npm test` script (fork cap baked in). Tier A globs below are the phase
gates; §9 has the pre-PR tier.

### Phase 1 — L2: the relation delete confirms (7 tasks)

1. `abwab.labels.ts` — add `relationDeleteConfirmTitle`, `relationDeleteConfirmBody`,
   `relationDeleteConfirmSides` (§2.1, verbatim). Export the kind+direction→group helper from
   `abwab.models.ts` if `groupAbwabRelations` does not already expose it per-relation.
2. `abwab-relations-modal.component.ts` — `pendingDelete`/`deleteBusy`/`deleteError` signals;
   chip remove sets `pendingDelete`; `confirmRemove()` (busy-guarded, dispatches
   `deleteRelation`, error → `deleteError`, success → clear + `refetchAfterWrite`);
   `cancelRemove()` (busy-guarded). The old direct-dispatch body of `remove()` moves into
   `confirmRemove()`.
3. `abwab-relations-modal.component.html` — the dialog block (§2.1) after the modal section,
   the sections-modal nesting pattern.
4. **Rewrite** `abwab-relations-modal.component.spec.ts:598-612` — the case that pins immediate
   deletion becomes: remove-click opens the confirm and dispatches nothing; its new rationale
   comment names this plan. New cases: confirm dispatches with the right id; cancel closes
   without dispatching; busy holds the dialog open and blocks a second confirm; a failed write
   renders the error inside the dialog and keeps it open; the body names both doors and the
   group; reveal-vs-remove independence (the surviving half of the old case).
5. `UI_STYLE_SYSTEM.md` §17 `qd-confirm-dialog` — extend the consumer list (`:783-789`): the
   relations modal's delete confirm, noting it is the primitive's first **new** consumer (no
   hand-written confirm existed here; the "retrofit complete" claim stays true).
6. Feature `README.md` — the relations-modal paragraph (the write-error sentence around `:647`)
   gains the confirm: deletes confirm first, naming both doors; write errors inside the dialog.
7. Tier A: `npm test -- --include="src/app/features/abwab/components/abwab-relations-modal/*.spec.ts" --include="src/app/features/abwab/models/*.spec.ts"` — all green, counts stated.

**Commit boundary:** `feat(abwab): confirm a relation delete, naming both sides`

### Phase 2 — L3: the menu opens inline-start and flips (7 tasks)

1. `context-menu.component.ts/.html/.scss` — the placement contract of §2.2: dir resolution,
   measure-then-place (hidden first frame), inline-start extension, both flips, clamping.
   Test ids and the projected-content contract byte-identical.
2. `abwab-tree.component.ts:323-330` — keyboard anchor x becomes
   `dir === 'rtl' ? rect.right : rect.left` via the existing `resolveDirection()`.
3. `abwab-template-tree.component.ts:126-131` — same change; mirror in `resolveDirection()`
   (verified absent there).
4. Existing menu specs stay green **unchanged** (page spec `:775-800`, `:1050-1076`; tree spec
   menu block; e2e ids) — that is the no-regression evidence for the extraction contract.
5. e2e — extend `e2e/abwab-operations.e2e.ts` (the file already owning the menu flows `:119,
   :141`) with one placement test: right-click a row mid-viewport → `boundingBox()` asserts the
   menu's **right** edge ≈ click x (±2px, extends leftward); dispatch a right-click at x≈20 →
   menu box stays fully inside the viewport (flipped/clamped). Keyboard: in
   `e2e/abwab-url-and-a11y.e2e.ts:171`'s Shift+F10 flow, add the assertion that the menu box is
   inside the viewport and its right edge is at/inside the focused row's right edge.
6. `UI_STYLE_SYSTEM.md` §17 `qd-context-menu` — rewrite gap 1 (`:1176-1180`) as **closed by
   slice L**, stating the new placement contract; gaps 2–3 remain, restated. `docs/TESTING_DEBT.md`
   abwab-templates row 9: restate — doors-page menu placement now has an e2e assertion; the
   template tree's menu paths remain browser-walk-only.
7. Tier A: `npm test -- --include="src/app/features/abwab/components/abwab-tree/*.spec.ts" --include="src/app/features/abwab/pages/**/*.spec.ts"` green; `npm run e2e` run for the two touched files' evidence (opt-in tier, run deliberately — jsdom cannot verify placement, this is the honest check); browser walk at 1024px both edges, both themes, noted in the commit message.

**Commit boundary:** `fix(ui): the context menu opens toward inline-start and flips at the viewport`

### Phase 3 — L1: highlight in the tree, count, seed expansion, B7-a (10 tasks)

1. `abwab-tree.builder.ts` — push/pop stack rewrite of `searchAbwabNodes` (§2.3); result shape
   unchanged.
2. `abwab-tree.builder.spec.ts` — M4/prune cases (`:290-319, :351-380`) pass **unmodified**
   (the byte-identical assertion); add the deep-chain stack case. The M4 describe comment gains
   the dual-consumer note: `matchedIds`+`autoExpandedIds` feed the tree's highlight,
   `visibleIds` feeds the cards/archive filter.
3. `abwab-tree.component.ts/.html/.scss` — `matchedIds` input + `--match` class + the inset
   box-shadow rule with its property-separation comment (§2.3); **delete** `forceExpandedIds`
   and the union (`:63, :99-101` — `effectiveExpandedIds` collapses to the manual set). The
   reveal ring's scss (`:119-140`) is untouched.
4. `abwab-tree.component.spec.ts` — new block: a matched row carries the class; a non-match
   doesn't; a revealed match carries both classes and the two marks live on **different
   properties** (assert the computed/declared property split — `--match` styles `box-shadow`,
   `--revealed` styles `outline` — so the composition cannot regress into a cascade race).
5. `abwab-page.component.ts/.html` — tree binds `visibleRoots()` + `[matchedIds]`; empty guard
   on `visibleRoots()`; `expandSeedIds` union computed (reveal ∪ search ancestors, `NO_IDS`
   identity preserved); reveal patch drops the `q` term (`:498`) and the comment block
   (`:451-463`) is rewritten to the new rationale; count computed
   (`archiveParam() ? archiveSearchResult : searchResult`).
6. `abwab-toolbar.component.ts/.html/.scss` — `searchMatchCount` input, the two-element
   count (visible `aria-hidden` span + always-mounted `.qd-sr-only` `role="status"` region),
   the 500 ms announcement debounce with its `DestroyRef` cleanup, empty-on-clear (§2.3);
   muted styling on the search container's type ramp.
7. Labels — `RESULT_FORMS`, `searchMatchCount`, the neutral `searchPlaceholder` (§2.3 verbatim);
   labels spec extended for the four count forms + zero.
8. **Rewrite the decision-record specs**, each with its new rationale in-comment:
   - page spec `:490-509` (T507 "hides a non-matching door") → the tree keeps every row, marks
     the match, seeds the ancestor open, shows the count; a follow-up case: clearing `q` leaves
     the seeded branch open; a third: cards view still prunes.
   - page spec `:893-898` (reveal clears `q`) → reveal **preserves** `q`: the patch carries no
     `q` key and the marks survive the reveal (slice D's decision reversed by the user,
     2026-08-02).
   - toolbar spec — the visible count renders only while a query is present and is
     `aria-hidden`; the status region is always mounted; with fake timers
     (`vi.useFakeTimers`): the region stays empty through rapid input changes, carries the
     settled phrase after `advanceTimersByTime(500)`, resets its window on a new keystroke,
     and empties immediately on clear with no announcement.
   - e2e `abwab-url-and-a11y.e2e.ts:158` (alias match) — re-point its assertion from
     "non-match hidden" to "non-match visible without the mark, match marked, count text
     correct"; `:10`'s `q`-survives-Back flow stays as-is (it pins the URL, not pruning).
9. Feature `README.md` — rewrite the search paragraphs: the per-view split (tree highlights,
   cards/archive filter, one search box, deliberately); seed-not-force expansion with the
   accumulate-on-broad-query behavior recorded **as intended**; clearing preserves expansion;
   the count; the no-match full-tree state; rewrite `:359-361` (reveal no longer touches `q` —
   premise gone with pruning). `UI_STYLE_SYSTEM.md` §17 Reveal-highlight entry gains the
   persistent match variant: 1px/2px pairing, the stacking rule, and the in-browser
   measurements (§7).
10. Tier A: `npm test -- --include="src/app/features/abwab/**/*.spec.ts"` (the feature's full
    glob — this phase touches builder, tree, toolbar, page, labels) — green, counts stated.

**Commit boundary:** `feat(abwab): search marks matches in place — the tree stops hiding doors`

### Phase 4 — L4: the retained key carries the reveal's anchor (9 tasks)

1. `abwab.models.ts` — `AbwabModalState.subjectDoorId: number | null`.
2. `abwab-url-sync.ts` — `parseModal`/`serializeAbwabModal` per the §2.4 grammar and fail-closed
   rules; `buildAbwabQueryParams` unchanged in shape.
3. `abwab-url-sync.spec.ts` — new grammar cases: `relations-17-closed` parses with the id and
   without needing `door=`; round-trips through serialize; `relations-17` (open+id),
   `edit-17-closed`, `relations-0-closed`, `relations-x-closed` all fail closed to null;
   existing cases green unmodified.
4. `abwab-modal-url.controller.ts` — `restorableModal`'s id-carrying branch (live-node check,
   subject pinned; §2.4).
5. `abwab-page.component.ts/.html` + `abwab-modal-restore.component.ts/.html` — reveal patch
   retains with the anchor id (§2.4, `anchorId` read before the overlay closes, null → discard
   fallback); restore handler's id branch (one push: `door` + bare open key); `subjectDoorName`
   input + `relationsOfDoorKindName` label wiring.
6. **Rewrite/extend the page spec decision records:**
   - `:1417` ("a reveal discards the key") → a reveal **retains** `relations-<sourceId>-closed`
     in its single patch; rationale comment: the key now carries the diverged subject itself.
   - `:1396` (dead-target discard) — green **unchanged**; assert it stays a discard.
   - New: the restore control renders after a reveal, naming the source door; restoring writes
     `door=<source>` + `modal=relations` in one patch and the modal reopens on the source
     (drive the params subject); the control does not render when the carried door is archived
     or absent; a carried key survives a selection change un-moved.
   - New — **the retained-key overwrite pins** (§2.4 lifecycle table, both orders): (a) after a
     reveal retains `relations-<src>-closed`, opening the sections modal overwrites the key and
     the control disappears; closing it retains `sections-closed` and the relations state does
     not return; (b) with `relations-7-closed` in the URL, opening relations fresh on door 3
     overwrites the carried key; closing retains plain `relations-closed` bound to door 3.
   - New — **the Back pin**: emission sequence reveal-patch → back-emission
     (`modal=relations&door=<source>`) reopens the modal on the source (README `:399-409`,
     previously pinned by nothing).
7. e2e — extend `e2e/abwab-relations.e2e.ts` with two tests using its passive read counter:
   (a) open relations on A (1 GET) → reveal B → restore control visible with
   «استعادة علاقات …» naming A → restore → modal open, title `علاقات «A»`, reads counter
   **still 1** (cache); (b) reveal → `page.goBack()` → modal reopens on A; then a reload of the
   retained URL → control renders → restore works (reads counter +1 — the fresh-cache case,
   stated as such).
8. Docs in the same commit: README locked key table (`:308-316`) gains the id-carrying row and
   the grammar/fail-closed rules; rewrite the reveal-and-`modal` paragraphs (`:348-361` — the
   reveal now retains with a pinned subject; the "the key holds no id of its own" sentence
   `:318-321` is scoped to the open and plain-closed forms) and the push/replace paragraph
   (`:399-409` — the reveal now retains by push; Back semantics unchanged and now pinned).
   Reconcile the stale e2e count sentences with the measured run: `TESTING_STRATEGY.md:417-418`
   still says "48 passed … five specs" against eight abwab spec files (pre-existing staleness,
   verified), and `e2e/README.md:74`'s "20 Abwab tests" — update both to the numbers the Phase-4
   `npm run e2e` actually prints.
9. Tier A: `npm test -- --include="src/app/features/abwab/state/*.spec.ts" --include="src/app/features/abwab/pages/**/*.spec.ts" --include="src/app/features/abwab/components/abwab-modal-restore/**"` green; `npm run e2e` full run (both projects), numbers recorded.

**Commit boundary:** `feat(abwab): the reveal retains the relations modal — restore reopens the source`

---

## 4. Accessibility & RTL obligations

- The match mark never relies on color alone: the ring is a geometry signal and the count is the
  textual signal. The visible count is `aria-hidden` and updates per keystroke; the spoken count
  lives in a separate always-mounted `.qd-sr-only` `role="status"` region that speaks **once,
  500 ms after typing settles** — never per keystroke — and is emptied silently on clear
  (deliberately not the announcer channel).
- The confirm follows J6 end to end: initial focus on cancel (primitive built-in), busy disables
  both buttons and suppresses Escape/backdrop, `aria-busy` on confirm, the error is `role="alert"`
  via `qd-state` inside the dialog. Focus returns to the relations modal's trap on close
  (nesting precedent: sections modal, no host gating needed — verified).
- The menu flip must not break keyboard entry: the Shift+F10/ContextMenu path anchors at the
  focused row's inline-start edge and the flipped menu stays fully on-screen (e2e-asserted).
  Escape dismissal and the document-level listener are untouched.
- Logical properties only in all new SCSS (`inline-size`, `inset-inline-*`); the menu's placement
  math is dir-resolved, not hardcoded-RTL.
- The restore control keeps naming its subject: «استعادة علاقات «X»» / «تجاهل علاقات «X»».

## 5. Measurement obligations (in-browser, recorded — never eyeballed)

- **Match mark** (Phase 3): at 1024px and 1440px, both themes: the 1px inset accent shadow is
  visible on (a) the plain row surface, (b) the hover fill, (c) the selected fill; a revealed
  match shows **both** marks simultaneously and they read as two signals (2px animated outline +
  1px static inset shadow), and the match-mark half survives focus (match + focused shows the
  focus ring plus the shadow). Record the observations in the §17 entry the phase updates. If
  the 1px mark fails legibility on the selected fill in either theme, stop condition 4 fires
  (do not silently thicken or recolor).
- **Menu placement** (Phase 2): right-click at mid-viewport, at the inline-start edge, and at the
  bottom edge, 1024px and 1440px, both themes: extends inline-start, flips, never clips. Recorded
  in the commit message alongside the e2e run.

## 6. Verification tiers (TESTING_STRATEGY §4: "Frontend feature only")

- Tier A per phase as listed. No route-smoke tier, no pipeline tiers — frontend-only (§8 verdict;
  stop condition 1 guards it).
- **Pre-PR Tier C:** full `npm test` **and** `npm run build`, counts stated. There is no CI —
  every gate is a local run whose evidence must state counts.
- e2e is opt-in and never Tier-C evidence — but L3 and L4 are browser-truths, so Phases 2 and 4
  run `npm run e2e` deliberately and record the real numbers (also feeding the count
  reconciliation in Phase 4 task 8).

## 7. Risks

- **Two seed sources merging on one input** (Phase 3): the reveal seed and the search seed now
  union; the shared-identity rule (`NO_IDS`) must survive or every CD tick re-runs the tree's
  merge effect. The union computed must return `NO_IDS` itself when both are empty.
- **The mark pairing is property-separated by design** (Phase 3): match = `box-shadow`,
  reveal = `outline` — composition is order-independent and a reformat cannot silently swap
  which mark shows. The one surviving ordered rule is pre-existing and untouched:
  `:focus-visible` beats the reveal on the outline channel (`scss:142-146`). Do not "unify"
  the two marks onto one property; that reintroduces the race this plan removed.
- **The announcement debouncer** (Phase 3): a timer signal-written from an effect — cancel on
  destroy (`DestroyRef`) and on clear, or a navigation away mid-typing announces into nothing;
  the toolbar spec's fake-timer cases are the fence.
- **Grammar widening** (Phase 4): every malformed variant must land on `null`, not on a partial
  parse — the url-sync spec's negative table is the fence.
- **The restore-race** (Phase 4): restore relies on the existing settle-gated open machinery
  (spec `:1187`'s shape). Do not open the overlay synchronously in the restore handler — write
  the patch and let the emission drive it, or the echo-no-op invariant breaks.
- **e2e flakiness at edges** (Phase 2): coordinate assertions use ±2px tolerance and
  `boundingBox()`, never screenshot comparison.

## 8. Rollback

Each phase is one commit; `git revert <sha>` restores the prior contract including its specs and
docs (docs ride in the same commit precisely so a revert is total). Phase 4 reverts cleanly to
the Phase-3 world (reveal discards the key — the pre-L4 behavior with L1's `q` change intact);
Phase 3 reverts to filtering; no phase leaves a mixed contract behind.

## 9. Stop conditions

1. Any change requires touching a `Backend/` file → stop, report; the frontend-only verdict is void.
2. A pinned spec outside the named rewrite list (§10) fails for a behavioral (non-mechanical)
   reason → stop; the plan missed a contract.
3. The Phase-4 grammar cannot fail closed for any input (something reaches the controller as a
   partial parse) → stop; do not ship a lenient parse.
4. A measurement obligation fails (mark illegible on a fill, menu clips after the fix) → stop,
   report the numbers; do not tune styles beyond the plan's values on this slice's authority.
5. `npm run e2e` fails on a test this slice did not touch → stop and report before rerunning;
   the abwab project writes to the shared local DB.

## 10. Decision-record specs — REWRITTEN, never silently deleted

| Spec | Today pins | Becomes |
|---|---|---|
| `abwab-relations-modal.component.spec.ts:598-612` | remove-click deletes immediately | remove-click opens the confirm; confirm deletes; busy/error/cancel cases |
| `abwab-page.component.spec.ts:490-509` (T507) | search hides non-matching doors | search marks matches, keeps every row, seeds ancestors, shows the count |
| `abwab-page.component.spec.ts:893-898` | reveal patch clears `q` | reveal patch carries no `q` key; search survives (slice D reversed, 2026-08-02) |
| `abwab-page.component.spec.ts:1417` | reveal discards the `modal` key | reveal retains `relations-<sourceId>-closed` in the single patch |
| `e2e/abwab-url-and-a11y.e2e.ts:158` | alias match hides the non-match | alias match marks the match; non-match stays visible unmarked; count correct |
| `abwab-tree.builder.spec.ts` M4/prune (`:290-319, :351-380`) | filter semantics | **unchanged and green** — the byte-identical fence for B7-a; comment gains the dual-consumer note |
| `abwab-page.component.spec.ts:1396` | dead-target reveal discards | **unchanged and green** — asserted to stay a discard |

Zero-coverage areas this slice closes, by name: menu placement (e2e, Phase 2), Back-after-reveal
(unit + e2e, Phase 4), relation-delete confirmation (unit, Phase 1). Remaining uncovered and
recorded: the template tree's menu paths (TESTING_DEBT row 9, restated in Phase 2).

Two decisions this slice **creates** are pinned at birth rather than left to archaeology: the
single-retained-state overwrite rule in both orders (§2.4 lifecycle table → Phase 4 pins) and
the match/reveal property split (`box-shadow` vs `outline` → the tree spec's property
assertion, Phase 3 task 4).

## 11. Acceptance criteria (each independently checkable)

1. Deleting a relation always shows a danger confirm naming both doors and the group, stays open
   busy until the write resolves, shows a write failure inside itself, and never double-dispatches.
2. The context menu extends toward inline-start from every entry path (right-click, ⋯, keyboard)
   on both consumer pages, and never clips at any viewport edge (e2e + recorded browser walk).
3. With a query typed: every door stays in the tree; matches carry the mark; ancestors open and
   remain collapsible; the visible count updates per keystroke while the announcement fires
   **once after typing settles** (fake-timer-asserted) and clearing announces nothing; the
   native X clears marks and count but collapses nothing.
4. Cards and archive still filter under the same query; the picker's search still filters
   (untouched); the README states the split.
5. A reveal during a search preserves `q` and the marks, and remains a single navigation.
6. After a reveal, the restore control appears naming the source door; restoring reopens the
   source's relations — same-session from cache with zero extra GETs (e2e-counted), after reload
   with exactly one. Back after a reveal reopens the source's relations (unit + e2e pinned).
   A revealed match shows both marks at once (property-separated), and focus keeps the match
   mark visible.
7. Opening any modal while `relations-<id>-closed` is retained overwrites it, control and all,
   in **both** orders (§2.4 lifecycle table) — pinned, so the single-retained-state rule is a
   decision, not an accident.
8. A malformed id-carrying key (`relations-x-closed`, `edit-5-closed`, `relations-5`) is inert:
   nothing opens, no control renders, the URL is not rewritten.
9. Tier C green: full `npm test` + `npm run build`, counts stated; the M4/prune and `:1396`
   fences green unmodified.

## 12. Named open decision for the user (surfaced, NOT implemented)

**Search history granularity.** Search pushes one history entry per keystroke, undebounced
(`abwab-page.component.ts:326-328`, `:688-694`), and e2e pins `q` surviving Back/Forward. After
this slice, Back through a typed query steps the marks and count backwards one keystroke at a
time while expansion never rewinds (seeds are merged manual state). That is coherent but chatty.
If it should change, the options are debouncing the URL write or `replaceUrl` per keystroke with
a final push — either alters the pinned Back/Forward contract and is **not** done under this
slice's authority.

## 13. Non-goals

- Any Backend change (stop condition 1).
- Highlight in the cards or archive views — both keep filtering, deliberately, same search box.
- The door picker's own search — stays a filter (`abwab-door-picker`); the asymmetry with the
  tree is deliberate and now README-recorded.
- Debounce/replaceUrl on search history (§12 — decision, not work).
- The deferred planning-artifact cleanup pass (`docs/feature-ux-slice-*` sweep).
- Anything arising from the pending J+K engineering review.
- Reconciling the `--danger` menu-item rest-state split or menu focus management (§17 gaps 2–3
  — recorded as remaining).
