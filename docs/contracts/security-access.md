# Security access — identity and authorization

Index only — defers to the linked code + README, which are the authority. See
[docs/contracts/README.md](./README.md).

## Authoritative sources

- API authentication, interactive identity evidence, and authorization wiring →
  [`Authentication/README.md`](../../Backend/api/QuranDashboard.Api/Authentication/README.md)
- Access routes and HTTP authorization classification →
  [`Controllers/Access/`](../../Backend/api/QuranDashboard.Api/Controllers/Access/),
  [`Controllers/README.md`](../../Backend/api/QuranDashboard.Api/Controllers/README.md)
- Application identity and authorization abstractions →
  [`Security/README.md`](../../Backend/application/QuranDashboard.Application.Abstractions/Security/README.md)
- Permission-catalogue serving/readiness and audit identity projection →
  [`Infrastructure/Access/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Access/README.md)
- Permission-catalogue startup synchronization, health integration, and configuration ownership →
  [`QuranDashboard.Api/README.md`](../../Backend/api/QuranDashboard.Api/README.md)
- Frontend session, route access, and generated permission-catalogue ownership →
  [`core/README.md`](../../Frontend/quran-dashboard-ui/src/app/core/README.md)
- Owner administration, permission-editor safety, and audit identity display →
  [`features/access-admin/README.md`](../../Frontend/quran-dashboard-ui/src/app/features/access-admin/README.md)

**Precedence:** backend code + the nearest backend README define server access; frontend code + its
nearest README define client-side access behaviour.
