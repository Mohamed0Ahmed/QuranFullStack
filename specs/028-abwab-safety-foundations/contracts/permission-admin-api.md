# Contract: Owner-only Permission Administration API + `/me` (Story 5)

**Source**: Master Plan §18.2 step 5 / exit. **Envelope**: shared `ApiResponse`
(`Backend/.architecture/API_GUIDELINES.md`) — success `{ isSuccess: true, message, data }`,
failure `{ isSuccess: false, message, errors }`. Keys are English; user-facing `message` is
Arabic by default. Business conflicts return **409**.

> This feature exposes the permission-administration surface as one security vertical slice.
> It **never** exposes Owner *membership* administration in the dashboard.

## Security-audit vs product-head separation (§6.1, §6.2, §6.7, §7.7, §8)

Permission **and** System Owner writes use a **separate permanent append-only security-audit
unit of work**. This unit of work:

- does **NOT** advance `AbwabRevisionState.AuditHeadSequence`;
- does **NOT** create Product-Restore-head events (its current state is never inverse-restored);
- **still** takes the `AbwabWriteBarrier` and `AbwabRevisionState` locks (in that order) and
  **carries `ExpectedTimelineGeneration`** for generation freshness (§6.2) — it enforces the
  same freshness/serialization gates, but never advances the product timeline head.

## Endpoints (Owner-only)

All three require an authenticated **System Owner**; a non-Owner is rejected (unauthorized),
and rejection is enforced by **backend policy** (frontend hiding is non-authoritative).

### List assignable permissions & current assignments

- **Purpose**: return the exact permission catalogue (codes + metadata + assignability) and
  current role/direct assignments.
- **Success `data`**: catalogue entries whose codes are **identical** to seed / policy /
  `/me` / frontend / test catalogues (0 drift), each with metadata (e.g. `SystemOwnerOnly`,
  `DashboardAdminBaseline`) and assignability.

### Grant a permission (role or direct subject)

- **Input**: target (role|subject) unique key, permission code, `ExpectedTimelineGeneration`,
  expected assignment version.
- **Rules**: uniquely keyed; **first-grant serialization** and **grant-vs-revoke
  serialization**; idempotent re-grant is a **no-op with no audit**; a real grant is
  **permanently audited** (security-audit UoW, no product-head advance) and invalidates cache
  on commit. `SystemOwnerOnly` codes (`permission.*`, `audit.restore`, `safetyPoint.*`) cannot
  be granted to ordinary users.
- **Conflicts (409)**: `abwab.permission_assignment_stale` (expected retained role/subject
  assignment state or Version changed); `abwab.timeline_generation_stale` (stale
  `ExpectedTimelineGeneration`, rejected **before any row mutation**);
  `abwab.permission_baseline_locked` (grant of a non-assignable/`SystemOwnerOnly` code or a
  baseline-violating revoke); `abwab.stabilization_active` (grant/revoke attempted while the
  barrier is `Stabilizing`).
- **Auth failure**: non-Owner → unauthorized.

### Revoke a permission (role or direct subject)

- **Rules**: mirror grant; idempotent no-op revoke produces **no audit**; a real revoke is
  audited and invalidates cache on commit. Attempting to remove the **`attribution.view`
  baseline** is **rejected**.
- **Conflicts (409)**: `abwab.permission_assignment_stale` (stale version);
  `abwab.timeline_generation_stale` (stale `ExpectedTimelineGeneration`, pre-mutation);
  `abwab.permission_baseline_locked` (revoke of the `attribution.view` baseline);
  `abwab.stabilization_active` (revoke during `Stabilizing`).

### `/me` projection

- **Purpose**: return the caller's effective permissions after cache invalidation.
- **Rule**: `/me`, backend policy, cache, and the UI **converge on the committed winner**.
  `attribution.view` baseline metadata/policy is identical here to the other layers.

## Operational owner membership (out of dashboard, §7.7, §11)

System Owner add/remove and the zero-to-one bootstrap are **operational** paths with **no
dashboard surface** here. They use the same security-audit UoW above. A removal that would
leave zero active enabled System Owners is rejected with **`abwab.last_system_owner`** (§11);
these operational commands are also gated by `abwab.stabilization_active` during `Stabilizing`.

## Parity & non-authority invariants (tested)

- Permission codes identical across **seed / policy / `/me` / frontend / test** — 0 drift.
- `list/grant/revoke` parity; assignability/baseline denial; exact role/direct unique keys;
  idempotent no-audit no-ops; first-grant and grant-vs-revoke serialization; stale-version;
  unauthorized; permanent-audit; cache-invalidation; stabilization.
- Frontend hiding is **demonstrably non-authoritative** (a hidden action is still rejected by
  backend policy when invoked directly).
- **Security-audit separation**: grant/revoke/bootstrap produce permanent security-audit events
  with **0** advances of `AuditHeadSequence` and **0** Product-Restore-head events, while still
  taking the barrier + `AbwabRevisionState` locks and carrying `ExpectedTimelineGeneration`.
- **SystemOwnerOnly assignability**: granting `permission.*`/`audit.restore`/`safetyPoint.*` to
  an ordinary user is rejected (`abwab.permission_baseline_locked`).
- **Out of scope here**: actual `attribution.view` Pending list/detail/count behavior — owned
  and tested by `032` (no forward Request-schema dependency in `028`).
