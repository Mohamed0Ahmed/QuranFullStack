# Contract: Owner-only Permission Administration API + `/me` (Story 5)

**Source**: Master Plan §18.2 step 5 / exit. **Envelope**: shared `ApiResponse`
(`Backend/.architecture/API_GUIDELINES.md`) — success `{ isSuccess: true, message, data }`,
failure `{ isSuccess: false, message, errors }`. Keys are English; user-facing `message` is
Arabic by default. Business conflicts return **409**.

> This feature exposes the permission-administration surface as one security vertical slice.
> It **never** exposes Owner *membership* administration in the dashboard.

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
  **permanently audited** and invalidates cache on commit.
- **Conflicts (409)**: stale expected version; stale `ExpectedTimelineGeneration` (rejected
  **before any row mutation**); attempt to grant a non-assignable/baseline-violating code.
- **Auth failure**: non-Owner → unauthorized.

### Revoke a permission (role or direct subject)

- **Rules**: mirror grant; idempotent no-op revoke produces **no audit**; a real revoke is
  audited and invalidates cache on commit. Attempting to remove the **`attribution.view`
  baseline** is **rejected**.
- **Conflicts (409)**: stale version; stale `ExpectedTimelineGeneration` (pre-mutation).

### `/me` projection

- **Purpose**: return the caller's effective permissions after cache invalidation.
- **Rule**: `/me`, backend policy, cache, and the UI **converge on the committed winner**.
  `attribution.view` baseline metadata/policy is identical here to the other layers.

## Parity & non-authority invariants (tested)

- Permission codes identical across **seed / policy / `/me` / frontend / test** — 0 drift.
- `list/grant/revoke` parity; assignability/baseline denial; exact role/direct unique keys;
  idempotent no-audit no-ops; first-grant and grant-vs-revoke serialization; stale-version;
  unauthorized; permanent-audit; cache-invalidation; stabilization.
- Frontend hiding is **demonstrably non-authoritative** (a hidden action is still rejected by
  backend policy when invoked directly).
- **Out of scope here**: actual `attribution.view` Pending list/detail/count behavior — owned
  and tested by `032` (no forward Request-schema dependency in `028`).
