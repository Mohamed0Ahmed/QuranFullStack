# Global Logging / Observability Foundation Implementation Plan

## 1. Scope and Non-Goals

This is a backend-wide foundation for Quran Dashboard / المنهج القرآني logging. It is intentionally small and should become the default convention for all current and future backend features.

Scope:

- Backend-wide logging conventions for API, Application, Infrastructure, DataPipelines/importers, and CLI tools.
- Minimal global exception/request trace enrichment using existing ASP.NET Core request context.
- A project logging convention document.
- Optional small test logging utility if it keeps logging tests focused and readable.
- Words Hub + Unique Words Explorer as the first adopter/example after the global foundation is in place.

Non-goals:

- Serilog migration.
- OpenTelemetry implementation.
- Distributed tracing.
- External log aggregation setup.
- Frontend logging.
- Logging every method.
- Changing `ApiResponse` shape unless existing API conventions clearly support it.
- Broad refactors or unrelated cleanups.

## 2. Current Baseline Summary

Current API startup:

- `Backend/api/QuranDashboard.Api/Program.cs` remains thin and delegates to `AddApplication()`, `AddInfrastructure(builder.Configuration)`, `AddApiServices(builder.Configuration)`, and `UseApiPipeline()`.
- `Backend/api/QuranDashboard.Api/Extensions/ServiceCollectionExtensions.cs` registers controllers, Swagger, health checks, `AddProblemDetails()`, `AddExceptionHandler<GlobalExceptionHandler>()`, and CORS.
- `Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs` calls `app.UseExceptionHandler()` before Swagger, HTTPS redirection, CORS, and `MapControllers()`.

Current logging configuration:

- `Backend/api/QuranDashboard.Api/appsettings.json`
  - `Logging:LogLevel:Default = Information`
  - `Logging:LogLevel:Microsoft.AspNetCore = Warning`
- `Backend/api/QuranDashboard.Api/appsettings.Development.json`
  - Same API logging levels as base settings.
- `Backend/tools/QuranDashboard.DataImporter/appsettings.json`
  - No `Logging` section today; CLI tools primarily use console output and generated reports.

Current `GlobalExceptionHandler` behavior:

- Logs one `Error` using `ILogger<GlobalExceptionHandler>` with exception and `{Path}` only.
- Returns `ApiResponse<object>.Fail(ApiMessages.UnexpectedError)` with HTTP 500 and `application/json` if the response has not started.
- Does not include trace/request IDs in the log or response today.

Current `ApiResponse` convention:

- `ApiResponse<T>` supports `isSuccess`, `message`, `data`, and `errors`.
- API guidelines make this envelope authoritative at the API boundary and require safe localized messages.
- There is no existing explicit metadata/trace field on `ApiResponse<T>`.

Request/trace IDs:

- ASP.NET Core already provides `HttpContext.TraceIdentifier`.
- `System.Diagnostics.Activity.Current` is normally available during ASP.NET Core request processing and can provide a trace identifier, but there is no custom correlation middleware or response header policy currently in this backend.

Duplicate exception logging:

- The only explicit `LogError` found in the API/Application/Infrastructure surface is `GlobalExceptionHandler`.
- Application handlers and Infrastructure readers generally do not catch/log/rethrow unexpected exceptions today.
- `GlobalExceptionHandler` currently logs before checking `Response.HasStarted`; the implementation should avoid producing duplicate boundary logs for exceptions it does not handle.

Existing Words logging audit:

- None. No Words-area logging/observability audit existed when this plan was written.

## 3. Proposed Global Logging Standard

Project-wide rules:

- Use structured placeholders only: `logger.LogInformation("Loaded {feature} {operation} for {kind}", feature, operation, kind);`
- Do not use string interpolation, concatenation, or serialized DTOs in log messages.
- Use stable field names across features. Prefer lower camel case placeholders:
  - `{traceId}`
  - `{requestId}`
  - `{feature}`
  - `{operation}`
  - `{path}`
  - `{method}`
  - `{elapsedMs}`
  - feature-specific safe IDs/counts/modes.
- Never log raw Quranic text, tafsir text, translation bodies, i'rab HTML, source JSON payloads, raw request bodies, raw response bodies, SQL rows, secrets, or raw user search text.
- Emit one `Error` log for unexpected exceptions at the global boundary.
- Use `Information` logs at use-case boundaries only, usually one completion log per meaningful use case.
- Use `Warning` logs for expected abnormal outcomes such as not found, invalid state, validation refusal, provenance mismatch, or source-package problems.
- Use `Debug`/`Trace` only for temporary or specifically needed diagnostics.
- Do not add cache hit/miss logs at `Information`.
- Do not add per-method entry/exit logs.

Recommended common fields:

- `traceId`: `Activity.Current?.TraceId.ToString()` when available.
- `requestId`: `HttpContext.TraceIdentifier` for API requests.
- `feature`: stable feature area, for example `Words`.
- `operation`: stable use-case name, for example `GetUniqueWordsPage`.
- `path`: request path for global/boundary logs.
- `method`: HTTP method for global/boundary logs.
- `elapsedMs`: optional, only when measured cheaply and without adding middleware complexity.
- Safe feature fields: IDs, counts, modes, page/pageSize, source keys, report paths, verdict/check IDs, `hasSearch`.

## 4. Global Exception and Request Tracing Plan

Minimal changes:

- Enrich `GlobalExceptionHandler`'s handled-exception `LogError` with:
  - `traceId`
  - `requestId`
  - `method`
  - `path`
- Keep the client response safe:
  - status code `500`
  - `ApiResponse<object>.Fail(ApiMessages.UnexpectedError)`
  - no stack trace, exception message, SQL details, file paths, or source data.
- Do not add duplicate `LogError` calls in controllers, handlers, readers, repositories, or caching decorators for unexpected exceptions.
- Do not catch/log/rethrow unexpected exceptions in use cases.
- Check `Response.HasStarted` before writing the response and avoid logging a handled-exception `Error` if this handler returns `false` and leaves handling to the server/another handler.
- Do not add custom correlation middleware unless a concrete need appears after using built-in `HttpContext.TraceIdentifier` and `Activity.Current`.
- Do not change `ApiResponse<T>` for trace IDs in this foundation. Because the existing response envelope has no metadata field and API guidelines require stable response shapes, keep responses unchanged unless a separate API contract decision is made.

Implementation note:

- If future API guidance approves exposing a trace identifier, prefer a dedicated envelope metadata field or a response header decision across the API, not a one-off `errors` entry or message suffix.

## 5. Logging Conventions Documentation

Add a new backend architecture guide:

- `Backend/.architecture/LOGGING_GUIDELINES.md`

The document should cover:

- Purpose and non-goals.
- Layer boundaries for logging.
- Standard log levels.
- Required structured logging style.
- Stable field-name catalog.
- Safe and unsafe fields.
- Quranic data logging safety.
- Exception policy and duplicate-log policy.
- DataPipelines/importer logging convention.
- CLI logging versus console/report output.
- Feature adoption checklist.
- Testing recommendations.

Also update:

- `Backend/AGENTS.md`
- `Backend/CLAUDE.md`

Both should reference `Backend/.architecture/LOGGING_GUIDELINES.md` when adding/changing backend logging, error handling, diagnostics, DataPipeline logging, or logging tests.

## 6. Layer-by-Layer Adoption Model

Controllers:

- Normally no routine success logs.
- Keep controllers thin and HTTP-focused.
- Boundary warnings are allowed only when the controller owns a boundary decision that is not already logged by the use case.
- No duplicate exception logs.
- No raw request/query/body logging.

Application handlers/use-cases:

- Primary place for use-case `Information` completion logs.
- Emit one completion log per meaningful operation, not one per internal step.
- Emit `Warning` for expected abnormal outcomes such as not-found, invalid state, invalid paging/sort/kind, or controlled refusal.
- Do not catch/log/rethrow unexpected exceptions.
- Do not log Quranic text or raw search text; use `hasSearch`.
- Adding `ILogger<THandler>` to handlers is acceptable, but keep dependencies small and layer-safe. If the Application project needs `Microsoft.Extensions.Logging.Abstractions`, add only that package and document the reason.

Infrastructure readers/repositories:

- Normally no logs.
- Let database exceptions bubble to the global boundary.
- Optional `Debug` diagnostics only for targeted investigation.
- Never log SQL result rows or loaded entities/DTO collections.

Caching decorators:

- No cache hit/miss logs at `Information`.
- Optional `Debug` only if specifically needed.
- Never include cache keys containing raw search text. Search-filtered Words list reads already bypass cache keys for free-text search and should preserve that safety.

DataPipelines/importers/generators/rebuilders:

- Start/end summary logs are appropriate.
- Include counts, verdicts, report path, safe source package key, source version/hash/check IDs where useful.
- Use `Warning` for validation, provenance, source-package, refusal, and partial-read issues.
- Use `Error` only where the failure is handled and will not also be logged globally by the host boundary.
- Never log Quran text, tafsir text, translation body, full i'rab HTML, source JSON payloads, or large source-derived DTOs.

CLI tools:

- Console output and generated reports remain primary.
- Logs should complement reports and should not duplicate huge report content.
- User-facing CLI output may stay concise; logs should add safe operational context only.

## 7. Quranic Data Safety Policy

Forbidden in logs:

- Quran ayah text.
- Tafsir text.
- Translation body.
- Full i'rab HTML.
- Large DTO collections.
- Raw request bodies.
- Raw response bodies.
- SQL result rows.
- Source JSON payloads.
- Connection strings/secrets.
- Raw user search text.

Safe in logs:

- IDs.
- Counts.
- Safe source keys.
- `surahNumber` / `ayahNumber`.
- `pageNumber` / `pageSize`.
- `mode` / `kind`.
- `hasSearch` instead of raw search.
- Verdict/check IDs.
- `elapsedMs`.
- Report path, when it is a local generated report path and not an upstream secret/source path.

## 8. Testing Plan

Focused tests only:

- Add a `GlobalExceptionHandler` test that verifies:
  - one `Error` log is emitted for a handled unexpected exception;
  - the log carries safe fields such as `traceId`, `requestId`, `method`, and `path`;
  - the response is a safe 500 `ApiResponse<object>` and does not expose the exception message.
- Add negative checks where practical that logs do not contain raw Quran text or raw search text.
- If handler logging tests become repetitive, add a small test logger helper under `Backend/tests/QuranDashboard.Tests/...` rather than pulling in a broad logging test dependency.
- Words first-adopter tests can assert:
  - one `Information` log for success per targeted handler scenario;
  - one `Warning` log for not-found/invalid outcomes where the handler owns the outcome;
  - raw search text is absent and `hasSearch` is present.

Avoid:

- Snapshotting full log messages.
- Exact `elapsedMs` assertions.
- Testing Microsoft logging internals.
- Forcing logging tests on every handler.
- Adding test-only logging complexity to production code.

## 9. Words Feature First Adoption

Apply Words adoption only after the global foundation/doc/exception behavior is in place.

Likely first-adopter files:

- `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordsPage/GetUniqueWordsPageHandler.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordSummary/GetUniqueWordSummaryHandler.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordSurahs/GetUniqueWordSurahsHandler.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordMissingSurahs/GetUniqueWordMissingSurahsHandler.cs`
- `Backend/application/QuranDashboard.Application/Quran/Words/Queries/GetUniqueWordAyahs/GetUniqueWordAyahsHandler.cs`
- Focused tests under `Backend/tests/QuranDashboard.Tests/Quran/Words/`

Recommended Words logs:

- List success `Information`: `feature=Words`, `operation=GetUniqueWordsPage`, `kind`, `sort`, `pageNumber`, `pageSize`, `totalCount`, `itemCount`, `hasSearch`.
- Drilldown summary success `Information`: `feature=Words`, `operation=GetUniqueWordSummary`, `kind`, `uniqueWordId`, safe occurrence/count fields if already returned.
- Mentioned surahs success `Information`: `operation=GetUniqueWordSurahs`, `kind`, `uniqueWordId`, `surahCount`.
- Missing surahs success `Information`: `operation=GetUniqueWordMissingSurahs`, `kind`, `uniqueWordId`, `missingSurahCount`.
- Ayah matches success `Information`: `operation=GetUniqueWordAyahs`, `kind`, `uniqueWordId`, `pageNumber`, `pageSize`, `totalCount`, `itemCount`.
- Word not found `Warning`: `operation`, `kind`, `uniqueWordId`.
- Invalid paging/sort/kind/id `Warning` where applicable: log safe invalid category and numeric bounds, but not raw search text.
- No raw search text; use `hasSearch`.
- No cache hit/miss `Information` logs in `CachedUniqueWordsReader`.

## 10. Implementation Phases

### Phase 1: Documentation Foundation

Files likely touched:

- `Backend/.architecture/LOGGING_GUIDELINES.md`
- `Backend/AGENTS.md`
- `Backend/CLAUDE.md`

Expected risk:

- Low. Documentation-only.

Verification commands:

- No test run required for docs-only.
- Optional: `git diff -- Backend/.architecture/LOGGING_GUIDELINES.md Backend/AGENTS.md Backend/CLAUDE.md`

Review notes:

- Verify the doc stays minimal and does not mandate Serilog, OpenTelemetry, or external infrastructure.
- Verify Quranic data safety rules are explicit enough for future feature work.

### Phase 2: Minimal GlobalExceptionHandler Enrichment

Files likely touched:

- `Backend/api/QuranDashboard.Api/Middleware/GlobalExceptionHandler.cs`
- New focused tests under `Backend/tests/QuranDashboard.Tests/...`
- Optional test project package/reference only if a small local helper is not sufficient.

Expected risk:

- Low to medium. Central API error behavior is touched, but response shape should remain unchanged.

Verification commands:

- `dotnet build Backend/QuranDashboard.sln`
- Focused exception-handler test command, for example:
  - `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter GlobalExceptionHandler`

Review notes:

- Confirm one handled unexpected exception produces one `Error`.
- Confirm response stays safe and localized.
- Confirm no exception message, stack trace, SQL detail, file path, Quranic text, request body, or raw payload is logged or returned.

### Phase 3: Optional Test Logger Utility

Files likely touched:

- A small helper under `Backend/tests/QuranDashboard.Tests/TestSupport/` or another existing test-support location.

Expected risk:

- Low if kept test-only and generic.

Verification commands:

- `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter GlobalExceptionHandler`

Review notes:

- Add this only if Phase 2 or Words logging tests would otherwise duplicate awkward logger-capture code.
- Do not introduce a broad testing framework just for logging assertions.

### Phase 4: Words First-Adopter Logging

Files likely touched:

- The five Words query handlers listed in section 9.
- `Backend/application/QuranDashboard.Application/QuranDashboard.Application.csproj` only if `Microsoft.Extensions.Logging.Abstractions` is needed.
- Focused Words tests under `Backend/tests/QuranDashboard.Tests/Quran/Words/`.

Expected risk:

- Medium. Logging should not change behavior, but constructor dependencies and tests can affect DI setup.

Verification commands:

- `dotnet build Backend/QuranDashboard.sln`
- Focused Words tests, for example:
  - `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj --filter UniqueWords`

Review notes:

- Confirm no raw search text is logged.
- Confirm no cache hit/miss `Information` logs are added.
- Confirm handlers do not catch/log/rethrow unexpected exceptions.
- Confirm logs use stable placeholders and safe fields only.

### Phase 5: Verification and Review

Files likely touched:

- No new files expected unless review findings require small follow-up fixes.

Expected risk:

- Low.

Verification commands:

- `dotnet build Backend/QuranDashboard.sln`
- Focused backend tests changed by this work.
- Full backend test suite if the implementation touches shared test infrastructure or DI broadly:
  - `dotnet test Backend/tests/QuranDashboard.Tests/QuranDashboard.Tests.csproj`

Review notes:

- Perform a clean-code self-check against the logging doc and existing architecture guides.
- Verify no production logging dependency was added to Domain.
- Verify Application logging remains layer-safe.
- Verify `ApiResponse<T>` shape did not change unless separately approved.

## 11. Verification Summary

Expected implementation verification:

- `dotnet build Backend/QuranDashboard.sln`
- Focused global exception handler tests.
- Focused Words tests after Words adoption.
- Full backend test suite if shared test infrastructure, DI registration, or Application constructor patterns are affected.

Do not run migrations for this work.

## 12. Commit and Review Recommendation

Prefer split commits:

1. `Add backend logging guidelines`
   - `Backend/.architecture/LOGGING_GUIDELINES.md`
   - `Backend/AGENTS.md`
   - `Backend/CLAUDE.md`
2. `Enrich global exception logging`
   - `GlobalExceptionHandler` enrichment
   - focused exception-handler test/helper if needed
3. `Adopt logging in Words handlers`
   - Words handler logging
   - focused Words logging tests

This split keeps the global convention, central API behavior, and first feature adoption independently reviewable.
