# Access application handlers

This folder owns application use cases for first-login provisioning, Owner reconciliation, and the
Owner-only administration surface. HTTP controllers bind requests and map outcomes; these handlers
validate request-level inputs and call the abstractions in
`QuranDashboard.Application.Abstractions/Access/`.

The administration handlers cover user listing/detail, Pending acceptance, disable/reactivate,
catalogue and direct-grant reads/replacement, audit retrieval, Logto subject relink preview/confirm,
and reconciliation status. They do not expose a generic status setter, role mutation, or Owner
configuration mutation.

Write handlers require a bounded audit reason and pass the authenticated caller's `sub` to the
Infrastructure transaction service. That service rechecks the caller as an active Owner, locks the
target and grants, performs optimistic `xmin` concurrency checking, appends audit events, and commits
once. A failed audit append therefore leaves the target and grants unchanged.

User-list paging follows the public `AccessUserPaging` contract. The reader calculates offsets in `long`,
returns the same successful paged shape with no items when an offset is at or after `totalCount`, and only
converts an offset after proving it is within the result count.

## Related

- Contracts: `../../QuranDashboard.Application.Abstractions/Access/README.md`
- Owner reconciliation: `OwnerReconciliation/README.md`
- API route boundary: `../../../api/QuranDashboard.Api/Controllers/README.md`
