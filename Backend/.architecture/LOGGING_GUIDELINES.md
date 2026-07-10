# Logging / Observability Guidelines

## Purpose

Define the backend logging, diagnostics, and error-handling boundaries so changes stay consistent, searchable, and safe.

## Non-goals

- No new logging vendor, infrastructure, or tracing stack.
- No Serilog, OpenTelemetry, distributed tracing, or broad refactors.
- No log-only work that changes domain behavior.

## Layer boundaries

- **Domain**: no logging dependency and no side effects for observability.
- **Application**: log use-case milestones, warnings, and expected failures through abstractions.
- **Infrastructure**: normally no routine logs; use targeted `Debug` diagnostics only when specifically needed for I/O, database, file, importer, or integration troubleshooting.
- **API / Host**: log request lifecycle, middleware handling, unhandled exceptions, and startup/shutdown events.

Log once at the boundary that can add the most context. Do not repeat the same failure in every layer.

## Structured logging style

- Use structured message templates, not string concatenation.
- Keep placeholder names stable and descriptive, using lower camel case: `{traceId}`, `{requestId}`, `{feature}`, `{operation}`, `{path}`, `{method}`, `{elapsedMs}`.
- Add feature-specific ids, counts, or modes only when they are safe and useful, for example `{entityId}`, `{itemCount}`, `{mode}`.
- Prefer concise messages with predictable field names.
- Keep casing and meaning consistent across files.

## Safe vs unsafe fields

Safe to log:

- operation names, feature names, ids, counts, status values, durations
- file names, relative paths, line numbers, trace ids, request ids
- exception type and high-level failure context

Avoid logging:

- raw Quran text, ayah text, tafsir bodies, translation bodies, or other source content snippets
- raw request bodies, raw response bodies, SQL rows, source JSON payloads, or other unredacted data dumps
- secrets, tokens, passwords, connection strings, personal data, or raw user search text
- user-facing exception internals

When a field may contain source data, summarize or redact it.

## Quranic data safety

- Log metadata and traceability, not content.
- Prefer source references, counts, and validation summaries over text samples.
- Do not invent, normalize, or silently alter religious content in logs.
- Do not hide data issues; surface missing records, duplicates, and validation failures clearly.

## Exception and duplicate-log policy

- Log an exception once at the handling boundary that can add context.
- If an exception is rethrown, do not log it again in the next layer.
- Use warning/info for expected validation issues; reserve error for unexpected failures.
- Aggregate repeated failures or duplicates into counts and representative references when volume is high.
- Keep stack traces in internal diagnostics when useful, but do not expose raw internals to users.

## DataPipelines and importer logging

- Include a run id, source package, totals, warnings, duplicates, and validation result.
- Log per-file or per-batch progress only when it adds signal.
- Keep raw source content out of logs.
- Write detailed outcomes to the report artifact; logs should support diagnosis, not replace the report.

## CLI logging vs console/report output

- Console output is for concise interactive status.
- Structured logs are for diagnostics.
- Report files are for final audit and validation summaries.
- Do not use console text as the only record of an importer or batch run.

## Feature adoption checklist

- Identify the boundary that owns the log.
- Use a structured template with stable field names.
- Verify every field is safe to emit.
- Redact or summarize Quranic/source data.
- Log handled exceptions once; avoid duplicate logs.
- Add or update report summaries for batch work.
- Add tests for emitted fields and redaction/duplication behavior.

## Testing recommendations

- Unit-test logging helpers and boundary behavior.
- Assert important fields, not exact prose, when possible.
- Cover absence of Quranic content and other sensitive values.
- For importer and DataPipeline work, verify counts, warnings, duplicates, and report generation.
- Keep log assertions focused on stable field names and ownership boundaries.
