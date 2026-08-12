# Words explorers — reads, identity, counts

Index only — defers to the linked code. See [docs/contracts/README.md](./README.md).

Covers the Roots, Lemmas, Stems, Word Types, and Unique Words explorers. **Word
identity keys, count-family rules, and ordering-as-contract are defined in reader code —
this index does not restate them** (see sources).

## Authoritative sources

- Read models, identity keys, count semantics, ordering → [`Reads/Quran/Words/`](../../Backend/infrastructure/QuranDashboard.Infrastructure/Persistence/Reads/Quran/Words/)
- HTTP endpoints → [`Controllers/Words/`](../../Backend/api/QuranDashboard.Api/Controllers/Words/) and [http-api.md](./http-api.md)
- Frontend explorers (routes, URL-state, cache) → [`features/words/`](../../Frontend/quran-dashboard-ui/src/app/features/words/)

**Precedence:** reader code wins; do not derive identity/count rules from a parallel prose copy.
