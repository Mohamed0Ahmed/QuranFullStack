# API Security surface (028 US5)

This area exposes **exactly one** security surface to the dashboard: **Owner-only permission
administration**.

## Exposed (dashboard surface)

- `GET /api/security/permissions` — list the permission catalogue + current assignments.
- `POST /api/security/permissions/grant` — grant a permission to a role or subject.
- `POST /api/security/permissions/revoke` — revoke a permission from a role or subject.

All three require an authenticated, active **System Owner**, enforced by the
`AuthorizationPolicyNames.SystemOwner` backend policy (`SystemOwnerAuthorizationHandler`). A
non-owner is rejected by the backend regardless of what the frontend shows — **frontend hiding is
non-authoritative**. The surface is additionally protected by the always-on `permission-admin`
named rate-limiter policy.

`GET /api/access/me` projects the caller's **effective permissions** (`EffectivePermissionResolver`)
so `/me`, backend policy, cache, and the UI converge on the committed winner.

## NOT exposed (deliberate boundary — FR-029, §7.7/§11)

System Owner **membership** administration — add, remove, and the zero-to-one bootstrap
(`SystemOwnerAdministrationHandler`) — is an **operational** concern with **no dashboard surface**.
There is intentionally **no controller** for it here. Those handlers run through the same separate
security-audit unit of work, are gated by `abwab.stabilization_active` during stabilization, and the
final-owner invariant rejects the last-owner removal with `abwab.last_system_owner`. The
`owner-bootstrap` named rate-limiter policy is reserved for that operational path.

## Security-audit separation (FR-039)

Permission and owner writes use a **separate permanent append-only security-audit unit of work**
(`SecurityAuditedCommitExecutor`). It takes the `AbwabWriteBarrier` then `AbwabRevisionState` row
locks and carries `ExpectedTimelineGeneration` for freshness, but **never** advances
`AbwabRevisionState.AuditHeadSequence` and **never** creates a Product-Restore-head event.
