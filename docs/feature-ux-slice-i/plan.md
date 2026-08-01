# Slice I — Cache (UX audit)

Source: `docs/abwab-ux-audit.md` "Slice I — Cache" (`:1133-1137`) — item 23 (`:938-1032`), the
series' only **NEW PATTERN on both ends**: the backend has never cached mutable data and has
zero HTTP caching (grep for `ETag` / `If-None-Match` / `ResponseCache` / `OutputCache` /
`304` across `Backend/` — zero hits); the frontend has never sent a conditional request (grep
for `If-None-Match` / `setHeaders` / `headers.get` across `src/` — zero non-test hits). This
slice is last **deliberately**: its correctness depends on every abwab write path being final,
and Slices F and G both changed writes. It is planned as a first, not as an extension — the
`Cached*Reader` precedent supplies a decorator *shape* and nothing else; no invalidation path
exists anywhere in the backend today.

**Mode when this plan was written:** plan-only. No code, no docs, no Git action. Everything
below is scheduled, nothing is done.

**Slice H status at plan time:** merged. The `ux-slice-h` commits (`4e184bd3`…`5adc9bc0`) sit
on `dev` first-parent; `core/navigation/nav-menu.ts` exists at the tip. This plan is measured
against `dev` (`5adc9bc0`, clean). **The H-DEPENDENT fact list is empty** — nothing in Slice I
consumes a Slice H primitive. One H-adjacent fact is honored as a *negative*: `TESTING_DEBT.md`
row H1 names "Slice I if caching adds any nav affordance" as a potential payer — this slice
adds **no** nav affordance, so H1's trigger does not fire here.

**Audit line references are partially stale and are superseded by the tables below.** Item 23
was written before F added the section reorder write and before G reshaped the apply; its
"small set of writers" prose is replaced by the exhaustive 21-route inventory in §5.1, grepped
at `5adc9bc0`, which is the list that binds.

## Precondition — VERIFIED on `dev` (`5adc9bc0`, clean) at plan time

| Consumed primitive / mechanism | Where it lives | Verified |
|---|---|---|
| Slices A–H merged to `dev` | `dev` tip `5adc9bc0` | ✅ |
| **The governing invariant** — refresh-after-write: every write resequences its scope, a root-affecting write resequences every live root; skipping the refresh reproduces spurious `409`s | `features/abwab/README.md:467-479` | ✅ the design derives from it (§4.1-2/-3) |
| **The recorded rule this slice must not violate** — *"`AbwabTreeDto.version` is diagnostics only. Per-row `xmin` tokens are the only concurrency currency; do not build snapshot-level conflict detection on it."* | `features/abwab/README.md:507-508`; backend side `Persistence/Reads/Abwab/README.md:88-92` ("`Version` deliberately ignores `abwab_door_relations`… safe **because** diagnostics-only") | ✅ **untouched** — the validator is a generation counter, not `version` (§4.1-1); the gotcha is *amended to distinguish, not weakened* (§5.4) |
| … and `GetSnapshotVersionAsync`'s actual coverage: `abwab_sections` + `abwab_doors` + `abwab_door_aliases` only — **not** relations, **not** templates | `EfAbwabTreeReader.cs:89-103`; relations counted separately `:62-82` | ✅ the audit's "field's gap" (`:994-997`) — moot under the counter design |
| **The recorded statement this slice reverses** — *"**No caching.** … Abwab is live admin-authored data with no invalidation story yet, and caching a snapshot an admin is actively editing would be a correctness risk"* | `Persistence/Reads/Abwab/README.md:106-108` | ✅ amended here (§5.4) — the invalidation story is what this slice builds |
| The three read routes gaining conditional GET | `AbwabTreeController.cs:10-21` (`GET api/abwab/tree`, unconditional `Ok(...)` at `:18`); `AbwabTemplatesController.cs:19` (`GET api/abwab/templates`), `:33` (`GET api/abwab/templates/{templateId}`) | ✅ |
| … the fourth read, deliberately excluded | `AbwabDoorRelationsController.cs:15` (`GET api/abwab/doors/{doorId}/relations`) | ✅ stays unconditional — §4.2-9 |
| **The complete write inventory: 21 routes through exactly 5 writer interfaces** | §5.1 table; controllers `api/QuranDashboard.Api/Controllers/Abwab/`; writers `Persistence/Writes/Abwab/` (`README.md:15-24` — "five writers back the twenty-one `/api/abwab` write endpoints") | ✅ grepped, not remembered |
| … **no mutation path bypasses the writers.** Grep of `AbwabDoor\|AbwabSection\|AbwabTemplate` across `Backend/` outside `Persistence/{Reads,Writes}/Abwab/`, migrations, tests, domain, and handlers: only outcomes/handlers/EF-configurations/DI — zero direct `DbSet` mutation, and no importer/tool touches abwab tables | plan-time grep at `5adc9bc0` | ✅ **stop condition 2 checked and clear** — every write can reach the invalidator through its writer seam |
| … **commit happens INSIDE the writer method, before it returns**; the controller serializes the response only after the handler returns | explicit txns: `EfAbwabDoorsWriter.CreateAsync:45-51`, `EditAsync:79-85`, `EfAbwabTemplatesWriter.CreateAsync:22-40`, `EfAbwabTemplateApplyWriter.ApplyAsync:31-168`; all other saves are per-`SaveChangesAsync` implicit txns | ✅ **this is why the writer decorator satisfies locked decision 3 by construction** (§4.2-3) |
| … writers translate every EF exception before it crosses the seam; some multi-save paths run on implicit txns where a second save can fail after a first committed | `Persistence/Writes/Abwab/README.md:26-46` | ✅ why the bump is in `finally`, not on success only (§4.2-3) |
| The caching precedent — decorator shape only | `Infrastructure/Caching/Quran/**` (12 `Cached*Reader` classes, e.g. `Caching/Quran/Words/Roots/CachedRootsReader.cs:7` — ctor `(EfRootsReader efReader, IMemoryCache cache)`, registered concrete-Ef + interface→decorator); `AddMemoryCache` at `MushafReaderDependencyInjection.cs:14`, `AccessDependencyInjection.cs:21` | ✅ shape copied (§4.2-4); **no invalidation exists anywhere** — zero `IMemoryCache.Remove` in `Backend/` |
| … `CacheLoadGate` — single-flight, and its own recorded limits: *"Gates are held per key for the process lifetime, so the key space must stay bounded … do not reuse this for unbounded or caller-supplied keys without adding eviction"* | `Infrastructure/Caching/CacheLoadGate.cs:1-50`, warning `:7-9` | ✅ **not reused** — DRIFT-3 |
| Abwab DI — direct `AddScoped`, no decorators today | `AbwabDependencyInjection.cs:11-18` (writers `:11-12,15,17-18`; readers `:13-14,16`) | ✅ the wiring point for §4.2-4/-5 |
| `ApiResponse<T>` contract — §4 status list has no `304`; `204` is the only sanctioned bodiless status ("only when intentionally returning no body") | `Backend/.architecture/API_GUIDELINES.md:85-100` (`204` at `:91`), envelope `:102-141` | ✅ `304` becomes the second sanctioned bodiless status (§5.3); the envelope **type** is untouched — stop condition 4 clear |
| … the frontend already survives a bodiless success: the `204` null-envelope gotcha | `features/abwab/README.md:703-718` | ✅ the precedent §5.3 cites |
| **ASP.NET Core gives bare controllers NO automatic conditional handling.** Automatic `If-None-Match` evaluation exists only in OutputCache/ResponseCaching middleware and File results ("the framework returns `304 Not Modified` with no body — no additional code is needed" — File results only) | learn.microsoft.com `aspnet/core/performance/caching/output#enable-cache-revalidation`; `aspnet/core/fundamentals/minimal-apis/responses#file-result-return-values` | ✅ read from the framework's documented contract, not assumed — the controllers evaluate the header themselves (§4.2-6); neither middleware is added (non-goal) |
| **Angular `HttpClient` delivers a `304` on the error channel, distinguishably.** `ok = status >= 200 && status < 300`; non-ok responses go to `observer.error(HttpErrorResponse)`; `HttpStatusCode.NotModified = 304` | installed source `node_modules/@angular/common/fesm2022/module.mjs:1192` (ok check), `:1884-1908` (FetchBackend error routing), `:1279-1286` (`HttpErrorResponse`), `:1333` | ✅ read from the installed package — `err.status === 304` in `catchError` needs **no new interceptor**; stop condition 3 clear |
| The registered interceptors — none touches caching | `app.config.ts:43` (`secureUrlInterceptor`, `authInterceptor()`, `devLatencyInterceptor`); `secure-url.interceptor.ts:47-53`; `dev-latency.interceptor.ts:5-6` | ✅ untouched |
| **CORS: the app is cross-origin in BOTH environments** — prod frontend → `https://quranfullstack-production.up.railway.app` (`environments/environment.ts:5`), dev app (`:4200`) → `https://localhost:5015` (`environment.development.ts:5`). Policy has `AllowAnyHeader()` (so the `If-None-Match` **request** header preflights fine) but **no `WithExposedHeaders`** — and `ETag` is not a CORS-safelisted response header, so `headers.get('ETag')` returns `null` in the browser today | `ServiceCollectionExtensions.cs:63-82` (`AllowAnyHeader` `:78`); `UseCors` `WebApplicationExtensions.cs:20` | ✅ **DRIFT-5 — a scheduled task (T303), not a footnote**: without it the frontend silently never caches |
| `AbwabSnapshotFacade` — the frontend cache and its recorded contract: *"On failure the previous snapshot is left in place"*, `shareReplay(1)`-backed | `state/abwab-snapshot.facade.ts:11-19` (class doc), state `:27-29`, `snapshot` computed `:35`, `load` `:42-44`, `refresh` `:46-48`, `fetch` `:50-76` (success/error tap `:58-60`, `catchError` `:66-69`) | ✅ the `304` path composes the existing failure semantics (§5.5) |
| `AbwabTemplatesFacade` — same contract, *"Root-scoped: it is a cache"*; the selected-template identity guard | `state/abwab-templates.facade.ts:22-30,64-75,87-149`; identity guard `:56-62`; contract recorded `features/abwab/README.md:257-259` | ✅ gains list + selected validators (§4.2-13) |
| The single write funnel and the post-write refetch this design must never betray | `state/abwab-write.controller.ts:73` (funnel), write methods `:119-194`, `handleSuccess → refreshAndRebind` `:236`, `refreshAndRebind` `:316-322`; templates writes refresh via `state/abwab-templates.controller.ts:106,120` | ✅ the just-wrote row of §6a is the acceptance |
| The unconditional `load()` on route entry — **kept**, per locked decision 6 | `pages/abwab-page/abwab-page.component.ts:278` | ✅ with a validator it costs a `304`, not a body |
| **A `304` cannot flash a skeleton — already true.** Both pages gate skeletons on value absence, not on loading alone | `abwab-page.component.html:63,70,92` (`isLoading() && !snapshot()`); `abwab-templates-page.component.html:13` (`isLoading() && templates().length === 0`) | ✅ verified — no UI change owed; T503 observes it |
| The archive view is a partition of the same snapshot, not a resource | `state/abwab-tree.builder.ts:91-94` (`archivedRoots`); URL contract `features/abwab/README.md:272-280` | ✅ covered by the tree entry, gets nothing of its own (§4.1-5) |
| The relations read is fetched per-modal-open with no held prior value | `state/abwab-relations.controller.ts:33-42`; modal effect `abwab-relations-modal.component.ts:232-253` | ✅ why it gains no conditional path (§4.2-9) |
| The API layer today: body-only `http.get<ApiResponse<T>>`, no `observe`, no header reads anywhere in `src/` | `data-access/abwab.api.ts:41-45`; `core/data-access/api-response.model.ts:1-6`; plan-time grep | ✅ T401 is the app's first `observe: 'response'` call |
| `SmokeRouteCatalog` — the three read entries and what they assert: `GET api/abwab/tree` **dispatched**, expects `200` (`:311`); `GET api/abwab/templates` `200` ParityOnly (`:320-323`); `GET api/abwab/templates/{templateId:int}` `404` ParityOnly (`:324-327`); parity keys on `"<METHOD> <template>"` both directions | `Tests/Smoke/SmokeRouteCatalog.cs:224-359`; `SmokeCoverageParityTests.cs` | ✅ **no entry moves** — DRIFT-6: the smoke client sends no `If-None-Match`, so every catalogued expectation still holds; no route is added ⇒ no new entry owed. The *tier* still runs (§7) |
| Route-smoke tier REQUIRED for contract changes on existing routes, with an explicit `Tests.Smoke.Data` RAN/SKIPPED statement | `TESTING_STRATEGY.md` §4 row `:309`, §3 Tier A/C, §10; `Backend/CLAUDE.md` | ✅ (§4.1-8) |
| The validated commands: no-pipeline (1,086 / ~21 s, `:350-354`), smoke (140 / ~52 s, `:356-358`), `Tests.Api` (60 / ~10 s, `:346-348`); frontend full suite (191 files / 2,161 tests / ~205 s, `:410-411`), `npm run build` `:414`; e2e opt-in, never a tier `:416-424` | `TESTING_STRATEGY.md` §5/§6 | ✅ the gates of §7 |
| `TESTING_DEBT.md` structure: per-slice sections, one concrete trigger per row; parity entries and required tiers are not debt-able | `docs/TESTING_DEBT.md:1-18`; `ux-slice-h` section ends the file at `:97-113` | ✅ the `ux-slice-i` section appends after it |
| `.architecture/API_INTEGRATION_GUIDELINES.md` is silent on caching and conditional requests — no `ETag`/`If-None-Match`/`304` anywhere; its "HTTP Errors vs Backend Failure Responses" section (`:285-308`) is where the third category lands | the file, read end to end | ✅ amended (§5.4) |
| Root `CLAUDE.md` Active Spec Kit Feature is `None` | root `CLAUDE.md:204-206` | ✅ set to `ux-slice-i` at T101, cleared at T602 |
| Local browser-driving needs Kestrel started with the frontend's mkcert PEM, else every API call reads as a backend failure | session memory `local-https-dev-cert-mismatch` | ✅ T503 setup note |

### DRIFT — where current code contradicts the audit or this commission

| # | The audit / commission says | `dev` at `5adc9bc0` says | This plan follows |
|---|---|---|---|
| DRIFT-1 | Item 23 shape 2 (`:1012-1015`): an invalidation surface "that **every** abwab write command calls after commit and before responding" — reading as handler-level calls, 21 call-sites. | Commits happen **inside the writer methods** (`EfAbwabDoorsWriter.CreateAsync:45-51` et al.); handlers only translate outcomes; the controller responds after the handler returns. A handler-level call would be 21 opportunities to forget and would sit *after* code that can throw between commit and call. | **The invalidator lives in five writer decorators, not 21 handler edits** (§4.2-3). A decorator method's `finally` runs after the inner method returns (commit already durable) and before the handler — let alone the response — resumes: locked decision 3's ordering holds *by construction*, and a new writer method fails compilation until the decorator implements it, which is the "make forgetting hard" mechanism decision 4 demands. |
| DRIFT-2 | Locked decision 1: the counter "resets on process restart. That is SAFE — every client refetches once." | A **bare** counter is not restart-safe: reset to 0, then N writes by other admins climb it back to a value an idle client still holds from before the restart — that client's next `If-None-Match` would false-match and receive a stale `304`. | **The validator is boot-scoped**: `"{resource}-{bootId}-{n}"` where `bootId` is a random per-process value captured once at singleton construction (§4.2-2). Cross-restart collision becomes impossible, and the locked claim ("every client refetches once") becomes literally true. This is inside decision 1's own allowance — "plus whatever the plan needs to make it opaque and resource-scoped". |
| DRIFT-3 | Item 23 (`:970-977`): the precedent is "`Cached*Reader` + `IMemoryCache`" plus `CacheLoadGate` — implying the new readers reuse the whole kit. | `CacheLoadGate` cannot express "hit but stale" (`TryGetValue` returns before any generation check, `:23-26`), and putting the generation in the key to work around that creates the unbounded, caller-influenced key space its own comment forbids (`:7-9`). | **The decorator shape is copied; the gate is not.** The new readers do their own `TryGet → generation check → load → Set` (§4.2-5). Single-flight is deliberately dropped: this is a single-admin product where a cold-miss stampede on one key cannot occur; recorded so nobody re-adds the gate "for consistency" against its own rule. |
| DRIFT-4 | Item 23's write-path prose predates Slices F and G ("the writes are already funnelled through a small set of writers", `:1013-1014`). | F added `POST api/abwab/sections/{id}/order` → `EfAbwabSectionsWriter.ReorderAsync:79-115` (resequences the **whole** sections table); G reshaped `EfAbwabTemplateApplyWriter.ApplyAsync:24-186` to children-only (still writes `abwab_doors` + `abwab_door_aliases`). | §5.1's 21-row inventory, grepped at `5adc9bc0`, supersedes the audit's prose. Both F and G paths are ordinary rows in it: sections reorder bumps the tree generation; apply bumps the **tree** generation (it writes doors, never templates rows). |
| DRIFT-5 | Item 23 says nothing about CORS. | Both environments are cross-origin, and `ETag` is not a CORS-safelisted response header: without `WithExposedHeaders("ETag")`, `resp.headers.get('ETag')` is `null` in the browser, the facade never stores a validator, never sends `If-None-Match`, and the whole slice silently degrades to today's behavior — no error anywhere. | T303 adds `.WithExposedHeaders("ETag")` to the policy (`ServiceCollectionExtensions.cs:75-80`); T503's walk asserts the header is actually **read** (a stored non-null validator after first load), because this failure class is invisible to every test that talks to the backend directly. |
| DRIFT-6 | Commission decision 9: "Verify whether any `SmokeRouteCatalog` entry actually moves." | The smoke client sends no `If-None-Match`, so all three read entries keep their expectations: tree `200` dispatched (`:311`), templates list `200` ParityOnly (`:320-323`), template detail `404` ParityOnly (`:324-327`). No route is added, no verb/template/constraint changes. | **Verify-only; no catalog edit is scheduled** — the F/G precedent (`ux-slice-g` DRIFT-3). The tier still runs and is not optional (§4.1-8). Recorded so a reviewer does not read a missing catalog diff as a missing obligation. |

## 0. Guard result

Task arithmetic: Phase 1 = 2, Phase 2 = 3, Phase 3 = 3, Phase 4 = 3, Phase 5 = 3, Phase 6 = 2.
**16 tasks — under the 30-task threshold. One slice, no split.**

Recorded so a mid-execution split does not get drawn on task count: if this slice had split,
the seam is **after Phase 2** — backend cache + invalidation with **no HTTP change** (fully
shippable and reviewable alone: the reads get faster, nothing on the wire moves) versus
ETag/`304` + the frontend's conditional requests (Phases 3–4, the parts that change response
semantics on three routes and touch the browser). That is the commission's named seam and the
honest risk boundary: Phase 2 can be reverted without any client noticing; Phases 3–4 cannot.

## 1. Objective

| # | Deliverable | Home | Audit item |
|---|---|---|---|
| 1 | The tree snapshot and the templates reads are served from `IMemoryCache` behind generation-stamped decorators; a repeat read with no intervening write does not touch the database | `Infrastructure/Caching/Abwab/` (new), `AbwabDependencyInjection.cs` | 23 |
| 2 | Every one of the 21 abwab writes evicts, by construction: five invalidating writer decorators bump a per-resource generation in `finally`, after commit, before the handler resumes — a client that has just written can never be served a `304` or a pre-write body | same | 23, invariant `:959-966` |
| 3 | `GET api/abwab/tree`, `GET api/abwab/templates`, `GET api/abwab/templates/{id}` answer conditional GETs: `ETag` on every `200`, `304` with no body on a validator match, `Cache-Control: no-store` on both | the three controllers | 23 |
| 4 | The frontend sends `If-None-Match` from the facades and treats `304` as "keep current value" — the existing failure-keeps-previous-value semantics, minus the error; the unconditional `load()` on route entry stays | `abwab.api.ts`, both facades | 23 |
| 5 | The archive view is explicitly *not* a resource — covered by the tree entry, recorded in the READMEs so nobody builds a third cache | docs (§5.4) | 23 (`:1024-1026`) |
| 6 | The single-instance constraint is a recorded constraint with a named migration path, in a README — not a comment, not tribal knowledge | `Persistence/Reads/Abwab/README.md` | commission |
| 7 | Docs true in the same change: `API_GUIDELINES.md` gains the `304` rule, `API_INTEGRATION_GUIDELINES.md` gains conditional-request handling, both abwab persistence READMEs and the feature README amended, `TESTING_DEBT.md` rows added | six files, named in §5.4 | repo law |

## 2. Scope

**In:**

- **Backend — `Infrastructure/Caching/Abwab/` (new folder, mirroring `Caching/Quran/`)**
  - `AbwabCacheGeneration.cs` — the singleton counter pair + boot id (§4.2-2).
  - `CachedAbwabTreeReader.cs`, `CachedAbwabTemplatesReader.cs` — generation-stamped read
    decorators (§4.2-5).
  - `InvalidatingAbwabSectionsWriter.cs`, `InvalidatingAbwabDoorsWriter.cs`,
    `InvalidatingAbwabRelationsWriter.cs`, `InvalidatingAbwabTemplatesWriter.cs`,
    `InvalidatingAbwabTemplateApplyWriter.cs` — the five write decorators (§4.2-3).
- **Backend — `Application.Abstractions/Abwab/`** — `IAbwabCacheInvalidator.cs`,
  `IAbwabCacheValidators.cs` (§4.2-1).
- **Backend — API** — conditional-GET evaluation in `AbwabTreeController.cs` and
  `AbwabTemplatesController.cs` (§4.2-6/-7); `.WithExposedHeaders("ETag")` in
  `ServiceCollectionExtensions.cs:75-80` (DRIFT-5).
- **Backend — DI** — `AbwabDependencyInjection.cs`: readers and writers re-wired
  concrete-Ef + interface→decorator (the `Caching/Quran` registration shape); `AddMemoryCache()`
  (idempotent — already called at `MushafReaderDependencyInjection.cs:14`).
- **Frontend — `features/abwab/`**
  - `data-access/abwab.api.ts` — the three reads gain `observe: 'response'` + an optional
    validator argument (§4.2-11).
  - `state/abwab-snapshot.facade.ts`, `state/abwab-templates.facade.ts` — validator storage
    beside the value, `304` handling in `catchError` (§4.2-12/-13).
- **Docs (same change, repo law)** — `Backend/.architecture/API_GUIDELINES.md`,
  `.architecture/API_INTEGRATION_GUIDELINES.md`, `Persistence/Reads/Abwab/README.md`,
  `Persistence/Writes/Abwab/README.md`, `features/abwab/README.md`, `docs/TESTING_DEBT.md`,
  `docs/feature-ux-slice-i/evidence.md` (new), root `CLAUDE.md` (Active Spec Kit Feature, set
  and cleared).

**Out (named so nobody "finishes the thought"):**

- **Any non-abwab read.** The 12 `Cached*Reader` classes under `Caching/Quran/` and
  `CacheLoadGate.cs` are precedent only and are not touched, not even to "align" them.
- **The relations read** (`GET api/abwab/doors/{doorId}/relations`) — no cache, no ETag
  (§4.2-9). Relations *writes* are in scope as invalidator callers.
- **Distributed cache, Redis, `OutputCache`/`ResponseCache` middleware, CDN** — none.
- **TTL-based expiry anywhere** — eviction is explicit and write-driven; no
  `MemoryCacheEntryOptions` expiration on any abwab entry.
- **Snapshot-level conflict detection** — `xmin` stays the only concurrency currency;
  `AbwabTreeDto.Version` and `GetSnapshotVersionAsync` are not extended, not consumed by the
  validator, not removed.
- **Any change to any write's behavior, contract, or response** beyond passing through a
  decorator that bumps a counter. Same statuses, same envelopes, same messages.
- **New packages** — nothing here needs one; `IMemoryCache` and `Interlocked` ship in-box.
- **Frontend second cache layer / TTLs** — the facades stay the only client cache; the
  server's `Cache-Control: no-store` (§4.2-8) keeps the browser HTTP cache out too.
- **Any planning-artifact sweep or N-2 deletion** — deferred to the post-Slice-I cleanup pass
  (§10 note).
- **Any `dev → main` merge.**

## 3. Non-goals

- **No new test suites, per the rush-period posture** (continued from F/G/H) — and stated
  honestly: this is the series' highest-risk correctness work and the posture gives it **zero
  automated coverage of the new behavior**. §7 states exactly what the browser walk must
  therefore prove and writes debt rows specific enough to be payable.
- **No litigation of the locked decisions** — the counter-not-version choice, the indivisible
  tree entry, the kept unconditional `load()`, and the archive-is-not-a-resource rule are
  planned to, not argued with.
- **No planning-artifact sweep — standing user decision.** This is the last slice; the sweep
  is the *next* piece of work, after this slice closes, as its own pass (§10).

## 4. Locked decisions

### 4.1 Carried in from the commission / the audit / standing rules

1. **The ETag validator is an in-memory generation counter — USER DECISION.** Bumped per
   cached resource on every abwab write; rendered as an opaque, resource-scoped `ETag`.
   Chosen precisely because it **sidesteps the `version`-is-diagnostics-only rule entirely**:
   no snapshot-level token derived from row data is repurposed for anything concurrency-like,
   so the README's rule is not even grazed. `AbwabTreeDto.version`'s aggregate is not
   extended; the payload is not hashed. It resets on process restart — **safe** (every client
   refetches once; made literally true by DRIFT-2's boot scoping), and **correct only for a
   single backend instance** — recorded as a constraint with a migration path in
   `Persistence/Reads/Abwab/README.md` (§4.2-10), not discovered by a future reader.
2. **The tree snapshot is a single indivisible cache entry.** No partial invalidation — one
   root-affecting write bumps every row's `xmin` (`features/abwab/README.md:467-479`), and the
   README already forbids the narrower option. ANY door/section/relation/alias write evicts it
   entirely.
3. **Eviction is strictly ordered before the response returns.** A client that has just
   written must never be served a `304` or a pre-write body. Satisfied by construction via the
   writer-decorator placement (DRIFT-1, §4.2-3) — stated as the rule, not left to
   implementation.
4. **Every write calls the invalidator, and forgetting is made hard** — an explicit surface
   (`IAbwabCacheInvalidator`) mirroring the frontend's single-funnel shape, reached through
   the five writer seams so that a new writer method cannot compile without passing through a
   decorator (§4.2-3).
5. **The archive view is not a resource.** It is the `archivedRoots` partition of the same
   snapshot (`abwab-tree.builder.ts:91-94`), covered by the tree entry, and gets nothing of
   its own — said here and in the READMEs (§5.4), or someone builds a third cache.
6. **Frontend: `304` means "keep current value"** — already the facades' failure semantics.
   **The unconditional `load()` on route entry stays** (`abwab-page.component.ts:278`); with a
   validator it costs a `304`, not a body. No TTL, no second cache layer in front of the
   facades.
7. **Same-change README + architecture-doc amendments are repo law and in scope** (§5.4).
8. **Testing posture:** rush period — no new suites; existing suites RUN before merge; the
   route-smoke tier is REQUIRED (response semantics change on three existing routes,
   `TESTING_STRATEGY.md` §4 `:309`) and evidence states whether `Tests.Smoke.Data` RAN or
   SKIPPED; gaps become `docs/TESTING_DEBT.md` rows in the same change (§7).
9. **No catalog edit is owed** — verified, DRIFT-6.
10. **This is the last slice.** No sweep, no deletion, no repointing here; the cleanup pass is
    the next work item (§10 note).

### 4.2 Decided by this plan

1. **Two interfaces over one singleton.** `Application.Abstractions/Abwab/` gains
   `IAbwabCacheInvalidator { void InvalidateTree(); void InvalidateTemplates(); }` (the write
   side) and `IAbwabCacheValidators { string TreeETag(); string TemplatesListETag(); string
   TemplateETag(int templateId); }` (the read side), both implemented by one singleton.
   Segregated so controllers depend on validation only and writers' decorators on invalidation
   only; both live in Abstractions because controllers must not reference Infrastructure
   (`API_GUIDELINES.md:53-55`) and the writer decorators live in Infrastructure.
2. **The generation singleton, precisely.** `AbwabCacheGeneration` in
   `Infrastructure/Caching/Abwab/`: a `readonly string _bootId` (random, captured once at
   construction — e.g. a shortened `Guid`), and two `long` fields `_treeGen`, `_templatesGen`.
   Bump = `Interlocked.Increment`; read = `Interlocked.Read` (both documented lock-free
   thread-safe primitives; `IMemoryCache` is itself thread-safe). Registered as a **singleton**
   implementing both interfaces — the whole point is one counter per process, and the
   decorators (scoped) receive it by interface. ETag rendering, exact:
   `"\"abwab-tree-{bootId}-{treeGen}\""`, `"\"abwab-templates-{bootId}-{templatesGen}\""`,
   `"\"abwab-template-{id}-{bootId}-{templatesGen}\""` — strong validators (quoted), opaque to
   clients, resource-scoped by prefix so a list validator can never `304` a detail request.
   Lifecycle: starts at 0 each process start; only ever incremented; boot id makes
   cross-restart equality impossible (DRIFT-2).
3. **The invalidator call-site is the writer decorator, and the bump is in `finally`.** Five
   decorators, one per writer interface, each ctor `(Ef*Writer inner, IAbwabCacheInvalidator
   invalidator)`, each method `try { return await inner.X(...); } finally { bump(); }`.
   Why `finally` and not success-only: several writers run multi-save operations on implicit
   transactions (`Writes/Abwab/README.md:26-46`), so a thrown translated exception does not
   prove nothing committed — bumping on a failed write costs one spurious refetch; not bumping
   on a partially-committed one serves stale data. Fail direction chosen accordingly.
   Ordering: the inner method returns only after its commit (Precondition table), the
   decorator's `finally` runs immediately after, the handler and controller run after that —
   locked decision 3 holds with no discipline required. Mapping: sections / doors / relations /
   **apply** decorators call `InvalidateTree()`; the templates decorator calls
   `InvalidateTemplates()`. Apply writes `abwab_doors` + `abwab_door_aliases` and reads
   templates without mutating them (`EfAbwabTemplateApplyWriter.ApplyAsync:24-186`), so it
   bumps tree only; template/node CRUD never touches the snapshot
   (`Reads/Abwab/README.md:102-105`), so it bumps templates only. Full route-by-route map in
   §6b.
4. **DI shape copies the `Caching/Quran` precedent.** In `AbwabDependencyInjection.cs`:
   each `Ef*` registered as its concrete self, each interface re-pointed at its decorator
   (readers → `Cached*`, writers → `Invalidating*`); `AbwabCacheGeneration` registered once as
   singleton against both interfaces. No Scrutor, no new package — manual decoration exactly as
   `CachedRootsReader` is wired.
5. **The cached readers are generation-stamped, and capture-before-load is the rule.**
   `CachedAbwabTreeReader(EfAbwabTreeReader ef, IMemoryCache cache, IAbwabCacheValidators v /
   AbwabCacheGeneration gen)` stores, under the fixed key `"abwab:tree"`, a record
   `(long Gen, AbwabTreeDto Tree)`. Read path: capture the current generation **first**, then
   `TryGet` — serve only if the stored stamp equals the captured generation; otherwise load
   from `ef`, store stamped with the **captured** generation, return. The capture-first order
   is load-bearing: if a write commits and bumps mid-load, the entry is stamped with the older
   generation and is dead on arrival — the failure direction is always an extra reload, never
   a stale hit. Same shape for `CachedAbwabTemplatesReader` with keys `"abwab:templates"`
   (list) and `"abwab:template:{id}"` (detail, both stamped with the templates generation).
   No `MemoryCacheEntryOptions` expiration (non-goal: no TTL); dead per-id entries for deleted
   templates linger as inert stamped tombstones bounded by the count of template ids ever
   fetched — accepted and recorded. **No `CacheLoadGate`** (DRIFT-3). No `IMemoryCache.Remove`
   anywhere — eviction is the stamp mismatch, which is atomic with the bump.
6. **The controller conditional flow, exact** (tree; templates identical with their
   validators):
   - Capture `etag = validators.TreeETag()` **before** anything else — the same
     capture-before-load rule as §4.2-5, same rationale: captured-then-bumped means the served
     `ETag` is older than current, and the next revalidation refetches; the reverse order
     could stamp fresh-looking validators onto pre-write bodies.
   - If the `If-None-Match` request header matches (§4.2-7): set `Response.Headers.ETag = etag`
     and `Cache-Control: no-store`, `return StatusCode(StatusCodes.Status304NotModified);` —
     `StatusCodeResult` writes status only, no body, which is exactly RFC 9110's `304`
     contract and the framework-documented shape ("`304 Not Modified` with no body").
     **The `304` path runs zero queries** — no handler call, no reader, no DB.
   - Otherwise: call the handler as today, set the same two headers on the response, return
     the existing `Ok(ApiResponse<T>.Ok(...))` unchanged. An **empty** tree is still a `200`
     with a body — empty is data, not not-modified.
7. **Header comparison: exact ordinal member match, fail-open.** Split `If-None-Match` on
   commas, trim, compare each member for ordinal equality with the current validator
   (weak-prefix `W/` members are compared by their opaque remainder — the backend never emits
   weak tags, so in practice they miss). Anything else — absent header, garbage, `*`,
   unparseable — is a non-match and gets a full `200`. Deviations from full RFC 9110
   `If-None-Match` semantics (notably `*`) are deliberate for a single first-party client and
   recorded in `API_GUIDELINES.md` (§5.4).
8. **`Cache-Control: no-store` on all three conditional reads, both `200` and `304`.**
   Without it, an `ETag`-bearing response is heuristically revalidatable by the **browser's**
   HTTP cache, which would become a second, invisible validator layer racing the facade's
   explicit one (locked decision 6 forbids a second layer). `no-store` keeps exactly one
   cache per end: the server's `IMemoryCache`, the client's facades. Deterministic and
   walkable.
9. **The relations read stays unconditional, on purpose.** The frontend fetches it per
   modal-open with no held prior value (`abwab-relations.controller.ts:33-42`) — a `304`
   would have nothing to render against, and a per-door validator inventory would be a third
   cache resource for zero saved bytes. Relations **writes** still bump the tree generation
   (they change `RelationCount` on two snapshot rows). Recorded in both READMEs (§5.4).
10. **The single-instance constraint, recorded where it can be found.** The canonical home is
    `Persistence/Reads/Abwab/README.md`'s new caching section (replacing the "No caching"
    bullet at `:106-108`): the generation pair is per-process memory; with two backend
    instances, a write on instance A leaves instance B's counter — and B's cached snapshot —
    untouched, so B serves stale `304`s/bodies and the refresh-after-write invariant breaks
    exactly as `features/abwab/README.md:467-479` warns (spurious `409`s follow). Production
    is Railway, currently single-instance — the same recorded posture as the rate limiter
    (`API_GUIDELINES.md:263-264`, "Per-instance: … acceptable at single-instance"). Migration
    path if a second instance ever runs: move the generation to shared state bumped in the
    write transaction (a one-row Postgres table or sequence read by the validator), which
    slots behind `IAbwabCacheInvalidator`/`IAbwabCacheValidators` without touching a caller.
    `API_GUIDELINES.md`'s `304` section points at this README rather than restating it.
11. **The API layer's first `observe: 'response'`.** The three read methods in `abwab.api.ts`
    gain an optional `etag: string | null` parameter and return
    `Observable<HttpResponse<ApiResponse<T>>>`; they attach `If-None-Match` only when a
    validator is passed, and stay mapping-free — reading `.body` and `.headers.get('ETag')`
    is the facade's job (`API_INTEGRATION_GUIDELINES.md`: API services minimal, facades own
    orchestration). No interceptor is added or changed.
12. **The snapshot facade's `304` path, exact.** New private `etagState: string | null`
    beside `rawTree` — **the validator and the value are one unit**: written together on a
    `200` (`rawTree` from `.body`, `etagState` from the `ETag` header), both kept on failure,
    both kept on `304`, never written separately. `fetch()` passes `etagState` to the api.
    In `catchError`: `if (err instanceof HttpErrorResponse && err.status === 304)` → set
    loading false, **do not set error**, return the current snapshot — byte-for-byte the
    existing keep-previous-value branch (`:66-69`) minus the error write. Every other error
    keeps today's behavior. The `304` is therefore distinguishable from failure by
    construction (`err.status`), and the facades' "failure leaves the previous value"
    semantics cannot silently absorb it because the error signal stays null — the page shows
    content, not an error banner. Loading/skeleton behavior needs no change: skeletons
    already gate on value absence (Precondition table).
13. **The templates facade: two validators, the selected one id-keyed.** `listEtagState:
    string | null` beside `rawList`; `selectedEtagState: { id: number; etag: string } | null`
    beside `rawSelected` — sent only when the id being fetched equals the stored id, dropped
    when `select()` targets a different template (a validator must never travel across
    resources). `304` handling identical to §4.2-12 in both `fetchList` and `fetchSelected`;
    the existing identity guard (`:56-62`) is untouched.
14. **Post-write refetches stay unconditional in *effect*, conditional in *form*.**
    `refreshAndRebind` (`abwab-write.controller.ts:316-322`) and the templates controller's
    refreshes (`abwab-templates.controller.ts:106,120`) call the same facade fetch and now
    send the pre-write validator — which the write's own bump has already invalidated, so the
    response is a guaranteed `200` with a fresh body (§6a row 7). No code in either write
    controller changes.

## 5. The ground truth this plan is derived from

### 5.1 The write-path inventory — 21 routes, 5 writer seams, each mapped to what it evicts

Grepped at `5adc9bc0`, controller-verified. **A write path missing from this table is the
defect class this slice exists to avoid.** "tree" = bump `InvalidateTree()` (evicts the tree
entry + changes the tree ETag); "templates" = bump `InvalidateTemplates()` (evicts list +
every per-id entry + changes both templates ETags).

| # | Route | Controller | Writer method | Mutates | Evicts |
|---|---|---|---|---|---|
| 1 | `POST api/abwab/sections` | `AbwabSectionsController.cs:17` | `EfAbwabSectionsWriter.CreateAsync:9-26` | `abwab_sections` | tree |
| 2 | `PUT api/abwab/sections/{id}` | `:37` | `RenameAsync:28-48` | `abwab_sections` | tree |
| 3 | `DELETE api/abwab/sections/{id}` | `:62` | `DeleteAsync:50-77` | `abwab_sections` | tree |
| 4 | `POST api/abwab/sections/{id}/order` (Slice F) | `:80` | `ReorderAsync:79-115` (whole-table resequence) | `abwab_sections` | tree |
| 5 | `POST api/abwab/doors` | `AbwabDoorsController.cs:25` | `EfAbwabDoorsWriter.CreateAsync:9-54` | `abwab_doors`, `abwab_door_aliases` | tree |
| 6 | `PUT api/abwab/doors/{id}` | `:49` | `EditAsync:56-88` | `abwab_doors`, `abwab_door_aliases` | tree |
| 7 | `POST api/abwab/doors/{id}/move` | `:73` | `MoveAsync:90-158` | `abwab_doors` | tree |
| 8 | `POST api/abwab/doors/{id}/order` | `:100` | `ReorderAsync:160-189` | `abwab_doors` | tree |
| 9 | `POST api/abwab/doors/bulk-move` | `:128` | `BulkMoveAsync:220-320` | `abwab_doors` | tree |
| 10 | `POST api/abwab/doors/bulk-archive` | `:156` | `BulkArchiveAsync:322-376` | `abwab_doors` | tree |
| 11 | `DELETE api/abwab/doors/{id}` (archive) | `:176` | `DeleteAsync:378-404` | `abwab_doors` | tree |
| 12 | `POST api/abwab/doors/{id}/restore` | `:193` | `RestoreAsync:406-495` | `abwab_doors` | tree |
| 13 | `POST api/abwab/doors/{doorId}/relations` | `AbwabDoorRelationsController.cs:31` | `EfAbwabRelationsWriter.AddAsync:9-65` | `abwab_door_relations` | tree (`RelationCount` on snapshot rows) |
| 14 | `DELETE api/abwab/relations/{relationId}` | `:64` | `DeleteAsync:67-93` | `abwab_door_relations` | tree |
| 15 | `POST api/abwab/templates` | `AbwabTemplatesController.cs:49` | `EfAbwabTemplatesWriter.CreateAsync:9-43` | `abwab_templates`, `abwab_template_nodes` | templates |
| 16 | `DELETE api/abwab/templates/{templateId}` | `:68` | `DeleteAsync:45-72` | `abwab_templates` | templates |
| 17 | `POST api/abwab/templates/{templateId}/apply` (Slice G, children-only) | `:84` | `EfAbwabTemplateApplyWriter.ApplyAsync:24-186` | `abwab_doors`, `abwab_door_aliases` | **tree** (reads templates, never writes them) |
| 18 | `POST api/abwab/templates/{templateId}/nodes` | `AbwabTemplateNodesController.cs:19` | `EfAbwabTemplatesWriter.AddNodeAsync:74-123` | `abwab_template_nodes` | templates |
| 19 | `PUT api/abwab/template-nodes/{nodeId}` | `:47` | `EditNodeAsync:125-151` | `abwab_template_nodes` | templates |
| 20 | `POST api/abwab/template-nodes/{nodeId}/order` | `:69` | `ReorderNodeAsync:153-187` | `abwab_template_nodes` | templates |
| 21 | `DELETE api/abwab/template-nodes/{nodeId}` | `:90` | `DeleteNodeAsync:189-227` | `abwab_template_nodes` | templates |

Rows 1–14 + 17 (15 routes, 4 writer interfaces) → tree. Rows 15–16 + 18–21 (6 routes, 1 writer
interface) → templates. Every row reaches its bump through the interface its handler already
injects — zero handler edits. The bypass census (Precondition table) found no 22nd path.

### 5.2 The cache-entry inventory

| Key | Holds | Stamped with | Logically evicted by | Must NOT be split into |
|---|---|---|---|---|
| `abwab:tree` | the whole `AbwabTreeDto` — live outline, **archived doors** (the `archivedRoots` partition is client-side, `abwab-tree.builder.ts:91-94`), aliases, `RelationCount`s, the diagnostics `Version` | tree generation | any of rows 1–14, 17 above | per-section / per-scope / live-vs-archive entries — locked decision 2 and `features/abwab/README.md:467-479` forbid it: one root-affecting write stales every row |
| `abwab:templates` | the templates list (`IReadOnlyList<AbwabTemplateSummaryDto>`) | templates generation | rows 15–16, 18–21 | — |
| `abwab:template:{id}` | one `AbwabTemplateDto` | templates generation | rows 15–16, 18–21 (shared generation — a node edit anywhere invalidates every detail entry; acceptable, admin-scale) | per-node entries |

Not cached, not conditional: the relations read (§4.2-9).

### 5.3 `ApiResponse<T>` × `304`

The envelope (`API_GUIDELINES.md:102-141`) assumes a body on every status its §4 lists
(`:85-100`); `204` is today's only sanctioned bodiless response (`:91`), and the frontend
already handles it as a null envelope (`features/abwab/README.md:703-718` — the shipped
precedent that a status, not the envelope, can carry the meaning). A `304` is the second
bodiless status and the first that is *not* a success-of-a-write: it means "your cached
representation is current", and RFC 9110 §15.4.5 plus the framework's own contract forbid
content on it. Resolution: **the envelope contract is scoped, not changed** — conditional
GETs return `ApiResponse<T>` on `200` and nothing but validator headers (`ETag`,
`Cache-Control`) on `304`. The `ApiResponse<T>` type itself does not change, so no non-abwab
route can be affected (stop condition 4 was checked and is clear). `API_GUIDELINES.md` §4
gains the `304` line and §5 gains the rule (T601), making it a rule rather than an abwab
exception.

### 5.4 The amendment ledger — every recorded statement, by file

| File | What is there now | Treatment |
|---|---|---|
| `Backend/.architecture/API_GUIDELINES.md` | §4 (`:85-100`) has no `304`; §5 (`:102-141`) assumes a body | §4 gains: `304 Not Modified` — conditional GETs only, no body, `ETag` + `Cache-Control` headers required, the second sanctioned bodiless status after `204`. §5 gains the conditional-GET rule: every `200` from a conditional read carries `ETag`; comparison is exact ordinal member match, fail-open (§4.2-7); validators are opaque server generations, never data-derived; single-instance constraint lives in `Persistence/Reads/Abwab/README.md` (pointer, not restatement). T601 |
| `.architecture/API_INTEGRATION_GUIDELINES.md` | Silent on caching; "HTTP Errors vs Backend Failure Responses" (`:285-308`) knows two failure categories | Gains a conditional-request section: the third category — `304` arrives on the error channel (`HttpClient` fact + the installed-source citation), is **not** a failure, means keep-current-value; validators live in facades beside the value they validate; API services pass them through and stay mapping-free; `observe: 'response'` is the sanctioned shape for validator-bearing reads. T601 |
| `Persistence/Reads/Abwab/README.md` | "**No caching.** … no invalidation story yet …" (`:106-108`) | Replaced by the caching section: the two decorators, the generation stamp + capture-before-load rule, the key inventory (§5.2), why `CacheLoadGate` is not used (DRIFT-3), **the single-instance constraint + migration path** (§4.2-10), and the relations-read exclusion. The `Version`-ignores-relations bullet (`:88-92`) gains one clause: the cache validator ignores `Version` right back — generations, not row data. T601 |
| `Persistence/Writes/Abwab/README.md` | Five writers, translation helpers, resequencing invariants (`:15-24,26-46`) | Gains the eviction obligation: every writer interface is DI-wrapped by an invalidating decorator; the bump is in `finally`, after the inner commit, before the handler resumes (the ordering rule, stated as such); **a sixth writer or a new method added to any interface MUST go through / be added to its decorator** — the compile error is the guard, the `finally` bump is the reviewer's checklist line. T601 |
| `features/abwab/README.md` | The `version` gotcha (`:507-508`); facade contracts (`:11-19` class doc, `:257-259`); refresh-after-write (`:467-479`); "apply refreshes nothing" (`:655-660`); the `204` gotcha (`:703-718`) | The gotcha is **amended to distinguish, not weakened**: `version` stays diagnostics-only and is *also not* the cache validator — cache validation (the `ETag` generation, server memory, no row data) and conflict detection (`xmin`, the only concurrency currency) are different jobs and neither uses `version`. New paragraphs: the facades hold `If-None-Match` validators beside their values (one unit); `304` = keep current value, no error; the archive view is a partition of the cached snapshot, **not a cacheable resource** (locked decision 5); the route-entry `load()` stays unconditional and now costs a `304` when nothing changed. T601 |
| `docs/TESTING_DEBT.md` | ends at `:113` (`ux-slice-h`) | New `ux-slice-i` section, §7's rows I1–I4. T602 |
| root `CLAUDE.md` | Active Spec Kit Feature: `None` (`:204-206`) | `ux-slice-i` + this plan at T101; back to `None` at T602. |

**Do not touch, and do not "fix" while here:** `AbwabTreeDto.Version` and
`GetSnapshotVersionAsync` (diagnostics, stays); `CacheLoadGate.cs` and every `Caching/Quran`
reader; the relations read; any writer's body, helper, or resequencing logic; the `204`
handling in `abwab-write.controller.ts#handleSuccess`; the smoke catalog's three read entries
(DRIFT-6); the rate limiter's per-instance paragraph (its posture is cited, not edited);
`devLatencyInterceptor` (a delayed `304` in dev is cosmetic).

### 5.5 The facades' state machine, before → after

| Event | Before (`abwab-snapshot.facade.ts:50-76`) | After |
|---|---|---|
| `fetch()` start | loading ← true, error ← null; pending unsubscribed | same; request carries `If-None-Match: etagState` when set |
| `200`, `isSuccess` | loading ← false, `rawTree` ← data | same, **plus** `etagState` ← `ETag` header (one unit with the value) |
| `200`, `!isSuccess` | loading ← false, error ← message | same; validator untouched (value untouched) |
| transport error | loading ← false, error set, **previous value kept** | same, previous **value + validator** kept |
| **`304`** | — (unreachable today) | loading ← false, **error stays null**, value + validator kept — keep-current-value, distinguishable from failure by `err.status === 304` |
| skeleton | `isLoading() && !snapshot()` — value present ⇒ no skeleton | unchanged; a `304` revisit shows content with zero flash (verified precondition) |

`AbwabTemplatesFacade`: identical per resource, with the selected validator id-keyed
(§4.2-13).

## 6. Phases

Every phase is one commit. Build green at each boundary.

### Phase 1 — Baseline and record (2 tasks)

**Files** — root `CLAUDE.md`; `docs/feature-ux-slice-i/evidence.md` (new).

- **T101 — Baseline, recorded before anything is touched.** Set Active Spec Kit Feature to
  `ux-slice-i` + this plan. Create `evidence.md`; record as measured numbers:
  `dotnet build Backend/QuranDashboard.sln`, the no-pipeline regression (expect 1,086 / ~21 s),
  the smoke tier (expect 140 / ~52 s, **with the `Tests.Smoke.Data` RAN/SKIPPED statement**),
  `npm test` (expect 191 files / 2,161 tests), `npm run build` — exact commands from
  `TESTING_STRATEGY.md` §5 `:341-358` / §6 `:401-414`. Record one wire measurement for later
  comparison: the byte size of the `GET api/abwab/tree` response body on the local dataset
  (browser network tab or `curl -w`). A baseline that is not green is a stop, not a start.
- **T102 — Sweep for recorded statements this slice falsifies.** `grep -rn` across
  `Backend/`, `Frontend/quran-dashboard-ui/src/`, `docs/`, `.architecture/`,
  `Backend/.architecture/` for: `No caching`, `no invalidation`, `ETag`, `If-None-Match`,
  `304`, `unconditional`, `diagnostics only`, `diagnostics-only`. Every hit must be in §5.4's
  ledger or its do-not-touch list, or it is a finding folded into the ledger before Phase 2.
  Record grep + result. (Plan-time sweep found: the two README gotchas, the Reads-README
  no-caching bullet, the audit itself — nothing else.)

### Phase 2 — Backend cache + invalidation, no HTTP change (3 tasks)

**Files** — `Application.Abstractions/Abwab/IAbwabCacheInvalidator.cs`,
`IAbwabCacheValidators.cs` (new); `Infrastructure/Caching/Abwab/*` (new);
`AbwabDependencyInjection.cs`.

- **T201 — The generation singleton and its two interfaces.** Per §4.2-1/-2: the boot id, the
  two `Interlocked` counters, the three ETag renderings. A `//` comment carries the one
  non-obvious rule each: capture-before-load / bump-in-`finally` reasons live where the code
  is (§4.2-5/-3); the single-instance constraint does **not** go in a comment — it goes in
  the README (T601), per the commission. DI: singleton against both interfaces.
- **T202 — The five invalidating writer decorators.** Per §4.2-3 and the §5.1 mapping. DI
  re-wire: concrete `Ef*Writer` + interface→decorator. Gate: `dotnet build` +
  no-pipeline filter (`Tests.Abwab` rides inside it) — every write behaves identically
  through the decorators.
- **T203 — The two cached readers.** Per §4.2-5; DI re-wire for the two reader interfaces
  (`IAbwabRelationsReader` stays direct). `AddMemoryCache()` in `AbwabDependencyInjection` if
  not already reachable (idempotent). Gate: build + no-pipeline + `Tests.Api`. Nothing on the
  wire has changed yet — this commit is revertable without any client noticing (the recorded
  split seam).

### Phase 3 — The conditional HTTP surface (3 tasks)

**Files** — `AbwabTreeController.cs`, `AbwabTemplatesController.cs`,
`ServiceCollectionExtensions.cs`.

- **T301 — The tree route.** Per §4.2-6/-7/-8: capture-first, exact-member match, `304` via
  `StatusCode(StatusCodes.Status304NotModified)` with `ETag` + `Cache-Control: no-store` set,
  `200` path unchanged plus the same two headers. Zero queries on the `304` path.
- **T302 — The two templates routes.** Same flow with `TemplatesListETag()` /
  `TemplateETag(id)`. The detail route's `404` (unknown id) is returned **without** validator
  headers — a not-found has no representation to validate; the ETag capture still happens
  first and is simply unused on that arm.
- **T303 — CORS exposure.** `.WithExposedHeaders("ETag")` added to the policy chain at
  `ServiceCollectionExtensions.cs:75-80` (DRIFT-5). Gate for the phase: build + `Tests.Api` +
  the **route-smoke tier** (`--filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."`),
  evidence stating `Tests.Smoke.Data` RAN or SKIPPED, and the recorded verification that all
  three catalog read entries pass **unedited** (DRIFT-6).

### Phase 4 — The frontend conditional requests (3 tasks)

**Files** — `data-access/abwab.api.ts`, `state/abwab-snapshot.facade.ts`,
`state/abwab-templates.facade.ts`.

- **T401 — The api layer.** Per §4.2-11: `getTree`, `getTemplates`, `getTemplate` gain the
  optional validator parameter and `observe: 'response'`; header attached only when a
  validator is passed; return type `Observable<HttpResponse<ApiResponse<T>>>`; no mapping, no
  interceptor. Callers (the two facades) updated in the same commit as their signatures move
  — this task and T402/T403 land as **one commit** (the phase boundary), split here only as
  work units.
- **T402 — The snapshot facade.** Per §4.2-12 and the §5.5 table: `etagState`, the one-unit
  write, the `304` branch in `catchError` before the generic error branch. The class doc
  (`:11-19`) gains one sentence: the validator is stored beside the snapshot and a `304`
  reuses the failure path's keep-previous-value semantics without the error.
- **T403 — The templates facade.** Per §4.2-13: `listEtagState`, id-keyed
  `selectedEtagState`, both `fetch*` paths. Gate for the phase: `npm test` (full — the facade
  specs `abwab-templates.facade.spec.ts` must stay green **unedited**; if a spec must change,
  that is a finding to record, not silently absorb) + `npm run build`.

### Phase 5 — Verification (3 tasks)

- **T501 — Backend gates.** `dotnet build`; no-pipeline (1,086 expected); `Tests.Api` (60);
  route-smoke tier (140) with the `Tests.Smoke.Data` RAN/SKIPPED statement. Counts that move
  against T101 are explained per-test or they are findings.
- **T502 — Frontend gates.** Full `npm test` (expect T101's 191 / 2,161 **unchanged** — this
  slice writes no spec) and `npm run build`.
- **T503 — The browser walk — the only check the posture leaves for the new behavior.**
  Kestrel started with the frontend's mkcert PEM (session memory: anything else reads as
  backend failure). Walk and record in `evidence.md`, with the network tab open:
  - **§6a row by row, URL and status observed, not inferred:** first `/abwab` load → `200` +
    `ETag` visible **and readable** (assert the facade stored a non-null validator — the
    DRIFT-5 acceptance; a null here is a CORS regression, not a pass); navigate away and back
    → `304`, no body bytes, **no skeleton flash**, content instant; a write (rename a door) →
    the funnel's refetch is `200` with a fresh body — **the just-wrote row, walked
    explicitly**; repeat for templates list and detail; a template-node edit → templates
    refetch `200` while a subsequent tree GET still answers `304` (generation independence);
    a relation add → tree refetch `200` (row 13's eviction); archive view toggle → **no new
    request semantics** (same snapshot, `archivedRoots` partition — locked decision 5
    observed live).
  - **Restart:** restart the backend, revisit → `200` (boot id changed), exactly one
    refetch, no error surfaced.
  - **Malformed header:** one manual request (curl/devtools) with `If-None-Match: garbage`
    and one with `*` → both `200` (fail-open, §4.2-7).
  - **Error path intact:** stop the backend, trigger a refetch → the existing error banner,
    previous content kept; restart, retry → recovery. A `304` must never present as this.
  - **The measurement (stated as numbers to take, not claims):** record `200` body bytes vs
    `304` (expect 0), and devtools timing for the `304` against T101's baseline `200`. These
    go in `evidence.md` as the slice's performance evidence.

### Phase 6 — Docs true again (2 tasks)

- **T601 — The six-file amendment pass** per §5.4, including the single-instance constraint
  in `Persistence/Reads/Abwab/README.md` and the `version` gotcha amendment that
  distinguishes validation from detection without weakening either rule.
- **T602 — Debt and close-out.** Append the `ux-slice-i` section to `docs/TESTING_DEBT.md`
  (§7 rows I1–I4). Re-run T102's sweep; every remaining hit amended or recorded. Clear the
  root `CLAUDE.md` Active Spec Kit Feature to `None`. **No planning folder deleted, swept, or
  repointed** — and `evidence.md` closes with the note that the series is complete and the
  deferred planning-artifact cleanup pass (root `CLAUDE.md` lifecycle rule, N-2 buffer) is
  the next piece of work, as its own commission.

| Phase | Commit | Gate before the next phase starts |
|---|---|---|
| 1 | `docs(ux-slice-i): baseline and falsified-statement sweep` | T101 green; T102 folded into §5.4 |
| 2 | `feat(ux-slice-i): abwab cache generations, invalidating writers, cached readers` | build + no-pipeline + `Tests.Api` green; wire unchanged |
| 3 | `feat(ux-slice-i): ETag/304 conditional GETs on the three abwab reads` | build + `Tests.Api` + smoke green, catalog unedited |
| 4 | `feat(ux-slice-i): the facades send If-None-Match and keep value on 304` | `npm test` + build green, facade specs unedited |
| 5 | `test(ux-slice-i): browser walk, conditional matrix, and measurements` | every §6a row observed and recorded |
| 6 | `docs(ux-slice-i): the 304 rule, the caching READMEs, and the debt this slice owes` | sweep clean |

## 6a. The conditional-request matrix — client state × response

The substance of the slice. Every row carries status, body, and headers; §4.2-6/-7/-8 are the
mechanism. "g" = the current generation's rendered validator.

| # | Client state / request | Response status | Body | Headers |
|---|---|---|---|---|
| 1 | Fresh client — no `If-None-Match` | `200` | full `ApiResponse<T>` envelope | `ETag: g`, `Cache-Control: no-store` |
| 2 | `If-None-Match` = current validator | `304` | **none** — zero bytes, zero DB queries | `ETag: g`, `Cache-Control: no-store` |
| 3 | `If-None-Match` = stale validator (older generation, or pre-restart boot id) | `200` | full envelope, fresh data | `ETag: g'` (new), `no-store` |
| 4 | Malformed header — garbage, unquoted, `*`, or a list with no exact member match | `200` | full envelope | `ETag: g`, `no-store` — fail-open, §4.2-7 |
| 5 | Absent header (row 1 restated for the matrix's completeness) | `200` | full envelope | same |
| 6 | Request racing a concurrent write | `200` with either the pre-write body + pre-write `ETag` (capture happened before the bump — the client revalidates next time and refetches) or the post-write body + post-write `ETag`; a `304` is possible only if the capture happened strictly before the bump, i.e. the response is honest about a moment before the commit. Capture-before-load (§4.2-5/-6) guarantees the served validator is never *newer* than the served data — the failure direction is an extra refetch, never a stale `304` | | |
| 7 | **The just-wrote client** — the trap this design exists to prevent: write returns → funnel refetches with the pre-write validator | **`200`, always.** The bump ran in the writer decorator's `finally` — after commit, before the handler resumed, therefore strictly before the write's HTTP response, therefore strictly before the refetch was sent. The pre-write validator cannot match the post-bump generation, and the boot id rules out every cross-restart accident. Walked explicitly in T503 | fresh envelope | `ETag: g'`, `no-store` |
| 8 | After backend restart, client holds any pre-restart validator | `200` (boot id differs — DRIFT-2); exactly one refetch per client per resource, then `304`s resume | fresh envelope | new-boot `ETag` |

Frontend rendering of each row: rows 1/3/5/8 → the facade's `200` path (value + validator
replaced); row 2 → the `304` path (keep both, no error, no skeleton); row 4 is
server-internal; rows 6/7 → ordinary `200` handling.

## 6b. The eviction map — writer seam × generation

| Writer seam (decorator) | Routes (§5.1) | Bumps | Why |
|---|---|---|---|
| `IAbwabSectionsWriter` | 1–4 | tree | sections shape the snapshot; F's reorder resequences the whole table |
| `IAbwabDoorsWriter` | 5–12 | tree | doors + aliases are the snapshot |
| `IAbwabRelationsWriter` | 13–14 | tree | `RelationCount` lives on snapshot rows (`EfAbwabTreeReader.cs:62-82`) |
| `IAbwabTemplateApplyWriter` | 17 | tree | writes doors/aliases; reads templates without mutating (`Reads/Abwab/README.md:102-105`) |
| `IAbwabTemplatesWriter` | 15–16, 18–21 | templates | template tables never touch the snapshot (same README) |

No seam bumps both; no write bumps neither. The templates generation covers the list **and**
every per-id entry (§5.2) — a node edit on template A also invalidates template B's cached
detail, accepted at admin scale in exchange for one counter instead of a per-id registry.

## 7. Testing posture and the debt it owes

**Posture (locked, §4.1-8):** no new suites; existing suites RUN; the route-smoke tier is
required and not debt-able. **The honest tension, stated:** this slice changes response
semantics on three routes, adds the backend's first invalidation machinery, and sends the
frontend's first conditional request — and the posture gives the *new* behavior zero automated
coverage. Every §6a row therefore rides on T503's browser walk plus the smoke tier's
"unconditional requests still answer exactly as catalogued" guarantee. That is why T503 is
written as an enumerated protocol with per-row observation, not a "click around" task, and why
the debt rows below are scoped to be individually payable.

**The gates that run (validated commands, `TESTING_STRATEGY.md` §5 `:341-358` / §6 `:401-414`):**

- `dotnet build Backend/QuranDashboard.sln` — T101, T202, T203, T303, T501.
- No-pipeline regression (1,086 / ~21 s) — T101, T202, T203, T501.
- `Tests.Api` (60 / ~10 s) — T203, T303, T501.
- Route-smoke tier (140 / ~52 s) — T101 (baseline), T303, T501 — **each run's evidence states
  whether `Tests.Smoke.Data` RAN or SKIPPED.**
- `npm test` (191 / 2,161 / ~205 s) + `npm run build` — T101, T403 (phase gate), T502.
- **Not a gate:** e2e (opt-in, never a tier); no full backend suite / pipeline families —
  no Tier D trigger fires (no pipeline code, tables, or shared persistence touched).

**The rows (`docs/TESTING_DEBT.md`, new `ux-slice-i` section):**

| # | Uncovered area | Where | Pays it |
|---|---|---|---|
| I1 | **The generation lifecycle** — capture-before-load never serving a validator newer than its data; bump-in-`finally` on the partially-committed implicit-txn paths; boot-scoped validators never colliding across restarts (the DRIFT-2 hole, closed by construction and asserted by nothing) | `Infrastructure/Caching/Abwab/` | The next cached resource, **or** the multi-instance migration (§4.2-10) — the shared-generation implementation must prove exactly these properties anyway |
| I2 | **The conditional-GET contract of the three routes** — the §6a matrix as dispatched smoke cases: match→`304` bodiless with headers, mismatch→`200`+`ETag`, malformed/`*` fail-open, `404` without validator headers. All three routes are already catalogued; these are additional dispatched cases, not entries | `Tests/Smoke/` | When write protection lands and `/api/abwab` stops being `Open` (the standing trigger for every abwab smoke row), **or** the next change to any conditional read |
| I3 | **The facades' `304` path** — keeps value + validator, sets no error, ends loading; the id-keyed selected validator never travels across templates. Assertable today in jsdom with `HttpTestingController` (`flush(null, { status: 304, … })`); the existing `abwab-templates.facade.spec.ts` is the shipped template and this is the cheapest row here | `abwab-snapshot.facade.ts`, `abwab-templates.facade.ts` | The next change to either facade or to the api layer's response shape |
| I4 | **The just-wrote invariant end to end** — one e2e flow: load `/abwab`, rename a door, assert the refetch was a `200` (not `304`) and the new name renders. The whole design exists for §6a row 7 and only a browser proves it | `e2e/` | The next write path added to abwab, or the multi-instance migration — both re-open the ordering question |

I3 is the honest one to flag in review: unlike H's rows it needs no browser, no new harness,
and an existing spec file demonstrates the pattern — deferring it is a choice, not a
constraint.

## 8. Risk register

| # | Risk | Likelihood | Blast radius | Mitigation in this plan |
|---|---|---|---|---|
| 1 | **A stale `304` after a write** — the one failure the series cannot absorb: the funnel's refetch keeps the pre-write snapshot with pre-write `xmin` tokens, and the next write `409`s spuriously (`features/abwab/README.md:467-479`) | low by construction, catastrophic by nature | The governing invariant breaks | Bump ordered inside the writer seam (§4.2-3); capture-before-load on both reader and controller (§4.2-5/-6); §6a row 7 walked explicitly in T503; debt row I4 |
| 2 | **Silent CORS degradation** — `ETag` unreadable, facade stores null, every request is a full `200` forever, nothing errors | certain without T303 | The slice ships and does nothing | DRIFT-5; T303; T503 asserts a stored non-null validator |
| 3 | A future write path (sixth writer, new interface method) skips the invalidator | medium over time | Stale cache re-appears | Interface growth breaks the decorator's build (§4.2-3); `Writes/Abwab/README.md` gains the obligation (§5.4); the bypass census is recorded as the review baseline |
| 4 | Restart-counter collision serves a stale `304` to an idle client | impossible after DRIFT-2 | — | Boot-scoped validator; T503 restart walk |
| 5 | The browser HTTP cache becomes a second validator (heuristic revalidation of `ETag`-bearing responses) and races the facade's | high without `no-store` | Nondeterministic staleness, unwalkable matrix | §4.2-8: `Cache-Control: no-store` on `200` and `304` |
| 6 | The facade's error path absorbs the `304` and shows a failure banner over live data, or clears the value | medium if implemented casually | Every cached revisit looks broken | §4.2-12: the `304` branch precedes the generic branch and is keyed on `err.status === 304` (installed-source fact); §5.5 table is the contract; facade specs must pass unedited (T403 gate) |
| 7 | Per-id template entries or gate semaphores grow unbounded | low | Memory creep | No `CacheLoadGate` (DRIFT-3); per-id entries bounded by admin-authored template count, recorded in §4.2-5 |
| 8 | A second Railway instance ships without the shared generation | low today, real later | Instance B serves stale everything; spurious `409`s | §4.2-10: recorded constraint + named migration path in the README, pointed at from `API_GUIDELINES.md`, precedent-matched to the rate limiter's per-instance paragraph |
| 9 | The `404` template-detail arm accidentally emits validator headers, or the `304` arm emits a body | low | Contract incoherence; Kestrel rejects content on `304` | T302 states the `404` arm explicitly; `StatusCodeResult` writes no body by contract; smoke tier re-asserts the `404` |
| 10 | READMEs drift — the next agent reads "No caching" or an unamended gotcha and designs against a dead fact | medium | Wrong plans downstream | §5.4 is a per-file ledger with named lines; T102/T602 sweep brackets the slice |

**Rollback:** every phase is one green commit. Reverting Phase 4 returns the frontend to
unconditional reads against a backend that still answers them identically (`no-store` + full
`200`s). Reverting Phases 3–4 removes the wire change entirely; reverting Phase 2 as well is
a clean return to `dev` behavior. No migration, no schema, no stored client state — the worst
stranded artifact is a browser-held validator string that row 3/8 semantics already make
harmless.

## 9. Obligations checklist (all must be true at close)

- [ ] All 21 write routes of §5.1 flow through an invalidating decorator; the bypass census is re-run at close and still finds no 22nd path.
- [ ] The bump is in `finally`, after the inner writer returns, and no handler or controller was edited to achieve it.
- [ ] The tree entry is one indivisible entry; nothing keys a cache on section, scope, or archive state.
- [ ] The three reads answer §6a exactly — including row 4 (fail-open) and row 7 (just-wrote `200`), both observed in T503, and the `304` path runs zero DB queries.
- [ ] `ETag` + `Cache-Control: no-store` on every `200` and `304` from the three reads; the `404` detail arm carries neither.
- [ ] `.WithExposedHeaders("ETag")` is in the CORS policy and T503 recorded a non-null stored validator.
- [ ] The relations read is byte-identical to `dev`; relations writes bump the tree generation.
- [ ] The facades hold validator-beside-value as one unit; `304` keeps both, sets no error, flashes no skeleton; facade specs passed **unedited**.
- [ ] The route-entry `load()` is still unconditional; no TTL exists anywhere on either end.
- [ ] `AbwabTreeDto.Version`, `GetSnapshotVersionAsync`, `xmin` handling, and every `Caching/Quran` file are untouched.
- [ ] `SmokeRouteCatalog` is unedited; the smoke tier ran at T303 and T501 with `Tests.Smoke.Data` RAN/SKIPPED stated each time.
- [ ] The six §5.4 files are amended, including the single-instance constraint + migration path in `Persistence/Reads/Abwab/README.md` and the distinguish-don't-weaken `version` gotcha.
- [ ] `TESTING_DEBT.md` carries `ux-slice-i` rows I1–I4.
- [ ] All gates green with T101 counts unchanged; the `200`-vs-`304` byte and timing measurements are in `evidence.md`.
- [ ] Root `CLAUDE.md` back to `None`; **no planning folder deleted, swept, or repointed**; no package installed; no `dev → main` merge; the close-out note names the cleanup pass as the next work item.

## 10. Execution note

Phase 2 lands the entire correctness core — generations, eviction ordering, cached reads —
while the wire stays byte-identical, which is why it is the recorded split seam: it can be
reviewed and even shipped alone, and reverting it later requires no client thought. Phase 3 is
the single commit where response semantics move, and it moves them only for clients that opt
in with a header no shipped client sends yet. Phase 4 is the opt-in. This ordering means at no
commit boundary does any deployed or local client observe a behavior change it did not itself
request — the same "nothing changed until the one commit where it is the point" discipline
Slice H used for its Phase 3.

**Branch:** off `dev`, PR into `dev`. Never `main`.

**After close:** the abwab UX/UI overhaul series (Slices A–I) is complete. The deferred
planning-artifact cleanup pass — the root `CLAUDE.md` lifecycle rule, the N-2 buffer
arithmetic across nine slice folders plus the audit — is the next piece of work, commissioned
separately. This plan schedules none of it.

## 11. Stop conditions

Stop and ask if any of these is true:

1. **The task count exceeds 30** — it is 16; if execution grows it past 30, bring the §0 seam
   (after Phase 2). (Commission-named.)
2. **A write path is found that cannot reach the invalidator without restructuring** — the
   census found all 21 routes flowing through the five writer interfaces and no bypass; a
   22nd path or a direct-`DbSet` mutation discovered during execution is a stop, not a
   workaround. (Commission-named.)
3. **Angular's `HttpClient` cannot surface the `304` distinguishably without a new
   interceptor** — checked and clear (`module.mjs:1192,1884-1908`: error channel,
   `err.status === 304`); if a framework update or the `devLatencyInterceptor` is found to
   swallow or reshape the status in practice, stop. (Commission-named.)
4. **`ApiResponse<T>` cannot accommodate the bodiless `304` without a change that reaches
   non-abwab routes** — checked and clear (§5.3: the envelope type is untouched; the rule is
   scoped to conditional GETs); if execution finds shared serialization/middleware that
   forces an envelope onto a `304`, stop. (Commission-named.)
5. **T101's baseline is not green.**
6. **T503 observes any §6a row wrong** — above all a `304` on the just-wrote refetch or a
   stale body after a write. That is a design failure, not a code bug; do not patch it
   locally.

**Flagged, not a stop — the user should see these before execution:** (a) the relations read
deliberately gains nothing (§4.2-9) even though the audit's item heading says "tree +
templates + archive… on both ends" — archive is covered via the tree entry and relations was
never one of the three routes; (b) `Cache-Control: no-store` means the browser's own cache
never accelerates these reads — deliberate (§4.2-8), so the only caches are the two this
slice builds; (c) a template-node edit invalidates every cached template detail, not just the
edited one — one counter, admin-scale trade (§6b).
