# Data Model: Abwab Ayah Linking

Twelve new PostgreSQL tables in three EF Core migrations. Two strictly separate families that are
never merged and share no status column (Locked §23): six **workspace** tables (mutable, per-user,
hard-delete) and six **confirmed** tables (durable, per-Door, soft-delete at the contribution
boundary only). All names snake_case per repo convention. Every migration is created with
`Backend/scripts/add-mig <Name>` (EF tooling only), then `check-pending-model`, then
`create-smoke-dump` (research.md R17).

**Two different string vocabularies exist for source kind — do not conflate them:**

| Context | Values |
| --- | --- |
| Source **identity string** (wire + workspace/contribution `source_identity` column, byte-exact per contracts/source-identity.md) | `manual-mushaf-ayahs`, `unique-word`, `root`, `lemma`, `stem`, `word-type` (kebab) |
| `source_kind` **column** values (DB CHECK) | `manual_mushaf_ayahs`, `unique_word`, `root`, `lemma`, `stem`, `word_type` (snake) |

**Common column shorthand** used below — *audit cols* = `created_at timestamptz`,
`created_by bigint FK→access_users RESTRICT`, `updated_at timestamptz`,
`updated_by bigint FK→access_users RESTRICT`. Linking is the **first area to actually populate
these** (research.md R12). *xmin* = the PostgreSQL system column mapped as the EF concurrency
token (`Version`), exactly as Abwab maps it.

**Shared descriptor column set** (appears in both `linking_workspace_sources` and
`linking_source_contributions`): `source_kind`, `source_identity text NOT NULL` (the raw,
byte-exact canonical identity — display/debug/parity and the final equality guard; never indexed
uniquely because manual identities are unbounded in length),
`source_identity_hash bytea NOT NULL` (the 32-byte SHA-256 digest of the UTF-8 bytes of the exact
raw identity — the value all uniqueness boundaries use; research.md R20), `label`, `scope jsonb`,
and six nullable dimension references — `root_id`, `lemma_id`, `stem_id`,
`unique_simple_word_id`, `unique_tashkeel_word_id`, `word_type_tashkeel_word_id` — each FK
`RESTRICT` to its existing morphology/word dimension table (exact target tables follow the
existing Words-explorer entity mappings; never invent new dimension tables). On
collision-sensitive paths (idempotent add, live-contribution matching) the writer compares the
raw `source_identity` as the final guard after the hash lookup.

**Shared descriptor CHECKs** (both tables):

1. `source_kind` ∈ the six snake values.
2. `scope` is a jsonb **object** carrying a numeric `schemaVersion` (same `jsonb_typeof` CHECK
   pattern `access_audit_events` uses).
3. **Kind/reference coherence** — exactly the expected dimension column(s) non-null per kind:
   `root` ⇒ only `root_id`; `lemma` ⇒ only `lemma_id`; `stem` ⇒ only `stem_id`;
   `unique_word` ⇒ exactly one of `unique_simple_word_id` / `unique_tashkeel_word_id` (per mode);
   `word_type` ⇒ exactly one of `root_id` / `lemma_id` / `stem_id` /
   `word_type_tashkeel_word_id` (per selection kind); `manual_mushaf_ayahs` ⇒ all six NULL.

---

## Migration M1 — `AddLinkingWorkspace` (5 tables)

### 1. `linking_workspaces`

| Aspect | Definition |
| --- | --- |
| Key | `id bigint` PK |
| Columns | `user_id bigint` FK→`access_users` RESTRICT; audit cols; xmin |
| Unique | `UNIQUE (user_id)` — one workspace per user |
| Behavior | Created **only by the first mutating operation** (typically the first add-source), atomically inside that mutation's transaction, with concurrent first mutations serialized by `UNIQUE (user_id)`. **Loading never writes** — a `GET` with no row returns an empty representation and inserts nothing (spec FR-019, research.md R21). Never deleted; "clear all" empties sources, not this row |

### 2. `linking_workspace_sources`

| Aspect | Definition |
| --- | --- |
| Key | `id bigint` PK |
| Columns | `workspace_id` FK→`linking_workspaces` RESTRICT (writer deletes explicitly — the only CASCADEs in the model are source→own children); `order_value int`; shared descriptor column set; `inclusion_mode text`; `automatic_word_matches_enabled bool NULL`; `manual_link_shape text NULL`; `last_resolved_count int NULL`; `last_resolved_at_utc timestamptz NULL`; audit cols; xmin |
| Unique | `UNIQUE (workspace_id, source_identity_hash)` — server-side idempotent add (raw `source_identity` compared as the final guard; research.md R20) |
| CHECKs | shared descriptor CHECKs; `inclusion_mode` ∈ (`all_except`, `only`); `manual_link_shape` ∈ (`grouped`, `independent`) or NULL; **kind/configuration coherence**: `automatic_word_matches_enabled IS NOT NULL` iff kind ≠ manual, `manual_link_shape IS NOT NULL` iff kind = manual (spec FR-022 — the DB itself rejects incoherent configuration) |
| Indexes | `(workspace_id, order_value)` |

### 3. `linking_workspace_source_manual_ayahs`

| Aspect | Definition |
| --- | --- |
| Key | PK `(workspace_source_id, ayah_id)` |
| Columns | `order_value int`; `page_hint int NULL` |
| FKs | `workspace_source_id` → sources **CASCADE**; `ayah_id` → `quran_ayahs` RESTRICT |
| Indexes | `(workspace_source_id, order_value)` |
| Note | Storing `ayah_id` (FK-validated) instead of the prototype's verse-key strings is a deliberate upgrade — the codec could only validate syntax (spec FR-024) |

### 4. `linking_workspace_source_ayah_overrides`

| Aspect | Definition |
| --- | --- |
| Key | PK `(workspace_source_id, ayah_id)` — the inclusion/exclusion set for `inclusion_mode` |
| FKs | source **CASCADE**; `quran_ayahs` RESTRICT |
| Indexes | the PK |

### 5. `linking_workspace_source_words`

| Aspect | Definition |
| --- | --- |
| Key | PK `(workspace_source_id, quran_word_id)` |
| Columns | `ayah_id` (denormalized for the per-ayah read) |
| FKs | source **CASCADE**; `quran_words` RESTRICT; `quran_ayahs` RESTRICT |
| Indexes | `(workspace_source_id, ayah_id)` |
| Semantics | **Manual Mushaf sources only** — user-authored selections. An automatic source never has rows here (it carries only `automatic_word_matches_enabled`; research.md R22). Cross-table rule enforced in the **writer** (no triggers). A manual ayah with zero rows is valid (spec FR-008) |
| Validation (writer, FR-023) | word exists, non-marker, belongs to declared ayah, and the ayah belongs to the source's manual verse set |

## Migration M2 — `AddLinkingWorkspaceDescriptions` (1 table)

### 6. `linking_workspace_source_descriptions`

| Aspect | Definition |
| --- | --- |
| Key | `id bigint` PK (bigserial) |
| Columns | `workspace_source_id`; `ayah_id`; `order_value int`; `body varchar(2000)`; audit cols; xmin |
| FKs | source **CASCADE**; `quran_ayahs` RESTRICT |
| CHECKs | `btrim(body) <> ''`; `order_value BETWEEN 1 AND 10` |
| Indexes | `UNIQUE (workspace_source_id, ayah_id, order_value)` — uniqueness is what turns the BETWEEN check into a hard "max 10 per (source, ayah)" database guarantee, given the writer resequences `1..N` on every mutation (spec FR-031/FR-035) |

## Migration M3 — `AddLinkingConfirmedState` (6 tables)

### 7. `linking_operations`

| Aspect | Definition |
| --- | --- |
| Key | `id bigint` PK |
| Columns | `door_id` FK→`abwab_doors` RESTRICT; `actor_user_id` FK→`access_users` RESTRICT; `idempotency_key uuid`; `confirmed_at timestamptz`; `source_count int`; `ayah_count int`; `outcome jsonb` |
| Unique | `UNIQUE (idempotency_key)` — the replay lookup (spec FR-050). A fully-unchanged no-op writes **no** row here, so it has no durable replay record — a repeat re-evaluates and returns the same no-op success (research.md R6) |
| Indexes | `(door_id, confirmed_at DESC)` |
| CHECKs | `outcome` is a jsonb object with numeric `schemaVersion` (`access_audit_events` pattern) |
| Lifecycle | **Immutable after its confirmation transaction commits** — never edited by later operations, never soft-deleted. Within its own creation transaction: inserted early (so `operation_id` is available to contributions), then `outcome` finalized exactly once — final contribution ids, applied classifications, counts — before COMMIT (an INSERT-then-UPDATE inside that one transaction is construction, not a later lifecycle update). `outcome` is a bounded response snapshot for idempotent replay, equal to the confirmation's returned result — never relational truth |

### 8. `linking_source_contributions`

| Aspect | Definition |
| --- | --- |
| Key | `id bigint` PK |
| Columns | `operation_id` FK→`linking_operations` RESTRICT (re-pointed to the newest operation that touched this contribution); `door_id` FK→`abwab_doors` RESTRICT (denormalized); `order_value int`; `contribution_mode text`; shared descriptor column set; `resolved_ayah_count int`; `resolved_at_utc timestamptz`; audit cols; `deleted_at timestamptz NULL`; `deleted_by bigint NULL`; xmin |
| Unique | **`UNIQUE (door_id, source_identity_hash) WHERE deleted_at IS NULL`** — the Door+Source boundary (Locked §4, spec FR-047; raw `source_identity` compared as the final guard, research.md R20); `UNIQUE (id, door_id)` (redundant; enables a future Door-scoped composite FK) |
| CHECKs | shared descriptor CHECKs; `contribution_mode` ∈ (`automatic`, `manual_single`, `manual_independent`, `manual_grouped`); manual modes iff `source_kind = 'manual_mushaf_ayahs'` |
| Indexes | `(operation_id, order_value)`; `(door_id) WHERE deleted_at IS NULL`; one filtered index per dimension column (the "which links came via root X" provenance question) |

### 9. `linking_units`

| Aspect | Definition |
| --- | --- |
| Key | `id bigint` PK |
| Columns | `source_contribution_id` FK RESTRICT; `order_value int`; `is_grouped bool` |
| Unique | `UNIQUE (source_contribution_id, order_value)`; `UNIQUE (id, source_contribution_id)` (enables the composite FK below) |
| CHECK / writer rule | `is_grouped = true` only when the parent contribution is `manual_grouped` — the cross-row half lives in the **writer** (this repository uses **no triggers**; record the honest limit in `Writes/Linking/README.md`) |

### 10. `linking_unit_ayahs`

| Aspect | Definition |
| --- | --- |
| Key | `id bigint` PK |
| Columns | `unit_id`; `source_contribution_id` (denormalized); `ayah_id` FK→`quran_ayahs` RESTRICT; `order_value int` |
| Composite FK | `(unit_id, source_contribution_id)` → `linking_units (id, source_contribution_id)` — the denormalized column **cannot** disagree with its grandparent |
| Unique | **`UNIQUE (source_contribution_id, ayah_id)`** — one source contributes an ayah at most once (Locked §5's grain: same ayah via *different* contributions is allowed) |
| Indexes | the unique one; `(unit_id, order_value)`; `(ayah_id)` (the future reverse "where is this ayah linked" read) |

### 11. `linking_unit_ayah_words`

| Aspect | Definition |
| --- | --- |
| Key | PK `(unit_ayah_id, quran_word_id)` |
| Columns | `ayah_id` |
| FKs | `unit_ayah_id` → unit-ayahs **CASCADE**; `quran_words` RESTRICT; `quran_ayahs` RESTRICT |
| Indexes | `(quran_word_id)` |
| Semantics | Materialized word contributions: **user-authored** for manual contributions, **derived from resolution at confirm time** for automatic contributions with the word-match toggle on; zero rows when the toggle is off or a manual ayah has no selections (research.md R22) |

### 12. `linking_unit_ayah_descriptions`

| Aspect | Definition |
| --- | --- |
| Key | `id bigint` PK |
| Columns | `unit_ayah_id` FK **CASCADE**; `order_value int`; `body varchar(2000)`; audit cols |
| CHECKs | `btrim(body) <> ''`; `order_value BETWEEN 1 AND 10` |
| Indexes | `UNIQUE (unit_ayah_id, order_value)` — uniqueness + the BETWEEN check together form the hard "max 10" database guarantee (an ordinary index would let eleven rows reuse one `order_value`); the writer still resequences `1..N` |
| Note | **No `deleted_at`** — children are hard-deleted by replacement semantics (research.md R5); soft delete lives only at the contribution boundary |

---

## Relationships

```text
access_users 1──1 linking_workspaces 1──* linking_workspace_sources
                                              ├──* linking_workspace_source_manual_ayahs   *──1 quran_ayahs
                                              ├──* linking_workspace_source_ayah_overrides *──1 quran_ayahs
                                              ├──* linking_workspace_source_words          *──1 quran_words, quran_ayahs
                                              └──* linking_workspace_source_descriptions   *──1 quran_ayahs

abwab_doors 1──* linking_operations (actor: access_users)
abwab_doors 1──* linking_source_contributions *──1 linking_operations (latest toucher)
linking_source_contributions 1──* linking_units 1──* linking_unit_ayahs *──1 quran_ayahs
linking_unit_ayahs 1──* linking_unit_ayah_words        *──1 quran_words, quran_ayahs
linking_unit_ayahs 1──* linking_unit_ayah_descriptions
```

Cascade policy: **only** a parent's own child collections cascade (workspace source → its four
child tables; unit-ayah → words/descriptions... via unit replacement the writer deletes
explicitly). Everything pointing at Quran or Access data is RESTRICT — Quran rows are never
deletable through Linking (gate G3).

## Lifecycle / state transitions

| Entity | Transitions |
| --- | --- |
| Workspace | created atomically by the first workspace mutation (loading never writes — an absent workspace reads as an empty representation) → lives forever (never deleted) |
| Workspace source | added (idempotent by identity — re-add refreshes label only) → configuration replaced wholesale (per-source xmin) → removed / cleared (hard delete, workspace xmin) |
| Contribution | created live by confirm (`NEW_SOURCE`) → updated **in place** by confirm (`UPDATE`: children replaced to exactly the submitted state, `updated_*` stamped, `operation_id` re-pointed, xmin advances) → *(future, out of scope)* soft-deleted → restored |
| Operation | created inside the confirmation transaction that changed anything (inserted early, outcome finalized once with the final contribution ids before COMMIT); immutable after commit — never edited, never soft-deleted; no row for fully-unchanged no-ops |
| Unit / unit-ayah / word / description | no independent lifecycle — replaced wholesale under their contribution by replacement semantics (spec FR-048) |

## Concurrency

- `xmin` mapped as the EF concurrency token on: `linking_workspaces`,
  `linking_workspace_sources`, `linking_workspace_source_descriptions`,
  `linking_source_contributions`.
- Update path applies the client token via `Entry(x).Property(x => x.Version).OriginalValue`
  (research.md R8), exactly as every Abwab write does.
- Every writer save translates: `DbUpdateConcurrencyException` → `LinkingStaleVersionException`
  (→ 409); Postgres `23505` → `LinkingDuplicateContributionException` (→ 409) (research.md R13).

## Validation rules → spec traceability

| Rule (enforced in writer + DB where shown) | Spec |
| --- | --- |
| One workspace per user (UNIQUE) | FR-019 |
| Idempotent add by `(workspace_id, source_identity_hash)` (UNIQUE) + raw-identity final guard | FR-020, FR-004 |
| Kind/configuration coherence (CHECK) | FR-022 |
| Kind/reference coherence (CHECK) | FR-001, FR-022 |
| Manual-only user-authored words: exist, non-marker, declared ayah, ayah in manual set (writer); automatic word rows derived from resolution, never authored | FR-021, FR-023 |
| Manual ayahs / overrides are FK-real ayahs | FR-024 |
| Descriptions ≤10, ≤2000 non-blank, contiguous order (CHECK + UNIQUE + writer) | FR-031..FR-035 |
| One live contribution per (door, identity) — partial UNIQUE on `(door_id, source_identity_hash)` + raw guard | FR-047 |
| One ayah per source contribution (UNIQUE) | Locked §5 grain |
| Denormalized `source_contribution_id` cannot drift (composite FK) | data integrity |
| ≥1 ayah per submitted source (writer + handler) | FR-044a |
| `is_grouped` iff manual-grouped parent (writer; no triggers) | FR-046 |
| Attribution on the audited aggregate/authored tables (workspaces, workspace sources, both description tables, contributions, operations); leaf rows inherit from their parent | FR-052 |
