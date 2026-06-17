# Phase 0 Research: Mushaf Reader Study Context

All product/UX decisions were locked in the planning report and spec, so this file resolves the **technical "how"** for the implementer (a cheaper model). Every decision below is concrete and testable. Format: Decision → Rationale → Alternatives considered.

---

## R1. Secure local environment: HTTPS for both apps, HTTPS-only data calls

**Decision**:
- **Backend** runs over HTTPS using the existing `https` launch profile (`https://localhost:5015`, with `http://localhost:5014` redirected via the existing `app.UseHttpsRedirection()`). Make `https` the default/used profile and trust the local dev cert (`dotnet dev-certs https --trust`).
- **Frontend** dev server runs over HTTPS at `https://localhost:4200` by enabling SSL in `angular.json` `serve` options (`"ssl": true`, with `"sslCert"`/`"sslKey"` pointing at a locally generated dev cert) and a `start:https` script (`ng serve --ssl --ssl-cert <cert> --ssl-key <key>`).
- **Frontend `apiBaseUrl`** is set to `https://localhost:5015` in `environment.development.ts` (currently `http://localhost:5014`). The production `environment.ts` keeps `apiBaseUrl: ''` (same-origin) and is out of scope here.
- **CORS** `Cors:AllowedOrigins` is restricted to `https://localhost:4200` only (remove the `http://localhost:4200` entry) so the secure dev origin is the only allowed caller.
- A **dev-time secure-URL guard** (`secure-url.interceptor.ts`) asserts that every outgoing API request URL is absolute, starts with `https://`, and begins with the configured `apiBaseUrl`; otherwise it fails fast with a controlled error (and never rewrites to HTTP). This makes FR-003/FR-004 enforceable and testable.

**Rationale**: The backend already ships an HTTPS profile, HTTPS redirection, and a CORS policy reading `Cors:AllowedOrigins` (which already lists the HTTPS origin). The only real gaps are (a) the frontend pointing at HTTP, (b) the dev server running on HTTP, and (c) CORS still allowing the HTTP origin. Driving the base URL from a single `environment.apiBaseUrl` plus an interceptor guard guarantees "HTTPS backend URL only" centrally instead of per-call.

**Alternatives considered**:
- *Angular dev-server proxy (`proxy.conf.json`)* to forward `/api` to the backend — rejected for v1: the app already uses a direct `apiBaseUrl` (no proxy file exists), and a same-origin proxy would hide the explicit HTTPS-only target the requirement calls for. Can be revisited later.
- *HSTS / production hardening* — out of scope; this is a local-dev requirement.
- *Trusting via reverse proxy (Caddy/nginx)* — unnecessary complexity for local dev; native `ng serve --ssl` + Kestrel HTTPS is enough.

---

## R2. Backend read architecture (no writes, no migrations)

**Decision**: Add three read use cases in Application (`GetMushafPage`, `GetAyahStudy`, `GetWordAnalysis`), each with a Query + Handler + Response record, backed by three read interfaces in Application.Abstractions (`IMushafPageReader`, `IAyahStudyReader`, `IWordAnalysisReader`) implemented by EF read repositories in Infrastructure (`Persistence/Reads/Quran/MushafReader/`). Controllers are thin and map the response to `ApiResponse<T>`.

**Rationale**: Matches CLEAN_ARCHITECTURE + BACKEND_STRUCTURE (use-case-first foldering, abstractions in Application, EF in Infrastructure, thin API). Read interfaces keep the handlers testable and let the cache decorators wrap them cleanly (R7). No Domain entity is needed because nothing is written or invariant-checked.

**Alternatives considered**:
- *Query EF directly from handlers* — rejected: handlers would depend on Infrastructure, breaking the dependency rule and the cache-decorator seam.
- *Dapper/raw ADO* — unnecessary; EF Core read projections over indexed tables are sufficient and consistent with the codebase.

---

## R3. Mushaf page assembly + marker placement

**Decision**:
- Page lines come from `quran_mushaf_lines` (ordered by `line_number`); words per line come from `quran_words` filtered by `page_number` + `line_number`, ordered by `line_word_order`; `verse_key` via `quran_ayahs`. Mushaf text is `quran_words.text_uthmani`; ayah-end markers are the `is_ayah_marker = true` rows.
- `surahs`, `ayahRange`, and the `navigation` summary (juz/hizb/rub numbers present) are derived from the page's distinct ayahs.
- **Division/sajda markers** (juz/hizb/rub/sajda): resolve the related ayah, and place the marker on `MIN(quran_words.line_number)` for that ayah on the current page (the locked first-line rule). Each marker carries `markerType`, `markerNumber`, `verseKey`, `lineNumber`, `wordLocation`.

**Rationale**: Exactly the join paths and the first-line rule validated in the capability report §2. Deriving "surahs/range/nav present on page" from the page's words is exact (vs. relying only on page-boundary columns).

**Alternatives considered**:
- *Use only `quran_mushaf_pages` boundary columns for surah/ayah range* — kept as a fast hint but not authoritative; the exact set is computed from the page's words to avoid edge errors at page seams.

---

## R4. Default source resolution + "three sources together"

**Decision**: `GetAyahStudyHandler` resolves each source kind in order: explicit query param → configured default (`MushafReaderOptions`) → controlled empty/error for that kind. The handler loads tafsir, translation, and full i3rab **together** in one response and echoes the resolved source key for each in `selectedSources`. A missing/unknown source key for a kind produces a clear empty/error state for that kind only (the other kinds still load); it never substitutes a different source.

**Rationale**: Locked decision (three together in v1, config-driven defaults `ar-muyassar` / `en-sahih-international` / `muyassar`, no silent substitution). Per-kind isolation keeps one missing source from failing the whole study.

**Alternatives considered**:
- *Per-tab/per-source endpoints* — explicitly deferred by the planning report; reconsider only if payload size becomes a real problem.
- *Hardcoded defaults in query logic* — rejected; defaults must be configuration-driven and validated against the source catalogue.

---

## R5. Word analysis + glued color-linked segments + fallback

**Decision**:
- Reject `is_ayah_marker = true` rows as not analyzable (controlled 400/empty result).
- Word data from `quran_words` (+ `quran_ayahs` for `verse_key`); morphology from `quran_word_morphology` (+ `quran_pos_tags`/`quran_roots`/`quran_lemmas`/`quran_stems`); identity counts from `quran_words_ordered_*`/`quran_words_unique_*`; segments from `quran_word_morphology_segments` ordered by `segment_number` (+ `quran_pos_tags`/`quran_i3rab_rules`).
- The backend assigns a **stable `segmentColorSlot`** per segment (by `segment_number` order) so the frontend can color-link the glued word, the segment data row, and the simple i3rab label with the same slot. Colors are **visual-linking only** (not POS-semantic). The slot→color mapping is a small frontend palette keyed by slot index.
- **Segment fallback**: if `form_arabic_normalized` is empty/null, emit `displayTextStatus: "missing"` (or equivalent) with no invented text; the frontend shows a placeholder for that segment, keeps the raw segment data visible, and preserves the full word from `text_uthmani`. (~208/128,219 rows need this.)

**Rationale**: Mirrors the capability report §4–§5. Putting the color **slot** (not the color) in the response keeps semantics on the backend and palette on the frontend, satisfying the "visual-linking only" rule.

**Alternatives considered**:
- *Reconstruct the word from segment forms* — forbidden; Mushaf and whole-word text always come from `text_uthmani`.
- *Backend emits hex colors* — rejected; the palette is a frontend/design concern (DESIGN.md), backend emits an integer slot.

---

## R6. HTML content safety (tafsir / full i3rab)

**Decision**: Render markup via Angular's built-in template sanitizer by binding `[innerHTML]` to the raw source string **without** `bypassSecurityTrustHtml`. Provide a small `safe-html` pipe only as a readability wrapper that still routes through the built-in sanitizer (no trust bypass). The database content is never altered or stripped server-side. A documented allowlist sanitizer (e.g., DOMPurify) is a **future** option only if i3rab formatting is materially degraded by the built-in sanitizer.

**Rationale**: Locked decision (sanitized by default, no default `bypassSecurityTrustHtml`). Angular's built-in `[innerHTML]` sanitizer strips scripts/unsafe attributes automatically and is the simplest correct default.

**Alternatives considered**:
- *`bypassSecurityTrustHtml`* — rejected as the default (unsafe).
- *DOMPurify allowlist now* — deferred; only adopt if needed and documented (it adds a dependency and an allowlist to maintain).

---

## R7. Caching (after API stabilization)

**Decision**:
- **Backend**: `IMemoryCache` decorators over the three readers, added only after the readers + tests are stable. Cache keys: `mushaf:page:{pageNumber}`, `mushaf:ayah-study:{verseKey}:taf:{tafsirSource}:tr:{translationSource}:i3rab:{fullI3rabSource}` (using the **resolved** source keys; sentinel `none` when a kind is empty), `mushaf:word-analysis:{wordLocation}`. Cache only successful, immutable reads; never cache failures/not-found or any user-specific state. No Redis.
- **Frontend**: `mushaf-reader-cache.ts` keeps a bounded cache of successful page/ayah/word responses keyed the same way, **deduplicates concurrent identical requests** (shared in-flight observable), and optionally **prefetches** previous/next page after the current page loads.

**Rationale**: Exactly the locked caching plan. Quran reads are immutable at runtime, so memory caching is safe; in-memory cache clears on restart (acceptable invalidation for v1).

**Alternatives considered**:
- *Redis / distributed cache* — out of scope for v1.
- *Cache-first from day one* — rejected; stabilize contracts/tests before adding the cache seam.

---

## R8. Frontend route + state-in-URL

**Decision**: Add a lazy route `dashboard/mushaf` in `app.routes.ts` loading `features/mushaf/mushaf.routes.ts` → `MushafReaderPageComponent`. View state is encoded as query params on that route: `page`, `ayah`, `word`, `segment`, `panel`, `ayahTab`, `wordTab`, `tafsirSource`, `translationSource`, `fullI3rabSource`, using natural Quran keys. The facade maps URL↔state both ways; selections update the URL (replace, not push, for fine-grained changes) so reload/deep-link reproduce the view. On wide desktop `panel` is focus state, not exclusive visibility.

**Rationale**: Matches the spec's URL contract and FRONTEND_STRUCTURE "Tabs and URL State" (query params for view-mode/selection state; stable keys; refresh/share/back support). `/dashboard/mushaf` is the spec-mandated path.

**Alternatives considered**:
- *Child routes per tab* — heavier than needed; these are selection/view-mode states, which the structure guide assigns to query params.
- *In-memory-only selection* — rejected; breaks reload/share (SC-005).

---

## R9. Layout realization (right Mushaf / left study)

**Decision**: A responsive grid in the page shell: on wide desktop, two columns — left wide study area (~40–45%) and right Mushaf area (~55–60%); the left column is a vertical split with `selected-word-section` on top (~35–40% height) and `selected-ayah-section` on bottom (~60–65%). Cards keep stable outer dimensions and scroll internally. Below the wide breakpoint, collapse to a single visible study section (driven by `panel`) with word/ayah toggle; on mobile, study sections become drawer/bottom-sheet. RTL throughout; compose `qd-` style primitives.

**Rationale**: Implements the locked layout and proportions while respecting UI_STYLE_SYSTEM and Arabic-first RTL. Suggested proportions are design guidance, not hard constraints.

**Alternatives considered**:
- *Center Mushaf with two flanking panels* — explicitly rejected by the locked decision.
- *Single contextual panel only* — rejected for wide desktop (word and ayah must be visible together).

---

## Resolved unknowns summary

| Topic | Resolution |
|---|---|
| HTTPS for both apps | Kestrel `https` profile + `ng serve --ssl`; CORS → HTTPS origin only; `apiBaseUrl=https://localhost:5015`; secure-URL interceptor guard |
| Backend read layering | Application queries/handlers + read abstractions; EF read repositories in Infrastructure; thin controllers |
| Marker placement | First line where the ayah appears on the current page (`MIN(line_number)`) |
| Ayah study loading | Three sources together; default→explicit resolution; missing source = per-kind empty/error |
| Segment colors | Backend emits integer `segmentColorSlot`; frontend palette maps slot→color (visual-linking only) |
| Segment fallback | `displayTextStatus` flag; no invented text; whole-word `text_uthmani` preserved |
| HTML safety | Angular built-in `[innerHTML]` sanitizer; no `bypassSecurityTrustHtml`; DOMPurify deferred |
| Caching | `IMemoryCache` decorators + bounded frontend cache/dedupe/prefetch, after stabilization |
| URL state | Query params on `/dashboard/mushaf` with natural Quran keys |
| Layout | Two-column desktop grid (left study split top/bottom, right Mushaf), responsive collapse/drawers |

No `NEEDS CLARIFICATION` items remain.
