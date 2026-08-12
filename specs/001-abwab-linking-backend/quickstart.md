# Quickstart: Validating Abwab Ayah Linking

Verification guide for this feature. **The Test Freeze is in force** — nothing here creates or
modifies an automated test; every check is a build, a retained gate re-run, a manual probe, or a
safe local database inspection. The authoritative full matrix is
`docs/abwab-linking-backend-implementation-plan.md` §14 (rows A1–F4); spec success criteria
SC-001…SC-011 map onto it.

## Prerequisites

- Local PostgreSQL with the canonical database restored — rebuild instructions:
  `Backend/scripts/README.md`.
- Backend runs locally (see `Backend/README.md`); Swagger UI available for manual probes.
- Frontend: `Frontend/quran-dashboard-ui/` with `npm ci` done; run per its README.
- An authenticated **Owner** session (every Linking route is `[RequireOwner]`).
- `psql` access to the local database for inspection — read-only except where a scenario says
  "deliberate bad INSERT" (constraint probes happen only on the local throwaway database).

## The standing gates (run per phase, as named in plan.md §Execution sequencing)

```bash
# Backend build
cd Backend && dotnet build

# Contract gate — after every contract-bearing phase (2, 3, 5, 6, 8, 9)
Backend/scripts/export-swagger        # then, in Frontend/quran-dashboard-ui:
npm run generate:api                  # commit swagger.json + generated models together (F1)
Backend/scripts/check-api-contract    # proves the committed artifacts match

# Migration ritual — after every migration phase (5, 6, 7)   (F2, research.md R17)
Backend/scripts/add-mig <Name>        # EF tooling only — never hand-written
Backend/scripts/check-pending-model   # no model drift
Backend/scripts/create-smoke-dump     # re-pin SmokeDumpGate to the new head migration

# Frontend gate — after every Frontend phase (10–13), run as independent commands, in order
npm run check:golden-ui               # only when templates/styles changed (12, 13)
npm run check:no-unit-specs
npm run typecheck:app
npm run build:verify
```

## Boundary probes (Swagger/`curl`, local database)

### 1. Resolution — `POST /api/linking/sources/resolve`

| Probe | Expect |
| --- | --- |
| Root with ~10 ayahs; Lemma ~200; Root ~2,000 (record size + wall time for the Phase 14 matrix) | Complete set in one response, Quran-ordered, every word carries `quranWordId`; every ayah of these **automatic** sources has non-empty `matchedQuranWordIds` (SC-001) |
| Same source again with EF command logging at `Information` | **Zero SQL** on the warm repeat (SC-002); two concurrent identical requests → one load |
| Word Type source, then change one scope field | Two distinct cache entries; no cross-serve |
| Manual source incl. a page-spanning ayah (long Baqarah verse) | One complete ordered word list; cross-check one verse against the Mushaf reader UI; `matchedQuranWordIds` may be empty — the ayah is still returned (FR-008) |
| Unknown verse key / unknown dimension id / cap exceeded (lower the configured cap locally to trigger) | Controlled 400/404 naming the offense — never truncation (FR-011) |
| Identity parity: one worked example per family vs `contracts/source-identity.md` table | Byte-identical to the TypeScript output (FR-003) |
| Existing explorer routes before/after | Byte-identical responses (FR-013) |

### 2. Workspace — the six routes

| Probe | Expect |
| --- | --- |
| `GET` as a fresh user, then check `linking_workspaces` row count in `psql` | Empty workspace representation returned; **zero rows inserted** — the row appears only after the first mutation (FR-019) |
| Add 3 sources (different families), configure, reorder, reload | Fully preserved (SC-003); the first add created the workspace row |
| `PUT` configuration with non-empty `selectedWords` on an **automatic** source | 400 — authored words are manual-only (FR-021/FR-023); a manual ayah with zero selected words saves fine |
| Re-add an equivalent descriptor | Label refreshes; order + configuration untouched |
| Two tabs: stale `sourceVersion`/`workspaceVersion` save | 409 with Arabic envelope; recoverable; nothing silently overwritten (FR-027) |
| Second user loads their workspace | Never sees the first user's rows (SC-009) |
| 11th description / 2001-char body / blank body | Refused by writer **and** by the database — the `BETWEEN 1 AND 10` CHECK plus the UNIQUE order-position index together (a deliberate bad INSERT reusing an existing `order_value` must also fail on the UNIQUE) |
| Deliberate incoherent INSERT (automatic source + `manual_link_shape`) in `psql` | Rejected by the CHECK itself (FR-022) |
| `psql` inspection after M1/M2 | Every constraint/index in data-model.md exists; each CHECK exercised once |

### 3. Preflight — `POST /api/linking/operations/preflight`

Hand-build the locked scenario locally (via confirm or `psql` seeding): Door holds «الرحمن» with
ayahs A, B, C.

| Probe | Expect |
| --- | --- |
| Preflight «الرحيم» (A, D, E) | `NEW_SOURCE`; A `OVERLAP_OTHER_SOURCE` with structured `overlappingSources[]` carrying «الرحمن»'s label + kind (not just the technical key); D, E `NEW_AYAH`; counts 3 = 2 + 1; overlap item individually inspectable (SC-004) |
| Identical source + brand-new source | `UNCHANGED` + `NEW_SOURCE`; `isNoOp=false`, not blocked |
| Everything identical / label-only rename | `isNoOp=true`; label-only diff classifies `UNCHANGED` (clarified) |
| Source missing an ayah it had confirmed | That ayah `REMOVE` for this source only |
| Archived Door / marker word / foreign word / zero-ayah source | `INVALID` + `isBlocked` (or 400 for FR-044a) |
| Row counts before/after any preflight | Identical — preflight writes nothing (SC-007) |

### 4. Confirm — `POST /api/linking/operations`

| Probe | Expect |
| --- | --- |
| Confirm **without** a `preflightToken` | Controlled 400 — the token is required (proves the flow ran preflight) yet never trusted: state is fully re-checked inside the write transaction (FR-036/FR-043) |
| Confirm the locked example | «الرحمن» byte-identical (compare `psql` dumps); «الرحيم» added; ayah A has two contributions (SC-004) |
| Re-confirm identical | Nothing written; «لا توجد تغييرات جديدة لتنفيذها» as success |
| Changed source | Same contribution `id`, advanced `xmin`, children replaced — old words `[A,B]` → new `[]` ⇒ no words (never union) |
| Replay same `idempotencyKey` | Prior outcome returned; row counts unchanged (SC-006) |
| Two concurrent confirms of one new source into the same Door | They serialize on the Door row lock: exactly one succeeds; the loser re-classifies against the committed state and gets a 409 (stale preflight or duplicate live contribution) — never two live contributions (SC-006) |
| Stale `existingContributionVersion` / stale preflight token | 409; nothing partially committed (SC-005); stale-preflight response carries fresh classification |
| Grouped `[[A,B]]` + automatic `[[A],[C]]` | 3 units, 2 contributions in `psql` — never `[[A,B,C]]` |
| Confirm an automatic source with the word-match toggle **off**; a manual ayah with zero selected words | Ayahs stored with zero `linking_unit_ayah_words` rows — valid in both cases; toggle **on** stores exactly the resolution's matched words (derived, never client-authored) |
| Multi-source op with one invalid source | Whole operation rejected; database untouched (SC-005) |

### 5. Frontend cutover (browser, per phase 10–13)

| Probe | Expect |
| --- | --- |
| Open a source editor (DevTools network) | **One** request, not `ceil(total/100)`; reopening: zero requests |
| `grep` the codebase | `presentation-occurrence`, `manual-word-location`, mock command port: gone |
| Manual Mushaf flow reader → editor | Same path as automatic; reader-mode "can this verse be added" gate still works |
| Sign out → different browser | Workspace reappears; old browser-local bucket cleared after first server hydration |
| 2,000-ayah source in the editor | One continuous scroll, no pagination control, bounded DOM node count (SC-008); exclusion near the end survives scrolling away/back |
| Editor at Wide / Medium / Compact | Exactly one vertical scroll owner; keyboard reachable |
| Preflight step UI | Arabic labels; counts + inspectable items; INVALID disables confirm with per-item reasons; no-op renders as success |
| Quran glyphs/metrics everywhere touched | Unchanged; `check:golden-ui` passes (SC-011) |

## Final hardening sweep (Phase 14)

Run the full plan §14 matrix (A1–F4), then confirm: shared `IMemoryCache` still has no
`SizeLimit` and no existing `Set` changed; `AbwabPermissionCatalogue` still 19 codes; every
Linking route exactly one `[RequireOwner]`; every writer save translates exceptions; no cache key
contains user/Door/configuration; `check-pending-model` clean; smoke-dump manifest matches head
migration; and `git diff --stat -- Backend/tests Frontend/quran-dashboard-ui/e2e` is **empty**.
