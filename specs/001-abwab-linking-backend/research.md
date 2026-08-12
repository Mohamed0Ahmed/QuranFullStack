# Phase 0 Research: Abwab Ayah Linking — Consolidated Decisions

No `NEEDS CLARIFICATION` markers exist: the execution plan
(`docs/abwab-linking-backend-implementation-plan.md`) locked every open question, three residual
ambiguities were resolved in the spec's Clarifications session (2026-08-12), and the
identity-format authority was read directly from the Frontend source. This document consolidates
every decision an implementer must not re-litigate. IDs R1–R22; repo facts cited as F1–F14 from
the execution plan §0. A remediation pass (2026-08-12) refined five points — read-only workspace
load (R21), hash-based identity uniqueness (R20), derived automatic word contributions (R22), a
preflight token without `resolvedAtUtc` (R8), and attribution scoped to authored/lifecycle records
(R12) — and the execution plan was synchronized to them in the same pass, so **all current-truth
documents now agree**; the R-entries keep the history of why each refinement was made.

---

## R1 · Source identity canonicalizer — exact JavaScript escape parity

- **Decision**: `LinkingSourceIdentity.For(descriptor)` produces the exact string
  `linking-source-key.ts` produces: parts joined with `|`, each part encoded with the JavaScript
  `encodeURIComponent` escape set, `null` rendered as the empty string. In .NET, implement as
  `Uri.EscapeDataString(part)` **followed by un-escaping the five sequences `%21 %27 %28 %29 %2A`
  back to `! ' ( ) *`** (or an equivalent custom encoder). Full format and worked examples:
  `contracts/source-identity.md`.
- **Rationale**: `encodeURIComponent` leaves `A–Z a–z 0–9 - _ . ! ~ * ' ( )` unescaped;
  `Uri.EscapeDataString` (RFC 3986) additionally escapes `! ' ( ) *`. A raw `EscapeDataString`
  port would silently diverge on any label-free part containing those characters, splitting the
  cache and breaking workspace idempotency (spec FR-003). Phase 1 acceptance requires a
  hand-checked worked example per family against the TypeScript output.
- **Alternatives considered**: Raw `Uri.EscapeDataString` — rejected (escape-set mismatch).
  Changing the Frontend key format to something .NET-native — rejected (breaks stored prototype
  keys and violates "V2 is the product reference").

## R2 · One POST for source resolution (plan D1)

- **Decision**: Single `POST /api/linking/sources/resolve` taking the typed descriptor body; no
  GET twin.
- **Rationale**: Word Type carries eleven discriminators; a query-string encoding would be
  fragile. The route is `[RequireOwner]`, which already satisfies the unsafe-endpoint metadata
  rule for a POST-as-read — no exception needed.
- **Alternatives considered**: GET with encoded query — rejected (fragile encoding); one route per
  family — rejected (six routes, six swagger surfaces, no product gain).

## R3 · No ETag / conditional GET (plan D2)

- **Decision**: No conditional-request machinery anywhere in this feature.
- **Rationale**: The existing conditional-read pattern (no-store + boot-id validator) is
  internally inconsistent for this use; a content-derived validator is new machinery. The Backend
  cache + Frontend session cache already satisfy "never repeat expensive work".
- **Alternatives considered**: Content-hash ETag — rejected as deferred work (§12.7).

## R4 · Existing explorer DTOs untouched (plan D3)

- **Decision**: Canonical word ids surface **only** on the new Linking resolution DTO.
  `RootAyahMatchDto`/`LemmaAyahMatchDto`/`StemAyahMatchDto` are not modified.
- **Rationale**: Changing them churns Words routes in `swagger.json` and generated models for no
  product reason, making the `check-api-contract` diff unattributable. `AyahWordHydration` already
  loads `QuranWordId` for every word (F4) — projecting it on the new DTO costs zero extra queries.
- **Alternatives considered**: Adding `quranWordId` to the explorer DTOs — rejected (contract
  churn, D3).

## R5 · Soft delete at the contribution boundary only (plan D4)

- **Decision**: `linking_source_contributions` carries `deleted_at`/`deleted_by`. All children
  (`linking_units`, `linking_unit_ayahs`, `linking_unit_ayah_words`,
  `linking_unit_ayah_descriptions`) are **hard-deleted** by replacement semantics.
- **Rationale**: Replacement (Locked §6) requires a live contribution's children to be exactly the
  last confirmed state; tombstoned children would force every uniqueness index to become partial
  and accumulate on every re-add. Restore (future) restores a whole contribution, matching
  `abwab.doors.restore`.
- **Alternatives considered**: Soft delete on every child — rejected (index and accumulation
  cost); no soft delete at all — rejected (Locked §20 restore-compatible state).

## R6 · Fully-unchanged operation writes nothing (plan D5)

- **Decision**: No `linking_operations` row when every source classifies UNCHANGED. Response is a
  controlled success carrying exactly `لا توجد تغييرات جديدة لتنفيذها`. Because no row exists, **no
  idempotency record is stored** for a fully-unchanged no-op: durable replay (spec FR-050) applies
  only to confirmations that actually wrote an operation row; a repeated no-op — with or without
  an idempotency key — simply re-evaluates and returns the same success again.
- **Rationale**: Such a request is naturally idempotent; recording it adds nothing.
- **Alternatives considered**: Recording a no-op operation row — rejected (noise, no replay
  value).

## R7 · `OVERLAP_OTHER_SOURCE` is mutually exclusive, with precedence (plan D6)

- **Decision**: Ayah classifications are mutually exclusive and partition the submitted set.
  A source-owned change wins: `UPDATE`/`REMOVE` keep their classification even when the ayah
  overlaps another source; `OVERLAP_OTHER_SOURCE` applies only where the item would otherwise be
  `NEW_AYAH`. Every item carries structured `overlappingSources[]` regardless — `{ sourceIdentity,
  label, sourceKind }` per overlapping source, from each overlapping live contribution's stored
  descriptor snapshot — because a canonical identity like `unique-word|simple|3209` is a technical
  key, not a human-readable name the Arabic UI can show.
- **Rationale**: Makes per-source counts partition exactly, which the locked example requires
  (57 = 43 new + 14 overlapping + 0 unchanged + 0 updates).
- **Alternatives considered**: Orthogonal boolean overlap flag — rejected (counts would not
  partition; the locked example becomes ambiguous).

## R8 · Preflight token: required at Confirm, advisory in trust; Confirm re-runs everything (plan D7)

- **Decision**: Preflight returns a `preflightToken` — a hash over the Door's identity and live
  state, each affected contribution's `(id, xmin)`, and the canonical **operation intent** — plus
  per-contribution `(contributionId, xmin)`. The operation intent is a deterministic canonical
  serialization (stable field order, sources ordered by `orderValue`, id sets sorted) of exactly
  the fields that affect the classified linking intent: `doorId`; per source — the
  identity-bearing descriptor fields, `contributionMode`, `automaticWordMatchesEnabled`,
  `orderValue`, the unit/grouping structure, the submitted ayah ids, manual `selectedWordIds`,
  and descriptions. **Excluded** as Confirm-only or non-semantic: the token itself,
  `idempotencyKey`, `existingContributionId`/`existingContributionVersion`, `resolvedAtUtc`, and
  the display-only label (excluded from classification per spec FR-004). Preflight and Confirm
  use the **same canonicalization function**, so an unchanged request always reproduces the same
  token — staleness can only originate from the Door/contribution components.
  **`resolvedAtUtc` is deliberately NOT part of the
  token** (remediation refinement of the execution plan's composition): the token reflects only
  the operation intent and the mutable confirmed state relevant to the operation. The token is a
  **required** Confirm input — a missing token is a controlled validation failure, so the flow
  cannot reach Confirm without passing through Preflight. Required is not trusted: it proves the
  workflow ran Preflight and is never write authority. Confirm always re-runs
  the **same pure classifier** inside the write transaction, fully re-resolves source truth, and
  applies each `xmin` via `Entry(x).Property(x => x.Version).OriginalValue` — every read of
  mutable confirmed state (Door liveness, live contributions, version application,
  re-classification, token recomputation and comparison) happens under the same transaction
  snapshot that writes, leaving no check-then-write gap. Because READ COMMITTED alone would let
  two same-Door Confirms interleave, the transaction opens by acquiring a row-level write lock on
  the target `abwab_doors` row (`FOR UPDATE` via the repository's EF equivalent), held until
  COMMIT/ROLLBACK: same-Door Confirms serialize, a concurrent Door archive cannot slip between
  classification and write, and different Doors never contend — no broader locking architecture.
  The classifier itself stays pure (state in → classification out; no repository access). A stale
  token yields `409 PreflightStale` carrying fresh classification.
- **Rationale**: Trusting preflight would be a TOCTOU hole; the token exists only to answer
  accurately, never as authority. One classifier shared by both stages makes disagreement
  structurally impossible. Including `resolvedAtUtc` would fabricate staleness whenever the same
  unchanged source is re-resolved after cache expiry — a false `409` for a change that never
  happened; mere re-resolution must never stale a preflight.
- **Alternatives considered**: Server-side preflight session state — rejected (stateful, adds
  expiry semantics); trusting client-echoed classification — rejected (tamperable).

## R9 · Manual Mushaf resolves from `quran_words` directly (plan D8, F5)

- **Decision**: No Mushaf page assembly on the Backend. Completeness proof per verse: ayah exists
  and `verse_key` matches; its `quran_words` ordered by `word_number`; non-marker numbers
  contiguous `1..N`; `N == quran_ayahs.words_count_real`; every non-marker `location` unique with
  `(surah, ayah)` prefix matching. Any failure blocks with the verse named.
- **Rationale**: The Frontend needed page assembly only because the page API was its sole word
  source. Direct `quran_words` reads are strictly stronger and much cheaper.
- **Alternatives considered**: Replicating the Frontend's page-walk proof server-side — rejected
  (weaker, slower, pointless indirection).

## R10 · Workspace configuration replaced wholesale per source (plan D9)

- **Decision**: The configuration route takes one complete document per prepared source; the
  writer diffs children, resequences description order `1..N`, and hard-deletes what is absent.
- **Rationale**: Matches the Frontend's existing atomic draft save and keeps per-source `xmin`
  meaningful.
- **Alternatives considered**: Fine-grained child routes (add/remove word, add description) —
  rejected (chatty, unversionable, diverges from the proven V2 save shape).

## R11 · Cache: dedicated instance, `Task<T>`-in-entry, descriptor-only key (F7, F8)

- **Decision**: A **dedicated** `new MemoryCache(new MemoryCacheOptions { SizeLimit = … })` owned
  by `LinkingSourceResolutionCache`; entry `Size` = resolved ayah count; key
  `linking:source:v1:{kind}:{sha256(canonicalScope)[..16]}` derived only from the typed
  descriptor (reusing `WordTypesCacheKeys.HashParts` delimiter escaping); value is the compact
  form (`{ayahId, quranWordIds[], matchedQuranWordIds[]}` + ordered ayah-id list) with Uthmani
  text hydrated via the shared ayah-keyed `LinkingAyahTextCache`; `SlidingExpiration = 30 min`
  **and** `AbsoluteExpirationRelativeToNow = 4 h`; stampede control by storing
  `Task<LinkingResolvedSourceCompact>` in the entry; faulted tasks evicted immediately; no
  write-driven invalidation, restart clears.
- **Rationale**: The shared `IMemoryCache` is registered size-less — adding `SizeLimit` would make
  every existing size-less `Set` throw (F7). `CacheLoadGate`'s own comment forbids unbounded or
  caller-supplied keys (F8). Compact form measured ≈210 KB vs ≈4 MB per 2,000-ayah source. The
  README must record all three reasons (dedicated instance, no `CacheLoadGate`, no user in key) so
  nobody "harmonizes" or "secures" it later.
- **Alternatives considered**: Shared `IMemoryCache` — rejected (F7); `CacheLoadGate` — rejected
  (F8); Redis — rejected (deferred §12.4, blocked upstream by single-instance Abwab cache
  generation); caching the full DTO — rejected (≈19× memory).

## R12 · Actor identity and attribution (F10, F11)

- **Decision**: Attribution and workspace ownership flow from
  `IAuthorizationStateResolver` → `AuthorizationState.UserId` (the internal `access_users.id`),
  never from `ICurrentUser` (which only exposes the Logto `Sub`) and never from a request field.
  Attribution columns (`created_*`/`updated_*`/`deleted_*` as applicable) live on the
  authored/lifecycle tables only: `linking_workspaces`, `linking_workspace_sources`,
  `linking_workspace_source_descriptions`, `linking_source_contributions`,
  `linking_unit_ayah_descriptions`, and `linking_operations` (via `actor_user_id` +
  `confirmed_at`). Leaf relational rows (`linking_units`, `linking_unit_ayahs`,
  `linking_unit_ayah_words`, and the workspace manual-ayah/override/word child rows) inherit
  ownership and history from their parent aggregate and carry no audit columns — the schema is
  not expanded to satisfy blanket "every row" wording.
- **Rationale**: F10 is a hard wiring fact. Linking is the **first** area to actually populate the
  attribution columns (no Abwab writer does, F11) — the writer README must state this so it is not
  mistaken for an inconsistency.
- **Alternatives considered**: Copying an existing attribution helper — impossible, none exists
  (F11).

## R13 · Exception translation is mandatory in every writer save (F12)

- **Decision**: Postgres `23505` on the live-contribution unique index →
  `LinkingDuplicateContributionException` → `409`; `DbUpdateConcurrencyException` →
  `LinkingStaleVersionException` → `409`. Every save path in both writers translates; an
  untranslated save reaching the global handler as `500` is a defect.
- **Rationale**: Matches every Abwab writer's contract (`Writes/Abwab/README.md`).
- **Alternatives considered**: None viable — repo convention.

## R14 · DI registration convention (F13)

- **Decision**: `AddScoped<EfLinkingSourceResolutionReader>()` then
  `AddScoped<ILinkingSourceResolutionReader>(sp => new CachedLinkingSourceResolutionReader(...))`
  in `LinkingDependencyInjection.cs` under `Infrastructure`'s service-registration area, called
  from `PersistenceDependencyInjection`; Application handlers registered in
  `Application/DependencyInjection.cs`.
- **Rationale**: Verbatim repo convention.

## R15 · Clarification session outcomes (2026-08-12)

- **Decision 1 — zero-ayah submission rejected**: every source submitted to preflight or confirm
  must contribute ≥1 ayah (spec FR-044a). Total retraction is out of scope with delete/restore.
- **Decision 2 — resolution cap default 3,000** (spec FR-011): clears the largest known source
  (≈2,200) with headroom; the guard is verified locally by lowering the configured value.
- **Decision 3 — label excluded from change classification** (spec FR-004/FR-037): a label-only
  difference classifies `UNCHANGED` and writes nothing; the stored label refreshes on the next
  real update. Nothing in scope reads the stored label back (Door-links display deferred).
- **Rationale**: Recorded in spec §Clarifications; the classifier and validators must implement
  exactly these three.

## R16 · Contract regeneration ritual (F1)

- **Decision**: Every phase that adds or changes a Backend contract runs
  `Backend/scripts/export-swagger` → `npm run generate:api` and commits **both**
  `Frontend/quran-dashboard-ui/openapi/swagger.json` and
  `src/app/core/api/generated/models/**` in the same change; `Backend/scripts/check-api-contract`
  proves it.
- **Rationale**: `check-api-contract` diffs the committed artifacts — this is the single most
  frequently forgotten step (F1).

## R17 · Migration ritual (F2)

- **Decision**: Every migration: `Backend/scripts/add-mig <Name>` (EF tooling only) →
  `Backend/scripts/check-pending-model` (no drift) → `Backend/scripts/create-smoke-dump`
  (regenerate the gitignored canonical dump so `SmokeDumpGate`'s pinned head-migration id
  matches) → report name, files, build status, and whether `update-db` ran.
- **Rationale**: `SmokeDumpGate` fails the `smoke`/`canonical-data` lanes whenever the tree's head
  migration ≠ the dump manifest's `MigrationId` (F2). This is local artifact regeneration, not a
  test edit — Test Freeze intact.

## R18 · Editor virtualization approach

- **Decision**: Replace the editor's `<ul>` + pagination with `<cdk-virtual-scroll-viewport>`
  using autosize/measured rows (Quran text wraps → variable row heights; a fixed `itemSize` is
  wrong). The viewport becomes the surface's **single** vertical scroll owner (the editor's own
  `overflow: auto` yields). Delete `EDITOR_PAGE_SIZE`, `page`, `setPage`, `pageCount`,
  `visibleAyahs`; expose `filteredAyahs`. Selection/search/select-all/clear-all already operate on
  the complete universe — only rendering changes.
- **Rationale**: `@angular/cdk ^20.2.14` with `ScrollingModule` is already a dependency
  (`shared/ui/data-table`) — not a new dependency. Golden-UI contract applies (Quran glyph
  rendering protected); run `check:golden-ui` before `build:verify`.
- **Alternatives considered**: Keeping pagination — rejected (locked direction); fixed `itemSize`
  — rejected (variable-height Quran text corrupts offsets); review-step virtualization — rejected
  (explicitly deferred, stays paged at 12).

## R19 · Frontend session cache cap

- **Decision**: `LinkingSourceCache extends ApiResponseCache`, keyed
  `linking:source:{sourceIdentity}`, capped at **≈6 entries** with an explicit comment-exempt
  rationale (README), not the `ApiResponseCache` default of 48. `MushafReaderCache` stays for
  ordinary reader use and leaves the Linking resolution path.
- **Rationale**: 48 complete sources would hold tens of MB in the heap; ~6 matches realistic
  working sets.
- **Alternatives considered**: Default cap — rejected (memory); no client cache — rejected
  (Locked §11 requires layered caching; reopening must be instant, spec SC-002).

## R20 · Identity storage: fixed-size hash for uniqueness, raw text preserved

- **Decision**: Both descriptor-bearing tables keep `source_identity text NOT NULL` (the raw,
  byte-exact canonical identity — display/debug/parity, and the final equality guard) **and** add
  `source_identity_hash bytea NOT NULL` — the 32-byte SHA-256 digest of the UTF-8 bytes of the
  exact raw identity. All unique/index boundaries use the hash with their parent scope:
  `UNIQUE (workspace_id, source_identity_hash)` and
  `UNIQUE (door_id, source_identity_hash) WHERE deleted_at IS NULL`. On collision-sensitive paths
  (idempotent add, live-contribution matching), the writer compares the raw `source_identity` as
  the final guard after the hash lookup. **No cap is imposed on the manual verse-set size** beyond
  the ordinary resolution size cap (`MaxResolvedAyahs = 3000`). The identity algorithm itself is unchanged
  (`contracts/source-identity.md`).
- **Rationale**: Manual identities grow with the verse set; a raw-text btree unique index would
  hit PostgreSQL's ~2.7 KB index-entry limit on large manual sources. An earlier draft proposed a
  ≤200-verse cap — rejected as an unapproved product limit introduced solely to satisfy a storage
  detail. A fixed-size hash removes the constraint without touching product behavior. This is a
  recorded storage refinement of the execution plan's raw-column uniqueness; the uniqueness
  *semantics* (identity equality per Door / per workspace) are identical.
- **Alternatives considered**: Raw-text unique index — rejected (index-entry limit); verse-count
  cap — rejected (unapproved product decision); hash-only without raw text — rejected (loses
  display/debug/parity value and the collision guard).

## R21 · Workspace load is strictly read-only

- **Decision**: `GET /api/linking/workspace` never writes. If the caller's workspace row exists it
  is returned; if not, an **empty workspace representation** is returned (null version, empty
  source list) with no database insert. The workspace row is created atomically inside the first
  real mutation (typically the first add-source), with concurrent first mutations serialized by
  the `UNIQUE (user_id)` index. The execution plan originally said "GET creates the workspace row
  lazily on first access"; it was synchronized to this rule in the same remediation pass
  (2026-08-12).
- **Rationale**: A read must not have write side effects — it complicates read-only verification
  (row-count proofs), breaks the query/command separation every other boundary here observes, and
  writes a row for every user who merely opens the page.
- **Alternatives considered**: Lazy-create-on-GET — rejected (write-on-read); explicit create
  endpoint — rejected (the plan and spec both forbid a provisioning step).

## R22 · Automatic word contributions are derived, never authored

- **Decision**: The five automatic families (Unique Word, Root, Lemma, Stem, Word Type) never
  accept user-authored per-ayah word selections — not in the workspace configuration and not in
  the operation request. They carry only `automaticWordMatchesEnabled`: **on** ⇒ word
  contributions are derived server-side from the fresh resolution (that ayah's
  `matchedQuranWordIds`); **off** ⇒ the ayah is included with zero word contributions. Only
  manual Mushaf sources carry user-authored `quranWordId` selections (validated per spec FR-023).
  Submissions that author words on an automatic source are rejected outright. The confirmed
  `linking_unit_ayah_words` rows are materialized either way — authored for manual, derived for
  automatic.
- **Rationale**: The V2 product model never offered per-word curation on automatic sources — the
  toggle is the whole contract; accepting client word lists for automatic sources would create a
  tamper surface and an impossible-to-classify hybrid state.
- **Alternatives considered**: Client-submitted word subsets for automatic sources validated
  against `matchedQuranWordIds` — rejected (authors state the product does not have; bigger
  attack/validation surface).

---

## Resolution query-shape constraints (carried from plan Phases 2–3)

- Reuse the existing 4–5-command hydration pattern with `Skip/Take` removed — command count is
  bounded and independent of ayah count; never one query per ayah.
- `AyahWordHydration`'s marker filter becomes a parameter: Unique Word / Manual Mushaf keep markers,
  Root / Lemma / Stem / Word Type stay marker-free — today's shapes preserved exactly. (Both Word
  Types explorer ayah reads are marker-free, so Word Type resolution must be too.)
- Reuse `WordMorphologies` id predicates and the Word Type shared `BaseRowsSql` occurrence base
  (`EfWordTypesReader.Sql.cs`).
- `quran_words.location` carries a UNIQUE index (F6) — `wordLocation → quran_word_id` resolution
  is one batched `WHERE location = ANY(...)`.
- Determinism is a contract: order ayahs `(surah_number, ayah_number)`, words by `word_number`,
  always — the CDK viewport computes offsets from index (spec FR-006).
