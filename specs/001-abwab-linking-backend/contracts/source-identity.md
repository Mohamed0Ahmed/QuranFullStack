# Contract: Source Identity (`LinkingSourceIdentity` ⇄ `linkingSourceKey`)

**This is the single highest-risk contract in the feature.** The Backend canonicalizer
`LinkingSourceIdentity.For(descriptor)` must produce a string **byte-identical** to the Frontend's
`linkingSourceKey(source)`. A silent divergence splits the cache and breaks workspace idempotency
(spec FR-002/FR-003). Authority read directly from:
`Frontend/quran-dashboard-ui/src/app/features/linking/utils/linking-source-key.ts` (+
`utils/manual-link-shape.ts`, `utils/linking-verse-order.ts`,
`models/linking-source.models.ts`).

## Format rules

1. **Join**: parts are joined with a literal `|` (the pipe itself is never escaped — it is the
   separator, applied *after* each part is encoded).
2. **Encoding**: each part is `encodeURIComponent(String(part ?? ''))` — the **JavaScript** escape
   set. Unescaped characters: `A–Z a–z 0–9 - _ . ! ~ * ' ( )`. Everything else becomes `%XX`
   uppercase-hex UTF-8. Example: `:` → `%3A`.
3. **Null/absent** renders as the **empty string** (producing consecutive or trailing pipes —
   they are significant and must be preserved).
4. **Kind tokens** are the Frontend's kebab strings: `manual-mushaf-ayahs`, `unique-word`,
   `root`, `lemma`, `stem`, `word-type`. (The DB `source_kind` column uses snake values — see
   data-model.md — never the identity string.)
5. **Numbers** render in invariant decimal (`String(42)` → `42`).
6. **The label is never part of the identity.**

## Part order per family

| Family | Parts (in order) | Count |
| --- | --- | --- |
| Root | `root` \| rootId | 2 |
| Lemma | `lemma` \| lemmaId \| typeCode (null → empty) | 3 |
| Stem | `stem` \| stemId \| typeCode (null → empty) | 3 |
| Unique Word | `unique-word` \| mode (`simple`/`tashkeel`) \| wordId | 3 |
| Manual Mushaf | `manual-mushaf-ayahs` \| verseKey₁ \| … \| verseKeyₙ | 1+N |
| Word Type (selection `word`) | `word-type` \| `word` \| tashkeelWordId \| contextCode \| case \| tense \| voice \| scope.type \| scope.childCode (null → empty) \| scope.case \| scope.tense \| scope.voice | 12 |
| Word Type (selection `root`/`stem`/`lemma`) | `word-type` \| selectionKind \| id \| scope.type \| scope.childCode (null → empty) \| scope.case \| scope.tense \| scope.voice | 8 |

**Manual verse-key normalization**: the identity uses **only** the verse-key set — de-duplicated,
numerically ordered by `(surah, ayah)` (`orderedUniqueLinkingVerseKeys`). Input order and
duplicates never change the identity. Verse keys have the form `s:a` (`2:255`), so each encodes
with `%3A`.

**Enum vocabularies** (exact tokens, from `linking-source.models.ts`): mode `simple|tashkeel`;
scope/selection type `noun|verb|particle|inl`; case `all|nominative|accusative|genitive|null`
(the literal string `null` is a valid *case token* — distinct from the JS `null` value that
renders empty!); tense `all|past|present|imperative`; voice `all|active|passive`.

## Worked examples (hand-checked against the TypeScript — Phase 1 acceptance repeats this check)

| # | Descriptor | Identity |
| --- | --- | --- |
| 1 | root, rootId 42 | `root\|42` |
| 2 | lemma, lemmaId 7, typeCode null | `lemma\|7\|` |
| 3 | lemma, lemmaId 7, typeCode `N` | `lemma\|7\|N` |
| 4 | stem, stemId 105, typeCode null | `stem\|105\|` |
| 5 | unique-word, mode tashkeel, wordId 3204 | `unique-word\|tashkeel\|3204` |
| 6 | manual, verses entered `[2:255, 1:1, 2:255]` | `manual-mushaf-ayahs\|1%3A1\|2%3A255` |
| 7 | word-type / word: tashkeelWordId 501, contextCode `W:501`, case `all`, tense `past`, voice `active`, scope {verb, childCode null, all, past, all} | `word-type\|word\|501\|W%3A501\|all\|past\|active\|verb\|\|all\|past\|all` |
| 8 | word-type / root: rootId 42, scope {noun, childCode `PN`, nominative, all, all} | `word-type\|root\|42\|noun\|PN\|nominative\|all\|all` |

(Pipes shown escaped `\|` only for this Markdown table — the real strings contain plain `|`.)

## .NET implementation requirement (research.md R1)

`Uri.EscapeDataString` escapes five characters that `encodeURIComponent` does **not**:
`! ' ( ) *`. The implementation must therefore be `Uri.EscapeDataString(part)` followed by
un-escaping `%21→!`, `%27→'`, `%28→(`, `%29→)`, `%2A→*` (or an equivalent custom encoder with the
exact JS set). Both encoders emit uppercase hex and UTF-8 `%XX` sequences, so no other adjustment
is needed. A raw `EscapeDataString` port is **wrong** and will pass every test that uses only
digits and plain tokens — the divergence appears only on codes containing those five characters,
which is why the escape rule must be implemented deliberately, not discovered by testing.

## Storage & uniqueness (research.md R20 — no verse-count cap)

The raw identity is preserved **byte-exactly** in `source_identity text NOT NULL` on both
descriptor-bearing tables (display, debugging, Frontend parity, final equality guard). Because
manual identities grow without bound with the verse set, the raw text is **never** placed in a
btree unique index. Uniqueness uses a fixed-size companion column:

- `source_identity_hash bytea NOT NULL` — the 32-byte SHA-256 digest of the UTF-8 bytes of the
  exact raw identity (no normalization beyond the canonical algorithm above; hash input = the
  identity string verbatim).
- Unique boundaries: `UNIQUE (workspace_id, source_identity_hash)` on workspace sources;
  `UNIQUE (door_id, source_identity_hash) WHERE deleted_at IS NULL` on contributions.
- On collision-sensitive paths (idempotent add, live-contribution matching), after the hash
  lookup the writer compares the raw `source_identity` as the final guard.

**No cap is imposed on the manual verse-set size** — an earlier draft's "≤200 verses per manual
source" recommendation is withdrawn; it was a storage workaround, not a product decision. The
canonical identity **algorithm** above is unchanged in every respect.
