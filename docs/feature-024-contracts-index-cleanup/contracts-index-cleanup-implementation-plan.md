# Implementation Plan — `docs/contracts/` Thin Index + Freeze `specs/` Contracts

**Feature:** 024 — contracts-index-cleanup
**Plan doc:** `docs/feature-024-contracts-index-cleanup/contracts-index-cleanup-implementation-plan.md` (this file)
**Branch (implementation):** `feature-024-contracts-index-cleanup`
**Nature:** docs + instruction rewiring + deletion of planning-time contract copies. **No code / API / schema / Quran-data change.**
**Status:** PLAN ONLY. No implementation phase started. Nothing committed.

> Note: partly superseded — the Spec-Kit review model was corrected after Run 2. The authoritative two-role model (per-feature planning contract in `specs/<feature>/contracts/` vs steady-state truth = code + nearest README indexed by `docs/contracts/`) lives in the instruction files + `specs/README.md`. This plan's Appendix A8 / original §4 reflect the pre-correction wording.

---

## 0. Assumptions / corrections (from repository evidence)

1. **`ApiResponse.cs` real path** is `Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs` (an earlier draft omitted `/Contracts`). This plan and the appendix use the real path.
2. **"Zero dangling references"** is scoped to the **live** layer (instructions, skills, guides, code-adjacent READMEs, and the `.specify/` tooling tree). **Frozen archives are excluded by design:** 42 non-contract files inside `specs/**` and the `docs/feature-021/022/**` planning docs link to `contracts/…` and will dangle after deletion — rewriting them would corrupt the historical record. The freeze notice acknowledges this.
3. **Frontend twins differ by design** in the H1 (`# Frontend Project Instructions` vs `# Frontend Agent Guide`) plus the `CLAUDE`/`AGENTS` self-reference tokens; they are not forced fully identical.
4. **`.agents/` and `.opencode/` skill files are routing stubs** ("route agents to the single source of truth" → the `.claude` skill). Only `.claude/skills/engineering-review/**` and `.claude/skills/test-guard/references/dotnet.md` hardcode contract paths. The stubs must be re-verified to stay path-free after edits.
5. **Root twins line 40** (`` `AGENTS.md` / `CLAUDE.md` / `.architecture/*` = HOW… ``) intentionally names both files and is byte-identical in both twins — the twin verification must not flag it.
6. **Confirmed next feature number = 024** (docs/feature-* max 022; Backend/report/feature-* max 009; specs max 019; branches max 023 → 024 free everywhere).

---

## 1. Objective & final behavior

Introduce `docs/contracts/` as a **thin index layer**: a small set of Markdown pages that **only point** to the authoritative current-truth sources (code files, nearest code-adjacent `README.md`, `Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs`, `Backend/.architecture/API_GUIDELINES.md`, and the "Route families" section of `Backend/api/QuranDashboard.Api/Controllers/README.md`). It restates **no** contract content — no route tables, no DTO field lists, no counts, no identity rules, no schemas.

After the change:

- An agent seeking "what is the current API / route / read / identity contract" reads `docs/contracts/` first, which routes it to **code + nearest README** (the authority).
- `specs/<feature>/` is a **frozen historical planning archive**; agents do not scan it routinely. Its `contracts/` subfolders are gone; the rest (`spec/plan/tasks/data-model/research/quickstart/checklists`, `002/source-provenance.md`) is untouched.
- Every live instruction / skill / guide that previously pointed at `specs/**/contracts/**` now points at `docs/contracts/` + code/README.

**What `docs/contracts/` is NOT:** not a mirror of specs, not a second copy of any contract, not the truth itself. It **defers** and says so on every page.

---

## 2. Scope & non-goals

**In scope:** create `docs/contracts/**` (index only); create `specs/README.md` (freeze notice); rewire the instruction twins + `docs/README.md` + `SKILLS_AND_ARCHITECTURE_GUIDE.md` + the `.claude` engineering-review Spec-Kit skill + `.claude/skills/test-guard/references/dotnet.md`; delete the 15 `specs/**/contracts/` folders (54 files).

**Non-goals (explicit):**

- No authored/copied contract content in `docs/contracts/` (pointers only).
- No code, API, controller, DTO, EF, schema, migration, or Quran-data change.
- No deletion beyond `specs/**/contracts/**`. **Keep** all other specs artifacts, `AGENTS.md.bak`, `.specify/**`, everything else.
- No change to `.specify/memory/constitution.md` (unfilled template; **not** a truth source — do not add it to the truth chain).
- **No change to the Spec-Kit generator skills** that scaffold `specs/<feature>/contracts/` for *future* features (`speckit-plan` / `speckit-specify` across `.claude`, `.agents`, `.opencode`). See §8 for the named out-of-scope follow-up.

---

## 3. Proposed `docs/contracts/` structure

**Naming convention (single, stated):** kebab-case, one page per **bounded context**, plain `.md`, no `api-` / `.api.` / `-api` affixes (the old `*.api.md` / `*-api.md` / `api-*.md` inconsistency dies with the deleted files). Pattern: `docs/contracts/<bounded-context>.md`.

| Page | Role (pointer only) | Authoritative sources it links to |
|---|---|---|
| `README.md` | Layer role + **precedence rule** (decision 2) + table of contents | `docs/README.md`, all pages below |
| `http-api.md` | HTTP API route families | `Controllers/README.md` "Route families"; `Contracts/ApiResponse.cs`; `API_GUIDELINES.md`; `Controllers/` dirs |
| `response-envelope.md` | Success/failure envelope | `Contracts/ApiResponse.cs`; `API_GUIDELINES.md` §5; frontend `api-response.model.ts` |
| `words-explorers.md` | Roots/Lemmas/Stems/WordTypes/Unique — reads, **identity keys, count families** (link only) | `…/Reads/Quran/Words/README.md`; `features/words/README.md`; `Controllers/Words/` |
| `mushaf-reader.md` | Pages / study / similarities / word analysis | `…/Reads/Quran/MushafReader/README.md`; `features/mushaf/README.md`; `Controllers/MushafReader/` |
| `import-pipelines.md` | Importer verbs + data pipelines + reports | `DataImporter/README.md`; DataPipelines READMEs; `Persistence/DataPipelines/Quran/README.md` |
| `frontend-shell.md` | Navigation, design tokens, URL-state | `core/README.md`; `shared/README.md`; `styles/README.md` |

Every page carries the header: *"Index only — defers to the linked code + README, which are the authority. See `docs/contracts/README.md`."* No counts, identity rules, or schemas restated (Quran-data safety, §Constraints). Ready-to-paste page texts are in the Appendix.

---

## 4. Affected files — exact edits enumerated

### 4A. Instruction twins — edit **identically** in each pair (lockstep)

**Root `CLAUDE.md` + `AGENTS.md`:**

- `:21–32` (Workspace Path Conventions): **add** bullet — *"Current contract index (thin, pointer-only) → `docs/contracts/`; it defers to code + nearest README."*
- `:30`: append to the Spec-Kit sentence — *"`specs/` is a **frozen** per-feature planning archive (contracts removed; not scanned routinely). Current contract truth: `docs/contracts/` → code + nearest README."*
- `:41`: change *"`specs/` = feature plans/contracts. Reports = evidence only."* → *"`specs/` = **frozen** historical feature plans (contracts removed → `docs/contracts/`). Reports = evidence only."*
- `:47`: change *"Specs remain planning/contract artifacts; README files do not replace them."* → *"Specs remain **frozen** planning artifacts; current truth is code + nearest README, indexed by `docs/contracts/`."*
- **Go-forward sentence (CORRECTION 1) — add to the same section:** *"`specs/<feature>/contracts` (existing 001–019, and any future scaffold a Spec-Kit generator may create) is planning-time only; the current contract truth is the code + nearest README, indexed by `docs/contracts/`. Contracts under specs are not maintained after a feature merges."*
- `:40`: **leave unchanged** (names both twins; keep identical).

**`Backend/CLAUDE.md` + `Backend/AGENTS.md`:**

- `:18`: in the "read nearest README before `.architecture/*`" bullet, **add** — *"and consult `docs/contracts/` (pointer index) to locate the authoritative README/code for a contract."*
- `:52`: change *"Spec Kit artifacts belong under `…/specs/`."* → *"Spec Kit artifacts belong under `…/specs/` (**frozen** archive; contracts removed → `docs/contracts/`)."*
- **Go-forward sentence (CORRECTION 1) — add the same verbatim sentence** as in the root twins immediately after the `:52` edit.

**`Frontend/quran-dashboard-ui/CLAUDE.md` + `AGENTS.md`** (H1 differs by design):

- `:22`: in the "read nearest README" bullet, **add** — *"use `docs/contracts/frontend-shell.md` / `words-explorers.md` / `mushaf-reader.md` to find the authoritative README/code."*

### 4B. `docs/README.md`

- `:5`: keep *"not the current-truth layer"*; **add** — *"For contracts, `docs/contracts/` is a pointer index (also not the truth; it defers to code + README)."*
- `:9`: keep *"Current truth of a code area → the local README.md nearest that code"*; **add** — *"`docs/contracts/` indexes these READMEs and **defers to them — the README/code wins.**"* (encodes decision-2 precedence).
- `:13`: change *"Feature plans / contracts → `specs/<feature>/` (unchanged; authoritative planning artifacts)."* → *"Feature plans → `specs/<feature>/` (**frozen** archive; contracts removed). Current contract index → `docs/contracts/`."*

### 4C. `SKILLS_AND_ARCHITECTURE_GUIDE.md`

- `:100`: *"Spec Kit change: … + the relevant `specs/<feature>/{spec,plan,tasks}.md`, `contracts/`, `quickstart.md`."* → replace `contracts/` with *"`docs/contracts/` (→ code + nearest README)"*; keep spec/plan/tasks/quickstart (still exist, frozen).
- `:106`: *"contract compliance (`contracts/api-*.md`, `ui-*.md`)"* → *"contract compliance via `docs/contracts/` → code + nearest README; response envelope via `Contracts/ApiResponse.cs` + `API_GUIDELINES.md` §5."*
- `:56`: *"Spec Kit per-feature details (those live in `specs/<feature>/`)."* → append *"(frozen; contracts removed → `docs/contracts/`)."*
- `:107`, `:210`, `:250`, `:293`: soften wording that implies `specs/**/contracts` is a **current** source — where each names `contracts/` as authoritative, repoint to `docs/contracts/` + README; where it names `specs/<feature>/` generally (spec/plan/tasks still exist), add "(frozen archive)".

### 4D. `.claude/skills/engineering-review/**`

- `SKILL.md:137`: *"the relevant files under `specs/<feature>/contracts/`"* → *"the current-contract index `docs/contracts/` (which points to the authoritative code + nearest README)."*
- `SPEC_KIT_IMPLEMENTATION_REVIEW.md`:
  - `:26` *"the relevant files under `contracts/`"* → *"`docs/contracts/` → the authoritative code + README."*
  - `:72` *"When a contract under `contracts/` is relevant…"* → *"When a contract is relevant, resolve it via `docs/contracts/` to its authoritative code + README, then compare."*
  - `:76` *"matching `contracts/api-*.md` (api-health.md, api-dashboard-info.md)"* → *"the endpoint's controller + `Controllers/README.md` route family (indexed by `docs/contracts/http-api.md`)."*
  - `:77` `contracts/ui-navigation.md` → *"`Frontend/…/core/README.md` (indexed by `docs/contracts/frontend-shell.md`)."*
  - `:78` `contracts/ui-design-tokens.md` → *"`Frontend/…/styles/README.md` (indexed by `docs/contracts/frontend-shell.md`)."*
  - `:81` `contracts/api-response-envelope.md` → *"`Contracts/ApiResponse.cs` + `API_GUIDELINES.md` §5 (indexed by `docs/contracts/response-envelope.md`)."*
  - `:117` example referencing `contracts/ui-navigation.md` → update to the README/code source or `docs/contracts/frontend-shell.md`.

### 4E. `.claude/skills/test-guard/references/dotnet.md`

- `:58`: *"matching the relevant `contracts/api-*.md`"* → *"matching `API_GUIDELINES.md` §5 / `Contracts/ApiResponse.cs` (indexed by `docs/contracts/response-envelope.md`)."*

### 4F. New files

- `docs/contracts/README.md` + the 6 index pages (§3, Appendix).
- `specs/README.md` — freeze notice (§7, Appendix).

### 4G. Deletions (Phase 3 only)

- The 15 folders `specs/{001,002,003,004,005,006,007,008,009,011,012,014,015,016,019}/contracts/` (54 files total).

### 4H. Explicitly **not** edited (leave as-is)

`.specify/memory/constitution.md`; `.specify/feature.json`; `AGENTS.md.bak`; the `speckit-plan` / `speckit-specify` generator skills in `.claude` / `.agents` / `.opencode` (future-scaffold `/contracts/`); code-adjacent README "Specs:" backlinks (`MorphologyImporting/README.md:52`, `Reads/Quran/Words/README.md:87`, `features/mushaf/README.md:43`, `features/words/README.md:93`) — they point to `specs/<feature>/` **dirs** (still exist); `docs/feature-021/022/**` historical planning docs (frozen).

---

## 5. Ordered phases (deletion last)

**Phase 1 — Author `docs/contracts/` thin index.** Create `README.md` + 6 pages (Appendix). Depends on: nothing. Output: index exists; every link resolves to a currently-existing file. The index never links to `specs/**/contracts`.

**Phase 2 — Rewire instructions + skills.** All edits in §4A–4E. Depends on: Phase 1 (so pointers target real `docs/contracts/` pages). Keep twins in lockstep.

**Phase 3 — Freeze + delete (only after 1–2).** (a) Create `specs/README.md` freeze notice; (b) delete the 54 files in the 15 `specs/**/contracts/` folders. Depends on: Phases 1–2 complete and verified — nothing live points at the deleted paths anymore.

**Phase 4 — Repo-wide verification.** §6. Depends on: Phase 3.

---

## 6. Verification per phase

**Phase 1:** `docs/contracts/` link-resolution — extract every link target from `docs/contracts/*.md`; assert each `test -e` exists. Content-audit: grep the pages for digit-heavy count tables, identity-key phrasing, or JSON schema bodies → must be **absent** (pointer-only; decision 1 + Quran-safety).

**Phase 2:** Twin byte-check —

- Root & Backend: diff expecting differences **only** on the self-reference lines (`:7 / :11 / :15`) plus the intentional new content added identically to both; treat line 40 as intentionally identical (do **not** use a blind `CLAUDE→AGENTS` sed that false-flags it).
- Frontend: differences only on H1 + self-reference tokens + the identical added bullet text.
Re-grep the `.agents` / `.opencode` engineering-review + test-guard **stubs** to confirm they remain path-free (still "route to single source of truth"; no hardcoded `contracts/…`).

**Phase 3:** `git status` shows **only** deletions under `specs/**/contracts/` (exactly 54) + the new `specs/README.md` and `docs/contracts/**` — no other specs artifact touched.

**Phase 4 — dangling-reference sweep (LIVE layer; MUST be zero):**

Across `.claude/`, `.agents/`, `.opencode/`, `.cursor/`, `.codex/`, root & `Backend/` & `Frontend/` instruction files, `SKILLS_AND_ARCHITECTURE_GUIDE.md`, and all code-adjacent `README.md`, grep for `specs/[0-9].*/contracts`, `contracts/api-*`, `contracts/ui-*`, and the deleted filenames (`validation-report.schema`, `cli-verb`, `backend-read-abstractions`, `frontend-routing-state`, `*-abstractions.md`, `*.api.md`, `*-api.md`, `import-manifest.schema`, `source-files.md`, `api-response-envelope`, `ui-design-tokens`, `ui-navigation`) **as current sources** → expect **0**. Sweep **excludes** frozen archives `specs/**` and `docs/feature-*/**` (dangles there are intentional, §0-2). Also confirm no `*.cs` / `*.ts` references any contract path (re-confirm none compile). Backend build/tests optional (docs-only, no code changed).

**Phase 4 — `.specify/` tooling sweep (CORRECTION 2):**

Sweep the entire `.specify/` tree — `scripts/`, `templates/`, `workflows/`, `extensions/`, `integrations/`, `memory/`, and root manifests. Goal: confirm **no script / workflow / template READS an existing `specs/**/contracts/` path as an input it parses** (e.g. a prerequisite checker that loads `contracts/*.md`, a plan/analyze step that enumerates `contracts/` as required inputs, a validation that fails when `contracts/` is absent).

- Search commands + templates for `contracts` used as an **input** (read/require/enumerate/`ls`/glob/`cat`/parse), e.g. `grep -rniE "contracts" .specify` then classify each hit.
- **Distinguish OUTPUT from INPUT:** generator templates that *emit* a future `contracts/` scaffold (Spec-Kit `plan`/`specify` producing `/contracts/*`) are **expected and out of scope** — do not touch them. Only a **reader of existing `specs/**/contracts/`** is a problem.
- **STOP CONDITION:** if any `.specify/` reader of existing `specs/**/contracts/` is found, **stop and report** — deletion would break that tool; it must be repointed or the plan revised before Phase 3.
- (Inspection to date found only generator/OUTPUT references in `.specify` and the mirrored skill trees; no reader. Re-verify at implementation time.)

**Phase 4 — link integrity:** re-run the Phase-1 link-resolution check now that `specs/README.md` exists and `specs/**/contracts` is gone; every `docs/contracts/**` and `specs/README.md` link must resolve.

---

## 7. Documentation updates required

- `docs/README.md` — §4B (add `docs/contracts/` pointer; state README-wins precedence).
- `specs/README.md` (new) — freeze notice **including the go-forward sentence (CORRECTION 1)**. Full text in the Appendix.
- `docs/contracts/README.md` (new) — layer role + precedence. Appendix.
- **Optional** (not required, report-only): add a one-line `docs/contracts/` pointer to the "Related" sections of `Controllers/README.md`, `Reads/Quran/Words/README.md`, `features/words/README.md`, `features/mushaf/README.md`. Not blocking.

---

## 8. Risks / rollback / stop conditions

**Risks & mitigations:**

- *Intra-archive dangles (42 specs files + docs/feature-021/022):* by design; freeze notice explains; sweep scoped to live layer + `.specify`. **Accepted.**
- *Future `/speckit.plan` re-creates `specs/<feature>/contracts/`:* the freeze applies to existing 001–019; the generators are unchanged. The go-forward sentence (CORRECTION 1) makes the policy explicit: any such future scaffold is planning-time only and unmaintained after merge. **Named out-of-scope follow-up (do not act now):** *"Converge the Spec-Kit generators to `docs/contracts/` — i.e. stop scaffolding `specs/**/contracts` in `speckit-plan` / `speckit-specify` across `.claude` / `.agents` / `.opencode`."* Tracked here as a separate decision; **not** part of Feature 024.
- *Weaker Spec-Kit contract check:* compliance now resolves through README/code instead of a frozen doc — consistent with decision 2 (code + README is truth). Reviewers gain accuracy, lose a frozen snapshot. **Accepted.**
- *Twin drift during edits:* Phase-2 byte-check catches it.

**Rollback (git):** single branch, single final commit (later, on authorization). Pre-commit: `git restore .` and remove new untracked dirs (`docs/contracts/`, `specs/README.md`) after review. Post-commit: `git revert <sha>` (deletions restored from history — all files tracked) or reset the unpushed branch.

**Stop conditions (report, don't force):**

- Any `specs/**/contracts/*.md` found to be **imported/parsed by code or a runtime script** (inspection: none — all references are prose).
- Any `.specify/` script/workflow/template that **reads existing `specs/**/contracts/` as parsed input** (CORRECTION 2 sweep).
- Any skill whose contract check **cannot be repointed** without breaking its control flow (inspection: all are prose pointers — none).
- A truth-model reference that cannot be cleanly repointed to a real README/code target.
- Working tree not clean, or branch already exists (checked before branch creation).

---

## 9. Acceptance criteria → locked decisions

| Decision | Acceptance criterion |
|---|---|
| **D1** thin index, no copied content | `docs/contracts/**` contains only pointers; Phase-1 content-audit finds no restated counts/identity/schemas/route-tables |
| **D2** precedence + defer + freeze | `docs/contracts/README.md` + `docs/README.md:9` state README/code **wins**; each index page carries the "index only — defers" header; instructions say `specs/` is frozen / not scanned routinely |
| **D3** delete only `specs/**/contracts` | `git status`: exactly 54 deletions under `specs/**/contracts/`; every other specs artifact + `AGENTS.md.bak` intact |
| **D4** `specs/README.md` freeze notice | file exists; points to `docs/contracts/` + nearest READMEs; carries the go-forward sentence; explains frozen archive + expected internal dangles |
| **Live layer clean** | Phase-4 sweep (incl. `.specify/`): **0** dangling contract references in instructions/skills/guides/READMEs/tooling; no `.specify` reader of deleted paths |

---

## 10. Commit boundary

Single coherent final commit (Phases 1–3 as one change; verification in 4), **only when the user later authorizes it**, on branch `feature-024-contracts-index-cleanup`. Suggested subject: `chore(docs): add docs/contracts thin index; freeze specs planning archive`. Do not stage, commit, branch elsewhere, or push as part of planning.

---

# Appendix — ready-to-paste page texts

> Pointer-only. No restated counts, identity rules, route tables, DTO fields, or schemas (Quran-data safety). Links are written relative to each file's eventual home. Every target path was verified to exist at plan time. Paste each block into the named file during the phase indicated.

## A1. `docs/contracts/README.md` (Phase 1)

```markdown
# Contracts index

Index only — defers to the linked code + README, which are the authority. See docs/contracts/README.md.

## What this layer is

`docs/contracts/` is a **thin pointer index** to the current contract truth of this
monorepo. It restates **no** contract content — no routes, no DTO fields, no counts,
no identity rules, no schemas. Each page links to the authoritative source.

## Precedence (truth model)

Current **code + the nearest `README.md`** is the authority ("current truth"). This
index **defers** to them; where this index and a README/code disagree, **the
README/code wins**. `specs/<feature>/` is a **frozen** historical planning archive and
is **not** scanned routinely — see [`../../specs/README.md`](../../specs/README.md).

## Pages

- [HTTP API — route families](./http-api.md)
- [Response envelope](./response-envelope.md)
- [Words explorers — reads, identity, counts](./words-explorers.md)
- [Mushaf reader](./mushaf-reader.md)
- [Import pipelines & CLI verbs](./import-pipelines.md)
- [Frontend shell — navigation, tokens, URL-state](./frontend-shell.md)

## Related

- Workspace docs layer: [`../README.md`](../README.md)
- Frozen planning archive: [`../../specs/README.md`](../../specs/README.md)
```

## A2. `docs/contracts/http-api.md` (Phase 1)

```markdown
# HTTP API — route families

Index only — defers to the linked code + README, which are the authority. See [docs/contracts/README.md](./README.md).

Current HTTP route families, status mapping, and the `ApiResponse<T>` envelope are
defined by the controllers and the API README. This page does **not** restate routes,
parameters, or payloads.

## Authoritative sources

- Route families overview → [`Controllers/README.md`](../../Backend/api/QuranDashboard.Api/Controllers/README.md) ("Route families" section)
- Controllers (actual routes) → [`Controllers/`](../../Backend/api/QuranDashboard.Api/Controllers/) — `Words/`, `MushafReader/`, `Dashboard/`, `System/`
- API boundary rules (verbs, status codes, response shape) → [`API_GUIDELINES.md`](../../Backend/.architecture/API_GUIDELINES.md)
- API project overview → [`api/QuranDashboard.Api/README.md`](../../Backend/api/QuranDashboard.Api/README.md)
- Response envelope → [response-envelope.md](./response-envelope.md)

**Precedence:** the controller code + `Controllers/README.md` win over any other description.
```

## A3. `docs/contracts/response-envelope.md` (Phase 1)

```markdown
# Response envelope

Index only — defers to the linked code + README, which are the authority. See [docs/contracts/README.md](./README.md).

The success/failure envelope shape is the C# `ApiResponse<T>` record plus the API
guidelines; the frontend mirror is the TypeScript model. This page does **not** restate
fields.

## Authoritative sources

- Backend envelope type → [`Contracts/ApiResponse.cs`](../../Backend/api/QuranDashboard.Api/Contracts/ApiResponse.cs)
- Envelope + status-code rules → [`API_GUIDELINES.md`](../../Backend/.architecture/API_GUIDELINES.md) (§5 Response Shape)
- Frontend mirror type → [`api-response.model.ts`](../../Frontend/quran-dashboard-ui/src/app/core/data-access/api-response.model.ts)

**Precedence:** `ApiResponse.cs` + `API_GUIDELINES.md` win.
```

## A4. `docs/contracts/words-explorers.md` (Phase 1)

```markdown
# Words explorers — reads, identity, counts

Index only — defers to the linked code + README, which are the authority. See [docs/contracts/README.md](./README.md).

Covers the Roots, Lemmas, Stems, Word Types, and Unique Words explorers. **Word
identity keys, count-family rules, and ordering-as-contract are defined in the reads
README and reader code — this index does not restate them** (see sources).

## Authoritative sources

- Read models, identity keys, count semantics, ordering → [`Reads/Quran/Words/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/README.md)
- HTTP endpoints → [`Controllers/Words/`](../../Backend/api/QuranDashboard.Api/Controllers/Words/) and [http-api.md](./http-api.md)
- Frontend explorers (routes, URL-state, cache) → [`features/words/README.md`](../../Frontend/quran-dashboard-ui/src/app/features/words/README.md)

**Precedence:** reader code + reads README win; do not derive identity/count rules from anywhere else.
```

## A5. `docs/contracts/mushaf-reader.md` (Phase 1)

```markdown
# Mushaf reader

Index only — defers to the linked code + README, which are the authority. See [docs/contracts/README.md](./README.md).

Covers page reading, selected-ayah study, ayah similarities / mutashabihat, and
selected-word analysis. This page does **not** restate routes or payloads.

## Authoritative sources

- Read models → [`Reads/Quran/MushafReader/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/MushafReader/README.md)
- HTTP endpoints → [`Controllers/MushafReader/`](../../Backend/api/QuranDashboard.Api/Controllers/MushafReader/) and [http-api.md](./http-api.md)
- Frontend reader (routes, URL-state) → [`features/mushaf/README.md`](../../Frontend/quran-dashboard-ui/src/app/features/mushaf/README.md)

**Precedence:** reader code + reads README win.
```

## A6. `docs/contracts/import-pipelines.md` (Phase 1)

```markdown
# Import pipelines & CLI verbs

Index only — defers to the linked code + README, which are the authority. See [docs/contracts/README.md](./README.md).

Covers the operator-only DataImporter verbs, the file→DB data pipelines, and the
validation / import report outputs. This page does **not** restate verb lists, manifest
schemas, report shapes, or output paths — **importer verbs, report locations, and
source-safety rules live in the code + these READMEs.**

## Authoritative sources

- CLI verbs / importer host → [`DataImporter/README.md`](../../Backend/tools/QuranDashboard.DataImporter/README.md)
- File data pipelines (overview) → [`Files/Quran/DataPipelines/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/README.md)
  - Foundation → [`DataPipelines/Foundation/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Foundation/README.md)
  - Morphology importing → [`DataPipelines/Words/MorphologyImporting/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/README.md)
  - Simple i3rab generation → [`DataPipelines/Words/SimpleI3rabGeneration/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/SimpleI3rabGeneration/README.md)
- Persistence data pipelines → [`Persistence/DataPipelines/Quran/README.md`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/README.md)

**Precedence:** importer code + pipeline READMEs win.
```

## A7. `docs/contracts/frontend-shell.md` (Phase 1)

```markdown
# Frontend shell — navigation, design tokens, URL-state

Index only — defers to the linked code + README, which are the authority. See [docs/contracts/README.md](./README.md).

Covers the app shell: navigation items, design tokens / styling, shared building
blocks, and cross-feature URL-state conventions. This page does **not** restate token
values, nav tables, or route keys.

## Authoritative sources

- Core (navigation, data-access, app shell) → [`app/core/README.md`](../../Frontend/quran-dashboard-ui/src/app/core/README.md)
- Shared building blocks → [`app/shared/README.md`](../../Frontend/quran-dashboard-ui/src/app/shared/README.md)
- Styles / design tokens → [`styles/README.md`](../../Frontend/quran-dashboard-ui/src/styles/README.md)
- Response envelope (frontend model) → [response-envelope.md](./response-envelope.md)

**Precedence:** frontend code + these READMEs win.
```

## A8. `specs/README.md` (Phase 3 — freeze notice)

```markdown
# specs/ — frozen per-feature planning archive

This folder is a **frozen historical planning archive** (Spec-Kit artifacts for
Features 001–019). It is **not** current truth and must **not** be scanned routinely.

## Current truth lives elsewhere

- Current contract index (thin, pointer-only) → [`../docs/contracts/`](../docs/contracts/README.md)
- Authority behind that index → the actual **code** + the **nearest `README.md`** to that code.

## Go-forward rule

`specs/<feature>/contracts` (existing 001–019, and any future scaffold a Spec-Kit
generator may create) is planning-time only; the current contract truth is the code +
nearest README, indexed by `docs/contracts/`. Contracts under specs are not maintained
after a feature merges.

## What is here

Per-feature `spec.md`, `plan.md`, `tasks.md`, `data-model.md`, `research.md`,
`quickstart.md`, `checklists/` (and `002/source-provenance.md`). The `contracts/`
subfolders were **removed** during Feature 024; archived documents may still link to
those removed paths — those links are historical and intentionally not maintained.
```
