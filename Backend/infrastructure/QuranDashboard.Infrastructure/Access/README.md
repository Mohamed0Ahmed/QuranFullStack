# Access infrastructure

This folder owns server-side access integrations: normalized email configuration, first-login
provisioning, the Logto Management API profile source, permission-catalogue synchronization, explicit
Owner reconciliation, and legacy-role conversion persistence.

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
Application use case returns only after the transaction commits.

Request-scoped authorization reads live in `Persistence/Reads/Access/AuthorizationStateResolver.cs`.
That resolver projects one local user by exact `LogtoSub`: status, the local Owner relation, and direct
non-retired permission codes only for an active non-Owner. It never provisions users and never receives
role or permission claims. The scoped instance memoizes its first subject/task, so multiple authorization
requirements share the one database projection; a second distinct subject is an invariant failure.

`UserProvisioningService` separately projects the `/api/access/me` snapshot after provisioning:
explicit `IsOwner` and ordered non-retired direct permission codes for active non-Owners only.

The Phase 6 administration implementation keeps EF projections in `Persistence/Reads/Access/` and writes
in `Persistence/Writes/Access/`. `AccessUserMutationTransaction` starts the one transaction for a user
transition, locks the acting Owner and target/grant rows, rechecks the acting Owner from the database, and
checks the target `xmin` version before mutation. `AccessAuditAppender` adds immutable audit rows to that
same DbContext without saving independently. The transaction saves and commits once.
`EfAccessAuditReader` identifies the latest Owner-reconciliation summary from metadata provenance
`operation=owner-reconciliation`, so a newer system event from legacy-role conversion cannot mask it.
`EfLogtoSubjectRelinkService` revalidates both the
interactive evidence and Logto Management profile email through the shared normalizer before it changes
only `LogtoSub`; its Owner path also requires current reconciliation status.

`LegacyRoleConversionStore` locks role rows, role-bearing users, and their direct grants in one database
transaction. It rejects a role mismatch or direct grant after preflight, clears only Admin/Editor
relations, and appends one system audit event per converted user. The generated cleanup migration deletes
the two obsolete seed rows; the existing restrictive `users.role_id` foreign key makes that migration fail
if conversion was skipped.

`IAccessRequestContext` has an Infrastructure default backed by the ambient activity trace, so non-HTTP
composition roots such as AccessAdmin remain able to construct the shared audit writer. The API replaces
that default with its HTTP request trace implementation; a non-HTTP operation without an ambient activity
stores no correlation identifier rather than fabricating an HTTP context.

The shared relink service also receives a fail-closed non-HTTP interactive-evidence validator. AccessAdmin
does not offer relinking, so it cannot manufacture interactive evidence; the API replaces the default with
the JWT-backed validator for its Owner-only relink endpoints.
