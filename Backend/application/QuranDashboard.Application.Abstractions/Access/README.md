# Access administration contracts

This folder defines the application boundary for the Owner-only access-administration API. It contains
the user list/detail and direct-grant projections, transition/relink commands, audit projections, and
the persistence abstractions consumed by `Application/Access/` handlers.

`AccessUserPaging` is the public user-list pagination contract: `page` is one-based from 1 through
`Int32.MaxValue`, `pageSize` is 1 through 100 with a default of 25, and a valid page whose offset is at
or after `totalCount` returns a successful empty collection.

`IAccessUserMutationService` owns pending-user acceptance, disable/reactivate transitions, and complete
direct-grant replacement. Every command carries the target `xmin` version and returns a controlled
operation result: invalid transitions and permission input are `400` concerns, unknown targets are
`404`, and version/identity races are `409` concerns at the HTTP boundary. Ordinary operations reject
Owner targets; Owner membership remains the reconciliation boundary.

`IAccessAuditAppender` only appends an audit entry to the caller's current EF unit of work. It has no
save or commit operation, so the mutation service can save state and audit history exactly once in the
same transaction. `IAccessAuditReader` exposes immutable snapshots through a bounded opaque keyset
cursor, newest first by `(occurredAtUtc, id)`.

`ILogtoSubjectRelinkService` separates a non-mutating preview from a confirmed update. Confirmation
revalidates the proposed subject and identity evidence, uses the target version, and never changes the
target's role, status, or direct grants.

`ILegacyRoleConversionService` is the operator boundary for the final Admin/Editor cleanup. Its inventory
returns each role-bearing user's ID, role ID/name, status, normalized email, `sub`, and direct-grant count.
Its conversion lease rereads and locks that state, refuses any preflight violation, clears only former
Admin/Editor `RoleId` values, and writes no inferred grants.

## Related

- Handler layer: `../../QuranDashboard.Application/Access/README.md`
- EF reads/writes: `../../../infrastructure/QuranDashboard.Infrastructure/Access/README.md`
- API routes: `../../../api/QuranDashboard.Api/Controllers/README.md`
