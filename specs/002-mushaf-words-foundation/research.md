# Phase 0 Research — Quran Mushaf Words & Layout Data Foundation

All items below were settled before planning (most in the companion plan `docs/manhaj-qurani-mushaf-words-layout-data-foundation-plan.md` and the data report). This file records each as a Decision / Rationale / Alternatives so the implementer needs no further investigation. **No NEEDS CLARIFICATION remain.**

---

### R1 — Import mechanism: custom console pipeline (not EF `HasData`)

- **Decision**: A custom, repeatable import pipeline run from a console host; EF migrations create **schema only**.
- **Rationale**: ~99,668 rows (esp. 83,668 words with several Arabic text columns) would bloat EF migration snapshots, can't be validated, and must be re-runnable. This is *data*, not *schema*.
- **Alternatives**: `HasData` (rejected — migration bloat, no validation, no re-run); raw SQL scripts (rejected — no validation/clean-arch fit; but `COPY` is used *inside* the pipeline); runtime JSON reads (rejected — defeats the DB).

### R2 — Bulk insert: Npgsql binary `COPY`

- **Decision**: Persist the 83,668-row table (and others) via Npgsql binary `COPY` within one transaction.
- **Rationale**: Fastest path for large inserts; keeps the whole load atomic.
- **Alternatives**: EF `AddRange`/`SaveChanges` (rejected — too slow/memory-heavy at this volume); per-row INSERT (rejected).

### R3 — Source location: manifest-described staging tree

- **Decision**: Importer reads `resources/import-sources/quran-foundation/` guided by `manifest.json` (per-file role, expected count, optional sha256). Path is configurable (`--source`, default to the staging tree).
- **Rationale**: One stable, self-describing root; fail-fast on any missing file / count / checksum mismatch gives the traceability the Quranic-data-safety rule requires. Large data stays out of the Backend repo.
- **Alternatives**: read the original scattered `resources/{mushaf,words,metadata}` (rejected — no single contract); commit data into the Backend repo (rejected — ~40 MB of source in app repo).

### R4 — Re-run safety: refuse-unless-empty + `--force`

- **Decision**: Default run aborts if any target table is non-empty; `--force` performs an atomic truncate-of-all-five-then-reload in one transaction.
- **Rationale**: Prevents accidental duplication/partial overwrite of Quran data; `--force` makes intentional reloads reproducible. (Locked clarification.)
- **Alternatives**: upsert-by-key (rejected for v1 — more complex, unnecessary for immutable data); silent truncate-always (rejected — destructive by default).

### R5 — `quran_words` primary key = source id (1..83,668), assigned not generated

- **Decision**: PK is the source `id` with `ValueGeneratedNever`; it also equals mushaf reading order. `quran_surahs.surah_number` likewise assigned.
- **Rationale**: Stable, meaningful, and lets "next/previous word" be `id ± 1`; layout `first/last_word_id` reference it directly.
- **Alternatives**: surrogate identity column + separate source-id column (rejected — redundant; loses the reading-order = PK property).

### R6 — Denormalize page/line/order onto `quran_words`

- **Decision**: Store `page_number`, `line_number`, `line_word_order` on each word, **and** keep `quran_mushaf_lines` as the authoritative line structure. Validate equality at import.
- **Rationale**: Immutable data → no update-anomaly risk; the two hottest reads ("page N's words", "where is this word") become single-table.
- **Alternatives**: join words→lines for every read (rejected — slower, no benefit for read-only data).

### R7 — Value objects persisted as plain strings

- **Decision**: `VerseKey` (`s:a`) and `WordLocation` (`s:a:w`) are Domain value objects for validation/logic but map to `string` columns (`verse_key`, `location`).
- **Rationale**: Safe construction in import/use-case code without EF owned-type complexity on an 83,668-row table.
- **Alternatives**: EF owned types / conversions (rejected — over-engineering here); raw strings only (rejected — loses validation).

### R8 — Ayah markers: stored and flagged

- **Decision**: All 6,236 ayah-end markers are stored as `quran_words` rows with `is_ayah_marker = true`; excluded everywhere from "readable word" counts. Detection cross-checks two signals: last word of the ayah **and** digit-only text in the imlaei form.
- **Rationale**: Markers are required to render the page faithfully but must never count as words or appear in word/search listings.
- **Alternatives**: drop markers (rejected — page can't render); separate markers table (rejected — breaks contiguous id/reading-order and layout ranges).

### R9 — Two ayah word-count fields

- **Decision**: `quran_ayahs` stores `words_count_source` (from metadata) and `words_count_real` (computed = occurrences − 1 marker). They differ only at `37:130` (4 vs 3).
- **Rationale**: Keeps the single known discrepancy honest and traceable; the word index is canonical.
- **Alternatives**: store only source (rejected — wrong for 37:130); store only computed (rejected — loses provenance).

### R10 — No `search_normalized_text`

- **Decision**: Omit it. The two no-tashkeel forms (`text_uthmani_simple`, `text_imlaei_simple`) are the searchable forms; normalization is a later Search feature.
- **Rationale**: Normalization rules are a search concern and were speculative; deferring avoids baking a guess into the foundation. (Product decision.)
- **Alternatives**: add it now (rejected — premature; no agreed normalization rules).

### R11 — Canonical word skeleton source

- **Decision**: Build the 83,668-row skeleton (`id`/`location`/`surah`/`ayah`/`word`) from any one of the four aligned word files; attach the other forms + glyph by `location`. All four are verified identical on these keys (0 mismatches).
- **Rationale**: They agree perfectly; a join miss therefore signals real source drift (a feature, not a bug).
- **Alternatives**: trust one file blindly without cross-check (rejected — loses drift detection).

### R12 — Page fonts excluded from this feature; keep `qpc_glyph` only

- **Decision**: The 604 page fonts are **out of scope** for Feature 002 (dashboard data foundation). `quran_mushaf_pages` has **no** font columns. `qpc_glyph` is retained on `quran_words` (from `qpc-v4.json`) as a lightweight, non-rendered future reference. The dashboard UI renders `text_uthmani`.
- **Rationale**: Glyph rendering needs page-specific fonts and is a public **Mushaf Reader** concern, not the dashboard foundation. Keeping `qpc_glyph` is free (aligned by `location`) and avoids a re-import later.
- **Alternatives**: import/validate fonts now (rejected — out of scope; adds ~50 MB + a 604-file gate the dashboard doesn't need); drop `qpc_glyph` too (rejected — cheap to keep, useful to the future reader).

### R13 — Validation report format: Markdown + JSON

- **Decision**: Every run writes both a human-readable `.md` and a machine-readable `.json` report to a configurable path (default alongside the source root / `resources/report/`).
- **Rationale**: Humans read the md; CI/automation read the json. Matches the existing resources report style.
- **Alternatives**: console-only output (rejected — not durable/inspectable); one format (rejected — serves only one audience).

### R14 — Testing: xUnit + Testcontainers PostgreSQL

- **Decision**: Add `tests/QuranDashboard.Tests` (xUnit). Persistence/validation correctness is verified with **Testcontainers-for-.NET** against a real PostgreSQL; pure logic (validators, value objects, readers) is unit-tested.
- **Rationale**: `test-guard` requires real infrastructure where correctness matters (persistence/queries); the import's whole value is correctness. No test project exists yet, so one is created.
- **Alternatives**: in-memory EF provider (rejected — doesn't represent PostgreSQL semantics/COPY/constraints); SQLite (rejected — different SQL/constraint behavior); mocking the DB (rejected — would test nothing real).

### R15 — Read endpoint deferred to 001b

- **Decision**: No API in this feature. Only the `IMushafPageReadRepository` interface is declared (for 001b to implement).
- **Rationale**: The import is verified by the validation report + DB inspection; an endpoint widens scope without proving import correctness.
- **Alternatives**: include one read endpoint now (rejected — scope creep; correctness already provable without it).

### R16 — Migration policy

- **Decision**: One schema-only migration creates the five tables + constraints + indexes, generated by EF tooling **only when explicitly requested**; `dotnet ef database update` run only on explicit request. No Quran data in migrations.
- **Rationale**: Matches `Backend/CLAUDE.md`; keeps snapshots clean.
- **Alternatives**: hand-written migrations (forbidden by repo policy).

---

**Outcome**: All technical unknowns resolved. Ready for Phase 1 design.
