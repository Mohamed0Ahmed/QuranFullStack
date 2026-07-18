# Feature 033 — Auth, Roles & Permissions — Decision Record (LOCKED)

- **Feature**: `033-auth-roles-permissions`
- **Branch**: `033-auth-roles-permissions` (off `dev`)
- **Status**: **LOCKED** — authoritative. `plan.md` and every Spec Kit artifact
  (`specs/033-*`) derive from this record and must not silently rework it.
- **Date**: 2026-07-18
- **Grounded in**: the feature-033 read-only inspection report (this session). Repo
  paths/line refs below were verified in that report; steady-state truth is the code +
  nearest README per workspace conventions.

## 0. Framing

Logto is the external OIDC IdP and is used for **login + social sign-in ONLY**. The
application **owns** its Users and Roles in its own PostgreSQL and **enforces
authorization in .NET**. The dashboard is **admin-only**. Sign-up is **open** (anyone
can register through Logto); access is gated inside the app by **deny-by-default** plus
an Owner-driven activation step.

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
- **B4 — No role ⇒ zero access.** A user with `RoleId = null` (or non-Active status) has
  **zero permissions**, cannot enter any dashboard admin page, and sees only a
  **"pending activation"** page.
- **B5 — Activation is Owner-driven.** The Owner reviews pending users and **assigns a
  role** from a user-management page. The Owner does **not** create roles from the UI.

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
- **E2 (D6) — Deny-by-default.** A **global fallback authorization policy** requires an
  authenticated user **with a role**. Explicit `[AllowAnonymous]` on: health, `/callback`
  (server side if any), the pending/activation endpoint(s), and any login-related
  endpoint.
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
  `OnForbidden` (403) writes the envelope directly. Mirror the 429 writer shape
  (`Backend/api/QuranDashboard.Api/RateLimiting/RateLimitRejectionWriter.cs:17-34`):
  `HasStarted` guard → `StatusCode` → `ContentType="application/json"` →
  `WriteAsJsonAsync(ApiResponse<object>.Fail(...))`.
- **F3 — Messages.** Add Arabic `ApiMessages.Unauthorized` and `ApiMessages.Forbidden`
  next to the existing consts (`Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:8-10`).
  Property names stay English; `Authorization`/`Bearer` are protocol strings, not
  user-facing messages (`API_GUIDELINES.md:197-198`). Do not invent Quranic content.

## G. Frontend integration (Angular 20, standalone)

- **G1 (D8) — Guarded route tree.** Refactor `/dashboard` into a **guarded parent**:
  `{ path: 'dashboard', canActivate: [authGuard], children: [...] }` so one guard covers
  the subtree (today it is three sibling top-level paths —
  `Frontend/quran-dashboard-ui/src/app/app.routes.ts:21-53`). Add a **public `/callback`**
  route and the **pending page** route **before** the `**` wildcard (`:49-52`, which
  otherwise swallows them). Behavior: unauthenticated → Logto; authenticated-but-no-role
  → pending page.
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
