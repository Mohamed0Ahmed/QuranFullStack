# Contract: GET /api/health

Reports overall service health plus dependency checks (at least the database). Used by the
footer status indicator.

## Request

```text
GET http://localhost:5014/api/health
```

No auth, no parameters.

## Response 200 — healthy

```json
{
  "isSuccess": true,
  "message": "الخدمة تعمل بشكل سليم",
  "data": {
    "status": "healthy",
    "checks": [
      { "name": "database", "status": "healthy" }
    ]
  }
}
```

## Response 200 — database down

```json
{
  "isSuccess": true,
  "message": "الخدمة تعمل مع وجود تنبيهات",
  "data": {
    "status": "unhealthy",
    "checks": [
      { "name": "database", "status": "unhealthy" }
    ]
  }
}
```

- The HTTP call itself still succeeds (`isSuccess: true`) — it is a *report*; the `status` field
  conveys health. `status` ∈ `healthy | unhealthy | degraded`.

## Rules

- Output MUST NOT include connection strings, hosts, credentials, SQL, or exception text.
- Implemented via `HealthCheckService` + `AddDbContextCheck<QuranDashboardDbContext>("database")`
  (see `research.md` R3). The controller maps the result into the envelope `data`.
- If the frontend request itself fails (network/5xx), the footer shows a calm error state with a
  retry control — it does NOT display a fabricated "healthy" status.
