# Linking persistence — shared read/write pieces

**Layer:** Infrastructure · persistence · **HOW rules:** `Backend/.architecture/CLEAN_ARCHITECTURE.md`

## What this area does

The two pieces the Linking **reader** (`../Reads/Linking/`) and **writer** (`../Writes/Linking/`) both
need. It exists because these are genuinely shared: putting either one in Reads would make the writer
depend on the read side, and putting it in Writes would make the reader depend on the write side. This
is the only folder directly under `Persistence/` besides `Configurations/`, `DataPipelines/`, `Reads/`
and `Writes/`; keep it to pieces that both sides truly share.

## Key pieces

- `LinkingSourceStorage.cs` — the descriptor ↔ storage codec, **both directions in one file on purpose**.
  `Encode` decomposes a typed `LinkingSourceDescriptor` into the stored column set (kind, raw
  `source_identity`, its SHA-256 `source_identity_hash`, label, the `scope` jsonb document, and exactly
  one dimension reference); `Decode` rebuilds the descriptor from those columns plus the source's manual
  verse keys. They are inverse functions and drift silently if separated — a descriptor that encodes into
  columns nothing can decode is only discoverable at read time, on data already written.
- `LinkingWorkspaceProjection.cs` — builds `LinkingWorkspaceDto` from a workspace row. Used by the reader
  for `GET` and by the writer to return post-mutation state, so a load and a mutation can never describe
  the same workspace differently.

## Invariants / caveats (read before changing)

- **Two source-kind vocabularies exist and must not be conflated.** The `source_kind` **column** stores
  snake tokens (`manual_mushaf_ayahs`, `unique_word`, `word_type`, …), owned by
  `../Configurations/Linking/LinkingSourceKindColumn.cs` and used only by the EF value converter and the
  `CHECK`. The **identity string** and the wire use kebab tokens (`manual-mushaf-ayahs`, `unique-word`,
  `word-type`), owned by `LinkingSourceTokens` in Abstractions. `LinkingSourceIdentity` is byte-exact with
  the shipped Frontend and must never be altered to "harmonize" the two.
- **What goes in `scope` is exactly what the columns cannot hold.** The dimension references live in real
  FK columns; `scope` carries only the remainder needed to rebuild the descriptor — `typeCode` for lemma
  and stem, and the full word-type selection (`selectionKind`, the Word arm's `contextCode`/`case`/
  `tense`/`voice`, and the scope fields). It is **not** a general side-channel: anything added here is
  invisible to SQL predicates and to every index.
- **The unique-word mode is DERIVED, not stored.** Which of `unique_simple_word_id` /
  `unique_tashkeel_word_id` is non-null *is* the mode, and the kind/reference coherence `CHECK`
  guarantees exactly one of them is set. Storing the mode in `scope` as well would be a second source of
  truth that a bad write could contradict.
- **`scope` always carries a numeric `schemaVersion` ≥ 1**, matching the `access_audit_events` CHECK
  pattern the database enforces on this column. It is serialized camelCase — the CHECK looks for the key
  `schemaVersion` literally, so changing the naming policy breaks every INSERT.
- **A decode failure throws rather than degrading — with no exceptions.** Stored state that cannot rebuild
  its own descriptor is corruption, and returning a partial descriptor would let the Frontend re-resolve
  the *wrong* source. This holds for **every** field the codec reads, including the Word arm's
  `case`/`tense`/`voice`. Those three once fell back to the first token of their vocabulary
  (`all`/`all`/`all`) when absent from `scope`, which contradicted this rule for the one selection whose
  identity string embeds all three: a source stored without them would have silently decoded into a
  *different* source than the one added. `Encode` cannot produce that state — `LinkingWordTypeSelection.Word`
  runs every token through `LinkingGuard.RequireToken`, so all three are always written — so the throw is
  the assertion of an invariant, not a reachable path, and no stored row can begin to fail on it. Verified
  by round-tripping all ten families (both unique-word modes, all three word-type dimension arms, the
  word-type Word arm, and manual) through add → `GET` with **non-default** tokens
  (`case=accusative tense=imperative voice=passive`): every decoded descriptor's identity string came back
  byte-identical to the stored `source_identity`.
