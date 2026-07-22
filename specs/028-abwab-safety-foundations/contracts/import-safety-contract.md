# Contract: Quran Import Safety & Destructive-Path Lockdown (Story 2)

**Source**: Master Plan §18.2 step 2 / exit. Applies primarily to
`Backend/tools/QuranDashboard.DataImporter` and any force/reseed path.

## Obligations

- **Enumerate** every destructive/force/importer path; **remove or prevent** all
  `TRUNCATE ... CASCADE` effects on Abwab.
- Add a **race-safe dependent lock/preflight**: before any destructive step, acquire a lock
  and check for dependents so a **concurrent dependent creation** cannot be destroyed —
  the import **fails closed** on contention.
- Apply **environment restrictions** and **restricted DB privileges** to import paths.
- **Pin canonical source identity** and verify **stable IDs**; refuse forbidden or
  wrong-identity source packages.

## Gate

- The **first Abwab Quran foreign key is prohibited** until this contract's exit is accepted.

## Test anchors (real PostgreSQL)

- Forbidden-source fixture → **refused** (actual fixtures, not mocks).
- Wrong source identity / unstable IDs → refused.
- Concurrent dependent creation during a destructive import → import **blocked / fails
  closed**, dependents preserved.
- No `TRUNCATE ... CASCADE` reaches Abwab tables.
- Quranic fixtures remain **source-safe** per repo test-data rules.
