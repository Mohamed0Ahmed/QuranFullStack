# Security access — identity and authorization

Index only — defers to the linked code + README, which are the authority. See
[docs/contracts/README.md](./README.md).

## Authoritative sources

- API authentication and authorization boundary → [`Authentication/README.md`](../../Backend/api/QuranDashboard.Api/Authentication/README.md)
- Access controller routes and HTTP authorization classification → [`Controllers/Access/`](../../Backend/api/QuranDashboard.Api/Controllers/Access/), [`Controllers/README.md`](../../Backend/api/QuranDashboard.Api/Controllers/README.md)
- Application identity and authorization abstractions → [`Security/README.md`](../../Backend/application/QuranDashboard.Application.Abstractions/Security/README.md)
- Frontend session and route access → [`core/README.md`](../../Frontend/quran-dashboard-ui/src/app/core/README.md)
- Owner administration UI → [`features/access-admin/README.md`](../../Frontend/quran-dashboard-ui/src/app/features/access-admin/README.md)

**Precedence:** backend code + the nearest backend README define server access; frontend code + its
nearest README define client-side access behaviour.

## Pointers worth naming

- `GET /api/access/permissions` returns `{ items, assignmentReady }`, not a bare array.
  `assignmentReady` states whether permission assignment can currently be persisted, so a safe read is
  never mistaken for a safe write → [`Access/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Access/README.md)
- Boot-time catalogue synchronization, its non-fatal failure policy, the `permission_catalogue`
  health check, and the `Access:PermissionCatalogueStartupSync` switch →
  [`QuranDashboard.Api/README.md`](../../Backend/api/QuranDashboard.Api/README.md)
- The Owner permission editor fails closed when the catalogue is unreadable or assignment is not
  ready — no save path, no empty replacement set, and only that region degrades →
  [`features/access-admin/README.md`](../../Frontend/quran-dashboard-ui/src/app/features/access-admin/README.md)
