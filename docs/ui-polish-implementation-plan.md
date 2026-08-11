# UI Polish Implementation Plan

**Status: PLAN ONLY.** No production code, styles, configuration or tests have been changed by
this document. Nothing has been committed. This is a normal standalone implementation plan — it is
not a Spec Kit artifact and does not open a Spec Kit feature.

---

## 1. Objective

Fix the confirmed UI defects in the two audits and converge the shared primitives they exposed, so
that across the affected surfaces:

- details content never disappears when the user switches tabs;
- details navigation uses its container with equal-width items and never scrolls horizontally;
- Quran text is never clipped or compressed by row geometry;
- loading states hold their geometry instead of jumping;
- the Mushaf reader stops wasting a third of a wide viewport without disturbing Quran layout;
- the two retired Access Management areas are removed cleanly from the frontend;
- Abwab's top area is a toolbar row plus a responsive sections grid;
- navbar dropdowns open inside the usable viewport.

The initiative is executed in small bounded phases. Each phase is verified in a real browser by the
implementing agent before the next phase starts.

---

## 2. Source Audits

Primary source of truth, in this order:

1. `docs/ui-polish-audit-mushaf-reader.md` — findings **M-1**, **M-2**, **M-3**, and its
   *Locked Decisions After Audit* section.
2. `docs/ui-polish-audit-remaining-pages.md` — findings **X-1 … X-7**, **U-1**, **R-1 … R-4**,
   **L-1**, **A-1**, **B-1**, **B-2**, **N-1**, **N-2**, the negative finding on Words details
   loading, the shared-primitive candidates **C-1 / C-2 / C-3**, and the Access removal enumeration.

Where this plan and an audit "Recommended direction" disagree, **this plan's Locked Decisions
win** — the audits were written before those decisions were made, and both audits already record
which of their own recommendations were superseded.

Repository governance that applies throughout: `CLAUDE.md` (root router),
`Frontend/quran-dashboard-ui/CLAUDE.md`, `FRONTEND_UI_RULES.md`, `CODING_PRINCIPLES.md` §2 (comment
policy — comments are forbidden by default), `TESTING_CONSTITUTION.md`, and the nearest-README duty
(a README whose described truth changes must be updated in the same change).

---

## 3. Locked Decisions

These are settled. **Do not reopen, re-litigate, or "improve" them mid-implementation.** If a phase
appears to require violating one, stop and report instead.

| # | Decision |
| --- | --- |
| L1 | **Remove the artificial dev API latency completely.** `devApiLatencyMs = 450` and the associated intentional development response delay are removed outright — not reduced, not made configurable in this initiative. |
| L2 | **Remove both unconditional 700 ms switch debounces completely.** `AYAH_STUDY_SWITCH_DELAY_MS` and `WORD_ANALYSIS_SWITCH_DELAY_MS` are deleted — not shortened, not made conditional. Correctness under keyboard word-stepping must be preserved by cancellation/latest-wins semantics that already exist, not by a replacement timer. |
| L3 | **Similar Ayahs and Mutashabihat stay lazy.** No preloading, no eager fetch from known counts. They load only when their tab is opened. Existing `MushafReaderCache` behavior is retained. |
| L4 | **Do not touch OIDC / auth bootstrap.** `withAppInitializerAuthCheck()`, `provideAuth`, guards, and the authorization architecture are out of scope entirely. |
| L5 | **One tabs primitive.** Improve `qd-tabs`; do not build a second tabs component. Add an explicit, *opt-in* responsive equal-width layout contract; do not force unrelated consumers into it. Locked target: large screens — equal width, fill the container, **approximately five items per row as the preferred density at normal large-desktop widths**, fewer than five distribute cleanly across the full width, more than that wrap naturally to another row; tablet/mobile — fewer columns, readable labels, natural wrapping. **Five is a density preference, not a hard cap:** on exceptionally wide screens more than five items in one row is acceptable provided the row stays balanced and readable. Do not add machinery to enforce an artificial five-column maximum. This must not weaken equal-width distribution, the no-horizontal-scroll rule, responsive wrapping, or label readability. Strict: **no internal horizontal scrollbar, no `overflow-x: auto` as the normal details-navigation behavior, no cramped content-sized tabs.** Preserve active-state language, keyboard accessibility, ARIA, and RTL. |
| L6 | **Converge on one shared details panel shell** for the genuinely duplicated Words details structures. The shell owns header structure, tabs placement, the stable content container, inline/modal/frameless behavior, ARIA/id wiring, and shared loading-geometry behavior. Labels, tab keys, disabled rules, content, empty/error wording and entity-specific views stay page-specific. **Do not** absorb the page-level domain `@switch` into a domain-aware shared component. |
| L7 | **Mushaf count badges: never show stale counts.** During loading, reserve the count slot geometry and keep tab width stable; the count *content* may be hidden/unknown; the arriving count appears in the same reserved geometry. No horizontal tab shift. |
| L8 | **Quran text always gets natural readable vertical space.** Never clip or compress it to satisfy fixed card height, touch-target minimums, fixed flex geometry, or viewport row assumptions. Fix the shared ayah-card / result-list geometry at the real owner; do not patch each explorer. |
| L9 | **Tab switching must never produce an unexplained blank content area.** The permanent blank-panel projection defect is fixed. Loading states are intentional and geometrically stable. **No new caching and no preloading** — the audit found the Words data layer already correct. |
| L10 | **Access Management:** remove سجل الوصول and الأمان المتقدم from the frontend; keep مساحة العمل. Remove frontend UI/components/routes/navigation/state/data wiring that becomes genuinely unused. With only Workspace left, remove the meaningless one-tab tabstrip if safe, and make stale `?tab=audit` / `?tab=security` URLs degrade safely to Workspace rather than error. **Do not delete or weaken backend security, authorization, access audit, permission enforcement, or owner/reconciliation safety infrastructure.** Backend endpoints that lose a frontend caller are **not** part of this plan. |
| L11 | **Abwab top area:** Row 1 = search + Tree/Cards controls; Row 2+ = sections only, as a responsive grid with consistent width and height, using available space, wrapping naturally, no horizontal internal scroll, no nested scrolling for sections, responsive column count. **Do not change Abwab business behavior.** |
| L12 | **Navbar dropdown placement only.** Edge-adjacent menus must open inward into the usable viewport, reusing the existing shared floating-layer collision logic. **Preserve current hover, click, dismissal and focus behavior** — this is a placement/collision correction, not an interaction redesign. |
| L13 | **Lemmas local defect:** delete `.lemma-details-panel__tab { inline-size: 100% }` as part of the appropriate tabs phase. Do not replace it with another Lemmas-only workaround. |
| L14 | **Dead/inert CSS** is removed only when the owning fix is implemented, and only where the audit proves irrelevance. This is not a licence for a broad stylesheet cleanup. |

---

## 4. Global Invariants

Every phase must hold all of these. A phase that cannot is a phase that stops and reports.

**Test freeze (project-wide).** This project is under an automated-test freeze.

- **Do not create** any automated test (Vitest / Jasmine / Jest / Playwright / backend).
- **Do not modify** any existing automated test, including "updating a spec to match the new
  implementation".
- **Do not delete or expand** the frozen automated-test estate.
- The Playwright specs under `Frontend/quran-dashboard-ui/e2e/` are frozen artifacts. Running them
  is permitted; editing them is not, and they are **not** a required gate for any phase.
- **Consequence, and it is load-bearing:** if a planned change would break a frozen spec, the
  correct response is to **change the implementation so the spec still passes, or stop and report
  to the user** — never to edit the spec. The specific contracts the frozen specs pin are listed
  below and must survive.

**Frozen-spec contracts that must not break** (verified against `e2e/*.e2e.ts` while writing this
plan):

| Contract | Pinned by | Phases at risk |
| --- | --- | --- |
| `data-testid` values `ayah-tab-similar-ayahs`, `ayah-tab-mutashabihat`, `selected-ayah-section`, `tafsir-card`, `translation-card`, `full-i3rab-card`, `similar-ayahs-list/-empty`, `mutashabihat-groups-list/-empty` | `mushaf-ayah-study.e2e.ts` | 9, 13, 16 |
| `mushaf-page-view`, `mushaf-page-area`, `mushaf-reader-page`, `mushaf-next-page`, `mushaf-prev-page`, page/surah/juz glyph testids | `mushaf-reader.e2e.ts` | 16 |
| `root-details-panel-entity`, `lemma-details-panel-entity`, `stem-details-panel-entity`, `word-type-details-panel-entity`, `word-drilldown-entity` — the `qd-sr-only` identity spans inside the panel headers | `words-explorers.e2e.ts` | 3, 8, 12, 15 |
| Navbar: `nav-words-trigger` opens its menu **on hover alone**; the menu element id `#words-menu`; `nav-more-trigger` opens **on click**; `nav-menu-link--mutashabihat`; `nav-link--mushaf` | `shell-nav.e2e.ts` | 17 |
| Abwab: `abwab-page`, `abwab-page-templates`, `abwab-page-add-root`, `abwab-door-modal` | `abwab-permissions.e2e.ts` | 10 |

No frozen spec references the Access Management page, so the Access phases carry no e2e risk.

**Other invariants:**

- `main` is protected Railway production. Work on a branch; never edit or commit to `main`.
- **No commits, pushes, PRs, formal reviews or deploys** unless the user asks for them — including
  at the end of this plan.
- **Comments are forbidden by default** in production source (`CODING_PRINCIPLES.md` §2). Do not
  narrate these fixes in code comments; the explanation belongs in the nearest README when it
  changes described truth.
- **RTL is the product default.** Every layout change uses logical properties.
  `npm run check:golden-ui` rejects physical `left`/`right`/`padding-left`/`margin-right` insets and
  colour literals outside `styles/_tokens.scss`; any change to the golden layer
  (`styles.scss`, `_tokens.scss`, `_breakpoints.scss`, `_layout.scss`, `_typography.scss`,
  `_components.scss`, `_forms.scss`, `_utilities.scss`, `shared/layout/breakpoints.ts`,
  `tailwind.config.js`) must keep that guard green.
- **Band vocabulary only.** Use the existing breakpoint bands (Compact ≤767, Medium 768–1079,
  Wide ≥1080, Wide-plus ≥1440) via `styles/_breakpoints.scss`; never introduce a raw threshold.
- **Nearest-README duty.** When a phase changes what a README describes
  (`features/words/README.md`, `features/mushaf/README.md`, `features/access-admin/README.md`,
  `features/abwab/README.md`), update that README **in the same phase**.
- **Scope discipline.** A phase touches only the files it lists. Unrelated cleanup found along the
  way is reported, not performed.
- **Never invent or silently correct Quran data.** No change in this initiative alters Quran text,
  ordering, markers, or source provenance.

---

## 5. Scope

**In scope (frontend only):**

- Findings **X-1, X-2, X-3, X-4, X-5, X-6, X-7**
- **U-1, R-1, R-2, R-3, R-4, L-1, B-1, B-2, N-1, N-2**
- **A-1** frontend removal surface (sections A and B of the audit's removal enumeration)
- **M-1, M-2**, and the parts of **M-3** covered by locked decisions L1–L3
- Shared primitive candidates **C-1** (correct `qd-tabs`), **C-2** (details shell),
  **C-3** (loading geometry reservation utility)

**Routes touched:** `/dashboard/mushaf`, `/dashboard/words/roots`, `/dashboard/words/lemmas`,
`/dashboard/words/stems`, `/dashboard/words/types`, `/dashboard/words/unique/:mode`, `/abwab`,
`/settings/access`, plus the navbar on every route.

---

## 6. Out of Scope

- **Every automated test.** No creation, no modification, no deletion. (§4)
- **OIDC / auth bootstrap / authorization architecture** (L4).
- **Any backend change**, including the four endpoints that lose their frontend caller
  (`GET /api/access/audit-events`, the two `logto-sub/relink` endpoints,
  `GET /api/access/owner-reconciliation/status`). They remain *"possible unused API surface —
  separate review required"* and are handled by a separate backend review with its own
  authorization-safety analysis.
- **Backend security, authorization, access-audit, security-logging, permission-enforcement and
  owner/reconciliation infrastructure** — untouched regardless of frontend removals (L10).
- **Preloading or new caching** anywhere (L3, L9).
- **Change-detection strategy migrations** (the `OnPush` promotions floated in M-3 recommendation 4)
  — not required by any locked decision, and they would broaden every phase they touched.
- **The page-level `@switch` domain content blocks** as an extraction target (L6).
- **Abwab business behavior** — selection, counts, search scoping, archive mode, permissions (L11).
- **`--qd-page-gutter`** and any other genuinely shared token not named by a phase.
- Broad stylesheet cleanup beyond the audit-proven dead rules (L14).
- Generated API model files under the `access-admin` generated re-exports — imports are removed,
  generated files are not.

---

## 7. Dependency Graph

```
P1  dev latency removal ────────────────► (makes every later browser observation honest)
P2  debounce removal      independent

P3  X-1 projection fix ──► P8 (tabs consumer migration touches the same 4 templates)
                       └─► P12 (R-4 header) ──► P15 (X-5/C-2 shell extraction)
P4  X-7 fallbacks        independent (page templates, not panel templates)

P5  X-6 cascade ownership ──► P6  X-3 ayah card shrink        (one geometry problem)

P7  qd-tabs `tracks` mode + Roots canary
      ├─► P8   remaining Words details tabs, L-1, X-4 (subtabs)
      ├─► P9   Mushaf study strip + count slot (M-1, L7, B-2)
      ├─► P10  Abwab sections grid + X-4 (toolbar)
      └─► P11  retire `--scrollable` + scrollIntoView   (only after 8, 9, 10)

P9  ──► P13 C-3 reservation utility extraction ──► P14 R-2 Words content reservation
P9  ──► P16 M-2 Mushaf width reclaim (re-verify the tab strip at the new width)

P17 navbar placement        independent
P18 ─► P19 ─► P20  Access removal (audit → security → tabstrip/URL)   independent
```

**Serialization rules that matter:**

- **P3 and P7/P8 must not run in parallel** — both edit the four details-panel templates.
- **P7 is the only phase that changes the tabs primitive.** P8/P9/P10 are consumer migrations and
  must follow it, one at a time.
- **P11 is a deletion gated on P8+P9+P10** — `--scrollable` may only be removed once no consumer
  resolves to it.
- **P15 must be last among the Words phases** — extracting the shell before P3/P7/P12 land would
  bake the current defects into the new shell.
- **P17 and P18–P20 share no files with anything else** and may be executed at any point.

---

## 8. Implementation Phases

> **Execution model — read this before interpreting any "Stop condition" below.**
>
> - **One implementer agent executes exactly ONE phase.** It stays strictly inside that phase, never
>   implements a later phase, never commits or pushes, and never touches automated tests.
> - **The implementer stops at its phase boundary** and reports: changed files, implementation
>   summary, commands/gates run, Chrome/browser verification performed, acceptance result, and
>   blockers (if any).
> - **The orchestrator does NOT stop between phases.** No user approval is required between normal
>   phases. When a phase is accepted, the orchestrator immediately delegates the next phase to a
>   fresh implementer agent and continues unattended until the plan is complete or a genuine unsafe
>   blocker prevents further progress.
>
> Every "**Stop.**" in this section is an instruction to the *implementer of that phase*, not a
> checkpoint requiring user approval.

---

### Phase 0 — Baseline capture and green-gate confirmation

**Goal**

Establish that the repository is green and record the current rendered geometry of the surfaces
this initiative will change, so every later phase has something to compare against.

**Findings addressed**

None (preparation).

**Files / components expected to change**

**None.** This phase makes no production edit. Any notes are scratch-only and are not committed.

**Implementation scope**

- Create a working branch off `dev` (never `main`).
- Run the static gates and record the result: `npm run check:golden-ui`,
  `npm run check:permission-catalogue`, `npm run check:audit-action-types`,
  `npm run check:no-unit-specs`, `npm run typecheck`.
- With the running stack, capture "before" screenshots plus a few numbers per surface, at
  1440 px: Roots details tabs + content, Lemmas details tabs, Unique Words drilldown tabs,
  Roots الآيات card heights, Abwab toolbar, Mushaf study tab strip and reader column widths.
- Record whether an authenticated **owner** session is reachable. Access Management
  (`/settings/access`) and the navbar **الإعدادات** trigger require one; the audit could not reach
  it. If it is not reachable, note it now — Phases 17–20 depend on the answer.

**Explicitly out of scope**

Any code change whatsoever. Any test run beyond the static gates listed.

**Acceptance criteria**

- All five static gates pass on an unmodified tree.
- Before-state evidence exists for each surface listed above.
- The owner-session question is answered yes or no, in writing.

**Verification**

- The gate commands above.
- Browser: open `/dashboard/words/roots`, `/dashboard/words/lemmas`,
  `/dashboard/words/unique/:mode`, `/abwab`, `/dashboard/mushaf`, and `/settings/access`.

**Stop condition**

Complete when the gates are green and the baseline is recorded. **Stop.** If any gate is already
red on an unmodified tree, report that and stop — do not start Phase 1 on a red baseline.

---

### Phase 1 — Remove the artificial development API latency

**Goal**

Delete the 450 ms dev response delay so that every subsequent browser verification measures real
behavior.

**Findings addressed**

M-3 (dev-latency contributor); Locked Decision **L1**.

**Files / components expected to change**

- `src/environments/environment.development.ts` — `devApiLatencyMs: 450`
- `src/environments/environment.ts` — the corresponding `devApiLatencyMs: 0` field
- `src/app/core/data-access/dev-api-latency.ts` — `withDevApiLatency`
- `src/app/core/data-access/dev-latency.interceptor.ts`
- `src/app/app.config.ts` — the `withInterceptors([...])` registration
- Any environment *type* declaration that names the field
- Nearest README describing the interceptor, if one does

**Implementation scope**

Remove the interceptor, its helper, its registration and the environment field outright. Leave
`secureUrlInterceptor` and `authInterceptor()` untouched and in their current order.

**Explicitly out of scope**

Any other provider in `app.config.ts` — specifically `provideAuth` / `withAppInitializerAuthCheck()`
(L4) and `provideZoneChangeDetection`. Any other environment field.

**Acceptance criteria**

- No reference to `devApiLatencyMs`, `withDevApiLatency` or `devLatencyInterceptor` remains
  anywhere in `src/`.
- The app builds and runs; API calls still carry auth headers and still resolve against
  `https://localhost:5015`.
- Mushaf responses land at wire speed — single-digit to low-tens of milliseconds locally.

**Verification**

- `npm run typecheck`
- Browser, `/dashboard/mushaf`: open the reader, then select a different ayah. In DevTools →
  Network confirm the study/analysis requests complete in tens of milliseconds, not ~500 ms.
  Confirm content renders and no request fails.

**Stop condition**

Complete when the grep for the three identifiers is empty, typecheck passes, and the reader loads
data correctly in the browser. **Implementer stops here and reports; the orchestrator proceeds to
Phase 2.**

---

### Phase 2 — Remove the unconditional 700 ms Mushaf switch debounces

**Goal**

Delete both switch debounces so an ayah/word selection issues its request immediately, without
introducing request storms or stale panel state.

**Findings addressed**

M-3 (debounce contributor); Locked Decision **L2**.

**Files / components expected to change**

- `src/app/features/mushaf/state/mushaf-ayah-study-load.runner.ts` —
  `AYAH_STUDY_SWITCH_DELAY_MS`, the `timer` field, the `setTimeout` in `schedule()`, the
  `clearTimeout` in `clearPending()`
- `src/app/features/mushaf/state/mushaf-word-analysis-load.runner.ts` — the same three, plus
  `WORD_ANALYSIS_SWITCH_DELAY_MS`
- `src/app/features/mushaf/README.md` — if it documents the delay constants
- Any barrel/import that re-exports the two constants

**Implementation scope**

- In each runner, `schedule()` becomes: `clearPending()` → `applyCached()` early return → bump the
  request token → `runLoad(...)` directly.
- **Preserve, unchanged, the existing correctness machinery** — this is the substitute for the
  timer, and it already exists:
  - `clearPending()` unsubscribes the in-flight subscription and bumps the request token, so a
    superseded request cannot write;
  - both `onSuccess` and `onSettled` re-check `getRequestToken() !== requestToken` and bail;
  - `applyCached()` still short-circuits a cached target with no request at all;
  - `MushafReaderCache.getOrLoad` still de-duplicates in-flight requests.
- Confirm the F1 stranded-load contract still holds:
  `state/mushaf-url-hydration.ts` keys recovery off `ayahStudyIsLoading` / `wordAnalysisIsLoading`
  at rebind time, and `runLoad()` still sets `isLoading: true` before subscribing — so the signal
  the contract reads is still produced. Do not change `mushaf-url-hydration.ts`.
- Keyboard word-stepping (`MushafReaderPageComponent.onDocumentKeydown → facade.moveSelectedWord`)
  now issues one request per committed step. That is the accepted consequence of L2. **Do not add
  any replacement timer, `debounceTime`, `auditTime`, `throttle`, or scheduler.** If measured
  behavior is unacceptable, stop and report rather than reintroducing a delay.

**Explicitly out of scope**

`ExplorerKeyboardNavScheduler` and `EXPLORER_KEYBOARD_NAV_DEBOUNCE_MS` in `features/words/` — a
different subsystem, addressed (only as a fallback-rendering concern) in Phase 4. Similar
Ayahs / Mutashabihat loading (L3). Anything in `mushaf-reader.facade.ts` beyond what the runner
signature forces.

**Acceptance criteria**

- Neither delay constant exists anywhere in `src/`.
- Clicking a word in a different ayah issues the study and analysis requests immediately
  (no ~700 ms gap between click and request).
- Holding **ArrowRight** across a line of words and releasing: the panel finally shows **the last
  selected word**, never an earlier one; no request writes into the panel after a newer one has
  settled; no console error.
- Re-selecting a previously visited ayah/word still resolves from cache with no new request and no
  skeleton flash.
- After a full page reload while a load was in flight, the reader still recovers the selection
  (F1 stranded-load path).

**Verification**

- `npm run typecheck`
- Browser, `/dashboard/mushaf`, DevTools Network open:
  1. Click a word in another ayah → confirm request start is effectively immediate.
  2. Hold ArrowRight across ~8 words, release, wait → confirm the settled panel matches the final
     word and that superseded requests are cancelled rather than applied.
  3. Click back to the first ayah → confirm cache hit (no new request, no skeleton).
  4. Reload the page on a deep link (`?ayah=…&word=…&panel=word`) → confirm both panels populate.

**Stop condition**

Complete when both constants are gone, the four browser checks pass, and the panel never displays a
stale selection. **Stop.**

---

### Phase 3 — Fix the projected-content lifecycle in the four Words details panels

**Goal**

Make details content survive a backward tab move: switching to an earlier tab must never leave a
permanently empty content area.

**Findings addressed**

**X-1** (HIGH); the blank half of **R-2**; Locked Decision **L9**.

**Files / components expected to change**

- `features/words/components/root-details-panel/root-details-panel.component.html`
- `features/words/components/lemma-details-panel/lemma-details-panel.component.html`
- `features/words/components/stem-details-panel/stem-details-panel.component.html`
- `features/words/components/word-type-details-panel/word-type-details-panel.component.html`
- the matching `.component.ts` files, only if the panel id/ARIA computation must move
- `features/words/README.md` — the `.qd-explorer-subview-panel` / panel-id contract at ~`:173`, if
  the id set changes

**Implementation scope**

Adopt audit option 1: **project once into a single stable container.**

- Replace the `<ng-template #projectedContent><ng-content /></ng-template>` + five
  `*ngTemplateOutlet` sections with **one** always-mounted content container that holds
  `<ng-content />` directly.
- Move the tabpanel identity (`role="tabpanel"`, `id`, `aria-labelledby`, `tabindex`) onto that one
  container and have it track the active tab, so `aria-controls` on the selected tab still resolves.
- Keep the page-level `@switch` content untouched — it already renders only the active view.
- Apply the same edit to all four panels. They are byte-identical today and must stay in step;
  Phase 15 extracts the shell once the shape is proven correct.

**Explicitly out of scope**

The tab strip itself (Phase 7/8). The header (Phase 12). Any loading-geometry reservation
(Phase 14). Extracting the shared shell (Phase 15). `word-drilldown-modal` — it renders views
inline and is immune by construction; do not "align" it.

**Acceptance criteria**

- In Roots, moving through **every** tab forward and backward always shows content — no empty
  content area in any direction, at any point.
- The same holds in Lemmas, Stems and Word Types.
- The same holds in all three render paths: desktop inline panel, sub-1080 modal, and the global
  entity detail overlay.
- The `qd-sr-only` identity spans `root-details-panel-entity`, `lemma-details-panel-entity`,
  `stem-details-panel-entity`, `word-type-details-panel-entity` still exist with the same testids
  (frozen-spec contract).
- The selected tab's `aria-controls` resolves to an element that exists; the panel carries
  `role="tabpanel"` and is labelled by its tab.
- No console errors or Angular warnings during tab switching.

**Verification**

- `npm run typecheck`
- `npm run build:verify` (first structural checkpoint of the initiative)
- Browser, `/dashboard/words/roots` at 1440 px, root selected:
  1. Click through `الكلمات → الآيات → السور → الصيغ → الأصول` and confirm content each time.
  2. Click **backward** through the same five in reverse and confirm content each time — this is
     the exact 6/6 failure the audit reproduced.
  3. Repeat the backward sweep on `/dashboard/words/lemmas` and `/dashboard/words/stems`.
  4. Narrow to 1024 px so the modal path renders; repeat a forward+backward sweep.
  5. Open the global detail overlay (from a Mushaf word link) and repeat a forward+backward sweep.
  6. DevTools console: zero errors across all of the above.
- DOM check while on a backward-selected tab: the tabpanel exists, is non-empty, and has non-zero
  height.

**Stop condition**

Complete when the backward sweep shows content in all four explorers across all three render paths
with a clean console. **Stop.**

---

### Phase 4 — Add exhaustive fallbacks to the details content `@switch` blocks

**Goal**

Ensure no combination of panel status and active view can render literally nothing.

**Findings addressed**

**X-7** (LOW); Locked Decision **L9**.

**Files / components expected to change**

- `features/words/pages/{roots,lemmas,stems,word-types}-explorer-page/*.component.html`
- `features/words/entity-detail-overlay/adapters/*-detail-overlay-adapter.component.html`
  (four adapters — `unique-detail-overlay-adapter` has no `panelState().status` switch; it
  delegates to `word-drilldown-modal`, which already carries terminal `@default` branches at both
  switch levels, so the in-scope set is **8 templates**, not 9)

**Implementation scope**

- Add a terminal `@default` to each `@switch (panelState().status)` block that renders the same
  skeleton the `loading` branch renders for the active view.
- Add a terminal `@else` to the `@case('success')` view chain with the same treatment.
- Nothing else. This phase adds fallbacks; it does not re-point the content at a different signal.

**Explicitly out of scope**

Re-pointing content from `activeView()` to `panelState().view` — the audit flags that as a product
decision about the deliberate keyboard-preview behavior, and it is **not** taken here.
`ExplorerKeyboardNavScheduler` and its 500 ms constant are **not** modified. The panel templates
(Phase 3) are not touched.

**Acceptance criteria**

- Every `@switch` in the eight in-scope templates has a terminal branch; every `@case('success')` chain has a
  terminal `@else`.
- Keyboard-navigating across the Roots table's count columns never leaves the details area empty —
  worst case it shows a skeleton.
- No visual change in any already-working state.

**Verification**

- `npm run typecheck`
- Browser, `/dashboard/words/roots`: focus a table row, arrow across the count columns quickly
  (this is the 500 ms preview window the audit derived), and confirm the details area always shows
  either content or a skeleton — never nothing.
- Spot-check one overlay adapter for the same behavior.

**Stop condition**

Complete when all eight in-scope templates have terminal branches and the keyboard sweep never blanks.
**Stop.**

---

### Phase 5 — Resolve the explorer detail-list cascade ownership

**Goal**

Remove the dead and out-cascaded rules in `styles/_explorer-detail-lists.scss` and put the ayah-list
height policy in exactly one owner, so Phase 6 can reason about real geometry.

**Findings addressed**

**X-6** (MEDIUM); Locked Decision **L14**.

**Files / components expected to change**

- `src/styles/_explorer-detail-lists.scss` — delete `:345-410` (dead `.explorer-detail-panel__body`
  block); resolve `:61-88` (the `.ayah-matches-list__viewport` override that loses the cascade)
- `src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.scss` — the
  component that currently wins
- `src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.{ts,html}` — only
  if a variant input is the chosen ownership mechanism

**Implementation scope**

- Delete the `.explorer-detail-panel__body` block outright — the selector matches zero templates
  (re-confirm with a grep before deleting).
- For `:61-88`, **do not raise specificity.** Move the height decision into the component that
  renders the list: `ayah-matches-list` owns its own block-size policy, exposed as a variant/input
  the panel sets, rather than a global stylesheet reaching in.
- Apply the policy consistently across all four panels (Roots, Lemmas, Stems, Word Types) — the
  audit notes Stems and Word Types were never listed in the original override, so they diverge for
  no designed reason.
- Verify effect against **computed style**, not source — this stylesheet has a proven cascade race.

**Explicitly out of scope**

`flex-shrink` on the ayah card (Phase 6 — deliberately separated so the height owner is settled
first). The `.root-details-panel--frameless .qd-details__header { display: none }` workarounds at
`:61-62` (Phase 12). Any other block in the file.

**Acceptance criteria**

- `grep -rn "explorer-detail-panel__body" src/` returns nothing.
- The ayah-list viewport's computed `block-size` and `overflow` are what the code says they are, in
  all four explorers, verified in DevTools computed styles.
- All four panels resolve to the same policy.
- `npm run check:golden-ui` passes.

**Verification**

- `npm run check:golden-ui`
- `npm run typecheck`
- Browser, الآيات view in Roots **and** Lemmas **and** Stems **and** Word Types: read the computed
  `block-size` / `overflow` on `.ayah-matches-list__viewport` and confirm it matches the single
  intended owner in each.

**Stop condition**

Complete when the dead block is gone, the cascade race no longer exists, and computed styles agree
with source in all four explorers. **Stop.**

---

### Phase 6 — Stop ayah cards from being compressed by their list

**Goal**

Guarantee that Quran text in an ayah card always gets its natural height, in every list that renders
one.

**Findings addressed**

**X-3** (HIGH); Locked Decision **L8**.

**Files / components expected to change**

- `src/app/shared/ui/ayah-card/ayah-card.component.scss` — the missing `flex-shrink`
- `src/styles/_components.scss:911-921` — `.qd-result-item`'s blanket
  `min-block-size: var(--qd-hit-target-min)` and `align-items: center`
- `src/app/features/words/components/ayah-matches-list/ayah-matches-list.component.html` — the
  element that carries both `qdAyahCard` and `qdResultItem`, if the overlap is resolved there

**Implementation scope**

- Give the ayah card `flex-shrink: 0` **on the shared card**, because "a Quran card sizes to its
  text" is a property of the card, not of any one list.
- Resolve the `qdAyahCard` + `qdResultItem` collision: today they overwrite each other's `display`,
  `align-items` and `padding` at equal specificity, so the winner depends on injection order. Decide
  which one owns geometry and remove the duplicated geometry declarations from the other. Keep the
  list semantics/ARIA that `qdResultItem` provides.
- Scope the 44 px touch-target floor to **interactive** rows so a content card does not inherit it.
  Do not remove the accessibility property from the rows that legitimately need it.
- Do **not** fix this by raising the viewport height — the content is variable-height by nature.

**Explicitly out of scope**

The list height policy (settled in Phase 5). Virtual scrolling (not used in details lists). Any
change to Quran text rendering, fonts, or the ayah link markup.

**Acceptance criteria**

- In Roots الآيات, no card's `scrollHeight` exceeds its `clientHeight`; the audit's failing cards
  (44 px box against 63–90 px content) now render at their content height.
- No ayah line is sliced through its glyphs; cards do not overlap.
- Short ayat: the ayah link is no longer cross-axis-centred at fit-content against a much wider card
  (the 130 px-in-604 px case).
- The same holds in Lemmas, Stems, Word Types, and `word-drilldown-modal`.
- Mushaf `similar-ayahs-card` / `mutashabihat-groups-card` are unchanged visually, **and** their
  loading placeholders still match their loaded cards' geometry (the audit's explicit re-check).
- The list still scrolls; it does not grow the page unboundedly.

**Verification**

- `npm run check:golden-ui`, `npm run typecheck`
- Browser, `/dashboard/words/roots` → الآيات: measure the first six cards
  (`getBoundingClientRect().height` vs `scrollHeight`) and confirm no clipping. Screenshot for
  comparison against the Phase 0 baseline.
- Browser, `/dashboard/words/lemmas`, `/stems`, `/types` → same view, visual check.
- Browser, `/dashboard/mushaf`: open **آيات قريبة** and **المتشابهات** on an ayah that has both.
  Watch the transition from placeholder to loaded and confirm the geometry does not jump — this is
  the specific desync risk the audit named.
- Responsive: repeat the Roots check at 1080 px and at 768 px.

**Stop condition**

Complete when no ayah card clips in any explorer and the Mushaf similarity placeholders still match
their loaded cards. **Stop.**

---

### Phase 7 — Add an equal-width wrapping layout contract to `qd-tabs` (primitive + one canary)

**Goal**

Give `qd-tabs` a **declared** layout that implements the locked tabs contract, and prove it on one
consumer.

**Findings addressed**

**X-2** (HIGH) — the primitive half; Locked Decision **L5**.

**Files / components expected to change**

- `src/app/shared/ui/tabs/tabs.component.ts` — extend `QdTabsLayout` with the new declared value
- `src/app/shared/ui/tabs/tabs.component.html` — the corresponding class binding
- `src/styles/_components.scss:298-350, 380-383` — the new mode's rules
- `src/app/shared/ui/tabs/tabs.component.scss` — only if the host needs it
- `features/words/components/root-details-panel/root-details-panel.component.html` — the canary
  (5 tabs — the densest details row in the product)
- `src/app/shared/ui/README.md` or the nearest README documenting the tabs primitive

**Implementation scope**

- Add one new **opt-in** layout value (e.g. `'tracks'`) to `QdTabsLayout`. Its rules:
  - `display: grid`, equal-width tracks via `repeat(auto-fit, minmax(<floor>, 1fr))`, so fewer
    items distribute across the full width, around five sit comfortably per row at normal
    large-desktop widths, and more wrap to another row. **Do not build an artificial hard cap at
    exactly five columns** — the floor plus `auto-fit` is the mechanism, and a wider screen fitting
    six balanced, readable columns is acceptable;
  - the column floor is sized so the **longest label stays readable** and drives when the row wraps;
  - **no `overflow-x`, no scrollbar, ever** in this mode;
  - labels are single-line — the audit proved that with `flex-basis: 0` and `white-space: normal` the
    intrinsic width collapses to the widest *word*, which is what wraps `لم يذكر فيها` onto two
    lines;
  - the column count reduces naturally at Medium and Compact via the existing band vocabulary — no
    raw thresholds.
- **Preserve unchanged:** the roving-focus keyboard model, `aria-*` wiring, `role="tablist"`, the
  RTL arrow-key direction handling, and the selected-state visual language
  (`.qd-tabs__tab.qd-is-selected`).
- **Do not** change the `inline` heuristic (`segmented` / `scrollable`) in this phase — unmigrated
  consumers must keep rendering exactly as they do today (L5: do not force unrelated consumers into
  the new layout).
- Migrate **one** consumer as the canary: `root-details-panel` (5 tabs).

**Explicitly out of scope**

Every other consumer (Phases 8, 9, 10). Deleting `--scrollable` or the `scrollIntoView` effect
(Phase 11 — it is still live for unmigrated consumers). Folding `--grid`'s two existing consumers
into the new mode. Any change to `QD_TABS_SEGMENTED_MAX` behavior for unmigrated consumers.

**Acceptance criteria**

- Roots details tabs: 5 equal-width tabs spanning the full tab slot (baseline was 297.6 px of a
  634 px slot, 47 % used); every tab the same width; no horizontal scrollbar;
  `scrollWidth === clientWidth`. At normal large-desktop widths all five sit on one row; a wider
  row that stays balanced and readable is acceptable and is not a failure.
- Labels render on a single line and are readable; no mid-label wrap.
- Keyboard: `ArrowRight`/`ArrowLeft` move selection in the correct RTL direction; `Home`/`End` work;
  focus is visible; disabled tabs are skipped.
- Selected-tab visual treatment is unchanged from the Phase 0 baseline screenshot.
- Unique Words, Lemmas, Stems, Word Types, Abwab, Access and the Mushaf study strip render exactly
  as before (unmigrated).
- `npm run check:golden-ui` passes.

**Verification**

- `npm run check:golden-ui`, `npm run typecheck`, `npm run build:verify`
- Browser, `/dashboard/words/roots`, at **1920 / 1440 / 1080 / 768 / 390 px**:
  - measure the tablist `clientWidth` vs `scrollWidth` (must be equal at every width);
  - measure each tab's width (must be equal within sub-pixel rounding at ≥1080);
  - confirm wrapping — not scrolling — is what happens when the row cannot fit;
  - confirm no tab label is truncated into unreadability at 390 px, and that labels wrap to more
    rows rather than compressing into tiny cells.
- Browser, one unmigrated consumer (`/dashboard/words/unique/:mode` drilldown): confirm it is
  visually identical to the baseline.

**Stop condition**

Complete when the Roots row meets the locked contract at all five widths and no unmigrated consumer
moved. **Stop.**

---

### Phase 8 — Migrate the remaining Words details tabs and delete the local workarounds

**Goal**

Bring Lemmas, Stems, Word Types and the Unique Words drilldown onto the new tabs contract, and
delete the local CSS that existed only because the contract was missing.

**Findings addressed**

**L-1** (HIGH), **U-1**, **R-1**, **R-3**, **X-4** (subtabs half); Locked Decisions **L5**, **L13**,
**L14**.

**Files / components expected to change**

- `features/words/components/lemma-details-panel/lemma-details-panel.component.html` + `.scss`
  (delete `inline-size: 100%` at `:33-41`, and the now-redundant
  `justify-content` / `overflow` / `text-overflow` / `white-space` block)
- `features/words/components/stem-details-panel/stem-details-panel.component.html` + `.scss`
  (the local `overflow/text-overflow/white-space` truncation block)
- `features/words/components/word-type-details-panel/word-type-details-panel.component.html` + `.scss`
- `features/words/components/word-drilldown-modal/word-drilldown-modal.component.html`
- `src/styles/_words-explorer-layout.scss:34-40` — delete `flex-wrap: wrap` from
  `.qd-explorer-subtabs` (proven inert), and migrate the six sub-tab rows
- the `.qd-explorer-subtabs` rows in the Roots/Lemmas/Stems pages and their three overlay adapters

**Implementation scope**

- Switch each consumer's `<qd-tabs>` to the declared layout from Phase 7.
- Delete `.lemma-details-panel__tab { inline-size: 100% }` — the single genuine page-specific defect
  in the report. **Do not** replace it with another Lemmas-only rule.
- Delete the inert `flex-wrap: wrap` on `.qd-explorer-subtabs`. Do **not** "fix" it with `::ng-deep`
  or a descendant selector into the primitive's internals.
- Leave per-consumer concerns alone: labels, tab keys, disabled predicates, count badge content,
  `aria-controls` targets.

**Explicitly out of scope**

`.abwab-toolbar__tabs` (Phase 10). The Mushaf study strip (Phase 9). Deleting `--scrollable`
(Phase 11). Any change to panel content or the header.

**Acceptance criteria**

- **Lemmas:** the horizontal scrollbar is gone. `scrollWidth === clientWidth`. All four tabs are
  visible simultaneously and equal width. (Baseline: `scrollWidth 2550` vs `clientWidth 634`, tabs
  2–4 at x = −429 / −1067 / −1706.)
- **Unique Words drilldown:** three equal tabs fill the 666 px slot (baseline 212.5 px, 32 %);
  `لم يذكر فيها` renders on one line and its tab is the same height as its neighbours (baseline
  60 px vs 40 px).
- **Stems / Word Types:** equal-width tabs filling their slot; no scrollbar.
- The six `.qd-explorer-subtabs` rows render correctly and wrap rather than scroll.
- No `overflow-x: auto` is reachable on any Words details tab strip at any width.
- All four `*-details-panel-entity` testids still resolve (frozen-spec contract).
- Phase 3's backward-tab-move fix still holds in every migrated panel.

**Verification**

- `npm run check:golden-ui`, `npm run typecheck`
- Browser at **1920 / 1440 / 1080 / 768 / 390 px** on `/dashboard/words/lemmas`,
  `/stems`, `/types`, `/unique/:mode`:
  - `clientWidth === scrollWidth` on every tablist;
  - equal tab widths at ≥1080; sensible wrapping below;
  - no clipped or two-line labels.
- Browser regression check, `/dashboard/words/lemmas`: run the Phase 3 forward+backward tab sweep
  again — the content must still never blank.
- Keyboard check on one migrated panel: arrows, Home/End, disabled-tab skipping.

**Stop condition**

Complete when no Words details tab strip scrolls horizontally at any of the five widths and the
Phase 3 behavior still holds. **Stop.**

---

### Phase 9 — Mushaf study tab strip: stable geometry and a reserved count slot

**Goal**

Make the five-tab study row's geometry fixed from first render through the loaded state, for any
count value, without ever showing the previous ayah's counts.

**Findings addressed**

**M-1** (Mushaf report), **B-2**; Locked Decisions **L5**, **L7**.

**Files / components expected to change**

- `features/mushaf/components/selected-ayah-section/selected-ayah-section.component.html:44-49` —
  the `@if (tabCount(tab.key) !== null)` badge
- `features/mushaf/components/selected-ayah-section/selected-ayah-section.component.ts` —
  `tabCount()`
- `src/styles/_components.scss:391-412` — `.qd-tabs__count`, `.qd-tabs__count--empty`
- `features/mushaf/README.md` — the study-tab geometry description

**Implementation scope**

- **Keep the count element mounted at all times** for the two count-bearing tabs and vary only its
  appearance while loading — the pattern `abwab-toolbar.component.html:14-15` already uses
  correctly. The badge renders in an "unknown" state during loading.
- **Do not display the outgoing ayah's counts** (L7). The slot is reserved; the value is hidden or
  rendered as unknown, never stale. This also preserves the feature's `null` = unknown / `0` = known
  empty semantics that the similarity placeholders depend on.
- **Give the count slot a stable inline size** independent of digit count — a `ch`-based floor sized
  for the largest realistic count (`font-variant-numeric: tabular-nums` is already set, so it is
  exact). This removes the residual 5.6 px per-badge shift the audit measured between `"5"` and
  `"13"`, and fixes **B-2** for the Abwab consumer at the same time.
- Migrate this `<qd-tabs>` to the Phase 7 declared layout, which makes tab width independent of
  content entirely.

**Explicitly out of scope**

Similar Ayahs / Mutashabihat loading behavior — **stays lazy** (L3). The N3 vertical reservation
(Phase 13). The reader column and page width (Phase 16). The study card prose cap (Phase 16).

**Acceptance criteria**

- Selecting a different ayah produces **zero** horizontal movement of any of the five tabs.
  (Baseline: +28.0 px and +56.0 px on the last two tabs.)
- The count badges never show the previous ayah's numbers.
- A count changing from one digit to two produces no width change.
- Tabs are equal width and fill the study tab slot; no horizontal scrollbar at any width.
- `ayah-tab-similar-ayahs` and `ayah-tab-mutashabihat` testids still resolve, and both tabs still
  open their content on click (frozen-spec contract).
- Opening **آيات قريبة** for the first time still triggers its request only then (L3).

**Verification**

- `npm run check:golden-ui`, `npm run typecheck`
- Browser, `/dashboard/mushaf` at 1440 px:
  1. Record `getBoundingClientRect()` for all five tabs on ayah `1:3`.
  2. Select a word in ayah `1:5`; re-record during loading and after settle.
  3. Confirm Δx = 0 for every tab in both samples.
  4. Confirm no badge shows `1:3`'s counts while `1:5` loads.
  5. Find an ayah with a two-digit similar-ayah count and confirm no width change against a
     one-digit ayah.
- Browser, `/abwab`: confirm the section count badges did not change the section strip geometry
  (shared `.qd-tabs__count` floor).
- Responsive: repeat step 1–3 at 1080 px and 768 px.

**Stop condition**

Complete when an ayah switch moves no tab horizontally and no stale count is ever visible. **Stop.**

---

### Phase 10 — Abwab: split the toolbar into a controls row and a sections grid

**Goal**

Restructure the Abwab top area into Row 1 (search + Tree/Cards) and Row 2+ (sections as a responsive
grid), with no horizontal or nested scrolling.

**Findings addressed**

**B-1** (MEDIUM), **X-4** (toolbar half); Locked Decision **L11**.

**Files / components expected to change**

- `features/abwab/components/abwab-toolbar/abwab-toolbar.component.html`
- `features/abwab/components/abwab-toolbar/abwab-toolbar.component.scss` — delete the inert
  `.abwab-toolbar__tabs { flex-wrap: wrap }`
- `features/abwab/pages/abwab-page/abwab-page.component.html:74-86` — the toolbar placement, if the
  rows are hoisted
- `features/abwab/README.md`

**Implementation scope**

- **Row 1:** a `.qd-toolbar` containing only the search field (`__filters`) and the Tree/Cards
  toggle (`__actions`). This is what `.qd-toolbar`'s single-row slot vocabulary is for.
- **Row 2+:** the sections in their own block below the toolbar, as a responsive grid with
  consistent item width and height, wrapping naturally, responsive column count, **no horizontal
  scroller and no nested scroller**. If the sections keep `role="tab"` semantics, consume the
  Phase 7 tabs layout rather than inventing an Abwab-only grid.
- Delete the inert `flex-wrap: wrap` on `.abwab-toolbar__tabs`.
- **Carry `hideSectionControls()` correctly.** It currently hides *both* the section strip and the
  view toggle from one condition. Splitting the rows means applying it in two places — getting this
  wrong leaves an empty row in archive mode, which is the phase's main hazard.

**Explicitly out of scope**

**Any Abwab business behavior** (L11): section selection, count computation, search scoping, archive
mode semantics, permissions, the door/root modals. The templates page. The `abwab-move-picker` and
`abwab-relations-modal` tabs.

**Acceptance criteria**

- Row 1 holds search and the view toggle only; Row 2+ holds sections only.
- Section items are consistently sized (same width, same height) and fill the available width.
- Adding sections wraps to a new row; the page grows taller. No horizontal scrollbar anywhere in the
  sections area; no scroll container nested inside it.
- Column count reduces at Medium and Compact; labels remain readable.
- Archive mode still hides exactly what it hid before — no empty row, no orphaned control.
- Section selection, counts, search and the Tree/Cards toggle behave exactly as before.
- `abwab-page`, `abwab-page-templates`, `abwab-page-add-root`, `abwab-door-modal` testids still
  resolve (frozen-spec contract).

**Verification**

- `npm run check:golden-ui`, `npm run typecheck`
- Browser, `/abwab` at **1920 / 1440 / 1080 / 768 / 390 px**:
  - confirm the two-row structure and the grid's column count at each width;
  - confirm `scrollWidth === clientWidth` on the sections container at every width;
  - select a section and confirm the door list updates as before;
  - type in search and confirm scoping is unchanged;
  - toggle Tree/Cards and confirm both views render.
- Browser: enter archive mode and confirm the section controls hide cleanly with no empty row.
- If more than five sections exist (or can be created in the local environment), confirm wrapping
  rather than scrolling; if not, record that the >5 case was verified only by simulated width
  reduction.

**Stop condition**

Complete when the two-row layout holds at all five widths with no horizontal or nested scrolling and
archive mode is correct. **Stop.**

---

### Phase 11 — Retire the scrolling tabs mode (opportunistic, non-blocking cleanup)

**Goal**

Remove `--scrollable` and its `scrollIntoView` effect **if** no legitimate consumer still needs
them; otherwise leave the mode in place, document who still needs it, and move on.

**This phase can never block the initiative.** It has two acceptable outcomes — *removed* or
*retained with documented consumers* — and both are a pass.

**Findings addressed**

**X-2** (cleanup half), **X-4**; Locked Decisions **L5**, **L14**.

**Files / components expected to change**

- `src/app/shared/ui/tabs/tabs.component.ts` — the `scrollable` computed, `QD_TABS_SEGMENTED_MAX` if
  it becomes unused, and the `effect(() => selected?.scrollIntoView())`
- `src/app/shared/ui/tabs/tabs.component.html` — the `qd-tabs--scrollable` class binding
- `src/styles/_components.scss:313-318` — the `.qd-tabs--scrollable` rules
- the nearest README documenting the tabs primitive

**Implementation scope**

1. **Inventory first.** Enumerate every `<qd-tabs>` consumer and determine which can resolve to
   `--scrollable` (any `layout="inline"` consumer whose tab count can exceed
   `QD_TABS_SEGMENTED_MAX`, including dynamically — Abwab sections are variable-length).
2. **If zero legitimate consumers remain:** delete the mode, its class binding, its CSS, and the
   now-pointless `scrollIntoView` effect. Verify and continue.
3. **If one or more legitimate consumers outside this initiative's migrated UI scope still require
   it:** **do not force-migrate them and do not broaden scope.** Leave `--scrollable` in place,
   record which consumers still require it and why, verify that every targeted surface in this
   initiative no longer uses horizontal tab scrolling, and continue to the next phase.
- Leave `--segmented` and `--vertical` in place for their current consumers in either outcome.

**Explicitly out of scope**

`--segmented`, `--vertical`, `--grid` and their consumers. Migrating any additional consumer —
that work finished in Phases 8–10, and no consumer outside the initiative's scope is migrated here
merely to enable a deletion.

**Acceptance criteria**

Outcome-dependent — either of these is a pass:

- **Removed:** no `overflow-x` remains in any `.qd-tabs*` rule; the `scrollIntoView` effect is gone;
  every remaining consumer renders unchanged.
- **Retained:** the consumers that still require `--scrollable` are documented (file + reason), and
  nothing was migrated to avoid that outcome.

In **both** outcomes:

- No tab strip on a surface targeted by this initiative (Words details, Mushaf study, Abwab
  sections) exposes a horizontal scrollbar at any of the five verification widths.
- `npm run build:verify` passes.

**Verification**

- `npm run check:golden-ui`, `npm run typecheck`, `npm run build:verify`
- Browser sweep at 1440 px and 390 px across `/dashboard/words/roots`, `/lemmas`, `/stems`,
  `/types`, `/unique/:mode`, `/abwab`, `/dashboard/mushaf`: for each tablist assert
  `scrollWidth === clientWidth`.

**Stop condition**

Complete when the inventory is done and **either** outcome above is reached, and the sweep shows no
scrolling tablist on any targeted surface. **Implementer stops and reports which outcome applied;
the orchestrator proceeds regardless of which one it was.**

---

### Phase 12 — Give the details workspace a real "no header" condition

**Goal**

Stop rendering an empty padded, bordered header in the frameless path, and delete the two
per-consumer `display: none` workarounds.

**Findings addressed**

**R-4** (LOW); Locked Decision **L14**.

**Files / components expected to change**

- `src/app/shared/ui/details-workspace/details-workspace.component.html:11-23` — the unconditional
  `<header class="qd-details__header">`
- `src/app/shared/ui/details-workspace/details-workspace.component.ts` — the absence condition
- `src/styles/_explorer-detail-lists.scss:61-62` — delete the two
  `.…--frameless .qd-details__header { display: none }` workarounds
- the four Words details panels, to pass the new condition
- `src/styles/_components.scss:967-974` — only if the header rules need adjusting

**Implementation scope**

- Add an explicit input-driven absence condition (e.g. a `hideHeader` / `headerless` input, or a
  computed over the `identity()` / metadata / actions inputs the consumers already pass).
  **Do not** try to detect whether projected content is empty — `<ng-content>` presence cannot be
  measured reliably, and the identity spans the frozen specs depend on live inside those slots.
- Set the condition from the frameless consumers only. All four frameless panels behave the same
  way afterwards — today only Roots and Lemmas hide the strip, Stems / Word Types / word-drilldown
  keep it.
- Delete the two `display: none` workarounds.
- **Do not remove the header where it carries identity, panel label, or the close button** — that is
  the inline and modal path, where it does real work.

**Explicitly out of scope**

The double border question (`.qd-modal-shell` wrapping `.qd-details__shell`) — the audit raises it
as a separate decision and it is **not** taken here. `.qd-modal-shell__header--bare`, which already
collapses correctly. The tab strip and content container.

**Acceptance criteria**

- In the frameless/overlay path, all four Words panels plus the word-drilldown frameless path show
  **no** empty header box and no orphan divider.
- In the inline and modal paths, the header still shows identity, metadata and close, and
  `*-details-panel-entity` testids still resolve (frozen-spec contract).
- `aria-labelledby` on `.qd-details__shell` still resolves or is correctly `null` — it is already
  guarded by `@if (identity())`.
- `grep` shows the two `display: none` workarounds are gone.

**Verification**

- `npm run typecheck`, `npm run check:golden-ui`
- Browser: open the global entity detail overlay for **each** of Roots, Lemmas, Stems, Word Types
  and the word drilldown; confirm no empty header strip in any of them and that they are now
  consistent with each other.
- Browser: open the inline panel and the sub-1080 modal for Roots; confirm the header is intact with
  its identity and close button.
- Screen-reader-facing check: confirm the identity span is still in the DOM on the inline path.

**Stop condition**

Complete when all five frameless surfaces are consistent and the inline/modal headers are intact.
**Stop.**

---

### Phase 13 — Extract the shared loading-geometry reservation utility

**Goal**

Turn the two hand-ported Mushaf reservation implementations into one reviewed shared utility,
without changing their behavior.

**Findings addressed**

**C-3**; the Mushaf README's decision **N3-a** (extract on a third consumer); Locked Decision **L9**.

**Files / components expected to change**

- a new shared utility under `src/app/shared/` (placement per the nearest README's conventions)
- `features/mushaf/components/selected-word-section/*` (Feature 029 U1 implementation)
- `features/mushaf/components/selected-ayah-section/*` (Feature 030 N3 row 10 implementation)
- `features/mushaf/README.md` — record that N3-a's third-consumer threshold was reached and where
  the utility now lives

**Implementation scope**

- Extract the existing contract as-is: hold the last known natural block size of a content region
  while it is loading, release on settle, and **invalidate on an inline-size change** via the
  guarded `ResizeObserver` both implementations already use. Numeric geometry only.
- Migrate the two Mushaf consumers onto it. **This phase changes no observable behavior** — it is a
  refactor whose success criterion is that nothing moves.
- Carry forward, and state in the README, the accepted trade-off the Mushaf README already records:
  the reservation holds the previous entity's height while a different entity loads.

**Explicitly out of scope**

The third consumer (Phase 14). Any change to the reservation's semantics, its per-band baseline
floors, or the similarity placeholders. Change-detection strategy.

**Acceptance criteria**

- Both Mushaf sections behave exactly as before: no vertical jump when ayah study or word analysis
  loads; the reservation releases on settle; a viewport width change clears a stale reservation.
- No duplicated reservation logic remains in the two components.
- `selected-ayah-section` testid and the study card testids still resolve (frozen-spec contract).

**Verification**

- `npm run typecheck`, `npm run build:verify`
- Browser, `/dashboard/mushaf`: switch ayah and switch word, watching the study and word sections —
  no vertical collapse or jump during loading.
- Browser: with a load in flight, change the window width and confirm the reservation does not
  strand the panel at a stale height.
- Compare against the Phase 0 baseline screenshots — this phase should be visually invisible.

**Stop condition**

Complete when both Mushaf sections use the shared utility and behave identically to before.
**Stop.**

---

### Phase 14 — Reserve the Words details content geometry across loading

**Goal**

Stop the Words details content box from collapsing and re-expanding on every tab switch.

**Findings addressed**

**R-2** (MEDIUM, the non-blank half); Locked Decision **L9**.

**Files / components expected to change**

- the four Words details panels' content container (the single container introduced in Phase 3)
- `src/styles/_words-explorer-layout.scss:42-47` — `.qd-explorer-subview-panel`, if the reservation
  host is styled there
- `features/words/README.md`

**Implementation scope**

- Apply the Phase 13 utility to the Words details content area as its **third consumer** — exactly
  the threshold decision N3-a named.
- Hold the last known natural block size while `panelState().status === 'loading'`; release on
  settle; invalidate on inline-size change.
- **Add no caching and no preloading** (L9) — the audit established that the Words data layer is
  correct, that cached views resolve synchronously, and that the jump happens on cache hits too.

**Explicitly out of scope**

Any change to `AbstractDetailController`, `DetailRequestLifecycle`, the caches, or request
orchestration. The `loadSummaryAndRestore` serialization the audit noted — recorded as a future
observation, **not** acted on here.

**Acceptance criteria**

- Switching Roots tabs no longer snaps the content box through wildly different heights (baseline:
  570 → 334 → 1422 px in one interaction).
- The skeleton still appears immediately on a forward move — the reservation must not delay it.
- A cached tab still resolves with no skeleton at all.
- No panel is stranded at a stale height after a load settles or after a width change.
- The same holds in Lemmas, Stems and Word Types.

**Verification**

- `npm run typecheck`
- Browser, `/dashboard/words/roots` at 1440 px: record `.qd-explorer-subview-panel` height across a
  forward tab move at t+0 and after settle; confirm the intermediate collapse is gone.
- Browser: repeat a full forward+backward sweep (Phase 3 regression) and confirm content is always
  present **and** the geometry is stable.
- Browser: resize mid-load and confirm no stale reservation.
- Spot-check Lemmas and Stems.

**Stop condition**

Complete when tab switching is geometrically stable in all four explorers with no stranded heights.
**Stop.**

---

### Phase 15 — Extract the shared Words details panel shell

**Goal**

Replace the four byte-identical details-panel templates with one shared shell, now that the correct
shape is proven.

**Findings addressed**

**X-5** (MEDIUM), **R-3**, **C-2**; Locked Decision **L6**.

**Files / components expected to change**

- a new shared details-panel shell component (placement per `features/words/README.md` conventions,
  or `shared/ui/` if it is genuinely entity-agnostic)
- `features/words/components/{root,lemma,stem,word-type}-details-panel/*` (12 files) — reduced to
  thin per-entity compositions
- `features/words/README.md` — the panel-id and `.qd-explorer-subview-panel` contract at ~`:173`

**Implementation scope**

- The shell owns: the three render paths (frameless / inline / modal), the `qd-details-workspace`
  composition, the tab loop, the single stable content container from Phase 3, the per-instance
  ARIA id generation, the header absence condition from Phase 12, the loading-geometry reservation
  from Phase 14, and the close/escape wiring.
- Each entity keeps: its view key list, labels and aria strings, its disabled predicate, its
  empty/not-found copy, and its content template.
- **Per-instance ARIA ids are load-bearing** — the overlay and the side panel can be on screen
  simultaneously. `instanceId` generation in
  `shared/ui/details-workspace/details-workspace.component.ts` must be preserved exactly.
- **Do not** absorb the page-level `@switch` domain content into the shell (L6) — it carries
  entity-specific view keys and list components, and `FRONTEND_UI_RULES.md` §1 prohibits domain
  names in a shared component.

**Explicitly out of scope**

`word-drilldown-modal` — it correctly diverges by rendering views inline and not projecting;
folding it in is a separate decision. The overlay adapters' content templates. Any behavior change
whatsoever — this phase is a consolidation whose success criterion is that nothing moves.

**Acceptance criteria**

- All four explorers behave exactly as they did at the end of Phase 14: tabs, content, backward
  moves, geometry, header, ARIA.
- `root-details-panel-entity`, `lemma-details-panel-entity`, `stem-details-panel-entity`,
  `word-type-details-panel-entity` still resolve (frozen-spec contract).
- Opening the overlay while the side panel is mounted produces **no duplicate DOM ids**.
- The documented panel-id contract in `features/words/README.md` still holds, or the README is
  updated in this same phase to describe what now holds.
- Net template duplication across the four panels is materially reduced.

**Verification**

- `npm run typecheck`, `npm run build:verify`, `npm run check:golden-ui`
- Browser, each of `/dashboard/words/roots`, `/lemmas`, `/stems`, `/types`: forward+backward tab
  sweep, inline path.
- Browser: sub-1080 modal path and the global overlay path for at least Roots and Lemmas.
- DOM check: with the overlay open over a mounted side panel, assert every `id` in the two subtrees
  is unique.
- Console clean throughout.

**Stop condition**

Complete when all four explorers are behaviorally identical to Phase 14 through one shared shell and
ids remain unique. **Stop.**

---

### Phase 16 — Reclaim Mushaf wide-screen width without disturbing Quran layout

**Goal**

Use more of a wide viewport on the Mushaf route while leaving the Quran reading column's geometry
byte-for-pixel unchanged.

**Findings addressed**

**M-2** (Mushaf report).

**Files / components expected to change**

- `src/styles/_tokens.scss:211` — `--qd-page-measure-protected-mushaf: 90rem`
- `src/styles/_tokens.scss:218-219` — `--qd-split-mushaf`, `--qd-split-gap`
- `src/styles/_layout.scss:63-65, 185-187` — `.qd-page-shell--protected-mushaf`,
  `.qd-page-split--mushaf`
- `features/mushaf/components/_study-card.shared.scss` — `.study-card__body` prose cap
- `features/mushaf/README.md` — the recorded measured geometry

**Implementation scope**

Apply the audit's ordered steps — **the order is load-bearing**:

1. **Make the reader track content-sized rather than percentage-sized at Wide-plus.** Derive it
   from `--qd-mushaf-text-column-width` plus the page-view padding instead of `40%`, so the Quran
   measure stays exactly what it is today and every reclaimed pixel flows to the study side.
2. **Then** let the shell use more viewport above 1440 px — raise
   `--qd-page-measure-protected-mushaf` or make it a width-responsive `clamp()`. **Step 2 without
   step 1 only converts outer whitespace into reader-column whitespace at a 40 % rate.**
3. **Cap the study prose independently** — apply `--qd-measure-prose` (or equivalent) to
   `.study-card__body`, so reclaimed width goes to card and list surfaces rather than to ~100 ch
   tafsir lines (measured today at ~72 ch against a 68 ch token).
4. **Leave Compact and Medium alone**, including `.mushaf-reader__page`'s deliberate negative-margin
   cancellation of the Compact gutter, which exists to stop a Madani line from wrapping.

**This phase must explicitly protect:** Quran line wrapping, Quran font rendering, ayah/juz/surah
markers, word rects, and the reserved page-area height baseline. `features/mushaf/README.md` warns
that this baseline invalidates silently when the column-width token or font metrics move.

**Explicitly out of scope**

`--qd-page-gutter` — genuinely shared, not touched for this. `--qd-mushaf-text-column-width` itself
(28rem stays). The other three page intents (`capped-reading`, `full-data`, `split-workspace`). Any
Mushaf component logic.

**Acceptance criteria**

- At 1920 px, the dead outer margin is materially reduced from the measured 465 px, and the share of
  the viewport carrying no content falls well below the baseline 33.6 %.
- **The Quran page still renders exactly 15 non-wrapping lines**; no line wraps at any verified
  width.
- The Quran text column width, font size, line height, markers and word rects are unchanged at
  1080 px and 1440 px against the Phase 0 baseline.
- Tafsir/translation body lines are capped at the prose measure, not ~100 ch.
- Compact (390 px) and Medium (768 px) are pixel-identical to baseline.
- The Phase 9 study tab strip still shows zero horizontal movement at the new width.
- `npm run check:golden-ui` passes (golden-layer files are edited here).

**Verification**

- `npm run check:golden-ui`, `npm run typecheck`, `npm run build:verify`
- Browser, `/dashboard/mushaf`, at **390 / 768 / 1080 / 1440 / 1920 px** — re-measure and compare
  against Phase 0:
  - `.qd-page-shell.mushaf-reader` width and x;
  - resolved `grid-template-columns`;
  - `.mushaf-page-view__text-column` width;
  - **line count on the rendered page, and per-line bounding boxes** — this is the protected
    invariant;
  - `.study-card__body` width in `ch`.
- Browser: page-forward and page-back through several Mushaf pages at 1920 px and confirm no line
  wraps and markers stay in place.
- Browser: re-run the Phase 9 tab-geometry check at 1920 px.

**Stop condition**

Complete when the reclaimed width is measurable at 1920 px **and** the Quran column geometry is
unchanged at every verified width. If any Quran line wraps or any measured reader constant moves,
revert the width change and report. **Stop.**

---

### Phase 17 — Correct navbar dropdown placement

**Goal**

Make edge-adjacent navbar menus open inward into the usable viewport, by reusing the existing shared
floating-layer collision logic — without changing any interaction behavior.

**Findings addressed**

**N-1** (MEDIUM), **N-2** (LOW); Locked Decision **L12**.

**Files / components expected to change**

- `src/app/core/layout/app-navigation/app-navigation.component.html:41-51` — the
  `<ul class="qd-nav__menu">`
- `src/app/core/layout/app-navigation/app-navigation.component.ts` — the placement wiring
- `src/styles/_components.scss:1276-1295` — remove the hand-written `position: absolute` /
  `inset-inline-start: 0` / `max-block-size` trio
- `src/styles/_components.scss:1243-1246` — `.qd-nav__item { position: relative }`, if it becomes
  redundant
- consumers of the existing `shared/ui/floating-layer/floating-layer-placement.ts` (read-only
  reference — do not modify the primitive)

**Implementation scope**

- Move `.qd-nav__menu` onto the existing `qdFloatingLayer` placement, which already implements
  RTL-aware preferred alignment, an 8 px viewport margin, inline clamping, block-axis flipping and a
  measured `maxBlockSize`. It is already used by `mushaf/source-selector`,
  `mushaf/surah-jump-picker`, `words/explorer-association-filter` and `shared/ui/context-menu`.
- **Preserve the navbar's existing interaction contract exactly** (L12, and the frozen specs pin it):
  - `nav-words-trigger` opens its menu **on hover-intent alone**, with no click
    (`onMenuPointerEnter` / `onMenuPointerLeave` in `top-navbar.component.ts`);
  - a trigger **click** also opens (`nav-more-trigger`);
  - the menu element keeps its id (`#words-menu` and siblings) — `shell-nav.e2e.ts` locates the
    menu by that id;
  - dismissal and focus behavior are unchanged.
- If the floating-layer directive brings its own focus/keyboard/dismissal semantics that would
  change any of the above, **adapt the integration, not the navbar's behavior** — and if that proves
  impossible, stop and report rather than shipping an interaction change.
- Watch the `position: fixed` interaction with the sticky navbar's `z-index`
  (`--qd-z-mobile-nav`).

**Explicitly out of scope**

The mobile nav drawer. `shared/ui/floating-layer/*` itself — it is correct and is consumed, not
modified. Nav item structure, labels, routes, or the owner gating on the settings trigger. Any auth
change (L4).

**Acceptance criteria**

- The **الإعدادات** menu (actions cluster, RTL — the left edge of the viewport) opens fully inside
  the viewport with no clipping, at 1920 / 1440 / 1080 px. Baseline arithmetic put it 92–220 px
  outside.
- `الكلمات والجذور`, `الأبواب` and `المزيد` menus still open in place and stay inside the viewport.
- Hover-intent opening still works on `nav-words-trigger` **without a click**; the menu id is
  unchanged; a link inside it is clickable.
- Click opening still works on `nav-more-trigger`, and `nav-menu-link--mutashabihat` still
  navigates.
- Dismissal (pointer-leave / outside click / Escape) behaves exactly as before.
- Keyboard focus order through the navbar is unchanged.
- No `z-index` regression: menus paint above page content and below nothing that should cover them.

**Verification**

- `npm run check:golden-ui`, `npm run typecheck`
- Browser at **1920 / 1440 / 1080 px**, RTL: open each of the four menus and measure the menu's
  bounding rect against the viewport — `left >= 0` and `right <= clientWidth` in every case.
- Browser: hover `nav-words-trigger` **without clicking**, confirm the menu opens and a link inside
  it is clickable; then click `nav-more-trigger` and confirm click-open still works.
- Browser: verify dismissal by pointer-leave, by outside click, and by Escape.
- Compact (390 px): confirm the mobile navigation path is unaffected.
- **الإعدادات 
  requires an owner session.** If Phase 0 established none is reachable, do **not** weaken auth, do
  **not** modify DB roles, do **not** forge tokens, and do **not** bypass guards. Verify the three
  reachable menus, verify the settings menu's placement by temporarily inspecting an equivalently
  positioned actions-cluster menu if one exists, and **record the الإعدادات check as an outstanding
  human acceptance item** in the final verification section.

**Stop condition**

Complete when all reachable menus stay inside the viewport at three widths and hover/click/dismiss
behavior is provably unchanged. **Stop.**

---

### Phase 18 — Remove the Access Management audit tab (سجل الوصول)

**Goal**

Delete the audit log surface from the frontend, including its eager load.

**Findings addressed**

**A-1** section A (audit); Locked Decision **L10**.

**Files / components expected to change**

Per the audit's enumeration:

- `features/access-admin/components/access-audit-log/access-audit-log.component.{ts,html,scss}`
- `features/access-admin/state/access-audit.store.ts`
- `features/access-admin/data-access/access-admin.api.ts:69-73` — `listAuditEvents` and its
  `auditParams` helper
- `features/access-admin/state/access-admin.facade.ts` — the `audit` field (`:53`), the
  `auditEvents`/`auditNextCursor`/`auditQuery`/`auditLoading`/`auditError`/`auditAppending`/
  `auditAppendError`/`auditAppendedCount` readonlys (`:98-105`), `updateAuditQuery` (`:383`),
  `loadNextAuditPage` (`:388`), `loadAuditEvents` (`:396`), and the `this.loadAuditEvents()` leg of
  `Promise.all` (`:124`)
- `features/access-admin/pages/access-admin-page/access-admin-page.component.html` — the whole
  `@case ('audit')` block
- `features/access-admin/pages/access-admin-page/access-admin-page.component.ts` —
  `auditTargetSearch()`, `auditActorSearch()`, `applyAuditFilters`, `loadNextAuditPage`,
  `searchAuditTarget`, `searchAuditActor`
- `features/access-admin/models/access-admin.labels.ts` — `auditAppendedAnnouncement` (`:19`),
  `auditActionType` (`:47`), `AUDIT_ACTION_TYPE_LABELS`, and the `tab === 'audit'` branch of
  `labels.tab` (`:25`)
- `features/access-admin/models/access-admin.models.ts` — audit-only types
  (`AccessAuditQuery`, `AccessAuditEventPage`)
- `features/access-admin/README.md`

**Implementation scope**

Delete the surfaces above. Two hazards to handle explicitly:

1. **`ACCESS_AUDIT_ACTION_TYPES` must NOT be deleted.**
   `access-admin.models.ts:6` declares it, `:19` derives `AccessAuditActionType` from it, and `:22`
   is its type guard. `npm run check:audit-action-types` reads **this exact file** and fails if the
   declaration is missing or drifts from
   `Backend/domain/QuranDashboard.Domain/Access/AccessAuditActionType.cs`. It is the frontend mirror
   of a backend enum, not audit-UI code. Keep it and its derived type; if it becomes unreferenced by
   components, that is expected and correct.
2. **`AccessAdminApi.findUsers` / the audit-backed user search** (`facade:188` delegating to
   `AccessAuditStore.findUsers`) — **verify before deleting the store.** If the Workspace context
   search uses this path, the search must keep working; extract or retain what Workspace needs.

**Explicitly out of scope**

The security tab (Phase 19). The tabstrip and `?tab=` contract (Phase 20). **Any backend change** —
`GET /api/access/audit-events` is recorded as *possible unused API surface — separate review
required* and is not touched (L10). Generated API model files. `access-admin-unsaved-changes.guard.ts`
and `access-permission-draft.store.ts` (Workspace-owned — keep).

**Acceptance criteria**

- The Access page renders Workspace and Advanced Security; the audit tab and its content are gone.
- `GET /api/access/audit-events` is no longer requested on page load.
- Workspace user search still works (or is proven never to have used the audit path).
- `npm run check:audit-action-types` **passes**.
- `npm run typecheck` passes with no unused-symbol or unresolved-import errors.
- `features/access-admin/README.md` no longer describes the removed surface.

**Verification**

- `npm run check:audit-action-types`, `npm run check:permission-catalogue`, `npm run typecheck`
- Browser, `/settings/access` **with an owner session**: confirm the audit tab is gone, the page
  loads, Workspace functions (list users, open a user, search), and DevTools Network shows no
  `audit-events` request.
- If no owner session is reachable (per Phase 0), rely on typecheck + build + the static guards,
  perform the deletion, and **record the browser confirmation as an outstanding human acceptance
  item**. Do not weaken auth, modify DB roles, forge tokens, or bypass guards to reach the page.

**Stop condition**

Complete when the audit surface is gone, `check:audit-action-types` is green, and Workspace is
unaffected. **Stop.**

---

### Phase 19 — Remove the Access Management advanced security tab (الأمان المتقدم)

**Goal**

Delete the advanced security / owner reconciliation surface from the frontend, including its eager
load.

**Findings addressed**

**A-1** section A (security); Locked Decision **L10**.

**Files / components expected to change**

- `features/access-admin/components/access-advanced-security/*`
- `features/access-admin/components/access-owner-reconciliation/*`
- `features/access-admin/data-access/access-admin.api.ts` — `previewRelink` (`:75`),
  `confirmRelink` (`:85`), `getOwnerReconciliationStatus` (`:95`)
- `features/access-admin/state/access-admin.facade.ts` — `reconciliationState`/
  `reconciliationLoadingState`/`reconciliationErrorState` (`:62-64`), `relinkPreviewState`/
  `relinkEvidenceTokenState` (`:65-66`), `relinkPreviewRequestVersion` (`:71`), the public
  `reconciliationStatus`/`reconciliationLoading`/`reconciliationError`/`relinkPreview` readonlys
  (`:106-109`), `previewSelectedUserRelink` (`:309`), `confirmSelectedUserRelink` (`:355`),
  `cancelSelectedUserRelink` (`:376`), `loadReconciliationStatus` (`:404`),
  `invalidateRelinkPreviewRequest`, `isCurrentRelinkPreviewRequest`, and the
  `this.loadReconciliationStatus()` leg of `Promise.all` (`:125`)
- `access-admin-page.component.html` — the whole `@case ('security')` block
- `access-admin-page.component.ts` — `workflowResetToken()`, `previewRelink`, `confirmRelink`,
  `cancelRelink`
- `features/access-admin/models/access-admin.labels.ts` — `reconciliationCandidateState` (`:49`),
  `RECONCILIATION_CANDIDATE_STATE_LABELS`, the `tab === 'security'` branch of `labels.tab` (`:28`)
- `features/access-admin/models/access-admin.models.ts` — `AccessRelinkPreviewRequest`,
  `AccessRelinkConfirmRequest`, and the *imports* of the generated re-exports
- `features/access-admin/README.md`

**Implementation scope**

- Delete the surfaces above.
- **`busyAction` is the hazard.** It is a union that includes `'relink-preview'` and
  `'relink-confirm'`, consumed by Workspace components (`access-permission-editor`,
  `access-lifecycle-actions`, `access-change-review`) through `[busyAction]`. Narrowing the union
  must not break their template type-checking — verify each consumer compiles and still shows the
  correct busy state.
- **The `Promise.all` in `load()` drops from four legs to two.** Re-verify the `accessStateKnown()`
  gate: the page's readiness signal now settles on a different set of responses.
- **Do not delete generated model files** — remove the imports of `LogtoSubjectRelinkPreview`,
  `OwnerReconciliationStatus`, `PreviewLogtoSubjectRelinkBody`, `ConfirmLogtoSubjectRelinkBody`
  only.

**Explicitly out of scope**

The tabstrip and `?tab=` contract (Phase 20). **Any backend change** — the two relink endpoints and
the owner-reconciliation status endpoint are *possible unused API surface — separate review
required*, and no backend authorization, security, audit, permission-enforcement or owner/
reconciliation safety infrastructure is touched (L10).

**Acceptance criteria**

- The Access page renders Workspace only; the security tab and its content are gone.
- Neither the relink endpoints nor the owner-reconciliation status endpoint is requested on page
  load.
- Workspace's busy states still render correctly for every remaining `busyAction` value.
- `accessStateKnown()` still gates the Workspace correctly — no flash of an ungated state, no
  permanent spinner.
- `npm run typecheck` and `npm run build:verify` pass.

**Verification**

- `npm run typecheck`, `npm run build:verify`, `npm run check:permission-catalogue`
- Browser, `/settings/access` with an owner session: page loads to Workspace; DevTools Network shows
  only the users and permission-catalogue requests; trigger a permission edit and a lifecycle action
  and confirm the busy state renders.
- If no owner session is reachable, proceed as in Phase 18 and record the browser confirmation as an
  outstanding human acceptance item.

**Stop condition**

Complete when only Workspace remains functional, the two eager loads are gone, and Workspace busy
states are intact. **Stop.**

---

### Phase 20 — Collapse the one-tab Access tabstrip and make stale tab URLs degrade safely

**Goal**

Remove the now-meaningless single-tab navigation while keeping `?tab=audit` / `?tab=security` links
harmless.

**Findings addressed**

**A-1** section B; Locked Decision **L10**.

**Files / components expected to change**

- `features/access-admin/pages/access-admin-page/access-admin-page.component.html` — the
  `<qd-tabs>` block and the `@switch` around the tab cases
- `features/access-admin/pages/access-admin-page/access-admin-page.component.ts` — `activeTab`,
  `selectTab` (`:202`), `showTab` (`:213`), the `route.queryParamMap` subscription (`:134-136`)
- `features/access-admin/models/access-admin-tabs.ts` — `ACCESS_ADMIN_TAB_KEYS`, `AccessAdminTab`,
  `parseAccessAdminTab`, `DEFAULT_ACCESS_ADMIN_TAB`
- `features/access-admin/models/access-admin.labels.ts` — the residual `labels.tab` function
- `features/access-admin/README.md`

**Implementation scope**

- Remove the one-tab tablist and the `@switch` wrapper; render the Workspace directly.
- **Keep `?tab=` degradation safe.** `parseAccessAdminTab`'s `?? DEFAULT_ACCESS_ADMIN_TAB` already
  makes any unknown value fall back to Workspace, so **retaining the parser is the safer option**
  even with the strip removed. Whether the parser stays or goes, the acceptance criterion is
  identical: `/settings/access?tab=audit` and `?tab=security` must render Workspace with **no error,
  no blank region, and no console error**.
- Before deleting `access-admin-tabs.ts` entirely, confirm no remaining consumer (labels, page,
  guard) needs `AccessAdminTab`. If anything still does, keep the file and delete only the dead
  members.
- Confirm no design intent to add a fourth tab before removing the strip — if that is unknown, the
  strip removal is the one item in this phase that may be deferred; the URL degradation is not.

**Explicitly out of scope**

Anything outside `features/access-admin/`. The route path `/settings/access` itself and its guards.
The navbar entry that links to it (that is Phase 17's territory and its link target is unchanged).
Any backend change.

**Acceptance criteria**

- `/settings/access` renders the Workspace with no tab strip.
- `/settings/access?tab=audit` renders the Workspace: no error, no blank area, no console error.
- `/settings/access?tab=security` behaves the same.
- `/settings/access?tab=nonsense` behaves the same.
- Browser back/forward across those URLs does not produce a broken state.
- `npm run typecheck`, `npm run build:verify`, and all four static guards pass.
- `features/access-admin/README.md` describes the page as Workspace-only.

**Verification**

- `npm run typecheck`, `npm run build:verify`, `npm run check:golden-ui`,
  `npm run check:permission-catalogue`, `npm run check:audit-action-types`,
  `npm run check:no-unit-specs`
- Browser with an owner session: visit all four URLs above in turn, then use browser back/forward.
- If no owner session is reachable, complete the code change, verify by static gates, and record the
  four URL checks as outstanding human acceptance items.

**Stop condition**

Complete when the page is Workspace-only and all three stale-URL forms degrade silently to it.
**Implementer stops and reports. This is the last implementation phase; the orchestrator proceeds
directly to §9 Final Verification and then to the mandatory final engineering review — no user
approval is required.**

---

## 9. Final Verification

One cumulative pass after all phases are accepted. **This is not a phase that fixes things** — if it
finds a regression, name the phase that owns it and fix it there.

### 9.1 Static and build gates

```
npm run check:golden-ui
npm run check:permission-catalogue
npm run check:audit-action-types
npm run check:no-unit-specs
npm run typecheck
npm run build:verify
```

(Equivalent to `npm run test:pre-pr`, which chains exactly these six.)

**No automated-test phase exists and none is added.** The frozen Playwright estate is neither
modified nor required as a gate.

### 9.2 Browser verification matrix

Widths: **390 (Compact) / 768 (Medium) / 1080 (Wide) / 1440 (Wide) / 1920 (Wide-plus)**.
Direction: **RTL** (product default); spot-check LTR only if a phase introduced a direction-sensitive
rule. Theme: light and dark on any surface whose colour or border changed (Phases 6, 7, 10, 12, 16,
17).

| Route | Checks |
| --- | --- |
| `/dashboard/words/roots` | forward+backward tab sweep never blanks; tabs equal width, no h-scroll; ayah cards not clipped; content height stable across switches |
| `/dashboard/words/lemmas` | **no horizontal scrollbar in the tab header** (the L-1 regression check); same sweep and card checks |
| `/dashboard/words/stems` | same sweep, tabs, cards |
| `/dashboard/words/types` | same sweep, tabs, cards |
| `/dashboard/words/unique/:mode` | drilldown tabs fill the container; `لم يذكر فيها` on one line; cards not clipped |
| Global detail overlay | no empty header strip; tabs and content correct; ids unique alongside the side panel |
| `/dashboard/mushaf` | study tab row never shifts horizontally; no stale counts; **Quran page still 15 non-wrapping lines**; markers intact; reclaimed width visible at 1920; similarity placeholders match loaded cards; **Similar Ayahs / Mutashabihat still load only on tab open** |
| `/abwab` | two-row structure; sections grid wraps, consistent size, no h-scroll, no nested scroll; archive mode clean; business behavior unchanged |
| `/settings/access` | Workspace only; `?tab=audit`, `?tab=security`, `?tab=nonsense` all degrade to Workspace with no error |
| Navbar (any route) | every dropdown opens inside the viewport, including الإعدادات; hover-open and click-open both work; dismissal and focus unchanged |

### 9.3 Explicit end-state assertions

- No horizontal internal scrolling on any details tab/navigation surface at any verified width.
- No blank details panel in any direction, in any of the three render paths.
- No Quran text clipped or compressed anywhere.
- Loading geometry stable in the Mushaf study/word sections and in the Words details content.
- Mushaf wide-screen space usage improved **with Quran column geometry unchanged**.
- Access Management shows Workspace only; stale tab URLs fail safe.
- Abwab sections use the approved two-row/grid structure.
- Navbar dropdowns stay inside the viewport.
- **No eager preloading of Similar Ayahs or Mutashabihat** — confirmed in DevTools Network by
  loading an ayah and observing that neither request fires until its tab is opened.
- **No artificial 450 ms latency** — confirmed by request durations in DevTools.
- **No unconditional 700 ms debounce** — confirmed by click-to-request timing.
- **No count badge is clipped into a different number** — on the widest realistic count available in
  the local data, confirm the `.qd-tabs__count` text is fully rendered inside its tab in `tracks`
  mode (see §9.5; the tab carries `overflow: hidden`).
- **No automated test was created, modified, or deleted** — confirmed by `git status` /
  `git diff --stat` showing no changes under any test path, and by `npm run check:no-unit-specs`.

### 9.4 Outstanding human acceptance items

Carry forward anything that could not be verified by the implementing agent, with the reason:

- Any Access Management or navbar **الإعدادات** check that required an owner session which was not
  safely reachable. **Auth must not be weakened, DB roles must not be modified, tokens must not be
  forged, and guards must not be bypassed** to close these out.
- Any responsive band that could not be exercised in the available browser environment.
- The **backend endpoint review** for the four endpoints that lost their frontend caller — out of
  frontend scope entirely, requiring its own authorization-safety analysis.

---

## 9.5 Deferred items (intentionally not fixed in this initiative)

Recorded here so Definition of Done item 1 is satisfied — each is a conscious deferral, not an
oversight.

| Item | Why deferred |
| --- | --- |
| The **last** `quran-result` ayah card computes `border-block-end: 0`, because `:where(.qd-result-list) .qd-result-item:last-child` (0,2,0) beats `qd-ayah-card`'s `:host` border (0,1,0). | **Pre-existing, not a regression** — verified against `3c846fc5`: the baseline had the same two specificities and the same effect, and the only change to `ayah-card.component.scss` in this initiative is `flex-shrink: 0`. It is not an L8 concern (a missing 1 px border is neither clipping nor compression of Quran text), and L14 forbids opportunistic stylesheet cleanup while a fix is in flight. Conceptually the last residue of the `qdAyahCard` + `qdResultItem` overlap behind X-3; affects `ayah-matches-list` only — the two Mushaf `quran-result` lists carry no `qdResultItem`. |
| `.qd-result-list--linked .qd-result-item, .qd-result-item--selectable` at `_components.scss:921-924` and the Compact-band twin at `:1193-1196` remain unwrapped at (0,2,0), where the baseline blanket rule was (0,1,0). | Harmless today — nothing in `_explorer-detail-lists.scss` declares `min-block-size` on a linked row — but it is the same specificity-raising shape that caused review finding ER-1, surviving only because it currently has no competitor. Wrapping both in `:where()` would make the block uniform. Advisory; no behavioural defect. |
| Inert residue in `_explorer-detail-lists.scss`: `.explorer-detail-modal .ayah-matches-list { gap: 0 }` and the two `__meta-row` / `__meta` gap rules lose to the component's own `gap`. | Not height policy, so outside Phase 5's proven-dead scope under L14. |
| `/api/access/me` takes ~1017–1029 ms on every page load and is the only request over 400 ms. | Sits on the OIDC/auth bootstrap path, which **L4** puts entirely out of scope. Survived the dev-latency removal at the same magnitude, so it is unrelated to Phase 1. |
| In `tracks` mode the `overflow: hidden` added for review finding ER-1 clips a `.qd-tabs__count` as well as a label. Clipping a label is cosmetic; clipping a **number** silently renders a *different* number — a 3-digit count cut to two digits reads as a smaller, wrong value. | Not currently reachable: the Mushaf strip measures 146.44 px against a 124 px floor and each extra digit adds ~7 px, so there is real slack. Recorded because it is the one place in this initiative where a layout guard could become a data-correctness issue, and it did not exist before Phase 7. **Carried as an explicit item in §9 Final Verification.** A surgical fix, if it ever becomes reachable, is to clip the *label* element rather than the whole tab, leaving the count unclipped. |
| `unique-words-tabs` (the Unique Words route-mode switcher) stays `--segmented`, with `بدون تشكيل` still rendering two-line at 63 px against its 40.5 px neighbour. | It is a page mode switcher, not details navigation, so **L5**'s "do not force unrelated consumers into this layout merely because the primitive exists" applies. It does not scroll (185/185 at every width). Assigned to the Phase 11 inventory to be recorded, not migrated. |

---

## 10. Definition of Done

This initiative is complete only when **all** of the following hold:

1. Every accepted audit finding is either fixed or **explicitly documented as intentionally
   deferred**, with the reason recorded.
2. Every locked decision (L1–L14) is preserved in the shipped result.
3. All affected UI has been **personally verified in a real browser by the implementing agent**, per
   phase, not deferred to the end.
4. No horizontal internal tab navigation remains in the targeted details surfaces.
5. Details content no longer disappears on any tab transition.
6. Quran text is never clipped, in any explorer or in the Mushaf.
7. Loading geometry is stable — no collapse-and-expand on tab switch, no horizontal tab shift on
   ayah switch.
8. Mushaf wide-screen space usage is improved **without damaging Quran layout** — line count, line
   geometry, fonts and markers verified unchanged.
9. The two Access frontend areas are removed cleanly, Workspace is intact, and stale tab URLs
   degrade safely.
10. Abwab sections use the approved two-row/grid structure with no horizontal or nested scrolling,
    and Abwab business behavior is unchanged.
11. Navbar dropdowns remain inside the viewport, with hover/click/dismissal/focus behavior unchanged.
12. All six static/typecheck/build gates are green.
13. **No automated tests were created, modified, or deleted**, and the frozen estate is untouched.
14. Every affected README describes the new truth.
15. Commits exist only as orchestrator-created checkpoints (roughly every 3–4 accepted phases, plus
    a final one), made on a working branch off `dev`. **Nothing was pushed, no PR was opened, and
    nothing was deployed.** Implementer and reviewer agents never commit.

---

Status: PLAN ONLY — NOT IMPLEMENTED, NOT COMMITTED
