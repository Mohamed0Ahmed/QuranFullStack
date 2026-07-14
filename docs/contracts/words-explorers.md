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
