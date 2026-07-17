# `restyle/flat-green-light` — Engineering and Performance Review

- **Review date:** 2026-07-17
- **Branch:** `restyle/flat-green-light` at `b633559`
- **Base:** merge-base with `dev`, `6f457a003212a3625100eb799a67b44ffd56329c`
- **Scope:** the flat-green restyle, Feature 029, Feature 030, and the later M1/M2
  corrections that are part of the reviewed branch
- **Mode:** read-only review; this report is the only review-owned source/document
  change
- **Overall verdict:** **BLOCKED**
- **Backend performance verdict:** **PASS WITH NOTES**
- **Frontend performance verdict:** **PASS WITH NOTES**

## Executive summary

The branch builds and its full automated suites pass, and the N8 production sort
implementation is allowlisted, deterministic, scope-preserving, cache-aware, and free
of request-text SQL interpolation. The shared ayah card also remains presentation-only,
the Mushaf Word Type adapter uses the locked identity, no Quran rendering or
normalization logic moved into shared UI, and the current M1 word-hover ladder has been
measured in both themes.

The branch is nevertheless not merge-ready. The most serious defect is an identity
race in all five new detail controllers: a prior detail request remains live while a
new entity summary loads, so a late response can render one entity's Quran/detail data
under another entity's URL and title. The global history implementation also has two
locked-contract violations, including a same-session deep-link revisit that can send
Back to unrelated history. These are correctness issues that passing unit tests do not
cover.

Finding count:

| Severity | Count |
|---|---:|
| Critical | 1 |
| High | 3 |
| Medium | 6 |
| Low | 3 |

## Critical findings

### C1. A stale detail response can overwrite a newly selected entity

- **Evidence:** Full identity changes unsubscribe only the summary subscription in
  `Frontend/quran-dashboard-ui/src/app/features/words/state/roots-detail.controller.ts:123-125`,
  `lemmas-detail.controller.ts:130-132`, `stems-detail.controller.ts:130-132`,
  `word-types-detail.controller.ts:144-147`, and
  `unique-words-drilldown.controller.ts:202-220`. The previous detail request is not
  cancelled until the replacement summary succeeds and calls the next load:
  `roots-detail.controller.ts:330-409`,
  `lemmas-detail.controller.ts:397-482`,
  `stems-detail.controller.ts:397-482`,
  `word-types-detail.controller.ts:197-258`, and
  `unique-words-drilldown.controller.ts:255-344`.
- **Impact:** While entity B's summary is pending, a late detail callback from entity A
  can populate B's panel, or overwrite B's `notFound`/error state. This can visibly
  associate Quran ayahs, words, counts, or morphology with the wrong identity. That is a
  Quran-data integrity failure even though it does not mutate the database.
- **Ownership:** **Branch-owned**, introduced by Feature 029's route-independent
  controller refactor.
- **Smallest safe remediation:** Cancel both summary and detail/drilldown subscriptions
  immediately on every complete-identity transition, and guard every callback with the
  complete frame identity or a monotonically increasing request generation. Add tests
  where the old detail responds while the new summary is pending and after the new
  summary returns success, 404, and transport error.
- **Test gap:** Existing stale-response tests cover the summary request only:
  `roots-detail.controller.spec.ts:104-130`,
  `lemmas-detail.controller.spec.ts:134-160`,
  `stems-detail.controller.spec.ts:135-161`, and
  `word-types-detail.controller.spec.ts:162-192`.

## High findings

### H1. URL-only session seeding can make Back leave the overlay/app

- **Evidence:** `DetailOverlayHistoryService` trusts `kind: 'seed'` and calls browser
  Back at
  `Frontend/quran-dashboard-ui/src/app/core/navigation/detail-overlay/detail-overlay-history.service.ts:138-148`.
  Deep-link seeding stores only a URL-wide session flag at `:288-320` and `:397-410`.
  A later visit to the identical shared URL in the same tab therefore fabricates seed
  provenance without proving that the immediately previous history entry is the parent
  prefix. Conversely, `replaceTopFrame()` changes the URL at `:113-124` but preserves
  only `history.state` at `:335-361`; reloading the replaced URL can miss the old
  URL-keyed flag and duplicate the prefix chain.
- **Impact:** Dialog Back can leave to an unrelated or external predecessor instead of
  popping the modal stack. Reload after a top-frame replacement can duplicate history
  entries. Both violate the locked “never exits to unrelated history” rule in
  `docs/feature-029-floating-detail-navigation-ui/plan.md:222-236`.
- **Ownership:** **Branch-owned**, Feature 029.
- **Smallest safe remediation:** Make seed idempotence entry/chain-specific rather than
  URL/session-global. Preserve or recover a unique chain marker tied to the current
  history entry and re-seed whenever parent adjacency cannot be proved. Add tests for
  external predecessor → seed → navigate away → same-URL revisit, and reload after a
  top-frame replacement.
- **Misleading test:** The current reload test at
  `detail-overlay-history.service.spec.ts:292-306` reloads the same current entry; it
  does not model a fresh same-session revisit from unrelated history.

### H2. Ayah continuity makes dialog Back and browser Back diverge

- **Evidence:** Ayah base navigation rewrites provenance to `kind: 'replace'` in
  `Frontend/quran-dashboard-ui/src/app/core/navigation/detail-overlay/detail-overlay-history.service.ts:189-218`.
  The tests explicitly expect browser Back to restore the Words parent at
  `detail-overlay-history.service.spec.ts:352-382`, while dialog Back remains on the
  Mushaf and removes only the frame at `:384-402`. The integration test repeats that
  divergence in
  `entity-detail-overlay/entity-detail-overlay-ayah-continuity.spec.ts:295-306`.
- **Impact:** Two controls that the locked plan says must converge produce different
  bases and histories. This breaks the user's scholarly navigation context and makes
  the URL-authoritative state machine non-authoritative in practice.
- **Ownership:** **Branch-owned**, Feature 029 B7/B8.
- **Smallest safe remediation:** Preserve the immediate parent's base signature and
  provenance across the Mushaf base replacement so both Back mechanisms resolve to the
  same parent frame and its historical base. Replace the divergence assertions with
  convergence assertions.
- **Contract:** `docs/feature-029-floating-detail-navigation-ui/plan.md:227-234` and
  `:274-282`.

### H3. The branch crosses or worsens mandatory hard structure thresholds

- **Evidence:** The architecture rules define 300 lines as the controller hard limit,
  600 as the read-service hard limit, and require an immediate cohesive split:
  `Backend/.architecture/BACKEND_STRUCTURE.md:407-423`, `:452-464`, and `:489-500`.
  The Angular component hard limit is 400 lines with the same stop/split rule:
  `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md:77-94` and
  `:163-174`.

  | File | Exact base | Reviewed branch | Ownership |
  |---|---:|---:|---|
  | `Backend/api/QuranDashboard.Api/Controllers/Words/RootsController.cs` | 281 | 311 | Branch creates the hard breach |
  | `Backend/api/QuranDashboard.Api/Controllers/Words/WordTypesController.cs` | 312 | 332 | Pre-existing hard breach, branch worsens it |
  | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs` | 606 | 631 | Pre-existing hard breach, branch worsens it |
  | `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts` | 404 | 442 | Pre-existing hard breach, Features 029/030 worsen it |
  | `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts` | 617 | 620 | Pre-existing 600-line state/facade hard breach, branch worsens it |

- **Impact:** This is a mandatory repository gate, not a cosmetic line-count target.
  The duplicated detail-transition lifecycle has already propagated C1 across five
  controllers, demonstrating the maintainability risk the thresholds are intended to
  catch.
- **Soft-threshold evidence:** The same state/facade rule sets a 400-line review
  threshold at `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md:136-148`.
  Feature 029 creates `detail-overlay-history.service.ts` (439), the Roots detail
  controller (432), Lemmas/Stems detail controllers (507 each), and the Unique
  drilldown controller (413) above that threshold. The branch also grows
  `word-types-explorer.facade.ts` from 448 to 452. These files are cohesive enough not
  to be separate findings, but their repeated lifecycle code is not justified by the
  observed C1 failure.
- **Smallest safe remediation:** Split list/detail endpoint groups in the two
  controllers, extract the N8 Word Types ordering/query fragment into a focused reader
  partial, move one cohesive Word Types page responsibility into its existing
  facade/controller or a focused child component, and extract one Mushaf workflow from
  the oversized facade. Consolidate the shared complete-identity cancellation pattern
  without performing an unrelated broad rewrite.

## Medium findings

### M1. Sort-header arrow keys invoke row/detail navigation

- **Evidence:** Four table roots receive bubbled keydown events at line 6 of the Roots,
  Lemmas, Stems, and Unique Words table templates. The shared handler resolves arrow
  keys before checking the target, and its blocker excludes only pagination and
  `input`/`select`/`textarea`:
  `Frontend/quran-dashboard-ui/src/app/features/words/utils/explorer-table-keydown.ts:19-30`
  and `:104-115`. With a selected row/current detail column, it calls
  `preventDefault()` and emits a row/column target at `:38-68`. N8's new sort buttons
  are descendants of those table roots, for example
  `roots-table/roots-table.component.html:130-152`.
- **Impact:** A keyboard user focusing a sortable header can press an arrow key and
  unexpectedly change the selected detail/row. The header action and the table's
  domain-specific row navigation interfere.
- **Ownership:** **Branch-owned**, Feature 030 N8. Word Types is unaffected because its
  table does not install the row-navigation handler.
- **Smallest safe remediation:** Exclude
  `.qd-explorer-table__sort-button`/column-header-originated events in the shared
  blocker. Do not block every button because arrow navigation from row count chips is
  intentional. Add an integration test with a focused sort button, selected row, and
  active detail column.

### M2. The global modal does not satisfy its focus-management contract

- **Evidence — competing traps:** The global shell installs a trap at
  `Frontend/quran-dashboard-ui/src/app/shared/ui/detail-modal-shell/detail-modal-shell.component.html:1-14`,
  while the underlying mobile drawers keep unconditional traps at
  `root-details-panel.component.html:100-106`,
  `lemma-details-panel.component.html:99-105`,
  `stem-details-panel.component.html:99-105`,
  `word-type-details-panel.component.html:99-105`, and
  `word-drilldown-modal.component.html:107-113`. `app.ts:18-19` makes the shell inert
  but does not disable the underlying CDK traps.
- **Evidence — lost focus:** The Back button exists only for depth greater than one at
  `detail-modal-shell.component.html:16-26`. Focus restoration runs only for
  open → closed at `detail-modal-shell.component.ts:76-85`. Popping depth 2 → 1
  therefore destroys the focused Back button without choosing a new focus target.
- **Impact:** Two active focus traps can compete on mobile, and final Back can leave
  focus on the document instead of inside the still-open dialog. Both are accessibility
  failures in a central navigation flow.
- **Ownership:** **Branch-owned**, Feature 029. The plan explicitly requires only the
  top trap to remain active and opener/fallback focus restoration at
  `docs/feature-029-floating-detail-navigation-ui/plan.md:286-299` and `:336-345`.
- **Smallest safe remediation:** Disable/suspend the underlying drawer trap whenever
  the global overlay is open. After a pop, restore the connected invoking link when
  possible, otherwise focus Close or the dialog heading. Add active-element and
  exactly-one-enabled-trap tests.

### M3. Modal transport errors are sticky and expose no retry action

- **Evidence:** The five overlay error branches render text only:
  `root-detail-overlay-adapter.component.html:100-103`,
  `lemma-detail-overlay-adapter.component.html:105-108`,
  `stem-detail-overlay-adapter.component.html:105-108`,
  `word-type-detail-overlay-adapter.component.html:36-39`, and
  `word-drilldown-modal.component.html:27-29`. Controllers short-circuit identical
  complete identities, while Close/Restore retains the same stack and component, so
  reopening does not naturally issue a new request.
- **Impact:** A transient network/server failure leaves the retained detail unusable
  until the user changes identity or reloads the page. Loading/error/not-found states
  exist, but recovery does not.
- **Ownership:** **Branch-owned**, Feature 029.
- **Smallest safe remediation:** Add one accessible Arabic retry action backed by a
  controller `retryCurrentIdentity()` path that reuses the current complete frame.
  Test summary error → retry → success and detail error → retry → success.

### M4. N3's zero-outer-layout-shift acceptance criterion remains incomplete

- **Evidence — acknowledged Unique Words residual:** Restored `notFound`/error banners
  remain above the grid at
  `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.html:80-108`,
  because the state closes the drilldown at
  `utils/unique-words-drilldown.state.ts:73-86`. Their insertion still pushes the grid.
- **Evidence — pagination reservation:** Both reserved slots use `2.75rem` at
  `src/styles/_explorer-detail-lists.scss:75-89` and
  `src/styles/_words-explorer-layout.scss:128-135`. A normal button's line,
  vertical padding, and border are about `2.53125rem` at
  `src/styles/_components.scss:50-61`, and pagination adds `0.75rem` top margin at
  `shared/ui/pagination/pagination.component.scss:1-7`; the mounted minimum is about
  `3.28125rem`, leaving roughly `0.53rem` unreserved before wrapping.
- **Evidence — wrapped Word Type toolbar:** Every first-load skeleton trigger includes
  the selected-state badge at
  `word-type-filter/word-type-filter.component.html:117-133`, but loaded markup includes
  it only on the selected trigger at `:23-28`. The toolbar wraps and each trigger has a
  `16rem` basis at `word-type-filter.component.scss:13-29`, so non-selected flex lines
  shrink when data settles.
- **Evidence — zero is treated as unknown:** The child cards mount only after the parent
  study finishes at
  `selected-ayah-section/selected-ayah-section.component.html:155-206`, yet
  `similar-ayahs-card.component.ts:42-54` maps a known zero to three placeholders and
  `mutashabihat-groups-card.component.ts:63-90` maps it to two groups. A real zero
  therefore paints tall loading geometry and collapses to empty.
- **Impact:** Static state flow and CSS geometry indicate block-size changes on these
  paths, especially at wrapped 768/390 widths; the exact visible magnitude was not
  browser-measured in this review. Treating known zero as “unknown” also violates the
  explicit state distinction even though no synthetic Quran content is displayed.
- **Ownership:** The Unique Words behavior is **pre-existing but deliberately left
  incomplete by N3**; the other three are **branch-owned N3 implementation misses**.
- **Smallest safe remediation:** Re-home/reserve the Unique restored state without
  hiding the populated table; size pagination from the real shared control geometry;
  put the hidden badge only in the skeleton's selected trigger; and model expected
  counts as `number | null` so `null` means unknown and `0` means known empty. Verify
  geometry in a real browser at 1440/768/390.

### M5. New tests use untraceable real-looking Uthmani text

- **Evidence:** `ٱلْكِتَـٰبُ` was added directly to synthetic adapter/controller fixtures,
  including
  `root-detail-overlay-adapter.component.spec.ts:53`,
  `lemma-detail-overlay-adapter.component.spec.ts:59`,
  `stem-detail-overlay-adapter.component.spec.ts:60`,
  `unique-detail-overlay-adapter.component.spec.ts:46`,
  `word-type-detail-overlay-adapter.component.spec.ts:32,57`,
  `lemmas-detail.controller.spec.ts:244`,
  `stems-detail.controller.spec.ts:245`, and
  `word-types-detail.controller.spec.ts:273`.
- **Impact:** The fixture looks like authoritative scripture but has no traceable source.
  This violates the Test Guard/Quran-data rule even though it is test-only and does not
  ship to production.
- **Ownership:** **Branch-owned**, Feature 029 tests.
- **Smallest safe remediation:** Replace it with an unmistakably synthetic,
  non-religious placeholder, or load it from and cite an authoritative project fixture.

### M6. N8 database ordering tests leave contract-critical branches unproved

- **Evidence:** The plan requires primary ordering for every column/direction and an
  exercised final Unique Words `Id` tie-break at
  `docs/feature-030-explorer-polish/plan.md:828-849`. Unique Words asserts exact
  occurrences and alpha ordering, but `ayahs`/`surahs` are checked only as a row set at
  `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsSearchSortPagingTests.cs:224-247`;
  the seeded tie test states that `Id` never participates at `:166-183`. Word Types
  tests exact grouped Mushaf/alpha order at
  `WordTypesTableReadTests.cs:136-256`, while count-direction tokens receive row-set and
  total-count checks only at `:490-520`.
- **Impact:** Reversing an `ayahs`/`surahs` SQL arm or removing Unique Words'
  `.ThenBy(Id)` can remain green. Production code currently appears correct, but the
  tests do not enforce the locked ordering contract.
- **Ownership:** **Branch-owned**, Feature 030 N8 test gap.
- **Smallest safe remediation:** Add real-PostgreSQL exact-order assertions for each
  count column/direction in Words, grouped, and Unique views. Add a small synthetic seam
  where equal primary and Mushaf keys reach the final identity tie-break.

## Low findings

### L1. The modal URL codec accepts unsafe integers

- **Evidence:** `parsePositiveInt()` checks only decimal syntax before calling `Number`
  in
  `Frontend/quran-dashboard-ui/src/app/core/navigation/detail-overlay/detail-overlay-url-codec.ts:63-65`.
  Strict parser tests cover zero, negatives, and non-numeric IDs but not values above
  `Number.MAX_SAFE_INTEGER` at `detail-overlay-url-codec.spec.ts:87-106`.
- **Impact:** A very large shared ID/page can round to another integer or become
  non-finite before canonicalization, weakening complete-identity guarantees.
- **Ownership:** **Branch-owned**, Feature 029.
- **Smallest safe remediation:** Require `Number.isSafeInteger(parsed) && parsed > 0`
  and add overflow/unsafe-integer cases.

### L2. A retained-closed shared URL can issue invisible detail requests

- **Evidence:** The host instantiates adapters whenever a stack exists, regardless of
  visibility, at
  `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/entity-detail-overlay-host.component.html:1-55`.
  Each adapter immediately applies its frame and loads summary/detail state.
- **Impact:** A fresh URL containing a retained closed stack can make hidden summary and
  detail HTTP calls. This is real redundant work, although bounded caches and the
  product's roughly three-user load keep server impact low.
- **Ownership:** **Branch-owned**, Feature 029 performance note.
- **Smallest safe remediation:** While closed, load only the lightweight title data
  required by the restore control, or gate full adapter hydration until visibility is
  open while retaining already-loaded state across a normal Close.

### L3. Documentation/telemetry and diff hygiene have small drift

- **Evidence:** The reads README says handler logs emit the canonical sort token at
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md:30-41`,
  but successful Word Types handlers log raw `sortValue` at
  `GetWordTypeRows/GetWordTypeRowsHandler.cs:68-80` and
  `GetWordTypeTable/GetWordTypeTableHandler.cs:86-99`, fragmenting alias telemetry.
  The README also names the deleted `WordTypeSort.Occurrences` at `:182-191`.
  `git diff --check` reports trailing whitespace in
  `docs/design-preview/mushaf.html:34`, `:55`, `:57`, and `:85`.
- **Impact:** Runtime behavior is unaffected, but the nearest authoritative README is
  inaccurate and the branch fails its diff-hygiene gate.
- **Ownership:** **Branch-owned** documentation/hygiene drift.
- **Smallest safe remediation:** Log `sort.CanonicalToken()`, replace the deleted type
  name with the current sort-spec expression, and remove the four whitespace-only
  tails.

## Plan and task compliance matrix

| Scope | Result | Review conclusion |
|---|---|---|
| Flat-green restyle | PASS WITH NOTE | Light parchment/green tokens and current docs agree; dark intentionally keeps its documented navy/gold mapping. No invented gradients or theme-bypassing colors were found. The browser eye-check was not repeated in this review. |
| 029 Change A — shared ayah card | PASS | `qdAyahCard` is a projection-only frame. Feature components still own mapping, highlighting, Quran text, markers, and navigation. |
| 029 Change B — global entity navigation | **BLOCKED** | C1, H1, H2, M2, M3, L1, and L2 violate identity, history, focus, recovery, or hidden-work requirements. |
| 029 Word Type identity adapter | PASS | `word-type-detail-frame.adapter.ts:17-42` uses `identity.uniqueTashkeel.id`; verb context is tense/`unspecified`, non-verb context is `headPos`; case/tense/voice are `all`; ayahs page 1; missing identity fails closed. |
| 029 U1 | PASS | Selected-word loading retains natural-size reservation without changing Quran rendering. |
| 029 U2 | PASS | Count-range filters use the full row as planned. |
| 029 U3 | PASS | Word Type tabs remain mounted above the table with existing state ownership. |
| 030 C1 | PASS | Current shared ayah-card look is consistent with Change A. |
| 030 N1 | PASS | Active type selection returns before output, URL, HTTP, or loading work. |
| 030 N2 | PASS | The modal has fixed axes, a non-shrinking header, a body-only scroller, and responsive caps. |
| 030 N6 | PASS | Kind/count context is wired through the shared shell; zero is preserved separately from null. |
| 030 N3 | **PARTIAL / FAIL** | Most reservations exist, but M4 documents four remaining state/geometry failures. |
| 030 N4 | PASS | Three choices per metric, centralized family thresholds, and draft Enter/Apply commit preserve URL/API/cache identity. |
| 030 N5 | PASS | Focus opens only for a selected/typed value; ArrowDown explicitly opens; normal option-button semantics and cleanup remain. |
| 030 N7 | SUPERSEDED | The ayah-wide wash is no longer current behavior; M1 is authoritative. |
| M1 N7 replacement | PASS | CSS-only one-word hover/focus, stronger persistent selection, no Quran font/glyph/spacing changes, and no hover signal fan-out. |
| 030 N8 backend | PASS IN PRODUCTION / TEST NOTE | Parsing, SQL/LINQ ordering, tie chains, aliases, scope invariance, paging, and cache identity are correct by static review and passing suites; M6 is the missing exact-order matrix. |
| 030 N8 frontend | **PARTIAL / FAIL** | URL/cache/token/`aria-sort` behavior passes, but M1 is a keyboard interaction regression. |
| M2 | PASS | Stable scrollbar gutter removes the modal/body-scroll width jolt. |

The active Spec Kit feature is 026 and is outside this branch review. Features 029 and
030 are governed here by their locked `docs/` plans; no active Spec Kit artifact was
changed by this branch.

ARIA/RTL checks otherwise pass: the global shell has a named `role="dialog"` with
`aria-modal`, Arabic controls, and a focus trap at
`detail-modal-shell.component.html:1-14`; layout uses logical block/inline properties
and places Restore at RTL `inset-inline-end` at
`detail-modal-shell.component.scss:90-103`. Sort state is exposed on the
`columnheader`. No separate RTL regression was found beyond the keyboard/focus defects
in M1/M2.

## Quran-data integrity review

### Passed integrity gates

- The branch does not change Quran imports, stored source text, counts, source
  normalization, or data provenance.
- Shared `AyahCardComponent` contains only presentation/projection; highlighting and
  Quran rendering remain feature-owned.
- Current Mushaf production changes are SCSS/presentation plus tests. No Quran glyph,
  ligature, normalization, segment slicing, or highlight algorithm moved into shared
  UI.
- Quran text is not animated. The M1 effect changes only a word background/ring, and
  selected state disables transition:
  `mushaf-word/mushaf-word.component.scss:1-66`.
- The Word Type adapter follows the locked identity and fails closed rather than
  deriving from a localized label.
- No synthetic fallback data was added to production ayah paths.

### Failed integrity gates

- C1 can transiently display detail/Quran content under the wrong identity.
- M5 violates the source-safe test-fixture rule.
- M4 treats known zero as unknown in loading geometry; it does not fabricate content,
  but the state distinction is still wrong.

## N8 contract and SQL safety

The production N8 implementation passes the requested SQL-safety review:

- `WordSortToken.TrySplit()` only decomposes the grammar and states that callers must
  allowlist at
  `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordSortToken.cs:24-57`.
- Every explorer then applies a closed column allowlist and rejects suffixes on
  `mushaf-order`; Roots is representative at
  `Roots/RootSortSpec.cs:86-147`.
- Word Types chooses `ORDER BY` from compiler-known constant switch arms. Neither the
  column nor direction is request interpolation:
  `EfWordTypesReader.Sql.cs:313-328` and `:516-533`.
- Unique Words applies typed LINQ arms and uses the same
  `FirstWordOrderInMushaf → Id` tie chain in both directions:
  `EfUniqueWordsReader.List.cs:171-196`.
- Roots/Lemmas/Stems also use typed ordering before paging. Alpha deliberately retains
  its legacy text → Id chain because adding Mushaf order would change old links; this
  deviation is documented in
  `docs/feature-030-explorer-polish/verification.md:93-101`.
- Sort changes ordering only. Filtering/count/detail scope does not consume sort, and
  the full suite's scope-invariance tests pass.
- Frontend cache keys include canonical sort in all five explorers, for example
  `roots-cache.ts:8-9`, `unique-words-cache.ts:9-17`, and
  `word-types-cache.ts:13-25`.
- Legacy natural-direction aliases canonicalize to the same typed spec and cache key;
  contract tests cover this at
  `Backend/tests/QuranDashboard.Tests/Quran/Words/WordSortTokenContractTests.cs:43-60`
  and `WordTypesTableReadTests.cs:404-437`.
- Swagger documents the token grammar and allowlists for all five endpoints, and
  `docs/contracts/words-explorers.md:1-15` correctly remains a thin pointer to code and
  the nearest README.

No sort column or direction reaches SQL as untrusted text. No N8 N+1, repeated metadata
round trip, detail-scope drift, count drift, or nondeterministic tie was found.

## Test Guard review

**Verdict: CHANGES REQUESTED.**

Strengths:

- The branch adds substantial behavior-oriented parser, alias, cache, URL, modal,
  scroll-lock, and real-PostgreSQL coverage.
- Tests generally construct real DTO/state values and mock legitimate HTTP/router
  boundaries rather than framework internals.
- Backend structural data is explicitly synthetic and transaction-isolated.
- The current full suites pass.

Required improvements:

1. Add the stale-detail race cases from C1; current tests cancel only stale summaries.
2. Replace H1's reload-only test with entry-adjacency/revisit coverage.
3. Stop encoding H2's browser/dialog Back divergence as correct behavior.
4. Add focused sort-header + selected-row arrow coverage for M1.
5. Assert exactly one enabled focus trap and actual `document.activeElement` after final
   Back for M2.
6. Add summary/detail retry behavior for M3.
7. Replace or source the Uthmani fixtures in M5.
8. Add the exact PostgreSQL ordering matrix in M6.
9. Browser-measure M4; structural DOM tests cannot prove layout geometry.

## Clean-code guard review

- **Naming/functions:** Names are generally domain-specific and intention-revealing.
  The main failure is responsibility/size, captured by H3.
- **Comments/formatting:** Most long comments document non-obvious contract rationale
  rather than restating individual statements. L3 captures the actual formatting
  failure.
- **SOLID:** Layer direction and API/application/infrastructure ownership remain sound.
  H3 and the duplicated transition lifecycle show SRP/DRY pressure.
- **DRY/KISS/YAGNI:** No speculative plugin/factory/config surface was added. The same
  incomplete cancellation lifecycle is duplicated across five controllers and has
  produced C1.
- **AI failure modes:** No production hardcoded success, swallowed catch-all failure,
  invented package/API, or fake production Quran data was found. M5 is the test-fixture
  exception.

## Backend performance review

**Verdict: PASS WITH NOTES.**

- N8 adds no query or request round trip. Roots/Lemmas/Stems sort already-materialized
  cached summaries. Unique Words sorts its `IQueryable` before paging. Word Types
  extends the existing aggregate query with constant ordering arms.
- No N+1, unbounded detail hydration, repeated metadata lookup, or count/detail scope
  drift was found.
- Read-only catalog estimates are modest for this approximately three-user admin
  dashboard: about 1,642 roots, 4,817 lemmas, 11,843 stems, 14,783 simple unique words,
  21,294 tashkeel unique words, and 77,432 morphology rows. Count sorts may use a scan
  plus top-N sort because the unique tables do not have count-column indexes, but their
  heaps are under roughly 3 MB. There is no evidence-backed reason to add indexes.
- `EXPLAIN (ANALYZE, BUFFERS)` was attempted but could not be run: peer connection
  succeeds as role `mohamed`, while the role lacks `SELECT` on
  `quran_word_morphology` and `quran_words_unique_tashkeel`
  (`permission denied for table ...`). No permission or database change was made.
- Recommendation: no backend performance change. If a read-capable role is made
  available later, capture representative count asc/desc plans before considering any
  index.

## Frontend performance review

**Verdict: PASS WITH NOTES.**

- Components remain OnPush and signal/computed-driven; lists retain stable tracking and
  existing virtualization. N8 header helpers are trivial and do not introduce a broad
  re-render path.
- The overlay adapters are deferred into separate lazy chunks. A current production
  build succeeds with an initial bundle of 435.82 kB raw / 114.84 kB estimated transfer,
  versus 392.29 kB / 103.31 kB at the exact base: +43.53 kB raw / +11.53 kB transfer.
  The increase is real but remains below the 500 kB initial warning budget and is
  reasonable for the global URL/history/dialog infrastructure.
- N1 removes redundant selected-type work; N4 commits range requests rather than
  fetching on every draft keystroke; N8 issues one request per committed sort and keys
  the cache by canonical sort.
- M1's hover is CSS-only and removes the original ayah-wide signal fan-out.
- L2 is the one confirmed redundant HTTP path. M4 contains real visual-stability gaps,
  not a CPU/change-detection bottleneck.
- The production build reports two known component-style warnings:
  `selected-word-section` 4.32 kB and `selected-ayah-section` 4.38 kB. Neither reaches
  the 8 kB error budget; both are tied to reservation styling.

## Explicit resolution of the three pre-flagged items

1. **N7 “dark was calibrated only in light”: closed/superseded, not a current defect.**
   The top of the verification record explicitly supersedes the old Outstanding row at
   `docs/feature-030-explorer-polish/verification.md:7-15`. Current tokens record
   measured dark values—canvas 0.189 → hover 0.235 → selection 0.381, text 8.43:1—at
   `Frontend/quran-dashboard-ui/src/styles/_tokens.scss:77-103`. M1 also changed the
   interaction from ayah-wide to one-word, so the old N7 light-only statement is not
   current truth.
2. **Unique Words restored `notFound`/error shift: confirmed.** It is the first
   sub-case in M4. It is an acknowledged pre-existing state-contract limitation that
   Feature 030 deliberately left unresolved, so it is not misreported as a newly
   introduced regression.
3. **Stale backend/restart note: operational, not a source-code defect.** The new
   frontend emits opposite-direction tokens that a process running the old backend
   binary correctly rejects. The branch contains the backend parser/SQL change first,
   and a freshly built merged backend accepts the contract; see
   `docs/feature-030-explorer-polish/verification.md:33-35`. A backend process started
   before commit `0623d12` must be rebuilt/restarted to load the new assembly. The
   current source and full backend suite do not show a compatibility defect.

## Verification evidence and limitations

| Check | Result |
|---|---|
| Backend full suite | **1,532 passed, 0 failed, 0 skipped**; 5m 51s |
| Frontend full suite | **152 files / 1,743 tests passed**; 0 failures |
| Backend compile through `dotnet test` | Succeeded before test execution |
| Frontend production build | Succeeded; two non-failing component-style warnings |
| Exact-base frontend production build | Succeeded; used for the bundle comparison |
| Branch `git diff --check` | **Failed** only on the four L3 trailing-whitespace lines |
| SQL `EXPLAIN (ANALYZE, BUFFERS)` | Blocked by local role table permissions; no DB mutation attempted |
| Swagger/generated contract | Source, committed Swagger, and consumers inspected and consistent; the mutating generation script was not rerun under the single-write constraint |
| Live browser geometry | Not repeated; M4 is based on DOM/state flow and exact CSS geometry, and still requires the plan's 1440/768/390 browser overlay pass |

An unrelated, concurrently created untracked
`docs/feature-031-words-explainers/plan.md` was present during the review. It was not
created, read as review truth, modified, or deleted by this review.

## Final recommendation

**BLOCKED.** Fix C1 and the three High findings before merge. Then address the Medium
contract/accessibility/Test Guard findings, rerun both full suites, rerun the production
build, perform the required real-browser keyboard/layout matrix, and obtain read-only
PostgreSQL plans only if a read-capable role is available. The backend and frontend
performance designs do not require broad optimization; the merge blockers are
correctness, history, accessibility, test safety, and mandatory structure gates.
