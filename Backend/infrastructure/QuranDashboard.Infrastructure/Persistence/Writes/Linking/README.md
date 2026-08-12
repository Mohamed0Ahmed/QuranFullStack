# Linking write path

**Layer:** Infrastructure · write seam · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`,
`API_GUIDELINES.md`

## What this area does

`EfLinkingWorkspaceWriter` is the only writer here. It backs the five mutating
`/api/linking/workspace` routes (`api/QuranDashboard.Api/Controllers/Linking/LinkingWorkspaceController.cs`)
and implements `ILinkingWorkspaceWriter`. The read sibling is `../../Reads/Linking/`; the shared
descriptor codec and DTO projection both live in `../../Linking/`. The precedent this folder follows
is `../Abwab/README.md` — read it first; only the deltas are recorded here.

- `EfLinkingWorkspaceWriter.cs` — add / remove / reorder / clear, plus the shared helpers.
- `EfLinkingWorkspaceWriter.Configuration.cs` — the wholesale per-source configuration replacement and
  every validation it owns. Split by use case, not by size.

## The workspace is NOT a cache

Nothing in this folder is cache maintenance. `Infrastructure/Caching/Linking/` caches **source
resolution** — the ayah set a descriptor resolves to, keyed by the descriptor alone, with no user in the
key. This folder stores **durable user preparation state**. The two never meet: no workspace write
invalidates a cache generation, and no cached value is ever derived from a workspace row. There is
deliberately **no invalidating decorator** on `ILinkingWorkspaceWriter` — the one place this area departs
from `../Abwab/README.md`, where every writer interface is DI-wrapped. Adding one here would be wrong,
not merely unnecessary: the resolution cache holds nothing keyed by user or workspace, so there is no
generation a workspace write could correctly bump. The workspace is also not a UI-state channel — checked
sources, active surface, search text, scroll and review position, and the selected Door all stay
client-side (spec FR-028).

## Attribution is populated here for the first time

No Abwab writer assigns `created_by` / `updated_by`; those columns are always NULL there (repo fact F11),
so there was no helper to copy. Linking is the first area that actually populates them, and the actor is
always `AuthorizationState.UserId` resolved through `IAuthorizationStateResolver` (F10) and passed in as
`userId` — never `ICurrentUser` (which exposes only the Logto `sub`), and never a request field.

The columns are mapped **`NOT NULL`**, a deliberate strengthening of `data-model.md`, which specifies the
audit columns without stating nullability. Because every write path here stamps them, a missing stamp is a
defect, and a `NOT NULL` column fails that defect loudly at the INSERT instead of leaving a silent NULL
that looks exactly like Abwab's unpopulated columns. Attribution lives only on the two audited aggregate
tables — `linking_workspaces` and `linking_workspace_sources`. The three child tables (manual ayahs,
overrides, words) carry no audit columns and inherit ownership from their parent source (research R12);
do not "complete" them.

## Reconciled artifact errors — verified against the live database

Two errors in `specs/001-abwab-linking-backend/data-model.md` were resolved against the running schema
before M1 was generated. Both are applied in `../../Configurations/Linking/`; do not restore the
artifact's wording.

- **The attribution FK target table is `users`, not `access_users`.** `data-model.md` (and `research.md`
  R12, and the docs plan) name `access_users` throughout. No such relation exists — the name is an
  artifact of the `db.AccessUsers` **DbSet**, whose entity `User` is mapped
  `builder.ToTable("users")` in `../../Configurations/Access/UserConfiguration.cs`. Every FK here targets
  `users`. Design intent is unchanged; only the name was wrong.
- **Attribution and reference FK columns are `integer`, not `bigint`.** `data-model.md` specifies `bigint`
  FK columns, but every referenced primary key is `integer` — `users.id`, `quran_ayahs.id`,
  `quran_words.id`, and the morphology/display dimension tables — and `AuthorizationState.UserId` is
  `int`. A FK must match the width of the key it references or the index on the referenced side stops
  being usable for the join. So `user_id`, `created_by`, `updated_by`, `ayah_id`, `quran_word_id`, and the
  six dimension references are all `integer`. The linking tables' **own surrogate primary keys stay
  `bigint`**, exactly as `data-model.md` specifies.

## Conventions and invariants (read before changing)

- **Every save translates** (F12, research R13), through `SaveTranslatingWriteExceptionsAsync`:
  `DbUpdateConcurrencyException` → `LinkingStaleVersionException` (`409`), Postgres `23505` →
  `LinkingDuplicateContributionException` (`409`), Postgres `23503` → `LinkingWorkspaceViolationException`
  with `ReferenceUnknown` (`400`). There is one helper and every save in both files goes
  through it — unlike `../Abwab/`, which needs three variants because its writes differ in which
  violations are reachable. An untranslated save reaches the global handler as a `500` where the contract
  says `409`. Both entities written here are mapped `IsRowVersion()`, so `xmin` is in every UPDATE's WHERE
  clause whether or not the writer pinned `OriginalValue` — what decides that a save needs translation is
  what EF can raise, not what the client sent.
  - **One honest imprecision.** Two concurrent *first* mutations by the same user race on
    `UNIQUE (user_id)`; the loser's `23505` is translated to `LinkingDuplicateContributionException`, so it
    is answered "this source is already added" when the accurate answer is "reload, your workspace now
    exists". Both are `409` and both instruct the same recovery — reload and re-present — so the status
    contract holds and only the sentence is imprecise. Discriminating on
    `PostgresException.ConstraintName` is the fix if that sentence ever matters.
- **Every reference the request carries is proved to exist BEFORE the insert, and the FK is only the
  backstop.** `EnsureDimensionReferencesExistAsync` runs first thing in `AddSourceAsync`, before the
  transaction opens, and checks each non-null column of the encoded `LinkingSourceStorageForm` against its
  target table — `root_id`, `lemma_id`, `stem_id`, `unique_simple_word_id`, `unique_tashkeel_word_id`,
  `word_type_tashkeel_word_id`. Driving it off the **form** rather than the descriptor is what makes the
  coverage total: those six columns are exactly the six RESTRICT dimension FKs, so every family — root,
  lemma, stem, both unique-word modes, all three Word Type dimension arms, and the Word Type Word arm — is
  covered by construction, and a new family cannot be added without either reusing a checked column or
  adding one. The reported field is the **wire** field (`rootId`, `wordId`, `selection.rootId`,
  `selection.tashkeelWordId`), prefixed `selection.` when the kind is Word Type, so the Arabic message names
  the field the client actually sent. This mirrors `EnsureAyahsExistAsync` on the configuration path; before
  it existed, a non-existent dimension id reached the INSERT, raised `23503`, escaped untranslated, and
  answered `500` where the contract says `400`.
  - **It is `400`, not `404` — deliberately, and it differs from `POST /api/linking/sources/resolve` on
    purpose.** The resolve route answers `404` (`LinkingSourceNotFoundException`) because there the
    descriptor **is** the addressed resource: you are fetching that source, and an unknown dimension means
    the thing you addressed does not exist. On `POST /api/linking/workspace/sources` the addressed resource
    is the caller's own source collection, which exists; the descriptor is a **field of a create request**,
    so an unresolvable id is a validation failure of the body. `contracts/linking-workspace-api.md` is not
    silent on this: its status table reserves `404` for "source id not found *in the caller's own
    workspace*" and assigns `400` to "validation failure (any rule above)", where the rules explicitly
    include "descriptor valid per family" and the FK-backed ayah-reference rule. The already-shipped
    sibling settles it — a non-existent `ayahId` in `ayahOverrides` is `AyahReferenceUnknown` → `400` — and
    answering the identical class of defect with two different statuses inside one controller would be the
    real inconsistency. It also keeps the route's declared response set (`200/400/409`) unchanged.
  - **The `23503` arm carries no field.** By the time Postgres raises it the only identification available
    is a constraint name, which must never reach the envelope, so the violation is built with
    `Field = null` and `ApiMessages` renders the bare Arabic sentence. `LinkingWorkspaceViolation.Field` is
    nullable for exactly this case. The arm is unreachable for the six dimension columns above; it exists
    for the FKs the writer cannot pre-check — `user_id`/`created_by`/`updated_by` against `users`, and the
    child rows' parent references — so no FK violation anywhere in this writer can surface as a `500`
    again. Forcing one (a write attributed to a user id with no `users` row) now answers `400`
    «العنصر المشار إليه غير موجود» with the transaction rolled back.
- **Optimistic concurrency is Postgres `xmin`, applied as `OriginalValue`** — never `CurrentValue`, which
  would compare the row against the value the writer's own query just read and could never conflict. Which
  row's token guards which write is the whole point of the split:
  - **Structural writes (add, remove, reorder, clear) are guarded by the WORKSPACE row's `xmin`**, and
    every one of them therefore **stamps `updated_at`/`updated_by` on the workspace row**. That stamp is
    not bookkeeping — it is what advances the workspace `xmin`. A structural write that only touched
    `linking_workspace_sources` would leave the workspace token frozen forever and the client's
    `workspaceVersion` would guard nothing. Do not "optimize" the stamp away.
  - **Configuration replacement is guarded by the SOURCE row's `xmin` and deliberately does NOT touch the
    workspace row**, so two sources can be configured concurrently without a false conflict (spec FR-027).
- **A missing version is refused, never defaulted.** `ApplyWorkspaceVersion` throws
  `LinkingStaleVersionException` when the caller sent no token but a workspace row exists, and add refuses
  a caller who sent a token when no workspace exists. Both directions are a genuine client/server
  disagreement about whether the workspace exists; answering `409` sends the client to reload, which
  resolves it.
- **The workspace row is created only by the first mutation, inside that mutation's transaction.** Loading
  never writes (spec FR-019, research R21) — see `../../Reads/Linking/`. `AddSourceAsync` opens an explicit
  transaction because its result spans more than one `SaveChangesAsync`: the workspace row must exist
  before the source can reference its generated id, and the source must exist before its manual-ayah rows
  can reference *its* id. These entities carry FK properties and no navigation properties (repo
  convention), so EF cannot propagate a generated key across them in one save. Concurrent first mutations
  serialize on `UNIQUE (user_id)`. There is no provisioning step and no create endpoint.
- **Add is idempotent by identity, with the raw identity as the final guard.** `FindEquivalentSourceAsync`
  looks the candidate up by `(workspace_id, source_identity_hash)` and then compares the raw
  `source_identity` with `StringComparison.Ordinal`. The hash is what the unique index can hold — manual
  identities grow with the verse set and would blow PostgreSQL's ~2.7 KB btree index-entry limit as raw
  text (research R20) — and the raw comparison is what makes a SHA-256 collision a refusal rather than a
  silent merge into someone else's source. **A re-add refreshes the LABEL ONLY.** Order, inclusion mode,
  overrides, words, and the manual verse set are all left untouched, matching the Frontend's own
  `addSource`.
- **The manual verse set is identity-bearing and is written exactly once, at add time.** It is not part of
  the configuration document and `ReplaceSourceConfigurationAsync` never touches it. Changing the verses
  changes the identity, which by definition produces a *different* source — the flow for that is
  add-new-source. `page_hint` is populated from the ayah's own `quran_ayahs.page_from` at add time; no
  wire field supplies it.
- **`MaxPreparedSources` is enforced HERE, not in the handler.** `tasks.md` T037 lists it as a handler
  responsibility; it is in the writer because only here is the count read in the same transaction that
  performs the insert. A handler-side count is a separate round trip outside the transaction and races
  trivially past the limit.
- **Word rules are family-specific and are checked in order** (spec FR-021/FR-023, research R22).
  A non-empty `selectedWords` on an **automatic** source is rejected outright, **before** the words
  themselves are validated — the combination is invalid regardless of whether the words are real, so
  reporting a word-level problem would describe the wrong offence. Only manual Mushaf sources carry
  user-authored words, and each must exist, be non-marker, belong to its declared ayah, and that ayah must
  belong to this source's own manual verse set. Automatic sources carry only
  `automatic_word_matches_enabled`; their word contributions are derived from resolution at confirm time,
  never authored. **A manual ayah with zero selected words is valid** (spec FR-008) and must stay valid.
  - **De-duplication of `selectedWords` never swallows a contradiction.** A word belongs to exactly one
    ayah, so two entries sharing a `quranWordId` but naming *different* `ayahId`s are not a duplicate —
    one of them is false. `DistinctSelectedWords` walks the list in order and rejects that case with
    `SelectedWordAyahConflict` (`400`, naming the word) **before** collapsing anything; an **exact**
    duplicate (same word, same ayah) is still collapsed silently, because it asserts nothing new. The
    order matters: a plain `DistinctBy(quranWordId)` ahead of validation kept the first entry, validated it
    cleanly, and answered `200` to a request that carried an impossible claim.
- **Two coherence rules are enforced by the DATABASE, not only here** (spec FR-022) — see
  `../../Configurations/Linking/LinkingWorkspaceSourceConfiguration.cs`. `ck_..._kind_configuration_coherence`
  makes `automatic_word_matches_enabled IS NOT NULL` iff the kind is not manual and
  `manual_link_shape IS NOT NULL` iff it is; `ck_..._kind_reference_coherence` makes exactly the expected
  dimension column(s) non-null per kind. The writer refuses the same combinations first, with an Arabic
  message naming the field, so the CHECK is the backstop for a defective client that bypasses the API —
  not the primary user-facing validation. **Keep both halves**; deleting the writer check turns a `400`
  into a `500`, and deleting the CHECK removes the durable guarantee the spec asks for.
- **Children are diffed, not deleted-and-reinserted.** `ReplaceOverridesAsync` and
  `ReplaceSelectedWordsAsync` remove only what is absent and add only what is new. This is not a
  micro-optimization: both tables are keyed on a natural composite PK, and issuing a delete and an insert
  for the same key in one `SaveChanges` lets EF order the insert first and violate the PK.
- **Removal and clear hard-delete; the workspace row itself is never deleted.** "Clear all" empties
  `linking_workspace_sources` and leaves `linking_workspaces` standing. The source → child cascade is the
  only cascade in the model, so removing a source lets the database drop its manual ayahs, overrides, and
  words; everything pointing at Quran or Access data is `RESTRICT`, so no Linking write can ever delete a
  Quran row. There is no soft delete anywhere in the workspace family.
- **Every removal leaves `order_value` at `1..N`.** `Resequence` renumbers the survivors, and the survivor
  query excludes the row being removed — the database still shows it until `SaveChanges`, so a query that
  included it would renumber as if it had never left. Reorder validates that the submitted list is an exact
  permutation of the workspace's current source ids — nothing added, nothing missing, no duplicates —
  before it renumbers anything.

## Related

- Read side: `../../Reads/Linking/` (`EfLinkingWorkspaceReader`) and its `README.md`.
- Shared codec and projection: `../../Linking/` (`LinkingSourceStorage`, `LinkingWorkspaceProjection`).
- EF mappings, CHECKs and indexes: `../../Configurations/Linking/`.
- Contracts and exception types: `application/QuranDashboard.Application.Abstractions/Linking/`.
- Handlers: `application/QuranDashboard.Application/Linking/`.
- Controller and status mapping: `api/QuranDashboard.Api/Controllers/Linking/` and that project's README.
- Tests: **none.** The Test Freeze (`TESTING_CONSTITUTION.md`) is in force for this feature; no retained
  Backend test covers this writer, and the six workspace routes are deliberately absent from
  `SmokeRouteCatalog`, so `SmokeCoverageParityTests` reports them as uncovered. That is accepted, recorded
  debt, not an oversight.
