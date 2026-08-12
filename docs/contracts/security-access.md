# Security access — identity and authorization

Index only — defers to the linked code and security architecture authority. See
[docs/contracts/README.md](./README.md).

## Authoritative sources

- API authentication, interactive identity evidence, and authorization wiring →
  [`Authentication/`](../../Backend/api/QuranDashboard.Api/Authentication/)
- Access routes and HTTP authorization classification →
  [`Controllers/Access/`](../../Backend/api/QuranDashboard.Api/Controllers/Access/)
- Application identity and authorization abstractions →
  [`Security/`](../../Backend/application/QuranDashboard.Application.Abstractions/Security/)
- Permission-catalogue serving/readiness and audit identity projection →
  [`Infrastructure/Access/`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Access/)
- Permission-catalogue startup synchronization, health integration, and configuration ownership →
  [`QuranDashboard.Api/`](../../Backend/api/QuranDashboard.Api/)
- Frontend session, route access, and generated permission-catalogue ownership →
  [`core/`](../../Frontend/quran-dashboard-ui/src/app/core/)
- Owner administration, permission-editor safety, and audit identity display →
  [`features/access-admin/`](../../Frontend/quran-dashboard-ui/src/app/features/access-admin/)

**Precedence:** backend code defines server access; frontend code defines client-side access
behaviour. `Backend/.architecture/API_GUIDELINES.md` §11 governs the security boundary.
