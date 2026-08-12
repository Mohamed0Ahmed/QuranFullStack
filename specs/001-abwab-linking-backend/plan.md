# Implementation Plan: Abwab Ayah Linking — Real Persistence, Preflight, and Confirmation

**Branch**: `feature/abwab-linking-frontend-prototype` (kept by user decision; the feature slug is `001-abwab-linking-backend`) | **Date**: 2026-08-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-abwab-linking-backend/spec.md`

**Execution authority**: The phase-by-phase execution plan is
`docs/abwab-linking-backend-implementation-plan.md` (14 phases, locked decisions D1–D9, verified
repository facts F1–F14, final acceptance matrix §14). This plan.md and its sibling artifacts
(research.md, data-model.md, contracts/, quickstart.md) restate that authority in Spec Kit form
and add the design-level detail an implementer needs; on any conflict, **stop and reconcile — do
not pick silently**.

## Summary

Replace the Frontend V2 linking prototype's browser-only persistence and mock confirmation with a
real Backend: one complete source-resolution boundary for all six source families (with canonical
`quran_words.id` word identity), a dedicated bounded source-result cache, per-user durable
workspace persistence, a read-only preflight classification engine, and one atomic
confirmation/update command with replacement semantics and idempotency — then cut the Frontend
over (resolution adapter + session cache, workspace adapter, preflight + confirm, virtualized
editor + descriptions UI + merged provenance). Backend phases ship dark; the proven V2 UX is not
redesigned.

## Technical Context

**Language/Version**: Backend C# on .NET 10 (`net10.0`); Frontend TypeScript ~5.9 with Angular ^20.3

**Primary Dependencies**: ASP.NET Core (controllers), EF Core 10 with `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.0, `Microsoft.Extensions.Caching.Memory` (dedicated instance); Angular CDK `^20.2.14` (`ScrollingModule` already in use by `shared/ui/data-table`)

**Storage**: PostgreSQL. Twelve new `linking_*` tables in three EF migrations (M1 `AddLinkingWorkspace`, M2 `AddLinkingWorkspaceDescriptions`, M3 `AddLinkingConfirmedState`), all created with `Backend/scripts/add-mig` — never hand-written. `xmin` optimistic concurrency, consistent with Abwab.

**Testing**: **None — the Test Freeze is in force** (`TESTING_CONSTITUTION.md`). No automated test is created or modified. Verification = `dotnet build`, `Backend/scripts/check-api-contract`, `Backend/scripts/check-pending-model`, the frontend gate commands (`check:no-unit-specs`, `typecheck:app`, `build:verify`, plus `check:golden-ui` for template/style changes), manual Swagger/`curl`/browser checks, and safe local `psql` inspection. Two retained *gates* are touched only as re-runs: `SmokeDumpGate` requires re-running `Backend/scripts/create-smoke-dump` after every migration (repo fact F2), and `AbwabSchemaTests` is unaffected by `linking_*` tables (F3).

**Target Platform**: Linux server (Railway production off `main`); browser SPA frontend

**Project Type**: Full-stack monorepo — `Backend/` (.NET, layered: domain / application / infrastructure / api) + `Frontend/quran-dashboard-ui/` (Angular feature folders)

**Performance Goals**: A ~2,000-ayah source resolves in **one** request with a bounded number of database commands (4–5, independent of ayah count — never one query per ayah); a warm repeat resolution issues **zero** SQL; the compact cached form is ≈210 KB per 2,000-ayah source (vs ≈4 MB full DTO); a warm set of ~8 large sources stays in the low tens of MB; the editor renders 2,000+ ayahs with bounded DOM via CDK virtualization

**Constraints**: `LinkingSourceIdentity` must be **byte-identical** to the Frontend's `linkingSourceKey` (JavaScript `encodeURIComponent` escape set — `Uri.EscapeDataString` alone is wrong, see research.md R1); every contract-bearing phase must regenerate and commit `Frontend/quran-dashboard-ui/openapi/swagger.json` + `src/app/core/api/generated/models/**` (F1); the shared `IMemoryCache` must not gain a `SizeLimit` and no existing `Set` call may change (F7); `CacheLoadGate` must not be reused for caller-supplied keys (F8); `AbwabPermissionCatalogue` stays at exactly 19 codes (F9); actor identity flows from `IAuthorizationStateResolver`/`AuthorizationState.UserId`, never `ICurrentUser` (F10); every writer save translates `DbUpdateConcurrencyException` and Postgres `23505` to Abstractions exceptions (F12); DI follows the `Ef*` + `Cached*` decorator registration convention in a `LinkingDependencyInjection.cs` (F13); production-source comments are forbidden by default (`CODING_PRINCIPLES.md` §2); READMEs update in the same change that changes their truth

**Scale/Scope**: Single privileged actor (Owner) and a handful of users; 6,236 ayahs total in the corpus; largest legitimate source ≈2,200 ayahs; caps — 3,000 ayahs per resolution (clarified), 100 prepared sources per workspace, 10 descriptions × 2,000 chars per (source, ayah); 14 implementation phases; ~12 new tables, 4 API boundaries, ~10 new/changed Frontend files plus deletions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` is an unfilled template, so the gates below come from the
repository's actual governing law: the root `CLAUDE.md` kernel, `TESTING_CONSTITUTION.md`,
`CODING_PRINCIPLES.md`, and the golden-UI contract.

| # | Gate | Verdict |
| --- | --- | --- |
| G1 | **Scope discipline** — stay within the locked scope; stop before broadening a phase, contract, or schema boundary | PASS — spec §Out of Scope mirrors the plan's §12 deferred list; phases are boundary-explicit |
| G2 | **Protected `main`** — never modify or commit to `main` | PASS — work continues on `feature/abwab-linking-frontend-prototype` by user decision |
| G3 | **Quran data integrity** — never invent or mutate Quran source data | PASS — all Quran/morphology access is read-only; every FK to `quran_*`/`access_*` is `RESTRICT`; word identity is canonical `quran_words.id` |
| G4 | **Test Freeze** — no automated test created or modified | PASS — Testing Decision `none`; only gate re-runs (smoke dump regeneration per migration, F2); final check `git diff --stat -- Backend/tests Frontend/quran-dashboard-ui/e2e` must be empty |
| G5 | **Contract gate** — swagger + generated models regenerated and committed with every contract change | PASS — named in every contract-bearing phase (2, 3, 5, 6, 8, 9); the single most frequently forgotten step (F1) |
| G6 | **Comment policy & README truth** — comments forbidden by default; READMEs updated in the same change | PASS — plan phases name their README updates; cache READMEs must record the three deliberate divergences (research.md R11) |
| G7 | **Authorization boundary** — Owner-only; permission catalogue untouched | PASS — exactly one `[RequireOwner]` per route; no `linking.*` codes; catalogue stays 19 codes / 5 groups (F9) |

**Post-Phase-1 re-check**: PASS — the design artifacts introduce no new scope, no test files, no
permission codes, no Quran-data writes, and no shared-cache modification.

## Project Structure

### Documentation (this feature)

```text
specs/001-abwab-linking-backend/
├── plan.md              # This file
├── spec.md              # Feature specification (clarified 2026-08-12)
├── research.md          # Phase 0 — consolidated decisions R1–R22
├── data-model.md        # Phase 1 — all 12 tables, 3 migrations, lifecycle rules
├── quickstart.md        # Phase 1 — environment, gates, end-to-end validation walkthrough
├── contracts/
│   ├── source-identity.md        # THE byte-exact identity/key contract + worked examples
│   ├── linking-sources-api.md    # POST /api/linking/sources/resolve
│   ├── linking-workspace-api.md  # the six workspace routes
│   └── linking-operations-api.md # preflight + confirm
├── checklists/requirements.md
└── tasks.md             # Phase 2 — /speckit-tasks output (not created here)
```

### Source Code (repository root)

```text
Backend/
├── domain/QuranDashboard.Domain/Linking/                    # NEW — enums, LinkingSourceDescriptor,
│                                                            #   5 workspace + 6 confirmed entities
├── application/QuranDashboard.Application.Abstractions/Linking/
│   ├── (root)                                               # NEW — LinkingSourceIdentity,
│   │                                                        #   LinkingSourceDescriptorValidation,
│   │                                                        #   LinkingLimits, 5 exception types,
│   │                                                        #   ILinking* reader/writer ports
│   ├── Responses/                                           # NEW — resolution / workspace /
│   │                                                        #   confirmation DTOs
│   └── Preflight/                                           # NEW — operation request + preflight DTOs
├── application/QuranDashboard.Application/Linking/
│   ├── Queries/ResolveLinkingSource/                        # NEW
│   ├── Queries/GetLinkingWorkspace/                         # NEW
│   ├── Queries/PreflightLinkingOperation/                   # NEW
│   ├── Commands/ (workspace: add/remove/reorder/replace-configuration/clear)   # NEW
│   ├── Commands/ConfirmLinkingOperation/                    # NEW
│   └── LinkingOperationClassifier.cs                        # NEW — pure; shared by preflight + confirm
├── infrastructure/QuranDashboard.Infrastructure/
│   ├── Persistence/Reads/Linking/                           # NEW — EfLinkingSourceResolutionReader
│   │                                                        #   (+ .Automatic/.UniqueWord/.WordType/
│   │                                                        #   .ManualMushaf partials),
│   │                                                        #   LinkingAyahHydration,
│   │                                                        #   EfLinkingWorkspaceReader,
│   │                                                        #   EfLinkingConfirmedStateReader, README
│   ├── Persistence/Writes/Linking/                          # NEW — EfLinkingWorkspaceWriter,
│   │                                                        #   EfLinkingConfirmationWriter, README
│   ├── Persistence/Configurations/Linking/                  # NEW — 12 EF configurations
│   ├── Persistence/Migrations/                              # M1, M2, M3 (EF tooling only)
│   ├── Caching/Linking/                                     # NEW — LinkingSourceResolutionCache (own
│   │                                                        #   MemoryCache), CachedLinkingSource-
│   │                                                        #   ResolutionReader, compact value types,
│   │                                                        #   LinkingAyahTextCache, README
│   └── ServiceRegistration → DependencyInjection/           # MODIFY — add LinkingDependencyInjection.cs
└── api/QuranDashboard.Api/
    ├── Controllers/Linking/                                 # NEW — LinkingSourcesController,
    │                                                        #   LinkingWorkspaceController,
    │                                                        #   LinkingOperationsController
    └── Contracts/Linking/                                   # NEW — LinkingSourceDescriptorBody etc.

Frontend/quran-dashboard-ui/src/app/features/linking/
├── data-access/          # ADD http adapters (resolution, workspace, command, preflight);
│                         # DELETE complete-paged-source.loader, the 6 per-family resolvers,
│                         #   manual-mushaf-ayah.reader, mock-linking-command.port,
│                         #   local-storage-linking-workspace.repository
├── state/                # ADD linking-source.cache (≈6-entry cap); MODIFY workspace store,
│                         #   manual-word-editor facade, source-editor facade (pagination out),
│                         #   workflow facade (+preflight step)
├── components/           # ADD linking-preflight-step/; MODIFY linking-ayah-selection (CDK
│                         #   viewport), linking-source-ayah-editor (descriptions UI),
│                         #   linking-ayah-card + direct-link-workflow (merged provenance)
├── models/               # MODIFY — canonical word identity non-nullable, preflight models,
│                         #   workspace/labels updates
└── utils/                # MODIFY linking-merge (merge by canonical id); DELETE
                          #   manual-mushaf-ayah-completeness
```

**Structure Decision**: Full-stack change inside the existing monorepo layout. Backend follows the
established layered convention (Domain entities → Abstractions ports/DTOs → Application handlers →
Infrastructure EF/cache → thin API controllers), Linking mirroring the Abwab area's patterns
exactly (registration, exception translation, `xmin`, `ApiResponse<T>` envelope). Frontend stays
inside `features/linking/` and swaps adapters behind unchanged store/facade public surfaces.

## Execution sequencing (authoritative: docs plan §Phase map, §13)

14 phases in dependency order; Backend 1–9 ship dark, Frontend 10–13 are the cutover, 14 hardens:

| # | Phase | Ships | Gate |
| --- | --- | --- | --- |
| 1 | Shared contracts + `LinkingSourceIdentity` canonicalizer | BE | build + hand-checked identity parity |
| 2 | Resolution: 5 automatic families + canonical word ids | BE+API | `check-api-contract` |
| 3 | Resolution: manual Mushaf (server-side completeness proof) | BE+API | `check-api-contract` |
| 4 | Dedicated source-result cache (decorator) | BE | build + zero-SQL proof |
| 5 | Workspace schema (M1) + persistence + 6 routes | BE+API+DB | contract + `check-pending-model` + smoke dump |
| 6 | Workspace descriptions (M2) | BE+API+DB | contract + `check-pending-model` + smoke dump |
| 7 | Confirmed schema (M3) — schema/mapping only | BE+DB | `check-pending-model` + smoke dump |
| 8 | Preflight engine (pure classifier + read-only endpoint) | BE+API | `check-api-contract` |
| 9 | Atomic confirm/update engine | BE+API | `check-api-contract` |
| 10 | FE: resolution adapter + session cache + canonical identity | FE | 3-command gate |
| 11 | FE: workspace adapter (replaces browser-local storage) | FE | 3-command gate |
| 12 | FE: preflight step + real confirm (mock deleted) | FE | 4-command gate (golden-ui) |
| 13 | FE: CDK virtualized list + descriptions UI + provenance | FE | 4-command gate |
| 14 | Hardening checklist + current-truth READMEs + acceptance matrix | Both | full matrix §14 |

Parallelizable: Phase 7 alongside 2–4; Phases 5–6 independent of Phase 8. Phases 10–13 strictly
sequential. **Do not release Phase 12 without Phase 13** (a real write must not sit behind the old
paginated, description-less editor).

## Complexity Tracking

No constitution-gate violations — no entries required. The two deliberate pattern divergences
(dedicated `MemoryCache` instead of the shared singleton; `Task<T>`-in-entry instead of
`CacheLoadGate`) are mandated by repo facts F7/F8, decided in research.md R11, and must be recorded
in `Caching/Linking/README.md` so they are not "harmonized" away later.
