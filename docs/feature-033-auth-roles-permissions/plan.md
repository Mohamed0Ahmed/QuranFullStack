# Feature 033 — Auth, Roles & Permissions — Phased Plan

- **Feature**: `033-auth-roles-permissions` · **Branch**: `033-auth-roles-permissions`
- **Status**: high-level plan. Authoritative decisions live in
  [`decision-record.md`](./decision-record.md) (LOCKED) — this plan sequences them.
- **Grounded in**: the feature-033 inspection report (this session); repo line refs below
  were verified there.
- **Consumption**: Spec Kit `/speckit.specify` reads this **one phase at a time**. Each
  phase is self-contained: it names its objective, scope/non-goals, exact affected files,
  the locked decisions it depends on, tests, acceptance criteria, and a single PR
  boundary. A cheaper model implements each phase from the Spec Kit artifacts derived
  here, so phase boundaries must stay unambiguous.

## Shared references (repo reality)

| Area | Path / anchor |
|---|---|
| Pipeline reserved auth slot | `Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs:21` (after CORS `:20`, before RateLimiter `:24` / MapControllers `:25`) |
| Service registration | `Backend/api/QuranDashboard.Api/Extensions/ServiceCollectionExtensions.cs` — `AddCors` `:62-81`, `AddRateLimiting` `:82` |
| Cross-cutting pattern to mirror | `Backend/api/QuranDashboard.Api/RateLimiting/` (`RateLimitingRegistration.cs:21-30`, `RateLimitingOptions.cs:12-14`, `RateLimitRejectionWriter.cs:17-34`) |
| Error envelope | `Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs:20-25`; messages `Common/ApiMessages.cs:8-10`; contract `Backend/.architecture/API_GUIDELINES.md:93-94,132-134` |
| EF | `…/Persistence/QuranDashboardDbContext.cs:18-49,51-55`; provider `…/DependencyInjection/PersistenceDependencyInjection.cs:9-15`; config template `…/Configurations/Quran/Words/Morphology/QuranLemmaConfiguration.cs:5-40`; active migrations `…/Infrastructure/Migrations/`; `scripts/add-mig`, `scripts/update-db` |
| Structure conventions | `Backend/.architecture/BACKEND_STRUCTURE.md:87-103,134-143,218-236` |
| FE bootstrap/providers | `Frontend/quran-dashboard-ui/src/app/app.config.ts:10-18` (interceptors `:15`); `src/main.ts:5` |
| FE routing | `src/app/app.routes.ts:21-53` (wildcard `:49-52`) |
| FE HTTP guard | `src/app/core/data-access/secure-url.interceptor.ts:15-31,37-43`; api pattern `…/features/words/data-access/word-types.api.ts:49-56` |
| FE env | `src/environments/environment.ts:1-5`, `environment.development.ts:1-6`; `angular.json:89-94` |
| FE UI/RTL | `src/index.html:2`; navbar `…/core/layout/top-navbar/top-navbar.component.html:124-173`; `…/core/navigation/nav-items.ts:9-21` |
| Versions (verified) | Angular `20.3.24`; `@logto/js 6.1.2`; `angular-auth-oidc-client 21.0.2` (peer `@angular/* >=20`) |

## Phase overview & boundary rationale

- **Phase 1 — Authentication end-to-end.** Prove the full Logto → Angular → .NET
  round-trip: login works, the .NET API validates the access token, users are
  provisioned on first login (as `Pending`, `RoleId = null`). Enforcement is **minimal
  and non-breaking** — authentication is wired but the global deny-by-default lockdown and
  role gating are **not** switched on, so currently-anonymous endpoints keep working.
- **Phase 2 — Roles & role-based authorization.** Introduce the `Roles` table + seed
  (fixed set incl. Owner), Owner-by-email bootstrap, the deny-by-default fallback policy,
  role checks, the pending/activation gate, and Owner role-assignment + user-management.

**Why Owner bootstrap is in Phase 2 (dependency, resolved):** the Owner bootstrap assigns
the **Owner role**, which requires the `Roles` table — that table lands in Phase 2.
Therefore in **Phase 1 every user, including the owner email, is provisioned
`RoleId = null` / `Status = Pending`** (Phase 1 has no role concept). Phase 2 adds the
`Roles` seed and extends the (idempotent, upsert-per-login) provisioning with the
owner-email exception; when the owner next logs in under Phase 2, the upsert re-evaluates
and upgrades them to `Owner` / `Active`. We deliberately do **not** pull a minimal `Roles`
seed into Phase 1 — it would fragment the role model across two phases for no gain, since
nothing in Phase 1 enforces roles. This keeps Phase 1 a clean, demonstrable
authentication slice.

---

# Phase 1 — Authentication end-to-end

## Objective & final behavior
A visitor can sign in through Logto from the Angular app; the SPA obtains a Logto access
token (audience = our API resource) and attaches it to every API call; the .NET API
validates that token against Logto (JWKS auto-discovered). On the first successful login
the backend upserts a local `Users` row keyed by Logto `sub` (email fetched from Logto
userinfo), with `RoleId = null` and `Status = Pending`. The SPA can read "who am I / what
is my status" from the API. Sign-out works. **No endpoints are locked down beyond, at
most, requiring an authenticated user on the new `Access` endpoint(s);** existing public
endpoints keep working.

## Scope (in)
- **FE — config & bootstrap.** Typed `Environment` interface + `logto: { endpoint, appId,
  redirectUri, postLogoutRedirectUri, scope, resource }` in **both** env files
  (`environment.ts:1-5`, `environment.development.ts:1-6`). `provideAuth(...)` (from
  `buildAngularAuthConfig`) appended to `appConfig.providers` (`app.config.ts:10-18`).
- **FE — token attach.** `authInterceptor()` added to `withInterceptors([...])`
  (`app.config.ts:15`) as `[secureUrlInterceptor, authInterceptor, devLatencyInterceptor]`
  with `secureRoutes: [apiBaseUrl]`. IdP traffic stays off `HttpClient`.
- **FE — routing & UI.** Public `/callback` route added **before** the `**` wildcard
  (`app.routes.ts:49-52`). Refactor `/dashboard` into a guarded **parent+children** tree
  with an `authGuard` that, **in Phase 1, only requires authentication** (unauthenticated
  → Logto). Sign-in / sign-out controls in the navbar `.actions` block
  (`top-navbar.component.html:124-173`) + mobile twin. Post-callback, the SPA calls the
  `Access` "me" endpoint and stores the result. Arabic strings; RTL inherited.
- **BE — package.** Add `Microsoft.AspNetCore.Authentication.JwtBearer` (net10.0).
- **BE — authentication registration.** New `Authentication/` registration (mirroring
  `RateLimiting/`): `AddAuthentication().AddJwtBearer(...)` with `Authority = Logto issuer`
  and `Audience = API resource`; options bound + validated
  (`AddOptions<T>().Bind(GetSection(SectionName)).ValidateOnStart()` +
  `IValidateOptions<T>` validator). Registered between `AddCors` (`:81`) and
  `AddRateLimiting` (`:82`).
- **BE — pipeline.** Insert `UseAuthentication()`/`UseAuthorization()` at
  `WebApplicationExtensions.cs:21` (reserved slot). `UseAuthorization` present but with
  **no global fallback policy yet** (Phase 2).
- **BE — 401 envelope.** `JwtBearerEvents.OnChallenge` (call `HandleResponse()`) writes
  `ApiResponse<object>.Fail(ApiMessages.Unauthorized)`; add the Arabic
  `ApiMessages.Unauthorized` const. (403/`OnForbidden` is meaningful once policies exist —
  may be stubbed here, fully exercised in Phase 2.)
- **BE — `Access` context (Users only).** Domain `Access/` `User` entity + `Status` enum
  (`Pending`/`Active`/`Disabled`); `IUserProvisioningService` + `ICurrentUser` contracts
  in `Application.Abstractions/Security/`; provisioning use case in `Application/Access/`;
  EF `UserConfiguration` in `…/Configurations/Access/`; `DbSet<User>` in
  `QuranDashboardDbContext`. One migration (`AddAccessUsers`) via `scripts/add-mig`.
- **BE — provisioning + "me" endpoint.** An `[Authorize]` (authenticated-only) endpoint
  (e.g. `GET /api/access/me`, and/or a `POST` login-callback) that upserts by `sub`
  (email via Logto userinfo) and returns `{ sub, email, displayName, status, roleId }` in
  the `ApiResponse` envelope. This is the Phase-1 write surface and the Phase-2 gate's
  data source.

## Non-goals (out — deferred to Phase 2)
- `Roles` table, role seed, Owner-by-email bootstrap (owner is `Pending`/`null` in P1).
- Global **deny-by-default** fallback policy and any `[Authorize(Policy=...)]` role checks.
- `IClaimsTransformation` role loading + cache (no roles to load yet).
- Pending/activation **page** and the authenticated-but-no-role redirect.
- User-management page and role assignment.
- Locking down existing public endpoints.

## Depends on (locked decisions)
A1–A4 (access-token validation, Authority/JWKS, email via userinfo, `sub` key),
B1–B2 (open sign-up, upsert `Pending`/`null`), D1/D3/D4 (Users model, snake_case,
placement), E3–E5 (pipeline slot, registration neighborhood, JwtBearer package),
F1–F3 (401 envelope + Arabic message), G2–G5 (interceptor, provideAuth, env, Arabic UI),
plus G1 **partially** (route refactor + `/callback` + authenticated-only guard; the
no-role→pending behavior is Phase 2), H1 (migration governance).

## Tests
- **BE unit/integration** (Testcontainers per repo test conventions): token with valid
  `aud`/issuer → 200 on the `me` endpoint and a `Users` row exists (`Pending`/`null`);
  first vs repeat login is **idempotent** (no duplicate rows); missing/invalid/expired
  token → **401 with the `ApiResponse.Fail` envelope** (camelCase `isSuccess:false`),
  **not** problem-details; email is populated from userinfo. Existing public endpoints
  still return 200 without a token.
- **FE unit** (Vitest): `authInterceptor` attaches `Authorization: Bearer` only to
  `apiBaseUrl` requests and never to foreign origins; `authGuard` redirects an
  unauthenticated user to login; `/callback` resolves and lands on the dashboard.

## Acceptance criteria
- Real login via Logto from `https://localhost:<port>` completes through `/callback` and
  reaches the dashboard; sign-out returns to the origin root.
- API calls carry a valid Bearer access token; the API validates it against Logto.
- First login creates exactly one `Users` row (`sub`, email, `Pending`, `RoleId=null`);
  the owner email is **also** `Pending`/`null` in Phase 1 (documented, expected).
- 401 responses use the shared failure envelope with an Arabic message.
- No currently-anonymous endpoint became inaccessible.
- `dotnet build` + backend tests green; FE build + tests green (report after the run;
  migration applied locally only, `scripts/update-db`, never against remote).

## Commit / PR boundary
**One PR** into `dev` titled ~`feat(auth): Phase 1 — Logto authentication end-to-end`.
Includes FE auth wiring, BE JwtBearer + `Access.Users` + provisioning + `me` endpoint +
one migration. Owner reviews. Do not open against `main`.

---

# Phase 2 — Roles & role-based authorization

## Objective & final behavior
The system enforces **role-based, deny-by-default** authorization. A fixed set of `Roles`
(incl. `Owner`) is seeded. The owner email is bootstrapped to `Owner`/`Active`. An
authenticated user with **no role / non-Active status** is blocked from all admin pages
and sees a **pending activation** page; the API rejects them with 403 (envelope). The
Owner sees a **user-management page** listing pending/all users and **assigns a role**;
the change takes effect immediately (cache eviction). Role capabilities are enforced in
.NET by role name.

## Scope (in)
- **BE — `Roles`.** Domain `Access/` `Role` entity; EF `RoleConfiguration`
  (`…/Configurations/Access/`); `DbSet<Role>`; `Users.RoleId` FK → `Roles`
  (`HasOne/WithMany/HasForeignKey`, nullable). Migration `AddAccessRoles` (+ the FK) via
  `scripts/add-mig`. **Seed** the fixed role set (incl. `Owner`) — seeding mechanism per
  repo convention (data seeding in a migration or a startup seeder; decide in the spec,
  respecting "Railway does not auto-migrate", H1).
- **BE — Owner bootstrap.** Extend provisioning (B3): if `email ==` config
  `Auth:BootstrapOwnerEmail`, set `RoleId = Owner`, `Status = Active`. Idempotent across
  logins.
- **BE — role loading (E1/D5).** `IClaimsTransformation` that loads the user's role by
  `sub` into claims, backed by a **short-TTL cache** keyed by `sub`; **idempotent** (no
  duplicate claims across multiple per-request invocations); **evict immediately** on role
  change.
- **BE — deny-by-default (E2/D6).** Global **fallback authorization policy** requiring an
  authenticated user **with a role**; `AddAuthorization` fallback + named role policies
  registered next to the Phase-1 auth registration. Explicit `[AllowAnonymous]` on health,
  `/callback`-related, login, and the pending/activation endpoint(s). `[Authorize(Policy=…)]`
  applied to the admin/data controllers.
- **BE — 403 envelope (F2).** `JwtBearerEvents.OnForbidden` (and policy failure) writes
  `ApiResponse<object>.Fail(ApiMessages.Forbidden)`; add the Arabic `ApiMessages.Forbidden`.
- **BE — user management.** Use cases + endpoints (Owner-only policy) to **list users**
  (esp. `Pending`) and **assign/change a user's role** (and optionally set `Status`); on
  assignment, evict that user's role cache (E1).
- **FE — gating (G1 completion).** Tighten `authGuard`: authenticated **but no role /
  `Pending`** → redirect to the **pending activation page** (new public/authenticated
  route registered before the `**` wildcard). Add the pending page (Arabic).
- **FE — user-management page.** Owner-only page listing pending/all users with a
  role-assignment control, consuming the new endpoints; surfaced via a nav entry
  (`nav-items.ts` `actions`/appropriate group) visible to the Owner role.

## Non-goals (out)
- `permissions` / `role_permissions` tables, per-user grants, multi-role (I1–I2 —
  additive later only if needed).
- Role creation/editing from the UI (roles are a fixed seeded set; Owner assigns only).
- Per-user rate limiting (I3).
- Self-service role requests / email notifications (not in scope unless specified later).

## Depends on (locked decisions)
C1–C4 (role-only, fixed seeded set, capabilities-in-code, attribute+policy enforcement),
B3–B5 (owner bootstrap, no-role⇒zero-access, Owner-driven activation), D2 (Roles model),
E1 (`IClaimsTransformation` + cache + eviction), E2 (deny-by-default), F2–F3 (403 envelope
+ message), G1 (pending gate completion). Hard dependency on **Phase 1** (Users,
provisioning, JwtBearer, pipeline slot).

## Tests
- **BE**: role seed present after migration; owner-email login yields `Owner`/`Active`;
  non-owner stays `Pending`/`null`. Deny-by-default: authenticated **no-role** → **403**
  envelope on a protected endpoint; `[AllowAnonymous]` endpoints (health, pending) reachable
  without a role. `IClaimsTransformation` **idempotency** (invoked twice → single role
  claim) and **immediate eviction** (assign role → next request sees the new role without
  waiting for TTL). Owner-only endpoints reject non-owner roles (403).
- **FE**: authenticated-but-no-role is routed to the pending page and cannot reach admin
  routes; an Active-with-role user reaches the dashboard; user-management assign flow calls
  the endpoint and reflects the new state.

## Acceptance criteria
- Fixed roles seeded; owner (`mahmmaad96@gmail.com`) is `Owner`/`Active` after login.
- A new user lands on the **pending activation** page and the API denies admin access with
  a 403 envelope.
- Owner assigns a role from the user-management page; the user gains access on their next
  request (no stale cache).
- All admin/data controllers enforce `[Authorize(Policy=…)]`; `[AllowAnonymous]` set only
  on health, login/callback, and pending endpoints.
- `dotnet build` + tests green; FE build + tests green; migration(s) applied locally only,
  production apply is a deliberate manual `scripts/update-db` step (H1).

## Commit / PR boundary
**One PR** into `dev` titled ~`feat(auth): Phase 2 — roles & deny-by-default authorization`.
Includes `Access.Roles` + seed + `Users.RoleId` migration, Owner bootstrap,
`IClaimsTransformation` + cache, deny-by-default policies, 403 envelope, user-management
endpoints + page, FE pending gate. Owner reviews. Never against `main`.

---

## Cross-phase constraints (both PRs)
- **No Quran data touched** — `Access/` is additive; verify the Quran `DbSet`s/configs are
  unchanged (decision-record §0).
- **Migrations**: only via `scripts/add-mig` on explicit request; never hand-write;
  production apply is manual (Railway does not auto-migrate) — H1.
- **`ApiResponse` write contract** governs the new write endpoints — H2,
  `Backend/.architecture/API_GUIDELINES.md`.
- **Branching**: feature branch → PR into `dev`; releases to `main` are a separate,
  explicit boundary — H3.
- **Spec Kit**: derive `specs/033-auth-roles-permissions/` artifacts per phase from this
  plan; do not restate decisions — defer to `decision-record.md`.
