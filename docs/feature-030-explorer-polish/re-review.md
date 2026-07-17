# Feature 030 Explorer Polish — Fix-Commit Engineering Re-review

- **Review date:** 2026-07-17
- **Branch:** `restyle/flat-green-light`
- **Fix range:** `b633559c82fe..2dbf3f783ea7` (`origin/restyle/flat-green-light..HEAD`)
- **Commits reviewed:** 13 (1 original review, 3 backend fixes, 9 frontend fixes)
- **Follow-up correction:** H1/H2 fix requested after the first re-review verdict
- **Original findings:** `docs/feature-030-explorer-polish/review.md`
- **Mode:** engineering re-review plus the requested H1/H2 correction

## Final verdict

# PASS WITH NOTES

All thirteen original findings are **CLOSED**. The follow-up correction removes
URL/session-ledger ownership inference, validates provenance against the live
history entry, and repairs Restore-derived base transitions before the final
Mushaf replacement. The previously blocking same-URL and Restore sequences now
have exact behavioral coverage.

No new regression was found in navigation, Quran rendering/data, ordering,
backend/frontend contracts, routes, DI, or API envelopes.

The full backend and frontend suites are green. The backend build is warning-free.
The frontend build succeeds but still emits two style-budget warnings in files
unchanged by this fix range, so it is successful but not literally warning-free.
`DetailOverlayHistoryService` is now 432 lines, down from 477 and below its
600-line hard limit, but it remains above the 400-line soft review threshold.
Those two pre-existing build warnings and the soft-threshold note are why the
verdict is **PASS WITH NOTES** rather than an unqualified PASS.

The user-confirmed 768/390, light/dark, RTL browser/keyboard matrix for M1, M2,
and M4 is accepted as execution evidence and was not repeated here.

## Follow-up closure evidence

### H1 — CLOSED: history ownership is entry-specific

The URL/session ledger and document-load reconstruction were deleted. The only
ownership source is the current entry's `qdDetailNav` marker:

- the provenance module now contains only the typed marker reader and stable stack
  hash: `detail-overlay-provenance.ts:17-53`;
- open entries without matching proof are seeded:
  `detail-overlay-history.service.ts:265-308`;
- `currentEntryProvenance()` rejects a marker unless both `baseSignature` and
  `stackHash` match the live URL state:
  `detail-overlay-history.service.ts:411-421`;
- dialog Back additionally requires `push`/`seed` and the exact live parent hash:
  `detail-overlay-history.service.ts:123-135,327-336`;
- provenance-preserving top replacement also starts from validated current-entry
  proof: `detail-overlay-history.service.ts:338-365`.

The same-session collision test now seeds a two-frame deep link, returns to its
bare base, revisits the identical URL, asserts two fresh prefix entries, and
proves dialog Back lands on the one-frame parent:

- `detail-overlay-history.service.spec.ts:293-326`.

Mismatched base and stack hashes are data-driven negative cases:

- `detail-overlay-history.service.spec.ts:328-360`.

Reload-after-replace coverage now performs the real production ordering available
to the unit harness: it starts the coordinator before initial navigation, applies
an actual top-frame replacement, tears down the TestBed, recreates Router and
coordinator, restores only the browser entry's URL/state, and asserts zero
seeding plus exact provenance/frame retention:

- `detail-overlay-history.bootstrap.spec.ts:35-133`.

This is not a real browser process reload, but it exercises Angular's installed
initial-navigation state-preservation path with fresh service/router instances.
The user-provided browser matrix remains the real-browser evidence.

### H2 — CLOSED: Restore-derived Mushaf Back paths converge

Before an open-stack base replacement, the coordinator accepts the current entry
only when it proves an adjacent parent. Restore/fallback entries with a
multi-frame stack re-materialize their URL prefixes first; the final Mushaf
navigation still replaces the top entry:

- base transition: `detail-overlay-history.service.ts:173-205`;
- prefix repair and parent proof:
  `detail-overlay-history.service.ts:310-336`.

Ordinary Restore remains a push whose browser Back returns to the retained closed
entry:

- implementation: `detail-overlay-history.service.ts:138-154`;
- unchanged contract tests:
  `detail-overlay-history.service.spec.ts:191-233`.

The new compound test executes
**Close → Restore → Mushaf replacement → browser Back → Forward → dialog Back**
and asserts both controls reach the exact open Words root parent:

- service contract test:
  `detail-overlay-history.service.spec.ts:479-519`;
- real overlay/Mushaf integration:
  `entity-detail-overlay-ayah-continuity.spec.ts:323-377`.

The steady-state core README now records entry-bound ownership, same-URL reseeding,
reload idempotence, and Restore-derived prefix repair:

- `Frontend/quran-dashboard-ui/src/app/core/README.md:27-49`.

## Original finding closure matrix

| Finding | Status | Re-review conclusion |
|---|---|---|
| C1 | **CLOSED** | Both request slots are consolidated in one lifecycle, every callback is generation-guarded, and all five controllers carry the required four stale-detail race cases. |
| H1 | **CLOSED** | Ownership is attached to and validated from the current history entry; markerless same-URL revisits reseed, mismatched proof fails closed, and a fresh Router/coordinator reload harness proves top-replacement idempotence. |
| H2 | **CLOSED** | Restore-derived multi-frame base transitions repair the prefix chain before the final replace; service and integration tests assert browser/dialog Back reach the exact same open Words parent. |
| H3 | **CLOSED** | All five former hard-limit breaches are below their limits, and the splits preserve routes, DI, contracts, and behavior. |
| M1 | **CLOSED** | Sort-header arrow keys are excluded without disabling row-chip navigation. |
| M2 | **CLOSED** | Exactly one focus trap remains enabled, and pop/push focus has deterministic restoration/fallback. |
| M3 | **CLOSED** | All five detail surfaces expose one retry action backed by complete-identity retry behavior. |
| M4 | **CLOSED** | Restored-state and pagination geometry are reserved from real tokens; the selected badge mirrors the loaded trigger; known zero is distinct from unknown. |
| M5 | **CLOSED** | The unsourced real-looking Uthmani fixture was removed from the affected test fixtures and replaced with explicit synthetic text. |
| M6 | **CLOSED** | Real-PostgreSQL tests assert exact ascending and descending order for every count arm and exercise the reachable final Unique Words ID tie-break. |
| L1 | **CLOSED** | Unsafe IDs/pages are rejected with `Number.isSafeInteger`, including overflow and boundary tests. |
| L2 | **CLOSED** | Retained-closed URLs do no hidden adapter work; restore hydrates once and normal close/restore retains the loaded adapter. |
| L3 | **CLOSED** | Canonical sort logging, README drift, whitespace, and diff hygiene are corrected. |

## Closure evidence

### C1 — stale summary/detail races

The shared lifecycle owns both subscriptions. `beginTransition()` cancels both and
advances a monotonic generation; every callback can verify its token:

- `Frontend/quran-dashboard-ui/src/app/features/words/state/detail-request-lifecycle.ts:21-69`

All five controllers use it for complete-identity transitions and guard summary
success/error plus detail success/error callbacks:

- Roots:
  `roots-detail.controller.ts:322-441`
- Lemmas:
  `lemmas-detail.controller.ts:385-518`
- Stems:
  `stems-detail.controller.ts:385-516`
- Word Types:
  `word-types-detail.controller.ts:190-298`
- Unique Words:
  `unique-words-drilldown.controller.ts:238-441`

The stale-detail matrices explicitly cover an old detail callback:

- while the new summary is pending;
- after new-summary success;
- after new-summary 404;
- after new-summary transport failure.

Evidence:

- Roots: `roots-detail.controller.spec.ts:196-247`
- Lemmas: `lemmas-detail.controller.spec.ts:226-277`
- Stems: `stems-detail.controller.spec.ts:227-278`
- Word Types: `word-types-detail.controller.spec.ts:277-327`
- Unique Words: `unique-words-drilldown.controller.spec.ts:118-168`

The four controller/view-loader fixtures manually invoke captured handlers, so
they strongly pin the generation guard but do not independently pin the
`Subscription.unsubscribe()` call. Unique Words uses a real subject. The
production lifecycle visibly performs both unsubscriptions, so this is a
test-hardening note rather than a closure blocker.

### H3 — architecture thresholds and split integrity

Raw current line counts:

| Former breach | Hard limit | Current primary | Extracted cohesive file |
|---|---:|---:|---:|
| `RootsController.cs` | 300 | 127 | `RootsController.Details.cs`: 207 |
| `WordTypesController.cs` | 300 | 216 | `WordTypesController.Details.cs`: 133 |
| `EfWordTypesReader.Sql.cs` | 600 | 507 | `EfWordTypesReader.GroupedTable.Sql.cs`: 139 |
| `word-types-explorer-page.component.ts` | 400 | 385 | `word-types-detail-panel.view-model.ts`: 108 |
| `mushaf-reader.facade.ts` | 600 | 582 | `mushaf-study-source-catalog.store.ts`: 61 |

The backend controller primary partials retain the API attributes, route, DI
constructor, and defaults; the extracted partials retain the original action
signatures. The grouped SQL block was moved into the same partial class with
compiler-known ordering arms. The Word Types view-model extraction is pure, and
the root-scoped Mushaf catalogue store preserves the facade's public signals and
load-once behavior.

No generated model, OpenAPI, API-reference, application-abstraction, frontend
data-access, `ApiResponse`, or `PagedResult` file changed in the fix range. Both
builds and both full suites compile/exercise the new imports, DI, routes, actions,
and queries.

### M1–M6

- **M1:** column headers and sort buttons are blocked at
  `explorer-table-keydown.ts:104-120`; focused-header no-op and row-chip navigation
  are both asserted at `roots-table.component.spec.ts:222-269`.
- **M2:** the shell tracks focus per depth and restores the surviving opener or
  Close/heading fallback at
  `detail-modal-shell.component.ts:68-127,148-211`. All five underlying drawers
  compute `drawerTrapEnabled = !detailOverlayHistory.isOpen()`. Active-element
  tests are at `detail-modal-shell.component.spec.ts:204-279`; exactly one enabled
  trap is asserted at `app.nested-layers.spec.ts:225-243`.
- **M3:** the shared state component permits exactly one error recovery action at
  `shared/ui/state/state.component.ts:14-18,31-34` and
  `state.component.html:7-15`. Every adapter delegates to
  `retryCurrentIdentity()`. Summary-error, detail-error, and no-identity cases are
  covered in Roots `:250-300`, Lemmas `:280-330`, Stems `:281-331`, Word Types
  `:331-387`, and Unique Words `:198-260`.
- **M4:** the always-mounted Unique Words restored slot is at
  `unique-words-page.component.html:80-111` and its real control-size reservation
  at `unique-words-page.component.scss:5-20`. Shared pagination geometry is
  derived at `_tokens.scss:129-158` and consumed by the two reservation slots.
  The Word Type skeleton badges only the selected trigger at
  `word-type-filter.component.html:116-135`, with restored-selection coverage at
  `word-type-filter.component.spec.ts:320-344`. Similar/Mutashabihat expected
  counts are `number | null`, and tests distinguish fallback `null` from known
  empty `0` at `similar-ayahs-card.component.spec.ts:171-188` and
  `mutashabihat-groups-card.component.spec.ts:437-465`. The supplied real-browser
  matrix confirms the geometry.
- **M5:** repository search finds no `ٱلْكِتَـٰبُ` in frontend `*.spec.ts` files.
  The affected adapter/controller fixtures use explicit values such as
  `SYNTHETIC_WORD_TEXT = 'كلمة-اختبار'`; the new backend ordering fixtures name
  themselves synthetic/non-Quranic and are transaction-rolled back.
- **M6:** Unique Words asserts exact IDs for all six count column/direction tokens,
  Mushaf ties, and the final ID rung at
  `UniqueWordsOrderingContractTests.cs:70-120`, through the real handler and
  PostgreSQL transaction at `:122-160`. Word Types asserts exact IDs for Words and
  the shared grouped ordering arm across all six count tokens at
  `WordTypesOrderingContractTests.cs:79-149`, through the real reader/PostgreSQL at
  `:151-181`.

### L1–L3

- **L1:** `parsePositiveInt()` requires decimal syntax plus
  `Number.isSafeInteger(parsed) && parsed > 0` at
  `detail-overlay-url-codec.ts:63-75`. Tests cover values just above
  `MAX_SAFE_INTEGER`, very long digit runs, unsafe IDs/pages, stack truncation, and
  the exact safe boundary at `detail-overlay-url-codec.spec.ts:87-119,147-160`.
- **L2:** all five adapters are gated by `@defer (when isOverlayOpen())` at
  `entity-detail-overlay-host.component.html:21-57`. Tests prove a fresh
  retained-closed URL makes no detail request and that restore → close → restore
  makes no second read at `entity-detail-overlay-host.component.spec.ts:369-453`.
- **L3:** successful Word Types handlers log `sort.CanonicalToken()` at
  `GetWordTypeRowsHandler.cs:68-80` and `GetWordTypeTableHandler.cs:86-99`; the
  reader README names `WordTypeSortSpec.Natural(...)`; all diff checks pass.

## Regression and integrity review

### Quran-data and identity integrity

- C1 now prevents a late previous-identity summary or detail callback from
  populating the active panel.
- No production Quran import, migration, staged resource, seed, text renderer,
  normalization, matched-word, or word-order implementation changed in this fix
  range.
- M4 changes loading-state geometry and the unknown/known-empty distinction; it
  does not alter loaded Quran text or counts.
- M6's added rows are unmistakably synthetic structural data, isolated in rolled
  back PostgreSQL transactions.

### N8 ordering

The allowlists and canonical token parsers are unchanged. The split Word Types
reader retains constant-only SQL arms and deterministic ties:

- `EfWordTypesReader.Sql.cs:411-428`
- `EfWordTypesReader.GroupedTable.Sql.cs:75-98`

Unique Words production ordering was not changed by the fix range. The new tests
now pin count column, direction, Mushaf, and ID order exactly.

### Backend/frontend contract

- Controller actions moved between partial files without signature, route, or
  envelope changes.
- No backend response DTO/application-abstraction or frontend generated/data-access
  contract changed.
- `ApiResponse` and `PagedResult` remain intact.
- Full compilation and tests found no broken imports, DI registrations, routes, or
  endpoints.

### Clean-code guard

**Verdict: PASS WITH NOTE.**

Positive evidence:

- C1 replaces five divergent request lifecycles with one small, named primitive.
- H3 splits are cohesive rather than arbitrary line-count shuffles.
- M4 derives geometry from shared tokens instead of duplicating magic sizes.
- H1 removes the storage ledger, navigation-timing branch, and fabricated
  provenance path instead of layering another heuristic over them.
- H2 keeps prefix materialization in the existing history coordinator and reuses
  the same provenance predicate used by dialog Back.
- No new production hardcoded Quran values, swallowed operational errors, debug
  code, or speculative abstraction was found.

`DetailOverlayHistoryService` fell from 477 to 432 lines. It remains above the
400-line state-service soft review threshold but is below the 600-line hard
limit; the remaining methods form one URL/history state machine, so no further
split is required for this focused correction. `stackHash` is now an active
ownership check rather than a write-only field.

## Test Guard review

**Verdict: PASS.**

The C1, focus, retry, safe-integer, geometry-structure, and PostgreSQL ordering
tests remain behavior-oriented and source-safe. The follow-up history tests use
the real Angular Router plus `SpyLocation` boundary, assert observable URL,
visibility, stack, and history outcomes, and cover:

1. the remembered two-frame same-URL collision;
2. base-signature and stack-hash rejection;
3. fresh Router/coordinator bootstrap after a real top-frame replacement;
4. exact browser/dialog parent convergence after
   **Close → Restore → Mushaf base replacement**;
5. unchanged ordinary Restore behavior.

Recommended nonblocking hardening: add a direct `DetailRequestLifecycle` test that
asserts both tracked subscriptions are unsubscribed on every generation change.

## Verification

| Gate | Result |
|---|---|
| Backend build | **PASS** — 0 warnings, 0 errors |
| Backend full suite | **PASS** — 1,574 passed, 0 failed, 0 skipped; 6m 7s |
| Frontend production build | **PASS with existing warnings** — output generated successfully; two style-budget warnings |
| Frontend full suite | **PASS** — 154 files, 1,809 tests passed |
| Detail-overlay focused suite | **PASS** — 13 files, 151 tests passed |
| Fix-range `git diff --check` | **PASS** |
| Working-tree `git diff --check` | **PASS** |
| Report-file `git diff --no-index --check` | **PASS** |
| Staged `git diff --cached --check` | **PASS** |
| Browser/keyboard matrix | **CONFIRMED BY USER** — accepted, not rerun |

Frontend build warnings:

- `selected-word-section.component.scss`: 4.32 kB vs 4.00 kB budget.
- `selected-ayah-section.component.scss`: 4.38 kB vs 4.00 kB budget.

Neither stylesheet nor `angular.json` changed in
`origin/restyle/flat-green-light..HEAD`; these warnings are not fix-round
regressions.

The verification numbers above are fresh reruns after the follow-up correction;
the full frontend suite preceded only formatting/comment normalization, and the
focused suite plus production build were rerun afterward.

## Merge recommendation

The fix round is review-complete. H1 and H2 are closed by the follow-up correction, all
original findings are closed, and no new blocking regression was found. The
branch is suitable for focused commits and a normal push; the two pre-existing
frontend budget warnings remain documented above.

The owner requested commit and push after verification. No PR action is part of
this review.
