# Response envelope

Index only — defers to the linked code. See [docs/contracts/README.md](./README.md).

The success/failure envelope shape is the C# `ApiResponse<T>` record; the frontend mirror
is the TypeScript model. This page does **not** restate
fields.

## Authoritative sources

- Backend envelope type → [`Contracts/ApiResponse.cs`](../../Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs)
- Frontend mirror type → [`api-response.model.ts`](../../Frontend/quran-dashboard-ui/src/app/core/data-access/api-response.model.ts)
- Generated payload models (frontend expression of the wire DTOs) → [`core/api/generated/`](../../Frontend/quran-dashboard-ui/src/app/core/api/generated/) — generated from `openapi/swagger.json`; the envelope itself stays hand-written

**Precedence:** `ApiResponse.cs` wins.
