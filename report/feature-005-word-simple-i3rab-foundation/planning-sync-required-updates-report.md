# Feature 005 — Planning Sync: Required Updates Before `/speckit.specify` (Review-Only)

**Type:** Review & synchronization plan only. No file edits, no code, no DB, no migrations, no Spec Kit artifacts.
**Branch:** `005-word-simple-i3rab-foundation` · **Date:** 2026-06-12
**Primary source of truth:** [`segment-pattern-rule-coverage-report.md`](segment-pattern-rule-coverage-report.md) (the **16:39** finalized inventory — 100% approved, final labels).

> This report compares every Feature 005 planning/report file against the finalized coverage report and
> lists exactly what must change **before** we run `/speckit.specify`. It changes **nothing** itself.

---

## 1. Verdict

**PASS WITH REQUIRED DOC UPDATES.**

The v1 design is sound and locked (inline `i3rab_*` columns on `quran_word_morphology_segments` + one
`quran_i3rab_rules` table; word summaries composed at read time; idioms/roles are read-layer;
**100% segment coverage**). **No design rework is needed.** However, three documentation problems must be
fixed before `/speckit.specify`, or the spec would inherit wrong numbers and an ambiguous column contract:

1. **Stale coverage numbers** in the planning report (still `97.17% / 2.83%`; final is `100% / 0%`).
2. **An unresolved data-model naming conflict** — `i3rab_status`/`i3rab_review_reason` (planning report)
   vs `i3rab_is_supported`/`i3rab_unsupported_reason` (coverage §9 + inventory §6/§7). Must be normalized.
3. **The machine-readable companions (JSON/CSV) are stale** — they still encode needs-review/unsupported
   statuses and pre-final labels, disagreeing with their own updated markdown. **(Resolved: both
   companions were subsequently deleted to prevent stale-seed use — see §4.4.)**

---

## 2. Files inspected

| # | File | Date | Role | State |
|---|---|---|---|---|
| 1 | `Backend/report/.../segment-pattern-rule-coverage-report.md` | 16:39 | **source of truth** | Current except §9 naming + 95.33% residual |
| 2 | `Backend/report/.../simple-i3rab-label-inventory-report.md` | 02:52 | sibling evidence (oldest) | **Superseded** — boolean naming + old label recs |
| 3 | `docs/.../feature-005-word-simple-i3rab-foundation-planning-report.md` | 03:43 | **spec input** | Stale numbers; correct (enum) naming |
| 4 | `Backend/report/.../segment-pattern-rule-coverage.json` | 03:30 | (former) companion | **Removed** — deleted in cleanup to prevent stale-seed use |
| 5 | `Backend/report/.../segment-pattern-rule-coverage.csv` | 03:30 | (former) companion | **Removed** — deleted in cleanup to prevent stale-seed use |
| 6 | `docs/feature-005-word-simple-i3rab-foundation/` (other docs) | — | — | none present (only file #3) |
| 7 | `specs/005-word-simple-i3rab-foundation/` | — | Spec Kit | **does not exist** (no drift; not yet started) |

---

## 3. Source-of-truth final decisions (extracted from the finalized coverage report)

These are the values everything else must match.

| Decision | Final value (coverage report) |
|---|---|
| Total readable words / morphology rows | 77,432 |
| Total segment rows | 128,219 |
| POS-only / kind+POS / enriched patterns | 358 / 371 / 1,337 |
| Segment-token signatures (rule basis) | **142** |
| Proposed rule families | **67** |
| **Segment approved-candidate** | **100.0%** (128,219 / 128,219) |
| **Segment needs-review** | **0.0%** |
| **Segment unsupported** | **0.0%** |
| All 142 segment-token signatures | **approved-candidate** (§3.4 legend: every row ✅) |
| Words displayable | **100.0%** |
| Data model | inline `i3rab_*` on `quran_word_morphology_segments`; keep `quran_i3rab_rules`; **no** `quran_word_segment_i3rab`; **no** `quran_word_i3rab` in v1 |
| Word summary | composed at read time (ordered segment labels joined with «، ») |
| Idiom collapses / role refinements | **read-layer only**, not importer |
| `V+PRON`, `ACC+PRON` role refinements | **read-layer interpretive notes**, *not* counted in segment-row coverage |
| Syntactic roles (فاعل/مفعول به/مبتدأ/خبر/حال) | **not stored/generated** in v1 |
| Label ownership | Feature 005 **rule layer owns Arabic labels**; `quran_pos_tags` is a technical dictionary only |

**Final approved labels now locked** (these supersede every older recommendation):

`INT → حرف تفسير` · `EXH → حرف تحضيض` · `SUR → حرف فجاءة` · `INL → حروف مقطّعة (فواتح السور)` ·
`EQ → همزة التسوية` · `VOC.SUFFIX → ميم عوض عن حرف النداء` · `COM → واو المعية` · `P.SUFFIX → لام الجر` ·
`N.GEN.1S → اسم مجرور مضاف إلى ياء المتكلم` · `PREV → ما الكافّة` · `INC → حرف ابتداء/استفتاح` ·
`EXL → حرف تفصيل` · `SUP → حرف زائد` · `AMD → حرف استدراك` · `SUB → حرف مصدري` · `RES → أداة حصر` ·
`STEM:INTG → اسم استفهام` · `PREFIX:INTG → همزة استفهام` · `T → ظرف زمان` · `REM → حرف استئناف`.

**Pattern-aware read-layer overrides locked** (per coverage §5):

- `P+SUB` (كَمَا/كَأَن/عَمَّا): per-segment display `جار، مجرور` (not the base `حرف مصدري` for all SUB).
- `SUP+AMD` (وَلَـٰكِن): segment labels `حرف زائد، حرف استدراك`; combined display `حرف استدراك`.
- `ACC+PREV` (إِنَّمَا): segment labels `حرف نصب، ما الكافّة`; combined display `كافّة ومكفوفة`.
- `P+PRON` / `P+N:GEN` / `P+N:GEN+PRON`: read-layer `جار ومجرور` / `… مضاف إليه`.

---

## 4. Drift found, by file

Severity legend: **[BLOCK]** = fix before `/speckit.specify` · **[CLEANUP]** = recommended before specify · **[NOTE]** = minor/clarity.

### 4.1 `docs/.../feature-005-...-planning-report.md` (spec input — highest priority)

| Location | Outdated text / decision | Required replacement | Severity |
|---|---|---|---|
| §1.1 evidence table | `Segment approved-candidate coverage 97.17%`; `Segment needs-review 2.83%` | `100.0%` / `0.0%` | **[BLOCK]** |
| §1.1 evidence table & §5.3 | `Word fully-approved 95.33%` | `100.0%` (all segments approved ⇒ all words fully-approved). **Verify**, then reconcile — see §4.2 | **[BLOCK]** |
| §5.3 closing line | `Inventory result: 0.0% unsupported, 2.83% needs-review, 97.17% approved-candidate` | `0.0% unsupported, 0.0% needs-review, 100.0% approved-candidate` | **[BLOCK]** |
| §5 idiom table | Lists only `P+PRON`, `P+N:GEN`, `P+N:GEN+PRON`, `INTG+V` | **Add** the three pattern-aware overrides (`P+SUB`→`جار، مجرور`; `SUP+AMD`→`حرف استدراك`; `ACC+PREV`→`كافّة ومكفوفة`) and the `V+PRON`/`ACC+PRON` read-layer role notes (`في محل نصب مفعول به` / `اسم إنّ`) | **[CLEANUP]** |
| §2.2 / §3.2 / §7.2 / §10 seed-label list | Lists a partial set (`REM, RES, T, AMD, INL, PREV, IMPV-prefix`) | **Expand** to the full final set incl. `SUB→حرف مصدري`, `STEM:INTG→اسم استفهام`, `EXL→حرف تفصيل`, `INT→حرف تفسير`, `EXH→حرف تحضيض`, `SUR→حرف فجاءة`, `EQ→همزة التسوية`, `VOC.SUFFIX→ميم عوض عن حرف النداء`, `COM→واو المعية`, `P.SUFFIX→لام الجر`, `N.GEN.1S→اسم مجرور مضاف إلى ياء المتكلم`, `INC→حرف ابتداء/استفتاح` | **[CLEANUP]** |
| §3.2 rules table `default_status` | `approved / needs_review for the family` | Clarify: **all 67 families are approved-candidate in v1**; `needs_review`/`unsupported` are schema-reserved (0 rows in v1) | **[NOTE]** |
| §3.1 columns | Uses `i3rab_status` + `i3rab_review_reason` (enum) | **Keep** — this is the recommended naming (see §5); the conflict is in the *other* files | **[NOTE]** |

### 4.2 `Backend/report/.../segment-pattern-rule-coverage-report.md` (source of truth — minor internal fixes)

| Location | Outdated text / decision | Required replacement | Severity |
|---|---|---|---|
| §9 data-model bullet | `inline … (i3rab_arabic, i3rab_rule_id, i3rab_is_supported, i3rab_unsupported_reason)` | Normalize to `i3rab_status` + `i3rab_review_reason` (see §5) | **[CLEANUP]** |
| §1 summary table | `Words fully-approved (all segments approved) 95.33%` | Inconsistent with `100% approved / 0% needs-review` (if every segment is approved, every word is fully-approved). **Reconcile to 100.0%** (or footnote the alternative definition). Verify against the JSON before changing. | **[CLEANUP]** |
| §8 coverage table | `Word display coverage: approved-only 95.33% / approved+needs-review 100.0%` | With needs-review = 0%, both columns must be equal → `100.0% / 100.0%` | **[CLEANUP]** |

> Everything else in this report (labels, 142-token table, 67 families, read-layer §5, NULL-form §6,
> §7 problematic-label table, §10) is **current and authoritative** — no change needed.

### 4.3 `Backend/report/.../simple-i3rab-label-inventory-report.md` (oldest sibling — superseded)

This is historical inspection evidence; the **coverage report supersedes it**. Recommended fix: add a
one-line **superseded banner** at the top pointing to the coverage report + planning report as
authoritative for final labels/model/status, rather than rewriting the analysis. Enumerated stale items
the banner supersedes:

| Location | Outdated text / decision | Now superseded by | Severity |
|---|---|---|---|
| §0 + §1.6 + §8.2 | `RES → حرف حصر/قصر` | `RES → أداة حصر` (approved) | **[CLEANUP]** |
| §1.6 | `T (head) … needs review`; `AMD → حرف استدراك (review)` | `T → ظرف زمان` (approved); `AMD → حرف استدراك` (approved) | **[CLEANUP]** |
| §8.2 | "Rare particles `SUR, AVR, EQ, COM, IMPN, INT, SUP` — label individually but **flag for sign-off**" | All **approved-candidate** with final labels (`SUR→حرف فجاءة`, `EQ→همزة التسوية`, `COM→واو المعية`, `INT→حرف تفسير`, `SUP→حرف زائد`, `IMPN→اسم فعل أمر`, `AVR→حرف ردع (كلّا)`) | **[CLEANUP]** |
| §8.2 + §2 | `V+PRON` / `ACC+PRON` pronoun role marked **needs-review** | **Read-layer** interpretive notes; not a segment-coverage gap | **[CLEANUP]** |
| §1.1 recommendations (not the seed column) | No correction carried for `PREV` (→`ما الكافّة`), `SUR` (`إذا الفجائية`→`حرف فجاءة`), `P` suffix (`حرف جر`→`لام الجر`), `VOC` suffix (`حرف نداء`→`ميم عوض عن حرف النداء`), `INL` (`قسم`→`حروف مقطّعة`), `N.GEN.1S` (→`اسم مجرور مضاف إلى ياء المتكلم`) | Final labels in §3 of this report | **[CLEANUP]** |
| §6.1 / §6.2 / §6.3 / §6.4 / §7 | Data model + validation use boolean `i3rab_is_supported` / `i3rab_unsupported_reason`; check `I3RAB-SUPPORTED-CONSISTENT` | `i3rab_status` + `i3rab_review_reason`; status-aware checks (see §5) | **[CLEANUP]** |

> Note: §1.1 explicitly shows the **current `quran_pos_tags` seed** labels (with ⚠️), so those cells are
> *accurate as a depiction of the seed* and should stay; only the **recommendation** sections (§1.4, §1.6,
> §8.2) are stale.

### 4.4 `…coverage.json` + `…coverage.csv` (former companions — removed)

Generated at **03:30**, *before* the **16:39** markdown finalization, and **not regenerated**. Verified by direct probe:

| Evidence (probe) | JSON | CSV | Implication |
|---|---|---|---|
| `needs-review` status tokens | 165 refs | 147 rows | markdown final = **0%** needs-review → companions stale |
| `unsupported` status tokens | 1 | 0 | markdown final = **0%** unsupported → JSON stale |
| `حرف حصر/قصر` (old RES) | 4 | 1 | should be `أداة حصر` |
| `إذا الفجائية` (old SUR) | 5 | 1 | should be `حرف فجاءة` |
| `حرف تحضيض (لولا/هلّا)` (old EXH, parenthetical) | 5 | 1 | should be `حرف تحضيض` (parenthetical → internal note) |
| `أنْ المفسِّرة` (old INT as label) | 5 | 1 | should be `حرف تفسير` (parenthetical → internal note) |
| `حرف فجاءة` (final SUR) | **0** | **0** | **missing** |
| `لام الجر` (final P.SUFFIX) | **0** | **0** | **missing** |
| `أداة حصر` (final RES) | **0** | **0** | **missing** |
| `اسم مجرور مضاف إلى ياء المتكلم` (final N.GEN.1S) | **0** | **0** | **missing** |

**Resolution (applied):** both companions were **deleted** (`segment-pattern-rule-coverage.json` /
`.csv`) to prevent accidental use as canonical rule seeds. The finalized markdown coverage report and the
planning report are **authoritative** for Feature 005. If a machine-readable rule seed is needed during
implementation, regenerate it later from the finalized **142-signature / 67-family** approved catalogue
(a read-only DB export — not done in this documentation pass). The probe table above is retained as the
historical record of *why* the snapshots were unsafe to keep.

---

## 5. Data-model naming conflict (must be normalized before `/speckit.specify`)

Two column contracts are in circulation:

| Naming | Where it appears | Shape |
|---|---|---|
| `i3rab_status` (text enum) + `i3rab_review_reason` | **planning report** §3.1 (newest, 03:43) | 3-state: `approved` / `needs_review` / `unsupported` |
| `i3rab_is_supported` (bool) + `i3rab_unsupported_reason` | coverage report §9; inventory §6/§7 (older) | 2-state boolean |

Both keep `i3rab_arabic` and `i3rab_rule_id` identically — the conflict is only the status/reason pair.

**Recommendation: standardize on `i3rab_status` (text enum) + `i3rab_review_reason`.** Reasons:

1. The analysis defined **three** meaningful states (approved-candidate / needs-review / unsupported-v1).
   A boolean cannot encode the middle `needs_review` state; you would need a second boolean — exactly the
   awkwardness an enum removes.
2. Even though v1 data collapsed to **100% approved / 0% needs-review / 0% unsupported**, the schema must
   still *represent* `needs_review`/`unsupported` for (a) future catalogue growth and new corpus patterns,
   (b) the deliberately-held-back read-layer role refinements, and (c) a review workflow. The enum is their
   natural home; a boolean discards the concept.
3. It is the most recent, considered decision and matches the reports' own three-state narrative.
4. It is extensible (e.g. add `experimental`) without a schema change.

**Consequently:** keep the planning report's naming; update the **coverage report §9** and the
**inventory §6/§7** to the enum. Do **not** silently flip the planning report to boolean.

> Edge case to record in the spec: with no `unsupported` rows in v1, the
> "`unsupported` requires `i3rab_review_reason`" check is vacuously satisfied — keep the check anyway for
> future safety.

---

## 6. Final proposed update plan (no edits performed in this pass)

Apply in this order, **documentation only**:

1. **`docs/.../feature-005-...-planning-report.md`** *(blocking)*
   - §1.1 evidence table: `97.17%→100.0%`, `2.83%→0.0%`; reconcile `95.33%→100.0%` (verify first).
   - §5.3 closing line: `→ 0.0% unsupported, 0.0% needs-review, 100.0% approved-candidate`.
   - §5: add the three **pattern-aware read-layer overrides** + the `V+PRON`/`ACC+PRON` role notes.
   - §2.2 / §3.2 / §7.2 / §10: expand the **seed-label correction list** to the full final set (§3 here).
   - §3.2: clarify all 67 families are `approved` in v1 (`needs_review`/`unsupported` reserved).
   - Keep §3.1 naming (`i3rab_status` / `i3rab_review_reason`) — it is the standard.

2. **`Backend/report/.../segment-pattern-rule-coverage-report.md`** *(cleanup)*
   - §9: rename `i3rab_is_supported`/`i3rab_unsupported_reason` → `i3rab_status`/`i3rab_review_reason`.
   - §1 + §8: reconcile the `95.33%` word figures to `100.0%` (or footnote the definition).

3. **`Backend/report/.../simple-i3rab-label-inventory-report.md`** *(cleanup)*
   - Add a top **"Superseded — see coverage report & planning report for final labels/model/status"** banner.
   - Optionally align §6/§7 naming and the §1.6/§8.2 label recommendations (or rely on the banner).

4. **`…coverage.json` + `…coverage.csv`** *(done — removed)*
   - **Deleted** to prevent accidental use as canonical rule seeds. Regenerate later from the finalized
     142-signature / 67-family approved catalogue only if implementation needs a machine-readable seed.

No `specs/005-…` changes (doesn't exist). No DB, code, or migration changes anywhere.

---

## 7. Safe follow-up prompt for the next step (documentation updates only)

> Paste this after approving the plan above to apply the doc updates (still no code/DB/migration/spec):

```
Apply the Feature 005 planning-sync documentation updates from
Backend/report/feature-005-word-simple-i3rab-foundation/planning-sync-required-updates-report.md.

Documentation-only. Do not change code, DB, migrations, Spec Kit artifacts, or any
Backend/Frontend source. No commit.

1. docs/.../feature-005-word-simple-i3rab-foundation-planning-report.md (BLOCKING):
   - §1.1: segment approved 97.17%→100.0%, needs-review 2.83%→0.0%; word fully-approved
     95.33%→100.0% (since all 142 segment tokens are approved, every word is fully-approved).
   - §5.3 closing line → "0.0% unsupported, 0.0% needs-review, 100.0% approved-candidate".
   - §5: add the read-layer pattern-aware overrides — P+SUB → "جار، مجرور"; SUP+AMD →
     combined "حرف استدراك"; ACC+PREV → combined "كافّة ومكفوفة"; and the V+PRON / ACC+PRON
     role notes (في محل نصب مفعول به / اسم إنّ) as read-layer, not segment coverage.
   - Expand the seed-label correction list to the full final set (INT/EXH/SUR/INL/EQ/
     VOC.SUFFIX/COM/P.SUFFIX/N.GEN.1S/PREV/INC/EXL/SUP/AMD/SUB/RES/STEM:INTG/PREFIX:INTG/T/REM).
   - §3.2: note all 67 families are "approved" in v1; needs_review/unsupported are reserved.
   - Keep the i3rab_status + i3rab_review_reason column naming.

2. Backend/report/.../segment-pattern-rule-coverage-report.md (CLEANUP):
   - §9: rename i3rab_is_supported/i3rab_unsupported_reason → i3rab_status/i3rab_review_reason.
   - §1 and §8: reconcile the 95.33% word figures to 100.0%.

3. Backend/report/.../simple-i3rab-label-inventory-report.md (CLEANUP):
   - Add a "Superseded — final labels/model/status live in the coverage & planning reports" banner.

The JSON/CSV companions were deleted (pre-final snapshots, unsafe as rule seeds) — do not recreate
them in this pass; regenerate later from the finalized 142/67 catalogue only if a machine-readable
seed is needed. Report the exact edits made.
```

---

### Quranic data safety
This review reads existing reports only, modifies nothing, invents no Qur'anic text or grammar, and
preserves the rule that Feature 005 adds **derived labels only** — never altering morphology source
fields and never replacing original morphology data. Simplified labels are **not** authoritative
scholarly i‘rab.
