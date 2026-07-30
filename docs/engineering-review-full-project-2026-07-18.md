# Engineering Review — Full Project Code Audit

- **Date:** 2026-07-18
- **Branch reviewed:** `033-auth-roles-permissions` (whole codebase, not diff-only)
- **Verdict:** **CHANGES REQUESTED**
- **Method:** 18 scoped reviewers (backend layers, frontend areas, cross-cutting sweeps, test-guard gates) + adversarial verification; every non-NOTE finding refutation-tested by 1–2 independent verifiers. 3 findings refuted and excluded.
- **Excluded:** `docs/` content review (stale), generated code (`Migrations/`, `*.Designer.cs`, OpenAPI client), `resources/`.

## Verification evidence

| Check | Result |
|---|---|
| Backend build | PASS — 0 warnings / 0 errors |
| Backend tests | PASS — 1622/1622 (Testcontainers, 5m48s) |
| Frontend build | PASS — 3 budget warnings (initial bundle 565.87 kB > 500 kB; 2 mushaf SCSS > 4 kB) |
| Frontend tests (branch 033) | **FAIL — 30 failed / 1829 passed** (see BLOCKING-1) |
| Frontend tests (dev) | PASS — 1832/1832 |

## Test Guard verdicts

| Suite | Verdict |
|---|---|
| Backend pipelines/import tests | CHANGES REQUESTED |
| Backend API/explorer tests | PASS WITH NOTES |
| Frontend words tests | CHANGES REQUESTED |
| Frontend other tests | PASS WITH NOTES |

## Finding counts (after dedup)

| Severity | Count |
|---|---|
| BLOCKING | 2 |
| MAJOR | 33 |
| MINOR | 83 |
| NOTE | 55 |

Verification status legend: **CONFIRMED** = independently re-verified against source; **PLAUSIBLE** = one verifier dissented; **NOTE-unverified** = observation only, not verification-gated.

## BLOCKING — must fix before merge (2)

### B1. `Frontend/quran-dashboard-ui/src/app/app.routes.spec.ts:29`

*category: test-quality · found by: manual-verification · verification: CONFIRMED*

**Issue:** The spec awaits every route.loadChildren() to flatten the route tree, force-evaluating the entire lazy module universe into the shared Vitest fork's module cache. Mushaf specs running later in the same worker inherit the polluted module state: selected-ayah-section's template reads AYAH_STUDY_TAB_LABELS as undefined ('Cannot read properties of undefined (reading tafsir)'), similarity cards render empty. 30 tests fail deterministically in the full suite.

**Why it matters:** Frontend test suite is red on branch 033 (30 failed / 1829 passed) while dev is green (1832/1832). Failing specs pass in isolation; pairing app.routes.spec.ts + selected-ayah-section.component.spec.ts in one worker reproduces all 21 of that file's failures. Branch cannot merge green.

**Suggested fix:** Assert the no-guard posture without executing loadChildren (static route-config inspection), or isolate this spec in its own worker/environment per the frontend-test-harness-constraints reference.

### B2. `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTestFixture.cs:61`

*category: quran-safety · found by: tg-be-pipelines · verification: CONFIRMED*

**Issue:** RunImportAsync builds ImportMorphologyCommand without a ReportOutDir, so ImportMorphologyHandler.ResolveReportOutDir falls back to <repo>/resources/report/words-morphology. Every morphology test run overwrites the canonical local import evidence with a synthetic report. Verified in the working tree: resources/report/words-morphology/morphology-import-report.json now has verdict "pass" with "readableWords": 5 (the fixture's 5-word synthetic seed), mtime today 19:24 — the real ~77k-word import evidence is gone and replaced by a misleading passing report.

**Why it matters:** Quran-data-safety rule 4 (never drop traceability/provenance): the canonical evidence of the real morphology import is silently destroyed on every test run and replaced with a synthetic report that reads as a genuine passing import. Anyone auditing import provenance now reads fabricated evidence.

**Suggested fix:** Default reportOutDir in the fixture to a per-test temp dir (reportOutDir ??= Path.Combine(Path.GetTempPath(), $"morph-report-{Guid.NewGuid():N}")) exactly like TafsirImportTestFixture:205 / TranslationImportTestFixture:326 / NavigationImportTestFixture:157 already do; then regenerate or restore the real reports under resources/report/words-morphology/.

## MAJOR — fix soon (33)

### M1. `Backend/api/QuranDashboard.Api/Controllers/System/HealthController.cs:32`

*category: error-handling · found by: be-api · verification: CONFIRMED*

**Issue:** The health endpoint always returns 200 OK with isSuccess:true, even when HealthCheckService reports Unhealthy (database unreachable); the Unhealthy message is also the 'degraded' text 'الخدمة تعمل مع وجود تنبيهات' (service running with alerts), which is untrue when the DB check fails.

**Why it matters:** Railway (and most infra probes) key on HTTP status. With a dead database the instance still reports 200, so the deploy/health-probe gates described in API_GUIDELINES section 14 pass and traffic keeps routing to a broken instance. isSuccess:true on an unhealthy report also bends the envelope semantics.

**Suggested fix:** Return 503 (and isSuccess:false or at least a truthful message) when report.Status == HealthStatus.Unhealthy; keep 200 for Healthy/Degraded. Add a distinct ApiMessages.HealthUnhealthy Arabic message instead of reusing the degraded text.

### M2. `Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs:25`

*category: api-guidelines · found by: be-api · verification: CONFIRMED*

**Issue:** UseRateLimiter() runs AFTER UseAuthentication/UseAuthorization, but API_GUIDELINES.md section 14 (canonical) says the limiter is wired 'after CORS, in the reserved pre-auth slot'. The code comment (lines 23-24) documents the opposite order as deliberate, so code and canonical doc now contradict each other.

**Why it matters:** Two concrete effects: (1) requests to [Authorize] endpoints (today /api/access/me) that fail authentication are 401-challenged by the authorization middleware and never reach the limiter, so unauthenticated hammering of protected endpoints is completely unlimited; (2) every eventually-429'd request first pays JWT validation plus the RoleClaimsTransformation role lookup (DB on cache miss), weakening the DoS/DB-load bound the limiter exists to provide. The workspace rule also requires the canonical doc to be updated in the same change that alters documented behavior.

**Suggested fix:** Either move UseRateLimiter back to the documented pre-auth slot (per-user keying is not used yet), or keep the new order deliberately and update API_GUIDELINES.md section 14 in the same change — and if kept post-auth, add rate limiting or lockout coverage for the 401-challenge path.

### M3. `Backend/api/QuranDashboard.Api/appsettings.Production.json:9`

*category: api-guidelines · found by: be-api · verification: CONFIRMED*

**Issue:** A live production Neon database owner credential (Username=neondb_owner;Password=npg_13ihUKfwGQaP) sits in plaintext in appsettings.Production.json. The file is gitignored (local-only), but the base appsettings.json convention is 'SET_VIA_USER_SECRETS' and this file is auto-loaded by ASP.NET and copied into bin/ publish output.

**Why it matters:** A real production DB password in a plaintext workspace file leaks via local publishes (bin/Release exists), backups, screen shares, or any future .gitignore change — and it has now been exposed to this review. The project's own convention keeps secrets out of config files.

**Suggested fix:** Rotate the Neon credential, remove the connection string from the file, and supply it via user-secrets or environment variables like the base configuration prescribes.

### M4. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/DisplayRebuilding/RebuildDisplayWordsHandler.cs:33`

*category: architecture · found by: be-domain-app · verification: CONFIRMED*

**Issue:** Application-layer handlers perform direct file-system access: Directory.CreateDirectory (line 33), DirectoryInfo/AppContext.BaseDirectory walking and Directory.Exists (lines 64-81). The same pattern exists in ImportMorphologyHandler.cs (45, 82-99), ImportMutashabihatHandler.cs (65, 97-114), and GenerateI3rabHandler.cs (50-75, incl. Directory.GetCurrentDirectory).

**Why it matters:** CLEAN_ARCHITECTURE.md explicitly forbids file-system access in Application ('Forbidden in Application: ... file system access'). The newer pipelines (Translations, Navigation, Tafsirs, FullI3rab) prove the clean alternative already exists in this codebase: they require ReportOutDir from the caller and keep directory creation inside the Infrastructure report writer.

**Suggested fix:** Adopt the newer pattern in the four legacy handlers: require command.ReportOutDir from the caller (the DataImporter tool resolves defaults), and move Directory.CreateDirectory into the Infrastructure report-writer implementations.

### M5. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/MorphologyImporting/ImportMorphologyHandler.cs:62`

*category: error-handling · found by: be-domain-app · verification: CONFIRMED*

**Issue:** catch (InvalidDataException) and catch (FileNotFoundException or IOException) both discard ex.Message and return the generic MorphologyInvariants.SourceMismatch constant ('Local morphology source files do not match manifest.json (presence/count/size/sha256)'), and these refusal paths return without writing any report artifact.

**Why it matters:** A data-shape/alignment failure raised during parsing is reported to the operator with a misleading manifest-checksum message, and the specific diagnostic (which record, what mismatched) is lost with no report trail. Quran data safety requires not hiding data problems and producing clear importer reports; the Translations handler shows the better pattern (InvalidDataException keeps ex.Message, refusals emit a refusal report).

**Suggested fix:** Keep ex.Message for InvalidDataException (as ImportTranslationsHandler.cs:63-67 does), reserve SourceMismatch for genuine manifest/IO mismatch, and emit a refusal report on these paths via the emitter pattern used by the newer pipelines.

### M6. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/SimpleI3rabGeneration/GenerateI3rabHandler.cs:74`

*category: dry-kiss-yagni · found by: be-domain-app · verification: CONFIRMED*

**Issue:** ResolveRepositoryRoot()/ResolveReportOutDir() are copy-pasted verbatim into 4 handlers (GenerateI3rab, RebuildDisplayWords, ImportMorphology, ImportMutashabihat) and have already drifted: GenerateI3rabHandler silently falls back to Directory.GetCurrentDirectory() (line 74) where the other three throw InvalidOperationException, and it skips Path.GetFullPath normalization of a caller-supplied ReportOutDir (line 47) which the others apply.

**Why it matters:** One piece of knowledge (how the default report dir is resolved) has four representations that no longer agree — a classic copy-paste-drift failure mode (clean-code-guard dry-kiss-yagni + ai-failure-modes). The CWD fallback can silently write reports to an arbitrary directory instead of failing, and the missing GetFullPath yields inconsistent relative-path handling across pipelines.

**Suggested fix:** Eliminate the duplication together with the layering fix above (caller supplies ReportOutDir). If default resolution must remain, extract one shared resolver used by all four and pick one failure behavior (throw, never CWD fallback).

### M7. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/DisplayRebuilding/RebuildDisplayWordsHandler.cs:48`

*category: error-handling · found by: be-domain-app, x-ai-failures · verification: CONFIRMED*

**Issue:** catch (Exception ex) { return RebuildDisplayWordsResult.Failure(ex.Message); } — a catch-all that flattens ANY exception (NullReferenceException, EF/Npgsql faults, even OperationCanceledException from Ctrl+C) into a pipeline 'Failure' result carrying only the message string. Every sibling import handler (ImportTafsirsHandler, ImportTranslationsHandler, ImportNavigationMetadataHandler, ImportMutashabihatHandler, ImportFullI3rabHandler, ImportMorphologyHandler) deliberately catches only specific exception types and lets the rest propagate.

**Why it matters:** Failure mode #1 (catch-all swallowing) + #10 (inconsistency with surrounding code). In a Quran data pipeline this misreports a code defect as a data/validation failure, discards the exception type and stack trace needed for diagnosis, and reports operator cancellation as a rebuild failure — CODING_PRINCIPLES §10 says do not hide data problems; this hides code problems inside them.

**Suggested fix:** Mirror the sibling handlers: enumerate the recoverable exception types (IOException, UnauthorizedAccessException, InvalidDataException, the invariant InvalidOperationException already handled) and let unexpected exceptions and OperationCanceledException propagate.

### M8. `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:102`

*category: error-handling · found by: be-infra-core · verification: CONFIRMED*

**Issue:** The catch (DbUpdateException) in CreateAsync only recovers the same-sub unique-index race: it re-queries by LogtoSub and rethrows the raw DbUpdateException when no winner exists. A collision on the OTHER unique index (users.email) — e.g. a user deleted and re-created in Logto, arriving with the same primaryEmail but a new sub — always lands in the rethrow path.

**Why it matters:** That subject can never be provisioned: every login attempt re-inserts, hits the email unique index, finds no row for the new sub, and rethrows an unactionable raw DbUpdateException (a 500 via global handling) until someone hand-edits the database. CODING_PRINCIPLES §8 requires specific, actionable errors; this is a permanent auth-path failure for a realistic operational scenario.

**Suggested fix:** Inspect the inner PostgresException (SqlState 23505 + constraint name) to distinguish the email-unique violation from the sub race, and translate it into a specific controlled failure (e.g. a provisioning conflict result/message stating the email is already registered to another subject) instead of rethrowing the raw exception.

### M9. `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Translations/TranslationManifestReader.cs:352`

*category: dry-kiss-yagni · found by: be-infra-pipelines · verification: CONFIRMED*

**Issue:** The staged-package integrity knowledge (sha256 checksum, file-size, record-count validation, digest capture + VerifyDigestsUnchangedAsync, per-domain FileDigest records with custom equality) is re-implemented in each pipeline: 8 private ValidateChecksum methods, 21 SHA256.HashData call sites, and 6 near-identical digest-capture/verify implementations across Foundation/Navigation/Translations/Tafsirs/Mutashabihat/FullI3rab/Morphology readers.

**Why it matters:** This is single-knowledge duplication (per the DRY guard: 'every piece of knowledge must have a single authoritative representation'), well past the Rule of 3, and drift is already observable: Foundation/Translations hash via File.ReadAllBytes (whole file in memory) while Navigation streams; NavigationFileDigests is a record with proper GetHashCode while TranslationFileDigests is a class whose Equals doesn't override object.Equals and whose GetHashCode() => Digests.Count. A future fix to one copy (e.g. hashing, digest equality) will silently miss the others.

**Suggested fix:** Extract a shared package-integrity helper next to Files/Quran/DataPipelines/Foundation/ (checksum, size, record-count, digest capture/compare) and have each domain reader compose it, keeping only the domain-specific manifest schema/shape checks local.

### M10. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyValidationRunner.cs:7`

*category: threshold · found by: be-infra-pipelines · verification: CONFIRMED*

**Issue:** 781 lines, far over the 450-line hard threshold for services; it mixes at least four separable check families (US1 count/location checks, segment-dimension checks, secondary-stem curation checks, word-lemma normalization checks) plus two streaming render-provenance comparators (legacy and enriched) and raw NpgsqlCommand reader loops.

**Why it matters:** BACKEND_STRUCTURE.md's hard threshold means 'stop and split'; the check families have different reasons to change (schema invariants vs curated-artifact policy vs normalization artifact vs rendering) and each already has its own MorphologyInvariants id-group, so the split lines are natural. Verified against the code: the class is cohesive in purpose but not in responsibility.

**Suggested fix:** Split by check family into focused classes (e.g. MorphologyCoreChecks, SegmentDimensionChecks, SecondaryStemCurationChecks, WordLemmaNormalizationChecks, RenderProvenanceChecks) composed by a thin RunAllHardChecksAsync orchestrator; keep shared scalar/reader helpers in MorphologyCommandExecutor.

### M11. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/MorphologyImporting/ImportMorphologyHandler.cs:84`

*category: architecture · found by: be-infra-pipelines, x-arch · verification: CONFIRMED*

**Issue:** Four Application handlers perform direct file-system access and each carries a verbatim copy of ResolveRepositoryRoot() that walks AppContext.BaseDirectory probing for 'resources'/'Backend' folders: ImportMorphologyHandler.cs:45,82-100; RebuildDisplayWordsHandler.cs:33,64-72; GenerateI3rabHandler.cs:57-75; ImportMutashabihatHandler.cs:65,97-99. The canonical copy of this exact code already lives in the host tool (Backend/tools/QuranDashboard.DataImporter/Import/DefaultPaths/DataImporterDefaults.cs:57-75). The GenerateI3rabHandler copy has silently diverged: on failure it falls back to Directory.GetCurrentDirectory() instead of throwing, so the same knowledge now has two behaviors.

**Why it matters:** CLEAN_ARCHITECTURE.md explicitly forbids file system access in Application ('Forbidden in Application: ... file system access'). Repository-layout probing is a host/deployment concern that makes these use cases behave differently depending on where the binary runs (repo checkout vs Railway container). Five duplicated copies of one piece of knowledge (DRY per clean-code-guard: same ≥5-line token sequence in ≥2 functions), one already divergent, guarantee further drift.

**Suggested fix:** Adopt the pattern the same codebase already uses in ImportTafsirsHandler.cs:202-212: make ReportOutDir a required command input and throw if absent; let the DataImporter tool (DataImporterDefaults) supply defaults, and move Directory.CreateDirectory into the Infrastructure report-writer implementations. Delete the four handler-local ResolveRepositoryRoot copies.

### M12. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs:51`

*category: dry-kiss-yagni · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** The ~100-line ayah-match hydration pipeline (matched ayah ids → paged ayah metas + surah join → matched rows → wordsByAyah → DTO assembly) is duplicated near-verbatim in EfLemmasReader.GetLemmaAyahMatchesAsync (51-159), EfRootsReader.GetRootAyahMatchesAsync (115-210), EfStemsReader.GetStemAyahMatchesAsync (49-163) and largely again in EfUniqueWordsReader.GetAyahMatchesAsync; likewise the mentioned/missing-surah reads, the word-group load/slice machinery, and the private records (AyahMetaRow/AyahWordRow/SurahOccurrenceRow/…) are triplicated. The copies have already drifted: ResolveAyahPageNumber has three different implementations (Roots' version at EfRootsReader.cs:388 ignores IsAyahMarker entirely; Lemmas/Stems check a flag that is always false after the !IsAyahMarker query filter), Stems filters !w.IsAyahMarker in its matched/surah queries while Roots/Lemmas do not, and Roots pages with raw Skip while Lemmas/Stems use ReadPaging.CalculateSafeSkip.

**Why it matters:** This is knowledge duplication (one hydration/ordering contract expressed four times) and it is actively drifting — each divergence is a place where the explorers' documented shared contract can silently split; it is also the main reason all five reader files exceed the 400-line soft threshold.

**Suggested fix:** Extract one shared internal ayah-match hydration helper (parameterized by the matched-word IQueryable and a DTO factory) plus shared AyahMetaRow/AyahWordRow/SurahOccurrenceRow records and ONE ResolveAyahPageNumber next to ReadPaging/ArabicSearchQueryNormalizer in the Words folder; do the same for the mentioned/missing-surah pair. The knowledge is nameable ('scoped ayah-match page hydration'), and the Rule of 3 is well past.

### M13. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs:1`

*category: threshold · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** EfLemmasReader.cs is 605 lines, over the hard 600-line threshold for repository/read services (BACKEND_STRUCTURE.md §4: hard threshold = 'stop and split, or split immediately'). EfStemsReader (489), EfUniqueWordsReader (437) and EfRootsReader (432) are all over the soft 400 threshold; EfWordTypesReader.cs sits at 586.

**Why it matters:** The project's own canonical structure doc treats a hard-threshold breach as a must-split, and the sibling readers already established the exact convention to follow (EfStemsReader.Summary.cs, EfUniqueWordsReader.List.cs — 'partial-split by size' is documented in the Words README as the convention to keep).

**Suggested fix:** Move LoadWholeSummaryAsync + the two raw SQL blocks + the aggregation/distribution records into an EfLemmasReader.Summary.cs partial (mirroring EfStemsReader). The soft-threshold breaches in the other three readers largely dissolve if the duplicated ayah/surah/word-group machinery is extracted per the DRY finding.

### M14. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/RootsListDerivation.cs:138`

*category: dry-kiss-yagni · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** The Arabic search-normalization knowledge (fold table + IsSkippable ranges + NormalizeArabicQuery) exists in four C# copies — ArabicSearchQueryNormalizer (shared), RootsListDerivation:138-172, LemmasListDerivation:178-212, StemsListDerivation:179-213 — plus a fifth divergent SQL variant (replace(translate(lower(...)))) in the whole-summary queries. The copies already disagree: the three derivation copies strip all whitespace, the shared normalizer keeps interior whitespace, and the SQL variant strips only ASCII space and does NOT strip diacritics/tatweel. Roots is the only explorer whose row-side normalized text comes from the SQL variant (RootSummaryRow.NormalizedRootText) while its query side uses the C# variant — two different normalizers feed the two sides of the same Contains comparison.

**Why it matters:** This is textbook DRY knowledge duplication (same fold table/SQL fragment in 5 sites, clean-code-guard dry-kiss-yagni smell list): any fold-table fix must be replicated in lockstep across all sites with no compile-time link, and the Roots SQL/C# asymmetry is a latent search-miss bug if any root_text ever carries a diacritic or Quranic mark.

**Suggested fix:** Make ArabicSearchQueryNormalizer the single authority: expose FoldFrom/FoldTo from it, delete the three private NormalizeArabicQuery/IsSkippable/Fold copies and the per-derivation ArabicFoldFrom/ArabicFoldTo constants (a whitespace-stripping overload can live on the shared class), and normalize the Roots row text in C# like Lemmas/Stems do (or document why the SQL fold is intentionally weaker).

### M15. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfAyahStudyReader.cs:341`

*category: quran-safety · found by: be-infra-reads, x-quran-safety · verification: CONFIRMED*

**Issue:** ParseCoveredAyahKeys catches JsonException and silently returns [] with no logging; EfWordAnalysisReader.ParseFeaturesJson (line 287) does the same for segment features JSON. Corrupt covered-ayah-keys JSON stored for a tafsir/full-i3rab entry is presented to the user as 'this entry covers no ayahs' while CoveredAyahCount still reports a positive number from the same row.

**Why it matters:** Quran-data-safety rule 3 ('never hide missing or unknown data; no plausible-looking fallback') and rule 4 (coverage keys are provenance metadata): masking stored-data corruption as a normal empty list, with a self-contradictory DTO (count > 0, keys = []), hides a data problem that should surface. This is also AI failure mode #1 (catch-all that swallows failures).

**Suggested fix:** At minimum log a warning with the ayah/source identity when deserialization fails (readers can take an ILogger); preferably surface a controlled 'coverage unavailable' marker instead of an empty list so corruption is distinguishable from genuinely empty coverage.

### M16. `Frontend/quran-dashboard-ui/src/app/features/auth/pages/auth-callback/auth-callback.component.ts:42`

*category: error-handling · found by: fe-auth-dash · verification: CONFIRMED*

**Issue:** A failed OIDC login is silently swallowed. The callback subscribes to isAuthenticated$ once and unconditionally navigates to /dashboard. The in-code comment only justifies the ABANDONED-login case, but a genuinely FAILED login (Logto returns ?error=..., or the code/state exchange fails in the app-initializer checkAuth) also arrives here with isAuthenticated=false and produces zero user-facing signal — no component in the app observes OIDC errors (grep: no PublicEventsService / CheckingAuthFinishedWithError / checkAuth-result subscriber anywhere).

**Why it matters:** The user clicks "تسجيل الدخول", the exchange fails (expired state, misconfiguration, denied consent), and they land back on the dashboard anonymous with the sign-in button still showing — indistinguishable from never having tried. This violates CODING_PRINCIPLES §8 (errors specific and actionable) and API_INTEGRATION_GUIDELINES "Do not silently swallow API failures"; the review brief explicitly targets swallowed auth failures.

**Suggested fix:** Distinguish failure from abandonment before navigating: inspect the callback URL's error/code query params (available via ActivatedRoute before navigateByUrl wipes them) or subscribe to angular-auth-oidc-client's PublicEventsService for CheckingAuthFinishedWithError. On failure, render a calm Arabic error state on the callback page with a retry sign-in action (or navigate with a flag the dashboard surfaces) instead of silently landing anonymous.

### M17. `Frontend/quran-dashboard-ui/src/environments/environment.ts:9`

*category: ai-failure-mode · found by: fe-core, x-ai-failures · verification: CONFIRMED*

**Issue:** The production environment file ships placeholder Logto config (endpoint 'https://REPLACE-WITH-YOUR-TENANT.logto.app', appId 'REPLACE_WITH_LOGTO_SPA_APP_ID', placeholder redirect URIs and resource). angular.json fileReplacements confirm this file IS the production build config, and app.config.ts wires it into provideAuth with withAppInitializerAuthCheck(), which (with the library's default eager well-known loading) attempts OIDC discovery against the placeholder authority at every production bootstrap.

**Why it matters:** When branch 033 reaches main (Railway auto-deploys), the sign-in button silently redirects to a non-existent tenant and every page load fires a doomed discovery request. Nothing fails the build on placeholder values, so the break is silent.

**Suggested fix:** Fill real production Logto values before release, or add a build-time guard (e.g. a check script or environment validation that throws when a value contains 'REPLACE') so a production bundle with placeholders cannot ship.

### M18. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts:394`

*category: error-handling · found by: fe-mushaf · verification: CONFIRMED*

**Issue:** loadPage subscribes via subscribeToApiLoad with no request-token/staleness guard: onSuccess unconditionally sets _page and onSettled sets _pageLoadState. Every other resource in this facade (ayah study, word analysis, similar ayahs, mutashabihat) uses bumpRequestToken/getRequestToken guards; the page load is the one path without it, and the subscription is also never cancelled in unbindFromRoute().

**Why it matters:** Rapid page changes (page-jump input, surah jump, browser back/forward) can leave two uncached page requests in flight; if the earlier one resolves last, the reader renders page N while the URL and _pageNumber say page M — displaying the wrong Mushaf page is a correctness and data-trust failure in a Quran reader. It also triggers prefetch of the wrong adjacent pages.

**Suggested fix:** Apply the same token pattern used by the runners: capture a token in loadPage (or compare data.pageNumber against this._pageNumber()) before applying onSuccess/onSettled, and cancel the stored subscription in unbindFromRoute().

### M19. `Frontend/quran-dashboard-ui/src/app/shared/ui/state/state.component.html:11`

*category: ai-failure-mode · found by: fe-shared-styles · verification: CONFIRMED*

**Issue:** The qd-state error retry button is classed `qd-button qd-state__action`, but no `.qd-button` class exists anywhere — the global button family is `.qd-btn` (src/styles/_components.scss:50). The one sanctioned retry affordance (Feature 030 M3) renders as an unstyled native browser button: no tokens, no themed hover, off-system in dark theme. UI_STYLE_SYSTEM.md §17 (qd-state) repeats the same phantom name '.qd-button', so the doc canonizes the bug.

**Why it matters:** Hallucinated/phantom class reference (clean-code-guard ai-failure-modes #6/#10 — inconsistency with the surrounding `.qd-btn` convention used by every other shared component). The single retry control on every error state looks broken in both themes.

**Suggested fix:** Change the class to `qd-btn` (plus an appropriate variant, e.g. `qd-btn-secondary`), fix the comment in state.component.scss:6, and correct '.qd-button' to '.qd-btn' in UI_STYLE_SYSTEM.md §17.

### M20. `Frontend/quran-dashboard-ui/src/styles/_components.scss:609`

*category: architecture · found by: fe-shared-styles · verification: CONFIRMED*

**Issue:** Lines 609–703 style mushaf-feature component internals from the global components partial: `qd-selected-ayah-section.qd-selected-ayah-section--embedded` (and its `.selected-ayah-section__tabs/__content` internals), `qd-selected-word-section`, `.selected-ayah-section__tab-count` (666), and `.selected-word-section__morphology-skeleton` (693). styles/README.md's boundary says 'Keep feature- or component-specific selectors in local component .scss files' and UI_STYLE_SYSTEM §2 says 'Do not put feature-specific styles in global files'; the README also documents _components.scss as holding only global cards/buttons/badges/modal/detail-panel/skeleton patterns.

**Why it matters:** Global scope is being used to pierce component encapsulation: one component's layout behavior is split between its own SCSS and a global file 600 lines into _components.scss — action at a distance that future edits to the mushaf components will miss (SOLID/SRP boundary erosion).

**Suggested fix:** Move these rules into the selected-ayah-section / selected-word-section component SCSS (using :host-context or shared tokens where the responsive override needs page context), or promote the genuinely reusable part (the tab-count pill duplicates `.qd-tabs__count` almost verbatim) into a proper `qd-` primitive.

### M21. `Frontend/quran-dashboard-ui/src/styles/_forms.scss:39`

*category: ui-style · found by: fe-shared-styles · verification: CONFIRMED*

**Issue:** `.qd-select` draws its custom chevron with physical background-position anchored to the right edge (`calc(100% - …)`) while the arrow gutter is reserved with `padding-inline-end` — which is the physical LEFT under the app's default dir="rtl". The chevron therefore lands where the RTL text starts, and the reserved space sits empty on the opposite side. `appearance: none` is also never set, so browsers still render their native select indicator (Chrome places it at inline-end/left in RTL), giving a duplicated indicator.

**Why it matters:** This is exactly the physical-vs-logical mismatch the RTL-first mandate (UI_STYLE_SYSTEM §8, PRODUCT.md 'Arabic-first, genuinely') forbids; it hits a shared form primitive used by the explorer sort fallbacks (`roots-explorer-page.component.html:52`, all five explorers) and `surah-jump-picker`.

**Suggested fix:** Set `appearance: none` and position the chevron on the side matching `padding-inline-end` — for the RTL-only app, physical left (e.g. `background-position: calc(var(--qd-space-3) + 0.125rem) …`), or add a `[dir]`-aware override if LTR must survive.

### M22. `Frontend/quran-dashboard-ui/src/styles/_typography.scss:18`

*category: ui-style · found by: fe-shared-styles · verification: CONFIRMED*

**Issue:** Only 400 and 700 faces of IBM Plex Sans Arabic are declared (and only those .woff2 files exist in public/fonts), yet UI_STYLE_SYSTEM §15A — explicitly still in force ('ship them, do not rely on only 400/700; mid-weights 500/600 carry nav links, card titles, labels') — requires 500/600. There are 59 `font-weight: 500|600` declarations across styles/ and app/ (e.g. `.qd-btn` 500 at _components.scss:62, tab/chip counts 600 at :218/:283, table headers 600 at _explorer-tables.scss:63); CSS font matching resolves 500→400 and 600→700.

**Why it matters:** The typographic hierarchy the design contract relies on silently collapses: every 500-weight control label renders regular, every 600 renders full bold — a systemic, invisible divergence from the shipped design system.

**Suggested fix:** Add the Medium (500) and SemiBold (600) IBM Plex Sans Arabic woff2 files with matching @font-face declarations, or amend §15A/DESIGN.md if the 400/700-only compromise is a deliberate decision.

### M23. `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.controller.ts:107`

*category: dry-kiss-yagni · found by: fe-words-state · verification: CONFIRMED*

**Issue:** The roots (460 ln), lemmas (541 ln), and stems (539 ln) detail controllers, plus their *-detail-panel.updates.ts and *-detail-view.loader.ts siblings, are copy-paste parallel: an entity-name-normalized diff of lemmas vs stems controllers reduces to import/method ordering. The same applyUrlState/retryCurrentIdentity/applyIdentity/loadSummaryAndRestore/applyIfCurrent/notFound-vs-error skeleton is repeated 5x (incl. word-types controller and unique-words drilldown), and the panel-update builders (buildAyahsPanelUpdate etc., extractPanelErrorMessage) are repeated near-verbatim per entity.

**Why it matters:** This is knowledge duplication (one rule — the detail-identity lifecycle: dedupe → cancel both slots → summary-then-view load under a generation token → notFound/error mapping — represented five times), well past the Rule of 3. It is already causing drift with real bugs: the unbind stuck-loading fix landed only in roots (see the MAJOR above), and stems' setWordView/setSurahView clear ayahTypeCode in the panel update while lemmas' do not (benign today only because guards keep view !== 'ayahs'). Every future fix (e.g. Feature 030 M3 retry) must be hand-replicated in 3-5 files.

**Suggested fix:** Extract the shared skeleton the way DetailRequestLifecycle already was: a generic abstract detail controller parameterized by <TUrlState, TPanelState, TSummary> taking an equality fn, initial panel, summary loader/cache-key, view loader, and notFound/error builders — each entity keeps only its typed identity, view-setter guards, and update builders. If that abstraction proves wrong-shaped, at minimum unify the unbind/cancel semantics and the generic panel-update builders (buildPagedUpdate/buildListUpdate/extractPanelErrorMessage) into one shared module.

### M24. `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-detail.facade.ts:71`

*category: error-handling · found by: fe-words-state · verification: CONFIRMED*

**Issue:** unbindFromRoute() only calls controller.cancelPendingLoads(), leaving the controller's activeUrlState set while the panel is stuck at status 'loading'. The same defect exists in stems-detail.facade.ts:71 and word-types-detail.facade.ts:106-110 (cancelPendingLoads keeps this.activeUrlState). RootsDetailFacade.unbindFromRoute (roots-detail.facade.ts:69-78) explicitly fixed this — its comment describes exactly this failure — and UniqueWordsDrilldownController.cancelPendingWork clears the identity; the fix was never propagated to the three siblings.

**Why it matters:** If a user opens a detail deep link, navigates away while the summary/detail request is in flight (page destroy → unbind cancels the request, panel stays 'loading', identity retained by the root-scoped facade), then returns to the same URL (e.g. browser Back), applyUrlState()/syncFromUrlState() short-circuits on the equal identity and never re-issues the load. The panel is stranded on a spinner with no request in flight and no retry affordance ('loading' is not the error state), recoverable only by changing the selection or reloading the app.

**Suggested fix:** Mirror the roots/unique-words semantics in all three facades: on unbind either call controller.clearSelection() (roots approach) or add a cancelPendingWork() that cancels subscriptions AND nulls activeUrlState without resetting held panel data (unique-words approach, preserves the loaded-state fast path). Add a rebind-after-mid-flight-unbind spec for each.

### M25. `Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.ts:131`

*category: api-integration · found by: fe-words-ui · verification: CONFIRMED*

**Issue:** The queryParamMap subscription restores restoredColumn and ranges but never searchDraft, even though parseRootsQueryParams(params) already returns search and the template binds [searchValue]="searchDraft()" (roots-explorer-page.component.html:31). All four sibling explorers restore it (lemmas:157, stems:164, word-types:216, unique-words via effect).

**Why it matters:** On a shared deep link, refresh, or Back/Forward with ?search=..., the Roots list renders filtered while the search input shows empty — the user cannot see or clear the active filter, violating the documented URL-state contract (search 'restored into the input on refresh/Back') that every other explorer honors.

**Suggested fix:** Add this.searchDraft.set(parsed.search) inside the querySyncSub handler (mirroring lemmas-explorer-page.component.ts:157), and cover it with a URL-restore test like the siblings'.

### M26. `Backend/tests/QuranDashboard.Tests/Quran/Import/ImportReconstructionTests.cs:40`

*category: test-quality · found by: tg-be-pipelines · verification: CONFIRMED*

**Issue:** The foundation (Quran text) import suite proves exact canonical counts (114/6236/604/9046/83668/77432) but persisted text integrity is asserted only as NotBeNullOrWhiteSpace for one sample word here, plus TextImlaeiSimple equality for two spot words in ImlaeiCleanKeyImportTests. The pipeline's own SourceAlignmentCheck validates the in-memory assembled data before persistence, so nothing verifies DB rows against the source after the write stage.

**Why it matters:** A persist-stage defect that swaps or garbles TextUthmani/TextUthmaniSimple/QpcGlyph for all 83,668 words (e.g. a COPY column-binding bug) would pass every existing test: counts are unchanged and the spot checks touch only TextImlaeiSimple. For the actual Quran text this is exactly the "counts/samples-only" gap that lets a broken import go green.

**Suggested fix:** Add a round-trip test that reads the staged source (via the existing JsonWordSourceReader) and compares a full-column fingerprint (e.g. SHA256 over ordered Location|TextUthmani|TextUthmaniSimple|TextImlaeiSimple|QpcGlyph) of all persisted quran_words rows against the same fingerprint computed from source records.

### M27. `Backend/tests/QuranDashboard.Tests/Quran/Mutashabihat/MutashabihatImportTestFixture.cs:75`

*category: quran-safety · found by: tg-be-pipelines · verification: CONFIRMED*

**Issue:** RunImportAsync passes reportOutDir straight through (default null); ImportMutashabihatHandler then defaults to <repo>/resources/report/mutashabihat. ~25 test call sites (MutashabihatImportTests, MutashabihatValidationFailureTests, MutashabihatRefusalForceTests, MutashabihatReadQueryTests) pass no report dir. Verified: resources/report/mutashabihat/mutashabihat-import-report.json now contains "groupRows": 1 synthetic totals with verdict "pass", mtime today 19:23.

**Why it matters:** Same provenance destruction as the morphology finding: canonical mutashabihat import evidence is replaced by a synthetic 1-group "pass" report on every test run.

**Suggested fix:** In RunImportAsync add reportOutDir ??= Path.Combine(Path.GetTempPath(), $"mutashabihat-report-{Guid.NewGuid():N}") and restore the real report files.

### M28. `Frontend/quran-dashboard-ui/src/app/features/words/data-access/roots.api.ts:28`

*category: test-quality · found by: tg-fe-words · verification: CONFIRMED*

**Issue:** The HTTP request contract for three of five explorers is untested anywhere: roots.api.ts, stems.api.ts, and lemmas.api.ts have no spec files, and every consumer suite (roots/stems/lemmas explorer page specs, detail controller specs, facade specs) replaces those services with vi.fn() stubs (e.g. roots-explorer-page.component.spec.ts:77-107 repeated per describe). word-types.api and unique-words.api have specs, and the word-types page/facade suites exercise the true HTTP boundary via HttpTestingController, so the gap is only in these three explorers.

**Why it matters:** Test-guard Rule 2 (jest.md): mock at the true boundary, not your own fetch wrapper. getRootsList carries real logic (search trim + conditional param, appendRangeApiParams wiring, sort/page serialization) and every endpoint has a hand-built URL (`/api/words/roots/${id}/words/${view}` etc.). A typo'd path, dropped param, or broken range wiring passes the entire 23k-line suite green — a can-pass-while-broken seam on the frontend↔backend contract.

**Suggested fix:** Add thin HttpTestingController specs for roots.api/stems.api/lemmas.api asserting method, path, and params per call (mirror word-types.api.spec.ts), or migrate the three page suites to the word-types FakeServer pattern so the real api services are exercised.

### M29. `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.spec.ts:1`

*category: dry-kiss-yagni · found by: tg-fe-words · verification: CONFIRMED*

**Issue:** Mirrored copy-paste suites across the explorers: stems (1135 lines) vs lemmas (1134 lines) are ~70% line-identical after entity-name substitution (357 differing normalized lines); roots (1230) follows the same shape. Within roots-explorer-page.component.spec.ts the identical 8-method rootsApi stub object plus ~40-line TestBed configuration is re-declared 8 times across describe blocks (stems and lemmas 3 times each).

**Why it matters:** Test-guard Rules 3/4 (bloat, maintenance drag): a shell-level behavior change (toolbar, sort fallback, panel states) now requires 3-5 synchronized edits across 1100+ line files, and reviewers cannot see which lines are explorer-specific vs boilerplate. This is the main driver of the 23.4k-line spec footprint in one feature.

**Suggested fix:** Extract a shared explorer-page spec harness (api-stub factory + configureExplorerPage(config) with per-explorer labels/query-keys/fixtures), and within each file hoist the stub/TestBed setup to one helper reused by all describe blocks.

### M30. `Backend/api/QuranDashboard.Api/Extensions/ServiceCollectionExtensions.cs:18`

*category: api-guidelines · found by: x-api-contract · verification: CONFIRMED*

**Issue:** Model-binding failures bypass the ApiResponse envelope. services.AddControllers() has no ConfigureApiBehaviorOptions (verified: no InvalidModelStateResponseFactory/SuppressModelStateInvalidFilter anywhere in Backend), so [ApiController]'s automatic 400 for typed-parameter binding failures (e.g. ?page=abc against [FromQuery] int? page in UniqueWordsController.cs:59) returns an English ValidationProblemDetails body, not {isSuccess:false, message, errors}.

**Why it matters:** API_GUIDELINES §5 requires all error statuses to reuse the failure envelope and §10 requires Arabic user-facing messages. Every frontend error reader (extractPanelErrorMessage etc.) probes error.error.message, finds none on a ProblemDetails body, and falls back to a misleading generic message — a malformed query on this public API renders as 'no data/connection' instead of a real validation message. Route-constraint 404s ({id:int} given non-int) similarly return an empty body.

**Suggested fix:** Configure ApiBehaviorOptions.InvalidModelStateResponseFactory to return BadRequest(ApiResponse<object>.Fail(...)) with an Arabic message (e.g. a Common.ValidationFailed-style ApiMessages constant), keeping property names English.

### M31. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfAyahStudyReader.cs:117`

*category: quran-safety · found by: x-quran-safety · verification: CONFIRMED*

**Issue:** MapAyahCore coalesces nullable navigation metadata to zero: 'ayah.JuzNumber ?? 0', 'ayah.HizbNumber ?? 0', 'ayah.RubNumber ?? 0' (lines 117-119) into non-nullable AyahCoreDto ints.

**Why it matters:** Quran-data-safety rule 3 verbatim: 'unknown is not zero'. If navigation metadata has not been imported (the columns are nullable by design), the API asserts the ayah is in juz 0 / hizb 0 / rub 0 — fabricated impossible values — instead of surfacing the data gap. The surah-jump-picker path models juzNumber as 'number | null' correctly; this contract does not.

**Suggested fix:** Make JuzNumber/HizbNumber/RubNumber nullable in AyahCoreDto and pass nulls through (frontend already renders controlled '—' placeholders elsewhere), or fail the read explicitly when required navigation metadata is absent.

### M32. `Frontend/quran-dashboard-ui/src/app/features/words/data-access/words-association-options.service.ts:49`

*category: api-integration · found by: x-quran-safety · verification: CONFIRMED*

**Issue:** searchRoots/searchLemmas/wordTypeOptions collapse both transport errors (catchError → of([]) at lines 49, 65, 83) and isSuccess=false responses (lines 44-47, 60-63, 73-75) into an empty options array; explorer-association-filter.component.html has loading and options states but no error state.

**Why it matters:** A backend failure while searching roots/lemmas or loading the word-type catalogue is rendered as an empty picker, indistinguishable from 'no roots match this query' — masking a failed load of Quran-derived data as absence (rule 3: show a controlled error/unknown state, never substitute a plausible empty). All sibling facades (stems/lemmas/roots explorers) correctly set status='error'; only this picker path swallows.

**Suggested fix:** Return a discriminated result (options | error) or add an error signal the picker renders as a distinct 'تعذر تحميل الخيارات' state; keep [] strictly for genuine zero-match responses.

### M33. `Frontend/quran-dashboard-ui/src/styles/_typography.scss:63`

*category: quran-safety · found by: x-quran-safety · verification: CONFIRMED*

**Issue:** 'Mushaf Surah Name' (line 47), 'Mushaf Surah Name V2' (line 55), and 'Mushaf Common' (line 63) are declared with font-display: swap, but the strings rendered in these fonts are ASCII ligature triggers ('surah001', 'header', 'makkah', 'j001' per assets/mushaf-common.ligatures.json and sura-names.ligatures.json, rendered by mushaf-line.component.html lines 9-13).

**Why it matters:** During the swap period on a cold load — and permanently if the woff2 fails to fetch — the Mushaf page renders literal Latin text ('header', 'surah002', 'makkah') in a serif fallback inside the Quran page, corrupting Mushaf rendering. Quran-data-safety rule 6: do not swap Mushaf font rendering in a way that mis-renders glyphs.

**Suggested fix:** Use font-display: block (or preload + font-display: optional, or gate rendering on document.fonts.load) for the three ligature-trigger Mushaf fonts so trigger strings are never painted by a fallback font. Consider the same for 'Uthmanic Hafs' (ayah-marker font, line 39).

## MINOR — cleanup / clarity (83)

### M1. `Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:49`

*category: api-guidelines · found by: be-api · verification: CONFIRMED*

**Issue:** With MapInboundClaims=false, a validated Logto token that carries a claim literally typed 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' would satisfy RequireRole/IsInRole directly, bypassing the database role load. RoleClaimsTransformation's own comment (lines 33-35) identifies token-borne role smuggling as a threat but only defends the idempotency check — the smuggled claim itself is never neutralized and still counts for authorization.

**Why it matters:** Defense-in-depth gap in the roles design's stated threat model: role authority is supposed to be the local database exclusively. Exploitation requires Logto to emit such a claim (custom-claims config), so it is not currently reachable, but the mitigation is one line.

**Suggested fix:** Set options.TokenValidationParameters.RoleClaimType to a dedicated never-issued claim type on the JWT identity (RoleClaimsTransformation's separate identity keeps ClaimTypes.Role default and continues to work), or strip ClaimTypes.Role claims from the token identity in OnTokenValidated.

### M2. `Backend/api/QuranDashboard.Api/Common/ApiMessages.cs:3`

*category: clean-code · found by: be-api · verification: CONFIRMED*

**Issue:** ApiMessages.cs in Common/ has become a single dump of 100+ user-facing constants spanning at least eight unrelated features (Health, Dashboard, Mushaf reader, UniqueWords, Roots, Lemmas, Stems, WordTypes, Access).

**Why it matters:** API_GUIDELINES section 10 says to centralize messages 'close to the owning feature' with only truly shared messages in a shared/common location, and 'do not create broad dumping folders for unrelated messages'. Every new feature grows this one file, and per-feature message ownership/localization boundaries blur.

**Suggested fix:** Keep only genuinely shared messages (UnexpectedError, TooManyRequests, Unauthorized, NotFound, OperationSuccess) in Common; move feature blocks to per-feature message classes next to their controllers (e.g. Controllers/Words/LemmasMessages.cs), preserving the existing constant names.

### M3. `Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:23`

*category: api-guidelines · found by: be-api · verification: CONFIRMED*

**Issue:** GET /api/access/me performs writes: first-login user INSERT and the Owner-email promotion UPDATE happen inside a GET handler (ProvisionCurrentUserHandler → UserProvisioningService.GetOrCreateAsync).

**Why it matters:** API_GUIDELINES section 3 says 'Do not use GET for operations that mutate state'. The operation is a convergent get-or-create so it is idempotent, but GETs are assumed side-effect-free by proxies, retries, and prefetchers, and the guideline is explicit.

**Suggested fix:** Either split provisioning into POST /api/access/me (provision) + GET /api/access/me (read), or record the accepted deviation explicitly in API_GUIDELINES/the Access README since the XML doc already advertises the side effect.

### M4. `Backend/api/QuranDashboard.Api/Extensions/ServiceCollectionExtensions.cs:105`

*category: api-guidelines · found by: be-api · verification: CONFIRMED*

**Issue:** The Vercel-preview CORS rule allows any https origin whose host ends with .vercel.app and merely STARTS WITH the configured prefix ('manhag-qurany'), combined with AllowCredentials().

**Why it matters:** Vercel subdomains are first-come: any Vercel account can deploy a project named e.g. manhag-qurany-phish and obtain a credential-allowed origin. Practical impact is low today because auth is bearer-token (no cookies), but the policy silently becomes dangerous if cookie-based auth or CSRF-sensitive endpoints are ever added.

**Suggested fix:** Anchor the match to the exact preview-URL shape (e.g. prefix + '-' + git-branch pattern + the team scope suffix, or an explicit allowlist per preview), or at minimum document that AllowCredentials must be dropped/re-reviewed before any cookie usage.

### M5. `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:35`

*category: api-guidelines · found by: be-api · verification: PLAUSIBLE*

**Issue:** Owner bootstrap (the highest-privilege role) keys solely on Logto Management API primaryEmail matching BootstrapOwnerEmail. The code asserts everywhere that the email is 'server-verified', but LogtoManagementApiUserProfileSource.cs:49 reads only primaryEmail — no verified flag is fetched or checked, and Logto emails can be synced unverified from social/enterprise connectors depending on tenant config.

**Why it matters:** Least-privilege for Owner rests on an unenforced assumption living entirely in Logto tenant configuration outside this repo. If a social connector that reports unverified emails is ever enabled, anyone registering with the configured owner email is silently provisioned Owner/Active on first /api/access/me call.

**Suggested fix:** Verify the email's verified status where Logto exposes it (e.g. via the user's identities/verification data), or document the invariant as a hard deployment precondition (email/password + verification-code only) next to OwnerBootstrapOptions and fail closed if it cannot be established; alternatively bootstrap Owner by pinned Logto sub instead of email.

### M6. `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:63`

*category: api-guidelines · found by: be-api · verification: CONFIRMED*

**Issue:** ReconcileExistingAsync promotes any existing owner-email user to Owner/Active regardless of current status — including UserStatus.Disabled. A deliberately disabled owner account is silently re-activated and re-elevated on its next login while BootstrapOwnerEmail remains configured.

**Why it matters:** Disabling an account should be authoritative until an admin re-enables it; an auto-re-elevation path that overrides Disabled undermines the status model for the most privileged account (e.g. during compromise response).

**Suggested fix:** Exclude Disabled users from the upgrade path (only promote Pending/role-less accounts), or log a warning and require explicit re-enable before promotion.

### M7. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Foundation/ImportQuranFoundationHandler.cs:41`

*category: quran-safety · found by: be-domain-app · verification: CONFIRMED*

**Issue:** Early-exit paths produce no report artifact: the TablesNotEmpty refusal (line 41) and the source-load failure catch (lines 69-72) return before any reportWriter.WriteAsync call. Mutashabihat refusal paths (lines 33-53) behave the same.

**Why it matters:** The project rule says importers must produce a clear report with validation results, and the newer pipelines (Translations/Navigation) treat refusals and pre-persistence failures as reportable outcomes via BuildRefusal/TryWriteRefusalAsync — so refused older-pipeline runs leave no audit trail while refused newer-pipeline runs do.

**Suggested fix:** Retrofit the refusal-report emitter pattern (BuildRefusal + TryWriteRefusalAsync) into Foundation, Morphology, and Mutashabihat refusal/early-failure paths.

### M8. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Translations/ImportTranslationsHandler.cs:58`

*category: error-handling · found by: be-domain-app · verification: CONFIRMED*

**Issue:** Exception discrimination via message-string equality — catch (InvalidOperationException ex) when (ex.Message == TranslationInvariants.AyahsMissing / TargetsNotEmpty) — appears at 11 sites across 6 pipeline handlers; invariant message constants double as control-flow discriminators.

**Why it matters:** Matching on message text is fragile magic-string control flow: any throw site that wraps or rewords the message silently reroutes to a different handling path (in RebuildDisplayWordsHandler it falls into the catch-all; in ImportMorphologyHandler it escapes unhandled). Typed exceptions already exist in this codebase (TranslationValidationException, NavigationMetadataSourceException) and show the right approach.

**Suggested fix:** Introduce small typed exceptions (e.g. ImportTargetsNotEmptyException, AyahsMissingException) in Application.Abstractions next to the invariants and catch by type; keep the constants purely as user-facing messages.

### M9. `Backend/application/QuranDashboard.Application/Quran/Words/Roots/Queries/GetRootsPage/GetRootsPageHandler.cs:13`

*category: dry-kiss-yagni · found by: be-domain-app · verification: CONFIRMED*

**Issue:** The shared paging contract (MinPage=1, MinPageSize=1, MaxPageSize=1000 for list reads; 100 for detail reads) is independently re-declared as const in each explorer page handler (GetRootsPageHandler, GetLemmasPageHandler, GetStemsPageHandler, GetUniqueWordsPageHandler, ...) and again in WordTypesHandlerValidation (MaxListPageSize/MaxDetailPageSize).

**Why it matters:** One piece of knowledge — the documented 1..1000 list / 1..100 detail paging contract shared by the five Words explorers — has 6+ authoritative representations; changing the cap requires shotgun edits and the explorers can silently drift apart (DRY: single authoritative representation of knowledge).

**Suggested fix:** Move the caps into one shared constants type (e.g. WordsPagingLimits in Application.Abstractions/Quran/Words next to WordSortToken) and reference it from every handler.

### M10. `Backend/infrastructure/QuranDashboard.Infrastructure/Access/LogtoManagementApiUserProfileSource.cs:43`

*category: api-integration · found by: be-infra-core · verification: CONFIRMED*

**Issue:** Both Logto calls rely on bare response.EnsureSuccessStatusCode(): a 401/403 from a rotated or revoked M2M secret, or a 404 for a subject deleted in Logto, surface as a generic HttpRequestException with only a status code and no indication of which Logto call failed or why.

**Why it matters:** This sits on the first-login provisioning path; when it breaks, the operator gets an unactionable generic exception, which contrasts with the carefully specific errors this same class throws for missing configuration (CODING_PRINCIPLES §8).

**Suggested fix:** Check response.IsSuccessStatusCode and throw an InvalidOperationException naming the call (token endpoint vs user fetch), the status code, and the likely cause (e.g. invalid AppId/AppSecret, unknown subject) — without echoing the secret or response body verbatim.

### M11. `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Roots/CachedRootsReader.cs:138`

*category: architecture · found by: be-infra-core · verification: CONFIRMED*

**Issue:** CachedRootsReader has drifted from its Lemmas/Stems siblings: its grouped-words and whole-summary loads skip the CacheLoadGate single-flight that CachedLemmasReader/CachedStemsReader apply to the identical pattern (both citing performance finding B6), so concurrent cold callers each materialize the full roots list. Its ayah-page caching policy also differs (roots caches any non-null page; lemmas/stems cache only non-empty pages).

**Why it matters:** Three copies of the same decorator now encode different stampede protection and different negative-caching behavior for equivalent whole-catalogue reads — inconsistent performance characteristics and exactly the 'same knowledge expressed two different ways' cost DRY warns about.

**Suggested fix:** Route CachedRootsReader's GetOrLoadWholeSummaryAsync and GetOrLoadGroupedWordsAsync through CacheLoadGate.GetOrLoadAsync like Lemmas/Stems, and align the empty-page caching policy across the three readers (pick one policy and document it).

### M12. `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/Roots/RootsCacheKeys.cs:9`

*category: dry-kiss-yagni · found by: be-infra-core · verification: CONFIRMED*

**Issue:** Dead cache plumbing left behind by the whole-list refactor: RootsCacheKeys.Summary(int) and RootsCacheKeys.Words(id, kind, page, pageSize) are referenced nowhere (including tests); LemmasCacheEntryOptions.PagedWords() and StemsCacheEntryOptions.PagedWords() are likewise unused.

**Why it matters:** Unused public key/option factories imply cache entries that no longer exist, misleading readers about what is cached and inviting reuse of stale key schemes (YAGNI / dead-code smell).

**Suggested fix:** Delete RootsCacheKeys.Summary and RootsCacheKeys.Words, and remove the unused PagedWords() factories from LemmasCacheEntryOptions and StemsCacheEntryOptions.

### M13. `Backend/infrastructure/QuranDashboard.Infrastructure/Caching/Quran/Words/WordTypes/CachedWordTypesReader.cs:25`

*category: dry-kiss-yagni · found by: be-infra-core · verification: CONFIRMED*

**Issue:** The identical ~10-line cache-aside block (build key → TryGetValue → load → Set-if-not-null → return) is repeated ~30 times across the cached readers: 11x in CachedWordTypesReader, 6x in CachedRootsReader, plus CachedLemmasReader, CachedStemsReader, CachedUniqueWordsReader, and the five Mushaf decorators.

**Why it matters:** This is an 'identical token sequence of ≥5 non-trivial lines in ≥2 functions' smell far past the Rule of 3 (clean-code-guard dry-kiss-yagni), and the duplication has already produced real drift between siblings (see the CachedRootsReader finding). The knowledge is nameable — 'cache-aside read that never caches a null result' — and CacheLoadGate.GetOrLoadAsync already proves the abstraction shape.

**Suggested fix:** Extract one shared internal helper (e.g. an IMemoryCache extension GetOrLoadAsync<T>(key, loader, entryOptions, ct), optionally with a single-flight variant reusing CacheLoadGate) and collapse each decorator method to a one-line delegation.

### M14. `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/MorphologyImportDependencyInjection.cs:13`

*category: dry-kiss-yagni · found by: be-infra-core · verification: CONFIRMED*

**Issue:** public enum MorphologySourceSelection is declared in the DI registration file but referenced nowhere in the backend (the CLI selects sources via MorphologyImportSourceKeys strings). The class's LegacySourceKey/EnrichedSourceKey constants also merely alias MorphologyImportSourceKeys one line above.

**Why it matters:** Dead public API in a composition-root file; BACKEND_STRUCTURE.md also places types with the feature that owns them, not inside DI wiring, so an unused public enum here is both YAGNI and a placement smell.

**Suggested fix:** Delete MorphologySourceSelection (reintroduce near the morphology import feature if/when a caller actually needs it) and drop the redundant aliasing constants in favor of MorphologyImportSourceKeys directly.

### M15. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Translations/ImportTranslationsHandler.cs:100`

*category: error-handling · found by: be-infra-pipelines · verification: CONFIRMED*

**Issue:** Control flow discriminates exceptions by message equality: `catch (InvalidOperationException ex) when (ex.Message == TranslationInvariants.TargetsNotEmpty)` — a pattern repeated across FullI3rab, Morphology, Mutashabihat, and DisplayRebuilding handlers.

**Why it matters:** Catching by message string is fragile typed-exception emulation: any other InvalidOperationException carrying a coincidentally equal message would be misclassified as a refusal, and renaming the constant silently changes catch behavior. The invariant constants make it workable today, but dedicated exception types (as TranslationValidationException/TranslationSourceException already demonstrate in the same file) are the established, safer pattern here.

**Suggested fix:** Introduce small typed exceptions (e.g. ImportTargetsNotEmptyException, FoundationNotLoadedException) thrown by the writers and catch by type, removing the ex.Message == constant filters.

### M16. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/SimpleI3rabGeneration/GenerateI3rabHandler.cs:74`

*category: error-handling · found by: be-infra-pipelines · verification: CONFIRMED*

**Issue:** ResolveRepositoryRoot silently falls back to Directory.GetCurrentDirectory() when the repo-root probe fails, while the three other copy-pasted implementations (ImportMorphologyHandler, RebuildDisplayWordsHandler, ImportMutashabihatHandler) throw InvalidOperationException in the same situation.

**Why it matters:** A silent CWD fallback means the generation report can land in an arbitrary directory with no error — a silent fallback in a data pipeline whose report is the audit trail. The divergence among four duplicated copies of the same knowledge is exactly the DRY drift the guard warns about.

**Suggested fix:** Deduplicate ResolveRepositoryRoot/ResolveReportOutDir into one shared implementation (behind an Infrastructure abstraction per the Application-layer finding) and make not-found behavior consistently fail-fast.

### M17. `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Corrections/WordLemmaNormalizationApplier.cs:106`

*category: clean-code · found by: be-infra-pipelines · verification: CONFIRMED*

**Issue:** In the default switch case, Fail(true, ...) always throws, so the following `failed++` is unreachable and the summary's FailedOrSkipped counter can only ever be 0.

**Why it matters:** The report field 'Failed or skipped: N' (rendered by MarkdownJsonMorphologyReportWriter) is structurally pinned to zero, which misleads a reader into thinking failures are being counted when they actually abort the whole apply; dead code also hides the real fail-fast contract.

**Suggested fix:** Delete the unreachable `failed++; break;` and either remove FailedOrSkipped from the summary or document that apply is all-or-nothing (any failing entry throws), so the report field reflects reality.

### M18. `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Enriched/EnrichedDimensionBuilder.cs:531`

*category: dry-kiss-yagni · found by: be-infra-pipelines · verification: CONFIRMED*

**Issue:** MapVerbTense, MapVerbVoice, MapCaseFeature, ParseFeatureTokens and BuildFeaturesJson are copied verbatim from MorphologyAssembler (the section is even titled 'mirrors the legacy assembler's pure logic').

**Why it matters:** The Corpus feature-token → tense/voice/case mapping is linguistic business knowledge; if one copy is ever corrected the two live import pathways would silently diverge in the morphology they persist. The README's parity rationale (keeping the pathways separate) does not require duplicating these pure, pathway-independent functions.

**Suggested fix:** Move the five pure feature-mapping helpers into one shared internal class (e.g. MorphologyFeatureMapping) used by both assemblers; this does not affect legacy/enriched parity.

### M19. `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/MorphologyAssembler.cs:5`

*category: threshold · found by: be-infra-pipelines · verification: CONFIRMED*

**Issue:** MorphologyAssembler (701 lines) and Enriched/EnrichedDimensionBuilder.cs (701 lines) both exceed the 450-line hard threshold; each mixes word projection, dimension index minting, per-segment dimension resolution, and pure feature mapping in one class.

**Why it matters:** Both are over the hard threshold that requires a split; responsibilities are separable (dimension minting vs segment resolution vs feature mapping). Mitigating: both are well-documented, fail-closed, and the enriched/legacy dual-path is an explicitly documented transitional state in the MorphologyImporting README, so this is cleanup rather than risk.

**Suggested fix:** When the documented legacy-path cleanup phase happens, split each into a dimension-index builder, a segment-dimension resolver, and shared pure feature-mapping helpers; do not restructure the dual pathway before that phase.

### M20. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologySql.cs:204`

*category: quran-safety · found by: be-infra-pipelines · verification: CONFIRMED*

**Issue:** CheckSegLemmaNoFanout is literally `SELECT 0::int` — the 'schema_violations' half of the SEG-LEMMA-ID-NO-FANOUT hard check is a constant zero presented in the report as a measured DB observation.

**Why it matters:** The report prints 'schema_violations=0' as if the database was checked; the real signal comes only from the assembler's in-memory resolver issues. Fanout may indeed be structurally impossible (one lemma_id column per segment), but the constant query silently encodes that assumption without stating it, which is misleading in a validation report that admins trust.

**Suggested fix:** Either drop the SQL half and report only resolver_issues for this check, or replace the constant with a comment-documented explanation and change the observed text to 'structurally impossible (single lemma_id column)' instead of a fake count.

### M21. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/Words/MorphologyImporting/MorphologyValidationRunner.cs:778`

*category: quran-safety · found by: be-infra-pipelines · verification: CONFIRMED*

**Issue:** The hard check SEG-STEM-ID-MULTI-STEM-CURATED passes vacuously when the curated artifact covers zero DB secondary-STEM segments: Violations counts Uncovered only when CoveredPresent > 0, a tolerance the comment says exists for synthetic test data.

**Why it matters:** If artifact location formats ever drift from segment_location (same drift would also make MorphologyAssembler.ResolveStemId assign null stems), every secondary segment becomes 'uncovered', CoveredPresent = 0, and the check PASSES while all 479 approved stem links are silently missing. SEG-STEM-ID-REQUIRED-FOR-STEM only covers single/primary stems and SEG-STEM-ID-ARTIFACT-SHAPE only inspects the artifact, so nothing else fails. This hides missing Quran morphology data under a pass verdict, and production validation was weakened to accommodate tests.

**Suggested fix:** Make the check fail (or at minimum add a hard companion check) when SecondaryPresent > 0 and CoveredPresent == 0, e.g. assert ApprovedApplied equals MorphologyInvariants.ExpectedApprovedSecondaryStems for the real import; gate the synthetic-data tolerance behind an explicit test-only expected-counts parameter instead of inferring it.

### M22. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/EfUniqueWordsReader.cs:385`

*category: ui-style · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** The POS-category → Arabic broad-label mapping is hardcoded twice in the persistence layer with diverging values: ResolvePrimaryWordTypeBroadLabel maps particle → "حرف" and unknown → null, while EfWordTypesReader.ResolveBroadLabelFromCategory (EfWordTypesReader.cs:526-532) and ResolveBroadLabel (Sql.cs:430-436) map particle → "حرف وأداة" and unknown → "اسم"; the INL → "حروف مقطّعة" special case is repeated in all three, and the tree/case/tense option labels (EfWordTypesReader.cs:469-489) plus the NoType placeholder "غير محدَّد" (Lemmas/Stems derivations) scatter more user-facing Arabic vocabulary across infrastructure files.

**Why it matters:** User-facing label vocabulary should be centralized near the owning feature (Backend CLAUDE.md response-message rule); duplicated mappings with different outputs for the same category mean the Unique Words chip and the Word Types badge can disagree on the same word's broad type, and the unknown-category fallbacks differ (null vs silently 'اسم').

**Suggested fix:** Confirm whether حرف vs حرف وأداة is an intentional per-page distinction; either way pull the category→broad-label mapping (and the INL special case) into one shared vocabulary class in the Words read folder (or Application.Abstractions) consumed by both readers, and make the unknown-category behavior explicit and identical.

### M23. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs:273`

*category: ai-failure-mode · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** The whole-summary SQL computes NormalizedLemmaText via replace(translate(lower(...), @foldFrom, @foldTo)) and binds both fold parameters (lines 322-323), but LoadWholeSummaryAsync never reads a.NormalizedLemmaText — line 367 recomputes the value in C# via LemmasListDerivation.NormalizeArabicQuery(a.LemmaText). EfStemsReader.Summary.cs has the identical dead projection (line 29 computed, line 104 recomputed).

**Why it matters:** Dead code (AI failure mode #11): the SQL column, its two parameters, and the record field are computed and transferred for every row of both catalogues and then discarded, while leaving two competing normalization implementations alive inside the same read — a future editor cannot tell which one is authoritative.

**Suggested fix:** Delete the normalized-text column from both SQL projections, the foldFrom/foldTo parameters, and the NormalizedLemmaText/NormalizedStemText fields on LemmaAggregationRow/StemAggregationRow; keep only the C# normalization (or vice versa, but pick one).

### M24. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/EfRootsReader.cs:355`

*category: error-handling · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** LoadWholeSummaryAsync selects agg.ayahs_count, agg.surahs_count, agg.simple_words_count, agg.tashkeel_words_count, agg.stems_count from a LEFT JOIN without COALESCE, yet maps them into non-nullable ints on RootSummaryRow. Line 359 wraps only distinct_lemmas_count in COALESCE(agg…, r.distinct_lemmas_count) — proving the authors anticipated agg being NULL for a root with no morphology head rows — while the five neighboring columns would throw on NULL.

**Why it matters:** If a single orphaned quran_roots row ever exists (data drift, partial import), Npgsql fails to read NULL into int and the ENTIRE roots catalogue read throws — every Roots list/summary endpoint 500s, not just one row. The sibling Lemmas/Stems whole-summary SQL COALESCEs every aggregate to 0; this query is internally inconsistent with both.

**Suggested fix:** Either COALESCE(agg.x, 0) all five aggregate columns (matching EfLemmasReader/EfStemsReader.Summary), or make the join INNER and delete the line-359 fallback so the invariant 'every root has morphology heads' fails loudly and consistently.

### M25. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Roots/EfRootsReader.cs:153`

*category: error-handling · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** GetRootAyahMatchesAsync pages with .Skip((page - 1) * pageSize) in int arithmetic; EfUniqueWordsReader.GetAyahMatchesAsync (line 193) does the same. The handler validates only page >= 1 and pageSize <= 1000 (GetRootAyahsHandler), so page >= 2,147,485 with pageSize 1000 overflows int to a negative skip, which PostgreSQL rejects ('OFFSET must not be negative') — an uncontrolled 500 on a publicly browsable endpoint. ReadPaging.CalculateSafeSkip exists precisely to do this math in long and short-circuit out-of-range pages, and Lemmas/Stems/WordTypes ayah reads all use it.

**Why it matters:** Remotely triggerable unhandled exception with inputs the validation accepts, in a codebase whose contract is controlled 400s/empty pages; also an inconsistency where two of the five explorers silently lack the guard their siblings have.

**Suggested fix:** Use ReadPaging.CalculateSafeSkip(page, pageSize, totalCount) in EfRootsReader.GetRootAyahMatchesAsync and EfUniqueWordsReader.GetAyahMatchesAsync, returning the empty PagedResult when it yields null (this also removes the pointless page query when skip >= totalCount).

### M26. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Stems/EfStemsReader.cs:68`

*category: clean-code · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** The magic string "STEM" appears seven times in LINQ predicates in this file (lines 68, 113, 180, 219, 248, 400, 412) and again as 'STEM' inside the StemMatchingSegmentPredicate SQL constant in EfStemsReader.Summary.cs (line 15).

**Why it matters:** CODING_PRINCIPLES §6 forbids magic strings; the segment-kind discriminator is a single piece of knowledge (Domain even has SegmentKind) repeated 8 times in one class — a typo in one predicate would silently return wrong stems rather than fail to compile.

**Suggested fix:** Introduce a private const string StemSegmentKind = "STEM" (or a shared constant next to the SegmentKind enum since the importer files repeat the same literal) and use it in all LINQ predicates and the SQL constant.

### M27. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.Sql.cs:405`

*category: api-integration · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** CaseOrFeatureSelect interpolates the request-derived context.Case directly into the SQL string ($"'{context.Case}'::text"). It is currently safe only because every handler path runs WordTypesHandlerValidation.IsValidFilter (AllowedCases = all/nominative/accusative/genitive/null) before the reader — a non-local invariant two projects away; every other user-influenced value in this file family is a parameter or an enum-switched constant.

**Why it matters:** Defense-in-depth: this is the single spot in the Reads SQL builders where injection safety depends on an Application-layer allowlist instead of being structurally impossible; a future caller (new handler, test harness, cache decorator path) that skips IsValidFilter would create an injection point.

**Suggested fix:** Map the case value through a local exhaustive switch (nominative/accusative/genitive → the literal, anything else → throw), or select the already-bound @caseFilter parameter instead of interpolating, so the reader is injection-safe by construction.

### M28. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs:418`

*category: dry-kiss-yagni · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** The verb-tense vocabulary ('past'/'present'/'imperative') is re-enumerated inline at least five times in the word-types reader alone: MatchedMorphologyQuery's context-code check (418-421, duplicating WordTypeIdentityMatcher.IsVerbContextCode which exists for exactly this), the tense→Arabic-label switch in ResolveTypeLabel (517-523), SecondaryOptions (476-478), VerbTenseChildren (581-584), and the SQL CASE in TypeLabelExpression (EfWordTypesReader.Sql.cs:388) — plus the Application-side AllowedTenses/VerbChildCodes sets.

**Why it matters:** One vocabulary expressed in six unlinked places; the MatchedMorphologyQuery instance is avoidable today (identity.ContextCode is a constant in the expression — hoist a local bool from WordTypeIdentityMatcher.IsVerbContextCode before building the query), and the label map is duplicated between C# and SQL with only comments keeping them aligned.

**Suggested fix:** Hoist the IsVerbContextCode check out of the expression tree, and centralize the tense codes + Arabic labels in one static vocabulary (e.g., extend WordTypeIdentityMatcher/WordTypeRowContext) that SecondaryOptions, VerbTenseChildren, ResolveTypeLabel, and the SQL CASE builder all read from.

### M29. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/WordTypeGrouping.cs:3`

*category: ai-failure-mode · found by: be-infra-reads · verification: CONFIRMED*

**Issue:** The WordTypeGrouping record has zero references anywhere in the Backend solution (grep over all *.cs including tests finds only its own definition).

**Why it matters:** Dead code (AI failure mode #11) — an unused file in a carefully-documented area misleads readers into thinking a grouping value object participates in the read path.

**Suggested fix:** Delete WordTypeGrouping.cs (restore from git if a future feature actually needs it).

### M30. `Frontend/quran-dashboard-ui/src/app/features/dashboard/pages/dashboard-home/dashboard-home.component.html:36`

*category: dry-kiss-yagni · found by: fe-auth-dash · verification: CONFIRMED*

**Issue:** The five dashboard cards hardcode route literals ('/dashboard/mushaf', '/dashboard/words', '/tafsirs', '/gates', '/resources') and Arabic labels ('التفاسير', 'الأبواب', 'المصادر') that duplicate NAV_ITEMS in core/navigation/nav-items.ts, where the same key→route→labelAr pairs are declared as the source of truth, and route-paths.ts already exports MUSHAF_ROUTE_PATH / WORDS_ROUTE_PATH constants.

**Why it matters:** FRONTEND_STRUCTURE.md makes nav-items the stable route/label contract (labels renameable by the owner, routes stable). Duplicating the pairs here means a label rename or route change in nav-items silently drifts from the dashboard cards — exactly the coupling the structure doc warns against.

**Suggested fix:** Build the card list in the component from NAV_ITEMS (filter by key) or at minimum use the exported route-path constants for routerLink values, keeping only the card-specific description text local.

### M31. `Frontend/quran-dashboard-ui/src/app/core/caching/api-response-cache.ts:73`

*category: architecture · found by: fe-core · verification: CONFIRMED*

**Issue:** Cache entries are stored and replayed by reference: getOrLoad()/peek() hand every consumer the same ApiResponse object (and the same nested data/items arrays). Generated DTO interfaces are not readonly, so any consumer that mutates in place (e.g. sorting data.items for display) silently corrupts the cached copy for all later readers.

**Why it matters:** Shared-mutable cache state is a classic stale/corrupt-data source; the bug would surface far from the mutation site and is invisible in the cache's own spec.

**Suggested fix:** Either document the entries as frozen and Object.freeze() on store, or return structuredClone()/shallow-defensive copies from peek()/the cached-hit path.

### M32. `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-line/mushaf-page-marker-visibility.ts:3`

*category: dry-kiss-yagni · found by: fe-mushaf · verification: CONFIRMED*

**Issue:** HIDDEN_MUSHAF_PAGE_MARKER_TYPES contains all four members of the closed markerType union ('juz','hizb','rub','sajda'), so isVisibleMushafPageMarker always returns false, lineMarkers is always empty, and the entire marker render chain (MushafMarkerComponent, its template/scss, mushafJuzNumberLigature, the marker loop in mushaf-line.component.html) is unreachable production code.

**Why it matters:** A filter that excludes every member of its closed vocabulary is a trap: readers (and the feature README, which lists mushaf-marker as a live piece) assume markers render. Backend marker data is fetched, mapped, and then invisibly dropped with no comment explaining the product decision. KISS/YAGNI: dead-but-wired render paths accrete maintenance cost.

**Suggested fix:** If hiding all inline markers is the product decision, remove the marker rendering chain (or the filter plus dead component) and note the decision in the feature README; if it is temporary, add a comment stating why all types are hidden and what re-enables them.

### M33. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.ts:185`

*category: dry-kiss-yagni · found by: fe-mushaf · verification: CONFIRMED*

**Issue:** Dead/unused public surface across the reader state layer: the aggregate `state` computed (facade:185-199) has zero consumers (specs included); `mushafHighlightVerseKey` (facade:183) is a trivial alias of the also-exposed `focusAyahKey`; `panel()`, `wordTab()`, `selectedSegmentLocation()` signals and `setPanel()` are consumed by no component/template (the study context renders both sections unconditionally); MUSHAF_WIDE_DESKTOP_MIN_PX (mushaf-url-sync.ts:37) is an unused export; SimilarAyahsLoadRunner/MutashabihatLoadRunner carry a `timer` field no code ever sets; `sectionFocus` outputs and source-selector's `usedLabel` input are never bound.

**Why it matters:** YAGNI: unconsumed public API inflates the facade's 582-line footprint (already over the 400 soft threshold) and misleads readers about what the UI actually does — e.g. panel/wordTab look like live view modes but are behaviorally inert (URL keys must stay for contract stability, but the unused accessors need not).

**Suggested fix:** Delete the `state` computed, the alias signal, the unused exports/fields/outputs, and either wire panel/wordTab/segment state to real UI or reduce them to URL-contract parsing without public accessors; this alone brings the facade comfortably under the soft threshold.

### M34. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-study-source-catalog.api.mock.ts:1`

*category: architecture · found by: fe-mushaf · verification: CONFIRMED*

**Issue:** A vitest-importing test fixture (import { vi } from 'vitest') lives in the production state/ folder. tsconfig.app.json includes src/**/*.ts and excludes only *.spec.ts, so this file is compiled/type-checked as application code (it stays out of the bundle only because nothing in the app graph imports it).

**Why it matters:** Test infrastructure inside a production source folder blurs the state/ boundary defined in FRONTEND_STRUCTURE.md, and the vitest value-import in app-compiled sources is a latent build hazard (one accidental import from production code ships/breaks the app build).

**Suggested fix:** Move the fixture next to the specs under a testing/ folder or rename it to a pattern excluded from tsconfig.app (e.g. *.spec-fixtures.ts and add the exclusion), keeping the single-source Quran-safe fixture intent.

### M35. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-study-source-catalog.store.ts:44`

*category: api-integration · found by: fe-mushaf · verification: CONFIRMED*

**Issue:** The catalog store passes empty/notFound/connection messages to subscribeToApiLoad but its onSettled discards the resulting loadState entirely — no error signal is exposed. On failure the tafsir/translation/i3rab pickers silently render with empty option lists (source-selector hides the picker when options.length <= 1) with no indication anything failed, until a future component mount retries.

**Why it matters:** API_INTEGRATION_GUIDELINES: 'Do not silently swallow API failures' and every API-backed state must define error behavior. The three Arabic message strings are dead configuration — constructed, never surfaced — which misleads readers into thinking failures are handled.

**Suggested fix:** Expose a minimal ResourceLoadState (or at least an errorMessage signal) from the store and let selected-ayah-section show a calm inline hint when the catalog failed; alternatively drop the dead message parameters and document the silent-degrade decision explicitly.

### M36. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-url-sync.ts:40`

*category: typing · found by: fe-mushaf · verification: CONFIRMED*

**Issue:** clampMushafPageNumber uses Number(raw) and clamps to [1, 604] without integer normalization, so '?page=5.7' (or typing 5.7 in the page-jump input, which routes through the same function via commitPageEdit) yields pageNumber 5.7, which is written back to the URL, used as the cache key, and sent to GET /api/mushaf/pages/5.7. buildUrlEnumCorrections compares Number(raw) === snapshot.pageNumber so no correction fires.

**Why it matters:** The mushaf URL is documented as a shareable contract; a fractional page silently produces a backend 4xx which (per the finding above) renders as 'الصفحة غير موجودة' instead of being normalized to a valid page. Non-numeric input ('abc') silently becomes page 1, which is surprising in the jump input.

**Suggested fix:** Truncate to an integer in clampMushafPageNumber (e.g. Math.trunc after Number, or parseInt) so both hydration and the page-jump input normalize to a real page; the existing buildUrlEnumCorrections comparison will then self-correct fractional URLs.

### M37. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-api-load.helpers.ts:16`

*category: error-handling · found by: fe-mushaf, x-api-contract · verification: CONFIRMED*

**Issue:** mapApiFailureToLoadState maps EVERY 4xx status to { isEmpty: true, errorMessage: backendMessage ?? notFoundMessage }. A 401/403 (auth feature 033 is in progress on this branch) or 429 (per-IP rate limiting just merged, typically with no ApiResponse body) is therefore presented as 'الصفحة غير موجودة' / 'الآية غير موجودة' / 'الكلمة غير موجودة' and flagged isEmpty.

**Why it matters:** Telling a scholar an ayah/page/word 'does not exist' when the real cause is authorization or rate limiting is actively misleading and borders on hiding a data problem (Quran data safety: never misrepresent missing data). It also conflates empty and error semantics: isEmpty=true always arrives with a non-null errorMessage, so the dedicated qd-empty-state branches after the errorMessage check are unreachable, violating the API guidelines' 'empty state should be explicit' rule.

**Suggested fix:** Restrict the notFound/isEmpty mapping to status 404 (optionally 400 for malformed keys); map 401/403/429 and other 4xx to a non-empty error state with an accurate message. Keep isEmpty for genuine no-data outcomes and stop setting errorMessage on true empties so templates can distinguish the two.

### M38. `Frontend/quran-dashboard-ui/src/app/shared/ui/ayah-card/ayah-card.component.scss:10`

*category: ui-style · found by: fe-shared-styles · verification: CONFIRMED*

**Issue:** The qdAyahCard frame uses the dedicated recessed token `--qd-ayah-card-bg` (defined _tokens.scss:10, dark override _themes.scss:8), but UI_STYLE_SYSTEM §17's qdAyahCard contract still says the frame owns '`--qd-surface` background'. The shared README was updated; the §17 contract was not.

**Why it matters:** §17 is the 'live contract' consumers are told to compose against; a stale surface spec invites someone to 'fix' the card back to --qd-surface.

**Suggested fix:** Update UI_STYLE_SYSTEM.md §17 (qdAyahCard) to name `--qd-ayah-card-bg` and its rationale.

### M39. `Frontend/quran-dashboard-ui/src/styles/README.md:32`

*category: architecture · found by: fe-shared-styles · verification: CONFIRMED*

**Issue:** The README's partial-groups list and 12-step import order omit `_words-explainer.scss`, which styles.scss imports 7th (between words-explorer-layout and explorer-tables). UI_STYLE_SYSTEM §2's current-state list omits it as well.

**Why it matters:** Workspace rule: a change that alters what a README documents must update that README in the same change; the import order is exactly the boundary this README exists to state.

**Suggested fix:** Add `_words-explainer.scss` to the partial groups and the import-order list (and to the §2 current-state note in UI_STYLE_SYSTEM.md).

### M40. `Frontend/quran-dashboard-ui/src/styles/_components.scss:609`

*category: dry-kiss-yagni · found by: fe-shared-styles · verification: CONFIRMED*

**Issue:** Raw pixel media queries duplicate the canonical Sass breakpoints: `_components.scss` imports `bp` and uses `bp.$qd-bp-tablet-max` at line 435 but hardcodes `@media (max-width: 1023px)` at 609 and `767px` at 683; same pattern in shared/ui/pagination/pagination.component.scss:84, _layout.scss:62 (420px, an undocumented fourth breakpoint), and ~10 feature component SCSS files (top-navbar, mushaf-reader-page, study-context-section, words-hub-page, …).

**Why it matters:** The whole point of `_breakpoints.scss` + the breakpoints.ts mirror (README invariant: 'keep breakpoint values synchronized') is defeated when the values are re-typed; a future breakpoint change silently misses these sites (DRY).

**Suggested fix:** Systematically use `@use 'breakpoints' as bp` values in every media query; decide whether the 420px micro-phone band is canon (add it to _breakpoints.scss and breakpoints.ts) or fold it into the phone band.

### M41. `Frontend/quran-dashboard-ui/src/styles/_explorer-detail-lists.scss:180`

*category: architecture · found by: fe-shared-styles · verification: CONFIRMED*

**Issue:** UI_STYLE_SYSTEM §17's `.qd-detail-list` contract states 'per-component SCSS supplies only grid-template-columns and column extras', but the grid templates and column extras for all 10 families (`.root-words-list__header`, `.stem-lemmas-list__row`, …) are centralized in this global partial (lines 180–332), alongside per-family element selectors (`qd-root-words-list` etc., lines 105–116). The five explorer tables follow the opposite (documented) pattern — their `grid-template-columns` live in each component's SCSS.

**Why it matters:** The live §17 contract and the implementation disagree, and the two sibling class families (.qd-explorer-table vs .qd-detail-list) split the same responsibility in opposite directions — the next contributor cannot tell where a new list's columns belong.

**Suggested fix:** Either move the per-family grid templates back into each list component's SCSS to match §17 and the table pattern, or amend §17 to state that detail-list column templates are centralized in _explorer-detail-lists.scss.

### M42. `Frontend/quran-dashboard-ui/src/styles/_words-explorer-layout.scss:3`

*category: ui-style · found by: fe-shared-styles · verification: CONFIRMED*

**Issue:** Reusable global classes without the mandated `qd-` prefix: `.uw-intro-band` / `.uw-toolbar-recess` here, and `.explorer-panel-header` (_components.scss:303), `.explorer-detail-panel` (:342), `.explorer-detail-modal` (:400) — mixed in the same files with correctly-prefixed `qd-` classes.

**Why it matters:** UI_STYLE_SYSTEM §3: 'All reusable global UI classes must use the qd- prefix'; unprefixed globals risk collisions and make the reusable surface harder to grep.

**Suggested fix:** Rename to `qd-` equivalents (e.g. `.qd-explorer-panel-header`, `.qd-words-intro-band`) in a mechanical sweep of the small number of call-sites, or record an explicit exception in the style doc.

### M43. `Frontend/quran-dashboard-ui/src/app/features/words/data-access/words-association-options.service.ts:49`

*category: error-handling · found by: fe-words-state · verification: CONFIRMED*

**Issue:** All three option loaders (searchRoots, searchLemmas, wordTypeOptions) map both transport errors and isSuccess=false responses to an empty options array via catchError(() => of([])) / `: []`, making a failed load indistinguishable from a genuine zero-match result for the association pickers.

**Why it matters:** API_INTEGRATION_GUIDELINES.md: "Do not silently swallow API failures" and empty state "should be explicit and not confused with" failure. An admin filtering by root during a backend blip sees a silently empty picker and may conclude the root does not exist.

**Suggested fix:** Return a small discriminated result ({ status: 'success' | 'error', options }) or let the error propagate so the picker component can show its لا توجد نتائج vs تعذّر التحميل states distinctly.

### M44. `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-url-sync.ts:144`

*category: dry-kiss-yagni · found by: fe-words-state · verification: CONFIRMED*

**Issue:** A private parsePositiveInt is re-declared in five url-sync files (lemmas-url-sync.ts:144, roots-url-sync.ts:130, stems-url-sync.ts:151, unique-words-url-sync.ts:157, word-types-url-sync.ts:365) and normalizeOptionalText in three, while the byte-identical shared parsePositiveIntParam already exists in words-association-filters.ts:18 — and is even imported by the very same files (e.g. lemmas-url-sync.ts:5 imports it, then defines its own copy).

**Why it matters:** Same regex/validation knowledge (`/^[1-9]\d*$/` fail-closed int parse) in six places; a future change (e.g. allowing an upper bound) would have to be found and applied six times, and the coexistence of import + local duplicate inside one file is confusing.

**Suggested fix:** Delete the local copies and use parsePositiveIntParam everywhere; move normalizeOptionalText next to it in a shared url-param helper module.

### M45. `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts:44`

*category: clean-code · found by: fe-words-state · verification: CONFIRMED*

**Issue:** The `_loadedPage` signal is write-only: it is set in handleListResponse (line 224) and resetAccumulatedList (line 238) but never read and never exposed. Together with the resetAccumulatedList name it is a leftover of a removed accumulate-pages design (the facade now replaces items wholesale per page).

**Why it matters:** Dead state misleads readers into thinking page accumulation still exists and adds a signal write on every response for nothing.

**Suggested fix:** Delete `_loadedPage` and rename resetAccumulatedList to resetList.

### M46. `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail.facade.ts:74`

*category: threshold · found by: fe-words-state · verification: CONFIRMED*

**Issue:** At 530 lines this facade exceeds the state-service soft threshold (400) and, unlike the other four explorers, still owns two distinguishable responsibilities in one file: route binding/URL-state dedupe AND full orchestration for both word-kind and grouped root/stem/lemma selections (summary descriptors, selection parsing, seven module-level helpers). It also re-implements inline the generation+two-subscription lifecycle that DetailRequestLifecycle was later extracted to own.

**Why it matters:** FRONTEND_STRUCTURE.md requires soft-threshold overruns to be justified or split by state slice; the README notes the non-refactor was accepted for Feature 029, but the file keeps absorbing growth (retry, presence flags) and its hand-rolled generation guard is a second copy of lifecycle knowledge that the controllers already share.

**Suggested fix:** When next touched, split grouped-selection orchestration from word-kind orchestration (WordTypesDetailController already exists for word-kind and could be reused as the facade's word path), and adopt DetailRequestLifecycle instead of the inline generation/summarySub/detailSub trio.

### M47. `Frontend/quran-dashboard-ui/src/app/features/words/components/unique-words-table/unique-words-table.component.html:177`

*category: dry-kiss-yagni · found by: fe-words-ui · verification: CONFIRMED*

**Issue:** The full ~70-line row markup (row number, word button, type cell, root link, four count chips) is duplicated verbatim between the cdkVirtualFor body (lines 94–175) and the @for fallback body (lines 177–260). The four sibling tables already solve this with a shared row-cells ng-template referenced from both bodies (lemmas `lemmaRowCells`, stems `stemRowCells`, word-types `rowCells`, roots partially via `chipCells`).

**Why it matters:** Same knowledge in two places that must be edited in lockstep — a change applied to one body silently diverges the ResizeObserver-less fallback rendering. It is also why this is the feature's largest template (349 lines, over the 300 soft threshold).

**Suggested fix:** Extract the row cells into one ng-template (as the sibling tables do) and reference it from both the virtual and fallback bodies; this also brings the template back near the soft threshold.

### M48. `Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.ts:136`

*category: dry-kiss-yagni · found by: fe-words-ui · verification: CONFIRMED*

**Issue:** The desktop matchMedia boilerplate — desktopQuery field, onDesktopChange arrow, the typeof window/matchMedia guard + isDesktop.set + addEventListener block, and the removeEventListener teardown — is copy-pasted verbatim in all five explorer pages (roots 57–58/136–140/148, lemmas 65–66/171–175/184, stems 67–68/187–191/201, word-types 119–120/218–222/230, unique-words 133–136/237–241/250).

**Why it matters:** Pure mechanical duplication of one piece of knowledge ('am I at the desktop breakpoint, as a signal') — past the Rule of 3 at five occurrences — that also repeats a manual-teardown bug surface in every page. This is not part of the intentional parallel-explorer pattern; the breakpoint constant is already shared.

**Suggested fix:** Add a shared helper next to QD_BP_DESKTOP_MIN_QUERY (e.g. an injectable or factory returning a cleanup-managed isDesktop Signal<boolean>) and replace the five copies; the five tables' row-height matchMedia blocks can reuse the same helper with a different query.

### M49. `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.html:179`

*category: ui-style · found by: fe-words-ui · verification: CONFIRMED*

**Issue:** The words/surahs sub-view switchers are hand-rolled role="tablist"/role="tab" strips built from qd-btn qd-btn-secondary buttons with aria-selected but no roving tabindex and no Arrow/Home/End key handling — duplicated six times (stems html 178–219, and the matching blocks in roots-explorer-page.component.html and lemmas-explorer-page.component.html), while UI_STYLE_SYSTEM §17 declares qd-tabs 'the one tab-strip implementation app-wide' (RTL-aware keyboard nav, roving tabindex) and unique-words-tabs already composes it.

**Why it matters:** Violates the live §17 contract ('compose it — do not re-style it or hand-roll an equivalent') and ships a non-conforming ARIA tabs pattern: every tab is a Tab stop and arrow keys do nothing, inconsistent with the properly-behaving tab strips elsewhere in the same feature. Six duplicated copies also drift independently.

**Suggested fix:** Replace the six hand-rolled strips with the shared qd-tabs component (or, minimally, the qd-tabs backing classes plus roving tabindex/keydown like word-type-table-view-tabs), keeping the existing testids and emit wiring.

### M50. `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts:169`

*category: dry-kiss-yagni · found by: fe-words-ui · verification: CONFIRMED*

**Issue:** The association-picker search pipeline (loading.set(true) trigger + Subject + debounceTime(300) + switchMap(associationOptions.searchX) + catchError(() => of([])) + subscribe setting options/loading) is duplicated four times: unique-words-page.component.ts:227–234 (roots), lemmas-explorer-page.component.ts:162–170 (roots), stems-explorer-page.component.ts:169–177 (roots) and 178–186 (lemmas), each with its own Subject/Subscription/options/loading/selectedLabel fields.

**Why it matters:** One piece of knowledge (how a server-searched picker debounces, cancels, fails open, and reports loading) has four representations, and it puts RxJS API orchestration in page shells that FRONTEND_STRUCTURE says should lean on facade/state helpers. A behavior change (debounce time, error handling) requires four synchronized edits.

**Suggested fix:** Extract a small reusable picker-options controller (e.g. in state/ or on WordsAssociationOptionsService: takes a search fn, exposes options/loading signals and a search(term) method, owns teardown) and instantiate it per picker in the three pages.

### M51. `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts:299`

*category: dry-kiss-yagni · found by: fe-words-ui · verification: CONFIRMED*

**Issue:** The view→column mapping trio — defaultColumnForView/defaultColumnForEvent switch, syncTableFocusToPanelView, and commitCountOpened's wordView/surahView defaulting — is repeated across roots (221–239), lemmas (270–287), stems (294–311) with only the entity name and one column ('lemmas' vs 'stems') differing.

**Why it matters:** The rule 'words view maps to simple/tashkeel column, surahs to surahs, ayahs to occurrences, related views to their column' is one piece of knowledge encoded three times; it already half-lives in utils/explorer-count-active.resolveMorphologyActiveColumn, so a change (as happened repeatedly in features 029/030 per the README history) must be made in four places. The overall parallel-page structure is intentional and should stay — only this mapping is shared knowledge.

**Suggested fix:** Move the mapping into utils/explorer-count-active (or the ExplorerTableFocusController config) as one generic morphologyDefaultColumnForView(view, wordView) parameterized by the page's excluded column, and have the three pages delegate; do not introduce a page base class.

### M52. `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts:382`

*category: clean-code · found by: fe-words-ui · verification: CONFIRMED*

**Issue:** mentionedSurahs() and missingSurahs() are plain methods called from the template (word-types-explorer-page.component.html:235/239) that .map() fresh arrays on every change-detection pass, while the sibling derivations in the same class (activeSummary, memberWordsForView, ayahsPageForView, ayahParentFrame lines 176–179) are computed() signals over the same panelState.

**Why it matters:** Every unrelated CD cycle allocates new arrays and hands the OnPush surah-list children new input references, re-rendering them needlessly; the inconsistency with the four adjacent computed() derivations also invites the wrong pattern to be copied.

**Suggested fix:** Convert both to computed(() => wordTypeMentionedSurahViews(this.panelState())) / computed(... missing ...) like their siblings, and update the two template call sites.

### M53. `Backend/api/QuranDashboard.Api/Controllers/System/HealthController.cs:25`

*category: test-coverage · found by: tg-be-api · verification: CONFIRMED*

**Issue:** The health endpoint's custom logic — HealthStatus→string mapping, HealthOk vs HealthDegraded message selection, per-check items, and the fact that it returns 200 even when unhealthy — has zero behavioral test coverage. RateLimitingApiFactory stubs the DB check to always-healthy and asserts only status codes; AccessMeEndpointTests.PublicEndpoint_WithoutToken_StillOk asserts only 200. No test ever exercises a Degraded/Unhealthy report or asserts the payload (status string, checks list) or the envelope message.

**Why it matters:** All existing tests pass if MapStatus returns wrong strings, the message switch is inverted, or the checks list is dropped. The unhealthy case is the entire point of a health endpoint, and the untested always-200-when-unhealthy behavior is exactly what platform health checking depends on; today that contract is defined by nothing.

**Suggested fix:** Add a WebApplicationFactory test that registers a stub Unhealthy (and Degraded) health check and asserts: HTTP status, envelope message (HealthDegraded), data.status == "unhealthy"/"degraded", and per-check items; plus one healthy-path payload assertion (data.status == "healthy", checks contains "database").

### M54. `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/LemmasListReadTests.cs:577`

*category: test-quality · found by: tg-be-api · verification: CONFIRMED*

**Issue:** GetLemmasPage_log_carries_required_fields_and_no_lemma_or_search_text searches with "كلمة" but its leak assertions check NotContain("نِعْمَة")/NotContain("نعمة") — L501's lemma text, which the search filters OUT of the result set and which never appears in a list-operation log anyway. The actual search text "كلمة" is never asserted absent, so the test passes even if the raw search term (or the full payload) leaks into the log.

**Why it matters:** Vacuous assertion / false-positive risk: the test's name claims a no-leak guarantee it does not check. The contract is only safe because MorphologyExplorersLoggingTests.GetLemmasPage_with_search_logs_hasSearch_without_raw_search_text independently asserts AssertNoText(entry, "كلمة") correctly — this duplicate is misleading dead weight (test-guard Rule 4; the Roots and UniqueWords logging suites assert their real search text).

**Suggested fix:** Change the assertions to NotContain("كلمة") on message and structured-field values (matching AssertNoText in MorphologyExplorersLoggingTests), or delete the leak assertions from this test and let the dedicated logging suite own that contract.

### M55. `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/LemmasListReadTests.cs:55`

*category: test-quality · found by: tg-be-api · verification: CONFIRMED*

**Issue:** GetLemmasPage_carries_all_counts_for_every_lemma iterates page.Items.Where(i => i.OccurrencesCount > 0) and then asserts OccurrencesCount > 0 inside the loop. The filter makes that assertion tautological, and if a derivation regression zeroed every lemma's counts the loop body never executes and the test passes green.

**Why it matters:** A test named "for_every_lemma" that silently skips lemmas with broken counts can pass while the exact behavior it names is broken. Exact-count tests for IDs 500/503 limit the blast radius, but this test adds no protection for the remaining seeded lemmas it claims to cover.

**Suggested fix:** Materialize the filtered list and assert it is non-empty with the expected membership (e.g. equal to the seeded IDs minus the known zero-stem compound/marker IDs), or drop the filter and branch the StemsCount expectation on the known exception IDs as it already does.

### M56. `Backend/tests/QuranDashboard.Tests/Quran/Import/ImportCountsTests.cs:22`

*category: quran-safety · found by: tg-be-pipelines · verification: CONFIRMED*

**Issue:** Foundation import tests pass ReportOutDir: null (also ImportReconstructionTests.cs:23, ImlaeiCleanKeyImportTests.cs:25, and DisplayWordsRealImportFixture.cs:99), so ImportQuranFoundationHandler defaults to <sourceParent>/../report = resources/report/. Verified: resources/report/quran-foundation-import-report.json/.md were rewritten today 19:25 by the test run.

**Why it matters:** Even though these tests import the real staged package (so counts look genuine), each test run silently replaces the real import-run evidence (run timestamp, environment) with a test-container run's report — provenance is no longer trustworthy.

**Suggested fix:** Pass an explicit per-test temp ReportOutDir in these tests and in DisplayWordsRealImportFixture.ImportAndRebuildAsync (the pattern ValidationReportTests.cs:19 already uses), and restore the genuine report files.

### M57. `Backend/tests/QuranDashboard.Tests/Quran/Import/ImportTestFixture.cs:26`

*category: test-quality · found by: tg-be-pipelines · verification: CONFIRMED*

**Issue:** InitializeAsync throws DirectoryNotFoundException when the gitignored resources/import-sources/quran-foundation package is missing, and the foundation tests use plain [Fact], so on a fresh clone/CI the whole ImportTestCollection errors as failures instead of skipping. The WordsDisplay area already solved this with CanonicalImportSourceTestGate + CanonicalImportSourceFactAttribute (skip with reason).

**Why it matters:** Workspace rules state resources/ is local and not available in other clones; hard-failing turns an expected environment gap into red test noise and hides real failures, while the sibling suite skips cleanly.

**Suggested fix:** Reuse the CanonicalImportSourceTestGate skip-attribute pattern for all foundation-import test classes so missing local source data yields skips with a reason, not errors.

### M58. `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTestFixture.cs:150`

*category: test-quality · found by: tg-be-pipelines · verification: CONFIRMED*

**Issue:** WriteSyntheticSourceFolderAsync (and the WriteSourceFolderWith* variants) create morph-source-* directories under Path.GetTempPath() but never track or delete them; every test run leaks dozens of temp source trees. TafsirImportTestFixture (tempDirs list + DisposeAsync cleanup) and FullI3rabSyntheticPackage (IDisposable) show the established cleanup pattern.

**Why it matters:** Unbounded temp accumulation across runs on dev machines/CI; inconsistent with the cleanup discipline the sibling fixtures already implement.

**Suggested fix:** Track created directories in a list and delete them in DisposeAsync, mirroring TafsirImportTestFixture.

### M59. `Backend/tests/QuranDashboard.Tests/Quran/WordsSimpleI3rab/I3rabGenerationTestFixture.cs:75`

*category: quran-safety · found by: tg-be-pipelines · verification: CONFIRMED*

**Issue:** RunGenerationAsync forwards a null reportOutDir into GenerateI3rabCommand; GenerateI3rabHandler.ResolveReportOutDir defaults to <repo>/resources/report/words-simple-i3rab. Tests that omit reportOutDir (e.g. I3rabGenerationTests.cs:103, plus idempotency/refusal/label tests) clobber the canonical generation report. Verified: simple-i3rab-generation-report.json/.md mtimes are today 19:23 (test run).

**Why it matters:** Canonical simple-i3rab generation evidence is silently overwritten by synthetic-fixture runs — provenance rule 4 violation via test infrastructure.

**Suggested fix:** Default reportOutDir to a per-call temp dir inside RunGenerationAsync and restore the real report files.

### M60. `Frontend/quran-dashboard-ui/src/app/core/data-access/dev-latency.interceptor.spec.ts:36`

*category: test-quality · found by: tg-fe-other · verification: CONFIRMED*

**Issue:** 'forwards successful responses after the configured dev latency' runs against the real clock (performance.now, real 350ms delay) and hardcodes 350 instead of importing the interceptor's configured constant.

**Why it matters:** Adds 350ms+ wall time to every suite run for one assertion, and the magic 350 duplicates the production config — if the dev latency value changes, the test fails (or silently under-asserts) for the wrong reason. Clock is a mock-justified boundary (test-guard Rule 2, jest reference).

**Suggested fix:** Use vi.useFakeTimers + advanceTimersByTime around the imported latency constant: assert the response has not been delivered before the window and is delivered exactly after it.

### M61. `Frontend/quran-dashboard-ui/src/app/features/dashboard/pages/dashboard-home/dashboard-home.component.spec.ts:84`

*category: ai-failure-mode · found by: tg-fe-other · verification: PLAUSIBLE*

**Issue:** The 'reserves the loaded badge line box' test re-derives the expected height from constants hardcoded in the test (2 × --qd-space-1 + 0.75rem × 1.4 + 2 × 1px) instead of reading the loaded .qd-badge's own declared styles. Only the skeleton side of the skeleton-height == badge-height invariant is actually checked against anything real.

**Why it matters:** Test can pass while the required behavior is broken: any .qd-badge CSS change (padding token, font-size, line-height, border) silently breaks the no-layout-shift reservation while the test keeps passing, because the badge-side expectation is a test constant, not a measurement. The margin cross-check at lines 97-100 shows the right pattern (compare skeleton vs loaded element), but the height check does not follow it.

**Suggested fix:** Derive badgeLineBoxPx from getComputedStyle of the rendered .qd-badge (padding-block, font-size, line-height, border-width) the way the marginBlockStart cross-check already compares both elements, so drift on either side fails the test.

### M62. `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-ayah-section/selected-ayah-section.component.spec.ts:359`

*category: dry-kiss-yagni · found by: tg-fe-other · verification: CONFIRMED*

**Issue:** The ~45-line FakeResizeObserver class (instances registry, observe/unobserve/disconnect, trigger with getBoundingClientRect spying) is duplicated verbatim in selected-word-section.component.spec.ts lines 466-505.

**Why it matters:** Two hand-rolled copies of a stateful test double invite semantic drift (e.g. trigger() targeting the last observed element) between the two loading-reservation suites that are supposed to test the same mechanism; clean-code-guard DRY applies to test infrastructure too.

**Suggested fix:** Extract a shared helper (e.g. src/app/testing/fake-resize-observer.ts) exporting the class plus the stubGlobal/unstub beforeEach-afterEach pair, and use it from both specs.

### M63. `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.spec.ts:452`

*category: test-quality · found by: tg-fe-other · verification: CONFIRMED*

**Issue:** 'does not flip from failed-empty back to skeleton when loadState reports isEmpty without isLoading' has setup identical to the previous test (lines 438-450) and never performs a state transition — it sets one static state and asserts empty-state text.

**Why it matters:** The name promises a flip/transition regression guard the body cannot catch (test-guard Rules 4/5): it would pass even if a loading→empty transition did re-show the skeleton. It is duplicate coverage with a misleading name.

**Suggested fix:** Either drive the real transition (set isLoading:true, then isEmpty:true, assert no skeleton reappears across detectChanges) or delete the test as redundant with the one above it.

### M64. `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/selected-word-section/selected-word-section.component.spec.ts:647`

*category: test-quality · found by: tg-fe-other · verification: CONFIRMED*

**Issue:** The skeleton/row min-height match test guards only against '0px' (expect(skeletonMinHeight).not.toBe('0px')) before asserting equality. jsdom getComputedStyle returns '' when a declaration is absent, and '' passes the guard, making the equality '' === '' vacuously true.

**Why it matters:** If the shared min-height rule both elements rely on is removed or stops applying under jsdom, both sides compute to '' and the test passes while the reserved-height contract (UI-001 no-shift) is broken. One-sided regressions do fail, so the escape is the both-empty edge.

**Suggested fix:** Tighten the guard to a concrete length pattern, e.g. expect(skeletonMinHeight).toMatch(/^\d+(\.\d+)?(px|rem)$/), before comparing the two values.

### M65. `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/source-selector/source-selector.component.spec.ts:244`

*category: test-quality · found by: tg-fe-other · verification: CONFIRMED*

**Issue:** 'fits the panel max-height to the remaining viewport space' computes the expected value by calling the production function under test: expectedMaxHeight = sourceSelectorPanelMaxHeightPx(viewportHeight, panelTop). Only the clamp-floor case (line 251) pins a literal constant.

**Why it matters:** Self-referential assertion: any bug in the sizing formula is mirrored into the expectation, so the fit behavior itself has no independent coverage — the test only proves the value is wired into the CSS var (test-guard Rule 1/Rule 4).

**Suggested fix:** Assert a literal expected pixel value for the 900/400 case (keeping the wiring assertion), or add a small pure-function spec for sourceSelectorPanelMaxHeightPx with concrete inputs/outputs.

### M66. `Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.spec.ts:75`

*category: dry-kiss-yagni · found by: tg-fe-other · verification: CONFIRMED*

**Issue:** The same ~60-line TestBed provider block (Router stub, MushafPagesApi/AyahStudyApi/WordAnalysisApi/StudySourceCatalogApi mocks with identical page DTO) is repeated verbatim four times (lines 75-135, 156-216, 234-294, 315-375).

**Why it matters:** Pure maintenance drag: a DTO or provider change must be edited in four places, and the file is 4× longer than its content. Sibling specs (mushaf-reader.facade.lifecycle.spec.ts createFacadeTestBed) already model the factored pattern.

**Suggested fix:** Extract one local createReaderPageTestBed(queryParams) helper returning { fixture, facade } and use it in all four tests.

### M67. `Frontend/quran-dashboard-ui/src/app/features/mushaf/pages/mushaf-reader-page/mushaf-reader-page.component.spec.ts:306`

*category: test-quality · found by: tg-fe-other · verification: CONFIRMED*

**Issue:** 'ignores arrow keys when focus is inside an input' calls the component method directly ((componentInstance as unknown as {...}).onDocumentKeydown(event)) with a synthetic event whose target is a detached input, bypassing the actual document-level listener wiring.

**Why it matters:** Rule 1 implementation poke: the test would keep passing if the host/document listener registration changed in a way that never delivers events for focused inputs differently, and the type-cast erases the access-modifier boundary. The sibling test (line 224) proves dispatch works, so this one should use the same real path.

**Suggested fix:** Append a real input to the fixture DOM, focus it, dispatch a bubbling keydown on document, and assert moveSelectedWord was not called.

### M68. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.url-sync.spec.ts:216`

*category: threshold · found by: tg-fe-other · verification: CONFIRMED*

**Issue:** Debounce boundary is asserted with hardcoded 699/700(/3000) literals in eight tests, while the sibling lifecycle spec correctly imports AYAH_STUDY_SWITCH_DELAY_MS / WORD_ANALYSIS_SWITCH_DELAY_MS from the runners.

**Why it matters:** If the switch-delay constant is tuned, eight url-sync tests break with noise (or would mis-assert the boundary if loosely rewritten), while the lifecycle spec keeps tracking the contract. Two files encode the same threshold two different ways.

**Suggested fix:** Import the delay constants in mushaf-reader.facade.url-sync.spec.ts and advance by CONSTANT - 1 / 1, matching the lifecycle spec.

### M69. `Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.spec.ts:333`

*category: test-quality · found by: tg-fe-words · verification: CONFIRMED*

**Issue:** The core drill-in interaction (count chip → open details → URL write) is driven exclusively by private-handler invocation — component['onCountOpened'](...), also onRowSelected/onWordViewChange/onSurahViewChange/onClearSelection — in roots (6 call sites), stems (4), and lemmas (6) page specs. No test clicks a rendered count chip inside these pages. The table components' own specs verify chip-click → countOpened emission, but the page template binding (roots-explorer-page.component.html:97-98 `(countOpened)="onCountOpened($event)"`) is the only glue and is never exercised.

**Why it matters:** Test-guard Rule 1: test behavior from the caller's (user's) perspective. Renaming the output, dropping the template binding, or changing the event payload shape at the seam keeps both the table specs and the page specs green while the primary user interaction is dead in the browser. The word-types page spec proves the right pattern (real DOM click at word-types-explorer-page.component.spec.ts:1105).

**Suggested fix:** In each explorer page suite, replace at least the primary onCountOpened paths with DOM clicks on the rendered chip (querySelector('[data-word-count-column="..."] button').click()) and keep private-handler calls only for debounce/edge variants.

### M70. `Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.spec.ts:820`

*category: test-quality · found by: tg-fe-words · verification: CONFIRMED*

**Issue:** 'renders lemmas and stems as anchors with counts' packs two scenarios into one test, calling getTestBed().resetTestingModule() mid-test (line 832) and manually rebuilding the whole TestBed + stub set to run the second (stems) scenario.

**Why it matters:** Test-guard Rule 3: one scenario per test. A lemmas-view failure aborts the test before the stems assertions run, silently losing stems coverage; the mid-test module reset also bypasses the suite's beforeEach/afterEach isolation contract.

**Suggested fix:** Split into two tests (or an it.each over {view, testids}) each with its own beforeEach-provisioned TestBed.

### M71. `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-detail.controller.spec.ts:76`

*category: test-quality · found by: tg-fe-words · verification: CONFIRMED*

**Issue:** The controller specs stub the internal collaborator StemsDetailViewLoader ({ loadActiveView: vi.fn() }) and assert call counts (e.g. expect(loadActiveView).toHaveBeenCalledTimes(2) at line 100); roots-detail.controller.spec.ts and lemmas-detail.controller.spec.ts follow the same pattern.

**Why it matters:** Test-guard Rule 2: the view loader is an internal class of the same feature, not a system boundary; asserting its call count couples the test to the controller/loader split and breaks on refactor while catching no user-visible bug. Partially mitigated by page-level integration specs and the loader's own spec, but the controller↔loader seam remains mock-shaped.

**Suggested fix:** Construct the real view loader with the stubbed api (the true boundary) and assert the resulting panelState/view content instead of loadActiveView call counts.

### M72. `Frontend/quran-dashboard-ui/src/app/features/words/words.routes.spec.ts:41`

*category: test-quality · found by: tg-fe-words · verification: CONFIRMED*

**Issue:** Config-echo tests: 'redirects unique to unique/tashkeel (config)' asserts route.redirectTo/pathMatch literals, and 'registers the lemmas and stems explorer child routes' asserts path strings exist in WORDS_ROUTES. No test navigates to '/dashboard/words/unique' to observe the redirect actually landing on the tashkeel mode.

**Why it matters:** Test-guard Rules 4/7: these restate the config literal (would pass even if the redirect were mis-nested and non-functional) while the framework guarantee (router executes redirectTo) is not the project's logic. The adjacent navigateByUrl tests show the stronger behavioral pattern.

**Suggested fix:** Replace the literal assertions with a navigateByUrl('/dashboard/words/unique') test asserting the resolved snapshot/mode, and drop the path-exists checks (the page-render tests already fail if routes vanish).

### M73. `Backend/shared/QuranDashboard.Shared/Common/Result.cs:3`

*category: ai-failure-mode · found by: x-ai-failures · verification: CONFIRMED*

**Issue:** Result, Result<T> (Result.cs) and Error (Error.cs) — the entire code content of the QuranDashboard.Shared project — are used by nothing: no 'using QuranDashboard.Shared' exists anywhere in api/application/domain/infrastructure/tools/tests, no GlobalUsings imports it, and Error.None is referenced only by its own declaration; yet five csproj files reference the project.

**Why it matters:** Failure modes #3/#11 (premature abstraction, dead code): a result-pattern abstraction scaffolded before any consumer existed and never wired in — real outcomes use per-feature Outcome types and the ApiResponse envelope. BACKEND_STRUCTURE.md lists Result/Error as *allowed* Shared content, not required, and today they only invite drift from the real error-handling conventions.

**Suggested fix:** Either remove Result/Result<T>/Error (and the now-empty project references) or adopt them where the architecture intends; do not keep a parallel unused error taxonomy.

### M74. `Frontend/quran-dashboard-ui/src/app/features/words/pages/stems-explorer-page/stems-explorer-page.component.ts:172`

*category: ai-failure-mode · found by: x-ai-failures · verification: CONFIRMED*

**Issue:** Four copy-pasted call sites wrap the association-picker search in a redundant catchError: stems-explorer-page.component.ts:172 and :181, lemmas-explorer-page.component.ts:165, unique-words-page.component.ts:230 all do this.associationOptions.searchRoots/searchLemmas(term).pipe(catchError(() => of([]))) — but WordsAssociationOptionsService already terminates every one of those pipelines with catchError(() => of([])) (words-association-options.service.ts:50, :67, :86), so the outer handler can never fire.

**Why it matters:** Failure mode #2 (defensive guard for an impossible case), replicated by copy-paste across three pages. It also cements a double swallow: a picker API outage is indistinguishable from 'no matching roots' at both layers, with no error signal anywhere.

**Suggested fix:** Delete the outer catchError at the four call sites (the service is the single owner of the empty-on-failure contract). If picker failures should be visible, surface a distinct error state from the service instead of [].

### M75. `Frontend/quran-dashboard-ui/src/app/features/words/state/roots-detail-panel.updates.ts:148`

*category: dry-kiss-yagni · found by: x-ai-failures · verification: CONFIRMED*

**Issue:** The 'extract backend ApiResponse message from an HttpErrorResponse body, else fallback' helper is copy-pasted near-verbatim SEVEN times: extractPanelErrorMessage in roots-detail-panel.updates.ts:148, lemmas-detail-panel.updates.ts:128, stems-detail-panel.updates.ts:128, word-types-detail-panel.updates.ts:69; extractDrilldownMessage in features/words/utils/unique-words-drilldown.state.ts:64; and private readApiMessage/resolveErrorMessage duplicated in core/data-access/system.api.ts:78 and core/auth/current-user.store.ts:108 (verbatim copy).

**Why it matters:** Failure mode #5 (duplication instead of reuse): a ≥5-line block replicated across both core and feature layers. A future change to the ApiResponse failure envelope must be found and applied in seven places; the two core copies already predate the auth branch and were copied again rather than extracted.

**Suggested fix:** Extract one shared extractApiErrorMessage(err: unknown, fallback: string) in core/data-access next to api-response.model.ts and delete the six clones (the feature copies can re-export it if the local names aid readability).

### M76. `Frontend/quran-dashboard-ui/src/app/features/words/state/roots-url-sync.ts:130`

*category: dry-kiss-yagni · found by: x-ai-failures · verification: CONFIRMED*

**Issue:** parsePositiveInt is defined seven times with two divergent semantics: private lax copies in roots/lemmas/stems/word-types/unique-words -url-sync.ts (regex then Number.parseInt, no overflow guard), the exported parsePositiveIntParam in words-association-filters.ts:18 (same folder, same lax body, unused by the five neighbors), and detail-overlay-url-codec.ts:69 which alone adds a Number.isSafeInteger guard — with a doc comment explicitly explaining that decimal syntax alone lets an oversized digit run silently round to a different integer.

**Why it matters:** Failure mode #5 + #13 (copy-drift with semantic divergence): the codebase itself documents why the safe-integer guard matters for URL-borne entity ids, yet six of the seven copies of the same parse omit it, so ?rootId=12345678901234567890 parses to a silently rounded id in the explorers while the overlay codec rejects it. The shared helper that already exists in the same directory is not reused.

**Suggested fix:** Keep one shared fail-closed parser (with the safe-integer guard from the overlay codec) in a shared util and have the five url-sync files and words-association-filters delegate to it.

### M77. `Frontend/quran-dashboard-ui/src/app/features/words/state/stems-url-sync.ts:144`

*category: ai-failure-mode · found by: x-ai-failures · verification: CONFIRMED*

**Issue:** buildStemsDeepLink (stems-url-sync.ts:144), buildWordTypesDeepLink (word-types-url-sync.ts:215) and buildUniqueWordsDeepLink (unique-words-url-sync.ts:119, ~35 lines) are exported but have zero production consumers — only their own .spec.ts files reference them. Their twins buildRootsDeepLink/buildLemmasDeepLink ARE consumed (stems-table, lemmas-table, unique-words-table cross-links).

**Why it matters:** Failure mode #11 (dead 'just in case' exports): these were produced for five-way parity with the roots/lemmas builders rather than for a caller, and unlike roleGuard they carry no documented decision-record intent. Tests exercising uncalled code create false coverage confidence.

**Suggested fix:** Delete the three unused builders and their spec coverage, and reintroduce each one with the feature that actually cross-links to that explorer.

### M78. `Frontend/quran-dashboard-ui/src/app/features/words/state/word-types-detail-panel.updates.ts:69`

*category: dry-kiss-yagni · found by: x-api-contract · verification: CONFIRMED*

**Issue:** The knowledge 'how to read the backend ApiResponse message out of an HttpErrorResponse body' is duplicated 8 times with three drifting variants: word-types-detail-panel.updates.ts:69 (truthiness check), roots-detail-panel.updates.ts:148 / stems-detail-panel.updates.ts:131 / lemmas-detail-panel.updates.ts:131 / unique-words-drilldown.state.ts:64 / mushaf-api-load.helpers.ts:11-14 (length > 0), and system.api.ts:78 / current-user.store.ts:108 (trim().length > 0).

**Why it matters:** This is single-contract knowledge (the ApiResponse envelope shape) expressed in 8 places — well past the Rule of 3, and the copies have already diverged in whitespace/empty-message handling. If the envelope contract evolves (e.g. surfacing errors[]), 8 sites must change in lockstep; API_INTEGRATION_GUIDELINES requires ApiResponse<T> handling to stay consistent.

**Suggested fix:** Extract one extractApiErrorMessage(err: unknown, fallback: string) helper in core/data-access next to api-response.model.ts, pick one whitespace rule (trim), and replace the 8 local copies.

### M79. `Backend/api/QuranDashboard.Api/Controllers/Access/AccessController.cs:3`

*category: architecture · found by: x-arch · verification: CONFIRMED*

**Issue:** Api code consumes Domain types directly through the transitive reference: AccessController.cs:3,36-42 switches on Domain enum UserStatus; AuthorizationPolicyNames.cs:13-15 and AuthenticationRegistration.cs:5 use Domain RoleNames. CLEAN_ARCHITECTURE.md's allowed-reference list for QuranDashboard.Api (Application, Application.Abstractions, Infrastructure, Shared) does not include Domain, and Api.csproj declares no Domain reference. The root cause is that the Abstractions contract ProvisionedUser exposes the Domain UserStatus enum, which forces Api onto Domain.

**Why it matters:** The code and the canonical dependency-direction doc contradict each other; either the doc's allowed list is wrong or Domain types are leaking through the Abstractions surface into the HTTP boundary. Unresolved, every future reviewer must re-litigate whether Api-to-Domain usage is a violation.

**Suggested fix:** Pick one: (a) add QuranDashboard.Domain to Api's allowed references in CLEAN_ARCHITECTURE.md (and optionally an explicit ProjectReference so the dependency is visible, not transitive), or (b) keep Domain types out of Abstractions response contracts (ProvisionedUser carries a status string/abstraction-local enum) so Api never touches Domain.

### M80. `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:50`

*category: architecture · found by: x-arch · verification: CONFIRMED*

**Issue:** The Access feature's first-login business policy lives entirely in Infrastructure: UserProvisioningService decides the default new-user policy (Pending, no role, lines 78-89), the owner-bootstrap promotion rule (ReconcileExistingAsync lines 50-71: configured owner email is promoted to Owner/Active), the no-email refusal rule (lines 35-40), and cache-eviction sequencing — while the Application use case (ProvisionCurrentUserHandler.cs:10-16) is a two-line pass-through to the abstraction.

**Why it matters:** CLEAN_ARCHITECTURE.md forbids in Infrastructure 'use-case orchestration that should live in Application' and 'domain rules that should live in Domain', and states Application owns 'use-case orchestration' and 'application-level validation'. With feature 033 (auth/roles/permissions) actively growing, access-control policy will keep accreting below the abstraction line where it can only be tested through a real DbContext and bypasses the Application layer entirely.

**Suggested fix:** Move the policy into the Application layer: have ProvisionCurrentUserHandler orchestrate via finer abstractions (a user store/repository abstraction plus the existing IExternalUserProfileSource and IUserRoleResolver), keeping only EF persistence, the unique-index race recovery, and query mechanics in Infrastructure. Alternatively, if the team deliberately keeps this shape, record the exception in CLEAN_ARCHITECTURE.md so the rule and the code stop contradicting each other.

### M81. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfAyahStudyReader.cs:351`

*category: quran-safety · found by: x-quran-safety · verification: PLAUSIBLE*

**Issue:** MapSajdahType has a defensive default '_ => "required"': any SajdahType value outside Required/Optional is silently reported as an obligatory sajdah.

**Why it matters:** EF Core materializes out-of-range enum ints without validation, so a bad DB value (or a future enum member) would flow here and the API would fabricate a religious ruling ('required' prostration) instead of failing. Never-invent rule plus clean-code-guard ai-failure-modes §12 (hardcoded fallback values in production paths). The import side (NavigationMetadataAssembler.ParseSajdaType) correctly hard-validates; the read side undoes that discipline.

**Suggested fix:** Replace '_ => "required"' with '_ => throw new ArgumentOutOfRangeException(nameof(sajdahType), sajdahType, null)'.

### M82. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs:286`

*category: quran-safety · found by: x-quran-safety · verification: CONFIRMED*

**Issue:** ParseFeaturesJson silently swallows JsonException and returns [] when a morphology segment's stored FeaturesJson is corrupt.

**Why it matters:** Same hide-invalid-data pattern as EfAyahStudyReader: corrupt morphology feature data renders as 'segment has no features' with no log or error, masking import/DB corruption of Quran morphology from operators (rule 3: never hide missing or invalid data).

**Suggested fix:** Let the JsonException propagate or log the segment location and surface an explicit incomplete status (MushafWordAnalysisIncomplete already exists in ApiMessages) instead of returning [].

### M83. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfWordAnalysisReader.cs:218`

*category: quran-safety · found by: x-quran-safety · verification: CONFIRMED*

**Issue:** headPosTag?.ArabicLabel ?? core.HeadPos! (lines 218-219, same pattern for segments at 251-252) substitutes the raw Corpus POS code (e.g. 'PN') as the Arabic and English display label when the POS catalog row is missing.

**Why it matters:** Honest data (the real code) but a silent degradation: a gap in the POS label catalog is masked as a normal-looking label instead of being surfaced, so catalog incompleteness in Quran morphology display goes unnoticed (rule 3, low impact since the code shown is real source data).

**Suggested fix:** Log or count missing catalog rows and/or mark the label as untranslated in the DTO (e.g. labelStatus) so the gap is visible to operators rather than blended into normal output.

## NOTE — observations (55)

### N1. `Backend/api/QuranDashboard.Api/Authentication/AuthenticationRegistration.cs:65`

*category: api-guidelines · found by: be-api · verification: CONFIRMED*

**Issue:** 401 challenges get the shared ApiResponse envelope via UnauthorizedRejectionWriter, but there is no equivalent for 403: once the registered Owner/Admin/Editor policies are applied to endpoints, authorization failures will emit the framework's default empty-body 403.

**Why it matters:** API_GUIDELINES section 5 requires error statuses to reuse the failure envelope. 403 is unreachable today (policies applied to no endpoint), but the policies exist specifically for imminent admin surfaces, and the gap will surface as an inconsistent contract the moment the first [Authorize(Policy=...)] lands.

**Suggested fix:** Handle JwtBearerEvents.OnForbidden (or register an IAuthorizationMiddlewareResultHandler) to write ApiResponse.Fail with a new Arabic ApiMessages.Forbidden, mirroring UnauthorizedRejectionWriter.

### N2. `Backend/api/QuranDashboard.Api/Authentication/RoleClaimsTransformation.cs:48`

*category: error-handling · found by: be-api · verification: NOTE-unverified*

**Issue:** The role resolution DB call passes CancellationToken.None, so on a role-cache miss the query outlives an aborted request.

**Why it matters:** IClaimsTransformation exposes no token, but the class already runs inside a request scope; an aborted request currently still pays the DB round-trip. Impact is bounded by the 30s role cache.

**Suggested fix:** Inject IHttpContextAccessor and pass HttpContext?.RequestAborted ?? CancellationToken.None to GetActiveRoleNameAsync.

### N3. `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs:22`

*category: threshold · found by: be-api · verification: CONFIRMED*

**Issue:** LemmasController.cs is 290 lines and StemsController.cs is 284 — both between the soft (200) and hard (300) controller thresholds. Judgment: a split is NOT warranted today — the bulk is Arabic Swagger XML documentation plus mechanical outcome-to-envelope switches, with zero business logic, EF, or parsing. But both files are within one endpoint of the hard stop, and the sibling RootsController already established the documented partial-split pattern (list part + .Details.cs, same class to preserve OpenAPI tags). Note WordTypesController already totals 349 lines as a class across its two partials (216 + 133), each file individually compliant.

**Why it matters:** BACKEND_STRUCTURE.md treats thresholds as review signals; these controllers are cohesive single-resource surfaces, so a forced split now would be mechanical. The risk is the next added endpoint silently crossing 300 without the required justify-or-split step.

**Suggested fix:** When the next endpoint lands on Lemmas or Stems, apply the RootsController.Details.cs partial split (same class, documented OpenAPI-tag rationale) rather than growing the single file past 300.

### N4. `Backend/api/QuranDashboard.Api/Controllers/Words/LemmasController.cs:31`

*category: dry-kiss-yagni · found by: be-api · verification: NOTE-unverified*

**Issue:** DefaultPage = 1 / DefaultListPageSize = 1000 / DefaultDetailPageSize = 100 are re-declared privately in six Words controllers (Lemmas, Stems, Roots, UniqueWords, WordTypes, WordTypeGroupedDetails).

**Why it matters:** Same magic numbers repeated well past the rule-of-three; if one explorer's default ever diverges intentionally the shared knowledge is lost, and if it diverges accidentally nothing catches it.

**Suggested fix:** Introduce one small WordsPagingDefaults constants type in Controllers/Words/ (feature-local, not a global dumping folder) and reference it from the six controllers.

### N5. `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/Roots/RootSortSpec.cs:71`

*category: dry-kiss-yagni · found by: be-domain-app · verification: NOTE-unverified*

**Issue:** The five sort-spec files (Root/Lemma/Stem/WordType/UniqueWord, ~120-150 lines each) are structurally identical, and within each the column set is spelled three times (enum, ColumnKey switch, TryParseColumn switch); the sibling *WordsHandler families (Root/Lemma/Stem) are likewise near-identical modulo rename.

**Why it matters:** The shared grammar is correctly extracted into WordSortToken and each explorer's column allowlist is legitimately separate knowledge, so this is acceptable per the wrong-abstraction guidance — but the triple in-file spelling means adding one column touches three switch sites and a missed site only surfaces via the throwing default arm at runtime.

**Suggested fix:** No structural change needed now; if a third mechanical change to these files lands, consider a per-file dictionary (key -> column) driving both ColumnKey and TryParseColumn so a column is declared exactly once.

### N6. `Backend/application/QuranDashboard.Application.Abstractions/Quran/Words/WordTypes/IWordTypesReader.cs:6`

*category: solid · found by: be-domain-app · verification: NOTE-unverified*

**Issue:** IWordTypesReader exposes 11 methods (IRootsReader 8, ILemmasReader/IStemsReader 7), each consumed by a different single-method handler, whereas the MushafReader area follows the documented per-use-case reader convention (IMushafPageReader, IAyahStudyReader, ...).

**Why it matters:** Interface-segregation drift between the two feature areas: CLEAN_ARCHITECTURE.md prefers 'focused interfaces such as IMushafPageReader'. The Words readers stay within one bounded feature so this is cohesive, but every handler depends on ten methods it never calls and Infrastructure implementations grow monolithically.

**Suggested fix:** Accept as-is or, when the WordTypes read surface next grows, split along its natural seams (tree/table reads vs identity-detail reads vs grouped reads).

### N7. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Foundation/QuranFoundationAssembler.cs:9`

*category: threshold · found by: be-domain-app · verification: NOTE-unverified*

**Issue:** QuranFoundationAssembler is 313 lines, slightly over the 300-line soft review threshold for application services.

**Why it matters:** BACKEND_STRUCTURE.md treats soft thresholds as review-and-justify signals; this file passes review — it has one cohesive responsibility (assembling foundation entities from source DTOs), every build step is a small focused private method, and all data problems fail loudly with specific InvalidDataException messages (good Quran-data-safety behavior).

**Suggested fix:** No action required; if it grows further, the Build* methods for pages/lines/words are natural extraction seams.

### N8. `Backend/shared/QuranDashboard.Shared/Common/Result.cs:3`

*category: dry-kiss-yagni · found by: be-domain-app · verification: CONFIRMED*

**Issue:** Shared's Result/Result<T>/Error primitives are used nowhere: no file in the entire backend (Api, Application, Abstractions, Infrastructure, Domain, tools) has a 'using QuranDashboard.Shared' directive; handlers use per-use-case outcome types and typed import results instead.

**Why it matters:** Dead speculative code (YAGNI): the project carries and references an assembly whose only two types have zero consumers, while the codebase has standardized on a different outcome pattern. Unused primitives invite accidental divergent adoption later.

**Suggested fix:** Either delete Result/Error (and the Shared project references if nothing else lands there) or record a deliberate decision to adopt them; do not leave them as an unused parallel error channel.

### N9. `Backend/application/QuranDashboard.Application/DependencyInjection.cs:63`

*category: architecture · found by: be-domain-app, x-arch · verification: NOTE-unverified*

**Issue:** AddApplication registers ~60 handlers in one flat method backed by 57 per-feature using directives (lines 2-57), growing by one using plus one registration per new use case; Infrastructure meanwhile splits its wiring into 17 per-feature DI modules under DependencyInjection/.

**Why it matters:** Not a layering violation (explicit registration is legitimate), but the two layers follow different composition conventions and the flat file is a permanent merge-conflict hotspot; a forgotten registration only fails at first runtime resolution.

**Suggested fix:** Optionally mirror the Infrastructure convention with per-feature AddX extension methods (e.g. AddWordsExplorers, AddDataPipelines, AddAccess) composed by AddApplication; a smoke test that resolves every registered handler would also close the forgotten-registration gap.

### N10. `Backend/infrastructure/QuranDashboard.Infrastructure/Access/UserProvisioningService.cs:58`

*category: error-handling · found by: be-infra-core · verification: NOTE-unverified*

**Issue:** If the seeded Owner role row is absent (GetOwnerRoleAsync returns null), a login matching the configured bootstrap owner email is silently provisioned/left as Pending with no role — no log, no warning.

**Why it matters:** The Owner role is HasData-seeded so this only happens on a misapplied database, but that is precisely when an operator needs a signal; silently degrading the owner bootstrap hides the misconfiguration behind a locked-out owner.

**Suggested fix:** Log a warning (or throw, since this state is unreachable on a correctly migrated database) when IsConfiguredOwner matches but the Owner role row cannot be found.

### N11. `Backend/infrastructure/QuranDashboard.Infrastructure/DependencyInjection/AccessDependencyInjection.cs:6`

*category: clean-code · found by: be-infra-core · verification: NOTE-unverified*

**Issue:** All 16 DI extension files live in the DependencyInjection/ folder but declare namespace QuranDashboard.Infrastructure.ServiceRegistration; the root DependencyInjection.cs then imports ServiceRegistration from a folder named DependencyInjection.

**Why it matters:** Folder and namespace disagree everywhere in this area, so type-search by namespace and file-search by path point at different names; it is consistent (deliberate) but undocumented and mildly disorienting for navigation.

**Suggested fix:** Align one to the other — either rename the folder to ServiceRegistration or move the files' namespace to QuranDashboard.Infrastructure.DependencyInjection — in a mechanical, standalone change.

### N12. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Configurations/Access/UserConfiguration.cs:16`

*category: typing · found by: be-infra-core · verification: CONFIRMED*

**Issue:** All string columns on users (logto_sub, email, user_name, display_name, title) are mapped without HasMaxLength, while the sibling RoleConfiguration deliberately bounds name(64)/display_name(128).

**Why it matters:** These values originate outside the system (Logto profile payloads) and land in unbounded text columns with unique indexes on two of them; bounded lengths are cheap defense-in-depth and the Access configurations are currently internally inconsistent about it.

**Suggested fix:** Add sensible HasMaxLength values (e.g. logto_sub 128, email 320, user_name/display_name/title 256) in a follow-up migration, matching the bounded style RoleConfiguration already uses.

### N13. `Backend/application/QuranDashboard.Application/Quran/DataPipelines/Words/DisplayRebuilding/RebuildDisplayWordsHandler.cs:48`

*category: error-handling · found by: be-infra-pipelines · verification: NOTE-unverified*

**Issue:** catch (Exception ex) converts every exception — including OperationCanceledException and unexpected programming errors — into RebuildDisplayWordsResult.Failure(ex.Message).

**Why it matters:** Cancellation should propagate, not be reported as a rebuild failure; and turning NullReferenceException-class bugs into a polite failure result hides defects. Sibling handlers catch specific exception types only.

**Suggested fix:** Narrow to the expected exception types (IO/InvalidData/Npgsql) and let OperationCanceledException and unexpected exceptions propagate, matching the other pipeline handlers.

### N14. `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Navigation/NavigationManifestReader.cs:314`

*category: clean-code · found by: be-infra-pipelines · verification: NOTE-unverified*

**Issue:** ValidateFileSize reports its result under NavigationMetadataInvariants.CheckSourceHash, so a file-size mismatch appears in the report under the hash-check id; TranslationManifestReader.ValidateFileSize (line 364) does the same.

**Why it matters:** Report readers diagnosing a failed run see a 'source hash' failure whose expected/observed are byte counts, not hashes — mildly misleading in the audit trail that these pipelines otherwise keep very precise.

**Suggested fix:** Add a distinct CheckSourceSize invariant id (or fold size into the hash check's expected/observed text explicitly labeled 'sizeBytes=').

### N15. `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Tafsirs/TafsirAssembler.cs:129`

*category: quran-safety · found by: be-infra-pipelines · verification: NOTE-unverified*

**Issue:** MapSourceDto silently defaults missing manifest License and Provenance to the string "unknown".

**Why it matters:** Provenance is part of source traceability; a missing provenance field is quietly normalized rather than surfaced as a warning in the import report, so a staged package that lost its provenance metadata imports indistinguishably from one that declared 'unknown'.

**Suggested fix:** Emit a warning check (soft severity) when License or Provenance is absent from the manifest instead of, or in addition to, the 'unknown' default.

### N16. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/EfAyahStudyReader.cs:351`

*category: quran-safety · found by: be-infra-reads · verification: NOTE-unverified*

**Issue:** MapSajdahType defaults an unhandled SajdahType to "required" (_ => "required"). The enum currently has only Required/Optional, so the arm is unreachable — but it silently asserts the stricter religious ruling for any future unknown value instead of failing loudly.

**Why it matters:** The explorers' own convention for unhandled enum values is to throw ('fails loudly instead of silently serving…'); silently classifying an unknown sajdah as required is exactly the kind of plausible-looking fallback the Quran-safety rules warn against.

**Suggested fix:** Replace the discard arm with a thrown InvalidOperationException naming the unhandled SajdahType value, matching the ApplySort/OrderBy switches.

### N17. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/Lemmas/EfLemmasReader.cs:18`

*category: comments-and-formatting · found by: be-infra-reads · verification: NOTE-unverified*

**Issue:** The class doc says 'the remaining detail methods stay stubbed for later story phases'; EfStemsReader.cs lines 14-16 similarly claim 'the later detail methods remain stubbed'. Every interface method in both classes is fully implemented.

**Why it matters:** Stale comments actively mislead (clean-code-guard comments guidance): a reviewer scanning for unfinished work is pointed at stubs that no longer exist.

**Suggested fix:** Trim both class summaries to describe the current, fully-implemented state.

### N18. `Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/WordTypes/EfWordTypesReader.cs:10`

*category: architecture · found by: be-infra-reads · verification: NOTE-unverified*

**Issue:** EfWordTypesReader is one class spread over six partial files (~1,690 lines total) implementing roughly eleven read operations (tree, rows, table, summary, ayahs, surahs, grouped summary/member-words/ayahs/surahs, scope counts) behind one IWordTypesReader.

**Why it matters:** The partial-split-by-size convention keeps each FILE under threshold (and is documented in the README), but file splitting does not reduce class-level responsibility: the grouped-detail reads (Feature 023) form a cohesive sub-aggregate that could stand as its own reader, and every new word-types feature keeps landing in the same class.

**Suggested fix:** No action required now; when the next word-types read is added, consider carving the grouped-detail reads into a focused reader class (optionally its own interface) rather than a seventh partial.

### N19. `Frontend/quran-dashboard-ui/src/app/features/auth/pages/auth-callback/auth-callback.component.ts:40`

*category: error-handling · found by: fe-auth-dash · verification: CONFIRMED*

**Issue:** currentUserStore.load() is fired here, but its failure state dead-ends: CurrentUserStore.errorMessage is rendered by no component in the app (grep confirmed — the only consumers of the store are this callback and the unattached roleGuard). GET /api/access/me is also the call that auto-provisions the local account on first login, so a provisioning failure immediately after a successful Logto login is completely invisible to the user.

**Why it matters:** The store carefully captures a calm Arabic errorMessage "so it can never crash the callback", but nothing displays it — the error handling is currently write-only. Acceptable for Phase 2 (infrastructure-only, no role-gated UI), but the moment any feature consumes currentUser, a silently unprovisioned account becomes a confusing support case (signed-in navbar, no local account).

**Suggested fix:** Surface the store's errorMessage in the account area (navbar) or as a dismissible dashboard notice, or explicitly tie its surfacing to the first admin feature in the decision record so it cannot ship forgotten.

### N20. `Frontend/quran-dashboard-ui/src/app/features/dashboard/pages/dashboard-home/dashboard-home.component.ts:23`

*category: architecture · found by: fe-auth-dash · verification: NOTE-unverified*

**Issue:** The routeable page calls SystemApi directly and holds view state in plain mutable class fields (viewState/appInfo/errorMessage) rather than signals. This fits API_INTEGRATION_GUIDELINES' documented small-component exception (single simple endpoint, no shared state, no URL sync), but it is now the only API-backed page in the app not using signal-based state — every words facade/table is signal-first.

**Why it matters:** Not a defect (zone change detection keeps it correct, and the exception is explicitly allowed), but the inconsistency will read as the odd one out as the codebase converges on signals.

**Suggested fix:** On next touch, convert the three fields to signals (or a tiny computed view-state) — no facade needed while the page stays this small.

### N21. `Frontend/quran-dashboard-ui/src/app/core/auth/current-user.store.ts:101`

*category: dry-kiss-yagni · found by: fe-core · verification: NOTE-unverified*

**Issue:** resolveErrorMessage/readApiMessage here are line-for-line identical to system.api.ts:66-88 — the second occurrence of the 'extract the ApiResponse envelope message from an HttpErrorResponse body' knowledge, both inside core.

**Why it matters:** This is boundary-contract knowledge core itself owns (the ApiResponse envelope), so duplication risks divergence; per the clean-code-guard Rule of 3, extraction can wait for a third consumer, hence NOTE not MINOR.

**Suggested fix:** On the next consumer, extract a shared readApiErrorMessage(error, fallback) helper into core/data-access next to api-response.model.ts.

### N22. `Frontend/quran-dashboard-ui/src/app/core/auth/current-user.store.ts:31`

*category: architecture · found by: fe-core · verification: NOTE-unverified*

**Issue:** CurrentUserStore has no reset(): after sign-out the stale user/role stays in the signals and ensureLoadedPromise. Correctness today relies entirely on logoff() performing a full-page redirect that wipes memory.

**Why it matters:** Safe in the current flow (logoff redirects; roleGuard checks isAuthenticated first), but any future local-logout path (logoffLocal on network failure, session-expiry handling) would leave a stale identity displayed and a cached /me for the wrong session.

**Suggested fix:** Add a small reset() clearing both signals and the cached promise, and call it from the sign-out action.

### N23. `Frontend/quran-dashboard-ui/src/app/core/caching/api-response-cache.ts:7`

*category: architecture · found by: fe-core · verification: NOTE-unverified*

**Issue:** ApiResponseCache has LRU eviction (48 entries) but no TTL and no clear()/invalidate() API — a successful response is served for the whole application lifetime.

**Why it matters:** Acceptable now: consumers cache immutable Quran corpus reads (words/roots/lemmas). But the first mutating feature (gates curation, admin edits) building on 'the same idea' per the core README will need explicit invalidation, and today there is no hook.

**Suggested fix:** No change needed now; when the first mutating feature lands, add clear(keyPrefix) or key-based invalidation rather than working around the cache.

### N24. `Frontend/quran-dashboard-ui/src/app/core/data-access/secure-url.interceptor.ts:16`

*category: error-handling · found by: fe-core · verification: CONFIRMED*

**Issue:** isUrlUnderApiBase() returns true for EVERY url when apiBaseUrl is falsy — the security-style interceptor fails open on misconfiguration. It also only compares origins, so the name 'under API base' overpromises: if apiBaseUrl ever gains a path segment (e.g. https://host/api), the whole origin is allowed. Relative URLs are silently blocked (new URL throws → false) with no distinct signal.

**Why it matters:** The interceptor's whole purpose is blocking foreign origins; an empty env value (the most likely misconfiguration) disables it entirely and silently. Origin-only matching is fine today but the name will mislead a future maintainer adding a pathed base.

**Suggested fix:** Fail closed (or throw at startup) when apiBaseUrl is empty, and rename to isSameOrigin / document origin-only semantics.

### N25. `Frontend/quran-dashboard-ui/src/app/core/navigation/detail-overlay/detail-overlay-history.service.ts:32`

*category: threshold · found by: fe-core · verification: NOTE-unverified*

**Issue:** 432 lines exceeds the 400-line soft review threshold for state services (FRONTEND_STRUCTURE.md §5). Judged JUSTIFIED: the service is one cohesive URL-authoritative state machine; the codec (278 lines), provenance (53), models (163), and the two link directives are already extracted; the remaining code is interdependent history/provenance invariants (seedChain, ensureBaseTransitionProvenance, navigate) whose split would scatter a single responsibility.

**Why it matters:** The threshold is a review trigger, not an automatic failure; the file passes the review — well under the 600 hard threshold, heavily documented, exercised by 586 lines of spec.

**Suggested fix:** None required. If it grows further, the next natural seam is href-building (buildFrameHref/buildBaseWithOverlayHref) into a small collaborator.

### N26. `Frontend/quran-dashboard-ui/src/app/core/navigation/detail-overlay/detail-overlay.models.ts:88`

*category: architecture · found by: fe-core · verification: NOTE-unverified*

**Issue:** core/navigation/detail-overlay hard-codes the Words entity vocabulary (unique/root/lemma/stem/wordType frames) in its URL contract, so every new overlay entity kind must edit core models + codec.

**Why it matters:** Boundary judged acceptable — the frame union is the deliberate app-wide, versioned URL contract, explicitly decoupled from Words feature models, and entity rendering stays in features/words. Recording so the trade-off (core edit per new entity kind) is a known cost, not an accident.

**Suggested fix:** None now; keep the documented discipline that only serialization vocabulary (never rendering or feature models) lives here.

### N27. `Frontend/quran-dashboard-ui/src/app/core/navigation/route-paths.ts:3`

*category: typing · found by: fe-core · verification: NOTE-unverified*

**Issue:** navItem/navRoute/navLabel take key: string and throw at runtime for unknown keys; NAV_ITEMS is NavItem[] so no key union exists for compile-time checking.

**Why it matters:** A typo'd key currently fails at module-load (fast, but at runtime); app.routes.ts calls navLabel('mushaf')/navLabel('words') on this untyped path. A derived key union would move the failure to the compiler.

**Suggested fix:** Declare NAV_ITEMS with `as const satisfies readonly NavItem[]` (or an explicit NavKey union) and type navLabel/navRoute keys as that union.

### N28. `Frontend/quran-dashboard-ui/src/app/features/mushaf/components/mushaf-word/mushaf-word.component.ts:38`

*category: architecture · found by: fe-mushaf · verification: NOTE-unverified*

**Issue:** One word click emits both ayahSelect and wordSelect, producing two sequential router.navigate merge patches (facade.selectAyah then facade.selectWord) and therefore two URL hydrations per click. When the clicked word is in a different ayah, the first hydration clears the word selection, so the second sees hadWordSelection=false and calls loadWordAnalysis immediately — bypassing the 700ms word-switch debounce that same-ayah word switches get.

**Why it matters:** Doubled hydration/session writes per click and an asymmetric debounce are emergent rather than designed behavior; the request-token and cache layers absorb the cost today, but the intermediate 'panel=ayah, word cleared' URL state is a subtle trap for future hydration logic.

**Suggested fix:** Have the facade expose a single selectWordWithAyah(wordLocation) that patches ayah+word+panel in one router.navigate call (mushaf-word can emit one event), keeping hydration single-pass and the debounce policy uniform.

### N29. `Frontend/quran-dashboard-ui/src/app/shared/layout/breakpoints.ts:2`

*category: typing · found by: fe-shared-styles · verification: NOTE-unverified*

**Issue:** The TS mirror carries only 3 of the 4 canonical breakpoints; `$qd-bp-wide-desktop-min: 1440px` (_breakpoints.scss:6) has no counterpart despite the 'keep in sync' invariant both files state.

**Why it matters:** The first TS consumer of the wide-desktop band will hardcode 1440, starting the drift the mirror exists to prevent.

**Suggested fix:** Add `QD_BP_WIDE_DESKTOP_MIN_QUERY = '(min-width: 1440px)'` (or note in the comment that wide-desktop is deliberately SCSS-only).

### N30. `Frontend/quran-dashboard-ui/src/app/shared/ui/detail-modal-shell/detail-modal-shell.component.scss:101`

*category: dry-kiss-yagni · found by: fe-shared-styles · verification: NOTE-unverified*

**Issue:** The restore control's fixed offset hardcodes `3.5rem` (`inset-block-start: calc(var(--qd-space-4) + 3.5rem)`) — the navbar height that already exists as `--qd-navbar-block-size` in _tokens.scss:78.

**Why it matters:** A navbar height change moves everything except this control; the token exists precisely so chrome geometry has one source.

**Suggested fix:** Use `calc(var(--qd-space-4) + var(--qd-navbar-block-size))`.

**Fixed — `docs/feature-ux-slice-a/plan.md` T204, 2026-07-30:** `detail-modal-shell.component.scss:101` now reads `inset-block-start: calc(var(--qd-space-4) + var(--qd-navbar-block-size))`, exactly the suggested fix. (The `_tokens.scss:78` citation for the token itself was already off by two lines before this change — the token has been at `_tokens.scss:76` since before this review was written; unrelated to the fix above.)

### N31. `Frontend/quran-dashboard-ui/src/app/shared/ui/placeholder-page/placeholder-page.component.ts:12`

*category: clean-code · found by: fe-shared-styles · verification: NOTE-unverified*

**Issue:** PlaceholderPageComponent is the only shared UI component without `ChangeDetectionStrategy.OnPush`, and `titleAr$` is the raw `route.data` observable whose name implies a title stream; the template index-accesses `['titleAr']` untyped.

**Why it matters:** Inconsistent with every sibling shared component and weakly typed against route data.

**Suggested fix:** Add OnPush and map the stream (`title$ = this.route.data.pipe(map(d => d['titleAr'] as string))`) or use `input()` route binding.

### N32. `Frontend/quran-dashboard-ui/src/styles/_explorer-tables.scss:161`

*category: ui-style · found by: fe-shared-styles · verification: NOTE-unverified*

**Issue:** The green-thread selected-row edge is a physical `box-shadow: inset -2px 0 0` justified by an 'app is RTL-only' comment, while sibling shared code (qd-tabs keyboard nav) resolves direction dynamically and supports LTR.

**Why it matters:** Two shared primitives encode opposite assumptions about direction; if LTR ever appears, the 'current' edge lands on the wrong side silently. The comment makes this an accepted, documented trade — flagged only for the inconsistency.

**Suggested fix:** None required now; if LTR support ever lands, replace with a border-inline-start or a `[dir]`-scoped pair, and grep for other 'RTL-only' physical values.

### N33. `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-cache.ts:9`

*category: api-integration · found by: fe-words-state · verification: NOTE-unverified*

**Issue:** List cache keys and the facades' distinctUntilChanged request keys embed the raw user search string with ':' / '|' joiners (e.g. `lemmas:list:${sort}:${search}:p${page}`, listRequestKey().join('|')), while word-types-cache.ts:65 deliberately encodes search with encodeURIComponent into its own segment.

**Why it matters:** A search term containing the delimiter characters can in principle blur segment boundaries between two different queries (missed reload via an equal request key, or a cross-served cache entry); word-types already demonstrates the collision-safe convention, so the five stores are inconsistent about the same knowledge.

**Suggested fix:** Adopt the word-types searchSegment approach (encodeURIComponent the free-text component) in the roots/lemmas/stems/unique-words cache key builders and facade request keys.

### N34. `Frontend/quran-dashboard-ui/src/app/features/words/state/lemmas-explorer.facade.ts:87`

*category: clean-code · found by: fe-words-state · verification: NOTE-unverified*

**Issue:** bindToRoute subscribes combineLatest([route.paramMap, route.queryParamMap]) and immediately discards paramMap (`[, queryParams]`); roots-explorer.facade.ts:74 and stems-explorer.facade.ts:79 do the same. The pattern was copied from UniqueWordsFacade, where paramMap genuinely carries the :mode route param — these three routes have no route params.

**Why it matters:** Copy-paste residue that implies a route-param dependency that does not exist; queryParamMap alone expresses the actual contract.

**Suggested fix:** Subscribe route.queryParamMap directly in the three facades without route params.

### N35. `Frontend/quran-dashboard-ui/src/app/features/words/components/word-type-filter/word-type-filter.component.scss:175`

*category: ui-style · found by: fe-words-ui · verification: NOTE-unverified*

**Issue:** Comment reads '§16 allowed-gold (solid selection indicator)' — the doctrine's accent has been green since the flat-parchment direction (UI_STYLE_SYSTEM §16.3 'allowed-green list'); the code itself correctly uses var(--qd-accent). Also note the secondary-field select below (lines 255–261) hand-styles a form control instead of composing the qd-select class used by the page-level sort selects.

**Why it matters:** A stale doctrine reference in a style comment can steer a future edit toward the superseded navy+gold rules; the hand-styled select is a small §10 deviation (component SCSS should not re-create input primitives).

**Suggested fix:** Reword the comment to '§16.3 allowed-green (solid selection indicator)' and switch the secondary-filter selects to the qd-select class, dropping the local border/background/padding rules.

### N36. `Frontend/quran-dashboard-ui/src/app/features/words/pages/roots-explorer-page/roots-explorer-page.component.ts:24`

*category: clean-code · found by: fe-words-ui · verification: NOTE-unverified*

**Issue:** Roots/lemmas/stems pages use 300–700-character single-line imports (roots:24–25, lemmas:29–30, stems:29–30) and multi-statement one-line method bodies (e.g. roots:152, 167, 200), while word-types and unique-words pages are conventionally formatted; package.json configures prettier with printWidth 100, so these files were evidently never run through it.

**Why it matters:** Inconsistent formatting across sibling files of the same feature hurts diff review (a one-line body hides three statements from line-based diffs); the project's own formatter settings would fix it mechanically.

**Suggested fix:** Run prettier over the three page components (no semantic change) and keep them formatted.

### N37. `Frontend/quran-dashboard-ui/src/app/features/words/pages/unique-words-page/unique-words-page.component.ts:90`

*category: threshold · found by: fe-words-ui · verification: NOTE-unverified*

**Issue:** 391 lines, near the 400 hard threshold. Judged: the responsibility mix is acceptable — list + drilldown orchestration, focus-controller wiring, and URL writes are page-shell work — but roughly 60 lines are the duplicated association-picker pipeline and matchMedia boilerplate flagged separately.

**Why it matters:** Extracting the two mechanical duplications (picker controller, shared isDesktop helper) drops this file comfortably below the soft threshold without touching its architecture, so no dedicated split is warranted.

**Suggested fix:** No standalone action; resolved as a side effect of the picker-pipeline and matchMedia extractions.

### N38. `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.ts:112`

*category: threshold · found by: fe-words-ui · verification: CONFIRMED*

**Issue:** At 398 lines the page sits 2 lines under the 400 hard threshold. Judged on responsibility mix: it is still an orchestrator (route binding, debounced search, media query, selection/scope matching, URL writes) and already delegates derivations to word-types-detail-panel.view-model.ts and state to two facades — acceptable today, but with zero headroom.

**Why it matters:** FRONTEND_STRUCTURE's hard threshold demands stop-and-split at 400; the next addition of any handler pushes it over, and the selection-matching block (selectedRow computed + matchesWordIdentity + isSameScope, lines 137–163/269–284) is a cohesive pure unit that does not need component state beyond its inputs.

**Suggested fix:** Before the next change to this file, extract the selection-matching logic into a pure helper beside word-types-detail-panel.view-model.ts (e.g. selectedRowForPanel(listState, panelState)), which frees ~50 lines and keeps the page a shell.

### N39. `Backend/tests/QuranDashboard.Tests/Api/Access/AuthorizationPolicyRegistrationTests.cs:26`

*category: test-coverage · found by: tg-be-api · verification: NOTE-unverified*

**Issue:** The Phase-2 role policies are pinned only at DI level (requirements contain DenyAnonymous + the single role). There is no end-to-end 403 test, and production AuthenticationRegistration.cs has an OnChallenge handler (401 envelope, tested) but no OnForbidden handler — a 403 today would emit the framework's empty body, not the ApiResponse envelope. Unreachable in this phase (no endpoint carries a policy), but nothing will catch the envelope break when the first admin surface applies a policy.

**Why it matters:** Deferred contract risk: the first policy-protected endpoint will ship a non-envelope 403 unless a test forces the issue then. Flagging now so the requirement travels with the feature.

**Suggested fix:** When the first endpoint gets [Authorize(Policy=...)], add WebApplicationFactory tests: valid token + wrong role → 403 with the full ApiResponse failure envelope (mirroring InvalidCredential_Returns401FailureEnvelope), and valid token + matching role → 200; add the OnForbidden envelope writer alongside.

### N40. `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersCacheReadTests.cs:270`

*category: test-quality · found by: tg-be-api · verification: NOTE-unverified*

**Issue:** No_lemma_or_stem_cache_key_method_accepts_raw_search_text asserts via reflection that no LemmasCacheKeys/StemsCacheKeys method has a parameter NAMED search/query/term/etc. A parameter renamed to e.g. "s" that still embeds raw search text in the key passes; conversely renaming a harmless parameter to "filter" fails the test.

**Why it matters:** Tests implementation detail (parameter names) rather than behavior (test-guard Rule 1). Low risk in practice because the behavioral complement exists — Lemma/Stem_catalogue_reuses_summary_all_cache_across_sort_and_page_changes proves search changes produce zero new SQL, so search cannot be part of the effective cache key — but the reflection test alone would not catch a leak.

**Suggested fix:** Keep the behavioral catalogue-reuse tests as the authoritative guard; either drop the reflection heuristic or extend it to assert on generated key STRINGS for representative inputs (key for search-A equals key for search-B).

### N41. `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/MorphologyExplorersFixtureSmokeTests.cs:16`

*category: test-quality · found by: tg-be-api · verification: NOTE-unverified*

**Issue:** Fixture_StartsAndSeedsDatabase_Successfully asserts only Database.CanConnectAsync(); it does not verify the seed slice loaded. If seeding failed, InitializeAsync would already fail every test in the collection, so this test catches nothing the rest of the suite would not (test-guard Rule 4/7 borderline).

**Why it matters:** Near-zero marginal value; the name over-claims ("AndSeeds") relative to what is asserted.

**Suggested fix:** Either assert one cheap seed invariant (e.g. quran_lemmas count == 9) to make the name honest, or accept it as a deliberate CP-0 phase gate and leave as-is; no action required.

### N42. `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphologyExplorers/StemsListReadTests.cs:220`

*category: test-coverage · found by: tg-be-api · verification: NOTE-unverified*

**Issue:** The Stems list suite has invalid-sort and unknown-id outcome tests but no direct GetStemsPageOutcome.InvalidPaging test, unlike the Lemmas suite (GetLemmasPage_invalid_paging_returns_validation_outcome with page 0 / size 0 / size 1001). The stems invalid-paging refusal is only proven indirectly through the warning-log field assertion in MorphologyExplorersLoggingTests.StemsPageWarningCases.

**Why it matters:** Asymmetric coverage between two parallel handlers invites drift: a stems-side paging-validation regression that still logs a warning (e.g. wrong outcome type returned after logging) would pass the logging test.

**Suggested fix:** Add the same three-case InlineData invalid-paging theory to StemsListReadTests asserting GetStemsPageOutcome.InvalidPaging, mirroring the Lemmas suite.

### N43. `Backend/tests/QuranDashboard.Tests/Quran/WordsMorphology/MorphologyImportTestFixture.cs:825`

*category: quran-safety · found by: tg-be-pipelines · verification: NOTE-unverified*

**Issue:** MorphologySyntheticSeed attaches fabricated morphology (Buckwalter forms bi/somi/l~Ahi, stems بِسْمِ/لِٰهِ, roots/lemmas) to real Quran locations 1:1:1–1:2:2 (bismillah/Fatiha), while word texts are placeholders (ت-١). Other pipelines deliberately use unmistakably synthetic surah 900 locations.

**Why it matters:** The mix of real locations with hand-typed near-real morphology blurs the synthetic/real boundary the other fixtures keep crisp; a future reader could mistake seed segments for authoritative corpus values. Contained to test containers, so observation only.

**Suggested fix:** Move the synthetic morphology seed to surah-900 locations (matching the Tafsir/Mutashabihat convention) or rename values with an explicit SYNTHETIC_ prefix.

### N44. `Frontend/quran-dashboard-ui/src/app/app.nested-layers.spec.ts:27`

*category: quran-safety · found by: tg-fe-other · verification: NOTE-unverified*

**Issue:** Fixtures attach invented statistics (occurrencesCount: 5, ayahsCount: 4, etc.) to real Arabic root/lemma text ('كتب', 'كتاب', 'كِتاب'), unlike the rest of the frontend suites which consistently use clearly-synthetic markers ('جذر-تجريبي', 'كلمة-تجريبية').

**Why it matters:** Not a violation — it is obviously a UI fixture behind a mocked HTTP boundary — but §10/§9 prefer test data that cannot be mistaken for source truth, and the real root كتب has real corpus counts these numbers contradict. The suite's own convention already solves this.

**Suggested fix:** Rename the fixture texts to the -تجريبي synthetic convention (e.g. 'جذر-تجريبي', 'لِمَة-تجريبية') used everywhere else.

### N45. `Frontend/quran-dashboard-ui/src/app/app.sanity.spec.ts:4`

*category: test-quality · found by: tg-fe-other · verification: NOTE-unverified*

**Issue:** 'executes a passing assertion' is expect(true).toBe(true) — a test of the test framework itself (test-guard Rule 7).

**Why it matters:** It would pass with the entire application deleted. Its only value is as a runner canary for the Vitest/Angular-builder harness, which the file name suggests is intentional; keep or remove deliberately.

**Suggested fix:** Keep it only if the team wants an explicit harness canary; otherwise delete — every other spec already proves the runner works.

### N46. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader.facade.url-sync.spec.ts:817`

*category: test-quality · found by: tg-fe-other · verification: NOTE-unverified*

**Issue:** 'preserves URL-driven state when viewport layout mode changes' dispatches window resize under jsdom, where matchMedia is undefined and the responsive branch is guarded, so the resize handler is very likely a no-op in this environment.

**Why it matters:** The test passes trivially whether or not real responsive relayout preserves URL state — an environment-limits proxy, per the harness-constraints reference ('this can't be measured under jsdom'). It still documents intent and costs little, but it should not be counted as coverage of the responsive behavior.

**Suggested fix:** Either stub matchMedia (as app.nested-layers.spec.ts does) so a real layout-mode change is exercised, or add a comment stating this is a jsdom no-op guard and the real check is a browser concern.

### N47. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-study-source-catalog.api.mock.ts:57`

*category: test-quality · found by: tg-fe-other · verification: NOTE-unverified*

**Issue:** The exported providers wrap module-level vi.fn() singletons (getCatalog/getSimilarAyahs/getAyahMutashabihat), so call history accumulates across tests within a spec file and the same instance is shared by every TestBed configuration that imports it.

**Why it matters:** Currently harmless — every test that asserts call counts builds its own local vi.fn — but the shared instances are a latent order-dependence trap: the first test that asserts toHaveBeenCalledTimes on a shared provider fn will pass/fail depending on which tests ran before it.

**Suggested fix:** Export factory functions (e.g. mushafStudySourceCatalogApiProvider()) that create fresh vi.fn instances per TestBed configuration, or document loudly that these fns must never be asserted on.

### N48. `Frontend/quran-dashboard-ui/src/app/features/words/entity-detail-overlay/entity-detail-overlay-invariant.spec.ts:26`

*category: quran-safety · found by: tg-fe-words · verification: NOTE-unverified*

**Issue:** Fixture conventions are mixed across the suite: word-types suites use the exemplary clearly-synthetic convention (SYNTH_* text, 999:999 verse keys) and stems-detail.controller.spec explicitly documents 'unmistakably synthetic' rows, but this spec uses the real root كتب / lemma كِتاب with fabricated counts, and roots-explorer-page.component.spec.ts:36-48 attaches synthetic letter-words to the real verse key 1:1 / الفاتحة.

**Why it matters:** CODING_PRINCIPLES §9 requires Quranic test data be clearly synthetic. No safety breach here — nothing is presented as source data and the words are obviously non-scriptural — but real identifiers with invented statistics blur the line the SYNTH_/999 convention exists to keep bright.

**Suggested fix:** Standardize on the word-types convention (SYNTH_-prefixed text, out-of-range verse keys/ids) for all words-feature fixtures; keep real morphology only where a test specifically needs it and mark it as such.

### N49. `Frontend/quran-dashboard-ui/src/app/features/words/pages/word-types-explorer-page/word-types-explorer-page.component.spec.ts:1537`

*category: test-quality · found by: tg-fe-words · verification: NOTE-unverified*

**Issue:** 'exposes the Words hub access route for Word Types' asserts the ADDITIONAL_ACTIVE_HUB_SECTIONS constant contains a literal entry — a constant-echo test (Rule 4) that also lives in the wrong suite (it tests a labels-module constant, not this page component).

**Why it matters:** It duplicates the constant's value rather than any behavior; the hub page component spec is where 'the hub renders a Word Types card routing to /dashboard/words/types' would catch a real regression.

**Suggested fix:** Move/replace with a words-hub-page rendering assertion on the link's href, and delete the constant-membership check.

### N50. `Frontend/quran-dashboard-ui/src/app/features/mushaf/state/mushaf-reader-session.ts:87`

*category: error-handling · found by: x-ai-failures · verification: NOTE-unverified*

**Issue:** saveMushafReaderSession's catch block is empty with a bare blank line (lines 87–89), as is ThemeService.writeStoredTheme (core/theme/theme.service.ts:62–64). Swallowing storage-write failures is legitimate, but the blocks carry no rationale, while the repo's own convention annotates best-effort catches (e.g. 'catch { /* best-effort */ }' in EnrichedMorphology tests).

**Why it matters:** Failure mode #1's smell in its mildest form: an unannotated empty catch is indistinguishable from an accidental swallow to the next reader, and it diverges from the codebase's established annotated pattern (#10).

**Suggested fix:** Add the one-line rationale comment inside both empty catch bodies (e.g. /* sessionStorage/localStorage unavailable (private mode) — persistence is best-effort */).

### N51. `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts:34`

*category: clean-code · found by: x-ai-failures · verification: NOTE-unverified*

**Issue:** UniqueWordsFacade hardcodes its Arabic list-failure message inline (const CONNECTION_ERROR_MESSAGE = 'تعذّر تحميل الكلمات الفريدة…') while the four sibling explorer facades alias a label from their models/*.labels.ts module (ROOTS_LIST_ERROR_LABEL, STEMS_LIST_ERROR_LABEL, LEMMAS_LIST_ERROR_LABEL); unique-words.labels.ts holds every other unique-words label including DRILLDOWN_ERROR_LABEL.

**Why it matters:** Failure mode #10 (inconsistency with surrounding code): the one message that escaped the feature's label module is exactly where a future copy edit or localization pass will be missed.

**Suggested fix:** Move the string to models/unique-words.labels.ts as UNIQUE_WORDS_LIST_ERROR_LABEL and alias it in the facade like the sibling explorers do.

### N52. `Backend/api/QuranDashboard.Api/Controllers/System/HealthController.cs:25`

*category: api-guidelines · found by: x-api-contract · verification: NOTE-unverified*

**Issue:** The message switch maps every non-Healthy report — including Unhealthy — to ApiMessages.HealthDegraded ('الخدمة تعمل مع وجود تنبيهات', 'the service works with warnings'), so a fully unhealthy database returns data.status 'unhealthy' alongside a message claiming the service is working.

**Why it matters:** Message and data contradict each other on the wire for the Unhealthy case. No current UI harm (the footer renders data.status, not message), but CODING_PRINCIPLES §8 asks for the clearer message when one is possible, and any consumer that surfaces message would understate an outage.

**Suggested fix:** Add a distinct HealthUnhealthy Arabic message and map HealthStatus.Unhealthy to it, keeping HealthDegraded for Degraded only.

### N53. `Frontend/quran-dashboard-ui/src/app/core/auth/access.api.ts:13`

*category: clean-code · found by: x-api-contract · verification: NOTE-unverified*

**Issue:** AccessApi's doc-comment claims it is 'Thin by contract (mirrors core/data-access/system.api.ts)' — but SystemApi does the opposite: it unwraps the envelope to AppInfo/HealthStatus, converts failures to thrown Errors, and holds application-lifetime cache state (dashboardInfo$ at system.api.ts:19), while AccessApi correctly returns the raw Observable<ApiResponse<T>>.

**Why it matters:** The comment misdescribes the codebase's two coexisting API-service patterns: AccessApi actually follows the API_INTEGRATION_GUIDELINES contract (service returns envelope, store unwraps) while SystemApi is the sanctioned deviation. A reader copying 'the SystemApi pattern' on the strength of this comment would build the non-guideline shape.

**Suggested fix:** Reword the comment to cite the guideline (service returns ApiResponse<T>, store unwraps) and drop the 'mirrors system.api.ts' claim, or note explicitly that SystemApi is the documented exception.

### N54. `Frontend/quran-dashboard-ui/src/app/features/words/state/unique-words.facade.ts:216`

*category: error-handling · found by: x-api-contract · verification: NOTE-unverified*

**Issue:** handleListResponse uses EMPTY_LIST_LABEL ('لا توجد نتائج') as the errorMessage fallback when an envelope arrives with isSuccess=false or null data, so an error-status banner would display an 'empty results' message.

**Why it matters:** Blurs the empty-vs-error distinction the guidelines require: a failed load labeled 'no results' hides a data problem behind an absence claim. Only latent today (the backend never returns 200 with isSuccess=false), but the defensive branch should not mislabel failure as emptiness.

**Suggested fix:** Fall back to the existing CONNECTION_ERROR_MESSAGE (or a dedicated list-error label) instead of EMPTY_LIST_LABEL in the failure branch.

### N55. `Backend/.architecture/BACKEND_STRUCTURE.md:208`

*category: architecture · found by: x-arch · verification: NOTE-unverified*

**Issue:** EF Core migrations live at Backend/infrastructure/QuranDashboard.Infrastructure/Migrations/ (39 files), while BACKEND_STRUCTURE.md's preferred Infrastructure layout places them under Persistence/Migrations/ (and an empty local Persistence/Migrations directory exists alongside).

**Why it matters:** Doc/code drift: the canonical placement doc shows one location, the tooling-generated reality is another. Harmless today, but each new contributor must guess which is authoritative.

**Suggested fix:** Update BACKEND_STRUCTURE.md's example layout to reflect the actual Infrastructure/Migrations location (moving generated migrations is not worth the churn), and remove the stray empty Persistence/Migrations directory locally.

---

*Review-only audit: no code was modified. Generated by engineering-review (multi-agent, adversarially verified). Full machine-readable findings with per-verifier reasoning retained in the session scratchpad (`review-result.json`).*