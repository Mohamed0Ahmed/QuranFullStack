# Test Suite Rationalization V2 — Proven Stems/Lemmas Sorting Pilot Implementation Plan

Either Claude or Sol/Codex can execute this plan directly and sequentially. The checkboxes below are
the implementation record; they do not authorize Git delivery or require an auxiliary orchestration
workflow.

**Goal:** Remove one directly proven source of duplicated frontend test maintenance by sharing the
six-case sorting behavior contract between the Stems and Lemmas explorer page specs while preserving
all twelve page-specific executions and every current assertion.

**Architecture:** Add one stateless Stems/Lemmas-only behavior-registration helper under the Words
pages test area. Each existing page spec keeps its own TestBed, component, API fakes, query-param
subject, router spy, synthetic data, and page-specific cases; it passes only lifecycle and explicit
selector callbacks to the helper. The helper registers six separate behavior-named cases per caller
and does not become a generic harness for the other Words explorers.

**Tech stack:** Angular TestBed, Vitest over jsdom, Angular Router, TypeScript, the existing
`npm run test:*` lanes, and the current Test Guard rules.

**Fixed baseline:** Workflow & Instruction Routing V2, Skills V2, Testing Strategy V2, Engineering
Review Workflow V2, and Documentation & README Simplification V2 are implemented. Their routers,
Skill ownership, test lanes/triggers/freshness, review boundaries, documentation model, CI posture,
and delivery workflow are not redesigned here.

**Evidence basis:** `02-test-suite-audit.md` §§4, 6.1, 6.3, 6.5, 9, 11, and P1;
`13-sol-independent-review.md` C4 and WS5; `03-testing-strategy-v2.md` only as the implemented-plan
record; current `TESTING_STRATEGY.md` §§1–5; the current Test Guard Angular and harness references;
the current Words and test READMEs; and direct reads of every candidate file named in §§2–4 below.
Audit similarity and size observations are discovery evidence only. Current files and assertion-level
mapping control this plan.

## Global constraints

- This artifact is a plan. Creating it does not authorize any production/test implementation,
  product test run, formal review, commit, push, PR, deploy, database operation, or audit cleanup.
- When implementation is separately authorized, keep the cumulative implementation diff to §6's
  three test-code paths. The plan artifact already exists and is not an implementation target.
- Preserve all twelve logical sorting executions: the same six behaviors must execute once through
  the Stems component and once through the Lemmas component.
- Share assertion and registration code only. Do not share TestBed setup, page components, API fakes,
  DTO factories, facades, route subjects, router instances, fixtures, or mutable state.
- Do not use case-count parity as replacement proof. The assertion-level matrix in §5, page-identity
  sentinels, independent exact-spec execution, and negative controls in §8 are required.
- Do not use `describe.each`/`test.each` across the two pages. The six rows are different behaviors
  with different setup/actions/assertions; Test Guard Rule 3 requires six explicit cases. Page reuse
  comes from invoking the same behavior-registration function from two page-owned suites.
- Keep every page-specific behavior in its current spec. Do not include Roots, Word Types, Unique
  Words, API specs, component specs, URL-sync specs, facades/stores, or E2E in the abstraction.
- Do not edit production TypeScript/templates/selectors to make the helper easier to write.
- Do not delete or weaken accessibility, URL-state, cache, error mapping, deep-link, focus/history,
  or browser-only coverage. Required jsdom coverage must not be replaced by opt-in E2E.
- Do not invent, rewrite, normalize, or centralize Quran text or religious labels. The selected
  sorting cases continue to use their page-local, clearly synthetic fixture data.
- No test-file, test-case, LOC, or runtime reduction target is a success criterion. Runtime changes,
  if any, are incidental and must not influence acceptance.
- Do not change `TESTING_STRATEGY.md`, lane scripts/catalogs/configuration, `angular.json`, CI, Skills,
  routers, Engineering Review, Spec Kit, persistent memory, or any Project Simplification report.
- A README changes only when its current documented truth changes. Direct inspection found no README
  that documents these two inline sorting blocks, so this pilot expects no README edit.

---

## 1. Current duplication and maintenance problem

The current near-clone claim is true only at a bounded assertion block, not at whole-suite scale:

1. `stems-explorer-page.component.spec.ts` contains six sorting cases under
   `sorting (Feature 030, N8)` at current lines 652–736.
2. `lemmas-explorer-page.component.spec.ts` contains the same six titles, setup sequence, DOM
   interactions, option assertions, and `router.navigate` expectations under the same describe title
   at current lines 588–672.
3. Inside those blocks, the meaningful differences are the page-owned selectors
   (`stems-*` versus `lemmas-*`) and the page-local closures used to create a component fixture,
   publish query params, and read the current router spy.
4. The surrounding suites are not interchangeable. Stems provides
   `WordsAssociationOptionsService`, has root-and-lemma association behavior, and uses Stems API/model
   fixtures. Lemmas provides Angular HTTP testing, has root-only association behavior, and uses
   Lemmas API/model fixtures. Their detail, count-click, type, related-entity, restoration, and
   navigation tails contain real contract differences.
5. Keeping the six assertion bodies inline in both files creates drift risk: a sorting contract fix
   can be applied to one page but not the other, while copying the bodies into more explorers would
   compound that maintenance cost.
6. No inspected case is low-value enough to delete. Each catches a distinct failure in responsive
   control ownership, option availability, header navigation, canonical default release, or fallback
   navigation. The duplication is in test implementation, not in behavioral protection.

The smallest justified correction is therefore one shared six-case behavior helper. A whole-page
harness, shared TestBed, or test deletion would exceed the evidence.

## 2. Candidate inventory and direct evidence

| Candidate | Current direct evidence | Safety and maintenance judgment | Decision for this package |
|---|---|---|---|
| Stems/Lemmas whole page suites | Many titles are similar, but their TestBed providers, API models, association behavior, detail relations, count-click behavior, type clearing, and restoration deltas differ. | Whole-suite parameterization could configure away page-specific failures or execute the wrong fixture. | `KEEP` both complete page suites as separate owners. |
| Stems/Lemmas six-case sorting blocks | The twelve inline cases are assertion-for-assertion equivalents after page selector substitution; §5 maps every one. | Clear maintenance value, three-path implementation footprint, no production/data/config change, and page identity can remain explicit. | **Selected: `SHARE_HELPER`.** |
| Other repeated Stems/Lemmas titles | Result counts, list states, detail views, surah views, clear-selection, caching, deep links, not-found, Back/Forward, and 404 titles look similar, but several bodies and fixtures carry page deltas. | Title similarity is not replacement proof. Expanding now would turn the pilot into a page harness. | `KEEP`; a later candidate needs a new assertion-level adjudication. |
| Backend RefusalForce classes | Twelve refusal cases repeat only the `Succeeded == false`, pipeline exit-code, and exact/containing message triple. Seven classes retain different snapshots, reports, force behavior, source precedence, foundation protection, and writer probes. | A stateless assertion helper is supportable, but it would touch seven protected Pipeline classes and require sequential database verification across distinct features. | **Deferred ready candidate:** classes stay separate; no change in this package. |
| Four near-clone explorer fixtures | `RootsExplorerTestFixture`, `UniqueWordsTestFixture`, `MushafReaderTestFixture`, and `MorphologyExplorersTestFixture` share lease/provider/seed/disposal shape. `WordTypesTestFixture` is not part of the pool because it owns a lazy API factory/client lifecycle. | A composition host may be viable, but it changes shared PostgreSQL fixture infrastructure, four collections' lifecycle plumbing, and two owning READMEs; isolation/disposal proof and the full shared-runtime gate union are required. | **Deferred ready candidate:** no fixture/helper/README change in this package. |
| Exact safe logging-field suites | Current suites protect explicit safe-field allowlists plus redaction, level, reason, operation identity, and absence of sensitive/Quran/search content. | A generic no-sensitive-content sentinel is weaker and can miss a newly emitted unsafe field. | `KEEP` exactly; not a candidate. |
| Large/selector-heavy Angular specs | Selector counts include deliberate `data-testid` behavioral hooks; jsdom assertions and browser-only coverage protect different layers. | Size or selector count alone proves no duplicate behavior. | `KEEP`; no split, selector rewrite, or deletion campaign. |

### RefusalForce files inspected and deferred

- `Backend/tests/QuranDashboard.Tests/Quran/FullI3rab/FullI3rabRefusalForceTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Mutashabihat/MutashabihatRefusalForceTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Navigation/NavigationRefusalForceTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Tafsirs/TafsirRefusalForceTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Translations/TranslationRefusalForceTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsDisplay/DisplayWordsRefusalForceTests.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyRefusalForceTests.cs`

### Fixture files inspected and deferred

- `Backend/tests/QuranDashboard.Tests/Quran/WordsRoots/RootsExplorerTestFixture.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/Words/UniqueWordsTestFixture.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/MushafReader/MushafReaderTestFixture.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersTestFixture.cs`
- `Backend/tests/QuranDashboard.Tests/Quran/WordsWordTypes/WordTypesTestFixture.cs` — inspected and
  explicitly excluded from the near-clone pool.

## 3. Exact selected pilot and acceptance boundary

Create one Stems/Lemmas-only helper that registers these six cases for each caller:

1. the sort select exists inside `.qd-explorer-sort-fallback`;
2. default and required bidirectional sort options remain present;
3. the occurrences header cycle writes `sort=occurrences` and resets `page`;
4. releasing the occurrences cycle removes `sort` and resets `page`;
5. the fallback select writes `sort=alpha-desc` and resets `page`; and
6. choosing `mushaf-order` in the fallback removes `sort` and resets `page`.

Acceptance is behavior-for-behavior:

- each page retains its own outer suite, `beforeEach`, TestBed, mocks, route subject, router spy, and
  `initLifecycle`;
- the helper is invoked once inside the Stems US2 suite and once inside the Lemmas US1 suite;
- the helper registers six distinct `it` cases, producing one Stems and one Lemmas execution of every
  matrix row;
- the old inline bodies are removed only after the helper-generated successor cases have executed
  successfully against that same page;
- no other case, file, fixture, selector, product behavior, or test lane changes; and
- completion depends on the assertions and failure modes in §5, never on a smaller file or case count.

The nested describe becomes behavior-oriented (`sorting contract`) inside the helper. The historical
`Feature 030, N8` label is not copied into the shared API; individual case titles remain scenario
descriptions.

## 4. Candidates explicitly rejected or deferred

### Keep in the two page specs

- Stems root-plus-lemma association filters and root-picker failure behavior.
- Lemmas root-only association, nine-column contract, count-click and zero-count activation behavior.
- Page-specific table headers, related Lemmas/Stems views, type-filter API behavior, catalogue-page
  preservation, matched-word rendering, and Mushaf links.
- Each page's result-count, list-error/empty placement, row selection, detail views, surah views,
  stale-type clearing, clear-selection, deep-link restoration, unknown-identity, controlled empty,
  cache reuse, Back/Forward, and HTTP 404 cases.
- All page-local fixture builders and synthetic data.

### Defer outside this package

- Any shared Stems/Lemmas TestBed, API fake, DTO factory, facade, query subject, or mutable context.
- Any abstraction of the other similar page-case titles without a new assertion-level matrix.
- Roots, Word Types, Unique Words, Words API, component, URL-sync, facade/store, or E2E suites.
- The RefusalForce assertion helper. Its safe future boundary is only a stateless assertion triple;
  every class, collection, fixture, snapshot, report, force case, source case, and test identity stays
  separate.
- The four-fixture composition host. Its safe future boundary keeps all four fixture wrapper types,
  seed resources, external-read-only rules, collection rows, provider-before-lease disposal, and
  consumer tests intact; it must not become an importer fixture framework.
- Importer fixtures, safe logging, giant-spec splitting, selector rewrites, E2E/unit movement, and any
  test deletion campaign.

### Low-value deletion verdict

No current test is classified `DELETE_DUPLICATE` in this pilot. The old inline TypeScript bodies may
be removed only because the same logical cases continue to be registered and executed through the
helper. Their behavior classification is `KEEP`; their repeated implementation classification is
`SHARE_HELPER`.

## 5. Replacement coverage matrix

Path aliases used only to keep the matrix readable:

- `S` = `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts`
- `L` = `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.spec.ts`
- `H` = `Frontend/quran-dashboard-ui/src/app/features/words/pages/testing/stems-lemmas-sorting.behavior.ts`

Every replacement is one named case in `H::describeStemsLemmasSortingBehavior`, executed through the
named page's local invocation. “Preserved” means the existing assertion remains; §8's page sentinel,
explicit element existence, navigate-spy clearing, and select-value checks are additive safeguards.

| Existing test/case | Behavior protected | Replacement location/case | Assertion preserved | Data/fixture preserved | Failure mode still detectable | Classification |
|---|---|---|---|---|---|---|
| `S::has no desktop sort dropdown: the only select sits in the ≤1023px fallback wrapper` | Stems exposes one fallback sort control under its responsive owner. | `H` same title via Stems invocation. | Stems select is truthy; closest `.qd-explorer-sort-fallback` is non-null. | Stems TestBed and `initLifecycle`; `qd-stems-table` sentinel; Stems selector. | Missing select, wrong selector, wrong component fixture, or control outside wrapper fails. | `SHARE_HELPER` |
| `L::has no desktop sort dropdown: the only select sits in the ≤1023px fallback wrapper` | Lemmas exposes one fallback sort control under its responsive owner. | `H` same title via Lemmas invocation. | Lemmas select is truthy; closest `.qd-explorer-sort-fallback` is non-null. | Lemmas TestBed and `initLifecycle`; `qd-lemmas-table` sentinel; Lemmas selector. | Missing select, wrong selector, wrong component fixture, or control outside wrapper fails. | `SHARE_HELPER` |
| `S::offers the default order plus every sortable column in both directions` | Stems option surface retains the current default and required direction tokens. | `H` same title via Stems invocation. | First value is `mushaf-order`; values contain `alpha`, `alpha-desc`, `occurrences`, `occurrences-asc`. | Rendered Stems option DOM from the current Stems fixture. | Wrong default order or any currently asserted token missing fails. | `SHARE_HELPER` |
| `L::offers the default order plus every sortable column in both directions` | Lemmas option surface retains the current default and required direction tokens. | `H` same title via Lemmas invocation. | First value is `mushaf-order`; values contain `alpha`, `alpha-desc`, `occurrences`, `occurrences-asc`. | Rendered Lemmas option DOM from the current Lemmas fixture. | Wrong default order or any currently asserted token missing fails. | `SHARE_HELPER` |
| `S::navigates { sort: token, page: null } when a header cycle step is emitted` | Stems header sorting writes the canonical token and resets list paging. | `H` same title via Stems invocation. | Click occurrences header; navigate with `{ sort: 'occurrences', page: null }`, current route, and merge handling. | Stems router spy and Stems header button. | Missing event, stale initialization navigation, wrong token/page/reset/merge target fails. | `SHARE_HELPER` |
| `L::navigates { sort: token, page: null } when a header cycle step is emitted` | Lemmas header sorting writes the canonical token and resets list paging. | `H` same title via Lemmas invocation. | Click occurrences header; navigate with `{ sort: 'occurrences', page: null }`, current route, and merge handling. | Lemmas router spy and Lemmas header button. | Missing event, stale initialization navigation, wrong token/page/reset/merge target fails. | `SHARE_HELPER` |
| `S::navigates { sort: null, page: null } when the cycle releases` | Stems canonicalizes the default by removing the sort query param. | `H` same title via Stems invocation. | Seed `sort=occurrences-asc`; click header; navigate with `{ sort: null, page: null }` and merge handling. | Stems `queryParamMap$`, fixture, and router spy. | Query seed ignored, cycle not released, default serialized, or page not reset fails. | `SHARE_HELPER` |
| `L::navigates { sort: null, page: null } when the cycle releases` | Lemmas canonicalizes the default by removing the sort query param. | `H` same title via Lemmas invocation. | Seed `sort=occurrences-asc`; click header; navigate with `{ sort: null, page: null }` and merge handling. | Lemmas `queryParamMap$`, fixture, and router spy. | Query seed ignored, cycle not released, default serialized, or page not reset fails. | `SHARE_HELPER` |
| `S::drives the same URL contract from the fallback select` | Stems fallback and header share the URL/reset contract. | `H` same title via Stems invocation. | Set select to `alpha-desc`, dispatch `change`, expect merged `{ sort: 'alpha-desc', page: null }`. | Stems select DOM, fixture, and router spy. | Option not present, change not handled, wrong token, or missing page reset fails. | `SHARE_HELPER` |
| `L::drives the same URL contract from the fallback select` | Lemmas fallback and header share the URL/reset contract. | `H` same title via Lemmas invocation. | Set select to `alpha-desc`, dispatch `change`, expect merged `{ sort: 'alpha-desc', page: null }`. | Lemmas select DOM, fixture, and router spy. | Option not present, change not handled, wrong token, or missing page reset fails. | `SHARE_HELPER` |
| `S::releases the param when the fallback select picks the default order` | Stems fallback removes the default token rather than spelling it in the URL. | `H` same title via Stems invocation. | Seed `sort=alpha`; choose `mushaf-order`; expect merged `{ sort: null, page: null }`. | Stems query subject, select DOM, fixture, and router spy. | Query seed ignored, default option unavailable, param retained, or page not reset fails. | `SHARE_HELPER` |
| `L::releases the param when the fallback select picks the default order` | Lemmas fallback removes the default token rather than spelling it in the URL. | `H` same title via Lemmas invocation. | Seed `sort=alpha`; choose `mushaf-order`; expect merged `{ sort: null, page: null }`. | Lemmas query subject, select DOM, fixture, and router spy. | Query seed ignored, default option unavailable, param retained, or page not reset fails. | `SHARE_HELPER` |

There are no `PARAMETERIZE`, `DELETE_DUPLICATE`, or selected `NEEDS_ADJUDICATION` rows. If an
implementer cannot preserve one row exactly through `SHARE_HELPER`, that row remains inline and the
pilot stops for scope adjudication; it is not silently dropped.

## 6. Exact implementation file set

| Action | Path | Responsibility |
|---|---|---|
| Create | `Frontend/quran-dashboard-ui/src/app/features/words/pages/testing/stems-lemmas-sorting.behavior.ts` | Stateless Stems/Lemmas-only config type plus the six explicit behavior cases and non-vacuity guards. No TestBed, page/API imports, shared data, or mutable state. |
| Modify | `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts` | Import and invoke the helper from the current US2 suite with Stems-owned closures/selectors; remove only the mapped inline sorting block after its successor runs. |
| Modify | `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.spec.ts` | Import and invoke the helper from the current US1 suite with Lemmas-owned closures/selectors; remove only the mapped inline sorting block after its successor runs. |

**Delete:** no file, test, fixture, seed, resource, snapshot, report, selector, or catalog/config row.

**README disposition:** keep `Frontend/quran-dashboard-ui/src/app/features/words/README.md` and
`Frontend/quran-dashboard-ui/testing/README.md` unchanged. They own product behavior and lane
mechanics, not inline assertion placement. If implementation reveals a current sentence that does
claim these blocks are duplicated inline, stop and add only the exact truth repair to this plan
before editing the README.

No other path is allowed. In particular, do not create a feature-generic `helpers.ts`, put this
two-page helper under app-wide `shared/`, or add a new `*.spec.ts` file.

## 7. Shared-helper design

The helper's exact public surface is intentionally narrow:

```ts
import type { ComponentFixture } from '@angular/core/testing';
import type { Router } from '@angular/router';

export interface StemsLemmasSortingBehaviorConfig<TComponent> {
  readonly initLifecycle: () => Promise<ComponentFixture<TComponent>>;
  readonly setQueryParams: (params: Readonly<Record<string, string>>) => void;
  readonly getRouter: () => Router;
  readonly pageTableSelector: 'qd-stems-table' | 'qd-lemmas-table';
  readonly sortSelectSelector: string;
  readonly occurrencesSortButtonSelector: string;
}

export function describeStemsLemmasSortingBehavior<TComponent>(
  config: StemsLemmasSortingBehaviorConfig<TComponent>,
): void;
```

The Stems caller supplies exactly:

```ts
describeStemsLemmasSortingBehavior({
  initLifecycle,
  setQueryParams: (params) => queryParamMap$.next(convertToParamMap(params)),
  getRouter: () => router,
  pageTableSelector: 'qd-stems-table',
  sortSelectSelector: '[data-testid="stems-sort-select"]',
  occurrencesSortButtonSelector: '[data-testid="stems-table-sort-occurrences"]',
});
```

The Lemmas caller supplies exactly:

```ts
describeStemsLemmasSortingBehavior({
  initLifecycle,
  setQueryParams: (params) => queryParamMap$.next(convertToParamMap(params)),
  getRouter: () => router,
  pageTableSelector: 'qd-lemmas-table',
  sortSelectSelector: '[data-testid="lemmas-sort-select"]',
  occurrencesSortButtonSelector: '[data-testid="lemmas-table-sort-occurrences"]',
});
```

Implementation rules inside the helper:

1. Import `describe`, `expect`, `it`, and `vi` directly from Vitest. Register one nested
   `describe('sorting contract', ...)` containing six explicit `it` blocks with the existing titles.
2. For every case, call the page-owned `initLifecycle`; do not cache a fixture between cases.
3. Assert `config.pageTableSelector` exists before any sorting assertion. This binds the invocation to
   the intended real page and makes a cross-wired Stems/Lemmas fixture fail.
4. Keep the expected sorting contract inside the helper: `mushaf-order` first; required
   `alpha`, `alpha-desc`, `occurrences`, and `occurrences-asc`; header token `occurrences`; fallback
   token `alpha-desc`; and default release to `null`. Do not pass expected values from the caller,
   because a caller feeding both input and expected output can make the test vacuous.
5. Query the explicit configured select/button selector, assert it exists, and only then interact.
6. After lifecycle initialization and immediately before a navigation-producing action, clear
   `vi.mocked(config.getRouter().navigate)`. Initialization navigation must not satisfy the case.
7. After assigning `alpha-desc` or `mushaf-order` to a select, assert the select resolved to that value
   before dispatching `change`.
8. Preserve `relativeTo: expect.anything()`, `queryParamsHandling: 'merge'`, the exact `sort` value,
   and `page: null` in every current navigation assertion.
9. Store no module-level mutable fixture, router, subject, mock, or configuration. Do not import either
   page component, API, facade, model, label, or test-data factory.
10. Keep all Stems/Lemmas outer setup and teardown unchanged, including `resetTestingModule()`,
    `destroyAfterEach: true`, query-map reset, existing mock restoration, and the repository global
    cleanup in `src/test-setup.ts`.

The helper is a two-page pilot by name and location. Reusing it from a third page is a scope change
that requires fresh direct comparison and an updated matrix; it is not an implementation shortcut.

## 8. Non-vacuity and isolation safeguards

### Structural proof

- Six separate `it` blocks remain. Do not loop over behavior names or combine different actions into
  one parameterized test.
- Each caller is nested under its existing page-owned outer describe, so failure output identifies
  Stems versus Lemmas and the exact behavior.
- Each case obtains a new page-local fixture through the callback after that page's `beforeEach`.
- The helper has no shared state and cannot acquire TestBed, API mocks, query subjects, or a router on
  its own.
- Page table sentinels and explicit selectors make a Stems/Lemmas cross-wire fail before an assertion
  could accidentally exercise the other page.
- Expected tokens live in the assertion helper, not in caller-supplied expectations.
- Router spy clearing prevents setup navigation from satisfying an interaction assertion.
- Page-specific fixtures/data remain in their existing files; no fixture reuse or data leakage is
  introduced.

### Required negative controls during implementation

These are transient verification edits and must be reverted before continuing:

1. With only the Stems helper invocation added, temporarily change its `pageTableSelector` to
   `qd-lemmas-table`; the exact Stems spec must fail in the helper-generated sorting cases. Restore the
   Stems selector and rerun to pass.
2. Repeat inversely for Lemmas with `qd-stems-table`; the exact Lemmas spec must fail, then pass after
   restoration.
3. Temporarily change only the header-cycle case's expected navigation token from `occurrences` to an
   impossible sentinel; that header-cycle case must fail in both exact specs while the other five
   sorting behaviors remain independently registered. Restore before final verification.

The negative controls prove that each page invocation reaches its own component and that the shared
assertion is live. They supplement, but do not replace, the §5 failure-mode review. Do not commit or
leave any negative-control mutation in the worktree.

### Replacement proof before inline removal

For each page separately:

- first run the helper-generated six cases while the old inline block is still present;
- compare each helper case to its §5 predecessor at the assertion level;
- remove only that page's old inline block;
- run that exact page spec again; and
- confirm the other page's spec and helper invocation have not been rewritten as part of the step.

Observed case totals may be recorded with the run evidence, but matching totals alone cannot close
the replacement gate.

## 9. Protected tests and behaviors that remain untouched

- All authentication, authorization, Owner, permissions, audit, concurrency/xmin/conflict,
  transaction/rollback, migration/schema/upgrade, route/auth/binding/serialization, and PostgreSQL
  ownership tests.
- All canonical Quran, source/hash/manifest/provenance, importer refusal/force/rollback, safe logging,
  redaction, level/reason, and external-read-only protections.
- Every RefusalForce class, test name, fixture, collection, snapshot, report, source package, force
  case, and test catalog/resource row.
- Every importer/explorer fixture, seed SQL resource, lease/reset/disposal rule, collection type,
  consumer test, and test catalog/resource row.
- Stems/Lemmas URL-sync, API-boundary, facade/store/cache, component, table, association, detail,
  accessibility, restoration, and E2E suites.
- Roots, Word Types, Unique Words, Abwab, Mushaf, auth, access-admin, shared, and all other Frontend
  feature tests.
- Existing `data-testid` values and ARIA/role assertions. Selector volume is not deletion evidence.
- Browser-only focus, history, layout, geometry, RTL-input, and real-network protection. This test-only
  refactor preserves the current jsdom DOM-ownership assertion but makes no claim that jsdom proves
  the `≤1023px` visual geometry; existing browser coverage remains where it is.

## 10. Small sequential implementation steps

### Task 1 — Freeze the assertion contract and baseline

**Files:** read only; no implementation path changes yet.

- [ ] Confirm the current branch is not `main` and record pre-existing worktree changes. Stop before
  editing if the implementation cannot isolate §6's paths from user-owned work.
- [ ] Re-open the two current sorting blocks by their six case titles, not only historical line
  numbers, and compare them against every §5 row.
- [ ] Confirm the three selectors for each caller exist in the current templates/components:
  `qd-stems-table`/`qd-lemmas-table`, each `*-sort-select`, and each
  `*-table-sort-occurrences`.
- [ ] From `Frontend/quran-dashboard-ui/`, run each exact spec through `npm test --` and record the
  actual result. These are baseline observations, not standing test-count targets.
- [ ] Stop if either baseline fails or unexpectedly skips; do not hide the failure by narrowing
  further or changing expectations.

### Task 2 — Prove the helper through Stems before removing Stems duplication

**Files:**

- Create: `Frontend/quran-dashboard-ui/src/app/features/words/pages/testing/stems-lemmas-sorting.behavior.ts`
- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts`

- [ ] Create the stateless helper with exactly the §7 interface, six titled cases, fixed expectations,
  page sentinel, explicit element checks, router-spy clearing, and select-value checks.
- [ ] Import and invoke it from `StemsExplorerPageComponent US2` with the exact Stems config in §7;
  leave the old Stems sorting block temporarily intact.
- [ ] Run `npm run typecheck:spec`, then the exact Stems spec. The original and helper-generated blocks
  must both exercise the same six §5 behaviors against the Stems component.
- [ ] Perform the Stems page-sentinel negative control from §8, restore it, and rerun the exact Stems
  spec successfully.
- [ ] Compare the helper cases to the six Stems matrix rows. Remove only the old inline Stems sorting
  block and its obsolete plan-ID describe label.
- [ ] Run the exact Stems spec again. Do not change any other Stems case or the Lemmas file in this
  task.

### Task 3 — Adopt the proven helper in Lemmas

**Files:**

- Modify: `Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.spec.ts`

- [ ] Import and invoke the existing helper from `LemmasExplorerPageComponent US1` with the exact
  Lemmas config in §7; leave the old Lemmas sorting block temporarily intact.
- [ ] Run the exact Lemmas spec. Both its original and helper-generated blocks must exercise the same
  six §5 behaviors against the Lemmas component.
- [ ] Perform the Lemmas page-sentinel negative control, restore it, and rerun the exact Lemmas spec
  successfully.
- [ ] Perform the shared header-token negative control only after both page invocations exist; restore
  it and rerun both exact specs successfully.
- [ ] Compare the helper cases to the six Lemmas matrix rows. Remove only the old inline Lemmas sorting
  block and its obsolete plan-ID describe label.
- [ ] Run the exact Lemmas spec again. If the helper changed after the last Stems run, rerun the exact
  Stems spec too; otherwise the settled final verification in Task 4 covers the cumulative union.

### Task 4 — Review the settled cumulative diff and verify once

**Files:** only §6.

- [ ] Perform a direct test-quality self-check against Rules 1–9 in the current canonical Test Guard
  source:
  observable DOM/router behavior remains the subject; boundary mocks stay page-local; six distinct
  scenarios remain; every case has the §5 bug-catching reason; and behavior names replace the
  historical plan-ID describe. This check does not invoke the Test Guard Skill or make it an
  automatic workflow stage.
- [ ] Confirm the helper imports neither page/API/facade/model/test-data module, stores no state, and
  exposes no callback that supplies expected sort values or assertions.
- [ ] Confirm the two specs retain all non-sorting cases and page-specific setup unchanged.
- [ ] Confirm the final diff contains no production template/selector, README, configuration,
  backend, resource, audit-report, or additional Words-suite change.
- [ ] Run the focused commands in §11 in order against the settled state. Do not run a
  broad/composite suite after each refactoring task.
- [ ] Recompute the cumulative-final-diff trigger union and run `npm run test:pre-pr` once. Do not run
  its unchanged `typecheck`, build, or full-suite legs separately immediately beforehand.
- [ ] Record actual pass/fail/skip output. A failure, unexpected skip, timeout, or unknown result is not
  passing evidence and must be reported without changing scope to make the run green.
- [ ] Run the static completion checks in §11. Do not commit, push, open a PR, invoke formal
  Engineering Review, or deploy unless separately requested.

## 11. Focused verification according to Testing Strategy V2

Run commands from `Frontend/quran-dashboard-ui/` unless a row says otherwise.

| Boundary | Command | Why selected | Required evidence |
|---|---|---|---|
| `FOCUSED` | `npm run typecheck:spec` | The new imported TypeScript helper and both specs must compile under the spec project. | Command, pass/fail, and no hidden unsupported compiler invocation. |
| `FOCUSED` | `npm test -- --watch=false --include=src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts` | Exact Stems replacement and negative-control recovery. | Actual result; all six mapped sorting behaviors execute under the Stems suite; no unexpected skip. |
| `FOCUSED` | `npm test -- --watch=false --include=src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.spec.ts` | Exact Lemmas replacement and negative-control recovery. | Actual result; all six mapped sorting behaviors execute under the Lemmas suite; no unexpected skip. |
| `FOCUSED` | `npm run test:feature:words` | Both changed specs and their imported helper belong to the Words feature slice. | Named lane result with actual output. |
| `FINAL_BOUNDARY` | `npm run test:pre-pr` | Current policy selects the final Frontend composite for a cumulative spec/test-support diff. Run once after the helper and both callers settle. | Permission/audit parity, combined typecheck, production build, and full jsdom suite all complete in the composite; record actual results. |

`test:composition` is not selected. The helper registers and asserts behavior while TestBed setup,
`ComponentFixture` creation and ownership, and Angular rendering infrastructure remain page-local.
Current `TESTING_STRATEGY.md` §4 selects that protected lane when a shared component harness, Angular
rendering, overlay composition, or broad component infrastructure changes; none changes in this
pilot. Two `.component.spec.ts` consumers alone do not activate the trigger.

Static completion checks from the repository root:

1. `git status --short` and `git diff --name-status` show only §6's implementation paths plus the
   already-approved plan artifact and any explicitly recorded pre-existing user changes.
2. `git diff --check` reports no whitespace errors.
3. `rg -n "sorting \(Feature 030, N8\)" Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts Frontend/quran-dashboard-ui/src/app/features/words/pages/lemmas-explorer-page/lemmas-explorer-page.component.spec.ts`
   returns no match.
4. Each of the six case titles appears exactly once in the helper, and
   `describeStemsLemmasSortingBehavior` is invoked once in each changed page spec.
5. The helper contains both `qd-stems-table`/`qd-lemmas-table` only through caller config; it imports
   no production page/API/facade/model module and declares no module-level mutable state.
6. `git diff --name-only -- resources Backend` and
   `git diff --cached --name-only -- resources Backend` are empty; `git status --short` shows no
   untracked source/canonical/synthetic fixture content.
7. `Frontend/quran-dashboard-ui/src/app/features/words/README.md` and
   `Frontend/quran-dashboard-ui/testing/README.md` remain unchanged because their documented truth
   did not change.

Do not run Browser E2E: no production browser behavior, geometry, focus, history, RTL input, or
network path changes. Do not run Backend tests: no Backend, generated API contract, or Frontend auth
surface changes. Do not run standalone `test:full`, `typecheck`, or `build:verify` immediately before
the unchanged-state `test:pre-pr`; the composite already owns those legs.

## 12. Expected maintenance benefit

- One directly proven sorting contract body replaces two drifting copies while retaining one failure
  report per page and behavior.
- A future change to the common default/token/navigation contract is asserted in one place; each page
  still supplies explicit selectors and executes the real page component.
- Page-specific TestBed/API/data complexity stays visible in the owning specs instead of moving into a
  conditional generic harness.
- The helper's two-page name and narrow interface make scope creep observable: adding another page
  requires a deliberate new adjudication rather than one more permissive config object.
- Removing historical plan-ID wording from only the selected describe leaves behavior-oriented test
  output without renaming unrelated suites.

Success is lower repeated-maintenance risk with equivalent-or-stronger protection. It is not fewer
files, fewer logical tests, fewer lines, or a faster suite.

## 13. Explicit non-goals

- No Testing Strategy, lane, script, test catalog, Angular configuration, timeout, fork-cap, cadence,
  final-boundary, CI, or E2E-status change.
- No Workflow/Instruction Routing, Skill, Engineering Review, documentation model, Git workflow,
  Spec Kit, persistent-memory, PR, deployment, or release change.
- No production code, component/template/style/selector, API, DTO, schema, database, migration,
  import, source package, Quran data, fixture seed, or generated artifact change.
- No generic Words explorer page harness, shared TestBed, shared page/API fake, shared route subject,
  cross-page `test.each`, or adoption by Roots/Word Types/Unique Words.
- No RefusalForce implementation in this package and no merge of RefusalForce classes.
- No explorer/importer fixture helper, base class, reset/report/snapshot framework, or fixture reuse
  outside the current owning domains.
- No logging-field reduction, generic sensitive-content sentinel, selector-count pruning, giant-spec
  split, file-count target, case-count target, LOC target, or runtime target.
- No deletion or movement between unit and E2E coverage.
- No README edit unless direct implementation evidence first proves the current documented truth
  changed and this plan is corrected before that edit.
- No formal review, commit, push, PR, deploy, or cleanup as an implicit final step.

## 14. Stop conditions

Stop and report rather than broadening or weakening the pilot when any of the following is true:

1. The branch is `main`, or user-owned changes overlap a §6 implementation path and cannot be
   preserved safely.
2. Either exact baseline spec fails or unexpectedly skips before the refactor.
3. Any §5 assertion, page-owned fixture/data source, or named failure mode cannot be mapped exactly to
   the helper-generated successor.
4. The helper needs to import a page component, API, facade, model, label, DTO factory, TestBed, or
   page-owned mock to express the sorting contract.
5. The helper needs mutable module state, a shared fixture/router/query subject, a generic expected-
   assertion callback, reflection, or a permissive config that can make both pages exercise one
   fixture accidentally.
6. A third explorer or any non-sorting case must join the helper to make the design worthwhile.
7. Stems and Lemmas differ in a current sorting assertion, token, setup, selector meaning, or failure
   mode that the §5 matrix does not capture. Keep the differing case local; do not add conditionals to
   hide the divergence.
8. A page-identity or header-token negative control does not fail as specified, or a restored control
   does not return the exact spec to green.
9. A protected accessibility, URL-state, focus/history, safe logging, Quran/source, importer refusal,
   rollback, fixture-isolation, or browser-only invariant becomes less explicit or moves to weaker
   enforcement.
10. Fixture/data reuse risks cross-test state leakage, or Quran/source provenance becomes less clear.
11. Production code, selectors, configuration, lane policy, README truth, Backend code/tests,
    resources, audit reports, Skills/routers, CI, Spec Kit, or delivery workflow would need to change.
12. The cumulative final diff expands beyond §6, a required current Testing Strategy trigger is
    omitted, or broad gates are being repeated after individual refactoring steps instead of selected
    once from the settled cumulative diff.
13. Completion is being justified by matching case counts, reduced LOC/files, or a green broad suite
    without the assertion-level matrix and non-vacuity proof.

Implementation is complete only when the three-path diff retains every matrix behavior for both real
pages, both page-specific exact specs demonstrate independent non-vacuity, the Words and
final cumulative boundaries pass with honest evidence, all protected areas remain untouched, and no
logical test has been deleted.
