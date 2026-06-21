# Feature 012 — Mushaf Reader Ayah Similarities — Implementation Completion Report

**Feature**: Mushaf Reader Ayah Similarities
**Branch**: `012-mushaf-reader-ayah-similarities`
**Date**: 2026-06-21
**Phase**: 7 — Polish & Cross-Cutting Concerns (T080–T090)
**Scope**: Full-stack, read-only. .NET read APIs + Angular Mushaf Reader extension. No schema changes, migrations, imports, or writes.

## 1. Summary

Feature 012 extends the existing Mushaf Reader selected-ayah study area with two new study
actions — **similar meaning ayahs** (`آيات قريبة في المعنى`) and **mutashabihat for memorization**
(`المتشابهات اللفظية للحفظ`) — plus a lightweight `similaritySummary` on the selected ayah study
response. The initial Mushaf page response is unchanged: no similarity counters or detail payloads
are added to page/line/word/page-ayah DTOs. Detail payloads are lazy-loaded only when their action
is opened (or restored from a URL/session). All four user stories (US1–US4) are implemented and
verified; this report records the Phase 7 polish, verification, and self-check results.

## 2. User Story / Phase status

| Phase | User Story | Status | Verification |
|---|---|---|---|
| 3 | US1 — Similarity summary counts on selected ayah study | Complete | Backend + frontend tests green |
| 4 | US2 — Similar meaning ayahs as a flat list | Complete | Engineering review PASS after page-context fix |
| 5 | US3 — Mutashabihat as phrase groups | Complete | Engineering review PASS after test-wiring + SCSS-token fixes |
| 6 | US4 — Reopen a similarity study view from the URL | Complete | Engineering review PASS WITH NOTES |
| 7 | Polish & cross-cutting | Complete (with two manual items deferred — see §9) | This report |

## 3. Phase 7 task results (T080–T090)

| Task | Description | Result |
|---|---|---|
| T080 | No EF migration / model-snapshot files created | ✅ PASS — `git diff --name-only main...HEAD` shows no `Migrations/`, `*.Designer.cs`, or `ModelSnapshot` files |
| T081 | No importer / source-package / `resources/` changes | ✅ PASS — no `DataImporter/` or `resources/` paths in the feature diff |
| T082 | API contracts vs implementation (3 contracts) | ✅ PASS — all three match (see §5) |
| T083 | Frontend URL-state & lazy-loading contract | ✅ PASS — matches (see §5) |
| T084 | Full backend test suite (Feature 012) | ✅ PASS — **56 passed, 0 failed, 0 skipped** (`dotnet test --filter FullyQualifiedName~MushafReader`, Testcontainers PostgreSQL) |
| T085 | Full frontend test suite (Feature 012 / mushaf) | ✅ PASS — **40 files, 218 tests passed, 0 failed** (`npm test`, `VITEST_MAX_FORKS=2`) |
| T086 | Quickstart smoke-test flow | ⚠️ Deferred to a human operator with the running HTTPS stack; automated suites cover the equivalent behaviors (see §9) |
| T087 | SC-008 label-comprehension validation | ⚠️ Design rationale recorded; participant/product sign-off still required (see §9) |
| T088 | Clean-code self-check | ✅ PASS (see §6) |
| T089 | Test-code self-check | ✅ PASS (see §7) |
| T090 | This completion report | ✅ Created |

## 4. Build & test evidence

- **Backend build**: `dotnet build -c Debug` → Build succeeded, **0 warnings, 0 errors**.
- **Backend tests**: `dotnet test --filter "FullyQualifiedName~MushafReader"` → **Passed! Failed: 0, Passed: 56, Skipped: 0, Total: 56** (~10s, real PostgreSQL via Testcontainers). Covers similarity-summary counts, similar-ayahs read/dedupe/sort/validation, mutashabihat grouped read/phrase-derivation/validation, and cache behavior.
- **Frontend tests**: `npm test -- --watch=false` (fork cap `VITEST_MIN_FORKS=1 VITEST_MAX_FORKS=2`) → **Test Files 40 passed (40), Tests 218 passed (218)**, ~44s. Includes the Feature 012 specs:
  - `similar-ayahs-card.component.spec` (4), `mutashabihat-groups-card.component.spec` (5)
  - `mushaf-reader.facade.similar-ayahs.spec` (3), `mushaf-reader.facade.mutashabihat.spec` (3)
  - `mushaf-reader.facade.spec`, `mushaf-reader.facade.ayah-study.spec` (7), `mushaf-url-sync.spec` (8), `mushaf-reader-session.spec` (7)

## 5. Contract compliance (T082, T083)

| Contract | Verdict | Notes |
|---|---|---|
| `ayah-study-similarity-summary.api.md` | Matches | `SimilaritySummaryDto(SimilarAyahCount, MutashabihatGroupCount, MutashabihatOccurrenceCount)` field names and count semantics align; counts use combined incoming+outgoing distinct logic; not added to the page response. |
| `similar-ayahs.api.md` | Matches | `GET /api/mushaf/ayahs/{verseKey}/similar-ayahs`; 200/400/404; `ApiResponse<SimilarAyahsResponse>`; message `تم تحميل الآيات القريبة في المعنى`; flat list, incoming+outgoing merge, bidirectional dedupe, score-then-Mushaf-order sort; canonical ayah text. |
| `ayah-mutashabihat.api.md` | Matches | `GET /api/mushaf/ayahs/{verseKey}/mutashabihat`; 200/400/404; `ApiResponse<AyahMutashabihatResponse>`; message `تم تحميل المتشابهات اللفظية`; grouped (never flattened); `groupKey` = `mutashabihat:{sourceGroupId}`; nullable phrase text with range fallback (no fabrication). |
| `frontend-url-state-and-lazy-loading.md` | Matches | Accepted `ayahTab` set incl. `similar-ayahs`/`mutashabihat`; unknown→default normalization; URL & session round-trip preserve the values; only the active detail loads on restore; Arabic labels/empty/loading strings match. |

## 6. Clean-code self-check (T088)

Checked against `.claude/skills/engineering-review/references/clean-code-guard/`.

- **Naming & functions**: Intention-revealing names throughout (`EfAyahSimilaritiesReader`, `MergeSimilarLinks`, `DerivePhraseText`, `MutashabihatLoadRunner`). Reader methods are small and single-level. No vague names.
- **SOLID / layering**: Clean Architecture respected — response DTOs + reader interfaces in `Application.Abstractions`, verse-key validation in Application handlers, EF read/merge/group/phrase logic in Infrastructure, thin controllers mapping outcome unions to `ApiResponse`. Cache decorators wrap readers via DI, matching the existing `CachedWordAnalysisReader` pattern.
- **DRY/KISS/YAGNI**: Each detail action follows the same focused runner/facade/card pattern; no speculative abstractions. The redundant empty-state branch flagged in US2 was collapsed.
- **Strong typing**: Explicit C#/TS types; relationship direction as a constant set / TS union; no `any`.
- **Quranic data safety**: Ayah text from `quran_ayahs.text_uthmani`; phrase text derived at read time from canonical `quran_words` with a count guard returning `null` (range only) when a span cannot be cleanly resolved — never fabricated or copied from mutashabihat storage.
- **AI failure modes**: No swallowed errors, no impossible-case guards, no hallucinated APIs (backend builds clean), no hardcoded success/mock returns in production code.
- **Resolved during review**: US2 page-context omission (fixed + tested); US5/SCSS undefined `--qd-surface-muted`/`--qd-border-subtle` tokens → switched to `--qd-surface-recessed`/`--qd-border`; duplicate test providers removed.

## 7. Test-code self-check (T089)

Checked against `.claude/skills/test-guard/`.

- Tests assert behavior with specific values (counts, verse keys, direction, sort order, phrase text, which detail API is/ isn't called), not implementation details.
- Boundary mocks only (API services, router); real DTOs/entities constructed; backend read/dedupe/group/phrase correctness verified against real PostgreSQL via Testcontainers over a seeded fixture.
- Data-driven variants used (malformed verse keys; both widened `ayahTab` values).
- Lazy-load discipline proven by asserting the *other* detail API is **not** called (SC-007 / US4).
- Quranic test data is synthetic Arabic placeholders frontend; backend fixture (`mushaf-reader-seed.sql`) is isolated and source-safe; phrase assertions are grounded in seeded canonical words (ayah 25 words 1–4, ayah 26 word 1).
- Isolation: session round-trip test clears `sessionStorage` between iterations. Minor note: one facade US4 session test does not clear `sessionStorage` afterward (low impact — last test in file); recommended cleanup left as a non-blocking follow-up.

## 8. Quranic data safety

PASS. Read-only feature; no writes, migrations, imports, or source mutation. Ayah text is canonical;
phrase text is derived from canonical words or omitted (range-only) rather than invented; empty/missing
data shows controlled Arabic states; no fabricated counts or text.

## 9. Outstanding / manual items

- **T086 (quickstart smoke test)** — The live browser walkthrough and `curl` API smoke checks require the
  running HTTPS dev stack (`https://localhost:5015` backend + seeded Feature 006 data, `https://localhost:4200`
  frontend), which is outside this automated run. The same backend behaviors (summary present, similar flat/deduped,
  mutashabihat grouped, canonical text, phrase derivation, malformed-key 400/well-formed-unknown 404) are covered by
  the T084 integration tests, and the UI behaviors by the T085 specs. **Recommended**: a human runs `quickstart.md`
  steps 1–10 once against the dev stack to confirm end-to-end.
- **T087 (SC-008 label comprehension)** — The two action labels are distinct, register-appropriate Arabic phrases:
  `آيات قريبة في المعنى` (semantic closeness) vs. `المتشابهات اللفظية للحفظ` (verbal/wording similarity for
  memorization). A formal reviewer-sample or product sign-off (≥90% can distinguish the two) is still required and
  cannot be produced by an automated run; **recommended** as a short product review before release.

## 10. Definition of Done

- **Changed files**: Backend read APIs (2 controllers, 2 query handlers + outcomes, 2 readers, 2 cache decorators,
  3 response DTO files, 2 reader interfaces, `ApiMessages`, `MushafReaderCacheKeys`, both `DependencyInjection.cs`,
  `EfAyahStudyReader`/`AyahStudyResponse` summary extension) + tests; Frontend models, facade, 2 API services,
  2 load runners, 2 cards, selected-ayah-section/study-context/page wiring, URL/session state, cache keys + specs.
- **Build status**: Backend `dotnet build` clean (0/0).
- **Test status**: Backend 56/56 passed; Frontend 40 files / 218 tests passed.
- **Validation/report path**: this report (`Backend/report/feature-012-mushaf-reader-ayah-similarities/001-implementation-completion-report.md`).
- **Skipped/uncertain**: T086 live smoke and T087 participant validation deferred to a human operator (see §9).

## 11. Engineering review history

- **US2 (Phase 4)**: CHANGES REQUESTED → page-context (FR-013) added + test → PASS.
- **US3 (Phase 5)**: CHANGES REQUESTED → facade test-bed `MushafAyahMutashabihatApi` wiring + SCSS token fix → PASS.
- **US4 (Phase 6)**: PASS WITH NOTES — implementation already satisfied by the generic tab model from earlier phases; verification tests added.
