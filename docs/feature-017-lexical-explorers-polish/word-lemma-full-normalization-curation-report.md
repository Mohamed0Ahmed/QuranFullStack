# Word-Level Lemma Full Normalization — Phase 0 Curation Report

**Project:** Quran Dashboard / المنهج القرآني
**Feature:** 017 — Lexical Explorers Polish
**Branch:** `017-lexical-explorers-polish`
**Date:** 2026-06-27
**Status:** **REPORT ONLY** for curation. No production importer code, DI, hard-checks, report-builder,
migrations, source files, or DB changes were made.
**Original verdict (2026-06-27): IMPLEMENTATION BLOCKED — 30 active blockers remain (Phase 0D/0B RED).**
**Updated verdict (2026-06-28, Phase 0F promotion): MASTER GATE GREEN — 0 blockers; 30 resolved (27 replace, 2 keep, 1 exception). See §14.**

This report is the authoritative Phase 0 curation output for
`word-level-lemma-full-normalization-implementation-plan.md`. It records a final
decision for every known problem class. Per the plan's tightening rules, every
candidate received one of: `approved add`, `approved remove`, `approved replace`,
`keep` (accepted-exception), `accepted exception`, or `blocker`. **Unresolved
blockers are NOT promoted into the active artifact**, so the MASTER GATE is RED.

## 1. Methodology (reproduced, deterministic)

Phase 0 reproduced the full audit (`full-word-level-lemma-alignment-audit-report.md`
§3) over the staged source JSON so that *every* candidate — not just the samples
shown in the audit tables — receives a decision:

1. Loaded all 77,432 aligned readable Corpus/QPC words and the QUL word-level
   lemma map (72,507 entries).
2. Built the same-word Corpus-Buckwalter → Arabic-lemma mapping from existing
   QUL assignments, **excluding the 63 known remove locations** so a defect
   cannot train its own wrong mapping. Result: **4,797 reliable** mappings
   (unique, or ≥5 examples and ≥80% share) and **9 ambiguous** Buckwalters —
   matching the audit.
3. Classified every readable word; emitted the full candidate sets.
4. Applied the decision policy below; de-duplicated to one operation per
   location (priority 0A > 0B > 0C > 0D/0E).
5. Self-validated the draft and active artifacts against raw QUL
   (`validate.py`): zero duplicate ids, zero duplicate operation locations,
   every `expectedCurrentLemmaArabic` matches raw QUL, all operation shapes valid.

Reproducible scripts live in `curation-tmp/` (`audit.py`, `curate.py`,
`validate.py`).

## 2. Operation-kind semantics (authoritative for this feature)

| kind | meaning | mutates map? |
| --- | --- | --- |
| `add` | raw QUL lemma absent; corrected lemma added | yes |
| `remove` | raw QUL lemma present; should become null (no own reliable lemma) | yes |
| `replace` | raw QUL lemma present; should become a different own reliable lemma | yes |
| `keep` | reviewed suspicious case left unchanged (QUL accepted after review) | no |
| `exception` | reviewed non-correction that suppresses/justifies a diagnostic | no |

`keep` vs `exception` disambiguation (the plan flagged possible overlap):
- **`keep`** = "I reviewed the QUL-present value; the evidence is *insufficient to
  overrule* QUL, so it stays." Used for ambiguous/unmapped/null own evidence and
  valid-null missing cases.
- **`exception`** = "I reviewed a *known modeling divergence* (multi-STEM
  compound particles, etc.); QUL is *intentionally* accepted and this entry
  exists to suppress a hard check." Mutates nothing but carries stronger
  semantic "this is expected" intent.

Both are non-mutating; both record a reason.

## 3. Counts

### 3.1 By operation kind (active artifact = approved + accepted-exception)

| kind | count |
| --- | ---: |
| `add` | 1658 |
| `remove` | 70 |
| `replace` | 62 → **89** (Phase 0F: +27) |
| `keep` | 91 → **93** (Phase 0F: +2) |
| `exception` | 7 → **8** (Phase 0F: +1) |
| **active total** | 1888 → **1918** |
| `candidate` (blocker, **not** in active) | 30 → **0** |
| draft total (incl. blockers) | 1918 |

### 3.2 By decision

| decision | count |
| --- | ---: |
| `approved` | 1790 → **1817** (Phase 0F: +27 replace) |
| `accepted-exception` | 98 → **101** (Phase 0F: +2 keep, +1 exception) |
| `candidate` (blocker) | 30 → **0** |

### 3.3 By Phase 0 gate

| Phase | problem class | resolved (approved/keep/exception) | blocker |
| --- | --- | ---: | ---: |
| **0A** | shift-63 / shift-63-replace | 126 (63 add + 60 remove + 3 replace) | **0** ✅ |
| **0B** | shift-59 (59 §11 + 7 secondary-chain) | 125 (59 add + 56 replace + 10 remove) | **1** |
| **0C** | missing-recovery (1,595 raw candidates) | 1536 approved add (≈59 absorbed into shift targets via dedup) | **0** ✅ |
| **0D** | uncertain (86 QUL-present) + no-reliable-map (48) | 91 keep + 3 replace (invalid multi-word lemma) | **29** |
| **0E** | multi-STEM compound | 7 exception | **0** ✅ |

## 4. Gate status

| Gate | Status |
| --- | --- |
| 0A — reconcile 63 | **GREEN** (63 add + 60 remove + 3 replace; 3 spot-checks correct) |
| 0B — curate 59+7 | **GREEN** (Phase 0F: `33:61:2` → exception) |
| 0C — missing recovery | **GREEN** (all 1,595 resolved: 1536 approved add + ~59 absorbed + 0 multistem-keep promoted) |
| 0D — uncertain/no-map | **GREEN** (Phase 0F: 29 blockers → 27 replace + 2 keep) |
| 0E — multi-STEM | **GREEN** (7 compound divergences → exception; no one-STEM auto-correction) |
| **Active artifact implementation-ready** | **YES** (0 blockers; 1918 entries; validator GREEN; `implementationReady: true`) |
| **MASTER GATE** | **GREEN** |
| **Code implementation may start** | **YES** (importer implementation per the full-normalization plan) |

## 5. Phase 0A detail — the 63 reconciled (GREEN)

Converted from schemaVersion 1 paired entries to schemaVersion 2 flat entries:

- **63 `add`** at the content/target locations (lemma recovered).
- **60 `remove`** at defect locations that have **no** own reliable lemma
  (rootless particle/pronoun words).
- **3 `replace`** at defect locations that **own** their own reliable lemma —
  the core correction vs. the old draft which would have nulled them:
  - `3:33:7` `ءَال` → `إِبْرَاهِيم` (`<iboraAhiym`, 55/56 = 98.2%)
  - `21:51:3` `آتَى` → `إِبْرَاهِيم` (`<iboraAhiym`, 55/56 = 98.2%)
  - `28:50:11` `أَضَلّ` → `مِن` (`man` 870/870 = 100%; `min` 3103/3225 = 96.2%)

Spot-checks (validated): `3:33:7`→إِبْرَاهِيم, `21:51:3`→إِبْرَاهِيم,
`28:50:10`→أَضَلّ (add), `28:50:11`→مِن (replace). Zero duplicates; every
`expectedCurrentLemmaArabic` matches raw QUL.

## 6. Phase 0B detail — the 59 §11 + 7 secondary-chain candidates

All 59 audit §11 candidates are confirmed (every one independently reproduced by
the deterministic scan). Decisions per the plan's rule:

- **`add` at the previous content location** + **`replace` at the defect
  location** when the defect owns a reliable lemma (e.g. `2:126:23 قَلِيلًا`
  carries `مَّتَّعْ` but owns `قَلِيل` → replace; `3:116:15 ٱلنَّارِ` carries
  `أَصْحَٰب` but owns `نَار` → replace).
- **`add`** + **`remove`-to-null** when the defect has no own lemma
  (own Arabic candidate `-`, e.g. `3:49:13 لَكُم`, `28:79:12 لَنَا`,
  `41:15:19 هُوَ`).

The deterministic scan additionally surfaced **7 secondary-chain detections**
not listed in §11 (e.g. `3:116:16`, `3:33:8`, `4:54:13`, `5:41:14`,
`7:134:18`, `58:17:13`) — these are locations whose previous word is itself an
§11 candidate. They are real shifts (not hidden); each was given the same
add/replace/remove decision and evidence.

**1 blocker** (kept honest, not auto-resolved):
- `33:61:2` `أَيْنَمَا` — flagged as a shift-59 add target, but the previous word
  *already has* a QUL lemma, so it is not a clean "lemma shifted off a missing
  word" case. Requires scholarly review of the `أَيْن`/`أَيْنَمَا` modeling.
  **→ Resolved in §14: `exception` (multi-STEM compound particle).**

## 7. Phase 0C detail — missing recovery (GREEN)

1,595 raw QUL-missing words with reliable Arabic candidates. Every candidate
received a final decision:

- **1,536 approved `add`** (reliable single-STEM Buckwalter→Arabic mapping,
  unambiguous, not contradicted by own Corpus evidence). The ~59-candidate
  difference vs. 1,595 is the overlap with the shift-63/59 target locations,
  de-duplicated to a single operation per location (the shift-class entry wins,
  same corrected value).
- **0 ambiguous mappings promoted** (the 9 ambiguous Buckwalters excluded).
- **0 multi-STEM auto-adds** (multi-STEM words excluded from automatic add; the
  one multi-STEM true defect `28:50:11` is handled as a `replace` in 0A).
- **0 blockers.**

Example approved add: `2:10:11 كَانُوا۟` → `كَانَ` (`kaAna`, 1000/1005 = 99.5%).

## 8. Phase 0D detail — uncertain + no-reliable-map (RED: 29 blockers)

130 uncertain/manual-review-equivalent cases (86 QUL-present unsupported + 48
no-reliable-map) resolved as follows:

- **91 `keep`** — QUL accepted:
  - 22 with no own STEM lemma evidence (null) → no contradicting evidence.
  - 25 with only ambiguous own evidence (below reliable threshold) → insufficient
    to overrule QUL.
  - 48 QUL-missing with Corpus evidence but **no reliable Arabic mapping** →
    valid null; absence accepted (not a blocker — absence is acceptable when no
    reliable recovery exists).
- **3 `replace`** — mechanical rule: QUL lemma is structurally invalid
  (a two-word token stored as one word's lemma, e.g. `بَعْدَ مَا` on a single
  `بَعْدَ` word). Auto-replaced with the own reliable lemma (`بَعْد`):
  `13:37:8`, `2:181:3`, `8:6:4`.
- **7 `exception`** — multi-STEM compound divergences (أَنَّمَا, etc.; see 0E).
- **29 `blocker`** — QUL-present words whose **own reliable Corpus mapping
  conflicts with QUL's chosen lemma**. These are genuine internal QUL
  inconsistencies, but overruling QUL's lemma choice is a Quranic-linguistic
  decision (e.g. `4:116:1 إِنّ`→`أَنّ` changes meaning; `17:7:22 عَلا`→`تَعَٰلَىٰ`
  is a different lemma). Per the tightening rule they are **not** auto-resolved
  and are **not** promoted. Full list: `12:53:7`, `17:7:22`, `18:57:26`,
  `18:96:16`, `20:123:16`, `20:40:29`, `24:61:63`, `2:144:23`, `2:148:9`,
  `2:187:19`, `2:203:16`, `2:221:19`, `33:5:25`, `33:6:25`, `35:18:24`,
  `35:41:13`, `3:161:16`, `4:116:1`, `4:162:19`, `58:22:44`, `5:44:29`,
  `60:10:45`, `6:101:14`, `6:151:11`, `6:94:25`, `74:31:27`, `7:38:32`,
  `7:38:34`, `9:93:10`.
  **→ Resolved in §14: 27 `replace` + 2 `keep` (`17:7:22`, `4:116:1`).**

## 9. Phase 0E detail — multi-STEM/compound (GREEN)

Explicit allow-list; no one-STEM heuristic auto-correction:

- `8:28:2`, `11:14:5`, `18:110:8`, `21:108:5`, `38:70:5`, `41:6:8` —
  `أَنَّمَآ` QUL `إِنّ` vs Corpus `>an~ + maA` → **exception** (legitimate
  compound divergence).
- `8:73:6` — `إِلَّا` QUL `إِلَّا` vs Corpus `<in + laA` → **exception**.
- `28:50:11` `مِمَّنِ` — the **one true multi-STEM defect** → **replace**
  `أَضَلّ`→`مِن` (handled in 0A, not auto-derived by a one-STEM rule).

Other compound particles (`إِنَّمَا`, `مِمَّا`, `عَمَّا`, `مِمَّن`) default to
`keep`/`exception` — none were auto-corrected.

## 10. Structured mapping evidence

Machine-readable: `word-lemma-arabic-mapping-evidence.json` (155 Buckwalter→Arabic
records actually exercised by approved operations). Each records: Buckwalter,
Arabic lemma, supporting occurrence count, total occurrence count, dominance %,
ambiguity status (`reliable`), up to 3 example locations, and
`allowAutoAddReplace` (true iff in the 4,797 reliable allow-list). This is what
the importer's validation (§6.1 "add/replace corrected Arabic must resolve under
a reliable mapping") will check against; the full 4,797 allow-list is
reproducible from `curation-tmp/reliable-mappings.json`.

## 11. Artifacts produced

| artifact | path | status |
| --- | --- | --- |
| Curation draft (may contain candidate/blocker) | `docs/feature-017-lexical-explorers-polish/word-lemma-normalization.draft.json` | written (1918 entries, validated) |
| Active artifact (staging) | `docs/feature-017-lexical-explorers-polish/word-lemma-normalization.active-staging.json` | **updated (1918 entries, validated, `implementationReady: true`)** |
| Mapping evidence | `docs/feature-017-lexical-explorers-polish/word-lemma-arabic-mapping-evidence.json` | 155 records (unchanged — all 27 replaces resolve against existing reliable records) |
| Active artifact target (backend, embedded) | `Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/Words/MorphologyImporting/Corrections/word-lemma-normalization.json` | **CREATED (Phase 0F)** — 1918 entries; SHA-256 `7df9cd8bd7cf837f88eee1a492cc3e24f3ac54f882a37c0f1e72b8603811fc50` |

The backend active artifact is now promoted (Phase 0F, 2026-06-28) because all
gates are GREEN and 0 blockers remain. It is **not** wired into the importer and
**not** added to the source-package manifest. No C# code was written.

## 12. Spot checks (all validated)

| location | expected | result |
| --- | --- | --- |
| `3:33:7` | replace → `إِبْرَاهِيم` | ✅ |
| `21:51:3` | replace → `إِبْرَاهِيم` | ✅ |
| `28:50:10` | add → `أَضَلّ` | ✅ |
| `28:50:11` | replace → `مِن` | ✅ |
| 59-candidate example `3:116:15` | replace → `نَار` (prev `3:116:14` → `أَصْحَٰب` add) | ✅ |
| 1595 example `2:10:11` | add → `كَانَ` | ✅ |
| multi-STEM `8:28:2` | exception → kept `إِنّ` | ✅ |

## 13. What is needed to turn gates GREEN

The 30 blockers are **scholarly judgment calls**, not data-mechanical fixes.
Resolving each requires a human Quranic-linguistic decision per location: confirm
the QUL value (`keep`/`exception`) **or** approve the own-reliable replacement
(promote to `replace`). Until that review is supplied for all 30, Phase 0B/0D
stay RED and no importer code may be written.

**→ Resolved in §14 (Phase 0F, 2026-06-28).**

## 14. Phase 0F — Blocker Resolution + Promotion (2026-06-28)

The 30 blockers were reviewed in
`word-lemma-phase-0f-blocker-resolution-review.md`, accepted by human sign-off,
and promoted into the curation artifacts. Outcome: **27 replace, 2 keep, 1
exception, 0 still-blocked**.

### 14.1 Decisions promoted

- **27 `replace`** (approved) — mechanical source-misalignment: each token's own
  reliable lemma replaces a QUL lemma that belongs to another word in the same
  ayah. All 27 are REL-canonical (corrected = `reliable-mappings[buckwalter]`),
  so each merges into the existing lemma dimension (no lemma split). `2:221:19`
  (`ءَامَنَ`→`مُؤْمِن`) kept at **medium confidence** (participle/verb; keep is
  the conservative fallback) per the review.
- **2 `keep`** (accepted-exception, non-mutating) — `17:7:22` (`عَلا`) and
  `4:116:1` (`إِنّ`): QUL is correct; the Corpus candidate is a modeling/mapping
  artifact (special-caution cases). Corpus must NOT override QUL here.
- **1 `exception`** (accepted-exception, non-mutating) — `33:61:2` `أَيْنَمَا`:
  multi-STEM compound particle (`أَيْن + مَا`); legitimate QUL-vs-Corpus modeling
  divergence (same family as `أَنَّمَآ`/`إِلَّا`).

### 14.2 Neighbour micro-check (`33:61:3`) — resolved to `replace` (2026-06-28)

Phase 0F flagged the *real* misalignment in 33:61 as the neighbour `33:61:3`
ثُقِفُوٓا۟ (carrying `أَيْن`). A focused micro-check **upgraded it from `remove`→null
to `replace` `أَيْن` → `ثُقِفُ`**, because under full-normalization rules a content
word must not be left null when its own lemma is well-supported:

- `33:61:3` is the **identical word** to `3:112:6` ثُقِفُوٓا۟, which QUL lemmatises `ثُقِفُ`.
- raw QUL root at `33:61:3` = `ث ق ف`; raw QUL **stem = ثُقِفُ**; only the word-level
  lemma is the leaked `أَيْن` from the neighbouring multi-STEM compound `أَيْنَمَا`
  (`33:61:2`, `أَيْن`+`مَا`).
- Corpus STEM: BW `vuqifu`, root `vqf`, POS `V` (content verb).
- QUL uses `ثُقِفُ` for **every** other QUL-present `vuqifu` word — `2:191:3`,
  `3:112:6`, `8:57:2`, `60:2:2` (4/5 = 80%; the lone exception is this defect) —
  identical canonical spelling `062B 064F 0642 0650 0641 064F`, so the replace
  **merges into the existing `ثُقِفُ` lemma dimension** (no spelling split).
- Evidence basis is QUL-usage-consistency (recorded in
  `word-lemma-arabic-mapping-evidence.json` as `vuqifu`→`ثُقِفُ`,
  `allowAutoAddReplace: false`); `vuqifu` is below the auto-reliable threshold but
  the curated `replace` is approved on the QUL-consistency + identical-word evidence.

**`4:91:26` micro-check (2026-06-28) — resolved to `add`.** Same verb (`vuqifu`),
**QUL-missing**, was previously `keep` (valid-null). Resolved under full
normalization to an approved **`add أَيْن→…` →** `add` with `correctedLemmaArabic:
ثُقِفُ`:

- `4:91:26` word `ثَقِفْتُمُوهُمْ` is the **identical surface** to `2:191:3`
  ثَقِفْتُمُوهُمْ (QUL lemma `ثُقِفُ`); raw QUL root `ث ق ف`, stem `ثَقِفْ`.
- Corpus: **single STEM**, BW `vuqifu`, root `vqf`, POS `V` (content verb).
- raw QUL word-level lemma is **absent** → `add` (not `replace`/`remove`).
- Same QUL-consistency basis as `33:61:3` (`vuqifu`→`ثُقِفُ`, canonical
  `062B 064F 0642 0650 0641 064F`, merges existing lemma dimension). Below the
  auto-reliable threshold; approved on curated QUL-consistency evidence
  (`allowAutoAddReplace: false`).
- **Implementation note:** this was applied by **converting** the existing
  valid-null `keep` entry for `4:91:26` to `add` (not appending a new entry), to
  keep one operation per location. Net count effect: `keep` 93→92, `add`
  1658→1659, active total **stays 1918** (the earlier "expected 1919" assumed the
  location was absent; it already existed as a keep).

### 14.3 Final counts (active artifact, 1918 entries)

| kind | count | | decision | count |
| --- | ---: | --- | --- | ---: |
| `add` | 1659 | | `approved` | 1818 |
| `remove` | 69 | | `accepted-exception` | 100 |
| `replace` | 90 | | `candidate`/blocker | 0 |
| `keep` | 92 | | | |
| `exception` | 8 | | | |
| **candidate/blocker** | **0** | | | |

> Counts after the two neighbour micro-checks (2026-06-28):
> - `33:61:3`: approved-`remove`→approved-`replace` (`remove` 70→69, `replace` 89→90; decision stays `approved`).
> - `4:91:26`: `keep`(accepted-exception)→`add`(approved) (`keep` 93→92, `add` 1658→1659; one entry moved out of accepted-exception into approved → `approved` 1817→1818, `accepted-exception` 101→100).
>
> Active total **1918** (unchanged — both micro-checks converted existing entries
> in place, no new entries). 0 blockers. add/replace resolvability: **1745
> auto-reliable + 4 curated QUL-consistency** (`5:5:9`, `5:5:12`→`حِلّ`;
> `33:61:3`, `4:91:26`→`ثُقِفُ`), each backed by an explicit mapping-evidence
> record (`allowAutoAddReplace: false`); global validation was **not** weakened.

### 14.4 Validation

- `validate.py draft.json` → **PASS** (1918 entries).
- `validate.py active.json` → **PASS** (1918 entries, active=True; 0 candidate/`_`-prefixed).
- Extra checks: 27/27 replaces REL-canonical; 0 add/replace contradict the reliable
  allow-list; all 70 `remove`s have no reliable own lemma; all `keep`/`exception`
  non-mutating; upstream QUL/Corpus source files unchanged; artifact absent from
  the source-package manifest.

### 14.5 MASTER GATE: **GREEN**

All gates 0A–0E GREEN; 0 blockers. The backend embedded artifact is promoted
(§11). **Importer implementation may now begin** per
`word-level-lemma-full-normalization-implementation-plan.md` (§5 onward). This
task wrote no C# code, no DI, no hard-checks, no report-builder, no migrations.
