# Import pipelines & CLI verbs

Index only — defers to the linked code and retained operator documentation. See [docs/contracts/README.md](./README.md).

Covers the operator-only DataImporter verbs, the file→DB data pipelines, and the
validation / import report outputs. This page does **not** restate verb lists, manifest
schemas, report shapes, or output paths. The retained DataImporter README owns CLI operation;
code owns pipeline implementation and `CODING_PRINCIPLES.md` §10 owns source safety.

## Authoritative sources

- CLI verbs / importer host → [`DataImporter/README.md`](../../Backend/tools/QuranDashboard.DataImporter/README.md)
- Operational rebuild, PhraseSearch build, and canonical-dump runbook → [`Backend/scripts/README.md`](../../Backend/scripts/README.md)
- PhraseSearch strict one-shot, single-data-generation build implementation → [`DataPipelines/Quran/PhraseSearch/`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/PhraseSearch/)
- File data pipelines → [`Files/Quran/DataPipelines/`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Files/Quran/DataPipelines/)
- Persistence data pipelines → [`Persistence/DataPipelines/Quran/`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/DataPipelines/Quran/)

**Precedence:** importer and pipeline code win for implementation; the retained DataImporter README
owns operator commands and safety.
