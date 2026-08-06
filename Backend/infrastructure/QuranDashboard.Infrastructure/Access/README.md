# Access infrastructure

This folder owns server-side access integrations: normalized email configuration, first-login
provisioning, the Logto Management API profile source, permission-catalogue synchronization, and
the explicit Owner reconciliation transaction.

`LogtoManagementApiUserProfileSource` reads `primaryEmail` from Logto `GET /api/users/{sub}` as
identity/matching data only. The Management API has no documented durable email-verification field
for this M2M client, so this source never represents primary email as verification evidence.

`OwnerBootstrap:Emails` is the complete desired Owner set. Each entry is parsed and normalized by
the shared `IEmailIdentityNormalizer`; empty, invalid, and duplicate normalized sets are rejected.
The reconciliation store compares that set, `Users.NormalizedEmail`, and freshly retrieved Logto
profile identity data. It never compares raw display email values. The Application reconciliation
use case can promote only the authenticated caller whose already-validated OIDC identity has the
matching `sub`, a present normalized `email`, and `email_verified=true`.

Every apply opens one transaction, obtains the dedicated PostgreSQL transaction-scoped advisory
lock, reloads its inputs, and then either commits all membership/grant/audit changes or none.
Interactive sign-in promotes only its verified configured caller and revokes that caller's direct
grants before attaching the Owner role. CLI reconciliation never adds Owners: it can only remove
resolved unconfigured Owners or clean existing Owner direct grants after last-active-Owner and
provider checks pass. Configured users awaiting their verified interactive sign-in do not block an
already active Owner. Disabled configured users remain Disabled. The store writes append-only
system-actor audit events with immutable before/after snapshots and provenance, then the
Application use case evicts changed role-cache entries after commit.
