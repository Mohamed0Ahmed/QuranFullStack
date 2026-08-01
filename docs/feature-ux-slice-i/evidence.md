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
1,086 / 60 / 140 / 191 files / 2,161 tests unchanged — this slice writes no test.

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
