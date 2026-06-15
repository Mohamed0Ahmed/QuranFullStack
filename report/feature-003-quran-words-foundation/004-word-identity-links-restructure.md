# Feature 003 — Word Identity Links Restructure (Analysis Report)

**Date:** 2026-06-10
**Status:** Report only. No code, no migrations, no DB changes, no commits.
**Scope:** Analyze how to restructure Feature 003 so each readable `quran_words` row can
expose stable identity links (`unique_tashkeel_word_id`, `unique_simple_word_id`) to the
unique word tables after `rebuild-words`, and so that "without-tashkeel" identity is based on
`word_key_imlaei_simple` instead of `text_uthmani_simple`.

All numbers below were measured directly from the enriched import sources
(`resources/import-sources/quran-foundation/words/*.json`) over the **authoritative readable
set** (the 77,432 words flagged readable by imlaei-digit marker detection, joined by
`location`). They are not copied from prior expectations.

---

## 1. Verdict / Recommendation

**Recommendation: proceed, in two separable concerns, with one blocking decision to confirm first.**

1. **Switch `unique_simple` identity to `word_key_imlaei_simple`** (away from `text_uthmani_simple`).
   This is the substantive product change and is well-founded: the clean imlaei key yields
   **14,783** distinct readable forms — exactly the long-standing "without-tashkeel"
   expectation — whereas the current `text_uthmani_simple` grouping yields **15,826**
   (confirmed by the existing `003-words-unique-tables-audit.md`). Display stays Uthmani.

2. **Add per-occurrence identity links so Mushaf/page reads can color words.** The user's
   preference — **nullable columns on `quran_words`** — is the right call **for read
   ergonomics**, and I confirm it as the recommended shape (Option A), *with two refinements*:
   - The link columns must be **nullable plain `integer`s populated by the rebuild**, not
     classic enforced FKs to the unique tables, because the unique tables are rebuilt with
     `TRUNCATE … RESTART IDENTITY` (see §6, §9). A hard FK to a table that is truncated every
     rebuild creates an operational ordering problem and buys little.
   - **Surrogate-id stability is a prerequisite, not a detail.** The product wants Gates/Topics
     to link to `unique_simple_word_id = N` and have the page API color by that id. That only
     works if `N` is **stable across rebuilds**. Today it is **not** (`RESTART IDENTITY`
     reassigns ids). This is the one decision that must be settled before implementation
     (see §14, Decision D1).

**Headline challenge to the current preference:** the columns-on-`quran_words` shape is fine;
the *durability of the id being linked* is the real risk. Resolve id stability (or link Gates
by the natural key `word_key_imlaei_simple`) and Option A is safe and clean.

---

## 2. Current State

### 2.1 Tables / entities

| Concern | Table | Entity | EF config |
| --- | --- | --- | --- |
| Ordered tashkeel | `quran_words_ordered_tashkeel` | `OrderedTashkeelWord` | `OrderedTashkeelWordConfiguration` |
| Ordered simple | `quran_words_ordered_simple` | `OrderedSimpleWord` | `OrderedSimpleWordConfiguration` |
| Unique tashkeel | `quran_words_unique_tashkeel` | `UniqueTashkeelWord` | `UniqueTashkeelWordConfiguration` |
| Unique simple | `quran_words_unique_simple` | `UniqueSimpleWord` | `UniqueSimpleWordConfiguration` |

All four are populated by `SqlDisplayWordsRebuilder` using the four `INSERT … SELECT`
statements in `DisplayWordsSql`, inside one transaction, behind the `rebuild-words` verb
(`tools/QuranDashboard.DataImporter/Program.cs`).

### 2.2 What each currently groups by

| Table | Grouping key (today) | First-occurrence ordering |
| --- | --- | --- |
| `quran_words_ordered_tashkeel` | per-occurrence (1 row per readable word); stats joined by `text_uthmani` | `ROW_NUMBER() OVER (ORDER BY id)` |
| `quran_words_ordered_simple` | per-occurrence; stats joined by `text_uthmani_simple` | same |
| `quran_words_unique_tashkeel` | **`GROUP BY text_uthmani`** | `DISTINCT ON (text_uthmani) … ORDER BY text_uthmani, word_order_in_mushaf` |
| `quran_words_unique_simple` | **`GROUP BY text_uthmani_simple`** | `DISTINCT ON (text_uthmani_simple) … ORDER BY text_uthmani_simple, word_order_in_mushaf` |

### 2.3 Does simple grouping use `text_uthmani_simple` or `word_key_imlaei_simple`?

**`text_uthmani_simple`.** The clean imlaei key (`word_key_imlaei_simple`) was added by the
Feature 002 binding change (migration `20260610023128_AddWordKeyImlaeiSimple`) but the Feature
003 SQL has **not** been switched to it. `DisplayWordsSql` does not reference
`word_key_imlaei_simple` at all.

### 2.4 Does `quran_words` reference unique rows?

**No.** The reference direction today is the opposite: the derived tables point *back* to
`quran_words`.

- `quran_words_ordered_*` → `quran_words` via `quran_word_id` (unique index, FK).
- `quran_words_unique_*` → `quran_words` via `first_quran_word_id` (FK).
- `QuranWord` (Domain) has **no** `unique_tashkeel_word_id` / `unique_simple_word_id` and no
  navigation to any unique/ordered table.

### 2.5 Measured distinct counts (authoritative readable set, 77,432 words)

| Candidate key | Distinct readable forms | Role |
| --- | --- | --- |
| `text_uthmani` (with tashkeel) | **21,294** | current unique-tashkeel grouping |
| `text_uthmani_simple` | **15,826** | current unique-simple grouping |
| `text_imlaei_simple` (raw) | 14,881 | not a grouping key (carries ۞/۩/RLM variants) |
| `word_key_imlaei_simple` (clean) | **14,783** | proposed unique-simple grouping |

The prior-project informational figures baked into `DisplayWordsInvariants`
(`InformationalUniqueTashkeel = 21,210`, `InformationalUniqueSimple = 14,783`) are emitted as
**warnings only** (`UNQ-EXPECT-TASHKEEL`, `UNQ-EXPECT-SIMPLE`) and currently deviate from the
real `text_uthmani`/`text_uthmani_simple` counts (21,294 / 15,826). Notably the simple
informational figure (14,783) already equals the imlaei-clean distinct — strong evidence that
the long-standing "without-tashkeel" expectation was always the imlaei identity, and the
current `text_uthmani_simple` grouping has simply never matched it.

---

## 3. Target Model

Confirmed and refined against the measured data:

- ✅ `unique_tashkeel_words` stays based on **Uthmani with tashkeel** (`text_uthmani`). For now,
  leave its grouping unchanged (see §8 for the accepted risk).
- ✅ `unique_simple_words` becomes based on **`quran_words.word_key_imlaei_simple`** (clean
  imlaei identity), not `text_uthmani_simple`.
- ✅ Every **readable** `quran_words` row receives `unique_tashkeel_word_id` and
  `unique_simple_word_id`.
- ✅ **Ayah-marker rows keep both IDs NULL** (markers are excluded from all derived tables; the
  existing `ORD-NO-MARKERS` check already guarantees no marker leaks into the derived tables).
- ✅ Mushaf/page APIs can return these IDs for coloring/linking.
- ✅ `word_key_imlaei_simple` remains a backend grouping/search/rebuild key; it does **not** need
  to appear in Mushaf DTOs (the surrogate `unique_simple_word_id` is enough for coloring).

One addition the product statement implies but does not state: for the page API to color "all
no-tashkeel forms of this word," the **`unique_simple_word_id` returned in a page must be the
same id a Gate/Topic was linked to**. That equality is the crux of §14/D1.

---

## 4. Schema Options

Three ways to attach `unique_tashkeel_word_id` / `unique_simple_word_id` to a readable
occurrence.

### Option A — nullable columns on `quran_words`

Add `unique_tashkeel_word_id` and `unique_simple_word_id` directly to `quran_words`.

| Dimension | Assessment |
| --- | --- |
| Pros | Mushaf page rendering already reads `quran_words`; coloring ids come back with **zero joins**. Simplest possible page DTO. A `WHERE unique_simple_word_id = @id` page query is index-friendly. |
| Cons | `quran_words` mixes **immutable foundation** columns (imported) with **derived** columns (rebuild-populated). Columns are NULL between `import-foundation` and `rebuild-words`. Rebuild must `UPDATE` 77,432 rows after building the unique tables. If modeled as real FKs, the `TRUNCATE … RESTART IDENTITY` rebuild fights the FK. |
| Mushaf rendering | Best: single-table read. |
| Gate/Topic linking | Page-side filtering is trivial (`WHERE unique_simple_word_id = @gateWordId`). Durability still depends on id stability (D1), independent of option. |
| Rebuild complexity | +1 `UPDATE` pass per key (2 updates), after unique tables exist. |
| Migration complexity | 2 nullable `integer` columns + 2 indexes (+ optional 2 FKs). Low. |
| Performance | Best read path. Extra write pass on rebuild is one-time/offline. |
| Clean Architecture | Foundation entity `QuranWord` gains derived attributes — a mild SRP/layering smell. Tolerable if the columns are plain nullable ints documented as rebuild-owned, not navigations. |

### Option B — separate mapping table `quran_word_identity_links`

`quran_word_id` (PK, FK → `quran_words`), `unique_tashkeel_word_id`, `unique_simple_word_id`.

| Dimension | Assessment |
| --- | --- |
| Pros | `quran_words` stays **pure foundation**. Derived links live with the other derived tables and are **truncated/rebuilt with them** — clean lifecycle symmetry. No nullable columns on foundation. Cleanest layering. |
| Cons | Mushaf page API must **JOIN** `quran_words → quran_word_identity_links` (1:1 on PK — cheap, but it is an extra join on every page). One more table + entity + config + rebuild step. |
| Mushaf rendering | One indexed 1:1 join; negligible cost, slightly more query/DTO plumbing. |
| Gate/Topic linking | Same as A once joined. |
| Rebuild complexity | Build links table from `ordered_*`/unique join; `TRUNCATE` it with the rest (no `UPDATE` of foundation). |
| Migration complexity | 1 new table + indexes + FKs. Comparable to A. |
| Performance | One extra join per page; immaterial at 1:1 on PK. |
| Clean Architecture | **Best**: derived stays out of the foundation aggregate. |

### Option C — links only in ordered display tables

Add `unique_tashkeel_word_id` to `ordered_tashkeel` and `unique_simple_word_id` to
`ordered_simple`; Mushaf API joins `quran_words → ordered_* → unique_*`.

| Dimension | Assessment |
| --- | --- |
| Pros | No new columns on `quran_words`; ordered tables are already 1:1 with readable words and already FK to `quran_words`. |
| Cons | Mushaf rendering must join **two** ordered tables (one per key) to color a page — and the ordered tables exist to express *display ordering*, not to be the page render's link source. Semantically muddy; two joins per page. |
| Mushaf rendering | Worst: two joins, and conceptual coupling of "ordering" tables into the render path. |
| Gate/Topic linking | Works but indirected. |
| Rebuild complexity | The id must still be computed and stored; similar effort to A/B but spread across two tables. |
| Migration complexity | 2 columns across 2 tables + indexes. |
| Performance | Two joins per page. |
| Clean Architecture | Overloads the ordered tables' responsibility. |

### Verdict on options

It is genuinely **A vs B**; C overloads the ordered tables and is not recommended.

- **A** optimizes the hot path (page rendering reads `quran_words`) at the cost of a small
  foundation-purity compromise.
- **B** optimizes architectural purity at the cost of one cheap 1:1 join per page.

I **confirm the user's preference for A** as the recommended shape, because Mushaf rendering
is the dominant, latency-sensitive read and "color this page" should not require a join fan-out
to display tables. But I explicitly note **B is the more Clean-Architecture-pure option** and is
the right fallback if the team prefers to keep `quran_words` strictly foundation-only. The
choice is a deliberate trade and is listed as Decision D2 (§14).

---

## 5. Recommended Schema

**Recommended: Option A, with link columns as nullable `integer`s, populated by `rebuild-words`,
guarded by hard validation checks — not enforced FKs to the unique tables.**

On `quran_words`:

| Column | Type | Null | Meaning |
| --- | --- | --- | --- |
| `unique_tashkeel_word_id` | `integer` | **nullable** | → `quran_words_unique_tashkeel.id` for this occurrence's tashkeel form |
| `unique_simple_word_id` | `integer` | **nullable** | → `quran_words_unique_simple.id` for this occurrence's clean-imlaei form |

Constraints / indexes (see §9 for the reasoning):

- **Nullable** because the columns are empty after `import-foundation` and before
  `rebuild-words`, and because **ayah markers stay NULL permanently**.
- **No hard DB FK** to the unique tables *while those tables use `TRUNCATE … RESTART IDENTITY`*.
  A real FK to a table that is fully truncated each rebuild forces null-out-then-repopulate
  dancing and adds nothing the rebuild checks don't already prove. (If D1 makes unique ids
  stable via upsert instead of truncate, a deferred/managed FK becomes viable and could be
  added then.)
- **Indexes:** two filtered b-tree indexes
  `WHERE is_ayah_marker = false AND unique_simple_word_id IS NOT NULL` (and the tashkeel
  analogue) to make "color every word on this page whose `unique_simple_word_id = @id`" and
  Gate-driven lookups fast without indexing the all-NULL marker rows.
- **Validation:** hard checks that every readable row has both ids non-null and every marker row
  has both null (see §12).

If the team chooses **Option B** instead, the same column set, nullability, indexing, and checks
apply to a `quran_word_identity_links` row keyed by `quran_word_id` — only the lifecycle changes
(truncate-and-rebuild vs update-in-place).

---

## 6. Rebuild Flow

The links can only be computed once the unique tables exist, so the rebuild order must be:

1. `TRUNCATE` derived tables (existing `TruncateDerivedTables`). **If Option A**, also null the
   two link columns on `quran_words` for readable rows first (so a partial/failed rebuild never
   leaves stale ids). **If Option B**, the links table is truncated here with the rest.
2. Build `quran_words_unique_tashkeel` (group by `text_uthmani`) — **unchanged**.
3. Build `quran_words_unique_simple` (**group by `word_key_imlaei_simple`** — the change) with
   representative Uthmani display fields (see §7).
4. Build `quran_words_ordered_tashkeel` and `quran_words_ordered_simple` (the ordered-simple
   stats join must also move to `word_key_imlaei_simple` for internal consistency — see §7.4).
5. **Populate the links:**
   - `unique_tashkeel_word_id` = the `unique_tashkeel.id` whose `text_uthmani` equals the row's
     `text_uthmani`.
   - `unique_simple_word_id` = the `unique_simple.id` whose key equals the row's
     `word_key_imlaei_simple`.
   - Option A: two `UPDATE quran_words … FROM quran_words_unique_* WHERE is_ayah_marker = false`.
   - Option B: one `INSERT … SELECT` into `quran_word_identity_links`.
6. Run **hard checks** (existing + new link checks, §12) inside the same transaction; commit only
   if all hard checks pass, else roll back (current `SqlDisplayWordsRebuilder` already does
   exactly this commit/rollback gating).

**Should this live inside `rebuild-words --force`?** **Yes.** It is the natural home: the command
already truncates+rebuilds all derived state in one transaction and validates before commit.
Adding the unique-simple key switch and the link population keeps a **single atomic rebuild** and
preserves the existing guarantee that the source `quran_words` foundation counts are untouched
(`SRC-UNTOUCHED`). The only nuance for Option A is that `quran_words` is now *partially* written
(two derived columns) during rebuild — `SRC-UNTOUCHED` must be redefined to check the
**foundation columns / row counts**, not "no write to the table at all" (see §12, §14/D3).

---

## 7. Unique Simple Redesign

### 7.1 Identity key

Group by **`word_key_imlaei_simple`** (readable rows only). Measured result: **14,783** rows.
This replaces the current `text_uthmani_simple` grouping (15,826 rows).

### 7.2 Should the table store `word_key_imlaei_simple`?

**Yes** — store it as the table's natural/identity column (e.g. `word_key_imlaei_simple`), with a
**unique index**. Reasons: it is the grouping key, the join target for link population, the
durable natural key for Gate/Topic linking (D1), and it makes the table self-describing for
admin/debug. The current unique index on `text_uthmani_simple` moves to this column.

### 7.3 Should it store representative Uthmani display fields?

**Yes.** Identity is imlaei; **display must stay Uthmani/Mushaf**. Keep representative display
columns chosen deterministically from the **first occurrence** (the `DISTINCT ON … ORDER BY key,
word_order_in_mushaf` already gives a deterministic first occurrence):

- `text_uthmani` (representative, with tashkeel) — for reverent display.
- `text_uthmani_simple` (representative) — optional, for a "simple Uthmani" display variant.
- `qpc_glyph` (representative) — **recommended to add** so an admin UI can render the word in the
  Mushaf font without re-joining to `quran_words`. (Currently the unique tables do not carry
  `qpc_glyph`; adding the representative glyph is a small, useful enrichment.)
- The existing `first_*` provenance columns (`first_quran_word_id`, `first_location`,
  `first_surah_number`, `first_ayah_number`, `first_word_order_in_mushaf`, `first_page_number`,
  `first_line_number`) stay and remain the deterministic representative occurrence.

### 7.4 Display vs grouping divergence — the `الرحمان` case

This is the crux of "group by imlaei, display Uthmani," and it is **lossy by design**:

- The clean imlaei key for "the Most Merciful" is **`الرحمان`** (imlaei spelling, with the long
  alif). It has **45** readable occurrences (measured).
- Within those 45 occurrences the **Uthmani** display form is **not unique** — Uthmani writes it
  `ٱلرَّحْمَـٰن`/`الرحمن` (dagger-alif, no full alif), and even the Uthmani-simple form differs from
  the imlaei key. So one imlaei identity group maps to one *representative* Uthmani display, but
  the underlying Uthmani forms inside the group can vary by vocalization.
- **Consequence:** `unique_simple` rows are keyed/counted by imlaei (`الرحمان`, 45) but shown in
  Uthmani (`الرحمن`). The admin UI must understand that the *identity/stat* is the imlaei key and
  the *label* is a representative Uthmani string. Surfacing `word_key_imlaei_simple` in the
  **admin** DTO (not the public Mushaf DTO) is recommended so a curator can see why two
  Uthmani-looking variants share one identity.

Other measured anchors that this key choice fixes vs `text_uthmani_simple`:

| Clean imlaei key | Occurrences | Note |
| --- | --- | --- |
| `الله` | **2,155** | ۞-prefixed variants collapse onto the bare word (was split under raw text) |
| `العظيم` | **36** | sajdah ۩ / RLM variants collapse |
| `الرحمان` | **45** | imlaei spelling; Uthmani display is `الرحمن` |
| `ال ياسين` | **1** | stays a single multi-token key (not auto-joined) |
| `دايرة` (5:52:12) | **3** | imlaei `دايرة` (yāʾ), not Uthmani `دائرة`; unchanged by cleaning |

### 7.5 Public/admin DTO

- **Admin word-table DTO:** display Uthmani (`text_uthmani` representative), but **group/filter/
  sort by the imlaei identity**, and expose `word_key_imlaei_simple` for transparency plus the
  occurrence/ayah/surah stats.
- **Public/Mushaf DTO:** does **not** need `word_key_imlaei_simple`; the surrogate
  `unique_simple_word_id` is sufficient for coloring.

---

## 8. Unique Tashkeel Handling

**Keep grouped by raw `text_uthmani`** for now (product decision: with-tashkeel stats stay on the
original Uthmani-with-tashkeel text). Measured distinct = **21,294**.

**Known, accepted risk:** raw `text_uthmani` still carries some non-letter Quranic marks that
split otherwise-identical words into separate "unique" forms. Concretely, the delta between the
measured 21,294 and the prior-project informational 21,210 (≈84 forms) is consistent with
mark-driven splitting (e.g. a word carrying a sajdah ۩ / rub ۞ / waqf annotation becomes a
distinct `text_uthmani` from the same word without it). The same kind of pollution that the
imlaei *clean* key removes for the simple side is **left in place** on the tashkeel side by this
decision.

- **Status:** accepted for now; with-tashkeel identity remains "exactly as written in Uthmani,"
  marks included. The `UNQ-EXPECT-TASHKEEL` warning will continue to report the deviation
  (21,294 vs 21,210) as informational, not a failure.
- **Future option (not in scope):** introduce a `word_key_uthmani_tashkeel` clean key (strip only
  annotation/waqf/sajdah/rub marks, keep harakat) if curators later want tashkeel identity that is
  not split by non-vocalization marks. Flag only; no action now.

---

## 9. QuranWord Identity Links

### 9.1 Nullability

**Both columns nullable.** Justified by lifecycle:

- After `import-foundation`, before `rebuild-words`: **NULL** for all rows (links not built yet).
- After a successful `rebuild-words`: **every readable row has both ids non-null**.
- **Ayah-marker rows: permanently NULL** (markers are not in any unique table).

A `NOT NULL` column is therefore impossible without a sentinel, and a sentinel would corrupt the
"markers are null" semantics. Nullable + hard validation is the correct contract.

### 9.2 FK constraints

**Recommended: no enforced FK to the unique tables in the truncate-rebuild model.** The unique
tables are emptied and re-id'd every rebuild (`TRUNCATE … RESTART IDENTITY`); a real FK would
require nulling all 77,432 references before every truncate and re-pointing them after — pure
overhead for a guarantee the in-transaction hard checks already provide. Treat the columns as
**logical references validated by checks**.

*Conditional:* if Decision D1 is resolved by making unique ids **stable** (upsert by natural key
instead of restart-identity), then a `NOT VALID` / deferred FK becomes cheap and can be added for
defense-in-depth. Recommend revisiting after D1.

### 9.3 Indexes

- Filtered b-tree on `unique_simple_word_id` `WHERE is_ayah_marker = false AND unique_simple_word_id
  IS NOT NULL` — powers Gate-driven "color all page words of this identity."
- Same for `unique_tashkeel_word_id`.
- Do **not** index the marker rows (all NULL) — the filter keeps the indexes small.

### 9.4 Hard validation checks

See §12 (LINK-READABLE-COMPLETE, LINK-MARKERS-NULL, LINK-RESOLVES, LINK-CONSISTENT).

---

## 10. API / DTO Impact

> No Mushaf/page API exists yet — the API project currently exposes only
> `DashboardController` (`/api/dashboard/info`) and `HealthController`. So all DTO work below is
> **net-new** and should follow `Backend/.architecture/API_GUIDELINES.md` (Arabic-default
> messages, English identifiers, `ApiResponse<T>` envelope). This section is a recommendation for
> the follow-up API feature, not a change to ship in the rebuild work.

### 10.1 Mushaf / page DTO (minimal — coloring)

Per readable word on a page:

- `quran_word_id` — color one exact occurrence.
- Layout/glyph fields already on `quran_words`: `page_number`, `line_number`, `line_word_order`,
  `qpc_glyph`, `text_uthmani` (display), `location`/`verse_key`.
- `unique_tashkeel_word_id` — color same tashkeel form.
- `unique_simple_word_id` — color all no-tashkeel forms.
- **Omit** `word_key_imlaei_simple` (debug/admin only).
- Ayah-marker rows: return as markers with both unique ids `null`.

### 10.2 Word-table / admin DTO

- **Display:** representative `text_uthmani` (+ `qpc_glyph` if added, §7.3).
- **Filtering/identity:** `word_key_imlaei_simple` and `unique_simple_word_id`.
- **Statistics:** `occurrences_count`, `ayahs_count`, `surahs_count`.
- **Linking to Gates/Topics:** the **stable** identity to link against (D1) — recommended
  `unique_simple_word_id` **and/or** the natural `word_key_imlaei_simple`.
- **Provenance:** `first_location` / `first_word_order_in_mushaf` for "jump to first occurrence."

---

## 11. Database Reset / Migration Guidance

### 11.1 Development

**Recommended: drop/reset the dev DB, apply migrations, `import-foundation`, then
`rebuild-words --force`.** Reasons:

- The unique-simple identity change alters which rows exist and their ids; a clean rebuild from a
  freshly imported foundation removes any chance of stale derived state.
- New nullable link columns backfill cleanly on a fresh import + rebuild (no historical rows to
  reconcile).
- Dev data is disposable and reproducible from the import sources — the cheapest path to a known-
  good state.

### 11.2 Production-like

**Do not casually drop.** Use a controlled sequence:

1. Apply the additive migration(s) (new nullable columns / new table — all additive, no data
   loss).
2. Re-run `rebuild-words --force` (idempotent: truncates+rebuilds derived tables, repopulates
   links) during a maintenance window.
3. **If Gates/Topics already reference unique ids**, run the D1 remap/verify step *before*
   exposing the page API, because a rebuild can change unique-simple ids and counts (15,826 →
   14,783). This is the production-critical reason id stability (D1) must be settled first.

Rationale: the schema change is additive and safe to migrate, but the *semantic* change to
unique-simple identity is not value-preserving for any existing links — production needs an
explicit data-migration/verification step, not a blind rebuild.

---

## 12. Validation Plan

Hard checks to run inside the rebuild transaction (commit only if all pass), extending the
existing `DisplayWordsSql` check set:

**Existing, must still hold (unchanged):**

| Check | Expected |
| --- | --- |
| `ORD-READABLE` / total `quran_words` | 77,432 readable / 83,668 total |
| markers | 6,236 |
| `ORD-COUNT` (ordered_tashkeel / ordered_simple rows) | 77,432 each |
| `ORD-NO-MARKERS` | 0 markers in derived tables |
| `ORD-BIJECTION` | ordered ↔ readable 1:1 |
| `STAT-MATCH`, `FIRST-OCC`, `SRC-UNTOUCHED` | pass |

**Changed expectations from the imlaei-simple switch:**

| Check | New expected | Evidence |
| --- | --- | --- |
| `UNQ-COUNT` simple (`unique_simple` rows == distinct key) | **14,783** | measured distinct `word_key_imlaei_simple` |
| `UNQ-EXPECT-SIMPLE` (warning) | now **matches** 14,783 | the prior informational figure finally aligns |
| `UNQ-COUNT` tashkeel | **21,294** (unchanged behavior) | measured distinct `text_uthmani` |
| `UNQ-EXPECT-TASHKEEL` (warning) | still deviates (21,294 vs 21,210) | accepted, §8 |

**New link checks:**

| Check | Expected |
| --- | --- |
| `LINK-READABLE-COMPLETE` | count of readable rows with NULL `unique_tashkeel_word_id` OR NULL `unique_simple_word_id` = **0** |
| `LINK-MARKERS-NULL` | count of marker rows with non-NULL either id = **0** |
| `LINK-RESOLVES` | every non-NULL `unique_*_word_id` resolves to an existing unique row = **0 dangling** |
| `LINK-CONSISTENT` | for each readable row, its `unique_simple` row's key == the row's `word_key_imlaei_simple` (and tashkeel analogue on `text_uthmani`) = **0 mismatches** |

**Spot-value checks (anchors, measured):**

| Identity | Expected occurrences |
| --- | --- |
| `الله` (clean simple) | **2,155** |
| `العظيم` (clean simple) | **36** |
| `الرحمان` (clean simple; display `الرحمن`) | **45** |
| `ال ياسين` | single multi-token key (**1** occurrence) |
| `5:52:12` → `دايرة` | stays `دايرة` |

**Note on `SRC-UNTOUCHED` (Option A only):** redefine it to assert foundation **row counts and
foundation columns** are unchanged, since the rebuild now legitimately writes the two derived
link columns on `quran_words`. Under Option B, `SRC-UNTOUCHED` can stay literally "no write to
`quran_words`."

---

## 13. Proposed Implementation Phases

Do **not** implement yet. Suggested phasing:

1. **Phase 0 — Decisions.** Resolve D1 (id stability) and D2 (Option A vs B) — §14. Everything
   else depends on D1.
2. **Phase 1 — Unique-simple key switch (no links yet).** Change `DisplayWordsSql` unique-simple
   (and ordered-simple stats) to `word_key_imlaei_simple`; add `word_key_imlaei_simple` (+ optional
   representative `qpc_glyph`) to `UniqueSimpleWord`/config; move the unique index. Update
   `UNQ-COUNT`/checks to 14,783. Migration: generated via `./scripts/add-mig` (not applied).
3. **Phase 2 — Identity link schema.** Option A: nullable `unique_tashkeel_word_id` /
   `unique_simple_word_id` + filtered indexes on `quran_words`. Option B: `quran_word_identity_links`
   table + entity + config. Migration generated, not applied.
4. **Phase 3 — Rebuilder link population.** Extend `SqlDisplayWordsRebuilder` with the link
   `UPDATE`/`INSERT` steps (§6) inside the existing transaction.
5. **Phase 4 — Hard checks.** Add `LINK-*` checks + the spot-value anchors; adjust
   `SRC-UNTOUCHED` (Option A).
6. **Phase 5 — Tests.** Real-Postgres rebuild tests (Testcontainers) for: 14,783 unique-simple,
   readable-complete / markers-null links, link resolves+consistent, anchor counts; keep synthetic
   seed data Quran-safe per `test-guard`.
7. **Phase 6 — Reports.** Capture a real `rebuild-words` report (verdict PASS, new counts) and a
   restructure report.
8. **Phase 7 (separate feature) — API/DTO.** Mushaf page + admin word DTOs (§10), following
   `API_GUIDELINES.md`. Out of scope for the rebuild work.

---

## 14. Risks and Open Decisions

**D1 — Surrogate id stability (BLOCKING).** The product wants Gates/Topics to link to
`unique_simple_word_id = N` and the page API to color by `N`. Today the rebuild does
`TRUNCATE … RESTART IDENTITY`, so `N` can change on every rebuild and any stored Gate link would
silently point at the wrong word. **Decision needed:** (a) make unique ids **stable** by
upserting on the natural key (`word_key_imlaei_simple`) instead of restart-identity; (b) link
Gates by the **natural key** (`word_key_imlaei_simple` text) and treat `unique_simple_word_id` as
a per-response convenience only; or (c) accept a mandatory **remap step** after every rebuild.
Recommendation: **(a) or (b)**. This must be settled before Phase 2.

**D2 — Option A vs Option B (§4).** Columns on `quran_words` (best read path, mild foundation
impurity) vs separate `quran_word_identity_links` (purest Clean Architecture, one cheap join).
Recommendation: **A** for page-render performance; **B** if the team wants `quran_words` to stay
strictly foundation-only.

**D3 — `SRC-UNTOUCHED` semantics (Option A).** Rebuild now writes derived columns on
`quran_words`; the "source untouched" guarantee must be redefined to foundation columns/counts,
not "no write." Confirm acceptable.

**D4 — Unique-simple count changes 15,826 → 14,783.** Any existing dashboards, tests, fixtures, or
docs asserting 15,826 (e.g. `003-words-unique-tables-audit.md`) must be updated. The change is
intended, but it is a visible behavioral change.

**D5 — Tashkeel mark pollution (§8).** Leaving `unique_tashkeel` on raw `text_uthmani` keeps ~84
mark-split forms (21,294 vs 21,210). Confirm this is accepted for now (recommended yes; a future
`word_key_uthmani_tashkeel` can address it).

**D6 — Representative display determinism.** Confirm "first occurrence by `word_order_in_mushaf`"
is the desired representative for the Uthmani display label (it is deterministic and already used
for `first_*`). Recommendation: yes.

**D7 — Should `qpc_glyph` be added to `unique_simple`/`unique_tashkeel`?** Enables admin Mushaf-font
rendering without a join. Recommendation: yes (small, additive).

---

## 15. Final Recommendation

Proceed in the phases of §13, but **gate everything on Decision D1 (id stability)** — it is the
only issue that can make the feature quietly incorrect after a future rebuild.

Concretely:

1. **Settle D1 first.** Either make `unique_simple` ids stable (upsert by `word_key_imlaei_simple`)
   or have Gates/Topics link by the natural key. Without this, surrogate-id coloring is fragile.
2. **Switch `unique_simple` identity to `word_key_imlaei_simple`** → 14,783 rows, display stays
   Uthmani, anchors verified (`الله` 2,155, `العظيم` 36, `الرحمان` 45).
3. **Adopt Option A** (nullable `unique_tashkeel_word_id` / `unique_simple_word_id` on
   `quran_words`, filtered indexes, **no hard FK** while truncate-rebuild stands), unless the team
   prefers the purer **Option B** mapping table — confirm D2.
4. **Populate links inside `rebuild-words --force`**, validate with the existing + new `LINK-*`
   hard checks, commit only on all-pass.
5. **Keep `unique_tashkeel` on raw Uthmani** for now (D5 accepted), and treat the Mushaf/admin
   **API/DTO as a separate downstream feature** (§10).
6. **Dev:** drop/reset → migrate → import → rebuild. **Prod-like:** additive migrate → rebuild in
   a window → D1 remap/verify before exposing the page API.

Quranic data safety is preserved throughout: **display remains Uthmani/Mushaf**, identity/stats
move to the clean imlaei key, occurrence coloring uses ids (`quran_word_id`,
`unique_*_word_id`) — never raw word text — and no Quranic content is invented or normalized
beyond the already-reviewed clean-key derivation.
