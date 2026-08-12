# Contract: Preflight & Confirmation API

> Wire truth is the regenerated swagger + generated models (research.md R16). Both routes
> `[RequireOwner]`, `ApiResponse<T>` envelope, Arabic messages. **The two routes share one pure
> classifier** (`LinkingOperationClassifier`) — preflight and confirm can never disagree about
> semantics (plan Phase 8; research.md R8).

## Shared request shape — `LinkingOperationRequest`

Preflight takes exactly this; Confirm adds `idempotencyKey` and the per-source/global staleness
fields marked *(confirm)*:

```jsonc
{
  "doorId": 12,
  "preflightToken": "…",                    // (confirm, REQUIRED — missing ⇒ controlled 400) — proves the flow ran preflight; never trusted as write authority
  "idempotencyKey": "8d0f…-uuid",           // (confirm, required) — one per user attempt, reused across retries
  "sources": [                               // submitted order is preserved
    {
      "descriptor": { /* LinkingSourceDescriptorBody — linking-sources-api.md */ },
      "contributionMode": "automatic",       // automatic | manual_single | manual_independent | manual_grouped
      "automaticWordMatchesEnabled": true,   // automatic sources only (null for manual): ON ⇒ words derived server-side from resolution; OFF ⇒ zero word contributions
      "orderValue": 1,
      "existingContributionId": 55,          // (confirm, optional) — from preflight when a live contribution exists
      "existingContributionVersion": 91011,  // (confirm, optional) — its xmin, from preflight
      "units": [
        { "ayahs": [
            { "ayahId": 262,
              "selectedWordIds": [ 12345, 12351 ],   // MANUAL sources only — canonical quranWordIds (Locked §13); may be empty; MUST be empty/absent for automatic sources
              "descriptions": [ "نص أول", "نص ثانٍ" ] // ordered bodies; index+1 = orderValue
            } ] }
      ]
    }
  ]
}
```

**Structural rules** (rejected with 400 when violated):

- ≥1 source; ≥1 ayah per source (**FR-044a** — zero-ayah submission is a controlled validation
  failure; total retraction is out of scope).
- Grouping (FR-046): `automatic` / `manual_single` / `manual_independent` ⇒ **every unit has
  exactly one ayah**; `manual_grouped` ⇒ **exactly one unit**. Manual modes only with
  `manual-mushaf-ayahs` descriptors; `automatic` only with the five automatic families.
- No duplicate source identity within one operation; no duplicate ayah within a source;
  descriptions ≤10 × ≤2000 trimmed non-blank per ayah.
- **Word authorship per family** (spec FR-021, research.md R22): `selectedWordIds` is legal only
  on manual sources — any authored word on an automatic source ⇒ 400. Automatic sources carry
  `automaticWordMatchesEnabled` instead (required non-null for automatic, must be null for
  manual); their word contributions are derived server-side. A manual ayah with zero
  `selectedWordIds` is valid (spec FR-008).
- Positions, indexes, `wordNumber`, or text as word identity → 400 at contract level (only
  `quranWordId`).

## `POST /api/linking/operations/preflight` — read-only

**Performs no writes** (spec FR-042 — acceptance proves row counts identical before/after).
Source membership is read through the Phase 4 cached boundary; the Door's confirmed state is read
fresh (never cached).

### Response — `200` `ApiResponse<LinkingPreflightResultDto>`

```jsonc
{
  "doorId": 12, "doorName": "باب الرحمة",
  "isNoOp": false,                       // true ⇔ every source UNCHANGED
  "isBlocked": false,                    // true ⇔ any INVALID anywhere
  "preflightToken": "…",                 // hash(door identity + live state, each affected (contributionId, xmin), canonical OPERATION INTENT — composition defined below) — deliberately NO resolvedAtUtc: cache expiry / re-resolution never stales a preflight (research.md R8)
  "totals": { "requested": 3, "new": 2, "overlapping": 1, "unchanged": 0, "updated": 0, "removed": 0, "invalid": 0 },
  "sources": [
    {
      "sourceIdentity": "unique-word|simple|3210", "label": "الرحيم",
      "sourceKind": "unique-word", "contributionMode": "automatic",
      "classification": "NEW_SOURCE",     // NEW_SOURCE | UNCHANGED | UPDATE | INVALID
      "existingContributionId": null, "existingContributionVersion": null,
      "counts": { "requested": 3, "new": 2, "overlapping": 1, "unchanged": 0, "updated": 0, "removed": 0, "invalid": 0 },
      "ayahs": [
        {
          "ayahId": 1, "verseKey": "1:1", "surahNumber": 1, "ayahNumber": 1,
          "classification": "OVERLAP_OTHER_SOURCE",
          "overlappingSources": [                                   // ALWAYS populated, every classification —
            { "sourceIdentity": "unique-word|simple|3209",          // structured provenance from each overlapping
              "label": "الرحمن",                                     // live contribution's stored descriptor snapshot,
              "sourceKind": "unique-word" } ],                      // so the Arabic UI shows a name, not a technical key
          "wordChanges": { "added": [ 3 ], "removed": [], "unchanged": [] },        // canonical quranWordIds
          "descriptionChanges": { "added": [], "removed": [], "changed": [], "unchanged": [] },
          "invalidReason": null
        }
      ]
    }
  ]
}
```

### Classification semantics (the pure classifier — spec FR-037..FR-041)

- Ayah values `NEW_AYAH | OVERLAP_OTHER_SOURCE | UNCHANGED | UPDATE | REMOVE | INVALID` are
  **mutually exclusive**; per source, `requested = new + overlapping + unchanged + updated +
  invalid` partitions exactly; `removed` counts separately (absent from the submitted set).
- **Precedence (plan D6)**: a source-owned change wins — `UPDATE`/`REMOVE` keep their
  classification even when overlapping; `OVERLAP_OTHER_SOURCE` only where the item would otherwise
  be `NEW_AYAH`.
- `UNCHANGED` comparison covers ayah membership, words, descriptions, grouping, and source-owned
  configuration — **the label is excluded** (clarified; spec FR-004): a label-only difference is
  `UNCHANGED`.
- `wordChanges` compares **effective** word contributions against the confirmed rows: for manual
  sources the user-authored set, for automatic sources the derived set (toggle ⊕ fresh
  resolution) — so flipping the toggle surfaces as word diffs / `UPDATE`.

### Preflight token composition

`preflightToken = hash(doorComponent, contributionComponents, operationIntent)`:

- **Door component** — the Door's identity and live state.
- **Contribution components** — each affected live contribution's `(id, xmin)`.
- **Operation intent** — a deterministic canonical serialization (stable field order, sources
  ordered by `orderValue`, id sets sorted) of exactly the fields that affect the classified
  linking intent: `doorId`; per source — the identity-bearing descriptor fields,
  `contributionMode`, `automaticWordMatchesEnabled`, `orderValue`, the unit/grouping structure,
  the submitted ayah ids, manual `selectedWordIds`, and descriptions.

**Excluded** (Confirm-only or non-semantic — never hashed): `preflightToken` itself,
`idempotencyKey`, `existingContributionId`, `existingContributionVersion`, `resolvedAtUtc`, and
the display-only `label` (excluded from change classification by spec FR-004).

Preflight and Confirm MUST use the **same canonicalization function**, so an unchanged request
always reproduces the same token — staleness can only originate from the Door/contribution
components, never from resubmitting identical intent.
- Only `INVALID` blocks (`isBlocked`); archived Door, unknown ayah, marker word, foreign word,
  grouping violation → `INVALID` with per-item `invalidReason`.
- Counts always accompany items; items are never replaced by counts (spec FR-040). The locked
  example must reproduce: Door holds «الرحمن» (A,B,C); preflighting «الرحيم» (A,D,E) →
  `NEW_SOURCE`, A = `OVERLAP_OTHER_SOURCE` naming «الرحمن», D,E = `NEW_AYAH`, counts 3 = 2+1.

## `POST /api/linking/operations` — atomic confirm/update

### Validation & the transaction boundary (plan Phase 9)

**Phase A — before the write transaction** (immutable inputs and Quran source truth only; Quran
data never changes at runtime, so these reads need no transaction):

1. **Structure** — request shape; `preflightToken` present (**required** — missing ⇒ controlled
   400); ≥1 source and ≥1 ayah per source (FR-044a); no intra-source duplicate ayahs; grouping per
   FR-046; description limits + contiguous order.
2. **Actor** from `AuthorizationState.UserId` (F10) + **Owner** re-check in the handler.
3. **Descriptors** valid; dimension ids exist.
4. **Source membership** — re-resolve every source through the cached boundary; every submitted
   ayah must be a member (anti-tamper; warm-cache cost ≈0).
5. **Words** — manual sources only (the only family with authored words): canonical id exists,
   non-marker, belongs to the declared ayah. Automatic sources must author no words; their word
   contributions are **derived here** from the fresh resolution — toggle on ⇒ that ayah's
   `matchedQuranWordIds`, toggle off ⇒ none (spec FR-021, research.md R22).

**Phase B — inside the single write transaction** (every check that reads mutable confirmed state
runs under the same transaction snapshot that writes — **no gap** between classification and
write; all-or-nothing, spec FR-044). READ COMMITTED alone would let two concurrent Confirms
interleave, so Phase B **serializes on the target Door row**:

1. **Idempotency** — existing `linking_operations.idempotency_key` → return its stored `outcome`,
   200, write nothing (FR-050). Durable replay exists **only** for confirmations that wrote an
   operation row — a fully-unchanged no-op never stored one (D5), so a repeated no-op falls
   through, re-evaluates, and returns the same no-op success again.
2. **Door lock** — acquire a row-level write lock on the target `abwab_doors` row (`FOR UPDATE`,
   via the repository's EF equivalent), held until COMMIT/ROLLBACK; then verify the Door still
   exists and `deleted_at IS NULL`. Two Confirms for the same Door serialize here, and a
   concurrent Door archive/update cannot slip between classification and the Linking writes.
   Confirms against different Doors never contend. No broader locking is introduced.
3. **Load** the Door's current live contributions and children.
4. **Versions** — apply each `existingContributionVersion` via
   `Entry(x).Property(x => x.Version).OriginalValue`.
5. **Re-classify** with the same pure classifier against the state just loaded (state in →
   classification out; the classifier performs no repository access).
6. **Token check** — recompute the `preflightToken` from current state using the **same
   canonicalization function** Preflight used (composition above) and compare with the required
   supplied token; mismatch → **409 PreflightStale** carrying the fresh classification
   (client re-presents, never fails — spec FR-043); zero writes.
7. **Uniqueness / current state** — `NEW_SOURCE` colliding with a live
   `(door_id, source_identity_hash)` → 409, zero writes; any `INVALID` → 400, zero writes.

```text
if every source classifies UNCHANGED:
    write NOTHING (no operation row, no idempotency record — a repeat re-evaluates
    and returns this same success); return 200 «لا توجد تغييرات جديدة لتنفيذها»   (D5, FR-049/FR-050)

INSERT linking_operations (door, actor, idempotency_key)    -- early: operation_id now available
for each source in submitted order:
    UNCHANGED  → skip entirely (no row touched — label-only diffs land here)
    NEW_SOURCE → INSERT contribution → units → unit_ayahs → words → descriptions
    UPDATE     → replace the already-loaded live contribution's children to exactly the
                 submitted state (add new unit-ayahs, hard-delete absent ones, replace each
                 ayah's words + descriptions wholesale);
                 stamp updated_at/updated_by; re-point operation_id
finalize the operation exactly once — counts + the outcome snapshot carrying the final
contribution ids and applied classifications (the same logical result this confirmation
returns); construction inside the creation transaction, not a later update
COMMIT   -- after commit the operation row is immutable forever (never edited, never soft-deleted)
```

**Replacement, never union** (Locked §6, FR-048). **Attribution** stamped on every audited
authored/lifecycle record — contributions, both description tables, and the operation via
`actor_user_id` + `confirmed_at`; leaf rows (units, unit-ayahs, unit-ayah words) inherit from
their parent aggregate (FR-052 — first area to populate these columns, F11). **Exception
translation mandatory** (F12):
`23505` → `LinkingDuplicateContributionException`; `DbUpdateConcurrencyException` →
`LinkingStaleVersionException`; an untranslated 500 is a defect.

### Response — `200` `ApiResponse<LinkingConfirmationResultDto>`

Also used for the no-op success. Carries the applied outcome: per source — identity, final
classification, `contributionId`, counts; operation totals; the stored snapshot equals what an
idempotent replay returns.

### Status mapping

| Status | When |
| --- | --- |
| 200 | Applied; idempotent replay; or all-unchanged no-op («لا توجد تغييرات جديدة لتنفيذها») |
| 400 | Any structural/validation failure (Phase A; FR-044a) — including a **missing `preflightToken`** — or an `INVALID` classification (Phase B) |
| 404 | Door not found |
| 409 | Stale contribution version · stale preflight (fresh classification attached) · duplicate live contribution |

### Post-write invariants (acceptance-checked, plan Phase 9)

- Confirming «الرحيم» (A,D,E) against a Door holding «الرحمن» (A,B,C) leaves «الرحمن»
  **byte-identical**; ayah A holds two contributions.
- Updated contribution keeps its `id`; `xmin` advances; no delete-and-recreate.
- Grouped `[[A,B]]` + automatic `[[A],[C]]` persist as 3 units / 2 contributions, never merged.
- One rejected source ⇒ database untouched.
- Confirmation invalidates **no** cache entry — it writes no Quran data, so an invalidating
  decorator by analogy with Abwab would be incorrect.
