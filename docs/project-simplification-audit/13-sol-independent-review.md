# Sol Independent Audit Review

## 1. Overall Verdict

`AUDIT_READY_WITH_CORRECTIONS`

The review gate passed. The current branch is `audit/project-simplification`, HEAD is
`6116dac9073d840ab0d3178459e986074f8227f6`, and
`72792ba9ff589c66aa25632a464b56b8bf7787af` is the exact merge base. The one commit over
that baseline adds only the 29 audit-pack files under `docs/project-simplification-audit/`;
it contains no production, test, schema, configuration, instruction, or Skill remediation.

The audit is trustworthy enough to avoid another repository-wide discovery pass. Its
baseline inventories are generally strong, baseline-stamped, and internally useful. The
topic reports also disclose many of their own limitations. The cross-cutting synthesis is
not reliable enough to use uncorrected: it overstates modeled context and runtime claims,
fails to carry several corrections from topic reports, broadens README and test work beyond
the evidence, and treats storage retention as sufficient audit retrievability.

This Sol pass used repository reads, independent recounts, direct source samples, git
history, and three separate adversarial evidence lanes. It did not rerun a build or broad
test gate. The committed runtime JSON has no raw logs or repeat-run variance, and rerunning
the already-measured broad gates was unnecessary for this review-only task.

## 2. Recomputed Headline Claims

| Claim | Fable result | Sol result | Confidence | Evidence |
|---|---|---|---|---|
| Review scope | Baseline plus audit artifacts only | Confirmed: one commit over the exact merge base; all 29 changed paths are additions under the audit folder | CONFIRMED | `git merge-base`, `git rev-list`, and `git diff --name-status 72792ba9..6116dac9` |
| Handwritten production LOC | 113,155 / 27.8% | Exact at the stated newline-count definitions: Backend 56,934 + Frontend 56,221 = 113,155; 27.79% of 407,221 | CONFIRMED | Independent `find`/tracked-file recount; `data/loc-inventory.json`; `08-architecture-code-size-audit.md:15-24` |
| Test LOC | 111,921 / ~27.5% | Exact canonical count: 55,808 Backend + 54,145 Frontend specs + 1,968 E2E = 111,921; 27.48%. The alternate 112,268 figure uses physical-line semantics that add one line per Backend file | CONFIRMED | Direct recount; `02-test-suite-audit.md:28-35`; `data/loc-inventory.json` |
| Generated/support proportion | ~35% generated/support | Label correction required. Generated artifacts are 91,010 LOC / 22.3%. Embedded morphology-correction JSON is data: 53,367 / 13.1%. Their combined 144,377 / 35.5% arithmetic is correct, but it is not one generated/support-code category | CONFIRMED | The three JSON data files under `Backend/infrastructure/.../Corrections/`; `08-architecture-code-size-audit.md:42-45,85-88` |
| Instructions duplicated | Root and Backend pairs identical; Frontend substantively mirrored | Root 14,915 B and Backend 7,335 B pairs are byte-identical; Frontend differs only in its H1 (4,016 B vs 4,025 B) | CONFIRMED | `cmp`, hashes, and `AGENTS.md:7-15`; `.cursor/rules/always-read-agents.mdc:41` |
| Tiny frontend task context | 293,528 B / ~73,382 tokens | The byte sum is reproducible. The token value is `bytes/4`, not tokenizer output or observed context. T2 also treats conditionally worded `PRODUCT.md`/`DESIGN.md` reads and the largest README as mandatory full reads | LIKELY | `data/instruction-inventory.json` T2; `03-agent-context-instruction-audit.md:123-140`; `AGENTS.md:245-248` |
| Routing-only reduction | 41.7%, ~103k tokens over six traces; T2 ~73k to ~30k | The arithmetic `(988,533 - 576,423) / 988,533 = 41.689%` is correct. The label is not: the model changes trigger semantics for `TESTING_STRATEGY.md`, UI law, and other protection-bearing reads. It is an unweighted static scenario model, not observed recurring savings; excluding the unusually large T2 yields 34.6% | NEEDS_MEASUREMENT | `03-agent-context-instruction-audit.md:247-263`; current entrypoint and testing rules |
| Engineering-review context | ~17.1k floor; ~51.6k measured; ~90k worst case | 68,411 B is a plausible effective floor, not a runtime measurement. The 206,417 B T7 trace incorrectly makes all six clean-code references mandatory; the canonical Skill routes them per finding or on a thorough pass. Removing those 54,174 B gives a 152,243 B base (~38.1k by bytes/4) before relevant clean-code references. 359,926 B is a broad fixed closure, not a true worst case because live feature artifacts are variable and excluded | LIKELY | `.claude/skills/engineering-review/SKILL.md:44-67`; `data/instruction-inventory.json` T7; `data/skill-inventory.json` closure |
| README concentration | 40 READMEs, 489,912 B, median 7,910 B; top five 48% | Counts and concentration reproduce for the audit's scope, excluding `.specify`. The actual top five include `Persistence/Writes/Abwab/README.md`, not the tests README used by WS2. The 84-97% unique-content shares are reviewer judgments, not measurements | CONFIRMED (sizes); LIKELY (content shares) | Baseline `git ls-tree`/`git show` recount; `06-readme-markdown-decision-audit.md:28-42,133-145` |
| Tailwind adoption | 0.0%; qd system heavily adopted | Zero Tailwind utility tokens and zero `@apply` are confirmed. Tailwind preflight is nevertheless compiled and referenced by production geometry, so Tailwind is not behavior-free dead weight. `qd-*` is load-bearing: 1,584/3,101 inventoried class tokens, 186 defined, 169 used | CONFIRMED | `styles.scss:13-15`; `tailwind.config.js:3-7`; `_utilities.scss:13-52`; `07-frontend-styling-audit.md:89-134` |
| Test runtime | Runtime solved; 3,231 Backend tests in 357.7 s; Frontend 2,964 in 232.2 s | The committed single-run results are internally coherent and the measured ordinary lanes look acceptable. 3,231 is overlapping lane-executions, not the effective distinct suite. The measured noncanonical partition is 2,211 executions / 258 classes. Canonical, E2E, actual Backend pre-PR, flakiness, and variance remain unmeasured; “solved” is too strong | LIKELY | `02-test-suite-audit.md:107-140`; `data/runtime-measurements.json`; `TESTING_STRATEGY.md:230-248` |
| API inventory | 85 operations / 78 paths / 229 schemas; 55 unused-field candidates; 2 no-consumer endpoints | Swagger recount is exact: 58 GET, 18 POST, 5 DELETE, 4 PUT. Two endpoints have no in-repo production caller, but external consumers are unproved. The regex-based 55-field classification is not deletion evidence; it already misattributes same-named filter fields. Only 3 of the 7 nominally unreferenced models are genuinely dead according to report 09 itself | CONFIRMED (surface); NEEDS_MEASUREMENT (removal) | `swagger.json`; `endpoint-consumers-frontend.json`; `09-api-surface-payload-audit.md:1-18,373-387` |
| Arabic JSON savings | `\uXXXX` causes ~3x Arabic string bytes and 30-60% hot-payload savings | Absence of custom encoder and app response-compression configuration is confirmed. Actual wire bytes, payload mix, Railway-edge compression, latency, and safe encoder behavior were not measured | NEEDS_MEASUREMENT | `Backend/api/QuranDashboard.Api/Extensions/ServiceCollectionExtensions.cs:18-35`; `WebApplicationExtensions.cs:63-86` |
| Architecture is not the main problem | Layering is sound; architecture cleanup low priority | The main prioritization is likely right: 78 abstraction interfaces all resolve, 19 decorator seams are active, and representative readers/outcomes own real behavior. The categorical phrasing is too strong: only 13/92 handlers were sampled, Mushaf was not sampled deeply, threshold breaches were omitted, and several proposed cleanup candidates are unsafe or speculative | LIKELY | `IRootsReader.cs:6-47`; `CachedRootsReader.cs:7-168`; `08-architecture-code-size-audit.md:122-160,366-369` |

## 3. Critical Challenges

### C1. The synthesis loses corrections that the topic reports already made

- `11-cross-cutting-priorities.md:58` proposes compressing five READMEs, but report 06 says
  access-admin needs a full read, Writes/Abwab and tests are KEEP, and only a bounded scripts
  runbook is on-demand. The top-five list in WS2 also substitutes the tests README for the
  larger Writes/Abwab README.
- WS6 proposes acting on “7 never-referenced models” (`11-cross-cutting-priorities.md:98`),
  while report 09 proves four are structurally consumed (`09-api-surface-payload-audit.md:373-387`).
- Report 07 keeps tiny SCSS files (`07-frontend-styling-audit.md:452-454`), while WS8 proposes
  tidying 21 of them (`11-cross-cutting-priorities.md:118`).
- The index/executive call 3,231 overlapping Backend lane-executions “tests”; report 02
  correctly identifies 2,211 measured noncanonical partition executions.

The topic reports and direct code win whenever they conflict with reports 01 or 11.

### C2. The context model changes protection, not only routing

The 41.7% model removes the full testing strategy from routine paths and narrows style/product
reads. The entrypoint summary does not reproduce the complete execution-trigger matrix,
canonical fail-not-skip rules, shared test-runtime triggers, route-smoke parity, data-tier
separation, or operational E2E constraints. A future plan may replace full reads with a
mechanically complete safety card or exact section pointers; it may not call their removal
“routing-only.” The model also misroutes T7 clean-code references and reverses required vs
conditional reads in its performance-backend trace.

### C3. The proposed logging-test rewrite weakens a fail-closed security contract

Report 02 proposes replacing per-handler exact field assertions with one generic
no-sensitive-content property. `LOGGING_GUIDELINES.md:22-45,76-92` requires stable fields,
per-field safety verification, emitted-field tests, and redaction coverage. The four current
logging suites total 1,583 LOC but have low raw pairwise source similarity; they enforce an
allowlist, levels, reasons, operation identity, and absence of content. A sentinel-only test
can pass while a newly added raw field leaks data. Consolidation is acceptable only if an
exact per-handler safe-field allowlist remains, perhaps as parameter data.

### C4. Broad test-harness savings are not replacement coverage

- Stems and Lemmas are genuinely similar; the five Words page suites are not. A two-page
  pilot is supported, a five-page generic harness is not.
- RefusalForce classes share an invariant but not a common implementation. A shared assertion
  helper is supported; merging classes is not.
- The ten importer fixtures total 4,559 LOC, but their median normalized similarity is only
  0.144 and feature-specific fixture reuse is deliberately local
  (`Backend/tests/QuranDashboard.Tests/README.md:168-190`). Four explorer fixtures are the
  actual near-clone pool.
- `abwab-page.component.spec.ts` has 235 selector calls, not 370 independent markup
  assertions. Most selectors are stable `data-testid` attributes. A behavior-named file split
  may help, but the advertised selector rewrite and LOC reduction are unsupported.

Case-count parity and TSV selection parity do not prove a parameterized replacement is
non-vacuous.

### C5. Scoped post-review re-verification can create a regression escape path

Proposal A in report 10 preserves full reruns for route/auth/migration/canonical changes,
but omits shared pipeline/Quran persistence and shared test/runtime infrastructure triggers
required at `TESTING_STRATEGY.md:241-243`. Selecting gates from only the last fix rather than
the cumulative final diff can miss a cross-scope regression. With no CI, a release several
features later is not an adequate PR backstop. Scheduling `check-api-contract` is a sound
separate improvement, but its trigger must cover all exporter-visible sources—controllers,
request/response DTOs, envelopes, Swagger configuration, and serialization metadata—not only
`Backend/api/`.

### C6. Audit storage is not the same as audit retrievability

`EfAccessAuditReader` currently fetches and serializes five JSON documents per list item, and
the only Angular template does not render them. However, the application contract says the
reader exposes immutable snapshots (`Application.Abstractions/Access/README.md:17-20`), and
there is no event-detail endpoint. Dropping the documents from the list would make them
database-operator-only. Preserving database rows is mandatory but insufficient: a shrink
must preserve an authorized retrieval surface or explicitly change the audit-read contract.

The same API audit has further static blind spots:

- `actorUserId`/`targetUserId` usage was inferred from unrelated filter properties, while the
  access README calls the IDs a deliberate round-trip contract.
- The access page eagerly loads audit and reconciliation data even when its default tab is
  workspace, yielding at least two additional static lazy-load candidates.
- `/table?tableView=words` is a semantic superset, not a wire-compatible replacement.
- Full equivalence between the API surah catalog and static frontend catalog is not proved by
  a count of 114 and three sample pages.
- Five 1,000-row Words caches use count-based 48-entry LRUs without a byte budget; heap cost is
  unmeasured.

### C7. Styling direction is not a technical consequence of zero utility tokens

Tailwind utilities are unused, but Tailwind preflight is live CSS and production SCSS refers
to inherited preflight line-height. Removing it is a rendering change. A Tailwind-dominant
future risks physical-property regressions in an RTL-first codebase; removing Tailwind risks
reset/geometry regressions. The strong token layer, semantic `qd-*` primitives, logical
properties, and Quran typography must survive either direction. The evidence makes
qd/custom consolidation the lower-migration-risk default, but the future vocabulary is an
owner architecture choice.

### C8. The architecture report misses threshold debt and includes unsafe candidates

The architecture audit supports keeping the layer boundaries, not a claim that no structure
debt exists. It omits current threshold exceptions such as the 757-line
`EfAbwabDoorsWriter`, 781-line `MorphologyValidationRunner`, 701-line
`MorphologyAssembler`, 693-line `EnrichedDimensionBuilder`, and the documented six-part,
1,591-line `EfWordTypesReader` partial type. Frontend facades at 535-596 lines also deserve
focused cohesion review.

G3 should be removed: `HighlightedAyahComponent` owns sacred-rendering marker and Arabic
accessibility behavior, folding `UniqueWordsTabsComponent` would push its parent over the
frontend threshold, and `WordSectionCardComponent` is instantiated from a data-driven loop.
G3 saves approximately zero LOC. G1 remains prototype-only, and G6 explicitly touches
Quran-source/refusal seams despite report 08's statement that no candidate touches protected
boundaries.

### C9. Some audit evidence is not reproducible from the pack alone

The JSON files name scan methods but the generator scripts and raw runtime logs are not in
the audit commit. Several conclusions can be independently sampled, as this review did, but
the full inventories cannot be regenerated byte-for-byte from a committed command. Dynamic
`refs/codex` evidence is also not commit-pinned: six refs existed at the audited point and
seven exist now. Ref names/dates are confirmed; writer identity is likely and usage level or
actual files read remains unmeasured.

## 4. Workstream Verdicts

### WS1 — `AGREE_WITH_CHANGES`

- **Evidence:** The instruction mirrors, Cursor contradiction, recurring drift history, and
  static trace byte arithmetic are real. A non-Claude entrypoint directly routes into Claude
  files (`AGENTS.md:7-15`).
- **Important corrections:** Relabel 41.7% as a scenario estimate; recompute T7/T8; keep the
  nearest-README model; exclude Spec Kit fork redesign, which the brief places out of scope.
  A repository pointer does not clearly satisfy Cursor's “unless the user explicitly asks”
  escape clause, so Cursor must be updated atomically.
- **Safety constraints:** No Quran-data, auth, test-trigger, deployment, or local-README rule
  may disappear. Both AGENTS and CLAUDE entrypoints must remain valid until their supported
  consumers are decided.
- **Prerequisites:** Owner decisions on Cursor, Codex, and the canonical entrypoint; an exact
  inbound-reference map; a controlled routing probe for trigger changes.
- **Small-plan readiness:** A pointer conversion is ready to plan only after those decisions
  and only as an atomic all-entrypoint/Cursor change. The modeled 42% trigger reduction is not
  ready.

### WS2 — `AGREE_WITH_CHANGES`

- **Evidence:** The scoped 40-README corpus is 489,912 B with a 7,910 B median; the tail is
  real. The Abwab URL contract, Gotchas, E2E membership glob, and reversal rationale are
  unique, current information.
- **Important corrections:** Do not target a generic five-file tail. Access-admin needs a
  full adjudication; Writes/Abwab, tests, and scripts core stay. Abwab and Words are the only
  supported shortening candidates, one file at a time. Content-uniqueness percentages are
  judgments. A second conflict was missed: `src/styles/README.md:115-125` lists nine contrast
  rows while `docs/TESTING_DEBT.md:209` still specifies seven.
- **Safety constraints:** Preserve xmin/409, access, identity-key, URL, E2E membership,
  typography, and measured contrast invariants. The contrast table cannot move until a test
  covers all nine rows and both themes.
- **Prerequisites:** Full per-file invariant/rationale maps; owner font-weight decision;
  repoint-before-delete search for every changed path or section.
- **Small-plan readiness:** Security contract-index repair and contrast-debt reconciliation
  are ready. README shortening is gated on the per-file maps; access-admin/tests/scripts-core
  must not enter that plan.

### WS3 — `AGREE_WITH_CHANGES`

- **Evidence:** The three TESTING_STRATEGY restatements and both adapter defects are direct,
  current facts. The commit adapter says sync-to-main twice while the canonical Skill says
  sync-to-dev and forbids syncing main; the test-guard adapter omits the frontend harness
  reference.
- **Important corrections:** The reference pack is 13 files, not 12; the reported skill total
  excludes ten `agents/openai.yaml` sidecars. Use concise pointers to exact canonical testing
  sections, not a new second reference layer. A rule living only in the formal review Skill
  is not automatically orphaned; “zero skill-only rules” contradicts the finding that every
  Skill owns unique behavior.
- **Safety constraints:** Route-smoke, canonical fail-not-skip, Quran safety, verdict/output
  gates, and branch protections remain mandatory at their enforcement points.
- **Prerequisites:** Exhaustive checklist-to-canonical mapping before extraction; controlled
  prompt probes before trigger narrowing.
- **Small-plan readiness:** The two adapter repairs and direct testing-block deduplication are
  ready as separate small plans. Checklist extraction and trigger narrowing are not.

### WS4 — `USER_DECISION_REQUIRED`

- **Evidence:** Zero Tailwind utility adoption, strong qd adoption, 10,346 SCSS LOC, repeated
  declaration groups, and the 103,970 B style guide are credible measurements.
- **Important corrections:** Tailwind preflight is live behavior, so the hybrid is not wholly
  dead. Separate the guide split and primitive deduplication from the Tailwind-vs-qd
  migration. Remove WS8's stub-SCSS deletion target because report 07 keeps those files.
- **Safety constraints:** Preserve tokens/themes, logical RTL properties, focus states,
  Arabic register, Quran fonts/rendering, and both light/dark behavior. Browser geometry and
  visual checks are required for either utility-system change.
- **Prerequisites:** Owner styling direction; compiled CSS/preflight baseline; browser checks;
  token-mapped policy if Tailwind wins.
- **Small-plan readiness:** The guide split, unused-class reference check, and confirmed
  primitive deduplication are bounded. Utility-system removal/adoption is not ready until the
  decision and measurements exist.

### WS5 — `AGREE_WITH_CHANGES`

- **Evidence:** Test LOC is large and ordinary measured runtime is acceptable; Stems/Lemmas
  specs and four explorer fixtures contain genuine duplication. Protection-heavy suites are
  correctly identified as KEEP.
- **Important corrections:** Limit P1 to a Stems/Lemmas pilot; limit P2 to shared assertion
  helpers consumed by existing classes. Reject P3's removal of exact log-field allowlists.
  P4's one importer skeleton is unsupported; P5's 370-assertion and 300-600 LOC claims are
  not established. Preserve behavior-by-behavior mapping, not only case counts.
- **Safety constraints:** Authorization, Owner, audit, concurrency, migration, source/hash/
  manifest, refusal/rollback, Quran provenance, safe-log-field, accessibility, URL-state,
  and browser-only E2E protections remain executable. Do not move required coverage into
  opt-in E2E.
- **Prerequisites:** Assertion-level replacement matrices; non-vacuity checks for
  parameterized cases; test-data provenance classification; owner importer-rerun decision.
- **Small-plan readiness:** One Stems/Lemmas behavior-helper pilot, one RefusalForce assertion-
  helper pilot, or behavior-only page-spec splitting is ready. No broad harness/deletion plan
  is ready.

### WS6 — `NEEDS_MEASUREMENT`

- **Evidence:** Swagger counts, two absent in-repo endpoint consumers, full audit-entity
  materialization, and absent custom encoder/app compression are confirmed.
- **Important corrections:** Resolve field use type-safely; retain four structurally consumed
  models; enumerate all lazy-load candidates; treat `/table` as a contract migration; prove
  full surah-catalog equivalence; preserve an authorized audit-snapshot retrieval surface.
- **Safety constraints:** No immediate anonymous endpoint deletion; use deprecate, observe,
  then remove. Preserve complete audit storage and retrieval, OpenAPI parity, error semantics,
  Quran catalog correctness, and consumer compatibility.
- **Prerequisites:** Actual wire bytes with and without edge compression; audit-document size
  distribution; query plans/timings; type-aware field references; endpoint telemetry;
  frontend heap/cache measurement; surah checksum/projection parity.
- **Small-plan readiness:** Measurement and type-aware investigation are bounded. Response
  shrink, encoder change, endpoint retirement, surah single-sourcing, and page-size changes
  are not remediation-plan-ready.

### WS7 — `NEEDS_MEASUREMENT`

- **Evidence:** The documented gates are already risk-scoped; `check-api-contract` is absent
  from `TESTING_STRATEGY.md` despite a stale-spec incident. Gate frequency and several costs
  are unknown.
- **Important corrections:** Derive re-verification from the cumulative final diff and every
  row of the authoritative trigger matrix, including shared Pipeline/Quran persistence and
  shared test/runtime infrastructure. Do not use the later release as a PR escape-path
  backstop. Expand the contract-check trigger to every exporter-visible source.
- **Safety constraints:** Preserve full gates for auth/route/middleware, schema/migration,
  canonical data, shared pipeline/persistence, shared test infrastructure, and release. Keep
  PostgreSQL processes serialized and canonical failures non-skippable.
- **Prerequisites:** Two to three features of invocation/fix telemetry; canonical-data,
  dump-regeneration, check-api-contract, focused frontend-contract, and actual pre-PR timing;
  final-diff trigger mapping.
- **Small-plan readiness:** Scheduling `check-api-contract` with the corrected source closure
  is ready. The cadence/freshness rewrite is not.

### WS8 — `AGREE_WITH_CHANGES`

- **Evidence:** Interface/decorator/reader boundaries are active, typed outcomes and caches
  add behavior, the Words state duplication is real, and `type-distribution-list` is
  statically dead.
- **Important corrections:** Record current threshold exceptions; remove G3; remove the stub-
  SCSS target; treat G1 and the 1,700-2,500 LOC benefit as prototype estimates; acknowledge
  that G6 touches protected import seams. A hypothetical sixth entity is not a present value
  argument.
- **Safety constraints:** Do not flatten authorization, transactions, caching, typed outcome,
  Quran source, URL-state, sacred-rendering, accessibility, or optimistic-concurrency seams.
- **Prerequisites:** Net-complexity prototype for G1; focused cohesion review of threshold
  exceptions; WS5/test decisions before import plumbing.
- **Small-plan readiness:** The dead `type-distribution-list` removal is ready after the named
  build and Words gate. G1/G6 are not; G3 should not be planned.

### WS9 — `AGREE_WITH_CHANGES`

- **Evidence:** No tracked repository memory integration was found. The 456 B tracked
  `.claude/settings.local.json.bak` is stale and advertises a direct test command that bypasses
  current lane policy.
- **Important corrections:** Deleting the tracked backup does not repair the ignored 540 B
  live local settings file carrying the same bypass. Fable's five-file Claude memory result
  is environment-specific; this Sol environment exposes a materially larger Codex corpus as
  well. Filesystem visibility does not prove universal per-session injection.
- **Safety constraints:** Keep machine-local durable facts; remove repo-derivable and stale
  status/count chatter only through an explicitly authorized memory review. Never bundle
  persistent-memory mutation into repository remediation.
- **Prerequisites:** Separate per-environment memory adjudication and explicit user authority
  for any persistent-memory write/delete.
- **Small-plan readiness:** The tracked backup deletion is ready. Persistent-memory cleanup is
  not authorized or plan-ready in this review.

## 5. Safety Boundaries

| Boundary | Protection that must remain | Required proof before simplification |
|---|---|---|
| Branch/deployment | `main` stays protected and production-only; normal post-PR sync targets `dev` | Adapter text and all Git routing agree; no `sync-to-main` wording |
| Authentication/authorization | Two-token identity binding, Owner rules, direct permissions, account status, exact route metadata | Access + Smoke + Tier B as triggered; real bearer/ID-token subject checks remain |
| Audit | Append-only storage in the mutation transaction, immutable snapshots, authorized retrieval, complete actor/target context | Database row remains complete; reader contract has a supported snapshot path; persistence/failure tests remain |
| Concurrency/transactions | `xmin`, stale-version `409`, writer transactions, invalidation ordering | Existing write/read contracts and PostgreSQL-backed cases remain behaviorally equivalent |
| Migration/schema | Upgrade-from-previous-production path, preflight/collision checks, pending-model and dump head | Migration + Access + Smoke + Tier B + pending-model; dump regeneration remains explicit |
| Quran data/provenance | No invented scripture, staged canonical sources, hash/manifest/source gates, refusal/rollback, zero unexpected canonical skips | Pipeline/canonical gates and source-safe test data; provenance/checksum evidence retained |
| Logging/errors | Stable, explicitly safe fields; no Quran text/search/secrets; controlled public errors | Per-handler allowlist or equivalent fail-closed schema plus redaction and level/reason tests |
| API contracts | Swagger/client parity, out-of-repo compatibility, deliberate deprecation | `check-api-contract`, consumer observation, and generated-client/route-baseline updates |
| RTL/typography | Logical properties, token values, focus behavior, bundled Quran/Arabic fonts, both themes | Browser geometry/visual verification and font/contrast tests; no silent faux-weight change |
| Test infrastructure | Risk-scoped matrix, single PostgreSQL ownership, no concurrent DB-bearing runs, visible output | Final cumulative diff mapped to every trigger; canonical/data-tier result stated separately |

## 6. Measurement Gaps

### Must measure or complete before remediation planning

- WS1 trigger-reduction safety: corrected T7/T8 traces, controlled routing probes, and a
  complete compact replacement for any mandatory policy read proposed for removal.
- WS2 shortening: line-by-line invariant/rationale maps for each target; Access-admin and
  Words have not received Abwab-level adjudication.
- WS4 utility change: compiled preflight contribution, preflight dependence, and browser
  geometry/visual baselines.
- WS5 broad consolidation: assertion-level replacement coverage, non-vacuity, importer-
  fixture isolation/prototype evidence, and test-data provenance.
- WS6: actual wire bytes/encoding/compression, query cost, audit document distribution,
  type-aware references, external usage, cache heap, and surah parity.
- WS7 cadence: real invocation/fix frequency over two to three features; canonical/dump,
  contract, focused frontend, and actual pre-PR timings; cumulative-diff trigger mapping.
- WS8 G1/G6: prototype net complexity and protected import-seam behavior.

### Can measure during a later bounded workstream

- Actual agent file-read/token behavior rather than `bytes/4`, and actual frequency of each
  custom Skill.
- Exact utility-replaceable SCSS LOC and duplicate groups missed by textual normalization.
- E2E wall time/flakiness if E2E cadence remains unchanged; browser runs are still required
  for changes that alter geometry or browser-only behavior.
- Type-aware resolution of the 157 UNKNOWN_CONSUMER API fields, which remain untouchable in
  the meantime.
- Cohesion of the named Backend/Frontend threshold exceptions; this is a focused structure
  follow-up, not another repository-wide audit.

### Harmless known limitations

- `bytes/4` is a stable comparison heuristic, not tokenizer or billing output.
- Physical vs newline LOC differs by a final-line convention; the canonical percentages are
  unaffected when one definition is used consistently.
- Dynamic Codex checkpoint-ref count will continue to change; only the dated evidence and
  its limited inference matter.
- Text/title similarity misses renamed/reordered equivalence and can overvalue same-named
  cases; it is suitable for candidate discovery, not deletion proof.

## 7. User Decisions Required

1. **Cursor support:** keep, pointer-reduce, or retire the always-apply Cursor rule. A retained
   rule must be updated atomically with WS1.
2. **Codex support:** keep first-class AGENTS/adapters and decide whether Codex Spec Kit is a
   supported integration. Current activity evidence argues against deleting Codex-facing
   entrypoints, but support policy belongs to the owner.
3. **Canonical instruction entrypoint:** Claude-named canonical file with explicit pointers,
   AGENTS-named canonical file, or a neutral shared law. This changes WS1's shape.
4. **Styling direction:** Tailwind-dominant with token/logical-property constraints, or
   qd/custom consolidation. Current adoption favors qd as the lower-risk default; that does
   not decide the desired future ecosystem.
5. **Font weights:** preserve the actually bundled 400/700 rule, or deliberately add and
   validate 500/600 faces. The present documents conflict.
6. **Importer reruns:** whether completed importer pipelines remain operationally expected.
   Until answered, their refusal, validation, rollback, and source-safety coverage stays.
7. **No-CI stance:** retain fully local gates or introduce a hosted/scheduled subset. Canonical
   source/dump gates must not be moved blindly to an environment without staged resources.
8. **API compatibility policy:** required deprecation/observation window for anonymous and
   Owner APIs, and whether the static or API surah catalog is the intended canonical consumer
   surface. Static absence of in-repo callers does not decide this.
9. **Read-completion logging policy:** whether per-read completion logs remain operationally
   wanted. Safe-field and rejection protection remains regardless.
10. **Persistent memory remediation:** any edit/delete in Claude or Codex memory requires a
    separate explicit authorization; this review only inventories and classifies it.

## 8. Missing Audit Coverage

- **Type-aware API use:** field classification is regex/name based and produced at least one
  same-name false attribution. Focused LSP/type-flow work is enough.
- **Audit read contract:** report 09 checked storage preservation but not whether an authorized
  snapshot-retrieval path survives response shrink. Focused API-contract follow-up is enough.
- **Access lazy loading and client heap:** eager hidden-tab calls and count-based 1,000-row
  caches were not analyzed. Runtime network/heap measurement is enough.
- **Surah full-data equivalence:** count and sample-page checks do not prove names/start-page
  equality. A deterministic full projection/checksum test is enough.
- **Architecture thresholds:** report 08 did not adjudicate named Backend/Frontend threshold
  exceptions and undersampled Mushaf. A focused cohesion review is enough.
- **Production-comment surface:** report 07 counted comments/blanks as SCSS LOC but did not
  separate multi-line production commentary governed by the repository's strict comment
  rule. A scoped grep/adjudication is enough and does not affect the headline LOC result.
- **Contrast-debt drift:** the style README has nine rows while TESTING_DEBT still specifies
  seven. A focused documentation/test-debt correction is enough.
- **Raw reproducibility:** inventory generator scripts and full command logs were not shipped.
  Keeping reproducible scripts in any future refresh would improve auditability; the present
  independent samples do not justify another full pass.
- **External systems:** Railway/Vercel compression, production endpoint access logs, hosted
  checks, and out-of-repo API clients are outside repository evidence. Targeted operational
  observation is required.
- **Runtime coverage:** canonical-data, dump generation, E2E, actual pre-PR, variance, and
  historical flakiness remain unmeasured. These are focused gaps already acknowledged by the
  audit.
- **Environment memory:** report 05 inventories one Claude corpus, not all agent environments.
  The Sol/Codex corpus below materially changes WS9's scale, but not repository findings.

None of these omissions requires another repository-wide discovery pass.

## 9. Memory / Context Inventory

The following inventory separates repository truth, injected/tool context, and persistent
memory. It claims no access to hidden model memory and reveals no private reasoning.

### 9.1 Repository instructions and documentation

The Sol session had the repository's root `AGENTS.md` supplied as workspace instructions and
read the applicable root, Backend, Frontend, architecture, testing, product/design, Skill,
README, audit-report, and machine-readable evidence files. These are repository sources, not
persistent memory. `.cursor/`, `.agents/`, `.claude/`, and `.specify/` are tracked tool or
workflow configuration; none is itself proof of runtime injection into Sol.

### 9.2 Tool/config and injected session context

The session exposed the current cwd/branch/date, filesystem and approval boundaries, the
available Skill/plugin roster, root workspace instructions, the user's IDE-tab/attachment
context, and a Codex memory policy plus the contents of an 8,109 B memory summary. The Sol
review then explicitly read `MEMORY.md`, selected rollout evidence, and the filesystem memory
inventories below. Filesystem visibility proves availability, not universal injection into
every agent or session.

### 9.3 Sol/Codex persistent memory actually exposed

Path: `/home/mohamed/.codex/memories/`.

| Item | Observed content | Stale/duplicate risk | Classification |
|---|---|---|---|
| `memory_summary.md` — 8,109 B | Injected summary of user preferences, current workspace topics, and durable cautions | High-signal, but mixes durable preferences with dated feature/status and repo-derived contract summaries | MERGE |
| `MEMORY.md` — 61,954 B | Curated task groups, preferences, exact historical outcomes, commits, branches, commands, and test counts | Contains useful user boundaries, but also stale counts/status such as historical 584/333, 110/110, 104/Smoke/Tier-B results and repo law that should be re-read from the tree | MERGE |
| `raw_memories.md` — 192,718 B | Merged stage-1 histories for the same 38 threads | Heavy duplication of rollout summaries; includes obsolete child-repo/submodule topology, stale feature paths, exact old counts, commit-readiness, and status chatter | DELETE from the active/retrieval corpus; provenance remains in summaries |
| `rollout_summaries/` — 38 files | On-demand per-session provenance and evidence | Historical by nature; unsafe as current truth without live verification, but useful for archaeology | KEEP as archive/on-demand provenance |
| `skills/` and ad-hoc update notes | No memory Skill files and no ad-hoc notes observed | None | KEEP empty |

The internal memory-repository `.git` and instruction/config metadata are tool state, not
project-memory claims, and are excluded from KEEP/MERGE/DELETE adjudication.

### 9.4 Claude persistent memory actually inspectable from this environment

Path: `/home/mohamed/.claude/projects/-projects-Dashboard-App/memory/` — five files, 6,174 B.
These files were filesystem-readable to Sol but were not demonstrated to be injected into
this Sol session.

| Item | Observed content | Classification |
|---|---|---|
| `MEMORY.md` — 552 B | Three live pointers | KEEP |
| `design-preview-flat-green-direction.md` — 1,905 B | Duplicates DESIGN/UI law, points at deleted `docs/design-preview/`, but carries a machine-local inotify/ENOSPC fact | MERGE to the non-derivable machine-local residue |
| `fix-agent-context-threshold.md` — 1,293 B | Durable user preference, but anchored to an archived/non-current Skill name | KEEP, de-anchor the stale Skill name if later authorized |
| `local-https-dev-cert-mismatch.md` — 2,002 B | Machine-local browser/certificate workflow | KEEP, subject to live verification before reuse |
| `memory-system-smoke-test.md` — 422 B | Self-declared throwaway with no durable fact | DELETE |

No persistent-memory mutation was performed or authorized. Model-internal memory beyond
these exposed files remains UNKNOWN.

## 10. Final Recommendation

Another repository-wide audit is **not necessary**. Use the topic reports, machine-readable
inventories, and this correction layer as the discovery baseline; do not use report 11's
scope or ranking without these corrections.

Safe to turn into SMALL independent plans now:

1. WS3's two `.agents` adapter corrections.
2. WS7's `check-api-contract` scheduling, with the full exporter-visible trigger closure.
3. WS2's security contract-index repair and nine-row contrast-debt reconciliation.
4. WS9's tracked `.claude/settings.local.json.bak` removal.
5. WS8's dead `type-distribution-list` removal with README/style cleanup and the named build
   plus Words gate.
6. WS5's narrowly bounded Stems/Lemmas behavior-helper or RefusalForce assertion-helper
   pilot, with assertion-level replacement mapping.

Ready only after a focused prerequisite or owner choice:

- WS1 pointer conversion: Cursor/Codex/canonical-entrypoint decisions and corrected traces.
- WS2 README shortening: per-file invariant/rationale maps; never a generic five-file cut.
- WS4 guide split and font reconciliation: preserve live rules; utility direction remains an
  owner decision and requires browser/preflight evidence.
- WS5 broad harnessing, importer fixtures, logging-test reduction, and giant-spec rewrite:
  replacement coverage, provenance, pilot evidence, and importer-rerun decision.
- WS6 payload, encoder, page-size, response-field, or endpoint changes: wire/query/heap/type-
  flow/usage measurements and audit-retrieval design.
- WS7 cadence reduction: real gate-frequency data and cumulative-final-diff trigger proof.
- WS8 Words state/import consolidation: net-complexity prototype; G3 is rejected.
- Persistent Claude/Codex memory cleanup: separate explicit authorization and environment-
  specific review.

The corrected priority is safety-first rather than the report 11 score: fix the production-
branch adapter defect, schedule the missing API parity gate, reconcile the contrast/font and
contract-document conflicts, then pursue context/test/code reductions only inside the
bounded scopes above.
