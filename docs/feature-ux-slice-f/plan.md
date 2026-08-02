# Slice F — Sections (UX audit)

Source: `docs/abwab-ux-audit.md` "Slice F — Sections" (`:1109-1114`) — item 18
(`:621-677`) and item 19 (`:679-710`), whole and alone. The audit isolated this slice
because it is the **first backend slice** of the series: it adds a route, which forces a
`SmokeRouteCatalog` entry in the same change plus the route-smoke tier, and the evidence
must state whether `QuranDashboard.Tests.Smoke.Data` ran or skipped.

**Mode when this plan was written:** plan-only. No code, no Git, nothing amended.

**Slice E status at plan time:** merged. `ux-slice-e-overlays` merged into `dev` at
`7b0e8fba`; ancestry checked at plan time —
`git merge-base --is-ancestor ux-slice-e-overlays dev` exits 0, and
`state/abwab-modal-url.controller.ts` + `components/abwab-modal-restore/` are present in
the tree. This plan is measured against
`dev` (`7b0e8fba`, clean). **The E-DEPENDENT fact list is empty** — every row in §5 was
verified on `dev` itself. One Slice E consequence does reach this slice and is called out
where it lands (§4.2-9: the sections modal's Escape now also writes `modal=sections-closed`).

## Precondition — VERIFIED on `dev` (`7b0e8fba`, clean) at plan time

| Consumed primitive / mechanism | Where it lives | Verified |
|---|---|---|
| Slices A–E merged to `dev` | `dev` tip `7b0e8fba` (merge of `ux-slice-e-overlays`) | ✅ |
| `AbwabSection.OrderValue` exists — **no migration needed** | `domain/QuranDashboard.Domain/Abwab/AbwabSection.cs:7` | ✅ |
| **No unique index on `OrderValue`** — the section table indexes `Name` (unique, filtered on `deleted_at IS NULL`) and `DeletedAtUtc` only | `Persistence/Configurations/Abwab/AbwabSectionConfiguration.cs:57-61` | ✅ a naive `1..N` rewrite cannot collide mid-update |
| Why that matters, in the repo's own words: a unique order index is checked per statement, so `1..N` renumbering issued as one UPDATE per row would collide | `AbwabDoorConfiguration.cs:85-89` (the `global_order_value` index comment); the doors' scope index at `:81` is deliberately **non**-unique, the unique one is `(SectionId, ParentId, Name)` at `:94-97` | ✅ |
| `OrderValue` set on create as `count(live) + 1` | `Persistence/Writes/Abwab/EfAbwabSectionsWriter.cs:12,17` | ✅ — and see DRIFT-3 / §8 for the duplicate-order condition this leaves reachable |
| Sections are read ordered by `(OrderValue, Id)` — the tie-break the writer must match | `Persistence/Reads/Abwab/EfAbwabTreeReader.cs:14` | ✅ |
| `AbwabTreeSectionDto` carries `orderValue` + `version` + `doorsInScopeCount` | `EfAbwabTreeReader.cs:44-46`; wire model `core/api/generated/models/abwab-tree-section-dto.ts` | ✅ |
| **The doors reorder path, end to end** — the template item 18 mirrors | see the six rows below | ✅ |
| … route + status mapping (`Enum.IsDefined` scope guard, six outcomes) | `Controllers/Abwab/AbwabDoorsController.cs:100-126` | ✅ **verb is `POST`, not `PUT`** — DRIFT-1 |
| … body / command / handler / outcome quartet | `Application/Abwab/Commands/Doors/ReorderDoor/{ReorderDoorBody,ReorderDoorCommand,ReorderDoorHandler,ReorderDoorOutcome}.cs` | ✅ |
| … writer contract, documented in a `//` comment above the signature | `Application.Abstractions/Abwab/IAbwabDoorsWriter.cs:42-46` | ✅ |
| … writer implementation: load, bound, pin `OriginalValue`, remove/insert, resequence, save, DTO | `Persistence/Writes/Abwab/EfAbwabDoorsWriter.cs:160-218`; `Resequence` at `:695-702` | ✅ |
| … `AbwabInvalidPositionException` ("The requested position is outside the sibling range.") | `Application.Abstractions/Abwab/AbwabInvalidPositionException.cs` | ✅ reusable verbatim |
| … `AbwabReorderScope` is doors-only (`Section` / `Global`) and exists because doors have **two** order spaces | `Application.Abstractions/Abwab/AbwabReorderScope.cs:6-10`; both spaces described in `Persistence/Writes/Abwab/README.md:114-127` | ✅ — sections have one, so no scope (§4.2-3) |
| Sections controller today: `POST` (`:15`), `PUT {id:int}` rename (`:35`), `DELETE {id:int}` (`:60`) — **no reorder** | `Controllers/Abwab/AbwabSectionsController.cs` | ✅ |
| `IAbwabSectionsWriter` today: create / rename / delete only | `Application.Abstractions/Abwab/IAbwabSectionsWriter.cs:5-17` | ✅ |
| `SaveTranslatingConcurrencyAsync` — the helper a reorder uses (a write that only moves rows **out** of the unique name scope; a 23505 is structurally impossible) | `EfAbwabSectionsWriter.cs:98-110`; rule stated at `Writes/Abwab/README.md:36-38` | ✅ |
| `xmin` concurrency: `OriginalValue` (not `CurrentValue`) is what makes the check compare the client's last-seen token | `EfAbwabSectionsWriter.cs:37-40`; `AbwabSectionConfiguration.cs:50-53` | ✅ |
| Section `ApiMessages` already present: `AbwabSectionNotFound` (`:113`), `AbwabSectionStaleVersion` (`:116`); the doors' position message at `:134` is the shape the section one copies | `api/QuranDashboard.Api/Common/ApiMessages.cs` | ✅ two new constants needed (§4.2-5) |
| Handlers are registered explicitly, one `AddScoped` per handler | `Application/DependencyInjection.cs:148-150` (sections), `:155` (`ReorderDoorHandler`) | ✅ |
| `SmokeRouteCatalog` keys on `"<METHOD> <template>"` with **route constraints part of the key**; both directions asserted | `Tests/Smoke/SmokeCoverageParityTests.cs:10-35,63-68` | ✅ the entry must say `{id:int}` and `Method = HttpMethod.Post` |
| The doors `order` catalog entry — the row the section entry mirrors | `Tests/Smoke/SmokeRouteCatalog.cs:254-262` (`ParityOnly = true`) | ✅ |
| Backend abwab write tests are real-infrastructure behavior tests over a schema fixture; doors reorder is covered there (`ReorderAsync_ProducesContiguousOrderValues`, `:100-129`) | `Tests/Abwab/AbwabDoorWriteBehaviorTests.cs` (28 facts), `AbwabSchemaFixture.cs` | ✅ the precedent a section-writer test would follow — deferred to `TESTING_DEBT.md` (§4.1-6) |
| Contract regeneration pipeline: `Backend/scripts/export-swagger` → `npm run generate:api` (`ng-openapi-gen` + `scripts/prune-generated-api.mjs`, models-only) → `npm run docs:api`; `Backend/scripts/check-api-contract` runs all three and fails on `git diff --exit-code` | `Frontend/quran-dashboard-ui/ng-openapi-gen.json`, `package.json:15-16`, `Backend/scripts/README.md:13` | ✅ |
| Frontend reorder wiring to mirror: api method → write controller → 409 policy | `data-access/abwab.api.ts:72-74`; `state/abwab-write.controller.ts:142-144`; the shared failure map at `:34-47`; `dispatch` at `:204-210` | ✅ |
| Refresh-after-write is an invariant: `handleSuccess` calls `refreshAndRebind()` on **every** success | `state/abwab-write.controller.ts:211-236`; README `:440-450` | ✅ exactly what a whole-table resequence needs |
| The sections modal already reads the live `sections` input at submit time, never a value captured at edit-open | `abwab-sections-modal.component.ts:134-139`; the section-facing controller re-reads the snapshot per call (`state/abwab-sections.controller.ts:24`) | ✅ **this is the rebinding the reorder needs, already in place** |
| The modal's write functions arrive as inputs, bound by the page from the overlays controller | `abwab-sections-modal.component.ts:38-42`; `abwab-page.component.html:309-316`; `abwab-page-overlays.controller.ts:235-238` | ✅ the seam a fourth function joins |
| The tree's inline number editor — the grammar item 18 reuses: click the number → input, **Enter commits, blur and Escape both cancel** (Slice D's item 12, as shipped) | `abwab-tree.component.html:43-61`; `abwab-tree.component.ts:245-280` (`cancelOrderEdit` `:262-267`, `commitOrderEdit` `:269-279`) | ✅ |
| The tree's keydown handler opens with `event.stopPropagation()` unconditionally | `abwab-tree.component.ts:250-251` | ✅ — and the sections modal binds `(keydown.escape)="requestClose()"` on the dialog element (`abwab-sections-modal.component.html:14`), so the guard is **mandatory**, not cosmetic (§4.2-9) |
| `liveRoots` is exactly the depth-0 live partition, built once and sorted by global order | `state/abwab-tree.builder.ts:86-89` | ✅ item 19's source of truth |
| `doorsInScopeCount` is **all live doors in the section at any depth** — a different question from item 19's | `EfAbwabTreeReader.cs:37-40`; semantics recorded at `Persistence/Reads/Abwab/README.md:40-51` | ✅ |
| **Item 17's stats bar already shipped** and already consumes `doorsInScopeCount` | `pages/abwab-page/abwab-page.component.ts:141-143`; `state/abwab-tree.builder.ts:192-220`; README `:555-570`; commit `a0c9479a` (ux-slice-b2) | ✅ **DRIFT-2** — the audit says "nothing exists" |
| `.qd-tabs__count` ships with a selected-state rule and has **zero** HTML consumers | `src/styles/_components.scss:208-224`; repo-wide grep at plan time found the class only in those two SCSS rules | ✅ measured, not inherited from the audit |
| The dim-at-zero mechanism that composes with the selected-state rule: the sibling disabled rule dims with `opacity` only, four lines above | `src/styles/_components.scss:196-200` | ✅ (§4.2-11) |
| `qdTab` is a **`@Directive`**, host bindings only (`selected`, `disabled`, roving tabindex) | `shared/ui/tabs/tab.directive.ts:6-34` | ✅ — it **cannot** project a child span; see DRIFT-4 and §4.2-10 |
| §17 lists `.qd-tabs__count` as a `qd-tabs` **backing class** with "Compose, do not re-style" | `.architecture/UI_STYLE_SYSTEM.md:698-709` | ✅ the call-site rendering the class *is* composition |
| §17's count-meta convention: reserved box, `tabular-nums`, **Latin digits**, never appear/disappear | `.architecture/UI_STYLE_SYSTEM.md:893-899` | ✅ |
| The unimplemented contract line item 19 pays off | `docs/design-preview/abwab-tree-concept.html:41` (`.tab .badge`), `:207` (`كل الأبواب <span class="badge">33</span>`) | ✅ |
| The toolbar renders `section.name` alone and hides the tabs entirely in the archive view | `abwab-toolbar.component.html:1-25`; `hideSectionControls` bound to `archiveParam()` at `abwab-page.component.html:84` | ✅ — so "what the badge shows in the archive view" is *nothing renders* (§4.2-14) |
| The Arabic counted-noun helper and its per-noun form sets | `models/abwab.labels.ts:24` (`countPhrase`), `DOOR_FORMS`/`RELATION_FORMS`/`LEVEL_FORMS`/… ; mandated by README `:571-575` | ✅ |
| Existing gates for the touched frontend surfaces | `abwab-sections-modal.component.spec.ts` (15 cases), `abwab-toolbar.component.spec.ts` (5), `abwab-tree.builder.spec.ts` (18), `abwab.api.spec.ts`, `abwab-write.controller.spec.ts`; e2e `abwab-operations.e2e.ts`, `abwab-structure.e2e.ts` | ✅ |

### DRIFT — where current code contradicts the audit or this commission

| # | The audit / commission says | `dev` at `7b0e8fba` says | This plan follows |
|---|---|---|---|
| DRIFT-1 | `PUT api/abwab/sections/{id}/order` (audit `:665`, commission decision 2) | The doors reorder it tells us to mirror is **`POST api/abwab/doors/{id:int}/order`** (`AbwabDoorsController.cs:100`), and the frontend calls it with `http.post` (`abwab.api.ts:73`). The audit never states the doors verb, so the "materially differs from the audit's description" stop condition does not fire — this is the DRIFT rule's case. | **`POST api/abwab/sections/{id:int}/order`.** Decision 2's own words are "same base, same validation exception, same 409 policy" — mirroring means mirroring, and `SmokeRouteCatalog` keys on the verb (`SmokeCoverageParityTests.cs:41-42`), so the catalog entry has to match whatever ships. |
| DRIFT-2 | Item 17 "**Where:** nothing exists" (audit `:588`) | The stats bar shipped in ux-slice-b2 (`a0c9479a`) — two `qd-result-count` lines above the toolbar, the section one reading `doorsInScopeCount` (`abwab-page.component.ts:141-143`, README `:555-570`). | Item 17 is **out of scope and already done**. Its live behavior is a *constraint* on item 19, not a future one: the badge lands inches from two shipped all-depths numbers, so §4.2-13 makes the distinction legible instead of leaving two bare numbers to argue with each other. |
| DRIFT-3 | Audit `:666-672` reads the no-unique-index fact as making the resequence safe (true) and stops there | `EfAbwabSectionsWriter.cs:12` assigns `count(live) + 1` on create while `DeleteAsync` (`:50-77`) resequences nothing, so **duplicate `OrderValue`s are reachable today** (create three, delete the second, create a fourth → two rows at 3). | Not scoped (§3). Stated instead as the reason the writer must order by `(OrderValue, Id)` — the reader's own order (§4.2-4) — which makes the reorder correct *under* duplicates and heals them to `1..N` whenever it runs. Risk row §8, debt row §7. |
| DRIFT-4 | Commission decision 6: "`qdTab` gains `count?: number \| null` **rendering** `.qd-tabs__count`" | `qdTab` is a `@Directive` (`tab.directive.ts:6`) — host bindings only. A directive cannot project a child element declaratively, so it cannot render the span. | **Resolved with the user at plan time: the call-site renders the span.** §17 already lists `.qd-tabs__count` as a `qd-tabs` backing class to compose (`UI_STYLE_SYSTEM.md:707-709`), so `abwab-toolbar.component.html` rendering it *is* extending the base, not forking it. Consequence recorded honestly in §6/§7: `shared/` is untouched, so **item 19 no longer triggers Tier B** — the backend route still forces the route-smoke tier, and the pre-PR frontend gate is Tier C's full suite regardless. |

## 0. Guard result

Task arithmetic: Phase 1 = 2, Phase 2 = 5, Phase 3 = 1, Phase 4 = 1, Phase 5 = 2,
Phase 6 = 3, Phase 7 = 3, Phase 8 = 2, Phase 9 = 3. **22 tasks — under the 30-task
threshold. One slice, no split.**

Recorded so a mid-execution split does not get drawn on task count: if this slice had
split, the seam is **after Phase 4** (the backend green and the contract regenerated) —
"the route and its writer" (Phases 2–4: a new endpoint, a new writer method, a new
resequence rule, the parity entry; **nothing in the repo pins any of it yet**, and the
route-smoke tier is the only gate that exists for it) versus "the UI and the badge"
(Phases 5–7: every surface already carries a spec — `abwab.api.spec.ts`,
`abwab-write.controller.spec.ts`, `abwab-sections-modal.component.spec.ts`,
`abwab-tree.builder.spec.ts`, `abwab-toolbar.component.spec.ts`). The seam is who can be
hurt, the same test D and E used: Phase 2–4 creates behavior nothing pins; Phases 5–7
amend behavior that is already under test.

## 1. Objective

| # | Deliverable | Home | Audit item |
|---|---|---|---|
| 1 | `POST api/abwab/sections/{id:int}/order` taking `{ position, version }` — the doors reorder contract, minus the scope sections do not have | `AbwabSectionsController.cs` + a `ReorderSection` command quartet | 18 (gap 1) |
| 2 | `IAbwabSectionsWriter.ReorderAsync` + its EF implementation: bound the position, pin the client token, move, **resequence every live section to `1..N`**, save through the concurrency-translating helper | `IAbwabSectionsWriter.cs`, `EfAbwabSectionsWriter.cs` | 18 (gap 2) |
| 3 | Two new Arabic `ApiMessages` constants and one DI registration; `AbwabInvalidPositionException` reused verbatim, not re-declared | `ApiMessages.cs`, `Application/DependencyInjection.cs` | 18 |
| 4 | `SmokeRouteCatalog` entry in the **same change** (`POST`, `{id:int}`, `ParityOnly`), plus the route-smoke tier run with a RAN/SKIPPED statement for `Tests.Smoke.Data` | `SmokeRouteCatalog.cs`, `docs/feature-ux-slice-f/evidence.md` | 18 (Backend CLAUDE.md §10) |
| 5 | Contract regenerated end to end: `swagger.json` → `core/api/generated/reorder-section-body.ts` → `docs/api-reference/`; `check-api-contract` clean | the three generated artifacts | 18 (handoff) |
| 6 | Frontend write path: api method → write controller → sections controller → the overlays arrow the modal binds, all reusing the shared 409 policy and the refresh-after-write invariant | `abwab.api.ts`, `abwab-write.controller.ts`, `abwab-sections.controller.ts`, `abwab-page-overlays.controller.ts` | 18 (gap 3) |
| 7 | Number-click inline editor in the sections modal reusing the tree's grammar — Enter commits, blur cancels, Escape cancels — with the Escape guard that keeps it from closing the modal | `abwab-sections-modal.component.*` | 18 × D item 12 |
| 8 | `rootCountBySectionId` derived client-side in the builder beside `liveRoots`, pure and specced; **zero backend work** | `state/abwab-tree.builder.ts` + spec | 19 |
| 9 | Section tabs render `.qd-tabs__count` at the call-site — «كل الأبواب» shows total live roots, each section shows its own; Latin digits; always rendered, dimmed at zero | `abwab-toolbar.component.*` | 19 (+ D item 13's zero-state precedent) |
| 10 | The two counts stay distinguishable in the accessible layer, not only in the rationale: the badge's counted-noun `aria-label` names **أبواب رئيسية** against the shipped stat's **أبواب** | `models/abwab.labels.ts`, `abwab-toolbar.component.html` | 19 × 17 (DRIFT-2) |
| 11 | Docs true again in the same change: three backend READMEs' claims re-checked, the frontend abwab README, §17's `qd-tabs` entry, and the `TESTING_DEBT.md` rows this posture owes | six files, named in §9 | repo law |

## 2. Scope

**In:**

- **Backend**
  - `application/QuranDashboard.Application.Abstractions/Abwab/IAbwabSectionsWriter.cs` — one method + its `//` contract comment.
  - `infrastructure/…/Persistence/Writes/Abwab/EfAbwabSectionsWriter.cs` — `ReorderAsync` + a private `Resequence` helper.
  - `application/QuranDashboard.Application/Abwab/Commands/Sections/ReorderSection/` — four new files (`ReorderSectionBody`, `ReorderSectionCommand`, `ReorderSectionHandler`, `ReorderSectionOutcome`).
  - `application/QuranDashboard.Application/DependencyInjection.cs` — one `AddScoped` beside `:148-150`.
  - `api/QuranDashboard.Api/Controllers/Abwab/AbwabSectionsController.cs` — one action + one constructor parameter.
  - `api/QuranDashboard.Api/Common/ApiMessages.cs` — two constants.
  - `tests/QuranDashboard.Tests/Smoke/SmokeRouteCatalog.cs` — one entry.
- **Contract artifacts** — `Frontend/quran-dashboard-ui/openapi/swagger.json`, `src/app/core/api/generated/` (models-only), `docs/api-reference/`.
- **Frontend**
  - `features/abwab/data-access/abwab.api.ts` (+ `abwab.api.spec.ts`).
  - `features/abwab/state/abwab-write.controller.ts` (+ spec), `abwab-sections.controller.ts` (+ spec), `abwab-page-overlays.controller.ts`.
  - `features/abwab/components/abwab-sections-modal/` — `.ts` / `.html` / `.scss` / `.spec.ts`.
  - `features/abwab/state/abwab-tree.builder.ts` (+ spec), `features/abwab/models/abwab.models.ts` (the VM field).
  - `features/abwab/components/abwab-toolbar/` — `.html` / `.ts` / `.spec.ts` (`.scss` only if the tab needs a gap; the count box's own metrics are already in `_components.scss`).
  - `features/abwab/models/abwab.labels.ts` (+ spec) — the badge `aria-label`, the order-editor labels, `ROOT_DOOR_FORMS`.
  - `src/styles/_components.scss` — **one** rule: the `--empty` dim (§4.2-11).
  - `pages/abwab-page/abwab-page.component.html` — the fourth write function bound into the modal.
- **Docs (same change, repo law)** — `features/abwab/README.md`, `.architecture/UI_STYLE_SYSTEM.md` §17, `Backend/…/Persistence/Writes/Abwab/README.md`, `Backend/api/QuranDashboard.Api/Controllers/README.md`, `Backend/…/Persistence/Reads/Abwab/README.md` (verify-only unless a claim moved), `docs/TESTING_DEBT.md`, `docs/feature-ux-slice-f/evidence.md`.

**Out (named so nobody "finishes the thought"):**

- **Any migration, any schema change, any hand-written EF file.** `OrderValue` exists; no column is added, widened, indexed, or backfilled. Backend CLAUDE.md forbids hand-writing migrations, and none is generated either.
- **The `CountAsync + 1` create gap and the non-resequencing delete** (DRIFT-3) — the duplicate-`OrderValue` condition is documented and worked *around*, not fixed. Fixing it is a separate change with its own test obligations.
- **Template-list reorder** (audit `:673-677`) and any `AbwabTemplate` order column — flagged by the audit, never scoped by it.
- **Item 17's section stat** — shipped (DRIFT-2). Nothing in this slice edits `countLiveAbwabDoors`, `countAbwabDoorsInOpenScope`, or the two `qd-result-count` instances.
- **`EfAbwabDoorsWriter`'s 816 > 600 refactor debt** (`Writes/Abwab/README.md:49-52`) — untouched; this slice adds no line to that file.
- **The tree's own order editor** — its `<span>` trigger, its `stopPropagation`, its spec: read as the template, not edited. Item 12 shipped in D and stays as shipped.
- **Archive-view behavior** — unchanged. `hideSectionControls` already hides the whole tab strip there, so the badge simply does not render; no archived-door count is invented (§4.2-14).
- **Caching, ETags, snapshot-version reuse** — Slice I owns it, last, after F and G finalize the writes. §8 carries the one forward-looking note; nothing here is designed *for* it.
- **Slice G (templates) and Slice H (navbar)** work of any kind.
- **Any planning-artifact sweep or N-2 deletion** — deferred to the single cleanup pass after Slice I (§3).
- **Any `dev → main` merge.**

## 3. Non-goals

- **No planning-artifact sweep in this slice — standing user decision.** ALL planning-folder
  sweeps and N-2 evictions are deferred to one cleanup pass after Slice I. Nothing here
  deletes or repoints a planning folder. **Not deferred:** same-change README and §17
  amendments for behavior this slice changes — those stay mandatory (§1 row 11, §9).
- **No new test suites, per the rush-period posture** — existing suites still RUN before
  merge, parity one-liners are mandatory, and every gap becomes a row in
  `docs/TESTING_DEBT.md` in the same change (§7). **The route-smoke tier is not optional
  here** and is not debt-able: `TESTING_DEBT.md:12-14` says so in its own words, and
  `SmokeCoverageParityTests` fails by name without the catalog entry.
- **No caching design.** The snapshot fetch stays a single unparameterized tree GET; the
  reorder writes through the same `dispatch` → `refreshAndRebind` path every other write
  uses. The one thing this slice does that a future cache must respect is recorded in §8,
  as a risk, not as a design accommodation.
- **No drag-and-drop.** The audit sized item 18 as "the number-click editor in the sections
  modal reusing the tree's editor grammar" (`:664-666`); a drag interaction would be a new
  vocabulary, a new a11y contract, and a second way to express one intent.
- **No second order space.** Sections get `OrderValue` and nothing else — the `Section` /
  `Global` split exists because *doors* have two spaces (`Writes/Abwab/README.md:114-127`),
  and importing it here would be a fork of a doors-specific accident.

## 4. Locked decisions

### 4.1 Carried in from the audit / prior slices / standing rules

1. **No migration.** `AbwabSection.OrderValue` exists and carries no unique index, so a
   naive `1..N` resequence is safe (audit `:666-672`; both facts re-verified in the
   Precondition table against `AbwabSection.cs:7` and `AbwabSectionConfiguration.cs:57-61`).
2. **The doors reorder path is the template, followed exactly** — same position semantics
   (1-based, bounded against the ordered set the row already belongs to), same base route
   shape, the same `AbwabInvalidPositionException`, the same 409 concurrency policy.
   Where `dev` and the audit disagree, the code wins and the disagreement is a DRIFT row
   (DRIFT-1: the verb is `POST`).
3. **The writer resequences ALL live sections to `1..N` on every reorder, and
   refresh-after-write stays an invariant, not an optimization.** One reorder stales every
   sibling's `xmin` — exactly the doors case the abwab README already states
   (`README.md:440-450`). The command routes through `AbwabWriteController` so the 409
   policy and the snapshot refetch are shared, not forked.
4. **The rebinding this reorder needs is already in place.** `abwab-sections-modal`
   reads the section's row from the live `sections` input at submit time
   (`abwab-sections-modal.component.ts:134-139`), and `AbwabSectionsController.sections`
   is a `computed` over the facade snapshot read on every call
   (`abwab-sections.controller.ts:24`, class doc `:14-17`). After `refreshAndRebind`, the
   next submit therefore carries the *new* `version` automatically. **No new rebinding
   mechanism is built** — the plan's obligation is to route the reorder through the same
   controller so it inherits it.
5. **The editor grammar is the tree's, including item 12 as Slice D shipped it:**
   Enter commits, blur cancels, Escape cancels (`abwab-tree.component.ts:250-279`;
   README `:66-71`). No drag interaction is invented (§3).
6. **Rush-period testing posture:** no new suites per feature; existing suites run before
   merge; parity one-liners mandatory; gaps go to `docs/TESTING_DEBT.md` in the same
   change. The route-smoke tier is exempt from the posture — it runs.
7. **Same-change README + §17 amendments are repo law and in scope**; all planning-artifact
   sweeps stay deferred to the post-Slice-I pass (§3).

### 4.2 Decided by this plan

1. **Route: `POST api/abwab/sections/{id:int}/order`.** Verb from the doors precedent
   (DRIFT-1); `{id:int}` because the catalog key keeps route constraints and relaxing the
   constraint is itself a parity mismatch (`SmokeCoverageParityTests.cs:63-68`).
2. **Body: `ReorderSectionBody(int Position, uint Version)`** — `ReorderDoorBody` minus
   `Scope`. `uint Version` matches every other abwab body (`xmin`).
3. **No scope, and the narrowing is deliberate.** Sections have one order space, so there
   is no `AbwabReorderScope`, no `Enum.IsDefined` guard in the controller, and the outcome
   union drops `InvalidScope` and `ScopeNotApplicable` — four variants remain:
   `Success` / `NotFound` / `InvalidPosition` / `StaleVersion`. Stated here so a reviewer
   reads a decision rather than an incomplete mirror.
4. **The writer orders by `(OrderValue, Id)` — the reader's own order — and this is a
   deliberate deviation from the doors template.** Doors' section-scope reorder reads with
   a bare `.OrderBy(d => d.OrderValue)` (`EfAbwabDoorsWriter.cs:185`), but the sections
   reader tie-breaks on `Id` (`EfAbwabTreeReader.cs:14`). Since duplicate `OrderValue`s are
   reachable today (DRIFT-3), a writer that did not tie-break the same way would compute a
   different index than the one the user clicked. Matching the reader is what makes
   "position 3" mean the third row on screen. **Recorded in `Writes/Abwab/README.md` in the
   same change** so the next reader sees the reason, not an inconsistency.
5. **Two new `ApiMessages` constants, Arabic, beside the existing section block
   (`ApiMessages.cs:110-116`):** `AbwabSectionReordered` («تم تعديل ترتيب القسم») and
   `AbwabSectionInvalidPosition` («الترتيب المطلوب خارج نطاق الأقسام» — the doors' `:134`
   wording with the noun changed). `AbwabSectionNotFound` (`:113`) and
   `AbwabSectionStaleVersion` (`:116`) are reused as-is. Exact wording is settled at
   execution against `API_GUIDELINES.md`; the *existence, names, and which status each
   answers* are locked here.
6. **The catalog entry is `ParityOnly`, mirroring every other abwab write** — the generic
   sweep must never dispatch a write route (`SmokeRouteCatalog.cs:78-81`). `DerivedStatus`
   is `NotFound`: against a migrated-but-empty schema the writer finds no section 1 and the
   handler returns `NotFound` before any position check. **Confirmed by reading the outcome
   switch, not recorded from a run** — the catalog's own rule (`:60-64`).
7. **Only the moved section's `UpdatedAtUtc` is bumped**, matching doors
   (`EfAbwabDoorsWriter.cs:213`). Resequenced siblings get an `OrderValue` UPDATE — which
   moves their `xmin` and therefore their `version` — but not a new `updated_at`. That is
   sufficient for the snapshot version, which is `max(updated_at, deleted_at)` across the
   three tables (`Reads/Abwab/README.md:54`): the moved row's bump alone moves it.
8. **Concurrency is checked on the moved section *and* on every resequenced sibling, and
   that is correct.** `Version` is `IsRowVersion()` on the entity
   (`AbwabSectionConfiguration.cs:52-53`), so every UPDATE the resequence issues carries
   `AND xmin = @original` using the value loaded in this same call. A sibling mutated
   between the read and the save therefore 409s the whole reorder rather than silently
   overwriting it. The client token (`expectedVersion`) is additionally pinned onto the
   moved section via `OriginalValue`, exactly as rename does (`EfAbwabSectionsWriter.cs:37-40`).
9. **The modal's order editor MUST call `event.stopPropagation()` on keydown.** The dialog
   binds `(keydown.escape)="requestClose()"` on its `<section>`
   (`abwab-sections-modal.component.html:14`); without the guard, Escape-to-cancel-an-edit
   would close the whole modal — and post-Slice-E it would additionally write
   `modal=sections-closed` to the URL. The tree's handler opens with exactly this call
   (`abwab-tree.component.ts:250-251`) and this is why. Click propagation is already safe
   (the dialog stops clicks at `:13`), but the editor still stops its own click so a row
   click cannot be misread. **A spec cell pins Escape-cancels-edit-without-closing-modal.**
10. **The tab count span is rendered by the call-site; `qdTab` is unchanged** (DRIFT-4,
    resolved with the user). §17's `qd-tabs` entry already sanctions `.qd-tabs__count` as a
    backing class to compose (`UI_STYLE_SYSTEM.md:707-709`). §17 is amended with a
    **count-meta line** for `qd-tabs` (rendering rule, Latin digits, zero-state, a11y)
    rather than a new directive-input row. Converting `qdTab` to a component was considered
    and rejected: five existing call-sites, zero benefit here.
11. **Zero-state: visible and dimmed, per Slice D's ⟲13 precedent, implemented with
    `opacity` only.** A `.qd-tabs__count--empty` modifier setting `opacity` composes with
    both tab states, because the shipped selected-state rule
    (`_components.scss:222-224`) sets `background` and `color` and **not** `opacity` — no
    specificity fight, no fork. The mechanism has a precedent four lines above it: the
    disabled tab dims with `opacity: 0.5` (`:196-200`). *If* the exact ⟲13 colour treatment
    is wanted on the **selected** tab instead, that needs
    `.qd-tabs__tab.qd-is-selected .qd-tabs__count--empty` to override the colour —
    **flagged here, not built.** The box is never unmounted (§17's count-meta reservation
    rule, `UI_STYLE_SYSTEM.md:893-899`).
12. **Item 19's count is ROOT doors only, derived client-side, zero backend work.**
    `rootCountBySectionId: ReadonlyMap<number, number>` is built beside the `liveRoots`
    partition in `buildAbwabTreeSnapshot` (`abwab-tree.builder.ts:86-89`) and carried on
    `AbwabTreeSnapshotVm`. It is a map, not a per-section scan, so the toolbar's `@for`
    stays O(1) per tab. **«كل الأبواب» shows `liveRoots.length`**, which is *not*
    Σ over the map: a live root can have `sectionId === null` ("outside every section", a
    first-class state — `Writes/Abwab/README.md:80-86`), so the two are not reconcilable by
    arithmetic. The builder spec pins that non-identity explicitly, the same way the item-17
    helpers' doc comments already do (`abwab-tree.builder.ts:186-191`).
13. **`doorsInScopeCount` is NOT reused, and the two counts are made distinguishable in the
    accessible layer — not only in a rationale.** `doorsInScopeCount` is every live door in
    the section at any depth (`EfAbwabTreeReader.cs:37-40`, semantics recorded at
    `Reads/Abwab/README.md:40-51`) and it is already spoken for by item 17's shipped section
    stat (DRIFT-2). Item 19 asks a different question — *how many top-level doors are in
    this section* — so the honest answer is two numbers, not one number reused twice.
    Because they now sit inches apart (stat: «12», badge: «3»), the badge's visible digits
    are `aria-hidden="true"` and the **tab** carries an `aria-label` naming the noun:
    `«${section.name}»: ${countPhrase(n, ROOT_DOOR_FORMS)}` → «اللغة العربية: 3 أبواب
    رئيسية». `ROOT_DOOR_FORMS` is a new form set in `abwab.labels.ts` written out for all
    four Arabic forms («باب رئيسي واحد» / «بابان رئيسيان» / «N أبواب رئيسية» / «N بابًا
    رئيسيًا») rather than concatenating an adjective onto `DOOR_FORMS`, because Arabic
    adjective agreement changes with the count form. Visible digits stay **Latin**, per
    §17's count-meta convention.
14. **Archive view: nothing renders, and that is stated rather than derived.**
    `hideSectionControls` (bound to `archiveParam()`, `abwab-page.component.html:84`)
    already removes the whole tab strip in the archive view, so no badge exists there and no
    archived-door count is invented. The README sentence that explains why the tabs are
    hidden gains the badge by name.
15. **The order editor does not count as dirty work.** `isDirty`
    (`abwab-sections-modal.component.ts:56-66`) covers a typed section name and an altered
    rename draft. An editor whose own grammar is "blur cancels" is by definition not
    protected work, so an open order edit does **not** raise the discard confirm.
    `editingOrderId` is a separate signal from `editingId`; starting one does not clear the
    other, and `resetDraft` (`:93-99`) clears both. Stated so it is not decided by accident.
16. **The modal's order trigger is a real `<button>`, not the tree's `<span>`.** The tree
    renders the order chip as a clickable `<span>` (`abwab-tree.component.html:55-60`); the
    modal's rows already carry real buttons for rename and delete, and the abwab README
    records a "Zero dead controls" gotcha. Reusing the *grammar* (§4.1-5) does not mean
    reusing a dead element. The button carries an Arabic `aria-label` naming the section and
    its current order; the input carries its own `aria-label`. **The tree's `<span>` stays
    out of scope** — changing it is not this slice's item.
17. **Expected test-count delta, stated in advance: net increase, roughly +10 to +18**,
    all in existing spec files — `abwab-tree.builder.spec.ts` (the map, the non-identity),
    `abwab-toolbar.component.spec.ts` (badge renders, zero-state class, aria-label),
    `abwab-sections-modal.component.spec.ts` (the four editor states, Enter/blur/Escape,
    the Escape-does-not-close cell, error surfacing), `abwab.api.spec.ts` +
    `abwab-write.controller.spec.ts` (verb/URL/body; the 409 path), `abwab.labels.spec.ts`
    (`ROOT_DOOR_FORMS`' four forms). **Zero new spec files. Zero removals. Zero new backend
    tests** — that gap is the §7 debt rows, by posture.
18. **One light branch off `dev`: `ux-slice-f-sections`.** Per-phase commits, PR targets
    `dev`, never `main`.

## 5. The ground truth this plan is derived from

Read before executing; each row is a measured fact from `dev` at `7b0e8fba`, not an
assumption. Rows already carried in the Precondition table are not repeated.

| Fact | Where |
|---|---|
| The doors reorder controller action: `[HttpPost("{id:int}/order")]`, scope guard before the handler, six outcomes mapped to 200/404/400/400/400/409 | `AbwabDoorsController.cs:100-126` |
| The doors reorder handler catches exactly three exception types and maps them to outcome variants; `null` from the writer means NotFound | `ReorderDoorHandler.cs:12-43` |
| The doors reorder writer's shared tail: bound → pin `OriginalValue` → remove/insert → resequence → stamp `UpdatedAtUtc` → save → DTO | `EfAbwabDoorsWriter.cs:194-218` |
| `Resequence` is a 1-based renumber over an already-ordered enumerable | `EfAbwabDoorsWriter.cs:695-702` |
| Sections' two save helpers and which write uses which | `EfAbwabSectionsWriter.cs:82-110`; rationale at `Writes/Abwab/README.md:33-40` |
| Section delete already uses `SaveTranslatingConcurrencyAsync` and explains why in a comment — the same reasoning a reorder inherits | `EfAbwabSectionsWriter.cs:70-74` |
| `AbwabSectionDto` is `(Id, Name, OrderValue, Version)`; `ToDto` is a one-liner | `Responses/AbwabSectionDto.cs`; `EfAbwabSectionsWriter.cs:112-113` |
| The section-write outcome/status pairs already in the controller (400 invalid name, 404 not found, 409 stale, 409 duplicate) | `AbwabSectionsController.cs:44-57` |
| Smoke write tests already exercise the section routes' status contract by hand (`PostAsJsonAsync` / `PutAsJsonAsync` / `DeleteAsync`) — the file a future reorder smoke case would join | `Tests/Smoke/SmokeAbwabWriteTests.cs:21-171` |
| The doors reorder smoke cases (position out of range, stale version, malformed body) — the shape the deferred section cases would copy | `Tests/Smoke/SmokeAbwabWriteTests.cs:465-530` |
| `ParityOnly` semantics: catalogued so the gate sees it, deliberately not dispatched by the sweep | `SmokeRouteCatalog.cs:78-81` |
| Frontend 409 policy: 409 → `conflict`, 400/404 → `invalid`, anything else → `error`, backend message preferred over the fallback | `abwab-write.controller.ts:34-47` |
| `dispatch` + `handleSuccess` → `refreshAndRebind()` on every success, including payload-less ones | `abwab-write.controller.ts:204-236` |
| A door-scoped write passes `conflictClearsSelectionId`; section writes do not (no door selection to invalidate) | `abwab-write.controller.ts:122-128` vs `:134-144` |
| The modal's error strip is a single `qd-state variant="error"` fed by one `errorMessage` signal — the surface a reorder failure reuses | `abwab-sections-modal.component.html:21-23`; `.ts:51` |
| The modal resets every draft signal when `open()` flips true, because the instance outlives the close | `abwab-sections-modal.component.ts:80-99` |
| The modal row markup: `@for … track section.id`, rename/delete buttons with per-row testids | `abwab-sections-modal.component.html:26-64` |
| The tree's editor markup: `<input type="number" min="1">`, `(click)` stop, `(keydown)`, `(blur)` cancel | `abwab-tree.component.html:43-53` |
| `commitOrderEdit` clears `editingId` **before** emitting, and validates `Number.isInteger(value) && value >= 1` client-side | `abwab-tree.component.ts:269-279` |
| `AbwabTreeSnapshotVm` is the VM the new map joins | `models/abwab.models.ts` (`sections`, `liveRoots`, `archivedRoots`, `byId`, `version`) |
| The item-17 helpers' doc comments already state the "not reconcilable by arithmetic" rule for the *stats*; item 19 restates it for *roots* | `abwab-tree.builder.ts:185-191,202-210` |
| §17's `qd-tabs` entry: purpose, inputs/roles, selected/hover/disabled, backing classes | `.architecture/UI_STYLE_SYSTEM.md:698-709` |
| §17's count-meta convention (reserved box, `tabular-nums`, Latin digits, never unmount) | `.architecture/UI_STYLE_SYSTEM.md:893-899` |
| The frontend README's toolbar paragraph (what the tabs are, why they hide in the archive view) | `features/abwab/README.md:57-62` |
| The frontend README's sections-modal paragraph (list/add/rename/delete-empty, dirty guard, inputs-not-injection, live-row-at-submit) | `features/abwab/README.md:127-140` |
| The frontend README's refresh-after-write invariant paragraph | `features/abwab/README.md:440-450` |
| The frontend README's stats-bar paragraph — the text item 19 must not contradict | `features/abwab/README.md:555-570` |
| The Controllers README's abwab paragraph: "twenty write endpoints" — a count this slice moves to twenty-one | `Backend/api/QuranDashboard.Api/Controllers/README.md:8-16` |
| The Writes README's opening: "five writers back the twenty `/api/abwab` write endpoints" — same count, second place | `Backend/…/Persistence/Writes/Abwab/README.md:7-9` |
| The Writes README's sections-writer line: "create / rename / delete-empty" | `Backend/…/Persistence/Writes/Abwab/README.md:15` |
| `TESTING_DEBT.md`'s own exclusion: catalog parity entries are **not** debt-able | `docs/TESTING_DEBT.md:12-14` |
| Tier definitions and the change-to-tier matrix row for "API endpoint added" (A + `Tests.Api.*` + Smoke; pre-PR C + `Tests.Api.*` + Smoke; state whether the data tier ran or skipped) | `TESTING_STRATEGY.md:92-190,296-311` |
| The route obligation and the reviewer's blocking rule | `TESTING_STRATEGY.md` §10 |
| Validated commands (backend focused / no-pipeline / smoke; frontend focused / full / build) | `TESTING_STRATEGY.md:313-420` |

## 6. Phases

### Phase 1 — Baseline and record (2 tasks)

- **T101** — Baseline on `dev`, both stacks, recorded into
  `docs/feature-ux-slice-f/evidence.md` with the `dev` SHA: `dotnet build
  Backend/QuranDashboard.sln`, the no-pipeline filter, the **route-smoke tier**, then
  `npm test` + `npm run build`. Record file/test counts and timings, **and state whether
  `QuranDashboard.Tests.Smoke.Data` ran or skipped in this baseline** — the closing run is
  compared against it, and a tier that skipped at both ends must say so twice, not silently
  once. No CI exists (`TESTING_STRATEGY.md` §8); every later delta measures against this run.
- **T102** — Record the slice in the root `CLAUDE.md` "Active Spec Kit Feature" section
  (slug `ux-slice-f`, this plan, plan-driven — no `specs/` workspace). Create branch
  `ux-slice-f-sections` off `dev`. **Do not sweep any planning folder** (§3).

### Phase 2 — Backend: contract, writer, wiring, catalog entry (5 tasks)

- **T201** — `IAbwabSectionsWriter.ReorderAsync` — signature plus the `//` contract comment
  the interface's other three methods carry (`IAbwabSectionsWriter.cs:7-16`):

  ```csharp
  // Null = section missing or archived. Throws AbwabStaleVersionException,
  // AbwabInvalidPositionException. Resequences EVERY live section to 1..N — sections have one
  // order space, so there is no scope to narrow; the ordered read tie-breaks on Id to match
  // EfAbwabTreeReader's own order (duplicate OrderValues are reachable, see the area README).
  Task<AbwabSectionDto?> ReorderAsync(int id, int position, uint expectedVersion, CancellationToken cancellationToken);
  ```

- **T202** — `EfAbwabSectionsWriter.ReorderAsync`, the algorithm below verbatim, plus a
  private `static void Resequence(IEnumerable<AbwabSection>)` mirroring
  `EfAbwabDoorsWriter.cs:695-702`:

  1. Load the target: `FirstOrDefaultAsync(s => s.Id == id && s.DeletedAtUtc == null)`. `null` → return `null`.
  2. Load the ordered live set: `Where(s => s.DeletedAtUtc == null).OrderBy(s => s.OrderValue).ThenBy(s => s.Id).ToListAsync()` — **the reader's order** (§4.2-4). The target is in this list by construction.
  3. Bound: `position < 1 || position > list.Count` → `throw new AbwabInvalidPositionException()`.
  4. Pin the client token: `db.Entry(section).Property(s => s.Version).OriginalValue = expectedVersion`.
  5. `list.RemoveAll(s => s.Id == id); list.Insert(position - 1, section); Resequence(list);`
  6. `section.UpdatedAtUtc = DateTimeOffset.UtcNow;` — the moved row only (§4.2-7).
  7. `await SaveTranslatingConcurrencyAsync(cancellationToken);` — a reorder only moves rows *out* of the unique-name scope, so 23505 is structurally impossible (`Writes/Abwab/README.md:36-38`).
  8. `return ToDto(section);`

  Steps 2 and 4 are ordered as written on purpose: the tracked instance from step 1 is the
  same object the step-2 query materializes, so `OriginalValue` is pinned once, after both
  reads, exactly as `ReorderWithinAsync` does (`EfAbwabDoorsWriter.cs:194-215`).

- **T203** — The command quartet under
  `Application/Abwab/Commands/Sections/ReorderSection/`, each file mirroring its doors
  counterpart:
  - `ReorderSectionBody(int Position, uint Version)`
  - `ReorderSectionCommand(int Id, int Position, uint Version)`
  - `ReorderSectionOutcome` — sealed hierarchy with `Success(AbwabSectionDto)`, `NotFound`, `InvalidPosition`, `StaleVersion` (§4.2-3)
  - `ReorderSectionHandler` — `FeatureName = "AbwabSections"`, `OperationName = "ReorderSection"`; `null` → `NotFound`; `catch (AbwabInvalidPositionException)` → `InvalidPosition`; `catch (AbwabStaleVersionException)` → `StaleVersion`; the same `LogWarning`/`LogInformation` shape as `ReorderDoorHandler.cs:20-42`.

  Plus `services.AddScoped<ReorderSectionHandler>();` in
  `Application/DependencyInjection.cs` beside `:148-150`, and the matching `using`.

- **T204** — `AbwabSectionsController`: a fourth constructor parameter and the action.
  Full contract, exactly as it ships:

  | | |
  |---|---|
  | **Route / verb** | `POST api/abwab/sections/{id:int}/order` (`[HttpPost("{id:int}/order")]` under the `[Route("api/abwab/sections")]` class attribute) |
  | **Request** | `{id}` from the path (`int`); body `ReorderSectionBody { position: int, version: uint }` |
  | **Response type** | `ActionResult<ApiResponse<AbwabSectionDto>>`; `AbwabSectionDto { id: int, name: string, orderValue: int, version: uint }` — the section **after** the resequence |
  | **200 OK** | `ApiResponse<AbwabSectionDto>.Ok(section, ApiMessages.AbwabSectionReordered)` |
  | **400 Bad Request** | `AbwabInvalidPositionException` → `Outcome.InvalidPosition` → `ApiResponse<AbwabSectionDto>.Fail(ApiMessages.AbwabSectionInvalidPosition)` |
  | **400 Bad Request** | Malformed/unparsable body — the API's existing model-binding response, not this action's code (`ApiBehavior`; the doors' equivalent is pinned at `SmokeAbwabWriteTests.cs:520-530`) |
  | **404 Not Found** | writer returned `null` (section missing **or** archived) → `Outcome.NotFound` → `Fail(ApiMessages.AbwabSectionNotFound)` |
  | **409 Conflict** | `AbwabStaleVersionException` → `Outcome.StaleVersion` → `Fail(ApiMessages.AbwabSectionStaleVersion)` |
  | **`_ =>`** | `throw new InvalidOperationException($"Unhandled {nameof(ReorderSectionOutcome)} variant.")` — the file's own convention |

  Same task: the two `ApiMessages` constants (§4.2-5). **No `Enum.IsDefined` guard** — there
  is no scope to guard (§4.2-3).

- **T205** — `SmokeRouteCatalog.cs`: one entry in the sections block (`:224-239`), in the
  controller's own declaration order (create, rename, delete, **reorder**), matching the
  doors `order` row's shape (`:259-262`):

  ```csharp
  new("api/abwab/sections/{id:int}/order", "/api/abwab/sections/1/order", HttpStatusCode.NotFound)
  {
      Method = HttpMethod.Post, ParityOnly = true,
  },
  ```

  `DerivedStatus` is `NotFound` per §4.2-6, derived by reading the action's outcome switch
  against an empty schema — **not** recorded from a run. The block comment above the
  sections rows already explains `ParityOnly`; extend it only if it becomes untrue.

  **This lands inside Phase 2, not after it**, so the phase commit is green:
  `SmokeCoverageParityTests.EveryRegisteredRoute_HasACatalogEntry` fails by name from the
  moment the route is registered, and Backend CLAUDE.md §10's "in the same change" then
  holds per-commit rather than only per-PR. The gate still cannot be skipped — Phase 3
  running it is its own phase.

  *Verification (T201–T205):* `dotnet build Backend/QuranDashboard.sln`, then the focused
  namespace `--filter "FullyQualifiedName~QuranDashboard.Tests.Abwab"` and the API slice
  `--filter "FullyQualifiedName~QuranDashboard.Tests.Api"`. Both parity directions are
  proven by Phase 3's run, not asserted here.

### Phase 3 — The route gate (1 task)

- **T301** — Route-smoke tier, the validated command
  (`TESTING_STRATEGY.md:356-358`): `dotnet test … --filter
  "FullyQualifiedName~QuranDashboard.Tests.Smoke."`. Both parity directions must pass.
  **Record in `evidence.md` whether `QuranDashboard.Tests.Smoke.Data` RAN or SKIPPED**
  (it self-skips when `resources/db-dumps/quran-canonical/` is absent; a stale dump fails
  loud) — this statement is a required review artifact, not a nicety.

### Phase 4 — Contract regeneration (1 task)

- **T401** — `Backend/scripts/export-swagger`, then `npm run generate:api` (`ng-openapi-gen`
  + `scripts/prune-generated-api.mjs`, models-only), then `npm run docs:api`. Confirm
  `src/app/core/api/generated/models/reorder-section-body.ts` exists with `position: number`
  and `version: number`, and that `models.ts` re-exports it. Finish with
  `Backend/scripts/check-api-contract` reporting no staleness. **Never hand-edit generated
  files.** This is the Phase 2–3 → Phase 5 handoff artifact; Phase 5 cannot start before it.

  *Verification:* `npm run build` — the generated model must typecheck before anything
  consumes it.

### Phase 5 — Frontend write path (2 tasks)

- **T501** — Four small additions, each mirroring its `reorderDoor` neighbour:
  - `abwab.api.ts`: `reorderSection(id: number, body: ReorderSectionBody): Observable<ApiResponse<AbwabSectionDto>>` → `http.post(`${this.base}/sections/${id}/order`, body)` (beside `:72-74`).
  - `abwab-write.controller.ts`: `reorderSection(id, body)` → `this.dispatch(this.api.reorderSection(id, body))`. **No `conflictClearsSelectionId`** — a section write invalidates no door selection, matching `renameSection` (`:122-124`).
  - `abwab-sections.controller.ts`: `reorderSection(id: number, position: number, version: number)` → the write controller, matching `renameSection`'s (id, value, version) shape (`:30-32`).
  - `abwab-page-overlays.controller.ts`: `readonly reorderSection = (id: number, position: number, version: number) => this.sectionsController.reorderSection(id, position, version);` beside `:235-238`; bound into the modal in `abwab-page.component.html:309-316`.
- **T502** — Spec cells in the two existing suites: `abwab.api.spec.ts` — the request is a
  **POST** to `/sections/{id}/order` carrying `{ position, version }` (verb and URL are the
  contract; assert both); `abwab-write.controller.spec.ts` — success refreshes the snapshot,
  409 maps to `conflict` with the backend message, 400 maps to `invalid`, and **the door
  selection is not cleared**.

  *Verification:* focused glob
  `npm test -- --include="src/app/features/abwab/**/*.spec.ts"` (fork cap preserved via the
  npm script).

### Phase 6 — The sections-modal order editor (3 tasks)

- **T601** — The editor, per row, in `abwab-sections-modal.component.{ts,html,scss}`.
  State machine, all four states named:

  | State | Trigger in | Renders | Trigger out |
  |---|---|---|---|
  | **idle** | default; any commit/cancel/`resetDraft` | `<button type="button" class="abwab-sections-modal__order qd-chip qd-chip--pill">{{ section.orderValue }}</button>`, Arabic `aria-label` naming the section and its order, testid `abwab-sections-modal-order-{id}` | click → editing |
  | **editing** | order button click (`editingOrderId.set(section.id)`) | `<input type="number" min="1">` seeded from the **live** `sections()` row, own `aria-label`, testid `abwab-sections-modal-order-input-{id}` | Enter → submitting; Escape → idle; blur → idle |
  | **submitting** | Enter with an integer ≥ 1 | `editingOrderId` is cleared **before** the call (the tree's ordering, `abwab-tree.component.ts:273`), so the row renders idle with its **old** number until the refetch lands; no spinner is invented — the modal has no busy vocabulary today | success → idle with the new number; failure → error |
  | **error** | non-success outcome | the existing single `qd-state variant="error"` strip fed by `errorMessage` (`html:21-23`) — no second error surface | any subsequent action clears it, as `add`/`saveRename` already do (`.ts:114-121,140-147`) |

  **Retry story, stated because it is not what one would assume:** there is no retry
  affordance and none is added — the user simply reopens the editor and resubmits. On a
  **409 the client's `version` stays stale**, because `refreshAndRebind()` is called from
  `handleSuccess` only; `handleFailure` sets the announcement and returns
  (`abwab-write.controller.ts:211-248`). So an immediate resubmit 409s again until an
  unrelated successful write or a reload refreshes the snapshot. This is **pre-existing and
  identical for `renameSection` and `deleteSection`** — §8 carries it as a known condition;
  fixing it means changing the shared failure path and is out of scope (§2).

  Submit reads the row's **current** `version` from `sections()` at submit time, exactly as
  `saveRename` does (`.ts:135`) — never a value captured when the editor opened.
  Client-side validation mirrors the tree: `Number.isInteger(value) && value >= 1`, else the
  edit is abandoned silently (`abwab-tree.component.ts:276-278`). The server remains
  authoritative for the upper bound (400 `InvalidPosition`).
- **T602** — Keyboard, focus, RTL, a11y:
  - `onOrderKeydown` opens with `event.stopPropagation()` — **mandatory** (§4.2-9); Enter commits, Escape cancels; `(blur)` cancels.
  - The order button's `(click)` stops propagation so a future row-level click handler cannot fight it.
  - **Focus, named exactly — there is no in-modal precedent to copy.** `startRename` sets signals and lets the template swap in an `<input>` with **no focus call** (`abwab-sections-modal.component.ts:124-128`), and the tree's order editor does the same (its only `focus()` is row focus, `abwab-tree.component.ts:354`). Copying that would leave focus on `<body>` after the trigger button unmounts — Enter would do nothing and **`blur` would never fire, making "blur cancels" dead in the browser** (the tree's e2e only passes because Playwright's `fill()` focuses the input for it, `e2e/abwab-operations.e2e.ts:36-41`). So: a template ref `#orderInput` on the input, `private readonly orderInput = viewChild<ElementRef<HTMLInputElement>>('orderInput')` (unique because one editor is open at a time), and `afterNextRender(() => this.orderInput()?.nativeElement.focus(), { injector: this.injector })` inside the click handler. Both idioms already ship here — `viewChild` refs in `shared/ui/detail-modal-shell/detail-modal-shell.component.ts:50-53`, `afterNextRender` in `shared/ui/pagination/pagination.component.ts:81`. `queueMicrotask` (the tree's `:354` shape) is **rejected**: the input does not exist yet at microtask time. On commit or cancel, focus returns to the order button by the same mechanism. The modal's `cdkTrapFocus` (`html:10-11`) is untouched. **The tree's identical hole is an observation, not this slice's fix** (§2, out).
  - RTL: no `dir` override on the input — the dialog is `dir="rtl"` and the tree's editor carries none either; digits are Latin by §17's count-meta convention, which is a *rendering* convention, not a direction override. Confirm visually in T902 rather than asserting it here.
  - An open order edit does **not** make the modal dirty (§4.2-15).
- **T603** — Spec cells in `abwab-sections-modal.component.spec.ts` (existing file): click
  opens the editor; Enter submits `(id, position, version)` with the **live** version;
  Escape cancels **and the modal stays open** (the §4.2-9 cell); blur cancels without
  submitting; a 409 outcome renders the error strip; a non-integer/zero input submits
  nothing; `resetDraft` on reopen clears an open order edit.

  *Verification:* focused glob, as Phase 5.

### Phase 7 — Item 19: the tab count badges (3 tasks)

- **T701** — `state/abwab-tree.builder.ts`: build `rootCountBySectionId` in the same pass
  that produces `liveRoots` (`:86-89`) — one increment per root with a non-null `sectionId`
  — and carry it on `AbwabTreeSnapshotVm` (`models/abwab.models.ts`). **First, a ten-second
  ripple check:** `grep -rn "AbwabTreeSnapshotVm" src/app` — at plan time it resolves to five
  non-spec files (the model, the builder, the facade, the selection store, the README), and
  no spec constructs the VM literally (component specs pass `liveRoots` as an *input*, not a
  VM), so the new field breaks no test and §4.2-17's delta holds. If a literal VM
  construction has appeared since, every such site is fixed in this task and the delta is
  restated. Spec cells in
  `abwab-tree.builder.spec.ts`: counts roots only (a nested door in the section does not
  count), excludes archived roots, omits/zero-defaults a section with no roots, and — the
  cell that protects §4.2-12 — **Σ over the map can be less than `liveRoots.length`** when a
  root has `sectionId === null`.
- **T702** — The count markup at the call-site (§4.2-10) in
  `abwab-toolbar.component.html`, on both the «كل الأبواب» button and the `@for` tabs:

  ```html
  <span class="qd-tabs__count" [class.qd-tabs__count--empty]="n === 0" aria-hidden="true">{{ n }}</span>
  ```

  plus the one new SCSS rule in `src/styles/_components.scss` beside the shipped block
  (`:208-224`): `.qd-tabs__count--empty { opacity: 0.5; }` — opacity only, so it composes
  with the selected-state rule (§4.2-11). `qdTab` is **not** edited. The box is always
  rendered, never unmounted.
- **T703** — Toolbar inputs and labels: a `rootCountBySectionId` input (map) plus a
  `totalRootCount` input, both bound by `abwab-page.component.html:79-88` from the facade
  snapshot; `«كل الأبواب»` shows `totalRootCount`. `abwab.labels.ts` gains `ROOT_DOOR_FORMS`
  and `tabRootCountAriaLabel(name, count)` / `allDoorsTabRootCountAriaLabel(count)`
  (§4.2-13), bound as `[attr.aria-label]` on each tab button. Spec cells in
  `abwab-toolbar.component.spec.ts`: the badge renders on every tab, shows `0` with the
  `--empty` class rather than unmounting, «كل الأبواب» shows the total, and the tab's
  accessible name carries the counted-noun phrase. `abwab.labels.spec.ts` pins
  `ROOT_DOOR_FORMS`' four Arabic forms.

  *Verification:* focused glob; plus `npm run build` once at the end of the phase, since a
  new VM field crosses the builder/page/toolbar boundary.

### Phase 8 — Docs true again (2 tasks)

- **T801** — Backend docs, one coherent edit:
  - `Backend/…/Persistence/Writes/Abwab/README.md` — the sections-writer key-piece line (`:15`) becomes "create / rename / **reorder** / delete-empty"; the endpoint count (`:7-9`, "twenty") becomes twenty-one; a new invariant paragraph records **the `(OrderValue, Id)` ordering deviation and why** (§4.2-4), **the whole-table `1..N` resequence and the sibling-`xmin` consequence** (§4.2-8), and **the duplicate-`OrderValue` condition the create path leaves reachable** (DRIFT-3) — stated as a known condition the reorder heals, not as a fix.
  - `Backend/api/QuranDashboard.Api/Controllers/README.md` — the abwab paragraph's endpoint inventory (`:8-16`) gains the reorder route and its count moves to twenty-one.
  - `Backend/…/Persistence/Reads/Abwab/README.md` — **verify only.** `DoorsInScopeCount`'s recorded semantics (`:40-51`) must still read true after item 19 exists; amend only if this slice made a sentence false (it should not — item 19 adds a *client-side* count and touches no reader). Record the outcome either way in `evidence.md`.
  - `docs/contracts/http-api.md` — **verify only.** It is pointer-only by construction ("This page does **not** restate routes, parameters, or payloads", `:5-7`) and its precedence note already defers to the controller + `Controllers/README.md`, so a twenty-first route should need no edit. Confirm and record the outcome, as Slice E's close-out did for the same index.
- **T802** — Frontend docs, one coherent edit:
  - `features/abwab/README.md` — the sections-modal paragraph (`:127-140`) gains the order editor and its grammar **including the Escape guard and why** (§4.2-9); the toolbar paragraph (`:57-62`) gains the count badge, its root-only meaning, and the archive-view statement (§4.2-14); a sentence beside the stats-bar paragraph (`:555-570`) states that the badge and the stat answer **different questions** and must never be asserted to agree (§4.2-13); the refresh-after-write paragraph (`:440-450`) names the section reorder as a second whole-scope resequencer.
  - `.architecture/UI_STYLE_SYSTEM.md` §17 — the `qd-tabs` entry (`:698-709`) gains a **count-meta line**: `.qd-tabs__count` is rendered by the call-site (the directive cannot project it), Latin digits with `tabular-nums`, always rendered and dimmed at zero via `--empty` (opacity only, so it composes with the selected-state rule), and the visible digits are `aria-hidden` with the accessible name carried by the tab.
  - `docs/TESTING_DEBT.md` — the §7 rows, in the same change.

### Phase 9 — Verification and close-out (3 tasks)

- **T901** — Tier C against T101 (`TESTING_STRATEGY.md:166-190`, matrix row "API endpoint
  added/changed"):
  - `dotnet build Backend/QuranDashboard.sln`
  - no-pipeline regression (`--filter "FullyQualifiedName!~.Quran.Import.&…&FullyQualifiedName!~QuranDashboard.Tests.Smoke."`)
  - **route-smoke tier** (`--filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."`) — required, with the RAN/SKIPPED statement for `Tests.Smoke.Data` repeated for the closing run
  - `npm test` (full Frontend suite) + `npm run build`

  **Tier B fires too, on two independent grounds**, and the evidence names them: the slice
  edits the global stylesheet `src/styles/_components.scss` (Frontend CLAUDE.md's Tier B
  trigger list covers global styles / reusable UI classes / theming), and it **completes a
  full backend+frontend vertical slice** (`TESTING_STRATEGY.md:137-165`). The same
  `npm test` + `npm run build` satisfies both B and C — one run, two triggers, both stated.
  What is *not* claimed: item 19 touches no file under `shared/` (DRIFT-4), so `qdTab` and
  `qd-tabs` carry no behavior change. Expected delta per §4.2-17: +10 to +18 frontend tests, zero removals,
  zero new spec files, **backend test count unchanged**. Any other delta is explained or
  fixed before proceeding.
- **T902** — Browser acceptance matrix into `evidence.md`, both themes, RTL:
  reorder first→last, last→first, middle; a single-section list; a section with zero doors;
  an out-of-range position (server 400, message surfaces in the strip); a stale version
  forced by a second tab (409, message surfaces, and — per T601's retry note — **an
  immediate resubmit 409s again**; it recovers after a reload or an unrelated successful
  write, and the evidence records that as the observed pre-existing behavior, not a defect
  introduced here); **after a successful reorder the toolbar tab strip renders in the new
  order** (`abwab-toolbar.component.html:13-23` renders `sections()` in wire order — the
  observable point of item 18, not just the modal list); Escape cancels the edit **without
  closing the modal**; blur cancels **after only clicking the number, with no other click**
  (the T602 focus mechanism is what makes that reachable); the badges on every tab including
  a zero-root section; «كل الأبواب»'s total; the archive view showing no tabs at all; a
  keyboard-only pass over the editor and the tab strip.
  The five abwab e2e specs run once (`npm run e2e`, single-worker abwab project) as
  extraction-style evidence — **never cited as a gate**.
- **T903** — Close-out sweep: `grep -rn` (prose included) for `"twenty"`, `"20 write"`,
  `"create / rename / delete"`, and `"doorsInScopeCount"` across the repo — every hit either
  updated or verified still true (closed-slice evidence folders keep their historical
  wording; evidence is not rewritten). Final `evidence.md` entry: baseline vs closing
  numbers for both stacks, the two `Tests.Smoke.Data` statements, the acceptance artifacts,
  and the `Reads/Abwab/README.md` verify-only outcome. The Active-Feature record clears at
  merge, not before.

## 6a. Interaction matrix — `ReorderAsync`, operation × state

One operation (reorder section `id` to `position`), every state it can meet. The expected
outcome per cell is the contract; T902 walks the reachable ones in the browser and the
deferred backend behavior test in §7 row F1 would pin them.

| State | Expected outcome |
|---|---|
| Live section, `1 ≤ position ≤ liveCount`, fresh token | **200.** Every live section renumbered `1..N`; the moved section lands at `position`; only its `updated_at` moves; the response DTO carries its new `orderValue` and its new `version` |
| Only section in the table (`liveCount == 1`), `position == 1` | **200**, a no-op renumber: the list is `[section]`, remove+insert returns it to index 0, `OrderValue` is written as `1`. If it already was `1`, EF issues no UPDATE for it — but `updated_at` changed in step 6, so a save still occurs and the snapshot version still moves |
| First → last (`position == liveCount`) | **200.** Every other section shifts down by one |
| Last → first (`position == 1`) | **200.** Every other section shifts up by one |
| `position < 1` or `position > liveCount` | **400** `AbwabSectionInvalidPosition`. Thrown **before** any mutation and before the token is pinned, so nothing is written |
| `position` equals the section's current position | **200**, idempotent. Explicitly not a 400 — "no change" is a legal request, matching doors |
| Stale `version` (another client renamed/reordered this section meanwhile) | **409** `AbwabSectionStaleVersion`. The pinned `OriginalValue` makes the UPDATE affect zero rows → `DbUpdateConcurrencyException` → `AbwabStaleVersionException` (`EfAbwabSectionsWriter.cs:106-109`) |
| A **sibling** was mutated between this call's read and its save | **409**, same message. Every resequenced sibling's UPDATE carries its own `xmin` check (§4.2-8), so the resequence is all-or-nothing rather than partially applied |
| Section with zero doors | **200.** Door counts are irrelevant to section order; no door row is read or written |
| Section deleted (soft) before the call | **404** `AbwabSectionNotFound` — step 1's `DeletedAtUtc == null` filter returns `null`; the writer contract's "null = missing **or** archived" |
| Section deleted **between** the two reads | **404** if step 1 missed it; if step 1 saw it and a delete committed before the save, the pinned token no longer matches → **409**. Both are honest answers; neither corrupts the sequence |
| `id` never existed | **404** |
| Duplicate `OrderValue`s already in the table (DRIFT-3) | **200**, and the duplicates are **healed** to `1..N`. Correct because the writer reads in `(OrderValue, Id)` — the same order the tree reader renders (§4.2-4) — so the clicked position and the computed index agree even while duplicates exist |
| A section created concurrently, after the ordered read | **409** only if it collided with a tracked row's token; otherwise **200**, and the new section keeps its `count+1` value until the next reorder heals it. Stated so nobody reads the `1..N` guarantee as a table-wide lock |
| Malformed body (missing `position`, non-numeric `version`) | **400** from model binding, before the action runs |

## 7. Testing posture and the debt it owes

**Posture (rush period, §4.1-6):** no new test suites are written for this feature; the
existing suites still RUN before merge; parity one-liners are mandatory; every gap becomes
a row in `docs/TESTING_DEBT.md` **in the same change**.

**What runs, and the exact validated commands** (`TESTING_STRATEGY.md:313-420`):

- Per backend phase (Tier A):
  `dotnet build Backend/QuranDashboard.sln` then
  `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Abwab"` and
  `… --filter "FullyQualifiedName~QuranDashboard.Tests.Api"`.
- **Route-smoke tier — required, not optional** (matrix row "API endpoint added/changed"; §10's reviewer rule):
  `dotnet test … --no-build --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."`,
  with the **`Tests.Smoke.Data` RAN/SKIPPED statement in the evidence** at both baseline and close.
- Per frontend phase (Tier A):
  `npm test -- --include="src/app/features/abwab/**/*.spec.ts"` (fork cap
  `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2` preserved by the npm script).
- Pre-PR (Tier C): backend build + the no-pipeline filter + the smoke filter;
  `npm test` (full suite) + `npm run build`.
- `Backend/scripts/check-api-contract` in Phase 4 — a staleness gate, not a test tier.
- `npm run e2e` is **evidence only** and never cited as a gate.

**Tier accounting, stated so the record cannot overclaim:** the new route is what forces
`Tests.Api.*` + Smoke at both Tier A and Tier C. **Tier B fires on two grounds** — the
global-stylesheet edit (`src/styles/_components.scss`, §4.2-11) and the completion of a
backend+frontend vertical slice (`TESTING_STRATEGY.md:137-165`) — and Tier C's full
`npm test` + `npm run build` satisfies both; the evidence names the triggers rather than
letting one run imply the other. What is **not** claimed: no file under `shared/` is edited
(DRIFT-4), so `qdTab`/`qd-tabs` behavior is unchanged and no shared-primitive spec is owed.

**Rows this slice adds to `docs/TESTING_DEBT.md`** (new section, `ux-slice-f`, with today's
absolute date and the same table shape the two existing sections use):

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| F1 | **The section reorder writer's behavior** — contiguous `1..N` across every live section, first→last and last→first, single-section no-op, out-of-range refusal, the stale-token 409, and the sibling-token 409 that makes the resequence all-or-nothing | `Persistence/Writes/Abwab/EfAbwabSectionsWriter.ReorderAsync` | The next change to the sections writer, **or** the fix for the `CountAsync + 1` / non-resequencing-delete gap (F2) — both have to re-derive these rules anyway. `AbwabDoorWriteBehaviorTests.cs:100-176` is the shape it copies |
| F2 | **The duplicate-`OrderValue` condition itself** — create assigns `count(live) + 1` while delete resequences nothing, so two live sections can share an `OrderValue`; nothing anywhere asserts the reorder stays correct under it, and nothing asserts the heal | `EfAbwabSectionsWriter.cs:12,50-77` | Whoever fixes the create/delete gap. Until then the correctness rests entirely on the `(OrderValue, Id)` tie-break (§4.2-4), which is documented and untested |
| F3 | **Section reorder smoke** — the `200`/`400`/`404`/`409` status and envelope contract of the new route (catalogued `ParityOnly`, i.e. listed but not dispatched). The doors cases at `SmokeAbwabWriteTests.cs:465-530` are the template | When write protection lands and `/api/abwab` stops being `Open`: the auth cases force a dispatched test per route regardless |

**Not debt-able, and not listed above:** the `SmokeRouteCatalog` entry
(`TESTING_DEBT.md:12-14`) and any tier `TESTING_STRATEGY.md` requires.

**Behavior-first per `test-guard`:** the frontend cells assert behavior through real DTOs
and the real builder (no mocked snapshot shapes), the api spec asserts the wire request
(verb + URL + body), and no test is written for framework guarantees — an input binding is
not a test subject.

## 8. Risk register

| Risk | Why it is real | Mitigation |
|---|---|---|
| The clicked position ≠ the computed position | Duplicate `OrderValue`s are reachable today (DRIFT-3) and the doors template reads without a tie-break (`EfAbwabDoorsWriter.cs:185`) | §4.2-4: order by `(OrderValue, Id)`, the reader's own order; recorded as a deliberate deviation in the area README (T801), debt rows F1/F2 |
| A partially-applied resequence leaves a broken sequence | Every reorder rewrites every live row | §4.2-8: each resequenced sibling's UPDATE carries its own `xmin` check, so a concurrent sibling write 409s the whole operation; a save is one `SaveChangesAsync`, one transaction |
| The 409 storms the user because every sibling's token went stale | One reorder stales every section's `version` | §4.1-3: the command routes through `AbwabWriteController`, whose `handleSuccess` calls `refreshAndRebind()` (`:211-236`), and the modal reads the live row at submit time (`:134-139`). **The failure mode is skipping that route, not the resequence itself** |
| Escape closes the whole modal instead of cancelling the edit — and, post-Slice-E, writes `modal=sections-closed` | The dialog binds `(keydown.escape)` on its `<section>` (`html:14`) and the editor is a descendant | §4.2-9: `stopPropagation()` on the editor's keydown, with a dedicated spec cell (T603) and a browser cell (T902) |
| **A 409 leaves the client stuck: the immediate resubmit 409s again** | `refreshAndRebind()` runs from `handleSuccess` only; `handleFailure` sets the announcement and returns (`abwab-write.controller.ts:211-248`) | **Pre-existing and shared** — `renameSection`/`deleteSection` behave identically today. Documented in T601's retry note and observed in T902; **deliberately not fixed here**, because the fix is in the shared failure path and would change every abwab write's behavior in a slice scoped to two items |
| The editor is unusable because focus never reaches the input | Neither the modal's rename nor the tree's order editor focuses its swapped-in input; the tree's e2e passes only because Playwright's `fill()` focuses for it | §T602 names the mechanism concretely (`#orderInput` + `viewChild` + `afterNextRender`) and rejects the microtask shape with the reason; T902 walks click-then-type and click-then-click-away with no intermediate click |
| Item 19's badge is read as contradicting item 17's shipped stat | «12» and «3» sit inches apart and neither self-explains | §4.2-13: distinct nouns in the accessible layer (`أبواب` vs `أبواب رئيسية`), a README sentence forbidding an assertion that they agree, and the builder-spec cell pinning the non-identity |
| Someone "fixes" the badge by reusing `doorsInScopeCount` | It is already on the wire and looks like the same number | §4.2-13 + T801's verify-only pass over `Reads/Abwab/README.md:40-51`, which already records the all-depths semantics |
| The zero badge fights the selected-state rule | `.qd-tabs__tab.qd-is-selected .qd-tabs__count` sets `background`/`color` | §4.2-11: the `--empty` modifier is **opacity only**, so it composes in both states; the colour-override alternative is flagged and not built |
| A future cache keys the snapshot without accounting for a whole-table resequence | Slice I owns caching, and a section reorder invalidates every section row at once | **Flagged, not designed for** (§3). One line for Slice I to read: any snapshot cache must treat a section reorder as a **table-wide** invalidation, exactly like a `Global` door reorder, not a single-row one. Nothing in this slice is shaped around that |
| The endpoint count drifts in two READMEs | "twenty write endpoints" appears in both `Controllers/README.md:8-9` and `Writes/Abwab/README.md:7-9` | T801 edits both in one task; T903's grep sweep for `"twenty"` is the backstop |
| The route ships without a catalog entry | The whole reason §10 exists | T205 puts the entry in the **same commit** as the route, and Phase 3 exists solely to run the tier — so a missing entry fails the phase's own commit, and a skipped run is a missing phase rather than a forgotten flag |
| A migration gets generated "just in case" | Reorder features usually imply a column | §3 + §4.1-1: the column exists; Backend CLAUDE.md forbids generating one unasked. Zero files under `Migrations/` may appear in this branch's diff |

## 9. Obligations checklist (all must be true at close)

- [ ] Baseline recorded (T101) before any change — both stacks, with the `Tests.Smoke.Data` RAN/SKIPPED statement — and the closing Tier C compared against it (T901)
- [ ] `POST api/abwab/sections/{id:int}/order` ships with `{ position, version }`, four outcomes, and the exact status mapping in §6/T204
- [ ] `ReorderAsync` resequences **every live section** to `1..N`, reads in `(OrderValue, Id)`, pins the client token via `OriginalValue`, and saves through `SaveTranslatingConcurrencyAsync`
- [ ] No `AbwabReorderScope`, no `Enum.IsDefined` guard, no `InvalidScope`/`ScopeNotApplicable` variants — narrowing recorded as deliberate
- [ ] `AbwabInvalidPositionException` reused, not re-declared; two new Arabic `ApiMessages` constants; one `AddScoped`
- [ ] `SmokeRouteCatalog` entry lands in the **same change**, with `{id:int}`, `Method = HttpMethod.Post`, `ParityOnly = true`, and a `DerivedStatus` derived from the outcome switch
- [ ] Route-smoke tier ran; evidence states whether `QuranDashboard.Tests.Smoke.Data` RAN or SKIPPED — at baseline **and** at close
- [ ] `check-api-contract` clean; `reorder-section-body.ts` generated, never hand-edited
- [ ] The reorder routes through `AbwabWriteController` — shared 409 policy, shared refresh-after-write; no second dispatch path
- [ ] Modal editor: Enter commits, blur cancels, Escape cancels **and the modal stays open**; live `version` read at submit; four states rendered; a real `<button>` trigger with an Arabic accessible name; an open edit is not dirty
- [ ] `rootCountBySectionId` is pure, in the builder, specced — including the Σ-can-be-less-than-total cell
- [ ] Badges render on every tab via the call-site's `.qd-tabs__count`; `qdTab` untouched; always rendered, dimmed at zero with opacity only; Latin digits; «كل الأبواب» shows total live roots
- [ ] The badge and item 17's stat are distinguishable in the accessible layer; no test or doc asserts they agree
- [ ] Archive view unchanged — no tab strip, therefore no badge; stated in the README
- [ ] Five docs amended in the same change (two backend READMEs, `Reads/Abwab/README.md` verified, `features/abwab/README.md`, §17) + the three `TESTING_DEBT.md` rows
- [ ] Zero migration files; zero edits to `EfAbwabDoorsWriter.cs`; zero edits to item 17's helpers; zero template/navbar/cache work
- [ ] Test delta within §4.2-17's declared direction; zero tests removed; zero new spec files; fork cap preserved; no e2e cited as a gate
- [ ] No planning folder deleted or repointed; PR targets `dev`; no `dev → main`

## 10. Execution note

One light branch off `dev`: `ux-slice-f-sections` (§4.2-18). **Commits are per phase — they
are the bisection mechanism**, so a phase never lands half-committed and no commit leaves
the tree failing a gate that phase owns. Phases run in order; the ordering *is* the
discipline: backend contract before its gate, gate before regeneration, regeneration before
any consumer, the write path before the editor that calls it, item 18 complete before item
19 starts, docs after the behavior they describe is final.

**Every commit is green, including Phase 2's** — the `SmokeRouteCatalog` entry ships inside
the same commit as the route (T205), so `SmokeCoverageParityTests` never sees a registered
route without one and Backend CLAUDE.md §10's "in the same change" holds per-commit. Phase 3
is still its own phase: cataloguing the route and *running the tier* are separate
obligations, and separating them is what keeps the run from being skipped.

| Phase | Title | Items | Tasks |
|---|---|---|---|
| 1 | Baseline and record | — | T101–T102 (2) |
| 2 | Backend: contract, writer, wiring, catalog entry | 18 | T201–T205 (5) |
| 3 | The route gate | 18 | T301 (1) |
| 4 | Contract regeneration | 18 | T401 (1) |
| 5 | Frontend write path | 18 | T501–T502 (2) |
| 6 | The sections-modal order editor | 18 × D-12 | T601–T603 (3) |
| 7 | Item 19: the tab count badges | 19 | T701–T703 (3) |
| 8 | Docs true again | 18, 19 | T801–T802 (2) |
| 9 | Verification and close-out | — | T901–T903 (3) |

**22 tasks. Guard: under 30 — one slice, no split** (seam recorded in §0 in case execution
learns otherwise).

## 11. Stop conditions

Stop and ask rather than deciding in flight if:

- The doors reorder contract turns out to differ from §5's rows in a way the mirror cannot
  absorb (a second order space for sections, a scope requirement, a different token shape).
- `ReorderAsync`'s resequence turns out to need a two-phase write after all — i.e. a unique
  index on `order_value` is discovered in a migration this plan did not read.
- `check-api-contract` reports staleness that regeneration does not fix, which would mean
  the exporter and the committed spec disagree about something outside this slice.
- Item 19's root-only count cannot be derived without a backend change — which would
  contradict §4.2-12 and turn a component item into a contract item.
- Any planning-artifact deletion appears necessary — it is not; it is deferred to the
  post-Slice-I pass, without exception.
- The `--empty` opacity treatment reads wrong on the **selected** tab in either theme, in
  which case the colour-override alternative in §4.2-11 is a decision for the user, not a
  fix to apply.
