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
