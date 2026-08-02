# Slice I — Evidence

Plan: `docs/feature-ux-slice-i/plan.md`. Branch: `ux-slice-i`, off `dev` @ `5adc9bc0`
(clean). Plan committed to the branch as `65b664b4`, not to `dev`.

## T101 — Baseline (dev @ `5adc9bc0`, clean)

Commands taken verbatim from `TESTING_STRATEGY.md` §5 `:341-358` / §6 `:401-414`.

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Backend/QuranDashboard.sln` | Succeeded. 0 warnings, 0 errors. 36.87 s. |
| No-pipeline regression | `dotnet test … --no-build --filter "…!~ the ten pipeline namespaces …&FullyQualifiedName!~QuranDashboard.Tests.Smoke."` | **1,086 passed**, 0 failed, 0 skipped. 22 s. Matches the strategy's expected count exactly. |
| `Tests.Api` | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Api"` | **60 passed**, 0 failed, 0 skipped. 14 s. |
| Route-smoke tier | `dotnet test … --filter "FullyQualifiedName~QuranDashboard.Tests.Smoke."` | **140 passed**, 0 failed, **0 skipped**. 1 m 2 s. |
| **`Tests.Smoke.Data`** | — | **RAN.** The data tier skips per-test via `SmokeDumpFactAttribute`/`SmokeDumpTheoryAttribute` when the canonical dump is absent (`Smoke/Data/SmokeDumpGate.cs`); 0 skipped of 140 means the dump was present and every data case executed. |
| Frontend tests | `npm test` | **193 files, 2,343 tests** passed, 0 failed. 218.70 s. |
| Frontend build | `npm run build` | Succeeded, 18.264 s. Three pre-existing budget warnings (initial bundle +71.29 kB over the 500 kB budget; two mushaf SCSS files over their 4 kB budgets) — carried forward, none introduced here. |

**Frontend count discrepancy, recorded not rounded off:** `TESTING_STRATEGY.md` §6 `:410-411`
still says 191 files / 2,161 tests. The tree measures **193 / 2,343**, which is exactly what
Slice H's own T101 measured (`docs/feature-ux-slice-h/evidence.md`) — the strategy's frontend
line went stale before this slice and no slice has repaid it. This slice writes no spec, so
**193 / 2,343 is the number T502 must reproduce unchanged**; the plan's "191 / 2,161" is
inherited from the same stale line and is not the gate.

### The wire measurement (the number Phase 5 compares against)

Kestrel on the `http` launch profile (`http://localhost:5014`), local dataset. `curl -w`:

| Request | Status | Body bytes | Time |
|---|---|---|---|
| `GET /api/abwab/tree` (cold, first query of the process) | `200` | **140,187** | 3.434 s |
| `GET /api/abwab/tree` (repeat 1 / 2 / 3) | `200` | 140,187 | 0.135 s / 0.051 s / 0.032 s |
| `GET /api/abwab/templates` | `200` | 226 | 0.082 s |

Response headers on every one of them: `Content-Type`, `Date`, `Server`, `Transfer-Encoding`
— **no `ETag`, no `Cache-Control`**, confirming on the wire the plan's grep-based claim that
the backend has zero HTTP caching today. Each repeat re-queries the database; the falling
times are warm-EF/warm-Postgres effects, not caching.

**Baseline verdict: green.** Stop condition 5 does not fire. T501/T502 must reproduce
1,086 / 60 / 140 / 193 files / 2,343 tests unchanged — this slice writes no test.

## T102 — Sweep for recorded statements this slice falsifies

`grep -rn` across `Backend/`, `Frontend/quran-dashboard-ui/src/`, `docs/`,
`Backend/.architecture/`, and `Frontend/quran-dashboard-ui/.architecture/` for:
`No caching`, `no invalidation`, `ETag`, `If-None-Match`, `304`, `Not Modified`,
`unconditional`, `diagnostics only`, `diagnostics-only`. (`docs/abwab-ux-audit.md` and the
closed slices' own plans are historical record and excluded from the amendment obligation.)

Result — the plan-time prediction plus **one new hit**:

| Hit | Status |
|---|---|
| `Persistence/Reads/Abwab/README.md:106-108` — "**No caching.** … no invalidation story yet …" | In the §5.4 ledger. Replaced at T601. |
| `Persistence/Reads/Abwab/README.md:88-92` — `Version` is diagnostics-only, ignores relations | In the ledger. Amended (one clause) at T601. |
| `features/abwab/README.md:507-508` — the `version`-is-diagnostics-only gotcha | In the ledger. Amended to distinguish, not weakened, at T601. |
| **`features/abwab/README.md:285-296` — "`modal` … is not part of any cache key, restore identity, history identity or **ETag** … This is the one row of this table a future caching design must **not** pick up"** | **New — not in the plan-time sweep.** It is a *constraint this slice must honor*, not a falsified statement: the tree validator is a server-side generation counter keyed on nothing from the URL, the snapshot read stays one unparameterized root-scoped tree GET, and the relations read stays uncached (§4.2-9). All three clauses of the paragraph remain literally true. Folded into the ledger as a **confirming clause** on the `features/abwab/README.md` amendment (T601), so the next reader sees the constraint was met rather than merely unbroken by accident. |
| `models/abwab.models.ts:190` — "Diagnostics only — never used for conflict detection" | Still true; the validator consumes no DTO field. No edit. |
| `Domain/Abwab/AbwabDoorRelation.cs:35` — relation `Version` diagnostics-only | Still true. No edit (do-not-touch list). |
| `LOGGING_GUIDELINES.md:17`, enriched-morphology test string, controller "retag" comments | Unrelated senses of the search terms. No edit. |

Zero hits for `ETag` / `If-None-Match` / `304` / `Not Modified` in **code** on either end,
confirming the plan's "NEW PATTERN on both ends" premise at execution time.

## Phase 2 — Backend cache + invalidation, no HTTP change

| Gate | Result |
|---|---|
| `dotnet build` after T202 (writer decorators) | Succeeded, 0 warnings, 0 errors. |
| No-pipeline after T202 | 1,086 passed, 0 failed, 0 skipped. 19 s — **unchanged from T101**. |
| `dotnet build` after T203 (cached readers) | Succeeded, 0 warnings, 0 errors. |
| No-pipeline after T203 | 1,086 passed. 19 s. |
| `Tests.Api` after T203 | 60 passed. 11 s. |

**The singleton-identity check (the failure §7's gates cannot catch).** `AbwabCacheGeneration`
is registered once as a concrete singleton with both interfaces forwarding to it
(`AbwabDependencyInjection.cs`). Registering `IAbwabCacheInvalidator` and `IAbwabCacheValidators`
separately against the type would build **two** counters — writers bumping one, readers/controllers
reading the other — and every gate above would still be green while every client was served stale
data forever. Verified against the real composed DI graph rather than by reading the registration:

Kestrel on the `http` profile, EF's `Executed DbCommand` lines counted in the process log:

| Step | Response | Cumulative `Executed DbCommand` |
|---|---|---|
| `GET /api/abwab/tree` (first) | `200`, 140,187 B, 0.380 s | 8 |
| `GET /api/abwab/tree` (repeat) | `200`, 140,187 B, **0.015 s** | **8 — zero new queries: served from `IMemoryCache`** |
| `POST /api/abwab/sections` (probe section) | `201` | 10 |
| `GET /api/abwab/tree` (after the write) | `200`, **140,283 B** — the new section is present | 17 — **reloaded, so the write's bump reached the reader's stamp** |

The probe section was then deleted (`204`) and the tree returned to 140,188 B — the one byte of
difference from the baseline is the `xmin`-derived `version` digits of the rows the delete's
resequence touched, not a structural change. No other data was written.

This is the whole Phase 2 correctness core, and **nothing on the wire changed**: same statuses,
same envelopes, no `ETag`, no `Cache-Control`. It is the plan's recorded split seam — revertable
without any client noticing.

## Phase 3 — The conditional HTTP surface

| Gate | Result |
|---|---|
| `dotnet build` | Succeeded, 0 warnings, 0 errors. |
| `Tests.Api` | 60 passed, 0 failed, 0 skipped. 14 s — unchanged from T101. |
| **Route-smoke tier** | **140 passed**, 0 failed, **0 skipped**. 48 s. |
| **`Tests.Smoke.Data`** | **RAN** (0 skipped of 140 — the dump was present, same gate as T101). |
| `SmokeRouteCatalog` | **Unedited** — `git status` shows no test file in the Phase 3 diff. No route was added and no verb/template/constraint changed, so no entry was owed (DRIFT-6), and the three catalogued read entries still pass because the smoke client sends no `If-None-Match`. |

### The §6a matrix, observed on the wire (`curl`, local dataset)

The tree validator at the time of the run was `"abwab-tree-8292fd82-0"` — boot id `8292fd82`,
generation `0`.

| §6a row | Request | Observed |
|---|---|---|
| 1 / 5 | `GET /api/abwab/tree`, no header | `200`, 140,188 B, `ETag: "abwab-tree-8292fd82-0"`, `Cache-Control: no-store` |
| 2 | same, `If-None-Match:` current validator | **`304`, 0 bytes**, `ETag` + `Cache-Control: no-store` present |
| 2 (cost) | the `304` above, EF `Executed DbCommand` count before/after | **8 → 8 — zero database queries on the `304` path** |
| 4 | `If-None-Match: "garbage"` | `200`, full body — fail-open |
| 4 | `If-None-Match: *` | `200`, full body — the deliberate RFC deviation |
| 4 (list form) | `If-None-Match: "nope", "abwab-tree-8292fd82-0"` | `304` — exact member match inside a list |
| resource scoping | templates-list validator sent to `GET /api/abwab/tree` | `200` — a list validator cannot `304` another resource |
| resource scoping | templates-list validator sent to `GET /api/abwab/templates/3` | `200`, 965 B |
| 1 / 2 (templates) | list: `200` + `ETag: "abwab-templates-8292fd82-0"`; then matching → `304`, 0 B. Detail id 3: `200` + `ETag: "abwab-template-3-8292fd82-0"`; then matching → `304`, 0 B |
| `404` arm | `GET /api/abwab/templates/999999` | `404` with **no `ETag` and no `Cache-Control`** — an absence has no representation to validate |
| **7 — the just-wrote client** | captured `"abwab-tree-8292fd82-0"`, `POST /api/abwab/sections` (`201`), then re-GET **with the pre-write validator** | **`200`, 140,271 B, `ETag: "abwab-tree-8292fd82-1"`** — the trap the design exists to prevent did not fire |
| generation independence | captured the tree validator, `POST /api/abwab/templates` (`201`), then re-GET the tree with it | `304` — a templates write leaves the tree generation alone |

Both probe rows (`__probe-2` section, `__probe-template`) were deleted afterwards (`204` each);
no probe data remains.

**T303 (CORS).** `.WithExposedHeaders(HeaderNames.ETag)` added to the `AngularDev` policy.
`curl` is same-origin-blind, so this is asserted where it can actually fail — T503's browser
walk records whether the facade stored a non-null validator.

## Phase 4 — The frontend conditional requests

| Gate | Result |
|---|---|
| `npx tsc --noEmit` | Clean. |
| `npm test` (full) | **193 files, 2,343 tests passed, 0 failed** — identical to T101. |
| `npm run build` | Succeeded, 18.961 s. The same three pre-existing budget warnings; initial bundle 573.91 kB vs T101's 571.29 kB (**+2.62 kB**, the conditional-request plumbing). |

### FINDING — the plan's "facade specs pass unedited" gate rests on a premise the specs do not match

T403's gate and debt row I3 both assume the abwab specs drive the facades through
`HttpTestingController` (I3 names `flush(null, { status: 304, … })`). They do not: every one of
them stubs the **api object** with `of(envelope)`. Once `getTree`/`getTemplates`/`getTemplate`
observe the response — §4.2-11, which the plan locks — no stub of that shape can satisfy them, so
"specs unedited" was unreachable by construction, not by an implementation choice. Recorded rather
than absorbed.

What was actually changed, and how it was kept mechanical:

- Five spec files had their **stub construction** wrapped at the provider boundary —
  `of(envelope)` → `of(new HttpResponse({ body: envelope }))`, done once per `setup()` helper
  rather than at each of the ~30 stub literals: `abwab-snapshot.facade.spec.ts`,
  `abwab-templates.facade.spec.ts`, `abwab-write.controller.spec.ts`,
  `abwab-sections.controller.spec.ts`, `abwab-page.component.spec.ts`.
- The wrapped responses are **headerless on purpose**. A fake `ETag` would silently start
  exercising the validator-storage line without asserting it, which is precisely the gap debt row
  I3 owns; a headerless response keeps every existing test as unconditional as it was.
- **Zero assertions moved.** `git diff -U0 -- '*.spec.ts' | grep -iE 'expect|toBe|toEqual|toHaveBeenCalled'`
  returns exactly one line, in `abwab.api.spec.ts`: `resolves.toEqual(response)` →
  `resolves.toMatchObject({ body: response })`. That file uses `HttpTestingController` and asserts
  the api's own return value, so the change is the api's new return type stated directly — not a
  weakened expectation.
- Totals are unchanged (193 / 2,343): no spec was added, deleted, or split.

Debt row I3's wording is corrected in the same change (T602) to name the harness these specs
actually use — stubbing the api with `throwError(() => new HttpErrorResponse({ status: 304 }))` —
because a row naming a harness the specs do not use is not payable.

### Two scope deviations from §2, both deliberate

- **`data-access/conditional-request.ts` is a new file** the plan's scope list does not name. It
  holds one function: build the `If-None-Match` header only when a validator is held. Both api
  services need it, and inlining it twice would be the duplication the alternative avoids.
- **§2 places all three reads in `abwab.api.ts`**; on `dev` the two templates reads live in
  `abwab-templates.api.ts` (its own file since the templates feature landed). Both files were
  changed identically.

### One correctness fix the plan did not anticipate

`AbwabTemplatesFacade.clearSelection()` nulls `rawSelected`. Keeping the id-keyed validator across
that would let a re-select of the same template be answered `304` with nothing left to render —
the page would sit on «اختر قالبًا» forever. The validator is therefore dropped with the value it
validates, which is the plan's own one-unit rule (§4.2-12) applied to the one path where the value
is cleared rather than replaced.

## Phase 5 — Verification

### T501 — Backend gates (final)

| Gate | Result | vs T101 |
|---|---|---|
| `dotnet build Backend/QuranDashboard.sln` | Succeeded, 0 warnings, 0 errors | same |
| No-pipeline regression | 1,086 passed, 0 failed, 0 skipped, 21 s | **unchanged** |
| `Tests.Api` | 60 passed, 13 s | **unchanged** |
| Route-smoke tier | 140 passed, 0 skipped, 52 s | **unchanged** |
| **`Tests.Smoke.Data`** | **RAN** (0 skipped of 140) | same as T101 |

### T502 — Frontend gates

`npm test`: **193 files / 2,343 tests passed, 0 failed** — identical to T101. `npm run build`
succeeded with the three pre-existing budget warnings and no new ones.

### T503 — The browser walk

Setup: Kestrel on the `https` profile started with the frontend's own mkcert PEM
(`Frontend/quran-dashboard-ui/localhost.pem` / `-key.pem`) — without it every API call reads as a
backend failure — plus `npm run start:https` on `https://localhost:4200`. Statuses below are read
from the browser's network log; where that log proved unreliable (see the artifact note) the
backend's own handler log is used instead, and which source was used is stated.

| §6a row / walk step | Observed |
|---|---|
| First `/abwab` load | `GET /api/abwab/tree` → `200`, page renders 6 root doors, 35 total |
| **DRIFT-5 acceptance — the validator was actually readable** | Re-entering `/abwab` in-app issued `OPTIONS /api/abwab/tree` → `204` followed by `GET` → **`304`**. A `304` is only reachable if the facade read the `ETag` off a cross-origin response and sent it back, so `WithExposedHeaders("ETag")` is doing its job. The preflight is itself the proof that `If-None-Match` was on the request |
| Row 2 — the `304` revisit | Content rendered immediately: `treeitem` count 6, **no skeleton** (`document.querySelector('[class*="skeleton"]')` → null), no error banner |
| **Row 7 — the just-wrote client** | Renamed door 337 «الجهاد» → «الجهاد المعدل» through the app's edit modal: `PUT /api/abwab/doors/337` → `200`, then the funnel's refetch `OPTIONS` → `204`, `GET /api/abwab/tree` → **`200`** (not `304`), and the new name rendered. Renamed back the same way; the tree re-rendered «الجهاد» |
| Archive view — locked decision 5 live | Toggling الأرشيف moved the URL to `?archive=1` and issued **zero** API requests (`performance.getEntriesByType('resource')` filtered to `/api/abwab` → length 0). The archive really is a partition of the cached snapshot |
| Templates list | First `/abwab/templates` entry → `GET /api/abwab/templates` `200`; re-entry in-app → `OPTIONS` `204` + `GET` **`304`** |
| Template detail | Selecting «الثمرات» → `GET /api/abwab/templates/3` → `200` |
| **Generation independence** | Created a template through the UI: `POST /api/abwab/templates` `201` → templates list refetch **`200`** and detail `200`. Immediately navigating to `/abwab` gave `GET /api/abwab/tree` → **`304`** — a templates write does not touch the tree generation. The temporary template was then deleted through the UI (`DELETE` `204`); the list is back to its three templates |
| **Row 13 — relations evict the tree** | Added a relation on door 336 through the relations modal: `POST /api/abwab/doors/336/relations` → `201`, then `OPTIONS` `204` + `GET /api/abwab/tree` → **`200`** (evicted). The relation was deleted again (`DELETE /api/abwab/relations/48` → `204`, chip count back to 3). The relations read itself was a plain `200` **with no preflight** both times — unconditional by design (§4.2-9) |
| Row 4 — malformed headers | `curl` (Phase 3 table): `"garbage"` → `200`, `*` → `200`, and a list containing the current validator → `304` |
| **Row 8 — restart** | Backend restarted; the new process's validator was `"abwab-tree-70b3e363-0"` against the previous process's `"abwab-tree-26bbe37b-0"` — boot ids differ, so no pre-restart validator can match. Authoritative count from the backend's own log after the restart: **2** `Completed Abwab GetAbwabTree` lines — one for the `curl` that read the new `ETag`, one for the browser's single post-restart refetch. Every later in-app revisit produced **no handler line at all** and the browser log showed `304`. Exactly one refetch per client per resource, then `304`s resume |
| Error path — first-load failure | With the backend stopped, a fresh `/abwab` load showed the error banner «تعذر تحميل شجرة الأبواب. حاول مرة أخرى.» and 0 tree items |
| Error path — refetch failure | With the backend stopped and a snapshot already held, an in-app revisit **kept all 6 root doors and 35 doors on screen and set no error banner**. This is `dev` behavior, not a change: the banner is gated on `facade.errorMessage() && !facade.snapshot()` (`abwab-page.component.html:64,71,105`), so a failed *refresh* over live content has always been silent. Recorded because the plan's T503 line expects a banner here; the plan describes the first-load case |
| Recovery | Backend restarted; the next in-app revisit rendered content again with no error, then `304`s resumed |
| A `304` never presents as an error | Every observed `304` left the error banner absent and the content in place — the two states are visibly distinct (banner + empty vs no banner + content) |

**Browser-log artifact, stated so nobody reads it as a finding.** The extension's network log
reported `503` for several `/api/abwab/tree` entries whose requests demonstrably succeeded or were
cancelled — they cluster around (a) the facade's own `pendingRequest?.unsubscribe()` cancelling an
in-flight request and (b) the first request issued over a keep-alive connection to a process that
had just been restarted. The backend log contains **no `503` and no error entry** for any of them,
and `curl` against the same endpoint at the same moments returned `200`. Where a status mattered,
it was taken from the backend's handler log rather than from the extension.

### The measurement

Same process, warm, `curl` against `https://localhost:5015`:

| Request | Body bytes | Time |
|---|---|---|
| `GET /api/abwab/tree` — `200`, three runs | 140,189 | 0.0137 s / 0.0143 s / 0.0139 s |
| `GET /api/abwab/tree` — `304` (matching `If-None-Match`), three runs | **0** | 0.0082 s / 0.0083 s / 0.0079 s |
| T101 baseline (`dev`, no cache, no ETag) — cold / warm | 140,187 | 3.434 s cold, 0.032 s warm |

So a revalidation costs **zero body bytes and ~0.008 s** against ~140 kB and ~0.014 s for a cached
`200` — and against the 3.4 s cold read the baseline measured. The `304` path was separately shown
to run **zero database queries** (Phase 3's `Executed DbCommand` count).

## Phase 6 — Docs true again

### T601 — The amendment pass

| File | What changed |
|---|---|
| `Backend/.architecture/API_GUIDELINES.md` | §4 gains `304 Not Modified` as the second sanctioned bodiless status. §5 gains a **Conditional GETs** subsection: `ETag` + `Cache-Control: no-store` on every `200`, `304` bodiless with the same headers and **no query**, opaque server-generation validators, exact ordinal member match with fail-open on malformed and `*`, no validator headers on a `404`, and a pointer — not a restatement — to the reads README for the single-instance constraint. |
| `.architecture/API_INTEGRATION_GUIDELINES.md` | "HTTP Errors vs Backend Failure Responses" gains its third category: a `304` on the error channel is not a failure; handle it before the generic branch; validator lives beside the value as one unit and is dropped with it; per-resource validators are id-keyed; `observe: 'response'` is the sanctioned shape; and `ETag` is unreadable cross-origin without `WithExposedHeaders` — verifiable only in a browser. |
| `Persistence/Reads/Abwab/README.md` | The "No caching" bullet is replaced by a **Caching and invalidation** section: the two decorators, the entry inventory, the generation stamp and capture-before-load rule, why `CacheLoadGate` is not reused, why a template-detail miss is never cached, the archive-is-not-a-resource rule, the relations exclusion, and the **single-instance constraint with its migration path**. The `Version`-ignores-relations bullet gains the clause that the cache validator ignores `Version` right back. |
| `Persistence/Writes/Abwab/README.md` | Gains the eviction obligation as a convention: every writer interface is DI-wrapped by an invalidating decorator; the bump is in `finally` (why); it runs after the inner commit and before the handler resumes (why that satisfies the ordering rule by construction); and **a sixth writer or a new interface method must go through its decorator** — the compile error is the guard, the `finally` bump is the review line. |
| `features/abwab/README.md` | The `version` gotcha is amended to **distinguish, not weaken**: `version` describes, `xmin` detects conflicts, the `ETag` validates a representation. New gotchas: validator-beside-value as one unit (including the id-keyed selected validator and its `clearSelection` drop), `304` = keep current value and never a banner, the route-entry `load()` stays unconditional and now costs a `304`, and the archive view is a partition of the cached snapshot rather than a cacheable resource. The «`modal` enters no `ETag`» paragraph gains the confirming clause from T102. |
| `docs/TESTING_DEBT.md` | New `ux-slice-i` section with rows I1–I4 (T602). |
| root `CLAUDE.md` | Active Spec Kit Feature set at T101, back to `None` at T602. |

### T602 — Debt, sweep, close-out

- `docs/TESTING_DEBT.md` gains the `ux-slice-i` section, rows **I1–I4**. Row I3's wording names the
  harness these specs actually use — stubbing the api with `throwError(() => new HttpErrorResponse({ status: 304 }))` —
  rather than the plan's `HttpTestingController.flush`, which they do not use; a row naming the wrong
  harness is not payable.
- **T102's sweep re-run.** `No caching` and `no invalidation` now match **only inside the amended
  text itself** (`Reads/Abwab/README.md:108-109`, the sentence recording that the old rule stood
  while there was no invalidation story). Remaining `diagnostics only` hits are all still-true
  statements: the amended feature-README gotcha, `abwab.models.ts:190`, `AbwabDoorRelation.cs:35`,
  and unrelated logging/test uses. No unamended falsified statement remains.
- **Bypass census re-run at close.** Grepping every abwab `DbSet` name across `Backend/` outside
  `Persistence/{Reads,Writes}/Abwab/`, migrations, EF configurations and tests returns **nothing** —
  no 22nd write path, so all 21 routes still reach an invalidator through one of the five decorated
  writer seams (5 `Invalidating*` registrations in `AbwabDependencyInjection`).
- **No planning folder was deleted, swept, or repointed**, per the standing decision.

### Close-out

The abwab UX/UI overhaul series (Slices A–I) is complete with this slice. The deferred
**planning-artifact cleanup pass** — the root `CLAUDE.md` lifecycle rule and the N-2 buffer
arithmetic across the nine slice folders plus the audit — is the next piece of work and is
commissioned separately. This slice schedules none of it.

## Close-out — the branch diff against §2, and §9 item by item

### The whole diff, reconciled

`git diff dev...ux-slice-i --stat` — 34 files, +2,024 / −56. Every entry is either in §2's in-scope
list or one of the deviations already recorded above:

- **In scope, backend:** `API_GUIDELINES.md`; `Api/Common/ConditionalGet.cs` (new — the shared
  header comparison the two controllers use); `AbwabTreeController.cs`, `AbwabTemplatesController.cs`,
  `ServiceCollectionExtensions.cs`; `Application.Abstractions/Abwab/IAbwabCacheInvalidator.cs` +
  `IAbwabCacheValidators.cs`; the seven `Infrastructure/Caching/Abwab/` files;
  `AbwabDependencyInjection.cs`; both abwab persistence READMEs.
- **In scope, frontend:** `API_INTEGRATION_GUIDELINES.md`, `features/abwab/README.md`,
  `abwab.api.ts`, `abwab-snapshot.facade.ts`, `abwab-templates.facade.ts`.
- **Recorded deviations:** `data-access/conditional-request.ts` (new one-function file) and
  `abwab-templates.api.ts` (where two of the three reads actually live).
- **The spec-shape finding:** six `*.spec.ts` files, stub construction only.
- **Docs:** `docs/TESTING_DEBT.md`, `docs/feature-ux-slice-i/{plan,evidence}.md`.
- **Root `CLAUDE.md` is absent from the diff** — set at T101 and cleared at T602, so it nets to zero
  against `dev`, which is exactly the intended end state.

**What is absent is the point:** no `SmokeRouteCatalog.cs`, no `Caching/Quran/**`, no
`CacheLoadGate.cs`, no `AbwabTreeDto.cs`, no `EfAbwabTreeReader.cs` / `EfAbwabTemplatesReader.cs`,
no `AbwabDoorRelationsController.cs` / `EfAbwabRelationsReader.cs`, and no writer body — the Ef
writers are untouched; they are only re-registered. Nothing outside the plan's scope moved.

### §9 obligations checklist

| # | Obligation | Verdict | Evidenced by |
|---|---|---|---|
| 1 | All 21 write routes flow through an invalidating decorator; bypass census re-run finds no 22nd path | ✅ | 5 `Invalidating*` registrations in `AbwabDependencyInjection`; T602 census returns nothing outside the seams |
| 2 | The bump is in `finally`, after the inner writer returns, with no handler or controller edited | ✅ | The five decorator files; no handler is in the diff |
| 3 | The tree entry is one indivisible entry; nothing keys a cache on section, scope, or archive state | ✅ | `CachedAbwabTreeReader` — a single fixed key `abwab:tree`; archive toggle issued zero requests in T503 |
| 4 | The three reads answer §6a exactly, including row 4 (fail-open) and row 7 (just-wrote `200`), and the `304` path runs zero DB queries | ✅ | Phase 3 curl matrix (all eight rows) + T503's browser rows; `Executed DbCommand` unchanged across a `304` |
| 5 | `ETag` + `Cache-Control: no-store` on every `200` and `304` from the three reads; the `404` detail arm carries neither | ✅ (tree path measured; the rest by construction) | Measured on the tree's `200` and `304` and on the `404` arm (no headers). Both headers are written by the single `ConditionalGet.SetValidatorHeaders` call every arm uses, so the templates arms cannot carry one without the other |
| 6 | `.WithExposedHeaders("ETag")` is in the CORS policy and T503 recorded a readable validator | ✅ | `ServiceCollectionExtensions.cs`; the browser's `OPTIONS` + `304` pair, unreachable without a stored validator |
| 7 | The relations read is byte-identical to `dev`; relations writes bump the tree generation | ✅ | Neither the relations controller nor its reader appears in the diff; `InvalidatingAbwabRelationsWriter` bumps the tree, walked live (relation add → tree `200`) |
| 8 | Facades hold validator-beside-value as one unit; `304` keeps both, sets no error, flashes no skeleton; facade specs passed **unedited** | ⚠️ partly — see the Phase 4 finding | Behavior ✅ (both facades + T503). "Specs unedited" ❌ **and unreachable by construction**: the specs stub the api object, so `observe: 'response'` forces a stub-shape change. Stub construction only, zero assertions moved |
| 9 | The route-entry `load()` is still unconditional; no TTL on either end | ✅ | `abwab-page.component.ts` untouched; no `MemoryCacheEntryOptions` anywhere in `Caching/Abwab/` |
| 10 | `AbwabTreeDto.Version`, `GetSnapshotVersionAsync`, `xmin` handling and every `Caching/Quran` file untouched | ✅ | None of them appear in the branch diff |
| 11 | `SmokeRouteCatalog` unedited; the smoke tier ran at T303 and T501 with `Tests.Smoke.Data` stated each time | ✅ | Absent from the branch diff; both runs 140 passed / 0 skipped, **RAN** stated both times |
| 12 | The six §5.4 files amended, including the single-instance constraint + migration path and the distinguish-don't-weaken `version` gotcha | ✅ | T601 table |
| 13 | `TESTING_DEBT.md` carries `ux-slice-i` rows I1–I4 | ✅ | T602 |
| 14 | All gates green with T101 counts unchanged; the `200`-vs-`304` byte and timing measurements recorded | ✅ | T501 / T502 tables; the measurement table |
| 15 | Root `CLAUDE.md` back to `None`; no planning folder deleted, swept, or repointed; no package installed; no `dev → main` merge; the close-out names the cleanup pass | ✅ | T602 + the diff reconciliation above; no package manifest is in the diff |

One item is not a clean ✅ (#8's "specs unedited"), and it is the plan's own premise that failed
rather than the implementation — recorded in full under Phase 4.
