# Abwab Quran Ayah Linking — Backend / Database Implementation Plan

**Objective.** Replace the Frontend V2 prototype's temporary persistence and mock write with a real
Backend: complete source resolution, source-result caching, per-user workspace persistence, a Linking
Preflight stage, and one atomic confirmation/update command — without redesigning the proven V2 UX.

**Authorities used**
1. `docs/abwab-linking-backend-database-architecture-report.md`
2. `docs/abwab-linking-frontend-v2-current-state-report.md`
3. Current repository code and the nearest current-truth READMEs.

**This document is a plan.** Nothing was implemented, no production code was modified, no migration,
API, or test was created, no Frontend file was touched, nothing was committed.

**Testing Decision: `none`.** The Test Freeze is in force
(`TESTING_CONSTITUTION.md` §The Test Freeze). No phase creates or modifies an automated test.
Verification is builds, typecheck, static guards, contract gates, engineering review, targeted manual
and browser checks, and safe local database inspection. Two retained *gates* are touched by schema
work and are called out where they occur (§Phase 5, §Phase 7) — those are gate re-runs and one local
artifact regeneration, not test edits.

---

## 0. Repository facts this plan is built on

Verified in code during planning. Each one changes at least one phase's content.

| # | Fact | Consequence |
| --- | --- | --- |
| F1 | `check-api-contract` runs `export-swagger` → `npm run generate:api` → `git diff --exit-code`. Both `Frontend/quran-dashboard-ui/openapi/swagger.json` and `src/app/core/api/generated/models/**` are committed. | **Every phase that adds or changes a Backend contract must regenerate and commit both artifacts in the same change.** This is the single most frequently forgotten step. |
| F2 | `SmokeDumpGate` fails when the tree's head migration ≠ the canonical dump manifest's `MigrationId` (`tests/.../Smoke/Data/SmokeDumpGate.cs:43-48`). The dump lives under gitignored `Backend/resources/`. | Every migration-bearing phase must re-run `Backend/scripts/create-smoke-dump` locally, or the `smoke` / `canonical-data` lanes fail. Local artifact regeneration, not a test edit. |
| F3 | `AbwabSchemaTests` queries `information_schema` **per named `abwab_*` table**, not the whole schema. | New `linking_*` tables break no retained schema gate. |
| F4 | `AyahWordHydration.AyahWordRow` already carries `QuranWordId` for every word; Root/Lemma/Stem DTOs simply drop it (`Reads/Quran/Words/AyahWordHydration.cs`). | Canonical word ids for those three families cost **zero extra queries** — a projection change only. |
| F5 | `EfAyahStudyReader.cs:109` projects `quran_ayahs.words_count_real`, and `WordsCountReal = ayahWords.Count - 1` (marker excluded, `QuranFoundationAssembler.cs:82`). | The Backend manual-Mushaf completeness proof is a `quran_words WHERE ayah_id = X` read + a contiguity check. **No Mushaf page assembly is needed at all** — the Frontend only needed pages because the page API was its only word source. |
| F6 | `quran_words.location` carries a UNIQUE index (`QuranWordConfiguration.cs:85`). | `wordLocation → quran_word_id` is one batched `WHERE location = ANY(...)`. |
| F7 | `AddMemoryCache()` is registered three times against one shared singleton with **no `SizeLimit` and no entry `Size`**. | A `SizeLimit` on the shared instance would make every existing size-less `Set` throw. Linking must own a **separate** `MemoryCache` instance. |
| F8 | `CacheLoadGate`'s own comment forbids reuse for "unbounded or caller-supplied keys". | Linking source keys are caller-supplied and combinatorial. Use a `Task<T>`-in-entry gate instead. |
| F9 | `AbwabPermissionCatalogue` is the single allowlist consulted by `PermissionAuthorizationHandler:20` and `UnsafeEndpointMetadataValidator:93`; its static ctor asserts 19 codes / 5 groups. | Confirms the locked decision: `[RequireOwner]` only. The catalogue is not touched. |
| F10 | `AuthorizationState(UserId, …)` exposes the internal `access_users.id`; `ICurrentUser` exposes only the Logto `Sub`. | Linking attribution and workspace ownership must flow from `IAuthorizationStateResolver`, **not** `ICurrentUser`. |
| F11 | No Abwab writer assigns `CreatedBy`/`UpdatedBy` — those columns are always NULL. | Linking must actually populate them; there is no existing helper to copy. |
| F12 | Every Abwab writer translates `DbUpdateConcurrencyException` and `23505` into plain Abstractions exception types; an untranslated save reaches the global handler as a `500` (`Writes/Abwab/README.md`). | Every Linking writer save must translate. |
| F13 | Registration convention: `AddScoped<EfXReader>()` then `AddScoped<IXReader>(sp => new CachedXReader(...))` in a `*DependencyInjection.cs` under `Infrastructure/ServiceRegistration`. | Linking follows it verbatim. |
| F14 | Frontend verification is three independent commands: `npm run check:no-unit-specs`, `npm run typecheck:app`, `npm run build:verify`; template/style changes add `npm run check:golden-ui` first. | Named per phase. |

---

## 1. Architectural decisions this plan fixes

Refinements the architecture report left open, decided here so no phase has to re-litigate them. Each
is the smallest clean option consistent with current architecture.

**D1 · One POST for source resolution.** `POST /api/linking/sources/resolve` with a descriptor body.
No GET twin. Word Type carries eleven discriminators and a query-string encoding of them would be
fragile. Because Linking is Owner-only, the POST carries `[RequireOwner]` and is therefore *already
compliant* with the unsafe-endpoint metadata rule — no exception is needed.

**D2 · No ETag / conditional GET in this plan.** The existing conditional-read pattern pairs
`Cache-Control: no-store` with a process-boot-id validator, which the locked scope explicitly flags as
internally inconsistent for this use, and a content-derived validator would be new machinery. The
Backend source-result cache plus the Frontend session cache already satisfy the "do not repeat
expensive work" requirement. Recorded as deferred (§Deferred work).

**D3 · Existing Words explorer DTOs are not changed.** Canonical word ids are surfaced on the **new
Linking resolution DTO only**. Changing `RootAyahMatchDto`/`LemmaAyahMatchDto`/`StemAyahMatchDto`
would churn the Words routes in `swagger.json` and the generated models for no product reason, and
would make the `check-api-contract` diff hard to attribute.

**D4 · Soft delete at the aggregate boundary only.** `linking_source_contributions` carries
`deleted_at`/`deleted_by`. Its children (`linking_units`, `linking_unit_ayahs`,
`linking_unit_ayah_words`, `linking_unit_ayah_descriptions`) are **hard-deleted** when replacement
semantics remove them. Rationale: replacement semantics (§Locked 6) require a live contribution's
children to be exactly the last confirmed state; tombstoned children would force every uniqueness
index to become partial and would accumulate on every re-add. Restore therefore restores a whole
contribution, matching `abwab.doors.restore`. Per-ayah removal history belongs to the deferred Stage-2
audit initiative. This is a refinement of the architecture report's C6 column set, permitted by Locked
§23 and recorded here.

**D5 · A fully-unchanged operation writes nothing.** No `linking_operations` row is created when every
selected source classifies `UNCHANGED`. The response is a controlled success carrying
`لا توجد تغييرات جديدة لتنفيذها`. Such a request is idempotent by nature, so nothing is lost by not
recording an idempotency key for it: durable idempotent replay exists only for operations that
wrote a `linking_operations` row — a repeated fully-unchanged request (with or without a key)
simply re-evaluates and returns the same no-op success again.

**D6 · `OVERLAP_OTHER_SOURCE` is a mutually-exclusive ayah classification, not an orthogonal flag.**
An ayah that would be newly added for this source **and** already exists in the Door via another source
classifies `OVERLAP_OTHER_SOURCE`; otherwise it classifies `NEW_AYAH`. Precedence: a source-owned
change wins — an ayah that is `UPDATE` or `REMOVE` for this source keeps that classification even when
it overlaps. Every item additionally carries structured `overlappingSources[]` —
`{ sourceIdentity, label, sourceKind }` per overlapping source, taken from each overlapping live
contribution's stored descriptor snapshot — so the UI can name the other sources in
human-readable Arabic, not by technical key alone. This makes the per-source counts partition the
requested set exactly, which is what the locked
example requires (`57 = 43 new + 14 overlapping + 0 unchanged + 0 updates`).

**D7 · Preflight safety = replay + tokens, not trust.** Preflight returns a `preflightToken` and, per
affected live contribution, its `contributionId` + `xmin`. Confirm **always re-runs the full
classification inside the write transaction** and applies each `xmin` as
`Entry(x).Property(x => x.Version).OriginalValue`, exactly as every Abwab write does. The token is a
**required** Confirm input — a missing token is a controlled validation failure, so the flow cannot
reach Confirm without passing through Preflight. Required is not trusted: the token exists
only to prove the workflow ran Preflight and to produce an accurate `409 PreflightStale` message
with fresh classification attached; it is
never the write authority.

**D8 · Manual Mushaf resolves from `quran_words` directly.** Per F5, the Backend does not need Mushaf
page assembly. The completeness proof becomes: the ayah exists; its non-marker `quran_words` rows have
contiguous `word_number` `1..words_count_real`; the count matches; every `location` is unique. This is
strictly stronger than the Frontend's page-assembly proof and much cheaper.

**D9 · Workspace child collections are replaced wholesale per source.** The workspace configuration
route takes one complete document per prepared source. This matches the Frontend's existing atomic
manual-word draft save and keeps the per-source `xmin` meaningful.

---

## 2. Phase map

| # | Phase | Layer | DB | Contract gate |
| --- | --- | --- | --- | --- |
| 1 | Shared Linking contracts + typed source identity | BE | — | — |
| 2 | Complete source resolution — automatic families + canonical word ids | BE + API | — | ✅ |
| 3 | Manual Mushaf source resolution on the Backend | BE + API | — | ✅ |
| 4 | Backend source-result cache | BE | — | — |
| 5 | Workspace schema + persistence | BE + API + DB | **M1** | ✅ |
| 6 | Per-source / per-ayah descriptions in the workspace | BE + API + DB | **M2** | ✅ |
| 7 | Confirmed Linking schema | BE + DB | **M3** | — |
| 8 | Preflight engine | BE + API | — | ✅ |
| 9 | Atomic confirmation / update engine | BE + API | — | ✅ |
| 10 | FE: source-resolution adapter + Linking cache + canonical word identity | FE | — | — |
| 11 | FE: workspace persistence adapter | FE | — | — |
| 12 | FE: preflight + confirm cutover | FE | — | — |
| 13 | FE: CDK virtualized source list + descriptions UI + merged provenance | FE | — | — |
| 14 | Integration hardening, current-truth docs, manual acceptance | Both | — | — |

Phases 1–9 are Backend-only and ship no user-visible change. The Frontend keeps working on
localStorage and the mock until Phase 10 begins. That is deliberate: it means Backend work can land and
be reviewed without a coordinated cutover.

---

# PHASE 1 — Shared Linking contracts + typed source identity

### Objective
Establish the typed source descriptor, its deterministic `SourceIdentity`, and the shared Linking
contracts, before anything reads, caches, or stores them. No database, no endpoint, no behaviour.

### Dependencies
None.

### Exact areas / files

**Add — `domain/QuranDashboard.Domain/Linking/`**
- `LinkingSourceKind.cs` — enum: `UniqueWord, Root, Lemma, Stem, WordType, ManualMushafAyahs`
- `LinkingUniqueWordMode.cs` — `Simple | Tashkeel`
- `LinkingWordTypeSelectionKind.cs` — `Word | Root | Stem | Lemma`
- `LinkingWordTypeScope.cs` — value object `(Type, ChildCode, Case, Tense, Voice)`
- `LinkingSourceDescriptor.cs` — the typed descriptor value object
- `LinkingManualLinkShape.cs` — `Grouped | Independent`
- `LinkingContributionMode.cs` — `Automatic | ManualSingle | ManualIndependent | ManualGrouped`

**Add — `application/QuranDashboard.Application.Abstractions/Linking/`**
- `LinkingSourceIdentity.cs` — the canonicalizer (static, pure)
- `LinkingSourceDescriptorValidation.cs` — descriptor well-formedness
- `LinkingLimits.cs` — `MaxDescriptionsPerSourceAyah = 10`, `MaxDescriptionLength = 2000`,
  `MaxResolvedAyahs = 3000`, `MaxPreparedSources = 100`. **These four are the only numeric product
  limits.** No per-operation ayah or source count cap exists — the structural rules (≥1 source per
  operation, ≥1 included ayah per submitted source) are shape requirements, not numeric limits. If
  a transport/request-size limit ever becomes necessary, it must be raised separately during
  implementation, never silently added as a product rule.
- exception types following the Abwab precedent: `LinkingSourceNotFoundException`,
  `LinkingInvalidDescriptorException`, `LinkingStaleVersionException`,
  `LinkingDuplicateContributionException`, `LinkingPreflightStaleException`

**Read-only authority**
- `Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts`
- `Frontend/.../features/linking/models/linking-source.models.ts`

### Domain / model changes
`LinkingSourceDescriptor` is a discriminated value object, not a bag of nullables:

```
LinkingSourceDescriptor
  Kind
  UniqueWord        : (Mode, WordId)?
  Root              : (RootId)?
  Lemma             : (LemmaId, TypeCode?)?
  Stem              : (StemId, TypeCode?)?
  WordType          : (SelectionKind, DimensionId | (TashkeelWordId, ContextCode, Case, Tense, Voice),
                       Scope)?
  ManualMushafAyahs : (ordered, de-duplicated VerseKey set)?
  Label             : string   // display snapshot, never identity
```

### The `SourceIdentity` canonicalizer — the single highest-risk item in the phase

`LinkingSourceIdentity.For(descriptor)` must produce a string **byte-identical** to the Frontend's
`linkingSourceKey`:

- parts joined with `|`, each part `encodeURIComponent`-escaped (`Uri.EscapeDataString` is *not*
  identical to `encodeURIComponent` — it escapes `!'()*`; the implementation must use the exact
  JavaScript escape set or the two will silently diverge);
- `null` renders as the empty string;
- kind tokens are the Frontend's kebab strings: `manual-mushaf-ayahs`, `unique-word`, `root`, `lemma`,
  `stem`, `word-type`;
- manual identity is the verse-key set only, numerically ordered by `(surah, ayah)` and de-duplicated;
- Word Type order is exactly `kind | selectionKind | id[ | contextCode | case | tense | voice] |
  scope.type | scope.childCode | scope.case | scope.tense | scope.voice`.

A divergence silently splits the cache and breaks workspace idempotency, so the phase must include a
written enumeration of one worked example per family, checked by hand against the TypeScript.

### Database changes
None.

### API / application changes
None.

### Cache changes
None.

### Frontend adapter changes
None.

### Migration requirements
None.

### Authorization implications
None (no endpoint).

### Concurrency implications
None.

### Acceptance criteria
- Solution builds.
- Each of the six kinds round-trips descriptor → identity deterministically; ordering and duplicates in
  a manual verse set do not change the identity.
- Impossible descriptors (e.g. a Word Type with no selection, a manual source with no verses) cannot be
  constructed.
- No file exceeds its `BACKEND_STRUCTURE.md` soft threshold; DTO/contract files stay under 150 lines.
- No `linking_*` table, endpoint, or DI registration exists yet.

### Verification strategy
`cd Backend && dotnet build`. Manual side-by-side comparison of the identity output against the
TypeScript for one example per family. Engineering review of the descriptor shape.

### Explicit out-of-scope
No reader, no cache, no table, no endpoint, no permission, no Frontend change.

---

# PHASE 2 — Complete source resolution: automatic families + canonical word ids

### Objective
One Linking-owned boundary that returns the **complete** validated ayah set for the five automatic
source families, in deterministic Quran order, with canonical `quranWordId` on every word.

### Dependencies
Phase 1.

### Exact areas / files

**Add — Abstractions `.../Linking/`**
- `ILinkingSourceResolutionReader.cs`
- `Responses/LinkingResolvedSourceDto.cs`, `LinkingResolvedAyahDto.cs`, `LinkingResolvedWordDto.cs`

**Add — Infrastructure `Persistence/Reads/Linking/`**
- `EfLinkingSourceResolutionReader.cs` (dispatch by kind)
- `EfLinkingSourceResolutionReader.Automatic.cs` (Root / Lemma / Stem)
- `EfLinkingSourceResolutionReader.UniqueWord.cs`
- `EfLinkingSourceResolutionReader.WordType.cs`
- `LinkingAyahHydration.cs`

**Add — Application `Quran/…` sibling: `application/QuranDashboard.Application/Linking/Queries/ResolveLinkingSource/`**
- `ResolveLinkingSourceQuery.cs`, `ResolveLinkingSourceHandler.cs`, `ResolveLinkingSourceOutcome.cs`

**Add — API**
- `Controllers/Linking/LinkingSourcesController.cs`
- `Contracts/Linking/LinkingSourceDescriptorBody.cs`

**Modify**
- `infrastructure/.../DependencyInjection/` — add `LinkingDependencyInjection.cs`, call it from
  `PersistenceDependencyInjection`
- `application/QuranDashboard.Application/DependencyInjection.cs` — register the handler
- `api/QuranDashboard.Api/README.md`, `infrastructure/.../Persistence/Reads/Quran/Words/README.md`
  (a "second consumer" note), and a new `Persistence/Reads/Linking/README.md`

**Reuse without changing their contracts**
- `AyahWordHydration` — but its marker filter must become a parameter (Unique Word / Word Type keep
  markers; Root / Lemma / Stem stay marker-free, preserving today's shapes)
- `WordMorphologies.Where(m => m.RootId|LemmaId|StemId == id)` predicates
- the Word Type shared `BaseRowsSql` occurrence base (`EfWordTypesReader.Sql.cs`)

### Domain / model changes
None beyond Phase 1.

### Database changes
None. Read-only.

### API / application changes

```
POST /api/linking/sources/resolve        [RequireOwner]
body: LinkingSourceDescriptorBody
200 → ApiResponse<LinkingResolvedSourceDto>
400 → invalid descriptor / MaxResolvedAyahs exceeded
404 → dimension id not found
```

```
LinkingResolvedSourceDto
  sourceIdentity, resolvedAtUtc, totalAyahCount
  ayahs[]  (ordered by surahNumber, ayahNumber)
    ayahId, verseKey, surahNumber, ayahNumber, surahNameArabic, pageFrom, pageTo
    matchedQuranWordIds : int[]
    words[]  (ordered by wordNumber)
      quranWordId, wordNumber, textUthmani, isAyahMarker
```

**Determinism is a contract, not a nicety** — the Frontend CDK viewport computes item offsets from
index, so an unstable order corrupts the viewport rather than merely reshuffling rows. Order by
`(surah_number, ayah_number)` and words by `word_number`, always.

**Query shape must stay bounded** — reuse the existing 4–5-command pattern with `Skip/Take` removed.
Never one query per ayah.

### Cache changes
None in this phase — Phase 4 adds the decorator. The reader is written so a decorator can wrap it
unchanged.

### Frontend adapter changes
None yet, but **F1 applies**: run `Backend/scripts/export-swagger` and
`npm run generate:api`, and commit `openapi/swagger.json` + the new generated models.

### Migration requirements
None.

### Authorization implications
`[RequireOwner]`. A POST used as a read is deliberate (D1) and is already metadata-valid.

### Concurrency implications
None — read-only, no state.

### Acceptance criteria
- All five automatic families resolve their complete set in one call; a 2,000-ayah Root returns 2,000
  ayahs.
- **Every** word carries a canonical `quranWordId`, including Root / Lemma / Stem.
- `matchedQuranWordIds` is non-empty for every returned ayah (an ayah with no match should not be in
  the set).
- Marker behaviour per family is unchanged from today's explorer reads.
- Ordinary explorer routes, DTOs, cache keys, and page sizes are byte-identical.
- Command count per resolution is bounded and independent of ayah count.
- `MaxResolvedAyahs` returns a controlled `400`, never an unbounded body.
- `openapi/swagger.json` and the generated models are regenerated and committed;
  `Backend/scripts/check-api-contract` passes.

### Verification strategy
`dotnet build`; `Backend/scripts/check-api-contract`; manual Swagger/`curl` against a local database
for one small source (Root with ~10 ayahs), one medium (~200), one large (~2,000) — recording
response size and wall time for the Phase 14 matrix; `EXPLAIN ANALYZE` on the large case;
engineering review with attention to the reader's file size (expect the partial split).

### Explicit out-of-scope
Manual Mushaf (Phase 3). Caching (Phase 4). Any table. Any Frontend consumption.

---

# PHASE 3 — Manual Mushaf source resolution on the Backend

### Objective
Bring the manual Mushaf source under the same resolution boundary and the same trust model, moving the
completeness proof server-side and returning canonical word ids.

### Dependencies
Phase 2.

### Exact areas / files

**Add**
- `Persistence/Reads/Linking/EfLinkingSourceResolutionReader.ManualMushaf.cs`
- `Abstractions/Linking/LinkingManualAyahCompleteness.cs` (the proof, pure)

**Modify**
- `EfLinkingSourceResolutionReader.cs` — dispatch the sixth kind
- `Persistence/Reads/Linking/README.md`

**Read-only authority**
- `Frontend/.../linking/utils/manual-mushaf-ayah-completeness.ts` and
  `data-access/manual-mushaf-ayah.reader.ts` — the behaviour being replaced

### Domain / model changes
None.

### Database changes
None.

### API / application changes
Same endpoint, same DTO. `manual_mushaf_ayahs` descriptors now resolve. One family-level
difference: `matchedQuranWordIds` may be **empty** for a manual ayah — Phase 2's non-empty rule is
automatic-family-only. A manually selected ayah with zero selected words is valid and still
contributes the ayah; its complete canonical word list is returned regardless.

**The completeness proof (D8, F5).** For each requested verse key:
1. the ayah exists in `quran_ayahs` and its `verse_key` matches;
2. read its `quran_words` rows ordered by `word_number`;
3. non-marker `word_number` values are contiguous `1..N`;
4. `N == quran_ayahs.words_count_real`;
5. every non-marker `location` is unique and its `(surah, ayah)` prefix matches the verse key.

Any failure is one controlled blocking error naming the verse key. A partial ayah is never published.
`pageFrom`/`pageTo` come from `quran_ayahs` and are returned as read context.

**No Mushaf page assembly, and no `MushafWordAnalysisApi`-equivalent call.** The Frontend needed pages
because the page API was its only word source; the Backend reads `quran_words` directly.

### Cache changes
None yet (Phase 4 covers all six kinds uniformly).

### Frontend adapter changes
None yet. Regenerate contracts if the DTO changed (it should not).

### Migration requirements
None.

### Authorization implications
Unchanged.

### Concurrency implications
None.

### Acceptance criteria
- A manual descriptor of N verse keys returns exactly N complete ayahs in Quran order.
- A page-spanning ayah returns as one complete ordered word list.
- Every word carries `quranWordId`; markers are present and flagged, matching the Mushaf reader's own
  shape.
- An unknown or malformed verse key is a controlled `400`/`404` naming it — never a silent drop.
- Manual and automatic sources are now indistinguishable in trust: same endpoint, same DTO, same
  validation posture.

### Verification strategy
`dotnet build`; contract gate; manual `curl` for a single-ayah source, a multi-ayah source, and a
known page-spanning ayah (e.g. a long Baqarah verse); cross-check the returned word list against the
Mushaf reader UI for one verse; engineering review.

### Explicit out-of-scope
Caching. Any table. Removing the Frontend's own manual reader (Phase 10).

---

# PHASE 4 — Backend source-result cache

### Objective
Stop rebuilding a complete source from the database on every open, with a dedicated, bounded,
stampede-safe cache holding a compact representation.

### Dependencies
Phases 2–3.

### Exact areas / files

**Add — `infrastructure/.../Caching/Linking/`**
- `LinkingSourceCacheKeys.cs`
- `LinkingSourceCacheEntryOptions.cs`
- `LinkingSourceResolutionCache.cs` — owns its **own** `MemoryCache` instance (F7)
- `CachedLinkingSourceResolutionReader.cs`
- `LinkingResolvedSourceCompact.cs` — the compact cached value
- `LinkingAyahTextCache.cs` — the shared ayah-keyed hydration cache

**Modify**
- `DependencyInjection/LinkingDependencyInjection.cs` (F13 pattern)
- `Persistence/Reads/Linking/README.md` + a new `Caching/Linking/README.md`

### Cache design

| Aspect | Decision |
| --- | --- |
| **Instance** | A dedicated `MemoryCache` created with `new MemoryCache(new MemoryCacheOptions { SizeLimit = … })`, registered as `LinkingSourceResolutionCache` and injected only into the Linking decorator. **The shared `IMemoryCache` is not touched** (F7). |
| **Key** | `linking:source:v1:{kind}:{sha256(canonicalScope)[..16]}`, derived **only** from the typed descriptor. Reuse `WordTypesCacheKeys.HashParts`' delimiter escaping so a free-text part cannot collide with a different scope. |
| **Never in the key or value** | user, Door, ayah inclusion/exclusion, selected words, descriptions, checked workspace membership, preflight/confirm state. |
| **Value** | **Compact**: per ayah `{ ayahId, quranWordIds[], matchedQuranWordIds[] }` plus a small ordered ayah-id list. Uthmani text and display metadata are hydrated from `LinkingAyahTextCache`, keyed by ayah id, which deduplicates across overlapping sources. Measured basis: the full DTO is ≈4 MB for a 2,000-ayah source; the compact form is ≈210 KB. |
| **Entry size** | `Size` = resolved ayah count, so `SizeLimit` is expressed in ayahs and is directly reasonable to configure. |
| **Expiration** | **Both**: `SlidingExpiration = 30 min` **and** `AbsoluteExpirationRelativeToNow = 4 h`. The absolute bound is the locked requirement that a hot entry can never stay fresh forever. |
| **Invalidation** | None write-driven; nothing in the API mutates Quran/morphology data. Restart clears. **No `quran_data_generation` marker** (locked). |
| **Stampede** | Store `Task<LinkingResolvedSourceCompact>` in the entry so the entry itself is the gate and eviction is automatic. **`CacheLoadGate` is not reused** (F8) — record the reason in the README so a future reader does not "harmonize" it. |
| **Failure** | A faulted task is never left in the cache; the entry is removed so the next caller retries. |

Configuration lives in an options record with sane defaults, bound from `appsettings`, following
`MushafReaderOptions`' precedent.

### Domain / DB / API changes
None. The decorator is invisible on the wire.

### Frontend adapter changes
None.

### Migration requirements
None.

### Authorization implications
None — the cache is keyed on source truth only and is therefore safe to share across actors. Record
that reasoning in the README so nobody later adds a user to the key "to be safe" and destroys reuse.

### Concurrency implications
Concurrent identical resolutions collapse to one database load. Concurrent *different* resolutions are
independent.

### Acceptance criteria
- Second resolution of the same source performs **zero** database commands.
- Resolutions of two different sources do not cross-serve; a Word Type scope differing in one field is
  a different entry.
- A cached entry cannot outlive its absolute expiry.
- The shared `IMemoryCache` still has no `SizeLimit`, and no existing `Set` call was modified.
- Memory for a warm set of ~8 large sources stays in the low tens of MB.

### Verification strategy
`dotnet build`; local run with EF command logging at `Information` to prove the second call issues no
SQL; a deliberate two-concurrent-request check to observe a single load; a memory reading before and
after warming several large sources; engineering review of the decorator and the README's three
recorded reasons (dedicated instance, no `CacheLoadGate`, no user in the key).

### Explicit out-of-scope
Redis. ETag. Frontend caching (Phase 10). Any table.

---

# PHASE 5 — Workspace schema + persistence

### Objective
Real per-user server-side workspace storage replacing localStorage, preserving the proven V2
semantics. Descriptions are deliberately deferred one phase.

### Dependencies
Phase 1 (identity), Phase 2 (so a resolved count can be refreshed) — but not Phase 4.

### Exact areas / files

**Add — Domain `Linking/`**: `LinkingWorkspace.cs`, `LinkingWorkspaceSource.cs`,
`LinkingWorkspaceSourceManualAyah.cs`, `LinkingWorkspaceSourceAyahOverride.cs`,
`LinkingWorkspaceSourceWord.cs`

**Add — Infrastructure**
- `Persistence/Configurations/Linking/` — one configuration per entity
- `Persistence/Reads/Linking/EfLinkingWorkspaceReader.cs`
- `Persistence/Writes/Linking/EfLinkingWorkspaceWriter.cs`
- `Migrations/<ts>_AddLinkingWorkspace.cs` (**M1**, EF tooling only)

**Add — Abstractions**: `ILinkingWorkspaceReader.cs`, `ILinkingWorkspaceWriter.cs`,
`Responses/LinkingWorkspaceDto.cs`

**Add — Application** `Linking/Commands/…` + `Linking/Queries/GetLinkingWorkspace/`:
get, add source, remove source, reorder sources, replace source configuration, clear all

**Add — API**: `Controllers/Linking/LinkingWorkspaceController.cs`

**Modify**: `QuranDashboardDbContext` (five `DbSet`s), `LinkingDependencyInjection`,
`Application/DependencyInjection.cs`, `Persistence/Writes/Linking/README.md` (new),
`api/QuranDashboard.Api/README.md`

### Database changes — **M1**

| Table | Key | Notable columns | Unique / checks | Indexes |
| --- | --- | --- | --- | --- |
| `linking_workspaces` | `id` | `user_id`, `created_at`, `created_by`, `updated_at`, `updated_by`, `xmin` | `UNIQUE (user_id)`; FK → `access_users` RESTRICT | the unique index |
| `linking_workspace_sources` | `id` | `workspace_id`, `order_value`, `source_kind`, `source_identity` (raw canonical text — never in a UNIQUE btree), `source_identity_hash` (`bytea`, 32-byte SHA-256 of the exact UTF-8 raw identity), `label`, `scope jsonb`, `root_id`, `lemma_id`, `stem_id`, `unique_simple_word_id`, `unique_tashkeel_word_id`, `word_type_tashkeel_word_id`, `inclusion_mode`, `automatic_word_matches_enabled`, `manual_link_shape`, `last_resolved_count`, `last_resolved_at_utc`, audit cols, `xmin` | `UNIQUE (workspace_id, source_identity_hash)` (raw `source_identity` compared as the final equality guard); CHECK `source_kind` ∈ 6; CHECK `inclusion_mode` ∈ (`all_except`,`only`); CHECK `manual_link_shape` ∈ (`grouped`,`independent`) or NULL; CHECK jsonb object + numeric `schemaVersion`; **CHECK kind/configuration coherence** (`automatic_word_matches_enabled IS NOT NULL` iff kind ≠ manual; `manual_link_shape IS NOT NULL` iff kind = manual); **CHECK kind/reference coherence** (exactly the expected dimension column non-null per kind) | `(workspace_id, order_value)` |
| `linking_workspace_source_manual_ayahs` | `(workspace_source_id, ayah_id)` | `order_value`, `page_hint` | FKs: source CASCADE, `quran_ayahs` RESTRICT | `(workspace_source_id, order_value)` |
| `linking_workspace_source_ayah_overrides` | `(workspace_source_id, ayah_id)` | — | same FK pattern | PK |
| `linking_workspace_source_words` | `(workspace_source_id, quran_word_id)` | `ayah_id` (manual Mushaf sources only — automatic sources never have rows here; writer-enforced) | FKs: source CASCADE, `quran_words` RESTRICT, `quran_ayahs` RESTRICT | `(workspace_source_id, ayah_id)` |

`CASCADE` from a source to its own child collections is the only cascade in the model; everything
pointing at Quran or Access data is `RESTRICT`.

**Storing `ayah_id` rather than a verse-key string is a real upgrade over the prototype**, whose codec
could only validate syntax. The override set and manual verse set are now FK-validated.

### API / application changes

```
GET    /api/linking/workspace                             [RequireOwner]
POST   /api/linking/workspace/sources                     [RequireOwner]  idempotent by source_identity
DELETE /api/linking/workspace/sources/{id}                [RequireOwner]
PUT    /api/linking/workspace/sources/order               [RequireOwner]  workspace xmin
PUT    /api/linking/workspace/sources/{id}/configuration  [RequireOwner]  source xmin, whole document (D9)
DELETE /api/linking/workspace/sources                     [RequireOwner]  clear all, workspace xmin
```

- `GET` is **strictly read-only**: when no workspace row exists it returns an empty workspace
  representation (`workspaceVersion = null`, empty source list) and performs **zero inserts**. The
  first real mutation (typically the first add-source) creates the row atomically inside its own
  transaction, with concurrent first mutations serialized by `UNIQUE (user_id)`. No provisioning
  step, no separate create endpoint.
- The workspace is always **the caller's own**. There is no `?userId=` and no admin view.
- Manual selected words arrive as canonical `quranWordId` values already (the Frontend resolves them
  in Phase 11); the writer still validates them (exists, non-marker, belongs to the declared ayah,
  belongs to the source's manual verse set). User-authored words exist **only** on manual Mushaf
  sources — an automatic source carries `automatic_word_matches_enabled` instead (on ⇒ its word
  contributions derive from resolution; off ⇒ its ayahs contribute zero words), and a submission
  that authors words on an automatic source is rejected outright. A manual ayah with zero selected
  words is valid.

### Cache changes
None. Workspace persistence is not a cache — state this in the README so the three concerns stay
separate.

### Frontend adapter changes
None yet (Phase 11). Regenerate and commit contracts (F1).

### Migration requirements
- `Backend/scripts/add-mig AddLinkingWorkspace` — EF tooling only, never hand-written.
- Report the migration name, generated files, build status, and whether `update-db` was executed
  (`Backend/README.md` §Invariants).
- **Re-run `Backend/scripts/create-smoke-dump`** — the head migration moved and `SmokeDumpGate` will
  otherwise fail the `smoke` / `canonical-data` lanes (F2). Local artifact regeneration only.
- Run `Backend/scripts/check-pending-model` afterwards to prove no drift.

### Authorization implications
Every route `[RequireOwner]`. Ownership is enforced by resolving `AuthorizationState.UserId` (F10) and
scoping every query to it — never by trusting a body field.

### Concurrency implications
- Workspace-level `xmin` for add / remove / reorder / clear.
- Source-level `xmin` for configuration replacement, so two sources can be edited without a false
  conflict.
- A stale token is a **`409`**, never last-writer-wins. The writer must translate
  `DbUpdateConcurrencyException` → `LinkingStaleVersionException` and `23505` →
  `LinkingDuplicateContributionException` (F12).

### Acceptance criteria
- A workspace round-trips: add 3 sources of different families, configure each, reorder, reload — all
  preserved.
- Re-adding an equivalent descriptor updates the label and leaves order and configuration untouched
  (server-side idempotency matching the Frontend's `addSource`).
- The two coherence CHECKs make an automatic source with a manual link shape (and vice versa)
  **rejected by the database**, not just by the writer.
- Actor A cannot read or write actor B's workspace by any route.
- A stale `xmin` returns `409` with the Arabic envelope; no raw EF exception reaches the handler.
- `check-pending-model` reports no pending changes; `check-api-contract` passes.

### Verification strategy
`dotnet build`; migration applied to a local database; `psql` inspection of the created constraints and
indexes (each CHECK exercised once by hand with a deliberate bad INSERT that must fail); Swagger
round-trip of all six routes; a two-tab stale-token check; engineering review; re-run the affected
gates after `create-smoke-dump`.

### Explicit out-of-scope
Descriptions (Phase 6). Confirmed tables (Phase 7). Any Frontend change. Any permission code.

---

# PHASE 6 — Per-source / per-ayah descriptions in the workspace

### Objective
Add the locked descriptions model to workspace state, with limits enforced consistently in the
database, the writer, and the contract.

### Dependencies
Phase 5.

### Exact areas / files
- Domain: `LinkingWorkspaceSourceDescription.cs`
- Configuration: `LinkingWorkspaceSourceDescriptionConfiguration.cs`
- Migration `<ts>_AddLinkingWorkspaceDescriptions.cs` (**M2**)
- Extend `ILinkingWorkspaceWriter` / `EfLinkingWorkspaceWriter` / `LinkingWorkspaceDto`
- Extend the configuration-replacement command and its API contract
- `Persistence/Writes/Linking/README.md`

### Database changes — **M2**

`linking_workspace_source_descriptions`

| | |
| --- | --- |
| Key | `id bigserial` |
| Columns | `workspace_source_id`, `ayah_id`, `order_value int`, `body varchar(2000)`, audit cols, `xmin` |
| FKs | source CASCADE · `quran_ayahs` RESTRICT |
| Checks | `btrim(body) <> ''` · `order_value BETWEEN 1 AND 10` |
| Indexes | `UNIQUE (workspace_source_id, ayah_id, order_value)` |

The **hard** database guarantee for "max 10 per `(Source, Ayah)`" needs **both** halves: the
`BETWEEN 1 AND 10` check **and** the UNIQUE order position — without uniqueness, eleven rows could
reuse the same `order_value` and bypass the limit. The index above is therefore
`UNIQUE (workspace_source_id, ayah_id, order_value)`, and the writer still resequences `1..N` on
every mutation.

### API / application changes
Descriptions ride inside the existing per-source configuration document (D9) — no separate route. The
document carries, per ayah, an ordered list of bodies; the writer diffs, resequences `1..N`, and hard
-deletes what is absent. Validation: ≤10 per `(source, ayah)`, ≤2000 chars, non-blank, plain text, and
the ayah must belong to that source's own set.

The shared limits live in `LinkingLimits` (Phase 1) and are referenced by the writer, the validator,
and — in Phases 8–9 — preflight and confirm, so the four sites cannot drift.

### Cache changes
None. Descriptions are user data and must never enter the source-result cache.

### Frontend adapter changes
None yet. Regenerate contracts (F1).

### Migration requirements
`add-mig AddLinkingWorkspaceDescriptions`; `create-smoke-dump` again (F2);
`check-pending-model`.

### Authorization implications
Unchanged.

### Concurrency implications
Descriptions are replaced under the owning source's `xmin`; they have no independent token.

### Acceptance criteria
- Up to 10 ordered descriptions persist per `(source, ayah)`; an 11th is refused by both the writer and
  the CHECK.
- A 2001-character body is refused.
- Reordering and removing individual descriptions works and resequences `1..N`.
- Two sources contributing the same ayah keep entirely separate description lists — verified by
  inspection.

### Verification strategy
`dotnet build`; migration + `psql` constraint check including a deliberate 11th-row INSERT that must
fail; Swagger round-trip; engineering review.

### Explicit out-of-scope
Descriptions UI (Phase 13). Confirmed-side descriptions (Phase 7).

---

# PHASE 7 — Confirmed Linking schema

### Objective
Create the durable domain tables and their EF mapping. **Schema and mapping only — no behaviour.**
Isolating the migration from the engine keeps a large, hard-to-revert change reviewable on its own.

### Dependencies
Phase 1. (Independent of 5–6, but sequenced after so the migration order is linear.)

### Exact areas / files
- Domain `Linking/`: `LinkingOperation.cs`, `LinkingSourceContribution.cs`, `LinkingUnit.cs`,
  `LinkingUnitAyah.cs`, `LinkingUnitAyahWord.cs`, `LinkingUnitAyahDescription.cs`
- `Persistence/Configurations/Linking/` — six configurations
- `Migrations/<ts>_AddLinkingConfirmedState.cs` (**M3**)
- `QuranDashboardDbContext` — six `DbSet`s
- `Persistence/Writes/Linking/README.md`

### Database changes — **M3**

**`linking_operations`** — `id`; `door_id` FK→`abwab_doors` RESTRICT; `actor_user_id` FK→`access_users`
RESTRICT; `idempotency_key uuid`; `confirmed_at`; `source_count`; `ayah_count`; `outcome jsonb`.
`UNIQUE (idempotency_key)`. Index `(door_id, confirmed_at DESC)`. Append-only; never soft-deleted.
`outcome` is a **response snapshot for idempotent replay**, not relational truth — bounded, with a
numeric `schemaVersion` and the same `jsonb_typeof` CHECK pattern `access_audit_events` uses.
**Lifecycle precision:** "append-only / never updated" means immutable **after** its confirmation
transaction commits — never edited by later operations, never soft-deleted. Inside its own creation
transaction the row is inserted early (so `operation_id` is available to contributions) and its
`outcome` is finalized exactly once — with the final contribution ids, applied classifications, and
counts — before COMMIT. An INSERT followed by an UPDATE within that one creation transaction is
construction of the new operation, not a later lifecycle update; the stored outcome equals the
logical result the successful confirmation returned.

**`linking_source_contributions`** — `id`; `operation_id` FK RESTRICT; `door_id` FK RESTRICT
(denormalized); `order_value`; `contribution_mode`; the full descriptor column set from Phase 5
(`source_kind`, `source_identity`, `source_identity_hash`, `label`, `scope jsonb`, six dimension
FKs); `resolved_ayah_count`;
`resolved_at_utc`; audit cols; `deleted_at`, `deleted_by`; `xmin`.
**`UNIQUE (door_id, source_identity_hash) WHERE deleted_at IS NULL`** — the Door+Source boundary
(raw `source_identity` compared as the final equality guard; the raw text is never in a UNIQUE
btree because manual identities are unbounded in length).
Also `UNIQUE (id, door_id)` (redundant, enables a composite FK later if a Door-scoped child is ever
added). Indexes: `(operation_id, order_value)`; `(door_id) WHERE deleted_at IS NULL`; one filtered
index per dimension column for the "which links came via root X" provenance question.
CHECKs: `contribution_mode` ∈ 4; manual modes iff `source_kind = 'manual_mushaf_ayahs'`; the same
scope-jsonb and kind/reference coherence CHECKs as the workspace table.

**`linking_units`** — `id`; `source_contribution_id` FK RESTRICT; `order_value`; `is_grouped bool`.
`UNIQUE (source_contribution_id, order_value)`; `UNIQUE (id, source_contribution_id)` (enables the
composite FK below). CHECK: `is_grouped = false` unless the parent is a manual grouped contribution —
enforced in the writer for the cross-row half (this repository uses **no triggers**; the limit is
recorded honestly in the README).

**`linking_unit_ayahs`** — `id`; `unit_id`; `source_contribution_id` (denormalized); `ayah_id` FK
RESTRICT; `order_value`. **Composite FK `(unit_id, source_contribution_id)` → `linking_units(id,
source_contribution_id)`** so the denormalized column cannot disagree with its grandparent.
**`UNIQUE (source_contribution_id, ayah_id)`** — one source contributes an ayah at most once.
Indexes: the unique one; `(unit_id, order_value)`; **`(ayah_id)`** for the future reverse read.

**`linking_unit_ayah_words`** — `(unit_ayah_id, quran_word_id)` PK; `ayah_id`. FKs: unit-ayah CASCADE,
`quran_words` RESTRICT, `quran_ayahs` RESTRICT. Index `(quran_word_id)`.

**`linking_unit_ayah_descriptions`** — `id`; `unit_ayah_id` FK CASCADE; `order_value`;
`body varchar(2000)`; audit cols. CHECKs `btrim(body) <> ''`, `order_value BETWEEN 1 AND 10`. Index
`UNIQUE (unit_ayah_id, order_value)` (uniqueness + the `BETWEEN` check together form the hard
max-10 guarantee, exactly as on the workspace table). **No `deleted_at`** — per D4, children are hard-deleted by replacement
semantics and soft delete lives at the contribution boundary.

### API / application / cache changes
None.

### Frontend adapter changes
None. No contract is added, so no regeneration is needed.

### Migration requirements
`add-mig AddLinkingConfirmedState`; `create-smoke-dump` (F2); `check-pending-model`.

### Authorization implications
None yet.

### Concurrency implications
`xmin` mapped on `linking_source_contributions`. Phases 8–9 use it.

### Acceptance criteria
- Migration applies cleanly to an empty database and to a database already at Phase 6's head.
- Every constraint listed above exists, verified in `psql`.
- Inserting two live contributions with the same Door + source identity (via
  `source_identity_hash`) fails; inserting one after
  soft-deleting the other succeeds.
- Inserting a `linking_unit_ayahs` row whose `source_contribution_id` disagrees with its unit's parent
  fails on the composite FK.
- `check-pending-model` reports no drift. `AbwabSchemaTests` still passes untouched (F3).

### Explicit out-of-scope
Any write path, any endpoint, any classification logic.

---

# PHASE 8 — Preflight engine

### Objective
A read-only boundary that classifies a proposed operation against the Door's current confirmed state
and returns the **exact affected ayahs**, not counts alone.

### Dependencies
Phases 2–4 (resolution + cache), 7 (confirmed tables). Phase 5–6 are not required — preflight receives
the operation in the request body, not from the workspace.

### Exact areas / files
- Abstractions `Linking/Preflight/`: `LinkingOperationRequest.cs`,
  `LinkingPreflightResultDto.cs`, `LinkingSourcePreflightDto.cs`, `LinkingAyahPreflightDto.cs`,
  `LinkingPreflightClassification.cs`, `LinkingPreflightToken.cs`
- Application `Linking/Queries/PreflightLinkingOperation/` — query, handler, outcome
- Application/Domain service: `LinkingOperationClassifier.cs` (**pure**, shared with Phase 9)
- Infrastructure `Persistence/Reads/Linking/EfLinkingConfirmedStateReader.cs` — loads the Door's
  current live contributions and their children in **bounded batched queries**
- API: `Controllers/Linking/LinkingOperationsController.cs` (preflight action)

The classifier being pure and shared with Confirm is the design's load-bearing choice: Confirm re-runs
the *same code* inside the transaction, so the two can never disagree about semantics.

### The classification contract

**Source level** — `NEW_SOURCE | UNCHANGED | UPDATE | INVALID`

**Ayah level** (mutually exclusive; the per-source counts partition the requested set):

| Value | Meaning |
| --- | --- |
| `NEW_AYAH` | Not currently contributed by this source, and **not** present in this Door via any other source. |
| `OVERLAP_OTHER_SOURCE` | Would be newly added for this source, and the ayah **already exists in this Door via ≥1 other source**. Not a conflict, not skipped — the new contribution is still added independently. |
| `UNCHANGED` | Present for this source with identical effective words, descriptions, and grouping. No write. |
| `UPDATE` | Present for this source, but membership, words, descriptions, or source-owned confirmed configuration changed. |
| `REMOVE` | Present in the current confirmed contribution, absent from the newly confirmed complete source state. Removed from **this** contribution only. |
| `INVALID` | Door / source / ayah / word / grouping data is no longer valid. **Blocking.** |

**Precedence (D6):** a source-owned change wins. An ayah that is `UPDATE` or `REMOVE` for this source
keeps that classification even when it overlaps another source; `OVERLAP_OTHER_SOURCE` is used only
where the item would otherwise be `NEW_AYAH`. Every item carries structured `overlappingSources[]`
(`sourceIdentity`, `label`, `sourceKind` per overlapping source) regardless,
so the UI can name the other sources in human-readable form in all cases.

**Blocking vs informational:** only `INVALID` blocks. `OVERLAP_OTHER_SOURCE` and `UNCHANGED` are
informational and never stop the operation.

### API / application changes

```
POST /api/linking/operations/preflight   [RequireOwner]
body: LinkingOperationRequest  (the same immutable operation shape Confirm takes, minus the
                                idempotency key and the preflightToken this call produces)
200 → ApiResponse<LinkingPreflightResultDto>
```

```
LinkingPreflightResultDto
  doorId, doorName
  isNoOp : bool                    // every source UNCHANGED
  isBlocked : bool                 // any INVALID
  preflightToken : string
  totals : { requested, new, overlapping, unchanged, updated, removed, invalid }
  sources[] : LinkingSourcePreflightDto
      sourceIdentity, label, sourceKind, contributionMode
      classification : NEW_SOURCE | UNCHANGED | UPDATE | INVALID
      existingContributionId : long?      // present when a live contribution exists
      existingContributionVersion : uint? // its xmin — carried to Confirm (D7)
      counts : { requested, new, overlapping, unchanged, updated, removed, invalid }
      ayahs[] : LinkingAyahPreflightDto
          ayahId, verseKey, surahNumber, ayahNumber
          classification
          overlappingSources[] : { sourceIdentity, label, sourceKind }
                                          // other sources in this Door holding this ayah,
                                          // from their stored descriptor snapshots
          wordChanges       : { added[], removed[], unchanged[] }   // canonical quranWordIds
          descriptionChanges: { added[], removed[], changed[], unchanged[] }
          invalidReason?
```

**Counts always accompany the items; items are never replaced by counts.** The locked example
(`57 requested = 43 new + 14 overlapping`) must be reproducible from this shape, and the 14 must be
individually inspectable.

**Preflight performs no writes.** It is a query handler and takes no transaction.

### Cache changes
Preflight reads source membership through the Phase 4 cached boundary. Confirmed-state reads are **not**
cached — they are per-Door mutable state.

### Frontend adapter changes
None yet. Regenerate contracts (F1).

### Migration requirements
None.

### Authorization implications
`[RequireOwner]`.

### Concurrency implications
`preflightToken` is a hash over three components: the Door's identity and live state; each
affected contribution's `(id, xmin)`; and the canonical **operation intent** — a deterministic
canonical serialization (stable field order, sources ordered by `orderValue`, id sets sorted) of
exactly the fields that affect the classified linking intent: `doorId`, and per source the
identity-bearing descriptor fields, `contributionMode`, `automaticWordMatchesEnabled`,
`orderValue`, the unit/grouping structure, the submitted ayah ids, manual `selectedWordIds`, and
descriptions. **Excluded** as Confirm-only or non-semantic: the token itself, `idempotencyKey`,
`existingContributionId`/`existingContributionVersion`, `resolvedAtUtc`, and the display-only
label (excluded from change classification). Preflight and Confirm use the **same
canonicalization function**, so an unchanged request always reproduces the same token — staleness
can only originate from the Door/contribution components. `resolvedAtUtc` deliberately does
**not** participate: cache expiry or re-resolution of unchanged source truth can never fabricate
staleness (Confirm fully re-resolves and revalidates source truth anyway). It is **advisory**
(D7). It is returned so Confirm can detect drift and answer accurately; it is never authority.

### Acceptance criteria
- The locked worked example reproduces exactly: `الرحمن` already linked with A/B/C; a new `الرحيم`
  source with A/D/E preflights as `NEW_SOURCE` with A = `OVERLAP_OTHER_SOURCE` and D, E = `NEW_AYAH`.
- Root X unchanged + Lemma Z new → `UNCHANGED` + `NEW_SOURCE`, `isNoOp = false`, operation continues.
- All sources unchanged → `isNoOp = true`, no blocking.
- Removing an ayah from a source's confirmed state classifies `REMOVE` for that source and does not
  touch the same ayah under other sources.
- Word and description diffs are exact and canonical (`quranWordId` only).
- An archived Door, an unknown ayah, a marker word, or a word not belonging to its declared ayah
  classifies `INVALID` and sets `isBlocked`.
- No row is written by any preflight call — verified by row counts before/after.

### Verification strategy
`dotnet build`; contract gate; a hand-built local scenario in `psql` reproducing the locked example,
then preflighted through Swagger; row-count-before/after check to prove read-only; engineering review
focused on the classifier's purity (it must take state in and return classification out, with no
repository access inside).

### Explicit out-of-scope
Any write. The Door-links read model. Frontend integration.

---

# PHASE 9 — Atomic confirmation / update engine

### Objective
One atomic command that applies the classified operation: creating new contributions, updating existing
ones with **replacement** semantics, removing absent ayahs, and skipping unchanged sources.

### Dependencies
Phases 7 and 8.

### Exact areas / files
- Abstractions: `ILinkingConfirmationWriter.cs`, `Responses/LinkingConfirmationResultDto.cs`
- Infrastructure `Persistence/Writes/Linking/EfLinkingConfirmationWriter.cs`
- Application `Linking/Commands/ConfirmLinkingOperation/` — command, handler, outcome
- API: the confirm action on `LinkingOperationsController`
- `Persistence/Writes/Linking/README.md` — the full conventions record, in the style of
  `Writes/Abwab/README.md`

### API / application changes

```
POST /api/linking/operations   [RequireOwner]
body: LinkingOperationRequest + idempotencyKey (uuid)
      + per-source { existingContributionId?, existingContributionVersion? }
      + preflightToken   (REQUIRED — missing token is a controlled 400; see D7)
200 → ApiResponse<LinkingConfirmationResultDto>   (also the no-op success)
400 → validation / invalid classification / missing preflightToken
404 → door not found
409 → stale contribution version, stale preflight, or duplicate live contribution
```

### Validation and the transaction boundary

**Phase A — before the write transaction.** Only work on immutable inputs and Quran source truth
(which never changes at runtime); nothing here reads mutable confirmed state:

1. **Structure** — request shape; `preflightToken` present (required); ≥1 source; ≥1 included ayah
   per source; no duplicate ayah within a contribution.
2. **Actor** — `AuthorizationState.UserId` (F10); **Owner** — `[RequireOwner]` plus a re-check in
   the handler.
3. **Descriptors** — valid; dimension ids exist.
4. **Source membership** — re-resolve each source through the Phase 4 cached boundary; every submitted
   ayah must be a member. This is the anti-tamper boundary; warm-cache cost is ~0.
5. **Words** — manual sources only may author word ids: each must exist, be non-marker, and
   belong to the declared ayah. Automatic sources must author none — their word contributions are
   derived here from the fresh resolution (`automatic_word_matches_enabled` on ⇒ that ayah's
   `matchedQuranWordIds`; off ⇒ none).
6. **Grouping** — automatic ⇒ every unit has exactly one ayah; `manual_grouped` ⇒ exactly one unit.
7. **Descriptions** — ≤10 per `(source, ayah)`, ≤2000 chars, non-blank, contiguous order.

**Phase B — inside the single write transaction.** Everything that reads mutable confirmed state
runs under the same transaction snapshot that may write, so there is **no gap** between
classification and write. READ COMMITTED alone would let two concurrent Confirms interleave, so
Phase B **serializes on the target Door row**:

1. **Idempotency** — existing `linking_operations.idempotency_key` → return its stored `outcome`,
   `200`, nothing written (only confirmations that wrote an operation row have one; a no-op never did).
2. **Door lock** — acquire a row-level write lock on the target `abwab_doors` row (`FOR UPDATE`,
   via the repository's EF equivalent), held until COMMIT/ROLLBACK; then verify the Door still
   exists and `deleted_at IS NULL`. Two Confirms for the same Door serialize here, and a
   concurrent Door archive/update cannot slip between classification and the Linking writes.
   Confirms against different Doors never contend; no broader locking architecture is introduced.
3. **Load** the Door's current live contributions and their children.
4. **Versions** — apply each submitted `existingContributionVersion` as
   `Entry(x).Property(x => x.Version).OriginalValue`.
5. **Re-classify** — run the **same pure classifier** as Phase 8 against the state just loaded
   (state in → classification out; the classifier itself performs no repository access).
6. **Token** — recompute the `preflightToken` from current state using the **same
   canonicalization function** Preflight used (Phase 8) and compare with the required
   supplied token; a mismatch → `409 PreflightStale` carrying the fresh classification, zero writes.
7. **Uniqueness / current state** — a `NEW_SOURCE` whose `(door_id, source_identity_hash)` already
   has a live contribution → `409`, zero writes; any `INVALID` → `400`, zero writes.

Then, still inside that **same transaction**:

```
if every source classifies UNCHANGED:
    COMMIT nothing (no operation row, no idempotency record);
    return 200 "لا توجد تغييرات جديدة لتنفيذها"                      (D5)

INSERT linking_operations (door, actor, idempotency_key)   -- early: operation_id now available
for each source in submitted order:
    UNCHANGED  → skip entirely (no row touched)
    NEW_SOURCE → INSERT contribution → units → unit_ayahs → words → descriptions
    UPDATE     → replace the already-loaded live contribution's children to exactly
                 the submitted state:
                    add new unit-ayahs, delete absent ones (hard delete, D4),
                    replace that ayah's words and descriptions wholesale;
                 stamp updated_at / updated_by; re-point operation_id to the new operation
finalize the operation exactly once — counts and the outcome snapshot carrying the
final contribution ids and applied classifications (the same logical result this
confirmation returns)                                        -- construction, not a later update
COMMIT                                                       -- the operation row is immutable forever after
```

**Replacement, never union (Locked §6).** For an updated source, the newly confirmed state *is* the
complete current state. Old words `[A,B]` + new words `[]` ⇒ no words. Old descriptions replaced by
new. An ayah absent from the new state is removed from **that** contribution and from nowhere else.

**Failure is all-or-nothing.** One rejected source fails the whole operation. No partial multi-source
write.

**Exception translation is mandatory** (F12): `23505` on the live-contribution index →
`LinkingDuplicateContributionException` → `409`; `DbUpdateConcurrencyException` →
`LinkingStaleVersionException` → `409`. An untranslated save becomes a `500` and is a defect.

**Attribution (Locked §20, F11):** every **audited** record stamps `created_by` / `updated_by` /
`deleted_by` from the resolved actor — the authored/lifecycle tables (workspace, workspace
sources, workspace and confirmed descriptions, source contributions) plus operations via
`actor_user_id` + `confirmed_at`. Leaf relational rows (units, unit-ayahs, unit-ayah words, and
the workspace manual-ayah/override/word children) inherit history from their parent aggregate and
carry no audit columns of their own. Linking is the first area in this repository to actually populate
these columns; note it in the README so it is not mistaken for an inconsistency with Abwab.

### Cache changes
**None.** Confirmation writes no Quran data, so no source-resolution entry is invalidated. Record this
in the README so nobody adds a pointless invalidating decorator by analogy with Abwab.

### Migration requirements
None — Phase 7 created the schema.

### Authorization implications
`[RequireOwner]`, re-checked in the handler.

### Concurrency implications
Per-contribution `xmin` on every update. A stale token is `409` with a reload-and-retry instruction,
matching every Abwab write's contract. Confirms serialize **per Door** on the `abwab_doors` row
lock (Phase B step 2), held until COMMIT/ROLLBACK; operations on different Doors run unimpeded.

### Acceptance criteria
- The locked example: confirming `الرحيم` (A, D, E) against a Door already holding `الرحمن` (A, B, C)
  leaves `الرحمن` **byte-identical** and adds `الرحيم` independently. Ayah A now has two contributions.
- Re-confirming an identical source writes nothing and returns the no-op success message.
- Confirming a changed source updates the existing contribution in place; its `id` is stable and its
  `xmin` advances. No delete-and-recreate.
- Replacement semantics verified for all three of ayahs, words, and descriptions.
- Replaying the same `idempotencyKey` returns the prior result and writes nothing.
- A `NEW_SOURCE` colliding with a live `(door, source_identity)` returns `409`, not a duplicate row.
- A stale `existingContributionVersion` returns `409`; nothing partially committed.
- Grouped manual `[[A,B]]` and automatic `[[A],[C]]` are stored as three units under two contributions
  and never collapse into `[[A,B,C]]`.
- One rejected source leaves the database untouched.

### Verification strategy
`dotnet build`; contract gate; a scripted manual sequence through Swagger against a local database
covering every acceptance row above, with `psql` inspection after each step; a deliberate concurrent
double-confirm to observe `409`; a deliberate mid-operation invalid source to prove all-or-nothing;
engineering review against `Writes/Abwab/README.md`'s conventions checklist (transaction, translation,
attribution, no cache decorator).

### Explicit out-of-scope
Door-links read model. Audit-event table. Any Frontend change. Restore/undelete endpoints (the schema
supports them; no route is added).

---

# PHASE 10 — Frontend: source-resolution adapter + Linking cache + canonical word identity

### Objective
Point the Frontend at the Backend resolution boundary, add a Linking-sized session cache, and collapse
the three word-identity classes to one.

### Dependencies
Phases 2–4.

### Exact areas / files

**Add**
- `features/linking/data-access/linking-source-resolution.api.ts`
- `features/linking/state/linking-source.cache.ts` (`extends ApiResponseCache`)

**Modify**
- `features/linking/data-access/linking-source-resolver.registry.ts` — one implementation for all six
  kinds
- `features/linking/models/linking-ayah.models.ts` — `canonicalQuranWordId` becomes non-nullable
- `features/linking/models/linking-merge.models.ts` — `LinkingWordContribution` collapses to the
  canonical arm
- `features/linking/utils/linking-source-intents.ts` — one branch
- `features/linking/utils/linking-merge.ts` — merge by canonical id; the positional/text alignment guard
  is deleted
- `features/linking/README.md`

**Delete**
- `features/linking/data-access/complete-paged-source.loader.ts`
- `features/linking/data-access/resolvers/{unique-word,root,lemma,stem,word-type,manual-mushaf-ayahs}-linking-source.resolver.ts`
- `features/linking/data-access/manual-mushaf-ayah.reader.ts` **and**
  `features/linking/utils/manual-mushaf-ayah-completeness.ts` — superseded by Phase 3

**Careful:** `ManualMushafSelectionStore.readMetadata` currently uses `ManualMushafAyahReader` to
validate a selected verse before handoff. Repoint it at a light validation (the resolve endpoint, or
the existing ayah-study read) rather than deleting its behaviour — the reader-mode "can this verse be
added" gate must survive.

### Cache changes
`LinkingSourceCache` keyed `linking:source:{sourceIdentity}`. **Cap ≈6 entries**, not the
`ApiResponseCache` default of 48: 48 complete sources would be tens of megabytes in the heap. Override
the cap explicitly and comment why.

Keep `MushafReaderCache` for ordinary reader use; it is no longer on the Linking resolution path.

### Frontend adapter changes
`LinkingSourceResolver.resolve(source, onProgress)` keeps its signature, so
`LinkingSourceEditorFacade` and `LinkingSourceSetCoordinator` are untouched. `onProgress` becomes a
single `0 → total` tick (or is dropped and its call sites simplified) — decide once and apply
consistently.

### Migration / authorization / concurrency
None. Note that resolution now requires an authenticated Owner, so the Linking access gate must already
be satisfied before a resolve is attempted — it always is, since every entry point is Owner-gated.

### Acceptance criteria
- Opening a source's ayah editor issues **one** request instead of `ceil(total/100)`.
- Reopening the same source in the same session issues **zero** requests.
- `presentation-occurrence` and `manual-word-location` no longer exist anywhere in the codebase.
- The merge no longer aligns words positionally; a mixed manual+automatic operation over a shared ayah
  merges by canonical id.
- Manual sources resolve through the same path as automatic ones.
- `check:no-unit-specs`, `typecheck:app`, `build:verify` all pass.

### Verification strategy
The three frontend commands in order; browser DevTools network inspection for the request-count claims;
manual exercise of a small, a medium, and a 2,000-ayah source; manual exercise of a manual Mushaf
source end-to-end from reader selection to editor; engineering review.

### Explicit out-of-scope
Workspace persistence (Phase 11). Confirm (Phase 12). Virtualization (Phase 13).

---

# PHASE 11 — Frontend: workspace persistence adapter

### Objective
Replace localStorage with the Backend workspace, without touching `LinkingWorkspaceStore`'s public
surface or any component.

### Dependencies
Phases 5–6, Phase 10.

### Exact areas / files
- **Add** `features/linking/data-access/http-linking-workspace.repository.ts`
- **Modify** `features/linking/state/linking-workspace.store.ts` — inject the port, not the
  localStorage adapter; manual word selection now saves canonical `quranWordId`
- **Modify** `features/linking/state/linking-manual-word-editor.facade.ts` — the draft becomes
  `quranWordId`-based; `wordLocation` is used only as the click coordinate and resolved through the
  resolved source
- **Modify** `features/linking/models/linking-workspace.models.ts`,
  `features/linking/models/linking-manual-mushaf.models.ts`
- **Delete** `local-storage-linking-workspace.repository.ts`;
  keep or delete `linking-workspace.codec.ts` (recommend: **delete** — the server now owns validity and
  a second decoder is a divergence risk)
- `features/linking/README.md`

### Concurrency
Surface the `409` stale-token path as a real UX state: reload the workspace and tell the user, rather
than silently overwriting. The store already has a persistence-warning signal to reuse.

### Migration of existing local data
**None.** Per the V2 precedent (`qd-linking-workspace-v1` was never migrated), the V2 localStorage
bucket is **not** migrated to the server. It is a prototype artifact. On first load after cutover the
server workspace is empty and the user re-prepares their sources. Recommend clearing the old key on
first successful server hydration so a stale bucket cannot resurface.

### Acceptance criteria
- Prepared sources survive logout/login **and a different browser** — the localStorage-era limitation
  is gone.
- Actor B never sees actor A's workspace.
- Checked sources, surface, focus, search, viewport, review position, and selected Door remain
  client-side and reset as before.
- No component or editor file changed — only the store, the two facades, the models, and the new
  adapter.
- A stale-token `409` produces a visible, recoverable state.

### Verification strategy
Frontend three-command gate; two-browser manual check for cross-device persistence; two-tab manual
check for the stale-token path; engineering review confirming the store's public API is unchanged.

### Explicit out-of-scope
Confirm. Descriptions UI. Virtualization.

---

# PHASE 12 — Frontend: preflight + confirm cutover

### Objective
Insert the Preflight stage into the flow and replace the mock with the real command.

### Dependencies
Phases 8–9, Phase 11.

### Exact areas / files
- **Add** `features/linking/data-access/http-linking-command.port.ts`,
  `features/linking/data-access/linking-preflight.api.ts`,
  `features/linking/components/linking-preflight-step/` (component + template + styles),
  `features/linking/models/linking-preflight.models.ts`
- **Modify** `features/linking/state/linking-workflow.facade.ts` — the step union gains `preflight`
  between `door` and `review`; `LINKING_COMMAND_PORT` provider swaps to the HTTP adapter
- **Modify** `features/linking/components/direct-link-workflow/` — render the new step
- **Modify** `features/linking/models/linking.labels.ts` — Arabic copy for the six classifications,
  the no-op message, and the stale-preflight message
- **Delete** `features/linking/data-access/mock-linking-command.port.ts`
- `features/linking/README.md`

### Flow
`configure-source → resolve → door → **preflight** → review → confirm → success`.

Preflight runs after the Door is chosen (it is Door-relative) and before review. Review then shows the
merged display **plus** the preflight classification per source and per ayah.

### UX requirements from the locked scope
- Per source: requested / new / overlapping / unchanged / updated / removed / invalid counts.
- The overlapping ayahs must be **individually inspectable**, naming the other sources.
- A wholly-unchanged operation shows `لا توجد تغييرات جديدة لتنفيذها` as a **success**, not an error,
  and the flow completes.
- A blocking `INVALID` disables Confirm and explains why, per item.
- `409 PreflightStale` re-runs preflight and shows the fresh classification rather than failing.

### Concurrency
Carry `preflightToken` and each `existingContributionId`/`existingContributionVersion` from the
preflight response into the confirm request — the token is **required** by Confirm (a missing
token is a controlled `400`), so the flow cannot skip the Preflight stage. Generate one
`idempotencyKey` per confirm attempt and
**reuse it across retries** of the same attempt.

### Acceptance criteria
- No mock remains; `LINKING_COMMAND_PORT` resolves to the HTTP adapter.
- A confirmed operation is visible in the database with the expected contributions, units, ayahs,
  words, and descriptions.
- The success message reflects reality — a real link, not `نتيجة نموذج أولي`.
- Every locked classification renders with its exact affected ayahs.
- Retrying a failed submit with the same idempotency key does not double-write.

### Verification strategy
Frontend three-command gate plus `check:golden-ui` (new template); full manual walkthrough of the
locked scenarios against a local database with `psql` verification after each confirm; deliberate
stale-preflight test by mutating the Door's contributions in a second tab; engineering review.

### Explicit out-of-scope
Descriptions UI and virtualization (Phase 13). Door-links display.

---

# PHASE 13 — Frontend: CDK virtualized source list + descriptions UI + merged provenance

### Objective
Close the three known Frontend gaps together, because all three reshape the same per-ayah row.

### Dependencies
Phases 10–12.

### Exact areas / files
- **Modify** `features/linking/components/linking-ayah-selection/` — replace the `<ul>` + pagination
  with `<cdk-virtual-scroll-viewport>`; the viewport becomes **that surface's single vertical scroll
  owner** (the editor's own `overflow: auto` must yield, not nest)
- **Modify** `features/linking/state/linking-source-editor.facade.ts` — delete `EDITOR_PAGE_SIZE`,
  `page`, `setPage`, `pageCount`, `visibleAyahs`; expose `filteredAyahs`
- **Modify** `features/linking/models/linking-workflow.models.ts` — `page` leaves
  `LinkingSourceEditorState`
- **Modify** `features/linking/components/linking-source-ayah-editor/` — descriptions editor per ayah
  (add / edit / reorder / remove, ≤10, ≤2000 chars, plain text)
- **Modify** `features/linking/models/linking-workspace.models.ts` — descriptions on the configuration
- **Modify** `features/linking/components/linking-ayah-card/` + `direct-link-workflow` review — render
  the already-computed `MergedAyahSelection.words` union and its `sourceKeys` provenance instead of the
  first contributor's flags (gap **G4**)
- `features/linking/README.md`

### Notes the executor will need
- `@angular/cdk ^20.2.14` is already a dependency, and `ScrollingModule` is already used by
  `shared/ui/data-table` — this is not a new dependency.
- Quran text wraps, so rows have **variable height**: use `autosize` or a measured row, not a fixed
  `itemSize`.
- Quran glyph/text rendering is protected — `FRONTEND_UI_RULES.md` and the Golden UI contract apply;
  run `check:golden-ui` before `build:verify`.
- Selection, search, Select All, and Clear All already operate on the complete universe; only rendering
  changes.

### Acceptance criteria
- A 2,000-ayah source scrolls as one continuous list with **no user-facing pagination**, and any ayah
  can be excluded freely.
- Exactly one vertical scroll owner on the editor surface.
- Descriptions round-trip to the Backend with the limits enforced client-side too.
- Two sources contributing the same ayah show separate description lists.
- A merged ayah shows the union of highlighted words with provenance naming every contributing source.
- `check:golden-ui`, `check:no-unit-specs`, `typecheck:app`, `build:verify` all pass.

### Verification strategy
The four frontend commands in order; browser verification at Wide / Medium / Compact for scroll
ownership, keyboard reachability, and glyph/metric integrity; manual scroll of a 2,000-ayah source
watching DOM node count; engineering review plus a UI review against the Golden UI contract.

### Explicit out-of-scope
Door-links display. Review-step virtualization (still paged at 12 — deliberately not part of the
locked direction).

---

# PHASE 14 — Integration hardening, current-truth docs, manual acceptance

### Objective
Close the loop: documentation reflects reality, gates pass, and the runtime matrix is executed.

### Dependencies
All prior phases.

### Exact areas / files
- `Backend/README.md` §Current scope — Linking added
- `Backend/infrastructure/.../Persistence/Reads/Linking/README.md`,
  `Persistence/Writes/Linking/README.md`, `Caching/Linking/README.md` — final current truth
- `Backend/api/QuranDashboard.Api/README.md` and `Controllers/README.md` — the four Linking routes and
  their status mapping
- `Frontend/quran-dashboard-ui/src/app/features/linking/README.md` — full rewrite to the post-cutover
  truth
- `Frontend/.../features/words/README.md`, `features/mushaf/README.md`, `core/README.md` — only where
  their described truth changed
- `docs/contracts/README.md` — pointer entry if the index warrants one
- `CLAUDE.md` §Active Spec Kit Feature — records `001-abwab-linking-backend`; this work is driven
  through the Spec Kit artifacts in `specs/001-abwab-linking-backend/`. The section is cleared
  back to `None` in the same deletion commit that removes the feature's planning artifacts, per
  `docs/README.md` §Lifecycle

### Hardening checklist
- Confirm the shared `IMemoryCache` still has no `SizeLimit` and no existing `Set` was modified.
- Confirm `AbwabPermissionCatalogue` is untouched and still 19 codes.
- Confirm no Linking route carries anything other than exactly one `[RequireOwner]`.
- Confirm every Linking writer save translates its exceptions.
- Confirm no Linking cache key contains a user, Door, or configuration value.
- Confirm `check-pending-model` reports no drift and the smoke dump manifest matches the head
  migration.
- Confirm the automated test estate is byte-identical to its pre-plan state
  (`git diff --stat -- Backend/tests Frontend/quran-dashboard-ui/e2e` is empty).

### Acceptance criteria
The Phase 14 matrix below passes.

### Verification strategy
Backend `dotnet build`; `Backend/scripts/check-api-contract`; the frontend four-command gate; the
runtime/manual matrix; a formal engineering review of the whole feature.

---

# FINAL SECTIONS

## 1. Locked decisions carried into implementation

| # | Decision |
| --- | --- |
| 1 | **Ayah linking only.** No Surah linking table, command, or abstraction. |
| 2 | **Frontend V2 is the product reference.** The Backend supports the proven shape; the workflow is not redesigned. |
| 3 | **Six typed source families** with source-specific scope. No ambiguous generic string. |
| 4 | **One live contribution per `Door + SourceIdentity`.** Re-linking is `UNCHANGED` or `UPDATE`, never a duplicate. |
| 5 | **The same ayah may reach a Door from multiple sources**, stored independently; neither replaces the other. |
| 6 | **Update = replacement, not union** (see §6 below). |
| 7 | **Preflight is mandatory** before Confirm: `Resolve → Preflight → Review → Confirm`. |
| 8 | **Preflight returns exact affected ayahs**, with counts alongside items — never counts alone. |
| 9 | **An unchanged source does not stop the operation**; an all-unchanged operation is a controlled success. |
| 10 | **Complete source resolution** — no Linking-side walking of explorer pages; explorer pagination unchanged. |
| 11 | **Source-result caching is mandatory**, with the three concerns kept separate. |
| 12 | **Bounded expiration + restart** as invalidation. No `quran_data_generation`. Both sliding and absolute bounds. |
| 13 | **`quran_words.id` is the only durable word identity.** No render positions, indexes, or text. |
| 14 | **Manual Mushaf uses the same resolution boundary and trust model.** |
| 15 | **Descriptions: max 10 per `(Source, Ayah)`, max 2000 chars, plain text, ordered.** |
| 16 | **Words and descriptions belong to the source contribution**, not the ayah. |
| 17 | **Automatic ⇒ independent units; manual ⇒ grouped or independent.** Never inferred from merged display. |
| 18 | **`xmin` optimistic concurrency**, consistent with Abwab. |
| 19 | **In-place update** of the live contribution. No delete-and-recreate as the normal edit path. |
| 20 | **Audit Stage 1 only** — attribution columns, timestamps, confirmed-side soft delete, restore-compatible state. |
| 21 | **Owner-only** via `[RequireOwner]`. The 19-code Abwab catalogue is untouched. |
| 22 | **Backend workspace persistence** replaces localStorage; transient UI state stays client-side. |
| 23 | **Workspace and Confirmed state are separate table families.** No generic JSON blob for relational truth. |
| 24 | **Typed descriptor persistence**; confirmed links materialize ayahs/words; the descriptor becomes historical provenance. |
| 25 | **Two idempotency concepts**: the confirmation request key, and `Door + SourceIdentity` uniqueness. |
| 26 | **The preflight token is required by Confirm, yet advisory in trust**: it proves the workflow passed through Preflight (missing ⇒ controlled `400`) and is never write authority — Confirm re-checks everything inside the transaction. |
| 27 | **Confirm is one atomic command**; failure is all-or-nothing. |
| 28 | **Preflight performs no writes.** |
| 29 | **One descriptor-body resolution contract** (POST). Explorer APIs unchanged. |
| 30 | **Door-links GET remains deferred.** |

## 2. Database tables and migration sequence

**M1 — `AddLinkingWorkspace`** (Phase 5)
`linking_workspaces`, `linking_workspace_sources`, `linking_workspace_source_manual_ayahs`,
`linking_workspace_source_ayah_overrides`, `linking_workspace_source_words`

**M2 — `AddLinkingWorkspaceDescriptions`** (Phase 6)
`linking_workspace_source_descriptions`

**M3 — `AddLinkingConfirmedState`** (Phase 7)
`linking_operations`, `linking_source_contributions`, `linking_units`, `linking_unit_ayahs`,
`linking_unit_ayah_words`, `linking_unit_ayah_descriptions`

**Every migration:** created with `Backend/scripts/add-mig <Name>` (EF tooling only — never
hand-written); followed by `check-pending-model`; followed by a local
`Backend/scripts/create-smoke-dump` regeneration because `SmokeDumpGate` pins the head migration id
(F2); reported with its name, generated files, build status, and whether `update-db` ran.

**Ownership:** six workspace tables (mutable, per-user, hard-delete) and six confirmed tables (durable,
per-Door, soft-delete at the contribution boundary). They are never merged, and no status column
unifies them.

## 3. API / boundary sequence

| Phase | Boundary |
| --- | --- |
| 2–3 | `POST /api/linking/sources/resolve` — complete source resolution, read-only |
| 5 | `GET/POST/PUT/DELETE /api/linking/workspace[...]` — per-user workspace persistence |
| 8 | `POST /api/linking/operations/preflight` — classification, read-only |
| 9 | `POST /api/linking/operations` — atomic confirmation / update |

All `[RequireOwner]`. All use the `ApiResponse<T>` envelope with Arabic messages. Every contract change
regenerates and commits `openapi/swagger.json` and `src/app/core/api/generated/models/**` (F1).

**Not built:** the Door-links read, any approval/request workflow, any notification, any bulk edit of
confirmed links, any restore endpoint.

## 4. Cache architecture and invalidation policy

| Layer | What | Key | Lifetime | Invalidation |
| --- | --- | --- | --- | --- |
| **Backend source-truth cache** | complete resolved source, **compact** (ayah ids + word id arrays) | typed descriptor **only** — never user / Door / inclusion / words / descriptions / checked state | 30 min sliding **and** 4 h absolute | none write-driven; restart clears; no `quran_data_generation` |
| **Backend ayah-text cache** | Uthmani text + display metadata, keyed by ayah id | ayah id | same policy | same |
| **Frontend session cache** | the resolved-source response | `linking:source:{sourceIdentity}` | session; ~6-entry cap | page reload |
| **Workspace persistence** | per-user prepared workspace | — | durable | **not a cache** |

Mechanics: a **dedicated** `MemoryCache` instance with `SizeLimit` expressed in ayahs (never the shared
size-less singleton, F7); `Task<T>`-in-entry single-flight (never `CacheLoadGate`, F8); faulted tasks
are evicted so the next caller retries. No Redis. No ETag (D2).

## 5. Preflight classification contract

**Source level:** `NEW_SOURCE` · `UNCHANGED` · `UPDATE` · `INVALID`

**Ayah level** (mutually exclusive; counts partition the requested set):

| Value | Meaning | Blocking |
| --- | --- | --- |
| **`NEW_SOURCE`** | (source level) no live contribution exists for this `Door + SourceIdentity`; one will be created | no |
| **`NEW_AYAH`** | not contributed by this source, and not present in this Door via any other source | no |
| **`OVERLAP_OTHER_SOURCE`** | would be newly added for this source, and already exists in this Door via ≥1 other source. Not a conflict, not skipped — the new contribution is still added independently | no |
| **`UNCHANGED`** | present for this source with identical words, descriptions, and grouping; no write is performed | no |
| **`UPDATE`** | present for this source, but ayah membership, selected words, descriptions, or source-owned confirmed configuration changed | no |
| **`REMOVE`** | present in the current confirmed contribution, absent from the newly confirmed complete source state; removed from **this** contribution only, never from other sources | no |
| **`INVALID`** | Door / source / ayah / word / grouping data is no longer valid | **yes** |

Precedence: a source-owned change wins — `UPDATE` and `REMOVE` keep their classification even when the
ayah overlaps another source; `OVERLAP_OTHER_SOURCE` applies only where the item would otherwise be
`NEW_AYAH`. Every item carries structured `overlappingSources[]` (`sourceIdentity`, `label`,
`sourceKind`) regardless, so the UI names the other sources in human-readable form.

Every item also carries exact `wordChanges` and `descriptionChanges` diffs. Counts accompany items;
they never replace them. Preflight performs no writes.

## 6. Update / replace semantics

**The newly confirmed state replaces the previous state for that source. It is never automatically
unioned.**

```
Old:  Ayah A → words [الرحمن]
New:  Ayah A → words [الرحيم]
Final: [الرحيم]            NOT [الرحمن, الرحيم]

Old:  words [A, B]
New:  words []
Final: no words
```

Descriptions follow the identical rule. An ayah present in the old confirmed state and absent from the
new one is `REMOVE`d **from that contribution only** and does not affect the same ayah under any other
source. Removed children are hard-deleted (D4); soft delete lives at the contribution boundary so
Restore can bring back a whole contribution.

## 7. Grouped / overlap semantics

`linking_units` is a real table between the contribution and the ayah. Automatic contributions emit one
single-ayah unit per ayah; a `manual_grouped` contribution emits exactly one multi-ayah unit.

```
Manual grouped {A,B} → contribution#1 → unit#1 { A, B }
Lemma automatic {A,C} → contribution#2 → unit#2 { A }, unit#3 { C }

Durable intent: [[A,B]] and [[A],[C]] — never [[A,B,C]], and A is not lost.
```

Overlap is preserved by the grain: `UNIQUE (source_contribution_id, ayah_id)` prevents an intra-source
duplicate while allowing the same ayah under two different contributions. Grouping is never inferred
from a merged display or from a row count.

## 8. Manual word canonicalization

`quran_words.id` only. Automatic families become canonical by projecting the `QuranWordId` that
`AyahWordHydration` already loads (F4 — zero extra queries). Manual `wordLocation` is a click coordinate
resolved through the resolved source and the UNIQUE `quran_words.location` index (F6); the workspace
stores canonical ids from the moment the user saves. **Only manual Mushaf sources accept
user-authored word ids**; every submitted id is validated as **existing**, **non-marker**, and
**belonging to the declared ayah** (whose ayah is in the source's manual verse set) — and an ayah
with zero selected words is valid. Automatic families never accept authored ids: their word
contributions derive from `automatic_word_matches_enabled` (on ⇒ the resolution's matched words;
off ⇒ none). `renderPosition`, array index, `wordNumber`, `lineWordOrder`, and Quran text are
rejected as identity at the API contract.

## 9. Description semantics

- **Max 10** per `(Source, Ayah)` — enforced in the writer **and** by a `CHECK (order_value BETWEEN 1
  AND 10)` on both the workspace and confirmed tables.
- **Max 2000 characters** — `varchar(2000)` plus `CHECK (btrim(body) <> '')`.
- **Plain text.** No markup, no HTML; there is no sanitization or rich-text pipeline in this system.
- **Ordered** by `order_value`, resequenced `1..N` on every mutation, independently editable,
  removable, and reorderable.
- **Never merged across sources** — structurally impossible, since a description's parent is
  `(source contribution, ayah)`.
- The limits live once in `LinkingLimits` and are referenced by workspace persistence, preflight,
  confirm, and the database, so the four cannot drift.

## 10. Authorization scope

**Owner-only.** Every Linking route carries exactly one `[RequireOwner]`. No `linking.*` permission code
is introduced; `AbwabPermissionCatalogue` remains exactly 19 codes across 5 groups, and neither
`PermissionAuthorizationHandler` nor `UnsafeEndpointMetadataValidator` is modified. Ownership of a
workspace is derived from `AuthorizationState.UserId` (F10) and never from a request field. Confirmation
re-checks live access rather than trusting that a workspace row exists.

## 11. Audit scope

**Stage 1 only.** `created_by` / `updated_by` / `deleted_by` and timestamps populated on the
audited authored/lifecycle tables — workspace, workspace sources, workspace and confirmed
descriptions, source contributions — plus operations via `actor_user_id` + `confirmed_at` (the
first area in this repository to actually populate attribution, F11); leaf relational rows inherit
history from their parent aggregate;
soft delete at the `linking_source_contributions` boundary; state shaped so a future restore can bring
back a whole contribution the way `abwab.doors.restore` does. **No `linking_audit_events` table, no
append-only event model, no parallel audit architecture.**

## 12. Deferred work

Explicitly out of scope for this plan:

1. **`GET existing links for a Door`** — API and read model. The Frontend presentation is designed
   first; only then is the read model decided.
2. **Full Linking audit-history system** — the Stage-2 append-only `linking_audit_events` model.
3. **Linking permission family** (`linking.*`) and the combined-catalogue seam it requires.
4. **Redis / distributed cache** — blocked upstream anyway by the single-instance Abwab cache
   generation.
5. **`quran_data_generation` marker** — bounded expiration + restart is the agreed strategy.
6. **Surah linking** — the entire feature is ayah-based.
7. **ETag / conditional requests** for source resolution (D2).
8. **Restore endpoints** — the schema supports un-deleting a contribution; no route is built.
9. **Review-step virtualization** — the review list stays client-paged at 12.

## 13. Phase dependency graph

```
                    ┌──────────────────────────────────────────┐
                    │ 1  Contracts + SourceIdentity            │
                    └───────┬──────────────────────┬───────────┘
                            │                      │
            ┌───────────────▼──────────┐   ┌───────▼──────────────────┐
            │ 2  Resolution (auto)     │   │ 7  Confirmed schema (M3) │
            └───────────────┬──────────┘   └───────┬──────────────────┘
                            │                      │
            ┌───────────────▼──────────┐           │
            │ 3  Resolution (manual)   │           │
            └───────────────┬──────────┘           │
                            │                      │
            ┌───────────────▼──────────┐           │
            │ 4  Backend source cache  │           │
            └───────┬───────────┬──────┘           │
                    │           │                  │
        ┌───────────▼──┐   ┌────▼──────────────────▼───┐
        │ 5  Workspace │   │ 8  Preflight engine       │
        │    (M1)      │   └────┬──────────────────────┘
        └───────┬──────┘        │
                │               │
        ┌───────▼──────┐   ┌────▼──────────────────────┐
        │ 6  Descrip-  │   │ 9  Confirm engine         │
        │    tions(M2) │   └────┬──────────────────────┘
        └───────┬──────┘        │
                │               │
        ────────┴───────────────┴────────  BACKEND COMPLETE  ────────
                            │
            ┌───────────────▼──────────┐
            │ 10 FE resolution + cache │   (needs 2,3,4)
            └───────────────┬──────────┘
                            │
            ┌───────────────▼──────────┐
            │ 11 FE workspace adapter  │   (needs 5,6,10)
            └───────────────┬──────────┘
                            │
            ┌───────────────▼──────────┐
            │ 12 FE preflight + confirm│   (needs 8,9,11)
            └───────────────┬──────────┘
                            │
            ┌───────────────▼──────────┐
            │ 13 FE CDK + descriptions │
            │    + merged provenance   │
            └───────────────┬──────────┘
                            │
            ┌───────────────▼──────────┐
            │ 14 Hardening + docs      │
            └──────────────────────────┘
```

**Parallelizable:** Phase 7 can run alongside Phases 2–4 (it depends only on Phase 1). Phases 5–6 and
Phase 8 are independent of each other. Everything from Phase 10 onward is strictly sequential.

**Releasability:** Phases 1–9 ship no user-visible change and can each be reviewed and merged alone.
Phases 10–13 are a coordinated cutover — do not release Phase 12 without Phase 13, or the user gets a
real write behind a paginated editor with no descriptions UI.

## 14. Final runtime / manual acceptance matrix

Executed in Phase 14 against a local database. No automated test is added for any row.

| # | Scenario | Expected |
| --- | --- | --- |
| A1 | Resolve a ~10-ayah Root | complete set, one request, canonical word ids present |
| A2 | Resolve a ~200-ayah Lemma | complete set, one request |
| A3 | Resolve a ~2,000-ayah Root | complete set, one request; record payload size and wall time |
| A4 | Re-resolve A3 immediately | zero SQL commands (backend cache), zero network (frontend cache) |
| A5 | Resolve a Word Type scope, then change one scope field | two distinct cache entries, no cross-serve |
| A6 | Resolve a manual Mushaf source incl. a page-spanning ayah | complete ordered word list, canonical ids |
| A7 | Resolve beyond `MaxResolvedAyahs` | controlled `400`, no unbounded body |
| B1 | Prepare 3 sources, configure each, reorder, reload | fully preserved |
| B2 | Re-add an equivalent descriptor | label refreshed; order and configuration preserved |
| B3 | Log out, log in on a different browser | workspace present (localStorage-era limitation gone) |
| B4 | Actor B loads their workspace | actor A's rows never visible |
| B5 | Two tabs edit the same source | second save `409`, recoverable, no silent overwrite |
| B6 | Add an 11th description / a 2001-char body | refused by writer and by DB constraint |
| C1 | Preflight the locked example: Door has `الرحمن`(A,B,C); confirm `الرحيم`(A,D,E) | `NEW_SOURCE`; A = `OVERLAP_OTHER_SOURCE`; D,E = `NEW_AYAH`; the overlap item is inspectable |
| C2 | Preflight `Root X` (identical) + `Lemma Z` (new) | `UNCHANGED` + `NEW_SOURCE`; not a no-op; operation continues |
| C3 | Preflight with every source identical | `isNoOp = true`, not an error |
| C4 | Preflight after removing an ayah from a source | that ayah `REMOVE`, other sources untouched |
| C5 | Preflight with an archived Door / marker word / foreign ayah | `INVALID`, `isBlocked` |
| C6 | Row counts before/after any preflight | identical — preflight writes nothing |
| D1 | Confirm C1 | `الرحمن` byte-identical; `الرحيم` added; ayah A has two contributions |
| D2 | Confirm an identical source again | nothing written; `لا توجد تغييرات جديدة لتنفيذها` |
| D3 | Confirm a changed source | contribution `id` stable, `xmin` advanced, no delete-and-recreate |
| D4 | Old words `[A,B]` → new `[]` | words removed; **not** unioned |
| D5 | Replay the same `idempotencyKey` | prior result returned, nothing written |
| D6 | Two concurrent confirms of the same new source | one succeeds, one `409`; no duplicate live contribution |
| D7 | Confirm with a stale contribution version | `409`; nothing partially committed |
| D8 | Confirm manual grouped `[[A,B]]` + automatic `[[A],[C]]` | three units, two contributions; never `[[A,B,C]]` |
| D9 | Confirm where one source is invalid | whole operation rejected; database untouched |
| E1 | Open a 2,000-ayah source in the editor | one continuous virtualized list, no pagination control, DOM node count bounded |
| E2 | Exclude an ayah near the end of that list | selection retained while scrolling away and back |
| E3 | Editor surface scroll ownership at Wide / Medium / Compact | exactly one vertical scroller |
| E4 | Two sources contributing one ayah, each with descriptions | separate lists, never merged |
| E5 | A merged ayah matched by two sources | union of highlighted words, provenance names both sources |
| E6 | Quran glyphs, spacing, line metrics in the reader and the editor | unchanged; `check:golden-ui` passes |
| F1 | `Backend/scripts/check-api-contract` | passes; swagger + generated models committed |
| F2 | `Backend/scripts/check-pending-model` | no pending model changes |
| F3 | `npm run check:no-unit-specs && npm run typecheck:app && npm run build:verify` | all pass, run independently in order |
| F4 | `git diff --stat -- Backend/tests Frontend/quran-dashboard-ui/e2e` | empty — the frozen test estate is untouched |

---

*End of plan. No implementation, migration, API, test, Frontend change, or commit is included by
design. The Door-links read model remains deferred (§12).*
