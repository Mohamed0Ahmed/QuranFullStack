# Phase 1 Data Model: Quran Navigation Metadata Foundation

This feature adds **four new tables** and **three additive columns** on the existing `quran_ayahs`. Existing
columns and all other tables are read-only. Naming follows the established `quran_*` snake_case convention;
keys are `smallint` where the value range allows. Domain entities live under
`Quran/Navigation/`; the three ayah columns extend the existing `Ayah` entity.

Source → field mapping is traced from `resources/import-sources/quran-navigation-metadata/sources/*.json`
(see [research.md](./research.md) D1–D6).

---

## Entity: Juz  → table `quran_juzs`

Domain: `Domain/Quran/Navigation/Juz.cs`. EF config: `Configurations/Quran/Navigation/JuzConfiguration.cs`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `juz_number` | smallint | no | **PK**, value-generated-never, range 1..30. From source `juz_number`. |
| `verses_count` | smallint | no | **Computed** from `verse_mapping` ranges (D5). Source `verses_count` is informational. |
| `first_ayah_id` | int | no | FK → `quran_ayahs.id`. Resolved from `first_verse_key`. |
| `last_ayah_id` | int | no | FK → `quran_ayahs.id`. Resolved from `last_verse_key`. |
| `first_verse_key` | text | no | From source `first_verse_key` (e.g. `"1:1"`). |
| `last_verse_key` | text | no | From source `last_verse_key`. |

- Indexes: PK on `juz_number`; FK indexes on `first_ayah_id`, `last_ayah_id`.
- Row count after import: **30**.

## Entity: Hizb → table `quran_hizbs`

Domain: `Domain/Quran/Navigation/Hizb.cs`. EF config: `HizbConfiguration.cs`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `hizb_number` | smallint | no | **PK**, 1..60. From source `hizb_number`. |
| `juz_number` | smallint | no | FK → `quran_juzs.juz_number`. **Derived** by range containment (D6). |
| `verses_count` | smallint | no | Computed from `verse_mapping` ranges (D5). |
| `first_ayah_id` | int | no | FK → `quran_ayahs.id`. |
| `last_ayah_id` | int | no | FK → `quran_ayahs.id`. |
| `first_verse_key` | text | no | From source. |
| `last_verse_key` | text | no | From source. |

- Indexes: PK on `hizb_number`; FK indexes on `juz_number`, `first_ayah_id`, `last_ayah_id`.
- Invariant: exactly **2 hizb per juz** (validated, not assumed). Row count: **60**.

## Entity: Rub → table `quran_rubs`

Domain: `Domain/Quran/Navigation/Rub.cs`. EF config: `RubConfiguration.cs`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `rub_number` | smallint | no | **PK**, 1..240. From source `rub_number`. |
| `hizb_number` | smallint | no | FK → `quran_hizbs.hizb_number`. **Derived** by range containment (D6). |
| `verses_count` | smallint | no | Computed from `verse_mapping` ranges (D5). |
| `first_ayah_id` | int | no | FK → `quran_ayahs.id`. |
| `last_ayah_id` | int | no | FK → `quran_ayahs.id`. |
| `first_verse_key` | text | no | From source. |
| `last_verse_key` | text | no | From source. |

- Indexes: PK on `rub_number`; FK indexes on `hizb_number`, `first_ayah_id`, `last_ayah_id`.
- Invariant: exactly **4 rub per hizb** (validated). Row count: **240**.

## Entity: Sajda → table `quran_sajdas`

Domain: `Domain/Quran/Navigation/Sajda.cs` + enum `SajdahType.cs`. EF config: `SajdaConfiguration.cs`.

| Column | Type | Null | Notes |
|---|---|---|---|
| `sajdah_number` | smallint | no | **PK**, 1..15. From source `sajdah_number`. |
| `ayah_id` | int | no | FK → `quran_ayahs.id` (unique). Resolved from `verse_key`. |
| `verse_key` | text | no | From source `verse_key`. |
| `sajdah_type` | text (enum-backed) | no | `SajdahType` ∈ {`Required`,`Optional`}; stored lowercase `"required"`/`"optional"` (same value-conversion pattern as `Surah.RevelationPlace`). |

- Indexes: PK on `sajdah_number`; unique index on `ayah_id` (one sajda per ayah).
- Distribution (warning-checked, not hard): 11 `optional`, 4 `required`. Row count: **15**.

### Enum: `SajdahType`

```text
SajdahType { Required, Optional }   // domain enum, stored as "required" / "optional"
```

---

## Entity extension: Ayah → `quran_ayahs` (additive columns)

Domain: existing `Domain/Quran/Ayahs/Ayah.cs`. EF config: existing `Configurations/Quran/AyahConfiguration.cs`.
**Only these three columns are added; nothing else on `quran_ayahs` changes — `text_uthmani` is untouched.**

| Column | Type | Null | Notes |
|---|---|---|---|
| `juz_number` | smallint | **yes** (schema) | FK → `quran_juzs.juz_number`. Populated by importer for all 6,236 ayahs. |
| `hizb_number` | smallint | **yes** (schema) | FK → `quran_hizbs.hizb_number`. Populated by importer. |
| `rub_number` | smallint | **yes** (schema) | FK → `quran_rubs.rub_number`. Populated by importer. |

- Nullable at schema level for additive-migration safety (D11); the importer/validator require **non-null
  for all 6,236** after a successful run (`NAV-AYAH-COLUMNS-COMPLETE`). No fake default is written.
- Indexes: non-unique indexes on `juz_number`, `hizb_number`, `rub_number` to serve future ayah→division
  lookups.

---

## Relationships

```text
quran_juzs (1) ──< quran_hizbs (juz_number)
quran_hizbs (1) ──< quran_rubs (hizb_number)
quran_ayahs (1) ──< quran_juzs.first_ayah_id / last_ayah_id   (boundary refs)
quran_ayahs (1) ──< quran_hizbs.first_ayah_id / last_ayah_id
quran_ayahs (1) ──< quran_rubs.first_ayah_id  / last_ayah_id
quran_ayahs (1) ──1 quran_sajdas.ayah_id                       (0..1 sajda per ayah; 15 total)
quran_ayahs.juz_number  ──> quran_juzs.juz_number              (every ayah, after import)
quran_ayahs.hizb_number ──> quran_hizbs.hizb_number
quran_ayahs.rub_number  ──> quran_rubs.rub_number
```

## Derivation: `verse_mapping` → ayah assignments

Source per division: `verse_mapping = { "<surah>": "<from>-<to>", ... }` (inclusive ayah ranges per surah).

For each division record (juz/hizb/rub):
1. For each `(surah, "from-to")` entry, expand to ayah `verse_key`s `surah:from … surah:to`.
2. Resolve each `verse_key` to `quran_ayahs.id`.
3. Assign that division's number to every resolved ayah (`juz_number` / `hizb_number` / `rub_number`).
4. The division's stored `verses_count` = total ayahs expanded across its `verse_mapping` (D5).

Hierarchy (D6): a hizb's `juz_number` = the juz whose ayah-id range fully contains the hizb's range; a rub's
`hizb_number` likewise. Containment must be exact and unique.

## Validation rules (data-level — see [contracts/validation-report.schema.md](./contracts/validation-report.schema.md))

- Counts exact: juz 30, hizb 60, rub 240, sajda 15.
- `*_number` contiguous 1..N, no duplicates.
- Every `first_verse_key`, `last_verse_key`, sajda `verse_key`, and every expanded mapping `verse_key`
  resolves to a `quran_ayahs` row (matches `^\d+:\d+$`).
- Per division type, expanded ranges cover all 6,236 ayahs **exactly once** (no gaps, no overlaps).
- Hierarchy: each hizb ⊂ exactly one juz; each rub ⊂ exactly one hizb.
- `sajdah_type` ∈ {`required`,`optional`}.
- After import: all 6,236 ayahs have non-null `juz_number` / `hizb_number` / `rub_number`.
- Warnings (non-blocking): division source `verses_count` ≠ computed count (carry source value); sajda type
  split ≠ 11 optional / 4 required.

## State / lifecycle

Navigation data has a single lifecycle: **absent → imported (complete)**. There is no per-row mutable state.
Re-import is all-or-nothing: a normal run refuses when any target is populated; a `--force` run atomically
clears and reloads the four tables + the three ayah columns to a state identical to a fresh import.
