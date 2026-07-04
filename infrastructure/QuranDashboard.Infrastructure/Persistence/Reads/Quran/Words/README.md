# Words explorer read models

**Layer:** Infrastructure · read-only queries · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`, `API_GUIDELINES.md`

## What this area does

Read-only EF readers behind the five Words explorers — Roots, Lemmas, Stems, WordTypes,
and Unique Words. They back the `application/.../Quran/Words/**` query handlers and the
`api/.../Controllers/Words/*` endpoints. No writes happen here.

## Key pieces

- `EfUniqueWordsReader.cs`, `Roots/EfRootsReader.cs`, `Lemmas/EfLemmasReader.cs`,
  `Stems/EfStemsReader.cs` (+ `.Summary.cs`), `WordTypes/EfWordTypesReader.cs`
  (+ `.Sql.cs`) — the readers. Word-types and stems readers are **partial-split by size**
  (summary vs list/SQL); keep the split when adding to them.
- `*ListDerivation` / `*SummaryRow` — how list rows and summary aggregates are derived.
- `WordTypes/WordTypeIdentityMatcher.cs`, `WordTypeGrouping.cs` — POS/type identity + grouping.
- `MorphologyRelatedItemsOrdering.cs` — shared ordering for related lemmas/stems/roots.
- `ReadPaging.cs` — the paging contract shared by all list endpoints.

## Invariants / caveats (read before changing)

- **Word identity keys on clean imlaei-simple**; the Uthmani form is display only. Two
  words that differ only by tashkeel/Uthmani orthography are the same identity.
- **Ordering is part of the contract** — related-item and list ordering is deterministic
  via `MorphologyRelatedItemsOrdering` / `*ListDerivation`; do not reorder casually.
- **Read-only + `AsNoTracking`** semantics; these readers must not mutate state.
- Response shape/paging must stay aligned with `ReadPaging` and the API contract; changing
  a column or page shape is an API-contract change (update the controller + `API_GUIDELINES`
  expectations and any frontend model).

## Related

- Handlers: `application/QuranDashboard.Application/Quran/Words/**`.
- Frontend consumers: `Frontend/quran-dashboard-ui/src/app/features/words/README.md`.
- Specs: `specs/015-roots-explorer/`, `016-lemmas-stems-explorer/`, `019-word-types-explorer/`,
  `014-words-hub-unique-words/`. (Prior feature-015/016/017 evidence reports were purged.)
