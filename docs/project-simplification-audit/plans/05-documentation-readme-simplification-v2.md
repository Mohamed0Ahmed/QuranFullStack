# Documentation & README Simplification V2 — Local Truth Without Narrative Weight Implementation Plan

Either Claude or Sol/Codex can execute this plan directly and sequentially. It requires no new
documentation framework, generic cleanup pass, repository-wide Markdown rewrite, or external
orchestrator.

**Goal:** Reduce the recurring nearest-README context cost in the two currently proven frontend
outliers while preserving every local invariant, contract, safety rule, and irreconstructible
rationale, and repair two directly related documentation inconsistencies.

**Architecture:** Keep the nearest-README model. Re-register the Abwab and Words READMEs in place:
retain local current-state contracts and concise why-text, remove delivery chronology and mutable
status/count narration, and use existing canonical pointers only where the duplicated detail already
has an owner. Do not move the removed prose into a new shared document or make an ordinary local task
follow an additional mandatory read.

**Mechanisms:** Section-level preservation maps, an exact four-file implementation allowlist,
repoint-before-remove scans, stable-heading/link checks, before/after rule comparison, and small
nearest-README tabletop scenarios. Byte counts may be recorded as observations but are never an
acceptance threshold.

**Fixed baseline:** Workflow & Instruction Routing V2, Skills V2, Testing Strategy V2, and
Engineering Review Workflow V2 are implemented. Their routers, ownership, test-selection/freshness,
focused/formal review boundaries, and native Claude/Sol behavior are not redesigned by this plan.

**Evidence basis:** `06-readme-markdown-decision-audit.md` §§3.1–3.4, §5.1A/F/G, §6, and §8;
`13-sol-independent-review.md` WS2, contrast-debt coverage, and its final readiness split; the four
implemented V2 plans; and direct current reads of only the target/owner documents named below. The
topic report and current files win over synthesis reports 01/11.

## Global constraints

- Preserve the native nearest-README route. An agent touching Abwab or Words must still get the local
  facts it needs from that README without searching historical plans or following a new mandatory
  documentation chain.
- Shorten for register and ownership, never for size alone. Historical size estimates and
  content-uniqueness percentages are discovery evidence, not deletion proof or completion targets.
- For every edited passage, retain the current behavior, the important non-derivable reason, and a
  stable symbol/test/owner pointer when one exists. If the implementer cannot identify all three,
  keep the passage and report it.
- Do not replace either README with a large shared file, an on-demand companion, a feature history, or
  a second architecture/testing manual.
- Preserve all branch/deployment, authentication/authorization/identity, Quran provenance/rendering,
  migration/schema, transaction/audit/xmin/conflict, URL-state, E2E-membership, PostgreSQL/test-runtime,
  importer/refusal/rollback, typography/RTL, and destructive-operation protections.
- Keep the Abwab `URL contract`, `Gotchas / invariants`, browser-E2E contract, and reversal record
  materially unchanged. Their length is not authority to compress them.
- Keep frontend-specific Words URL/cache/UI rules locally. A pointer to the Backend Words read-model
  README may replace only backend-owned ordering/count implementation detail that an ordinary
  frontend task does not need.
- Do not change `TESTING_STRATEGY.md`, `CODING_PRINCIPLES.md`, architecture documents, Skills,
  routers, product/design owners, production code, test code, test configuration, CI/deployment,
  Spec Kit, persistent memory, or audit reports.
- Do not split or redesign `UI_STYLE_SYSTEM.md`, decide Tailwind versus qd/custom, reconcile font
  weights, or move the contrast table. The table remains the current measurement owner until a
  separate nine-row, both-theme test lands.
- Do not shorten `Backend/scripts/README.md` core mechanics,
  `Backend/tests/QuranDashboard.Tests/README.md`,
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md`, or
  `Frontend/quran-dashboard-ui/src/app/features/access-admin/README.md`.
- Do not delete any Markdown file, clean the project-simplification audit folder, or create another
  long-lived documentation category.
- No product build/test, formal review, commit, push, PR, deploy, migration, database/data change, or
  audit-folder cleanup is part of this documentation-only implementation unless separately requested.

---

## 1. Current documentation problems this plan fixes

1. `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md` is 97,791 bytes and 1,179
   lines. Its URL, conflict, cache, accessibility, E2E, and reversal contracts are protection-bearing,
   but the opening/component-map band also carries shipped-status prose, delivery sequence, mutable
   file lengths, repeated endpoint counts, and long current-behavior narratives that can be expressed
   as contract plus one-line rationale.
2. `Frontend/quran-dashboard-ui/src/app/features/words/README.md` is 37,786 bytes and 426 lines.
   Its contracts are largely unique, but they are framed by Feature/Slice/task identifiers and
   past-to-present implementation narration. It also repeats some backend-owned sorting/count
   semantics instead of distinguishing the frontend URL/cache/UI contract from the server owner, and
   its `Related` section still uses the unresolved placeholder path
   `Backend/.../Persistence/Reads/Quran/Words/README.md`.
3. `docs/contracts/security-access.md` calls itself an index but its
   `Pointers worth naming` section restates routes, response fields, readiness behavior,
   configuration, and UI behavior. The links are live; the defect is duplicated contract prose in a
   pointer-only layer, not a dangling link.
4. `Frontend/quran-dashboard-ui/src/styles/README.md` currently owns nine measured contrast rows.
   `docs/TESTING_DEBT.md` row P2 still says seven and enumerates only seven while also requiring a
   future test to cover every table row.
5. Reports 01/11's generic large-README recommendation is rejected. Direct current evidence and the
   topic/Sol reports support only Abwab and Words as shortening candidates, one preservation map at a
   time.

## 2. Target README model

An ordinary local README should contain only:

1. purpose and scope;
2. local invariants;
3. local contracts and safety boundaries;
4. local mechanics whose canonical owner is this README;
5. concise rationale that code/tests cannot safely reconstruct;
6. exact pointers to existing canonical owners; and
7. small gotchas that prevent a plausible regression.

Use these classifications:

| Classification | Meaning in this plan |
|---|---|
| `KEEP` | Preserve the current section and its protection; size alone is irrelevant. |
| `SHORTEN` | Re-register in place as current rule + concise why + stable owner/symbol/test pointer. |
| `POINTER_ONLY_SECTION` | Keep only scope/topic-to-owner routing because the detail already has a canonical owner. |
| `SPLIT_ON_DEMAND` | Consider only in a later plan when unique material has a sanctioned optional home. Not used by this implementation. |
| `DELETE` | Remove a file only when genuinely redundant and fully repointed. Not used by this implementation. |
| `NEEDS_ADJUDICATION` | Leave untouched until a separate full per-file mapping exists. |

No shortened local contract may be replaced by “see code” or “see tests.” Code/tests can prove
behavior, but the nearest README must retain non-obvious scope, safety, and why-text. Existing
canonical pointers may replace only genuine duplication.

### Expected context-path improvement

- An ordinary Abwab task follows the same native route to the same nearest README, reaches a compact
  current component/ownership map sooner, and still finds the full URL, Gotchas, E2E, and reversal
  contracts in that one file.
- An ordinary Words frontend task follows the same route to the same nearest README and keeps every
  frontend URL/cache/UI rule locally. Only a task that actually changes Backend-owned ordering/count
  behavior follows the exact Backend Words owner it already needs.
- A security/access task uses `docs/contracts/security-access.md` as one thin routing hop to the
  current owner instead of reading a second copy of response/configuration behavior.
- A future contrast-test task sees one internally consistent nine-row debt contract and its existing
  styles owner. No new test, policy, or mandatory read is introduced.

This is a shorter and less duplicative path, not a token-saving claim. The number of sources in each
native route is unchanged.

### Duplicated policy and stale-path disposition

| Current duplication or stale text | Change in this plan | Canonical owner afterward |
|---|---|---|
| Abwab component-map copies of global file-size thresholds and shared visual-primitive mechanics | Remove the copied mechanics while retaining each local cohesion exception, split trigger, and behavior | `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md` for thresholds; exact headings in `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` for shared visuals |
| Words restatement of server ordering, tie-break, primary-association, query/count, and hydration mechanics | Keep the complete frontend URL/control/cache contract; replace only server implementation detail with an exact pointer | `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md` |
| Security-index response fields, readiness behavior, startup/configuration detail, and UI rules | Replace prose with topic-to-owner pointers | Existing Authentication, Access controller/application/infrastructure, API, Frontend core, and access-admin READMEs |
| Words `Related` pointer `Backend/.../Persistence/Reads/Quran/Words/README.md` | Replace the placeholder with the exact path above | Existing Backend Words reads README |

No other stale or dangling current reference was confirmed. A new implementation-time finding stops
the plan unless it is repairable inside the same four-file allowlist without changing scope.

## 3. Evidence disposition and exact candidate set

### A. Safe to change now

| File | Decision | Bounded responsibility |
|---|---|---|
| `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md` | `SHORTEN` | Edit only the pre-URL header/purpose/component map and `Related`. Preserve the URL, Gotchas, E2E, and reversal contracts; no new document or moved rationale. |
| `Frontend/quran-dashboard-ui/src/app/features/words/README.md` | `SHORTEN` | Edit only the shared-pattern, sort/identity/result-count, association-filter, Word Types backend-restatement, and `Related` bands; repair the stale Backend Words pointer; keep the global overlay and every unlisted Gotcha unchanged. |

### B. Keep unchanged

| File | Why it stays |
|---|---|
| `Backend/scripts/README.md` core mechanics outside the two §3C sections | Canonical operational database/import/test mechanics; size is not redundancy proof. The entire file remains unedited in this plan. |
| `Backend/tests/QuranDashboard.Tests/README.md` | Unique PostgreSQL ownership, fixture, collection, and runtime safety contract. |
| `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Abwab/README.md` | Unique transaction, xmin, conflict, and writer safety contract. |
| `Frontend/quran-dashboard-ui/src/styles/README.md` | Owns the nine measured ratios, breakpoint sync, typography/RTL entry facts, and badge coupling. |
| `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` | Explicitly out of scope; no split, history extraction, font, color, or pointer repair is required here. |

All other current README/Markdown files are unchanged unless named in §3D.

### C. Needs further per-file adjudication

| Target | Why it is deferred |
|---|---|
| `Frontend/quran-dashboard-ui/src/app/features/access-admin/README.md` | Requires a complete contract/rationale map before any shortening; current structure appears protection-heavy. |
| `Backend/scripts/README.md` authorization activation/rollback and legacy-cleanup sections | Potential on-demand operator material, but no sanctioned optional owner exists and moving it would change the documentation taxonomy. |
| Paid-row narratives outside P2 in `docs/TESTING_DEBT.md` | Report 06 identifies historical bands, but this plan is not a general ledger cleanup and the exact current rows need a separate bounded map. |
| Any other large README | Neither byte rank nor heuristic uniqueness establishes a safe shortening target. |

### D. Direct stale-pointer/consistency repair only

| File | Exact repair |
|---|---|
| `docs/contracts/security-access.md` | Replace duplicated contract prose with a single thin authoritative-owner list. Keep the path and precedence; delete no owner content. |
| `docs/TESTING_DEBT.md` | Change only row P2 from seven to nine pairings and add the two omitted table rows/floors; retain floors-not-equality and both-theme requirements. |

## 4. Invariant and rationale preservation maps

### 4.1 Abwab README

Current in-repository inbound references point to the file as a whole from
`docs/contracts/abwab.md`, Backend Abwab read/write READMEs, the Frontend E2E/core/shared READMEs,
`UI_STYLE_SYSTEM.md` (announcer, nested-focus, TDZ-label, and snapshot-count rationale), and
source/spec comments. No current repository reference targets an Abwab README heading anchor. The
file path remains unchanged.

| Current section or band | Classification | Canonical owner after change | Exact information that remains | Inbound references affected | Risk if wrongly removed |
|---|---|---|---|---|---|
| Header/HOW/status/access preamble (current lines 1–24) | `SHORTEN` | Same README; architecture pointers remain pointers | Feature scope; public reads versus Backend-protected writes; capability checks shape UX but do not authorize; 401 starts login once with no mutation retry; 403 refreshes access with no retry and closes/disables stale write state; anonymous/read-only surfaces and explained restore disablement | File-level references only; no repair | Auth UX can imply client authorization or retry a mutation |
| `What this feature does` | `SHORTEN` | Same README; API methods/controllers remain executable truth | Tree/relations/templates surfaces, write families, and the rule that 409 is surfaced and never swallowed or auto-retried | None | A conflict can be hidden or retried; mutable endpoint counts drift |
| Route shell, modal URL, interactions, and overlay controllers | `SHORTEN` | Same README; file-size policy remains in `FRONTEND_STRUCTURE.md` | Each controller's one responsibility; page-provided scope; modal controller has no Router/ActivatedRoute; interactions retain permission/URL/focus orchestration; overlay-family cohesion and the URL-owned-overlay split trigger | None | State becomes root-global, Router ownership duplicates, or a split breaks responsibility |
| Toolbar and search behavior | `SHORTEN` | Same README; visual primitives remain in `UI_STYLE_SYSTEM.md` | Tree marks without filtering; cards/archive filter at current depth/shape; trimmed-query empty-state truth; 500 ms settled count announcement; manual expansion plus replaceable search-derived expansion; root-tab count differs from any-depth scope count | None | Search hides hierarchy, lies about emptiness, spams live announcements, or corrupts manual expansion |
| Tree, keyboard, relation flag, and count columns | `SHORTEN` | Same README; shared geometry points to `UI_STYLE_SYSTEM.md` | Flat ARIA tree and RTL key model; roving-tabindex exceptions; Enter-only reorder with blur/Escape cancel; menu anchor paths; always-present relation control with count and non-color empty state; three live-only counts, presentational headers plus full aria-labels; name-first width priority and synchronized responsive column/header removal | Generic file-level comments remain valid | Keyboard access, non-color meaning, focus, reorder confirmation, or count semantics regress |
| Cards, archive, side panel, move picker, restore, and sections | `SHORTEN` | Same README | Breadcrumb-owned cards empty state; sibling card/checkbox controls; archive restore rules; single reorder affordance; persistent section strip, no no-section destination, cross-section no-selection state, collapsed/search-derived destination tree, main-door option, and cycle exclusions; retired-section root restore destination; sections draft reset and live-row submit; order editor Escape propagation | None | Invalid nesting, wrong restore request, silent no-op move, cycle exposure, stale drafts, or accidental write |
| Shared door picker | `SHORTEN` | Same README | Consumer-owned selection; excluded doors remain visible at their true depth with a reason tag and disabled selection rather than being hidden with their subtree; single-selection mode uses radio semantics; unmatched search uses picker-owned no-match copy rather than the host's empty-state copy | None | Valid descendant targets disappear, exclusion meaning is lost, or the control promises the wrong selection cardinality/state |
| Shared authoring form, template modal/tree/copy, and templates page | `SHORTEN` | Same README | Shared presentational fields and labels; shell owns its write; template tree uses door-tree language but not its ARIA role; copy is live-door multi-select/all-or-nothing; page-scoped overlays versus root-scoped caches; delete guard/controller ownership | None | Shared form starts authorizing/injecting writes, caches become page-local, or template interaction semantics drift |
| Relations modal and announcer | `SHORTEN` | Same README | Four relation groups; direction wording per mode; blocked identity is pair+type and excludes anchor-pick mode; linked name and delete are separate controls; modal stays tree/URL-agnostic; exactly one failure live region, KEEP/DROP surface rule and `announceFailure` ownership; every success announces once | None | Relation direction reverses, valid anchors disappear, or failures announce twice/not at all |
| Snapshot/builder/selection/write/sections/relations/templates/url/data/model map | `SHORTEN` | Same README; server-side cache companion stays in Backend Reads/Abwab README | Unit responsibilities; builder outputs and allocation invariant; scope-owned bulk clearing; one 409/write policy; relation cache identity is door id + snapshot validator, global eviction on validator movement, forced post-write read, rename eviction pin, and explicit rejection of diagnostics-only tree version; templates cache/controller separation; seven-key fail-closed URL owner; separate data-access route families; TDZ-safe label getters | Existing generic source/spec references remain valid | Stale relation names/lists, cross-scope bulk writes, split 409 policy, wrong cache key, or TDZ label failure |
| `URL contract` | `KEEP` | Same README | Entire key/value/default table, cross-key validation, pinned relation restore subject, history semantics, reveal, scope invalidation, dirty-close, and selection-source contracts | Heading/path remain | Deep links, Back/Forward, modal restore, selection, caching, or unsaved behavior breaks |
| `Gotchas / invariants` | `KEEP` | Same README | Entire current protection-bearing section | Heading/path remain | High-risk local behavior is lost for a cosmetic reduction |
| `Browser e2e` | `KEEP` | Same README plus existing E2E/test-strategy pointers | Glob-is-membership rule, single-worker reason, coverage boundary, sandbox cleanup and fresh-version teardown | Heading/path remain | Specs silently leave the project or race global reorder; residue returns |
| `Decisions that reversed mid-series` | `KEEP` | Same README | Current decision, concise reversal reason, and stable symbol/selector anchors for all four reversals | Heading/path remain | A deliberately rejected behavior is re-derived and reintroduced |
| `Related` | `POINTER_ONLY_SECTION` | Existing `docs/README.md` lifecycle, `UI_STYLE_SYSTEM.md`, and shared README | Only current canonical pointers and the statement that this README owns current Abwab page behavior | None | A pointer dangles or current behavior is wrongly treated as historical |

Implementation rule: remove mutable line counts, shipped/arrival sequence, Slice/phase labels in the
bands being shortened, repeated endpoint totals, and past-to-present narration only after its current
rule and non-derivable reason appear in the replacement. Do not edit §4.1's four `KEEP` bands merely
to remove their historical labels.

#### Abwab shortening safety answers

1. **Value today:** the file is the only local map of page ownership plus URL, cache, accessibility,
   write-conflict, modal, and reversal behavior.
2. **Derivability:** code/specs prove much of the behavior; they do not safely reconstruct why the
   rejected alternative is wrong. Mutable LOC/count/status and delivery chronology are derivable or
   obsolete.
3. **Dependents:** nearest-README routing, `docs/contracts/abwab.md`, Backend read/write documents,
   Frontend core/shared/E2E/style documents, and generic source/spec comments.
4. **Removal risk:** loss of why-text can reintroduce silent retries, stale caches, invalid URL
   history, inaccessible controls, or previously rejected UX.
5. **Protection afterward:** all local rules and concise rationale remain in this same README; existing
   architecture/backend/test pointers retain only their already-owned detail.
6. **Inbound repair:** no path changes and no heading-anchor inbound references were found. Rerun the
   scan; any newly found anchor is repaired before heading change or the heading stays.
7. **Local sufficiency:** a local agent still sees feature scope, the compact component ownership map,
   and every high-risk contract without opening historical plans or a new companion file.

### 4.2 Words README

Current inbound references point to the whole file from `docs/README.md`,
`docs/contracts/words-explorers.md`, the Backend Words reads README, the Frontend project feature
map, Frontend Abwab/core/shared/detail-overlay READMEs, `UI_STYLE_SYSTEM.md`, and the
`words-explainer.content.ts` TDZ comment. No current repository reference targets a Words README
heading anchor. The file path remains unchanged.

| Current section or band | Classification | Canonical owner after change | Exact information that remains | Inbound references affected | Risk if wrongly removed |
|---|---|---|---|---|---|
| Header and `What this feature does` | `KEEP` | Same README | Five read-only explorers, hub, split-screen shape, and URL-owned selection/filter/paging | File-level references only | Local scope becomes unclear |
| `Shared pattern` | `SHORTEN` | Same README; generated DTO and structure detail stays in existing core/architecture owners | Per-explorer page/facade/cache/url/detail-loader/API/model/mapper responsibilities; Word Types view-model exception; shared explorer utilities/components; current `qd-page-frame` ownership and compatibility alias | None | A new explorer bypasses the stable local pattern or removes the live alias |
| `Global entity-detail overlay (Feature 029, Change B)` | `KEEP` | Same README plus existing core detail-overlay README | Entire current host/lazy-adapter/controller/focus/history/frameless/identity/link/ayah-continuity contract | Heading/path remain | Eager bundle growth, page/overlay state collision, double focus traps, broken history, or lost ayah continuity |
| Centralized table/list visuals, mounted state shells, and Unique Words not-found exception | `KEEP` | Same README; visual geometry remains in `UI_STYLE_SYSTEM.md` | Entire current shared-class, mounted-shell, error/not-found ownership, and Unique Words exception contract | None | Layout shifts, populated tables disappear, or duplicate messages return |
| TDZ getters, base URL-state rule, and responsive sort fallback | `KEEP` | Same README | Entire current TDZ, stable-param, desktop-header, and phone/tablet fallback contract | None | Shared links, mobile sorting, or labels regress |
| Sort grammar/default/cache bullet | `SHORTEN` | Same README for frontend behavior; Backend Words reads README for server ordering/tie-break semantics | Client exact/fail-closed parsing; bare-token natural direction and opposite-direction suffix; bare-only `mushaf-order`; canonical default absence; page reset; cache-token behavior; frontend sortable-column allowlists; related-column exclusions; and Word Types cycle/default exception. Server parser/tie-break/query detail becomes an exact existing pointer | None | URL/cache compatibility or frontend/backend token semantics break |
| Identity and headline result-count bullets | `SHORTEN` | Same README plus Backend Words identity owner and shared result-count README | Uthmani-display versus clean-imlaei identity, the four pages using the headline total, Word Types' separate summary, state behavior, and exact existing owner pointers | None | Word identity or count-surface ownership becomes ambiguous |
| Count ranges, custom commit, ayah chips, and controls-layout bullets | `KEEP` | Same README | Entire current threshold/URL/cache/commit/no-op/responsive/popover keyboard contract | None | Extra navigation/fetches, dead filters, URL drift, or inaccessible controls return |
| Association-filter bullet | `SHORTEN` | Same README for client rules; Backend Words reads README for primary/dominant association semantics | URL keys, frontend fail-closed parsing, page reset/cache fragment, picker sources, offered POS scope, and user-facing labels; server selection/order semantics become an exact owner pointer | None | Filters disagree with displayed identity or cross-serve cache entries |
| Ayah cards, Words explainer/hub, and local testing sentence | `KEEP` | Same README plus existing shared/style/testing owners | Entire current Quran row identity/rendering, first-paint/preference/content safety, and local testing pointer | None | Quran rendering, first-paint stability, approved examples, or test-safety discovery regresses |
| Word Types table/detail contract through grouped cache identity (current lines 326–372) | `KEEP` | Same README | Entire current placement/mounted-shell, selection, URL snapshot, focus/highlight, display-only row, pagination, cache, and terminology contract | None | Detail state crosses scopes, URL restore corrupts, or display-only rows become actions |
| Word Types search/presence/scoped-count/page-size/grouped-read band (current lines 373–419) | `SHORTEN` only for backend restatement | Same README for frontend URL/control/state/cache/API-client contract; Backend Words reads README for server query/count formulas | Search and presence URL/control scope; detail-preservation/clear rules; scoped-count placement/equality/loading/retry and frontend cache scope; 1000/100 sizes and virtual scroll; grouped-client request/cache dimensions. Server SQL/count/hydration formulas become exact owner pointers | None | Count families conflate, caches cross-serve, or frontend state no longer matches server scope |
| `Related` | `POINTER_ONLY_SECTION` | Exact Backend Words reads README, contracts index, current lifecycle owner | Replace `Backend/.../Persistence/Reads/Quran/Words/README.md` with `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md`; keep current frontend/backend/index ownership pointers only and remove completed-feature history | No inbound heading anchor; destination already exists | Ownership stays ambiguous or the stale placeholder survives |

The Words rewrite removes Feature/Change/Slice/task IDs and past-to-present narration only inside the
explicit `SHORTEN` bands above, and removes backend implementation narrative only where the
replacement retains the current frontend rule plus an exact existing owner pointer. Every unlisted
Gotcha and the global overlay section remain unchanged. It must not delete a URL key, token, default,
cache dimension, accessibility rule, Quran-display protection, page-size boundary, error/empty
ownership rule, or non-obvious no-op behavior.

#### Words shortening safety answers

1. **Value today:** the file is the nearest owner of frontend explorer/overlay URL, state, cache,
   rendering, accessibility, and Quran-display behavior.
2. **Derivability:** present component names and wire mechanics are partly derivable; intent behind
   focus traps, mounted shells, URL canonicalization, display-only rows, and no-op behavior is not.
   Feature IDs and delivery chronology provide no current protection.
3. **Dependents:** nearest-README routing, docs/contract indexes, Backend Words readers, the Frontend
   project map, shared/core/Abwab/detail-overlay/style docs, one TDZ production comment, and future
   explorer work.
4. **Removal risk:** URL/cache incompatibility, extra HTTP/history churn, focus/overlay regressions,
   hidden mobile controls, count-family confusion, or Quran rendering changes.
5. **Protection afterward:** frontend rules and concise rationale remain in the same README. Only
   server query/tie-break formulas defer to their existing Backend owner, and only cross-stack work
   needs that already-relevant read.
6. **Inbound repair:** no path changes and no heading-anchor inbound references were found. Preserve
   or repair any anchor discovered by the implementation-time scan.
7. **Local sufficiency:** an ordinary frontend task can still identify the responsible unit and all
   frontend behavior locally; no historical plan or new companion is required.

### 4.3 Direct repair preservation map

| Current passage | Classification | Canonical owner after change | Exact information preserved | Inbound references affected | Risk if wrongly removed |
|---|---|---|---|---|---|
| `docs/contracts/security-access.md` `Authoritative sources` | `KEEP` and extend pointer list | Existing auth, controller, application security, infrastructure access, API startup, core session/catalogue, and access-admin READMEs | Topic-to-owner routing and precedence; no routes, DTO fields, counts, configuration values, or behavior restatement | Path references from contract index, Abwab contract page, native area routers, and review Skills remain valid | Security work routes to the wrong owner |
| `docs/contracts/security-access.md` `Pointers worth naming` | `POINTER_ONLY_SECTION` folded into the owner list | Same existing owner READMEs | Catalogue/readiness, generated permission codes, startup sync/health, fail-closed editor, audit projection, and identifier-display topics remain discoverable as owner labels only | No inbound heading anchor found | Duplicated field/behavior prose drifts or an owner becomes undiscoverable |
| `docs/TESTING_DEBT.md` row P2 | Direct consistency repair; otherwise `KEEP` | Styles README owns measured rows; P2 owns the concrete future-test obligation | All existing seven floors; add the zero tab-count `--qd-text-muted` on `--qd-bg` at >= 4.5:1 and the selected tab-count `--qd-accent-text` on `--qd-surface` at >= 7:1; assert all nine rows, floors not exact equality, in both themes; future test path/trigger unchanged | Styles README's row-P2 pointer remains valid | A future test implements only seven cases and falsely closes the debt |

The security repair removes no protection: the detailed catalogue/readiness behavior remains at
`Backend/infrastructure/QuranDashboard.Infrastructure/Access/README.md`, startup behavior at the API
README, generated-code mechanics in the Frontend core README, and UI failure/identifier behavior in
the access-admin README. P2 changes no token, ratio, test, test trigger, or style behavior.

## 5. Exact implementation file set

### Modify

| File | Exact responsibility |
|---|---|
| `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md` | Apply only §4.1: compact header/purpose/component map and pointer-only `Related`; preserve the four protected bands. |
| `Frontend/quran-dashboard-ui/src/app/features/words/README.md` | Apply only §4.2's named `SHORTEN` bands; preserve the global overlay and every unlisted Gotcha. Replace only true backend duplication with existing pointers. |
| `docs/contracts/security-access.md` | One thin owner list, index precedence unchanged, no contract restatement. |
| `docs/TESTING_DEBT.md` | Row P2 nine-row reconciliation only. |

### Create

None.

### Delete

None.

These four paths are the implementation allowlist. This approved plan artifact may coexist in the
cumulative branch diff but is not an implementation path. No other README, contract index page,
architecture/policy/product/design file, test, production file, audit report/data file, router, Skill,
or persistent-memory path may change.

## 6. Small sequential implementation steps

### Step 1 — Freeze scope, state, and references

- [ ] Confirm the branch is not `main`, capture one root `git status --short`, and freeze §5's
  four implementation paths.
- [ ] Stop if either candidate README or either repair passage has overlapping user edits.
- [ ] Record current line/byte counts as observations only. Do not derive a reduction target.
- [ ] Rerun inbound scans for all four paths and for heading anchors. Record any new reference before
  editing; repoint first or preserve the heading.
- [ ] Confirm every destination named in §4 exists. Record the Words `Backend/...` placeholder as the
  one planned stale-path repair; any additional stale/dangling pointer is a stop condition. Historical
  audit references remain historical and are not rewritten.

### Step 2 — Re-register the Abwab pre-URL map

- [ ] Rewrite only the header/purpose/component-map bands and `Related` according to §4.1.
- [ ] For every component/state bullet, retain responsibility, current contract, concise
  non-derivable why, and stable symbol/test/owner pointer. Remove only counts, line snapshots,
  delivery order, phase labels, and repeated prose.
- [ ] Leave `URL contract`, `Gotchas / invariants`, `Browser e2e`, and
  `Decisions that reversed mid-series` materially unchanged.
- [ ] Run the Abwab preservation comparison before touching Words. If any §4.1 fact cannot be located
  in the result, restore it rather than compensating with a new file or broader pointer.

### Step 3 — Re-register the Words current-state contract

- [ ] Apply §4.2 only to the named `SHORTEN` bands. Remove Feature/Slice/task framing and past
  implementation narration there; retain every current frontend behavior and concise reason.
- [ ] Keep the frontend sort/URL/cache/default/fail-closed/mobile contract local. Replace only
  backend-owned query/tie-break/count formulas with the existing Backend Reads/Quran/Words pointer.
- [ ] Replace the `Related` placeholder with the exact repository path in §4.2.
- [ ] Leave the global entity-detail overlay and every unlisted Gotcha unchanged; add no new mandatory
  document.
- [ ] Run the Words preservation comparison. If an ordinary frontend task now needs historical plans
  or a broad search to recover a mapped fact, restore the local fact.

### Step 4 — Repair the two direct inconsistencies

- [ ] Collapse `docs/contracts/security-access.md` to one authoritative owner list. Preserve
  precedence and every needed topic-to-owner route; remove all response-field/route/config/behavior
  restatement.
- [ ] Change only P2 in `docs/TESTING_DEBT.md`: seven to nine and add the two omitted token/surface
  floors while retaining floors-not-exact, both-theme, path, and payment-trigger text.
- [ ] Confirm the styles README, access-admin README, API README, infrastructure Access README, core
  README, and all tests remain unchanged.

### Step 5 — Verify the cumulative documentation result

- [ ] Run §7's path, whitespace, inbound-reference, heading/link, and preservation checks.
- [ ] Evaluate §7's nearest-README scenarios as a tabletop preservation comparison. Do not start fresh
  Claude/Sol sessions: routing and expected reads do not materially change.
- [ ] Fix only documentation defects inside §5. An additional required owner/path or a lost local
  invariant is a stop condition, not authority to widen the allowlist.
- [ ] Confirm the final diff contains only §5's four implementation paths plus this approved plan.
  Run no product build/test, formal review, Git delivery, deploy, database, or release action.

## 7. Focused documentation verification

### Static and reference checks

1. `git status --short`, tracked path lists, and untracked path lists contain only §5 plus this
   plan. `git diff --check` passes. Any untracked file is checked with
   `git diff --no-index --check -- /dev/null <path>`; exit 1 means different, while output is a
   whitespace diagnostic.
2. Rerun whole-repository scans with hidden tracked directories included and `.git`, the audit
   folder, and dependency output excluded:
   - `(features/)?abwab/README[.]md|src/app/features/abwab/README[.]md`;
   - `(features/)?words/README[.]md|src/app/features/words/README[.]md`;
   - `docs/contracts/security-access[.]md`; and
   - `TESTING_DEBT[.]md` plus row P2.
   Cover every relative spelling, including plain `words/README.md`, and classify references from
   architecture files and production/test comments as well as Markdown. Every current path still
   resolves; no implementation-time anchor may dangle.
3. The Abwab README still contains exactly one each of these stable headings:
   ``URL contract (`state/abwab-url-sync.ts`)``, `Gotchas / invariants (read before changing)`,
   `Browser e2e (Slice B2)`, and `Decisions that reversed mid-series`. Diff inspection shows no
   material contract change inside those four bands.
4. Abwab rule-preservation review checks every §4.1 row, including public/protected access,
   no-retry 401/403, 409 surfacing, component provider/router boundaries, search behavior, keyboard/
   relation/count semantics, move/restore/section behavior, door-picker visibility/radio/no-match
   contracts, announcement ownership, selection invalidation, relation cache validator/rename pin,
   and TDZ labels.
5. Words rule-preservation review checks every §4.2 row, including lazy overlay adapters, controller
   isolation, one focus trap, history routing, frame/link semantics, mounted-shell/error ownership,
   client sort/URL/cache/default rules, mobile fallback, range/association behavior, no-per-keystroke
   commits, ayah/Quran rendering, explainer first-paint behavior, Word Types snapshots/details/counts/
   page sizes, and grouped cache dimensions.
6. A search under `docs/contracts/` finds no `assignmentReady`, audit display/email DTO fields,
   `{ items` response shape, or configuration value copied from the security owners. The security
   page still links to Authentication, Access controllers, Application Security, Infrastructure
   Access, API startup, Frontend core, and access-admin owners.
7. Styles README still has nine measured table rows and is unchanged. P2 says nine, names both omitted
   pairs, retains AA/AAA floors as applicable, requires floors rather than exact equality, requires
   both themes, and retains the same proposed test path and paying trigger.
8. The Words `Related` section contains the exact
   `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md`
   path and contains no `Backend/...` placeholder.
9. No new Markdown file, shared policy card, on-demand companion, history document, testing summary,
   architecture restatement, or mandatory read was introduced.
10. The explicitly kept files in §3B and deferred material in §3C are unchanged.
11. Record before/after byte and line counts only as observations. A smaller file fails if a mapped
    protection is missing; a larger-than-expected file passes if every retained line earns its local
    read.

### Nearest-README tabletop routing scenarios

Evaluate each row against the changed text without opening a fresh agent session. Start from the
current native route, then use the nearest relevant README and an exact existing owner pointer only
where that README explicitly requires it. Record the expected owner, current contract, non-obvious
rationale, and refused extra reads.

| Prompt | Required result |
|---|---|
| “I need to change Abwab modal restore/reveal URL synchronization.” | Select the Abwab README; recover all seven keys, door/modal cross-key validation, pinned `relations-<id>-closed` subject, history push/replace behavior, scope invalidation, dirty URL-close rule, and URL-authoritative selection without historical plans. |
| “I need to change the Abwab move picker and relation announcements.” | Select the compact component map plus retained Gotchas; recover section/main-door/cycle/search rules and the one-live-region KEEP/DROP ownership without opening a new companion document. |
| “I need to change Words sorting in the frontend.” | Recover local token grammar, canonical/default/fail-closed/page-reset/cache behavior and mobile fallback from Words; defer only server ordering/tie-break implementation to the existing Backend reads owner. |
| “I need to change the global Words detail overlay.” | Recover lazy adapter, component-scoped controller, shared cache, one-focus-trap, no-Router, history, frameless, identity, cross-link, and ayah-continuity contracts locally. |
| “Where is permission-catalogue readiness or audit display identity owned?” | Use the security index only as a thin router and select the exact existing Infrastructure/API/core/access-admin owner; do not treat the index as contract truth. |
| “What must a future contrast test cover?” | Read P2 and the styles table; answer nine rows, the two formerly omitted pairs, floors-not-equality, and both themes. Do not claim the test exists or move the measurements. |

Selecting a historical plan, a new mandatory document, the wrong owner, or an incomplete protected
contract fails the tabletop scenario.

## 8. Safety rules that must remain

- `main` remains protected Railway production; documentation work does not authorize Git delivery or
  deployment.
- The nearest README remains current local truth and is updated in place; contract indexes stay thin
  and defer to code plus nearest README.
- Backend authorization remains the final write authority; frontend capability checks only shape UX.
- Abwab 409/xmin/conflict, URL-state/history, cache invalidation, transaction-facing behavior, E2E
  membership/PostgreSQL serialization, and destructive cleanup facts remain reachable.
- Words clean-imlaei identity, deterministic ordering ownership, shareable URL/cache contracts,
  accessibility/focus, Quran rendering, count-family separation, and page/detail identity remain
  reachable.
- The nine measured contrast rows remain in the styles README until an actual nine-row, both-theme
  test exists. P2 remains open debt, not evidence.
- No branch/deployment, Quran/source, migration/schema, audit/transaction, importer/refusal/rollback,
  typography/RTL, test-runtime, or operational/destructive command protection is removed.

## 9. Explicit non-goals

- No root/area `CLAUDE.md` or `AGENTS.md` change and no routing-model redesign.
- No Skill, Testing Strategy, Engineering Review, Spec Kit, Git workflow, PR, deployment, CI, or
  persistent-memory change.
- No production/test/API/schema/database/migration/import/Quran/style/token/Tailwind/qd behavior change.
- No test implementation, including `token-contrast.spec.ts`; this plan only corrects P2's stated
  future coverage.
- No `UI_STYLE_SYSTEM.md` split/historical extraction, font-weight decision, allowed-green merge,
  color change, or browser geometry work.
- No scripts runbook relocation, access-admin shortening, Backend tests README shortening,
  Writes/Abwab shortening, paid-debt ledger cleanup, or generic top-N README pass.
- No deletion of plans, reports, audit data, the audit folder, or any current Markdown file.
- No byte, line, token, or percentage reduction target.
- No fresh Claude/Sol probe matrix; routing and expected reads stay unchanged.

## 10. Stop conditions

Stop and report rather than broaden or weaken the plan when:

1. The branch is `main`, user changes overlap §5, or the implementation cannot stay within the four
   paths plus this separately approved plan artifact.
2. A proposed cut has unique behavior or rationale with no exact surviving sentence and owner.
3. A local task would need a historical plan, Git history, broad search, or new mandatory document to
   recover a §4 contract.
4. A new inbound heading/path reference appears and cannot be repaired within §5 without changing its
   meaning.
5. The current code/README contradicts a preservation-map row. Keep the text and request a plan
   correction; do not silently choose a new contract.
6. Security/access, Quran/source/rendering, migration/schema, transaction/audit/xmin/conflict,
   URL/history, cache, E2E membership, PostgreSQL/test-runtime, importer/refusal/rollback,
   typography/RTL, or operational safety would be weakened.
7. The security index needs contract prose to stay useful rather than an exact owner pointer; report
   the missing owner instead of recreating the duplicate.
8. P2 cannot describe all nine current rows without changing a token, ratio, floor, theme requirement,
   test path, or test behavior.
9. Completion would require a router, Skill, strategy, review, architecture, style-system,
   production/test/configuration, Git/PR/deploy, database/data, persistent-memory, or audit-cleanup
   change.

Implementation is complete only when the two nearest READMEs contain the same local protections and
enough concise rationale for ordinary work, delivery chronology and duplicated policy are removed
only where safely mapped, the security index is pointer-only, P2 matches all nine measured rows,
every inbound reference resolves, the tabletop scenarios recover the protected contracts without
broad search and preserve the same context path, and the cumulative diff contains only §5's four
paths plus this plan.
