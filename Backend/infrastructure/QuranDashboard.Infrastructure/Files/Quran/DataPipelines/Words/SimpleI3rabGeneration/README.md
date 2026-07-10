# Simple I3rab generation (generate pipeline)

**Layer:** Infrastructure · **generate** (not import) · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `LOGGING_GUIDELINES.md`

## What this area does

**Generates** simplified إعراب (i3rab) labels for word segments from a seeded rule catalog —
there is **no external source package**. Each segment is reduced to a signature
(`SegmentSignatureBuilder`) that keys into `I3rabRuleCatalogSeed` (`TryGet(signatureKey)`); the
divine name is special-cased (`AllahLemmaMatcher.IsAllahLemma`); seeded labels are curated by
`I3rabSeedLabelCorrections`; `I3rabAssembler` assembles the rows. CLI: `generate-i3rab`
(no `--source`).

## Key pieces

- `SegmentSignatureBuilder.cs` — segment → deterministic signature key.
- `I3rabRuleCatalogSeed.cs` + `I3rabRuleCatalogSeedData.cs` — the rule catalog (signature → label).
- `I3rabSeedLabelCorrections.cs` — curated corrections to seeded labels.
- `AllahLemmaMatcher.cs` — special-case for the لفظ الجلالة.
- `I3rabAssembler.cs` — builds the generated i3rab rows.

## Current invariants / caveats (read before changing)

- **Labels are generated from a curated catalog, not read from Quran source** — do **not** invent
  or alter religious/Quranic content; changes go through `I3rabSeedLabelCorrections` /
  `I3rabRuleCatalogSeedData`, explicitly.
- **Signature keying must stay deterministic** — the same segment must always map to the same
  rule; changing the signature builder silently reshuffles labels.
- Regenerating replaces prior generated i3rab; it is not an incremental import. Prefer a
  validation/dry pass before a forced regenerate.

## Related

- Write mechanics: `../../../../Persistence/DataPipelines/Quran/Words/SimpleI3rabGeneration/`.
- CLI: `tools/QuranDashboard.DataImporter/README.md`.
