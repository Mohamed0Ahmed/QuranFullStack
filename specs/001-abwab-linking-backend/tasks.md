# Tasks: Abwab Ayah Linking — Real Persistence, Preflight, and Confirmation

**Input**: Design documents from `specs/001-abwab-linking-backend/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md, and the
execution authority `docs/abwab-linking-backend-implementation-plan.md` ("the docs plan")

**Tests**: NONE — the Test Freeze is in force (`TESTING_CONSTITUTION.md`). Do not create or modify
any automated test. Every checkpoint below is a build, a retained gate re-run, or a manual probe.
The only test-adjacent action allowed is regenerating the local smoke dump after a migration.

**Organization**: Story phases are sequenced by the docs plan's dependency graph (§13), not raw
priority order — US6 (caching) must land before US4/US5 (preflight/confirm re-resolve through the
cached boundary), and all Frontend work is a strictly sequential cutover after the Backend is
complete. Every task still carries its user-story label for traceability.

## How to execute any task (read this first)

1. Before touching a file, read the **governing reference** named in the task. The task tells you
   *what*; the reference tells you *exactly how*. On any conflict between artifacts: **stop and
   report — do not pick silently.**
2. Production-source comments are forbidden by default (`CODING_PRINCIPLES.md` §2). Active
   Spec Kit artifacts carry feature intent; implemented structure and behavior live in code and
   the existing architecture/contract authorities named by the task.
3. Follow existing neighbouring code patterns (registration, exception translation, envelope,
   naming). When a task says "match Abwab's pattern", open the named Abwab file and mirror it.
4. Never touch: `Backend/tests/**`, `Frontend/quran-dashboard-ui/e2e/**`, `main` branch,
   `AbwabPermissionCatalogue`, the shared `IMemoryCache` registrations, existing explorer DTOs.
5. Recurring rituals referenced by name below:
   - **Contract gate ritual**: `Backend/scripts/export-swagger` → in `Frontend/quran-dashboard-ui/`
     run `npm run generate:api` → stage BOTH `Frontend/quran-dashboard-ui/openapi/swagger.json`
     and `src/app/core/api/generated/models/**` with the change → `Backend/scripts/check-api-contract` passes.
   - **Migration ritual**: `Backend/scripts/add-mig <Name>` (EF tooling only — never hand-write) →
     `Backend/scripts/check-pending-model --no-build` (no drift) → `Backend/scripts/create-smoke-dump`
     (re-pins `SmokeDumpGate`) → report migration name, files, build status, whether `update-db` ran.
   - **FE gate**: `npm run check:no-unit-specs` → `npm run typecheck:app` → `npm run build:verify`,
     run as three independent commands, in that order. Template/style changes add
     `npm run check:golden-ui` FIRST.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1–US7 from spec.md; Setup/Foundational/Polish tasks carry no label

---

## Phase 1: Setup

**Purpose**: Verify the working environment before any change.

- [X] T001 Verify baseline health: `cd Backend && dotnet build` succeeds;
      `Backend/scripts/check-pending-model --no-build` reports no pending changes; local PostgreSQL is
      restored per `Backend/scripts/README.md`; `Frontend/quran-dashboard-ui/` has `npm ci` done
      and the FE gate passes untouched. Fix nothing yet — if baseline is red, stop and report.
- [X] T002 Read the governing artifacts in this order and keep them open throughout:
      `specs/001-abwab-linking-backend/spec.md` → `research.md` (R1–R22 are non-negotiable
      decisions) → `data-model.md` → all four files in `contracts/` → the docs plan phase named by
      each task below. Also follow `Backend/CLAUDE.md` and the then-current repository routing.

---

## Phase 2: Foundational — typed descriptor + byte-exact identity (docs plan Phase 1)

**Purpose**: The shared Linking contracts every story consumes. No database, no endpoint, no DI,
no behavior. **Blocks all user stories.**

- [X] T003 [P] Create the six enums in `Backend/domain/QuranDashboard.Domain/Linking/`:
      `LinkingSourceKind.cs` (UniqueWord, Root, Lemma, Stem, WordType, ManualMushafAyahs),
      `LinkingUniqueWordMode.cs` (Simple, Tashkeel), `LinkingWordTypeSelectionKind.cs` (Word,
      Root, Stem, Lemma), `LinkingManualLinkShape.cs` (Grouped, Independent),
      `LinkingContributionMode.cs` (Automatic, ManualSingle, ManualIndependent, ManualGrouped).
      Note: identity strings use kebab tokens, DB columns use snake tokens — the enum↔token maps
      live where they are used, never guessed (data-model.md §vocabularies).
- [X] T004 [P] Create `Backend/domain/QuranDashboard.Domain/Linking/LinkingWordTypeScope.cs` —
      value object `(Type, ChildCode, Case, Tense, Voice)` with the exact token vocabularies from
      `contracts/source-identity.md` §Enum vocabularies (note: the literal string `null` is a
      valid *case token*, distinct from an absent value).
- [X] T005 Create `Backend/domain/QuranDashboard.Domain/Linking/LinkingSourceDescriptor.cs` — a
      discriminated value object per docs plan Phase 1 (never a bag of nullables): Kind +
      per-family data (UniqueWord: mode+wordId; Root: rootId; Lemma/Stem: id+optional typeCode;
      WordType: selection union + scope; ManualMushafAyahs: ordered de-duplicated verse-key set)
      + Label (display snapshot, never identity). Impossible descriptors (Word Type without a
      selection, manual source with zero verses) must be **unconstructable**. Depends on T003+T004.
- [X] T006 [P] Create `Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingLimits.cs`
      with exactly four numeric limits: `MaxDescriptionsPerSourceAyah = 10`,
      `MaxDescriptionLength = 2000`, `MaxResolvedAyahs = 3000`, `MaxPreparedSources = 100`.
      Do NOT add per-operation ayah/source caps — they were explicitly removed (research.md, docs
      plan Phase 1).
- [X] T007 [P] Create the five exception types in
      `Backend/application/QuranDashboard.Application.Abstractions/Linking/`:
      `LinkingSourceNotFoundException`, `LinkingInvalidDescriptorException`,
      `LinkingStaleVersionException`, `LinkingDuplicateContributionException`,
      `LinkingPreflightStaleException` — following the existing Abwab Abstractions exception
      precedent (find and mirror one).
- [X] T008 Create `Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingSourceIdentity.cs`
      — the static, pure canonicalizer. **Highest-risk item in the feature.** Implement exactly
      per `contracts/source-identity.md`: parts joined `|`, JavaScript `encodeURIComponent`
      escape set (`Uri.EscapeDataString` then un-escape `%21 %27 %28 %29 %2A` → `! ' ( ) *`),
      null → empty string, kebab kind tokens, manual verse set de-duplicated and ordered by
      (surah, ayah), Word Type part orders (12 parts for word selection, 8 for root/stem/lemma).
      Also expose the 32-byte SHA-256 hash of the UTF-8 identity (`source_identity_hash`,
      research.md R20). Depends on T005.
- [X] T009 Create `Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingSourceDescriptorValidation.cs`
      — descriptor well-formedness per `contracts/linking-sources-api.md` §Request: positive ids,
      exact enum tokens, verse keys `^\d{1,3}:\d{1,3}$` (surah 1–114, ayah 1–286), manual set ≥1
      verse (NO manual-specific size cap), label non-blank. Depends on T005.
- [X] T010 **Checkpoint (Foundational)**: `cd Backend && dotnet build` passes. Hand-verify all 8
      worked examples from `contracts/source-identity.md` §Worked examples produce byte-identical
      output to the table (write a throwaway console check locally; do NOT add a test). Verify:
      no `linking_*` table, endpoint, or DI registration exists yet; DTO/contract files < 150
      lines (docs plan Phase 1 acceptance).

---

## Phase 3: User Story 1 — Complete, validated source resolution (P1) 🎯 Backend MVP (docs plan Phases 2–3)

**Goal**: One Owner-only boundary that returns the complete validated ayah set for all six source
families with canonical word ids, in one call.

**Independent Test**: Via Swagger/`curl` as an Owner — resolve one source of each family and
verify order, completeness, canonical ids, marker rules, and controlled failures (quickstart §1).

- [X] T011 [P] [US1] Create resolution abstractions in
      `Backend/application/QuranDashboard.Application.Abstractions/Linking/`:
      `ILinkingSourceResolutionReader.cs` and `Responses/LinkingResolvedSourceDto.cs`,
      `Responses/LinkingResolvedAyahDto.cs`, `Responses/LinkingResolvedWordDto.cs` — field-exact
      to `contracts/linking-sources-api.md` §Response (sourceIdentity, resolvedAtUtc,
      totalAyahCount; ayah: ayahId, verseKey, surahNumber, ayahNumber, surahNameArabic, pageFrom,
      pageTo, matchedQuranWordIds[], words[]; word: quranWordId, wordNumber, textUthmani,
      isAyahMarker).
- [X] T012 [P] [US1] Modify `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/AyahWordHydration.cs`:
      make the ayah-marker filter a parameter. Existing consumers keep today's exact shapes
      (Root/Lemma/Stem/Word Type marker-free — all five pre-existing callers omit the flag; only
      Linking's Unique Word and Manual Mushaf branches pass `true`). `AyahWordRow` already
      carries `QuranWordId` (repo fact F4) — no query change, projection only.
- [X] T013 [P] [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/LinkingAyahHydration.cs`
      — shared ayah-level hydration (verse key, surah name, pageFrom/pageTo from `quran_ayahs`)
      reusing the existing bounded 4–5-command pattern with `Skip/Take` removed (research.md
      §query-shape). Never one query per ayah.
- [X] T014 [US1] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfLinkingSourceResolutionReader.cs`
      (dispatch by descriptor kind) and partial `EfLinkingSourceResolutionReader.Automatic.cs`
      (Root/Lemma/Stem via `WordMorphologies` id predicates). Order: ayahs by
      (surah_number, ayah_number), words by word_number — always (spec FR-006). Enforce
      `MaxResolvedAyahs` → `LinkingInvalidDescriptorException`; unknown dimension id →
      `LinkingSourceNotFoundException`. Depends on T011–T013.
- [X] T015 [US1] Create partial `EfLinkingSourceResolutionReader.UniqueWord.cs` (simple/tashkeel
      modes; markers included and flagged) in the same folder. Depends on T014.
- [X] T016 [US1] Create partial `EfLinkingSourceResolutionReader.WordType.cs` reusing the shared
      `BaseRowsSql` occurrence base from
      `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/WordTypes/EfWordTypesReader.Sql.cs`
      (read it first; do not fork its SQL). Depends on T014.
- [X] T017 [US1] Create partial `EfLinkingSourceResolutionReader.ManualMushaf.cs` plus pure
      `Backend/application/QuranDashboard.Application.Abstractions/Linking/LinkingManualAyahCompleteness.cs`.
      Completeness proof per verse (docs plan Phase 3 / research R9): ayah exists + verse_key
      matches; non-marker word_numbers contiguous 1..N; N == `quran_ayahs.words_count_real`;
      every non-marker location unique with matching (surah, ayah) prefix. Any failure blocks the
      WHOLE resolution naming the exact verse. `matchedQuranWordIds` MAY be empty for manual
      ayahs (spec FR-008) — the ayah still returns with its complete word list. NO Mushaf page
      assembly anywhere. Depends on T014.
- [X] T018 [US1] Create `Backend/application/QuranDashboard.Application/Linking/Queries/ResolveLinkingSource/`
      — `ResolveLinkingSourceQuery.cs`, `ResolveLinkingSourceHandler.cs`,
      `ResolveLinkingSourceOutcome.cs`, matching the existing Quran query-handler pattern in the
      same project. Depends on T014–T017.
- [X] T019 [US1] Create `Backend/api/QuranDashboard.Api/Contracts/Linking/LinkingSourceDescriptorBody.cs`
      (discriminated body mirroring the TS union — see `contracts/linking-sources-api.md`
      §Request for all six JSON shapes) and
      `Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingSourcesController.cs` with
      exactly one action: `POST /api/linking/sources/resolve`, exactly one `[RequireOwner]`,
      `ApiResponse<T>` envelope, Arabic messages, status mapping 200/400/404 per the contract.
      Depends on T018.
- [X] T020 [US1] Register everything: create `LinkingDependencyInjection.cs` next to the existing
      `PersistenceDependencyInjection` (search
      `Backend/infrastructure/QuranDashboard.Infrastructure/` for it; F13 convention:
      `AddScoped<EfX>()` then interface factory) and call it from `PersistenceDependencyInjection`;
      register the handler in `Backend/application/QuranDashboard.Application/DependencyInjection.cs`.
      Depends on T019.
- [X] T021 [US1] Complete the then-required code-area documentation update for the resolution
      boundary and its second use of shared word hydration. This is completed historical work;
      the code-area documentation policy was retired after Phase 6.
- [X] T022 [US1] Run the **Contract gate ritual** (see Phase 0 rituals). Both artifacts committed
      together with this change.
- [X] T023 [US1] **Checkpoint (US1)**: `dotnet build`; run every quickstart §1 probe: ~10/~200/
      ~2,000-ayah roots (record payload size + wall time for the final matrix), manual source with
      a page-spanning ayah, manual ayah with empty matched set, unknown verse/dimension → 400/404
      naming the offense, cap guard by lowering `MaxResolvedAyahs` locally, existing explorer
      routes byte-identical, `EXPLAIN ANALYZE` on the large case, identity parity re-check.

---

## Phase 4: User Story 6 — Instant repeat access (P6) (docs plan Phase 4)

**Goal**: Warm repeat resolution does zero database work; bounded, stampede-safe, descriptor-keyed.

**Independent Test**: EF command logging at `Information` proves the second identical resolution
issues zero SQL; two concurrent identical requests collapse to one load (quickstart §1).

**⚠ Sequencing note**: runs before US2–US5 because preflight/confirm re-resolve through this
cached boundary (docs plan §13).

- [X] T024 [P] [US6] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Linking/LinkingResolvedSourceCompact.cs`
      — compact cached value: per ayah `{ayahId, quranWordIds[], matchedQuranWordIds[]}` + ordered
      ayah-id list (≈210 KB per 2,000-ayah source vs ≈4 MB full DTO — research R11).
- [X] T025 [P] [US6] Create `LinkingSourceCacheKeys.cs` and `LinkingSourceCacheEntryOptions.cs` in
      the same folder: key `linking:source:v1:{kind}:{sha256(canonicalScope)[..16]}` derived ONLY
      from the typed descriptor (reuse `WordTypesCacheKeys.HashParts` delimiter escaping);
      options record with defaults `SlidingExpiration = 30 min` AND
      `AbsoluteExpirationRelativeToNow = 4 h`, bound from appsettings following
      `MushafReaderOptions`' precedent. NEVER in key or value: user, Door, inclusion, words,
      descriptions, workspace or preflight state.
- [X] T026 [US6] Create `LinkingSourceResolutionCache.cs` owning its **own**
      `new MemoryCache(new MemoryCacheOptions { SizeLimit = … })` — the shared `IMemoryCache` is
      size-less and must not be touched (repo fact F7). Entry `Size` = resolved ayah count.
      Stampede control: store `Task<LinkingResolvedSourceCompact>` in the entry; evict faulted
      tasks immediately so the next caller retries. `CacheLoadGate` is FORBIDDEN here (F8).
      Depends on T024+T025.
- [X] T027 [P] [US6] Create `LinkingAyahTextCache.cs` in the same folder — Uthmani text + display
      metadata keyed by ayah id, deduplicating hydration across overlapping sources; same
      expiration policy.
- [X] T028 [US6] Create `CachedLinkingSourceResolutionReader.cs` decorator (wire-invisible: same
      DTO out) and switch DI in `LinkingDependencyInjection.cs` to the F13 decorator pattern:
      `AddScoped<EfLinkingSourceResolutionReader>()` +
      `AddScoped<ILinkingSourceResolutionReader>(sp => new CachedLinkingSourceResolutionReader(...))`.
      Depends on T026+T027.
- [X] T029 [US6] Complete the then-required code-area documentation update for the THREE deliberate
      divergences: dedicated instance (not shared IMemoryCache, F7), `Task<T>`-in-entry (not
      `CacheLoadGate`, F8), and no user/Door in keys (safe cross-actor sharing). This is completed
      historical work; the decisions remain mandatory through research R11 and the implementation.
- [X] T030 [US6] **Checkpoint (US6)**: warm repeat = zero SQL (EF logging); one-field scope change
      = different entry, no cross-serve; concurrent identical requests = one load; memory reading
      before/after warming ~8 large sources stays low tens of MB; `grep` proves no existing
      `IMemoryCache` `Set` call changed and no `SizeLimit` was added to the shared instance.

---

## Phase 5: User Story 2 — Durable personal workspace (P2) (docs plan Phase 5, migration M1)

**Goal**: Per-user server-side workspace replacing browser-local storage: load (read-only), add
(idempotent), remove, reorder, replace-configuration, clear — all Owner-only, all version-guarded.

**Independent Test**: Swagger round-trip of all six routes + two-tab stale-version probe + `psql`
constraint probes (quickstart §2).

- [X] T031 [P] [US2] Create the five workspace entities in
      `Backend/domain/QuranDashboard.Domain/Linking/`: `LinkingWorkspace.cs`,
      `LinkingWorkspaceSource.cs`, `LinkingWorkspaceSourceManualAyah.cs`,
      `LinkingWorkspaceSourceAyahOverride.cs`, `LinkingWorkspaceSourceWord.cs` — columns exactly
      per data-model.md tables 1–5 (including `source_identity` raw text AND
      `source_identity_hash bytea`).
- [X] T032 [US2] Create the five EF configurations in
      `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Linking/`
      — every key, FK behavior (source→children CASCADE; all Quran/Access FKs RESTRICT), CHECK,
      and index from data-model.md tables 1–5, including `UNIQUE (user_id)`,
      `UNIQUE (workspace_id, source_identity_hash)`, kind/configuration coherence CHECK,
      kind/reference coherence CHECK, and `xmin` mapped as the concurrency token exactly as Abwab
      maps it. Depends on T031.
- [X] T033 [US2] Add the five `DbSet`s to `QuranDashboardDbContext`, then run the **Migration
      ritual** for `AddLinkingWorkspace` (M1). Depends on T032.
- [X] T034 [P] [US2] Create abstractions `ILinkingWorkspaceReader.cs`, `ILinkingWorkspaceWriter.cs`,
      `Responses/LinkingWorkspaceDto.cs` in
      `Backend/application/QuranDashboard.Application.Abstractions/Linking/` — DTO field-exact to
      `contracts/linking-workspace-api.md` (workspaceVersion nullable; per source: id,
      sourceVersion, orderValue, descriptor, sourceIdentity, inclusionMode, ayahOverrides,
      selectedWords [manual only], automaticWordMatchesEnabled/manualLinkShape, manualAyahs,
      descriptions, lastResolvedCount/AtUtc).
- [X] T035 [US2] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfLinkingWorkspaceReader.cs`
      — **strictly read-only** (spec FR-019, research R21): no row → empty representation with
      `workspaceVersion = null`, ZERO inserts. Scoped to `AuthorizationState.UserId` (F10) — never
      a request-supplied user. Depends on T033+T034.
- [X] T036 [US2] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingWorkspaceWriter.cs`:
      first mutation creates the workspace row atomically (serialized by `UNIQUE (user_id)`); add
      idempotent by identity-hash + raw-identity final guard (label refresh only on re-add);
      remove/reorder/clear under workspace `xmin`; replace-configuration wholesale under source
      `xmin` (manual verse set is identity-bearing and NOT part of the configuration document);
      word validation per spec FR-023 (manual sources only; authored words on an automatic source
      → reject outright); EVERY save translates `DbUpdateConcurrencyException` →
      `LinkingStaleVersionException` and `23505` → `LinkingDuplicateContributionException` (F12);
      attribution stamped from the resolved actor (F11). Depends on T033+T034.
- [X] T037 [US2] Create the Application layer in
      `Backend/application/QuranDashboard.Application/Linking/`:
      `Queries/GetLinkingWorkspace/` + `Commands/` for add-source, remove-source, reorder-sources,
      replace-source-configuration, clear-all — thin handlers over reader/writer, `MaxPreparedSources`
      enforced on add. Depends on T035+T036.
- [X] T038 [US2] Create `Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingWorkspaceController.cs`
      with exactly the six routes, verbs, version placement, and status mapping from
      `contracts/linking-workspace-api.md` — each with exactly one `[RequireOwner]`. Depends on T037.
- [X] T039 [US2] Register in DI (T020's files). Workspace persistence is NOT a cache; attribution
      is a first — Abwab never populated these columns; GET never writes.
- [X] T040 [US2] Run the **Contract gate ritual**.
- [X] T041 [US2] **Checkpoint (US2)**: quickstart §2 probes — GET-as-fresh-user inserts zero rows
      (verify row count in `psql`); add 3 sources/configure/reorder/reload preserved; equivalent
      re-add refreshes label only; two-tab stale version → 409 Arabic envelope; second user sees
      nothing; `selectedWords` on an automatic source → 400; each CHECK exercised once by a
      deliberate bad INSERT in `psql`; `check-pending-model` clean.

---

## Phase 6: User Story 3 — Per-ayah descriptions (P3) (docs plan Phase 6, migration M2)

**Goal**: Up to 10 ordered plain-text descriptions (≤2000 chars) per (source, ayah) inside the
workspace configuration document.

**Independent Test**: Swagger round-trip + deliberate 11th-row and duplicate-order INSERTs in
`psql` that must fail (quickstart §2).

- [X] T042 [P] [US3] Create `Backend/domain/QuranDashboard.Domain/Linking/LinkingWorkspaceSourceDescription.cs`
      and its configuration in `Persistence/Configurations/Linking/` per data-model.md table 6:
      CHECKs `btrim(body) <> ''` and `order_value BETWEEN 1 AND 10`, and
      **`UNIQUE (workspace_source_id, ayah_id, order_value)`** — the uniqueness is half of the
      hard max-10 guarantee; a plain index is wrong.
- [X] T043 [US3] Add the `DbSet`, then run the **Migration ritual** for
      `AddLinkingWorkspaceDescriptions` (M2). Depends on T042.
- [X] T044 [US3] Extend `ILinkingWorkspaceWriter`/`EfLinkingWorkspaceWriter`,
      `LinkingWorkspaceDto`, and the replace-configuration command + API contract so descriptions
      ride inside the existing per-source configuration document (no new route): writer diffs,
      resequences 1..N, hard-deletes absent rows; validates ≤10 per (source, ayah), 1–2000 trimmed
      chars, plain text, ayah belongs to that source's own set — all limits referenced from
      `LinkingLimits` (single definition). Depends on T043.
- [X] T045 [US3] Run the **Contract gate ritual**.
- [X] T046 [US3] **Checkpoint (US3)**: 11th description refused by writer AND database (bad INSERT
      reusing an order_value fails on the UNIQUE); 2001-char and blank bodies refused;
      reorder/remove resequences contiguously; two sources sharing an ayah keep separate lists
      (verified by inspection); `check-pending-model` clean.

---

## Phase 7: User Story 4 — Preflight (P4) (docs plan Phases 7–8, migration M3)

**Goal**: The confirmed-state schema (behavior-free) plus a read-only classification engine
returning exact affected ayahs with counts that partition, structured overlap provenance, and the
required-but-untrusted freshness token.

**Independent Test**: Hand-built Door state in `psql`, then preflight through Swagger reproduces
the locked example with zero writes (quickstart §3).

- [ ] T047 [P] [US4] Create the six confirmed entities in
      `Backend/domain/QuranDashboard.Domain/Linking/`: `LinkingOperation.cs`,
      `LinkingSourceContribution.cs`, `LinkingUnit.cs`, `LinkingUnitAyah.cs`,
      `LinkingUnitAyahWord.cs`, `LinkingUnitAyahDescription.cs` — columns exactly per
      data-model.md tables 7–12.
- [ ] T048 [US4] Create the six EF configurations in `Persistence/Configurations/Linking/` with
      EVERY constraint from data-model.md tables 7–12 — notably `UNIQUE (idempotency_key)`;
      **`UNIQUE (door_id, source_identity_hash) WHERE deleted_at IS NULL`**; `UNIQUE (id, door_id)`;
      the composite FK `(unit_id, source_contribution_id)` → `linking_units`;
      `UNIQUE (source_contribution_id, ayah_id)`; **`UNIQUE (unit_ayah_id, order_value)`** on
      descriptions; the filtered per-dimension provenance indexes; `outcome` jsonb CHECK; `xmin`
      on contributions. The `is_grouped` cross-row rule is writer-enforced (NO triggers).
      Depends on T047.
- [ ] T049 [US4] Add the six `DbSet`s, then run the **Migration ritual** for
      `AddLinkingConfirmedState` (M3). Depends on T048.
- [ ] T050 [US4] **Checkpoint (schema)**: migration applies to an empty DB and to an M2-head DB;
      every constraint verified in `psql` per docs plan Phase 7 acceptance (duplicate live
      contribution fails; soft-delete-then-insert succeeds; composite-FK mismatch fails);
      `AbwabSchemaTests` untouched and still green via its lane (it queries per named `abwab_*`
      table — repo fact F3); `check-pending-model` clean.
- [ ] T051 [P] [US4] Create preflight abstractions in
      `Backend/application/QuranDashboard.Application.Abstractions/Linking/Preflight/`:
      `LinkingOperationRequest.cs`, `LinkingPreflightResultDto.cs`, `LinkingSourcePreflightDto.cs`,
      `LinkingAyahPreflightDto.cs`, `LinkingPreflightClassification.cs`, `LinkingPreflightToken.cs`
      — field-exact to `contracts/linking-operations-api.md`, including structured
      `overlappingSources[]` `{sourceIdentity, label, sourceKind}` (NEVER a bare key list) and
      per-source `automaticWordMatchesEnabled`.
- [ ] T052 [US4] Create `Backend/application/QuranDashboard.Application/Linking/LinkingOperationClassifier.cs`
      — **pure and shared with Confirm** (state in → classification out; zero repository access;
      the design's load-bearing choice). Implement the exact tables in
      `contracts/linking-operations-api.md`: source NEW_SOURCE/UNCHANGED/UPDATE/INVALID; ayah
      NEW_AYAH/OVERLAP_OTHER_SOURCE/UNCHANGED/UPDATE/REMOVE/INVALID (mutually exclusive; submitted
      = new+overlapping+unchanged+updated+invalid; removed separate); precedence: source-owned
      change wins, overlap only where otherwise NEW_AYAH; label EXCLUDED from comparison
      (spec FR-004); effective word sets (manual authored / automatic derived per toggle); exact
      word + description diffs; only INVALID blocks. Depends on T051.
- [ ] T053 [US4] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Linking/EfLinkingConfirmedStateReader.cs`
      — loads a Door's live contributions + children in bounded batched queries (never cached —
      per-Door mutable state). Depends on T049 (M3 tables + DbSets); may run in parallel with
      T051/T052 once T049 is done.
- [ ] T054 [US4] Implement the preflight token canonicalizer (inside `LinkingPreflightToken.cs`):
      hash(Door identity + live state, each affected contribution `(id, xmin)`, canonical
      **operation intent**) — inclusions/exclusions EXACTLY per `contracts/linking-operations-api.md`
      §Preflight token composition (NO resolvedAtUtc, NO idempotencyKey, NO existing* fields, NO
      label; deterministic ordering). One function used by BOTH preflight and confirm. Depends on T051.
- [ ] T055 [US4] Create `Queries/PreflightLinkingOperation/` (query/handler/outcome — performs NO
      writes, resolves sources through the cached boundary, reads confirmed state fresh) and
      `Backend/api/QuranDashboard.Api/Controllers/Linking/LinkingOperationsController.cs` with the
      `POST /api/linking/operations/preflight` action (`[RequireOwner]`; validation incl.
      FR-044a ≥1 ayah per source). Register in DI. Depends on T052–T054.
- [ ] T056 [US4] Run the **Contract gate ritual**.
- [ ] T057 [US4] **Checkpoint (US4)**: quickstart §3 probes — the locked example («الرحمن» A,B,C
      vs «الرحيم» A,D,E → NEW_SOURCE; A overlap naming «الرحمن» with label+kind; D,E new; counts
      3 = 2+1); unchanged+new → not blocked, not no-op; all-identical → isNoOp; label-only rename
      → UNCHANGED; REMOVE scoped to one source; archived Door/marker/foreign word/zero-ayah →
      INVALID or 400; row counts identical before/after every call.

---

## Phase 8: User Story 5 — Atomic confirmation (P5) (docs plan Phase 9)

**Goal**: One all-or-nothing command applying the classified operation with replacement semantics,
Door-row serialization, idempotent replay, and the finalize-once operation record.

**Independent Test**: Scripted Swagger sequence against a local Door with `psql` inspection after
every step (quickstart §4).

- [ ] T058 [P] [US5] Create `ILinkingConfirmationWriter.cs` and
      `Responses/LinkingConfirmationResultDto.cs` in
      `Backend/application/QuranDashboard.Application.Abstractions/Linking/` (result carries per
      source: identity, final classification, contributionId, counts; totals; equals the stored
      replay outcome).
- [ ] T059 [US5] Create `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Writes/Linking/EfLinkingConfirmationWriter.cs`
      implementing `contracts/linking-operations-api.md` §Validation & transaction boundary
      EXACTLY: **Phase A** outside the transaction (structure incl. REQUIRED preflightToken,
      actor/owner, descriptors, cached-boundary re-resolution + membership anti-tamper, manual-only
      word checks, automatic word derivation, grouping, description limits); **Phase B** inside
      ONE transaction — idempotency replay lookup; **`abwab_doors` row lock via `FOR UPDATE`
      (EF-equivalent), held to COMMIT/ROLLBACK**; Door liveness; load live contributions; apply
      `xmin` via `Entry(x).Property(x => x.Version).OriginalValue`; re-classify with T052's
      classifier; recompute + compare token (same canonicalizer as T054) → 409 PreflightStale with
      fresh classification; uniqueness → 409; all-UNCHANGED → zero writes + exact message
      «لا توجد تغييرات جديدة لتنفيذها» (no operation row, no idempotency record); otherwise INSERT
      operation early → per-source writes in submitted order (UNCHANGED skip; NEW_SOURCE insert
      tree; UPDATE replace children wholesale — replacement never union — stamp updated_*,
      re-point operation_id) → finalize outcome exactly once with final contribution ids → COMMIT
      (row immutable forever after). Every save translates exceptions (F12); attribution on all
      audited records (F11). Depends on T052+T054+T058.
- [ ] T060 [US5] Create `Commands/ConfirmLinkingOperation/` (command/handler/outcome) and add the
      `POST /api/linking/operations` action to `LinkingOperationsController` (`[RequireOwner]`,
      status mapping 200/400/404/409 per contract). Register in DI. Depends on T059.
- [ ] T062 [US5] Run the **Contract gate ritual**.
- [ ] T063 [US5] **Checkpoint (US5)**: quickstart §4 — confirm without token → 400; locked example
      leaves «الرحمن» byte-identical; identical re-confirm → nothing written + no-op message;
      changed source → same id, advanced xmin, replaced children ([w1,w2]→[] ⇒ none); idempotency
      replay writes nothing; two concurrent same-Door confirms serialize on the lock → one wins,
      one 409; stale version → 409, nothing partial; grouping [[A,B]]+[[A],[C]] stays 3 units /
      2 contributions; toggle-off automatic + zero-word manual ayahs store zero word rows; one
      invalid source ⇒ database untouched.

---

## Phase 9: Frontend cutover 1 — resolution adapter + session cache (docs plan Phase 10)

**⚠ Backend Phases 2–8 must be complete and merged-ready first. Phases 9→12 are strictly sequential.**

**Goal**: Point the Frontend at the Backend boundary; one request per source, zero on reopen.

- [ ] T064 [P] [US1] Create `Frontend/quran-dashboard-ui/src/app/features/linking/data-access/linking-source-resolution.api.ts`
      calling the generated client for `POST /api/linking/sources/resolve`.
- [ ] T065 [P] [US6] Create `Frontend/quran-dashboard-ui/src/app/features/linking/state/linking-source.cache.ts`
      extending `ApiResponseCache`, keyed `linking:source:{sourceIdentity}`, cap **≈6 entries**
      (NOT the default 48 — tens of MB; research R19).
      `MushafReaderCache` stays for ordinary reader use and leaves the Linking path.
- [ ] T066 [US1] Rewire `features/linking/data-access/linking-source-resolver.registry.ts` to ONE
      implementation for all six kinds; `resolve(source, onProgress)` keeps its signature with
      `onProgress` reduced to a single 0→total tick (facades untouched). DELETE:
      `complete-paged-source.loader.ts` and the six per-family resolvers under
      `data-access/resolvers/`. Depends on T064+T065.
- [ ] T067 [US1] Canonical identity collapse: `models/linking-ayah.models.ts`
      (`canonicalQuranWordId` non-nullable), `models/linking-merge.models.ts`
      (`LinkingWordContribution` canonical arm only), `utils/linking-source-intents.ts` (one
      branch), `utils/linking-merge.ts` (merge by canonical id; delete the positional/text
      alignment guard). DELETE `data-access/manual-mushaf-ayah.reader.ts` and
      `utils/manual-mushaf-ayah-completeness.ts` — but FIRST repoint
      `ManualMushafSelectionStore.readMetadata`'s "can this verse be added" gate at a light
      validation (the resolve endpoint or the existing ayah-study read) — that behavior must
      survive. Depends on T066.
- [ ] T068 [US1] Run the **FE gate**; browser probes: opening
      a source issues ONE request (was ceil(total/100)); reopening issues ZERO; a 2,000-ayah
      source and a manual source work end-to-end; `grep` proves `presentation-occurrence` and
      `manual-word-location` no longer exist anywhere.

---

## Phase 10: Frontend cutover 2 — workspace adapter (docs plan Phase 11)

**Goal**: Replace browser-local persistence with the Backend workspace, store surface unchanged.

- [ ] T069 [P] [US2] Create `features/linking/data-access/http-linking-workspace.repository.ts`
      implementing the store's existing repository port against the six workspace routes
      (versions carried per `contracts/linking-workspace-api.md`).
- [ ] T070 [US2] Swap `state/linking-workspace.store.ts` to the HTTP port (public API unchanged —
      no component edits); manual word selection saves canonical `quranWordId`
      (`state/linking-manual-word-editor.facade.ts`: draft becomes id-based; `wordLocation` is
      only the click coordinate resolved through the resolved source); update
      `models/linking-workspace.models.ts` + `models/linking-manual-mushaf.models.ts`.
      **The FE models and the replace-configuration document mapping MUST round-trip
      `descriptions` verbatim starting in THIS task** — configuration replacement hard-deletes
      absent children on the server, so omitting the field would erase existing descriptions; the
      descriptions editing UI arrives only in T081, but the passthrough exists from day one. DELETE
      `local-storage-linking-workspace.repository.ts` AND `linking-workspace.codec.ts` (server
      owns validity; a second decoder is divergence risk). Clear the old `qd-linking-workspace-v1`
      bucket after the first successful server hydration — never migrate it. Depends on T069.
- [ ] T071 [US2] Surface the 409 stale-version path as a visible, recoverable state via the
      store's existing persistence-warning signal — reload + inform, never silent overwrite.
- [ ] T072 [US2] Run the **FE gate**; probes: two-browser
      persistence (sources/config/order/descriptions reappear), two-tab 409 recovery, transient
      state (checked/search/scroll/Door) still client-side, no component file changed.

---

## Phase 11: Frontend cutover 3 — preflight + real confirm (docs plan Phase 12)

**Goal**: Insert the mandatory preflight step and replace the mock with the real command.

- [ ] T073 [P] [US4] Create `models/linking-preflight.models.ts` and
      `data-access/linking-preflight.api.ts` (structured `overlappingSources[]` with label+kind).
- [ ] T074 [P] [US5] Create `data-access/http-linking-command.port.ts`; DELETE
      `data-access/mock-linking-command.port.ts`; swap the `LINKING_COMMAND_PORT` provider in
      `state/linking-workflow.facade.ts`.
- [ ] T075 [US4] Add the `preflight` step to the workflow facade's step union (between `door` and
      `review`) and create `components/linking-preflight-step/` (component + template + styles):
      per-source classification + counts; expandable per-ayah items with classification,
      overlapping source LABELS (never raw keys), exact word/description diffs; INVALID disables
      Confirm with per-item reasons; render it in `components/direct-link-workflow/`. Depends on T073.
- [ ] T076 [P] [US4] Add Arabic copy to `models/linking.labels.ts`: the six classifications, the
      no-op success «لا توجد تغييرات جديدة لتنفيذها» (rendered as SUCCESS, flow completes), and
      the stale-preflight message.
- [ ] T077 [US5] Confirm wiring: carry `preflightToken` (REQUIRED) and each
      `existingContributionId`/`existingContributionVersion` from preflight into confirm; one
      `idempotencyKey` per attempt, reused across retries; on 409 PreflightStale auto re-run
      preflight and re-present the fresh classification instead of failing. Depends on T074+T075.
- [ ] T078 [US5] Run `npm run check:golden-ui` FIRST (new template), then the **FE gate**; full
      manual walkthrough of quickstart §4–§5 scenarios against a local database with `psql`
      verification after each confirm; deliberate stale-preflight test by mutating the Door's
      contributions in a second tab; success message reflects the real result — no
      `نتيجة نموذج أولي` anywhere.

---

## Phase 12: User Story 7 — Fluid large-source editing + provenance (P7) (docs plan Phase 13)

**Goal**: Virtualized continuous editor list, descriptions UI, merged provenance display.

- [ ] T079 [US7] Replace the `<ul>` + pagination in `components/linking-ayah-selection/` with
      `<cdk-virtual-scroll-viewport>` (ScrollingModule already a dependency via
      `shared/ui/data-table` — CDK ^20.2.14). Quran text wraps ⇒ VARIABLE row heights: use
      autosize/measured rows, never a fixed `itemSize`. The viewport becomes the surface's SINGLE
      vertical scroll owner (the editor's own `overflow: auto` must yield, not nest).
- [ ] T080 [US7] Remove pagination from state: delete `EDITOR_PAGE_SIZE`, `page`, `setPage`,
      `pageCount`, `visibleAyahs` from `state/linking-source-editor.facade.ts`, expose
      `filteredAyahs`; drop `page` from `LinkingSourceEditorState` in
      `models/linking-workflow.models.ts`. Selection/search/select-all/clear-all already operate
      on the complete universe — rendering only. Depends on T079.
- [ ] T081 [US7] Descriptions editor in `components/linking-source-ayah-editor/`: add / edit /
      reorder / remove per ayah, client-enforced ≤10 × ≤2000 plain text, riding the existing
      configuration-replacement save; extend `models/linking-workspace.models.ts`.
- [ ] T082 [US7] Merged provenance (gap G4): `components/linking-ayah-card/` + the
      `direct-link-workflow` review render the already-computed `MergedAyahSelection.words` union
      and its `sourceKeys` provenance naming EVERY contributing source — not the first
      contributor's flags. Descriptions stay listed per source.
- [ ] T083 [US7] Run `check:golden-ui` then the **FE gate**;
      browser verification at Wide/Medium/Compact: one scroll owner, keyboard reachability, glyph
      metrics unchanged; 2,000-ayah source scrolls continuously with bounded DOM node count;
      exclusion near the end survives scrolling away and back; two-source ayah shows union +
      both names, separate description lists.

---

## Phase 13: Polish & Final Acceptance (docs plan Phase 14)

**Retired by the code-area documentation cutover:** T061, T084, and T085. Their IDs are not
reused; they carried documentation-only work and no functional requirement or gate.

- [ ] T086 Hardening sweep (each item verified, not assumed): shared `IMemoryCache` still has no
      `SizeLimit` and no existing `Set` changed; `AbwabPermissionCatalogue` untouched at 19 codes;
      every Linking route carries exactly one `[RequireOwner]`; every writer save translates
      exceptions; no cache key contains user/Door/configuration; `check-pending-model` clean and
      smoke-dump manifest matches head migration;
      `git diff --stat -- Backend/tests Frontend/quran-dashboard-ui/e2e` is EMPTY.
- [ ] T087 Execute the full manual acceptance matrix — docs plan §14 rows A1–F4 (the quickstart
      maps them) — against a local database, recording A3's payload size and wall time. All four
      final gates green: `dotnet build`, `check-api-contract`, `check-pending-model`, the FE
      four-command gate.

---

## Dependencies & Execution Order

### Phase dependencies (mirrors docs plan §13 — this is the authoritative order)

- **Phase 1 → 2**: Setup then Foundational. Foundational **blocks everything**.
- **Phase 3 (US1)** ← Phase 2. **Phase 4 (US6)** ← Phase 3.
- **Phase 5 (US2)** ← Phase 2 + T014 (resolution reader; needs a resolvable count) — NOT the cache.
- **Phase 6 (US3)** ← Phase 5.
- **Phase 7 (US4)** ← Phases 3, 4 (cached boundary) — schema tasks T047–T050 only need Phase 2 and
  may run in parallel with Phases 3–6.
- **Phase 8 (US5)** ← Phase 7.
- **Phases 9→12 (Frontend)**: strictly sequential, each ← its Backend stories: Phase 9 ← 3+4;
  Phase 10 ← 5+6+9; Phase 11 ← 7+8+10; Phase 12 ← 11.
  **Do not ship Phase 11 without Phase 12** — a real write must not sit behind the old paginated,
  description-less editor (docs plan §13 Releasability).
- **Phase 13** ← everything.

### Why story order ≠ priority order

US6 (P6, cache) precedes US2–US5 because preflight and confirm re-resolve every source through the
cached boundary (docs plan Phase 8/9 dependencies). Frontend slices of US1/US2/US4/US5 land in
Phases 9–11 because the cutover is locked as sequential-after-Backend. Each phase remains
independently verifiable at its checkpoint.

### Parallel opportunities

- Phase 2: T003, T004, T006, T007 in parallel; then T005 → T008/T009.
- Phase 7's schema block (T047–T050) can run in parallel with Phases 3–6 (only needs Phase 2).
- Within phases: every [P] task touches distinct files with no incomplete dependencies —
  e.g. T011/T012/T013; T024/T025/T027; T031+T034; T047+T051; T064+T065; T073+T074+T076.
  T053 joins the parallel set only after T049 (it reads the M3 tables).
- Backend (Phases 3–8) and nothing on the Frontend may overlap — the FE cutover waits.

## Implementation Strategy

1. **Backend MVP** = Phases 1–3 (US1): resolution is real and testable via Swagger — no
   user-visible change yet (ships dark, by design).
2. Continue strictly in phase order; **stop at every checkpoint** and run its probes before
   moving on. Each Backend phase is independently reviewable/mergeable.
3. The user-visible cutover happens across Phases 9–12 and completes only with Phase 12.
4. Phase 13 closes the loop: hardening sweep and full acceptance matrix.
5. Commit/PR only when the user asks; when asked, follow
   `.claude/skills/commit-workflow/SKILL.md` (branch model: PR into `dev`).

## Notes

- 84 executable tasks; every task names exact file paths and its governing reference — read the reference
  before writing code.
- NO automated tests anywhere (Test Freeze). Checkpoints are builds + gates + manual probes.
- The contract gate ritual (T022, T040, T045, T056, T062) is historically the most-forgotten step
  (repo fact F1) — it is a named task on purpose. Same for the migration ritual (T033, T043, T049).
