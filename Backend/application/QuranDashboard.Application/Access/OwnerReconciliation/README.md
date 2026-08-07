# Owner reconciliation application use case

This use case owns Phase 3 Owner policy and orchestration. Infrastructure supplies the current
normalized configuration, provider identity data, and one transaction-scoped persistence lease.

Owner addition is permitted only for the authenticated interactive caller after validated OIDC
claims provide the matching `sub`, a configured normalized `email`, and `email_verified=true`.
Management API `primaryEmail` is used only to match current provider identity to the local user.
The CLI path cannot add Owners; it can remove only resolved unconfigured Owners and revoke direct
grants after the last-active-Owner check passes. Configured users waiting for their own verified
interactive sign-in are reported as `AwaitingVerifiedSignIn` and do not block another active Owner.
A configured Disabled Owner remains Disabled, but any direct grant still blocks readiness and is
reported for cleanup; reconciliation revokes that grant without reactivating the user.

The use case creates immutable role, grant, audit, and provenance decisions. The infrastructure
lease persists them atomically under the dedicated PostgreSQL advisory lock.
