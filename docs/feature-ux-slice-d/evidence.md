# Slice D — evidence

Companion to `plan.md`. Every number here is measured, not estimated.

## T101 — Baseline (before any change)

- Branch: `ux-slice-d-tree`, cut from `dev` at **`a6601a1f`** (clean tree; only
  `docs/feature-ux-slice-d/` untracked).
- Date: 2026-08-01.
- No CI exists (`TESTING_STRATEGY.md` §8) — this local run is the only comparison point
  for T901.

### Full Vitest suite (`npm test`, fork cap `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`)

```
Test Files  193 passed (193)
     Tests  2219 passed (2219)
  Duration  198.28s (transform 6.25s, setup 79.51s, collect 15.59s,
                    tests 61.32s, environment 181.66s, prepare 18.17s)
```

Exit code 0.

### Production build (`npm run build`)

```
Application bundle generation complete. [16.824 seconds]
```

Exit code 0, with three **pre-existing** budget warnings that are baseline noise, not
regressions introduced by this slice:

| Warning | Amount |
|---|---|
| `bundle initial exceeded maximum budget` (500.00 kB) | 569.06 kB (+69.06 kB) |
| `selected-ayah-section.component.scss` (4.00 kB) | 5.85 kB (+1.85 kB) |
| `selected-word-section.component.scss` (4.00 kB) | 4.65 kB (+649 B) |

Relevant lazy chunk baselines (raw / transfer):

| Chunk | Raw | Transfer |
|---|---|---|
| `abwab-page-component` | 103.34 kB | 18.19 kB |
| `abwab-templates-page-component` | 46.45 kB | 9.08 kB |

### Environment available for the browser/backend-dependent tasks

Checked at Phase 1 so the plan's browser tasks (T201/T202, T303, T502, T602, T804, T902)
are not silently downgraded:

| Prerequisite | State |
|---|---|
| PostgreSQL | up on `localhost:5432`, DB `quran_dashboard` present |
| Backend user secret (`ConnectionStrings:QuranDashboardDb`) | configured (`Password=123456`) |
| `dotnet` / `psql` / `docker` | all on PATH |
| Abwab data | 13 live doors, 343 total (330 archived), 46 relations, 166 sections |
| Browser automation | Chrome MCP + Playwright available |

So Phase 2's DevTools method and Phase 3's live reproduction are both runnable as
specified.

One setup note worth keeping: the API's own dev certificate is not in this machine's
Chrome trust store, so `https://localhost:4200` renders but every `https://localhost:5015`
call fails as «تعذر تحميل شجرة الأبواب» / `Failed to fetch`. Starting Kestrel with the
frontend's own cert makes both origins present the same certificate and the page loads
live data:

```
ASPNETCORE_Kestrel__Certificates__Default__Path=Frontend/quran-dashboard-ui/localhost.pem \
ASPNETCORE_Kestrel__Certificates__Default__KeyPath=Frontend/quran-dashboard-ui/localhost-key.pem \
dotnet run --project api/QuranDashboard.Api --launch-profile https
```

No repository file changes; environment only.

## T201 — Read-only performance pass over Slice C's modal work

**Method.** Chrome DevTools performance trace plus scripted measurement on
`https://localhost:4200/abwab` against the local backend and the real 13-door dataset,
1× CPU, no network throttling, light theme. Plus code reading of the six shells and their
page bindings. **No code was changed in this phase.** Note the dev build applies an
artificial `devApiLatencyMs: 450` to every API call (`environment.development.ts`), so
read latency below is deliberate and excluded from the open-path numbers — the modal
paints before its relations arrive.

**Measured baseline (relations modal, door «باب العلم بالله»):**

| Measurement | Value |
|---|---|
| Trace INP on the modal-open interaction (cold) | **162 ms** — input delay 3 ms, processing 2 ms, **presentation 157 ms** |
| Click handler's synchronous scripting | **0.6–0.8 ms** (3 opens) |
| Click → second rAF after paint (warm opens) | **34 / 54.5 / 59.5 ms** |
| DOM nodes added per open | 212 → 271–284 (**~60–72 nodes**) |
| Long tasks (>50 ms) during open or typing | **none observed** |
| Toolbar search, synchronous cost per keystroke | **5.1 / 5.7 / 14.2 / 7.7 / 5.3 ms** |

The split matters: the open path costs almost nothing in *script* (sub-millisecond) and
essentially all of its latency in **presentation** — style/layout/paint of the ~60-node
modal subtree that `@if (open())` inserts. Cold is ~3× warm, which is first-render cost,
not a per-open regression.

### Findings

**F1 — Every modal open fires two focus moves, ~5 ms apart. Severity: minor (a11y-shaped,
not perf-shaped).**
Instrumented `focusin` during one open of the relations modal:

```
t=28.9ms  focusin → abwab-relations-modal-type-similarity   (CDK cdkTrapFocusAutoCapture)
t=33.7ms  focusin → abwab-relations-modal-search            (queued focusSearch())
```

This is exactly the documented Slice C correction (`features/abwab/README.md`,
"Auto-capture is corrected where it lands wrong") working as designed, and its runtime
cost is negligible. What the measurement adds is the part the README does not state: the
correction is *observable* — the trap first lands on the «تشابه» type tab, so a screen
reader can announce that tab before the search box it is then moved to. Proposed fix (not
implemented): on the two modals that already correct focus themselves, drop
`cdkTrapFocusAutoCapture` and keep `cdkTrapFocus` + the explicit focus call, so there is
one focus event instead of two.

**F2 — Two page bindings mint a fresh array on every change-detection tick, marking two
OnPush modals dirty every tick while the snapshot is absent. Severity: minor.**
`abwab-page.component.html` binds the move picker and the relations modal with
`[liveRoots]="facade.snapshot()?.liveRoots ?? []"`. Once loaded this is the snapshot's own
stable array, so the input compares equal and nothing happens. While `snapshot()` is
`null` — the load window, and every error state until a retry succeeds — the `?? []`
allocates a **new** array per evaluation, so both OnPush children are marked dirty on
every tick of a zone-based CD app (`provideZoneChangeDetection({ eventCoalescing: true })`,
`app.config.ts`). Code-read finding, not a captured profile: the window is short and the
cost per tick is small, which is why it does not show up in the numbers above. Proposed
fix (not implemented): hoist to a page `computed` over one module-scope `EMPTY_ROOTS`
constant, so the reference is stable in every state.

**F3 — Each search keystroke costs two change-detection ticks, by design. Severity: info.**
Typing writes `q` into the URL, so every keystroke runs the input tick *and* the tick
caused by the router's `queryParamMap` emission (which writes six page signals). Measured
cost is 5–14 ms of synchronous work per keystroke with no long task, and the URL-as-source-
of-truth contract is a recorded invariant (`README.md`, "The URL is the single source of
truth for the selection"). Recorded so a future reader knows the double tick is understood,
not missed. No fix proposed.

**F4 — Every per-open derivation is already memoized. Severity: info (nothing to fix).**
Checked each shell the audit named: `abwab-door-picker.rows` / `pickedSet`,
`abwab-relations-modal.groups` / `nodesById` / `excludedIds` / `linkedIds` / `pickedNames`,
`abwab-move-picker.destinationRows`, and the overlays controller's `moveExcludedIds` /
`relationsBulkTargets` / `selectedDoor` are all `computed`. No template binding calls a
recomputing method; the only method calls in the templates are event handlers and pure
label getters. The "`pickerRows` walks / `nodesById` rebuilds recomputed per CD" hypothesis
in the plan is **not** what the code does.

**F5 — `qd-tabs`' roving-tabindex initialization inside the relations modal body is
negligible. Severity: info.** One `effect` over the three `contentChildren` tabs setting a
boolean each (`tabs.component.ts:65-71`); no DOM query, no layout read. Likewise
`qdModalScrollLock` reads no geometry — it toggles `body.style.overflow` behind a
reference count (`scroll-lock.service.ts`), so it cannot force a synchronous layout.

**F6 — All six shells are `OnPush` and all six guard on `@if (open())` as the first
template node. Severity: info (the plan's hypothesis, confirmed healthy).** Verified for
`abwab-door-modal`, `abwab-move-picker`, `abwab-relations-modal`, `abwab-sections-modal`,
`abwab-template-copy-modal`, `abwab-template-node-modal`. Closed cost is binding
evaluation plus a guard check — the static-sibling hosting pattern is not costing what the
plan suspected it might.

## T202 — Verdict

| # | Finding | Severity | Proposed fix (NOT implemented) |
|---|---|---|---|
| F1 | Two focus moves per modal open | minor | Drop `cdkTrapFocusAutoCapture` where the modal already focuses explicitly |
| F2 | `?? []` array literal dirties two OnPush modals per tick while the snapshot is null | minor | Hoist to a page `computed` over a module-scope `EMPTY_ROOTS` |
| F3 | Two CD ticks per search keystroke | info | None — the URL contract is deliberate |
| F4 | Per-open derivations all memoized | info | None |
| F5 | `qd-tabs` / scroll-lock init cost | info | None |
| F6 | Six shells `OnPush` + `@if (open())` | info | None |

**Zero blockers, zero majors. No fix is implemented in this slice** (plan §2 "Out":
performance *fixes* happen only if the user accepts a finding, as their own scoped
change). The slice proceeds.

**One finding is flagged against Phase 8**, per T202's stated exception: **F2**. Phase 8
adds page-side inputs to the relations modal for the reveal wiring, so any new binding
there must be a stable `computed` reference rather than an inline `?? []`/object literal,
or it will widen exactly this path. Recorded here so the Phase 8 work accounts for it
rather than repeating it.

## T303 (first half) — The bulk-archive 404, reproduced on the pre-fix SHA

Driven through the real UI at `https://localhost:4200/abwab` against the local backend,
on branch `ux-slice-d-tree` at **`c2254f01`** — before any Phase 3 code change. Sandbox
created over the API so no existing door was touched: section `383` «سلايس-د-اختبار»,
root door `679` «د-أب», its child `680` «د-ابن».

**Steps and observed state, in order:**

1. `?section=383`, expand «د-أب», enter «تحديد جماعي», tick both rows.
   → bulk bar reads **«2 باب محدد — د-أب، د-ابن»**.
2. «أرشفة الكل» → confirm strip reads **«سيتم أرشفة بابين»** (the union count is right).
3. Confirm → `POST /api/abwab/doors/bulk-archive` → **200**, snapshot refetched.
   → the tree is now **empty** (`abwab-tree-row-*`: none), and the bulk bar **still**
   reads «2 باب محدد — د-أب، د-ابن». The archived ids survived `rebindTo`.
4. «أرشفة الكل» again → the confirm strip now reads **«سيتم أرشفة لا أبواب»** — the exact
   disagreement the plan predicted (`bulkLiveSubtreeCount` walks live-only and counts 0,
   while `currentBulkRefs()` is about to send 2).
5. Confirm → `POST /api/abwab/doors/bulk-archive` → **404**.

**The failing request, verbatim:**

```
POST https://localhost:5015/api/abwab/doors/bulk-archive   404
request:  {"doors":[{"doorId":679,"version":9281},{"doorId":680,"version":9281}]}
response: {"isSuccess":false,"message":"الباب غير موجود","data":null,"errors":[]}
announcer: «الباب غير موجود»
```

Two things this pins beyond the plan's reading:

- The versions sent are **`9281` — freshly rebound from the post-archive snapshot**, not
  stale tokens. That is the proof that `rebindTo` re-bound the archived nodes instead of
  dropping them: `byId` still contained them, so the `if (node)` test passed.
- The failure is generic and names no door, exactly as `ApiMessages.cs` states — so the
  message the user is left with cannot identify «د-أب» or «د-ابن». The frontend can,
  because both names are in its own snapshot.

After the 404 the bulk bar **still** holds both ids, so the state is self-perpetuating:
every further submit re-sends the same two dead doors.
