# Feature 033 — Auth, Roles & Permissions — Decision Record (LOCKED)

- **Feature**: `033-auth-roles-permissions`
- **Branch**: `033-auth-roles-permissions` (off `dev`)
- **Status**: **LOCKED** — authoritative. `plan.md` and every Spec Kit artifact
  (`specs/033-*`) derive from this record and must not silently rework it.
- **Date**: 2026-07-18
- **Revision (2026-07-18, owner-directed)**: the authorization **posture** changed from
  "admin-only dashboard, deny-by-default" to **public-browse by default with an admin
  layer on top**. Additionally, the **admin surface itself is deferred**: there are no
  admin endpoints/pages in the product yet — the user-management page + endpoints, the
  pending-activation page, and **any** application of `[Authorize(Policy = …)]` arrive
  with future admin features. **Phase 2 ships roles infrastructure only** (entity +
  seed + FK, owner bootstrap, claims transformation + cache, policies registered but
  applied to nothing, public browsing without login). This revision supersedes the
  original §0 framing, **B4/B5 delivery**, **E2 (D6)**, **F2 (403 path)**, **G1 (D8)**,
  and the Phase-2 scope in `plan.md`. The roles-only model, owner bootstrap, claims
  transformation, and envelope contract are unchanged as decisions.
- **Grounded in**: the feature-033 read-only inspection report (this session). Repo
  paths/line refs below were verified in that report; steady-state truth is the code +
  nearest README per workspace conventions.

## 0. Framing

Logto is the external OIDC IdP and is used for **login + social sign-in ONLY**. The
application **owns** its Users and Roles in its own PostgreSQL and **enforces
authorization in .NET**. The product is **public-browse by default with an admin layer
on top**: browsing requires **no login** — anonymous users navigate freely; login (+ a
role) is required **only** for specific admin pages and admin actions. Sign-up is
**open** (anyone can register through Logto); **admin** access is gated inside the app
by **opt-in protection on the admin surfaces** plus an Owner-driven activation step.
There is **no global deny-by-default fallback**. Admin surfaces themselves do **not**
exist yet — they arrive with future admin features; until then **nothing is protected**.

### Quran-data safety (confirmed)
This feature touches **NO Quran data**. It adds a new, additive `Access/` bounded
context. The `QuranDashboardDbContext` Quran `DbSet`s
(`Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/QuranDashboardDbContext.cs:18-49`)
and their EF configurations are untouched. Note: this feature introduces the app's
**first API-side write surface** (login upsert, role assignment) — new for the
`ApiResponse` write contract, not a Quran-data risk.

### Conflict check vs repo reality
Every locked decision below was checked against the inspection report and **matches repo
reality** (reserved auth slot present, packages installed and Angular-20 compatible, no
existing user/identity concept, envelope + pipeline shape as cited). **No conflicts.**
Two facts that are *state*, not conflicts: (1) no `docs/feature-033-*` or `specs/033-*`
existed before this record; (2) `Microsoft.AspNetCore.Authentication.JwtBearer` is not
yet referenced and must be added in Phase 1 (not in this planning pass).

---

## A. Authentication (Logto)

- **A1 (D1) — Validate a Logto ACCESS TOKEN, not the ID token.** JwtBearer validates a
  Logto access token whose **audience = a registered Logto API resource** (resource
  indicator = our API). The ID token (audience = SPA `appId`) is **not** validated
  server-side.
- **A2 — Authority / JWKS.** `Authority` = the Logto issuer (`https://<endpoint>/oidc`);
  signing keys are **auto-discovered** via OIDC discovery (`jwks_uri`). No manual key
  handling.
- **A3 (D2) — `email` comes from the Logto userinfo endpoint** (or an explicitly
  configured token claim) during first-login provisioning. The access token does **not**
  carry `email` by default — do not assume a token `email` claim.
- **A4 — Identity key.** The stable join key between Logto and the local `Users` row is
  the Logto **`sub`** claim (stored as `Users.LogtoSub`, unique).

## B. User provisioning & lifecycle

- **B1 — Open sign-up.** Logto sign-up is **not** locked. Anyone may register.
- **B2 — Auto-provision on first login.** On the first successful login, **upsert** a
  local `Users` row keyed by Logto `sub`. Default new user: `RoleId = null`,
  `Status = Pending`.
- **B3 — Owner bootstrap (exception).** If the authenticated user's email equals config
  `Auth:BootstrapOwnerEmail` (value: `mahmmaad96@gmail.com`), provision as the **Owner**
  role with `Status = Active`. The owner email lives in **configuration**, not
  hard-coded.
- **B4 (REVISED) — No role ⇒ no ADMIN access; browsing stays open.** A user with
  `RoleId = null` (or non-Active status) has **no admin permissions**: they cannot enter
  any **admin** page or perform admin actions once those exist. Public browsing is
  unaffected — they browse exactly like an anonymous visitor. The **"pending
  activation"** page (Arabic, shown when a role-less logged-in user opens an admin
  route) is **deferred** to the first admin feature — no admin routes exist yet.
- **B5 — Activation is Owner-driven.** The Owner reviews pending users and **assigns a
  role** from a user-management page. The Owner does **not** create roles from the UI.
  **Delivery deferred**: the user-management page + its endpoints ship with a future
  admin feature, not in Phase 2.

## C. Authorization model (role-only, fixed set)

- **C1 — Role-only, ONE role per user.** `Users.RoleId` is a **nullable** FK → `Roles`.
- **C2 — Fixed role set, seeded.** Roles are a fixed set added via **seeding** (incl.
  Owner). The Owner assigns an existing role to a user but does **not** create roles.
- **C3 — Capabilities defined IN CODE by role name.** Each role's capabilities are
  enforced in .NET, keyed by role **name**. There are **no** `permissions` or
  `role_permissions` tables now.
- **C4 — Enforcement style.** Declarative `[Authorize(Policy = "…")]` on thin
  controllers + policy handlers in DI — no authorization logic in controllers
  (`Backend/.architecture/BACKEND_STRUCTURE.md:218-236`).

## D. Data model & placement (`Access/` bounded context)

- **D1 — `Users`**: `Id` (PK, `ValueGeneratedOnAdd`), `LogtoSub` (**unique index** — the
  login join key), `Email` (unique), `UserName`, `DisplayName`, `Title`, `RoleId`
  (nullable FK → `Roles`), `Status` (enum: `Pending` / `Active` / `Disabled`), audit
  (`CreatedAt`, …).
- **D2 — `Roles`**: `Id`, `Name` (unique), `DisplayName`. Fixed set, seeded. Capabilities
  enforced in code by `Name`.
- **D3 — Naming/conventions.** snake_case tables/columns applied **manually** per config
  (no naming-convention plugin), mirroring
  `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Quran/Words/Morphology/QuranLemmaConfiguration.cs:5-40`
  (`.ToTable("users")`, `.HasColumnName(...)`, `id` PK `ValueGeneratedOnAdd`,
  `HasIndex(...).IsUnique()`, `HasOne/WithMany/HasForeignKey`).
- **D4 — Placement** (report §3.3):
  - Domain entities → `Backend/domain/QuranDashboard.Domain/Access/…`
  - Identity/authz contracts → `Backend/application/QuranDashboard.Application.Abstractions/Security/`
    (existing empty folder) — e.g. `ICurrentUser`, `IUserProvisioningService`.
  - Use cases → `Backend/application/QuranDashboard.Application/Access/…`
  - EF configs → `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/…`
  - `DbSet`s → `QuranDashboardDbContext.cs` alongside `:18-49`; auto-registered via
    `ApplyConfigurationsFromAssembly` (`QuranDashboardDbContext.cs:51-55`).
  - Migration → the **active** `Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/`
    dir via `scripts/add-mig` (`--output-dir Migrations`). The
    `…/Persistence/Migrations/` dir is stale/empty — do not use it.

## E. Backend enforcement & pipeline

- **E1 (D5) — Role loading via `IClaimsTransformation` + short-TTL cache keyed by `sub`.**
  MUST be **idempotent** (it may run multiple times per request — never duplicate
  claims). MUST **evict the cached entry immediately** when a user's role changes (e.g.
  Owner assigns/changes a role).
- **E2 (D6, REVISED) — Public-by-default, opt-in protection.** There is **NO global
  fallback authorization policy**. Endpoints are **public by default**; protection is
  **opt-in** via explicit `[Authorize(Policy = "…")]` applied **only where needed** —
  future admin endpoints. All existing read/browse endpoints **stay public**: no
  `[Authorize]`, and no `[AllowAnonymous]` is needed because nothing falls back to a
  policy. **In Phase 2 the role-based named policies are REGISTERED but applied to
  NOTHING** — the first `[Authorize(Policy = …)]` application lands with the first
  admin feature.
- **E3 — Pipeline insertion.** Insert `app.UseAuthentication();` then
  `app.UseAuthorization();` at the **reserved slot**
  `Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs:21` — **after**
  `app.UseCors("AngularDev")` (`:20`), **before** `app.UseRateLimiter()` (`:24`) and
  `app.MapControllers()` (`:25`). Keep the `:22-23` rationale comment.
- **E4 — Service registration.** Register
  `AddAuthentication().AddJwtBearer(...)` + `AddAuthorization(...)` in
  `ServiceCollectionExtensions.AddApiServices` **between** `AddCors` (ends `:81`) and
  `AddRateLimiting(configuration)` (`:82`). Factor into a self-contained
  `Authentication/` (or `Auth/`) registration + options + validator, **mirroring the
  RateLimiting pattern** (`RateLimiting/RateLimitingRegistration.cs:21-30`,
  `RateLimitingOptions.cs:12-14`).
- **E5 — Package.** `Microsoft.AspNetCore.Authentication.JwtBearer` (net10.0) must be
  added — a deliberate **Phase-1** step. **Not** added in this planning pass.

## F. API error contract (401 / 403)

- **F1 (D7) — Reuse the `ApiResponse` failure envelope.** 401 and 403 emit
  `ApiResponse<object>.Fail(...)` — **not** framework problem-details — per the mandate
  in `Backend/.architecture/API_GUIDELINES.md:132-134` and status rules `:93-94`.
- **F2 — Wiring.** Via `JwtBearerEvents`: `OnChallenge` (401) MUST call
  `ctx.HandleResponse()` to suppress the default empty body, then write the envelope;
  `OnForbidden` (403) writes the envelope directly. **403 wiring deferred**: with no
  policy applied anywhere (E2 revised) no 403 path exists — the `OnForbidden` writer +
  `ApiMessages.Forbidden` land with the first protected endpoint. Mirror the 429 writer shape
  (`Backend/api/QuranDashboard.Api/RateLimiting/RateLimitRejectionWriter.cs:17-34`):
  `HasStarted` guard → `StatusCode` → `ContentType="application/json"` →
  `WriteAsJsonAsync(ApiResponse<object>.Fail(...))`.
- **F3 — Messages.** Add Arabic `ApiMessages.Unauthorized` and `ApiMessages.Forbidden`
  next to the existing consts (`Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:8-10`).
  Property names stay English; `Authorization`/`Bearer` are protocol strings, not
  user-facing messages (`API_GUIDELINES.md:197-198`). Do not invent Quranic content.

## G. Frontend integration (Angular 20, standalone)

- **G1 (D8, REVISED) — Public routes by default; guard ONLY admin routes.** The general
  `/dashboard` browse routes carry **no auth guard** — the app is browsable without
  login (the Phase-1 blanket `authGuard` on the `/dashboard` parent is **removed** in
  Phase 2). A reusable **auth+role guard** exists but is **attached to nothing** —
  future admin routes (user management) attach it. Keep the **public `/callback`**
  route registered **before** the `**` wildcard. The pending-activation route/page is
  **deferred** with the admin surface. Anonymous users are never prompted to log in
  while browsing; login stays on-demand via the navbar sign-in.
- **G2 (D9) — Bearer via library interceptor.** Attach the token with
  `angular-auth-oidc-client`'s functional `authInterceptor()` + `secureRoutes: [apiBaseUrl]`.
  Interceptor order in `app.config.ts:15`:
  `[secureUrlInterceptor, authInterceptor, devLatencyInterceptor]` (auth **after** the
  origin guard). Keep IdP login/refresh traffic **off** the guarded `HttpClient`
  (`secure-url.interceptor.ts:37-43` blocks foreign origins).
- **G3 — Bootstrap wiring.** `provideAuth(...)` (built from
  `buildAngularAuthConfig({...})`, `@logto/js`) is appended to the `appConfig.providers`
  array (`app.config.ts:10-18`).
- **G4 — Environment config.** Add `logto: { endpoint, appId, redirectUri,
  postLogoutRedirectUri, scope, resource }` to **BOTH** env files
  (`environment.ts:1-5`, `environment.development.ts:1-6`) and introduce a **typed
  `Environment` interface** to prevent drift (there is none today; `angular.json:89-94`
  swaps the whole file per build config, no per-key overlay). Dev redirect URIs must be
  `https://localhost:<port>` (dev server is SSL).
- **G5 — Auth UI.** Hard-coded **Arabic** strings (no i18n framework exists). RTL is
  inherited from the document root (`src/index.html:2` `dir="rtl"`). Sign-in/sign-out
  live in the navbar `.actions` block (`top-navbar.component.html:124-173`) + its mobile
  twin.

## H. Governance constraints (record, do not action here)

- **H1 — Migrations.** Never hand-write migrations/`.Designer.cs`/snapshot. Add only via
  `scripts/add-mig` **on explicit request**. Apply via `scripts/update-db`. **Railway does
  NOT auto-migrate** (`api/…/Program.cs` has no `Migrate()`), so the production apply of
  the auth migration(s) is a **deliberate manual step**.
- **H2 — First write surface.** Login upsert and role assignment are the app's first
  API-side writes; they must conform to the `ApiResponse` contract
  (`Backend/.architecture/API_GUIDELINES.md`).
- **H3 — PR flow.** One PR per phase, into `dev`, owner reviews. Never PR to `main`.

## I. Explicitly deferred (NOT now — additive later)

- **I1 —** No `permissions` / `role_permissions` tables. If flexible per-permission or
  multi-role access is needed later, add a permission model **then** (additive; do not
  build now).
- **I2 —** No per-user direct grants/overrides (role-only).
- **I3 —** No per-user rate limiting (the reserved rationale mentions future per-user
  keying; out of scope).
- **I4 (added by the 2026-07-18 revision) —** The **admin surface** is deferred to
  future admin features: user-management endpoints + page, the pending-activation
  route/page, any `[Authorize(Policy = …)]` application, and the 403 `OnForbidden`
  envelope writer + `ApiMessages.Forbidden`. Phase 2 delivers the roles infrastructure
  those features will attach to.
