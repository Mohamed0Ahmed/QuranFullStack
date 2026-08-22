# Engineering Review

- Review date: 2026-08-22
- Review mode: initial project-wide production-code review
- Current-state identity: `dev@1bcf56b1586153a65ee0cf74f0e944d3c04d3ed8`

## 1. Verdict

**CHANGES REQUESTED**

The current production code contains evidence-backed MAJOR findings in Quran-data provenance and
Arabic-field safety, protected-state publication, swallowed durable-recovery failures, feature/layer
coupling, duplicated feature stacks, and several classes that cross or effectively reach hard
responsibility thresholds. The review also found removable declarations, narrow but repeated
infrastructure/UI plumbing, and widespread production-comment debt.

## 2. Scope reviewed

This is a whole-current-state review, not a feature diff or re-review.

- Backend: **1,270** non-generated production `.cs` files / **82,785 LOC** under `Backend/api`,
  `Backend/application`, `Backend/domain`, `Backend/infrastructure`, `Backend/shared`, and production
  tooling under `Backend/tools`.
- Frontend: **762** tracked production `.ts`, `.html`, and `.scss` files / **77,880 LOC** under
  `Frontend/quran-dashboard-ui/src`, excluding tests and generated API clients and including
  application bootstrap, tracked permission-code output, and global style partials.
- Total: **2,032 files / 160,665 LOC**.
- Review execution: three parallel reviewer scopes for Backend, then three parallel reviewer scopes
  for Frontend, followed by parent-level de-duplication and manual verification of retained
  candidates.
- Static traversal included file/responsibility thresholds, exact clone windows, normalized
  feature-stack similarity, declarations/repository-wide references, production comments,
  catches/error handling, parameter counts, imports/layer direction, and direct data/state
  ownership.

Consulted context was limited to the implicated headings of:

- `CODING_PRINCIPLES.md` §§2–4, §7, and §10.
- `Backend/.architecture/BACKEND_STRUCTURE.md` responsibility and file-size guidance.
- `Backend/.architecture/CLEAN_ARCHITECTURE.md` dependency direction and data-access ownership.
- `Frontend/quran-dashboard-ui/.architecture/FRONTEND_STRUCTURE.md` responsibility, page/state, and
  file-size guidance.
- `Frontend/quran-dashboard-ui/.architecture/API_INTEGRATION_GUIDELINES.md` API/facade ownership.
- `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md` organization and Quran display
  safety.
- The Engineering Review clean-code traversal aid, AI-failure modes, and Quran-data-safety
  reference.

Explicitly excluded by request:

- Tests and E2E files, test-quality review, and Test Guard.
- Spec Kit artifacts, feature plans, and contracts.
- Generated code, build output, EF generated migration artifacts, dependency audit, and Git/PR work.
- Builds, typechecks, tests, analyzers, browser checks, database commands, and runtime verification.

The worktree was clean at review start. No application file was edited.

## 3. Findings

### BLOCKING

None.

### MAJOR

#### ER-1 — Approved I'rab catalog has no traceable source

- Path: `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/SimpleI3rabGeneration/I3rabRuleCatalogSeedData.cs:8`
- Evidence: 142 Arabic I'rab/morphology rules are embedded in source and marked `Approved`.
  `I3rabRuleSeedRow.cs:3` stores the signature, Arabic label, status, description, and order but no
  source package, manifest, checksum, or provenance identity. `I3rabRuleCatalogSeed.cs:9` consumes
  the in-code catalog directly.
- Why it matters: I'rab is source-sensitive. The production model cannot trace an approved religious
  label to source evidence, so code alone cannot distinguish curated truth from manually authored
  content.
- Suggested direction: preserve the values but place the catalog in a staged, manifested,
  checksummed source artifact with catalog provenance and fail-closed loading. Refusing generation
  when provenance is absent is a behavior change and needs explicit implementation approval.

#### ER-2 — Missing Arabic morphology is silently replaced by Buckwalter text

- Paths: `EnrichedDimensionBuilder.cs:336-344`, `EnrichedDimensionBuilder.cs:430-459`,
  `MorphologyBulkCopier.cs:31-74`.
- Evidence: missing `LemmaArabic` becomes `lemmaBuckwalter`; missing `RootArabic` becomes
  `rootBuckwalter`; those values are then written into `lemma_text`/root display fields. The hard
  checks validate selected corrections but do not reject this general fallback.
- Why it matters: an unknown Arabic value becomes a plausible non-Arabic display value instead of a
  controlled missing/invalid outcome, hiding source uncertainty.
- Suggested direction: represent missing Arabic explicitly and make it a hard validation/report
  outcome before persistence. Do not substitute Buckwalter into an Arabic display field. This is a
  behavior change and requires source-aware implementation.

#### ER-4 — Access Admin can republish protected data after access is cleared

- Path: `Frontend/quran-dashboard-ui/src/app/features/access-admin/state/access-admin.facade.ts:104-188,362-366`.
- Evidence: list/detail requests publish when their request version still matches, but
  `clearProtectedState()` clears signals without advancing those versions or re-checking
  `canAccess()` before publication. Permission-catalog loading also has no publication epoch.
- Why it matters: a response already in flight can restore protected rows/details after an
  authorization transition or forbidden operation cleared them.
- Suggested direction: invalidate all protected request epochs before clearing, guard publication by
  current authorization, and reset related loading/error/mutation state. This is a behavior-changing
  correctness fix.

#### ER-5 — Durable Linking recovery swallows failures and exposes no recovery error

- Path: `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-recovery.store.ts:209-253`.
- Evidence: both the recovery queue and each reconciliation end in `.catch(() => undefined)` or an
  empty broad `catch`; the store exposes records/recovering/pending but no failure state or bounded
  retry.
- Why it matters: a durable preparation/confirmation receipt can remain open and stuck while the UI
  presents an idle state, making failure indistinguishable from no work.
- Suggested direction: retain the receipt, publish a controlled recovery-failure state, and add a
  bounded retry or explicit retry action. This is a behavior change.

#### ER-6 — Abwab and Linking have a bidirectional feature dependency

- Paths: `abwab-door-link-copy.controller.ts:3-20`, `abwab-door-links.models.ts:3`,
  `abwab-door-link-editor.component.ts:8-9`, `linking-workflow.facade.ts:2,35-47`, and
  `linking-door-step.component.ts:3-10`.
- Evidence: Abwab imports Linking workflow/models/UI while Linking imports Abwab state and the Abwab
  management picker. A runtime path is
  `AbwabDoorLinkCopyController -> LinkingWorkflowFacade -> AbwabSnapshotFacade`.
- Why it matters: neither feature has a stable ownership boundary; lazy-loading, maintenance, and
  independent evolution are coupled in both directions.
- Suggested direction: keep one explicit integration boundary, move genuinely neutral ayah/link
  primitives to shared ownership, and eliminate the reverse feature-internal import without changing
  the existing modal workflow.

#### ER-7 — Core app chrome depends directly on Linking feature state

- Path: `Frontend/quran-dashboard-ui/src/app/core/layout/top-navbar/top-navbar.component.ts:28-30,62-78,194-202`.
- Evidence: `core/layout` imports and orchestrates three concrete Linking services in addition to
  menu, focus, responsive, auth, theme, and sheet behavior.
- Why it matters: the core shell cannot evolve independently of a feature, and the navbar has gained
  a feature workflow responsibility.
- Suggested direction: invert the dependency through a core navigation-action port/slot, with the
  Linking feature providing the action and count. Preserve access, count, focus, and open behavior.

#### ER-8 — Four Quran pipeline services cross hard responsibility thresholds

- `MorphologyValidationRunner.cs` — 781 lines: several independent validation families plus SQL
  readers.
- `MorphologyAssembler.cs` — 701 lines: full assembly, segment/dimension resolution, render
  collection, and projections.
- `EnrichedDimensionBuilder.cs` — 693 lines: mutable build state, identity allocation, mapping, DTO
  projection, and internal models.
- `SqlDisplayWordsRebuilder.cs` — 542 lines: SQL catalog, transaction execution, validation, outcome,
  totals, and command helpers.
- Why it matters: the Backend service hard threshold is 450 lines, and each file has multiple
  independent reasons to change.
- Suggested direction: split by the evidenced check families, dimension/segment projection,
  build-state, and execution/validation/outcome responsibilities. Preserve validation order,
  atomicity, source checks, and reports.

#### ER-9 — Backend mutation writers/stores remain overloaded

- `EfAbwabDoorsWriter.cs` — 757 lines, above the 600-line repository hard threshold; it owns CRUD,
  bulk lifecycle, hierarchy, archive traversal, ordering, aliases, projection, and exception mapping.
- `EfLinkingPreparedPreflightStore` — 1,967 aggregate lines across six partial files; it owns
  submission/status/cancel, worker leasing/progress, result persistence/finalization, detail reads,
  and retention maintenance.
- `EfLinkingConfirmationJobStore` — 982 aggregate lines across four partial files with the same
  lifecycle/worker/result/maintenance spread.
- Why it matters: partial files reduce visual size without reducing reasons to change; locking,
  lifecycle, reads, workers, and retention remain coupled to one class.
- Suggested direction: retain current public interfaces as facades and delegate to focused workflow,
  lease, result/detail, hierarchy/order, alias, and maintenance collaborators.

#### ER-10 — Linking/Abwab state owners combine independent workflows

- `linking-workflow.facade.ts` — 599 lines: 12 collaborators and source setup, door navigation,
  overlay/focus, preparation, execution, synchronization, and copy completion.
- `linking-workspace.store.ts` — 598 lines: actor/session state, CRUD/selection, surface navigation,
  source configuration, persistence queue, and undo/restore.
- `linking-recovery.store.ts` — 549 lines: recovery orchestration, capacity policy, IndexedDB,
  cross-tab coordination, leases, serialization, hashing, and codecs.
- `abwab-page-overlays.controller.ts` — 514 lines: create/edit/archive/bulk/move/restore/sections/
  relations/context-menu workflows.
- Why it matters: all exceed the 400-line state-owner soft threshold and each has concrete workflow
  split points.
- Suggested direction: split by state slice/workflow and retain a thin coordinating facade.

#### ER-11 — Roots, Lemmas, and Stems are copied explorer stacks

- Paths include `roots-detail.controller.ts`, `lemmas-detail.controller.ts`,
  `stems-detail.controller.ts`, their detail facades/loaders, explorer facades/pages/templates,
  table components, and three ayah mappers.
- Evidence: normalized pairwise similarity is 89.9–98.4% for the 372/380/375-line detail
  controllers, 89.0–98.5% for view loaders, 90.9–94.0% for explorer facades, and 78.1–93.5% for
  the three 300+ line templates. The ayah mappers are exact entity-name variants.
- Why it matters: state transitions, pagination, URL behavior, loading, and errors must be changed in
  three large places and can drift despite an existing abstract detail controller.
- Suggested direction: extend the shared state machine with focused entity capabilities, extract
  shared presentation/interactions, and consolidate structurally identical mappers. Avoid a single
  boolean-driven universal page.

#### ER-12 — Mushaf and Word Types state files cross responsibility thresholds

- `mushaf-reader.facade.ts` — 589 lines, near the 600-line hard threshold; it combines route/session,
  page/prefetch/focus, selection, and multiple study-resource lifecycles.
- `word-types-url-sync.ts` — 382 lines, above the 300-line utility hard threshold; it combines parse,
  serialize, compatibility, detail scope, identity, deep links, and normalization.
- `word-types-detail.facade.ts` — 535 lines; orchestration plus a large pure identity/selection mapping
  section.
- `word-types-explorer.facade.ts` — 446 lines; list/tree and scope-count workflows.
- Suggested direction: keep page-facing facades, but extract route/session, page/focus, resource,
  URL-codec, identity-mapping, and scope-count owners along the existing workflow boundaries.

#### ER-13 — Abwab labels and forwarding getters create a large avoidable surface

- Path: `Frontend/quran-dashboard-ui/src/app/features/abwab/models/abwab.labels.ts:80-478`.
- Evidence: the helper is 479 lines, above the 300-line hard helper threshold, and mixes all Abwab
  surfaces. Review found 183 component getters whose body only forwards one label, including 24 in
  `abwab-templates-page.component.ts:104-127`.
- Why it matters: unrelated text owners share one catalog, while proxy getters add production members
  without behavior and inflate components.
- Suggested direction: split labels by surface, expose the applicable label object directly to
  templates, and remove forwarding getters without changing text.

#### ER-14 — Two near-hard Abwab components own workflow/state responsibilities

- `abwab-tree.component.ts` — 398 lines plus a 252-line template; expansion, selection, bulk
  selection, context intent, links/relations, ordering, keyboard, focus, and direction.
- `abwab-relations-modal.component.ts` — 395 lines; presentation plus loading, grouping, selection,
  exclusion, mutations, destructive confirmation, retry, and reset state.
- Why it matters: both are at the 400-line component hard boundary and have concrete presentation vs
  workflow split points.
- Suggested direction: extract a focused tree-row/order editor and move relation loading/mutation/
  draft state into a controller/store, leaving presentational intent emission in the modal.

### MINOR

#### ER-15 — Verified dead production surfaces remain

- Backend examples with declaration-only repository searches:
  `Backend/application/QuranDashboard.Application/Linking/LinkingOperationValidation.cs:8-203`,
  `Backend/application/QuranDashboard.Application/Linking/LinkingPreflightProjection.cs:6-71`, and
  `Backend/domain/QuranDashboard.Domain/Quran/Words/Morphology/MorphologicalCase.cs:3-8`.
- Frontend example: the unused
  `Frontend/quran-dashboard-ui/src/app/features/words/components/type-distribution-list/type-distribution-list.component.ts:20-42`;
  only its dead global selector refers to the component. Other candidates include most of
  `explorer-table-scroll.ts`, `unique-words-surahs.ts`, `deep-link-href.ts`,
  `ModalScrollLockDirective`, unused breakpoint helpers, deep-link builders, Linking model exports,
  and several sort/view/label/predicate declarations.
- Why it matters: obsolete paths and speculative exports can drift from live behavior and keep files,
  selectors, and public surface alive.
- Suggested direction: delete declaration-only private/internal code now; confirm public/exported
  removal with compiler-backed evidence during implementation. Remove the out-of-scope global style
  selector with its dead component.

#### ER-16 — Narrow operational/UI knowledge is duplicated

Concrete retained groups include:

- Identical Npgsql command executors at
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Translations/TranslationCommandExecutor.cs:3-40`,
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Tafsirs/TafsirCommandExecutor.cs:3-40`,
  and
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/FullI3rab/FullI3rabCommandExecutor.cs:3-40`.
- Exact Roots/Lemmas/Stems cache-entry option implementations.
- The destructive Linking unit-graph guard/delete sequence in three mutation paths.
- Required-active-user and lifecycle-error factories copied across Linking/Abwab controllers.
- The same `matchMedia` lifecycle, for example at
  `Frontend/quran-dashboard-ui/src/app/features/words/components/roots-table/roots-table.component.ts:156-162`,
  `lemmas-table/lemmas-table.component.ts:120-126`, and
  `stems-table/stems-table.component.ts:124-130`.
- Duplicate Abwab overlay hosts and dirty-authoring modal lifecycle.
- Four copies of API-response message extraction.

Why it matters: these are shared knowledge rather than coincidental markup; schema, timeout,
authorization, responsive, dismissal, and error-shape behavior can drift.

Suggested direction: extract only narrow shared primitives with identical semantics. Keep
feature-specific mapping/orchestration explicit and avoid one giant generic pipeline/page/modal.

#### ER-17 — Source-integrity verification relies on hidden mutable call order

- Representative paths:
  `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Translations/TranslationImportSource.cs:13,38,120-127`,
  `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Navigation/NavigationMetadataImportSource.cs:10,29,54-61`,
  and
  `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Enriched/EnrichedMorphologyImportSource.cs:11,35,84-94`.
- Evidence: each interface separates `LoadAsync` from `SourceUnchangedAsync`; implementations store
  captured digests in mutable instance state. Correctness assumes load and verification happen once,
  on the same instance/path, without an intervening load.
- Suggested direction: return an immutable loaded-package/snapshot value containing data and captured
  digests, then verify that explicit snapshot while preserving fail-closed behavior.

#### ER-18 — Import outcome control flow compares exception prose

- Evidence: `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Translations/ImportTranslationsHandler.cs:58,100`,
  `Tafsirs/ImportTafsirsHandler.cs:52,94`, and `FullI3rab/ImportFullI3rabHandler.cs:52,94` catch
  `InvalidOperationException` only when `ex.Message` equals an invariant string; the paired import
  sources/writers throw that user-facing prose.
- Why it matters: editing wording can bypass the intended refusal/report path.
- Suggested direction: use typed refusal exceptions or a closed reason enum while preserving outward
  messages and report verdicts.

#### ER-19 — Several lower-level ownership and abstraction seams are misplaced

- `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:6-410` is a 410-line cross-feature
  message/mapping class spanning system, Quran/Words, Access, Abwab, and Linking.
- `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingSourceCacheKeys.cs:1-17`
  imports the generic SHA/encoding helper from Word Types.
- Linking repository interfaces have one implementation but consumers inject the concrete class;
  `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/http-linking-workspace.repository.ts:29-273`
  also mixes HTTP with extensive UI mapping.
- `IClientIpResolver` has one same-layer implementation and no substitute.
- Suggested direction: move feature-specific messages/mappers to their owners, move generic hashing
  to neutral cache ownership with byte-identical output, separate Linking API from pure mapping, and
  remove unused abstraction seams.

#### ER-20 — Several soft-threshold coordinators still have avoidable breadth

- `Backend/application/QuranDashboard.Application/Access/OwnerReconciliation/OwnerReconciliationService.cs:46-357`:
  lease orchestration, remote profile/policy evaluation, mutation planning, audit construction, and
  result mapping.
- `Backend/application/QuranDashboard.Application/DependencyInjection.cs:125-273`: one registration
  method spanning every Application feature.
- `Backend/tools/QuranDashboard.AccessAdmin/Program.cs:40-388`: host setup, authorization,
  ten-command dispatch, parsing, execution, usage, and presentation.
- Footer component: health HTTP orchestration, loading/error/retry, and duplicate status mapping.
- Suggested direction: keep public entry facades but delegate to focused planners, feature
  registration methods, command handlers/presenters, and a small health state owner.
- Behavior-change note: AccessAdmin currently maps several non-database failures to a diagnostic that
  says the database is unusable; narrowing that user-visible classification needs explicit approval.

#### ER-21 — Count-filter factories have positional parameter explosion

- `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/RootsCountFilter.cs:26-41`
  accepts 14 adjacent nullable integers; `Lemmas/LemmasCountFilter.cs:25-38` accepts 12 and
  `Stems/StemsCountFilter.cs:25-36` accepts 10.
- Why it matters: same-typed min/max positions are easy to transpose and difficult to audit.
- Suggested direction: use named raw-range input records or named `CountRange` values; share the range
  primitive, not a speculative generic filter hierarchy.

#### ER-22 — Production comments violate the canonical policy at scale

- Backend review found narrative/history comments in 89 production files, including `perf finding`
  references, prior-refactor narration, section banners, and authority restatement.
- Mushaf/Words alone contain 670 comment lines across 82 files; at least 67 carry old
  Feature/US/F/N/M/T/Slice/performance identifiers. Bound examples include
  `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts:102-109`,
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfMushafPageReader.cs:134-139`,
  and `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.html:107-116`.
- Global styles contain multi-line implementation narratives; safety-critical font facts may justify
  a one-line WHY, but not multi-line essays.
- Suggested direction: delete narration, JSDoc paraphrase, feature/finding history, and section
  banners. Retain only a one-line WHY that satisfies all three canonical exception conditions; use
  types, names, extracted responsibilities, or an existing authority for the rest.

### NOTE — authority verification, not a retained defect

#### ER-3 — Study surfaces intentionally own ayah-marker filtering, but source-display intent is unresolved

- `Frontend/quran-dashboard-ui/src/app/features/mushaf/utils/mushaf-verse-key-display.ts:15-20`
  removes a trailing `U+06DD` marker and Arabic digits from study-card `textUthmani`.
- `Frontend/quran-dashboard-ui/.architecture/UI_STYLE_SYSTEM.md:1030-1034` explicitly keeps marker
  filtering and `toStudyAyahDisplayText` with the consumer, so ownership is adopted rather than a
  misplaced presentation responsibility.
- Contracts/source-display authority were excluded, and code alone cannot prove whether marker-free
  study text is required or harmful. Do not change this behavior from the review alone; verify the
  authoritative display rule first.

## 4. Quranic data safety check

**CONCERN** — ER-1 lacks traceable I'rab provenance and ER-2 substitutes Buckwalter into Arabic
fields. ER-3 is explicitly not treated as a defect without the excluded source-display authority.
No recommendation in this review trades correctness, provenance, atomicity, validation, readable
Quran rendering, RTL semantics, or accessibility for code reduction.

## 5. Verification check

**Missing by explicit scope.** No build, typecheck, test, Test Guard, browser, database, or runtime
evidence was supplied or generated. Tests/specs/contracts were explicitly excluded. Static reference
analysis is strong enough to identify candidates but not to claim compiler/runtime reachability or
behavioral PASS.

## 6. Final recommendation

Address ER-1, ER-2, ER-4, and ER-5 first with source/security owners, then break the bidirectional/core
feature dependencies and hard-threshold owners along the evidenced responsibility boundaries.
Follow with a focused dead-code/DRY/comment cleanup. After implementation settles, run fresh project
verification and request a formal re-review that retains these `ER-*` IDs.
