# Permissions feature (US5)

Owner-only **permission administration** — the first real `@angular/forms` (Reactive Forms) surface
in the app, and the frontend half of the 028 security vertical slice.

## What it does

- `pages/permissions-page` — a Reactive Form (`ReactiveFormsModule`) to **grant** / **revoke** a
  permission for a **Role** or **Subject**, plus a read-only list of current assignments.
- `state/permissions.facade.ts` — page-scoped facade; unwraps the `ApiResponse` envelope, owns
  load/submit state, computes `expectedVersion` from the loaded assignments, and surfaces a 409
  (stale generation / assignment) through `AsyncAction`'s distinct `conflict` status.
- `data-access/permissions.api.ts` — thin API boundary (`GET/POST /api/security/permissions*`),
  returning the raw envelope.

## Authority boundary (non-authoritative hiding)

- The route is gated by `permissionGuard('permission.administer')` and the page additionally hides the
  form when the caller lacks the permission. **Both are non-authoritative** — the backend `SystemOwner`
  policy rejects a direct call regardless of what the UI shows (proved by
  `e2e/permissions/non-authoritative.spec.ts`).
- Permission codes come from the shared parity source `core/auth/permission-codes.ts`; the backend
  `PermissionParityTests` fails on any drift between that file and the backend catalogue.

## Not here

- System Owner **membership** administration (add/remove/bootstrap) has **no UI** — it is an
  operational backend-only concern (see `Backend/.../Api/Security/README.md`).
