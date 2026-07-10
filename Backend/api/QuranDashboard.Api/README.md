# Quran Dashboard API

## Connection String Setup

The database password is **not** committed to source control. Set it via .NET User Secrets or an environment variable.

### Option 1: User Secrets (recommended for local development)

```sh
cd api/QuranDashboard.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:QuranDashboardDb" "Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=<your-password>"
```

### Option 2: Environment Variable

```sh
export ConnectionStrings__QuranDashboardDb="Host=localhost;Port=5432;Database=quran_dashboard;Username=postgres;Password=<your-password>"
```
