# Dev CLI shortcuts

Short commands to build/run the backend API and Angular dev server from any directory.

## Commands

| Command | What it does |
|---------|----------------|
| `qd-build` | `dotnet build QuranDashboard.sln` for backend changes |
| `qd-api` | `dotnet run --launch-profile https --no-build`; opens Swagger when the API is ready |
| `qd-ui` | `npm run start:https` for the Angular dashboard |
| `export-swagger` | Builds the API (Release) and writes the OpenAPI spec to `Frontend/quran-dashboard-ui/openapi/swagger.json` via the Swashbuckle CLI (`Backend/dotnet-tools.json` manifest); no running server or database needed |
| `check-api-contract` | Runs `export-swagger`, regenerates the frontend API models (`npm run generate:api`) and the static API reference (`npm run docs:api`), then fails with `git diff --exit-code` if any committed generated output is stale. This is the authoritative contract-drift gate wired into CI (`.github/workflows/ci.yml`, `api-contract-drift` job) |
| `check-import-source-strategy` | Source-level gate (no import runs): fails if any importer `ResolveDefault*SourcePath` resolver does not resolve under `resources/import-sources/`, or hard-codes an absolute source path. Wired into the CI `backend-tests` job |
| `security-audit` | Dependency/security + secret/license gate. Blocking + **fail-closed** secret scan: `git grep -e` over tracked files (a git grep error aborts rather than passing), matching strong tokens (AWS/GitHub/Slack/Neon keys, private keys) plus `Password=<value>` filtered case-insensitively against dev-defaults/templates (`SET_VIA_USER_SECRETS`, `Password=postgres`, `<your-password>`, `***REDACTED***`); generated bundles (redoc HTML, ng-openapi models) and the lockfile are excluded, human-authored docs are not. Also blocking: forbidden strong-copyleft license check on frontend prod deps, and `dotnet list package --vulnerable`. Report-only: frontend `npm audit` (pre-existing transitive advisories are owned by the `dependency-audit` workflow). Wired into the CI `security-audit` job |

Typical daily flow:

```bash
qd-build
qd-api
qd-ui
```

After the first successful build, use `qd-api` directly until backend code changes.

## One-time setup (zsh)

Add the scripts folder to your `PATH` in `~/.zshrc`:

```bash
export PATH="/projects/Dashboard/App/Backend/scripts:$PATH"
```

Then reload:

```bash
source ~/.zshrc
```

**Alternative:** aliases instead of `PATH`:

```bash
alias qd-api='/projects/Dashboard/App/Backend/scripts/qd-api'
alias qd-build='/projects/Dashboard/App/Backend/scripts/qd-build'
alias qd-ui='/projects/Dashboard/App/Backend/scripts/qd-ui'
```

## Prerequisites

### Backend (`qd-build`, `qd-api`)

- .NET 10 SDK
- PostgreSQL with the seeded `quran_dashboard` database
- Trusted HTTPS dev certificate:

```bash
dotnet dev-certs https --trust
```

- Database connection in user secrets (do not commit secrets):

```bash
cd Backend/api/QuranDashboard.Api
dotnet user-secrets set "ConnectionStrings:QuranDashboardDb" "Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=<your-password>"
```

### Frontend (`qd-ui`)

- Node.js and npm
- Dependencies installed:

```bash
cd Frontend/quran-dashboard-ui
npm install
```

- Local HTTPS certificates in the frontend project root:

```bash
cd Frontend/quran-dashboard-ui
mkcert -install
mkcert localhost
```

This produces `localhost.pem` and `localhost-key.pem`, used by `npm run start:https`.

## URLs

| Service | URL |
|---------|-----|
| API | `https://localhost:5015` |
| Swagger | `https://localhost:5015/swagger` |
| Health | `https://localhost:5015/api/health` |
| Angular UI | `https://localhost:4200` |

## Troubleshooting

| Problem | Fix |
|---------|-----|
| `command not found: qd-api` | Add `Backend/scripts` to `PATH` or use the full path |
| `qd-api` says the app was not built | Run `qd-build` first |
| Build fails | Run `qd-build` and fix compile errors |
| API won't start | Check PostgreSQL is running and user secrets are set |
| `node_modules not found` | Run `npm install` in `Frontend/quran-dashboard-ui` |
| SSL cert missing for UI | Run `mkcert localhost` in the frontend project |
| Browser shows certificate warning | Trust .NET dev cert and/or mkcert root (`mkcert -install`) |
