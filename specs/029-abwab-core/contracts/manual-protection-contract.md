# Contract: Manual protection — apply / lift / full-preset + resolver

**Feature**: `029-abwab-core` | **Source**: Master Plan §7.2, §9, §11 (Manual protection), §18.3
steps 2 & 3. Realizes §18.3 only.

## Storage & resolver (Stage 2)

- **Record**: `ManualProtectionId`, `CategoryId`, typed `ProtectionType` ∈ {`CategoryData`,
  `InternalStructure`, `QuranContent`, `Deletion`, `Relationship`}, typed `ProtectionScope` ∈
  {`CategoryOnly`, `Subtree`}, applied/lifted actor + timestamps, active/soft-delete, `Version`.
- **One active per `(CategoryId, ProtectionType)`** via filtered unique index; scope is a column.
- **Resolver**: direct/inherited resolution evaluated from **current `AncestorIds`** (no descendant
  snapshot), returning type/scope, the **source ancestor**, and **server-clock-derived expiry** via
  server-clock DTOs, with action classification. A measured **deep-tree query budget** holds against
  real PostgreSQL.
- **Adapter acceptance**: the ManualProtection versioned adapter is **accepted before any protected
  category writer exists**.

## Operations (Stage 3)

- **Apply / lift** (`protection.apply` / `protection.lift`) — one tracked, audited, **reversible**
  ChangeSet. Applying the same active type/scope is **idempotent with no ChangeSet**. A scope change
  requires **Expected Version** and is one audited reversible edit. An existing protection does not
  block its **authorized lift**; **stabilization always does**.
- **Full protection (five-type preset)** — carries one selected `CategoryOnly`/`Subtree` scope and
  **atomically idempotent-upserts all five typed records** to that scope (never a sixth type).
  Same-scope records unchanged; each different-scope record requires its Expected Version → audited
  scope edit; missing types inserted. **All five already match → idempotent success, no ChangeSet.**
  Any stale/constraint/protection failure **rolls back all five**. Each type may later be lifted
  independently.
- **Soft-deleted targets** — effective reads and authorized lifts address a soft-deleted category by
  **immutable `CategoryId`**; a deletion cannot hide or strand a protection. This narrow security
  surface does **not** expose the deleted category to any ordinary command.

## Conflict codes (exact — §11)

| Code | When |
|---|---|
| `abwab.manual_protection` | applicable direct/inherited manual protection blocks a mutation/restore |
| `abwab.manual_protection_scope_conflict` | same active category/type found at a different scope during apply |
| `abwab.stabilization_active` | any mutation attempted before the exact two-hour end |
| `abwab.row_stale` / `abwab.timeline_generation_stale` | stale Version / generation |

## Read authorization

Full manual-protection metadata and the dedicated effective-protection read require
`protection.view`; without it only generic blocked flags are projected (see
`tree-read-contract.md`). Redaction is backend DTO projection.

## Tests

- One active record per `(CategoryId, type)`; idempotent same-scope apply with **no audit no-op**;
  expected-version audited scope change; conflicting-scope `abwab.manual_protection_scope_conflict`;
  apply/lift/preset atomicity; **stable preview blocker identity**; adapter round-trips → real-PG /
  race.
- Full-preset matrix: none/some/all pre-existing types, mixed pre-existing scopes, one scope applied
  to all five, required Expected Versions for **every** changed scope, all-matching no-op, per-type
  later lift, and a **concurrent stale scope edit rolling back the entire five-type command** →
  real-PG/API/mock/HTTP.
- Deep-tree resolver stays within the measured budget; direct/inherited source ancestor + server
  expiry shown, including authorized view/lift by immutable ID on a **soft-deleted** target.
