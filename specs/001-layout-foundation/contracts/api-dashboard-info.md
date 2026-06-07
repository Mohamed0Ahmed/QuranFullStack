# Contract: GET /api/dashboard/info

Returns application metadata for the home page. Values come from the running app — never
fabricated.

## Request

```text
GET http://localhost:5014/api/dashboard/info
```

No auth, no parameters.

## Response 200

```json
{
  "isSuccess": true,
  "message": "تم جلب معلومات التطبيق",
  "data": {
    "appName": "المنهج القرآني",
    "version": "0.1.0",
    "environment": "Development"
  }
}
```

## Field rules

| Field | Source | Rule |
|-------|--------|------|
| `appName` | constant | MUST be «المنهج القرآني» |
| `version` | entry assembly informational version, fallback `"0.1.0"` | Real value; not invented |
| `environment` | `IHostEnvironment.EnvironmentName` | Reflects the running environment |

## Rules

- Controller stays thin (API layer only); inject `IHostEnvironment`; read version via reflection
  on the entry assembly.
- On the frontend, the home page renders explicit loading → success/error states; on error it
  shows a calm message and NO fabricated metadata (FR-030).
