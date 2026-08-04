# Abwab documentation-vs-code contradiction sweep

**Mode:** read-only inspection. No source file, test, README, spec, or configuration was
modified. No planning artifact was deleted or moved. No migration, import, seed, build, or test
was run. No Git action beyond `status` / `log` / `show` / `ls-files` / `grep`.

**Repo state at sweep time:** branch `dev`, working tree **clean** (`git status --porcelain
--untracked-files=all` → 0 entries). `origin/main` and `origin/dev` are both at `4f1ac91c`;
`git rev-list --count origin/main..origin/dev` → 0. **The Abwab feature is in `main`** (180
Abwab paths tracked there).

**Method.** Twelve contract threads each checked one contract end-to-end — the doc claim *and*
the code truth. Every reported `path:LINE` anchor was then re-opened mechanically and the quoted
string re-matched against that exact line: **32/32 anchors verified, zero drift**. Findings that
could not be verified on both sides were dropped. Two subagent counts were wrong and are
corrected here from direct measurement (see F20 and F23).

> ### ⚠ Read F01 first
> The single most consequential result of this sweep is not a stale sentence. **The Abwab
> feature was released to production while its own README still states the release gate that
> forbids exactly that, and the write protection that gate waits on never landed.** Twenty-one
> unauthenticated write endpoints are live. Nothing was changed; see F01 and Open Question 1.

---

## 1. Scope actually covered

### Read

**Governing / long-lived docs.** `CLAUDE.md`, `AGENTS.md`, `Backend/CLAUDE.md`,
`Backend/AGENTS.md`, `Frontend/quran-dashboard-ui/CLAUDE.md`,
`Frontend/quran-dashboard-ui/AGENTS.md`, `CODING_PRINCIPLES.md`, `TESTING_STRATEGY.md`,
`docs/TESTING_DEBT.md`, `PRODUCT.md`, `DESIGN.md`, `SKILLS_AND_ARCHITECTURE_GUIDE.md`,
`.specify/memory/constitution.md`, `.specify/feature.json`, and the `.claude/skills/` /
`.agents/skills/` SKILL files.

**READMEs.** `docs/README.md`, `docs/contracts/README.md` + all six contract pages,
`specs/README.md`, `Backend/report/README.md`, `Backend/README.md`, `Backend/scripts/README.md`,
`Backend/api/QuranDashboard.Api/README.md`, `.../Controllers/README.md`,
`.../Authentication/README.md`, `.../RateLimiting/README.md`,
`.../Application.Abstractions/Security/README.md`, `.../Persistence/Reads/Abwab/README.md`,
`.../Persistence/Writes/Abwab/README.md`, `Backend/tools/QuranDashboard.DataImporter/README.md`,
`Backend/tests/QuranDashboard.Tests/README.md`, `Frontend/quran-dashboard-ui/README.md`,
`.../src/app/core/README.md`, `.../src/app/core/navigation/detail-overlay/README.md`,
`.../src/app/features/abwab/README.md` (964 lines, in full), `.../src/app/shared/README.md`,
`.../src/styles/README.md`, `.../e2e/README.md`, `docs/design-preview/README.md`.

**Architecture / style.** `UI_STYLE_SYSTEM.md` (incl. the §17 pattern registry, all 20 entries),
`FRONTEND_STRUCTURE.md`, `API_INTEGRATION_GUIDELINES.md`, `Backend/.architecture/API_GUIDELINES.md`,
`CLEAN_ARCHITECTURE.md`, `BACKEND_STRUCTURE.md`, `LOGGING_GUIDELINES.md`.

**Code.** All six Abwab controllers; the Abwab command handlers, writers, readers, caching
decorators, EF configurations, migrations and domain types; `Program.cs` +
`WebApplicationExtensions.cs` + `GlobalExceptionHandler.cs`; `SmokeRouteCatalog.cs` +
`SmokeCoverageParityTests.cs` + `SmokeAbwabWriteTests.cs`; the Abwab frontend state layer
(`abwab-url-sync.ts`, `abwab-modal-url.controller.ts`, `abwab-relations.controller.ts`,
`abwab-snapshot.facade.ts`, `abwab-tree.builder.ts`, `abwab-selection.store.ts`,
`abwab-write.controller.ts`, `abwab-page-overlays.controller.ts`); the tree, pickers, modals and
toolbar components with their SCSS and specs; both data-access files; `conditional-request.ts`;
`abwab.labels.ts`; `abwab.models.ts`; `abwab.routes.ts`; `app.routes.ts`; `core/auth/`;
`shared/ui/context-menu/` and `shared/ui/confirm-dialog/`; `src/styles/_tokens.scss` and
`_themes.scss`; `playwright.config.ts`; `package.json`; every script in `Backend/scripts/`.

### Deliberately not treated as authority

- **`specs/**` and `docs/feature-*/**` — superseded planning material.** Read only to understand
  intent and to detect plan-era decisions that leaked into long-lived docs. Code is never
  reported as non-compliant with them. (`specs/` currently contains only `README.md`.)
- **Dated evidence reports** — `docs/engineering-review-full-project-2026-07-18.md`,
  `docs/performance-review/report.md`, `Backend/report/**`. Evidence is true when written, not a
  claim about today; not used as claim sources.
- **`docs/api-reference/index.html`** — a 1.7 MB generated Redoc bundle. Verified to exist and to
  be built by `npm run docs:api` from `openapi/swagger.json` (both present). Its minified vendor
  payload was not audited; the one seeder-grep hit inside it (`id-map`) is a false positive in
  that bundle.
- **`docs/design-preview/*.html`** — historical design comps, read only for the decisions the
  slices reversed. Their **charter** (`docs/design-preview/README.md`) *is* treated as long-lived,
  because `CLAUDE.md:62` names `docs/design-preview/` a never-swept live folder and both
  `PRODUCT.md:41` and `DESIGN.md:2` send readers to that README.
- **`docs/abwab-ux-audit.md`** — treated as long-lived (it declares itself exempt from the
  lifecycle sweep at `:4-5`) but as a **dated backlog**, not a current behavior spec. Its 23
  per-item `file:line` citations are pre-slice and were not individually re-anchored; only its
  framing claims and its four recorded reversals were checked. Findings F30, F36 and F39 are
  samples of that staleness, not an exhaustive audit of it.

### Type C bright line

Type C is filed only for **contractual** categories — URL keys, cache identity/invalidation, HTTP
status codes, NOT NULL / cascade / uniqueness invariants, fail-closed parsing, and single-instance
deployment constraints. Internal implementation detail is out of scope.
`features/abwab/state/` has no README of its own; its nearest README is
`features/abwab/README.md`, which is what Type C was judged against — and that file is unusually
thorough, which is why only four Type C findings exist.

### Leftovers worth noting (not findings)

`docs/feature-abwab-legacy-seed/` and `Backend/report/feature-abwab-legacy-seed/railway/` survive
on disk as **empty directories**. Git tracks no files in them, so the tree reads clean; they are
inert residue of the seeder deletion.

---

## 2. Findings table

39 findings, sorted by severity then type.

*(The `409`-shape and empty-root-`400` staleness first drafted as a separate F04 is folded into
F03: same paragraph, same causing commit, one README edit closes both. IDs F05–F40 are left
unchanged so citations stay stable, so the sequence skips F04.)*

| ID | Type | Sev | Summary | Doc citation | Code citation |
|---|---|---|---|---|---|
| F01 | E | **HIGH** | Abwab shipped to production while its README still states the release gate forbidding it; write protection never landed | `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:11` | `Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs:27` |
| F02 | B | **HIGH** | `AGENTS.md` declares an open Spec-Kit feature; `CLAUDE.md` says "None" — the files are otherwise byte-identical | `AGENTS.md:206` | `CLAUDE.md:206` |
| F03 | E | **HIGH** | `Controllers/README.md` still documents root-inclusive template apply, the axiom ux-slice-g reversed | `Backend/api/QuranDashboard.Api/Controllers/README.md:40` | `.../Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs:126` |
| F05 | E | MED | abwab README says a reveal's only effect on `modal` is to clear it; ux-slice-l made it **retain** `relations-<id>-closed` | `Frontend/.../features/abwab/README.md:436` | `.../pages/abwab-page/abwab-page.component.ts:524` |
| F06 | E | MED | abwab README lists "the detach announcement" among covered e2e behaviors; detach-to-null-section was removed entirely | `Frontend/.../features/abwab/README.md:938` | `Frontend/quran-dashboard-ui/e2e/abwab-archive.e2e.ts:111` |
| F07 | A | MED | Writer README claims `BulkMoveAsync` resolves the section **before** loading doors and calls the asymmetry load-bearing; both paths load doors first | `.../Persistence/Writes/Abwab/README.md:145` | `.../Writes/Abwab/EfAbwabDoorsWriter.cs:240` |
| F08 | E | MED | `shared/README.md` says `qd-context-menu` deliberately does **not** clamp to the viewport; slice L made it flip and clamp | `Frontend/.../src/app/shared/README.md:27` | `.../shared/ui/context-menu/context-menu.component.ts:101` |
| F09 | A | MED | abwab README says `excludedIds` **hides** a door; since 9aef279c it renders it as a disabled row at true depth | `Frontend/.../features/abwab/README.md:253` | `.../components/abwab-door-picker/abwab-door-picker.component.ts:118` |
| F10 | A | MED | API README says authentication runs **before** the rate limiter; the pipeline runs the limiter first, deliberately | `Backend/api/QuranDashboard.Api/README.md:31` | `.../Extensions/WebApplicationExtensions.cs:25` |
| F11 | E | MED | `design-preview/README.md` still reads as a pre-adoption proposal ("nothing was modified", "to reconcile at implementation time"); the green direction was adopted | `docs/design-preview/README.md:90` | `Frontend/quran-dashboard-ui/src/styles/_tokens.scss:31` |
| F12 | D | MED | Three long-lived docs cite `Frontend/quran-dashboard-ui/report/ui/real-pages-visual-system-extraction-report.md`; that directory does not exist | `PRODUCT.md:51` | `Frontend/quran-dashboard-ui/report/` — absent |
| F13 | A | MED | `design-preview/README.md`'s Files table omits six files, including the three abwab concept HTMLs other docs cite as governing design contracts | `docs/design-preview/README.md:13` | `docs/design-preview/abwab-tree-concept.html` |
| F14 | B | MED | Skills guide says `DESIGN.md` is "still a seed doc" whose header asks for regeneration; DESIGN.md has no such header and PRODUCT.md calls it the system of record | `SKILLS_AND_ARCHITECTURE_GUIDE.md:383` | `DESIGN.md:1-9` |
| F15 | B | MED | `speckit-analyze` makes the constitution non-negotiable with no escape hatch; `speckit-converge` handles the unfilled-template case — and the constitution *is* an unfilled template | `.claude/skills/speckit-analyze/SKILL.md:66` | `.specify/memory/constitution.md:1` |
| F16 | A | MED | `TESTING_STRATEGY.md` enumerates four frontend features in both §4's matrix and §6; there are five — `abwab` is missing from both | `TESTING_STRATEGY.md:301`, `:427` | `Frontend/.../src/app/features/abwab/` |
| F17 | B | MED | §6 says "all 20 Abwab tests pass repeatably"; `e2e/README.md` says 40, and the eight specs produce 40 | `TESTING_STRATEGY.md:463` | `e2e/README.md:78` |
| F18 | A | MED | Tier B's "be accurate about this" family list omits `Tests.Abwab` (kept by the filter) and includes `Tests.TestSupport.Logging` (no tests) | `TESTING_STRATEGY.md:152` | `Backend/tests/QuranDashboard.Tests/Abwab/AbwabTreeReadTests.cs:3` |
| F19 | A | MED | TESTING_DEBT says the door-modal spec runs "unchanged (11/11)"; it has 17 cases and three later commits edited it | `docs/TESTING_DEBT.md:63` | `.../abwab-door-modal/abwab-door-modal.component.spec.ts` (17 `it(`) |
| F20 | A | MED | `docs/README.md` says two feature folders are "currently buffered"; **19** tracked `docs/feature-*/` folders exist | `docs/README.md:34` | `git ls-files docs/` → 19 folders |
| F21 | A | MED | `Backend/report/README.md` says deleting the feature-008/009 folders "breaks" two importer verbs; both report writers `Directory.CreateDirectory` first | `Backend/report/README.md:37` | `.../Reports/Quran/DataPipelines/Translations/MarkdownJsonTranslationReportWriter.cs:19` |
| F22 | D | MED | DataImporter README points at a "Backend Reports and Import Sources" section of the **root** `AGENTS.md`; that heading exists only in `Backend/AGENTS.md` | `Backend/tools/QuranDashboard.DataImporter/README.md:8` | `Backend/AGENTS.md:53` |
| F23 | C | MED | `Backend/scripts/README.md`'s table documents 7 of 12 commands; the five omitted include `drop-db` and `reset-db`, whose fail-closed `--yes` gates are documented nowhere | `Backend/scripts/README.md:15` | `Backend/scripts/drop-db:7` |
| F24 | A | MED | `shared/README.md` says no call-site turns on `qd-state`'s `reserve`; seven templates pass `[reserve]="true"` | `Frontend/.../src/app/shared/README.md:40` | `.../abwab-sections-modal/abwab-sections-modal.component.html:22` |
| F25 | A | MED | §17's Chrome-inert rule says nine surfaces hold the scroll lock; twelve do, and `qd-confirm-dialog` — which every confirm now composes — is absent from the list | `.architecture/UI_STYLE_SYSTEM.md:1492` | `.../shared/ui/confirm-dialog/confirm-dialog.component.html:9` |
| F26 | A | MED | §4's z-scale is declared exhaustive ("no exceptions") and stops at `--qd-z-modal`; `--qd-z-nav-progress: 60` sits above it | `.architecture/UI_STYLE_SYSTEM.md:176` | `Frontend/quran-dashboard-ui/src/styles/_tokens.scss:218` |
| F27 | C | MED | Restore's `409` for a still-archived parent is in no README, though `Controllers/README.md` enumerates restore's other statuses | `Backend/api/QuranDashboard.Api/Controllers/README.md:28` | `.../Controllers/Abwab/AbwabDoorsController.cs:220` |
| F28 | C | MED | The reorder body's required `scope` field and its two distinct `400`s are in no backend README | `Backend/api/QuranDashboard.Api/Controllers/README.md:28` | `.../Controllers/Abwab/AbwabDoorsController.cs:124` |
| F29 | C | MED | `GET /api/access/me` can answer `409` on a provisioning email collision; no README documents that status | `Backend/api/QuranDashboard.Api/README.md:39` | `.../Middleware/GlobalExceptionHandler.cs:38` |
| F30 | D | LOW | ux-audit item 10 describes a `forceExpandedIds` input that 711dcb6d deleted (now `expandSeedIds`) | `docs/abwab-ux-audit.md:353` | `.../abwab-tree/abwab-tree.component.ts:67` |
| F31 | A | LOW | `Backend/report/README.md`'s "What lives here now" table omits `feature-abwab-global-order/` | `Backend/report/README.md:20` | `Backend/report/feature-abwab-global-order/001-…md` |
| F32 | A | LOW | §17 says `.qd-modal-backdrop` is the base for "all twelve modal consumers"; thirteen templates apply it | `.architecture/UI_STYLE_SYSTEM.md:1109` | `.../shared/ui/confirm-dialog/confirm-dialog.component.html:2` |
| F33 | A | LOW | §17's truncation entry cites `abwab-tree.component.scss:70-75` for `__name`; that rule is now at `:209` | `.architecture/UI_STYLE_SYSTEM.md:1265` | `.../abwab-tree/abwab-tree.component.scss:209` |
| F34 | A | LOW | `Controllers/README.md` says error codes are documented via XML `<response>` tags; no `.cs` file in the API carries any XML doc — the same file says so 11 lines earlier | `Backend/api/QuranDashboard.Api/Controllers/README.md:126` | `.../Controllers/**/*.cs` — zero `///` |
| F35 | A | LOW | `Controllers/README.md` places all four template-node writes under `template-nodes/{nodeId}`; the add is `POST templates/{templateId}/nodes` | `Backend/api/QuranDashboard.Api/Controllers/README.md:37` | `.../Controllers/Abwab/AbwabTemplateNodesController.cs:19` |
| F36 | E | LOW | ux-audit item 15 says a `[title]` is "missing at all 11 sites", including the exact line that gained one in ux-slice-d | `docs/abwab-ux-audit.md:528` | `.../abwab-archive-view/abwab-archive-view.component.html:27` |
| F37 | B | LOW | `docs/README.md` lists `performance-review/` among never-swept folders; `CLAUDE.md`'s never-deleted list omits it | `docs/README.md:31` | `CLAUDE.md:60-63` |
| F38 | A | LOW | Skills guide says "14 `speckit-*` skills"; `.claude/skills/` has 15 (`speckit-converge` is unmentioned) and `.agents/skills/` has 9 | `SKILLS_AND_ARCHITECTURE_GUIDE.md:377` | `.claude/skills/speckit-*` → 15 |
| F39 | A | LOW | ux-audit calls `feature-abwab-templates/plan.md` "the **open** feature named in the root `CLAUDE.md`"; CLAUDE.md says "None" | `docs/abwab-ux-audit.md:11` | `CLAUDE.md:206` |
| F40 | A | LOW | abwab README's status header still reads "Slice B2 complete"; twelve UX slices merged after it | `Frontend/.../features/abwab/README.md:6` | `git log` — slices A–M merged post-B2 |

---

## 3. Finding detail

### F01 · TYPE E · HIGH · The release gate was crossed and the doc still states it

**Doc claims** — `Frontend/quran-dashboard-ui/src/app/features/abwab/README.md:10-13`:

> The routes are `Open` (no auth) per `plan.md` §10 — **do not** include this feature in a
> `dev → main` release until write protection lands; that block now covers **seven** more
> write-capable routes …

**Code / repo reality.** Three independent facts:

1. **The feature is in production.** `origin/main` == `origin/dev` == `4f1ac91c`. Commit
   `b666cb38` is titled *"chore: trigger Railway redeploy of the abwab release"* and its body
   reads: *"PR #63 merged while Railway auto-deploy was disabled, so bc61bdaa never built and
   `/api/abwab/*` still 404s in production. The schema is already migrated (19 → 24); this empty
   commit only re-triggers the deploy."*
2. **Write protection never landed.** `Backend/api/QuranDashboard.Api/Extensions/WebApplicationExtensions.cs:27`
   calls `app.UseAuthorization()` with **no fallback policy**; the only `[Authorize]` in the API
   is `Controllers/Access/AccessController.cs:9`. `Authentication/README.md:50-52` states this
   correctly and currently: *"The named policies are registered for future admin surfaces but are
   **not applied to any endpoint** in this phase … today only `api/access/me`."*
   `SmokeRouteCatalog.cs:219` says the same: *"access/me is the tree's only `[Authorize]`
   endpoint"*. All **21 Abwab write routes** are anonymous.
3. **Rate limiting is not a substitute.** `RateLimiting/README.md` documents per-client-IP
   throttling only, and it *"ships disabled"* unless configuration turns it on.

**Which later change caused it.** `bc61bdaa` (PR #63, `dev → main`) followed by `b666cb38`.
Neither commit amended the README's gate, and no long-lived document records a decision to waive
it.

**Systemic consequence.** Five open rows in `docs/TESTING_DEBT.md` (rows 3, 8, F3, G3, I2) are
scheduled to be paid *"When write protection lands and `/api/abwab` stops being `Open`"*. That
trigger now sits behind a feature that is already live, so the smoke-coverage debt on 21
unauthenticated production write routes is deferred to an event that has been overtaken.

**Smallest correction — the user's call, not a documentation edit.** This is the one finding
where "which side changes" is a product decision:

- If the release was deliberate and the exposure is accepted → **the DOC changes**: replace the
  gate at `:11-12` with the accepted-risk record (what is live, what is unprotected, what would
  re-close it), and re-trigger the five TESTING_DEBT rows on something that can actually happen.
- If the release outran the gate → **the CODE changes**: attach an authorization policy to the
  Abwab write routes, at which point the README line becomes true again.

**Not fixed, per this sweep's read-only scope.** See Open Question 1.

---

### F02 · TYPE B · HIGH · `AGENTS.md` still declares an open Spec-Kit feature

**Doc A** — `AGENTS.md:206-207`:

> - Open: `abwab-doors-a` (Abwab doors & sections, Slice A — backend only). Plan:
>   `docs/feature-abwab-doors/plan.md`. Design contract: `docs/design-preview/abwab-tree-concept.html`.

**Doc B** — `CLAUDE.md:206`: `None.`

**Settled by.** `.specify/feature.json` → `{"feature_directory": ""}`; `specs/` contains only
`README.md`. The feature is closed.

The two files are **byte-identical everywhere else** (verified by a name-normalised `diff`: the
only other difference is the H1 of the frontend pair). They are deliberate mirrors for different
agent runtimes, and `specs/README.md:36-37` names both as needing the same update at feature
close: *"Update the folder charters … and the **Active Spec Kit Feature** section of `CLAUDE.md` /
`AGENTS.md`."*

**Why HIGH.** Any agent reading `AGENTS.md` (Codex, Cursor, the `.agents/skills/` runtime) is told
that `docs/feature-abwab-doors/plan.md` is a **live planning input**. That plan is precisely where
the pre-reversal decisions live — including §10's release gate (F01) and the pre-ux-slice-g apply
semantics (F03). This finding is the delivery mechanism for the other two.

**Correction: DOC.** Replace `AGENTS.md:206-207` with `None.`, matching `CLAUDE.md:206`.

---

### F03 · TYPE E · HIGH · `Controllers/README.md` still documents root-inclusive apply

**Doc claims** — `Backend/api/QuranDashboard.Api/Controllers/README.md:39-43`:

> The apply copies the **template subtree as a new child** of each target door and is
> all-or-nothing … a target that already has a live child named like the **template root** fails
> the whole batch with one `409` naming every colliding target.

**Code does** — `.../Persistence/Writes/Abwab/EfAbwabTemplateApplyWriter.cs:118-130`: the writer
iterates `rootChildren` and inserts **each of the root's direct children** as a new child of every
target at `nextOrder + i`, then recursively copies each child's subtree. The root itself is never
inserted; the response is N created doors per target.

**Caused by.** `9158d584 feat(ux-slice-g): copy the template root's children, never its root`.
That slice's doc sweep updated `Persistence/Writes/Abwab/README.md:221`, the frontend
`features/abwab/README.md:866`, and `abwab.labels.ts:416` (all verified correct — see §4), but
never this file. It is the last surviving statement of the reversed axiom in a long-lived doc.

**Also stale in the same paragraph.** The `409` is raised on the root's **direct-child** names
(`:85` builds `rootChildNames` from `rootChildren`), and
`AbwabTemplateApplyCollisionException` carries `(targetName, childName)` **pairs**, not a list of
targets. The paragraph also omits the third refusal entirely: an empty-root template is a distinct
`400` raised before any target row is read (`:60`).

**Correction: DOC.** Rewrite `:40` to "copies the template root's **direct children** as new
children of each target (N created doors per target, each with its own subtree), never the root
itself"; rewrite `:43` to name every colliding **(target, child)** pair; add the empty-root `400`.

---

### F05 · TYPE E · MEDIUM · The reveal no longer clears `modal`, but line 436 still says it does

**Doc claims** — `features/abwab/README.md:436-441`:

> **Reveal-in-tree writes the keys above, and the only thing it does to `modal` is clear it.**
> … `modal: null` always, because the seventh key carries no id of its own … retaining
> `relations-closed` across a reveal would offer to reopen the **target's** relations …

**Code does** — `pages/abwab-page/abwab-page.component.ts:524`:

```ts
modal: anchorId === null ? null : { kind: 'relations' as const, closed: true, subjectDoorId: anchorId },
```

It **retains** the key with the source door pinned. The code's own comment at `:521` says *"That
ambiguity is why the reveal **used to** discard the key outright."*

**Caused by.** `b2771c61 feat(abwab): the reveal retains the relations modal — restore reopens the
source`. `git show b2771c61 -- .../abwab/README.md` shows that commit rewrote the neighbouring
URL-contract and history paragraphs — the same README states the *current* truth 65 lines later at
`:501` (*"Since ux-slice-l the reveal retains rather than discards"*) — but never touched `:436-441`.

**Why it matters more than a stale sentence.** The paragraph does not merely lag; it argues *for*
the reversed behavior, so a reader taking it at face value would "fix" the shipped code back.

**Correction: DOC.** Rewrite `:436-441` to say the reveal **rewrites** `modal` (retaining
`relations-<id>-closed` with the source anchor; `null` only when there is no anchor), and replace
the obsolete justification with a pointer to the `relations-<id>-closed` section at `:387`.

---

### F06 · TYPE E · MEDIUM · "the detach announcement" names a removed behavior

**Doc claims** — `features/abwab/README.md:938`: the e2e suite drives *"the parent-must-restore-first
rule and **the detach announcement**"*.

**Code does.** No detach announcement exists. `abwab-archive.e2e.ts:111` is
`test('restoring a door whose section was deleted meanwhile demands a destination', …)`;
`grep -n detach` over that file returns nothing. `ABWAB_LABELS.restoreAnnouncement`
(`abwab.labels.ts:231`) is the single string for every case, and `abwab.labels.spec.ts:14` states
*"No detach to announce any more"*. Backend-side, `AbwabRestoredDoorDto` and
`detachedFromArchivedSection` no longer exist; `EfAbwabDoorsWriter.ResolveRestoreSectionAsync:533-535`
throws `AbwabSectionRequiredException` (400) instead of detaching.

**Caused by.** `d90c65f3` (frontend/e2e removal) on top of `3612ea71` (the backend contract change).
The same README documents the current behavior correctly at `:211-213`; `:938` is the single
survivor.

**Correction: DOC.** Replace "the detach announcement" with "the retired-section restore that
demands a destination".

---

### F07 · TYPE A · MEDIUM · A documented "load-bearing" ordering that does not exist

**Doc claims** — `.../Persistence/Writes/Abwab/README.md:144-145`:

> Check ORDER differs on purpose and is load-bearing: `MoveAsync` loads the door first (unknown id
> stays a `404`), while `BulkMoveAsync` **resolves the target first** (request-shape validation
> before entity checks).

**Code does** — `EfAbwabDoorsWriter.cs`:

| Path | loads doors | resolves section |
|---|---|---|
| `MoveAsync` | `:97` | `:103` |
| `BulkMoveAsync` | `:231-234`, throws `AbwabNotFoundException` at `:237` | `:240` |

**Both are entity-check-first.** The documented asymmetry does not exist. Consequence: a bulk move
to root scope that names an unknown door **and** omits `targetSectionId` answers `404`
(`AbwabNotFoundException`), not the `400` the doc implies; the section-required rejection does not
fire *"regardless of what the batch names"*.

**Propagated into two test comments** — the same false premise appears at
`Backend/tests/QuranDashboard.Tests/Abwab/AbwabDoorWriteBehaviorTests.cs:332-333` and
`Backend/tests/QuranDashboard.Tests/Smoke/SmokeAbwabWriteTests.cs:771`. The behavior test at
`:335` names two valid live doors, so it never discriminates the order and cannot catch this.

**Which side should change: UNCLEAR — a design call.** If "request-shape validation before entity
checks" is the intended contract, the **CODE** should reorder `BulkMoveAsync`. If entity-first is
correct, **three docs** (the README plus both test comments) should change and the "differs on
purpose and is load-bearing" framing at `:144` should be dropped. No test currently pins either.

---

### F08 · TYPE E · MEDIUM · `qd-context-menu` clamps now; `shared/README.md` says it deliberately does not

**Doc claims** — `src/app/shared/README.md:26-28`:

> Deliberately does **not** clamp to the viewport (positions from the caller's raw pointer coords,
> matching both prior copies) …

**Code does** — `shared/ui/context-menu/context-menu.component.ts`: measures its own box
(`getBoundingClientRect`), resolves direction from `closest('[dir]')`, flips on viewport collision
(`:89`, `:96`), then clamps both axes (`:101-102`):

```ts
left: clamp(left, VIEWPORT_MARGIN, viewportWidth - width - VIEWPORT_MARGIN),
top:  clamp(top,  VIEWPORT_MARGIN, viewportHeight - height - VIEWPORT_MARGIN),
```

**Caused by.** `c5466811 fix(ui): the context menu opens toward inline-start and flips at the
viewport`. §17's registry entry was updated; `shared/README.md` was not.

**Correction: DOC.** Replace the clause with the slice-L placement contract (extends toward
inline-start, flips at either edge, clamps to an 8 px margin).

---

### F09 · TYPE A · MEDIUM · `excludedIds` disables rather than hides

**Doc claims** — `features/abwab/README.md:253`: *"`excludedIds` **hides** a door **without** hiding
its subtree"*.

**Code does** — `abwab-door-picker.component.ts:113,118` pushes **every** walked node including
excluded ones, tagged `isExcluded`; the template renders that row with no pick control plus an
`excludedTag` chip (`.html:61-62`), and `togglePicked` refuses it (`:166`). The class doc at
`:39-45` states the current behavior correctly.

**Caused by.** `9aef279c feat(abwab): render an excluded door as disabled context, its subtree
indented and collapsible`.

**Correction: DOC.** "`excludedIds` **disables** a door without hiding it or its subtree — it
renders as a non-selectable row at its true depth, `excludedTag` naming why."

---

### F10 · TYPE A · MEDIUM · Middleware order stated backwards

**Doc claims** — `Backend/api/QuranDashboard.Api/README.md:30-31`: `UseAuthentication` /
`UseAuthorization` run *"(pipeline, after CORS, **before the rate limiter**)"*.

**Code does** — `Extensions/WebApplicationExtensions.cs`: `UseCors` → `UseRateLimiter()` (`:25`) →
`UseAuthentication()` (`:26`) → `UseAuthorization()` (`:27`). The limiter runs **first**, and the
inline comment at `:21-24` says that is deliberate (*"keys per-client-IP, not per-user, so it
belongs pre-auth"*).

**Correction: DOC.** Change "before the rate limiter" to "after the rate limiter" at `:31`.

---

### F11 · TYPE E · MEDIUM · The design-preview charter still reads as a pre-adoption proposal

**Doc claims** — `docs/design-preview/README.md`:

- `:9` — *"These files are review artifacts only. **Nothing under the Angular app was modified.**"*
- `:90` — *"## Divergences … **(to reconcile at implementation time)**"*
- `:92-94` — *"**The current docs lock a navy + gold + parchment identity** … adopting them means
  updating the docs on every point below"*
- `:124-126` — item 12: PRODUCT.md's Visual Identity section *"**would be superseded** by this
  direction"*

**Code / docs reality.** The direction was adopted:

- `Frontend/quran-dashboard-ui/src/styles/_tokens.scss:31` — `--qd-accent: oklch(0.490 0.068 176.3)`
  (green) for light; `_themes.scss:29` keeps gold for dark.
- `UI_STYLE_SYSTEM.md:397` — §15 is titled *"(Navy + Gold + Parchment — **superseded**)"*;
  §16.3 is *"The allowed-**green** list (locked)"*.
- `PRODUCT.md:40-52` — green is *"The official visual identity"*, navy+gold marked
  *"**Superseded (historical)**"*.

**Why it is more than staleness.** The pointers are circular. `PRODUCT.md:41-42` sends readers to
this README *"(read its `README.md`; the divergence list there is the record of what changed)"*,
and `DESIGN.md:2-3` calls it the record of *"the divergences that were **reconciled** into this
document"* — yet the README they arrive at says the reconciliation has not happened and that the
current docs lock navy+gold. Two live docs cite a third that contradicts them both.

**Correction: DOC.** Re-tense `:9`, `:90` and `:92-94` to past ("adopted in …; each point below was
reconciled"), and mark item 12 as done rather than conditional. `docs/design-preview/` is a
never-swept live folder (`CLAUDE.md:62`), so this file will keep being read.

---

### F12 · TYPE D · MEDIUM · A three-way dangling reference

**Docs claim** — all three cite the same path:

- `PRODUCT.md:51` — *"Its extraction report remains at `Frontend/quran-dashboard-ui/report/ui/real-pages-visual-system-extraction-report.md` as historical reference."*
- `DESIGN.md:5`
- `UI_STYLE_SYSTEM.md:413`

**Reality.** `Frontend/quran-dashboard-ui/report/` **does not exist** (`ls` → No such file or
directory; `git ls-files` → nothing). Root `CLAUDE.md` is explicit that *"Dangling links are a
defect, not an acceptable cost."*

**Correction: DOC.** Either drop the three pointers, or replace them with the git ref where the
report last existed. UI_STYLE_SYSTEM §15 is the one that most needs it — it tells the reader to
read that file for the superseded contract.

---

### F13 · TYPE A · MEDIUM · The design-preview Files table omits its own abwab contracts

**Doc claims** — `docs/design-preview/README.md:13-23` presents a "## Files" table of eight
entries (`design-language`, `mushaf`, `roots`, `lemmas`, `stems`, `unique-words`, `word-types`,
`assets/`, `fonts/`).

**Reality.** The folder also contains `abwab-tree-concept.html`, `abwab-relations-concept.html`,
`abwab-templates-concept.html`, `decisions.html` and `words-pages-hero.html`. The three abwab
concepts are cited **as governing design contracts** across the codebase —
`features/abwab/README.md:960-963`, `abwab.labels.ts:254`, `abwab-tree.component.scss:263`,
`SmokeRouteCatalog`-adjacent design notes, and the ux audit's scope statement.

**Correction: DOC.** Add the five missing rows. While doing so, note the two superseded lines the
comps still carry (see §4's vocabulary entry): the relations concept's «أعم / أخص» and the
templates concept's «كاملًا بجذره».

---

### F14 · TYPE B · MEDIUM · Skills guide contradicts DESIGN.md about DESIGN.md

**Doc A** — `SKILLS_AND_ARCHITECTURE_GUIDE.md:383`: *"`DESIGN.md` is still a **seed** doc — its
header notes it should be regenerated (`/impeccable document`) once there is real UI code … Until
then, `UI_STYLE_SYSTEM.md` is the operative styling source."*

**Doc B / reality.** `DESIGN.md:1-9`'s header contains no such note —
`grep -i "seed\|regenerat\|impeccable document" DESIGN.md` returns nothing. It reads *"Visual
source of truth: the approved flat parchment + green comps"*. And `PRODUCT.md:43-44` states
*"`DESIGN.md` is the design system of record, with the token contract in … `UI_STYLE_SYSTEM.md`"*.

**Correction: DOC.** Delete or rewrite `SKILLS_AND_ARCHITECTURE_GUIDE.md:383`. (Its neighbouring
claim at `:384`, "No workspace-root `README.md`", **is** still accurate.)

---

### F15 · TYPE B · MEDIUM · Two skills disagree on the unfilled constitution

**Doc A** — `.claude/skills/speckit-analyze/SKILL.md:66`: *"The project constitution
(`.specify/memory/constitution.md`) is **non-negotiable** … Constitution conflicts are
automatically CRITICAL"*, with no unfilled-template branch anywhere in the file.

**Doc B** — `.claude/skills/speckit-converge/SKILL.md:92-93`: *"**If the constitution is an
unfilled template, skip constitution checks gracefully rather than failing.**"*

**Settled by code.** `.specify/memory/constitution.md` **is** an unfilled template: 18 literal
placeholders including `[PROJECT_NAME]`, `[PRINCIPLE_1_NAME]` … and the footer
`**Version**: [CONSTITUTION_VERSION] | **Ratified**: [RATIFICATION_DATE]`.

**Consequence.** `/speckit-analyze` run today would validate a spec against placeholder principles
and is instructed to raise any conflict as automatically CRITICAL. `/speckit-converge` would not.

**Correction: DOC (or the constitution).** Port converge's unfilled-template escape hatch into
`speckit-analyze/SKILL.md:66` — or fill in the constitution, which makes both skills correct.
Note `.agents/skills/speckit-analyze/SKILL.md` carries the same text and needs the same change.

---

### F16 · TYPE A · MEDIUM · `abwab` is missing from TESTING_STRATEGY's frontend feature list

**Doc claims** — two places:

- `TESTING_STRATEGY.md:301` (§4 change-to-tier matrix): `| Frontend feature only (`words`, `mushaf`, `auth`, `dashboard`) | A | C | No |`
- `TESTING_STRATEGY.md:427` (§6): *"The frontend features are `auth`, `dashboard`, `mushaf`, and `words`"*

**Code does.** `ls src/app/features/` → **abwab**, auth, dashboard, mushaf, words. `abwab/` is the
largest frontend feature in the tree (routes, 17 components, its own `state/` and `data-access/`,
~27 spec files) and is the only feature `docs/TESTING_DEBT.md` records debt for.

**Consequence.** An agent deriving a Tier A `--include` glob or a matrix row for an abwab-only
frontend change finds no entry, on the document `Frontend/CLAUDE.md` names as the single source of
truth for which tests to run.

**Correction: DOC.** Add `abwab` to both lines.

---

### F17 · TYPE B · MEDIUM · 20 vs 40 Abwab e2e tests

**Doc A** — `TESTING_STRATEGY.md:463`: *"all **20** Abwab tests pass repeatably"*.
**Doc B** — `Frontend/quran-dashboard-ui/e2e/README.md:78`: *"all **40** Abwab tests pass
repeatably"*, with a measured note at `:83-84` (*"Measured 2026-08-02: 68 passed — default 28 …
abwab 40"*).

**Settled by code.** The `abwab` Playwright project matches the eight `abwab-*.e2e.ts` specs
(`playwright.config.ts:38`). Statically declared cases total 28; `abwab-tree-row-budget.e2e.ts:15`
generates 3 viewports × 2 themes = 6, and `abwab-slice-j-widths.e2e.ts` generates the rest — 40.

**Correction: DOC.** Change `TESTING_STRATEGY.md:463` to 40. (The e2e README is the accurate side
and carries the measurement date.)

---

### F18 · TYPE A · MEDIUM · Tier B's family list is wrong in both directions

**Doc claims** — `TESTING_STRATEGY.md:149-152` enumerates what the no-pipeline filter keeps,
ending *"… `Tests.TestSupport.Logging`. It excludes the ten pipeline namespaces"*, and the
surrounding prose asks the reader to "be accurate about this".

**Code does.** `namespace QuranDashboard.Tests.Abwab;`
(`Backend/tests/QuranDashboard.Tests/Abwab/AbwabTreeReadTests.cs:3`) is matched by **none** of the
filter's `!~` exclusion terms, so Tier B/C keeps it — yet it is absent from the enumeration.
Conversely `Backend/tests/QuranDashboard.Tests/TestSupport/` holds only helper types and contains
no tests, so listing `Tests.TestSupport.Logging` as a kept *family* is misleading.

**Correction: DOC.** Add `Tests.Abwab`; drop `Tests.TestSupport.Logging` or mark it as a
test-support namespace with no tests.

---

### F19 · TYPE A · MEDIUM · "unchanged (11/11)" no longer holds

**Doc claims** — `docs/TESTING_DEBT.md:62-64`: the `abwab-door-fields-form` extraction needs no
spec of its own because `abwab-door-modal.component.spec.ts` runs green **unchanged (11/11)**.

**Code does.** That spec now contains **17** `it(` blocks, and `git log` shows three commits after
the extraction touched it: `d90c65f3`, `7249dd58`, `61168e7c`. The stated premise — that the spec
remains untouched — no longer holds, so the "not debt, and not deferrable" conclusion rests on a
false basis even if the conclusion itself is still reasonable.

**Correction: DOC.** Drop "unchanged (11/11)" and restate the reason as "the door-modal spec still
exercises the extracted fields through the `testIdPrefix` data-testids", or re-measure.

---

### F20 · TYPE A · MEDIUM · The buffered-features line is 17 folders out of date

**Doc claims** — `docs/README.md:34-35`: *"Currently buffered: `feature-033-auth-roles-permissions/`
(closed 2026-07-19) and `feature-032-rate-limiting/` (closed 2026-07-18)."*

**Reality.** `git ls-files docs/` shows **19** tracked `docs/feature-*/` folders — the two named
plus `feature-abwab-doors`, `-global-order`, `-mandatory-section`, `-relations`, `-templates`, and
`feature-ux-slice-a` through `-l`. A twentieth, `feature-abwab-legacy-seed/`, exists on disk as an
empty untracked directory. `CLAUDE.md:56-58` sets the N-2 rule: *"Keep the planning artifacts of
the two most recently closed features … plus every currently open feature"*, and `CLAUDE.md:206`
says no feature is open.

> **Count correction.** A subagent reported "fourteen"; direct measurement gives **19** tracked
> folders (20 directories on disk). This report uses the measured figure.

**Correction: DOC — and note the scope boundary.** The cleanup itself is explicitly out of this
sweep's scope. The doc-level fix is to rewrite `:34-35` to name the genuinely most-recent closures
(or to state that the un-numbered `feature-<name>/` folders sit outside the sweep, if that is the
intent). See Open Question 3.

---

### F21 · TYPE A · MEDIUM · The feature-008/009 exemption rests on a false causal claim

**Doc claims** — `Backend/report/README.md:35-37`: the folders are permanently exempt because
`DataImporterDefaults.cs` *"hardcodes both directories as the importers' default output targets —
deleting them **breaks** `import-translations` and `import-navigation-metadata`."*

**Code does.** `MarkdownJsonTranslationReportWriter.cs:19` calls `Directory.CreateDirectory(outputDir)`
before writing, and `MarkdownJsonNavigationMetadataReportWriter.cs:19` does the same. Neither
handler guards on directory existence (`ImportTranslationsHandler.ResolveReportOutDir:208-216`,
`ImportNavigationMetadataHandler.ResolveReportOutDir:222-230` only reject null/whitespace). Nothing
breaks; the directories are recreated.

**Correction: DOC.** Replace the causal claim: deletion loses historical evidence rather than
breaking a verb. The second bullet (source-verification / provenance evidence) already carries the
real exemption ground and is unaffected.

---

### F22 · TYPE D · MEDIUM · A cross-file pointer to a heading that is in the other AGENTS.md

**Doc claims** — `Backend/tools/QuranDashboard.DataImporter/README.md:8`: *"rules: root `AGENTS.md`
→ "Backend Reports and Import Sources""*.

**Reality.** Root `/AGENTS.md` has eleven headings and none is named that. The heading exists only
at `Backend/AGENTS.md:53` (and its `Backend/CLAUDE.md` mirror).

**Correction: DOC.** Change "root `AGENTS.md`" to "`Backend/AGENTS.md`".

---

### F23 · TYPE C · MEDIUM · Five scripts undocumented, two of them destructive

**Doc claims** — `Backend/scripts/README.md:7-15` presents a "## Commands" table. It documents
seven: `qd-build`, `qd-api`, `qd-ui`, `export-swagger`, `check-api-contract`, `create-smoke-dump`,
`wipe-abwab`.

**Reality.** `Backend/scripts/` contains twelve commands plus one internal helper
(`_preflight-sandbox.sh`). Undocumented: **`add-mig`, `clean-local-build`, `drop-db`, `reset-db`,
`update-db`**. Two of these are destructive and carry fail-closed gates that appear in no doc —
`drop-db:7` is `if [[ $# -ne 1 ]] || [[ "$1" != "--yes" ]]; then`, guarding
`dotnet ef database drop --force`; `reset-db` has the same gate.

> **Count correction.** A subagent reported four omissions; direct measurement gives **five**
> (`clean-local-build` was missed).

**Correction: DOC.** Add five rows, marking `drop-db` and `reset-db` destructive and `--yes`-gated.

---

### F24 · TYPE A · MEDIUM · `qd-state`'s `reserve` has seven consumers, not zero

**Doc claims** — `src/app/shared/README.md:40`: *"no current call-site turns it on"*.

**Code does.** Seven templates pass `[reserve]="true"`: `abwab-sections-modal:22`,
`abwab-template-copy-modal:23`, `abwab-relations-modal:36`, `abwab-door-fields-form:2`,
`abwab-door-picker:80`, `abwab-templates-page:30` and `:81`, `abwab-page:120`.

**Correction: DOC.** Name the abwab consumers and point at §17's `reserve`-under-`@if` note.

---

### F25 · TYPE A · MEDIUM · The Chrome-inert blast radius is twelve, and omits the confirm primitive

**Doc claims** — `UI_STYLE_SYSTEM.md:1492`: *"Blast radius: **nine** surfaces"*.

**Code does.** `confirm-dialog.component.html:9` applies `qdModalScrollLock`, so **every**
`qd-confirm-dialog` acquires the lock — and `top-navbar.component.ts:29` /
`top-navbar.component.html:5` make the navbar inert for any lock holder. Twelve templates now carry
the directive.

**Caused by.** `c48d6e08` (the primitive) + `15dc38ed` (five confirms migrated onto it) +
`524c59cf` (the relation-delete confirm). The registry entry was not re-counted.

**Correction: DOC.** Update to twelve and add `qd-confirm-dialog` to the enumerated radius. The
same stale sentence is mirrored at `src/app/shared/README.md:85` ("Nine surfaces hold the lock as
of this phase") and needs the same change.

---

### F26 · TYPE A · MEDIUM · The "exhaustive" z-scale stops one rung short

**Doc claims** — `UI_STYLE_SYSTEM.md` §4's ascending layer scale ends at `--qd-z-modal`
(`:176`, *"a future direct modal-box consumer"*), under an assertion that *"There are no
exceptions: every stacking layer in the app resolves through this scale."*

**Code does** — `src/styles/_tokens.scss:218`: `--qd-z-nav-progress: 60;`, above
`--qd-z-modal: 51`, consumed by `qd-nav-progress`. The token file's own comment at `:189` already
lists `modal-backdrop < modal < nav-progress`, and §17's `qd-nav-progress` entry (`:1564`) calls it
*"top of the layer scale"* — so §4 is the only place that is wrong.

**Caused by.** `4f1ac91c feat(shell): … nav progress bar + idle route preload`.

**Correction: DOC.** Append `--qd-z-nav-progress` to §4's list.

---

### F27 · TYPE C · MEDIUM · Restore's `409` for a still-archived parent is undocumented

**Code implements** — `AbwabDoorsController.cs:220` maps `RestoreDoorOutcome.ParentStillArchived`
to `Conflict(ApiMessages.AbwabDoorParentStillArchived)` — HTTP `409` with «لا يمكن استعادة الباب
لأن الباب الأب ما زال مؤرشفًا» (`ApiMessages.cs:141`). The throw is `EfAbwabDoorsWriter.cs:420`.

**No README describes it**, although `Controllers/README.md:24-28` enumerates restore's other
failure statuses in detail (the `400` for a retired section, the `404` for a section that no longer
exists). The frontend README covers the *UI* rule (`:154-155`, «استرجع الأب أولًا») but not the
wire status.

**Correction: DOC.** Add one clause to the restore sentence in `Controllers/README.md`.

---

### F28 · TYPE C · MEDIUM · The reorder `scope` field and its two `400`s are undocumented

**Code implements** — `POST api/abwab/doors/{id}/order` takes a required numeric `scope`
(`AbwabReorderScope.Section = 1`, `Global = 2`, `AbwabDoorsController.cs:124`). An omitted or
unrecognised value deserialises to `0` and is refused by the `Enum.IsDefined` guard at `:110` →
`400` «نطاق الترتيب غير صالح». A `Global` scope on a **nested** door is a separate `400`
(`AbwabScopeNotApplicableException`).

**No backend README describes the field or either refusal.** The frontend README covers the
concept thoroughly (`:638-647`) and names `ABWAB_ORDER_SCOPE_TO_WIRE` as the single mapping point,
but the wire contract itself is undocumented on the backend side, where `Controllers/README.md`
otherwise enumerates Abwab statuses exhaustively.

**Correction: DOC.** One sentence in `Controllers/README.md`'s Abwab paragraph.

---

### F29 · TYPE C · MEDIUM · `GET /api/access/me` can answer `409`, documented nowhere

**Code implements** — `GlobalExceptionHandler.cs:24,38` special-cases
`UserProvisioningEmailConflictException` and writes `409 Conflict` with
`ApiResponse<object>.Fail(ApiMessages.EmailAlreadyRegistered)`. It is the **only** non-500 status
the global handler produces, and the only path that can raise it is provisioning inside
`GET /api/access/me`.

**Doc gap** — `Backend/api/QuranDashboard.Api/README.md:39` documents the `401` for a
missing/invalid token and stops there; `Authentication/README.md` documents the `401` envelope
only.

**Correction: DOC.** One sentence near `README.md:39`.

---

### LOW findings (F30–F40) — condensed

| ID | Doc | Code / reality | Correction |
|---|---|---|---|
| **F30** (D) | `docs/abwab-ux-audit.md:353` — *"`AbwabTreeComponent` has `forceExpandedIds`"* (also `:356`, `:372`) | The input is `expandSeedIds` (`abwab-tree.component.ts:67`); `forceExpandedIds` survives only in two comments saying it *"is gone"*. `:372`'s fix recipe additionally prescribes the force semantics the shipped code deliberately rejected. Caused by `711dcb6d` | DOC |
| **F31** (A) | `Backend/report/README.md:20` — the "What lives here now" table ends at `feature-009-…` | `Backend/report/feature-abwab-global-order/` exists with five tracked reports (`001-` … `005-`). `specs/README.md:36` makes this table a close-out obligation | DOC |
| **F32** (A) | `UI_STYLE_SYSTEM.md:1109` — *"shared base for all twelve modal consumers"* | Thirteen templates apply `.qd-modal-backdrop`; `confirm-dialog.component.html:2` is the thirteenth | DOC |
| **F33** (A) | `UI_STYLE_SYSTEM.md:1265` — cites `abwab-tree.component.scss:70-75` for `__name` | `.abwab-tree__name` now begins at `:209`; `:66-80` holds `__lead` and a trailing-inset rule. The rule was at `:82`, then `:114`, then `:209` as slices pushed it down | DOC — cite the selector, not the line range |
| **F34** (A) | `Controllers/README.md:126` — *"today error codes are documented via XML `<response>` tags only"* | `grep -rn "///" Backend/api/…/Controllers/` returns zero hits in any `.cs`; the same file says so at `:115-116`. Error codes are documented nowhere in the exported spec | DOC |
| **F35** (A) | `Controllers/README.md:37` — the four node writes sit *"under `api/abwab/template-nodes/{nodeId}`"* | Only three do; the add is `[HttpPost("templates/{templateId:int}/nodes")]` (`AbwabTemplateNodesController.cs:19`) | DOC |
| **F36** (E) | `docs/abwab-ux-audit.md:528` — a `[title]` *"is missing at all 11 sites"* | `abwab-archive-view.component.html:27` — the exact line the audit cites at `:530` — now carries `[title]="node.name"`, as do the move-picker sites at `:531`. Landed in ux-slice-d (`35de4bcc`) | DOC |
| **F37** (B) | `docs/README.md:31-32` lists `performance-review/` among never-swept folders | `CLAUDE.md:60-63`'s "Never deleted by this rule" list names `Backend/report/architecture/`, `report/database/`, `report/database-inventory/`, `docs/contracts/`, `docs/api-reference/`, `docs/deployment-railway/`, `docs/design-preview/` and three READMEs — **not** `performance-review/` | DOC — pick one list and make the other defer to it |
| **F38** (A) | `SKILLS_AND_ARCHITECTURE_GUIDE.md:223`, `:377` — *"14 `speckit-*` skills"* | `.claude/skills/speckit-*` → **15** (`speckit-converge` appears nowhere in the guide); `.agents/skills/speckit-*` → **9**. The two skill trees have drifted apart | DOC |
| **F39** (A) | `docs/abwab-ux-audit.md:11` (and `:728`) — `docs/feature-abwab-templates/plan.md` is *"the **open** feature named in the root `CLAUDE.md`, so a live input, not an archive"* | `CLAUDE.md:206` → `None.` The audit's authority framing for its single most-cited source no longer holds — see also F02 | DOC |
| **F40** (A) | `features/abwab/README.md:6` — *"**Status: Slice B2 complete**"* | Twelve UX slices (A–M) plus the nav-progress fix merged after B2. The line does enumerate global order / relations / templates, but not the slice series that rewrote search, reveal, the move picker, the confirms and the menu | DOC |

---

## 4. Verified clean — seed items that checked out

These were checked on both sides and found **consistent**. An explicitly verified seed is a result.

**The deleted Abwab legacy seeder — fully cleaned.** A repo-wide grep (code, tests, scripts,
`package.json`, `.csproj`, docs, READMEs, `.specify/`, `.claude/skills/`, `.agents/skills/`) for
`legacy-seed`, `legacyseed`, `seed-abwab`, `seedabwab`, `"legacy seed"`, `id-map`, `idmap`,
`id_map`, `AbwabLegacy`, `LegacyAbwab`, `SeedAbwab`, `VerifyAbwab`, `verify-abwab`, `abwab-legacy`
and `legacy_seed` returns **zero real hits**. The two matches are false positives: an `IdMapping`
helper in a Quran determinism test (`DisplayWordsDeterministicIdTests.cs`) and one `id-map` token
inside the minified Redoc vendor bundle in `docs/api-reference/index.html`. No surviving verb,
runbook, script or README reference.

**Route/contract parity — exact, both directions.** A mechanical diff of every `[Route]` /
`[Http*]` attribute under `Backend/api/…/Controllers/**` against `SmokeRouteCatalog.Routes`
produced **no** catalog-without-endpoint and **no** endpoint-without-catalog entries.
`SmokeCoverageParityTests.cs` asserts both directions by name and keys on method + template with
constraints included, so this is machine-enforced rather than merely currently true.

**`wipe-abwab`'s six-table allowlist.** `Backend/scripts/README.md:59-61` names
`abwab_sections`, `abwab_doors`, `abwab_door_aliases`, `abwab_door_relations`, `abwab_templates`,
`abwab_template_nodes`. The script lists exactly those six (`wipe-abwab:13-18`), and a grep of
every `abwab_*` table name in the EF configurations and entities yields exactly that set — the
`CASCADE` closure argument in the README holds.

**Templates apply — children-only, and the empty-root `400`.**
`Writes/Abwab/README.md:221` (*"copies the root's DIRECT CHILDREN — never the root itself"*),
`:248-250` (*"an empty-root template … is refused a third, distinct `400` before any target is
read"*), `features/abwab/README.md:866-872`, and `abwab.labels.ts:416`
(«عناصر القالب (بدون جذره)») all match `EfAbwabTemplateApplyWriter.cs:60` and `:126`. Every copied
door at every depth inherits the target's section. *(The one exception is
`Controllers/README.md` — F03.)*

**Sections — all six claimed invariants hold.** (1) every door carries a section
(`AbwabDoor.cs:11`, non-nullable, every resolver returns non-nullable `int`); (2) `section_id` is
`NOT NULL` (`AbwabDoorConfiguration.cs:16-18` + migration `20260802062011_RequireAbwabDoorSection`);
(3) the null-section rejection is **root-scope only** while DTOs stay nullable and a child derives
from its parent (`ResolveCreateSectionAsync:552-575`); (4) section delete is `409` while live
children remain and `204` once empty (`EfAbwabSectionsWriter.DeleteAsync:59-64`,
`AbwabSectionsController.cs:69-73`); (5) restore demands a destination only for a **root** whose
section was retired — a child derives from its live parent (`ResolveRestoreSectionAsync:506-538`);
(6) re-sectioning cascades to descendants **including archived rows**
(`CascadeSectionToDescendantsAsync:589-615` has no `DeletedAtUtc == null` filter, unlike its
siblings at `:455` and `:682`).

**Relations cache identity and the zero-count short-circuit.** The client cache is keyed on the
tree-snapshot ETag exposed as `snapshotValidator`
(`abwab-relations.controller.ts:42-43,62` ← `abwab-snapshot.facade.ts:74`), **not** on
`AbwabTreeDto.version`, which both READMEs document as diagnostics-only and factually blind to
relation writes. Invalidation is all-or-nothing on validator change; a null validator serves
nothing; a `304` and a failed refresh both keep the map. A zero `relationCount` issues **no
request** (`abwab-relations-modal.component.ts:294`, inside an `untracked` block), asserted in a
browser by `e2e/abwab-relations.e2e.ts:13-20` with a passive request counter. Every writer that can
move a relation list bumps the generation — `InvalidatingAbwabRelationsWriter.cs:30,42` and all
eight methods of `InvalidatingAbwabDoorsWriter.cs`, including rename.

**The tree-snapshot ETag constraint IS written down — and the premise was stricter than assumed.**
`Persistence/Reads/Abwab/README.md:165-176` states both the constraint (*"correct for a single
backend instance only"*) **and** the migration path (*"move the generation to shared state bumped
inside the write transaction … behind the existing `IAbwabCacheInvalidator` / `IAbwabCacheValidators`
interfaces"*), cross-referenced as a pointer from `API_GUIDELINES.md:164-167`. It is registered as
a singleton (`AbwabDependencyInjection.cs:19-21`). Correction to the seed's premise: the ETag is
**not** a bare counter that "resets on restart" — `AbwabCacheGeneration.cs:11` mixes in a
per-process boot id, so cross-restart equality is impossible and a restart costs one refetch,
never a stale `304`. The single-instance constraint comes from multi-instance divergence, not from
restart. No long-lived doc implies horizontal scaling is safe.

**Search / reveal / `q` — the ux-slice-l reversal is documented correctly.** `q` is **not** touched
by a reveal (`features/abwab/README.md:449` vs `onRevealRequested`'s patch, which carries no `q`
key). The tree **marks** and hides nothing while cards and archive still **filter** via
`pruneAbwabNodesToVisible` (`abwab-page.component.html:165` vs `:146`/`:137`). A zero-match query
leaves the full tree standing. Matched ancestors are **seeded**, not forced, so the expansion
survives clearing `q` — pinned by `ec28c3c1`. *(The one stale survivor is `:436`'s reveal
paragraph — F05 — and the ux-audit's dead input name — F30.)*

**The `modal` URL key contract — no asymmetry in either direction.** The six kinds the README
table lists at `:375` are exactly `AbwabModalKind` (`abwab.models.ts:14`, guarded at `:128-140`).
The four door-dependent kinds match `ABWAB_DOOR_DEPENDENT_MODAL_KINDS` (`:142-150`) and fail closed
without a valid `door` in the same ParamMap (`abwab-url-sync.ts:65-67`). The
`relations-<id>-closed` fail-closed rules hold exactly as written (`:50-60`, `:71-77`), including
that it does **not** require `door=`. Restoring writes `door=<id>` plus the bare open key in one
patch. `modal` enters no cache identity. Bulk modes never write the key. No second long-lived doc
enumerates the kinds, so there is no doc-vs-doc surface here.

**Archive / move / restore / global order.** Twelve claims verified, including: a retired-section
root restore with no destination is `400` «قسم الباب الأصلي محذوف، حدد قسمًا للاسترجاع»; a stated
destination that no longer exists is `404`; the whole destination/re-section body is gated on the
door having actually been archived, so a `sectionId` on a live door is ignored; archive claims only
live descendants and restore returns exactly what that archive took, matched on `deleted_at`;
`AbwabReorderScope` is `Section = 1` / `Global = 2` with the frontend mapping in exactly one place
(`ABWAB_ORDER_SCOPE_TO_WIRE`); `GlobalOrderValue` is meaningful for live roots only; `depth === 0`
means restorable in the archive view; the move picker's strip is `qd-tabs` at `layout="grid"`;
its destination list opens collapsed with derived (never written) search expansion; confirm is
disabled with no `targetSectionId` while a bulk selection spans sections; bulk archive is
all-or-nothing on the version token.

**The comprehensiveness vocabulary — the forbidden wording is confined and recorded.**
«أعم/أخص» appears in **no** shipped UI copy. `abwab.labels.ts:319` uses «شمولية»;
`features/abwab/README.md:294-295` states the rule («أعم/أخص» appears nowhere in the copy) and it
holds. The only surviving occurrences are in `docs/design-preview/abwab-relations-concept.html:155,183`
— a historical mockup — and that exception is **explicitly recorded** in a long-lived place,
`abwab.labels.ts:254-257`: *"the contract's `TYPE_META.hier.label` («أعم / أخص», :183) and its hint
paragraph (:155) violate the locked comprehensiveness-only vocabulary. Both are unreachable in the
contract's own rendering, and neither string is reproduced here."* The direction labels are stated
from the anchor's side with two copies, one per mode. `أعمق` matches elsewhere are the unrelated
word "deepest". **No finding.**

**The ux-audit's four recorded reversals were all amended in the code.** Item 12 (blur cancels) →
`features/abwab/README.md:99-104` now names blur explicitly. Item 13 (the flag is a control) →
the *"A chip, not a control… plan §7 T603"* comment is **gone** from
`abwab-tree.component.scss`, replaced by the accent-tint rationale at `:249-261`, and the
"Zero dead controls" gotcha at `:818-826` records the change. Item 20 (children-only apply) →
«بجذره وكل فروعه» is **gone** from `abwab.labels.ts`, replaced by «بدون جذره» / «جذر القالب نفسه
لا يُنسخ» (`:416`, `:418`). Item 22 (nav dropdown) → `abwab.routes.ts:19-22`'s old *"reached from
the doors page header, not the sidebar"* comment is **gone**, replaced by the accurate
`nav-menu.ts` note. Cross-slice hygiene on these four was good.

**`core/README.md`'s auth posture is accurate.** `:28` and `:111` describe `roleGuard` as existing
and attached to no route; `core/auth/role.guard.ts` **does** exist (an early truncated listing in
this sweep suggested otherwise — re-verified with `git ls-files`), and `app.routes.ts:26-28` says
the same. `Authentication/README.md:50-52` accurately states that no fallback policy exists and
that only `api/access/me` opts in.

**`e2e/README.md` vs `playwright.config.ts`.** Eight `abwab-*.e2e.ts` specs, a `default` project
with `testIgnore: /abwab-.*\.e2e\.ts$/` at 2 workers and an `abwab` project with the matching
`testMatch` at 1 worker, run in sequence by `npm run e2e`. All accurate. *(Only the test **count**
conflicts, and only with TESTING_STRATEGY — F17.)*

**`docs/contracts/` holds to its own charter.** All six pages restate no contract content and
defer to code + the nearest README, exactly as `docs/contracts/README.md:7-15` promises. Every
outbound pointer resolves: `Frontend/quran-dashboard-ui/openapi/swagger.json` and
`docs/api-reference/index.html` both exist, and the `docs:api` / `generate:api` /
`check-api-contract` commands named at `http-api.md:16-21` match `package.json:15-16` and the
actual script.

**`docs/TESTING_DEBT.md` — 26 of 28 open rows verified.** Every row was opened against the code or
test file it names. Twenty-six are still real and accurately described (including J1's file-size
row: `EfAbwabDoorsWriter.cs` is now 867 lines against the stated *"816 before this feature and
larger after"*). Two are stale — F19 above, and F16/F18's strategy-side omissions.

---

## 5. Open questions requiring a decision

1. **F01 — was the Abwab production release a deliberate waiver of the documented gate?**
   This determines whether the fix is a doc edit or an auth policy. If the exposure is accepted,
   the five `docs/TESTING_DEBT.md` rows keyed to *"when write protection lands"* (3, 8, F3, G3, I2)
   also need a trigger that can actually fire. **Nothing was changed; this needs your call.**

2. **F07 — is `BulkMoveAsync` supposed to validate request shape before entity existence?**
   The README calls the ordering "load-bearing" and two test comments repeat it, but the code does
   the opposite and no test discriminates. Either the code reorders or three docs change. This is
   the one finding where the code may be the wrong side.

3. **F20 / F31 — are the un-numbered `feature-<name>/` folders inside or outside the N-2 sweep?**
   Nineteen tracked `docs/feature-*/` folders exist against a rule that keeps two plus open
   features, and `Backend/report/` has an unlisted sixth folder. The cleanup itself is explicitly a
   later task; the question here is only what the charters should *say*, and whether the ux-slice
   folders count as N-2 candidates at all.

4. **F15 — fill in `.specify/memory/constitution.md`, or teach `speckit-analyze` to skip it?**
   Filling it in makes both skills correct; adding the escape hatch is the smaller change.

5. **F13 / F11 — should `docs/design-preview/` be re-charted as adopted-and-historical?**
   Three live docs point into a folder whose README still describes the direction as unadopted, and
   whose file table omits the three abwab design contracts other docs treat as governing.

---

## Appendix — verification record

**Totals: 39 findings — by type A 20 · B 5 · C 4 · D 3 · E 7; by severity HIGH 3 · MEDIUM 25 · LOW 11.**

- **31 findings** returned by 12 contract threads; **32 line anchors** (31 doc + 1 second doc)
  re-opened mechanically and re-matched against the quoted string. **32/32 OK, zero drift, zero
  file-missing.**
- **9 further findings** were produced and verified directly rather than by a thread (F01, F02,
  F11–F15, F37–F40), because doc-vs-doc conflicts need two documents held at once.
- **Two subagent counts were wrong and are corrected in place**: F20 (19 tracked feature folders,
  not 14) and F23 (five undocumented scripts, not four).
- **Nothing was modified.** `git status --porcelain` after the sweep shows exactly one entry: this
  file.
